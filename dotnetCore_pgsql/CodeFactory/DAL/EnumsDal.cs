using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DBHelper;

namespace CodeFactory.DAL
{
	public class EnumsDal
	{
		/// <summary>
		/// 项目名称
		/// </summary>
		static string _projectName = string.Empty;
		/// <summary>
		/// model目录
		/// </summary>
		static string _modelPath = string.Empty;
		/// <summary>
		/// 根目录
		/// </summary>
		static string _rootPath = string.Empty;
		/// <summary>
		/// schemaList
		/// </summary>
		static List<string> _schemaList = new List<string>();
		/// <summary>
		/// 生成枚举数据库枚举类型(覆盖生成)
		/// </summary>
		/// <param name="rootPath">根目录</param>
		/// <param name="modelPath">Model目录</param>
		/// <param name="projectName">项目名称</param>
		public static void Generate(string rootPath, string modelPath, string projectName, List<string> schemaList)
		{
			_schemaList = schemaList;
			_rootPath = rootPath;
			_modelPath = modelPath;
			_projectName = projectName;
			var listEnum = GenerateEnum();
			var listComposite = GenerateComposites();

			GenerateMapping(listEnum, listComposite);
			GenerateRedisHepler(); //Create RedisHelper.cs
			GenerateCsproj();
		}
		/// <summary>
		/// 构建Enum文件(Model/_Enums.cs)
		/// </summary>
		/// <returns></returns>
		private static List<EnumTypeInfo> GenerateEnum()
		{
			var list = SQL.Select("a.oid, a.typname, b.nspname").From("pg_type", "a").InnerJoin("pg_namespace", "b", "a.typnamespace = b.oid").Where("a.typtype='e'").OrderBy("oid asc").ToList<EnumTypeInfo>();
			string fileName = Path.Combine(_modelPath, "_Enums.cs");
			using (StreamWriter writer = new StreamWriter(File.Create(fileName), Encoding.UTF8))
			{
				writer.WriteLine("using System;");
				writer.WriteLine();
				writer.WriteLine($"namespace {_projectName}.Model");
				writer.WriteLine("{");
				foreach (var item in list)
				{
					var enums = SQL.Select("enumlabel").From("pg_enum").Where($"enumtypid={item.Oid}").OrderBy("oid asc").ToList<string>();
					if (enums.Count > 0)
						enums[0] += " = 1";
					writer.WriteLine($"\tpublic enum {Types.DeletePublic(item.Nspname, item.Typname)}");
					writer.WriteLine("\t{");
					writer.WriteLine($"\t\t{string.Join(", ", enums)}");
					writer.WriteLine("\t}");

				}
				writer.WriteLine("}");
			}
			return list;
		}
		/// <summary>
		/// 构建覆盖Composites(Model/_Composites.cs)
		/// </summary>
		/// <returns></returns>
		public static List<CompositeTypeInfo> GenerateComposites()
		{
			var notCreateComposites = new[] { "'public.reclassarg'", "'public.geomval'", "'public.addbandarg'", "'public.agg_samealignment'", "'public.geometry_dump'", "'public.summarystats'", "'public.agg_count'", "'public.valid_detail'", "'public.rastbandarg'", "'public.unionarg'", "'topology.getfaceedges_returntype'", "'topology.topogeometry'", "'topology.validatetopology_returntype'", "'public.stdaddr'", "'tiger.norm_addy'" };
			var compositesSQL = SQL.Select("ns.nspname, a.typname as typename, c.attname, d.typname, c.attndims, d.typtype").From("pg_type")
				.InnerJoin("pg_class", "b", "b.reltype = a.oid and b.relkind = 'c'")
				.InnerJoin("pg_attribute", "c", "c.attrelid = b.oid and c.attnum > 0")
				.InnerJoin("pg_type", "d", "d.oid = c.atttypid")
				.InnerJoin("pg_namespace", "ns", "ns.oid = a.typnamespace")
				.LeftJoin("pg_namespace", "ns2", "ns2.oid = d.typnamespace")
				.WhereNotIn("ns.nspname || '.' || a.typname", notCreateComposites).ToString();
			Dictionary<string, string> dic = new Dictionary<string, string>();
			List<CompositeTypeInfo> composites = new List<CompositeTypeInfo>();
			var isFoot = false;
			PgSqlHelper.ExecuteDataReader(dr =>
		   {
			   var composite = new CompositeTypeInfo
			   {
				   Nspname = dr["nspname"].ToEmptyOrString(),
				   Typname = dr["typename"].ToEmptyOrString(),
			   };
			   var temp = $"{composite.Nspname}.{composite.Typname}";

			   if (!dic.ContainsKey(temp))
			   {
				   var str = "";
				   if (isFoot)
				   {
					   str += "\t}\n";
					   isFoot = false;
				   }
				   str += $"\t[JsonObject(MemberSerialization.OptIn)]\n";
				   str += $"\tpublic partial struct {Types.DeletePublic(composite.Nspname, composite.Typname)}\n";
				   str += "\t{";
				   dic.Add(temp, str);
				   composites.Add(composite);
			   }
			   else isFoot = true;
			   var isArray = Convert.ToInt16(dr["attndims"]) > 0;
			   string _type = Types.ConvertPgDbTypeToCSharpType(dr["typtype"].ToString(), dr["typname"].ToString());
			   var _notnull = string.Empty;
			   if (_type != "string" && _type != "JToken" && _type != "byte[]" && !isArray && _type != "object")
				   _notnull = "?";
			   string _array = isArray ? "[]" : "";
			   var relType = $"{_type}{_notnull}{_array}";
			   dic[temp] += $"\n\t\t[JsonProperty] public {relType} {dr["attname"].ToString().ToUpperPascal()} {{ get; set; }}";

		   }, compositesSQL);
			string fileName = Path.Combine(_modelPath, "_Composites.cs");
			using (StreamWriter writer = new StreamWriter(File.Create(fileName), Encoding.UTF8))
			{
				writer.WriteLine("using System;");
				writer.WriteLine("using Newtonsoft.Json;");
				writer.WriteLine();
				writer.WriteLine($"namespace {_projectName}.Model");
				writer.WriteLine("{");
				using (var e = dic.GetEnumerator())
				{
					while (e.MoveNext())
						writer.WriteLine(e.Current.Value);
					if (dic.Keys.Count > 0)
						writer.WriteLine("\t}");
				}
				writer.WriteLine("}");
			}
			return composites;
		}
		/// <summary>
		/// 生成初始化文件(覆盖生成)
		/// </summary>
		/// <param name="list"></param>
		public static void GenerateMapping(List<EnumTypeInfo> list, List<CompositeTypeInfo> listComposite)
		{
			string fileName = Path.Combine(_rootPath, "_Startup.cs");
			using (StreamWriter writer = new StreamWriter(File.Create(fileName), Encoding.UTF8))
			{
				writer.WriteLine($"using {_projectName}.Model;");
				writer.WriteLine("using System;");
				writer.WriteLine("using Microsoft.Extensions.Logging;");
				writer.WriteLine("using DBHelper;");
				writer.WriteLine("using Npgsql;");
				writer.WriteLine();
				writer.WriteLine($"namespace {_projectName}");
				writer.WriteLine("{");
				writer.WriteLine("\tpublic class _Startup");
				writer.WriteLine("\t{");
				writer.WriteLine();
				writer.WriteLine("\t\t/// <summary>");
				writer.WriteLine("\t\t/// 初始化数据库连接");
				writer.WriteLine("\t\t/// </summary>");
				writer.WriteLine("\t\t/// <param name=\"connectionSring\">读写数据库连接字符串</param>");
				writer.WriteLine("\t\t/// <param name=\"logger\">数据库语句执行日志</param>");
				writer.WriteLine("\t\t/// <param name=\"slaveConnectionString\">只读数据库连接字符串(为空直接调用读写数据库)</param>");
				writer.WriteLine("\t\tpublic static void Init(string connectionSring, ILogger logger, string slaveConnectionString = \"\")");
				writer.WriteLine("\t\t{");
				writer.WriteLine("\t\t\tPgSqlHelper.InitDBConnection(connectionSring, logger, slaveConnectionString);");
				writer.WriteLine();
				writer.WriteLine("\t\t\tNpgsqlNameTranslator translator = new NpgsqlNameTranslator();");
				writer.WriteLine("\t\t\tNpgsqlConnection.GlobalTypeMapper.UseJsonNet();");
				foreach (var item in list)
					writer.WriteLine($"\t\t\tNpgsqlConnection.GlobalTypeMapper.MapEnum<{Types.DeletePublic(item.Nspname, item.Typname)}>(\"{item.Nspname}.{item.Typname}\", translator);");
				foreach (var item in listComposite)
					writer.WriteLine($"\t\t\tNpgsqlConnection.GlobalTypeMapper.MapComposite<{Types.DeletePublic(item.Nspname, item.Typname)}>(\"{item.Nspname}.{item.Typname}\");");
				writer.WriteLine("\t\t}");
				writer.WriteLine("\t}");
				writer.WriteLine("\tpublic partial class NpgsqlNameTranslator : INpgsqlNameTranslator");
				writer.WriteLine("\t{");
				writer.WriteLine("\t\tpublic string TranslateMemberName(string clrName) => clrName;");
				writer.WriteLine("\t\tpublic string TranslateTypeName(string clrName) => clrName;");
				writer.WriteLine("\t}");
				writer.WriteLine("}"); // namespace end
			}
		}
		/// <summary>
		/// Generate project file({projectName}.csproj).
		/// </summary>
		public static void GenerateCsproj()
		{
			string csproj = Path.Combine(_rootPath, $"{_projectName}.db.csproj");
			if (File.Exists(csproj))
				return;
			using (StreamWriter writer = new StreamWriter(File.Create(csproj), Encoding.UTF8))
			{
				writer.WriteLine(@"<Project Sdk=""Microsoft.NET.Sdk"">");
				writer.WriteLine();
				writer.WriteLine("\t<PropertyGroup>");
				writer.WriteLine("\t\t<TargetFramework>netcoreapp2.1</TargetFramework>");
				writer.WriteLine("\t</PropertyGroup>");
				writer.WriteLine();
				writer.WriteLine("\t<ItemGroup>");

				writer.WriteLine("\t\t<Folder Include= \"DAL\\Build\\\" />");
				writer.WriteLine("\t\t<Folder Include= \"Model\\Build\\\" />");

				writer.WriteLine("\t</ItemGroup>");
				writer.WriteLine();
				writer.WriteLine("\t<ItemGroup>");
				writer.WriteLine("\t\t<ProjectReference Include=\"..\\Common\\Common.csproj\" />");
				//writer.WriteLine("<ProjectReference Include=""..\Infrastructure\Infrastructure.csproj"" />");
				writer.WriteLine("\t</ItemGroup>");
				writer.WriteLine();
				writer.WriteLine("</Project>");
			}
		}
		/// <summary>
		/// 生成RedisHelper.cs(存在不生成)
		/// </summary>
		public static void GenerateRedisHepler()
		{
			string fileName = Path.Combine(_rootPath, "RedisHelper.cs");
			if (File.Exists(fileName)) return;
			using (StreamWriter writer = new StreamWriter(File.Create(fileName), Encoding.UTF8))
			{
				writer.WriteLine("using System;");
				writer.WriteLine("using System.Collections.Generic;");
				writer.WriteLine("using Microsoft.Extensions.Configuration;");
				writer.WriteLine("using StackExchange.Redis;");
				writer.WriteLine("");
				writer.WriteLine($"namespace {_projectName}.db");
				writer.WriteLine("{");
				writer.WriteLine("\tpublic class RedisHelper");
				writer.WriteLine("\t{");
				writer.WriteLine("\t\tpublic static IConfiguration Configuration { get; internal set; }");
				writer.WriteLine("\t\tpublic static void InitializeConfiguration(IConfiguration cfg)");
				writer.WriteLine("\t\t{");
				//note: 
				writer.WriteLine("/*");
				writer.WriteLine("appsetting.json里面添加");
				writer.WriteLine("\"ConnectionStrings\": {");
				writer.WriteLine("\t\"redis\": \"127.0.0.1:6379,defaultDatabase=13,name = dev,abortConnect=false,password=123456\",");
				writer.WriteLine("}");
				writer.WriteLine("*/");

				writer.WriteLine("\t\t\tConfiguration = cfg;");
				writer.WriteLine("\t\t\tMultiplexer = ConnectionMultiplexer.Connect(cfg[\"ConnectionStrings:redis\"]);");
				writer.WriteLine("\t\t}");
				writer.WriteLine("\t\tpublic static IDatabase DbClient => Multiplexer.GetDatabase();");
				writer.WriteLine("\t\tpublic static ConnectionMultiplexer Multiplexer { get; internal set; }");
				writer.WriteLine("\t\tpublic static IDatabase GetDatabase(int db = -1) => Multiplexer.GetDatabase(db);");
				writer.WriteLine("\t}");
				writer.WriteLine("}");
			};
		}
	}
}
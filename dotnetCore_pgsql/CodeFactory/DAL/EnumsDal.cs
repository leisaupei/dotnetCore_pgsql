using CodeFactory.Extension;
using DBHelper;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

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
		/// 数据库类别名称
		/// </summary>
		static string _typeName = string.Empty;
		/// <summary>
		/// 生成枚举数据库枚举类型(覆盖生成)
		/// </summary>
		/// <param name="rootPath">根目录</param>
		/// <param name="modelPath">Model目录</param>
		/// <param name="projectName">项目名称</param>
		public static void Generate(string rootPath, string modelPath, string projectName, List<string> schemaList, string typeName)
		{
			_typeName = typeName;
			_schemaList = schemaList;
			_rootPath = rootPath;
			_modelPath = modelPath;
			_projectName = projectName;
			var listEnum = GenerateEnum();
			var listComposite = GenerateComposites();

			GenerateMapping(listEnum, listComposite);
			//GenerateRedisHepler(); //Create RedisHelper.cs
			GenerateCsproj();
		}

		private static List<EnumTypeInfo> GenerateEnum()
		{
			var list = SQL.Select("a.oid, a.typname, b.nspname").From("pg_type", "a").InnerJoin("pg_namespace", "b", "a.typnamespace = b.oid").Where("a.typtype='e'").OrderBy("oid asc").ToList<EnumTypeInfo>();
			string fileName = Path.Combine(_modelPath, $"_{TypeName}Enums.cs");
			using (StreamWriter writer = new StreamWriter(File.Create(fileName), Encoding.UTF8))
			{
				writer.WriteLine("using System;");
				writer.WriteLine();
				writer.WriteLine($"namespace {_projectName}.Model{NamespaceTypeName}");
				writer.WriteLine("{");
				foreach (var item in list)
				{
					var enums = SQL.Select("enumlabel").From("pg_enum").Where($"enumtypid={item.Oid}").OrderBy("oid asc").ToList<string>();
					if (enums.Count > 0)
						enums[0] += " = 1";
					writer.WriteLine($"\tpublic enum {TypeName}{Types.DeletePublic(item.Nspname, item.Typname)}");
					writer.WriteLine("\t{");
					writer.WriteLine($"\t\t{string.Join(", ", enums)}");
					writer.WriteLine("\t}");

				}
				writer.WriteLine("}");
			}
			return list;
		}

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
				   Nspname = dr["nspname"]?.ToString(),
				   Typname = dr["typename"]?.ToString(),
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
			   if (_type != "string" && _type != "JToken" && _type != "byte[]" && !isArray && _type != "object" && _type != "IPAdress")
				   _notnull = "?";
			   string _array = isArray ? "[]" : "";
			   var relType = $"{_type}{_notnull}{_array}";
			   dic[temp] += $"\n\t\t[JsonProperty] public {relType} {dr["attname"].ToString().ToUpperPascal()} {{ get; set; }}";

		   }, compositesSQL);
			string fileName = Path.Combine(_modelPath, $"_{TypeName}Composites.cs");
			using (StreamWriter writer = new StreamWriter(File.Create(fileName), Encoding.UTF8))
			{
				writer.WriteLine("using System;");
				writer.WriteLine("using Newtonsoft.Json;");
				writer.WriteLine();
				writer.WriteLine($"namespace {_projectName}.Model{NamespaceTypeName}");
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
		static string NamespaceTypeName => _typeName == GenerateModel.MASTER_DATABASE_TYPE_NAME ? "" : "." + _typeName;
		static string TypeName => _typeName == GenerateModel.MASTER_DATABASE_TYPE_NAME ? "" : _typeName;
		/// <summary>
		/// 生成初始化文件(覆盖生成)
		/// </summary>
		/// <param name="list"></param>
		public static void GenerateMapping(List<EnumTypeInfo> list, List<CompositeTypeInfo> listComposite)
		{
			var startupRoot = Path.Combine(_rootPath, "Startup");
			if (!Directory.Exists(startupRoot))
				Directory.CreateDirectory(startupRoot);
			var fileName = Path.Combine(startupRoot, $"_{TypeName}Startup.cs");
			using (StreamWriter writer = new StreamWriter(File.Create(fileName), Encoding.UTF8))
			{
				writer.WriteLine($"using {_projectName}.Model{NamespaceTypeName};");
				writer.WriteLine("using System;");
				writer.WriteLine("using Microsoft.Extensions.Logging;");
				writer.WriteLine("using DBHelper;");
				writer.WriteLine("using Npgsql;");
				writer.WriteLine();
				writer.WriteLine($"namespace {_projectName}.Startup");
				writer.WriteLine("{");
				writer.WriteLine($"\tpublic class _{TypeName}Startup");
				writer.WriteLine("\t{");
				writer.WriteLine();
				writer.WriteLine("\t\t/// <summary>");
				writer.WriteLine("\t\t/// 初始化数据库连接 如果报错需要仿照PgSqlHelper.InitDBConnection创建方法 用新的实例接收");
				writer.WriteLine("\t\t/// </summary>");
				writer.WriteLine("\t\t/// <param name=\"connectionString\">主库</param>");
				writer.WriteLine("\t\t/// <param name=\"logger\">数据库语句执行日志</param>");
				writer.WriteLine("\t\t/// <param name=\"slaveConnectionString\">从库(为空直接调用主库)</param>");
				writer.WriteLine("\t\tpublic static void Init(string connectionString, ILogger logger, string[] slaveConnectionString = null)");
				writer.WriteLine("\t\t{");
				writer.WriteLine("\t\t\tNpgsqlNameTranslator translator = new NpgsqlNameTranslator();");
				writer.WriteLine($"\t\t\tPgSqlHelper.{TypeName}InitDBConnection(connectionString, logger, slaveConnectionString, mapAction: conn =>");
				writer.WriteLine("\t\t\t{");
				writer.WriteLine("\t\t\t\tconn.TypeMapper.UseJsonNet();");
				foreach (var item in list)
					writer.WriteLine($"\t\t\t\tconn.TypeMapper.MapEnum<{TypeName}{Types.DeletePublic(item.Nspname, item.Typname)}>(\"{item.Nspname}.{item.Typname}\", translator);");
				//foreach (var item in listComposite)
				//	writer.WriteLine($"\t\t\t\tconn.TypeMapper.MapComposite<{TypeName}{Types.DeletePublic(item.Nspname, item.Typname)}>(\"{item.Nspname}.{item.Typname}\");");
				writer.WriteLine("\t\t\t});");
				writer.WriteLine("\t\t}");
				writer.WriteLine("\t}");
				writer.WriteLine("}"); // namespace end
			}
		}

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
				writer.WriteLine("\t\t<TargetFramework>netstandard2.1</TargetFramework>");
				writer.WriteLine("\t</PropertyGroup>");
				writer.WriteLine();
				writer.WriteLine("\t<ItemGroup>");

				writer.WriteLine("\t\t<Folder Include= \"DAL\\Build\\\" />");
				writer.WriteLine("\t\t<Folder Include= \"Model\\Build\\\" />");

				writer.WriteLine("\t</ItemGroup>");
				writer.WriteLine();
				writer.WriteLine("\t<ItemGroup>");
				writer.WriteLine("\t\t<ProjectReference Include=\"..\\Common\\Common.csproj\" />");
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
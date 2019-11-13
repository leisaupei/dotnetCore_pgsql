using CodeFactory.Extension;
using Meta.Common.DBHelper;
using Meta.Common.SqlBuilder;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace CodeFactory.DAL
{
	/// <summary>
	/// 
	/// </summary>
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
		/// 数据库类别名称
		/// </summary>
		static string _typeName = string.Empty;

		static StringBuilder _sbConstTypeName = new StringBuilder();

		static StringBuilder _sbConstTypeConstrutor = new StringBuilder();
		/// <summary>
		/// 生成枚举数据库枚举类型(覆盖生成)
		/// </summary>
		/// <param name="rootPath">根目录</param>
		/// <param name="modelPath">Model目录</param>
		/// <param name="projectName">项目名称</param>
		/// <param name="typeName">多库标签</param>
		public static void Generate(string rootPath, string modelPath, string projectName, string typeName)
		{
			_typeName = typeName;
			_rootPath = rootPath;
			_modelPath = modelPath;
			_projectName = projectName;
			var listEnum = GenerateEnum();
			var listComposite = new List<CompositeTypeInfo>();  // GenerateComposites();

			GenerateMapping(listEnum, listComposite);
			GenerateCsproj();
		}

		private static List<EnumTypeInfo> GenerateEnum()
		{
			var list = SqlInstance.Select("a.oid, a.typname, b.nspname").From("pg_type", "a").InnerJoin("pg_namespace", "b", "a.typnamespace = b.oid").Where("a.typtype='e'").OrderBy("oid asc").ToList<EnumTypeInfo>();
			string fileName = Path.Combine(_modelPath, $"_{TypeName}Enums.cs");
			using (StreamWriter writer = new StreamWriter(File.Create(fileName), Encoding.UTF8))
			{
				writer.WriteLine("using System;");
				writer.WriteLine();
				writer.WriteLine($"namespace {_projectName}.Model");
				writer.WriteLine("{");
				foreach (var item in list)
				{
					var enums = SqlInstance.Select("enumlabel").From("pg_enum").Where($"enumtypid={item.Oid}").OrderBy("oid asc").ToList<string>();
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
		/// <summary>
		/// 
		/// </summary>
		/// <returns></returns>
		public static List<CompositeTypeInfo> GenerateComposites()
		{
			var notCreateComposites = new[] { "'public.reclassarg'", "'public.geomval'", "'public.addbandarg'", "'public.agg_samealignment'", "'public.geometry_dump'", "'public.summarystats'", "'public.agg_count'", "'public.valid_detail'", "'public.rastbandarg'", "'public.unionarg'", "'topology.getfaceedges_returntype'", "'topology.topogeometry'", "'topology.validatetopology_returntype'", "'public.stdaddr'", "'tiger.norm_addy'" };
			var compositesSQL = SqlInstance.Select("ns.nspname, a.typname as typename, c.attname, d.typname, c.attndims, d.typtype").From("pg_type")
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
		/// <param name="listComposite"></param>
		public static void GenerateMapping(List<EnumTypeInfo> list, List<CompositeTypeInfo> listComposite)
		{
			_sbConstTypeName.AppendFormat("\t\t/// <summary>\n\t\t/// {0}主库\n\t\t/// </summary>\n", TypeName);
			_sbConstTypeName.AppendFormat("\t\tpublic const string {0}Master = \"{1}\";\n", TypeName.ToUpperPascal(), _typeName.ToLower());
			_sbConstTypeName.AppendFormat("\t\t/// <summary>\n\t\t/// {0}从库\n\t\t/// </summary>\n", TypeName);
			_sbConstTypeName.AppendFormat("\t\tpublic const string {0}Slave = \"{1}\";\n", TypeName.ToUpperPascal(), _typeName.ToLower() + PgSqlHelper.SlaveSuffix);

			_sbConstTypeConstrutor.AppendFormat("\t\t#region {0}\n", _typeName);
			_sbConstTypeConstrutor.AppendFormat("\t\tpublic class {0}DbOption : BaseDbOption\n", _typeName.ToUpperPascal());
			_sbConstTypeConstrutor.AppendLine("\t\t{");
			_sbConstTypeConstrutor.AppendFormat("\t\t\tpublic {0}DbOption(string connectionString, string[] slaveConnectionString, ILogger logger) : base({1}Master, connectionString, slaveConnectionString, logger)\n", _typeName.ToUpperPascal(), TypeName);
			_sbConstTypeConstrutor.AppendLine("\t\t\t{");
			_sbConstTypeConstrutor.AppendLine("\t\t\t\tNpgsqlNameTranslator translator = new NpgsqlNameTranslator();");
			_sbConstTypeConstrutor.AppendLine("\t\t\t\tMapAction = conn =>");
			_sbConstTypeConstrutor.AppendLine("\t\t\t\t{");
			_sbConstTypeConstrutor.AppendLine("\t\t\t\t\tconn.TypeMapper.UseJsonNet();");
			foreach (var item in list)
				_sbConstTypeConstrutor.AppendLine($"\t\t\t\t\tconn.TypeMapper.MapEnum<{TypeName}{Types.DeletePublic(item.Nspname, item.Typname)}>(\"{item.Nspname}.{item.Typname}\", translator);");
			foreach (var item in listComposite)
				_sbConstTypeConstrutor.AppendLine($"\t\t\t\t\tconn.TypeMapper.MapComposite<{TypeName}{Types.DeletePublic(item.Nspname, item.Typname)}>(\"{item.Nspname}.{item.Typname}\");");
			_sbConstTypeConstrutor.AppendLine("\t\t\t\t};");
			_sbConstTypeConstrutor.AppendLine("\t\t\t}");
			_sbConstTypeConstrutor.AppendLine("\t\t}");
			_sbConstTypeConstrutor.AppendLine("\t\t#endregion\n");
			if (_typeName == LetsGo.FinalType)
			{
				var startupRoot = Path.Combine(_rootPath, "Options");
				if (!Directory.Exists(startupRoot))
					Directory.CreateDirectory(startupRoot);
				var fileName = Path.Combine(startupRoot, $"DbOptions.cs");
				using StreamWriter writer = new StreamWriter(File.Create(fileName), Encoding.UTF8);
				writer.WriteLine($"using {_projectName}.Model;");
				writer.WriteLine("using System;");
				writer.WriteLine("using Microsoft.Extensions.Logging;");
				writer.WriteLine("using Meta.Common.Model;");
				writer.WriteLine("using Meta.Common.DBHelper;");
				writer.WriteLine("using Npgsql;");
				writer.WriteLine();
				writer.WriteLine($"namespace {_projectName}.Options");
				writer.WriteLine("{");
				writer.WriteLine($"\tpublic class DbOptions");
				writer.WriteLine("\t{");
				writer.Write(_sbConstTypeName);
				writer.WriteLine();
				writer.Write(_sbConstTypeConstrutor);
				writer.WriteLine("\t}");
				writer.WriteLine("}"); // namespace end
			}
		}
		/// <summary>
		/// 
		/// </summary>
		public static void GenerateCsproj()
		{
			string csproj = Path.Combine(_rootPath, $"{_projectName}.db.csproj");
			if (File.Exists(csproj))
				return;
			using StreamWriter writer = new StreamWriter(File.Create(csproj), Encoding.UTF8);
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
			writer.WriteLine("\t\t<ProjectReference Include=\"..\\Meta.Common\\Meta.Common.csproj\" />");
			writer.WriteLine("\t</ItemGroup>");
			writer.WriteLine();
			writer.WriteLine("</Project>");
		}
	}
}
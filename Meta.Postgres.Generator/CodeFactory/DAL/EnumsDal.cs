using Meta.Common.DbHelper;
using Meta.Common.Model;
using Meta.Common.SqlBuilder;
using Meta.Postgres.Generator.CodeFactory.Extension;
using Npgsql;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Meta.Postgres.Generator.CodeFactory.DAL
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

		static readonly StringBuilder _sbConstTypeName = new StringBuilder();

		static readonly StringBuilder _sbConstTypeConstrutor = new StringBuilder();
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
			var listComposite = GenerateComposites();

			GenerateMapping(listEnum, listComposite);
		}


		private static List<EnumTypeInfo> GenerateEnum()
		{
			var sql = $@"
SELECT a.oid, a.typname, b.nspname FROM pg_type a  
INNER JOIN pg_namespace b ON a.typnamespace = b.oid 
WHERE a.typtype='e'  
ORDER BY oid asc  
";
			var list = PgsqlHelper.ExecuteDataReaderList<EnumTypeInfo>(sql);
			string fileName = Path.Combine(_modelPath, $"_{TypeName}Enums.cs");
			using (StreamWriter writer = new StreamWriter(File.Create(fileName), Encoding.UTF8))
			{
				writer.WriteLine("using System;");
				writer.WriteLine();
				writer.WriteLine($"namespace {_projectName}.Model");
				writer.WriteLine("{");
				foreach (var item in list)
				{
					var sqlEnums = $@"SELECT enumlabel FROM pg_enum a  WHERE enumtypid=@oid ORDER BY oid asc";
					var enums = PgsqlHelper.ExecuteDataReaderList<string>(sqlEnums, System.Data.CommandType.Text, new[] { new NpgsqlParameter("oid", item.Oid) });
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
			var notCreateComposites = new[] { "public.reclassarg", "public.geomval", "public.addbandarg", "public.agg_samealignment", "public.geometry_dump", "public.summarystats", "public.agg_count", "public.valid_detail", "public.rastbandarg", "public.unionarg", "topology.getfaceedges_returntype", "topology.topogeometry", "topology.validatetopology_returntype", "public.stdaddr", "tiger.norm_addy" };
			var sql = $@"
SELECT ns.nspname, a.typname as typename, c.attname, d.typname, c.attndims, d.typtype
FROM pg_type a 
INNER JOIN pg_class b on b.reltype = a.oid and b.relkind = 'c'
INNER JOIN pg_attribute c on c.attrelid = b.oid and c.attnum > 0
INNER JOIN pg_type d on d.oid = c.atttypid
INNER JOIN pg_namespace ns on ns.oid = a.typnamespace
LEFT JOIN pg_namespace ns2 on ns2.oid = d.typnamespace
WHERE ns.nspname || '.' || a.typname not in ({Types.ConvertArrayToSql(notCreateComposites)})
";
			Dictionary<string, string> dic = new Dictionary<string, string>();
			List<CompositeTypeInfo> composites = new List<CompositeTypeInfo>();
			var isFoot = false;
			PgsqlHelper.ExecuteDataReader(dr =>
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

		   }, sql);

			if (dic.Count > 0)
			{
				string fileName = Path.Combine(_modelPath, $"_{TypeName}Composites.cs");
				using StreamWriter writer = new StreamWriter(File.Create(fileName), Encoding.UTF8);
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
		/// 
		/// </summary>
		public static List<string> XmlTypeName { get; } = new List<string>();

		/// <summary>
		/// 
		/// </summary>
		public static List<string> GeometryTableTypeName { get; } = new List<string>();

		/// <summary>
		/// 生成初始化文件(覆盖生成)
		/// </summary>
		/// <param name="list"></param>
		/// <param name="listComposite"></param>
		public static void GenerateMapping(List<EnumTypeInfo> list, List<CompositeTypeInfo> listComposite)
		{
			_sbConstTypeName.AppendFormat("\t/// <summary>\n\t/// {0}主库\n\t/// </summary>\n", TypeName);
			_sbConstTypeName.AppendFormat("\tpublic struct Db{0} : IDbName {{ }}\n", _typeName.ToUpperPascal());
			_sbConstTypeName.AppendFormat("\t/// <summary>\n\t/// {0}从库\n\t/// </summary>\n", TypeName);
			_sbConstTypeName.AppendFormat("\tpublic struct Db{0} : IDbName {{ }}\n", _typeName.ToUpperPascal() + PgsqlHelper.SlaveSuffix);

			_sbConstTypeConstrutor.AppendFormat("\t\t#region {0}\n", _typeName);
			_sbConstTypeConstrutor.AppendFormat("\t\tpublic class {0}DbOption : BaseDbOption<Db{0}, Db{1}>\n", _typeName.ToUpperPascal(), _typeName.ToUpperPascal() + PgsqlHelper.SlaveSuffix);
			_sbConstTypeConstrutor.AppendLine("\t\t{");
			_sbConstTypeConstrutor.AppendFormat("\t\t\tpublic {0}DbOption(string masterConnectionString, string[] slaveConnectionStrings, ILogger logger) : base(masterConnectionString, slaveConnectionStrings, logger)\n", _typeName.ToUpperPascal(), TypeName);
			_sbConstTypeConstrutor.AppendLine("\t\t\t{");
			_sbConstTypeConstrutor.AppendLine("\t\t\t\tOptions.MapAction = conn =>");
			_sbConstTypeConstrutor.AppendLine("\t\t\t\t{");
			_sbConstTypeConstrutor.AppendLine("\t\t\t\t\tconn.TypeMapper.UseJsonNetForJtype();");
			if (XmlTypeName.Contains(_typeName))
				_sbConstTypeConstrutor.AppendLine("\t\t\t\t\tconn.TypeMapper.UseCustomXml();");
			if (GeometryTableTypeName.Contains(_typeName))
				_sbConstTypeConstrutor.AppendLine("\t\t\t\t\tconn.TypeMapper.UseLegacyPostgis();");
			foreach (var item in list)
				_sbConstTypeConstrutor.AppendLine($"\t\t\t\t\tconn.TypeMapper.MapEnum<{TypeName}{Types.DeletePublic(item.Nspname, item.Typname)}>(\"{item.Nspname}.{item.Typname}\", _translator);");
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
				writer.WriteLine("using Meta.Common.DbHelper;");
				writer.WriteLine("using Newtonsoft.Json.Linq;");
				writer.WriteLine("using Npgsql.TypeMapping;");
				writer.WriteLine("using Meta.Common.Extensions;");
				writer.WriteLine("using Npgsql;");
				writer.WriteLine("using Meta.Common.Interface; ");
				writer.WriteLine();
				writer.WriteLine($"namespace {_projectName}.Options");
				writer.WriteLine("{");
				writer.WriteLine("\t#region DbTypeName");
				writer.Write(_sbConstTypeName);
				writer.WriteLine("\t#endregion");
				writer.WriteLine($"\t/// <summary>");
				writer.WriteLine($"\t/// 由生成器生成, 会覆盖");
				writer.WriteLine($"\t/// </summary>");
				writer.WriteLine($"\tpublic static class DbOptions");
				writer.WriteLine("\t{");
				writer.WriteLine();
				writer.Write(_sbConstTypeConstrutor);
				writer.WriteLine("\t\t#region Private Method And Field");
				writer.WriteLine("\t\tprivate static readonly NpgsqlNameTranslator _translator = new NpgsqlNameTranslator();");
				writer.WriteLine("\t\tprivate static void UseJsonNetForJtype(this INpgsqlTypeMapper mapper)");
				writer.WriteLine("\t\t{");
				writer.WriteLine("\t\t\tvar jtype = new[] { typeof(JToken), typeof(JObject), typeof(JArray) };");
				writer.WriteLine("\t\t\tmapper.UseJsonNet(jtype);");
				writer.WriteLine("\t\t}");
				writer.WriteLine("\t\tprivate class NpgsqlNameTranslator : INpgsqlNameTranslator");
				writer.WriteLine("\t\t{");
				writer.WriteLine("\t\t\tpublic string TranslateMemberName(string clrName) => clrName;");
				writer.WriteLine("\t\t\tpublic string TranslateTypeName(string clrName) => clrName;");
				writer.WriteLine("\t\t}");
				writer.WriteLine("\t\t#endregion");
				writer.WriteLine("\t}");
				writer.WriteLine("}"); // namespace end
			}
		}

	}
}
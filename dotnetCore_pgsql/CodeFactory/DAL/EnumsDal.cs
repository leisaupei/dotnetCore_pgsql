using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Data;
using System.IO;
using DBHelper;
using System.Text;

namespace Common.CodeFactory.DAL
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
		/// 生成枚举数据库枚举类型(覆盖生成)
		/// </summary>
		/// <param name="rootPath">根目录</param>
		/// <param name="modelPath">Model目录</param>
		/// <param name="projectName">项目名称</param>
		public static void Generate(string rootPath, string modelPath, string projectName)
		{
			_rootPath = rootPath;
			_modelPath = modelPath;
			_projectName = projectName;
			//string sqlText = @"
			//	SELECT 
			//	    A.oid,
			//	    A.typname,
			//	    b.nspname 
			//	FROM
			//	    pg_type A INNER JOIN pg_namespace b ON A.typnamespace = b.oid 
			//	WHERE
			//	    A.typtype = 'e' 
			//	ORDER BY
			//	    oid ASC
			//";
			var list = SQL.Select("a.oid,a.typname,b.nspname").From("pg_type", "a").InnerJoin("pg_namespace", "b", "a.typnamespace = b.oid").Where("a.typtype='e'").OrderBy("oid asc").ToList<EnumTypeInfo>();
			string fileName = Path.Combine(_modelPath, "_Enums.cs");
			using (StreamWriter writer = new StreamWriter(File.Create(fileName), Encoding.UTF8))
			{
				writer.WriteLine("using System;");
				writer.WriteLine();
				writer.WriteLine($"namespace {_projectName}.Model");
				writer.WriteLine("{");
				foreach (var item in list)
				{
					//string sql = $"select enumlabel from pg_enum WHERE enumtypid = {item.Oid} ORDER BY oid asc";
					//var enums = PgSqlHelper.ExecuteDataReaderList<string>(sql);
					var enums = SQL.Select("enumlabel").From("pg_enum").Where($"enumtypid={item.Oid}").OrderBy("oid asc").ToList<string>();
					if (enums.Count > 0)
						enums[0] = enums[0] + " = 1";
					writer.WriteLine($"\tpublic enum {item.Typname.ToUpperPascal()}");
					writer.WriteLine("\t{");
					writer.WriteLine($"\t\t{string.Join(", ", enums)}");
					writer.WriteLine("\t}");

				}

				writer.WriteLine("}");
			}
			GenerateMapping(list);
			GenerateRedisHepler();//Create RedisHelper.cs
			GenerateCsproj();
		}
		/// <summary>
		/// 生成初始化文件(覆盖生成)
		/// </summary>
		/// <param name="list"></param>
		public static void GenerateMapping(List<EnumTypeInfo> list)
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
				writer.WriteLine("\t\tpublic static void Init(int poolSize, string connectionSring, ILogger logger)");
				writer.WriteLine("\t\t{");
				writer.WriteLine("\t\t\tPgSqlHelper.InitDBConnection(poolSize, connectionSring, logger);");
				writer.WriteLine();
				writer.WriteLine("\t\t\tNpgsqlNameTranslator translator = new NpgsqlNameTranslator();");
				writer.WriteLine("\t\t\tNpgsqlConnection.GlobalTypeMapper.UseJsonNet();");
				foreach (var item in list)
					writer.WriteLine($"\t\t\tNpgsqlConnection.GlobalTypeMapper.MapEnum<{item.Typname.ToUpperPascal()}>(\"{item.Nspname}.{item.Typname}\", translator);");
				writer.WriteLine("\t\t}");
				writer.WriteLine("\t}");
				writer.WriteLine("\tpublic partial class NpgsqlNameTranslator : INpgsqlNameTranslator");
				writer.WriteLine("\t{");
				writer.WriteLine("\t\tprivate string clrName;");
				writer.WriteLine("\t\tprivate string clrTypeName;");
				writer.WriteLine("\t\tpublic string TranslateMemberName(string clrName)");
				writer.WriteLine("\t\t{");
				writer.WriteLine("\t\t\tthis.clrName = clrName;");
				writer.WriteLine("\t\t\treturn this.clrName;");
				writer.WriteLine("\t\t}");
				writer.WriteLine("\t\tpublic string TranslateTypeName(string clrName)");
				writer.WriteLine("\t\t{");
				writer.WriteLine("\t\t\tthis.clrTypeName = clrName;");
				writer.WriteLine("\t\t\treturn this.clrTypeName;");
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

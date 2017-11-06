using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Data;
using System.IO;
using Common.db.DBHelper;

namespace dotnetCore_pgsql_DevVersion.CodeFactory.DAL
{
    public class EnumsDal
    {
        private static string projectName = string.Empty;
        private static string modelPath = string.Empty;
        private static string rootPath = string.Empty;
        public static void Generate(string rootpath, string modelpath, string projectname)
        {
            rootPath = rootpath;
            modelPath = modelpath;
            projectName = projectname;
            string sqlText = @"
SELECT 
    A.oid,
    A.typname,
    b.nspname 
FROM
    pg_type A INNER JOIN pg_namespace b ON A.typnamespace = b.oid 
WHERE
    A.typtype = 'e' 
ORDER BY
    oid ASC
";
            List<EnumTypeInfo> list = GenericHelper<EnumTypeInfo>.Generic.ReaderToList<EnumTypeInfo>(PgSqlHelper.ExecuteDataReader(sqlText));

            string _fileName = Path.Combine(modelPath, "_Enums.cs");
            using (StreamWriter writer = new StreamWriter(File.Create(_fileName)))
            {
                writer.WriteLine("using System;");
                writer.WriteLine();
                writer.WriteLine($"namespace {projectName}.Model");
                writer.WriteLine("{");
                foreach (var item in list)
                {
                    string sql = $"select enumlabel from pg_enum WHERE enumtypid = {item.Oid} ORDER BY oid asc";
                    List<string> enums = GenericHelper<string>.Generic.ToListSingle<string>(PgSqlHelper.ExecuteDataReader(sql));
                    if (enums.Count > 0)
                        enums[0] = enums[0] + " = 1";
                    writer.WriteLine($"\tpublic enum {item.Typname.ToUpperPascal()}ENUM");
                    writer.WriteLine("\t{");
                    writer.WriteLine($"\t\t{string.Join(", ", enums)}");
                    writer.WriteLine("\t}");
                }

                writer.WriteLine("}");
            }

            GenerateMapping(list);
            GenerateRedisHepler();//Create RedisHelper
        }

        public static void GenerateMapping(List<EnumTypeInfo> list)
        {
            string _fileName = Path.Combine(rootPath, "_Startup.cs");
            using (StreamWriter writer = new StreamWriter(File.Create(_fileName)))
            {
                writer.WriteLine($"using {projectName}.Model;");
                writer.WriteLine("using System;");
                writer.WriteLine("using Microsoft.Extensions.Logging;");
                writer.WriteLine("using Common.db.DBHelper;");
                writer.WriteLine("using Npgsql;");
                writer.WriteLine();
                writer.WriteLine($"namespace {projectName}");
                writer.WriteLine("{");
                writer.WriteLine("\tpublic class _Startup");
                writer.WriteLine("\t{");
                writer.WriteLine("\t\tpublic static void Init(ILogger logger, string connectionString)");
                writer.WriteLine("\t\t{");
                writer.WriteLine("\t\t\tPgSqlHelper.InitDBConnection(logger, connectionString);");
                writer.WriteLine();
                writer.WriteLine("\t\t\tNpgsqlNameTranslator translator = new NpgsqlNameTranslator();");
                foreach (var item in list)
                {
                    writer.WriteLine($"\t\t\tNpgsqlConnection.MapEnumGlobally<{item.Typname.ToUpperPascal()}ENUM>(\"{item.Nspname}.{item.Typname}\", translator);");
                }
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
        public static void GenerateRedisHepler()
        {
            string _fileName = Path.Combine(rootPath, "RedisHelper.cs");
            using (StreamWriter writer = new StreamWriter(File.Create(_fileName)))
            {
                writer.WriteLine("using System;");
                writer.WriteLine("using System.Collections.Generic;");
                writer.WriteLine("using Microsoft.Extensions.Configuration;");
                writer.WriteLine("");
                writer.WriteLine("namespace dotnetCore_pgsql_DevVersion.db");
                writer.WriteLine("{");
                writer.WriteLine("\tpublic  class RedisHelper : CSRedis.QuickHelperBase");
                writer.WriteLine("\t{");
                writer.WriteLine("\t\tpublic static IConfiguration Configuration { get; internal set; }");
                writer.WriteLine("\t\tpublic static void InitializeConfiguration(IConfiguration cfg)");
                writer.WriteLine("\t\t{");
                //note: 
                writer.WriteLine("/*");
                writer.WriteLine("appsetting.json里面添加");
                writer.WriteLine("\"ConnectionStrings\": {");
                writer.WriteLine("\t\"redis\": {");
                writer.WriteLine("\t\t\"ip\": \"127.0.0.1\",");
                writer.WriteLine("\t\t\"port\": 6379,");
                writer.WriteLine("\t\t\"pass\": \"123456\",");
                writer.WriteLine("\t\t\"database\": 13,");
                writer.WriteLine("\t\t\"poolsize\": 50,");
                writer.WriteLine("\t\t\"name\": \"dev\"");
                writer.WriteLine("\t}");
                writer.WriteLine("}");
                writer.WriteLine("*/");

                writer.WriteLine("\t\t\tConfiguration = cfg;");
                writer.WriteLine("\t\t\tint port, poolsize, database;");
                writer.WriteLine("\t\t\tstring ip, pass;");
                writer.WriteLine("\t\t\tif (!int.TryParse(cfg[\"ConnectionStrings:redis:port\"], out port)) port = 6379;");
                writer.WriteLine("\t\t\tif (!int.TryParse(cfg[\"ConnectionStrings:redis:poolsize\"], out poolsize)) poolsize = 50;");
                writer.WriteLine("\t\t\tif (!int.TryParse(cfg[\"ConnectionStrings:redis:database\"], out database)) database = 0;");
                writer.WriteLine("\t\t\tip = cfg[\"ConnectionStrings:redis:ip\"];");
                writer.WriteLine("\t\t\tpass = cfg[\"ConnectionStrings:redis:pass\"];");
                writer.WriteLine("\t\t\tName = cfg[\"ConnectionStrings:redis:name\"];");
                writer.WriteLine("");
                writer.WriteLine("\t\t\tInstance = new CSRedis.ConnectionPool(ip, port, poolsize);");
                writer.WriteLine("\t\t\tInstance.Connected += (s, o) =>");
                writer.WriteLine("\t\t\t{");
                writer.WriteLine("\t\t\t\tCSRedis.RedisClient rc = s as CSRedis.RedisClient;");
                writer.WriteLine("\t\t\t\tif (!string.IsNullOrEmpty(pass)) rc.Auth(pass);");
                writer.WriteLine("\t\t\t\tif (database > 0) rc.Select(database);");
                writer.WriteLine("\t\t\t};");
                writer.WriteLine("\t\t}");
                writer.WriteLine("\t}");
                writer.WriteLine("}");
            };
        }
    }
}

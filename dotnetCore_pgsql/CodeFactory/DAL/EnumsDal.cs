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
                select a.oid,a.typname, b.nspname from pg_type a 
                INNER JOIN pg_namespace b on a.typnamespace = b.oid 
                where a.typtype = 'e' order by oid asc";
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
    }
}

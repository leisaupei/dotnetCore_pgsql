using dotnetCore_pgsql_DevVersion.CodeFactory.DAL;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Common.db.DBHelper;
using System.Data;
namespace dotnetCore_pgsql_DevVersion.CodeFactory
{
    public class LetsGo
    {
        private static string ModelPath = string.Empty;
        private static string DalPath = string.Empty;
        private static string ProjectName = string.Empty;
        private static string OutputDir = string.Empty;
        public static void Build(string output_dir, string proj_name)
        {
            if (string.IsNullOrEmpty(output_dir) || string.IsNullOrEmpty(proj_name))
                throw new ArgumentException("outputdir 或 projectname ", "不能为空");

            OutputDir = output_dir; ProjectName = proj_name;

            CreateDir(); CreateCsproj();

            EnumsDal.Generate(Path.Combine(OutputDir, ProjectName, ProjectName + ".db"), ModelPath, ProjectName);

            List<string> schemaList = SchemaDal.GetSchemas();
            foreach (var schemaName in schemaList)
            {
                List<TableViewModel> tableList = GetTables(schemaName);
                foreach (var item in tableList)
                {
                    TablesDal td = new TablesDal(ProjectName, ModelPath, DalPath, schemaName, item);
                    td.Generate();
                }
            }
        }
        private static void CreateDir()
        {
            ModelPath = Path.Combine(OutputDir, ProjectName, ProjectName + ".db", "Model", "Build");
            DalPath = Path.Combine(OutputDir, ProjectName, ProjectName + ".db", "DAL", "Build");
            string[] ps = { ModelPath, DalPath };
            for (int i = 0; i < ps.Length; i++)
            {
                if (!Directory.Exists(ps[i]))
                    Directory.CreateDirectory(ps[i]);
            }
        }
        private static void CreateCsproj()
        {
            string path = Path.Combine(OutputDir, ProjectName, $"{ProjectName}.db");

            string csproj = Path.Combine(path, $"{ProjectName}.db.csproj");

            if (File.Exists(csproj))
                return;
            using (StreamWriter writer = new StreamWriter(File.Create(csproj)))
            {
                writer.WriteLine(@"<Project Sdk=""Microsoft.NET.Sdk"">");
                writer.WriteLine();
                writer.WriteLine("\t<PropertyGroup>");
                writer.WriteLine("\t\t<TargetFramework>netcoreapp2.0</TargetFramework>");
                writer.WriteLine("\t</PropertyGroup>");
                writer.WriteLine();
                writer.WriteLine("\t<ItemGroup>");
                writer.WriteLine("\t\t<ProjectReference Include=\"..\\Common.db\\Common.db.csproj\" />");
                //writer.WriteLine("<ProjectReference Include=""..\Infrastructure\Infrastructure.csproj"" />");
                writer.WriteLine("\t</ItemGroup>");
                writer.WriteLine();
                writer.WriteLine("</Project>");

            }
            //unzip
            string common_file = Path.Combine(OutputDir, ProjectName, "Common.db");
            string system_directory = Path.Combine(Directory.GetParent(Environment.CurrentDirectory).FullName, "Common.db");
            string common_directory = Path.Combine(system_directory.Substring(0, system_directory.IndexOf("dotnetCore_pgsql") + 16), "Common.db");
            Console.WriteLine(common_directory);
            DirectoryCopy(common_directory, common_file);

            string sln_file = Path.Combine(OutputDir, ProjectName, $"{ProjectName}.sln");
            if (!File.Exists(sln_file))
            {
                using (StreamWriter writer = new StreamWriter(File.Create(sln_file)))
                {
                    writer.WriteLine("Microsoft Visual Studio Solution File, Format Version 12.00");
                    writer.WriteLine("# Visual Studio 15>");
                    writer.WriteLine($"VisualStudioVersion = 15.0.26430.13");

                    Guid db_guid = Guid.NewGuid();
                    writer.WriteLine($"Project(\"{Guid.NewGuid()}\") = \"{ProjectName}.db\", \"{ProjectName}.db\\{ProjectName}.db.csproj\", \"{ db_guid}\"");
                    writer.WriteLine($"EndProject");

                    Guid staging_guid = Guid.NewGuid();
                    writer.WriteLine($"Project(\"{Guid.NewGuid()}\") = \"Common.db\", \"Common.db\\Common.db.csproj\", \"{ staging_guid}\"");
                    writer.WriteLine($"EndProject");

                    writer.WriteLine("Global");
                    writer.WriteLine("\tGlobalSection(SolutionConfigurationPlatforms) = preSolution");
                    writer.WriteLine("\t\tDebug|Any CPU = Debug|Any CPU");
                    writer.WriteLine("\t\tRelease|Any CPU = Release|Any CPU");
                    writer.WriteLine("\tEndGlobalSection");

                    writer.WriteLine("\tGlobalSection(ProjectConfigurationPlatforms) = postSolution");
                    writer.WriteLine($"\t\t{db_guid}.Debug|Any CPU.ActiveCfg = Debug|Any CPU");
                    writer.WriteLine($"\t\t{db_guid}.Debug|Any CPU.Build.0 = Debug|Any CPU");
                    writer.WriteLine($"\t\t{db_guid}.Release|Any CPU.ActiveCfg = Release|Any CPU");
                    writer.WriteLine($"\t\t{db_guid}.Release|Any CPU.Build.0 = Release|Any CPU");
                    writer.WriteLine($"\t\t{staging_guid}.Debug|Any CPU.ActiveCfg = Debug|Any CPU");
                    writer.WriteLine($"\t\t{staging_guid}.Debug|Any CPU.Build.0 = Debug|Any CPU");
                    writer.WriteLine($"\t\t{staging_guid}.Release|Any CPU.ActiveCfg = Release|Any CPU");
                    writer.WriteLine($"\t\t{staging_guid}.Release|Any CPU.Build.0 = Release|Any CPU");
                    writer.WriteLine("\tEndGlobalSection");
                    writer.WriteLine("\tGlobalSection(SolutionProperties) = preSolution");
                    writer.WriteLine("\t\tHideSolutionNode = FALSE");
                    writer.WriteLine("\tEndGlobalSection");
                    writer.WriteLine("EndGlobal");
                }
            }
        }
        private static List<TableViewModel> GetTables(string schemaname)
        {
            string[] NotCreateSchemas = { "'pg_catalog'", "'information_schema'" };
            string[] NotCreateTables = { "'spatial_ref_sys'" };
            string[] NotCreateViews = { "'raster_columns'", "'raster_overviews'", "'geometry_columns'", "'geography_columns'" };
            string sqlText = $@"
SELECT
	tablename AS NAME,
	'table' AS TYPE 
FROM
	pg_tables 
WHERE
	schemaname NOT IN ({string.Join(",", NotCreateSchemas)}) 
	AND tablename NOT IN ({string.Join(",", NotCreateTables)}) 
	AND schemaname = '{schemaname}' 
UNION
	SELECT
		viewname AS NAME,
		'view' AS TYPE 
	FROM
		pg_views 
	WHERE
		viewname NOT IN ({string.Join(",", NotCreateViews)}) 
		AND schemaname = '{schemaname}'
";
            return GenericHelper<TableViewModel>.Generic.ReaderToList<TableViewModel>(PgSqlHelper.ExecuteDataReader(CommandType.Text, sqlText));
        }
        //复制目录递归
        private static void DirectoryCopy(string sourceDirectory, string targetDirectory)
        {
            if (!Directory.Exists(targetDirectory))
                Directory.CreateDirectory(targetDirectory);
            if (!Directory.Exists(sourceDirectory))
                return;
            DirectoryInfo sourceInfo = new DirectoryInfo(sourceDirectory);
            FileInfo[] fileInfo = sourceInfo.GetFiles();
            foreach (FileInfo fiTemp in fileInfo)
            {
                File.Copy(Path.Combine(sourceDirectory, fiTemp.Name), Path.Combine(targetDirectory, fiTemp.Name), true);
            }
            DirectoryInfo[] diInfo = sourceInfo.GetDirectories();
            foreach (DirectoryInfo diTemp in diInfo)
            {
                string sourcePath = diTemp.FullName;
                string targetPath = diTemp.FullName.Replace(sourceDirectory, targetDirectory);
                Directory.CreateDirectory(targetPath);
                DirectoryCopy(sourcePath, targetPath);
            }
        }
    }
}
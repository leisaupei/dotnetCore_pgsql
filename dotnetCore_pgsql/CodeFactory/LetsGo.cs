using CodeFactory.DAL;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DBHelper;
using System.Data;
using System.Text;

namespace CodeFactory
{
	public static class LetsGo
	{
		static string ModelPath = string.Empty;
		static string DalPath = string.Empty;
		static string ProjectName = string.Empty;
		static string OutputDir = string.Empty;
		/// <summary>
		/// Produce
		/// </summary>
		/// <param name="args"></param>
		public static void Produce(string args)
		{
			Console.OutputEncoding = Encoding.UTF8;
			GenerateModel model = new GenerateModel();
			var strings = args.Split(';');
			if (strings.Length != 7) throw new ArgumentException("Generate string is error");
			StringBuilder connection = new StringBuilder();
			foreach (var item in strings)
			{
				//host=localhost;port=5432;user=postgres;pwd=123456;db=postgres;maxpool=50;name=test;path=d:\workspace
				var sp = item.Split('=');
				var left = sp[0];
				var right = sp[1];
				switch (left.ToLower())
				{
					case "host": connection.Append($"host={right};"); break;
					case "port": connection.Append($"port={right};"); break;
					case "user": connection.Append($"username={right};"); break;
					case "pwd": connection.Append($"password={right};"); break;
					case "db": connection.Append($"database={right};"); break;
					case "name": model.ProjectName = right; break;
					case "path": model.OutputPath = right; break;
				}
			}
			model.ConnectionString = connection.ToString();
			PgSqlHelper.InitDBConnection(32, model.ConnectionString, null);
			Build(model.OutputPath, model.ProjectName);
		}
		/// <summary>
		/// Build
		/// </summary>
		/// <param name="outputDir"></param>
		/// <param name="projectName"></param>e
		public static void Build(string outputDir, string projectName)
		{
			if (string.IsNullOrEmpty(outputDir) || string.IsNullOrEmpty(projectName))
				throw new ArgumentException("outputdir 或 projectname ", "不能为空");
			OutputDir = outputDir;
			ProjectName = projectName;
			CreateDir();
			CreateCsproj();
			CreateSln();
			EnumsDal.Generate(Path.Combine(OutputDir, ProjectName, ProjectName + ".db"), ModelPath, ProjectName);
			List<string> schemaList = SchemaDal.GetSchemas();
			foreach (var schemaName in schemaList)
			{
				List<TableViewModel> tableList = GetTables(schemaName);
				foreach (var item in tableList)
				{
					TablesDal td = new TablesDal(ProjectName, ModelPath, DalPath, schemaName, item);
					td.ModelGenerator();
				}
			}
		}
		/// <summary>
		/// 创建目录
		/// </summary>
		static void CreateDir()
		{

			ModelPath = Path.Combine(OutputDir, ProjectName, ProjectName + ".db", "Model", "Build");
			DalPath = Path.Combine(OutputDir, ProjectName, ProjectName + ".db", "DAL", "Build");
			string[] ps = { ModelPath, DalPath };
			for (int i = 0; i < ps.Length; i++)
				if (!Directory.Exists(ps[i]))
					Directory.CreateDirectory(ps[i]);
		}
		/// <summary>
		/// 复制Common目录
		/// </summary>
		static void CreateCsproj()
		{
			string targetCommonDirectory = Path.Combine(OutputDir, ProjectName, "Common");
			if (!Directory.Exists(targetCommonDirectory))
			{
				var path = Path.Combine("..", "..", "..", "..", "Common");
				string commonDirectory = new DirectoryInfo(path).FullName;
				Console.WriteLine(commonDirectory);
				DirectoryCopy(commonDirectory, targetCommonDirectory);
			}

		}
		/// <summary>
		/// 创建sln解决方案文件
		/// </summary>
		static void CreateSln()
		{
			string sln_file = Path.Combine(OutputDir, ProjectName, $"{ProjectName}.sln");
			if (!File.Exists(sln_file))
			{
				using (StreamWriter writer = new StreamWriter(File.Create(sln_file), Encoding.UTF8))
				{
					writer.WriteLine("Microsoft Visual Studio Solution File, Format Version 12.00");
					writer.WriteLine("# Visual Studio 15>");
					writer.WriteLine($"VisualStudioVersion = 15.0.26430.13");

					Guid db_guid = Guid.NewGuid();
					writer.WriteLine($"Project(\"{Guid.NewGuid()}\") = \"{ProjectName}.db\", \"{ProjectName}.db\\{ProjectName}.db.csproj\", \"{ db_guid}\"");
					writer.WriteLine($"EndProject");

					Guid staging_guid = Guid.NewGuid();
					writer.WriteLine($"Project(\"{Guid.NewGuid()}\") = \"Common\", \"Common\\Common.csproj\", \"{ staging_guid}\"");
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

		/// <summary>
		/// 获取所有表
		/// </summary>
		/// <param name="schemaName"></param>
		/// <returns></returns>
		static List<TableViewModel> GetTables(string schemaName)
		{
			string[] notCreateSchemas = { "'pg_catalog'", "'information_schema'" };
			string[] notCreateTables = { "'spatial_ref_sys'" };
			string[] notCreateViews = { "'raster_columns'", "'raster_overviews'", "'geometry_columns'", "'geography_columns'" };

			return SQL.Select("tablename AS name,'table' AS type").From("pg_tables")
				.Where($"schemaname NOT IN({ string.Join(",", notCreateSchemas)})")
				.Where($"tablename NOT IN ({string.Join(",", notCreateTables)})")
				.Where($"schemaname = '{schemaName}'")
			.Union(SQL.Select("viewname AS name,'view' AS type ").From("pg_views")
				.Where($"viewname NOT IN ({string.Join(", ", notCreateViews)})")
				.Where($"schemaname = '{schemaName}'")).ToList<TableViewModel>();
		}
		/// <summary>
		/// 复制目录
		/// </summary>
		/// <param name="sourceDirectory"></param>
		/// <param name="targetDirectory"></param>
		static void DirectoryCopy(string sourceDirectory, string targetDirectory)
		{
			DirectoryInfo sourceInfo = new DirectoryInfo(sourceDirectory);
			if (ExceptDir.Contains(sourceInfo.Name))
				return;
			if (!Directory.Exists(targetDirectory))
				Directory.CreateDirectory(targetDirectory);
			if (!Directory.Exists(sourceDirectory))
				return;
			FileInfo[] fileInfo = sourceInfo.GetFiles();
			foreach (FileInfo fiTemp in fileInfo)
			{
				if (ExceptFile.Contains(fiTemp.Name))
					continue;
				var sourcePath = Path.Combine(sourceDirectory, fiTemp.Name);
				var targetPath = Path.Combine(targetDirectory, fiTemp.Name);
				File.Copy(sourcePath, targetPath, true);
			}

			DirectoryInfo[] diInfo = sourceInfo.GetDirectories();
			foreach (DirectoryInfo diTemp in diInfo)
			{
				string sourcePath = diTemp.FullName;
				string targetPath = diTemp.FullName.Replace(sourceDirectory, targetDirectory);
				DirectoryCopy(sourcePath, targetPath);
			}
		}
		/// <summary>
		/// 不复制的目录
		/// </summary>
		static readonly string[] ExceptDir = { "CodeFactory", "CSRedis", "MQHelper" };
		/// <summary>
		/// 不复制的文件
		/// </summary>
		static readonly string[] ExceptFile = { "Redis.zip" };
	}
}
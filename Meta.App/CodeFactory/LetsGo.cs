using CodeFactory.DAL;
using CodeFactory.Extension;
using Meta.Common.DBHelper;
using Meta.Common.SqlBuilder;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace CodeFactory
{
	/// <summary>
	/// 
	/// </summary>
	public static class LetsGo
	{
		static string ModelPath = string.Empty;
		static string DalPath = string.Empty;
		static string ProjectName = string.Empty;
		static string OutputDir = string.Empty;
		public static string FinalType;
		/// <summary>
		/// 生成
		/// </summary>
		/// <param name="args"></param>
		public static void Produce(string args)
		{
			Console.OutputEncoding = Encoding.UTF8;
			GenerateModel model = GetGenerateModel(args);
			PgSqlHelper.InitDBConnectionOption(new Meta.Common.Model.BaseDbOption("master", model.ConnectionString, null, null));
			Build(model);
			Console.WriteLine("Done...");
		}

		public static GenerateModel GetGenerateModel(string args)
		{
			GenerateModel model = new GenerateModel();
			var strings = args.Split(';');
			foreach (var item in strings)
			{
				var sp = item.Split('=');
				var left = sp[0];
				var right = sp[1];
				switch (left.ToLower())
				{
					case "host": model.ConnectionString += $"host={right};"; break;
					case "port": model.ConnectionString += $"port={right};"; break;
					case "user": model.ConnectionString += $"username={right};"; break;
					case "pwd": model.ConnectionString += $"password={right};"; break;
					case "db": model.ConnectionString += $"database={right};"; break;
					case "maxpool": model.ConnectionString += $"maximum pool size={right};pooling=true;"; break;
					case "name": model.ProjectName = right; break;
					case "path": model.OutputPath = right; break;
					case "type": model.TypeName = (string.IsNullOrEmpty(right) ? GenerateModel.MASTER_DATABASE_TYPE_NAME : right).ToUpperPascal(); break;
				}
			}
			return model;
		}

		/// <summary>
		/// 构建
		/// </summary>
		/// <param name="buildModel"></param>
		public static void Build(GenerateModel buildModel)
		{
			if (string.IsNullOrEmpty(buildModel.OutputPath) || string.IsNullOrEmpty(buildModel.ProjectName))
				throw new ArgumentException("outputdir 或 projectname ", "不能为空");

			OutputDir = buildModel.OutputPath;
			ProjectName = buildModel.ProjectName;
			CreateDir();
			CreateCsproj();
			CreateSln();
			List<string> schemaList = SchemaDal.GetSchemas();
			EnumsDal.Generate(Path.Combine(OutputDir, ProjectName + ".db"), ModelPath, ProjectName, buildModel.TypeName);
			foreach (var schemaName in schemaList)
			{
				List<TableViewModel> tableList = GetTables(schemaName);
				foreach (var item in tableList)
				{
					TablesDal td = new TablesDal(ProjectName, ModelPath, DalPath, schemaName, item, buildModel.TypeName);
					td.ModelGenerator();
				}
			}
		}
		/// <summary>
		/// 创建目录
		/// </summary>
		static void CreateDir()
		{
			ModelPath = Path.Combine(OutputDir, ProjectName + ".db", "Model", "Build");
			DalPath = Path.Combine(OutputDir, ProjectName + ".db", "DAL", "Build");
			string[] ps = { ModelPath, DalPath };
			for (int i = 0; i < ps.Length; i++)
			{
				if (!Directory.Exists(ps[i]))
					Directory.CreateDirectory(ps[i]);
			}
		}
		/// <summary>
		/// 创建csproj文件
		/// </summary>
		static void CreateCsproj()
		{
			//copy common directory
			string targetCommonDirectory = Path.Combine(OutputDir, "Meta.Common");
			if (!Directory.Exists(targetCommonDirectory)) //if is not exist
			{
				var path = Path.Combine("..", "..", "..", "..", "Meta.Common");
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
			string sln_file = Path.Combine(OutputDir, $"{ProjectName}.sln");
			if (!File.Exists(sln_file))
			{
				using StreamWriter writer = new StreamWriter(File.Create(sln_file), Encoding.UTF8);
				writer.WriteLine("Microsoft Visual Studio Solution File, Format Version 12.00");
				writer.WriteLine("# Visual Studio 15>");
				writer.WriteLine($"VisualStudioVersion = 15.0.26430.13");

				Guid dbId = Guid.NewGuid();
				writer.WriteLine($"Project(\"{Guid.NewGuid()}\") = \"{ProjectName}.db\", \"{ProjectName}.db\\{ProjectName}.db.csproj\", \"{dbId}\"");
				writer.WriteLine($"EndProject");

				Guid commonId = Guid.NewGuid();
				writer.WriteLine($"Project(\"{Guid.NewGuid()}\") = \"Meta.Common\", \"Meta.Common\\Meta.Common.csproj\", \"{commonId}\"");
				writer.WriteLine($"EndProject");

				writer.WriteLine("Global");
				writer.WriteLine("\tGlobalSection(SolutionConfigurationPlatforms) = preSolution");
				writer.WriteLine("\t\tDebug|Any CPU = Debug|Any CPU");
				writer.WriteLine("\t\tRelease|Any CPU = Release|Any CPU");
				writer.WriteLine("\tEndGlobalSection");

				writer.WriteLine("\tGlobalSection(ProjectConfigurationPlatforms) = postSolution");
				writer.WriteLine($"\t\t{dbId}.Debug|Any CPU.ActiveCfg = Debug|Any CPU");
				writer.WriteLine($"\t\t{dbId}.Debug|Any CPU.Build.0 = Debug|Any CPU");
				writer.WriteLine($"\t\t{dbId}.Release|Any CPU.ActiveCfg = Release|Any CPU");
				writer.WriteLine($"\t\t{dbId}.Release|Any CPU.Build.0 = Release|Any CPU");
				writer.WriteLine($"\t\t{commonId}.Debug|Any CPU.ActiveCfg = Debug|Any CPU");
				writer.WriteLine($"\t\t{commonId}.Debug|Any CPU.Build.0 = Debug|Any CPU");
				writer.WriteLine($"\t\t{commonId}.Release|Any CPU.ActiveCfg = Release|Any CPU");
				writer.WriteLine($"\t\t{commonId}.Release|Any CPU.Build.0 = Release|Any CPU");
				writer.WriteLine("\tEndGlobalSection");
				writer.WriteLine("\tGlobalSection(SolutionProperties) = preSolution");
				writer.WriteLine("\t\tHideSolutionNode = FALSE");
				writer.WriteLine("\tEndGlobalSection");
				writer.WriteLine("EndGlobal");
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
			string[] notCreateTables = { "'spatial_ref_sys'", "'us_gaz'", "'us_lex'", "'us_rules'" };
			string[] notCreateViews = { "'raster_columns'", "'raster_overviews'", "'geometry_columns'", "'geography_columns'" };

			return SqlInstance.Select("tablename AS name,'table' AS type").From("pg_tables")
				.WhereNotIn($"schemaname", notCreateSchemas)
				.WhereNotIn($"tablename", notCreateTables)
				.Where($"schemaname = '{schemaName}' and tablename not like '%copy%'")
			.Union(SqlInstance.Select("viewname AS name,'view' AS type ").From("pg_views")
				.WhereNotIn($"viewname", notCreateViews)
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
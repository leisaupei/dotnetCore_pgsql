using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Meta.Initiator
{
	public static class FileInitHelper
	{
		/// <summary>
		/// 
		/// </summary>
		public static void GenerateCsproj(string outputDir, string projectName)
		{
			var path = Path.Combine(outputDir, projectName + ".db");
			if (!Directory.Exists(path))
				Directory.CreateDirectory(path);
			string csproj = Path.Combine(path, $"{projectName}.db.csproj");
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
			writer.WriteLine("\t\t<PackageReference Include=\"Meta.Driver\" Version=\"1.0.14\" />");
			writer.WriteLine("\t</ItemGroup>");
			writer.WriteLine();
			writer.WriteLine("</Project>");
		}

		public static void GenerateDbConfig(string outputDir, string projectName)
		{
			var root = Path.Combine(Path.Combine(outputDir, projectName + ".db"), "Options");
			if (!Directory.Exists(root))
				Directory.CreateDirectory(root);

			var fileName = Path.Combine(root, "DbConfig.cs");
			if (File.Exists(fileName))
				return;

			using StreamWriter writer = new StreamWriter(File.Create(fileName), Encoding.UTF8);

			writer.WriteLine("using Microsoft.Extensions.Configuration;");
			writer.WriteLine("");
			writer.WriteLine($"namespace {projectName}.Options");
			writer.WriteLine("{");
			writer.WriteLine("\t/// <summary>");
			writer.WriteLine("\t/// 生成文件, 存在则不会覆盖");
			writer.WriteLine("\t/// </summary>");
			writer.WriteLine("\tpublic class DbConfig");
			writer.WriteLine("\t{");
			writer.WriteLine("\t\t/// <summary>");
			writer.WriteLine("\t\t/// 数据库缓存超时时间, 单位: 秒, 0为不用redis缓存 -1为不过期");
			writer.WriteLine("\t\t/// </summary>");
			writer.WriteLine("\t\tpublic const int DbCacheTimeOut = 60;");
			writer.WriteLine("\t\t/// <summary>");
			writer.WriteLine("\t\t/// 全局配置");
			writer.WriteLine("\t\t/// </summary>");
			writer.WriteLine("\t\tpublic static IConfiguration Configuration { get; private set; }");
			writer.WriteLine("");
			writer.WriteLine("\t\tpublic static void SetConfiguration(IConfiguration cfg)");
			writer.WriteLine("\t\t{");
			writer.WriteLine("\t\t\tConfiguration = cfg;");
			writer.WriteLine("\t\t}");
			writer.WriteLine("\t}");
			writer.WriteLine("}");
		}

		/// <summary>
		/// 创建csproj文件
		/// </summary>
		public static void CreateCsproj(string outputDir)
		{
			//copy common directory
			string targetCommonDirectory = Path.Combine(outputDir, "Meta.Common");
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
		public static void CreateSln(string outputDir, string projectName)
		{
			if (Directory.GetFiles(outputDir).Any(f => f.Contains(".sln")))
				return;
			string sln_file = Path.Combine(outputDir, $"{projectName}.sln");

			if (!File.Exists(sln_file))
			{
				using StreamWriter writer = new StreamWriter(File.Create(sln_file), Encoding.UTF8);
				writer.WriteLine("Microsoft Visual Studio Solution File, Format Version 12.00");
				writer.WriteLine("# Visual Studio 15>");
				writer.WriteLine($"VisualStudioVersion = 15.0.26430.13");

				Guid dbId = Guid.NewGuid();
				writer.WriteLine("Project(\"{0}\") = \"{1}.db\", \"{1}.db\\{1}.db.csproj\", \"{2}\"", Guid.NewGuid(), projectName, dbId);
				writer.WriteLine($"EndProject");


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
				writer.WriteLine("\tEndGlobalSection");
				writer.WriteLine("\tGlobalSection(SolutionProperties) = preSolution");
				writer.WriteLine("\t\tHideSolutionNode = FALSE");
				writer.WriteLine("\tEndGlobalSection");
				writer.WriteLine("EndGlobal");
			}
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

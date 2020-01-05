using Meta.Common.DbHelper;
using Meta.Common.Model;
using Meta.Common.SqlBuilder;
using Meta.Postgres.Generator.CodeFactory.DAL;
using Meta.Postgres.Generator.CodeFactory.Extension;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
namespace Meta.Postgres.Generator.CodeFactory
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
		/// <summary>
		/// 最后一个数据库名
		/// </summary>
		public static string FinalType;
		/// <summary>
		/// 生成
		/// </summary>
		/// <param name="args"></param>
		public static void Produce(GenerateModel model)
		{
			Console.OutputEncoding = Encoding.UTF8;
			PgsqlHelper.InitDBConnectionOption(new BaseDbOption("master", model.ConnectionString, null, new LoggerFactory().CreateLogger<BaseDbOption>()));
			Build(model);
			Console.WriteLine("Done...");
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
		/// 构建
		/// </summary>
		/// <param name="buildModel"></param>
		public static void Build(GenerateModel buildModel)
		{
			if (string.IsNullOrEmpty(buildModel.OutputPath))
				throw new ArgumentNullException(nameof(buildModel.OutputPath));
			if (string.IsNullOrEmpty(buildModel.ProjectName))
				throw new ArgumentNullException(nameof(buildModel.ProjectName));

			OutputDir = buildModel.OutputPath;
			ProjectName = buildModel.ProjectName;
			CreateDir();
			List<string> schemaList = SchemaDal.GetSchemas();
			foreach (var schemaName in schemaList)
			{
				List<TableViewModel> tableList = GetTables(schemaName);
				foreach (var item in tableList)
				{
					TablesDal td = new TablesDal(ProjectName, ModelPath, DalPath, schemaName, item, buildModel.TypeName);
					td.ModelGenerator();
				}
			}
			EnumsDal.Generate(Path.Combine(OutputDir, ProjectName + ".db"), ModelPath, ProjectName, buildModel.TypeName);
		}

		/// <summary>
		/// 获取所有表
		/// </summary>
		/// <param name="schemaName"></param>
		/// <returns></returns>
		static List<TableViewModel> GetTables(string schemaName)
		{
			string[] notCreateSchemas = { "pg_catalog", "information_schema" };
			string[] notCreateTables = { "spatial_ref_sys", "us_gaz", "us_lex", "us_rules" };
			string[] notCreateViews = { "raster_columns", "raster_overviews", "geometry_columns", "geography_columns" };
			var sql = $@"
SELECT tablename AS name,'table' AS type 
FROM pg_tables a  
WHERE schemaname NOT IN ({Types.ConvertArrayToSql(notCreateSchemas)}) 
AND tablename NOT IN ({Types.ConvertArrayToSql(notCreateTables)})
AND schemaname = '{schemaName}'
and tablename not like '%copy%'  
UNION (
	SELECT viewname AS name,'view' AS type  FROM pg_views a  
	WHERE viewname NOT IN ({Types.ConvertArrayToSql(notCreateViews)})
	AND schemaname = '{schemaName}'
)  
";
			return PgsqlHelper.ExecuteDataReaderList<TableViewModel>(sql);
		}
	}
}
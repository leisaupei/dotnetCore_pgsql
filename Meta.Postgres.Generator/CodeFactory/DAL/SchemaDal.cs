using System.Collections.Generic;
using System.Linq;
using Meta.Driver.DbHelper;
using Meta.Driver.SqlBuilder;

namespace Meta.Postgres.Generator.CodeFactory.DAL
{
	/// <summary>
	/// 
	/// </summary>
	public class SchemaDal
	{
		/// <summary>
		/// 
		/// </summary>
		/// <returns></returns>
		public static List<string> GetSchemas()
		{
			string[] notCreateSchemas = { "pg_toast", "pg_temp_1", "pg_toast_temp_1", "pg_catalog", "information_schema", "topology", "tiger", "tiger_data" };
			string sql = $@"
SELECT SCHEMA_NAME AS schemaname 
FROM information_schema.schemata a  
WHERE SCHEMA_NAME NOT IN ({Types.ConvertArrayToSql(notCreateSchemas)})  
ORDER BY SCHEMA_NAME";

			return PgsqlHelper.ExecuteDataReaderList<string>(sql);
		}

	}
}

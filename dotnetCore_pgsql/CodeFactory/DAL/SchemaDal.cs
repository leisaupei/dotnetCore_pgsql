using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DBHelper;
using System.Data;
namespace Common.CodeFactory.DAL
{
	public class SchemaDal
	{

		public static List<string> GetSchemas()
		{
			string[] notCreateSchemas = { "'pg_toast'", "'pg_temp_1'", "'pg_toast_temp_1'", "'pg_catalog'", "'information_schema'", "'topology'" };
			//string sql = $@"
			//	SELECT 
			//	    SCHEMA_NAME AS schemaname 
			//	FROM
			//	    information_schema.schemata 
			//	WHERE
			//	    SCHEMA_NAME NOT IN ({notCreateSchemas.Join(",")}) 
			//	ORDER BY
			//	    SCHEMA_NAME;
			//";
			return SQL.Select("SCHEMA_NAME AS schemaname").From("information_schema.schemata")
				.Where($"SCHEMA_NAME NOT IN ({notCreateSchemas.Join(", ")})").OrderBy("SCHEMA_NAME").ToList<string>();
		}
	}
}

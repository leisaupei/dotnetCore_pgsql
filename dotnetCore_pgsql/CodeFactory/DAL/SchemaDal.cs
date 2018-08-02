using System.Collections.Generic;
using DBHelper;
namespace Common.CodeFactory.DAL
{
	public class SchemaDal
	{
		public static List<string> GetSchemas()
		{
			string[] notCreateSchemas = { "'pg_toast'", "'pg_temp_1'", "'pg_toast_temp_1'", "'pg_catalog'", "'information_schema'", "'topology'" };
			return SQL.Select("SCHEMA_NAME AS schemaname").From("information_schema.schemata")
				.Where($"SCHEMA_NAME NOT IN ({notCreateSchemas.Join(", ")})").OrderBy("SCHEMA_NAME").ToList<string>();
		}
	}
}

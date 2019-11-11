using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DBHelper;
using System.Data;
namespace CodeFactory.DAL
{
	public class SchemaDal
	{

		public static List<string> GetSchemas()
		{
			string[] notCreateSchemas = { "'pg_toast'", "'pg_temp_1'", "'pg_toast_temp_1'", "'pg_catalog'", "'information_schema'", "'topology'", "'tiger'", "'tiger_data'" };

			return SQL.Select("SCHEMA_NAME AS schemaname").From("information_schema.schemata")
				.WhereNotIn("SCHEMA_NAME", notCreateSchemas).OrderBy("SCHEMA_NAME").ToList<string>();
		}
	}
}

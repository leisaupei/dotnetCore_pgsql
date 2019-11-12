using System.Collections.Generic;
using Meta.Common.SqlBuilder;

namespace CodeFactory.DAL
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
			string[] notCreateSchemas = { "'pg_toast'", "'pg_temp_1'", "'pg_toast_temp_1'", "'pg_catalog'", "'information_schema'", "'topology'", "'tiger'", "'tiger_data'" };

			return SqlInstance.Select("SCHEMA_NAME AS schemaname").From("information_schema.schemata")
				.WhereNotIn("SCHEMA_NAME", notCreateSchemas).OrderBy("SCHEMA_NAME").ToList<string>();
		}
	}
}

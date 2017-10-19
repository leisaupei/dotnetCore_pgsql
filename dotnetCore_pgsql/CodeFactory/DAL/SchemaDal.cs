using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Common.db.DBHelper;
using System.Data;
namespace dotnetCore_pgsql_DevVersion.CodeFactory.DAL
{
    public class SchemaDal
    {
      
        public static List<string> GetSchemas()
        {
            string sql = "SELECT schema_name as schemaname FROM information_schema.schemata WHERE SCHEMA_NAME NOT IN('pg_toast','pg_temp_1','pg_toast_temp_1','pg_catalog','information_schema') ORDER BY SCHEMA_NAME;";
            return GenericHelper<string>.Generic.ToListSingle<string>(PgSqlHelper.ExecuteDataReader(sql));
        }
    }
}

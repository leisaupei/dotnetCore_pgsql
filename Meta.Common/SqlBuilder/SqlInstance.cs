using Meta.Common.DbHelper;
using Meta.Common.Interface;
using Meta.Common.Model;
using System.Collections;
using System.Collections.Generic;
using System.Data;


namespace Meta.Common.SqlBuilder
{
	/// <summary>
	/// 复杂的sql语句不建议使用
	/// </summary>
	public class SqlInstance
	{
		public static SelectSQL Select() => new SelectSQL();
		public static SelectSQL Select(string fields) => new SelectSQL(fields);

		public static object[] SelectPipe(IEnumerable<ISqlBuilder> builders, string type = "master")
		{
			return PgSqlHelper.ExecuteDataReaderPipe(CommandType.Text, builders, type);
		}
	}
	public class SelectSQL : SelectBuilder<SelectSQL>
	{
		public SelectSQL() { }
		public SelectSQL(string fields) : base(fields) { }
	}

}
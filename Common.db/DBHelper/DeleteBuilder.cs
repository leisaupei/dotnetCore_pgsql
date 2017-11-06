using Common.db.Common;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq.Expressions;
using System.Text;

namespace Common.db.DBHelper
{
    public class DeleteBuilder<T> : QueryHelper<T> where T : class, new()
    {
        public new DeleteBuilder<T> Where(string filter, params object[] value) => base.Where(filter, value) as DeleteBuilder<T>;

        public int Commit()
        {
            if (WhereList.Count < 1) throw new Exception("delete语句必须带where条件");
            string tableName = MappingHelper.GetMapping(typeof(T));
            string sqlText = $"DELETE FROM {tableName} WHERE {string.Join("\nAND", WhereList)}";
            return PgSqlHelper.ExecuteNonQuery(CommandType.Text, sqlText, CommandParams.ToArray());
        }
    }
}

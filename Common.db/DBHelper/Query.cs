using Common.db.Common;
using Npgsql;
using NpgsqlTypes;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace Common.db.DBHelper
{
    public class Query<T> : QueryHelper<T> where T : class, new()
    {
        #region 条件集
        public Query<T> GroupBy(string s)
        {
            GroupByText = $"GROUP BY {s}";
            return this;
        }
        public Query<T> OrderBy(string s)
        {
            OrderByText = $"GROUP BY {s}";
            return this;
        }
        public Query<T> Having(string s)
        {
            HavingText = $"HAVING {s}";
            return this;
        }
        public Query<T> Limit(int i)
        {
            LimitText = $"LIMIT {i}";
            return this;
        }
        public Query<T> Skip(int i)
        {
            OffsetText = $"OFFSET {i}";
            return this;
        }



        public Query<T> Page(int index, int size)
        {
            return Limit(size).Skip(Math.Max(0, index - 1) * size);
        }
        #region where
        public Query<T> WhereOr(string filter, Array values) => base.Where(filter, values) as Query<T>;
        public Query<T> Where(bool isAdd, string filter, params object[] value) => isAdd ? Where(filter, value) : this;
        public new Query<T> Where(string filter, params object[] value) => base.Where(filter, value) as Query<T>;
        #endregion
        #region ToString
        //public override string ToString()
        //{
        //    return ToString(null);
        //}
        //public string ToString(string field)//调试用
        //{
        //    Fields.Clear();
        //    if (string.IsNullOrEmpty(field))
        //        Fields.AddRange(EntityHelper.GetAllFields(typeof(T), MasterAliasName + "."));
        //    else
        //        Fields.Add(field);
        //    //var params_str = string.Empty;
        //    //foreach (var item in CommandParams)
        //    //    params_str = string.Concat(params_str, item.ParameterName, ":", item.Value, "\n");
        //    return ToStringHelper.SqlToString(GetSqlString<T>(), CommandParams);
        //}
        #endregion

        #region union
        public new Query<T> Union<TModel>(UnionType unionType, string alias, string on) => base.Union<TModel>(unionType, alias, on) as Query<T>;
        public Query<T> InnerJoin<TModel>(string alias, string on) => Union<TModel>(UnionType.INNER_JOIN, alias, on);
        public Query<T> LeftJoin<TModel>(string alias, string on) => Union<TModel>(UnionType.LEFT_JOIN, alias, on);
        public Query<T> RightJoin<TModel>(string alias, string on) => Union<TModel>(UnionType.RIGHT_JOIN, alias, on);
        #endregion

        //public new Query<T> AddParameter(string field, NpgsqlDbType dbType, object value, int size, Type specificType) =>
            //AddParameter(field, dbType, value, size, specificType) as Query<T>;

        #endregion

        #region 结果集
        public T ToOne() => ToOne<T>();
        public List<T> ToList() => ToList<T>();
        public TResult ToTuple<TResult>(params string[] fields) => ToOne<TResult>(fields);
        public List<TResult> ToTupleList<TResult>(params string[] fields) => ToList<TResult>(fields);
        public long Count() => ToScalar<long>("COUNT(1)");
        public TResult Max<TResult>(string field) => ToScalar<TResult>($"COALESCE(MAX({field}),0)");
        public TResult Sum<TResult>(string field) => ToScalar<TResult>($"COALESCE(SUM({field}),0)");
        public TResult Avg<TResult>(string field) => ToScalar<TResult>($"COALESCE(AVG({field}),0)");
        #endregion
    }

}

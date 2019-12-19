using Meta.Common.DbHelper;
using Meta.Common.Extensions;
using Meta.Common.Interface;
using Meta.Common.Model;
using Meta.Common.SqlBuilder.AnalysisExpression;
using Npgsql;
using NpgsqlTypes;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace Meta.Common.SqlBuilder
{
    public abstract class WhereBase<TSQL, TModel> : BuilderBase<TSQL> where TSQL : class where TModel : IDbModel, new()
    {
        TSQL This => this as TSQL;
        #region Constructor
        protected WhereBase(string table, string alias) : base(table, alias) { }
        protected WhereBase(string table) : base(table) { }
        protected WhereBase() { }
        #endregion
        /// <summary>
        /// 子模型where
        /// </summary>
        /// <typeparam name="TSource"></typeparam>
        /// <param name="selector"></param>
        /// <returns></returns>
        public TSQL Where<TSource>(Expression<Func<TSource, bool>> selector) where TSource : IDbModel, new()
        {
            var info = SqlExpressionVisitor.Instance.VisitCondition(selector);
            AddParameter(info.Paras);
            return Where(info.SqlText);
        }

        /// <summary>
        /// 主模型重载
        /// </summary>
        /// <param name="selector"></param>
        /// <returns></returns>
        public TSQL Where(Expression<Func<TModel, bool>> selector) => Where<TModel>(selector);

        /// <summary>
        /// 字符串where语句
        /// </summary>
        /// <param name="where"></param>
        /// <returns></returns>
        public TSQL Where(string where)
        {
            base.WhereList.Add($"({where})");
            return This;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="TSource"></typeparam>
        /// <param name="selector"></param>
        /// <param name="values"></param>
        /// <example>WhereNotIn("a.id",new Guid[]{})</example>
        /// <exception cref="ArgumentNullException">values is null or length is zero</exception>
        /// <returns></returns>
        public TSQL WhereAny<TSource, TKey>(Expression<Func<TSource, TKey>> selector, IEnumerable<TKey> values) where TSource : IDbModel, new()
        {
            if (values.IsNullOrEmpty())
                throw new ArgumentNullException(nameof(values));
            var index = EntityHelper.ParamsIndex;
            AddParameter(new NpgsqlParameter<TKey[]>(index, values.ToArray()));
            return Where(string.Concat(SqlExpressionVisitor.Instance.VisitSingle(selector).SqlText, $" = any(@{index})"));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="TSource"></typeparam>
        /// <param name="selector"></param>
        /// <param name="values"></param>
        /// <example>WhereNotIn("a.id",new Guid[]{})</example>
        /// <exception cref="ArgumentNullException">values is null or length is zero</exception>
        /// <returns></returns>
        public TSQL WhereAny<TSource, TKey>(Expression<Func<TSource, TKey?>> selector, IEnumerable<TKey> values) where TSource : IDbModel, new() where TKey : struct
        {
            if (values.IsNullOrEmpty())
                throw new ArgumentNullException(nameof(values));
            var index = EntityHelper.ParamsIndex;
            AddParameter(new NpgsqlParameter<TKey[]>(index, values.ToArray()));
            return Where(string.Concat(SqlExpressionVisitor.Instance.VisitSingle(selector).SqlText, $" = any(@{index})"));
        }


        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="TSource"></typeparam>
        /// <param name="selector"></param>
        /// <param name="values"></param>
        /// <example>WhereNotIn("a.id",new Guid[]{})</example>
        /// <exception cref="ArgumentNullException">values is null or length is zero</exception>
        /// <returns></returns>
        public TSQL WhereAny<TKey>(Expression<Func<TModel, TKey>> selector, IEnumerable<TKey> values)
        {
            if (values.IsNullOrEmpty())
                throw new ArgumentNullException(nameof(values));
            var index = EntityHelper.ParamsIndex;
            AddParameter(new NpgsqlParameter<TKey[]>(index, values.ToArray()));
            return Where(string.Concat(SqlExpressionVisitor.Instance.VisitSingle(selector).SqlText, $" = any(@{index})"));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="TSource"></typeparam>
        /// <param name="selector"></param>
        /// <param name="values"></param>
        /// <example>WhereNotIn("a.id",new Guid[]{})</example>
        /// <exception cref="ArgumentNullException">values is null or length is zero</exception>
        /// <returns></returns>
        public TSQL WhereAny<TKey>(Expression<Func<TModel, TKey?>> selector, IEnumerable<TKey> values) where TKey : struct
            => WhereAny<TModel, TKey>(selector, values);


        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="TSource"></typeparam>
        /// <param name="selector"></param>
        /// <param name="values"></param>
        /// <example>WhereNotIn("a.id",new Guid[]{})</example>
        /// <exception cref="ArgumentNullException">values is null or length is zero</exception>
        /// <returns></returns>
        public TSQL WhereNotAny<TSource, TKey>(Expression<Func<TSource, TKey>> selector, IEnumerable<TKey> values) where TSource : IDbModel, new()
        {
            if (values.IsNullOrEmpty())
                throw new ArgumentNullException(nameof(values));
            var index = EntityHelper.ParamsIndex;
            AddParameter(new NpgsqlParameter<TKey[]>(index, values.ToArray()));
            return Where(string.Concat(SqlExpressionVisitor.Instance.VisitSingle(selector).SqlText, $" <> any(@{index})"));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="TSource"></typeparam>
        /// <param name="selector"></param>
        /// <param name="values"></param>
        /// <example>WhereNotIn("a.id",new Guid[]{})</example>
        /// <exception cref="ArgumentNullException">values is null or length is zero</exception>
        /// <returns></returns>
        public TSQL WhereNotAny<TSource, TKey>(Expression<Func<TSource, TKey?>> selector, IEnumerable<TKey> values) where TSource : IDbModel, new() where TKey : struct
        {
            if (values.IsNullOrEmpty())
                throw new ArgumentNullException(nameof(values));
            var index = EntityHelper.ParamsIndex;
            AddParameter(new NpgsqlParameter<TKey[]>(index, values.ToArray()));
            return Where(string.Concat(SqlExpressionVisitor.Instance.VisitSingle(selector).SqlText, $" <> any(@{index})"));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="selector"></param>
        /// <param name="sqlBuilder"></param>
        /// <exception cref="ArgumentNullException">sql is null or empty</exception>
        /// <returns></returns>
        public TSQL WhereNotIn<TSource>(Expression<Func<TSource, object>> selector, ISqlBuilder sqlBuilder) where TSource : IDbModel, new()
        {
            if (sqlBuilder == null)
                throw new ArgumentNullException(nameof(sqlBuilder));
            AddParameter(sqlBuilder.Params);
            return Where($"{SqlExpressionVisitor.Instance.VisitSingle(selector).SqlText} NOT IN ({sqlBuilder.GetCommandTextString()})");
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="selector"></param>
        /// <param name="sqlBuilder"></param>
        /// <exception cref="ArgumentNullException">value is null or empty</exception>
        /// <returns></returns>
        public TSQL WhereIn<TSource>(Expression<Func<TSource, object>> selector, ISqlBuilder sqlBuilder)
        {
            if (sqlBuilder == null)
                throw new ArgumentNullException(nameof(sqlBuilder));
            AddParameter(sqlBuilder.Params);
            return Where($"{SqlExpressionVisitor.Instance.VisitSingle(selector).SqlText} IN ({sqlBuilder.GetCommandTextString()})");
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sqlBuilder"></param>
        /// <exception cref="ArgumentNullException">sqlBuilder is null</exception>
        /// <returns></returns>
        public TSQL WhereExists(ISqlBuilder sqlBuilder)
        {
            if (sqlBuilder == null)
                throw new ArgumentNullException(nameof(sqlBuilder));
            AddParameter(sqlBuilder.Params);
            return Where($"EXISTS ({sqlBuilder.GetCommandTextString()})");
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sqlBuilder"></param>
        /// <exception cref="ArgumentNullException">sqlBuilder is null</exception>
        /// <returns></returns>
        public TSQL WhereNotExists(ISqlBuilder sqlBuilder)
        {
            if (sqlBuilder == null)
                throw new ArgumentNullException(nameof(sqlBuilder));
            AddParameter(sqlBuilder.Params);
            return Where($"NOT EXISTS ({sqlBuilder.GetCommandTextString()})");
        }

        /// <summary>
        /// where any 如果values 是空或长度为0 直接返回空数据
        /// </summary>
        /// <typeparam name="TSource"></typeparam>
        /// <typeparam name="TKey"></typeparam>
        /// <param name="selector"></param>
        /// <param name="values"></param>
        /// <returns></returns>
        public TSQL WhereAnyOrDefault<TSource, TKey>(Expression<Func<TSource, TKey>> selector, IEnumerable<TKey> values)
        {
            if (values.IsNullOrEmpty()) { IsReturnDefault = true; return This; }
            var index = EntityHelper.ParamsIndex;
            AddParameter(new NpgsqlParameter<TKey[]>(index, values.ToArray()));
            return Where(string.Concat(SqlExpressionVisitor.Instance.VisitSingle(selector).SqlText, $" <> any(@{index})"));
        }

        /// <summary>
        /// where any 如果values 是空或长度为0 直接返回空数据
        /// </summary>
        /// <typeparam name="TSource"></typeparam>
        /// <typeparam name="TKey"></typeparam>
        /// <param name="selector"></param>
        /// <param name="values"></param>
        /// <returns></returns>
        public TSQL WhereAnyOrDefault<TKey>(Expression<Func<TModel, TKey>> selector, IEnumerable<TKey> values)
        {
            return WhereAnyOrDefault<TModel, TKey>(selector, values);
        }

        /// <summary>
        /// where or条件
        /// </summary>
        /// <typeparam name="T">数组类型</typeparam>
        /// <param name="filter">xxx={0}</param>
        /// <param name="val">{0}的数组</param>
        /// <param name="dbType">CLR类型</param>
        /// <example>WhereOr("xxx={0}",new[]{1,2},NpgsqlDbType.Integer)</example>
        /// <returns></returns>
        public TSQL WhereOr<T>(string filter, IEnumerable<T> val, NpgsqlDbType? dbType = null)
        {
            object[] _val = null;
            var typeT = typeof(T);
            if (val == null)
                return Where(filter, null);
            if (val.Count() == 0)
                return This;
            else if (typeT == typeof(char))
                _val = val.Select(a => new DbTypeValue(a, dbType)).ToArray<object>();
            else if (typeT == typeof(object))
                _val = dbType.HasValue ? val.Select(a => new DbTypeValue(a, dbType)).ToArray<object>() : val as object[];
            else if (typeT == typeof(DbTypeValue))
                _val = val as object[];
            else if (val.Count() == 1)
            {
                if (val.ElementAt(0) == null) return Where(filter, null);
                _val = dbType.HasValue ? new object[] { new DbTypeValue(val.ElementAt(0), dbType) } : new object[] { val.ElementAt(0) };
            }
            string filters = filter;
            if (_val == null)
            {
                for (int a = 1; a < val.Count(); a++)
                    filters = string.Concat(filters, " OR ", string.Format(filter, "{" + a + "}"));
                _val = dbType.HasValue ? val.Select(a => new DbTypeValue(a, dbType)).ToArray<object>() : val.OfType<object>().ToArray();
            }
            return Where(filters, _val);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="isAdd"></param>
        /// <param name="filter"></param>
        /// <param name="val"></param>
        /// <returns></returns>
        public TSQL Where(bool isAdd, string filter, params object[] val) => isAdd ? Where(filter, val) : This;

        /// <summary>
        /// 是否添加func返回的where语句
        /// </summary>
        /// <param name="isAdd"></param>
        /// <param name="filter"></param>
        /// <example>Where(bool, () => $"xxx='{xxx}'")</example>
        /// <returns></returns>
        public TSQL Where(bool isAdd, Func<string> filter)
        {
            if (isAdd)
                Where(filter.Invoke());
            return This;
        }

        /// <summary>
        /// 是否添加func返回的where语句, format格式
        /// </summary>
        /// <param name="isAdd">是否添加</param>
        /// <param name="filter">返回Where(string,object) </param>
        /// <example>Where(bool, () => ("xxx={0}", value))</example>
        /// <returns></returns>
        public TSQL Where(bool isAdd, Func<(string, object)> filter)
        {
            if (isAdd)
            {
                var (sql, ps) = filter.Invoke();
                Where(sql, ps);
            }
            return This;
        }

        /// <summary>
        /// 双主键
        /// </summary>
        /// <typeparam name="T1"></typeparam>
        /// <typeparam name="T2"></typeparam>
        /// <param name="keys"></param>
        /// <param name="val"></param>
        /// <param name="dbTypes"></param>
        /// <returns></returns>
        public TSQL Where<T1, T2>(string[] keys, IEnumerable<(T1, T2)> val, NpgsqlDbType?[] dbTypes = null) => WhereTuple(f =>
        {
            var item = val.ElementAt(f.Item2 / keys.Length);
            if (dbTypes == null)
            {
                f.Item1.Add(item.Item1);
                f.Item1.Add(item.Item2);
            }
            else
            {
                f.Item1.Add(new DbTypeValue(item.Item1, dbTypes[0]));
                f.Item1.Add(new DbTypeValue(item.Item2, dbTypes[1]));
            }
        }, keys, val.Count());

        /// <summary>
        /// 三主键
        /// </summary>
        /// <typeparam name="T1"></typeparam>
        /// <typeparam name="T2"></typeparam>
        /// <typeparam name="T3"></typeparam>
        /// <param name="keys"></param>
        /// <param name="val"></param>
        /// <param name="dbTypes"></param>
        /// <returns></returns>
        public TSQL Where<T1, T2, T3>(string[] keys, IEnumerable<(T1, T2, T3)> val, NpgsqlDbType?[] dbTypes = null) => WhereTuple(f =>
        {
            var item = val.ElementAt(f.Item2 / keys.Length);
            if (dbTypes == null)
            {
                f.Item1.Add(item.Item1);
                f.Item1.Add(item.Item2);
                f.Item1.Add(item.Item3);
            }
            else
            {
                f.Item1.Add(new DbTypeValue(item.Item1, dbTypes[0]));
                f.Item1.Add(new DbTypeValue(item.Item2, dbTypes[1]));
                f.Item1.Add(new DbTypeValue(item.Item3, dbTypes[2]));
            }
        }, keys, val.Count());

        /// <summary>
        /// 四主键
        /// </summary>
        /// <typeparam name="T1"></typeparam>
        /// <typeparam name="T2"></typeparam>
        /// <typeparam name="T3"></typeparam>
        /// <typeparam name="T4"></typeparam>
        /// <param name="keys"></param>
        /// <param name="val"></param>
        /// <param name="dbTypes"></param>
        /// <returns></returns>
        public TSQL Where<T1, T2, T3, T4>(string[] keys, IEnumerable<(T1, T2, T3, T4)> val, NpgsqlDbType?[] dbTypes = null) => WhereTuple(f =>
        {
            var item = val.ElementAt(f.Item2 / keys.Length);
            if (dbTypes == null)
            {
                f.Item1.Add(item.Item1); f.Item1.Add(item.Item2);
                f.Item1.Add(item.Item3); f.Item1.Add(item.Item4);
            }
            else
            {
                f.Item1.Add(new DbTypeValue(item.Item1, dbTypes[0]));
                f.Item1.Add(new DbTypeValue(item.Item2, dbTypes[1]));
                f.Item1.Add(new DbTypeValue(item.Item3, dbTypes[2]));
                f.Item1.Add(new DbTypeValue(item.Item4, dbTypes[3]));
            }
        }, keys, val.Count());

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="filter"></param>
        /// <param name="value"></param>
        /// <param name="dbType"></param>
        /// <exception cref="ArgumentNullException">value is null or empty</exception>
        /// <returns></returns>
        public TSQL WhereArray<T>(string filter, IEnumerable<T> value, NpgsqlDbType? dbType = null)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));
            return dbType.HasValue ? Where(filter, new[] { new DbTypeValue(value, dbType) }) : Where(filter, new object[] { value });
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="filter"></param>
        /// <param name="val"></param>
        /// <returns></returns>
        public TSQL Where(string filter, params object[] val)
        {
            if (val.IsNullOrEmpty())
                filter = TypeHelper.GetNullSql(filter, @"\{\d\}");
            else
            {
                for (int i = 0; i < val.Length; i++)
                {
                    var index = string.Concat("{", i, "}");
                    if (filter.IndexOf(index, StringComparison.Ordinal) == -1)
                        throw new ArgumentException(nameof(filter));
                    if (val[i] == null)
                        filter = TypeHelper.GetNullSql(filter, index.Replace("{", @"\{").Replace("}", @"\}"));
                    else
                    {
                        var pIndex = EntityHelper.ParamsIndex;
                        AddParameter(new NpgsqlParameter(pIndex, val[i]));
                        filter = filter.Replace(index, "@" + pIndex);

                    }
                }
            }
            return Where($"{filter}");
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="action"></param>
        /// <param name="keys"></param>
        /// <param name="arrLength"></param>
        /// <returns></returns>
        protected TSQL WhereTuple(Action<(List<object>, int)> action, string[] keys, int arrLength)
        {
            var parms = new List<object>();
            var count = keys.Length;
            StringBuilder sb = new StringBuilder();
            for (int a = 0; a < arrLength * count; a += count)
            {
                if (a != 0) sb.Append(" OR ");
                sb.Append("(");
                for (int b = 0; b < count; b++)
                {
                    sb.Append($"{keys[b]} = {{{a + b}}}");
                    if (b != count - 1) sb.Append(" AND ");
                }
                sb.Append(")");
                action.Invoke((parms, a));
            }
            return Where(sb.ToString(), parms.ToArray());
        }
    }
}

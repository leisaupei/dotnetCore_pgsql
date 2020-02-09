using Meta.Driver.DbHelper;
using Meta.Driver.Extensions;
using Meta.Driver.Interface;
using Meta.Driver.Model;
using Meta.Driver.SqlBuilder.AnalysisExpression;
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

namespace Meta.Driver.SqlBuilder
{
	public abstract class WhereBuilder<TSQL, TModel> : SqlBuilder<TSQL>
		where TSQL : class, ISqlBuilder
		where TModel : IDbModel, new()
	{
		/// <summary>
		/// 
		/// </summary>
		TSQL This => this as TSQL;
		/// <summary>
		/// 是否or状态
		/// </summary>
		private bool _isOrState = false;
		/// <summary>
		/// or表达式
		/// </summary>
		private readonly List<string> _orExpression = new List<string>();

		#region Constructor
		protected WhereBuilder()
		{
			if (string.IsNullOrEmpty(MainTable))
				MainTable = EntityHelper.GetTableName<TModel>();
		}
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
			AddParameters(info.Paras);
			return Where(info.SqlText);
		}

		/// <summary>
		/// 主模型重载
		/// </summary>
		/// <param name="selector"></param>
		/// <returns></returns>
		public TSQL Where(Expression<Func<TModel, bool>> selector)
			=> Where<TModel>(selector);

		/// <summary>
		/// 子模型where
		/// </summary>
		/// <typeparam name="TSource"></typeparam>
		/// <param name="selector"></param>
		/// <param name="isAdd">是否添加条件</param>
		/// <returns></returns>
		public TSQL Where<TSource>(bool isAdd, Expression<Func<TSource, bool>> selector) where TSource : IDbModel, new()
			=> isAdd ? Where(selector) : This;

		/// <summary>
		/// 主模型重载
		/// </summary>
		/// <param name="selector"></param>
		/// <param name="isAdd">是否添加条件</param>
		/// <returns></returns>
		public TSQL Where(bool isAdd, Expression<Func<TModel, bool>> selector)
			=> isAdd ? Where<TModel>(selector) : This;

		/// <summary>
		/// 开始Or where表达式
		/// </summary>
		/// <returns></returns>
		public TSQL WhereStartOr()
		{
			_isOrState = true;
			return This;
		}

		/// <summary>
		/// 结束Or where表达式
		/// </summary>
		/// <returns></returns>
		public TSQL WhereEndOr()
		{
			_isOrState = false;
			if (_orExpression.Count > 0)
			{
				Where(string.Join(" OR ", _orExpression));
				_orExpression.Clear();
			}
			return This;
		}

		/// <summary>
		/// 字符串where语句
		/// </summary>
		/// <param name="where"></param>
		/// <returns></returns>
		public TSQL Where(string where)
		{
			if (_isOrState)
				_orExpression.Add($"({where})");
			else
				base.WhereList.Add($"({where})");
			return This;
		}

		/// <summary>
		/// any
		/// </summary>
		/// <typeparam name="TSource"></typeparam>
		/// <typeparam name="TKey"></typeparam>
		/// <param name="selector"></param>
		/// <param name="values"></param>
		/// <exception cref="ArgumentNullException">values is null or length is zero</exception>
		/// <returns></returns>
		public TSQL WhereAny<TSource, TKey>(Expression<Func<TSource, TKey>> selector, IEnumerable<TKey> values) where TSource : IDbModel, new()
			=> WhereAny(SqlExpressionVisitor.Instance.VisitSingle(selector).SqlText, values);

		/// <summary>
		/// any方法
		/// </summary>
		/// <typeparam name="TKey">key类型</typeparam>
		/// <param name="key">字段名片</param>
		/// <param name="values">值</param>
		/// <returns></returns>
		public TSQL WhereAny<TKey>(string key, IEnumerable<TKey> values)
		{
			if (values.IsNullOrEmpty())
				throw new ArgumentNullException(nameof(values));
			if (values.Count() == 1)
			{
				AddParameterT(values.ElementAt(0), out string index1);
				return Where(string.Concat(key, $" = @{index1}"));
			}
			AddParameterT(values.ToArray(), out string index);
			return Where(string.Concat(key, $" = any(@{index})"));
		}

		/// <summary>
		/// any 方法, optional字段
		/// </summary>
		/// <typeparam name="TSource"></typeparam>
		/// <typeparam name="TKey"></typeparam>
		/// <param name="selector"></param>
		/// <param name="values"></param>
		/// <exception cref="ArgumentNullException">values is null or length is zero</exception>
		/// <returns></returns>
		public TSQL WhereAny<TSource, TKey>(Expression<Func<TSource, TKey?>> selector, IEnumerable<TKey> values) where TSource : IDbModel, new() where TKey : struct
			=> WhereAny(SqlExpressionVisitor.Instance.VisitSingle(selector).SqlText, values);

		/// <summary>
		/// any方法
		/// </summary>
		/// <typeparam name="TKey"></typeparam>
		/// <param name="selector"></param>
		/// <param name="values"></param>
		/// <exception cref="ArgumentNullException">values is null or length is zero</exception>
		/// <returns></returns>
		public TSQL WhereAny<TKey>(Expression<Func<TModel, TKey>> selector, IEnumerable<TKey> values)
			=> WhereAny<TModel, TKey>(selector, values);

		/// <summary>
		/// any 方法, optional字段
		/// </summary>
		/// <typeparam name="TKey"></typeparam>
		/// <param name="selector"></param>
		/// <param name="values"></param>
		/// <exception cref="ArgumentNullException">values is null or length is zero</exception>
		/// <returns></returns>
		public TSQL WhereAny<TKey>(Expression<Func<TModel, TKey?>> selector, IEnumerable<TKey> values) where TKey : struct
			=> WhereAny<TModel, TKey>(selector, values);

		/// <summary>
		/// not equals any 方法
		/// </summary>
		/// <typeparam name="TSource"></typeparam>
		/// <typeparam name="TKey"></typeparam>
		/// <param name="selector"></param>
		/// <param name="values"></param>
		/// <exception cref="ArgumentNullException">values is null or length is zero</exception>
		/// <returns></returns>
		public TSQL WhereNotAny<TSource, TKey>(Expression<Func<TSource, TKey>> selector, IEnumerable<TKey> values) where TSource : IDbModel, new()
			=> WhereNotAny(SqlExpressionVisitor.Instance.VisitSingle(selector).SqlText, values);

		/// <summary>
		/// not equals any 方法, optional字段
		/// </summary>
		/// <typeparam name="TSource"></typeparam>
		/// <typeparam name="TKey"></typeparam>
		/// <param name="selector"></param>
		/// <param name="values"></param>
		/// <exception cref="ArgumentNullException">values is null or length is zero</exception>
		/// <returns></returns>
		public TSQL WhereNotAny<TSource, TKey>(Expression<Func<TSource, TKey?>> selector, IEnumerable<TKey> values) where TSource : IDbModel, new() where TKey : struct
			=> WhereNotAny(SqlExpressionVisitor.Instance.VisitSingle(selector).SqlText, values);

		/// <summary>
		/// not equals any 方法
		/// </summary>
		/// <typeparam name="TKey"></typeparam>
		/// <param name="key"></param>
		/// <param name="values"></param>
		/// <returns></returns>
		public TSQL WhereNotAny<TKey>(string key, IEnumerable<TKey> values)
		{
			if (values.IsNullOrEmpty())
				throw new ArgumentNullException(nameof(values));
			if (values.Count() == 1)
			{
				AddParameterT(values.ElementAt(0), out string index1);
				return Where(string.Concat(key, $" <> @{index1}"));
			}
			AddParameterT(values.ToArray(), out string index);
			return Where(string.Concat(key, $" <> any(@{index})"));
		}

		/// <summary>
		/// where not in
		/// </summary>
		/// <param name="selector"></param>
		/// <param name="sqlBuilder"></param>
		/// <exception cref="ArgumentNullException">sql is null or empty</exception>
		/// <returns></returns>
		public TSQL WhereNotIn<TSource>(Expression<Func<TSource, object>> selector, ISqlBuilder sqlBuilder) where TSource : IDbModel, new()
		{
			if (sqlBuilder == null)
				throw new ArgumentNullException(nameof(sqlBuilder));
			AddParameters(sqlBuilder.Params);
			return Where($"{SqlExpressionVisitor.Instance.VisitSingle(selector).SqlText} NOT IN ({sqlBuilder.CommandText})");
		}

		/// <summary>
		/// where in
		/// </summary>
		/// <param name="selector"></param>
		/// <param name="sqlBuilder"></param>
		/// <exception cref="ArgumentNullException">value is null or empty</exception>
		/// <returns></returns>
		public TSQL WhereIn<TSource>(Expression<Func<TSource, object>> selector, ISqlBuilder sqlBuilder) where TSource : IDbModel, new()
		{
			if (sqlBuilder == null)
				throw new ArgumentNullException(nameof(sqlBuilder));
			AddParameters(sqlBuilder.Params);
			return Where($"{SqlExpressionVisitor.Instance.VisitSingle(selector).SqlText} IN ({sqlBuilder.CommandText})");
		}

		/// <summary>
		/// where not in
		/// </summary>
		/// <param name="selector"></param>
		/// <param name="sqlBuilder"></param>
		/// <exception cref="ArgumentNullException">sql is null or empty</exception>
		/// <returns></returns>
		public TSQL WhereNotIn(Expression<Func<TModel, object>> selector, ISqlBuilder sqlBuilder)
			=> WhereNotIn(selector, sqlBuilder);

		/// <summary>
		/// where in
		/// </summary>
		/// <param name="selector"></param>
		/// <param name="sqlBuilder"></param>
		/// <exception cref="ArgumentNullException">value is null or empty</exception>
		/// <returns></returns>
		public TSQL WhereIn(Expression<Func<TModel, object>> selector, ISqlBuilder sqlBuilder)
			=> WhereIn(selector, sqlBuilder);

		/// <summary>
		/// where exists 
		/// </summary>
		/// <param name="sqlBuilder"></param>
		/// <exception cref="ArgumentNullException">sqlBuilder is null</exception>
		/// <returns></returns>
		public TSQL WhereExists(ISqlBuilder sqlBuilder)
		{
			if (sqlBuilder == null)
				throw new ArgumentNullException(nameof(sqlBuilder));
			AddParameters(sqlBuilder.Params);
			sqlBuilder.Fields = "1";
			return Where($"EXISTS ({sqlBuilder.CommandText})");
		}

		/// <summary>
		/// where exists 
		/// </summary>
		/// <param name="sqlBuilderSelector"></param>
		/// <returns></returns>
		private TSQL WhereExists(Expression<Func<TModel, ISqlBuilder>> sqlBuilderSelector)
		{
			return This;
		}

		/// <summary>
		/// where not exists 
		/// </summary>
		/// <param name="sqlBuilder"></param>
		/// <exception cref="ArgumentNullException">sqlBuilder is null</exception>
		/// <returns></returns>
		public TSQL WhereNotExists(ISqlBuilder sqlBuilder)
		{
			if (sqlBuilder == null)
				throw new ArgumentNullException(nameof(sqlBuilder));
			AddParameters(sqlBuilder.Params);
			sqlBuilder.Fields = "1";
			return Where($"NOT EXISTS ({sqlBuilder.CommandText})");
		}

		/// <summary>
		/// where not exists 
		/// </summary>
		/// <param name="sqlBuilderSelector"></param>
		/// <returns></returns>
		private TSQL WhereNotExists(Expression<Func<TModel, ISqlBuilder>> sqlBuilderSelector)
		{
			return This;
		}

		/// <summary>
		/// where any 如果values 是空或长度为0 直接返回空数据(无论 or and 什么条件)
		/// </summary>
		/// <typeparam name="TSource"></typeparam>
		/// <typeparam name="TKey"></typeparam>
		/// <param name="selector"></param>
		/// <param name="values"></param>
		/// <returns></returns>
		public TSQL WhereAnyOrDefault<TSource, TKey>(Expression<Func<TSource, TKey>> selector, IEnumerable<TKey> values)
			where TSource : IDbModel, new()
		{
			if (values.IsNullOrEmpty()) { IsReturnDefault = true; return This; }
			return WhereAny(selector, values);
		}

		/// <summary>
		/// where any 如果values 是空或长度为0 直接返回空数据(无论 or and 什么条件)
		/// </summary>
		/// <typeparam name="TKey"></typeparam>
		/// <param name="selector"></param>
		/// <param name="values"></param>
		/// <returns></returns>
		public TSQL WhereAnyOrDefault<TKey>(Expression<Func<TModel, TKey>> selector, IEnumerable<TKey> values)
			=> WhereAnyOrDefault<TModel, TKey>(selector, values);

		/// <summary>
		/// 可选添加, format写法
		/// </summary>
		/// <param name="isAdd"></param>
		/// <param name="filter"></param>
		/// <param name="values"></param>
		/// <returns></returns>
		public TSQL Where(bool isAdd, string filter, params object[] values) => isAdd ? Where(filter, values) : This;

		/// <summary>
		/// 可选添加添加func返回的where语句
		/// </summary>
		/// <param name="isAdd"></param>
		/// <param name="filter"></param>
		/// <returns></returns>
		public TSQL Where(bool isAdd, Func<string> filter)
		{
			if (isAdd)
				Where(filter.Invoke());
			return This;
		}

		/// <summary>
		/// 是否添加 添加func返回的where语句, format格式
		/// </summary>
		/// <param name="isAdd">是否添加</param>
		/// <param name="filter">返回Where(string,object) </param>
		/// <returns></returns>
		public TSQL Where(bool isAdd, Func<(string, object[])> filter)
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
		/// <param name="selectorT1"></param>
		/// <param name="selectorT2"></param>
		/// <param name="values"></param>
		/// <returns></returns>
		public TSQL Where<T1, T2>(
			Expression<Func<TModel, T1>> selectorT1,
			Expression<Func<TModel, T2>> selectorT2,
			IEnumerable<(T1, T2)> values)
		{
			var _values = values.ToArray();
			var t1 = SqlExpressionVisitor.Instance.VisitSingle(selectorT1).SqlText;
			var t2 = SqlExpressionVisitor.Instance.VisitSingle(selectorT2).SqlText;
			for (int i = 0; i < _values.Count(); i++)
			{
				WhereStartOr();
				Where(string.Concat(t1, "={0}"), _values[i].Item1);
				Where(string.Concat(t2, "={0}"), _values[i].Item2);
				WhereEndOr();
			}
			return This;
		}

		/// <summary>
		/// 三主键
		/// </summary>
		/// <typeparam name="T1"></typeparam>
		/// <typeparam name="T2"></typeparam>
		/// <typeparam name="T3"></typeparam>
		/// <param name="selectorT1"></param>
		/// <param name="selectorT2"></param>
		/// <param name="selectorT3"></param>
		/// <param name="values"></param>
		/// <returns></returns>
		public TSQL Where<T1, T2, T3>(
			Expression<Func<TModel, T1>> selectorT1,
			Expression<Func<TModel, T2>> selectorT2,
			Expression<Func<TModel, T3>> selectorT3,
			IEnumerable<(T1, T2, T3)> values)
		{
			var _values = values.ToArray();
			var t1 = SqlExpressionVisitor.Instance.VisitSingle(selectorT1).SqlText;
			var t2 = SqlExpressionVisitor.Instance.VisitSingle(selectorT2).SqlText;
			var t3 = SqlExpressionVisitor.Instance.VisitSingle(selectorT3).SqlText;
			for (int i = 0; i < _values.Count(); i++)
			{
				WhereStartOr();
				Where(string.Concat(t1, "={0}"), _values[i].Item1);
				Where(string.Concat(t2, "={0}"), _values[i].Item2);
				Where(string.Concat(t3, "={0}"), _values[i].Item3);
				WhereEndOr();
			}
			return This;
		}

		/// <summary>
		/// 四主键
		/// </summary>
		/// <typeparam name="T1"></typeparam>
		/// <typeparam name="T2"></typeparam>
		/// <typeparam name="T3"></typeparam>
		/// <typeparam name="T4"></typeparam>
		/// <param name="selectorT1"></param>
		/// <param name="selectorT2"></param>
		/// <param name="selectorT3"></param>
		/// <param name="selectorT4"></param>
		/// <param name="values"></param>
		/// <returns></returns>
		public TSQL Where<T1, T2, T3, T4>(
			Expression<Func<TModel, T1>> selectorT1,
			Expression<Func<TModel, T2>> selectorT2,
			Expression<Func<TModel, T3>> selectorT3,
			Expression<Func<TModel, T4>> selectorT4,
			IEnumerable<(T1, T2, T3, T4)> values)
		{
			var _values = values.ToArray();
			var t1 = SqlExpressionVisitor.Instance.VisitSingle(selectorT1).SqlText;
			var t2 = SqlExpressionVisitor.Instance.VisitSingle(selectorT2).SqlText;
			var t3 = SqlExpressionVisitor.Instance.VisitSingle(selectorT3).SqlText;
			var t4 = SqlExpressionVisitor.Instance.VisitSingle(selectorT4).SqlText;
			for (int i = 0; i < _values.Count(); i++)
			{
				WhereStartOr();
				Where(string.Concat(t1, "={0}"), _values[i].Item1);
				Where(string.Concat(t2, "={0}"), _values[i].Item2);
				Where(string.Concat(t3, "={0}"), _values[i].Item3);
				Where(string.Concat(t4, "={0}"), _values[i].Item4);
				WhereEndOr();
			}
			return This;
		}

		/// <summary>
		/// where format 写法
		/// </summary>
		/// <param name="filter"></param>
		/// <param name="values"></param>
		/// <returns></returns>
		public TSQL Where(string filter, params object[] values)
		{
			if (values.IsNullOrEmpty())
				return Where(TypeHelper.GetNullSql(filter, @"\{\d\}"));

			for (int i = 0; i < values.Length; i++)
			{
				var index = string.Concat("{", i, "}");
				if (filter.IndexOf(index, StringComparison.Ordinal) == -1)
					throw new ArgumentException(nameof(filter));
				if (values[i] == null)
					filter = TypeHelper.GetNullSql(filter, index.Replace("{", @"\{").Replace("}", @"\}"));
				else
				{
					AddParameter(values[i], out string pIndex);
					filter = filter.Replace(index, "@" + pIndex);
				}
			}
			return Where(filter);
		}

	}
}

using Meta.Common.DbHelper;
using Meta.Common.Interface;
using Meta.Common.Model;
using Meta.Common.SqlBuilder.AnalysisExpression;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Text.RegularExpressions;

namespace Meta.Common.SqlBuilder
{
	public abstract class SelectBuilder<TSQL, TModel> : WhereBase<TSQL> where TSQL : class where TModel : IDbModel, new()
	{
		#region Identity
		readonly UnionCollection _unionCollection;
		string _groupBy;
		string _orderBy;
		int? _limit;
		int? _offset;
		string _having;
		string _union;
		string _tablesampleSystem;
		#endregion

		#region Constructor
		protected SelectBuilder(string fields, string alias) : this(fields)
		{
			MainAlias = alias;
		}
		protected SelectBuilder(string fields) : this()
		{
			Fields = fields;
		}
		protected SelectBuilder()
		{
			MainTable = EntityHelper.GetTableName<TModel>();
			_unionCollection = new UnionCollection(MainAlias);
		}

		#endregion

		TSQL This => this as TSQL;

		///// <summary>
		///// from 表名 别名
		///// </summary>
		///// <param name="table"></param>
		///// <param name="alias"></param>
		///// <returns></returns>
		//public TSQL From(string table, string alias = "a")
		//{
		//	MainAlias = alias;
		//	if (new Regex(@"^SELECT\s.+\sFROM\s").IsMatch(table))
		//		MainTable = $"({table})";
		//	else
		//		MainTable = table;
		//	return This;
		//}

		/// <summary>
		/// sql语句group by
		/// </summary>
		/// <param name="s"></param>
		/// <example>GroupBy("xxx,xxx")</example>
		/// <returns></returns>
		public TSQL GroupBy(string s)
		{
			if (!string.IsNullOrEmpty(_groupBy))
				_groupBy += ", ";
			_groupBy = s;
			return This;
		}

		/// <summary>
		/// sql语句order by
		/// </summary>
		/// <param name="s"></param>
		/// <example>OrderBy("xxx desc,xxx asc")</example>
		/// <returns></returns>
		public TSQL OrderBy(string s)
		{
			if (!string.IsNullOrEmpty(_orderBy))
				_orderBy += ", ";
			_orderBy = s;
			return This;
		}

		/// <summary>
		/// having
		/// </summary>
		/// <param name="s"></param>
		/// <returns></returns>
		public TSQL Having(string s)
		{
			_having = s;
			return This;
		}

		/// <summary>
		/// limit
		/// </summary>
		/// <param name="i"></param>
		/// <returns></returns>
		public TSQL Limit(int i)
		{
			_limit = i;
			return This;
		}

		/// <summary>
		/// 等于数据库offset
		/// </summary>
		/// <param name="i"></param>
		/// <returns></returns>
		public TSQL Skip(int i)
		{
			_offset = i;
			return This;
		}

		/// <summary>
		/// 连接一个sql语句
		/// </summary>
		/// <param name="view"></param>
		/// <returns></returns>
		public TSQL Union(string view)
		{
			_union = $"({view})";
			return This;
		}

		/// <summary>
		/// 连接 selectbuilder
		/// </summary>
		/// <param name="selectBuilder"></param>
		/// <returns></returns>
		public TSQL Union(TSQL selectBuilder)
		{
			_union = $"({selectBuilder})";
			return This;
		}

		/// <summary>
		/// 分页
		/// </summary>
		/// <param name="pageIndex"></param>
		/// <param name="pageSize"></param>
		/// <returns></returns>
		public TSQL Page(int pageIndex, int pageSize)
		{
			Limit(pageSize); Skip(Math.Max(0, pageIndex - 1) * pageSize);
			return This;
		}

		/// <summary>
		/// 随机抽样
		/// </summary>
		/// <param name="percent">seed</param>
		/// <returns></returns>
		public TSQL TableSampleSystem(double percent)
		{
			_tablesampleSystem = $" tablesample system({percent}) ";
			return This;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="selector"></param>
		/// <returns></returns>
		public TSQL OrderBy(Expression<Func<TModel, object>> selector) => OrderBy<TModel>(selector);

		/// <summary>
		/// 
		/// </summary>
		/// <param name="selector"></param>
		/// <returns></returns>
		public TSQL GroupBy(Expression<Func<TModel, object>> selector) => GroupBy<TModel>(selector);

		/// <summary>
		/// 
		/// </summary>
		/// <param name="selector"></param>
		/// <returns></returns>
		public TSQL OrderByDescending(Expression<Func<TModel, object>> selector) => OrderByDescending<TModel>(selector);

		/// <summary>
		/// 
		/// </summary>
		/// <typeparam name="TSource"></typeparam>
		/// <param name="selector"></param>
		/// <returns></returns>
		public TSQL OrderBy<TSource>(Expression<Func<TSource, object>> selector) where TSource : IDbModel, new()
		{
			return OrderBy(SqlExpressionVisitor.Instance.VisitSingle(selector).SqlText);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <typeparam name="TSource"></typeparam>
		/// <param name="selector"></param>
		/// <returns></returns>
		public TSQL GroupBy<TSource>(Expression<Func<TSource, object>> selector) where TSource : IDbModel, new()
		{
			return GroupBy(SqlExpressionVisitor.Instance.VisitSingle(selector).SqlText);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <typeparam name="TSource"></typeparam>
		/// <param name="selector"></param>
		/// <returns></returns>
		public TSQL OrderByDescending<TSource>(Expression<Func<TSource, object>> selector) where TSource : IDbModel, new()
		{
			return OrderBy(string.Concat(SqlExpressionVisitor.Instance.VisitSingle(selector).SqlText, " desc"));
		}

		public TSQL Where(Expression<Func<TModel, bool>> selector) => base.Where(selector);

		/// <summary>
		/// 返回列表(管道)
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="fields"></param>
		/// <returns></returns>
		public TSQL ToListPipe<T>(string fields = null)
		{
			if (!string.IsNullOrEmpty(fields)) Fields = fields;
			return base.ToPipe<T>(PipeReturnType.List);
		}

		/// <summary>
		/// 返回列表(管道)
		/// </summary>
		/// <param name="fields"></param>
		/// <returns></returns>
		public TSQL ToListPipe(string fields = null) => this.ToListPipe<TModel>(fields);

		/// <summary>
		/// 返回列表
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="fields"></param>
		/// <returns></returns>
		public List<T> ToList<T>(string fields = null)
		{
			if (!string.IsNullOrEmpty(fields)) Fields = fields;
			if (IsReturnDefault) return new List<T>();
			return base.ToList<T>();
		}

		/// <summary>
		/// 返回列表
		/// </summary>
		/// <typeparam name="TKey"></typeparam>
		/// <param name="selector"></param>
		/// <returns></returns>
		public List<TKey> ToList<TKey>(Expression<Func<TModel, TKey>> selector) => ToList<TModel, TKey>(selector);

		/// <summary>
		/// 返回列表
		/// </summary>
		/// <typeparam name="TSource"></typeparam>
		/// <typeparam name="TKey"></typeparam>
		/// <param name="selector"></param>
		/// <returns></returns>
		public List<TKey> ToList<TSource, TKey>(Expression<Func<TSource, TKey>> selector) where TSource : IDbModel, new()
		{
			return ToList<TKey>(SqlExpressionVisitor.Instance.VisitSingle(selector).SqlText);
		}

		/// <summary>
		/// 返回列表
		/// </summary>
		/// <returns></returns>
		public List<TModel> ToList() => this.ToList<TModel>();

		/// <summary>
		/// 返回一行(管道)
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="fields"></param>
		/// <returns></returns>
		public TSQL ToOnePipe<T>(string fields = null)
		{
			Limit(1);
			if (!string.IsNullOrEmpty(fields)) Fields = fields;
			return base.ToPipe<T>(PipeReturnType.One);
		}

		/// <summary>
		/// 返回一行(管道)
		/// </summary>
		/// <param name="fields"></param>
		/// <returns></returns>
		public TSQL ToOnePipe(string fields = null) => this.ToOnePipe<TModel>(fields);

		/// <summary>
		/// 返回一行
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="fields"></param>
		/// <returns></returns>
		public T ToOne<T>(string fields = null)
		{
			Limit(1);
			if (!string.IsNullOrEmpty(fields)) Fields = fields;
			return base.ToOne<T>();
		}

		/// <summary>
		/// 返回一行
		/// </summary>
		/// <returns></returns>
		public TModel ToOne() => this.ToOne<TModel>();

		/// <summary>
		/// 返回一行
		/// </summary>
		/// <typeparam name="TKey"></typeparam>
		/// <param name="selector"></param>
		/// <returns></returns>
		public TKey ToOne<TKey>(Expression<Func<TModel, TKey>> selector) => ToOne<TModel, TKey>(selector);

		/// <summary>
		/// 返回一行
		/// </summary>
		/// <typeparam name="TSource"></typeparam>
		/// <typeparam name="TKey"></typeparam>
		/// <param name="selector"></param>
		/// <returns></returns>
		public TKey ToOne<TSource, TKey>(Expression<Func<TSource, TKey>> selector) where TSource : IDbModel, new() => this.ToScalar<TSource, TKey>(selector);


		/// <summary>
		/// 返回第一个元素
		/// </summary>
		/// <typeparam name="TKey"></typeparam>
		/// <param name="fields"></param>
		/// <returns></returns>
		public TKey ToScalar<TKey>(string fields)
		{
			Limit(1);
			Fields = fields;
			return (TKey)ToScalar();
		}

		/// <summary>
		/// 返回第一个元素
		/// </summary>
		/// <typeparam name="TKey"></typeparam>
		/// <param name="selector"></param>
		/// <returns></returns>
		public TKey ToScalar<TKey>(Expression<Func<TModel, TKey>> selector) => this.ToScalar<TModel, TKey>(selector);

		/// <summary>
		/// 返回第一个元素
		/// </summary>
		/// <typeparam name="TSource"></typeparam>
		/// <typeparam name="TKey"></typeparam>
		/// <param name="selector"></param>
		/// <returns></returns>
		public TKey ToScalar<TSource, TKey>(Expression<Func<TSource, TKey>> selector) where TSource : IDbModel, new()
		{
			return ToScalar<TKey>(SqlExpressionVisitor.Instance.VisitSingle(selector).SqlText);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <returns></returns>
		public long Count() => ToScalar<long>("COUNT(1)");

		/// <summary>
		/// 
		/// </summary>
		/// <typeparam name="TSource">model类型</typeparam>
		/// <typeparam name="TKey">返回值类型</typeparam>
		/// <param name="selector">key selector</param>
		/// <param name="defaultValue">默认值</param>
		/// <returns></returns>
		public TKey Max<TSource, TKey>(Expression<Func<TSource, TKey>> selector, TKey defaultValue = default) where TSource : IDbModel, new()
		{
			return ScalarTransfer(selector, defaultValue, "MAX");
		}

		private TKey ScalarTransfer<TSource, TKey>(Expression<Func<TSource, TKey>> selector, TKey defaultValue, string method) where TSource : IDbModel, new()
		{
			return ToScalar<TKey>($"COALESCE({method}({SqlExpressionVisitor.Instance.VisitSingle(selector).SqlText}),{defaultValue})");
		}

		/// <summary>
		/// 
		/// </summary>
		/// <typeparam name="TSource"></typeparam>
		/// <typeparam name="TKey"></typeparam>
		/// <param name="selector"></param>
		/// <param name="defaultValue"></param>
		/// <returns></returns>
		public TKey Min<TSource, TKey>(Expression<Func<TSource, TKey>> selector, TKey defaultValue = default) where TSource : IDbModel, new()
		{
			return ScalarTransfer(selector, defaultValue, "MIN");
		}

		/// <summary>
		/// 
		/// </summary>
		/// <typeparam name="TSource"></typeparam>
		/// <typeparam name="TKey"></typeparam>
		/// <param name="selector"></param>
		/// <param name="defaultValue"></param>
		/// <returns></returns>
		public TKey Sum<TSource, TKey>(Expression<Func<TSource, TKey>> selector, TKey defaultValue = default) where TSource : IDbModel, new()
		{
			return ScalarTransfer(selector, defaultValue, "SUM");
		}

		/// <summary>
		/// 
		/// </summary>
		/// <typeparam name="TSource"></typeparam>
		/// <typeparam name="TKey"></typeparam>
		/// <param name="selector"></param>
		/// <param name="defaultValue"></param>
		/// <returns></returns>
		public TKey Avg<TSource, TKey>(Expression<Func<TSource, TKey>> selector, TKey defaultValue = default) where TSource : IDbModel, new()
		{
			return ScalarTransfer(selector, defaultValue, "AVG");
		}

		/// <summary>
		/// 
		/// </summary>
		/// <typeparam name="TKey">返回值类型</typeparam>
		/// <param name="selector">字段</param>
		/// <param name="defaultValue">默认值</param>
		/// <returns></returns>
		public TKey Max<TKey>(Expression<Func<TModel, TKey>> selector, TKey defaultValue = default) => Max<TModel, TKey>(selector, defaultValue);

		/// <summary>
		/// 
		/// </summary>
		/// <typeparam name="TKey"></typeparam>
		/// <param name="selector"></param>
		/// <param name="defaultValue"></param>
		/// <returns></returns>
		public TKey Min<TKey>(Expression<Func<TModel, TKey>> selector, TKey defaultValue = default) => Min<TModel, TKey>(selector, defaultValue);

		/// <summary>
		/// 
		/// </summary>
		/// <typeparam name="TKey"></typeparam>
		/// <param name="selector"></param>
		/// <param name="defaultValue"></param>
		/// <returns></returns>
		public TKey Sum<TKey>(Expression<Func<TModel, TKey>> selector, TKey defaultValue = default) => Sum<TModel, TKey>(selector, defaultValue);

		/// <summary>
		/// 
		/// </summary>
		/// <typeparam name="TKey"></typeparam>
		/// <param name="selector"></param>
		/// <param name="defaultValue"></param>
		/// <returns></returns>
		public TKey Avg<TKey>(Expression<Func<TModel, TKey>> selector, TKey defaultValue = default) => Avg<TModel, TKey>(selector, defaultValue);
		#region Union
		/// <summary>
		/// 
		/// </summary>
		/// <typeparam name="TTarget"></typeparam>
		/// <param name="predicate"></param>
		/// <returns></returns>
		public TSQL InnerJoin<TTarget>(Expression<Func<TModel, TTarget, bool>> predicate) where TTarget : IDbModel, new()
			=> this.InnerJoin<TModel, TTarget>(predicate);

		/// <summary>
		/// 
		/// </summary>
		/// <typeparam name="TTarget"></typeparam>
		/// <param name="predicate"></param>
		/// <returns></returns>
		public TSQL LeftJoin<TTarget>(Expression<Func<TModel, TTarget, bool>> predicate) where TTarget : IDbModel, new()
				  => this.LeftJoin<TModel, TTarget>(predicate);

		/// <summary>
		/// 
		/// </summary>
		/// <typeparam name="TTarget"></typeparam>
		/// <param name="predicate"></param>
		/// <returns></returns>
		public TSQL RightJoin<TTarget>(Expression<Func<TModel, TTarget, bool>> predicate) where TTarget : IDbModel, new()
				  => this.RightJoin<TModel, TTarget>(predicate);

		/// <summary>
		/// 
		/// </summary>
		/// <typeparam name="TTarget"></typeparam>
		/// <param name="predicate"></param>
		/// <returns></returns>
		public TSQL InnerJoinRet<TTarget>(Expression<Func<TModel, TTarget, bool>> predicate) where TTarget : IDbModel, new()
			=> this.InnerJoinRet<TModel, TTarget>(predicate);

		/// <summary>
		/// 
		/// </summary>
		/// <typeparam name="TTarget"></typeparam>
		/// <param name="predicate"></param>
		/// <returns></returns>
		public TSQL LeftJoinRet<TTarget>(Expression<Func<TModel, TTarget, bool>> predicate) where TTarget : IDbModel, new()
			=> this.LeftJoinRet<TModel, TTarget>(predicate);

		/// <summary>
		/// 
		/// </summary>
		/// <typeparam name="TTarget"></typeparam>
		/// <param name="predicate"></param>
		/// <returns></returns>
		public TSQL RightJoinRet<TTarget>(Expression<Func<TModel, TTarget, bool>> predicate) where TTarget : IDbModel, new()
			=> this.RightJoinRet<TModel, TTarget>(predicate);

		/// <summary>
		/// 
		/// </summary>
		/// <typeparam name="TSource"></typeparam>
		/// <typeparam name="TTarget"></typeparam>
		/// <param name="predicate"></param>
		/// <returns></returns>
		public TSQL InnerJoin<TSource, TTarget>(Expression<Func<TSource, TTarget, bool>> predicate) where TSource : IDbModel, new() where TTarget : IDbModel, new()
			=> Join(predicate, UnionEnum.INNER_JOIN, false);

		/// <summary>
		/// 
		/// </summary>
		/// <typeparam name="TSource"></typeparam>
		/// <typeparam name="TTarget"></typeparam>
		/// <param name="predicate"></param>
		/// <returns></returns>
		public TSQL LeftJoin<TSource, TTarget>(Expression<Func<TSource, TTarget, bool>> predicate) where TSource : IDbModel, new() where TTarget : IDbModel, new()
			=> Join(predicate, UnionEnum.INNER_JOIN, false);

		/// <summary>
		/// 
		/// </summary>
		/// <typeparam name="TSource"></typeparam>
		/// <typeparam name="TTarget"></typeparam>
		/// <param name="predicate"></param>
		/// <returns></returns>
		public TSQL RightJoin<TSource, TTarget>(Expression<Func<TSource, TTarget, bool>> predicate) where TSource : IDbModel, new() where TTarget : IDbModel, new()
			=> Join(predicate, UnionEnum.INNER_JOIN, false);

		/// <summary>
		/// 
		/// </summary>
		/// <typeparam name="TSource"></typeparam>
		/// <typeparam name="TTarget"></typeparam>
		/// <param name="predicate"></param>
		/// <returns></returns>
		public TSQL InnerJoinRet<TSource, TTarget>(Expression<Func<TSource, TTarget, bool>> predicate) where TSource : IDbModel, new() where TTarget : IDbModel, new()
			=> Join(predicate, UnionEnum.INNER_JOIN, true);

		/// <summary>
		/// 
		/// </summary>
		/// <typeparam name="TSource"></typeparam>
		/// <typeparam name="TTarget"></typeparam>
		/// <param name="predicate"></param>
		/// <returns></returns>
		public TSQL LeftJoinRet<TSource, TTarget>(Expression<Func<TSource, TTarget, bool>> predicate) where TSource : IDbModel, new() where TTarget : IDbModel, new()
			=> Join(predicate, UnionEnum.INNER_JOIN, true);

		/// <summary>
		/// 
		/// </summary>
		/// <typeparam name="TSource"></typeparam>
		/// <typeparam name="TTarget"></typeparam>
		/// <param name="predicate"></param>
		/// <returns></returns>
		public TSQL RightJoinRet<TSource, TTarget>(Expression<Func<TSource, TTarget, bool>> predicate) where TSource : IDbModel, new() where TTarget : IDbModel, new()
			=> Join(predicate, UnionEnum.INNER_JOIN, true);

		/// <summary>
		/// 
		/// </summary>
		/// <param name="table"></param>
		/// <param name="alias"></param>
		/// <param name="on"></param>
		/// <returns></returns>
		public TSQL InnerJoin(string table, string alias, string on) => Join(UnionEnum.INNER_JOIN, table, alias, on);

		/// <summary>
		/// 
		/// </summary>
		/// <param name="table"></param>
		/// <param name="alias"></param>
		/// <param name="on"></param>
		/// <returns></returns>
		public TSQL LeftJoin(string table, string alias, string on) => Join(UnionEnum.LEFT_JOIN, table, alias, on);

		/// <summary>
		/// 
		/// </summary>
		/// <param name="table"></param>
		/// <param name="alias"></param>
		/// <param name="on"></param>
		/// <returns></returns>
		public TSQL RightJoin(string table, string alias, string on) => Join(UnionEnum.RIGHT_JOIN, table, alias, on);

		/// <summary>
		/// 
		/// </summary>
		/// <typeparam name="TSource"></typeparam>
		/// <typeparam name="TTarget"></typeparam>
		/// <param name="predicate"></param>
		/// <param name="unionType"></param>
		/// <param name="isReturn"></param>
		/// <returns></returns>
		public TSQL Join<TSource, TTarget>(Expression<Func<TSource, TTarget, bool>> predicate, UnionEnum unionType, bool isReturn = false) where TTarget : IDbModel, new() where TSource : IDbModel, new()
		{
			_unionCollection.Add(predicate, unionType, isReturn);
			return This;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="unionType"></param>
		/// <param name="table"></param>
		/// <param name="aliasName"></param>
		/// <param name="on"></param>
		/// <returns></returns>
		public TSQL Join(UnionEnum unionType, string table, string aliasName, string on)
		{
			_unionCollection.Add(new UnionModel(aliasName, table, on, unionType));
			return This;
		}
		#endregion

		#region ToUnion
		/// <summary>
		/// 
		/// </summary>
		/// <typeparam name="T1"></typeparam>
		/// <returns></returns>
		public (TModel, T1) ToOneUnion<T1>() where T1 : IDbModel, new()
			=> this.ToOne<(TModel, T1)>();

		/// <summary>
		/// 
		/// </summary>
		/// <typeparam name="T1"></typeparam>
		/// <returns></returns>
		public List<(TModel, T1)> ToListUnion<T1>() where T1 : IDbModel, new()
			=> this.ToList<(TModel, T1)>();

		/// <summary>
		/// 
		/// </summary>
		/// <typeparam name="T1"></typeparam>
		/// <returns></returns>
		public TSQL ToOneUnionPipe<T1>() where T1 : IDbModel, new()
			=> this.ToOnePipe<(TModel, T1)>();

		/// <summary>
		/// 
		/// </summary>
		/// <typeparam name="T1"></typeparam>
		/// <returns></returns>
		public TSQL ToListUnionPipe<T1>() where T1 : IDbModel, new()
			=> this.ToListPipe<(TModel, T1)>();

		/// <summary>
		/// 
		/// </summary>
		/// <typeparam name="T1"></typeparam>
		/// <typeparam name="T2"></typeparam>
		/// <returns></returns>
		public (TModel, T1, T2) ToOneUnion<T1, T2>() where T1 : IDbModel, new() where T2 : IDbModel, new()
			=> this.ToOne<(TModel, T1, T2)>();

		/// <summary>
		/// 
		/// </summary>
		/// <typeparam name="T1"></typeparam>
		/// <typeparam name="T2"></typeparam>
		/// <returns></returns>
		public List<(TModel, T1, T2)> ToListUnion<T1, T2>() where T1 : IDbModel, new() where T2 : IDbModel, new()
			=> this.ToList<(TModel, T1, T2)>();

		/// <summary>
		/// 
		/// </summary>
		/// <typeparam name="T1"></typeparam>
		/// <typeparam name="T2"></typeparam>
		/// <returns></returns>
		public TSQL ToOneUnionPipe<T1, T2>() where T1 : IDbModel, new() where T2 : IDbModel, new()
			=> this.ToOnePipe<(TModel, T1, T2)>();

		/// <summary>
		/// 
		/// </summary>
		/// <typeparam name="T1"></typeparam>
		/// <typeparam name="T2"></typeparam>
		/// <returns></returns>
		public TSQL ToListUnionPipe<T1, T2>() where T1 : IDbModel, new() where T2 : IDbModel, new()
			=> this.ToListPipe<(TModel, T1, T2)>();

		/// <summary>
		/// 
		/// </summary>
		/// <typeparam name="T1"></typeparam>
		/// <typeparam name="T2"></typeparam>
		/// <typeparam name="T3"></typeparam>
		/// <returns></returns>
		public (TModel, T1, T2, T3) ToOneUnion<T1, T2, T3>() where T1 : IDbModel, new() where T2 : IDbModel, new() where T3 : IDbModel, new()
			=> this.ToOne<(TModel, T1, T2, T3)>();

		/// <summary>
		/// 
		/// </summary>
		/// <typeparam name="T1"></typeparam>
		/// <typeparam name="T2"></typeparam>
		/// <typeparam name="T3"></typeparam>
		/// <returns></returns>
		public List<(TModel, T1, T2, T3)> ToListUnion<T1, T2, T3>() where T1 : IDbModel, new() where T2 : IDbModel, new() where T3 : IDbModel, new()
			=> this.ToList<(TModel, T1, T2, T3)>();

		/// <summary>
		/// 
		/// </summary>
		/// <typeparam name="T1"></typeparam>
		/// <typeparam name="T2"></typeparam>
		/// <typeparam name="T3"></typeparam>
		/// <returns></returns>
		public TSQL ToOneUnionPipe<T1, T2, T3>() where T1 : IDbModel, new() where T2 : IDbModel, new() where T3 : IDbModel, new()
			=> this.ToOnePipe<(TModel, T1, T2, T3)>();

		/// <summary>
		/// 
		/// </summary>
		/// <typeparam name="T1"></typeparam>
		/// <typeparam name="T2"></typeparam>
		/// <typeparam name="T3"></typeparam>
		/// <returns></returns>
		public TSQL ToListUnionPipe<T1, T2, T3>() where T1 : IDbModel, new() where T2 : IDbModel, new() where T3 : IDbModel, new()
			=> this.ToListPipe<(TModel, T1, T2, T3)>();
		#endregion

		#region Override
		/// <summary>
		/// 
		/// </summary>
		/// <returns></returns>
		public override string ToString() => base.ToString();

		/// <summary>
		/// 
		/// </summary>
		/// <param name="field"></param>
		/// <returns></returns>
		public new string ToString(string field) => base.ToString(field);

		/// <summary>
		/// 
		/// </summary>
		/// <returns></returns>
		public override string GetCommandTextString()
		{
			if (string.IsNullOrEmpty(Fields))
				Fields = EntityHelper.GetModelTypeFieldsString<TModel>(MainAlias);
			var field = new StringBuilder(Fields);
			var union = new StringBuilder();
			foreach (var item in _unionCollection.List)
			{
				union.AppendLine(string.Format("{0} {1} {2} ON {3}", item.UnionTypeString, item.Table, item.AliasName, item.Expression));
				if (item.IsReturn)
					field.Append(", ").Append(item.Fields);
			}
			StringBuilder sqlText = new StringBuilder($"SELECT {field} FROM {MainTable} {MainAlias} {_tablesampleSystem} {union}");

			// other
			if (WhereList?.Count() > 0)
				sqlText.AppendLine("WHERE " + string.Join(" AND ", WhereList));

			if (!string.IsNullOrEmpty(_groupBy))
				sqlText.AppendLine(string.Concat("GROUP BY ", _groupBy));

			if (!string.IsNullOrEmpty(_groupBy) && !string.IsNullOrEmpty(_having))
				sqlText.AppendLine(string.Concat("HAVING ", _having));

			if (!string.IsNullOrEmpty(_orderBy))
				sqlText.AppendLine(string.Concat("ORDER BY ", _orderBy));

			if (_limit.HasValue)
				sqlText.AppendLine(string.Concat("LIMIT ", _limit));

			if (_offset.HasValue)
				sqlText.AppendLine(string.Concat("OFFSET ", _offset));

			if (!string.IsNullOrEmpty(_union))
				sqlText.AppendLine(string.Concat("UNION ", _union));
			return sqlText.ToString();
		}
		#endregion

		#region Protected Method
		/// <summary>
		/// 设置redis cache
		/// </summary>
		/// <param name="key">redis key</param>
		/// <param name="model">model value</param>
		/// <param name="timeout">time out</param>
		/// <param name="func">修改/删除语句</param>
		/// <exception cref="ArgumentNullException">func is null or empty</exception>
		/// <returns></returns>
		protected static int SetRedisCache(string key, TModel model, int timeout, Func<int> func)
		{
			if (func == null)
				throw new ArgumentNullException(nameof(func));
			if (timeout == 0) return func.Invoke();
			RedisHelper.Set(key, model, timeout);
			int affrows;
			try { affrows = func.Invoke(); }
			catch (Exception ex)
			{
				RedisHelper.Del(key);
				throw ex;
			}
			if (affrows == 0) RedisHelper.Del(key);
			return affrows;
		}
		/// <summary>
		/// 
		/// </summary>
		/// <typeparam name="T">model 类型</typeparam>
		/// <param name="key"></param>
		/// <param name="timeout"></param>
		/// <param name="select"></param>
		/// <returns></returns>
		protected static TModel GetRedisCache(string key, int timeout, Func<TModel> select)
		{
			if (select == null)
				throw new ArgumentNullException(nameof(select));
			if (timeout == 0) return select.Invoke();
			var expre = RedisHelper.Ttl(key);
			string str = RedisHelper.Get(key);
			var info = RedisHelper.Get<TModel>(key);
			if (info == null)
			{
				info = select.Invoke();
				RedisHelper.Set(key, info, timeout);
			}
			return info;
		}
		#endregion
	}
}

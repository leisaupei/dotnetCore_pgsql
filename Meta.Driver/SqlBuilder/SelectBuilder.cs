using Meta.Driver.DbHelper;
using Meta.Driver.Interface;
using Meta.Driver.Model;
using Meta.Driver.SqlBuilder.AnalysisExpression;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Meta.Driver.SqlBuilder
{
	/// <summary>
	/// select 语句实例
	/// </summary>
	/// <typeparam name="TSQL"></typeparam>
	/// <typeparam name="TModel"></typeparam>
	public abstract class SelectBuilder<TSQL, TModel> : WhereBuilder<TSQL, TModel>
		where TSQL : class, ISqlBuilder
		where TModel : IDbModel, new()
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
		string _distinctOn;
		#endregion

		#region Constructor
		protected SelectBuilder() : base()
		{
			_unionCollection = new UnionCollection(MainAlias);
		}

		#endregion

		TSQL This => this as TSQL;

		/// <summary>
		/// 设置单个字段 常用于IN系列与EXISTS系列 会采用key selector别名为表别名
		/// </summary>
		/// <param name="selector"></param>
		/// <returns>ISqlBuilder</returns>
		public TSQL Field(Expression<Func<TModel, object>> selector)
		{
			var visitor = SqlExpressionVisitor.Instance.VisitSingle(selector);
			Fields = visitor.SqlText;
			MainAlias = visitor.Alias;
			return This;
		}

		/// <summary>
		/// 设置单个字段 常用于IN系列与EXISTS系列 会采用key selector别名为表别名
		/// </summary>
		/// <returns>ISqlBuilder</returns>
		public TSQL Field(string field)
		{
			if (field.Contains('.'))
			{
				var arr = field.Split('.');
				Fields = arr[1];
				MainAlias = arr[0];
			}
			else Fields = field;
			return This;
		}

		#region KeyWord
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
			_groupBy += s;
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
			_orderBy += s;
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
		/// <param name="sqlBuilder"></param>
		/// <returns></returns>
		public TSQL Union(ISqlBuilder sqlBuilder)
		{
			_union = $"({sqlBuilder.CommandText})";
			return AddParameters(sqlBuilder.Params);
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
		/// 去除重复, 建议与order by连用
		/// </summary>
		/// <param name="selector">key selector</param>
		/// <returns></returns>
		public TSQL DistinctOn(Expression<Func<TModel, object>> selector)
		{
			_distinctOn = SqlExpressionVisitor.Instance.VisitSingle(selector).SqlText;
			return This;
		}

		/// <summary>
		/// group by
		/// </summary>
		/// <param name="selector">key selector</param>
		/// <returns></returns>
		public TSQL GroupBy(Expression<Func<TModel, object>> selector)
			=> GroupBy<TModel>(selector);

		/// <summary>
		/// group by
		/// </summary>
		/// <typeparam name="TSource"></typeparam>
		/// <param name="selector"></param>
		/// <returns></returns>
		public TSQL GroupBy<TSource>(Expression<Func<TSource, object>> selector) where TSource : IDbModel, new()
			=> GroupBy(SqlExpressionVisitor.Instance.VisitSingle(selector).SqlText);

		/// <summary>
		/// order by asc
		/// </summary>
		/// <param name="selector">key selector</param>
		/// <param name="isNullsLast">use nulls last</param>
		/// <returns></returns>
		public TSQL OrderBy(Expression<Func<TModel, object>> selector, bool isNullsLast = false)
			=> OrderBy<TModel>(selector, isNullsLast);

		/// <summary>
		/// order by desc
		/// </summary>
		/// <param name="selector">key selector</param>
		/// <param name="isNullsLast">is nulls last</param>
		/// <returns></returns>
		public TSQL OrderByDescending(Expression<Func<TModel, object>> selector, bool isNullsLast = false)
			=> OrderByDescending<TModel>(selector, isNullsLast);

		/// <summary>
		/// order by asc
		/// </summary>
		/// <typeparam name="TSource">model type</typeparam>
		/// <param name="selector">key selector</param>
		/// <param name="isNullsLast">is nulls last</param>
		/// <returns></returns>
		public TSQL OrderBy<TSource>(Expression<Func<TSource, object>> selector, bool isNullsLast = false) where TSource : IDbModel, new()
			=> OrderBy(string.Concat(SqlExpressionVisitor.Instance.VisitSingle(selector).SqlText, isNullsLast ? " NULLS LAST" : ""));

		/// <summary>
		/// order by desc
		/// </summary>
		/// <typeparam name="TSource">model type</typeparam>
		/// <param name="selector">key selector</param>
		/// <param name="isNullsLast">is nulls last</param>
		/// <returns></returns>
		public TSQL OrderByDescending<TSource>(Expression<Func<TSource, object>> selector, bool isNullsLast = false) where TSource : IDbModel, new()
			=> OrderBy(string.Concat(SqlExpressionVisitor.Instance.VisitSingle(selector).SqlText, " desc", isNullsLast ? " NULLS LAST" : ""));
		#endregion

		#region ToList
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
			=> ToList<TKey>(SqlExpressionVisitor.Instance.VisitSingle(selector).SqlText);

		/// <summary>
		/// 返回列表
		/// </summary>
		/// <returns></returns>
		public List<TModel> ToList() => this.ToList<TModel>();
		#endregion

		#region ToOne
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
		public TModel ToOne()
			=> this.ToOne<TModel>();

		/// <summary>
		/// 返回一行
		/// </summary>
		/// <typeparam name="TKey"></typeparam>
		/// <param name="selector"></param>
		/// <returns></returns>
		public TKey ToOne<TKey>(Expression<Func<TModel, TKey>> selector)
			=> ToOne<TModel, TKey>(selector);

		/// <summary>
		/// 返回一行
		/// </summary>
		/// <typeparam name="TSource"></typeparam>
		/// <typeparam name="TKey"></typeparam>
		/// <param name="selector"></param>
		/// <returns></returns>
		public TKey ToOne<TSource, TKey>(Expression<Func<TSource, TKey>> selector) where TSource : IDbModel, new()
			=> this.ToScalar(selector);

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
			return ToScalar<TKey>();
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
			=> ToScalar<TKey>(SqlExpressionVisitor.Instance.VisitSingle(selector).SqlText);
		#endregion

		#region Single Method
		/// <summary>
		/// 返回行数
		/// </summary>
		/// <returns></returns>
		public long Count() => ToScalar<long>("COUNT(1)");

		/// <summary>
		/// 取最大值
		/// </summary>
		/// <typeparam name="TSource">model类型</typeparam>
		/// <typeparam name="TKey">返回值类型</typeparam>
		/// <param name="selector">key selector</param>
		/// <param name="defaultValue"></param>
		/// <returns></returns>
		public TKey Max<TSource, TKey>(Expression<Func<TSource, TKey>> selector, TKey defaultValue = default) where TSource : IDbModel, new()
			=> ScalarTransfer(selector, "MAX", defaultValue);

		/// <summary>
		/// 取最小值
		/// </summary>
		/// <typeparam name="TSource">model type</typeparam>
		/// <typeparam name="TKey">return value type</typeparam>
		/// <param name="selector">key selector</param>
		/// <param name="defaultValue"></param>
		/// <returns></returns>
		public TKey Min<TSource, TKey>(Expression<Func<TSource, TKey>> selector, TKey defaultValue = default) where TSource : IDbModel, new()
			=> ScalarTransfer(selector, "MIN", defaultValue);

		/// <summary>
		/// 取总和
		/// </summary>
		/// <typeparam name="TSource">model type</typeparam>
		/// <typeparam name="TKey">return value type</typeparam>
		/// <param name="selector">key selector</param>
		/// <param name="defaultValue"></param>
		/// <returns></returns>
		public TKey Sum<TSource, TKey>(Expression<Func<TSource, TKey>> selector, TKey defaultValue = default) where TSource : IDbModel, new()
			=> ScalarTransfer(selector, "SUM", defaultValue);

		/// <summary>
		/// 取平均值
		/// </summary>
		/// <typeparam name="TSource">model type</typeparam>
		/// <typeparam name="TKey">return value type</typeparam>
		/// <param name="selector">key selector</param>
		/// <param name="defaultValue"></param>
		/// <returns></returns>
		public TKey Avg<TSource, TKey>(Expression<Func<TSource, TKey>> selector, TKey defaultValue = default) where TSource : IDbModel, new()
			=> ScalarTransfer(selector, "AVG", defaultValue);

		/// <summary>
		/// 取最大值
		/// </summary>
		/// <typeparam name="TKey">return value type</typeparam>
		/// <param name="selector">key selector</param>
		/// <param name="defaultValue"></param>
		/// <returns></returns>
		public TKey Max<TKey>(Expression<Func<TModel, TKey>> selector, TKey defaultValue = default) => Max<TModel, TKey>(selector, defaultValue);

		/// <summary>
		/// 取最小值
		/// </summary>
		/// <typeparam name="TKey">return value type</typeparam>
		/// <param name="selector">key selector</param>
		/// <param name="defaultValue"></param>
		/// <returns></returns>
		public TKey Min<TKey>(Expression<Func<TModel, TKey>> selector, TKey defaultValue = default) => Min<TModel, TKey>(selector, defaultValue);

		/// <summary>
		/// 取总和
		/// </summary>
		/// <typeparam name="TKey">return value type</typeparam>
		/// <param name="selector">key selector</param>
		/// <param name="defaultValue"></param>
		/// <returns></returns>
		public TKey Sum<TKey>(Expression<Func<TModel, TKey>> selector, TKey defaultValue = default) => Sum<TModel, TKey>(selector, defaultValue);

		/// <summary>
		/// 去平均值
		/// </summary>
		/// <typeparam name="TKey">return value type</typeparam>
		/// <param name="selector">key selector</param>
		/// <param name="defaultValue"></param>
		/// <returns></returns>
		public TKey Avg<TKey>(Expression<Func<TModel, TKey>> selector, TKey defaultValue = default) => Avg<TModel, TKey>(selector, defaultValue);

		/// <summary>
		/// 取最大值
		/// </summary>
		/// <typeparam name="TSource">model类型</typeparam>
		/// <typeparam name="TKey">返回值类型</typeparam>
		/// <param name="selector">key selector</param>
		/// <param name="defaultValue"></param>
		/// <returns></returns>
		public TKey Max<TSource, TKey>(Expression<Func<TSource, TKey?>> selector, TKey defaultValue = default) where TSource : IDbModel, new() where TKey : struct
			=> ScalarTransfer(selector, "MAX", defaultValue);

		/// <summary>
		/// 取最小值
		/// </summary>
		/// <typeparam name="TSource">model type</typeparam>
		/// <typeparam name="TKey">return value type</typeparam>
		/// <param name="selector">key selector</param>
		/// <param name="defaultValue"></param>
		/// <returns></returns>
		public TKey Min<TSource, TKey>(Expression<Func<TSource, TKey?>> selector, TKey defaultValue = default) where TSource : IDbModel, new() where TKey : struct
			=> ScalarTransfer(selector, "MIN", defaultValue);

		/// <summary>
		/// 取总和
		/// </summary>
		/// <typeparam name="TSource">model type</typeparam>
		/// <typeparam name="TKey">return value type</typeparam>
		/// <param name="selector">key selector</param>
		/// <param name="defaultValue"></param>
		/// <returns></returns>
		public TKey Sum<TSource, TKey>(Expression<Func<TSource, TKey?>> selector, TKey defaultValue = default) where TSource : IDbModel, new() where TKey : struct
			=> ScalarTransfer(selector, "SUM", defaultValue);

		/// <summary>
		/// 取平均值
		/// </summary>
		/// <typeparam name="TSource">model type</typeparam>
		/// <typeparam name="TKey">return value type</typeparam>
		/// <param name="selector">key selector</param>
		/// <param name="defaultValue"></param>
		/// <returns></returns>
		public TKey Avg<TSource, TKey>(Expression<Func<TSource, TKey?>> selector, TKey defaultValue = default) where TSource : IDbModel, new() where TKey : struct
			=> ScalarTransfer(selector, "AVG", defaultValue);

		/// <summary>
		/// 取最大值
		/// </summary>
		/// <typeparam name="TKey">return value type</typeparam>
		/// <param name="selector">key selector</param>
		/// <param name="defaultValue"></param>
		/// <returns></returns>
		public TKey Max<TKey>(Expression<Func<TModel, TKey?>> selector, TKey defaultValue = default) where TKey : struct => Max<TModel, TKey>(selector, defaultValue);

		/// <summary>
		/// 取最小值
		/// </summary>
		/// <typeparam name="TKey">return value type</typeparam>
		/// <param name="selector">key selector</param>
		/// <param name="defaultValue"></param>
		/// <returns></returns>
		public TKey Min<TKey>(Expression<Func<TModel, TKey?>> selector, TKey defaultValue = default) where TKey : struct => Min<TModel, TKey>(selector, defaultValue);

		/// <summary>
		/// 取总和
		/// </summary>
		/// <typeparam name="TKey">return value type</typeparam>
		/// <param name="selector">key selector</param>
		/// <param name="defaultValue"></param>
		/// <returns></returns>
		public TKey Sum<TKey>(Expression<Func<TModel, TKey?>> selector, TKey defaultValue = default) where TKey : struct => Sum<TModel, TKey>(selector, defaultValue);

		/// <summary>
		/// 去平均值
		/// </summary>
		/// <typeparam name="TKey">return value type</typeparam>
		/// <param name="selector">key selector</param>
		/// <param name="defaultValue"></param>
		/// <returns></returns>
		public TKey Avg<TKey>(Expression<Func<TModel, TKey?>> selector, TKey defaultValue = default) where TKey : struct => Avg<TModel, TKey>(selector, defaultValue);
		#endregion

		#region Async

		#region ToList
		/// <summary>
		/// 返回列表
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="fields"></param>
		/// <param name="cancellationToken"></param>
		/// <returns></returns>
		public Task<List<T>> ToListAsync<T>(string fields = null, CancellationToken cancellationToken = default)
		{
			if (!string.IsNullOrEmpty(fields)) Fields = fields;
			if (IsReturnDefault) return Task.FromResult(new List<T>());
			return base.ToListAsync<T>(cancellationToken);
		}

		/// <summary>
		/// 返回列表
		/// </summary>
		/// <typeparam name="TKey"></typeparam>
		/// <param name="selector"></param>
		/// <param name="cancellationToken"></param>
		/// <returns></returns>
		public Task<List<TKey>> ToListAsync<TKey>(Expression<Func<TModel, TKey>> selector, CancellationToken cancellationToken = default)
			=> ToListAsync<TModel, TKey>(selector, cancellationToken);

		/// <summary>
		/// 返回列表
		/// </summary>
		/// <typeparam name="TSource"></typeparam>
		/// <typeparam name="TKey"></typeparam>
		/// <param name="selector"></param>
		/// <param name="cancellationToken"></param>
		/// <returns></returns>
		public Task<List<TKey>> ToListAsync<TSource, TKey>(Expression<Func<TSource, TKey>> selector, CancellationToken cancellationToken = default) where TSource : IDbModel, new()
			=> ToListAsync<TKey>(SqlExpressionVisitor.Instance.VisitSingle(selector).SqlText, cancellationToken);

		/// <summary>
		/// 返回列表
		/// </summary>
		/// <returns></returns>
		public Task<List<TModel>> ToListAsync(CancellationToken cancellationToken = default)
			=> this.ToListAsync<TModel>(cancellationToken);
		#endregion

		#region ToOne
		/// <summary>
		/// 返回一行
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="fields"></param>
		/// <param name="cancellationToken"></param>
		/// <returns></returns>
		public Task<T> ToOneAsync<T>(string fields = null, CancellationToken cancellationToken = default)
		{
			Limit(1);
			if (!string.IsNullOrEmpty(fields)) Fields = fields;
			return base.ToOneAsync<T>(cancellationToken);
		}

		/// <summary>
		/// 返回一行
		/// </summary>
		/// <param name="cancellationToken"></param>
		/// <returns></returns>
		public Task<TModel> ToOneAsync(CancellationToken cancellationToken = default)
			=> this.ToOneAsync<TModel>(cancellationToken);

		/// <summary>
		/// 返回一行
		/// </summary>
		/// <typeparam name="TKey"></typeparam>
		/// <param name="selector"></param>
		/// <param name="cancellationToken"></param>
		/// <returns></returns>
		public ValueTask<TKey> ToOneAsync<TKey>(Expression<Func<TModel, TKey>> selector, CancellationToken cancellationToken = default)
			=> ToOneAsync<TModel, TKey>(selector, cancellationToken);

		/// <summary>
		/// 返回一行
		/// </summary>
		/// <typeparam name="TSource"></typeparam>
		/// <typeparam name="TKey"></typeparam>
		/// <param name="selector"></param>
		/// <param name="cancellationToken"></param>
		/// <returns></returns>
		public ValueTask<TKey> ToOneAsync<TSource, TKey>(Expression<Func<TSource, TKey>> selector, CancellationToken cancellationToken = default) where TSource : IDbModel, new()
			=> this.ToScalarAsync(selector, cancellationToken);

		/// <summary>
		/// 返回第一个元素
		/// </summary>
		/// <typeparam name="TKey"></typeparam>
		/// <param name="fields"></param>
		/// <param name="cancellationToken"></param>
		/// <returns></returns>
		public ValueTask<TKey> ToScalarAsync<TKey>(string fields, CancellationToken cancellationToken = default)
		{
			Limit(1);
			Fields = fields;
			return base.ToScalarAsync<TKey>(cancellationToken);
		}

		/// <summary>
		/// 返回第一个元素
		/// </summary>
		/// <typeparam name="TKey"></typeparam>
		/// <param name="selector"></param>
		/// <param name="cancellationToken"></param>
		/// <returns></returns>
		public ValueTask<TKey> ToScalarAsync<TKey>(Expression<Func<TModel, TKey>> selector, CancellationToken cancellationToken = default)
			=> this.ToScalarAsync<TModel, TKey>(selector, cancellationToken);

		/// <summary>
		/// 返回第一个元素
		/// </summary>
		/// <typeparam name="TSource"></typeparam>
		/// <typeparam name="TKey"></typeparam>
		/// <param name="selector"></param>
		/// <param name="cancellationToken"></param>
		/// <returns></returns>
		public ValueTask<TKey> ToScalarAsync<TSource, TKey>(Expression<Func<TSource, TKey>> selector, CancellationToken cancellationToken = default) where TSource : IDbModel, new()
			=> ToScalarAsync<TKey>(SqlExpressionVisitor.Instance.VisitSingle(selector).SqlText, cancellationToken);
		#endregion

		#region ToOneUnion
		/// <summary>
		/// 返回联表实体
		/// </summary>
		/// <typeparam name="T1"></typeparam>
		/// <returns></returns>
		public Task<(TModel, T1)> ToOneUnionAsync<T1>(CancellationToken cancellationToken = default) where T1 : IDbModel, new()
			=> this.ToOneAsync<(TModel, T1)>(cancellationToken);

		/// <summary>
		/// 返回联表实体
		/// </summary>
		/// <typeparam name="T1"></typeparam>
		/// <typeparam name="T2"></typeparam>
		/// <returns></returns>
		public Task<(TModel, T1, T2)> ToOneUnionAsync<T1, T2>(CancellationToken cancellationToken = default) where T1 : IDbModel, new() where T2 : IDbModel, new()
			=> this.ToOneAsync<(TModel, T1, T2)>(cancellationToken);

		/// <summary>
		/// 返回联表实体
		/// </summary>
		/// <typeparam name="T1"></typeparam>
		/// <typeparam name="T2"></typeparam>
		/// <typeparam name="T3"></typeparam>
		/// <returns></returns>
		public Task<(TModel, T1, T2, T3)> ToOneUnionAsync<T1, T2, T3>(CancellationToken cancellationToken = default) where T1 : IDbModel, new() where T2 : IDbModel, new() where T3 : IDbModel, new()
			=> this.ToOneAsync<(TModel, T1, T2, T3)>(cancellationToken);
		#endregion

		#region ToListUnion
		/// <summary>
		/// 返回联表实体列表
		/// </summary>
		/// <typeparam name="T1"></typeparam>
		/// <returns></returns>
		public Task<List<(TModel, T1)>> ToListUnionAsync<T1>(CancellationToken cancellationToken = default) where T1 : IDbModel, new()
			=> this.ToListAsync<(TModel, T1)>(cancellationToken);

		/// <summary>
		/// 返回联表实体列表
		/// </summary>
		/// <typeparam name="T1"></typeparam>
		/// <typeparam name="T2"></typeparam>
		/// <returns></returns>
		public Task<List<(TModel, T1, T2)>> ToListUnionAsync<T1, T2>(CancellationToken cancellationToken = default) where T1 : IDbModel, new() where T2 : IDbModel, new()
			=> this.ToListAsync<(TModel, T1, T2)>(cancellationToken);

		/// <summary>
		/// 返回联表实体列表
		/// </summary>
		/// <typeparam name="T1"></typeparam>
		/// <typeparam name="T2"></typeparam>
		/// <typeparam name="T3"></typeparam>
		/// <returns></returns>
		public Task<List<(TModel, T1, T2, T3)>> ToListUnionAsync<T1, T2, T3>(CancellationToken cancellationToken = default) where T1 : IDbModel, new() where T2 : IDbModel, new() where T3 : IDbModel, new()
			=> this.ToListAsync<(TModel, T1, T2, T3)>(cancellationToken);
		#endregion

		#region SingleMethod

		/// <summary>
		/// 返回行数
		/// </summary>
		/// <param name="cancellationToken"></param>
		/// <returns></returns>
		public ValueTask<long> CountAsync(CancellationToken cancellationToken = default) => ToScalarAsync<long>("COUNT(1)", cancellationToken);

		/// <summary>
		/// 取最大值
		/// </summary>
		/// <typeparam name="TSource">model类型</typeparam>
		/// <typeparam name="TKey">返回值类型</typeparam>
		/// <param name="selector">key selector</param>
		/// <param name="defaultValue"></param>
		/// <param name="cancellationToken"></param>
		/// <returns></returns>
		public ValueTask<TKey> MaxAsync<TSource, TKey>(Expression<Func<TSource, TKey>> selector, TKey defaultValue = default, CancellationToken cancellationToken = default) where TSource : IDbModel, new()
			=> ScalarTransferAsync(selector, "MAX", defaultValue, cancellationToken);

		/// <summary>
		/// 取最小值
		/// </summary>
		/// <typeparam name="TSource">model type</typeparam>
		/// <typeparam name="TKey">return value type</typeparam>
		/// <param name="selector">key selector</param>
		/// <param name="defaultValue"></param>
		/// <param name="cancellationToken"></param>
		/// <returns></returns>
		public ValueTask<TKey> MinAsync<TSource, TKey>(Expression<Func<TSource, TKey>> selector, TKey defaultValue = default, CancellationToken cancellationToken = default) where TSource : IDbModel, new()
			=> ScalarTransferAsync(selector, "MIN", defaultValue, cancellationToken);

		/// <summary>
		/// 取总和
		/// </summary>
		/// <typeparam name="TSource">model type</typeparam>
		/// <typeparam name="TKey">return value type</typeparam>
		/// <param name="selector">key selector</param>
		/// <param name="defaultValue"></param>
		/// <param name="cancellationToken"></param>
		/// <returns></returns>
		public ValueTask<TKey> SumAsync<TSource, TKey>(Expression<Func<TSource, TKey>> selector, TKey defaultValue = default, CancellationToken cancellationToken = default) where TSource : IDbModel, new()
			=> ScalarTransferAsync(selector, "SUM", defaultValue, cancellationToken);

		/// <summary>
		/// 取平均值
		/// </summary>
		/// <typeparam name="TSource">model type</typeparam>
		/// <typeparam name="TKey">return value type</typeparam>
		/// <param name="selector">key selector</param>
		/// <param name="defaultValue"></param>
		/// <param name="cancellationToken"></param>
		/// <returns></returns>
		public ValueTask<TKey> AvgAsync<TSource, TKey>(Expression<Func<TSource, TKey>> selector, TKey defaultValue = default, CancellationToken cancellationToken = default) where TSource : IDbModel, new()
			=> ScalarTransferAsync(selector, "AVG", defaultValue, cancellationToken);

		/// <summary>
		/// 取最大值
		/// </summary>
		/// <typeparam name="TKey">return value type</typeparam>
		/// <param name="selector">key selector</param>
		/// <param name="defaultValue"></param>
		/// <param name="cancellationToken"></param>
		/// <returns></returns>
		public ValueTask<TKey> MaxAsync<TKey>(Expression<Func<TModel, TKey>> selector, TKey defaultValue = default, CancellationToken cancellationToken = default)
			=> MaxAsync<TModel, TKey>(selector, defaultValue, cancellationToken);

		/// <summary>
		/// 取最小值
		/// </summary>
		/// <typeparam name="TKey">return value type</typeparam>
		/// <param name="selector">key selector</param>
		/// <param name="defaultValue"></param>
		/// <param name="cancellationToken"></param>
		/// <returns></returns>
		public ValueTask<TKey> MinAsync<TKey>(Expression<Func<TModel, TKey>> selector, TKey defaultValue = default, CancellationToken cancellationToken = default)
			=> MinAsync<TModel, TKey>(selector, defaultValue, cancellationToken);

		/// <summary>
		/// 取总和
		/// </summary>
		/// <typeparam name="TKey">return value type</typeparam>
		/// <param name="selector">key selector</param>
		/// <param name="defaultValue"></param>
		/// <param name="cancellationToken"></param>
		/// <returns></returns>
		public ValueTask<TKey> SumAsync<TKey>(Expression<Func<TModel, TKey>> selector, TKey defaultValue = default, CancellationToken cancellationToken = default)
			=> SumAsync<TModel, TKey>(selector, defaultValue, cancellationToken);

		/// <summary>
		/// 去平均值
		/// </summary>
		/// <typeparam name="TKey">return value type</typeparam>
		/// <param name="selector">key selector</param>
		/// <param name="defaultValue"></param>
		/// <param name="cancellationToken"></param>
		/// <returns></returns>
		public ValueTask<TKey> AvgAsync<TKey>(Expression<Func<TModel, TKey>> selector, TKey defaultValue = default, CancellationToken cancellationToken = default)
			=> AvgAsync<TModel, TKey>(selector, defaultValue, cancellationToken);

		/// <summary>
		/// 取最大值
		/// </summary>
		/// <typeparam name="TSource">model类型</typeparam>
		/// <typeparam name="TKey">返回值类型</typeparam>
		/// <param name="selector">key selector</param>
		/// <param name="defaultValue"></param>
		/// <param name="cancellationToken"></param>
		/// <returns></returns>
		public ValueTask<TKey> MaxAsync<TSource, TKey>(Expression<Func<TSource, TKey?>> selector, TKey defaultValue = default, CancellationToken cancellationToken = default)
			where TSource : IDbModel, new() where TKey : struct
			=> ScalarTransferAsync(selector, "MAX", defaultValue, cancellationToken);

		/// <summary>
		/// 取最小值
		/// </summary>
		/// <typeparam name="TSource">model type</typeparam>
		/// <typeparam name="TKey">return value type</typeparam>
		/// <param name="selector">key selector</param>
		/// <param name="defaultValue"></param>
		/// <param name="cancellationToken"></param>
		/// <returns></returns>
		public ValueTask<TKey> MinAsync<TSource, TKey>(Expression<Func<TSource, TKey?>> selector, TKey defaultValue = default, CancellationToken cancellationToken = default)
			where TSource : IDbModel, new() where TKey : struct
			=> ScalarTransferAsync(selector, "MIN", defaultValue, cancellationToken);

		/// <summary>
		/// 取总和
		/// </summary>
		/// <typeparam name="TSource">model type</typeparam>
		/// <typeparam name="TKey">return value type</typeparam>
		/// <param name="selector">key selector</param>
		/// <param name="defaultValue"></param>
		/// <param name="cancellationToken"></param>
		/// <returns></returns>
		public ValueTask<TKey> SumAsync<TSource, TKey>(Expression<Func<TSource, TKey?>> selector, TKey defaultValue = default, CancellationToken cancellationToken = default)
			where TSource : IDbModel, new() where TKey : struct
			=> ScalarTransferAsync(selector, "SUM", defaultValue, cancellationToken);

		/// <summary>
		/// 取平均值
		/// </summary>
		/// <typeparam name="TSource">model type</typeparam>
		/// <typeparam name="TKey">return value type</typeparam>
		/// <param name="selector">key selector</param>
		/// <param name="defaultValue"></param>
		/// <param name="cancellationToken"></param>
		/// <returns></returns>
		public ValueTask<TKey> AvgAsync<TSource, TKey>(Expression<Func<TSource, TKey?>> selector, TKey defaultValue = default, CancellationToken cancellationToken = default)
			where TSource : IDbModel, new() where TKey : struct
			=> ScalarTransferAsync(selector, "AVG", defaultValue, cancellationToken);

		/// <summary>
		/// 取最大值
		/// </summary>
		/// <typeparam name="TKey">return value type</typeparam>
		/// <param name="selector">key selector</param>
		/// <param name="defaultValue"></param>
		/// <param name="cancellationToken"></param>
		/// <returns></returns>
		public ValueTask<TKey> MaxAsync<TKey>(Expression<Func<TModel, TKey?>> selector, TKey defaultValue = default, CancellationToken cancellationToken = default) where TKey : struct
			=> MaxAsync<TModel, TKey>(selector, defaultValue, cancellationToken);

		/// <summary>
		/// 取最小值
		/// </summary>
		/// <typeparam name="TKey">return value type</typeparam>
		/// <param name="selector">key selector</param>
		/// <param name="defaultValue"></param>
		/// <param name="cancellationToken"></param>
		/// <returns></returns>
		public ValueTask<TKey> MinAsync<TKey>(Expression<Func<TModel, TKey?>> selector, TKey defaultValue = default, CancellationToken cancellationToken = default) where TKey : struct
			=> MinAsync<TModel, TKey>(selector, defaultValue, cancellationToken);

		/// <summary>
		/// 取总和
		/// </summary>
		/// <typeparam name="TKey">return value type</typeparam>
		/// <param name="selector">key selector</param>
		/// <param name="defaultValue"></param>
		/// <param name="cancellationToken"></param>
		/// <returns></returns>
		public ValueTask<TKey> SumAsync<TKey>(Expression<Func<TModel, TKey?>> selector, TKey defaultValue = default, CancellationToken cancellationToken = default) where TKey : struct
			=> SumAsync<TModel, TKey>(selector, defaultValue, cancellationToken);

		/// <summary>
		/// 去平均值
		/// </summary>
		/// <typeparam name="TKey">return value type</typeparam>
		/// <param name="selector">key selector</param>
		/// <param name="defaultValue"></param>
		/// <param name="cancellationToken"></param>
		/// <returns></returns>
		public ValueTask<TKey> AvgAsync<TKey>(Expression<Func<TModel, TKey?>> selector, TKey defaultValue = default, CancellationToken cancellationToken = default) where TKey : struct
			=> AvgAsync<TModel, TKey>(selector, defaultValue, cancellationToken);
		#endregion

		#endregion

		#region Pipe
		#region ToOne

		/// <summary>
		/// 返回一行(管道)
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="fields">返回字段, 可选</param>
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
		/// <param name="fields">返回字段, 可选</param>
		/// <returns></returns>
		public TSQL ToOnePipe(string fields = null)
			=> this.ToOnePipe<TModel>(fields);

		/// <summary>
		/// 返回联表实体(管道)
		/// </summary>
		/// <typeparam name="T1"></typeparam>
		/// <returns></returns>
		public TSQL ToOneUnionPipe<T1>() where T1 : IDbModel, new()
			=> this.ToOnePipe<(TModel, T1)>();

		/// <summary>
		/// 返回联表实体(管道)
		/// </summary>
		/// <typeparam name="T1"></typeparam>
		/// <typeparam name="T2"></typeparam>
		/// <returns></returns>
		public TSQL ToOneUnionPipe<T1, T2>() where T1 : IDbModel, new() where T2 : IDbModel, new()
			=> this.ToOnePipe<(TModel, T1, T2)>();

		/// <summary>
		/// 返回联表实体(管道)
		/// </summary>
		/// <typeparam name="T1"></typeparam>
		/// <typeparam name="T2"></typeparam>
		/// <typeparam name="T3"></typeparam>
		/// <returns></returns>
		public TSQL ToOneUnionPipe<T1, T2, T3>() where T1 : IDbModel, new() where T2 : IDbModel, new() where T3 : IDbModel, new()
			=> this.ToOnePipe<(TModel, T1, T2, T3)>();
		#endregion

		#region ToList
		/// <summary>
		/// 返回列表(管道)
		/// </summary>
		/// <typeparam name="T">model type</typeparam>
		/// <param name="fields">指定输出字段</param>
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
		/// 返回联表实体列表(管道)
		/// </summary>
		/// <typeparam name="T1"></typeparam>
		/// <returns></returns>
		public TSQL ToListUnionPipe<T1>() where T1 : IDbModel, new()
			=> this.ToListPipe<(TModel, T1)>();

		/// <summary>
		/// 返回联表实体列表(管道)
		/// </summary>
		/// <typeparam name="T1"></typeparam>
		/// <typeparam name="T2"></typeparam>
		/// <returns></returns>
		public TSQL ToListUnionPipe<T1, T2>() where T1 : IDbModel, new() where T2 : IDbModel, new()
			=> this.ToListPipe<(TModel, T1, T2)>();

		/// <summary>
		/// 返回联表实体列表(管道)
		/// </summary>
		/// <typeparam name="T1"></typeparam>
		/// <typeparam name="T2"></typeparam>
		/// <typeparam name="T3"></typeparam>
		/// <returns></returns>
		public TSQL ToListUnionPipe<T1, T2, T3>() where T1 : IDbModel, new() where T2 : IDbModel, new() where T3 : IDbModel, new()
			=> this.ToListPipe<(TModel, T1, T2, T3)>();

		#endregion
		#endregion

		#region Union
		/// <summary>
		/// inner join
		/// </summary>
		/// <typeparam name="TTarget">table model type</typeparam>
		/// <param name="predicate"></param>
		/// <param name="isReturn">is add return fields</param>
		/// <returns></returns>
		public TSQL InnerJoin<TTarget>(Expression<Func<TModel, TTarget, bool>> predicate, bool isReturn = false) where TTarget : IDbModel, new()
			=> this.InnerJoin<TModel, TTarget>(predicate, isReturn);

		/// <summary>
		/// left join
		/// </summary>
		/// <typeparam name="TTarget">table model type</typeparam>
		/// <param name="predicate"></param>
		/// <param name="isReturn">is add return fields</param>
		/// <returns></returns>
		public TSQL LeftJoin<TTarget>(Expression<Func<TModel, TTarget, bool>> predicate, bool isReturn = false) where TTarget : IDbModel, new()
				  => this.LeftJoin<TModel, TTarget>(predicate, isReturn);

		/// <summary>
		/// right join
		/// </summary>
		/// <typeparam name="TTarget">table model type</typeparam>
		/// <param name="predicate"></param>
		/// <param name="isReturn">is add return fields</param>
		/// <returns></returns>
		public TSQL RightJoin<TTarget>(Expression<Func<TModel, TTarget, bool>> predicate, bool isReturn = false) where TTarget : IDbModel, new()
				  => this.RightJoin<TModel, TTarget>(predicate, isReturn);

		/// <summary>
		/// left outer join
		/// </summary>
		/// <typeparam name="TTarget">table model type</typeparam>
		/// <param name="predicate"></param>
		/// <param name="isReturn">is add return fields</param>
		/// <returns></returns>
		public TSQL LeftOuterJoin<TTarget>(Expression<Func<TModel, TTarget, bool>> predicate, bool isReturn = false) where TTarget : IDbModel, new()
				  => this.LeftJoin<TModel, TTarget>(predicate, isReturn);

		/// <summary>
		/// right outer join
		/// </summary>
		/// <typeparam name="TTarget">table model type</typeparam>
		/// <param name="predicate"></param>
		/// <param name="isReturn">is add return fields</param>
		/// <returns></returns>
		public TSQL RightOuterJoin<TTarget>(Expression<Func<TModel, TTarget, bool>> predicate, bool isReturn = false) where TTarget : IDbModel, new()
				  => this.RightJoin<TModel, TTarget>(predicate, isReturn);

		/// <summary>
		/// inner join
		/// </summary>
		/// <typeparam name="TSource">table model type</typeparam>
		/// <typeparam name="TTarget">table model type</typeparam>
		/// <param name="predicate"></param>
		/// <param name="isReturn">is add return fields</param>
		/// <returns></returns>
		public TSQL InnerJoin<TSource, TTarget>(Expression<Func<TSource, TTarget, bool>> predicate, bool isReturn = false) where TSource : IDbModel, new() where TTarget : IDbModel, new()
			=> Join(predicate, UnionEnum.INNER_JOIN, isReturn);

		/// <summary>
		/// left join
		/// </summary>
		/// <typeparam name="TSource">table model type</typeparam>
		/// <typeparam name="TTarget">table model type</typeparam>
		/// <param name="predicate"></param>
		/// <param name="isReturn">is add return fields</param>
		/// <returns></returns>
		public TSQL LeftJoin<TSource, TTarget>(Expression<Func<TSource, TTarget, bool>> predicate, bool isReturn = false) where TSource : IDbModel, new() where TTarget : IDbModel, new()
			=> Join(predicate, UnionEnum.LEFT_JOIN, isReturn);

		/// <summary>
		/// right join
		/// </summary>
		/// <typeparam name="TSource">table model type</typeparam>
		/// <typeparam name="TTarget">table model type</typeparam>
		/// <param name="predicate"></param>
		/// <param name="isReturn">is add return fields</param>
		/// <returns></returns>
		public TSQL RightJoin<TSource, TTarget>(Expression<Func<TSource, TTarget, bool>> predicate, bool isReturn = false) where TSource : IDbModel, new() where TTarget : IDbModel, new()
			=> Join(predicate, UnionEnum.RIGHT_JOIN, isReturn);

		/// <summary>
		/// left outer join
		/// </summary>
		/// <typeparam name="TSource">table model type</typeparam>
		/// <typeparam name="TTarget">table model type</typeparam>
		/// <param name="predicate"></param>
		/// <param name="isReturn">is add return fields</param>
		/// <returns></returns>
		public TSQL LeftOuterJoin<TSource, TTarget>(Expression<Func<TSource, TTarget, bool>> predicate, bool isReturn = false) where TSource : IDbModel, new() where TTarget : IDbModel, new()
			=> Join(predicate, UnionEnum.LEFT_OUTER_JOIN, isReturn);

		/// <summary>
		/// right outer join
		/// </summary>
		/// <typeparam name="TSource">table model type</typeparam>
		/// <typeparam name="TTarget">table model type</typeparam>
		/// <param name="predicate"></param>
		/// <param name="isReturn">is add return fields</param>
		/// <returns></returns>
		public TSQL RightOuterJoin<TSource, TTarget>(Expression<Func<TSource, TTarget, bool>> predicate, bool isReturn = false) where TSource : IDbModel, new() where TTarget : IDbModel, new()
			=> Join(predicate, UnionEnum.RIGHT_OUTER_JOIN, isReturn);

		/// <summary>
		/// join base method
		/// </summary>
		/// <typeparam name="TSource">table model type</typeparam>
		/// <typeparam name="TTarget">table model type</typeparam>
		/// <param name="predicate"></param>
		/// <param name="unionType">union type</param>
		/// <param name="isReturn">is add return fields</param>
		/// <returns></returns>
		private TSQL Join<TSource, TTarget>(Expression<Func<TSource, TTarget, bool>> predicate, UnionEnum unionType, bool isReturn = false) where TTarget : IDbModel, new() where TSource : IDbModel, new()
		{
			var paras = _unionCollection.Add(predicate, unionType, isReturn);
			return AddParameters(paras);
		}

		/// <summary>
		/// join base method with string
		/// </summary>
		/// <typeparam name="TTarget">table model type</typeparam>
		/// <param name="unionType">union type</param>
		/// <param name="aliasName">table alias name</param>
		/// <param name="on">on expression</param>
		/// <param name="isReturn">is add return fields</param>
		/// <returns></returns>
		public TSQL Join<TTarget>(UnionEnum unionType, string aliasName, string on, bool isReturn = false) where TTarget : IDbModel, new()
		{
			_unionCollection.Add<TTarget>(unionType, aliasName, on, isReturn);
			return This;
		}
		#endregion

		#region ToUnion
		#region ToOne
		/// <summary>
		/// 返回联表实体
		/// </summary>
		/// <typeparam name="T1"></typeparam>
		/// <returns></returns>
		public (TModel, T1) ToOneUnion<T1>() where T1 : IDbModel, new()
			=> this.ToOne<(TModel, T1)>();

		/// <summary>
		/// 返回联表实体
		/// </summary>
		/// <typeparam name="T1"></typeparam>
		/// <typeparam name="T2"></typeparam>
		/// <returns></returns>
		public (TModel, T1, T2) ToOneUnion<T1, T2>() where T1 : IDbModel, new() where T2 : IDbModel, new()
			=> this.ToOne<(TModel, T1, T2)>();

		/// <summary>
		/// 返回联表实体
		/// </summary>
		/// <typeparam name="T1"></typeparam>
		/// <typeparam name="T2"></typeparam>
		/// <typeparam name="T3"></typeparam>
		/// <returns></returns>
		public (TModel, T1, T2, T3) ToOneUnion<T1, T2, T3>() where T1 : IDbModel, new() where T2 : IDbModel, new() where T3 : IDbModel, new()
			=> this.ToOne<(TModel, T1, T2, T3)>();
		#endregion

		#region ToList
		/// <summary>
		/// 返回联表实体列表
		/// </summary>
		/// <typeparam name="T1"></typeparam>
		/// <returns></returns>
		public List<(TModel, T1)> ToListUnion<T1>() where T1 : IDbModel, new()
			=> this.ToList<(TModel, T1)>();

		/// <summary>
		/// 返回联表实体列表
		/// </summary>
		/// <typeparam name="T1"></typeparam>
		/// <typeparam name="T2"></typeparam>
		/// <returns></returns>
		public List<(TModel, T1, T2)> ToListUnion<T1, T2>() where T1 : IDbModel, new() where T2 : IDbModel, new()
			=> this.ToList<(TModel, T1, T2)>();

		/// <summary>
		/// 返回联表实体列表
		/// </summary>
		/// <typeparam name="T1"></typeparam>
		/// <typeparam name="T2"></typeparam>
		/// <typeparam name="T3"></typeparam>
		/// <returns></returns>
		public List<(TModel, T1, T2, T3)> ToListUnion<T1, T2, T3>() where T1 : IDbModel, new() where T2 : IDbModel, new() where T3 : IDbModel, new()
			=> this.ToList<(TModel, T1, T2, T3)>();
		#endregion
		#endregion

		#region Override
		/// <summary>
		/// 
		/// </summary>
		/// <returns></returns>
		public override string ToString()
			=> base.ToString();

		/// <summary>
		/// 
		/// </summary>
		/// <param name="field"></param>
		/// <returns></returns>
		public new string ToString(string field)
			=> base.ToString(field);

		/// <summary>
		/// 
		/// </summary>
		/// <returns></returns>
		public override string GetCommandTextString()
		{
			if (string.IsNullOrEmpty(Fields))
				Fields = EntityHelper.GetModelTypeFieldsString<TModel>(MainAlias);
			var field = new StringBuilder();
			var union = new StringBuilder();
			if (!string.IsNullOrEmpty(_distinctOn))
				field.AppendLine(string.Concat("DISTINCT ON (", _distinctOn, ")"));
			field.Append(Fields);
			foreach (var item in _unionCollection.List)
			{
				union.AppendLine(string.Format("{0} {1} {2} ON {3}", item.UnionTypeString, item.Table, item.AliasName, item.Expression));
				if (item.IsReturn) field.Append(", ").Append(item.Fields);
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
			return sqlText.ToString().TrimEnd();
		}
		#endregion

		#region Protected Method
		private TKey ScalarTransfer<TSource, TKey>(Expression<Func<TSource, TKey>> selector, string method, TKey defaultValue)
			where TSource : IDbModel, new()
		{
			var visit = SqlExpressionVisitor.Instance.VisitSingle(selector);
			AddParameters(visit.Paras);
			AddParameterT(defaultValue, out string pName);
			return ToScalar<TKey>($"COALESCE({method}({visit.SqlText}),@{pName})");
		}
		private TKey ScalarTransfer<TSource, TKey>(Expression<Func<TSource, TKey?>> selector, string method, TKey defaultValue)
			where TSource : IDbModel, new()
			where TKey : struct
		{
			var visit = SqlExpressionVisitor.Instance.VisitSingle(selector);
			AddParameters(visit.Paras);
			AddParameterT(defaultValue, out string pName);
			return ToScalar<TKey>($"COALESCE({method}({visit.SqlText}),@{pName})");
		}
		private ValueTask<TKey> ScalarTransferAsync<TSource, TKey>(Expression<Func<TSource, TKey>> selector, string method, TKey defaultValue, CancellationToken cancellationToken)
			where TSource : IDbModel, new()
		{
			var visit = SqlExpressionVisitor.Instance.VisitSingle(selector);
			AddParameters(visit.Paras);
			AddParameterT(defaultValue, out string pName);
			return ToScalarAsync<TKey>($"COALESCE({method}({visit.SqlText}),@{pName})", cancellationToken);
		}
		private ValueTask<TKey> ScalarTransferAsync<TSource, TKey>(Expression<Func<TSource, TKey?>> selector, string method, TKey defaultValue, CancellationToken cancellationToken)
			where TSource : IDbModel, new()
			where TKey : struct
		{
			var visit = SqlExpressionVisitor.Instance.VisitSingle(selector);
			AddParameters(visit.Paras);
			AddParameterT(defaultValue, out string pName);
			return ToScalarAsync<TKey>($"COALESCE({method}({visit.SqlText}),@{pName})", cancellationToken);
		}

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
			if (model == null)
				throw new ArgumentNullException(nameof(model));
			if (string.IsNullOrEmpty(key))
				throw new ArgumentNullException(nameof(key));
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
		/// 设置redis cache
		/// </summary>
		/// <param name="key">redis key</param>
		/// <param name="model">model value</param>
		/// <param name="timeout">time out</param>
		/// <param name="func">修改/删除语句</param>
		/// <exception cref="ArgumentNullException">func is null or empty</exception>
		/// <returns></returns>
		protected static async Task<TModel> SetRedisCacheAsync(string key, TModel model, int timeout, Func<Task<TModel>> func)
		{
			if (func == null)
				throw new ArgumentNullException(nameof(func));
			if (model == null)
				throw new ArgumentNullException(nameof(model));
			if (string.IsNullOrEmpty(key))
				throw new ArgumentNullException(nameof(key));
			if (timeout == 0) return await func.Invoke();
			await RedisHelper.SetAsync(key, model, timeout);
			TModel ret;
			try { ret = await func.Invoke(); }
			catch (Exception ex)
			{
				await RedisHelper.DelAsync(key);
				throw ex;
			}
			if (ret == null) await RedisHelper.DelAsync(key);
			return ret;
		}

		/// <summary>
		/// 设置redis cache
		/// </summary>
		/// <param name="key">redis key</param>
		/// <param name="model">model value</param>
		/// <param name="timeout">time out</param>
		/// <param name="func">修改/删除语句</param>
		/// <exception cref="ArgumentNullException">func is null or empty</exception>
		/// <returns></returns>
		protected static async ValueTask<int> SetRedisCacheAsync(string key, TModel model, int timeout, Func<ValueTask<int>> func)
		{
			if (func == null)
				throw new ArgumentNullException(nameof(func));
			if (model == null)
				throw new ArgumentNullException(nameof(model));
			if (string.IsNullOrEmpty(key))
				throw new ArgumentNullException(nameof(key));
			if (timeout == 0) return await func.Invoke();
			await RedisHelper.SetAsync(key, model, timeout);
			int affrows;
			try { affrows = await func.Invoke(); }
			catch (Exception ex)
			{
				await RedisHelper.DelAsync(key);
				throw ex;
			}
			if (affrows == 0) await RedisHelper.DelAsync(key);
			return affrows;
		}

		/// <summary>
		/// select 获取缓存key
		/// </summary>
		/// <param name="key"></param>
		/// <param name="timeout"></param>
		/// <param name="select"></param>
		/// <exception cref="ArgumentNullException">func is null or empty</exception>
		/// <returns></returns>
		protected static TModel GetRedisCache(string key, int timeout, Func<TModel> select)
		{
			if (select == null)
				throw new ArgumentNullException(nameof(select));
			if (string.IsNullOrEmpty(key))
				throw new ArgumentNullException(nameof(key));
			if (timeout == 0) return select.Invoke();
			var info = RedisHelper.Get<TModel>(key);
			if (info == null)
			{
				info = select.Invoke();
				RedisHelper.Set(key, info, timeout);
			}
			return info;
		}

		/// <summary>
		/// select 获取缓存key
		/// </summary>
		/// <param name="key"></param>
		/// <param name="timeout"></param>
		/// <param name="select"></param>
		/// <param name="cancellationToken"></param>
		/// <exception cref="ArgumentNullException">func is null or empty</exception>
		/// <returns></returns>
		protected static async Task<TModel> GetRedisCacheAsync(string key, int timeout, Func<Task<TModel>> select)
		{
			if (select == null)
				throw new ArgumentNullException(nameof(select));
			if (string.IsNullOrEmpty(key))
				throw new ArgumentNullException(nameof(key));
			if (timeout == 0) return await select.Invoke();
			var info = await RedisHelper.GetAsync<TModel>(key);
			if (info == null)
			{
				info = await select.Invoke();
				await RedisHelper.SetAsync(key, info, timeout);
			}
			return info;
		}

		/// <summary>
		/// 批量插入数据
		/// </summary>
		/// <param name="models"></param>
		/// <param name="sqlbuilders"></param>
		/// <param name="timeout"></param>
		/// <param name="func"></param>
		/// <returns></returns>
		protected static int InsertMultiple<TDbName>(IEnumerable<TModel> models, IEnumerable<ISqlBuilder> sqlbuilders, int timeout, Func<TModel, string> func) where TDbName : struct, IDbName
		{
			var rows = PgsqlHelper<TDbName>.ExecuteDataReaderPipe(sqlbuilders).OfType<int>();
			if (timeout != 0)
				RedisHelper.StartPipe(h =>
				{
					for (int i = 0; i < rows.Count(); i++)
					{
						if (rows.ElementAt(i) == 0) continue;

						var model = models.ElementAt(i);
						h.Set(func(model), model, timeout);
					}
				});
			return rows.Sum();
		}

		/// <summary>
		/// 批量插入数据
		/// </summary>
		/// <param name="sqlbuilders"></param>
		/// <returns></returns>
		protected static int InsertMultiple<TDbName>(IEnumerable<ISqlBuilder> sqlbuilders) where TDbName : struct, IDbName
		{
			var rows = PgsqlHelper<TDbName>.ExecuteDataReaderPipe(sqlbuilders).OfType<int>();
			return rows.Sum();
		}

		/// <summary>
		/// 批量插入数据
		/// </summary>
		/// <param name="models"></param>
		/// <param name="sqlbuilders"></param>
		/// <param name="timeout"></param>
		/// <param name="func"></param>
		/// <param name="cancellationToken"></param>
		/// <returns></returns>
		protected static async ValueTask<int> InsertMultipleAsync<TDbName>(IEnumerable<TModel> models, IEnumerable<ISqlBuilder> sqlbuilders, int timeout, Func<TModel, string> func, CancellationToken cancellationToken) where TDbName : struct, IDbName
		{
			var rows = (await PgsqlHelper<TDbName>.ExecuteDataReaderPipeAsync(sqlbuilders, CommandType.Text, cancellationToken)).OfType<int>();
			if (timeout != 0)
				RedisHelper.StartPipe(h =>
				{
					for (int i = 0; i < rows.Count(); i++)
					{
						if (rows.ElementAt(i) == 0) continue;

						var model = models.ElementAt(i);
						h.Set(func(model), model, timeout);
					}
				});
			return rows.Sum();
		}
		/// <summary>
		/// 批量插入数据
		/// </summary>
		/// <param name="sqlbuilders"></param>
		/// <param name="cancellationToken"></param>
		/// <returns></returns>
		protected static async ValueTask<int> InsertMultipleAsync<TDbName>(IEnumerable<ISqlBuilder> sqlbuilders, CancellationToken cancellationToken) where TDbName : struct, IDbName
		{
			var rows = (await PgsqlHelper<TDbName>.ExecuteDataReaderPipeAsync(sqlbuilders, CommandType.Text, cancellationToken)).OfType<int>();
			return rows.Sum();
		}
		#endregion
	}
}

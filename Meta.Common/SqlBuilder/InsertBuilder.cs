using Meta.Common.DbHelper;
using Meta.Common.Extensions;
using Meta.Common.Interface;
using Meta.Common.Model;
using Meta.Common.SqlBuilder.AnalysisExpression;
using Npgsql;
using NpgsqlTypes;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace Meta.Common.SqlBuilder
{
	public class InsertBuilder<TModel> : WhereBuilder<InsertBuilder<TModel>, TModel> 
		where TModel : IDbModel, new()
	{
		/// <summary>
		/// 字段列表
		/// </summary>
		readonly Dictionary<string, string> _insertList = new Dictionary<string, string>();

		/// <summary>
		/// 是否返回实体类
		/// </summary>
		bool _isReturn = false;
		public InsertBuilder()
		{
			//create table his_process_data_201405 as
			//(select * from his_process_data_201406 limit 0)
			MainTable = EntityHelper.GetTableName<TModel>();

		}

		/// <summary>
		/// 设置一个结果 调用Field方法定义
		/// </summary>
		/// <param name="selector">key selector</param>
		/// <param name="sqlBuilder">sql</param>
		/// <returns></returns>
		public InsertBuilder<TModel> Set(Expression<Func<TModel, object>> selector, ISqlBuilder sqlBuilder)
		{
			var key = SqlExpressionVisitor.Instance.VisitSingleForNoAlias(selector).SqlText;
			_insertList[key] = sqlBuilder.CommandText;
			return AddParameters(sqlBuilder.Params);
		}

		/// <summary>
		/// 设置语句 不可空参数
		/// </summary>
		/// <typeparam name="TKey"></typeparam>
		/// <param name="selector"></param>
		/// <param name="value"></param>
		/// <returns></returns>
		public InsertBuilder<TModel> Set<TKey>(Expression<Func<TModel, TKey>> selector, TKey value)
		{
			var key = SqlExpressionVisitor.Instance.VisitSingleForNoAlias(selector).SqlText;
			return Set(key, value);
		}

		/// <summary>
		/// 设置语句 可空重载
		/// </summary>
		/// <typeparam name="TKey"></typeparam>
		/// <param name="selector"></param>
		/// <param name="value"></param>
		/// <returns></returns>
		public InsertBuilder<TModel> Set<TKey>(Expression<Func<TModel, TKey?>> selector, TKey? value) where TKey : struct
		{
			var key = SqlExpressionVisitor.Instance.VisitSingleForNoAlias(selector).SqlText;
			if (value != null) return Set(key, value.Value);
			_insertList[key] = "null";
			return this;
		}

		/// <summary>
		/// 设置某字段的值
		/// </summary>
		/// <typeparam name="TKey"></typeparam>
		/// <param name="key"></param>
		/// <param name="value"></param>
		/// <returns></returns>
		protected InsertBuilder<TModel> Set<TKey>(string key, TKey value)
		{
			if (value == null)
			{
				_insertList[key] = "null";
				return this;
			}
			var index = EntityHelper.ParamsIndex;
			_insertList[key] = string.Concat("@", index);
			return AddParameterT(index, value);
		}
		/// <summary>
		/// 返回修改行数
		/// </summary>
		/// <returns></returns>
		public new int ToRows() => base.ToRows();

		/// <summary>
		/// 返回修改行数
		/// </summary>
		/// <returns></returns>
		public new ValueTask<int> ToRowsAsync(CancellationToken cancellationToken = default)
			=> base.ToRowsAsync(cancellationToken);

		/// <summary>
		/// 返回受影响行数
		/// </summary>
		/// <returns></returns>
		public InsertBuilder<TModel> ToRowsPipe() => base.ToPipe<int>(PipeReturnType.Rows);

		/// <summary>
		/// 插入数据库并返回数据
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <returns></returns>
		public int ToRows<T>(ref T info)
		{
			_isReturn = true;
			info = ToOne<T>();
			return info != null ? 1 : 0;
		}

		/// <summary>
		/// 插入数据库并返回数据
		/// </summary>
		/// <returns></returns>
		public TModel ToOne()
		{
			_isReturn = true;
			return ToOne<TModel>();
		}

		/// <summary>
		/// 插入数据库并返回数据
		/// </summary>
		/// <returns></returns>
		public Task<TModel> ToOneAsync(CancellationToken cancellationToken = default)
		{
			_isReturn = true;
			return base.ToOneAsync<TModel>(cancellationToken);
		}
		#region Override
		public override string ToString() => base.ToString();

		public override string GetCommandTextString()
		{
			if (_insertList.IsNullOrEmpty())
				throw new ArgumentNullException(nameof(_insertList));
			var field = string.Join(", ", _insertList.Keys);
			var ret = _isReturn ? $"RETURNING {field}" : "";
			if (WhereList.Count == 0)
				return $"INSERT INTO {MainTable} ({field}) VALUES({string.Join(", ", _insertList.Values)}) {ret}";
			return $"INSERT INTO {MainTable} ({field}) SELECT {string.Join(", ", _insertList.Values)} WHERE {string.Join("\nAND", WhereList)} {ret}";
		}
		#endregion
	}
}

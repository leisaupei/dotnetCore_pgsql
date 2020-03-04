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
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

namespace Meta.Driver.SqlBuilder
{
	public abstract class SqlBuilder<TSQL> : ISqlBuilder
		where TSQL : class, ISqlBuilder
	{
		#region Identity
		/// <summary>
		/// 主表
		/// </summary>
		protected string MainTable { get; set; }
		/// <summary>
		/// 主表别名, 默认为"a"
		/// </summary>
		protected string MainAlias { get; set; } = "a";
		/// <summary>
		/// where条件列表
		/// </summary>
		protected List<string> WhereList { get; } = new List<string>();
		/// <summary>
		/// 设置默认数据库
		/// </summary>
		protected string DbName { get; set; } = "DbMaster";
		/// <summary>
		/// 是否返回默认值
		/// </summary>
		public bool IsReturnDefault { get; set; } = false;
		/// <summary>
		/// 返回实例类型
		/// </summary>
		public Type Type { get; set; }
		/// <summary>
		/// 参数列表
		/// </summary>
		public List<DbParameter> Params { get; } = new List<DbParameter>();
		/// <summary>
		/// 返回类型
		/// </summary>
		public PipeReturnType ReturnType { get; set; }
		/// <summary>
		/// 查询字段
		/// </summary>
		public string Fields { get; set; }
		#endregion

		#region Constructor
		/// <summary>
		/// 默认构造函数
		/// </summary>
		protected SqlBuilder()
		{
		}
		#endregion

		public TSQL By<TDbName>() where TDbName : struct, IDbName
		{
			DbName = typeof(TDbName).Name;
			return This;
		}
		/// <summary>
		/// 添加参数
		/// </summary>
		/// <param name="parameterName"></param>
		/// <param name="value"></param>
		/// <returns></returns>
		public TSQL AddParameter(string parameterName, object value)
			=> AddParameter(new NpgsqlParameter(parameterName, value));

		/// <summary>
		/// 添加参数
		/// </summary>
		/// <param name="value"></param>
		/// <param name="parameterName"></param>
		/// <returns></returns>
		public TSQL AddParameter(object value, out string parameterName)
		{
			parameterName = EntityHelper.ParamsIndex;
			return AddParameter(parameterName, value);
		}

		/// <summary>
		/// 添加参数
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="parameterName"></param>
		/// <param name="value"></param>
		/// <returns></returns>
		public TSQL AddParameterT<T>(string parameterName, T value)
			=> AddParameter(new NpgsqlParameter<T>(parameterName, value));

		/// <summary>
		/// 添加参数
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="value"></param>
		/// <param name="parameterName"></param>
		/// <returns></returns>
		public TSQL AddParameterT<T>(T value, out string parameterName)
		{
			parameterName = EntityHelper.ParamsIndex;
			return AddParameterT(parameterName, value);
		}

		/// <summary>
		/// 添加参数
		/// </summary>
		/// <param name="ps"></param>
		/// <returns></returns>
		public TSQL AddParameter(DbParameter ps)
		{
			Params.Add(ps);
			return This;
		}

		/// <summary>
		/// 添加参数
		/// </summary>
		/// <param name="ps"></param>
		/// <returns></returns>
		public TSQL AddParameters(IEnumerable<NpgsqlParameter> ps)
		{
			Params.AddRange(ps);
			return This;
		}

		/// <summary>
		/// 添加参数
		/// </summary>
		/// <param name="ps"></param>
		/// <returns></returns>
		public TSQL AddParameters(IEnumerable<DbParameter> ps)
		{
			Params.AddRange(ps);
			return This;
		}

		/// <summary>
		/// 返回第一个元素
		/// </summary>
		/// <returns></returns>
		protected object ToScalar()
			=> PgsqlHelper.GetExecute(DbName).ExecuteScalar(CommandText, CommandType.Text, Params.ToArray());

		/// <summary>
		/// 返回第一个元素
		/// </summary>
		/// <returns></returns>
		protected ValueTask<object> ToScalarAsync(CancellationToken cancellationToken)
			=> PgsqlHelper.GetExecute(DbName).ExecuteScalarAsync(CommandText, CommandType.Text, Params.ToArray(), cancellationToken);
		/// <summary>
		/// 返回第一个元素
		/// </summary>
		/// <returns></returns>
		protected TKey ToScalar<TKey>()
			=> ToScalarAsync<TKey>(false, CancellationToken.None).ConfigureAwait(false).GetAwaiter().GetResult();

		/// <summary>
		/// 返回第一个元素
		/// </summary>
		/// <returns></returns>
		protected ValueTask<TKey> ToScalarAsync<TKey>(CancellationToken cancellationToken)
		=> ToScalarAsync<TKey>(true, cancellationToken);

		async ValueTask<TKey> ToScalarAsync<TKey>(bool async, CancellationToken cancellationToken)
		{
			var value = async 
				? await PgsqlHelper.GetExecute(DbName).ExecuteScalarAsync(CommandText, CommandType.Text, Params.ToArray(), cancellationToken)
				: PgsqlHelper.GetExecute(DbName).ExecuteScalar(CommandText, CommandType.Text, Params.ToArray());
			return value == null ? default : (TKey)Convert.ChangeType(value, typeof(TKey).GetOriginalType());
		}

		/// <summary>
		/// 返回list 
		/// </summary>
		/// <typeparam name="T">model type</typeparam>
		/// <returns></returns>
		protected List<T> ToList<T>()
			=> PgsqlHelper.GetExecute(DbName).ExecuteDataReaderListAsync<T>(CommandText, CommandType.Text, Params.ToArray(), false, CancellationToken.None).ConfigureAwait(false).GetAwaiter().GetResult();

		/// <summary>
		/// 返回list 
		/// </summary>
		/// <typeparam name="T">model type</typeparam>
		/// <returns></returns>
		protected Task<List<T>> ToListAsync<T>(CancellationToken cancellationToken)
			=> PgsqlHelper.GetExecute(DbName).ExecuteDataReaderListAsync<T>(CommandText, CommandType.Text, Params.ToArray(), true, cancellationToken);

		/// <summary>
		/// 返回一个Model
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <returns></returns>
		protected T ToOne<T>()
			=> PgsqlHelper.GetExecute(DbName).ExecuteDataReaderModelAsync<T>(CommandText, CommandType.Text, Params.ToArray(), false, CancellationToken.None).ConfigureAwait(false).GetAwaiter().GetResult();

		/// <summary>
		/// 返回一个Model
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <returns></returns>
		protected Task<T> ToOneAsync<T>(CancellationToken cancellationToken)
			=> PgsqlHelper.GetExecute(DbName).ExecuteDataReaderModelAsync<T>(CommandText, CommandType.Text, Params.ToArray(), true, cancellationToken);

		/// <summary>
		/// 返回行数
		/// </summary>
		/// <returns></returns>
		protected int ToRows()
			=> PgsqlHelper.GetExecute(DbName).ExecuteNonQuery(CommandText, CommandType.Text, Params.ToArray());

		/// <summary>
		/// 返回行数
		/// </summary>
		/// <returns></returns>
		protected ValueTask<int> ToRowsAsync(CancellationToken cancellationToken)
			=> PgsqlHelper.GetExecute(DbName).ExecuteNonQueryAsync(CommandText, CommandType.Text, Params.ToArray(), cancellationToken);

		/// <summary>
		/// 输出管道元素
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <returns></returns>
		protected TSQL ToPipe<T>(PipeReturnType returnType)
		{
			Type = typeof(T);
			ReturnType = returnType;
			return This;
		}

		/// <summary>
		/// Override ToString()
		/// </summary>
		/// <returns></returns>
		public override string ToString() => ToString(null);

		/// <summary>
		/// 输出sql语句
		/// </summary>
		public string CommandText => GetCommandTextString();

		/// <summary>
		/// 类型转换
		/// </summary>
		TSQL This => this as TSQL;

		/// <summary>
		/// 调试或输出用
		/// </summary>
		/// <param name="field"></param>
		/// <returns></returns>
		public string ToString(string field)
		{
			if (!string.IsNullOrEmpty(field)) Fields = field;
			return TypeHelper.SqlToString(CommandText, Params);
		}

		/// <summary>
		/// 设置sql语句
		/// </summary>
		/// <returns></returns>
		public abstract string GetCommandTextString();


		#region Implicit
		public static implicit operator string(SqlBuilder<TSQL> builder) => builder.ToString();
		#endregion
	}
}

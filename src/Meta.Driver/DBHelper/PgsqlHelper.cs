using Meta.Driver.Extensions;
using Meta.Driver.Interface;
using Meta.Driver.Model;
using Microsoft.Extensions.Logging;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Meta.Driver.DbHelper
{
	/// <summary>
	/// pgsql帮助类, 选择库
	/// </summary>
	/// <typeparam name="TDbName">库名</typeparam>
	public class PgsqlHelper<TDbName> : PgsqlHelper where TDbName : struct, IDbName
	{
		private static string GetDbName => typeof(TDbName).Name;

		/// <summary>
		/// 查询单个元素
		/// </summary>
		/// <param name="cmdType">命令类型</param>
		/// <param name="cmdText">sql语句</param>
		/// <param name="cmdParams">sql参数</param>
		/// <returns>返回(0,0)值</returns>
		public new static object ExecuteScalar(string cmdText, CommandType cmdType = CommandType.Text, DbParameter[] cmdParams = null)
			=> GetExecute(GetDbName).ExecuteScalar(cmdText, cmdType, cmdParams);

		/// <summary>
		/// 查询单个元素
		/// </summary>
		/// <param name="cmdType">命令类型</param>
		/// <param name="cmdText">sql语句</param>
		/// <param name="cmdParams">sql参数</param>
		/// <param name="cancellationToken"></param>
		/// <returns>返回(0,0)值</returns>
		public new static ValueTask<object> ExecuteScalarAsync(string cmdText, CommandType cmdType = CommandType.Text, DbParameter[] cmdParams = null, CancellationToken cancellationToken = default)
			=> GetExecute(GetDbName).ExecuteScalarAsync(cmdText, cmdType, cmdParams, cancellationToken);

		/// <summary>
		/// 查询单个元素
		/// </summary>
		/// <param name="cmdType">命令类型</param>
		/// <param name="cmdText">sql语句</param>
		/// <param name="cmdParams">sql参数</param>
		/// <returns>返回(0,0)值</returns>
		public new static object ExecuteScalar<T>(string cmdText, CommandType cmdType = CommandType.Text, DbParameter[] cmdParams = null)
			=> GetExecute(GetDbName).ExecuteScalar<T>(cmdText, cmdType, cmdParams);

		/// <summary>
		/// 查询单个元素
		/// </summary>
		/// <param name="cmdType">命令类型</param>
		/// <param name="cmdText">sql语句</param>
		/// <param name="cmdParams">sql参数</param>
		/// <param name="cancellationToken"></param>
		/// <returns>返回(0,0)值</returns>
		public new static ValueTask<T> ExecuteScalarAsync<T>(string cmdText, CommandType cmdType = CommandType.Text, DbParameter[] cmdParams = null, CancellationToken cancellationToken = default)
			=> GetExecute(GetDbName).ExecuteScalarAsync<T>(cmdText, cmdType, cmdParams, cancellationToken);

		/// <summary>
		/// 执行NonQuery
		/// </summary>
		/// <param name="cmdType">命令类型</param>
		/// <param name="cmdText">sql语句</param>
		/// <param name="cmdParams">sql参数</param>
		/// <returns>修改行数</returns>
		public new static int ExecuteNonQuery(string cmdText, CommandType cmdType = CommandType.Text, DbParameter[] cmdParams = null)
			=> GetExecute(GetDbName).ExecuteNonQuery(cmdText, cmdType, cmdParams);

		/// <summary>
		/// 执行NonQuery
		/// </summary>
		/// <param name="cmdType">命令类型</param>
		/// <param name="cmdText">sql语句</param>
		/// <param name="cmdParams">sql参数</param>
		/// <param name="cancellationToken"></param>
		/// <returns>修改行数</returns>
		public new static ValueTask<int> ExecuteNonQueryAsync(string cmdText, CommandType cmdType = CommandType.Text, DbParameter[] cmdParams = null, CancellationToken cancellationToken = default)
			=> GetExecute(GetDbName).ExecuteNonQueryAsync(cmdText, cmdType, cmdParams, cancellationToken);

		/// <summary>
		/// DataReader
		/// </summary>
		/// <param name="action">逐行Reader委托</param>
		/// <param name="cmdType">命令类型</param>
		/// <param name="cmdText">sql语句</param>
		/// <param name="cmdParams">sql参数</param>
		public new static void ExecuteDataReader(Action<DbDataReader> action, string cmdText, CommandType cmdType = CommandType.Text, DbParameter[] cmdParams = null)
			=> GetExecute(GetDbName).ExecuteDataReader(action, cmdText, cmdType, cmdParams);

		/// <summary>
		/// DataReader
		/// </summary>
		/// <param name="action">逐行Reader委托</param>
		/// <param name="cmdType">命令类型</param>
		/// <param name="cmdText">sql语句</param>
		/// <param name="cmdParams">sql参数</param>
		/// <param name="cancellationToken"></param>
		public new static Task ExecuteDataReaderAsync(Action<DbDataReader> action, string cmdText, CommandType cmdType = CommandType.Text, DbParameter[] cmdParams = null, CancellationToken cancellationToken = default)
			=> GetExecute(GetDbName).ExecuteDataReaderAsync(action, cmdText, cmdType, cmdParams, cancellationToken);

		/// <summary>
		/// 查询多行
		/// </summary>
		/// <typeparam name="T">列表类型</typeparam>
		/// <param name="cmdText">sql语句</param>
		/// <param name="cmdType"></param>
		/// <param name="cmdParams">sql参数</param>
		/// <returns>列表</returns>
		public new static List<T> ExecuteDataReaderList<T>(string cmdText, CommandType cmdType = CommandType.Text, DbParameter[] cmdParams = null)
			=> GetExecute(GetDbName).ExecuteDataReaderListAsync<T>(false, cmdText, cmdType, cmdParams, CancellationToken.None).ConfigureAwait(false).GetAwaiter().GetResult();

		/// <summary>
		/// 查询多行
		/// </summary>
		/// <typeparam name="T">列表类型</typeparam>
		/// <param name="cmdText">sql语句</param>
		/// <param name="cmdType"></param>
		/// <param name="cmdParams">sql参数</param>
		/// <param name="cancellationToken"></param>
		/// <returns>列表</returns>
		public new static Task<List<T>> ExecuteDataReaderListAsync<T>(string cmdText, CommandType cmdType = CommandType.Text, DbParameter[] cmdParams = null, CancellationToken cancellationToken = default)
			=> cancellationToken.IsCancellationRequested
			? Task.FromCanceled<List<T>>(cancellationToken)
			: GetExecute(GetDbName).ExecuteDataReaderListAsync<T>(true, cmdText, cmdType, cmdParams, cancellationToken);

		/// <summary>
		/// 查询一行
		/// </summary>
		/// <typeparam name="T">实体类型</typeparam>
		/// <param name="cmdText">sql语句</param>
		/// <param name="cmdType"></param>
		/// <param name="cmdParams">sql参数</param>
		/// <returns>实体</returns>
		public new static T ExecuteDataReaderModel<T>(string cmdText, CommandType cmdType = CommandType.Text, DbParameter[] cmdParams = null)
			=> GetExecute(GetDbName).ExecuteDataReaderModelAsync<T>(false, cmdText, cmdType, cmdParams, CancellationToken.None).ConfigureAwait(false).GetAwaiter().GetResult();

		/// <summary>
		/// 查询一行
		/// </summary>
		/// <typeparam name="T">实体类型</typeparam>
		/// <param name="cmdText">sql语句</param>
		/// <param name="cmdType"></param>
		/// <param name="cmdParams">sql参数</param>
		/// <param name="cancellationToken"></param>
		/// <returns>实体</returns>
		public new static Task<T> ExecuteDataReaderModelAsync<T>(string cmdText, CommandType cmdType = CommandType.Text, DbParameter[] cmdParams = null, CancellationToken cancellationToken = default)
			=> cancellationToken.IsCancellationRequested
			? Task.FromCanceled<T>(cancellationToken)
			: GetExecute(GetDbName).ExecuteDataReaderModelAsync<T>(true, cmdText, cmdType, cmdParams, cancellationToken);

		/// <summary>
		/// DataReader pipe
		/// </summary>
		/// <param name="builders">sql builder</param>
		/// <param name="cmdType">命令类型</param>
		/// <exception cref="ArgumentNullException">builders is null or empty</exception>
		/// <returns>实体</returns>
		public new static object[] ExecuteDataReaderPipe(IEnumerable<ISqlBuilder> builders, CommandType cmdType = CommandType.Text)
			=> GetExecute(GetDbName).ExecuteDataReaderPipeAsync(false, builders, cmdType, CancellationToken.None).ConfigureAwait(false).GetAwaiter().GetResult();

		/// <summary>
		/// DataReader pipe
		/// </summary>
		/// <param name="builders">sql builder</param>
		/// <param name="cmdType">命令类型</param>
		/// <param name="cancellationToken"></param>
		/// <exception cref="ArgumentNullException">builders is null or empty</exception>
		/// <returns>实体</returns>
		public new static Task<object[]> ExecuteDataReaderPipeAsync(IEnumerable<ISqlBuilder> builders, CommandType cmdType = CommandType.Text, CancellationToken cancellationToken = default)
			=> cancellationToken.IsCancellationRequested
			? Task.FromCanceled<object[]>(cancellationToken)
			: GetExecute(GetDbName).ExecuteDataReaderPipeAsync(true, builders, cmdType, cancellationToken).AsTask();

		/// <summary>
		/// 事务 (暂不支持分布式事务)
		/// </summary>
		/// <param name="action">Action委托</param>
		/// <exception cref="ArgumentNullException">委托是null</exception>
		public new static void Transaction(Action action)
			=> GetExecute(GetDbName).TransactionAsync(false, action, CancellationToken.None).ConfigureAwait(false).GetAwaiter().GetResult();

		/// <summary>
		/// 事务 (暂不支持分布式事务)
		/// </summary>
		/// <param name="action">Action委托</param>
		/// <param name="cancellationToken"></param>
		/// <exception cref="ArgumentNullException">委托是null</exception>
		public new static Task TransactionAsync(Action action, CancellationToken cancellationToken = default)
			=> cancellationToken.IsCancellationRequested
			? Task.FromCanceled(cancellationToken)
			: GetExecute(GetDbName).TransactionAsync(true, action, cancellationToken).AsTask();

		/// <summary>
		/// 开启事务
		/// </summary>
		public new static void BeginTransaction()
			=> GetExecute(GetDbName).BeginTransaction();

		/// <summary>
		/// 开启事务
		/// </summary>
		public new static Task BeginTransactionAsync(CancellationToken cancellationToken)
			=> GetExecute(GetDbName).BeginTransactionAsync(cancellationToken);

		/// <summary>
		/// 确认事务
		/// </summary>
		public new static void CommitTransaction()
			=> GetExecute(GetDbName).CommitTransaction();

		/// <summary>
		/// 确认事务
		/// </summary>
		public new static Task CommitTransactionAsync(CancellationToken cancellationToken)
			=> GetExecute(GetDbName).CommitTransactionAsync(cancellationToken);

		/// <summary>
		/// 回滚事务
		/// </summary>
		public new static void RollBackTransaction()
			=> GetExecute(GetDbName).RollBackTransaction();

		/// <summary>
		/// 回滚事务
		/// </summary>
		public new static Task RollBackTransactionAsync(CancellationToken cancellationToken)
			=> GetExecute(GetDbName).RollBackTransactionAsync(cancellationToken);
	}
	/// <summary>
	/// 
	/// </summary>
	public class PgsqlHelper
	{
		/// <summary>
		/// 实例键值对
		/// </summary>
		static readonly Dictionary<string, List<DbExecute>> _executeDict = new Dictionary<string, List<DbExecute>>();

		/// <summary>
		/// 是否使用主库优先
		/// </summary>
		internal static bool IsSlaveFirst = false;

		/// <summary>
		/// 当没有从库的时候自动使用主库
		/// </summary>
		static bool _useMasterIfSlaveIsEmpty = false;

		/// <summary>
		/// 默认数据库名称
		/// </summary>
		internal static string DefaultDbName;

		/// <summary>
		/// 实现Pgsql
		/// </summary>
		internal class PgsqlExecute : DbExecute
		{
			public PgsqlExecute(DbConnectionModel conn) : base(conn) { }
		}

		/// <summary>
		/// 从库后缀
		/// </summary>
		public const string SLAVE_SUFFIX = "Slave";

		/// <summary>
		/// 获取连接实例
		/// </summary>
		/// <param name="type">数据库类型</param>
		/// <exception cref="ArgumentNullException">没有找到对应名称实例</exception>
		/// <returns>对应实例</returns>
		internal static DbExecute GetExecute(string type)
		{
			if (_executeDict.ContainsKey(type))
			{
				var execute = _executeDict[type];

				return execute.Count switch
				{
					0 when _useMasterIfSlaveIsEmpty && type.EndsWith(SLAVE_SUFFIX) =>
						GetExecute(type.Replace(SLAVE_SUFFIX, string.Empty)),

					1 => execute[0],

					_ => execute[Math.Abs(Guid.NewGuid().GetHashCode() % execute.Count)],
				};
			}
			else if (_useMasterIfSlaveIsEmpty && type.EndsWith(SLAVE_SUFFIX))
					return GetExecute(type.Replace(SLAVE_SUFFIX, string.Empty));
			
			// 从没有从库连接会查主库->如果没有连接会报错
			throw new ArgumentNullException("connectionstring", $"not exist {type} execute");
		}

		/// <summary>
		/// 初始化一主多从数据库连接, 从库后缀默认:"Slave"
		/// </summary>
		/// <param name="options">数据库连接</param>
		/// <param name="useMasterIfSlaveIsEmpty">如果没有从库自动使用主库而不会抛出错误, 默认抛出, 从库后缀默认:"slave"</param>
		/// <param name="isSlaveFirst">不指定的情况下, 是否先查从库</param>
		/// <exception cref="ArgumentNullException">options is null</exception>
		/// <exception cref="ArgumentOutOfRangeException">options长度为0</exception>
		public static void InitDBConnectionOption<TDefaultDbName>(IDbOption[] options, bool useMasterIfSlaveIsEmpty = false, bool isSlaveFirst = false) where TDefaultDbName : struct, IDbName
		{
			IsSlaveFirst = isSlaveFirst;
			DefaultDbName = typeof(TDefaultDbName).Name;
			_useMasterIfSlaveIsEmpty = useMasterIfSlaveIsEmpty;
			if (options == null)
				throw new ArgumentNullException(nameof(options));
			if (options.Count() == 0)
				throw new ArgumentOutOfRangeException(nameof(options));
			foreach (var option in options)
			{
				if (option.Master == null)
					throw new ArgumentNullException(nameof(option.Master), $"Connection string model is null");

				_executeDict[option.Master.DbName] = new List<DbExecute> { new PgsqlExecute(option.Master) };

				if (option.Slave == null) continue;

				foreach (var item in option.Slave)
				{
					if (!_executeDict.ContainsKey(item.DbName))
						_executeDict[item.DbName] = new List<DbExecute> { new PgsqlExecute(item) };
					else
						_executeDict[item.DbName].Add(new PgsqlExecute(item));
				}
			}
		}

		/// <summary>
		/// 查询单个元素
		/// </summary>
		/// <param name="cmdType">命令类型</param>
		/// <param name="cmdText">sql语句</param>
		/// <param name="cmdParams">sql参数</param>
		/// <returns>返回(0,0)值</returns>
		public static object ExecuteScalar(string cmdText, CommandType cmdType = CommandType.Text, DbParameter[] cmdParams = null)
			=> GetExecute(DefaultDbName).ExecuteScalar(cmdText, cmdType, cmdParams);

		/// <summary>
		/// 查询单个元素
		/// </summary>
		/// <param name="cmdType">命令类型</param>
		/// <param name="cmdText">sql语句</param>
		/// <param name="cmdParams">sql参数</param>
		/// <param name="cancellationToken"></param>
		/// <returns>返回(0,0)值</returns>
		public static ValueTask<object> ExecuteScalarAsync(string cmdText, CommandType cmdType = CommandType.Text, DbParameter[] cmdParams = null, CancellationToken cancellationToken = default)
			=> GetExecute(DefaultDbName).ExecuteScalarAsync(cmdText, cmdType, cmdParams, cancellationToken);

		/// <summary>
		/// 查询单个元素
		/// </summary>
		/// <param name="cmdType">命令类型</param>
		/// <param name="cmdText">sql语句</param>
		/// <param name="cmdParams">sql参数</param>
		/// <returns>返回(0,0)值</returns>
		public static object ExecuteScalar<T>(string cmdText, CommandType cmdType = CommandType.Text, DbParameter[] cmdParams = null)
			=> GetExecute(DefaultDbName).ExecuteScalar<T>(cmdText, cmdType, cmdParams);

		/// <summary>
		/// 查询单个元素
		/// </summary>
		/// <param name="cmdType">命令类型</param>
		/// <param name="cmdText">sql语句</param>
		/// <param name="cmdParams">sql参数</param>
		/// <param name="cancellationToken"></param>
		/// <returns>返回(0,0)值</returns>
		public static ValueTask<T> ExecuteScalarAsync<T>(string cmdText, CommandType cmdType = CommandType.Text, DbParameter[] cmdParams = null, CancellationToken cancellationToken = default)
			=> GetExecute(DefaultDbName).ExecuteScalarAsync<T>(cmdText, cmdType, cmdParams, cancellationToken);

		/// <summary>
		/// 执行NonQuery
		/// </summary>
		/// <param name="cmdType">命令类型</param>
		/// <param name="cmdText">sql语句</param>
		/// <param name="cmdParams">sql参数</param>
		/// <returns>修改行数</returns>
		public static int ExecuteNonQuery(string cmdText, CommandType cmdType = CommandType.Text, DbParameter[] cmdParams = null)
			=> GetExecute(DefaultDbName).ExecuteNonQuery(cmdText, cmdType, cmdParams);

		/// <summary>
		/// 执行NonQuery
		/// </summary>
		/// <param name="cmdType">命令类型</param>
		/// <param name="cmdText">sql语句</param>
		/// <param name="cmdParams">sql参数</param>
		/// <param name="cancellationToken"></param>
		/// <returns>修改行数</returns>
		public static ValueTask<int> ExecuteNonQueryAsync(string cmdText, CommandType cmdType = CommandType.Text, DbParameter[] cmdParams = null, CancellationToken cancellationToken = default)
			=> GetExecute(DefaultDbName).ExecuteNonQueryAsync(cmdText, cmdType, cmdParams, cancellationToken);

		/// <summary>
		/// DataReader
		/// </summary>
		/// <param name="action">逐行Reader委托</param>
		/// <param name="cmdType">命令类型</param>
		/// <param name="cmdText">sql语句</param>
		/// <param name="cmdParams">sql参数</param>
		public static void ExecuteDataReader(Action<DbDataReader> action, string cmdText, CommandType cmdType = CommandType.Text, DbParameter[] cmdParams = null)
			=> GetExecute(DefaultDbName).ExecuteDataReader(action, cmdText, cmdType, cmdParams);

		/// <summary>
		/// DataReader
		/// </summary>
		/// <param name="action">逐行Reader委托</param>
		/// <param name="cmdType">命令类型</param>
		/// <param name="cmdText">sql语句</param>
		/// <param name="cmdParams">sql参数</param>
		/// <param name="cancellationToken"></param>
		public static Task ExecuteDataReaderAsync(Action<DbDataReader> action, string cmdText, CommandType cmdType = CommandType.Text, DbParameter[] cmdParams = null, CancellationToken cancellationToken = default)
			=> cancellationToken.IsCancellationRequested
			? Task.FromCanceled(cancellationToken)
			: GetExecute(DefaultDbName).ExecuteDataReaderAsync(action, cmdText, cmdType, cmdParams, cancellationToken);

		/// <summary>
		/// 查询多行
		/// </summary>
		/// <typeparam name="T">列表类型</typeparam>
		/// <param name="cmdText">sql语句</param>
		/// <param name="cmdType"></param>
		/// <param name="cmdParams">sql参数</param>
		/// <returns>列表</returns>
		public static List<T> ExecuteDataReaderList<T>(string cmdText, CommandType cmdType = CommandType.Text, DbParameter[] cmdParams = null)
			=> GetExecute(DefaultDbName).ExecuteDataReaderListAsync<T>(false, cmdText, cmdType, cmdParams, CancellationToken.None).ConfigureAwait(false).GetAwaiter().GetResult();

		/// <summary>
		/// 查询多行
		/// </summary>
		/// <typeparam name="T">列表类型</typeparam>
		/// <param name="cmdText">sql语句</param>
		/// <param name="cmdType"></param>
		/// <param name="cmdParams">sql参数</param>
		/// <param name="cancellationToken"></param>
		/// <returns>列表</returns>
		public static Task<List<T>> ExecuteDataReaderListAsync<T>(string cmdText, CommandType cmdType = CommandType.Text, DbParameter[] cmdParams = null, CancellationToken cancellationToken = default)
			=> cancellationToken.IsCancellationRequested
			? Task.FromCanceled<List<T>>(cancellationToken)
			: GetExecute(DefaultDbName).ExecuteDataReaderListAsync<T>(true, cmdText, cmdType, cmdParams, cancellationToken);

		/// <summary>
		/// 查询一行
		/// </summary>
		/// <typeparam name="T">实体类型</typeparam>
		/// <param name="cmdText">sql语句</param>
		/// <param name="cmdType"></param>
		/// <param name="cmdParams">sql参数</param>
		/// <returns>实体</returns>
		public static T ExecuteDataReaderModel<T>(string cmdText, CommandType cmdType = CommandType.Text, DbParameter[] cmdParams = null)
			=> GetExecute(DefaultDbName).ExecuteDataReaderModelAsync<T>(false, cmdText, cmdType, cmdParams, CancellationToken.None).ConfigureAwait(false).GetAwaiter().GetResult();

		/// <summary>
		/// 查询一行
		/// </summary>
		/// <typeparam name="T">实体类型</typeparam>
		/// <param name="cmdText">sql语句</param>
		/// <param name="cmdType"></param>
		/// <param name="cmdParams">sql参数</param>
		/// <param name="cancellationToken"></param>
		/// <returns>实体</returns>
		public static Task<T> ExecuteDataReaderModelAsync<T>(string cmdText, CommandType cmdType = CommandType.Text, DbParameter[] cmdParams = null, CancellationToken cancellationToken = default)
			=> cancellationToken.IsCancellationRequested ? Task.FromCanceled<T>(cancellationToken) : GetExecute(DefaultDbName).ExecuteDataReaderModelAsync<T>(true, cmdText, cmdType, cmdParams, cancellationToken);

		/// <summary>
		/// DataReader pipe
		/// </summary>
		/// <param name="builders">sql builder</param>
		/// <param name="cmdType">命令类型</param>
		/// <exception cref="ArgumentNullException">builders is null or empty</exception>
		/// <returns>实体</returns>
		public static object[] ExecuteDataReaderPipe(IEnumerable<ISqlBuilder> builders, CommandType cmdType = CommandType.Text)
			=> GetExecute(DefaultDbName).ExecuteDataReaderPipeAsync(false, builders, cmdType, CancellationToken.None).ConfigureAwait(false).GetAwaiter().GetResult();

		/// <summary>
		/// DataReader pipe
		/// </summary>
		/// <param name="builders">sql builder</param>
		/// <param name="cmdType">命令类型</param>
		/// <param name="cancellationToken"></param>
		/// <exception cref="ArgumentNullException">builders is null or empty</exception>
		/// <returns>实体</returns>
		public static Task<object[]> ExecuteDataReaderPipeAsync(IEnumerable<ISqlBuilder> builders, CommandType cmdType = CommandType.Text, CancellationToken cancellationToken = default)
			=> cancellationToken.IsCancellationRequested
			? Task.FromCanceled<object[]>(cancellationToken)
			: GetExecute(DefaultDbName).ExecuteDataReaderPipeAsync(true, builders, cmdType, cancellationToken).AsTask();

		/// <summary>
		/// 事务 (暂不支持分布式事务)
		/// </summary>
		/// <param name="action">Action委托</param>
		/// <exception cref="ArgumentNullException">委托是null</exception>
		public static void Transaction(Action action)
			=> GetExecute(DefaultDbName).TransactionAsync(false, action, CancellationToken.None).ConfigureAwait(false).GetAwaiter().GetResult();

		/// <summary>
		/// 事务 (暂不支持分布式事务)
		/// </summary>
		/// <param name="action">Action委托</param>
		/// <param name="cancellationToken"></param>
		/// <exception cref="ArgumentNullException">委托是null</exception>
		public static ValueTask TransactionAsync(Action action, CancellationToken cancellationToken = default)
			=> GetExecute(DefaultDbName).TransactionAsync(true, action, cancellationToken);

		/// <summary>
		/// 开启事务
		/// </summary>
		public static void BeginTransaction()
			=> GetExecute(DefaultDbName).BeginTransaction();

		/// <summary>
		/// 开启事务
		/// </summary>
		public static Task BeginTransactionAsync(CancellationToken cancellationToken)
			=> GetExecute(DefaultDbName).BeginTransactionAsync(cancellationToken);

		/// <summary>
		/// 确认事务
		/// </summary>
		public static void CommitTransaction()
			=> GetExecute(DefaultDbName).CommitTransaction();

		/// <summary>
		/// 确认事务
		/// </summary>
		public static Task CommitTransactionAsync(CancellationToken cancellationToken)
			=> GetExecute(DefaultDbName).CommitTransactionAsync(cancellationToken);

		/// <summary>
		/// 回滚事务
		/// </summary>
		public static void RollBackTransaction()
			=> GetExecute(DefaultDbName).RollBackTransaction();

		/// <summary>
		/// 回滚事务
		/// </summary>
		public static Task RollBackTransactionAsync(CancellationToken cancellationToken)
			=> GetExecute(DefaultDbName).RollBackTransactionAsync(cancellationToken);

	}
}

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
			=> GetExecute(GetDbName).ExecuteDataReaderListAsync<T>(cmdText, cmdType, cmdParams, false, CancellationToken.None).ConfigureAwait(false).GetAwaiter().GetResult();

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
			=> cancellationToken.IsCancellationRequested ? Task.FromCanceled<List<T>>(cancellationToken) : GetExecute(GetDbName).ExecuteDataReaderListAsync<T>(cmdText, cmdType, cmdParams, true, cancellationToken);

		/// <summary>
		/// 查询一行
		/// </summary>
		/// <typeparam name="T">实体类型</typeparam>
		/// <param name="cmdText">sql语句</param>
		/// <param name="cmdType"></param>
		/// <param name="cmdParams">sql参数</param>
		/// <returns>实体</returns>
		public new static T ExecuteDataReaderModel<T>(string cmdText, CommandType cmdType = CommandType.Text, DbParameter[] cmdParams = null)
			=> GetExecute(GetDbName).ExecuteDataReaderModelAsync<T>(cmdText, cmdType, cmdParams, false, CancellationToken.None).ConfigureAwait(false).GetAwaiter().GetResult();

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
			=> cancellationToken.IsCancellationRequested ? Task.FromCanceled<T>(cancellationToken) : GetExecute(GetDbName).ExecuteDataReaderModelAsync<T>(cmdText, cmdType, cmdParams, true, cancellationToken);

		/// <summary>
		/// DataReader pipe
		/// </summary>
		/// <param name="builders">sql builder</param>
		/// <param name="cmdType">命令类型</param>
		/// <exception cref="ArgumentNullException">builders is null or empty</exception>
		/// <returns>实体</returns>
		public new static object[] ExecuteDataReaderPipe(IEnumerable<ISqlBuilder> builders, CommandType cmdType = CommandType.Text)
			=> GetExecute(GetDbName).ExecuteDataReaderPipeAsync(builders, cmdType, false, CancellationToken.None).ConfigureAwait(false).GetAwaiter().GetResult();

		/// <summary>
		/// DataReader pipe
		/// </summary>
		/// <param name="builders">sql builder</param>
		/// <param name="cmdType">命令类型</param>
		/// <param name="cancellationToken"></param>
		/// <exception cref="ArgumentNullException">builders is null or empty</exception>
		/// <returns>实体</returns>
		public new static Task<object[]> ExecuteDataReaderPipeAsync(IEnumerable<ISqlBuilder> builders, CommandType cmdType = CommandType.Text, CancellationToken cancellationToken = default)
			=> cancellationToken.IsCancellationRequested ? Task.FromCanceled<object[]>(cancellationToken) : GetExecute(GetDbName).ExecuteDataReaderPipeAsync(builders, cmdType, true, cancellationToken).AsTask();

		/// <summary>
		/// 事务 (暂不支持分布式事务)
		/// </summary>
		/// <param name="action">Action委托</param>
		/// <exception cref="ArgumentNullException">委托是null</exception>
		public new static void Transaction(Action action)
			=> GetExecute(GetDbName).TransactionAsync(action, false, CancellationToken.None).ConfigureAwait(false).GetAwaiter().GetResult();

		/// <summary>
		/// 事务 (暂不支持分布式事务)
		/// </summary>
		/// <param name="action">Action委托</param>
		/// <param name="cancellationToken"></param>
		/// <exception cref="ArgumentNullException">委托是null</exception>
		public new static Task TransactionAsync(Action action, CancellationToken cancellationToken = default)
			=> cancellationToken.IsCancellationRequested ? Task.FromCanceled(cancellationToken) : GetExecute(GetDbName).TransactionAsync(action, true, cancellationToken).AsTask();

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
		/// 随机从库
		/// </summary>
		static readonly Random _ran;
		/// <summary>
		/// 当没有从库的时候自动使用主库
		/// </summary>
		static bool _useMasterIfSlaveIsEmpty = false;

		/// <summary>
		/// 默认数据库名称
		/// </summary>
		static string _defaultDbName;
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
		public const string SlaveSuffix = "Slave";

		/// <summary>
		/// 静态构造
		/// </summary>
		static PgsqlHelper()
		{
			_ran = new Random(DateTime.Now.GetHashCode());
		}

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
					0 when _useMasterIfSlaveIsEmpty && type.EndsWith(SlaveSuffix) =>
						GetExecute(type.Replace(SlaveSuffix, string.Empty)),

					1 => execute[0],

					_ => execute[_ran.Next(0, execute.Count)],
				};
			}
			// 从没有从库连接会查主库->如果没有连接会报错
			throw new ArgumentNullException("connectionstring", $"not exist {type} execute");
		}

		/// <summary>
		/// 初始化一主多从数据库连接, 从库后缀默认:"Slave"
		/// </summary>
		/// <param name="options">数据库连接</param>
		/// <param name="useMasterIfSlaveIsEmpty">如果没有从库自动使用主库而不会抛出错误, 默认抛出, 从库后缀默认:"slave"</param>
		/// <exception cref="ArgumentNullException"></exception>
		/// <exception cref="ArgumentOutOfRangeException"></exception>
		public static void InitDBConnectionOption<TDefaultDbName>(IDbOption[] options, bool useMasterIfSlaveIsEmpty = false) where TDefaultDbName : struct, IDbName
		{
			_defaultDbName = typeof(TDefaultDbName).Name;
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
			=> GetExecute(_defaultDbName).ExecuteScalar(cmdText, cmdType, cmdParams);

		/// <summary>
		/// 查询单个元素
		/// </summary>
		/// <param name="cmdType">命令类型</param>
		/// <param name="cmdText">sql语句</param>
		/// <param name="cmdParams">sql参数</param>
		/// <param name="cancellationToken"></param>
		/// <returns>返回(0,0)值</returns>
		public static ValueTask<object> ExecuteScalarAsync(string cmdText, CommandType cmdType = CommandType.Text, DbParameter[] cmdParams = null, CancellationToken cancellationToken = default)
			=> GetExecute(_defaultDbName).ExecuteScalarAsync(cmdText, cmdType, cmdParams, cancellationToken);

		/// <summary>
		/// 执行NonQuery
		/// </summary>
		/// <param name="cmdType">命令类型</param>
		/// <param name="cmdText">sql语句</param>
		/// <param name="cmdParams">sql参数</param>
		/// <returns>修改行数</returns>
		public static int ExecuteNonQuery(string cmdText, CommandType cmdType = CommandType.Text, DbParameter[] cmdParams = null)
			=> GetExecute(_defaultDbName).ExecuteNonQuery(cmdText, cmdType, cmdParams);

		/// <summary>
		/// 执行NonQuery
		/// </summary>
		/// <param name="cmdType">命令类型</param>
		/// <param name="cmdText">sql语句</param>
		/// <param name="cmdParams">sql参数</param>
		/// <param name="cancellationToken"></param>
		/// <returns>修改行数</returns>
		public static ValueTask<int> ExecuteNonQueryAsync(string cmdText, CommandType cmdType = CommandType.Text, DbParameter[] cmdParams = null, CancellationToken cancellationToken = default)
			=> GetExecute(_defaultDbName).ExecuteNonQueryAsync(cmdText, cmdType, cmdParams, cancellationToken);

		/// <summary>
		/// DataReader
		/// </summary>
		/// <param name="action">逐行Reader委托</param>
		/// <param name="cmdType">命令类型</param>
		/// <param name="cmdText">sql语句</param>
		/// <param name="cmdParams">sql参数</param>
		public static void ExecuteDataReader(Action<DbDataReader> action, string cmdText, CommandType cmdType = CommandType.Text, DbParameter[] cmdParams = null)
			=> GetExecute(_defaultDbName).ExecuteDataReader(action, cmdText, cmdType, cmdParams);

		/// <summary>
		/// DataReader
		/// </summary>
		/// <param name="action">逐行Reader委托</param>
		/// <param name="cmdType">命令类型</param>
		/// <param name="cmdText">sql语句</param>
		/// <param name="cmdParams">sql参数</param>
		/// <param name="cancellationToken"></param>
		public static Task ExecuteDataReaderAsync(Action<DbDataReader> action, string cmdText, CommandType cmdType = CommandType.Text, DbParameter[] cmdParams = null, CancellationToken cancellationToken = default)
			=> cancellationToken.IsCancellationRequested ? Task.FromCanceled(cancellationToken) : GetExecute(_defaultDbName).ExecuteDataReaderAsync(action, cmdText, cmdType, cmdParams, cancellationToken);

		/// <summary>
		/// 查询多行
		/// </summary>
		/// <typeparam name="T">列表类型</typeparam>
		/// <param name="cmdText">sql语句</param>
		/// <param name="cmdType"></param>
		/// <param name="cmdParams">sql参数</param>
		/// <returns>列表</returns>
		public static List<T> ExecuteDataReaderList<T>(string cmdText, CommandType cmdType = CommandType.Text, DbParameter[] cmdParams = null)
			=> GetExecute(_defaultDbName).ExecuteDataReaderListAsync<T>(cmdText, cmdType, cmdParams, false, CancellationToken.None).ConfigureAwait(false).GetAwaiter().GetResult();

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
			=> cancellationToken.IsCancellationRequested ? Task.FromCanceled<List<T>>(cancellationToken) : GetExecute(_defaultDbName).ExecuteDataReaderListAsync<T>(cmdText, cmdType, cmdParams, true, cancellationToken);

		/// <summary>
		/// 查询一行
		/// </summary>
		/// <typeparam name="T">实体类型</typeparam>
		/// <param name="cmdText">sql语句</param>
		/// <param name="cmdType"></param>
		/// <param name="cmdParams">sql参数</param>
		/// <returns>实体</returns>
		public static T ExecuteDataReaderModel<T>(string cmdText, CommandType cmdType = CommandType.Text, DbParameter[] cmdParams = null)
			=> GetExecute(_defaultDbName).ExecuteDataReaderModelAsync<T>(cmdText, cmdType, cmdParams, false, CancellationToken.None).ConfigureAwait(false).GetAwaiter().GetResult();

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
			=> cancellationToken.IsCancellationRequested ? Task.FromCanceled<T>(cancellationToken) : GetExecute(_defaultDbName).ExecuteDataReaderModelAsync<T>(cmdText, cmdType, cmdParams, true, cancellationToken);

		/// <summary>
		/// DataReader pipe
		/// </summary>
		/// <param name="builders">sql builder</param>
		/// <param name="cmdType">命令类型</param>
		/// <exception cref="ArgumentNullException">builders is null or empty</exception>
		/// <returns>实体</returns>
		public static object[] ExecuteDataReaderPipe(IEnumerable<ISqlBuilder> builders, CommandType cmdType = CommandType.Text)
			=> GetExecute(_defaultDbName).ExecuteDataReaderPipeAsync(builders, cmdType, false, CancellationToken.None).ConfigureAwait(false).GetAwaiter().GetResult();

		/// <summary>
		/// DataReader pipe
		/// </summary>
		/// <param name="builders">sql builder</param>
		/// <param name="cmdType">命令类型</param>
		/// <param name="cancellationToken"></param>
		/// <exception cref="ArgumentNullException">builders is null or empty</exception>
		/// <returns>实体</returns>
		public static Task<object[]> ExecuteDataReaderPipeAsync(IEnumerable<ISqlBuilder> builders, CommandType cmdType = CommandType.Text, CancellationToken cancellationToken = default)
			=> cancellationToken.IsCancellationRequested ? Task.FromCanceled<object[]>(cancellationToken) : GetExecute(_defaultDbName).ExecuteDataReaderPipeAsync(builders, cmdType, true, cancellationToken).AsTask();

		/// <summary>
		/// 事务 (暂不支持分布式事务)
		/// </summary>
		/// <param name="action">Action委托</param>
		/// <exception cref="ArgumentNullException">委托是null</exception>
		public static void Transaction(Action action)
			=> GetExecute(_defaultDbName).TransactionAsync(action, false, CancellationToken.None).ConfigureAwait(false).GetAwaiter().GetResult();

		/// <summary>
		/// 事务 (暂不支持分布式事务)
		/// </summary>
		/// <param name="action">Action委托</param>
		/// <param name="cancellationToken"></param>
		/// <exception cref="ArgumentNullException">委托是null</exception>
		public static ValueTask TransactionAsync(Action action, CancellationToken cancellationToken = default)
			=> GetExecute(_defaultDbName).TransactionAsync(action, true, cancellationToken);

		/// <summary>
		/// 开启事务
		/// </summary>
		public static void BeginTransaction()
			=> GetExecute(_defaultDbName).BeginTransaction();

		/// <summary>
		/// 开启事务
		/// </summary>
		public static Task BeginTransactionAsync(CancellationToken cancellationToken)
			=> GetExecute(_defaultDbName).BeginTransactionAsync(cancellationToken);

		/// <summary>
		/// 确认事务
		/// </summary>
		public static void CommitTransaction()
			=> GetExecute(_defaultDbName).CommitTransaction();

		/// <summary>
		/// 确认事务
		/// </summary>
		public static Task CommitTransactionAsync(CancellationToken cancellationToken)
			=> GetExecute(_defaultDbName).CommitTransactionAsync(cancellationToken);

		/// <summary>
		/// 回滚事务
		/// </summary>
		public static void RollBackTransaction()
			=> GetExecute(_defaultDbName).RollBackTransaction();
		/// <summary>
		/// 回滚事务
		/// </summary>
		public static Task RollBackTransactionAsync(CancellationToken cancellationToken)
			=> GetExecute(_defaultDbName).RollBackTransactionAsync(cancellationToken);

	}
}

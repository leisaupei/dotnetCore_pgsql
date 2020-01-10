using Meta.Common.Extensions;
using Meta.Common.Interface;
using Meta.Common.Model;
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

namespace Meta.Common.DbHelper
{
	public class PgsqlHelper<TDbName> : PgsqlHelper where TDbName : IDbName, new()
	{
		private static string GetDbName
		{
			get
			{
				var mapping = typeof(TDbName).GetCustomAttribute<DbNameAttribute>();
				if (mapping == null)
					throw new ArgumentNullException(nameof(DbNameAttribute));
				return mapping.DbName;
			}
		}
		/// <summary>
		/// 查询单个元素
		/// </summary>
		/// <param name="cmdType">命令类型</param>
		/// <param name="cmdText">sql语句</param>
		/// <param name="cmdParams">sql参数</param>
		/// <param name="type">数据库类型</param>
		/// <returns>返回(0,0)值</returns>
		public static object ExecuteScalar(string cmdText, CommandType cmdType = CommandType.Text, DbParameter[] cmdParams = null)
			=> ExecuteScalar(cmdText, cmdType, cmdParams, GetDbName);

		/// <summary>
		/// 查询单个元素
		/// </summary>
		/// <param name="cmdType">命令类型</param>
		/// <param name="cmdText">sql语句</param>
		/// <param name="cmdParams">sql参数</param>
		/// <param name="type">数据库类型</param>
		/// <param name="cancellationToken"></param>
		/// <returns>返回(0,0)值</returns>
		public static Task<object> ExecuteScalarAsync(string cmdText, CommandType cmdType = CommandType.Text, DbParameter[] cmdParams = null, CancellationToken cancellationToken = default)
			=> ExecuteScalarAsync(cmdText, cmdType, cmdParams, GetDbName, cancellationToken);

		/// <summary>
		/// 执行NonQuery
		/// </summary>
		/// <param name="cmdType">命令类型</param>
		/// <param name="cmdText">sql语句</param>
		/// <param name="cmdParams">sql参数</param>
		/// <param name="type">数据库类型</param>
		/// <returns>修改行数</returns>
		public static int ExecuteNonQuery(string cmdText, CommandType cmdType = CommandType.Text, DbParameter[] cmdParams = null)
			=> ExecuteNonQuery(cmdText, cmdType, cmdParams, GetDbName);

		/// <summary>
		/// 执行NonQuery
		/// </summary>
		/// <param name="cmdType">命令类型</param>
		/// <param name="cmdText">sql语句</param>
		/// <param name="cmdParams">sql参数</param>
		/// <param name="type">数据库类型</param>
		/// <param name="cancellationToken"></param>
		/// <returns>修改行数</returns>
		public static Task<int> ExecuteNonQueryAsync(string cmdText, CommandType cmdType = CommandType.Text, DbParameter[] cmdParams = null, CancellationToken cancellationToken = default)
			=> ExecuteNonQueryAsync(cmdText, cmdType, cmdParams, GetDbName, cancellationToken);

		/// <summary>
		/// DataReader
		/// </summary>
		/// <param name="action">逐行Reader委托</param>
		/// <param name="cmdType">命令类型</param>
		/// <param name="cmdText">sql语句</param>
		/// <param name="cmdParams">sql参数</param>
		/// <param name="type">数据库类型</param>
		public static void ExecuteDataReader(Action<DbDataReader> action, string cmdText, CommandType cmdType = CommandType.Text, DbParameter[] cmdParams = null)
			=> ExecuteDataReader(action, cmdText, cmdType, cmdParams, GetDbName);

		/// <summary>
		/// DataReader
		/// </summary>
		/// <param name="action">逐行Reader委托</param>
		/// <param name="cmdType">命令类型</param>
		/// <param name="cmdText">sql语句</param>
		/// <param name="cmdParams">sql参数</param>
		/// <param name="type">数据库类型</param>
		/// <param name="cancellationToken"></param>
		public static Task ExecuteDataReaderAsync(Action<DbDataReader> action, string cmdText, CommandType cmdType = CommandType.Text, DbParameter[] cmdParams = null, CancellationToken cancellationToken = default)
			=> ExecuteDataReaderAsync(action, cmdText, cmdType, cmdParams, GetDbName, cancellationToken);

		/// <summary>
		/// 查询多行
		/// </summary>
		/// <typeparam name="T">列表类型</typeparam>
		/// <param name="cmdText">sql语句</param>
		/// <param name="cmdType"></param>
		/// <param name="cmdParams">sql参数</param>
		/// <param name="type">数据库类型</param>
		/// <returns>列表</returns>
		public static List<T> ExecuteDataReaderList<T>(string cmdText, CommandType cmdType = CommandType.Text, DbParameter[] cmdParams = null)
			=> ExecuteDataReaderList<T>(cmdText, cmdType, cmdParams, GetDbName);

		/// <summary>
		/// 查询多行
		/// </summary>
		/// <typeparam name="T">列表类型</typeparam>
		/// <param name="cmdText">sql语句</param>
		/// <param name="cmdType"></param>
		/// <param name="cmdParams">sql参数</param>
		/// <param name="type">数据库类型</param>
		/// <param name="cancellationToken"></param>
		/// <returns>列表</returns>
		public static Task<List<T>> ExecuteDataReaderListAsync<T>(string cmdText, CommandType cmdType = CommandType.Text, DbParameter[] cmdParams = null, CancellationToken cancellationToken = default)
			=> ExecuteDataReaderListAsync<T>(cmdText, cmdType, cmdParams, GetDbName, cancellationToken);

		/// <summary>
		/// 查询一行
		/// </summary>
		/// <typeparam name="T">实体类型</typeparam>
		/// <param name="cmdText">sql语句</param>
		/// <param name="cmdType"></param>
		/// <param name="cmdParams">sql参数</param>
		/// <param name="type">数据库类型</param>
		/// <returns>实体</returns>
		public static T ExecuteDataReaderModel<T>(string cmdText, CommandType cmdType = CommandType.Text, DbParameter[] cmdParams = null)
			=> ExecuteDataReaderModel<T>(cmdText, cmdType, cmdParams, GetDbName);

		/// <summary>
		/// 查询一行
		/// </summary>
		/// <typeparam name="T">实体类型</typeparam>
		/// <param name="cmdText">sql语句</param>
		/// <param name="cmdType"></param>
		/// <param name="cmdParams">sql参数</param>
		/// <param name="type">数据库类型</param>
		/// <param name="cancellationToken"></param>
		/// <returns>实体</returns>
		public static Task<T> ExecuteDataReaderModelAsync<T>(string cmdText, CommandType cmdType = CommandType.Text, DbParameter[] cmdParams = null, CancellationToken cancellationToken = default)
			=> ExecuteDataReaderModelAsync<T>(cmdText, cmdType, cmdParams, GetDbName, cancellationToken);

		/// <summary>
		/// DataReader pipe
		/// </summary>
		/// <param name="builders">sql builder</param>
		/// <param name="cmdType">命令类型</param>
		/// <param name="type">数据库名称</param>
		/// <exception cref="ArgumentNullException">builders is null or empty</exception>
		/// <returns>实体</returns>
		public static object[] ExecuteDataReaderPipe(IEnumerable<ISqlBuilder> builders, CommandType cmdType = CommandType.Text)
			=> ExecuteDataReaderPipe(builders, cmdType, GetDbName);

		/// <summary>
		/// DataReader pipe
		/// </summary>
		/// <param name="builders">sql builder</param>
		/// <param name="cmdType">命令类型</param>
		/// <param name="type">数据库名称</param>
		/// <param name="cancellationToken"></param>
		/// <exception cref="ArgumentNullException">builders is null or empty</exception>
		/// <returns>实体</returns>
		public static Task<object[]> ExecuteDataReaderPipeAsync(IEnumerable<ISqlBuilder> builders, CommandType cmdType = CommandType.Text, CancellationToken cancellationToken = default)
			=> ExecuteDataReaderPipeAsync(builders, cmdType, GetDbName, cancellationToken);
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
		/// 实现Pgsql
		/// </summary>
		internal class PgsqlExecute : DbExecute
		{
			public PgsqlExecute(DbConnectionModel conn) : base(conn) { }
		}

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
				if (execute.Count == 0)
					if (type.EndsWith(BaseDbOption.SlaveSuffix))
						return GetExecute(type.Replace(BaseDbOption.SlaveSuffix, string.Empty));

				if (execute.Count == 1)
					return execute[0];

				if (execute.Count > 1)
					return execute[_ran.Next(0, execute.Count)];
			}
			// 从没有从库连接会查主库->如果没有连接会报错
			throw new ArgumentNullException("connectionstring", $"not exist {type} execute");
		}

		/// <summary>
		/// 初始化一主多从数据库连接
		/// </summary>
		/// <param name="options">数据库连接</param>
		/// <exception cref="ArgumentNullException"></exception>
		/// <exception cref="ArgumentOutOfRangeException"></exception>
		public static void InitDBConnectionOption(params BaseDbOption[] options)
		{
			if (options == null)
				throw new ArgumentNullException(nameof(options));
			if (options.Count() == 0)
				throw new ArgumentOutOfRangeException(nameof(options));
			foreach (var option in options)
			{
				if (string.IsNullOrEmpty(option.MasterConnectionString))
					throw new ArgumentNullException(nameof(option.MasterConnectionString), $"{option.TypeName} Connection String is null");
				var dbModel = new DbConnectionModel(option.MasterConnectionString, option.Logger, option.Type);
				dbModel.Options.MapAction = option.Options.MapAction;

				_executeDict[option.TypeName] = new List<DbExecute> { new PgsqlExecute(dbModel) };

				if ((option.SlaveConnectionStrings?.Length ?? 0) == 0) continue;

				_executeDict[option.TypeName + BaseDbOption.SlaveSuffix] = new List<DbExecute>();
				foreach (var item in option.SlaveConnectionStrings)
				{
					var dbModelSlave = new DbConnectionModel(item, option.Logger, option.Type);
					dbModelSlave.Options.MapAction = option.Options.MapAction;
					_executeDict[option.TypeName + BaseDbOption.SlaveSuffix].Add(new PgsqlExecute(dbModelSlave));
				}
			}
		}

		/// <summary>
		/// 查询单个元素
		/// </summary>
		/// <param name="cmdType">命令类型</param>
		/// <param name="cmdText">sql语句</param>
		/// <param name="cmdParams">sql参数</param>
		/// <param name="type">数据库类型</param>
		/// <returns>返回(0,0)值</returns>
		public static object ExecuteScalar(string cmdText, CommandType cmdType = CommandType.Text, DbParameter[] cmdParams = null, string type = "master")
			=> GetExecute(type).ExecuteScalar(cmdText, cmdType, cmdParams);

		/// <summary>
		/// 查询单个元素
		/// </summary>
		/// <param name="cmdType">命令类型</param>
		/// <param name="cmdText">sql语句</param>
		/// <param name="cmdParams">sql参数</param>
		/// <param name="type">数据库类型</param>
		/// <param name="cancellationToken"></param>
		/// <returns>返回(0,0)值</returns>
		public static Task<object> ExecuteScalarAsync(string cmdText, CommandType cmdType = CommandType.Text, DbParameter[] cmdParams = null, string type = "master", CancellationToken cancellationToken = default)
			=> cancellationToken.IsCancellationRequested ? Task.FromCanceled<object>(cancellationToken) : GetExecute(type).ExecuteScalarAsync(cmdText, cmdType, cmdParams, cancellationToken);

		/// <summary>
		/// 执行NonQuery
		/// </summary>
		/// <param name="cmdType">命令类型</param>
		/// <param name="cmdText">sql语句</param>
		/// <param name="cmdParams">sql参数</param>
		/// <param name="type">数据库类型</param>
		/// <returns>修改行数</returns>
		public static int ExecuteNonQuery(string cmdText, CommandType cmdType = CommandType.Text, DbParameter[] cmdParams = null, string type = "master")
			=> GetExecute(type).ExecuteNonQuery(cmdText, cmdType, cmdParams);

		/// <summary>
		/// 执行NonQuery
		/// </summary>
		/// <param name="cmdType">命令类型</param>
		/// <param name="cmdText">sql语句</param>
		/// <param name="cmdParams">sql参数</param>
		/// <param name="type">数据库类型</param>
		/// <param name="cancellationToken"></param>
		/// <returns>修改行数</returns>
		public static Task<int> ExecuteNonQueryAsync(string cmdText, CommandType cmdType = CommandType.Text, DbParameter[] cmdParams = null, string type = "master", CancellationToken cancellationToken = default)
			=> cancellationToken.IsCancellationRequested ? Task.FromCanceled<int>(cancellationToken) : GetExecute(type).ExecuteNonQueryAsync(cmdText, cmdType, cmdParams, cancellationToken);

		/// <summary>
		/// DataReader
		/// </summary>
		/// <param name="action">逐行Reader委托</param>
		/// <param name="cmdType">命令类型</param>
		/// <param name="cmdText">sql语句</param>
		/// <param name="cmdParams">sql参数</param>
		/// <param name="type">数据库类型</param>
		public static void ExecuteDataReader(Action<DbDataReader> action, string cmdText, CommandType cmdType = CommandType.Text, DbParameter[] cmdParams = null, string type = "master")
			=> GetExecute(type).ExecuteDataReader(action, cmdText, cmdType, cmdParams);

		/// <summary>
		/// DataReader
		/// </summary>
		/// <param name="action">逐行Reader委托</param>
		/// <param name="cmdType">命令类型</param>
		/// <param name="cmdText">sql语句</param>
		/// <param name="cmdParams">sql参数</param>
		/// <param name="type">数据库类型</param>
		/// <param name="cancellationToken"></param>
		public static Task ExecuteDataReaderAsync(Action<DbDataReader> action, string cmdText, CommandType cmdType = CommandType.Text, DbParameter[] cmdParams = null, string type = "master", CancellationToken cancellationToken = default)
			=> cancellationToken.IsCancellationRequested ? Task.FromCanceled(cancellationToken) : GetExecute(type).ExecuteDataReaderAsync(action, cmdText, cmdType, cmdParams, cancellationToken);

		/// <summary>
		/// 查询多行
		/// </summary>
		/// <typeparam name="T">列表类型</typeparam>
		/// <param name="cmdText">sql语句</param>
		/// <param name="cmdType"></param>
		/// <param name="cmdParams">sql参数</param>
		/// <param name="type">数据库类型</param>
		/// <returns>列表</returns>
		public static List<T> ExecuteDataReaderList<T>(string cmdText, CommandType cmdType = CommandType.Text, DbParameter[] cmdParams = null, string type = "master")
			=> ExecuteDataReaderListAsync<T>(cmdText, cmdType, cmdParams, type, false, CancellationToken.None).ConfigureAwait(false).GetAwaiter().GetResult();

		/// <summary>
		/// 查询多行
		/// </summary>
		/// <typeparam name="T">列表类型</typeparam>
		/// <param name="cmdText">sql语句</param>
		/// <param name="cmdType"></param>
		/// <param name="cmdParams">sql参数</param>
		/// <param name="type">数据库类型</param>
		/// <param name="cancellationToken"></param>
		/// <returns>列表</returns>
		public static Task<List<T>> ExecuteDataReaderListAsync<T>(string cmdText, CommandType cmdType = CommandType.Text, DbParameter[] cmdParams = null, string type = "master", CancellationToken cancellationToken = default)
			=> cancellationToken.IsCancellationRequested ? Task.FromCanceled<List<T>>(cancellationToken) : ExecuteDataReaderListAsync<T>(cmdText, cmdType, cmdParams, type, true, cancellationToken);

		/// <summary>
		/// 查询一行
		/// </summary>
		/// <typeparam name="T">实体类型</typeparam>
		/// <param name="cmdText">sql语句</param>
		/// <param name="cmdType"></param>
		/// <param name="cmdParams">sql参数</param>
		/// <param name="type">数据库类型</param>
		/// <returns>实体</returns>
		public static T ExecuteDataReaderModel<T>(string cmdText, CommandType cmdType = CommandType.Text, DbParameter[] cmdParams = null, string type = "master")
			=> ExecuteDataReaderModelAsync<T>(cmdText, cmdType, cmdParams, type, false, CancellationToken.None).ConfigureAwait(false).GetAwaiter().GetResult();

		/// <summary>
		/// 查询一行
		/// </summary>
		/// <typeparam name="T">实体类型</typeparam>
		/// <param name="cmdText">sql语句</param>
		/// <param name="cmdType"></param>
		/// <param name="cmdParams">sql参数</param>
		/// <param name="type">数据库类型</param>
		/// <param name="cancellationToken"></param>
		/// <returns>实体</returns>
		public static Task<T> ExecuteDataReaderModelAsync<T>(string cmdText, CommandType cmdType = CommandType.Text, DbParameter[] cmdParams = null, string type = "master", CancellationToken cancellationToken = default)
			=> cancellationToken.IsCancellationRequested ? Task.FromCanceled<T>(cancellationToken) : ExecuteDataReaderModelAsync<T>(cmdText, cmdType, cmdParams, type, true, cancellationToken);

		/// <summary>
		/// DataReader pipe
		/// </summary>
		/// <param name="builders">sql builder</param>
		/// <param name="cmdType">命令类型</param>
		/// <param name="type">数据库名称</param>
		/// <exception cref="ArgumentNullException">builders is null or empty</exception>
		/// <returns>实体</returns>
		public static object[] ExecuteDataReaderPipe(IEnumerable<ISqlBuilder> builders, CommandType cmdType = CommandType.Text, string type = "master")
			=> ExecuteDataReaderPipeAsync(builders, cmdType, type, false, CancellationToken.None).ConfigureAwait(false).GetAwaiter().GetResult();

		/// <summary>
		/// DataReader pipe
		/// </summary>
		/// <param name="builders">sql builder</param>
		/// <param name="cmdType">命令类型</param>
		/// <param name="type">数据库名称</param>
		/// <param name="cancellationToken"></param>
		/// <exception cref="ArgumentNullException">builders is null or empty</exception>
		/// <returns>实体</returns>
		public static Task<object[]> ExecuteDataReaderPipeAsync(IEnumerable<ISqlBuilder> builders, CommandType cmdType = CommandType.Text, string type = "master", CancellationToken cancellationToken = default)
			=> cancellationToken.IsCancellationRequested ? Task.FromCanceled<object[]>(cancellationToken) : ExecuteDataReaderPipeAsync(builders, cmdType, type, true, cancellationToken);

		static async Task<T> ExecuteDataReaderModelAsync<T>(string cmdText, CommandType cmdType, DbParameter[] cmdParams, string type, bool async, CancellationToken cancellationToken)
		{
			var list = await ExecuteDataReaderListAsync<T>(cmdText, cmdType, cmdParams, type, async, cancellationToken);
			return list.Count > 0 ? list[0] : default;
		}

		static async Task<List<T>> ExecuteDataReaderListAsync<T>(string cmdText, CommandType cmdType, DbParameter[] cmdParams, string type, bool async, CancellationToken cancellationToken)
		{
			var list = new List<T>();
			if (async)
				await ExecuteDataReaderAsync(dr =>
				{
					list.Add(dr.ReaderToModel<T>());
				}, cmdText, cmdType, cmdParams, type, cancellationToken);
			else
				ExecuteDataReader(dr =>
				{
					list.Add(dr.ReaderToModel<T>());
				}, cmdText, cmdType, cmdParams, type);
			return list;
		}

		/// <summary>
		/// DataReader pipe
		/// </summary>
		/// <param name="builders">sql builder</param>
		/// <param name="cmdType">命令类型</param>
		/// <param name="type">数据库名称</param>
		/// <param name="async"></param>
		/// <param name="cancellationToken"></param>
		/// <exception cref="ArgumentNullException">builders is null or empty</exception>
		/// <returns>实体</returns>
		static async Task<object[]> ExecuteDataReaderPipeAsync(IEnumerable<ISqlBuilder> builders, CommandType cmdType, string type, bool async, CancellationToken cancellationToken)
		{
			if (builders?.Any() != true)
				throw new ArgumentNullException(nameof(builders));

			object[] results = new object[builders.Count()];
			var paras = new List<DbParameter>();
			var cmdText = new StringBuilder();
			foreach (var item in builders)
			{
				paras.AddRange(item.Params);
				cmdText.Append(item.CommandText).AppendLine(";");
			}
			if (async)
				await GetExecute(type).ExecuteDataReaderBaseAsync(async dr =>
				{
					for (int i = 0; i < results.Length; i++)
					{
						var item = builders.ElementAt(i);
						List<object> list = new List<object>();
						while (await dr.ReadAsync(cancellationToken))
							list.Add(dr.ReaderToModel(item.Type));

						results[i] = GetResult(dr, item, list);

						await dr.NextResultAsync();
					}
				}, cmdText.ToString(), cmdType, paras.ToArray(), cancellationToken);
			else
				GetExecute(type).ExecuteDataReaderBase(dr =>
				{
					for (int i = 0; i < results.Length; i++)
					{
						var item = builders.ElementAt(i);
						List<object> list = new List<object>();
						while (dr.Read())
							list.Add(dr.ReaderToModel(item.Type));

						results[i] = GetResult(dr, item, list);

						dr.NextResult();
					}
				}, cmdText.ToString(), cmdType, paras.ToArray());

			static object GetResult(DbDataReader dr, ISqlBuilder item, List<object> list)
			{
				return item.ReturnType switch
				{
					var t when t == PipeReturnType.List =>
						list.ToArray(),
					var t when t == PipeReturnType.One =>
						list.Count > 0 ? list[0] : item.Type.IsTuple() ? Activator.CreateInstance(item.Type) : default, // 返回默认值
					var t when t == PipeReturnType.Rows =>
						dr.RecordsAffected,
					_ => throw new ArgumentException("ReturnType is wrong", nameof(item.ReturnType)),
				};
			}

			return results;
		}

		/// <summary>
		/// 事务 (暂不支持分布式事务)
		/// </summary>
		/// <param name="action">Action委托</param>
		/// <param name="type">数据库名称</param>
		/// <exception cref="ArgumentNullException">委托是null</exception>
		public static void Transaction(Action action, string type = "master")
			=> TransactionAsync(action, type, false, CancellationToken.None).ConfigureAwait(false).GetAwaiter().GetResult();

		/// <summary>
		/// 事务 (暂不支持分布式事务)
		/// </summary>
		/// <param name="action">Action委托</param>
		/// <param name="type">数据库名称</param>
		/// <param name="cancellationToken"></param>
		/// <exception cref="ArgumentNullException">委托是null</exception>
		public static Task TransactionAsync(Action action, string type = "master", CancellationToken cancellationToken = default)
			=> cancellationToken.IsCancellationRequested ? Task.FromCanceled(cancellationToken) : TransactionAsync(action, type, true, cancellationToken);

		static async Task TransactionAsync(Action action, string type, bool async, CancellationToken cancellationToken)
		{
			if (action == null)
				throw new ArgumentNullException(nameof(action));
			var exe = GetExecute(type);
			try
			{
				if (async) await exe.BeginTransactionAsync(cancellationToken);
				else exe.BeginTransaction();

				action?.Invoke();

				if (async) await exe.CommitTransactionAsync(cancellationToken);
				else exe.CommitTransaction();
			}
			catch (Exception e)
			{
				if (async) await exe.RollBackTransactionAsync(cancellationToken);
				else exe.RollBackTransaction();
				throw e;
			}
		}

		/// <summary>
		/// 事务 (暂不支持分布式事务)
		/// </summary>
		/// <remarks>func返回false, 则回滚事务</remarks>
		/// <param name="func">Func委托</param>
		/// <param name="type">数据库名称</param>
		/// <exception cref="ArgumentNullException">委托是null</exception>
		public static void Transaction(Func<bool> func, string type = "master")
		{
			if (func == null)
				throw new ArgumentNullException(nameof(func));
			try
			{
				GetExecute(type).BeginTransaction();
				if (func.Invoke())
					GetExecute(type).CommitTransaction();
				else
					GetExecute(type).RollBackTransaction();
			}
			catch (Exception e)
			{
				GetExecute(type).RollBackTransaction();
				throw e;
			}
		}
	}
}

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
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Meta.Common.DbHelper
{
	internal abstract class DbExecute
	{
		readonly DbConnectionModel _conn;
		/// <summary>
		/// 事务池
		/// </summary>
		readonly Dictionary<int, DbTransaction> _transPool = new Dictionary<int, DbTransaction>();
		/// <summary>
		/// constructer
		/// </summary>
		/// <param name="conn"></param>
		protected DbExecute(DbConnectionModel conn)
		{
			if (string.IsNullOrEmpty(conn.ConnectionString))
				throw new ArgumentNullException(nameof(conn.ConnectionString));
			_conn = conn;
		}

		/// <summary>
		/// 当前线程事务
		/// </summary>
		DbTransaction CurrentTransaction
		{
			get
			{
				int tid = Thread.CurrentThread.ManagedThreadId;
				if (_transPool.ContainsKey(tid) && _transPool[tid] != null)
					return _transPool[tid];
				return null;
			}
		}

		/// <summary>
		/// 返回一行数据
		/// </summary>
		internal object ExecuteScalar(string cmdText, CommandType cmdType, DbParameter[] cmdParams)
			=> ExecuteScalarAsync(cmdText, cmdType, cmdParams, false, CancellationToken.None).ConfigureAwait(false).GetAwaiter().GetResult();

		/// <summary>
		/// 返回一行数据
		/// </summary>
		internal Task<object> ExecuteScalarAsync(string cmdText, CommandType cmdType, DbParameter[] cmdParams, CancellationToken cancellationToken)
			=> cancellationToken.IsCancellationRequested ? Task.FromCanceled<object>(cancellationToken) : ExecuteScalarAsync(cmdText, cmdType, cmdParams, true, cancellationToken).AsTask();

		/// <summary>
		/// 执行sql语句
		/// </summary>
		internal int ExecuteNonQuery(string cmdText, CommandType cmdType, DbParameter[] cmdParams)
			=> ExecuteNonQueryAsync(cmdText, cmdType, cmdParams, false, CancellationToken.None).ConfigureAwait(false).GetAwaiter().GetResult();

		/// <summary>
		/// 执行sql语句
		/// </summary>
		internal Task<int> ExecuteNonQueryAsync(string cmdText, CommandType cmdType, DbParameter[] cmdParams, CancellationToken cancellationToken)
			=> cancellationToken.IsCancellationRequested ? Task.FromCanceled<int>(cancellationToken) : ExecuteNonQueryAsync(cmdText, cmdType, cmdParams, true, cancellationToken).AsTask();

		/// <summary>
		/// 读取数据库reader
		/// </summary>
		internal void ExecuteDataReader(Action<DbDataReader> action, string cmdText, CommandType cmdType, DbParameter[] cmdParams)
			=> ExecuteDataReaderBaseAsync(dr =>
			{
				while (dr.Read())
					action?.Invoke(dr);

			}, cmdText, cmdType, cmdParams, false, CancellationToken.None).ConfigureAwait(false).GetAwaiter().GetResult();

		/// <summary>
		/// 读取数据库reader
		/// </summary>
		internal Task ExecuteDataReaderAsync(Action<DbDataReader> action, string cmdText, CommandType cmdType, DbParameter[] cmdParams, CancellationToken cancellationToken)
			=> cancellationToken.IsCancellationRequested ? Task.FromCanceled(cancellationToken)
			: ExecuteDataReaderBaseAsync(async dr =>
			{
				while (await dr.ReadAsync(cancellationToken))
					action?.Invoke(dr);

			}, cmdText, cmdType, cmdParams, true, cancellationToken).AsTask();

		/// <summary>
		/// 读取数据库reader
		/// </summary>
		void ExecuteDataReaderBase(Action<DbDataReader> action, string cmdText, CommandType cmdType, DbParameter[] cmdParams)
		   => ExecuteDataReaderBaseAsync(action, cmdText, cmdType, cmdParams, false, CancellationToken.None).ConfigureAwait(false).GetAwaiter().GetResult();

		/// <summary>
		/// 读取数据库reader
		/// </summary>
		Task ExecuteDataReaderBaseAsync(Action<DbDataReader> action, string cmdText, CommandType cmdType, DbParameter[] cmdParams, CancellationToken cancellationToken)
		   => cancellationToken.IsCancellationRequested ? Task.FromCanceled(cancellationToken) : ExecuteDataReaderBaseAsync(action, cmdText, cmdType, cmdParams, true, cancellationToken).AsTask();

		async ValueTask ExecuteDataReaderBaseAsync(Action<DbDataReader> action, string cmdText, CommandType cmdType, DbParameter[] cmdParams, bool async, CancellationToken cancellationToken)
		{
			DbDataReader dr = null;
			DbCommand cmd = null;
			try
			{
				cmd = await PrepareCommandAsync(cmdText, cmdType, cmdParams, async, cancellationToken);
				dr = async ? await cmd.ExecuteReaderAsync(cancellationToken) : cmd.ExecuteReader();
				using (dr)
					action?.Invoke(dr);
			}
			catch (Exception ex)
			{
				ThrowException(cmd, ex);
				throw ex;
			}
			finally
			{
				await CloseCommandAsync(cmd, async);
				if (dr != null && !dr.IsClosed)
				{
					if (async)
						await dr.DisposeAsync();
					else
						dr.Dispose();
				}
			}
		}

		async ValueTask<int> ExecuteNonQueryAsync(string cmdText, CommandType cmdType, DbParameter[] cmdParams, bool async, CancellationToken cancellationToken)
		{
			int affrows = 0;
			DbCommand cmd = null;
			try
			{
				cmd = await PrepareCommandAsync(cmdText, cmdType, cmdParams, async, cancellationToken);
				affrows = async ? await cmd.ExecuteNonQueryAsync(cancellationToken) : cmd.ExecuteNonQuery();
			}
			catch (Exception ex)
			{
				ThrowException(cmd, ex);
				throw ex;
			}
			finally
			{
				await CloseCommandAsync(cmd, async);
			}
			return affrows;
		}

		async ValueTask<object> ExecuteScalarAsync(string cmdText, CommandType cmdType, DbParameter[] cmdParams, bool async, CancellationToken cancellationToken)
		{
			DbCommand cmd = null;
			object ret = null;
			try
			{
				cmd = await PrepareCommandAsync(cmdText, cmdType, cmdParams, async, cancellationToken);
				ret = async ? await cmd.ExecuteScalarAsync(cancellationToken) : cmd.ExecuteScalar();
			}
			catch (Exception ex)
			{
				ThrowException(cmd, ex);
				throw ex;
			}
			finally
			{
				await CloseCommandAsync(cmd, async);
			}
			return ret;
		}

		async ValueTask<DbCommand> PrepareCommandAsync(string cmdText, CommandType cmdType, DbParameter[] cmdParams, bool async, CancellationToken cancellationToken)
		{

			if (string.IsNullOrEmpty(cmdText))
				throw new ArgumentNullException(nameof(cmdText));
			DbCommand cmd;
			using (cancellationToken.Register(cmd => ((NpgsqlCommand)cmd!).Cancel(), this))
			{
				if (CurrentTransaction == null)
				{
					var conn = async ? await _conn.GetConnectionAsync(cancellationToken) : _conn.GetConnection();
					cmd = conn.CreateCommand();
				}
				else
				{
					cmd = CurrentTransaction.Connection.CreateCommand();
					cmd.Transaction = CurrentTransaction;
				}
				cmd.CommandText = cmdText;
				cmd.CommandType = cmdType;
				if (cmdParams?.Any() != true) return cmd;

				foreach (var p in cmdParams)
				{
					if (p == null) continue;
					if ((p.Direction == ParameterDirection.Input || p.Direction == ParameterDirection.InputOutput) && p.Value == null)
						p.Value = DBNull.Value;
					cmd.Parameters.Add(p);
				}
			}
			return cmd;
		}

		/// <summary>
		/// 查询一行(helper类包装)
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="cmdText"></param>
		/// <param name="cmdType"></param>
		/// <param name="cmdParams"></param>
		/// <param name="async"></param>
		/// <param name="cancellationToken"></param>
		/// <returns></returns>
		internal async Task<T> ExecuteDataReaderModelAsync<T>(string cmdText, CommandType cmdType, DbParameter[] cmdParams, bool async, CancellationToken cancellationToken)
		{
			var list = await ExecuteDataReaderListAsync<T>(cmdText, cmdType, cmdParams, async, cancellationToken);
			return list.Count > 0 ? list[0] : default;
		}

		/// <summary>
		/// 查询多行(helper类包装)
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="cmdText"></param>
		/// <param name="cmdType"></param>
		/// <param name="cmdParams"></param>
		/// <param name="async"></param>
		/// <param name="cancellationToken"></param>
		/// <returns></returns>
		internal async Task<List<T>> ExecuteDataReaderListAsync<T>(string cmdText, CommandType cmdType, DbParameter[] cmdParams, bool async, CancellationToken cancellationToken)
		{
			var list = new List<T>();
			if (async)
				await ExecuteDataReaderAsync(dr =>
				{
					list.Add(dr.ReaderToModel<T>());
				}, cmdText, cmdType, cmdParams, cancellationToken);
			else
				ExecuteDataReader(dr =>
				{
					list.Add(dr.ReaderToModel<T>());
				}, cmdText, cmdType, cmdParams);
			return list;
		}

		/// <summary>
		/// 事务(helper类包装)
		/// </summary>
		/// <param name="action"></param>
		/// <param name="async"></param>
		/// <param name="cancellationToken"></param>
		/// <returns></returns>
		internal async Task TransactionAsync(Action action, bool async, CancellationToken cancellationToken)
		{
			if (action == null)
				throw new ArgumentNullException(nameof(action));

			try
			{
				if (async) await BeginTransactionAsync(cancellationToken);
				else BeginTransaction();

				action?.Invoke();

				if (async) await CommitTransactionAsync(cancellationToken);
				else CommitTransaction();
			}
			catch (Exception e)
			{
				if (async) await RollBackTransactionAsync(cancellationToken);
				else RollBackTransaction();
				throw e;
			}
		}

		/// <summary>
		/// 管道(helper类包装)
		/// </summary>
		/// <param name="builders"></param>
		/// <param name="cmdType"></param>
		/// <param name="async"></param>
		/// <param name="cancellationToken"></param>
		/// <returns></returns>
		internal async Task<object[]> ExecuteDataReaderPipeAsync(IEnumerable<ISqlBuilder> builders, CommandType cmdType, bool async, CancellationToken cancellationToken)
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
				await ExecuteDataReaderBaseAsync(async dr =>
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
				ExecuteDataReaderBase(dr =>
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
		/// 抛出异常
		/// </summary>
		void ThrowException(DbCommand cmd, Exception ex)
		{
			ex.Data["ConnectionString"] = cmd?.Connection.ConnectionString;
			string str = string.Empty;
			if (cmd?.Parameters != null)
				foreach (DbParameter item in cmd.Parameters)
					str += $"{item.ParameterName}:{item.Value}\n";

			_conn.Logger.LogError(new EventId(111111), ex, "数据库执行出错：===== \n{0}\n{1}\nConnectionString:{2}", cmd?.CommandText, str, cmd?.Connection.ConnectionString);//输出日志

		}

		async Task CloseConnectionAsync(DbConnection connection, bool async)
		{
			if (connection != null && connection.State != ConnectionState.Closed)
			{
				if (async)
					await connection.DisposeAsync();
				else
					connection.Dispose();
			}
		}

		async Task CloseCommandAsync(DbCommand cmd, bool async)
		{
			if (cmd == null)
				return;
			if (cmd.Parameters != null)
				cmd.Parameters.Clear();

			if (CurrentTransaction == null)
				await CloseConnectionAsync(cmd.Connection, async);

			if (async)
				await cmd.DisposeAsync();
			else
				cmd.Dispose();
		}

		#region 事务
		/// <summary>
		/// 开启事务
		/// </summary>
		internal void BeginTransaction()
			=> BeginTransactionAsync(false, CancellationToken.None).ConfigureAwait(false).GetAwaiter().GetResult();

		/// <summary>
		/// 开启事务
		/// </summary>
		internal Task BeginTransactionAsync(CancellationToken cancellationToken)
			=> cancellationToken.IsCancellationRequested ? Task.FromCanceled(cancellationToken) : BeginTransactionAsync(true, cancellationToken).AsTask();

		async ValueTask BeginTransactionAsync(bool async, CancellationToken cancellationToken)
		{
			var tid = Thread.CurrentThread.ManagedThreadId;
			if (CurrentTransaction != null || _transPool.ContainsKey(tid))
				throw new Exception("this thread exists a transaction already");

			var conn = async ? await _conn.GetConnectionAsync(cancellationToken) : _conn.GetConnection();
			var tran = async ? await conn.BeginTransactionAsync(cancellationToken) : conn.BeginTransaction();
			_transPool[tid] = tran;
		}

		/// <summary>
		/// 确认事务
		/// </summary>
		internal void CommitTransaction()
			=> CommitTransactionAsync(false, CancellationToken.None).ConfigureAwait(false).GetAwaiter().GetResult();

		/// <summary>
		/// 确认事务
		/// </summary>
		internal Task CommitTransactionAsync(CancellationToken cancellationToken)
			=> cancellationToken.IsCancellationRequested ? Task.FromCanceled(cancellationToken) : CommitTransactionAsync(false, CancellationToken.None).AsTask();

		/// <summary>
		/// 回滚事务
		/// </summary>
		internal void RollBackTransaction()
			=> RollBackTransactionAsync(false, CancellationToken.None).ConfigureAwait(false).GetAwaiter().GetResult();
		/// <summary>
		/// 回滚事务
		/// </summary>
		internal Task RollBackTransactionAsync(CancellationToken cancellationToken)
			=> cancellationToken.IsCancellationRequested ? Task.FromCanceled(cancellationToken) : RollBackTransactionAsync(false, CancellationToken.None).AsTask();

		async ValueTask CommitTransactionAsync(bool async, CancellationToken cancellationToken)
		{
			var tran = GetTransaction();
			using var conn = tran?.Connection;
			if (async)
				await tran.CommitAsync(cancellationToken);
			else
				tran.Commit();
		}

		async ValueTask RollBackTransactionAsync(bool async, CancellationToken cancellationToken)
		{
			var tran = GetTransaction();
			using var conn = tran?.Connection;
			if (async)
				await tran.RollbackAsync(cancellationToken);
			else
				tran.Rollback();
		}

		private DbTransaction GetTransaction()
		{
			var tran = CurrentTransaction;
			if (tran == null)
			{
				var tid = Thread.CurrentThread.ManagedThreadId;
				_transPool.Remove(tid);
			}

			return tran;
		}
		#endregion

	}
}

using Meta.Common.Model;
using Microsoft.Extensions.Logging;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
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
		public object ExecuteScalar(string cmdText, CommandType cmdType, DbParameter[] cmdParams)
			=> ExecuteScalarAsync(cmdText, cmdType, cmdParams, false, CancellationToken.None).ConfigureAwait(false).GetAwaiter().GetResult();

		/// <summary>
		/// 返回一行数据
		/// </summary>
		public Task<object> ExecuteScalarAsync(string cmdText, CommandType cmdType, DbParameter[] cmdParams, CancellationToken cancellationToken)
			=> cancellationToken.IsCancellationRequested ? Task.FromCanceled<object>(cancellationToken) : ExecuteScalarAsync(cmdText, cmdType, cmdParams, true, cancellationToken).AsTask();

		/// <summary>
		/// 执行sql语句
		/// </summary>
		public int ExecuteNonQuery(string cmdText, CommandType cmdType, DbParameter[] cmdParams)
			=> ExecuteNonQueryAsync(cmdText, cmdType, cmdParams, false, CancellationToken.None).ConfigureAwait(false).GetAwaiter().GetResult();

		/// <summary>
		/// 执行sql语句
		/// </summary>
		public Task<int> ExecuteNonQueryAsync(string cmdText, CommandType cmdType, DbParameter[] cmdParams, CancellationToken cancellationToken)
			=> cancellationToken.IsCancellationRequested ? Task.FromCanceled<int>(cancellationToken) : ExecuteNonQueryAsync(cmdText, cmdType, cmdParams, true, cancellationToken).AsTask();

		/// <summary>
		/// 读取数据库reader
		/// </summary>
		public void ExecuteDataReader(Action<DbDataReader> action, string cmdText, CommandType cmdType, DbParameter[] cmdParams)
			=> ExecuteDataReaderBaseAsync(dr =>
			{
				while (dr.Read())
					action?.Invoke(dr);

			}, cmdText, cmdType, cmdParams, false, CancellationToken.None).ConfigureAwait(false).GetAwaiter().GetResult();

		/// <summary>
		/// 读取数据库reader
		/// </summary>
		public Task ExecuteDataReaderAsync(Action<DbDataReader> action, string cmdText, CommandType cmdType, DbParameter[] cmdParams, CancellationToken cancellationToken)
			=> cancellationToken.IsCancellationRequested ? Task.FromCanceled(cancellationToken)
			: ExecuteDataReaderBaseAsync(async dr =>
			{
				while (await dr.ReadAsync(cancellationToken))
					action?.Invoke(dr);

			}, cmdText, cmdType, cmdParams, true, cancellationToken).AsTask();

		/// <summary>
		/// 读取数据库reader
		/// </summary>
		public void ExecuteDataReaderBase(Action<DbDataReader> action, string cmdText, CommandType cmdType, DbParameter[] cmdParams)
			=> ExecuteDataReaderBaseAsync(action, cmdText, cmdType, cmdParams, false, CancellationToken.None).ConfigureAwait(false).GetAwaiter().GetResult();

		/// <summary>
		/// 读取数据库reader
		/// </summary>
		public Task ExecuteDataReaderBaseAsync(Action<DbDataReader> action, string cmdText, CommandType cmdType, DbParameter[] cmdParams, CancellationToken cancellationToken)
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
		/// 抛出异常
		/// </summary>
		public void ThrowException(DbCommand cmd, Exception ex)
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
		public void BeginTransaction()
			=> BeginTransactionAsync(false, CancellationToken.None).ConfigureAwait(false).GetAwaiter().GetResult();

		/// <summary>
		/// 开启事务
		/// </summary>
		public Task BeginTransactionAsync(CancellationToken cancellationToken)
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
		public void CommitTransaction()
			=> CommitTransactionAsync(false, CancellationToken.None).ConfigureAwait(false).GetAwaiter().GetResult();

		/// <summary>
		/// 确认事务
		/// </summary>
		public Task CommitTransactionAsync(CancellationToken cancellationToken)
			=> cancellationToken.IsCancellationRequested ? Task.FromCanceled(cancellationToken) : CommitTransactionAsync(false, CancellationToken.None).AsTask();

		/// <summary>
		/// 回滚事务
		/// </summary>
		public void RollBackTransaction()
			=> RollBackTransactionAsync(false, CancellationToken.None).ConfigureAwait(false).GetAwaiter().GetResult();
		/// <summary>
		/// 回滚事务
		/// </summary>
		public Task RollBackTransactionAsync(CancellationToken cancellationToken)
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

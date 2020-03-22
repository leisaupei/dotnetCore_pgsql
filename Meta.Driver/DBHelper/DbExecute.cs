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
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Meta.Driver.DbHelper
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
				if (_transPool.ContainsKey(tid))
				{
					if (_transPool[tid] != null && _transPool[tid].Connection != null)
						return _transPool[tid];
					_transPool.Remove(tid);
				}
				return null;
			}
		}

		/// <summary>
		/// 返回一行数据
		/// </summary>
		internal object ExecuteScalar(string cmdText, CommandType cmdType, DbParameter[] cmdParams)
			=> ExecuteScalarAsync(false, cmdText, cmdType, cmdParams, CancellationToken.None).ConfigureAwait(false).GetAwaiter().GetResult();

		/// <summary>
		/// 返回一行数据
		/// </summary>
		internal ValueTask<object> ExecuteScalarAsync(string cmdText, CommandType cmdType, DbParameter[] cmdParams, CancellationToken cancellationToken)
			=> cancellationToken.IsCancellationRequested
			? new ValueTask<object>(Task.FromCanceled<object>(cancellationToken))
			: ExecuteScalarAsync(true, cmdText, cmdType, cmdParams, cancellationToken);

		/// <summary>
		/// 返回一行数据
		/// </summary>
		internal T ExecuteScalar<T>(string cmdText, CommandType cmdType, DbParameter[] cmdParams)
			=> ExecuteScalarAsync<T>(false, cmdText, cmdType, cmdParams, CancellationToken.None).ConfigureAwait(false).GetAwaiter().GetResult();

		/// <summary>
		/// 返回一行数据
		/// </summary>
		internal ValueTask<T> ExecuteScalarAsync<T>(string cmdText, CommandType cmdType, DbParameter[] cmdParams, CancellationToken cancellationToken)
			=> cancellationToken.IsCancellationRequested
			? new ValueTask<T>(Task.FromCanceled<T>(cancellationToken))
			: ExecuteScalarAsync<T>(true, cmdText, cmdType, cmdParams, cancellationToken);

		/// <summary>
		/// 执行sql语句
		/// </summary>
		internal int ExecuteNonQuery(string cmdText, CommandType cmdType, DbParameter[] cmdParams)
			=> ExecuteNonQueryAsync(false, cmdText, cmdType, cmdParams, CancellationToken.None).ConfigureAwait(false).GetAwaiter().GetResult();

		/// <summary>
		/// 执行sql语句
		/// </summary>
		internal ValueTask<int> ExecuteNonQueryAsync(string cmdText, CommandType cmdType, DbParameter[] cmdParams, CancellationToken cancellationToken)
			=> cancellationToken.IsCancellationRequested
			? new ValueTask<int>(Task.FromCanceled<int>(cancellationToken))
			: ExecuteNonQueryAsync(true, cmdText, cmdType, cmdParams, cancellationToken);

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
			=> cancellationToken.IsCancellationRequested
			? Task.FromCanceled(cancellationToken)
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
				cmd = await PrepareCommandAsync(async, cmdText, cmdType, cmdParams, cancellationToken);
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
				await CloseCommandAsync(async, cmd);
				if (dr != null && !dr.IsClosed)
				{
					if (async)
						await dr.DisposeAsync();
					else
						dr.Dispose();
				}
			}
		}

		async ValueTask<int> ExecuteNonQueryAsync(bool async, string cmdText, CommandType cmdType, DbParameter[] cmdParams, CancellationToken cancellationToken)
		{
			int affrows = 0;
			DbCommand cmd = null;
			try
			{
				cmd = await PrepareCommandAsync(async, cmdText, cmdType, cmdParams, cancellationToken);
				affrows = async ? await cmd.ExecuteNonQueryAsync(cancellationToken) : cmd.ExecuteNonQuery();
			}
			catch (Exception ex)
			{
				ThrowException(cmd, ex);
				throw ex;
			}
			finally
			{
				await CloseCommandAsync(async, cmd);
			}
			return affrows;
		}

		async ValueTask<object> ExecuteScalarAsync(bool async, string cmdText, CommandType cmdType, DbParameter[] cmdParams, CancellationToken cancellationToken)
		{
			DbCommand cmd = null;
			object ret = null;
			try
			{
				cmd = await PrepareCommandAsync(async, cmdText, cmdType, cmdParams, cancellationToken);
				ret = async ? await cmd.ExecuteScalarAsync(cancellationToken) : cmd.ExecuteScalar();
			}
			catch (Exception ex)
			{
				ThrowException(cmd, ex);
				throw ex;
			}
			finally
			{
				await CloseCommandAsync(async, cmd);
			}
			return ret;
		}

		async ValueTask<T> ExecuteScalarAsync<T>(bool async, string cmdText, CommandType cmdType, DbParameter[] cmdParams, CancellationToken cancellationToken)
		{
			var value = async
				? await ExecuteScalarAsync(cmdText, cmdType, cmdParams, cancellationToken)
				: ExecuteScalar(cmdText, cmdType, cmdParams);
			return value == null ? default : (T)Convert.ChangeType(value, typeof(T).GetOriginalType());
		}

		async ValueTask<DbCommand> PrepareCommandAsync(bool async, string cmdText, CommandType cmdType, DbParameter[] cmdParams, CancellationToken cancellationToken)
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

		internal async Task<T> ExecuteDataReaderModelAsync<T>(bool async, string cmdText, CommandType cmdType, DbParameter[] cmdParams, CancellationToken cancellationToken)
		{
			var list = await ExecuteDataReaderListAsync<T>(async, cmdText, cmdType, cmdParams, cancellationToken);
			return list.Count > 0 ? list[0] : default;
		}

		internal async Task<List<T>> ExecuteDataReaderListAsync<T>(bool async, string cmdText, CommandType cmdType, DbParameter[] cmdParams, CancellationToken cancellationToken)
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

		internal async ValueTask TransactionAsync(bool async, Action action, CancellationToken cancellationToken)
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

		internal async ValueTask<object[]> ExecuteDataReaderPipeAsync(bool async, IEnumerable<ISqlBuilder> builders, CommandType cmdType, CancellationToken cancellationToken)
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

		async Task CloseConnectionAsync(bool async, DbConnection connection)
		{
			if (connection != null && connection.State != ConnectionState.Closed)
			{
				if (async)
					await connection.DisposeAsync();
				else
					connection.Dispose();
			}
		}

		async Task CloseCommandAsync(bool async, DbCommand cmd)
		{
			if (cmd == null)
				return;
			if (cmd.Parameters != null)
				cmd.Parameters.Clear();

			if (CurrentTransaction == null)
				await CloseConnectionAsync(async, cmd.Connection);

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
			=> cancellationToken.IsCancellationRequested ? Task.FromCanceled(cancellationToken) : CommitTransactionAsync(true, cancellationToken).AsTask();

		/// <summary>
		/// 回滚事务
		/// </summary>
		internal void RollBackTransaction()
			=> RollBackTransactionAsync(false, CancellationToken.None).ConfigureAwait(false).GetAwaiter().GetResult();
		/// <summary>
		/// 回滚事务
		/// </summary>
		internal Task RollBackTransactionAsync(CancellationToken cancellationToken)
			=> cancellationToken.IsCancellationRequested ? Task.FromCanceled(cancellationToken) : RollBackTransactionAsync(true, cancellationToken).AsTask();

		async ValueTask CommitTransactionAsync(bool async, CancellationToken cancellationToken)
		{
			using var tran = GetTransaction();
			using var conn = tran?.Connection;
			if (async)
				await tran.CommitAsync(cancellationToken);
			else
				tran.Commit();
			RemoveCurrentTransaction();
		}

		async ValueTask RollBackTransactionAsync(bool async, CancellationToken cancellationToken)
		{
			using var tran = GetTransaction();
			using var conn = tran?.Connection;
			if (async)
				await tran.RollbackAsync(cancellationToken);
			else
				tran.Rollback();
			RemoveCurrentTransaction();
		}

		DbTransaction GetTransaction()
		{
			var tran = CurrentTransaction;
			if (tran == null)
			{
				RemoveCurrentTransaction();
				tran = null;
			}
			return tran;
		}

		void RemoveCurrentTransaction()
		{
			var tid = Thread.CurrentThread.ManagedThreadId;
			_transPool.Remove(tid);
		}
		#endregion

	}
}

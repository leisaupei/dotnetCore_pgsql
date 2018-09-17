using System;
using System.Collections.Generic;
using System.Data;
using Npgsql;
using Microsoft.Extensions.Logging;
using System.Threading;

namespace DBHelper
{
	public abstract class PgExecute
	{

		public static ILogger _logger;
		Dictionary<int, NpgsqlTransaction> _transPool = new Dictionary<int, NpgsqlTransaction>();
		static readonly object _lockTrans = new object();
		ConnectionPool _pool;
		/// <summary>
		/// 只读连接池
		/// </summary>
		ConnectionPool _slavePool;
		readonly bool _hasSlave = false;
		bool _isNonQuery = false;
		protected PgExecute(int poolSize, string connectionString, ILogger logger, int? slavePoolSize, string slaveConnectionString)
		{
			_logger = logger;
			_pool = new ConnectionPool(poolSize, connectionString);
			if (slavePoolSize.HasValue && slaveConnectionString.IsNotNullOrEmpty())
			{
				_hasSlave = true;
				_slavePool = new ConnectionPool(slavePoolSize.Value, slaveConnectionString);
			}
		}
		NpgsqlTransaction CurrentTransaction
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
		/// Prepare command before execute.
		/// </summary>
		protected void PrepareCommand(NpgsqlCommand cmd, CommandType cmdType, string cmdText, NpgsqlParameter[] cmdParams)
		{

			if (cmdText.IsNullOrEmpty() || cmd == null) throw new ArgumentNullException("Command is error");
			if (CurrentTransaction == null)
			{
				if (_hasSlave && !_isNonQuery)
				{
					if (_slavePool == null) throw new ArgumentException("Slave connection pool is null");
					cmd.Connection = _slavePool.GetConnection();
				}
				else
				{
					if (_pool == null) throw new ArgumentException("Connection pool is null");
					cmd.Connection = _pool.GetConnection();
				}
			}
			else
			{
				cmd.Connection = CurrentTransaction.Connection;
				cmd.Transaction = CurrentTransaction;
			}
			cmd.CommandText = cmdText;
			cmd.CommandType = cmdType;
			if (cmdParams != null)
			{
				foreach (var p in cmdParams)
				{
					if (p == null) continue;
					if ((p.Direction == ParameterDirection.Input || p.Direction == ParameterDirection.InputOutput) && p.Value == null)
						p.Value = DBNull.Value;
					cmd.Parameters.Add(p);
				}
			}
		}
		/// <summary>
		/// Return scalar.
		/// </summary>
		public object ExecuteScalar(CommandType cmdType, string cmdText, params NpgsqlParameter[] cmdParameters)
		{
			object ret = null;
			NpgsqlCommand cmd = new NpgsqlCommand();
			try
			{
				PrepareCommand(cmd, cmdType, cmdText, cmdParameters);
				ret = cmd.ExecuteScalar();

			}
			catch (Exception ex)
			{
				ThrowException(cmd, ex);
				throw ex;
			}
			finally
			{
				if (CurrentTransaction == null)
					Close(cmd, cmd.Connection);
			}
			return ret;
		}
		/// <summary>
		/// Execute non query.
		/// </summary>
		public int ExecuteNonQuery(CommandType cmdType, string cmdText, params NpgsqlParameter[] cmdParameters)
		{
			int ret = 0;
			NpgsqlCommand cmd = new NpgsqlCommand();
			try
			{
				PrepareCommand(cmd, cmdType, cmdText, cmdParameters);
				ret = cmd.ExecuteNonQuery();
			}
			catch (Exception ex)
			{
				ThrowException(cmd, ex);
				throw ex;
			}
			finally
			{
				if (CurrentTransaction == null)
					Close(cmd, cmd.Connection);
			}
			return ret;
		}
		/// <summary>
		/// Execute data reader with action.
		/// </summary>
		public void ExecuteDataReader(Action<NpgsqlDataReader> action, CommandType cmdType, string cmdText, params NpgsqlParameter[] cmdParameters)
		{
			_isNonQuery = true;
			NpgsqlCommand cmd = new NpgsqlCommand(); NpgsqlDataReader reader = null;
			try
			{
				PrepareCommand(cmd, cmdType, cmdText, cmdParameters);
				using (reader = cmd.ExecuteReader())
					while (reader.Read())
						action?.Invoke(reader);
			}
			catch (Exception ex)
			{
				ThrowException(cmd, ex);
				throw ex;
			}
			finally
			{
				if (CurrentTransaction == null)
					Close(cmd, cmd.Connection);
				if (reader != null && !reader.IsClosed)
					reader.Close();
			}
		}
		/// <summary>
		/// Throw exception.
		/// </summary>
		protected void ThrowException(NpgsqlCommand cmd, Exception ex)
		{
			string str = string.Empty;
			if (cmd.Parameters != null)
				foreach (NpgsqlParameter item in cmd.Parameters)
					str += $"{item.ParameterName}:{item.Value}\n";
			//done: export error.
			_logger.LogError(new EventId(111111), ex, "数据库执行出错：===== \n {0}\n{1}\n{2}", cmd.CommandText, cmd.Parameters, str);//输出日志

		}
		/// <summary>
		/// Close cmd and current connection
		/// </summary>
		public void Close(NpgsqlCommand cmd, NpgsqlConnection connection)
		{
			if (cmd != null)
			{
				if (cmd.Parameters != null)
					cmd.Parameters.Clear();
				cmd.Dispose();
			}
			if (_hasSlave && !_isNonQuery)
				_slavePool.ReleaseConnection(connection);
			else
				_pool.ReleaseConnection(connection);
		}
		#region Transaction
		/// <summary>
		/// Open transaction.
		/// </summary>
		public void BeginTransaction()
		{
			var tid = Thread.CurrentThread.ManagedThreadId;
			if (CurrentTransaction != null || _transPool.ContainsKey(tid))
				CommitTransaction();
			var tran = _pool.GetConnection().BeginTransaction();
			lock (_lockTrans)
				_transPool.Add(tid, tran);
		}
		/// <summary>
		/// Commit transaction.
		/// </summary>
		public void CommitTransaction() => ReleaseTransaction(tran => tran.Commit());

		/// <summary>
		/// Rollback transaction.
		/// </summary>
		public void RollBackTransaction() => ReleaseTransaction(tran => tran.Rollback());

		void ReleaseTransaction(Action<NpgsqlTransaction> action)
		{
			var tran = CurrentTransaction;
			if (tran == null || tran.Connection == null || tran?.IsCompleted == true) return;
			var tid = Thread.CurrentThread.ManagedThreadId;
			lock (_lockTrans)
				_transPool.Remove(tid);
			var conn = tran.Connection;
			action?.Invoke(tran);
			tran.Dispose();
			_pool.ReleaseConnection(conn);
		}
		#endregion

	}
}

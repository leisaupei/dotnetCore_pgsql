using System;
using System.Collections.Generic;
using System.Data;
using Npgsql;
using Microsoft.Extensions.Logging;
using System.Threading;
using System.Text.RegularExpressions;

namespace DBHelper
{
	public abstract class PgExecute
	{
		public static ILogger _logger;
		/// <summary>
		/// 事务池
		/// </summary>
		Dictionary<int, NpgsqlTransaction> _transPool = new Dictionary<int, NpgsqlTransaction>();
		/// <summary>
		/// 事务线程锁
		/// </summary>
		static readonly object _lockTrans = new object();
		/// <summary>
		/// 连接池
		/// </summary>
		ConnectionPool _pool;
		/// <summary>
		/// constructer
		/// </summary>
		/// <param name="poolSize"></param>
		/// <param name="connectionString"></param>
		/// <param name="logger"></param>
		protected PgExecute(string connectionString, ILogger logger)
		{
			_logger = logger;
			var poolSize = ConnectionPool.GetConnectionPoolSize(connectionString);
			_pool = new ConnectionPool(poolSize, connectionString);
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
		/// 执行命令前准备
		/// </summary>
		protected void PrepareCommand(NpgsqlCommand cmd, CommandType cmdType, string cmdText, NpgsqlParameter[] cmdParams)
		{
			if (_pool == null) throw new ArgumentException("Connection pool is null");
			if (cmdText.IsNullOrEmpty() || cmd == null) throw new ArgumentNullException("Command is error");
			if (CurrentTransaction == null)
				cmd.Connection = _pool.GetConnection();
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
		/// 返回一行数据
		/// </summary>
		public object ExecuteScalar(CommandType cmdType, string cmdText, params NpgsqlParameter[] cmdParams)
		{
			object ret = null;
			NpgsqlCommand cmd = new NpgsqlCommand();
			try
			{
				PrepareCommand(cmd, cmdType, cmdText, cmdParams);
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
		/// 执行sql语句
		/// </summary>
		public int ExecuteNonQuery(CommandType cmdType, string cmdText, params NpgsqlParameter[] cmdParams)
		{
			int ret = 0;
			NpgsqlCommand cmd = new NpgsqlCommand();
			try
			{
				PrepareCommand(cmd, cmdType, cmdText, cmdParams);
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
		/// 重构读取数据库数据
		/// </summary>
		public void ExecuteDataReader(Action<NpgsqlDataReader> action, CommandType cmdType, string cmdText, params NpgsqlParameter[] cmdParams)
		{
			NpgsqlCommand cmd = new NpgsqlCommand(); NpgsqlDataReader reader = null;
			try
			{
				PrepareCommand(cmd, cmdType, cmdText, cmdParams);
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
		/// 抛出异常
		/// </summary>
		public void ThrowException(NpgsqlCommand cmd, Exception ex)
		{
			string str = string.Empty;
			if (cmd?.Parameters != null)
				foreach (NpgsqlParameter item in cmd.Parameters)
					str += $"{item.ParameterName}:{item.Value}\n";
			//done: 输出错误日志
			_logger.LogError(new EventId(111111), ex, "数据库执行出错：===== \n {0}\n{1}\n{2}", cmd.CommandText, cmd.Parameters, str);//输出日志

		}
		/// <summary>
		/// 关闭命令及连接
		/// </summary>
		public void Close(NpgsqlCommand cmd, NpgsqlConnection connection)
		{
			if (cmd != null)
			{
				if (cmd.Parameters != null)
					cmd.Parameters.Clear();
				cmd.Dispose();
			}
			_pool.ReleaseConnection(connection);
		}
		#region 事务
		/// <summary>
		/// 开启事务
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
		/// 确认事务
		/// </summary>
		public void CommitTransaction() => ReleaseTransaction(tran => tran.Commit());

		/// <summary>
		/// 回滚事务
		/// </summary>
		public void RollBackTransaction() => ReleaseTransaction(tran => tran.Rollback());

		/// <summary>
		/// 释放事务
		/// </summary>
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

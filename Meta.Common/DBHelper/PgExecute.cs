using Microsoft.Extensions.Logging;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Threading;

namespace Meta.Common.DBHelper
{
	public abstract class PgExecute
	{
		/// <summary>
		/// 连接字符串
		/// </summary>
		readonly string _connectionString;
		/// <summary>
		/// logging日志
		/// </summary>
		readonly ILogger _logger;
		/// <summary>
		/// npgsql CLR 映射
		/// </summary>
		readonly Action<NpgsqlConnection> _mapAction;
		/// <summary>
		/// 事务池
		/// </summary>
		readonly Dictionary<int, NpgsqlTransaction> _transPool = new Dictionary<int, NpgsqlTransaction>();
		/// <summary>
		/// 事务线程锁
		/// </summary>
		static readonly object _lockTrans = new object();
		/// <summary>
		/// constructer
		/// </summary>
		/// <param name="poolSize"></param>
		/// <param name="connectionString"></param>
		/// <param name="logger"></param>
		protected PgExecute(string connectionString, ILogger logger, Action<NpgsqlConnection> mapAction = null)
		{
			_connectionString = connectionString;
			_logger = logger;
			_mapAction = mapAction;
		}

		/// <summary>
		/// 当前线程事务
		/// </summary>
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
		protected NpgsqlCommand PrepareCommand(CommandType cmdType, string cmdText, DbParameter[] cmdParams)
		{
			if (string.IsNullOrEmpty(cmdText))
				throw new ArgumentNullException("Command is error");
			NpgsqlCommand cmd;
			if (CurrentTransaction == null)
			{
				cmd = CreateConnection.CreateCommand();
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
			return cmd;
		}
		/// <summary>
		/// 返回一行数据
		/// </summary>
		public object ExecuteScalar(CommandType cmdType, string cmdText, NpgsqlParameter[] cmdParams)
		{
			NpgsqlCommand cmd = null;
			object ret = null;
			try
			{
				cmd = PrepareCommand(cmdType, cmdText, cmdParams);
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
					CloseCommand(cmd);
			}
			return ret;
		}
		/// <summary>
		/// 执行sql语句
		/// </summary>
		public int ExecuteNonQuery(CommandType cmdType, string cmdText, NpgsqlParameter[] cmdParams)
		{
			int ret = 0;
			NpgsqlCommand cmd = null;
			try
			{
				cmd = PrepareCommand(cmdType, cmdText, cmdParams);
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
					CloseCommand(cmd);
			}
			return ret;
		}
		/// <summary>
		/// 读取数据库reader
		/// </summary>
		public void ExecuteDataReader(Action<NpgsqlDataReader> action, CommandType cmdType, string cmdText, NpgsqlParameter[] cmdParams)
		{
			ExecuteDataReaderBase(dr =>
			{
				while (dr.Read())
					action?.Invoke(dr);

			}, cmdType, cmdText, cmdParams);
		}
		/// <summary>
		/// 读取数据库reader
		/// </summary>
		public void ExecuteDataReaderBase(Action<NpgsqlDataReader> action, CommandType cmdType, string cmdText, NpgsqlParameter[] cmdParams)
		{

			NpgsqlDataReader dr = null;
			NpgsqlCommand cmd = null;
			try
			{
				cmd = PrepareCommand(cmdType, cmdText, cmdParams);
				using (dr = cmd.ExecuteReader())
					action?.Invoke(dr);
			}
			catch (Exception ex)
			{
				ThrowException(cmd, ex);
				throw ex;
			}
			finally
			{
				if (CurrentTransaction == null)
					CloseCommand(cmd);
				if (dr != null && !dr.IsClosed)
					dr.Close();
			}
		}
		/// <summary>
		/// 抛出异常
		/// </summary>
		public void ThrowException(NpgsqlCommand cmd, Exception ex)
		{
			ex.Data["ConnectionString"] = cmd?.Connection.ConnectionString;
			string str = string.Empty;
			if (cmd?.Parameters != null)
				foreach (NpgsqlParameter item in cmd.Parameters)
					str += $"{item.ParameterName}:{item.Value}\n";

			_logger.LogError(new EventId(111111), ex, "数据库执行出错：===== \n{0}\n{1}\nConnectionString:{2}", cmd?.CommandText, str, cmd?.Connection.ConnectionString);//输出日志

		}
		/// <summary>
		/// 创建连接
		/// </summary>
		/// <returns></returns>
		NpgsqlConnection CreateConnection
		{
			get
			{
				if (string.IsNullOrEmpty(_connectionString))
					throw new ArgumentNullException(nameof(_connectionString));
				var conn = new NpgsqlConnection(_connectionString);
				OpenConnection(conn);
				return conn;
			}
		}

		/// <summary>
		/// 打开连接
		/// </summary>
		/// <param name="conn"></param>
		void OpenConnection(NpgsqlConnection conn)
		{
			ConnectionNullCheck(conn);

			if (conn.State == ConnectionState.Broken)
				conn.Close();
			if (conn.State != ConnectionState.Open)
			{
				conn.Open();
				_mapAction?.Invoke(conn);
			}
		}
		/// <summary>
		/// 检查连接是否为空
		/// </summary>
		/// <param name="conn"></param>
		void ConnectionNullCheck(NpgsqlConnection conn)
		{
			if (conn == null)
				throw new ArgumentNullException(nameof(conn));
		}
		/// <summary>
		/// 关闭连接
		/// </summary>
		/// <param name="conn"></param>
		void CloseConnection(NpgsqlConnection conn)
		{
			if (conn == null) return;
			if (conn.State != ConnectionState.Closed)
				conn.Close();
		}
		/// <summary>
		/// 关闭命令及连接
		/// </summary>
		void CloseCommand(NpgsqlCommand cmd)
		{
			if (cmd == null) return;
			if (cmd.Parameters != null)
				cmd.Parameters.Clear();
			cmd.Dispose();
			CloseConnection(cmd.Connection);
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
			var tran = CreateConnection.BeginTransaction();
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
			if (tran == null || tran.Connection == null) return;
			var tid = Thread.CurrentThread.ManagedThreadId;
			lock (_lockTrans)
				_transPool.Remove(tid);
			action?.Invoke(tran);
			CloseConnection(tran.Connection);
			tran.Dispose();
		}
		#endregion

	}
}

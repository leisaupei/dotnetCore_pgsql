using Meta.Common.Model;
using Microsoft.Extensions.Logging;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Threading;

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
		/// 执行命令前准备
		/// </summary>
		protected DbCommand PrepareCommand(string cmdText, CommandType cmdType, DbParameter[] cmdParams)
		{
			if (string.IsNullOrEmpty(cmdText))
				throw new ArgumentNullException(nameof(cmdText));
			DbCommand cmd;
			if (CurrentTransaction == null)
			{
				cmd = _conn.GetConnection.CreateCommand();
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
		public object ExecuteScalar(string cmdText, CommandType cmdType, DbParameter[] cmdParams)
		{
			DbCommand cmd = null;
			object ret = null;
			try
			{
				cmd = PrepareCommand(cmdText, cmdType, cmdParams);
				ret = cmd.ExecuteScalar();
			}
			catch (Exception ex)
			{
				ThrowException(cmd, ex);
				throw ex;
			}
			finally
			{
				CloseCommand(cmd);
			}
			return ret;
		}
		/// <summary>
		/// 执行sql语句
		/// </summary>
		public int ExecuteNonQuery(string cmdText, CommandType cmdType, DbParameter[] cmdParams)
		{
			int affrows = 0;
			DbCommand cmd = null;
			try
			{
				cmd = PrepareCommand(cmdText, cmdType, cmdParams);
				affrows = cmd.ExecuteNonQuery();
			}
			catch (Exception ex)
			{
				ThrowException(cmd, ex);
				throw ex;
			}
			finally
			{
				CloseCommand(cmd);
			}
			return affrows;
		}
		/// <summary>
		/// 读取数据库reader
		/// </summary>
		public void ExecuteDataReader(Action<DbDataReader> action, string cmdText, CommandType cmdType, DbParameter[] cmdParams)
		{
			ExecuteDataReaderBase(dr =>
			{
				while (dr.Read())
					action?.Invoke(dr);

			}, cmdText, cmdType, cmdParams);
		}
		/// <summary>
		/// 读取数据库reader
		/// </summary>
		public void ExecuteDataReaderBase(Action<DbDataReader> action, string cmdText, CommandType cmdType, DbParameter[] cmdParams)
		{

			DbDataReader dr = null;
			DbCommand cmd = null;
			try
			{
				cmd = PrepareCommand(cmdText, cmdType, cmdParams);
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
				CloseCommand(cmd);
				if (dr != null && !dr.IsClosed)
					dr.Close();
			}
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

		/// <summary>
		/// 关闭连接
		/// </summary>
		/// <param name="connection"></param>
		void CloseConnection(DbConnection connection)
		{
			if (connection == null) return;
			if (connection.State != ConnectionState.Closed)
				connection.Dispose();
		}
		/// <summary>
		/// 检查连接是否为空
		/// </summary>
		/// <param name="connection"></param>
		void ConnectionNullCheck(DbConnection connection)
		{
			if (connection == null)
				throw new ArgumentNullException(nameof(connection));
		}
		/// <summary>
		/// 打开连接
		/// </summary>
		/// <param name="connection"></param>
		void OpenConnection(DbConnection connection)
		{
			ConnectionNullCheck(connection);

			if (connection.State == ConnectionState.Broken)
				connection.Close();
			if (connection.State != ConnectionState.Open)
				connection.Open();
		}
		/// <summary>
		/// 关闭命令及连接
		/// </summary>
		void CloseCommand(DbCommand cmd)
		{
			if (cmd == null) return;
			if (cmd.Parameters != null)
				cmd.Parameters.Clear();
			if (CurrentTransaction == null)
				CloseConnection(cmd.Connection);
			cmd.Dispose();
		}

		#region 事务
		/// <summary>
		/// 开启事务
		/// </summary>
		public void BeginTransaction()
		{
			var tid = Thread.CurrentThread.ManagedThreadId;
			if (CurrentTransaction != null || _transPool.ContainsKey(tid))
				throw new Exception("this thread exists a transaction already");
			var tran = _conn.GetConnection.BeginTransaction();
			_transPool[tid] = tran;
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
		void ReleaseTransaction(Action<DbTransaction> action)
		{
			var tran = CurrentTransaction;
			if (tran == null) return;
			var tid = Thread.CurrentThread.ManagedThreadId;
			_transPool.Remove(tid);
			using var conn = tran.Connection;
			action.Invoke(tran);
		}
		#endregion

	}
}

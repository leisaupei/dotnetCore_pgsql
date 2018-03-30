using System;
using System.Collections.Generic;
using System.Data;
using Npgsql;
using Microsoft.Extensions.Logging;
namespace DBHelper
{
	public abstract class PgExecute : DBConnection
	{
		public static ILogger _logger = null;
		private NpgsqlTransaction _transaction = null;
		public PgExecute(ILogger logger)
		{
			_logger = logger;
		}
		public PgExecute() { }

		/// <summary>
		/// 执行命令前准备
		/// </summary>
		protected void PrepareCommand(NpgsqlCommand command, CommandType commandType, string commandText, NpgsqlParameter[] commandParameters)
		{
			if (commandText == null || commandText.Length == 0 || command == null) throw new ArgumentNullException("commandText error");
			if (_connection == null)
				_connection = GetConnection();
			command.Connection = _connection;
			command.CommandText = commandText;
			command.CommandType = commandType;
			if (commandParameters != null)
			{
				foreach (var p in commandParameters)
				{
					if (p == null) continue;
					if ((p.Direction == ParameterDirection.Input || p.Direction == ParameterDirection.InputOutput) && p.Value == null)
						p.Value = DBNull.Value;
					command.Parameters.Add(p);
				}
			}
		}
		/// <summary>
		/// 返回一行数据
		/// </summary>
		public object ExecuteScalar(CommandType commandType, string commandText, params NpgsqlParameter[] commandParameters)
		{
			object ret = null;
			NpgsqlCommand cmd = new NpgsqlCommand();
			try
			{
				PrepareCommand(cmd, commandType, commandText, commandParameters);
				OpenConnection(cmd.Connection);
				ret = cmd.ExecuteScalar();

			}
			catch (Exception ex)
			{
				ThrowException(cmd, ex);
				throw ex;
			}
			finally
			{
				//mark: 有事务不能直接释放命令
				if (_transaction != null)
					Close(cmd, cmd.Connection);
			}
			return ret;
		}
		/// <summary>
		/// 执行sql语句
		/// </summary>
		public int ExecuteNonQuery(CommandType commandType, string commandText, params NpgsqlParameter[] commandParameters)
		{
			int ret = 0;
			NpgsqlCommand cmd = new NpgsqlCommand();
			try
			{
				PrepareCommand(cmd, commandType, commandText, commandParameters);
				OpenConnection(cmd.Connection);
				ret = cmd.ExecuteNonQuery();
			}
			catch (Exception ex)
			{
				ThrowException(cmd, ex);
				throw ex;
			}
			finally
			{
				//mark: 若有事务不能直接释放命令
				if (_transaction != null)
					Close(cmd, cmd.Connection);
			}
			return ret;
		}
		/// <summary>
		/// 重构读取数据库数据
		/// </summary>
		public void ExecuteDataReader(Action<NpgsqlDataReader> action, CommandType commandType, string commandText, params NpgsqlParameter[] commandParameters)
		{
			NpgsqlCommand cmd = new NpgsqlCommand(); NpgsqlDataReader reader = null;
			try
			{
				PrepareCommand(cmd, commandType, commandText, commandParameters);
				if (cmd.Connection.State != ConnectionState.Open)
					cmd.Connection.Open();
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
				if (_transaction == null)
					Close(cmd, cmd.Connection);
				if (!reader.IsClosed)
					reader.Close();
			}
		}
		/// <summary>
		/// 抛出异常
		/// </summary>
		protected void ThrowException(NpgsqlCommand cmd, Exception ex)
		{
			string str = string.Empty;
			if (cmd.Parameters != null)
			{
				foreach (NpgsqlParameter item in cmd.Parameters)
					str += $"{item.ParameterName}:{item.Value}\n";
			}
			Close(cmd, cmd.Connection);
			//mark: 如果有事务, 则回滚事务
			RollBackTransaction();
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
			if (connection != null)
			{
				StopConnection(connection);
				//mark: 释放连接委托
			}
		}
		#region 事务
		/// <summary>
		/// 开启事务
		/// </summary>
		public void BeginTransaction()
		{
			if (_transaction == null)
				throw new Exception("the transaction is opened");
			_connection = GetConnection();
			OpenConnection(_connection);
			_transaction = _connection.BeginTransaction();
		}
		/// <summary>
		/// 确认是事务
		/// </summary>
		public void CommitTransaction()
		{
			if (_transaction != null)
			{
				_transaction.Commit();
				_transaction.Dispose();
			}
			Close(null, _transaction.Connection);
		}
		/// <summary>
		/// 回滚事务
		/// </summary>
		public void RollBackTransaction()
		{
			if (_transaction != null)
			{
				_transaction.Rollback();
				_transaction.Dispose();
			}
		}
		#endregion

	}
}

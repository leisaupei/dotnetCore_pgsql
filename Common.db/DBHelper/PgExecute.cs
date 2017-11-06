using System;
using System.Collections.Generic;
using System.Data;
using Npgsql;
using Microsoft.Extensions.Logging;
namespace Common.db.DBHelper
{
    public abstract class PgExecute : DBConnection
    {
        public static ILogger _logger = null;
        private NpgsqlTransaction _tran = null;
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
            if (_conn == null)
                _conn = GetConnection();
            command.Connection = _conn;
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
                if (_tran != null)
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
                if (_tran != null)
                    Close(cmd, cmd.Connection);
            }
            return ret;
        }
        /// <summary>
        /// 读取数据库数据
        /// </summary>
        /// <returns>The reader.</returns>
        /// <param name="commandType">Command type.</param>
        /// <param name="commandText">Command text.</param>
        /// <param name="commandParameters">Command parameters.</param>
        public NpgsqlDataReader ExecuteDataReader(CommandType commandType, string commandText, params NpgsqlParameter[] commandParameters)
        {
            NpgsqlCommand cmd = new NpgsqlCommand();
            NpgsqlDataReader reader = null;
            try
            {
                PrepareCommand(cmd, commandType, commandText, commandParameters);
                OpenConnection(cmd.Connection);
                reader = cmd.ExecuteReader();
            }
            catch (Exception ex)
            {
                ThrowException(cmd, ex);
                throw ex;
            }
            finally
            {
                //mark: 若有事务不能直接释放命令
                if (_tran != null)
                    Close(cmd, cmd.Connection);
            }
            return reader;
        }
        /// <summary>
        /// 重构读取数据库数据
        /// </summary>
        public void ExecuteDataReader(Action<NpgsqlDataReader> action, CommandType commandType, string commandText, params NpgsqlParameter[] commandParameters)
        {
            NpgsqlCommand _cmd = new NpgsqlCommand();
            try
            {
                PrepareCommand(_cmd, commandType, commandText, commandParameters);
                if (_cmd.Connection.State != ConnectionState.Open)
                    _cmd.Connection.Open();
                using (NpgsqlDataReader reader = _cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        action?.Invoke(reader);
                    }
                }
            }
            catch (Exception ex)
            {
                ThrowException(_cmd, ex);
                throw ex;
            }
            finally
            {
                if (_tran == null)
                    Close(_cmd, _cmd.Connection);
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
        public void Close(NpgsqlCommand cmd, NpgsqlConnection conn)
        {
            if (cmd != null)
            {
                if (cmd.Parameters != null)
                    cmd.Parameters.Clear();
                cmd.Dispose();
            }
            if (conn != null)
            {
                StopConnection(conn);
                //mark: 释放连接委托
            }
        }
        #region 事务
        /// <summary>
        /// 开启事务
        /// </summary>
        public void BeginTransaction()
        {
            if (_tran == null)
                throw new Exception("the transaction is opened");
            _conn = GetConnection();
            OpenConnection(_conn);
            _tran = _conn.BeginTransaction();
        }
        /// <summary>
        /// 确认是事务
        /// </summary>
        public void CommitTransaction()
        {
            if (_tran != null)
            {
                _tran.Commit();
                _tran.Dispose();
            }
            Close(null, _tran.Connection);
        }
        /// <summary>
        /// 回滚事务
        /// </summary>
        public void RollBackTransaction()
        {
            if (_tran != null)
            {
                _tran.Rollback();
                _tran.Dispose();
            }
        }
        #endregion

    }
}

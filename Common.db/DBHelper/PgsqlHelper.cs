using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.Logging;
using Npgsql;
using System.Data;
namespace Common.db.DBHelper
{
    public partial class PgSqlHelper
    {
        public partial class _execute : PgExecute
        {
            public _execute() { }
        }
        private static PgExecute Execute => new _execute();
        private static ILogger _logger;
        public static void InitDBConnection(ILogger logger, string connectionString)
        {
            if (string.IsNullOrEmpty(connectionString))
                throw new ArgumentNullException("connectionString is null");
            //mark: 日志 
            _logger = logger;
            PgExecute._logger = _logger;
            DBConnection.ConnectionString = connectionString;
        }
        public static object ExecuteScalar(CommandType commandType, string commandText, params NpgsqlParameter[] commandParameters) =>
            Execute.ExecuteScalar(commandType, commandText, commandParameters);
        public static int ExecuteNonQuery(CommandType commandType, string commandText, params NpgsqlParameter[] commandParameters) =>
            Execute.ExecuteNonQuery(commandType, commandText, commandParameters);
        public static NpgsqlDataReader ExecuteDataReader(CommandType commandType, string commandText, params NpgsqlParameter[] commandParameters) =>
            Execute.ExecuteDataReader(commandType, commandText, commandParameters);
        public static NpgsqlDataReader ExecuteDataReader(string commandText, params NpgsqlParameter[] commandParameters) =>
            Execute.ExecuteDataReader(CommandType.Text, commandText, commandParameters);
        public static void ExecuteDataReader(Action<NpgsqlDataReader> action, string commandText, params NpgsqlParameter[] commandParameters) =>
            Execute.ExecuteDataReader(action, CommandType.Text, commandText, commandParameters);
        public static int ExecuteNonQuery(string commandText) =>
            Execute.ExecuteNonQuery(CommandType.Text, commandText, null);
        /// <summary>
        /// 事务
        /// </summary>
        public static void Transaction(Action action)
        {
            try
            {
                Execute.BeginTransaction();
                action?.Invoke();
                Execute.CommitTransaction();
            }
            finally
            {
                Execute.Close(null, Execute._conn);
            }
        }
    }
}

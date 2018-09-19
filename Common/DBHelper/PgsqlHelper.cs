using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.Logging;
using Npgsql;
using System.Data;
using System.Reflection;
using System.ComponentModel;
using System.Linq;
using Newtonsoft.Json.Linq;
using Microsoft.AspNetCore.Server.Kestrel.Internal.System;

namespace DBHelper
{
	public partial class PgSqlHelper
	{
		public partial class Execute : PgExecute
		{
			public Execute(string connectionString, ILogger logger)
				: base(connectionString, logger) { }
		}
		/// <summary>
		/// 主库实例
		/// </summary>
		static Execute _masterExecute = null;
		public static PgExecute MasterExecute => _masterExecute;

		/// <summary>
		/// 从库实例
		/// </summary>
		static Execute _slaveExecute = null;
		public static PgExecute SlaveExecute => _slaveExecute;
		/// <summary>
		/// 日志
		/// </summary>
		static ILogger _logger;
		/// <summary>
		/// 初始化一主一从数据库连接
		/// </summary>
		/// <param name="connectionString">主库</param>
		/// <param name="logger"></param>
		/// <param name="slaveConnectionString">从库</param>
		public static void InitDBConnection(string connectionString, ILogger logger, string slaveConnectionString = "")
		{
			if (connectionString.IsNullOrEmpty())
				throw new ArgumentNullException("Connection String is null");
			//mark: 日志 
			_logger = logger;
			_masterExecute = new Execute(connectionString, logger);
			if (!slaveConnectionString.IsNullOrDBNull())
				_slaveExecute = new Execute(connectionString, logger);
		}
		/// <summary>
		/// 主库_返回(0,0)值
		/// </summary>
		/// <param name="cmdType"></param>
		/// <param name="cmdText"></param>
		/// <param name="cmdParams"></param>
		/// <returns></returns>
		public static object ExecuteScalar(CommandType cmdType, string cmdText, params NpgsqlParameter[] cmdParams) =>
			MasterExecute.ExecuteScalar(cmdType, cmdText, cmdParams);
		/// <summary>
		/// 主库_执行NonQuery
		/// </summary>
		/// <param name="cmdType"></param>
		/// <param name="cmdText"></param>
		/// <param name="cmdParams"></param>
		/// <returns></returns>
		public static int ExecuteNonQuery(CommandType cmdType, string cmdText, params NpgsqlParameter[] cmdParams) =>
			MasterExecute.ExecuteNonQuery(cmdType, cmdText, cmdParams);
		/// <summary>
		/// 主库_DataReader
		/// </summary>
		/// <param name="action"></param>
		/// <param name="cmdText"></param>
		/// <param name="cmdParams"></param>
		public static void ExecuteDataReader(Action<NpgsqlDataReader> action, string cmdText, params NpgsqlParameter[] cmdParams) =>
			MasterExecute.ExecuteDataReader(action, CommandType.Text, cmdText, cmdParams);
		/// <summary>
		/// 主库_重构Type为Text
		/// </summary>
		/// <param name="cmdText"></param>
		/// <param name="cmdParams"></param>
		/// <returns></returns>
		public static int ExecuteNonQuery(string cmdText, params NpgsqlParameter[] cmdParams) =>
			MasterExecute.ExecuteNonQuery(CommandType.Text, cmdText, cmdParams);
		/// <summary>
		/// 主库_重构Type为Text
		/// </summary>
		/// <param name="cmdText"></param>
		/// <param name="cmdParams"></param>
		/// <returns></returns>
		public static object ExecuteScalar(string cmdText, params NpgsqlParameter[] cmdParams) =>
			MasterExecute.ExecuteScalar(CommandType.Text, cmdText, cmdParams);
		/// <summary>
		/// 主库_返回T类型列表
		/// </summary>
		/// <typeparam name="T"></typeparam>
		public static List<T> ExecuteDataReaderList<T>(string cmdText, params NpgsqlParameter[] cmdParams)
		{
			var list = new List<T>();
			MasterExecute.ExecuteDataReader(dr =>
			{
				list.Add(dr.ReaderToModel<T>());
			}, CommandType.Text, cmdText, cmdParams);
			return list;
		}
		/// <summary>
		/// 主库_返回T对象
		/// </summary>
		public static T ExecuteDataReaderModel<T>(string cmdText, params NpgsqlParameter[] cmdParams)
		{
			var list = ExecuteDataReaderList<T>(cmdText, cmdParams);
			return list.Count > 0 ? list[0] : default(T);
		}
		/// <summary>
		/// 主库_重构
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="cmdText"></param>
		/// <param name="func"></param>
		/// <param name="cmdParams"></param>
		/// <returns></returns>
		public static List<T> ExecuteDataReaderList<T>(string cmdText, Func<T, T> func, params NpgsqlParameter[] cmdParams)
		{
			var list = new List<T>();
			MasterExecute.ExecuteDataReader(dr =>
			{
				var model = dr.ReaderToModel<T>();
				if (func != null)
				{
					model = func(model);
					if (model != null) list.Add(model);
				}
				else list.Add(model);
			}, CommandType.Text, cmdText, cmdParams);
			return list;
		}
		/// <summary>
		/// 主库_重构
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="cmdText"></param>
		/// <param name="func"></param>
		/// <param name="cmdParams"></param>
		/// <returns></returns>
		public static T ExecuteDataReaderModel<T>(string cmdText, Func<T, T> func, params NpgsqlParameter[] cmdParams)
		{
			var list = ExecuteDataReaderList<T>(cmdText, func, cmdParams);
			return list.Count > 0 ? list[0] : default(T);
		}

		/// <summary>
		/// 从库_返回(0,0)值
		/// </summary>
		/// <param name="cmdType"></param>
		/// <param name="cmdText"></param>
		/// <param name="cmdParams"></param>
		/// <returns></returns>
		public static object ExecuteScalarSlave(CommandType cmdType, string cmdText, params NpgsqlParameter[] cmdParams) =>
			SlaveExecute.ExecuteScalar(cmdType, cmdText, cmdParams);
		/// <summary>
		/// 从库_执行NonQuery
		/// </summary>
		/// <param name="cmdType"></param>
		/// <param name="cmdText"></param>
		/// <param name="cmdParams"></param>
		/// <returns></returns>
		public static int ExecuteNonQuerySlave(CommandType cmdType, string cmdText, params NpgsqlParameter[] cmdParams) =>
			SlaveExecute.ExecuteNonQuery(cmdType, cmdText, cmdParams);
		/// <summary>
		/// 从库_DataReader
		/// </summary>
		/// <param name="action"></param>
		/// <param name="cmdText"></param>
		/// <param name="cmdParams"></param>
		public static void ExecuteDataReaderSlave(Action<NpgsqlDataReader> action, string cmdText, params NpgsqlParameter[] cmdParams) =>
			SlaveExecute.ExecuteDataReader(action, CommandType.Text, cmdText, cmdParams);
		/// <summary>
		/// 从库_重构Type为Text
		/// </summary>
		/// <param name="cmdText"></param>
		/// <param name="cmdParams"></param>
		/// <returns></returns>
		public static int ExecuteNonQuerySlave(string cmdText, params NpgsqlParameter[] cmdParams) =>
			SlaveExecute.ExecuteNonQuery(CommandType.Text, cmdText, cmdParams);
		/// <summary>
		/// 从库_重构Type为Text
		/// </summary>
		/// <param name="cmdText"></param>
		/// <param name="cmdParams"></param>
		/// <returns></returns>
		public static object ExecuteScalarSlave(string cmdText, params NpgsqlParameter[] cmdParams) =>
			SlaveExecute.ExecuteScalar(CommandType.Text, cmdText, cmdParams);
		/// <summary>
		/// 从库_返回T类型列表
		/// </summary>
		/// <typeparam name="T"></typeparam>
		public static List<T> ExecuteDataReaderListSlave<T>(string cmdText, params NpgsqlParameter[] cmdParams)
		{
			var list = new List<T>();
			SlaveExecute.ExecuteDataReader(dr =>
			{
				list.Add(dr.ReaderToModel<T>());
			}, CommandType.Text, cmdText, cmdParams);
			return list;
		}
		/// <summary>
		/// 从库_返回T对象
		/// </summary>
		public static T ExecuteDataReaderModelSlave<T>(string cmdText, params NpgsqlParameter[] cmdParams)
		{
			var list = ExecuteDataReaderList<T>(cmdText, cmdParams);
			return list.Count > 0 ? list[0] : default(T);
		}
		/// <summary>
		/// 从库_重构
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="cmdText"></param>
		/// <param name="func"></param>
		/// <param name="cmdParams"></param>
		/// <returns></returns>
		public static List<T> ExecuteDataReaderListSlave<T>(string cmdText, Func<T, T> func, params NpgsqlParameter[] cmdParams)
		{
			var list = new List<T>();
			SlaveExecute.ExecuteDataReader(dr =>
			{
				var model = dr.ReaderToModel<T>();
				if (func != null)
				{
					model = func(model);
					if (model != null) list.Add(model);
				}
				else list.Add(model);
			}, CommandType.Text, cmdText, cmdParams);
			return list;
		}
		/// <summary>
		/// 从库_重构
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="cmdText"></param>
		/// <param name="func"></param>
		/// <param name="cmdParams"></param>
		/// <returns></returns>
		public static T ExecuteDataReaderModelSlave<T>(string cmdText, Func<T, T> func, params NpgsqlParameter[] cmdParams)
		{
			var list = ExecuteDataReaderList<T>(cmdText, func, cmdParams);
			return list.Count > 0 ? list[0] : default(T);
		}


		/// <summary>
		/// 事务
		/// </summary>
		public static void Transaction(Action action)
		{
			try
			{
				MasterExecute.BeginTransaction();
				action?.Invoke();
				MasterExecute.CommitTransaction();
			}
			catch (Exception e)
			{
				MasterExecute.RollBackTransaction();
				throw e;
			}
		}
	}
}

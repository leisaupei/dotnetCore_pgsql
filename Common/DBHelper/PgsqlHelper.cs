using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using Npgsql;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Text;

namespace DBHelper
{
	public static class PgSqlHelper
	{
		public class Execute : PgExecute
		{
			public Execute(string connectionString, ILogger logger)
				: base(connectionString, logger) { }
		}
		/// <summary>
		/// 主库实例
		/// </summary>
		static Execute _masterExecute;
		public static PgExecute MasterExecute => _masterExecute;

		/// <summary>
		/// 从库实例
		/// </summary>
		static List<Execute> _slaveExecute = new List<Execute>();
		public static PgExecute SlaveExecute
		{
			get
			{
				if (SlaveCount == 0)
					return MasterExecute;
				if (SlaveCount == 1) return _slaveExecute[0];
				var exe = _slaveExecute.OrderBy(f => f.Pool.Wait.Count).First();
				if (exe == null)
					throw new Exception("choose slave execute error");
				return exe;
			}
		}
		public static int SlaveCount = 0;
		/// <summary>
		/// 是否有从库
		/// </summary>
		public static bool HasSlave = false;
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
		public static void InitDBConnection(string connectionString, ILogger logger, string[] slaveConnectionString = null)
		{
			if (connectionString.IsNullOrEmpty())
				throw new ArgumentNullException("Connection String is null");
			//mark: 日志 
			_logger = logger;
			_masterExecute = new Execute(connectionString, logger);
			if (!slaveConnectionString.IsNullOrEmpty())
			{
				foreach (var item in slaveConnectionString)
					_slaveExecute.Add(new Execute(item, logger));
				SlaveCount = _slaveExecute.Count;
				HasSlave = true;
			}
		}
		/// <summary>
		/// 主库_返回(0,0)值
		/// </summary>
		/// <param name="cmdType"></param>
		/// <param name="cmdText"></param>
		/// <param name="cmdParams"></param>
		/// <returns></returns>
		public static object ExecuteScalar(CommandType cmdType, string cmdText, NpgsqlParameter[] cmdParams, DatabaseType type = HostConfig.DefaultDatabase) =>
			GetExcute(type).ExecuteScalar(cmdType, cmdText, cmdParams);
		/// <summary>
		/// 主库_执行NonQuery
		/// </summary>
		/// <param name="cmdType"></param>
		/// <param name="cmdText"></param>
		/// <param name="cmdParams"></param>
		/// <returns></returns>
		public static int ExecuteNonQuery(CommandType cmdType, string cmdText, NpgsqlParameter[] cmdParams, DatabaseType type = HostConfig.DefaultDatabase) =>
			GetExcute(type).ExecuteNonQuery(cmdType, cmdText, cmdParams);
		/// <summary>
		/// 主库_DataReader
		/// </summary>
		/// <param name="action"></param>
		/// <param name="cmdText"></param>
		/// <param name="cmdParams"></param>
		public static void ExecuteDataReader(Action<NpgsqlDataReader> action, string cmdText, NpgsqlParameter[] cmdParams, DatabaseType type = HostConfig.DefaultDatabase) =>
			GetExcute(type).ExecuteDataReader(action, CommandType.Text, cmdText, cmdParams);
		/// <summary>
		/// 主库_重构Type为Text
		/// </summary>
		/// <param name="cmdText"></param>
		/// <param name="cmdParams"></param>
		/// <returns></returns>
		public static int ExecuteNonQuery(string cmdText, NpgsqlParameter[] cmdParams, DatabaseType type = HostConfig.DefaultDatabase) =>
			GetExcute(type).ExecuteNonQuery(CommandType.Text, cmdText, cmdParams);
		/// <summary>
		/// 主库_重构Type为Text
		/// </summary>
		/// <param name="cmdText"></param>
		/// <param name="cmdParams"></param>
		/// <returns></returns>
		public static object ExecuteScalar(string cmdText, NpgsqlParameter[] cmdParams, DatabaseType type = HostConfig.DefaultDatabase) =>
			GetExcute(type).ExecuteScalar(CommandType.Text, cmdText, cmdParams);
		/// <summary>
		/// 主库_返回T类型列表
		/// </summary>
		/// <typeparam name="T"></typeparam>
		public static List<T> ExecuteDataReaderList<T>(string cmdText, NpgsqlParameter[] cmdParams, DatabaseType type = HostConfig.DefaultDatabase)
		{
			var list = new List<T>();
			GetExcute(type).ExecuteDataReader(dr =>
			{
				list.Add(dr.ReaderToModel<T>());
			}, CommandType.Text, cmdText, cmdParams);
			return list;
		}
		/// <summary>
		/// 主库_返回T对象
		/// </summary>
		public static T ExecuteDataReaderModel<T>(string cmdText, NpgsqlParameter[] cmdParams, DatabaseType type = HostConfig.DefaultDatabase)
		{
			var list = ExecuteDataReaderList<T>(cmdText, cmdParams, type);
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
		public static List<T> ExecuteDataReaderList<T>(string cmdText, Func<T, T> func, NpgsqlParameter[] cmdParams, DatabaseType type = HostConfig.DefaultDatabase)
		{
			var list = new List<T>();
			GetExcute(type).ExecuteDataReader(dr =>
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
		public static T ExecuteDataReaderModel<T>(string cmdText, Func<T, T> func, NpgsqlParameter[] cmdParams, DatabaseType type = HostConfig.DefaultDatabase)
		{
			var list = ExecuteDataReaderList<T>(cmdText, func, cmdParams, type);
			return list.Count > 0 ? list[0] : default(T);
		}
		/// <summary>
		/// 获取当前从库
		/// </summary>
		/// <param name="type"></param>
		/// <returns></returns>
		static PgExecute GetExcute(DatabaseType type)
		{
			switch (type)
			{
				case DatabaseType.Master: return MasterExecute;
				case DatabaseType.Slave: return SlaveExecute;
				default:
					throw new ArgumentException("method GetExcute(DatabaseType) enum value error");
			}
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

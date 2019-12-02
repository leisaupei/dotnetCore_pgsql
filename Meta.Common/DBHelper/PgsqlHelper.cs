using Meta.Common.Extensions;
using Meta.Common.Interface;
using Meta.Common.Model;
using Microsoft.Extensions.Logging;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace Meta.Common.DbHelper
{
	public static class PgSqlHelper
	{
		/// <summary>
		/// 从库后缀
		/// </summary>
		public const string SlaveSuffix = "-slave";
		/// <summary>
		/// 实例键值对
		/// </summary>
		static readonly Dictionary<string, List<Execute>> _executeDictString = new Dictionary<string, List<Execute>>();
		/// <summary>
		/// 随机从库
		/// </summary>
		static readonly Random _ran;
		internal class Execute : PgExecute
		{
			public Execute(string connectionString, ILogger logger, Action<NpgsqlConnection> mapAction)
				: base(connectionString, logger, mapAction) { }
		}

		static PgSqlHelper()
		{
			_ran = new Random(DateTime.Now.GetHashCode());
		}

		/// <summary>
		/// 获取连接实例
		/// </summary>
		/// <param name="type">数据库类型</param>
		/// <returns>对应实例</returns>
		static PgExecute GetExecute(string type)
		{
			if (_executeDictString.ContainsKey(type))
			{
				var execute = _executeDictString[type];
				if (execute.Count == 0)
					if (type.EndsWith(SlaveSuffix))
						return GetExecute(type.Replace(SlaveSuffix, string.Empty));

				if (execute.Count == 1)
					return execute[0];

				else if (execute.Count > 1)
					return execute[_ran.Next(0, execute.Count)];
			}
			// 从没有从库连接会查主库->如果没有连接会报错
			throw new ArgumentNullException($"not exist {type} execute");
		}
		/// <summary>
		/// 初始化一主多从数据库连接
		/// </summary>
		/// <param name="options">数据库连接</param>
		public static void InitDBConnectionOption(params BaseDbOption[] options)
		{
			if (options == null)
				throw new ArgumentNullException(nameof(options));
			if (options.Count() == 0)
				throw new ArgumentOutOfRangeException(nameof(options));
			foreach (var item in options)
				InitDB(item.ConnectionString, item.Logger, item.SlaveConnectionString, item.MapAction, item.TypeName);
		}
		/// <summary>
		/// 初始化数据库
		/// </summary>
		/// <param name="connectionString"></param>
		/// <param name="logger"></param>
		/// <param name="slaveConnectionString"></param>
		/// <param name="mapAction"></param>
		/// <param name="type"></param>
		static void InitDB(string connectionString, ILogger logger, string[] slaveConnectionString, Action<NpgsqlConnection> mapAction, string type)
		{
			if (string.IsNullOrEmpty(connectionString))
				throw new ArgumentNullException(nameof(connectionString), $"{type} Connection String is null");
			_executeDictString[type] = new List<Execute> { new Execute(connectionString, logger, mapAction) };
			if (slaveConnectionString?.Length > 0)
			{
				_executeDictString[type + SlaveSuffix] = new List<Execute>();
				foreach (var item in slaveConnectionString)
					_executeDictString[type + SlaveSuffix].Add(new Execute(item, logger, mapAction));
			}
		}
		/// <summary>
		/// 查询单个元素
		/// </summary>
		/// <param name="cmdType">命令类型</param>
		/// <param name="cmdText">sql语句</param>
		/// <param name="cmdParams">sql参数</param>
		/// <param name="type">数据库类型</param>
		/// <returns>返回(0,0)值</returns>
		public static object ExecuteScalar(CommandType cmdType, string cmdText, NpgsqlParameter[] cmdParams = null, string type = "master") =>
			GetExecute(type).ExecuteScalar(cmdType, cmdText, cmdParams);
		/// <summary>
		/// 执行NonQuery
		/// </summary>
		/// <param name="cmdType">命令类型</param>
		/// <param name="cmdText">sql语句</param>
		/// <param name="cmdParams">sql参数</param>
		/// <param name="type">数据库类型</param>
		/// <returns>修改行数</returns>
		public static int ExecuteNonQuery(CommandType cmdType, string cmdText, NpgsqlParameter[] cmdParams = null, string type = "master") =>
			GetExecute(type).ExecuteNonQuery(cmdType, cmdText, cmdParams);
		/// <summary>
		/// DataReader
		/// </summary>
		/// <param name="action">逐行Reader委托</param>
		/// <param name="cmdType">命令类型</param>
		/// <param name="cmdText">sql语句</param>
		/// <param name="cmdParams">sql参数</param>
		/// <param name="type">数据库类型</param>
		public static void ExecuteDataReader(Action<NpgsqlDataReader> action, CommandType cmdType, string cmdText, NpgsqlParameter[] cmdParams = null, string type = "master") =>
			GetExecute(type).ExecuteDataReader(action, cmdType, cmdText, cmdParams);
		/// <summary>
		/// 查询多行
		/// </summary>
		/// <typeparam name="T">列表类型</typeparam>
		/// <param name="cmdText">sql语句</param>
		/// <param name="cmdParams">sql参数</param>
		/// <param name="type">数据库类型</param>
		/// <returns>列表</returns>
		public static List<T> ExecuteDataReaderList<T>(CommandType cmdType, string cmdText, NpgsqlParameter[] cmdParams = null, string type = "master")
		{
			var list = new List<T>();
			ExecuteDataReader(dr =>
			{
				list.Add(dr.ReaderToModel<T>());
			}, cmdType, cmdText, cmdParams, type);
			return list;
		}
		/// <summary>
		/// 查询一行
		/// </summary>
		/// <typeparam name="T">实体类型</typeparam>
		/// <param name="cmdText">sql语句</param>
		/// <param name="cmdParams">sql参数</param>
		/// <param name="type">数据库类型</param>
		/// <returns>实体</returns>
		public static T ExecuteDataReaderModel<T>(CommandType cmdType, string cmdText, NpgsqlParameter[] cmdParams = null, string type = "master")
		{
			var list = ExecuteDataReaderList<T>(cmdType, cmdText, cmdParams, type);
			return list.Count > 0 ? list[0] : default;
		}

		#region overload
		/// <summary>
		/// DataReader
		/// </summary>
		/// <param name="action">逐行Reader委托</param>
		/// <param name="cmdText">sql语句</param>
		/// <param name="cmdParams">sql参数</param>
		/// <param name="type">数据库类型</param>
		public static void ExecuteDataReader(Action<NpgsqlDataReader> action, string cmdText, NpgsqlParameter[] cmdParams = null, string type = "master") =>
			ExecuteDataReader(action, CommandType.Text, cmdText, cmdParams, type);
		/// <summary>
		/// 重构Type为Text
		/// </summary>
		/// <param name="cmdText">sql语句</param>
		/// <param name="cmdParams">sql参数</param>
		/// <param name="type">数据库类型</param>
		/// <returns>修改行数</returns>
		public static int ExecuteNonQuery(string cmdText, NpgsqlParameter[] cmdParams = null, string type = "master") =>
			ExecuteNonQuery(CommandType.Text, cmdText, cmdParams, type);
		/// <summary>
		/// 重构Type为Text
		/// </summary>
		/// <param name="cmdText">sql语句</param>
		/// <param name="cmdParams">sql参数</param>
		/// <param name="type">数据库类型</param>
		/// <returns></returns>
		public static object ExecuteScalar(string cmdText, NpgsqlParameter[] cmdParams = null, string type = "master") =>
			ExecuteScalar(CommandType.Text, cmdText, cmdParams, type);


		/// <summary>
		/// 查询多行
		/// </summary>
		/// <typeparam name="T">列表类型</typeparam>
		/// <param name="cmdText">sql语句</param>
		/// <param name="cmdParams">sql参数</param>
		/// <param name="type">数据库类型</param>
		/// <returns>列表</returns>
		public static List<T> ExecuteDataReaderList<T>(string cmdText, NpgsqlParameter[] cmdParams = null, string type = "master") =>
			ExecuteDataReaderList<T>(CommandType.Text, cmdText, cmdParams, type);

		/// <summary>
		/// 查询一行
		/// </summary>
		/// <typeparam name="T">实体类型</typeparam>
		/// <param name="cmdText">sql语句</param>
		/// <param name="cmdParams">sql参数</param>
		/// <param name="type">数据库类型</param>
		/// <returns>实体</returns>
		public static T ExecuteDataReaderModel<T>(string cmdText, NpgsqlParameter[] cmdParams = null, string type = "master") =>
			ExecuteDataReaderModel<T>(CommandType.Text, cmdText, cmdParams, type);

		#endregion
		/// <summary>
		/// DataReader pipe
		/// </summary>
		/// <param name="cmdText">sql语句</param>
		/// <param name="cmdParams">sql参数</param>
		/// <param name="type">数据库类型</param>
		/// <returns>实体</returns>
		public static object[] ExecuteDataReaderPipe(CommandType cmdType, IEnumerable<ISqlBuilder> builders, string type = "master")
		{
			if (builders?.Any() != true)
				throw new ArgumentNullException(nameof(builders));
			object[] results = new object[builders.Count()];
			List<NpgsqlParameter> paras = new List<NpgsqlParameter>();
			int _paramsCount = 0;
			string ParamsIndex() => "p" + _paramsCount++.ToString().PadLeft(6, '0');

			var cmdText = string.Empty;
			foreach (var item in builders)
			{
				var itemCmdText = item.GetCommandTextString();

				foreach (var p in item.Params)
				{
					var newParaName = $"@{ParamsIndex()}";
					itemCmdText = itemCmdText.Replace($"@{p.ParameterName}", newParaName);
					p.ParameterName = newParaName;
				}
				paras.AddRange(item.Params);
				cmdText += itemCmdText + ";";
			}
			GetExecute(type).ExecuteDataReaderBase(dr =>
			{
				for (int i = 0; i < results.Length; i++)
				{
					var item = builders.ElementAt(i);
					List<object> list = new List<object>();
					while (dr.Read())
						list.Add(dr.ReaderToModel(item.Type));

					results[i] = item.ReturnType switch
					{
						var t when t == PipeReturnType.List => list.ToArray(),
						var t when t == PipeReturnType.One => list.Count > 0 ? list[0] : null,
						var t when t == PipeReturnType.Rows => "",
						_ => null,
					};
					
					dr.NextResult();
				}
			}, cmdType, cmdText, paras.ToArray());
			return results;
		}

		/// <summary>
		/// 事务 (暂不支持分布式事务)
		/// </summary>
		/// <param name="action">Action委托</param>
		/// <param name="type">数据库类型</param>
		public static void Transaction(Action action, string type = "master")
		{
			try
			{
				GetExecute(type).BeginTransaction();
				action?.Invoke();
				GetExecute(type).CommitTransaction();
			}
			catch (Exception e)
			{
				GetExecute(type).RollBackTransaction();
				throw e;
			}
		}
		/// <summary>
		/// 事务 (暂不支持分布式事务)
		/// </summary>
		/// <remarks>func返回false, 则回滚事务</remarks>
		/// <param name="action">Func委托</param>
		/// <param name="type">数据库类型</param>
		public static void Transaction(Func<bool> func, string type = "master")
		{
			if (func == null)
				throw new ArgumentNullException(nameof(func));
			try
			{
				GetExecute(type).BeginTransaction();
				if (func.Invoke())
					GetExecute(type).CommitTransaction();
				else
					GetExecute(type).RollBackTransaction();
			}
			catch (Exception e)
			{
				GetExecute(type).RollBackTransaction();
				throw e;
			}
		}
	}
}

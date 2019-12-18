using Meta.Common.Extensions;
using Meta.Common.Interface;
using Meta.Common.Model;
using Microsoft.Extensions.Logging;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Text;

namespace Meta.Common.DbHelper
{
	public static class PgsqlHelper
	{

		/// <summary>
		/// 实例键值对
		/// </summary>
		static readonly Dictionary<string, List<DbExecute>> _executeDictString = new Dictionary<string, List<DbExecute>>();

		/// <summary>
		/// 随机从库
		/// </summary>
		static readonly Random _ran;

		/// <summary>
		/// 实现Pgsql
		/// </summary>
		internal class PgsqlExecute : DbExecute
		{
			public PgsqlExecute(DbConnectionModel conn) : base(conn) { }
		}

		/// <summary>
		/// 静态构造
		/// </summary>
		static PgsqlHelper()
		{
			_ran = new Random(DateTime.Now.GetHashCode());
		}

		/// <summary>
		/// 获取连接实例
		/// </summary>
		/// <param name="type">数据库类型</param>
		/// <exception cref="ArgumentNullException">没有找到对应名称实例</exception>
		/// <returns>对应实例</returns>
		static DbExecute GetExecute(string type)
		{
			if (_executeDictString.ContainsKey(type))
			{
				var execute = _executeDictString[type];
				if (execute.Count == 0)
					if (type.EndsWith(BaseDbOption.SlaveSuffix))
						return GetExecute(type.Replace(BaseDbOption.SlaveSuffix, string.Empty));

				if (execute.Count == 1)
					return execute[0];

				else if (execute.Count > 1)
					return execute[_ran.Next(0, execute.Count)];
			}
			// 从没有从库连接会查主库->如果没有连接会报错
			throw new ArgumentNullException("connectionstring", $"not exist {type} execute");
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
			foreach (var option in options)
			{
				if (string.IsNullOrEmpty(option.MasterConnectionString))
					throw new ArgumentNullException(nameof(option.MasterConnectionString), $"{option.TypeName} Connection String is null");
				var dbModel = new DbConnectionModel(option.MasterConnectionString, option.Logger, option.Type);
				dbModel.Options.MapAction = option.Options.MapAction;

				_executeDictString[option.TypeName] = new List<DbExecute> { new PgsqlExecute(dbModel) };

				if ((option.SlaveConnectionStrings?.Length ?? 0) == 0) continue;

				_executeDictString[option.TypeName + BaseDbOption.SlaveSuffix] = new List<DbExecute>();
				foreach (var item in option.SlaveConnectionStrings)
				{
					var dbModelSlave = new DbConnectionModel(item, option.Logger, option.Type);
					dbModelSlave.Options.MapAction = option.Options.MapAction;
					_executeDictString[option.TypeName + BaseDbOption.SlaveSuffix].Add(new PgsqlExecute(dbModelSlave));
				}
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
		public static object ExecuteScalar(string cmdText, CommandType cmdType = CommandType.Text, DbParameter[] cmdParams = null, string type = "master") =>
			GetExecute(type).ExecuteScalar(cmdText, cmdType, cmdParams);

		/// <summary>
		/// 执行NonQuery
		/// </summary>
		/// <param name="cmdType">命令类型</param>
		/// <param name="cmdText">sql语句</param>
		/// <param name="cmdParams">sql参数</param>
		/// <param name="type">数据库类型</param>
		/// <returns>修改行数</returns>
		public static int ExecuteNonQuery(string cmdText, CommandType cmdType = CommandType.Text, DbParameter[] cmdParams = null, string type = "master") =>
			GetExecute(type).ExecuteNonQuery(cmdText, cmdType, cmdParams);

		/// <summary>
		/// DataReader
		/// </summary>
		/// <param name="action">逐行Reader委托</param>
		/// <param name="cmdType">命令类型</param>
		/// <param name="cmdText">sql语句</param>
		/// <param name="cmdParams">sql参数</param>
		/// <param name="type">数据库类型</param>
		public static void ExecuteDataReader(Action<DbDataReader> action, string cmdText, CommandType cmdType = CommandType.Text, DbParameter[] cmdParams = null, string type = "master") =>
			GetExecute(type).ExecuteDataReader(action, cmdText, cmdType, cmdParams);

		/// <summary>
		/// 查询多行
		/// </summary>
		/// <typeparam name="T">列表类型</typeparam>
		/// <param name="cmdText">sql语句</param>
		/// <param name="cmdParams">sql参数</param>
		/// <param name="type">数据库类型</param>
		/// <returns>列表</returns>
		public static List<T> ExecuteDataReaderList<T>(string cmdText, CommandType cmdType = CommandType.Text, DbParameter[] cmdParams = null, string type = "master")
		{
			var list = new List<T>();
			ExecuteDataReader(dr =>
			{
				list.Add(dr.ReaderToModel<T>());
			}, cmdText, cmdType, cmdParams, type);
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
		public static T ExecuteDataReaderModel<T>(string cmdText, CommandType cmdType = CommandType.Text, DbParameter[] cmdParams = null, string type = "master")
		{
			var list = ExecuteDataReaderList<T>(cmdText, cmdType, cmdParams, type);
			return list.Count > 0 ? list[0] : default;
		}

		/// <summary>
		/// DataReader pipe
		/// </summary>
		/// <param name="builders">sql builder</param>
		/// <param name="cmdType">命令类型</param>
		/// <param name="type">数据库名称</param>
		/// <exception cref="ArgumentNullException">builders is null or empty</exception>
		/// <returns>实体</returns>
		public static object[] ExecuteDataReaderPipe(IEnumerable<ISqlBuilder> builders, CommandType cmdType = CommandType.Text, string type = "master")
		{
			if (builders?.Any() != true)
				throw new ArgumentNullException(nameof(builders));

			object[] results = new object[builders.Count()];
			var paras = new List<DbParameter>();
			var cmdText = new StringBuilder();
			foreach (var item in builders)
			{
				paras.AddRange(item.Params);
				cmdText.Append(item.GetCommandTextString()).AppendLine(";");
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
						var t when t == PipeReturnType.List =>
							list.ToArray(),
						var t when t == PipeReturnType.One =>
							list.Count > 0 ? list[0] : item.Type.IsTuple() ? Activator.CreateInstance(item.Type) : default, // 返回默认值
						var t when t == PipeReturnType.Rows =>
							dr.RecordsAffected,
						_ => throw new ArgumentException("ReturnType is wrong", nameof(item.ReturnType)),
					};

					dr.NextResult();
				}
			}, cmdText.ToString(), cmdType, paras.ToArray());
			return results;
		}

		/// <summary>
		/// 事务 (暂不支持分布式事务)
		/// </summary>
		/// <param name="action">Action委托</param>
		/// <param name="type">数据库名称</param>
		/// <exception cref="ArgumentNullException">委托是null</exception>
		public static void Transaction(Action action, string type = "master")
		{
			if (action == null)
				throw new ArgumentNullException(nameof(action));
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
		/// <param name="func">Func委托</param>
		/// <param name="type">数据库名称</param>
		/// <exception cref="ArgumentNullException">委托是null</exception>
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

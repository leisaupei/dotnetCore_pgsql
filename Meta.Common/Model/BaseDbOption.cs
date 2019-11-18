using Meta.Common.Interface;
using Microsoft.Extensions.Logging;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Text;

namespace Meta.Common.Model
{
	public class BaseDbOption
	{
		public BaseDbOption(string typeName, string connectionString, string[] slaveConnectionString, ILogger logger)
		{
			TypeName = typeName;
			ConnectionString = connectionString;
			SlaveConnectionString = slaveConnectionString;
			Logger = logger;
		}
		/// <summary>
		/// CLR映射
		/// </summary>
		public Action<NpgsqlConnection> MapAction { get; protected set; }
		/// <summary>
		/// 从库连接
		/// </summary>
		public string[] SlaveConnectionString { get; }
		/// <summary>
		/// logger
		/// </summary>
		public ILogger Logger { get; }
		/// <summary>
		/// 主库连接
		/// </summary>
		public string ConnectionString { get; }
		/// <summary>
		/// 数据库别名
		/// </summary>
		public string TypeName { get; }
	}
}

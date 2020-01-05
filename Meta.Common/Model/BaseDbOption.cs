using Meta.Common.Interface;
using Microsoft.Extensions.Logging;
using Npgsql;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.Common;
using System.Text;

namespace Meta.Common.Model
{
	public class BaseDbOption
	{
		/// <summary>
		/// 从库后缀
		/// </summary>
		public const string SlaveSuffix = "-slave";
		public BaseDbOption(string typeName, string connectionString, string[] slaveConnectionStrings, ILogger logger)
		{
			TypeName = typeName;
			MasterConnectionString = connectionString;
			SlaveConnectionStrings = slaveConnectionStrings;
			Logger = logger;
		}
		/// <summary>
		/// 从库连接
		/// </summary>
		public string[] SlaveConnectionStrings { get; }
		/// <summary>
		/// logger
		/// </summary>
		public ILogger Logger { get; }
		/// <summary>
		/// 主库连接
		/// </summary>
		public string MasterConnectionString { get; }
		/// <summary>
		/// 数据库别名
		/// </summary>
		public string TypeName { get; }
		/// <summary>
		/// 数据库类型
		/// </summary>
		public DatabaseType Type { get; } = DatabaseType.Postgres;
		/// <summary>
		/// 数据库配置
		/// </summary>
		public DbConnectionOptions Options { get; private set; } = new DbConnectionOptions();

	}
	internal class DbConnectionModel
	{
		public DbConnectionModel(string connectionString, ILogger logger, DatabaseType type)
		{
			Logger = logger;
			ConnectionString = connectionString;
			Type = type;
		}

		/// <summary>
		/// logger
		/// </summary>
		public ILogger Logger { get; }
		/// <summary>
		/// 数据库连接
		/// </summary>
		public string ConnectionString { get; }
		/// <summary>
		/// 数据库类型
		/// </summary>
		public DatabaseType Type { get; } = DatabaseType.Postgres;
		/// <summary>
		/// 针对不同类型的数据库需要响应的配置
		/// </summary>
		public DbConnectionOptions Options { get; private set; } = new DbConnectionOptions();
		/// <summary>
		/// 创建连接
		/// </summary>
		/// <returns></returns>
		internal DbConnection GetConnection
		{
			get
			{
				DbConnection connection = null;
				if (Type == DatabaseType.Postgres)
					connection = new NpgsqlConnection(ConnectionString);

				if (connection == null)
					throw new ArgumentNullException(nameof(connection));
				connection.Open();

				SetDatabaseOption(connection);
				return connection;
			}
		}

		void SetDatabaseOption(DbConnection connection)
		{
			if (Type == DatabaseType.Postgres)
				Options.MapAction?.Invoke((NpgsqlConnection)connection);
		}
	}
	public class DbConnectionOptions
	{
		/// <summary>
		/// CLR映射
		/// </summary>
		public Action<NpgsqlConnection> MapAction { get; set; }
	}
}

using Meta.Common.DbHelper;
using Meta.Common.Interface;
using Microsoft.Extensions.Logging;
using Npgsql;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Meta.Common.Model
{
	public class BaseDbOption<TDbMaterName, TDbSlaveName> : IDbOption
		where TDbMaterName : struct, IDbName
		where TDbSlaveName : struct, IDbName
	{
		private readonly string _masterConnectionString;
		private readonly string[] _slaveConnectionStrings;
		private readonly ILogger _logger;

		public DbConnectionOptions Options { get; private set; } = new DbConnectionOptions();

		public BaseDbOption(string masterConnectionString, string[] slaveConnectionStrings, ILogger logger)
		{
			_masterConnectionString = masterConnectionString;
			_slaveConnectionStrings = slaveConnectionStrings;
			_logger = logger;
		}

		DbConnectionModel IDbOption.Master => new DbConnectionModel(_masterConnectionString, _logger, DatabaseType.Postgres, typeof(TDbMaterName).Name, Options);

		DbConnectionModel[] IDbOption.Slave => _slaveConnectionStrings?.Select(f => new DbConnectionModel(f, _logger, DatabaseType.Postgres, typeof(TDbSlaveName).Name, Options)).ToArray();
	}
	internal class DbConnectionModel
	{
		public DbConnectionModel(string connectionString, ILogger logger, DatabaseType type, string dbName, DbConnectionOptions options)
		{
			Logger = logger;
			ConnectionString = connectionString;
			Type = type;
			DbName = dbName;
			Options = options;
		}
		public string DbName { get; }
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
		public DbConnectionOptions Options { get; }
		/// <summary>
		/// 创建连接
		/// </summary>
		/// <returns></returns>
		internal DbConnection GetConnection() => GetConnectionAsync(false, CancellationToken.None).ConfigureAwait(false).GetAwaiter().GetResult();

		/// <summary>
		/// 创建连接
		/// </summary>
		/// <returns></returns>
		internal Task<DbConnection> GetConnectionAsync(CancellationToken cancellationToken)
			=> cancellationToken.IsCancellationRequested ? Task.FromCanceled<DbConnection>(cancellationToken) : GetConnectionAsync(true, cancellationToken);

		async Task<DbConnection> GetConnectionAsync(bool async, CancellationToken cancellationToken)
		{
			if (cancellationToken.IsCancellationRequested)
				return await Task.FromCanceled<DbConnection>(cancellationToken);

			DbConnection connection = null;
			if (Type == DatabaseType.Postgres)
				connection = new NpgsqlConnection(ConnectionString);

			if (connection == null)
				throw new ArgumentNullException(nameof(connection));
			if (async)
				await connection.OpenAsync(cancellationToken);
			else
				connection.Open();

			SetDatabaseOption(connection);
			return connection;
		}

		void SetDatabaseOption(DbConnection connection)
		{
			if (Type == DatabaseType.Postgres)
				Options?.MapAction?.Invoke((NpgsqlConnection)connection);
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

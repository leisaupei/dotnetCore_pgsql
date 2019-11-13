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

		public Action<NpgsqlConnection> MapAction { get; protected set; }
		public string[] SlaveConnectionString { get; }
		public ILogger Logger { get; }
		public string ConnectionString { get; }
		public string TypeName { get; }
	}
}

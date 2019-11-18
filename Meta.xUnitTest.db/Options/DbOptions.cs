using Meta.xUnitTest.Model;
using System;
using Microsoft.Extensions.Logging;
using Meta.Common.Model;
using Meta.Common.DBHelper;
using Npgsql;

namespace Meta.xUnitTest.Options
{
	public static class DbOptions
	{
		/// <summary>
		/// 主库
		/// </summary>
		public const string Master = "master";
		/// <summary>
		/// 从库
		/// </summary>
		public const string Slave = "master-slave";

		#region Master
		public class MasterDbOption : BaseDbOption
		{
			public MasterDbOption(string connectionString, string[] slaveConnectionString, ILogger logger) : base(Master, connectionString, slaveConnectionString, logger)
			{
				NpgsqlNameTranslator translator = new NpgsqlNameTranslator();
				MapAction = conn =>
				{
					conn.TypeMapper.UseJsonNet();
				};
			}
		}
		#endregion

	}
}

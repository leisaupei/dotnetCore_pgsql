using Meta.Common.DBHelper;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using static Meta.xUnitTest.Options.DbOptions;

namespace Meta.xUnitTest
{
	public class BaseTest
	{
		public const string TestConnectionString = "host=localhost;port=5432;username=postgres;password=123456;database=meta;maximum pool size=50;pooling=true;Timeout=1024;CommandTimeout=1024;";
		public BaseTest()
		{
			var logger = new LoggerFactory();
			PgSqlHelper.InitDBConnectionOption(new MasterDbOption(TestConnectionString, null, logger.CreateLogger<BaseTest>()));
		}
	}
}

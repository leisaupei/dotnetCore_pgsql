using Meta.Common.DbHelper;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit.Abstractions;
using static Meta.xUnitTest.Options.DbOptions;

namespace Meta.xUnitTest
{
	public class BaseTest
	{
		public const string TestConnectionString = "host=localhost;port=5432;username=postgres;password=123456;database=meta;maximum pool size=10;pooling=true;Timeout=1024;CommandTimeout=1024;";

		public static readonly Guid StuPeopleId1 = Guid.Parse("da58b577-414f-4875-a890-f11881ce6341");

		public static readonly Guid StuPeopleId2 = Guid.Parse("5ef5a598-e4a1-47b3-919e-4cc1fdd97757");
		public static readonly Guid GradeId = Guid.Parse("81d58ab2-4fc6-425a-bc51-d1d73bf9f4b1");

		public static readonly string StuNo1 = "1333333";
		public static readonly string StuNo2 = "1333334";

		public static bool IsInit = false;
		protected readonly ITestOutputHelper _output;

		public BaseTest()
		{
			if (!IsInit)
			{
				var logger = new LoggerFactory();
				PgSqlHelper.InitDBConnectionOption(new MasterDbOption(TestConnectionString, null, logger.CreateLogger<BaseTest>()));
				RedisHelper.Initialization(new CSRedis.CSRedisClient("172.16.1.250:6379,defaultDatabase=13,name=weibo,password=Gworld2017,prefix=weibo,abortConnect=false"));
				IsInit = true;
			}
		}

		public BaseTest(ITestOutputHelper output) : this()
		{
			_output = output;
		}

	}
}

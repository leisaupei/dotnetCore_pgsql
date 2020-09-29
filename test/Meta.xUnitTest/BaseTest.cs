using Meta.Driver.DbHelper;
using Meta.Driver.Extensions;
using Meta.xUnitTest.Model;
using Meta.xUnitTest.Options;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit.Abstractions;
using static Meta.xUnitTest.Options.DbOptions;

namespace Meta.xUnitTest
{
	public class BaseTest
	{
		public const string TestConnectionString = "host=localhost;port=5432;username=postgres;password=123456;database=meta;maximum pool size=10;pooling=true;Timeout=10;CommandTimeout=10;";

		public static readonly Guid StuPeopleId1 = Guid.Parse("da58b577-414f-4875-a890-f11881ce6341");

		public static readonly Guid StuPeopleId2 = Guid.Parse("5ef5a598-e4a1-47b3-919e-4cc1fdd97757");
		public static readonly Guid GradeId = Guid.Parse("81d58ab2-4fc6-425a-bc51-d1d73bf9f4b1");

		public static readonly string StuNo1 = "1333333";
		public static readonly string StuNo2 = "1333334";

		public static bool IsInit;
		protected readonly ITestOutputHelper _output;

		public BaseTest()
		{
			if (!IsInit)
			{
				var logger = new LoggerFactory();
				var options = new[] {
					new MasterDbOption(TestConnectionString, null, logger.CreateLogger<BaseTest>())
				};
				PgsqlHelper.InitDBConnectionOption<DbMaster>(options, true, true);
				RedisHelper.Initialization(new CSRedis.CSRedisClient("localhost:6379,defaultDatabase=13,name=test,password=12345,prefix=test,abortConnect=false"));
				IsInit = true;
				JsonConvert.DefaultSettings = () =>
				{
					var st = new JsonSerializerSettings
					{
						Formatting = Formatting.Indented,
					};
					st.Converters.Add(new StringEnumConverter());
					st.Converters.Add(new IPConverter());
					st.Converters.Add(new PhysicalAddressConverter());
					st.Converters.Add(new NpgsqlTsQueryConverter());
					st.Converters.Add(new NpgsqlTsVectorConverter());
					st.Converters.Add(new BitArrayConverter());
					st.Converters.Add(new NpgsqlPointListConverter());
					st.Converters.Add(new BooleanConverter());
					st.Converters.Add(new DateTimeConverter());

					st.ContractResolver = new LowercaseContractResolver();
					return st;
				};
			}
		}

		public BaseTest(ITestOutputHelper output) : this()
		{
			_output = output;
		}

	}
}

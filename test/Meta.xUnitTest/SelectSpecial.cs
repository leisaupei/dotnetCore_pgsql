using Meta.Driver.Interface;
using Meta.Driver.SqlBuilder;
using Meta.xUnitTest.DAL;
using Meta.xUnitTest.Model;
using Meta.xUnitTest.Options;
using Npgsql;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Xunit;
using Xunit.Extensions.Ordering;
using Meta.xUnitTest.Extensions;
using Newtonsoft.Json.Linq;
using Meta.Driver.DbHelper;

namespace Meta.xUnitTest
{
	public class SelectSpecial : BaseTest
	{
		[Fact]
		public void ReturnJTokenInTuple()
		{
			//var info = People.Select.Where(a => a.Id == StuPeopleId1).ToOne<(Guid id, string name, JToken address_detail, EDataState state)>("id,name,address_detail,state");
			PgsqlHelper.ExecuteNonQuery("update class.grade set create_time = now() + @years", System.Data.CommandType.Text,
				new[] { new Npgsql.NpgsqlParameter<TimeSpan>("years", TimeSpan.FromDays(365)) });

		}
		[Fact]
		public void ReturnStringOrValueType()
		{
			//var info = People.Select.WhereId(StuPeopleId1).ToOne<string>("name");
			RedisHelper.Publish("change_gift_change_listener_channel", "11");
			//var info1 = People.Select.Where(a => a.Id == StuPeopleId1).ToOne<Guid>("id");
		}
	}
}

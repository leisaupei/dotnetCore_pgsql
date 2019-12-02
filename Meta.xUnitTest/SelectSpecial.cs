using Meta.Common.Interface;
using Meta.Common.SqlBuilder;
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

namespace Meta.xUnitTest
{
	public class SelectSpecial : BaseTest
	{
		[Fact]
		public void ReturnJTokenInTuple()
		{
			var info = People.Select.WhereId(StuPeopleId1).ToOne<(Guid id, string name, JToken address_detail, EDataState state)>("id,name,address_detail,state");
		}
		[Fact]
		public void ReturnStringOrValueType()
		{
			//var info = People.Select.WhereId(StuPeopleId1).ToOne<string>("name");

			var info1 = People.Select.WhereId(StuPeopleId1).ToOne<Guid>("id");
		}
	}
}

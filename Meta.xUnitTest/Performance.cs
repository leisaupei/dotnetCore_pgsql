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
using Meta.Common.DbHelper;
using System.Threading.Tasks;
using System.Threading;

namespace Meta.xUnitTest
{
	public class Performance : BaseTest
	{
		[Fact]
		public void InertTenThousandData()
		{
			for (int i = 0; i < 10000; i++)
			{
				PgsqlHelper.Transaction(() =>
				{
					new PeopleModel
					{
						Address = "address" + i,
						Id = Guid.NewGuid(),
						Age = i,
						Create_time = DateTime.Now,
						Name = "性能测试" + i,
						Sex = true,
					}.Insert();
				});
			}
		}
		[Fact]
		public Task TestAsync()
		{
			return PgsqlHelper.ExecuteNonQueryAsync("update people set age = 2 where id = '5ef5a598-e4a1-47b3-919e-4cc1fdd97757';").AsTask();

		}

		public class Test
		{
			public int Code { get; set; }
		}
		[Fact]
		public async Task TestAsync1()
		{
			var affrows = await PgsqlHelper.ExecuteNonQueryAsync("update people set age = 2 where id = '5ef5a598-e4a1-47b3-919e-4cc1fdd97757';");

		}

	}
}

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

namespace Meta.xUnitTest
{
	public class Performance : BaseTest
	{
		[Fact]
		public void InertTenThousandData()
		{
			for (int i = 0; i < 10000; i++)
			{
				PgSqlHelper.Transaction(() =>
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
	}
}

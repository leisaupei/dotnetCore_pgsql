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

namespace Meta.xUnitTest
{
	[Order(3)]
	public class SelectExpression : BaseTest
	{
		[Fact]
		public void WherePropertyIsNull()
		{
			var list = People.Select.OrderByDescing(a => a.Create_time).ToList();

			//Assert.Null(info?.Address);
		}

	}
}

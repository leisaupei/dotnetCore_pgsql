using Meta.Common.Interface;
using Meta.Common.SqlBuilder;
using Meta.xUnitTest.DAL;
using Meta.xUnitTest.Model;
using Meta.xUnitTest.Options;
using Npgsql;
using NpgsqlTypes;
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
		public void OrderBy()
		{
			var list = People.Select.OrderByDescending(a => a.Create_time).ToList();

			//Assert.Null(info?.Address);
		}
		[Fact]
		public void Join()
		{
			var union = Student.Select
					.InnerJoin<ClassmateModel>((a, b) => a.Id == b.Student_id)
					.ToOne();

			//Assert.Null(info?.Address);
		}
		[Fact]
		public void Where()
		{
			var union = Student.Select
					.Where(a => a.People_id == StuPeopleId1)
					.InnerJoin<ClassmateModel>((a, b) => a.Id == b.Student_id)
					.ToOne();

			//Assert.Null(info?.Address);
		}
	}
}

using Meta.xUnitTest.DAL;
using Npgsql;
using System;
using Xunit;

namespace Meta.xUnitTest
{
	public class SelectNormal : BaseTest
	{
		[Fact]
		public void ToOne()
		{
			var info = Student.Select.WhereId(Guid.Empty).ToOne();
		}
		[Fact]
		public void GetItem()
		{
			var info = Student.GetItem(Guid.Empty);
		}
		[Fact]
		public void GetItems()
		{
			var info = Student.GetItems(new[] { Guid.Empty, Guid.Empty });
		}

	}
}

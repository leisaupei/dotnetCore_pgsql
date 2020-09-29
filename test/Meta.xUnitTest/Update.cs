using Meta.Driver.Interface;
using Meta.Driver.SqlBuilder;
using Meta.xUnitTest.DAL;
using Meta.xUnitTest.Model;
using Meta.xUnitTest.Options;
using Newtonsoft.Json;
using Npgsql;
using NpgsqlTypes;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text.RegularExpressions;
using Xunit;
using Xunit.Extensions.Ordering;

namespace Meta.xUnitTest
{
	[Order(4)]
	public class Update : BaseTest
	{
		[Fact]
		public void SetEnumToInt()
		{
			//var info = TypeTest.GetItem(Guid.Empty);
			var affrows = TypeTest.UpdateBuilder.Set(a => a.Enum_type, EDataState.正常).Where(a => a.Int4_type > Math.Abs(-3)).ToRows();
		}

	}
}

﻿using Meta.Driver.Interface;
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
using Meta.Driver.DbHelper;
using System.Threading.Tasks;
using Meta.Driver.Model;
using System.Threading;

namespace Meta.xUnitTest
{
	public class Performance : BaseTest
	{
		[Fact]
		public void InertTenThousandData()
		{
			//for (int i = 0; i < 10000; i++)
			//{
			PgsqlHelper.Transaction(() =>
			{
				var total = TypeTest.Select.Sum(a => a.Int8_type, 0);
			});

			//}
		}
		//[Fact]
		//public async Task TransactionAsync()
		//{
		//	await PgsqlHelper.TransactionAsync(() =>
		//	{
		//		ClassGrade.Update(Guid.Parse("81d58ab2-4fc6-425a-bc51-d1d73bf9f4b1")).Set(a => a.Name, "软件技术").ToRows();
		//	}, CancellationToken.None);
		//}
		[Fact]
		public void TestAsync()
		{
			var affrows = PgsqlHelper<DbMaster>.ExecuteScalar("update people set age = 2 where id = '5ef5a598-e4a1-47b3-919e-4cc1fdd97757';");
			PgsqlHelper.ExecuteScalar("update people set age = 2 where id = '5ef5a598-e4a1-47b3-919e-4cc1fdd97757';");

		}
		[Fact]
		public void GetYearSection()
		{
			var defaultDatetime = new DateTime(1970, 1, 1, 0, 0, 0);
			var datetime = new DateTime(2020, 1, 1);
			var offsetYears = datetime.Year - defaultDatetime.Year;
			var splitYears = 2;
			var seed = offsetYears / splitYears;
			var begin = defaultDatetime.Year + (seed * splitYears);
			var end = defaultDatetime.Year + ((seed + 1) * splitYears - 1);

		}
		[Fact]
		public void GetMonthSection()
		{
			var defaultDatetime = new DateTime(1970, 1, 1, 0, 0, 0);
			var datetime = new DateTime(1970, 4, 12);
			var splitMonths = 3;
			var offsetMonths = (datetime.Year - defaultDatetime.Year) * 12 + datetime.Month;
			var seed = offsetMonths / splitMonths;
			var begin = defaultDatetime.AddMonths(seed * splitMonths);
			var end = begin.AddMonths(splitMonths - 1);
			var beginStr = begin.ToString("yyyyMM");
			var endStr = end.ToString("yyyyMM");
		}
		[Fact]
		public void GetDateSection()
		{
			var defaultDatetime = new DateTime(1970, 1, 1, 0, 0, 0);
			var datetime = new DateTime(1970, 4, 2);
			var splitDays = 100;
			var offsetDays = (int)(datetime - defaultDatetime).TotalDays;
			var seed = offsetDays / splitDays;
			var begin = defaultDatetime.AddDays(seed * splitDays);
			var end = begin.AddDays(splitDays - 1);
			var beginStr = begin.ToString("yyyyMMdd");
			var endStr = end.ToString("yyyyMMdd");
		}
		[Fact]
		public void SplitTest()
		{
			object[] value = new Enum[][] {
				new Enum[] { SplitType.DateTimeEveryDays, SplitType.DateTimeEveryMonths},
				new Enum[] { SplitType.DateTimeEveryDays }
			};
			var map = value.OfType<int[]>().ToArray();
		}
	}
}

using Meta.Driver.DbHelper;
using Meta.Driver.Interface;
using Meta.Driver.SqlBuilder;
using Meta.xUnitTest.DAL;
using Meta.xUnitTest.Model;
using Meta.xUnitTest.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Npgsql;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Xunit;
using Xunit.Extensions.Ordering;

namespace Meta.xUnitTest
{
	[Order(5)]
	public class Delete : BaseTest
	{
		[Fact]
		public void Union()
		{
			decimal d = 19200.32M;
			var a = d.ToString("#0.00");
			var b = string.Format("{0:N2}", d);
			//var jobj = new JArray {

			//};
			//var list = JsonConvert.DeserializeObject<List<ModelInvoiceItem>>(jobj.ToString());
		}

		public class ModelInvoiceItem
		{
			public decimal Amount { get; set; }
			public string Pdf { get; set; }
			public string InvoiceCode { get; set; }
			public string Status { get; set; }

		}
	}
}

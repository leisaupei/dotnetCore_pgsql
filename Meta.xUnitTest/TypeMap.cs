using Meta.Common.Interface;
using Meta.Common.SqlBuilder;
using Meta.xUnitTest.DAL;
using Meta.xUnitTest.Model;
using Meta.xUnitTest.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Npgsql;
using NpgsqlTypes;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using Xunit;
using Xunit.Extensions.Ordering;

namespace Meta.xUnitTest
{
	[Order(4)]
	public class TypeMap : BaseTest
	{
		const string _name = "lsp";
		[Fact]
		public void Insert()
		{
			var dt = new DateTime(2019, 12, 14, 18, 34, 45, 756, DateTimeKind.Local);
			var info = new TypeTestModel
			{
				Bit_type = false,
				Bool_type = false,
				Box_type = new NpgsqlTypes.NpgsqlBox(1D, 1D, 0D, 0D),
				Bytea_type = Encoding.UTF8.GetBytes(_name),
				Char_type = _name,
				Cidr_type = (IPAddress.Parse("127.0.0.1"), 32),
				Circle_type = new NpgsqlCircle(0D, 0D, 1D),
				Composite_type = new Info { Id = Guid.Empty, Name = _name },
				Date_type = DateTime.Now.Date,
				Id = Guid.NewGuid(),
				Decimal_type = 1.1M,
				Enum_type = EDataState.Õý³£,
				Float4_type = 1.1f,
				Float8_type = 1.1,
				Hstore_type = new Dictionary<string, string> { { "name", _name } },
				Inet_type = IPAddress.Parse("127.0.0.1"),
				Int2_type = 1,
				Int4_type = 2,
				Int8_type = 3,
				Interval_type = TimeSpan.FromDays(1),
				Jsonb_type = new JObject { { "name", _name } },
				Json_type = new JObject { { "name", _name } },
				Line_type = new NpgsqlLine(0D, 1D, 2D),
				Lseg_type = new NpgsqlLSeg(new NpgsqlPoint(0, 0), new NpgsqlPoint(1, 1)),
				Macaddr_type = System.Net.NetworkInformation.PhysicalAddress.Parse("44-45-53-54-00-00"),
				Money_type = 1.1M,
				Path_type = new NpgsqlPath(new NpgsqlPoint(0, 0), new NpgsqlPoint(1, 1)),
				Point_type = new NpgsqlPoint(0, 0),
				Polygon_type = new NpgsqlPolygon(new NpgsqlPoint(0, 0), new NpgsqlPoint(1, 1), new NpgsqlPoint(0, 2)),
				Serial2_type = 2,
				Serial4_type = 4,
				Serial8_type = 8,
				Text_type = _name,
				Timestamptz_type = dt,
				Timestamp_type = dt,
				Timetz_type = dt,
				Time_type = DateTime.Now - DateTime.Today,
				Tsquery_type = NpgsqlTsQuery.Parse(_name),
				Tsvector_type = NpgsqlTsVector.Parse(_name),
				Varbit_type = new System.Collections.BitArray(Encoding.UTF8.GetBytes(_name)),
				Varchar_type = _name,
				Xml_type = $"<summary>{_name}</summary>"
			}.Commit();
		}
		[Fact]
		public void Bit()
		{
			var affrows = TypeTest.Update(Guid.Empty).Set(a => a.Bit_type, false).ToRows();
			Assert.True(affrows > 0);
		}
		[Fact]
		public void BitLength()
		{
			var affrows = TypeTest.Update(Guid.Empty).Set(a => a.Bit_length_type, new BitArray(new byte[] { 0 })).ToRows();
			Assert.True(affrows > 0);
		}
		[Fact]
		public void Bool()
		{
			var affrows = TypeTest.Update(Guid.Empty).Set(a => a.Bool_type, false).ToRows();
			Assert.True(affrows > 0);
		}

		[Fact]
		public void Box()
		{
			var affrows = TypeTest.Update(Guid.Empty).Set(a => a.Box_type, new NpgsqlBox(1D, 1D, 0D, 0D)).ToRows();
			Assert.True(affrows > 0);
		}

		[Fact]
		public void Bytea()
		{
			var affrows = TypeTest.Update(Guid.Empty).Set(a => a.Bytea_type, Encoding.UTF8.GetBytes(_name)).ToRows();
			Assert.True(affrows > 0);
		}

		[Fact]
		public void Char()
		{
			var affrows = TypeTest.Update(Guid.Empty).Set(a => a.Char_type, "l").ToRows();
			Assert.True(affrows > 0);
		}

		[Fact]
		public void Cidr()
		{
			var affrows = TypeTest.Update(Guid.Empty).Set(a => a.Cidr_type, (IPAddress.Parse("127.0.0.1"), 32)).ToRows();
			Assert.True(affrows > 0);
		}

		[Fact]
		public void Circle()
		{
			var affrows = TypeTest.Update(Guid.Empty).Set(a => a.Circle_type, new NpgsqlCircle(0D, 0D, 1D)).ToRows();
			Assert.True(affrows > 0);
		}

		[Fact]
		public void Composite()
		{
			var affrows = TypeTest.Update(Guid.Empty).Set(a => a.Composite_type, new Info { Id = Guid.Empty, Name = _name }).ToRows();
			Assert.True(affrows > 0);
		}

		[Fact]
		public void Date()
		{
			var affrows = TypeTest.Update(Guid.Empty).SetIncrement(a => a.Date_type, TimeSpan.FromDays(3)).ToRows();
			affrows = TypeTest.Update(Guid.Empty).Set(a => a.Date_type, DateTime.Now).ToRows();
			Assert.True(affrows > 0);
		}

		[Fact]
		public void Uuid()
		{
			var affrows = TypeTest.Update(Guid.Empty).Set(a => a.Id, Guid.Empty).ToRows();
			Assert.True(affrows > 0);
		}
		[Fact]
		public void Decimal()
		{
			var affrows = TypeTest.Update(Guid.Empty).SetIncrement(a => a.Decimal_type, 1.2M).ToRows();
			affrows = TypeTest.Update(Guid.Empty).Set(a => a.Decimal_type, 1.2M).ToRows();
			Assert.True(affrows > 0);
		}

		[Fact]
		public void Enum()
		{
			var affrows = TypeTest.Update(Guid.Empty).Set(a => a.Enum_type, EDataState.ÒÑÉ¾³ý).ToRows();
			Assert.True(affrows > 0);
		}

		[Fact]
		public void Float4()
		{
			var affrows = TypeTest.Update(Guid.Empty).SetIncrement(a => a.Float4_type, 1.2f).ToRows();
			affrows = TypeTest.Update(Guid.Empty).Set(a => a.Float4_type, 1.2f).ToRows();
			Assert.True(affrows > 0);
		}

		[Fact]
		public void Float8()
		{
			var affrows = TypeTest.Update(Guid.Empty).SetIncrement(a => a.Float8_type, 1.3).ToRows();
			affrows = TypeTest.Update(Guid.Empty).Set(a => a.Float8_type, 1.3).ToRows();
			Assert.True(affrows > 0);
		}

		[Fact]
		public void Hstore()
		{
			var affrows = TypeTest.Update(Guid.Empty).Set(a => a.Hstore_type, new Dictionary<string, string> { { "name", _name } }).ToRows();
			Assert.True(affrows > 0);
		}

		[Fact]
		public void Inet()
		{
			var affrows = TypeTest.Update(Guid.Empty).Set(a => a.Inet_type, IPAddress.Parse("127.0.0.1")).ToRows();
			Assert.True(affrows > 0);
		}

		[Fact]
		public void Int2()
		{
			var affrows = TypeTest.Update(Guid.Empty).SetIncrement(a => a.Int2_type, 12).ToRows();
			affrows = TypeTest.Update(Guid.Empty).Set(a => a.Int2_type, 12).ToRows();
			Assert.True(affrows > 0);
		}

		[Fact]
		public void Int4()
		{
			var affrows = TypeTest.Update(Guid.Empty).SetIncrement(f => f.Int4_type, 1).ToRows();
			affrows = TypeTest.Update(Guid.Empty).Set(a => a.Int4_type, 23).ToRows();
			Assert.True(affrows > 0);
		}

		[Fact]
		public void Int8()
		{
			var affrows = TypeTest.Update(Guid.Empty).SetIncrement(a => a.Int8_type, 34, 0).ToRows();
			affrows = TypeTest.Update(Guid.Empty).SetBuilder(a => a.Int8_type, People.SelectDiy("count(1)")).ToRows();
			affrows = TypeTest.Update(Guid.Empty).Set(a => a.Int8_type, 34).ToRows();
			Assert.True(affrows > 0);
		}

		[Fact]
		public void Interval()
		{
			var affrows = TypeTest.Update(Guid.Empty).SetIncrement(a => a.Interval_type, TimeSpan.FromSeconds(22)).ToRows();
			affrows = TypeTest.Update(Guid.Empty).Set(a => a.Interval_type, TimeSpan.FromSeconds(22)).ToRows();
			Assert.True(affrows > 0);
		}

		[Fact]
		public void Jsonb()
		{
			var affrows = TypeTest.Update(Guid.Empty).Set(a => a.Jsonb_type, new JObject { { "name", _name } }).ToRows();
			Assert.True(affrows > 0);
		}

		[Fact]
		public void Json()
		{
			var affrows = TypeTest.Update(Guid.Empty).Set(a => a.Json_type, new JObject { { "name", _name } }).ToRows();
			Assert.True(affrows > 0);
		}

		[Fact]
		public void Line()
		{
			var affrows = TypeTest.Update(Guid.Empty).Set(a => a.Line_type, new NpgsqlLine(0D, 1D, 2D)).ToRows();
			Assert.True(affrows > 0);
		}

		[Fact]
		public void Lseg()
		{
			var affrows = TypeTest.Update(Guid.Empty).Set(a => a.Lseg_type, new NpgsqlLSeg(new NpgsqlPoint(0, 0), new NpgsqlPoint(1, 1))).ToRows();
			Assert.True(affrows > 0);
		}

		[Fact]
		public void Macaddr()
		{
			var affrows = TypeTest.Update(Guid.Empty).Set(a => a.Macaddr_type, System.Net.NetworkInformation.PhysicalAddress.Parse("44-45-53-54-00-00")).ToRows();
			Assert.True(affrows > 0);
		}

		[Fact]
		public void Money()
		{
			var affrows = TypeTest.Update(Guid.Empty).SetIncrement(a => a.Money_type, 12.3M).ToRows();
			affrows = TypeTest.Update(Guid.Empty).Set(a => a.Money_type, 12.3M).ToRows();
			Assert.True(affrows > 0);
		}

		[Fact]
		public void Path()
		{
			var affrows = TypeTest.Update(Guid.Empty).Set(a => a.Path_type, new NpgsqlPath(new NpgsqlPoint(0, 0), new NpgsqlPoint(1, 1))).ToRows();
			Assert.True(affrows > 0);
		}

		[Fact]
		public void Point()
		{
			var affrows = TypeTest.Update(Guid.Empty).Set(a => a.Point_type, new NpgsqlPoint(0, 0)).ToRows();
			Assert.True(affrows > 0);
		}

		[Fact]
		public void Polygon()
		{
			var affrows = TypeTest.Update(Guid.Empty).Set(a => a.Polygon_type, new NpgsqlPolygon(new NpgsqlPoint(0, 0), new NpgsqlPoint(1, 1), new NpgsqlPoint(0, 2))).ToRows();
			Assert.True(affrows > 0);
		}

		[Fact]
		public void Serial2()
		{
			var affrows = TypeTest.Update(Guid.Empty).SetIncrement(a => a.Serial2_type, 12).ToRows();
			affrows = TypeTest.Update(Guid.Empty).Set(a => a.Serial2_type, 12).ToRows();
			Assert.True(affrows > 0);
		}

		[Fact]
		public void Serial4()
		{
			var affrows = TypeTest.Update(Guid.Empty).SetIncrement(a => a.Serial4_type, 23).ToRows();
			affrows = TypeTest.Update(Guid.Empty).Set(a => a.Serial4_type, 23).ToRows();
			Assert.True(affrows > 0);
		}

		[Fact]
		public void Serial8()
		{
			var affrows = TypeTest.Update(Guid.Empty).SetIncrement(a => a.Serial8_type, 33).ToRows();
			affrows = TypeTest.Update(Guid.Empty).Set(a => a.Serial8_type, 33).ToRows();
			Assert.True(affrows > 0);
		}

		[Fact]
		public void Text()
		{
			var affrows = TypeTest.Update(Guid.Empty).Set(a => a.Text_type, _name).ToRows();
			Assert.True(affrows > 0);
		}

		[Fact]
		public void Timestamptz()
		{
			var affrows = TypeTest.Update(Guid.Empty).SetIncrement(a => a.Timestamptz_type, TimeSpan.FromDays(17)).ToRows();
			affrows = TypeTest.Update(Guid.Empty).Set(a => a.Timestamptz_type, DateTime.Now).ToRows();
			Assert.True(affrows > 0);
		}

		[Fact]
		public void Timestamp()
		{
			var affrows = TypeTest.Update(Guid.Empty).SetIncrement(f => f.Timestamp_type, TimeSpan.FromSeconds(10), DateTime.Now).ToRows();
			affrows = TypeTest.Update(Guid.Empty).Set(a => a.Timestamp_type, DateTime.Now).ToRows();
			Assert.True(affrows > 0);
		}

		[Fact]
		public void Timetz()
		{
			var affrows = TypeTest.Update(Guid.Empty).SetIncrement(a => a.Timetz_type, TimeSpan.FromSeconds(10)).ToRows();
			affrows = TypeTest.Update(Guid.Empty).Set(a => a.Timetz_type, DateTime.Now).ToRows();
			Assert.True(affrows > 0);
		}

		[Fact]
		public void Time()
		{
			var affrows = TypeTest.Update(Guid.Empty).SetIncrement(a => a.Time_type, TimeSpan.FromSeconds(22)).ToRows();
			affrows = TypeTest.Update(Guid.Empty).Set(a => a.Time_type, TimeSpan.FromSeconds(22)).ToRows();
			Assert.True(affrows > 0);
		}

		[Fact]
		public void Tsquery()
		{
			var affrows = TypeTest.Update(Guid.Empty).Set(a => a.Tsquery_type, NpgsqlTsQuery.Parse(_name)).ToRows();
			Assert.True(affrows > 0);
		}

		[Fact]
		public void Tsvector()
		{
			var affrows = TypeTest.Update(Guid.Empty).Set(a => a.Tsvector_type, NpgsqlTsVector.Parse(_name)).ToRows();
			Assert.True(affrows > 0);
		}

		[Fact]
		public void Varbit()
		{
			var affrows = TypeTest.Update(Guid.Empty).Set(a => a.Varbit_type, new System.Collections.BitArray(Encoding.UTF8.GetBytes(_name))).ToRows();
			Assert.True(affrows > 0);
		}

		[Fact]
		public void Varchar()
		{
			var affrows = TypeTest.Update(Guid.Empty).Set(a => a.Varchar_type, _name).ToRows();
			Assert.True(affrows > 0);
		}

		[Fact]
		public void Xml()
		{
			var affrows = TypeTest.Update(Guid.Empty).Set(a => a.Xml_type, $"<summary>{_name}</summary>").ToRows();
			Assert.True(affrows > 0);

		}
		[Fact]
		public void Array()
		{
			var affrows = TypeTest.Update(Guid.Empty).SetAppend(a => a.Array_type, 1, 1, 2, 3).ToRows();
			affrows = TypeTest.Update(Guid.Empty).SetRemove(a => a.Array_type, 1).ToRows();
			Assert.True(affrows > 0);

		}
		[Fact]
		public void Read()
		{
			var info = TypeTest.GetItem(Guid.Empty);
			info = TypeTest.GetItem(Guid.Empty);
		}

	}
}

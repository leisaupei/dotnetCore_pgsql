using Meta.Common.Model;
using System;
using System.Collections.Generic;
using System.Collections;
using System.Net.NetworkInformation;
using NpgsqlTypes;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Meta.Common.Interface;
using System.Net;
using Meta.xUnitTest.DAL;

namespace Meta.xUnitTest.Model
{
	[DbTable("type_test")]
	public partial class TypeTestModel : IDbModel
	{
		#region Properties
		[JsonProperty, DbField(16, NpgsqlDbType.Uuid)]
		public Guid Id { get; set; }
		[JsonProperty, DbField(1, NpgsqlDbType.Bit)]
		public bool? Bit_type { get; set; }
		[JsonProperty, DbField(1, NpgsqlDbType.Boolean)]
		public bool? Bool_type { get; set; }
		[JsonProperty, DbField(32, NpgsqlDbType.Box)]
		public NpgsqlBox? Box_type { get; set; }
		[JsonProperty, DbField(-1, NpgsqlDbType.Bytea)]
		public byte[] Bytea_type { get; set; }
		[JsonProperty, DbField(1, NpgsqlDbType.Char)]
		public string Char_type { get; set; }
		[JsonProperty, DbField(-1, NpgsqlDbType.Cidr)]
		public (IPAddress, int)? Cidr_type { get; set; }
		[JsonProperty, DbField(24, NpgsqlDbType.Circle)]
		public NpgsqlCircle? Circle_type { get; set; }
		[JsonProperty, DbField(4, NpgsqlDbType.Date)]
		public DateTime? Date_type { get; set; }
		[JsonProperty, DbField(-1, NpgsqlDbType.Numeric)]
		public decimal? Decimal_type { get; set; }
		[JsonProperty, DbField(4, NpgsqlDbType.Real)]
		public float? Float4_type { get; set; }
		[JsonProperty, DbField(8, NpgsqlDbType.Double)]
		public double? Float8_type { get; set; }
		[JsonProperty, DbField(-1, NpgsqlDbType.Inet)]
		public IPAddress Inet_type { get; set; }
		[JsonProperty, DbField(2, NpgsqlDbType.Smallint)]
		public short? Int2_type { get; set; }
		[JsonProperty, DbField(4, NpgsqlDbType.Integer)]
		public int? Int4_type { get; set; }
		[JsonProperty, DbField(8, NpgsqlDbType.Bigint)]
		public long? Int8_type { get; set; }
		[JsonProperty, DbField(16, NpgsqlDbType.Interval)]
		public TimeSpan? Interval_type { get; set; }
		[JsonProperty, DbField(-1, NpgsqlDbType.Json)]
		public JToken Json_type { get; set; }
		[JsonProperty, DbField(-1, NpgsqlDbType.Jsonb)]
		public JToken Jsonb_type { get; set; }
		[JsonProperty, DbField(24, NpgsqlDbType.Line)]
		public NpgsqlLine? Line_type { get; set; }
		[JsonProperty, DbField(32, NpgsqlDbType.LSeg)]
		public NpgsqlLSeg? Lseg_type { get; set; }
		[JsonProperty, DbField(6, NpgsqlDbType.MacAddr)]
		public PhysicalAddress Macaddr_type { get; set; }
		[JsonProperty, DbField(8, NpgsqlDbType.Money)]
		public decimal? Money_type { get; set; }
		[JsonProperty, DbField(-1, NpgsqlDbType.Path)]
		public NpgsqlPath? Path_type { get; set; }
		[JsonProperty, DbField(16, NpgsqlDbType.Point)]
		public NpgsqlPoint? Point_type { get; set; }
		[JsonProperty, DbField(-1, NpgsqlDbType.Polygon)]
		public NpgsqlPolygon? Polygon_type { get; set; }
		[JsonProperty, DbField(2, NpgsqlDbType.Smallint)]
		public short Serial2_type { get; set; }
		[JsonProperty, DbField(4, NpgsqlDbType.Integer)]
		public int Serial4_type { get; set; }
		[JsonProperty, DbField(8, NpgsqlDbType.Bigint)]
		public long Serial8_type { get; set; }
		[JsonProperty, DbField(-1, NpgsqlDbType.Text)]
		public string Text_type { get; set; }
		[JsonProperty, DbField(8, NpgsqlDbType.Time)]
		public TimeSpan? Time_type { get; set; }
		[JsonProperty, DbField(8, NpgsqlDbType.Timestamp)]
		public DateTime? Timestamp_type { get; set; }
		[JsonProperty, DbField(8, NpgsqlDbType.TimestampTz)]
		public DateTime? Timestamptz_type { get; set; }
		[JsonProperty, DbField(12, NpgsqlDbType.TimeTz)]
		public DateTimeOffset? Timetz_type { get; set; }
		[JsonProperty, DbField(-1, NpgsqlDbType.TsQuery)]
		public NpgsqlTsQuery Tsquery_type { get; set; }
		[JsonProperty, DbField(-1, NpgsqlDbType.TsVector)]
		public NpgsqlTsVector Tsvector_type { get; set; }
		[JsonProperty, DbField(-1, NpgsqlDbType.Varbit)]
		public BitArray Varbit_type { get; set; }
		[JsonProperty, DbField(-1, NpgsqlDbType.Varchar)]
		public string Varchar_type { get; set; }
		[JsonProperty, DbField(-1, NpgsqlDbType.Xml)]
		public string Xml_type { get; set; }
		[JsonProperty, DbField(-1, NpgsqlDbType.Hstore)]
		public Dictionary<string, string> Hstore_type { get; set; }
		[JsonProperty, DbField(4)]
		public EDataState? Enum_type { get; set; }
		[JsonProperty, DbField(-1)]
		public Info Composite_type { get; set; }
		[JsonProperty, DbField(8, NpgsqlDbType.Bit)]
		public BitArray Bit_length_type { get; set; }
		[JsonProperty, DbField(-1, NpgsqlDbType.Integer | NpgsqlDbType.Array)]
		public int[] Array_type { get; set; }
		#endregion

		#region Update/Insert
		public TypeTest.TypeTestUpdateBuilder Update => DAL.TypeTest.Update(this);

		public int Delete() => DAL.TypeTest.Delete(this);
		public int Commit() => DAL.TypeTest.Commit(this);
		public TypeTestModel Insert() => DAL.TypeTest.Insert(this);
		#endregion
	}
}

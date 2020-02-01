using Meta.Common.Model;
using System;
using System.Collections.Generic;
using System.Collections;
using System.Net.NetworkInformation;
using NpgsqlTypes;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Meta.Common.Interface;
using System.Xml;
using System.Net;
using System.Threading.Tasks;
using System.Threading;
using Meta.Common.SqlBuilder;
using Meta.xUnitTest.DAL;

namespace Meta.xUnitTest.Model
{
	[DbTable("type_test")]
	public partial class TypeTestModel : IDbModel
	{
		#region Properties
		[JsonProperty] public Guid Id { get; set; }
		[JsonProperty] public BitArray Bit_type { get; set; }
		[JsonProperty] public bool? Bool_type { get; set; }
		[JsonProperty] public NpgsqlBox? Box_type { get; set; }
		[JsonProperty] public byte[] Bytea_type { get; set; }
		[JsonProperty] public string Char_type { get; set; }
		[JsonProperty] public (IPAddress, int)? Cidr_type { get; set; }
		[JsonProperty] public NpgsqlCircle? Circle_type { get; set; }
		[JsonProperty] public DateTime? Date_type { get; set; }
		[JsonProperty] public decimal? Decimal_type { get; set; }
		[JsonProperty] public float? Float4_type { get; set; }
		[JsonProperty] public double? Float8_type { get; set; }
		[JsonProperty] public IPAddress Inet_type { get; set; }
		[JsonProperty] public short? Int2_type { get; set; }
		[JsonProperty] public int? Int4_type { get; set; }
		[JsonProperty] public long? Int8_type { get; set; }
		[JsonProperty] public TimeSpan? Interval_type { get; set; }
		[JsonProperty] public JToken Json_type { get; set; }
		[JsonProperty] public JToken Jsonb_type { get; set; }
		[JsonProperty] public NpgsqlLine? Line_type { get; set; }
		[JsonProperty] public NpgsqlLSeg? Lseg_type { get; set; }
		[JsonProperty] public PhysicalAddress Macaddr_type { get; set; }
		[JsonProperty] public decimal? Money_type { get; set; }
		[JsonProperty] public NpgsqlPath? Path_type { get; set; }
		[JsonProperty] public NpgsqlPoint? Point_type { get; set; }
		[JsonProperty] public NpgsqlPolygon? Polygon_type { get; set; }
		[JsonProperty] public string Text_type { get; set; }
		[JsonProperty] public TimeSpan? Time_type { get; set; }
		[JsonProperty] public DateTime? Timestamp_type { get; set; }
		[JsonProperty] public DateTime? Timestamptz_type { get; set; }
		[JsonProperty] public DateTimeOffset? Timetz_type { get; set; }
		[JsonProperty] public NpgsqlTsQuery Tsquery_type { get; set; }
		[JsonProperty] public NpgsqlTsVector Tsvector_type { get; set; }
		[JsonProperty] public BitArray Varbit_type { get; set; }
		[JsonProperty] public string Varchar_type { get; set; }
		[JsonProperty] public XmlDocument Xml_type { get; set; }
		[JsonProperty] public Dictionary<string, string> Hstore_type { get; set; }
		[JsonProperty] public EDataState? Enum_type { get; set; }
		[JsonProperty] public Info Composite_type { get; set; }
		[JsonProperty] public BitArray Bit_length_type { get; set; }
		[JsonProperty] public int[] Array_type { get; set; }
		[JsonProperty] public short Serial2_type { get; set; }
		[JsonProperty] public int Serial4_type { get; set; }
		[JsonProperty] public long Serial8_type { get; set; }
		[JsonProperty] public Guid[] Uuid_array_type { get; set; }
		#endregion

		#region Update/Insert
		public UpdateBuilder<TypeTestModel> Update => DAL.TypeTest.Update(this.Id);

		public int Commit() => DAL.TypeTest.Commit(this);
		public TypeTestModel Insert() => DAL.TypeTest.Insert(this);
		public ValueTask<int> CommitAsync(CancellationToken cancellationToken = default) => DAL.TypeTest.CommitAsync(this, cancellationToken);
		public Task<TypeTestModel> InsertAsync(CancellationToken cancellationToken = default) => DAL.TypeTest.InsertAsync(this, cancellationToken);
		#endregion
	}
}

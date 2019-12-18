using Meta.Common.SqlBuilder;
using Meta.Common.Model;
using Meta.xUnitTest.Model;
using Meta.xUnitTest.Options;
using System.Collections;
using System.Net.NetworkInformation;
using NpgsqlTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using System.Net;

namespace Meta.xUnitTest.DAL
{
	[DbTable("type_test")]
	public sealed partial class TypeTest : SelectBuilder<TypeTest, TypeTestModel>
	{
		#region Properties
		public const string CacheKey = "meta_xunittest_model_typetestmodel_{0}";
		private TypeTest() { }
		public static TypeTest Select => new TypeTest();
		public static TypeTest SelectDiy(string fields) => new TypeTest { Fields = fields };
		public static TypeTest SelectDiy(string fields, string alias) => new TypeTest { Fields = fields, MainAlias = alias };
		public static TypeTestUpdateBuilder UpdateDiy => new TypeTestUpdateBuilder();
		public static DeleteBuilder DeleteDiy => new DeleteBuilder("type_test");
		public static InsertBuilder InsertDiy => new InsertBuilder("type_test");
		#endregion

		#region Delete
		public static int Delete(TypeTestModel model) => Delete(new[] { model.Id });
		public static int Delete(Guid id) => Delete(new[] { id });
		public static int Delete(IEnumerable<TypeTestModel> models) => Delete(models.Select(a => a.Id));
		public static int Delete(IEnumerable<Guid> ids)
		{
			if (ids == null)
				throw new ArgumentNullException(nameof(ids));
			RedisHelper.Del(ids.Select(f => string.Format(CacheKey, f)).ToArray());
			return DeleteDiy.WhereOr("id = {0}", ids, NpgsqlDbType.Uuid).ToRows();
		}
		#endregion

		#region Insert
		public static int Commit(TypeTestModel model) => SetRedisCache(string.Format(CacheKey, model.Id), model, DbConfig.DbCacheTimeOut, () => GetInsertBuilder(model).ToRows());
		public static TypeTestModel Insert(TypeTestModel model)
		{
			SetRedisCache(string.Format(CacheKey, model.Id), model, DbConfig.DbCacheTimeOut, () => GetInsertBuilder(model).ToRows(ref model));
			return model;
		}
		private static InsertBuilder GetInsertBuilder(TypeTestModel model)
		{
			if (model == null)
				throw new ArgumentNullException(nameof(model));
			return InsertDiy
				.Set("id", model.Id = model.Id == Guid.Empty ? Guid.NewGuid() : model.Id, 16, NpgsqlDbType.Uuid)
				.Set("bit_type", model.Bit_type, 1, NpgsqlDbType.Bit)
				.Set("bool_type", model.Bool_type, 1, NpgsqlDbType.Boolean)
				.Set("box_type", model.Box_type, 32, NpgsqlDbType.Box)
				.Set("bytea_type", model.Bytea_type, -1, NpgsqlDbType.Bytea)
				.Set("char_type", model.Char_type, 1, NpgsqlDbType.Char)
				.Set("cidr_type", model.Cidr_type, -1, NpgsqlDbType.Cidr)
				.Set("circle_type", model.Circle_type, 24, NpgsqlDbType.Circle)
				.Set("date_type", model.Date_type, 4, NpgsqlDbType.Date)
				.Set("decimal_type", model.Decimal_type, -1, NpgsqlDbType.Numeric)
				.Set("float4_type", model.Float4_type, 4, NpgsqlDbType.Real)
				.Set("float8_type", model.Float8_type, 8, NpgsqlDbType.Double)
				.Set("inet_type", model.Inet_type, -1, NpgsqlDbType.Inet)
				.Set("int2_type", model.Int2_type, 2, NpgsqlDbType.Smallint)
				.Set("int4_type", model.Int4_type, 4, NpgsqlDbType.Integer)
				.Set("int8_type", model.Int8_type, 8, NpgsqlDbType.Bigint)
				.Set("interval_type", model.Interval_type, 16, NpgsqlDbType.Interval)
				.Set("json_type", model.Json_type ??= JToken.Parse("{}"), -1, NpgsqlDbType.Json)
				.Set("jsonb_type", model.Jsonb_type ??= JToken.Parse("{}"), -1, NpgsqlDbType.Jsonb)
				.Set("line_type", model.Line_type, 24, NpgsqlDbType.Line)
				.Set("lseg_type", model.Lseg_type, 32, NpgsqlDbType.LSeg)
				.Set("macaddr_type", model.Macaddr_type, 6, NpgsqlDbType.MacAddr)
				.Set("money_type", model.Money_type, 8, NpgsqlDbType.Money)
				.Set("path_type", model.Path_type, -1, NpgsqlDbType.Path)
				.Set("point_type", model.Point_type, 16, NpgsqlDbType.Point)
				.Set("polygon_type", model.Polygon_type, -1, NpgsqlDbType.Polygon)
				.Set("serial2_type", model.Serial2_type, 2, NpgsqlDbType.Smallint)
				.Set("serial4_type", model.Serial4_type, 4, NpgsqlDbType.Integer)
				.Set("serial8_type", model.Serial8_type, 8, NpgsqlDbType.Bigint)
				.Set("text_type", model.Text_type, -1, NpgsqlDbType.Text)
				.Set("time_type", model.Time_type, 8, NpgsqlDbType.Time)
				.Set("timestamp_type", model.Timestamp_type, 8, NpgsqlDbType.Timestamp)
				.Set("timestamptz_type", model.Timestamptz_type, 8, NpgsqlDbType.TimestampTz)
				.Set("timetz_type", model.Timetz_type, 12, NpgsqlDbType.TimeTz)
				.Set("tsquery_type", model.Tsquery_type, -1, NpgsqlDbType.TsQuery)
				.Set("tsvector_type", model.Tsvector_type, -1, NpgsqlDbType.TsVector)
				.Set("varbit_type", model.Varbit_type, -1, NpgsqlDbType.Varbit)
				.Set("varchar_type", model.Varchar_type, -1, NpgsqlDbType.Varchar)
				.Set("xml_type", model.Xml_type, -1, NpgsqlDbType.Xml)
				.Set("hstore_type", model.Hstore_type, -1, NpgsqlDbType.Hstore)
				.Set("enum_type", model.Enum_type, 4)
				.Set("composite_type", model.Composite_type, -1)
				.Set("bit_length_type", model.Bit_length_type, 8, NpgsqlDbType.Bit)
				.Set("array_type", model.Array_type, -1, NpgsqlDbType.Integer | NpgsqlDbType.Array);
		}
		#endregion

		#region Select
		public static TypeTestModel GetItem(Guid id) => GetRedisCache(string.Format(CacheKey, id), DbConfig.DbCacheTimeOut, () => Select.WhereId(id).ToOne());
		public static List<TypeTestModel> GetItems(IEnumerable<Guid> id) => Select.WhereId(id.ToArray()).ToList();
		public TypeTest WhereId(params Guid[] id) => WhereOr($"{MainAlias}.id = {{0}}", id, NpgsqlDbType.Uuid);
		public TypeTest WhereBit_type(params bool?[] bit_type) => WhereOr($"{MainAlias}.bit_type = {{0}}", bit_type, NpgsqlDbType.Bit);
		public TypeTest WhereBit_type(params bool[] bit_type) => WhereOr($"{MainAlias}.bit_type = {{0}}", bit_type, NpgsqlDbType.Bit);
		public TypeTest WhereBool_type(params bool?[] bool_type) => WhereOr($"{MainAlias}.bool_type = {{0}}", bool_type, NpgsqlDbType.Boolean);
		public TypeTest WhereBool_type(params bool[] bool_type) => WhereOr($"{MainAlias}.bool_type = {{0}}", bool_type, NpgsqlDbType.Boolean);
		public TypeTest WhereBox_type(params NpgsqlBox?[] box_type) => WhereOr($"{MainAlias}.box_type = {{0}}", box_type, NpgsqlDbType.Box);
		public TypeTest WhereBox_type(params NpgsqlBox[] box_type) => WhereOr($"{MainAlias}.box_type = {{0}}", box_type, NpgsqlDbType.Box);
		public TypeTest WhereBytea_type(byte[] bytea_type) => WhereArray("{MainAlias}.bytea_type = {{0}}", bytea_type, NpgsqlDbType.Bytea);
		public TypeTest WhereChar_type(params string[] char_type) => WhereOr($"{MainAlias}.char_type = {{0}}", char_type, NpgsqlDbType.Char);
		public TypeTest WhereChar_typeLike(params string[] char_type) => WhereOr($"{MainAlias}.char_type LIKE {{0}}", char_type.Select(a => $"%{a}%"), NpgsqlDbType.Char);
		public TypeTest WhereCidr_type(params (IPAddress, int)?[] cidr_type) => WhereOr($"{MainAlias}.cidr_type = {{0}}", cidr_type, NpgsqlDbType.Cidr);
		public TypeTest WhereCidr_type(params (IPAddress, int)[] cidr_type) => WhereOr($"{MainAlias}.cidr_type = {{0}}", cidr_type, NpgsqlDbType.Cidr);
		public TypeTest WhereCircle_type(params NpgsqlCircle?[] circle_type) => WhereOr($"{MainAlias}.circle_type = {{0}}", circle_type, NpgsqlDbType.Circle);
		public TypeTest WhereCircle_type(params NpgsqlCircle[] circle_type) => WhereOr($"{MainAlias}.circle_type = {{0}}", circle_type, NpgsqlDbType.Circle);
		public TypeTest WhereDate_type(params DateTime?[] date_type) => WhereOr($"{MainAlias}.date_type = {{0}}", date_type, NpgsqlDbType.Date);
		public TypeTest WhereDate_type(params DateTime[] date_type) => WhereOr($"{MainAlias}.date_type = {{0}}", date_type, NpgsqlDbType.Date);
		public TypeTest WhereDate_typeRange(DateTime? begin = null, DateTime? end = null) => Where($"{MainAlias}.date_type BETWEEN {{0}} AND {{1}}", begin ?? DateTime.Parse("1970-1-1"), end ?? DateTime.Now);
		public TypeTest WhereDecimal_type(params decimal?[] decimal_type) => WhereOr($"{MainAlias}.decimal_type = {{0}}", decimal_type, NpgsqlDbType.Numeric);
		public TypeTest WhereDecimal_type(params decimal[] decimal_type) => WhereOr($"{MainAlias}.decimal_type = {{0}}", decimal_type, NpgsqlDbType.Numeric);
		public TypeTest WhereDecimal_typeThan(decimal val, string sqlOperator = ">") => Where($"{MainAlias}.decimal_type {sqlOperator} {{0}}", new DbTypeValue(val, NpgsqlDbType.Numeric));
		public TypeTest WhereFloat4_type(params float?[] float4_type) => WhereOr($"{MainAlias}.float4_type = {{0}}", float4_type, NpgsqlDbType.Real);
		public TypeTest WhereFloat4_type(params float[] float4_type) => WhereOr($"{MainAlias}.float4_type = {{0}}", float4_type, NpgsqlDbType.Real);
		public TypeTest WhereFloat4_typeThan(float val, string sqlOperator = ">") => Where($"{MainAlias}.float4_type {sqlOperator} {{0}}", new DbTypeValue(val, NpgsqlDbType.Real));
		public TypeTest WhereFloat8_type(params double?[] float8_type) => WhereOr($"{MainAlias}.float8_type = {{0}}", float8_type, NpgsqlDbType.Double);
		public TypeTest WhereFloat8_type(params double[] float8_type) => WhereOr($"{MainAlias}.float8_type = {{0}}", float8_type, NpgsqlDbType.Double);
		public TypeTest WhereFloat8_typeThan(double val, string sqlOperator = ">") => Where($"{MainAlias}.float8_type {sqlOperator} {{0}}", new DbTypeValue(val, NpgsqlDbType.Double));
		public TypeTest WhereInet_type(params IPAddress[] inet_type) => WhereOr($"{MainAlias}.inet_type = {{0}}", inet_type, NpgsqlDbType.Inet);
		public TypeTest WhereInt2_type(params short?[] int2_type) => WhereOr($"{MainAlias}.int2_type = {{0}}", int2_type, NpgsqlDbType.Smallint);
		public TypeTest WhereInt2_type(params short[] int2_type) => WhereOr($"{MainAlias}.int2_type = {{0}}", int2_type, NpgsqlDbType.Smallint);
		public TypeTest WhereInt2_typeThan(short val, string sqlOperator = ">") => Where($"{MainAlias}.int2_type {sqlOperator} {{0}}", new DbTypeValue(val, NpgsqlDbType.Smallint));
		public TypeTest WhereInt4_type(params int?[] int4_type) => WhereOr($"{MainAlias}.int4_type = {{0}}", int4_type, NpgsqlDbType.Integer);
		public TypeTest WhereInt4_type(params int[] int4_type) => WhereOr($"{MainAlias}.int4_type = {{0}}", int4_type, NpgsqlDbType.Integer);
		public TypeTest WhereInt4_typeThan(int val, string sqlOperator = ">") => Where($"{MainAlias}.int4_type {sqlOperator} {{0}}", new DbTypeValue(val, NpgsqlDbType.Integer));
		public TypeTest WhereInt8_type(params long?[] int8_type) => WhereOr($"{MainAlias}.int8_type = {{0}}", int8_type, NpgsqlDbType.Bigint);
		public TypeTest WhereInt8_type(params long[] int8_type) => WhereOr($"{MainAlias}.int8_type = {{0}}", int8_type, NpgsqlDbType.Bigint);
		public TypeTest WhereInt8_typeThan(long val, string sqlOperator = ">") => Where($"{MainAlias}.int8_type {sqlOperator} {{0}}", new DbTypeValue(val, NpgsqlDbType.Bigint));
		public TypeTest WhereInterval_type(params TimeSpan?[] interval_type) => WhereOr($"{MainAlias}.interval_type = {{0}}", interval_type, NpgsqlDbType.Interval);
		public TypeTest WhereInterval_type(params TimeSpan[] interval_type) => WhereOr($"{MainAlias}.interval_type = {{0}}", interval_type, NpgsqlDbType.Interval);
		public TypeTest WhereInterval_typeRange(TimeSpan? begin = null, TimeSpan? end = null) => Where($"{MainAlias}.interval_type BETWEEN {{0}} AND {{1}}", begin ?? TimeSpan.MinValue, end ?? TimeSpan.MaxValue);
		public TypeTest WhereLine_type(params NpgsqlLine?[] line_type) => WhereOr($"{MainAlias}.line_type = {{0}}", line_type, NpgsqlDbType.Line);
		public TypeTest WhereLine_type(params NpgsqlLine[] line_type) => WhereOr($"{MainAlias}.line_type = {{0}}", line_type, NpgsqlDbType.Line);
		public TypeTest WhereLseg_type(params NpgsqlLSeg?[] lseg_type) => WhereOr($"{MainAlias}.lseg_type = {{0}}", lseg_type, NpgsqlDbType.LSeg);
		public TypeTest WhereLseg_type(params NpgsqlLSeg[] lseg_type) => WhereOr($"{MainAlias}.lseg_type = {{0}}", lseg_type, NpgsqlDbType.LSeg);
		public TypeTest WhereMacaddr_type(params PhysicalAddress[] macaddr_type) => WhereOr($"{MainAlias}.macaddr_type = {{0}}", macaddr_type, NpgsqlDbType.MacAddr);
		public TypeTest WhereMoney_type(params decimal?[] money_type) => WhereOr($"{MainAlias}.money_type = {{0}}", money_type, NpgsqlDbType.Money);
		public TypeTest WhereMoney_type(params decimal[] money_type) => WhereOr($"{MainAlias}.money_type = {{0}}", money_type, NpgsqlDbType.Money);
		public TypeTest WhereMoney_typeThan(decimal val, string sqlOperator = ">") => Where($"{MainAlias}.money_type {sqlOperator} {{0}}", new DbTypeValue(val, NpgsqlDbType.Money));
		public TypeTest WherePath_type(params NpgsqlPath?[] path_type) => WhereOr($"{MainAlias}.path_type = {{0}}", path_type, NpgsqlDbType.Path);
		public TypeTest WherePath_type(params NpgsqlPath[] path_type) => WhereOr($"{MainAlias}.path_type = {{0}}", path_type, NpgsqlDbType.Path);
		public TypeTest WherePoint_type(params NpgsqlPoint?[] point_type) => WhereOr($"{MainAlias}.point_type = {{0}}", point_type, NpgsqlDbType.Point);
		public TypeTest WherePoint_type(params NpgsqlPoint[] point_type) => WhereOr($"{MainAlias}.point_type = {{0}}", point_type, NpgsqlDbType.Point);
		public TypeTest WherePolygon_type(params NpgsqlPolygon?[] polygon_type) => WhereOr($"{MainAlias}.polygon_type = {{0}}", polygon_type, NpgsqlDbType.Polygon);
		public TypeTest WherePolygon_type(params NpgsqlPolygon[] polygon_type) => WhereOr($"{MainAlias}.polygon_type = {{0}}", polygon_type, NpgsqlDbType.Polygon);
		public TypeTest WhereSerial2_type(params short[] serial2_type) => WhereOr($"{MainAlias}.serial2_type = {{0}}", serial2_type, NpgsqlDbType.Smallint);
		public TypeTest WhereSerial2_typeThan(short val, string sqlOperator = ">") => Where($"{MainAlias}.serial2_type {sqlOperator} {{0}}", new DbTypeValue(val, NpgsqlDbType.Smallint));
		public TypeTest WhereSerial4_type(params int[] serial4_type) => WhereOr($"{MainAlias}.serial4_type = {{0}}", serial4_type, NpgsqlDbType.Integer);
		public TypeTest WhereSerial4_typeThan(int val, string sqlOperator = ">") => Where($"{MainAlias}.serial4_type {sqlOperator} {{0}}", new DbTypeValue(val, NpgsqlDbType.Integer));
		public TypeTest WhereSerial8_type(params long[] serial8_type) => WhereOr($"{MainAlias}.serial8_type = {{0}}", serial8_type, NpgsqlDbType.Bigint);
		public TypeTest WhereSerial8_typeThan(long val, string sqlOperator = ">") => Where($"{MainAlias}.serial8_type {sqlOperator} {{0}}", new DbTypeValue(val, NpgsqlDbType.Bigint));
		public TypeTest WhereText_type(params string[] text_type) => WhereOr($"{MainAlias}.text_type = {{0}}", text_type, NpgsqlDbType.Text);
		public TypeTest WhereText_typeLike(params string[] text_type) => WhereOr($"{MainAlias}.text_type LIKE {{0}}", text_type.Select(a => $"%{a}%"), NpgsqlDbType.Text);
		public TypeTest WhereTime_type(params TimeSpan?[] time_type) => WhereOr($"{MainAlias}.time_type = {{0}}", time_type, NpgsqlDbType.Time);
		public TypeTest WhereTime_type(params TimeSpan[] time_type) => WhereOr($"{MainAlias}.time_type = {{0}}", time_type, NpgsqlDbType.Time);
		public TypeTest WhereTime_typeRange(TimeSpan? begin = null, TimeSpan? end = null) => Where($"{MainAlias}.time_type BETWEEN {{0}} AND {{1}}", begin ?? TimeSpan.MinValue, end ?? TimeSpan.MaxValue);
		public TypeTest WhereTimestamp_typeRange(DateTime? begin = null, DateTime? end = null) => Where($"{MainAlias}.timestamp_type BETWEEN {{0}} AND {{1}}", begin ?? DateTime.Parse("1970-1-1"), end ?? DateTime.Now);
		public TypeTest WhereTimestamptz_typeRange(DateTime? begin = null, DateTime? end = null) => Where($"{MainAlias}.timestamptz_type BETWEEN {{0}} AND {{1}}", begin ?? DateTime.Parse("1970-1-1"), end ?? DateTime.Now);
		public TypeTest WhereTimetz_type(params DateTimeOffset?[] timetz_type) => WhereOr($"{MainAlias}.timetz_type = {{0}}", timetz_type, NpgsqlDbType.TimeTz);
		public TypeTest WhereTimetz_type(params DateTimeOffset[] timetz_type) => WhereOr($"{MainAlias}.timetz_type = {{0}}", timetz_type, NpgsqlDbType.TimeTz);
		public TypeTest WhereTsquery_type(params NpgsqlTsQuery[] tsquery_type) => WhereOr($"{MainAlias}.tsquery_type = {{0}}", tsquery_type, NpgsqlDbType.TsQuery);
		public TypeTest WhereTsvector_type(params NpgsqlTsVector[] tsvector_type) => WhereOr($"{MainAlias}.tsvector_type = {{0}}", tsvector_type, NpgsqlDbType.TsVector);
		public TypeTest WhereVarbit_type(params BitArray[] varbit_type) => WhereOr($"{MainAlias}.varbit_type = {{0}}", varbit_type, NpgsqlDbType.Varbit);
		public TypeTest WhereVarchar_type(params string[] varchar_type) => WhereOr($"{MainAlias}.varchar_type = {{0}}", varchar_type, NpgsqlDbType.Varchar);
		public TypeTest WhereVarchar_typeLike(params string[] varchar_type) => WhereOr($"{MainAlias}.varchar_type LIKE {{0}}", varchar_type.Select(a => $"%{a}%"), NpgsqlDbType.Varchar);
		public TypeTest WhereHstore_type(params Dictionary<string, string>[] hstore_type) => WhereOr($"{MainAlias}.hstore_type = {{0}}", hstore_type, NpgsqlDbType.Hstore);
		public TypeTest WhereEnum_type(params EDataState?[] enum_type) => WhereOr($"{MainAlias}.enum_type = {{0}}", enum_type);
		public TypeTest WhereEnum_type(params EDataState[] enum_type) => WhereOr($"{MainAlias}.enum_type = {{0}}", enum_type);
		public TypeTest WhereBit_length_type(params BitArray[] bit_length_type) => WhereOr($"{MainAlias}.bit_length_type = {{0}}", bit_length_type, NpgsqlDbType.Bit);
		public TypeTest WhereArray_type(int[] array_type) => WhereArray($"{MainAlias}.array_type = {{0}}", array_type, NpgsqlDbType.Integer | NpgsqlDbType.Array);
		public TypeTest WhereArray_typeAny(params int[] array_type) => WhereOr($"array_position({MainAlias}.array_type, {{0}}) > 0", array_type, NpgsqlDbType.Integer);
		public TypeTest WhereArray_typeLength(int len, string sqlOperator = "=") => Where($"array_length({MainAlias}.array_type, 1) {sqlOperator} {{0}}", len);

		#endregion

		#region Update
		public static TypeTestUpdateBuilder Update(TypeTestModel model) => Update(new[] { model.Id });
		public static TypeTestUpdateBuilder Update(Guid id) => Update(new[] { id });
		public static TypeTestUpdateBuilder Update(IEnumerable<TypeTestModel> models) => Update(models.Select(a => a.Id));
		public static TypeTestUpdateBuilder Update(IEnumerable<Guid> ids)
		{
			if (ids == null)
				throw new ArgumentNullException(nameof(ids));
			RedisHelper.Del(ids.Select(f => string.Format(CacheKey, f)).ToArray());
			return UpdateDiy.WhereOr("id = {0}", ids, NpgsqlDbType.Uuid);
		}
		public class TypeTestUpdateBuilder : UpdateBuilder<TypeTestUpdateBuilder, TypeTestModel>
		{
		}
		#endregion

	}
}

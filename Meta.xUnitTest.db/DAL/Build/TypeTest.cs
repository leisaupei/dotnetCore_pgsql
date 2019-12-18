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
		public static DeleteBuilder<TypeTestModel> DeleteDiy => new DeleteBuilder<TypeTestModel>();
		public static InsertBuilder<TypeTestModel> InsertDiy => new InsertBuilder<TypeTestModel>();
		#endregion

		#region Delete
		public static int Delete(TypeTestModel model) => Delete(new[] { model.Id });
		public static int Delete(Guid id) => Delete(new[] { id });
		public static int Delete(IEnumerable<TypeTestModel> models) => Delete(models.Select(a => a.Id));
		public static int Delete(IEnumerable<Guid> ids)
		{
			if (ids == null)
				throw new ArgumentNullException(nameof(ids));
			if (DbConfig.DbCacheTimeOut != 0)
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
		private static InsertBuilder<TypeTestModel> GetInsertBuilder(TypeTestModel model)
		{
			if (model == null)
				throw new ArgumentNullException(nameof(model));
			return InsertDiy
				.Set(a => a.Id, model.Id = model.Id == Guid.Empty ? Guid.NewGuid() : model.Id)
				.Set("bit_type", model.Bit_type, 1, NpgsqlDbType.Bit)
				.Set(a => a.Bool_type, model.Bool_type)
				.Set(a => a.Box_type, model.Box_type)
				.Set(a => a.Bytea_type, model.Bytea_type)
				.Set(a => a.Char_type, model.Char_type)
				.Set(a => a.Cidr_type, model.Cidr_type)
				.Set(a => a.Circle_type, model.Circle_type)
				.Set(a => a.Date_type, model.Date_type)
				.Set(a => a.Decimal_type, model.Decimal_type)
				.Set(a => a.Float4_type, model.Float4_type)
				.Set(a => a.Float8_type, model.Float8_type)
				.Set(a => a.Inet_type, model.Inet_type)
				.Set(a => a.Int2_type, model.Int2_type)
				.Set(a => a.Int4_type, model.Int4_type)
				.Set(a => a.Int8_type, model.Int8_type)
				.Set(a => a.Interval_type, model.Interval_type)
				.Set(a => a.Json_type, model.Json_type ??= JToken.Parse("{}"))
				.Set(a => a.Jsonb_type, model.Jsonb_type ??= JToken.Parse("{}"))
				.Set(a => a.Line_type, model.Line_type)
				.Set(a => a.Lseg_type, model.Lseg_type)
				.Set(a => a.Macaddr_type, model.Macaddr_type)
				.Set(a => a.Money_type, model.Money_type)
				.Set(a => a.Path_type, model.Path_type)
				.Set(a => a.Point_type, model.Point_type)
				.Set(a => a.Polygon_type, model.Polygon_type)
				.Set(a => a.Serial2_type, model.Serial2_type)
				.Set(a => a.Serial4_type, model.Serial4_type)
				.Set(a => a.Serial8_type, model.Serial8_type)
				.Set(a => a.Text_type, model.Text_type)
				.Set(a => a.Time_type, model.Time_type)
				.Set(a => a.Timestamp_type, model.Timestamp_type)
				.Set(a => a.Timestamptz_type, model.Timestamptz_type)
				.Set(a => a.Timetz_type, model.Timetz_type)
				.Set(a => a.Tsquery_type, model.Tsquery_type)
				.Set(a => a.Tsvector_type, model.Tsvector_type)
				.Set(a => a.Varbit_type, model.Varbit_type)
				.Set(a => a.Varchar_type, model.Varchar_type)
				.Set("xml_type", model.Xml_type, -1, NpgsqlDbType.Xml)
				.Set(a => a.Hstore_type, model.Hstore_type)
				.Set(a => a.Enum_type, model.Enum_type)
				.Set(a => a.Composite_type, model.Composite_type)
				.Set(a => a.Bit_length_type, model.Bit_length_type)
				.Set("array_type", model.Array_type, -1, NpgsqlDbType.Integer | NpgsqlDbType.Array);
		}
		#endregion

		#region Select
		public static TypeTestModel GetItem(Guid id) => GetRedisCache(string.Format(CacheKey, id), DbConfig.DbCacheTimeOut, () => Select.Where(a => a.Id == id).ToOne());
		public static List<TypeTestModel> GetItems(IEnumerable<Guid> ids) => Select.WhereAny(a => a.Id, ids).ToList();
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
			if (DbConfig.DbCacheTimeOut != 0)
				RedisHelper.Del(ids.Select(f => string.Format(CacheKey, f)).ToArray());
			return UpdateDiy.WhereOr("id = {0}", ids, NpgsqlDbType.Uuid);
		}
		public class TypeTestUpdateBuilder : UpdateBuilder<TypeTestUpdateBuilder, TypeTestModel>
		{
		}
		#endregion

	}
}

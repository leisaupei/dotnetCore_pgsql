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
using System.Xml;
using System.Net;
using System.Threading.Tasks;
using System.Threading;
using Meta.Common.Interface;

namespace Meta.xUnitTest.DAL
{
	public sealed partial class TypeTest : SelectBuilder<TypeTest, TypeTestModel>
	{
		#region Properties
		public const string CacheKey = "meta_xunittest_model_typetestmodel_{0}";
		private TypeTest() { }
		public static TypeTest Select => new TypeTest();
		public static UpdateBuilder<TypeTestModel> UpdateBuilder => new UpdateBuilder<TypeTestModel>();
		public static DeleteBuilder<TypeTestModel> DeleteBuilder => new DeleteBuilder<TypeTestModel>();
		public static InsertBuilder<TypeTestModel> InsertBuilder => new InsertBuilder<TypeTestModel>();
		#endregion

		#region Delete
		public static int Delete(params Guid[] ids)
			=> DeleteAsync(false, CancellationToken.None, ids).ConfigureAwait(false).GetAwaiter().GetResult();

		public static ValueTask<int> DeleteAsync(CancellationToken cancellationToken = default, params Guid[] ids)
			=> DeleteAsync(true, cancellationToken, ids);

		private static async ValueTask<int> DeleteAsync(bool async, CancellationToken cancellationToken, params Guid[] ids)
		{
			if (ids == null)
				throw new ArgumentNullException(nameof(ids));
			if (DbConfig.DbCacheTimeOut != 0)
			{
				var keys = ids.Select(f => string.Format(CacheKey, f)).ToArray();
				if(async)
					await RedisHelper.DelAsync(keys);
				else
					RedisHelper.Del(keys);
			}
			if(async)
				return await DeleteBuilder.WhereAny(a => a.Id, ids).ToRowsAsync(cancellationToken);
			return DeleteBuilder.WhereAny(a => a.Id, ids).ToRows();
		}
		#endregion

		#region Insert
		public static int Commit(TypeTestModel model) 
			=> SetRedisCache(string.Format(CacheKey, model.Id), model, DbConfig.DbCacheTimeOut, () => GetInsertBuilder(model).ToRows());

		public static TypeTestModel Insert(TypeTestModel model)
		{
			SetRedisCache(string.Format(CacheKey, model.Id), model, DbConfig.DbCacheTimeOut, () => GetInsertBuilder(model).ToRows(ref model));
			return model;
		}

		public static int Commit(IEnumerable<TypeTestModel> models, bool isExceptionCancel = true)
		{
			if (models == null)
				throw new ArgumentNullException(nameof(models));
			var sqlbuilders = GetSqlBuilder(models, isExceptionCancel);
			return InsertMultiple<DbMaster>(models, sqlbuilders, DbConfig.DbCacheTimeOut, (model) => string.Format(CacheKey, model.Id));
		}

		public static Task<TypeTestModel> InsertAsync(TypeTestModel model, CancellationToken cancellationToken = default)
			=> SetRedisCacheAsync(string.Format(CacheKey, model.Id), model, DbConfig.DbCacheTimeOut, () => GetInsertBuilder(model).ToOneAsync(cancellationToken));

		public static ValueTask<int> CommitAsync(TypeTestModel model, CancellationToken cancellationToken = default)
			=> SetRedisCacheAsync(string.Format(CacheKey, model.Id), model, DbConfig.DbCacheTimeOut, () => GetInsertBuilder(model).ToRowsAsync(cancellationToken), cancellationToken);

		public static ValueTask<int> CommitAsync(IEnumerable<TypeTestModel> models, bool isExceptionCancel = true, CancellationToken cancellationToken = default)
		{
			if (models == null)
				return new ValueTask<int>(Task.FromException<int>(new ArgumentNullException(nameof(models))));
			var sqlbuilders = GetSqlBuilder(models, isExceptionCancel);
			return InsertMultipleAsync<DbMaster>(models, sqlbuilders, DbConfig.DbCacheTimeOut, (model) => string.Format(CacheKey, model.Id), cancellationToken);
		}

		private static IEnumerable<ISqlBuilder> GetSqlBuilder(IEnumerable<TypeTestModel> models, bool isExceptionCancel)
		{
			return isExceptionCancel
				? models.Select(f => GetInsertBuilder(f).ToRowsPipe())
				: models.Select(f => GetInsertBuilder(f).WhereNotExists(Select.Where(a => a.Id == f.Id)).ToRowsPipe());
		}

		private static InsertBuilder<TypeTestModel> GetInsertBuilder(TypeTestModel model)
		{
			if (model == null)
				throw new ArgumentNullException(nameof(model));
			return InsertBuilder
				.Set(a => a.Id, model.Id = model.Id == Guid.Empty ? Guid.NewGuid() : model.Id)
				.Set(a => a.Bit_type, model.Bit_type)
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
				.Set(a => a.Text_type, model.Text_type)
				.Set(a => a.Time_type, model.Time_type)
				.Set(a => a.Timestamp_type, model.Timestamp_type)
				.Set(a => a.Timestamptz_type, model.Timestamptz_type)
				.Set(a => a.Timetz_type, model.Timetz_type)
				.Set(a => a.Tsquery_type, model.Tsquery_type)
				.Set(a => a.Tsvector_type, model.Tsvector_type)
				.Set(a => a.Varbit_type, model.Varbit_type)
				.Set(a => a.Varchar_type, model.Varchar_type)
				.Set(a => a.Xml_type, model.Xml_type)
				.Set(a => a.Hstore_type, model.Hstore_type)
				.Set(a => a.Enum_type, model.Enum_type)
				.Set(a => a.Composite_type, model.Composite_type)
				.Set(a => a.Bit_length_type, model.Bit_length_type)
				.Set(a => a.Array_type, model.Array_type)
				.Set(a => a.Serial2_type, model.Serial2_type)
				.Set(a => a.Serial4_type, model.Serial4_type)
				.Set(a => a.Serial8_type, model.Serial8_type)
				.Set(a => a.Uuid_array_type, model.Uuid_array_type);
		}
		#endregion

		#region Select

		public static TypeTestModel GetItem(Guid id) 
			=> GetRedisCache(string.Format(CacheKey, id), DbConfig.DbCacheTimeOut, () => Select.Where(a => a.Id == id).ToOne());

		public static Task<TypeTestModel> GetItemAsync(Guid id, CancellationToken cancellationToken = default) 
			=> GetRedisCacheAsync(string.Format(CacheKey, id), DbConfig.DbCacheTimeOut, () => Select.Where(a => a.Id == id).ToOneAsync(cancellationToken), cancellationToken);

		public static List<TypeTestModel> GetItems(IEnumerable<Guid> ids) 
			=> Select.WhereAny(a => a.Id, ids).ToList();

		public static Task<List<TypeTestModel>> GetItemsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default) 
			=> Select.WhereAny(a => a.Id, ids).ToListAsync(cancellationToken);

		#endregion

		#region Update
		public static UpdateBuilder<TypeTestModel> Update(params Guid[] ids)
		{
			if (ids == null)
				throw new ArgumentNullException(nameof(ids));
			if (DbConfig.DbCacheTimeOut != 0)
				RedisHelper.Del(ids.Select(f => string.Format(CacheKey, f)).ToArray());
			return UpdateBuilder.WhereAny(a => a.Id, ids);
		}
		#endregion

	}
}

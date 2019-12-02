using Meta.Common.SqlBuilder;
using Meta.Common.Model;
using Meta.xUnitTest.Model;
using Meta.xUnitTest.Options;
using NpgsqlTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using System.Net;

namespace Meta.xUnitTest.DAL
{
	[Mapping("people")]
	public partial class People : SelectExchange<People, PeopleModel>
	{
		#region Properties
		public const string CacheKey = "meta_xunittest_model_peoplemodel_{0}";
		public static People Select => new People();
		public static People SelectDiy(string fields) => new People { Fields = fields };
		public static People SelectDiy(string fields, string alias) => new People { Fields = fields, MainAlias = alias };
		public static PeopleUpdateBuilder UpdateDiy => new PeopleUpdateBuilder();
		public static DeleteBuilder DeleteDiy => new DeleteBuilder("people");
		public static InsertBuilder InsertDiy => new InsertBuilder("people");
		#endregion

		#region Delete
		public static int Delete(PeopleModel model) => Delete(new[] { model.Id });
		public static int Delete(Guid id) => Delete(new[] { id });
		public static int Delete(IEnumerable<PeopleModel> models) => Delete(models.Select(a => a.Id));
		public static int Delete(IEnumerable<Guid> ids)
		{
			if (ids == null)
				throw new ArgumentNullException(nameof(ids));
			RedisHelper.Del(ids.Select(f => string.Format(CacheKey, f)).ToArray());
			return DeleteDiy.WhereOr("id = {0}", ids, NpgsqlDbType.Uuid).ToRows();
		}
		#endregion

		#region Insert
		public static int Commit(PeopleModel model) => SetRedisCache(string.Format(CacheKey, model.Id), model, DbConfig.DbCacheTimeOut, () => GetInsertBuilder(model).ToRows());
		public static PeopleModel Insert(PeopleModel model)
		{
			SetRedisCache(string.Format(CacheKey, model.Id), model, DbConfig.DbCacheTimeOut, () => GetInsertBuilder(model).ToRows(ref model));
			return model;
		}
		private static InsertBuilder GetInsertBuilder(PeopleModel model)
		{
			if (model == null)
				throw new ArgumentNullException(nameof(model));
			return InsertDiy
				.Set("id", model.Id = model.Id == Guid.Empty ? Guid.NewGuid() : model.Id, 16, NpgsqlDbType.Uuid)
				.Set("age", model.Age, 4, NpgsqlDbType.Integer)
				.Set("name", model.Name, 255, NpgsqlDbType.Varchar)
				.Set("sex", model.Sex, 1, NpgsqlDbType.Boolean)
				.Set("create_time", model.Create_time = model.Create_time.Ticks == 0 ? DateTime.Now : model.Create_time, 8, NpgsqlDbType.Timestamp)
				.Set("address", model.Address, 255, NpgsqlDbType.Varchar)
				.Set("address_detail", model.Address_detail ??= JToken.Parse("{}"), -1, NpgsqlDbType.Jsonb)
				.Set("state", model.State, 4);
		}
		#endregion

		#region Select
		public static PeopleModel GetItem(Guid id) => GetRedisCache(string.Format(CacheKey, id), DbConfig.DbCacheTimeOut, () => Select.WhereId(id).ToOne());
		public static List<PeopleModel> GetItems(IEnumerable<Guid> id) => Select.WhereId(id.ToArray()).ToList();
		public People WhereId(params Guid[] id) => WhereOr($"{MainAlias}.id = {{0}}", id, NpgsqlDbType.Uuid);
		public People WhereAge(params int[] age) => WhereOr($"{MainAlias}.age = {{0}}", age, NpgsqlDbType.Integer);
		public People WhereAgeThan(int val, string sqlOperator = ">") => Where($"{MainAlias}.age {sqlOperator} {{0}}", new DbTypeValue(val, NpgsqlDbType.Integer));
		public People WhereName(params string[] name) => WhereOr($"{MainAlias}.name = {{0}}", name, NpgsqlDbType.Varchar);
		public People WhereNameLike(params string[] name) => WhereOr($"{MainAlias}.name LIKE {{0}}", name.Select(a => $"%{a}%"), NpgsqlDbType.Varchar);
		public People WhereSex(params bool?[] sex) => WhereOr($"{MainAlias}.sex = {{0}}", sex, NpgsqlDbType.Boolean);
		public People WhereSex(params bool[] sex) => WhereOr($"{MainAlias}.sex = {{0}}", sex, NpgsqlDbType.Boolean);
		public People WhereCreate_timeRange(DateTime? begin = null, DateTime? end = null) => Where($"{MainAlias}.create_time BETWEEN {{0}} AND {{1}}", begin ?? DateTime.Parse("1970-1-1"), end ?? DateTime.Now);
		public People WhereAddress(params string[] address) => WhereOr($"{MainAlias}.address = {{0}}", address, NpgsqlDbType.Varchar);
		public People WhereAddressLike(params string[] address) => WhereOr($"{MainAlias}.address LIKE {{0}}", address.Select(a => $"%{a}%"), NpgsqlDbType.Varchar);
		public People WhereState(params EDataState[] state) => WhereOr($"{MainAlias}.state = {{0}}", state);

		#endregion

		#region Update
		public static PeopleUpdateBuilder Update(PeopleModel model) => Update(new[] { model.Id });
		public static PeopleUpdateBuilder Update(Guid id) => Update(new[] { id });
		public static PeopleUpdateBuilder Update(IEnumerable<PeopleModel> models) => Update(models.Select(a => a.Id));
		public static PeopleUpdateBuilder Update(IEnumerable<Guid> ids)
		{
			if (ids == null)
				throw new ArgumentNullException(nameof(ids));
			RedisHelper.Del(ids.Select(f => string.Format(CacheKey, f)).ToArray());
			return UpdateDiy.WhereOr("id = {0}", ids, NpgsqlDbType.Uuid);
		}
		public class PeopleUpdateBuilder : UpdateBuilder<PeopleUpdateBuilder, PeopleModel>
		{
			public PeopleUpdateBuilder SetId(Guid id) => Set("id", id, 16, NpgsqlDbType.Uuid);
			public PeopleUpdateBuilder SetAge(int age) => Set("age", age, 4, NpgsqlDbType.Integer);
			public PeopleUpdateBuilder SetAgeIncrement(int age) => SetIncrement("age", age, 4, NpgsqlDbType.Integer);
			public PeopleUpdateBuilder SetName(string name) => Set("name", name, 255, NpgsqlDbType.Varchar);
			public PeopleUpdateBuilder SetSex(bool? sex) => Set("sex", sex, 1, NpgsqlDbType.Boolean);
			public PeopleUpdateBuilder SetCreate_time(DateTime create_time) => Set("create_time", create_time, 8, NpgsqlDbType.Timestamp);
			public PeopleUpdateBuilder SetCreate_timeIncrement(TimeSpan timeSpan) => SetIncrement("create_time", timeSpan, 8, NpgsqlDbType.Timestamp);
			public PeopleUpdateBuilder SetAddress(string address) => Set("address", address, 255, NpgsqlDbType.Varchar);
			public PeopleUpdateBuilder SetAddress_detail(JToken address_detail) => Set("address_detail", address_detail, -1, NpgsqlDbType.Jsonb);
			public PeopleUpdateBuilder SetState(EDataState state) => Set("state", state, 4);
		}
		#endregion

	}
}

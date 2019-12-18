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
	[DbTable("people")]
	public sealed partial class People : SelectBuilder<People, PeopleModel>
	{
		#region Properties
		public const string CacheKey = "meta_xunittest_model_peoplemodel_{0}";
		private People() { }
		public static People Select => new People();
		public static People SelectDiy(string fields) => new People { Fields = fields };
		public static People SelectDiy(string fields, string alias) => new People { Fields = fields, MainAlias = alias };
		public static PeopleUpdateBuilder UpdateDiy => new PeopleUpdateBuilder();
		public static DeleteBuilder<PeopleModel> DeleteDiy => new DeleteBuilder<PeopleModel>();
		public static InsertBuilder<PeopleModel> InsertDiy => new InsertBuilder<PeopleModel>();
		#endregion

		#region Delete
		public static int Delete(PeopleModel model) => Delete(new[] { model.Id });
		public static int Delete(Guid id) => Delete(new[] { id });
		public static int Delete(IEnumerable<PeopleModel> models) => Delete(models.Select(a => a.Id));
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
		public static int Commit(PeopleModel model) => SetRedisCache(string.Format(CacheKey, model.Id), model, DbConfig.DbCacheTimeOut, () => GetInsertBuilder(model).ToRows());
		public static PeopleModel Insert(PeopleModel model)
		{
			SetRedisCache(string.Format(CacheKey, model.Id), model, DbConfig.DbCacheTimeOut, () => GetInsertBuilder(model).ToRows(ref model));
			return model;
		}
		private static InsertBuilder<PeopleModel> GetInsertBuilder(PeopleModel model)
		{
			if (model == null)
				throw new ArgumentNullException(nameof(model));
			return InsertDiy
				.Set(a => a.Id, model.Id = model.Id == Guid.Empty ? Guid.NewGuid() : model.Id)
				.Set(a => a.Age, model.Age)
				.Set(a => a.Name, model.Name)
				.Set(a => a.Sex, model.Sex)
				.Set(a => a.Create_time, model.Create_time = model.Create_time.Ticks == 0 ? DateTime.Now : model.Create_time)
				.Set(a => a.Address, model.Address)
				.Set(a => a.Address_detail, model.Address_detail ??= JToken.Parse("{}"))
				.Set(a => a.State, model.State);
		}
		#endregion

		#region Select
		public static PeopleModel GetItem(Guid id) => GetRedisCache(string.Format(CacheKey, id), DbConfig.DbCacheTimeOut, () => Select.Where(a => a.Id == id).ToOne());
		public static List<PeopleModel> GetItems(IEnumerable<Guid> ids) => Select.WhereAny(a => a.Id, ids).ToList();

		#endregion

		#region Update
		public static PeopleUpdateBuilder Update(PeopleModel model) => Update(new[] { model.Id });
		public static PeopleUpdateBuilder Update(Guid id) => Update(new[] { id });
		public static PeopleUpdateBuilder Update(IEnumerable<PeopleModel> models) => Update(models.Select(a => a.Id));
		public static PeopleUpdateBuilder Update(IEnumerable<Guid> ids)
		{
			if (ids == null)
				throw new ArgumentNullException(nameof(ids));
			if (DbConfig.DbCacheTimeOut != 0)
				RedisHelper.Del(ids.Select(f => string.Format(CacheKey, f)).ToArray());
			return UpdateDiy.WhereOr("id = {0}", ids, NpgsqlDbType.Uuid);
		}
		public class PeopleUpdateBuilder : UpdateBuilder<PeopleUpdateBuilder, PeopleModel>
		{
		}
		#endregion

	}
}

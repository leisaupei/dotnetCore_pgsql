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

namespace Meta.xUnitTest.DAL
{
	public sealed partial class People : SelectBuilder<People, PeopleModel>
	{
		#region Properties
		public const string CacheKey = "meta_xunittest_model_peoplemodel_{0}";
		private People() { }
		public static People Select => new People();
		public static People SelectDiy(string fields) => new People { Fields = fields };
		public static People SelectDiy(string fields, string alias) => new People { Fields = fields, MainAlias = alias };
		public static UpdateBuilder<PeopleModel> UpdateDiy => new UpdateBuilder<PeopleModel>();
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
			return DeleteDiy.WhereAny(a => a.Id, ids).ToRows();
		}
		#endregion

		#region Insert
		public static int Commit(PeopleModel model) => SetRedisCache(string.Format(CacheKey, model.Id), model, DbConfig.DbCacheTimeOut, () => GetInsertBuilder(model).ToRows());
		public static PeopleModel Insert(PeopleModel model)
		{
			SetRedisCache(string.Format(CacheKey, model.Id), model, DbConfig.DbCacheTimeOut, () => GetInsertBuilder(model).ToRows(ref model));
			return model;
		}
		public static int Commit(IEnumerable<PeopleModel> models, bool isExceptionCancel = true)
		{
			if (models == null)
				throw new ArgumentNullException(nameof(models));
			var sqlbuilders = isExceptionCancel ? models.Select(f => GetInsertBuilder(f).ToRowsPipe()) :
				models.Select(f => GetInsertBuilder(f).WhereNotExists(Select.Where(a => a.Id == f.Id)).ToRowsPipe());
			return InsertMultiple<DbMaster>(models, sqlbuilders, DbConfig.DbCacheTimeOut, (model) => string.Format(CacheKey, model.Id));
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
		public static UpdateBuilder<PeopleModel> Update(PeopleModel model) => Update(new[] { model.Id });
		public static UpdateBuilder<PeopleModel> Update(Guid id) => Update(new[] { id });
		public static UpdateBuilder<PeopleModel> Update(IEnumerable<PeopleModel> models) => Update(models.Select(a => a.Id));
		public static UpdateBuilder<PeopleModel> Update(IEnumerable<Guid> ids)
		{
			if (ids == null)
				throw new ArgumentNullException(nameof(ids));
			if (DbConfig.DbCacheTimeOut != 0)
				RedisHelper.Del(ids.Select(f => string.Format(CacheKey, f)).ToArray());
			return UpdateDiy.WhereAny(a => a.Id, ids);
		}
		#endregion

	}
}

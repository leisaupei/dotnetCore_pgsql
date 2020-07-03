using Meta.Driver.SqlBuilder;
using Meta.Driver.Model;
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
using Meta.Driver.Interface;

namespace Meta.xUnitTest.DAL
{
	public sealed partial class People : SelectBuilder<People, PeopleModel>
	{
		#region Properties
		public const string CacheKey = "meta_xunittest_model_peoplemodel_{0}";
		private People() { }
		public static People Select => new People();
		public static UpdateBuilder<PeopleModel> UpdateBuilder => new UpdateBuilder<PeopleModel>();
		public static DeleteBuilder<PeopleModel> DeleteBuilder => new DeleteBuilder<PeopleModel>();
		public static InsertBuilder<PeopleModel> InsertBuilder => new InsertBuilder<PeopleModel>();
		#endregion

		#region Delete
		public static int Delete(params Guid[] ids)
			=> DeleteAsync(false, CancellationToken.None, ids).ConfigureAwait(false).GetAwaiter().GetResult();

		public static ValueTask<int> DeleteAsync(Guid[] ids, CancellationToken cancellationToken = default)
			=> DeleteAsync(true, cancellationToken, ids);

		private static async ValueTask<int> DeleteAsync(bool async, CancellationToken cancellationToken, Guid[] ids)
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
		public static int Commit(PeopleModel model) => GetInsertBuilder(model).ToRows();

		public static PeopleModel Insert(PeopleModel model)
		{
			GetInsertBuilder(model).ToRows(ref model);
			return model;
		}

		public static int Commit(IEnumerable<PeopleModel> models, bool isExceptionCancel = true)
		{
			if (models == null)
				throw new ArgumentNullException(nameof(models));
			var sqlbuilders = GetSqlBuilder(models, isExceptionCancel);
			return InsertMultiple<DbMaster>(models, sqlbuilders, DbConfig.DbCacheTimeOut, (model) => string.Format(CacheKey, model.Id));
		}

		public static Task<PeopleModel> InsertAsync(PeopleModel model, CancellationToken cancellationToken = default)
			=> SetRedisCacheAsync(string.Format(CacheKey, model.Id), model, DbConfig.DbCacheTimeOut, () => GetInsertBuilder(model).ToOneAsync(cancellationToken));

		public static ValueTask<int> CommitAsync(PeopleModel model, CancellationToken cancellationToken = default)
			=> SetRedisCacheAsync(string.Format(CacheKey, model.Id), model, DbConfig.DbCacheTimeOut, () => GetInsertBuilder(model).ToRowsAsync(cancellationToken));

		public static ValueTask<int> CommitAsync(IEnumerable<PeopleModel> models, bool isExceptionCancel = true, CancellationToken cancellationToken = default)
		{
			if (models == null)
				return new ValueTask<int>(Task.FromException<int>(new ArgumentNullException(nameof(models))));
			var sqlbuilders = GetSqlBuilder(models, isExceptionCancel);
			return InsertMultipleAsync<DbMaster>(models, sqlbuilders, DbConfig.DbCacheTimeOut, (model) => string.Format(CacheKey, model.Id), cancellationToken);
		}

		public static IEnumerable<ISqlBuilder> GetSqlBuilder(IEnumerable<PeopleModel> models, bool isExceptionCancel)
		{
			return isExceptionCancel
				? models.Select(f => GetInsertBuilder(f).ToRowsPipe())
				: models.Select(f => GetInsertBuilder(f).WhereNotExists(Select.Where(a => a.Id == f.Id)).ToRowsPipe());
		}

		public static InsertBuilder<PeopleModel> GetInsertBuilder(PeopleModel model)
		{
			if (model == null)
				throw new ArgumentNullException(nameof(model));
			return InsertBuilder
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
		public static PeopleModel GetItem(Guid id) 
			=> GetRedisCache(string.Format(CacheKey, id), DbConfig.DbCacheTimeOut, () => Select.Where(a => a.Id == id).ToOne());

		public static Task<PeopleModel> GetItemAsync(Guid id, CancellationToken cancellationToken = default) 
			=> GetRedisCacheAsync(string.Format(CacheKey, id), DbConfig.DbCacheTimeOut, () => Select.Where(a => a.Id == id).ToOneAsync(cancellationToken));

		public static List<PeopleModel> GetItems(IEnumerable<Guid> ids) 
			=> Select.WhereAny(a => a.Id, ids).ToList();

		public static Task<List<PeopleModel>> GetItemsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default) 
			=> Select.WhereAny(a => a.Id, ids).ToListAsync(cancellationToken);
		#endregion

		#region Update
		public static UpdateBuilder<PeopleModel> Update(params Guid[] ids)
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

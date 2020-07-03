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
	public sealed partial class Stat : SelectBuilder<Stat, StatModel>
	{
		#region Properties
		public const string CacheKey = "meta_xunittest_model_statmodel_{0}";
		private Stat() { }
		public static Stat Select => new Stat();
		public static UpdateBuilder<StatModel> UpdateBuilder => new UpdateBuilder<StatModel>();
		public static DeleteBuilder<StatModel> DeleteBuilder => new DeleteBuilder<StatModel>();
		public static InsertBuilder<StatModel> InsertBuilder => new InsertBuilder<StatModel>();
		#endregion

		#region Delete
		public static int Delete(params int[] ids)
			=> DeleteAsync(false, CancellationToken.None, ids).ConfigureAwait(false).GetAwaiter().GetResult();

		public static ValueTask<int> DeleteAsync(int[] ids, CancellationToken cancellationToken = default)
			=> DeleteAsync(true, cancellationToken, ids);

		private static async ValueTask<int> DeleteAsync(bool async, CancellationToken cancellationToken, int[] ids)
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
		public static int Commit(StatModel model) => GetInsertBuilder(model).ToRows();

		public static StatModel Insert(StatModel model)
		{
			GetInsertBuilder(model).ToRows(ref model);
			return model;
		}

		public static int Commit(IEnumerable<StatModel> models, bool isExceptionCancel = true)
		{
			if (models == null)
				throw new ArgumentNullException(nameof(models));
			var sqlbuilders = GetSqlBuilder(models, isExceptionCancel);
			return InsertMultiple<DbMaster>(models, sqlbuilders, DbConfig.DbCacheTimeOut, (model) => string.Format(CacheKey, model.Id));
		}

		public static Task<StatModel> InsertAsync(StatModel model, CancellationToken cancellationToken = default)
			=> SetRedisCacheAsync(string.Format(CacheKey, model.Id), model, DbConfig.DbCacheTimeOut, () => GetInsertBuilder(model).ToOneAsync(cancellationToken));

		public static ValueTask<int> CommitAsync(StatModel model, CancellationToken cancellationToken = default)
			=> SetRedisCacheAsync(string.Format(CacheKey, model.Id), model, DbConfig.DbCacheTimeOut, () => GetInsertBuilder(model).ToRowsAsync(cancellationToken));

		public static ValueTask<int> CommitAsync(IEnumerable<StatModel> models, bool isExceptionCancel = true, CancellationToken cancellationToken = default)
		{
			if (models == null)
				return new ValueTask<int>(Task.FromException<int>(new ArgumentNullException(nameof(models))));
			var sqlbuilders = GetSqlBuilder(models, isExceptionCancel);
			return InsertMultipleAsync<DbMaster>(models, sqlbuilders, DbConfig.DbCacheTimeOut, (model) => string.Format(CacheKey, model.Id), cancellationToken);
		}

		public static IEnumerable<ISqlBuilder> GetSqlBuilder(IEnumerable<StatModel> models, bool isExceptionCancel)
		{
			return isExceptionCancel
				? models.Select(f => GetInsertBuilder(f).ToRowsPipe())
				: models.Select(f => GetInsertBuilder(f).WhereNotExists(Select.Where(a => a.Id == f.Id)).ToRowsPipe());
		}

		public static InsertBuilder<StatModel> GetInsertBuilder(StatModel model)
		{
			if (model == null)
				throw new ArgumentNullException(nameof(model));
			return InsertBuilder
				.Set(a => a.Times, model.Times)
				.Set(a => a.Haoshi, model.Haoshi);
		}
		#endregion

		#region Select
		public static StatModel GetItem(int id) 
			=> GetRedisCache(string.Format(CacheKey, id), DbConfig.DbCacheTimeOut, () => Select.Where(a => a.Id == id).ToOne());

		public static Task<StatModel> GetItemAsync(int id, CancellationToken cancellationToken = default) 
			=> GetRedisCacheAsync(string.Format(CacheKey, id), DbConfig.DbCacheTimeOut, () => Select.Where(a => a.Id == id).ToOneAsync(cancellationToken));

		public static List<StatModel> GetItems(IEnumerable<int> ids) 
			=> Select.WhereAny(a => a.Id, ids).ToList();

		public static Task<List<StatModel>> GetItemsAsync(IEnumerable<int> ids, CancellationToken cancellationToken = default) 
			=> Select.WhereAny(a => a.Id, ids).ToListAsync(cancellationToken);
		#endregion

		#region Update
		public static UpdateBuilder<StatModel> Update(params int[] ids)
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

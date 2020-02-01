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
	public sealed partial class ClassGrade : SelectBuilder<ClassGrade, ClassGradeModel>
	{
		#region Properties
		public const string CacheKey = "meta_xunittest_model_classgrademodel_{0}";
		private ClassGrade() { }
		public static ClassGrade Select => new ClassGrade();
		public static UpdateBuilder<ClassGradeModel> UpdateBuilder => new UpdateBuilder<ClassGradeModel>();
		public static DeleteBuilder<ClassGradeModel> DeleteBuilder => new DeleteBuilder<ClassGradeModel>();
		public static InsertBuilder<ClassGradeModel> InsertBuilder => new InsertBuilder<ClassGradeModel>();
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
		public static int Commit(ClassGradeModel model) 
			=> SetRedisCache(string.Format(CacheKey, model.Id), model, DbConfig.DbCacheTimeOut, () => GetInsertBuilder(model).ToRows());

		public static ClassGradeModel Insert(ClassGradeModel model)
		{
			SetRedisCache(string.Format(CacheKey, model.Id), model, DbConfig.DbCacheTimeOut, () => GetInsertBuilder(model).ToRows(ref model));
			return model;
		}

		public static int Commit(IEnumerable<ClassGradeModel> models, bool isExceptionCancel = true)
		{
			if (models == null)
				throw new ArgumentNullException(nameof(models));
			var sqlbuilders = GetSqlBuilder(models, isExceptionCancel);
			return InsertMultiple<DbMaster>(models, sqlbuilders, DbConfig.DbCacheTimeOut, (model) => string.Format(CacheKey, model.Id));
		}

		public static Task<ClassGradeModel> InsertAsync(ClassGradeModel model, CancellationToken cancellationToken = default)
			=> SetRedisCacheAsync(string.Format(CacheKey, model.Id), model, DbConfig.DbCacheTimeOut, () => GetInsertBuilder(model).ToOneAsync(cancellationToken));

		public static ValueTask<int> CommitAsync(ClassGradeModel model, CancellationToken cancellationToken = default)
			=> SetRedisCacheAsync(string.Format(CacheKey, model.Id), model, DbConfig.DbCacheTimeOut, () => GetInsertBuilder(model).ToRowsAsync(cancellationToken), cancellationToken);

		public static ValueTask<int> CommitAsync(IEnumerable<ClassGradeModel> models, bool isExceptionCancel = true, CancellationToken cancellationToken = default)
		{
			if (models == null)
				return new ValueTask<int>(Task.FromException<int>(new ArgumentNullException(nameof(models))));
			var sqlbuilders = GetSqlBuilder(models, isExceptionCancel);
			return InsertMultipleAsync<DbMaster>(models, sqlbuilders, DbConfig.DbCacheTimeOut, (model) => string.Format(CacheKey, model.Id), cancellationToken);
		}

		private static IEnumerable<ISqlBuilder> GetSqlBuilder(IEnumerable<ClassGradeModel> models, bool isExceptionCancel)
		{
			return isExceptionCancel
				? models.Select(f => GetInsertBuilder(f).ToRowsPipe())
				: models.Select(f => GetInsertBuilder(f).WhereNotExists(Select.Where(a => a.Id == f.Id)).ToRowsPipe());
		}

		private static InsertBuilder<ClassGradeModel> GetInsertBuilder(ClassGradeModel model)
		{
			if (model == null)
				throw new ArgumentNullException(nameof(model));
			return InsertBuilder
				.Set(a => a.Id, model.Id = model.Id == Guid.Empty ? Guid.NewGuid() : model.Id)
				.Set(a => a.Name, model.Name)
				.Set(a => a.Create_time, model.Create_time = model.Create_time.Ticks == 0 ? DateTime.Now : model.Create_time);
		}
		#endregion

		#region Select
		public static ClassGradeModel GetItem(Guid id) 
			=> GetRedisCache(string.Format(CacheKey, id), DbConfig.DbCacheTimeOut, () => Select.Where(a => a.Id == id).ToOne());

		public static Task<ClassGradeModel> GetItemAsync(Guid id, CancellationToken cancellationToken = default) 
			=> GetRedisCacheAsync(string.Format(CacheKey, id), DbConfig.DbCacheTimeOut, () => Select.Where(a => a.Id == id).ToOneAsync(cancellationToken), cancellationToken);

		public static List<ClassGradeModel> GetItems(IEnumerable<Guid> ids) 
			=> Select.WhereAny(a => a.Id, ids).ToList();

		public static Task<List<ClassGradeModel>> GetItemsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default) 
			=> Select.WhereAny(a => a.Id, ids).ToListAsync(cancellationToken);
		#endregion

		#region Update
		public static UpdateBuilder<ClassGradeModel> Update(params Guid[] ids)
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

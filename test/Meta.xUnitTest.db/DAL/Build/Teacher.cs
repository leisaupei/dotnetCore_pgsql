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
	public sealed partial class Teacher : SelectBuilder<Teacher, TeacherModel>
	{
		#region Properties
		public const string CacheKey = "meta_xunittest_model_teachermodel_{0}";
		private Teacher() { }
		public static Teacher Select => new Teacher();
		public static UpdateBuilder<TeacherModel> UpdateBuilder => new UpdateBuilder<TeacherModel>();
		public static DeleteBuilder<TeacherModel> DeleteBuilder => new DeleteBuilder<TeacherModel>();
		public static InsertBuilder<TeacherModel> InsertBuilder => new InsertBuilder<TeacherModel>();
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
		public static int Commit(TeacherModel model) => GetInsertBuilder(model).ToRows();

		public static TeacherModel Insert(TeacherModel model)
		{
			GetInsertBuilder(model).ToRows(ref model);
			return model;
		}

		public static int Commit(IEnumerable<TeacherModel> models, bool isExceptionCancel = true)
		{
			if (models == null)
				throw new ArgumentNullException(nameof(models));
			var sqlbuilders = GetSqlBuilder(models, isExceptionCancel);
			return InsertMultiple<DbMaster>(models, sqlbuilders, DbConfig.DbCacheTimeOut, (model) => string.Format(CacheKey, model.Id));
		}

		public static Task<TeacherModel> InsertAsync(TeacherModel model, CancellationToken cancellationToken = default)
			=> SetRedisCacheAsync(string.Format(CacheKey, model.Id), model, DbConfig.DbCacheTimeOut, () => GetInsertBuilder(model).ToOneAsync(cancellationToken));

		public static ValueTask<int> CommitAsync(TeacherModel model, CancellationToken cancellationToken = default)
			=> SetRedisCacheAsync(string.Format(CacheKey, model.Id), model, DbConfig.DbCacheTimeOut, () => GetInsertBuilder(model).ToRowsAsync(cancellationToken));

		public static ValueTask<int> CommitAsync(IEnumerable<TeacherModel> models, bool isExceptionCancel = true, CancellationToken cancellationToken = default)
		{
			if (models == null)
				return new ValueTask<int>(Task.FromException<int>(new ArgumentNullException(nameof(models))));
			var sqlbuilders = GetSqlBuilder(models, isExceptionCancel);
			return InsertMultipleAsync<DbMaster>(models, sqlbuilders, DbConfig.DbCacheTimeOut, (model) => string.Format(CacheKey, model.Id), cancellationToken);
		}

		public static IEnumerable<ISqlBuilder> GetSqlBuilder(IEnumerable<TeacherModel> models, bool isExceptionCancel)
		{
			return isExceptionCancel
				? models.Select(f => GetInsertBuilder(f).ToRowsPipe())
				: models.Select(f => GetInsertBuilder(f).WhereNotExists(Select.Where(a => a.Id == f.Id)).ToRowsPipe());
		}

		public static InsertBuilder<TeacherModel> GetInsertBuilder(TeacherModel model)
		{
			if (model == null)
				throw new ArgumentNullException(nameof(model));
			return InsertBuilder
				.Set(a => a.Teacher_no, model.Teacher_no)
				.Set(a => a.People_id, model.People_id)
				.Set(a => a.Create_time, model.Create_time = model.Create_time.Ticks == 0 ? DateTime.Now : model.Create_time)
				.Set(a => a.Id, model.Id = model.Id == Guid.Empty ? Guid.NewGuid() : model.Id);
		}
		#endregion

		#region Select
		public static TeacherModel GetItem(Guid id) 
			=> GetRedisCache(string.Format(CacheKey, id), DbConfig.DbCacheTimeOut, () => Select.Where(a => a.Id == id).ToOne());

		public static Task<TeacherModel> GetItemAsync(Guid id, CancellationToken cancellationToken = default) 
			=> GetRedisCacheAsync(string.Format(CacheKey, id), DbConfig.DbCacheTimeOut, () => Select.Where(a => a.Id == id).ToOneAsync(cancellationToken));

		public static List<TeacherModel> GetItems(IEnumerable<Guid> ids) 
			=> Select.WhereAny(a => a.Id, ids).ToList();

		public static Task<List<TeacherModel>> GetItemsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default) 
			=> Select.WhereAny(a => a.Id, ids).ToListAsync(cancellationToken);
		#endregion

		#region Update
		public static UpdateBuilder<TeacherModel> Update(params Guid[] ids)
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

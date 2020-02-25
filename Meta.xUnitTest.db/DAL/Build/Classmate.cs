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
	public sealed partial class Classmate : SelectBuilder<Classmate, ClassmateModel>
	{
		#region Properties
		public const string CacheKey = "meta_xunittest_model_classmatemodel_{0}_{1}_{2}";
		private Classmate() { }
		public static Classmate Select => new Classmate();
		public static UpdateBuilder<ClassmateModel> UpdateBuilder => new UpdateBuilder<ClassmateModel>();
		public static DeleteBuilder<ClassmateModel> DeleteBuilder => new DeleteBuilder<ClassmateModel>();
		public static InsertBuilder<ClassmateModel> InsertBuilder => new InsertBuilder<ClassmateModel>();
		#endregion

		#region Delete
		/// <summary>
		/// (teacher_id, student_id, grade_id)
		/// </summary>
		public static int Delete(params (Guid, Guid, Guid)[] values)
			=> DeleteAsync(false, CancellationToken.None, values).ConfigureAwait(false).GetAwaiter().GetResult();

		/// <summary>
		/// (teacher_id, student_id, grade_id)
		/// </summary>
		public static ValueTask<int> DeleteAsync(CancellationToken cancellationToken = default, params (Guid, Guid, Guid)[] values)
			=> DeleteAsync(true, cancellationToken, values);

		private static async ValueTask<int> DeleteAsync(bool async, CancellationToken cancellationToken, params (Guid, Guid, Guid)[] values)
		{
			if (values == null)
				throw new ArgumentNullException(nameof(values));
			if (DbConfig.DbCacheTimeOut != 0)
			{
				var keys = values.Select(f => string.Format(CacheKey, f.Item1, f.Item2, f.Item3)).ToArray();
				if(async)
					await RedisHelper.DelAsync(keys);
				else
					RedisHelper.Del(keys);
			}
			if(async)
				return await DeleteBuilder.Where(a => a.Teacher_id, a => a.Student_id, a => a.Grade_id, values).ToRowsAsync(cancellationToken);
			return DeleteBuilder.Where(a => a.Teacher_id, a => a.Student_id, a => a.Grade_id, values).ToRows();
		}
		#endregion

		#region Insert
		public static int Commit(ClassmateModel model) 
			=> SetRedisCache(string.Format(CacheKey, model.Teacher_id, model.Student_id, model.Grade_id), model, DbConfig.DbCacheTimeOut, () => GetInsertBuilder(model).ToRows());

		public static ClassmateModel Insert(ClassmateModel model)
		{
			SetRedisCache(string.Format(CacheKey, model.Teacher_id, model.Student_id, model.Grade_id), model, DbConfig.DbCacheTimeOut, () => GetInsertBuilder(model).ToRows(ref model));
			return model;
		}

		public static int Commit(IEnumerable<ClassmateModel> models, bool isExceptionCancel = true)
		{
			if (models == null)
				throw new ArgumentNullException(nameof(models));
			var sqlbuilders = GetSqlBuilder(models, isExceptionCancel);
			return InsertMultiple<DbMaster>(models, sqlbuilders, DbConfig.DbCacheTimeOut, (model) => string.Format(CacheKey, model.Teacher_id, model.Student_id, model.Grade_id));
		}

		public static Task<ClassmateModel> InsertAsync(ClassmateModel model, CancellationToken cancellationToken = default)
			=> SetRedisCacheAsync(string.Format(CacheKey, model.Teacher_id, model.Student_id, model.Grade_id), model, DbConfig.DbCacheTimeOut, () => GetInsertBuilder(model).ToOneAsync(cancellationToken));

		public static ValueTask<int> CommitAsync(ClassmateModel model, CancellationToken cancellationToken = default)
			=> SetRedisCacheAsync(string.Format(CacheKey, model.Teacher_id, model.Student_id, model.Grade_id), model, DbConfig.DbCacheTimeOut, () => GetInsertBuilder(model).ToRowsAsync(cancellationToken), cancellationToken);

		public static ValueTask<int> CommitAsync(IEnumerable<ClassmateModel> models, bool isExceptionCancel = true, CancellationToken cancellationToken = default)
		{
			if (models == null)
				return new ValueTask<int>(Task.FromException<int>(new ArgumentNullException(nameof(models))));
			var sqlbuilders = GetSqlBuilder(models, isExceptionCancel);
			return InsertMultipleAsync<DbMaster>(models, sqlbuilders, DbConfig.DbCacheTimeOut, (model) => string.Format(CacheKey, model.Teacher_id, model.Student_id, model.Grade_id), cancellationToken);
		}

		public static IEnumerable<ISqlBuilder> GetSqlBuilder(IEnumerable<ClassmateModel> models, bool isExceptionCancel)
		{
			return isExceptionCancel
				? models.Select(f => GetInsertBuilder(f).ToRowsPipe())
				: models.Select(f => GetInsertBuilder(f).WhereNotExists(Select.Where(a => a.Teacher_id == f.Teacher_id && a.Student_id == f.Student_id && a.Grade_id == f.Grade_id)).ToRowsPipe());
		}

		public static InsertBuilder<ClassmateModel> GetInsertBuilder(ClassmateModel model)
		{
			if (model == null)
				throw new ArgumentNullException(nameof(model));
			return InsertBuilder
				.Set(a => a.Teacher_id, model.Teacher_id)
				.Set(a => a.Student_id, model.Student_id)
				.Set(a => a.Grade_id, model.Grade_id)
				.Set(a => a.Create_time, model.Create_time ??= DateTime.Now);
		}
		#endregion

		#region Select
		public static ClassmateModel GetItem(Guid teacher_id, Guid student_id, Guid grade_id) 
			=> GetRedisCache(string.Format(CacheKey, teacher_id, student_id, grade_id), DbConfig.DbCacheTimeOut, () => Select.Where(a => a.Teacher_id == teacher_id && a.Student_id == student_id && a.Grade_id == grade_id).ToOne());

		public static Task<ClassmateModel> GetItemAsync(Guid teacher_id, Guid student_id, Guid grade_id, CancellationToken cancellationToken = default) 
			=> GetRedisCacheAsync(string.Format(CacheKey, teacher_id, student_id, grade_id), DbConfig.DbCacheTimeOut, () => Select.Where(a => a.Teacher_id == teacher_id && a.Student_id == student_id && a.Grade_id == grade_id).ToOneAsync(cancellationToken), cancellationToken);

		/// <summary>
		/// (teacher_id, student_id, grade_id)
		/// </summary>
		public static List<ClassmateModel> GetItems(IEnumerable<(Guid, Guid, Guid)> values) 
			=> Select.Where(a => a.Teacher_id, a => a.Student_id, a => a.Grade_id, values).ToList();

		/// <summary>
		/// (teacher_id, student_id, grade_id)
		/// </summary>
		public static Task<List<ClassmateModel>> GetItemsAsync(IEnumerable<(Guid, Guid, Guid)> values, CancellationToken cancellationToken = default) 
			=> Select.Where(a => a.Teacher_id, a => a.Student_id, a => a.Grade_id, values).ToListAsync(cancellationToken);
		#endregion

		#region Update
		/// <summary>
		/// (teacher_id, student_id, grade_id)
		/// </summary>
		public static UpdateBuilder<ClassmateModel> Update(params (Guid, Guid, Guid)[] values)
		{
			if (values == null)
				throw new ArgumentNullException(nameof(values));
			if (DbConfig.DbCacheTimeOut != 0)
				RedisHelper.Del(values.Select(f => string.Format(CacheKey, f.Item1, f.Item2, f.Item3)).ToArray());
			return UpdateBuilder.Where(a => a.Teacher_id, a => a.Student_id, a => a.Grade_id, values);
		}
		#endregion
	}
}

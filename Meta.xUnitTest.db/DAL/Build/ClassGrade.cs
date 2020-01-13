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
	public sealed partial class ClassGrade : SelectBuilder<ClassGrade, ClassGradeModel>
	{
		#region Properties
		public const string CacheKey = "meta_xunittest_model_classgrademodel_{0}";
		private ClassGrade() { }
		public static ClassGrade Select => new ClassGrade();
		public static ClassGrade SelectDiy(string fields) => new ClassGrade { Fields = fields };
		public static ClassGrade SelectDiy(string fields, string alias) => new ClassGrade { Fields = fields, MainAlias = alias };
		public static UpdateBuilder<ClassGradeModel> UpdateBuilder => new UpdateBuilder<ClassGradeModel>();
		public static DeleteBuilder<ClassGradeModel> DeleteBuilder => new DeleteBuilder<ClassGradeModel>();
		public static InsertBuilder<ClassGradeModel> InsertBuilder => new InsertBuilder<ClassGradeModel>();
		#endregion

		#region Delete
		public static int Delete(ClassGradeModel model) => Delete(new[] { model.Id });
		public static int Delete(Guid id) => Delete(new[] { id });
		public static int Delete(IEnumerable<ClassGradeModel> models) => Delete(models.Select(a => a.Id));
		public static int Delete(IEnumerable<Guid> ids)
		{
			if (ids == null)
				throw new ArgumentNullException(nameof(ids));
			if (DbConfig.DbCacheTimeOut != 0)
				RedisHelper.Del(ids.Select(f => string.Format(CacheKey, f)).ToArray());
			return DeleteBuilder.WhereAny(a => a.Id, ids).ToRows();
		}
		#endregion

		#region Insert
		public static int Commit(ClassGradeModel model) => SetRedisCache(string.Format(CacheKey, model.Id), model, DbConfig.DbCacheTimeOut, () => GetInsertBuilder(model).ToRows());
		public static ClassGradeModel Insert(ClassGradeModel model)
		{
			SetRedisCache(string.Format(CacheKey, model.Id), model, DbConfig.DbCacheTimeOut, () => GetInsertBuilder(model).ToRows(ref model));
			return model;
		}
		public static int Commit(IEnumerable<ClassGradeModel> models, bool isExceptionCancel = true)
		{
			if (models == null)
				throw new ArgumentNullException(nameof(models));
			var sqlbuilders = isExceptionCancel ? models.Select(f => GetInsertBuilder(f).ToRowsPipe()) :
				models.Select(f => GetInsertBuilder(f).WhereNotExists(Select.Where(a => a.Id == f.Id)).ToRowsPipe());
			return InsertMultiple<DbMaster>(models, sqlbuilders, DbConfig.DbCacheTimeOut, (model) => string.Format(CacheKey, model.Id));
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
		public static ClassGradeModel GetItem(Guid id) => GetRedisCache(string.Format(CacheKey, id), DbConfig.DbCacheTimeOut, () => Select.Where(a => a.Id == id).ToOne());
		public static List<ClassGradeModel> GetItems(IEnumerable<Guid> ids) => Select.WhereAny(a => a.Id, ids).ToList();

		#endregion

		#region Update
		public static UpdateBuilder<ClassGradeModel> Update(ClassGradeModel model) => Update(new[] { model.Id });
		public static UpdateBuilder<ClassGradeModel> Update(Guid id) => Update(new[] { id });
		public static UpdateBuilder<ClassGradeModel> Update(IEnumerable<ClassGradeModel> models) => Update(models.Select(a => a.Id));
		public static UpdateBuilder<ClassGradeModel> Update(IEnumerable<Guid> ids)
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

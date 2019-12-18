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
	[DbTable("class.grade")]
	public sealed partial class ClassGrade : SelectBuilder<ClassGrade, ClassGradeModel>
	{
		#region Properties
		public const string CacheKey = "meta_xunittest_model_classgrademodel_{0}";
		private ClassGrade() { }
		public static ClassGrade Select => new ClassGrade();
		public static ClassGrade SelectDiy(string fields) => new ClassGrade { Fields = fields };
		public static ClassGrade SelectDiy(string fields, string alias) => new ClassGrade { Fields = fields, MainAlias = alias };
		public static ClassGradeUpdateBuilder UpdateDiy => new ClassGradeUpdateBuilder();
		public static DeleteBuilder<ClassGradeModel> DeleteDiy => new DeleteBuilder<ClassGradeModel>();
		public static InsertBuilder<ClassGradeModel> InsertDiy => new InsertBuilder<ClassGradeModel>();
		#endregion

		#region Delete
		public static int Delete(ClassGradeModel model) => Delete(new[] { model.Id });
		public static int Delete(Guid id) => Delete(new[] { id });
		public static int Delete(IEnumerable<ClassGradeModel> models) => Delete(models.Select(a => a.Id));
		public static int Delete(IEnumerable<Guid> ids)
		{
			if (ids == null)
				throw new ArgumentNullException(nameof(ids));
			RedisHelper.Del(ids.Select(f => string.Format(CacheKey, f)).ToArray());
			return DeleteDiy.WhereOr("id = {0}", ids, NpgsqlDbType.Uuid).ToRows();
		}
		#endregion

		#region Insert
		public static int Commit(ClassGradeModel model) => SetRedisCache(string.Format(CacheKey, model.Id), model, DbConfig.DbCacheTimeOut, () => GetInsertBuilder(model).ToRows());
		public static ClassGradeModel Insert(ClassGradeModel model)
		{
			SetRedisCache(string.Format(CacheKey, model.Id), model, DbConfig.DbCacheTimeOut, () => GetInsertBuilder(model).ToRows(ref model));
			return model;
		}
		private static InsertBuilder<ClassGradeModel> GetInsertBuilder(ClassGradeModel model)
		{
			if (model == null)
				throw new ArgumentNullException(nameof(model));
			return InsertDiy
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
		public static ClassGradeUpdateBuilder Update(ClassGradeModel model) => Update(new[] { model.Id });
		public static ClassGradeUpdateBuilder Update(Guid id) => Update(new[] { id });
		public static ClassGradeUpdateBuilder Update(IEnumerable<ClassGradeModel> models) => Update(models.Select(a => a.Id));
		public static ClassGradeUpdateBuilder Update(IEnumerable<Guid> ids)
		{
			if (ids == null)
				throw new ArgumentNullException(nameof(ids));
			RedisHelper.Del(ids.Select(f => string.Format(CacheKey, f)).ToArray());
			return UpdateDiy.WhereOr("id = {0}", ids, NpgsqlDbType.Uuid);
		}
		public class ClassGradeUpdateBuilder : UpdateBuilder<ClassGradeUpdateBuilder, ClassGradeModel>
		{
		}
		#endregion

	}
}

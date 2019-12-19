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
	[DbTable("teacher")]
	public sealed partial class Teacher : SelectBuilder<Teacher, TeacherModel>
	{
		#region Properties
		public const string CacheKey = "meta_xunittest_model_teachermodel_{0}";
		private Teacher() { }
		public static Teacher Select => new Teacher();
		public static Teacher SelectDiy(string fields) => new Teacher { Fields = fields };
		public static Teacher SelectDiy(string fields, string alias) => new Teacher { Fields = fields, MainAlias = alias };
		public static TeacherUpdateBuilder UpdateDiy => new TeacherUpdateBuilder();
		public static DeleteBuilder<TeacherModel> DeleteDiy => new DeleteBuilder<TeacherModel>();
		public static InsertBuilder<TeacherModel> InsertDiy => new InsertBuilder<TeacherModel>();
		#endregion

		#region Delete
		public static int Delete(TeacherModel model) => Delete(new[] { model.Id });
		public static int Delete(Guid id) => Delete(new[] { id });
		public static int Delete(IEnumerable<TeacherModel> models) => Delete(models.Select(a => a.Id));
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
		public static int Commit(TeacherModel model) => SetRedisCache(string.Format(CacheKey, model.Id), model, DbConfig.DbCacheTimeOut, () => GetInsertBuilder(model).ToRows());
		public static TeacherModel Insert(TeacherModel model)
		{
			SetRedisCache(string.Format(CacheKey, model.Id), model, DbConfig.DbCacheTimeOut, () => GetInsertBuilder(model).ToRows(ref model));
			return model;
		}
		private static InsertBuilder<TeacherModel> GetInsertBuilder(TeacherModel model)
		{
			if (model == null)
				throw new ArgumentNullException(nameof(model));
			return InsertDiy
				.Set(a => a.Teacher_no, model.Teacher_no)
				.Set(a => a.People_id, model.People_id)
				.Set(a => a.Create_time, model.Create_time = model.Create_time.Ticks == 0 ? DateTime.Now : model.Create_time)
				.Set(a => a.Id, model.Id = model.Id == Guid.Empty ? Guid.NewGuid() : model.Id);
		}
		#endregion

		#region Select
		public static TeacherModel GetItem(Guid id) => GetRedisCache(string.Format(CacheKey, id), DbConfig.DbCacheTimeOut, () => Select.Where(a => a.Id == id).ToOne());
		public static List<TeacherModel> GetItems(IEnumerable<Guid> ids) => Select.WhereAny(a => a.Id, ids).ToList();
		public static TeacherModel GetItemByTeacher_no(string teacher_no) => Select.Where(a => a.Teacher_no == teacher_no).ToOne();
		public static List<TeacherModel> GetItemsByTeacher_no(IEnumerable<string> teacher_nos) => Select.WhereAny(a => a.Teacher_no, teacher_nos).ToList();
		public static TeacherModel GetItemByPeople_id(Guid people_id) => Select.Where(a => a.People_id == people_id).ToOne();
		public static List<TeacherModel> GetItemsByPeople_id(IEnumerable<Guid> people_ids) => Select.WhereAny(a => a.People_id, people_ids).ToList();

		#endregion

		#region Update
		public static TeacherUpdateBuilder Update(TeacherModel model) => Update(new[] { model.Id });
		public static TeacherUpdateBuilder Update(Guid id) => Update(new[] { id });
		public static TeacherUpdateBuilder Update(IEnumerable<TeacherModel> models) => Update(models.Select(a => a.Id));
		public static TeacherUpdateBuilder Update(IEnumerable<Guid> ids)
		{
			if (ids == null)
				throw new ArgumentNullException(nameof(ids));
			if (DbConfig.DbCacheTimeOut != 0)
				RedisHelper.Del(ids.Select(f => string.Format(CacheKey, f)).ToArray());
			return UpdateDiy.WhereOr("id = {0}", ids, NpgsqlDbType.Uuid);
		}
		public class TeacherUpdateBuilder : UpdateBuilder<TeacherUpdateBuilder, TeacherModel>
		{
		}
		#endregion

	}
}

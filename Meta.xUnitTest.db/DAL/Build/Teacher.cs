using Meta.Common.SqlBuilder;
using Meta.Common.Model;
using Meta.xUnitTest.Model;
using Meta.xUnitTest.Options;
using NpgsqlTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using System.Net;

namespace Meta.xUnitTest.DAL
{
	[Mapping("teacher")]
	public partial class Teacher : SelectExchange<Teacher, TeacherModel>
	{
		#region Properties
		public const string CacheKey = "meta_xunittest_model_teachermodel_{0}";
		public static Teacher Select => new Teacher();
		public static Teacher SelectDiy(string fields) => new Teacher { Fields = fields };
		public static Teacher SelectDiy(string fields, string alias) => new Teacher { Fields = fields, MainAlias = alias };
		public static TeacherUpdateBuilder UpdateDiy => new TeacherUpdateBuilder();
		public static DeleteBuilder DeleteDiy => new DeleteBuilder("teacher");
		public static InsertBuilder InsertDiy => new InsertBuilder("teacher");
		#endregion

		#region Delete
		public static int Delete(TeacherModel model) => Delete(new[] { model.Id });
		public static int Delete(Guid id) => Delete(new[] { id });
		public static int Delete(IEnumerable<TeacherModel> models) => Delete(models.Select(a => a.Id));
		public static int Delete(IEnumerable<Guid> ids)
		{
			if (ids == null)
				throw new ArgumentNullException(nameof(ids));
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
		private static InsertBuilder GetInsertBuilder(TeacherModel model)
		{
			if (model == null)
				throw new ArgumentNullException(nameof(model));
			return InsertDiy
				.Set("teacher_no", model.Teacher_no, 32, NpgsqlDbType.Varchar)
				.Set("people_id", model.People_id, 16, NpgsqlDbType.Uuid)
				.Set("create_time", model.Create_time = model.Create_time.Ticks == 0 ? DateTime.Now : model.Create_time, 8, NpgsqlDbType.Timestamp)
				.Set("id", model.Id = model.Id == Guid.Empty ? Guid.NewGuid() : model.Id, 16, NpgsqlDbType.Uuid);
		}
		#endregion

		#region Select
		public static TeacherModel GetItem(Guid id) => GetRedisCache(string.Format(CacheKey, id), DbConfig.DbCacheTimeOut, () => Select.WhereId(id).ToOne());
		public static List<TeacherModel> GetItems(IEnumerable<Guid> id) => Select.WhereId(id.ToArray()).ToList();
		public static TeacherModel GetItemByTeacher_no(string teacher_no) => Select.WhereTeacher_no(teacher_no).ToOne();
		public static List<TeacherModel> GetItemsByTeacher_no(IEnumerable<string> teacher_nos) => Select.WhereTeacher_no(teacher_nos.ToArray()).ToList();
		public static TeacherModel GetItemByPeople_id(Guid people_id) => Select.WherePeople_id(people_id).ToOne();
		public static List<TeacherModel> GetItemsByPeople_id(IEnumerable<Guid> people_ids) => Select.WherePeople_id(people_ids.ToArray()).ToList();
		public Teacher WhereTeacher_no(params string[] teacher_no) => WhereOr($"{MainAlias}.teacher_no = {{0}}", teacher_no, NpgsqlDbType.Varchar);
		public Teacher WhereTeacher_noLike(params string[] teacher_no) => WhereOr($"{MainAlias}.teacher_no LIKE {{0}}", teacher_no.Select(a => $"%{a}%"), NpgsqlDbType.Varchar);
		public Teacher WherePeople_id(params Guid[] people_id) => WhereOr($"{MainAlias}.people_id = {{0}}", people_id, NpgsqlDbType.Uuid);
		public Teacher WhereCreate_timeRange(DateTime? begin = null, DateTime? end = null) => Where($"{MainAlias}.create_time BETWEEN {{0}} AND {{1}}", begin ?? DateTime.Parse("1970-1-1"), end ?? DateTime.Now);
		public Teacher WhereId(params Guid[] id) => WhereOr($"{MainAlias}.id = {{0}}", id, NpgsqlDbType.Uuid);

		#endregion

		#region Update
		public static TeacherUpdateBuilder Update(TeacherModel model) => Update(new[] { model.Id });
		public static TeacherUpdateBuilder Update(Guid id) => Update(new[] { id });
		public static TeacherUpdateBuilder Update(IEnumerable<TeacherModel> models) => Update(models.Select(a => a.Id));
		public static TeacherUpdateBuilder Update(IEnumerable<Guid> ids)
		{
			if (ids == null)
				throw new ArgumentNullException(nameof(ids));
			RedisHelper.Del(ids.Select(f => string.Format(CacheKey, f)).ToArray());
			return UpdateDiy.WhereOr("id = {0}", ids, NpgsqlDbType.Uuid);
		}
		public class TeacherUpdateBuilder : UpdateBuilder<TeacherUpdateBuilder, TeacherModel>
		{
			public TeacherUpdateBuilder SetTeacher_no(string teacher_no) => Set("teacher_no", teacher_no, 32, NpgsqlDbType.Varchar);
			public TeacherUpdateBuilder SetPeople_id(Guid people_id) => Set("people_id", people_id, 16, NpgsqlDbType.Uuid);
			public TeacherUpdateBuilder SetCreate_time(DateTime create_time) => Set("create_time", create_time, 8, NpgsqlDbType.Timestamp);
			public TeacherUpdateBuilder SetCreate_timeIncrement(TimeSpan timeSpan) => SetIncrement("create_time", timeSpan, 8, NpgsqlDbType.Timestamp);
			public TeacherUpdateBuilder SetId(Guid id) => Set("id", id, 16, NpgsqlDbType.Uuid);
		}
		#endregion

	}
}

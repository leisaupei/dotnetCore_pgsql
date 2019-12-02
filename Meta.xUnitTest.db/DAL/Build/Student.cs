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
	[Mapping("student")]
	public partial class Student : SelectExchange<Student, StudentModel>
	{
		#region Properties
		public const string CacheKey = "meta_xunittest_model_studentmodel_{0}";
		public static Student Select => new Student();
		public static Student SelectDiy(string fields) => new Student { Fields = fields };
		public static Student SelectDiy(string fields, string alias) => new Student { Fields = fields, MainAlias = alias };
		public static StudentUpdateBuilder UpdateDiy => new StudentUpdateBuilder();
		public static DeleteBuilder DeleteDiy => new DeleteBuilder("student");
		public static InsertBuilder InsertDiy => new InsertBuilder("student");
		#endregion

		#region Delete
		public static int Delete(StudentModel model) => Delete(new[] { model.Id });
		public static int Delete(Guid id) => Delete(new[] { id });
		public static int Delete(IEnumerable<StudentModel> models) => Delete(models.Select(a => a.Id));
		public static int Delete(IEnumerable<Guid> ids)
		{
			if (ids == null)
				throw new ArgumentNullException(nameof(ids));
			RedisHelper.Del(ids.Select(f => string.Format(CacheKey, f)).ToArray());
			return DeleteDiy.WhereOr("id = {0}", ids, NpgsqlDbType.Uuid).ToRows();
		}
		#endregion

		#region Insert
		public static int Commit(StudentModel model) => SetRedisCache(string.Format(CacheKey, model.Id), model, DbConfig.DbCacheTimeOut, () => GetInsertBuilder(model).ToRows());
		public static StudentModel Insert(StudentModel model)
		{
			SetRedisCache(string.Format(CacheKey, model.Id), model, DbConfig.DbCacheTimeOut, () => GetInsertBuilder(model).ToRows(ref model));
			return model;
		}
		private static InsertBuilder GetInsertBuilder(StudentModel model)
		{
			if (model == null)
				throw new ArgumentNullException(nameof(model));
			return InsertDiy
				.Set("stu_no", model.Stu_no, 32, NpgsqlDbType.Varchar)
				.Set("grade_id", model.Grade_id, 16, NpgsqlDbType.Uuid)
				.Set("people_id", model.People_id, 16, NpgsqlDbType.Uuid)
				.Set("create_time", model.Create_time = model.Create_time.Ticks == 0 ? DateTime.Now : model.Create_time, 8, NpgsqlDbType.Timestamp)
				.Set("id", model.Id = model.Id == Guid.Empty ? Guid.NewGuid() : model.Id, 16, NpgsqlDbType.Uuid);
		}
		#endregion

		#region Select
		public static StudentModel GetItem(Guid id) => GetRedisCache(string.Format(CacheKey, id), DbConfig.DbCacheTimeOut, () => Select.WhereId(id).ToOne());
		public static List<StudentModel> GetItems(IEnumerable<Guid> id) => Select.WhereId(id.ToArray()).ToList();
		public static StudentModel GetItemByStu_no(string stu_no) => Select.WhereStu_no(stu_no).ToOne();
		public static List<StudentModel> GetItemsByStu_no(IEnumerable<string> stu_nos) => Select.WhereStu_no(stu_nos.ToArray()).ToList();
		public static StudentModel GetItemByPeople_id(Guid people_id) => Select.WherePeople_id(people_id).ToOne();
		public static List<StudentModel> GetItemsByPeople_id(IEnumerable<Guid> people_ids) => Select.WherePeople_id(people_ids.ToArray()).ToList();
		public Student WhereStu_no(params string[] stu_no) => WhereOr($"{MainAlias}.stu_no = {{0}}", stu_no, NpgsqlDbType.Varchar);
		public Student WhereStu_noLike(params string[] stu_no) => WhereOr($"{MainAlias}.stu_no LIKE {{0}}", stu_no.Select(a => $"%{a}%"), NpgsqlDbType.Varchar);
		public Student WhereGrade_id(params Guid[] grade_id) => WhereOr($"{MainAlias}.grade_id = {{0}}", grade_id, NpgsqlDbType.Uuid);
		public Student WherePeople_id(params Guid[] people_id) => WhereOr($"{MainAlias}.people_id = {{0}}", people_id, NpgsqlDbType.Uuid);
		public Student WhereCreate_timeRange(DateTime? begin = null, DateTime? end = null) => Where($"{MainAlias}.create_time BETWEEN {{0}} AND {{1}}", begin ?? DateTime.Parse("1970-1-1"), end ?? DateTime.Now);
		public Student WhereId(params Guid[] id) => WhereOr($"{MainAlias}.id = {{0}}", id, NpgsqlDbType.Uuid);

		#endregion

		#region Update
		public static StudentUpdateBuilder Update(StudentModel model) => Update(new[] { model.Id });
		public static StudentUpdateBuilder Update(Guid id) => Update(new[] { id });
		public static StudentUpdateBuilder Update(IEnumerable<StudentModel> models) => Update(models.Select(a => a.Id));
		public static StudentUpdateBuilder Update(IEnumerable<Guid> ids)
		{
			if (ids == null)
				throw new ArgumentNullException(nameof(ids));
			RedisHelper.Del(ids.Select(f => string.Format(CacheKey, f)).ToArray());
			return UpdateDiy.WhereOr("id = {0}", ids, NpgsqlDbType.Uuid);
		}
		public class StudentUpdateBuilder : UpdateBuilder<StudentUpdateBuilder, StudentModel>
		{
			public StudentUpdateBuilder SetStu_no(string stu_no) => Set("stu_no", stu_no, 32, NpgsqlDbType.Varchar);
			public StudentUpdateBuilder SetGrade_id(Guid grade_id) => Set("grade_id", grade_id, 16, NpgsqlDbType.Uuid);
			public StudentUpdateBuilder SetPeople_id(Guid people_id) => Set("people_id", people_id, 16, NpgsqlDbType.Uuid);
			public StudentUpdateBuilder SetCreate_time(DateTime create_time) => Set("create_time", create_time, 8, NpgsqlDbType.Timestamp);
			public StudentUpdateBuilder SetCreate_timeIncrement(TimeSpan timeSpan) => SetIncrement("create_time", timeSpan, 8, NpgsqlDbType.Timestamp);
			public StudentUpdateBuilder SetId(Guid id) => Set("id", id, 16, NpgsqlDbType.Uuid);
		}
		#endregion

	}
}

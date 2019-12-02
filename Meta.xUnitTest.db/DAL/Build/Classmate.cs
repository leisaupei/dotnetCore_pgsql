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
	[Mapping("classmate")]
	public partial class Classmate : SelectExchange<Classmate, ClassmateModel>
	{
		#region Properties
		public const string CacheKey = "meta_xunittest_model_classmatemodel_{0}_{1}_{2}";
		public static Classmate Select => new Classmate();
		public static Classmate SelectDiy(string fields) => new Classmate { Fields = fields };
		public static Classmate SelectDiy(string fields, string alias) => new Classmate { Fields = fields, MainAlias = alias };
		public static ClassmateUpdateBuilder UpdateDiy => new ClassmateUpdateBuilder();
		public static DeleteBuilder DeleteDiy => new DeleteBuilder("classmate");
		public static InsertBuilder InsertDiy => new InsertBuilder("classmate");
		#endregion

		#region Delete
		public static int Delete(ClassmateModel model) => Delete(new[] { (model.Teacher_id, model.Student_id, model.Grade_id) });
		public static int Delete(Guid teacher_id, Guid student_id, Guid grade_id) => Delete(new[] { (teacher_id, student_id, grade_id) });
		public static int Delete(IEnumerable<ClassmateModel> models) =>  Delete(models.Select(a => (a.Teacher_id, a.Student_id, a.Grade_id)));
		/// <summary>
		/// (teacher_id, student_id, grade_id)
		/// </summary>
		public static int Delete(IEnumerable<(Guid, Guid, Guid)> val)
		{
			if (val == null)
				throw new ArgumentNullException(nameof(val));
			RedisHelper.Del(val.Select(f => string.Format(CacheKey, f.Item1, f.Item2, f.Item3)).ToArray());
			return DeleteDiy.Where(new[] { "teacher_id", "student_id", "grade_id" }, val, new NpgsqlDbType?[]{ NpgsqlDbType.Uuid, NpgsqlDbType.Uuid, NpgsqlDbType.Uuid }).ToRows();
		}
		#endregion

		#region Insert
		public static int Commit(ClassmateModel model) => SetRedisCache(string.Format(CacheKey, model.Teacher_id, model.Student_id, model.Grade_id), model, DbConfig.DbCacheTimeOut, () => GetInsertBuilder(model).ToRows());
		public static ClassmateModel Insert(ClassmateModel model)
		{
			SetRedisCache(string.Format(CacheKey, model.Teacher_id, model.Student_id, model.Grade_id), model, DbConfig.DbCacheTimeOut, () => GetInsertBuilder(model).ToRows(ref model));
			return model;
		}
		private static InsertBuilder GetInsertBuilder(ClassmateModel model)
		{
			if (model == null)
				throw new ArgumentNullException(nameof(model));
			return InsertDiy
				.Set("teacher_id", model.Teacher_id, 16, NpgsqlDbType.Uuid)
				.Set("student_id", model.Student_id, 16, NpgsqlDbType.Uuid)
				.Set("grade_id", model.Grade_id, 16, NpgsqlDbType.Uuid)
				.Set("create_time", model.Create_time ??= DateTime.Now, 8, NpgsqlDbType.Timestamp);
		}
		#endregion

		#region Select
		public static ClassmateModel GetItem(Guid teacher_id, Guid student_id, Guid grade_id) => GetRedisCache(string.Format(CacheKey, teacher_id, student_id, grade_id), DbConfig.DbCacheTimeOut, () => Select.WhereTeacher_id(teacher_id).WhereStudent_id(student_id).WhereGrade_id(grade_id).ToOne());
		/// <summary>
		/// (teacher_id, student_id, grade_id)
		/// </summary>
		public static List<ClassmateModel> GetItems(IEnumerable<(Guid, Guid, Guid)> val) => Select.Where(new[] { "teacher_id", "student_id", "grade_id" }, val, new NpgsqlDbType?[]{ NpgsqlDbType.Uuid, NpgsqlDbType.Uuid, NpgsqlDbType.Uuid }).ToList();
		public Classmate WhereTeacher_id(params Guid[] teacher_id) => WhereOr($"{MainAlias}.teacher_id = {{0}}", teacher_id, NpgsqlDbType.Uuid);
		public Classmate WhereStudent_id(params Guid[] student_id) => WhereOr($"{MainAlias}.student_id = {{0}}", student_id, NpgsqlDbType.Uuid);
		public Classmate WhereGrade_id(params Guid[] grade_id) => WhereOr($"{MainAlias}.grade_id = {{0}}", grade_id, NpgsqlDbType.Uuid);
		public Classmate WhereCreate_timeRange(DateTime? begin = null, DateTime? end = null) => Where($"{MainAlias}.create_time BETWEEN {{0}} AND {{1}}", begin ?? DateTime.Parse("1970-1-1"), end ?? DateTime.Now);

		#endregion

		#region Update
		public static ClassmateUpdateBuilder Update(ClassmateModel model) => Update(new[] { (model.Teacher_id, model.Student_id, model.Grade_id) });
		public static ClassmateUpdateBuilder Update(Guid teacher_id,Guid student_id,Guid grade_id) => Update(new[] { (teacher_id, student_id, grade_id) });
		public static ClassmateUpdateBuilder Update(IEnumerable<ClassmateModel> models) => Update(models.Select(a => (a.Teacher_id, a.Student_id, a.Grade_id)));
		/// <summary>
		/// (teacher_id, student_id, grade_id)
		/// </summary>
		public static ClassmateUpdateBuilder Update(IEnumerable<(Guid, Guid, Guid)> val)
		{
			if (val == null)
				throw new ArgumentNullException(nameof(val));
			RedisHelper.Del(val.Select(f => string.Format(CacheKey, f.Item1, f.Item2, f.Item3)).ToArray());
			return UpdateDiy.Where(new[] { "teacher_id", "student_id", "grade_id" }, val, new NpgsqlDbType?[]{ NpgsqlDbType.Uuid, NpgsqlDbType.Uuid, NpgsqlDbType.Uuid });
		}
		public class ClassmateUpdateBuilder : UpdateBuilder<ClassmateUpdateBuilder, ClassmateModel>
		{
			public ClassmateUpdateBuilder SetTeacher_id(Guid teacher_id) => Set("teacher_id", teacher_id, 16, NpgsqlDbType.Uuid);
			public ClassmateUpdateBuilder SetStudent_id(Guid student_id) => Set("student_id", student_id, 16, NpgsqlDbType.Uuid);
			public ClassmateUpdateBuilder SetGrade_id(Guid grade_id) => Set("grade_id", grade_id, 16, NpgsqlDbType.Uuid);
			public ClassmateUpdateBuilder SetCreate_time(DateTime? create_time) => Set("create_time", create_time, 8, NpgsqlDbType.Timestamp);
			public ClassmateUpdateBuilder SetCreate_timeIncrement(TimeSpan timeSpan) => SetIncrement("create_time", timeSpan, 8, NpgsqlDbType.Timestamp);
		}
		#endregion

	}
}

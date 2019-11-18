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
		public static int Delete(IEnumerable<Guid> id) => DeleteDiy.WhereOr("id = {0}", id, NpgsqlDbType.Uuid).Commit();
		#endregion

		#region Insert
		public static int Commit(StudentModel model) => GetInsertBuilder(model).Commit();
		public static StudentModel Insert(StudentModel model) => GetInsertBuilder(model).Commit<StudentModel>();
		private static InsertBuilder GetInsertBuilder(StudentModel model)
		{
			return InsertDiy
				.Set("id", model.Id == Guid.Empty ? Guid.NewGuid() : model.Id, 16, NpgsqlDbType.Uuid)
				.Set("stu_no", model.Stu_no, 32, NpgsqlDbType.Varchar)
				.Set("grade_id", model.Grade_id, 16, NpgsqlDbType.Uuid);
		}
		#endregion

		#region Select
		public static StudentModel GetItem(Guid id) => Select.WhereId(id).ToOne();
		public static List<StudentModel> GetItems(IEnumerable<Guid> id) => Select.WhereOr("id = {0}", id, NpgsqlDbType.Uuid).ToList();
		public Student WhereId(params Guid[] id) => WhereOr($"{MainAlias}.id = {{0}}", id, NpgsqlDbType.Uuid);
		public Student WhereStu_no(params string[] stu_no) => WhereOr($"{MainAlias}.stu_no = {{0}}", stu_no, NpgsqlDbType.Varchar);
		public Student WhereStu_noLike(params string[] stu_no) => WhereOr($"{MainAlias}.stu_no LIKE {{0}}", stu_no.Select(a => $"%{a}%"), NpgsqlDbType.Varchar);
		public Student WhereGrade_id(params Guid[] grade_id) => WhereOr($"{MainAlias}.grade_id = {{0}}", grade_id, NpgsqlDbType.Uuid);

		#endregion

		#region Update
		public static StudentUpdateBuilder Update(StudentModel model) => Update(new[] { model.Id });
		public static StudentUpdateBuilder Update(Guid id) => Update(new[] { id });
		public static StudentUpdateBuilder Update(IEnumerable<StudentModel> models) => Update(models.Select(a => a.Id));
		public static StudentUpdateBuilder Update(IEnumerable<Guid> ids) => UpdateDiy.WhereOr("id = {0}", ids, NpgsqlDbType.Uuid);
		public class StudentUpdateBuilder : UpdateBuilder<StudentUpdateBuilder, StudentModel>
		{
			public StudentUpdateBuilder SetId(Guid id) => Set("id", id, 16, NpgsqlDbType.Uuid);
			public StudentUpdateBuilder SetStu_no(string stu_no) => Set("stu_no", stu_no, 32, NpgsqlDbType.Varchar);
			public StudentUpdateBuilder SetGrade_id(Guid grade_id) => Set("grade_id", grade_id, 16, NpgsqlDbType.Uuid);
		}
		#endregion

	}
}

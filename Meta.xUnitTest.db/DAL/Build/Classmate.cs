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
	[DbTable("classmate")]
	public sealed partial class Classmate : SelectBuilder<Classmate, ClassmateModel>
	{
		#region Properties
		public const string CacheKey = "meta_xunittest_model_classmatemodel_{0}_{1}_{2}";
		private Classmate() { }
		public static Classmate Select => new Classmate();
		public static Classmate SelectDiy(string fields) => new Classmate { Fields = fields };
		public static Classmate SelectDiy(string fields, string alias) => new Classmate { Fields = fields, MainAlias = alias };
		public static ClassmateUpdateBuilder UpdateDiy => new ClassmateUpdateBuilder();
		public static DeleteBuilder<ClassmateModel> DeleteDiy => new DeleteBuilder<ClassmateModel>();
		public static InsertBuilder<ClassmateModel> InsertDiy => new InsertBuilder<ClassmateModel>();
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
		private static InsertBuilder<ClassmateModel> GetInsertBuilder(ClassmateModel model)
		{
			if (model == null)
				throw new ArgumentNullException(nameof(model));
			return InsertDiy
				.Set(a => a.Teacher_id, model.Teacher_id)
				.Set(a => a.Student_id, model.Student_id)
				.Set(a => a.Grade_id, model.Grade_id)
				.Set(a => a.Create_time, model.Create_time ??= DateTime.Now);
		}
		#endregion

		#region Select
		public static ClassmateModel GetItem(Guid teacher_id, Guid student_id, Guid grade_id) => GetRedisCache(string.Format(CacheKey, teacher_id, student_id, grade_id), DbConfig.DbCacheTimeOut, () => Select.Where(a => a.Teacher_id == teacher_id && a.Student_id == student_id && a.Grade_id == grade_id).ToOne());
		/// <summary>
		/// (teacher_id, student_id, grade_id)
		/// </summary>
		public static List<ClassmateModel> GetItems(IEnumerable<(Guid, Guid, Guid)> val) => Select.Where(new[] { "teacher_id", "student_id", "grade_id" }, val, new NpgsqlDbType?[]{ NpgsqlDbType.Uuid, NpgsqlDbType.Uuid, NpgsqlDbType.Uuid }).ToList();

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
		}
		#endregion

	}
}

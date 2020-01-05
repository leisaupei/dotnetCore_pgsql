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
	[DbTable("student")]
	public sealed partial class Student : SelectBuilder<Student, StudentModel>
	{
		#region Properties
		public const string CacheKey = "meta_xunittest_model_studentmodel_{0}";
		private Student() { }
		public static Student Select => new Student();
		public static Student SelectDiy(string fields) => new Student { Fields = fields };
		public static Student SelectDiy(string fields, string alias) => new Student { Fields = fields, MainAlias = alias };
		public static UpdateBuilder<StudentModel> UpdateDiy => new UpdateBuilder<StudentModel>();
		public static DeleteBuilder<StudentModel> DeleteDiy => new DeleteBuilder<StudentModel>();
		public static InsertBuilder<StudentModel> InsertDiy => new InsertBuilder<StudentModel>();
		#endregion

		#region Delete
		public static int Delete(StudentModel model) => Delete(new[] { model.Id });
		public static int Delete(Guid id) => Delete(new[] { id });
		public static int Delete(IEnumerable<StudentModel> models) => Delete(models.Select(a => a.Id));
		public static int Delete(IEnumerable<Guid> ids)
		{
			if (ids == null)
				throw new ArgumentNullException(nameof(ids));
			if (DbConfig.DbCacheTimeOut != 0)
				RedisHelper.Del(ids.Select(f => string.Format(CacheKey, f)).ToArray());
			return DeleteDiy.WhereAny(a => a.Id, ids).ToRows();
		}
		#endregion

		#region Insert
		public static int Commit(StudentModel model) => SetRedisCache(string.Format(CacheKey, model.Id), model, DbConfig.DbCacheTimeOut, () => GetInsertBuilder(model).ToRows());
		public static StudentModel Insert(StudentModel model)
		{
			SetRedisCache(string.Format(CacheKey, model.Id), model, DbConfig.DbCacheTimeOut, () => GetInsertBuilder(model).ToRows(ref model));
			return model;
		}
		public static int Commit(IEnumerable<StudentModel> models, bool isExceptionCancel = true)
		{
			if (models == null)
				throw new ArgumentNullException(nameof(models));
			var sqlbuilders = isExceptionCancel ? models.Select(f => GetInsertBuilder(f).ToRowsPipe()) :
				models.Select(f => GetInsertBuilder(f).WhereNotExists(Select.Where(a => a.Id == f.Id)).ToRowsPipe());
			return InsertMultiple(models, sqlbuilders, DbOptions.Master, DbConfig.DbCacheTimeOut, (model) => string.Format(CacheKey, model.Id));
		}
		private static InsertBuilder<StudentModel> GetInsertBuilder(StudentModel model)
		{
			if (model == null)
				throw new ArgumentNullException(nameof(model));
			return InsertDiy
				.Set(a => a.Stu_no, model.Stu_no)
				.Set(a => a.Grade_id, model.Grade_id)
				.Set(a => a.People_id, model.People_id)
				.Set(a => a.Create_time, model.Create_time = model.Create_time.Ticks == 0 ? DateTime.Now : model.Create_time)
				.Set(a => a.Id, model.Id = model.Id == Guid.Empty ? Guid.NewGuid() : model.Id);
		}
		#endregion

		#region Select
		public static StudentModel GetItem(Guid id) => GetRedisCache(string.Format(CacheKey, id), DbConfig.DbCacheTimeOut, () => Select.Where(a => a.Id == id).ToOne());
		public static List<StudentModel> GetItems(IEnumerable<Guid> ids) => Select.WhereAny(a => a.Id, ids).ToList();
		public static StudentModel GetItemByStu_no(string stu_no) => Select.Where(a => a.Stu_no == stu_no).ToOne();
		public static List<StudentModel> GetItemsByStu_no(IEnumerable<string> stu_nos) => Select.WhereAny(a => a.Stu_no, stu_nos).ToList();
		public static StudentModel GetItemByPeople_id(Guid people_id) => Select.Where(a => a.People_id == people_id).ToOne();
		public static List<StudentModel> GetItemsByPeople_id(IEnumerable<Guid> people_ids) => Select.WhereAny(a => a.People_id, people_ids).ToList();

		#endregion

		#region Update
		public static UpdateBuilder<StudentModel> Update(StudentModel model) => Update(new[] { model.Id });
		public static UpdateBuilder<StudentModel> Update(Guid id) => Update(new[] { id });
		public static UpdateBuilder<StudentModel> Update(IEnumerable<StudentModel> models) => Update(models.Select(a => a.Id));
		public static UpdateBuilder<StudentModel> Update(IEnumerable<Guid> ids)
		{
			if (ids == null)
				throw new ArgumentNullException(nameof(ids));
			if (DbConfig.DbCacheTimeOut != 0)
				RedisHelper.Del(ids.Select(f => string.Format(CacheKey, f)).ToArray());
			return UpdateDiy.WhereAny(a => a.Id, ids);
		}
		#endregion

	}
}

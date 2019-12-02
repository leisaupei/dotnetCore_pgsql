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
	[Mapping("class.grade")]
	public partial class ClassGrade : SelectExchange<ClassGrade, ClassGradeModel>
	{
		#region Properties
		public const string CacheKey = "meta_xunittest_model_classgrademodel_{0}";
		public static ClassGrade Select => new ClassGrade();
		public static ClassGrade SelectDiy(string fields) => new ClassGrade { Fields = fields };
		public static ClassGrade SelectDiy(string fields, string alias) => new ClassGrade { Fields = fields, MainAlias = alias };
		public static ClassGradeUpdateBuilder UpdateDiy => new ClassGradeUpdateBuilder();
		public static DeleteBuilder DeleteDiy => new DeleteBuilder("class.grade");
		public static InsertBuilder InsertDiy => new InsertBuilder("class.grade");
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
		private static InsertBuilder GetInsertBuilder(ClassGradeModel model)
		{
			if (model == null)
				throw new ArgumentNullException(nameof(model));
			return InsertDiy
				.Set("id", model.Id = model.Id == Guid.Empty ? Guid.NewGuid() : model.Id, 16, NpgsqlDbType.Uuid)
				.Set("name", model.Name, 255, NpgsqlDbType.Varchar)
				.Set("create_time", model.Create_time = model.Create_time.Ticks == 0 ? DateTime.Now : model.Create_time, 8, NpgsqlDbType.Timestamp);
		}
		#endregion

		#region Select
		public static ClassGradeModel GetItem(Guid id) => GetRedisCache(string.Format(CacheKey, id), DbConfig.DbCacheTimeOut, () => Select.WhereId(id).ToOne());
		public static List<ClassGradeModel> GetItems(IEnumerable<Guid> id) => Select.WhereId(id.ToArray()).ToList();
		public ClassGrade WhereId(params Guid[] id) => WhereOr($"{MainAlias}.id = {{0}}", id, NpgsqlDbType.Uuid);
		public ClassGrade WhereName(params string[] name) => WhereOr($"{MainAlias}.name = {{0}}", name, NpgsqlDbType.Varchar);
		public ClassGrade WhereNameLike(params string[] name) => WhereOr($"{MainAlias}.name LIKE {{0}}", name.Select(a => $"%{a}%"), NpgsqlDbType.Varchar);
		public ClassGrade WhereCreate_timeRange(DateTime? begin = null, DateTime? end = null) => Where($"{MainAlias}.create_time BETWEEN {{0}} AND {{1}}", begin ?? DateTime.Parse("1970-1-1"), end ?? DateTime.Now);

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
			public ClassGradeUpdateBuilder SetId(Guid id) => Set("id", id, 16, NpgsqlDbType.Uuid);
			public ClassGradeUpdateBuilder SetName(string name) => Set("name", name, 255, NpgsqlDbType.Varchar);
			public ClassGradeUpdateBuilder SetCreate_time(DateTime create_time) => Set("create_time", create_time, 8, NpgsqlDbType.Timestamp);
			public ClassGradeUpdateBuilder SetCreate_timeIncrement(TimeSpan timeSpan) => SetIncrement("create_time", timeSpan, 8, NpgsqlDbType.Timestamp);
		}
		#endregion

	}
}

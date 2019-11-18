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
	[Mapping("people")]
	public partial class People : SelectExchange<People, PeopleModel>
	{
		#region Properties
		public static People Select => new People();
		public static People SelectDiy(string fields) => new People { Fields = fields };
		public static People SelectDiy(string fields, string alias) => new People { Fields = fields, MainAlias = alias };
		public static PeopleUpdateBuilder UpdateDiy => new PeopleUpdateBuilder();
		public static DeleteBuilder DeleteDiy => new DeleteBuilder("people");
		public static InsertBuilder InsertDiy => new InsertBuilder("people");
		#endregion

		#region Delete
		public static int Delete(PeopleModel model) => Delete(new[] { model.Id });
		public static int Delete(Guid id) => Delete(new[] { id });
		public static int Delete(IEnumerable<PeopleModel> models) => Delete(models.Select(a => a.Id));
		public static int Delete(IEnumerable<Guid> id) => DeleteDiy.WhereOr("id = {0}", id, NpgsqlDbType.Uuid).Commit();
		#endregion

		#region Insert
		public static int Commit(PeopleModel model) => GetInsertBuilder(model).Commit();
		public static PeopleModel Insert(PeopleModel model) => GetInsertBuilder(model).Commit<PeopleModel>();
		private static InsertBuilder GetInsertBuilder(PeopleModel model)
		{
			return InsertDiy
				.Set("id", model.Id == Guid.Empty ? Guid.NewGuid() : model.Id, 16, NpgsqlDbType.Uuid)
				.Set("age", model.Age, 4, NpgsqlDbType.Integer)
				.Set("name", model.Name, 255, NpgsqlDbType.Varchar)
				.Set("sex", model.Sex, 1, NpgsqlDbType.Boolean);
		}
		#endregion

		#region Select
		public static PeopleModel GetItem(Guid id) => Select.WhereId(id).ToOne();
		public static List<PeopleModel> GetItems(IEnumerable<Guid> id) => Select.WhereOr("id = {0}", id, NpgsqlDbType.Uuid).ToList();
		public People WhereId(params Guid[] id) => WhereOr($"{MainAlias}.id = {{0}}", id, NpgsqlDbType.Uuid);
		public People WhereAge(params int[] age) => WhereOr($"{MainAlias}.age = {{0}}", age, NpgsqlDbType.Integer);
		public People WhereAgeThan(int val, string sqlOperator = ">") => Where($"{MainAlias}.age {sqlOperator} {{0}}", new DbTypeValue(val, NpgsqlDbType.Integer));
		public People WhereName(params string[] name) => WhereOr($"{MainAlias}.name = {{0}}", name, NpgsqlDbType.Varchar);
		public People WhereNameLike(params string[] name) => WhereOr($"{MainAlias}.name LIKE {{0}}", name.Select(a => $"%{a}%"), NpgsqlDbType.Varchar);
		public People WhereSex(params bool[] sex) => WhereOr($"{MainAlias}.sex = {{0}}", sex, NpgsqlDbType.Boolean);

		#endregion

		#region Update
		public static PeopleUpdateBuilder Update(PeopleModel model) => Update(new[] { model.Id });
		public static PeopleUpdateBuilder Update(Guid id) => Update(new[] { id });
		public static PeopleUpdateBuilder Update(IEnumerable<PeopleModel> models) => Update(models.Select(a => a.Id));
		public static PeopleUpdateBuilder Update(IEnumerable<Guid> ids) => UpdateDiy.WhereOr("id = {0}", ids, NpgsqlDbType.Uuid);
		public class PeopleUpdateBuilder : UpdateBuilder<PeopleUpdateBuilder, PeopleModel>
		{
			public PeopleUpdateBuilder SetId(Guid id) => Set("id", id, 16, NpgsqlDbType.Uuid);
			public PeopleUpdateBuilder SetAge(int age) => Set("age", age, 4, NpgsqlDbType.Integer);
			public PeopleUpdateBuilder SetAgeIncrement(int age) => SetIncrement("age", age, 4, NpgsqlDbType.Integer);
			public PeopleUpdateBuilder SetName(string name) => Set("name", name, 255, NpgsqlDbType.Varchar);
			public PeopleUpdateBuilder SetSex(bool sex) => Set("sex", sex, 1, NpgsqlDbType.Boolean);
		}
		#endregion

	}
}

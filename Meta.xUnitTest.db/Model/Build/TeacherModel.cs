using Meta.Common.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NpgsqlTypes;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Net;
using Meta.xUnitTest.DAL;

namespace Meta.xUnitTest.Model
{
	[Mapping("teacher"), JsonObject(MemberSerialization.OptIn)]
	public partial class TeacherModel
	{
		#region Properties
		/// <summary>
		/// 学号
		/// </summary>
		[JsonProperty] public string Teacher_no { get; set; }
		[JsonProperty] public Guid People_id { get; set; }
		[JsonProperty] public DateTime Create_time { get; set; }
		[JsonProperty] public Guid Id { get; set; }
		#endregion

		#region Foreign Key
		private PeopleModel _getPeople = null;
		public PeopleModel GetPeople => _getPeople ??= People.GetItem(People_id);
		#endregion

		#region Update/Insert
		public Teacher.TeacherUpdateBuilder Update => DAL.Teacher.Update(this);

		public int Delete() => DAL.Teacher.Delete(this);
		public int Commit() => DAL.Teacher.Commit(this);
		public TeacherModel Insert() => DAL.Teacher.Insert(this);
		#endregion

		public override string ToString() => JsonConvert.SerializeObject(this);
		public static TeacherModel Parse(string json) => string.IsNullOrEmpty(json) ? null : JsonConvert.DeserializeObject<TeacherModel>(json);
	}
}

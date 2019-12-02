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
	[Mapping("classmate"), JsonObject(MemberSerialization.OptIn)]
	public partial class ClassmateModel
	{
		#region Properties
		[JsonProperty] public Guid Teacher_id { get; set; }
		[JsonProperty] public Guid Student_id { get; set; }
		[JsonProperty] public Guid Grade_id { get; set; }
		[JsonProperty] public DateTime? Create_time { get; set; }
		#endregion

		#region Foreign Key
		private TeacherModel _getTeacher = null;
		public TeacherModel GetTeacher => _getTeacher ??= Teacher.GetItem(Teacher_id);

		private ClassGradeModel _getClassGrade = null;
		public ClassGradeModel GetClassGrade => _getClassGrade ??= ClassGrade.GetItem(Grade_id);
		#endregion

		#region Update/Insert
		public Classmate.ClassmateUpdateBuilder Update => DAL.Classmate.Update(this);

		public int Delete() => DAL.Classmate.Delete(this);
		public int Commit() => DAL.Classmate.Commit(this);
		public ClassmateModel Insert() => DAL.Classmate.Insert(this);
		#endregion

		public override string ToString() => JsonConvert.SerializeObject(this);
		public static ClassmateModel Parse(string json) => string.IsNullOrEmpty(json) ? null : JsonConvert.DeserializeObject<ClassmateModel>(json);
	}
}

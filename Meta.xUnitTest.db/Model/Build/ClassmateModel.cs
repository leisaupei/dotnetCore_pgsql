using Meta.Common.Model;
using System;
using System.Collections.Generic;
using System.Collections;
using System.Net.NetworkInformation;
using NpgsqlTypes;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Meta.Common.Interface;
using System.Net;
using Meta.xUnitTest.DAL;

namespace Meta.xUnitTest.Model
{
	[DbTable("classmate")]
	public partial class ClassmateModel : IDbModel
	{
		#region Properties
		[JsonProperty, DbField(16, NpgsqlDbType.Uuid)]
		public Guid Teacher_id { get; set; }
		[JsonProperty, DbField(16, NpgsqlDbType.Uuid)]
		public Guid Student_id { get; set; }
		[JsonProperty, DbField(16, NpgsqlDbType.Uuid)]
		public Guid Grade_id { get; set; }
		[JsonProperty, DbField(8, NpgsqlDbType.Timestamp)]
		public DateTime? Create_time { get; set; }
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
	}
}

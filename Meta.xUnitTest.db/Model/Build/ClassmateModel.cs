using Meta.Driver.Model;
using System;
using System.Collections.Generic;
using System.Collections;
using System.Net.NetworkInformation;
using NpgsqlTypes;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Meta.Driver.Interface;
using System.Xml;
using System.Net;
using System.Threading.Tasks;
using System.Threading;
using Meta.Driver.SqlBuilder;
using Meta.xUnitTest.DAL;

namespace Meta.xUnitTest.Model
{
	[DbTable("classmate"), DbName(typeof(Options.DbMaster))]
	public partial class ClassmateModel : IDbModel
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

		private StudentModel _getStudent = null;
		public StudentModel GetStudent => _getStudent ??= Student.GetItem(Student_id);
		#endregion

		#region Update/Insert
		public UpdateBuilder<ClassmateModel> Update => DAL.Classmate.Update((this.Teacher_id, this.Student_id, this.Grade_id));

		public int Commit() => DAL.Classmate.Commit(this);
		public ClassmateModel Insert() => DAL.Classmate.Insert(this);
		public ValueTask<int> CommitAsync(CancellationToken cancellationToken = default) => DAL.Classmate.CommitAsync(this, cancellationToken);
		public Task<ClassmateModel> InsertAsync(CancellationToken cancellationToken = default) => DAL.Classmate.InsertAsync(this, cancellationToken);
		#endregion
	}
}

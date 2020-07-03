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
	[DbTable("student"), DbName(typeof(Options.DbMaster))]
	public partial class StudentModel : IDbModel
	{
		#region Properties
		/// <summary>
		/// 学号
		/// </summary>
		[JsonProperty] public string Stu_no { get; set; }
		[JsonProperty] public Guid Grade_id { get; set; }
		[JsonProperty] public Guid People_id { get; set; }
		[JsonProperty] public DateTime Create_time { get; set; }
		[JsonProperty] public Guid Id { get; set; }
		#endregion

		#region Foreign Key
		private ClassGradeModel _getClassGrade = null;
		public ClassGradeModel GetClassGrade => _getClassGrade ??= ClassGrade.GetItem(Grade_id);

		private PeopleModel _getPeople = null;
		public PeopleModel GetPeople => _getPeople ??= People.GetItem(People_id);
		#endregion

		#region Update/Insert
		public UpdateBuilder<StudentModel> Update => DAL.Student.Update(this.Id);

		public int Commit() => DAL.Student.Commit(this);
		public StudentModel Insert() => DAL.Student.Insert(this);
		public ValueTask<int> CommitAsync(CancellationToken cancellationToken = default) => DAL.Student.CommitAsync(this, cancellationToken);
		public Task<StudentModel> InsertAsync(CancellationToken cancellationToken = default) => DAL.Student.InsertAsync(this, cancellationToken);
		#endregion
	}
}

using Meta.Common.Model;
using System;
using System.Collections.Generic;
using System.Collections;
using System.Net.NetworkInformation;
using NpgsqlTypes;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Meta.Common.Interface;
using System.Xml;
using System.Net;
using System.Threading.Tasks;
using System.Threading;
using Meta.Common.SqlBuilder;
using Meta.xUnitTest.DAL;

namespace Meta.xUnitTest.Model
{
	[DbTable("teacher")]
	public partial class TeacherModel : IDbModel
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
		public UpdateBuilder<TeacherModel> Update => DAL.Teacher.Update(this.Id);

		public int Commit() => DAL.Teacher.Commit(this);
		public TeacherModel Insert() => DAL.Teacher.Insert(this);
		public ValueTask<int> CommitAsync(CancellationToken cancellationToken = default) => DAL.Teacher.CommitAsync(this, cancellationToken);
		public Task<TeacherModel> InsertAsync(CancellationToken cancellationToken = default) => DAL.Teacher.InsertAsync(this, cancellationToken);
		#endregion
	}
}

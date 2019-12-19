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
		[JsonProperty, DbField(32, NpgsqlDbType.Varchar)]
		public string Teacher_no { get; set; }
		[JsonProperty, DbField(16, NpgsqlDbType.Uuid)]
		public Guid People_id { get; set; }
		[JsonProperty, DbField(8, NpgsqlDbType.Timestamp)]
		public DateTime Create_time { get; set; }
		[JsonProperty, DbField(16, NpgsqlDbType.Uuid)]
		public Guid Id { get; set; }
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
	}
}

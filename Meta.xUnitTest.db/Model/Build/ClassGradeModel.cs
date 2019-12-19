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
	[DbTable("class.grade")]
	public partial class ClassGradeModel : IDbModel
	{
		#region Properties
		[JsonProperty, DbField(16, NpgsqlDbType.Uuid)]
		public Guid Id { get; set; }
		/// <summary>
		/// 班级名称
		/// </summary>
		[JsonProperty, DbField(255, NpgsqlDbType.Varchar)]
		public string Name { get; set; }
		[JsonProperty, DbField(8, NpgsqlDbType.Timestamp)]
		public DateTime Create_time { get; set; }
		#endregion

		#region Update/Insert
		public ClassGrade.ClassGradeUpdateBuilder Update => DAL.ClassGrade.Update(this);

		public int Delete() => DAL.ClassGrade.Delete(this);
		public int Commit() => DAL.ClassGrade.Commit(this);
		public ClassGradeModel Insert() => DAL.ClassGrade.Insert(this);
		#endregion
	}
}

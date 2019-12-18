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
	[DbTable("people")]
	public partial class PeopleModel : IDbModel
	{
		#region Properties
		[JsonProperty, DbField(16, NpgsqlDbType.Uuid)]
		public Guid Id { get; set; }
		/// <summary>
		/// 年龄
		/// </summary>
		[JsonProperty, DbField(4, NpgsqlDbType.Integer)]
		public int Age { get; set; }
		/// <summary>
		/// 姓名
		/// </summary>
		[JsonProperty, DbField(255, NpgsqlDbType.Varchar)]
		public string Name { get; set; }
		/// <summary>
		/// 性别
		/// </summary>
		[JsonProperty, DbField(1, NpgsqlDbType.Boolean)]
		public bool? Sex { get; set; }
		[JsonProperty, DbField(8, NpgsqlDbType.Timestamp)]
		public DateTime Create_time { get; set; }
		/// <summary>
		/// 家庭住址
		/// </summary>
		[JsonProperty, DbField(255, NpgsqlDbType.Varchar)]
		public string Address { get; set; }
		/// <summary>
		/// 详细住址
		/// </summary>
		[JsonProperty, DbField(-1, NpgsqlDbType.Jsonb)]
		public JToken Address_detail { get; set; }
		[JsonProperty, DbField(4)]
		public EDataState State { get; set; }
		#endregion

		#region Update/Insert
		public People.PeopleUpdateBuilder Update => DAL.People.Update(this);

		public int Delete() => DAL.People.Delete(this);
		public int Commit() => DAL.People.Commit(this);
		public PeopleModel Insert() => DAL.People.Insert(this);
		#endregion
	}
}

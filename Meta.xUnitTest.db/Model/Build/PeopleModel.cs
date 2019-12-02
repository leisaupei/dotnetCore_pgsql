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
	[Mapping("people"), JsonObject(MemberSerialization.OptIn)]
	public partial class PeopleModel
	{
		#region Properties
		[JsonProperty] public Guid Id { get; set; }
		/// <summary>
		/// 年龄
		/// </summary>
		[JsonProperty] public int Age { get; set; }
		/// <summary>
		/// 姓名
		/// </summary>
		[JsonProperty] public string Name { get; set; }
		/// <summary>
		/// 性别
		/// </summary>
		[JsonProperty] public bool? Sex { get; set; }
		[JsonProperty] public DateTime Create_time { get; set; }
		/// <summary>
		/// 家庭住址
		/// </summary>
		[JsonProperty] public string Address { get; set; }
		/// <summary>
		/// 详细住址
		/// </summary>
		[JsonProperty] public JToken Address_detail { get; set; }
		[JsonProperty] public EDataState State { get; set; }
		#endregion

		#region Foreign Key
		#endregion

		#region Update/Insert
		public People.PeopleUpdateBuilder Update => DAL.People.Update(this);

		public int Delete() => DAL.People.Delete(this);
		public int Commit() => DAL.People.Commit(this);
		public PeopleModel Insert() => DAL.People.Insert(this);
		#endregion

		public override string ToString() => JsonConvert.SerializeObject(this);
		public static PeopleModel Parse(string json) => string.IsNullOrEmpty(json) ? null : JsonConvert.DeserializeObject<PeopleModel>(json);
	}
}

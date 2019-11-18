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
	[Mapping("class.grade"), JsonObject(MemberSerialization.OptIn)]
	public partial class ClassGradeModel
	{
		#region Properties
		[JsonProperty] public Guid Id { get; set; }
		[JsonProperty] public string Name { get; set; }
		[JsonProperty] public DateTime Create_time { get; set; }
		#endregion

		#region Foreign Key
		#endregion

		#region Update/Insert
		public ClassGrade.ClassGradeUpdateBuilder Update => DAL.ClassGrade.Update(this);

		public int Delete() => DAL.ClassGrade.Delete(this);
		public int Commit() => DAL.ClassGrade.Commit(this);
		public ClassGradeModel Insert() => DAL.ClassGrade.Insert(this);
		#endregion

		public override string ToString() => JsonConvert.SerializeObject(this);
		public static ClassGradeModel Parse(string json) => string.IsNullOrEmpty(json) ? null : JsonConvert.DeserializeObject<ClassGradeModel>(json);
	}
}

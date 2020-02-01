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
	[DbTable("people")]
	public partial class PeopleModel : IDbModel
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

		#region Update/Insert
		public UpdateBuilder<PeopleModel> Update => DAL.People.Update(this.Id);

		public int Commit() => DAL.People.Commit(this);
		public PeopleModel Insert() => DAL.People.Insert(this);
		public ValueTask<int> CommitAsync(CancellationToken cancellationToken = default) => DAL.People.CommitAsync(this, cancellationToken);
		public Task<PeopleModel> InsertAsync(CancellationToken cancellationToken = default) => DAL.People.InsertAsync(this, cancellationToken);
		#endregion
	}
}

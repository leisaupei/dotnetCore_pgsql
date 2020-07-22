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
	[DbTable("stat")]
	public partial class StatModel : IDbModel
	{
		#region Properties
		[JsonProperty] public int Times { get; set; }
		/// <summary>
		/// 单位毫秒
		/// </summary>
		[JsonProperty] public decimal Haoshi { get; set; }
		/// <summary>
		/// 自增id
		/// </summary>
		[JsonProperty] public int Id { get; set; }
		#endregion

		#region Update/Insert
		public UpdateBuilder<StatModel> Update => DAL.Stat.Update(this.Id);

		public int Commit() => DAL.Stat.Commit(this);
		public StatModel Insert() => DAL.Stat.Insert(this);
		public ValueTask<int> CommitAsync(CancellationToken cancellationToken = default) => DAL.Stat.CommitAsync(this, cancellationToken);
		public Task<StatModel> InsertAsync(CancellationToken cancellationToken = default) => DAL.Stat.InsertAsync(this, cancellationToken);
		#endregion
	}
}

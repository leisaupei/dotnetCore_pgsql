using Meta.Driver.Model;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace Meta.Driver.Interface
{
	/// <summary>
	/// 数据库表模型
	/// </summary>
	[JsonObject(MemberSerialization.OptIn)]
	public interface IDbModel
	{
	}
}

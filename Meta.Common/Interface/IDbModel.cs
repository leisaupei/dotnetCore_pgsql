using Meta.Common.Model;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace Meta.Common.Interface
{
	[JsonObject(MemberSerialization.OptIn)]
	public interface IDbModel
	{
	}
}

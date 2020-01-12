using Meta.Common.Model;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace Meta.Common.Interface
{
	public interface IDbOption
	{
		/// <summary>
		/// 主库
		/// </summary>
		internal DbConnectionModel Master { get; }
		/// <summary>
		/// 从库
		/// </summary>
		internal DbConnectionModel[] Slave { get; }
	}
}

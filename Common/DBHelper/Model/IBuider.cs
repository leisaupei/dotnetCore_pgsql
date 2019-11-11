using Npgsql;
using System;
using System.Collections.Generic;
using System.Text;

namespace DBHelper
{
	public interface IBuilder
	{
		/// <summary>
		/// sql参数列表
		/// </summary>
		List<NpgsqlParameter> Params { get; }
		/// <summary>
		/// 
		/// </summary>
		/// <returns>参数化sql语句</returns>
		string GetCommandTextString();
		/// <summary>
		/// 返回实例类型
		/// </summary>
		Type Type { get; set; }
		/// <summary>
		/// 是否列表
		/// </summary>
		bool IsList { get; set; }
	}
}

﻿using Meta.Common.Extensions;
using Meta.Common.Model;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Text;

namespace Meta.Common.Interface
{
	public interface ISqlBuilder
	{
		/// <summary>
		/// sql参数列表
		/// </summary>
		List<DbParameter> Params { get; }
		/// <summary>
		/// 
		/// </summary>
		/// <returns>参数化sql语句</returns>
		string GetCommandTextString();
		/// <summary>
		/// 返回实例类型
		/// </summary>
		Type Type { get; }
		/// <summary>
		/// 是否列表
		/// </summary>
		PipeReturnType ReturnType { get; }
		/// <summary>
		/// 是否直接返回默认值
		/// </summary>
		bool IsReturnDefault { get; }

	}
}

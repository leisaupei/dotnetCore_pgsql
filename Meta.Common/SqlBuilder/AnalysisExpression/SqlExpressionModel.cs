using Npgsql;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Text;

namespace Meta.Common.SqlBuilder.AnalysisExpression
{
	public class SqlExpressionModel
	{
		/// <summary>
		/// 转换成的sql语句 all
		/// </summary>
		public string SqlText { get; set; }
		/// <summary>
		/// 参数化列表 union/where
		/// </summary>
		public List<DbParameter> Paras { get; } = new List<DbParameter>();
		/// <summary>
		/// 关联查询的表别名 union/子表别名
		/// </summary>
		public string Alias { get; set; }
		/// <summary>
		/// 获取单个键名 single
		/// </summary>
		public string KeyName { get; set; }
		/// <summary>
		/// 连接的类型 用于获取表明和字段名 union
		/// </summary>
		public Type UnionType { get; set; }
	}
}

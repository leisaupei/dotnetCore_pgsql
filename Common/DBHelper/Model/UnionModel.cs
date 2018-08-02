using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

namespace DBHelper
{
	public class Union
	{
		public string AliasName { get; set; }
		public string Table { get; set; }
		public string Expression { get; set; }
		public UnionEnum UnionType { get; set; }
		public Union(string aliasName, string table, string expression, UnionEnum unionType)
		{
			AliasName = aliasName;
			Table = table;
			Expression = expression;
			UnionType = unionType;
		}
	}
	public enum UnionEnum
	{
		INNER_JOIN = 1, LEFT_JOIN, RIGHT_JOIN, LEFT_OUTER_JOIN, RIGHT_OUTER_JOIN
	}
}


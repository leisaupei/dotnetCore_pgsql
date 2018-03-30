using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

namespace DBHelper
{
	public class UnionModel
	{
		public string AliasName { get; set; }
		public Type Model { get; set; }
		public string Expression { get; set; }
		public UnionType UnionType { get; set; }
	}
	public enum UnionType
	{
		INNER_JOIN = 1, LEFT_JOIN, RIGHT_JOIN, LEFT_OUTER_JOIN, RIGHT_OUTER_JOIN
	}
}

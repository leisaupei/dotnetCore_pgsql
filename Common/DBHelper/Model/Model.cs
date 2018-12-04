using NpgsqlTypes;
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
	public class DbTypeValue
	{
		public DbTypeValue() { }
		public static DbTypeValue New(object value, NpgsqlDbType? dbType = null)
		{
			return new DbTypeValue(value, dbType);
		}
		public DbTypeValue(object value)
		{
			Value = value;
		}
		public DbTypeValue(object value, NpgsqlDbType? dbType)
		{
			DbType = dbType;
			Value = value;
		}
		public override string ToString()
		{
			return Value.ToString();
		}
		public NpgsqlDbType? DbType { get; set; } = null;
		public object Value { get; set; }

	}
	/// <summary>
	/// 并联
	/// </summary>
	public enum UnionEnum
	{
		INNER_JOIN = 1, LEFT_JOIN, RIGHT_JOIN, LEFT_OUTER_JOIN, RIGHT_OUTER_JOIN
	}
	/// <summary>
	/// 数据库枚举名称 用于查询选库
	/// </summary>
	public enum DatabaseType
	{
		Master = 1, Slave
	}
}


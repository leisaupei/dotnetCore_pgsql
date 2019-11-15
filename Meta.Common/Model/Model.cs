using Npgsql;
using NpgsqlTypes;
using System;

namespace Meta.Common.Model
{
	/// <summary>
	/// 联表实体类
	/// </summary>
	public class Union
	{
		/// <summary>
		/// 别名
		/// </summary>
		public string AliasName { get; set; }
		/// <summary>
		/// 标明
		/// </summary>
		public string Table { get; set; }
		/// <summary>
		/// on表达式
		/// </summary>
		public string Expression { get; set; }
		/// <summary>
		/// 联表类型
		/// </summary>
		public UnionEnum UnionType { get; set; }
		public Union(string aliasName, string table, string expression, UnionEnum unionType)
		{
			AliasName = aliasName;
			Table = table;
			Expression = expression;
			UnionType = unionType;
		}
	}
	/// <summary>
	/// 
	/// </summary>
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
			return Value?.ToString();
		}
		public NpgsqlDbType? DbType { get; set; } = null;
		public object Value { get; set; }

	}
	public enum UnionEnum
	{
		INNER_JOIN = 1, LEFT_JOIN, RIGHT_JOIN, LEFT_OUTER_JOIN, RIGHT_OUTER_JOIN
	}
	public enum DatabaseType
	{
		Master = 1, Slave
	}

	public partial class NpgsqlNameTranslator : INpgsqlNameTranslator
	{
		public string TranslateMemberName(string clrName) => clrName;
		public string TranslateTypeName(string clrName) => clrName;
	}
}


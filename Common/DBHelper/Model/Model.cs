using Npgsql;
using NpgsqlTypes;
using System;

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


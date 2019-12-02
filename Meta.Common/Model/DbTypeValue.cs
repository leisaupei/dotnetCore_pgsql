using NpgsqlTypes;
using System;
using System.Collections.Generic;
using System.Text;

namespace Meta.Common.Model
{
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
}

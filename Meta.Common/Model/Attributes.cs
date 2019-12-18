using NpgsqlTypes;
using System;
using System.Collections.Generic;
using System.Text;

namespace Meta.Common.Model
{
	[AttributeUsage(AttributeTargets.Class, Inherited = true)]
	public class DbTableAttribute : Attribute
	{
		public string TableName { get; set; }
		public DbTableAttribute(string tableName) => TableName = tableName;
	}
	[AttributeUsage(AttributeTargets.Property, Inherited = true)]
	public class DbFieldAttribute : Attribute
	{
		public DbFieldAttribute(int size) : this() => DbField.Size = size;
		public DbFieldAttribute(NpgsqlDbType npgsqlDbType) : this() => DbField.NpgsqlDbType = npgsqlDbType;
		public DbFieldAttribute(int size, NpgsqlDbType npgsqlDbType) : this(size) => DbField.NpgsqlDbType = npgsqlDbType;
		public DbFieldAttribute() { DbField = new DbFieldModel(); }
		public DbFieldModel DbField { get; private set; }
	}
}

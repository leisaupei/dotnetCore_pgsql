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
	[AttributeUsage(AttributeTargets.Class, Inherited = true)]
	public class DbNameAttribute : Attribute
	{
		public string DbName { get; set; }
		public DbNameAttribute(string dbName) => DbName = dbName;
	}
}

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
}

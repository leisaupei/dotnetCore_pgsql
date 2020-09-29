using NpgsqlTypes;
using System;
using System.Collections.Generic;
using System.Text;

namespace Meta.Driver.Model
{
	/// <summary>
	/// 数据库表特性
	/// </summary>
	[AttributeUsage(AttributeTargets.Class, Inherited = true)]
	public class DbTableAttribute : Attribute
	{
		/// <summary>
		/// 表名
		/// </summary>
		public string TableName { get; set; }

		/// <summary>
		/// 初始化
		/// </summary>
		/// <param name="tableName">表名</param>
		public DbTableAttribute(string tableName) => TableName = tableName;
	}
	[AttributeUsage(AttributeTargets.Class, Inherited = true)]
	public class DbNameAttribute : Attribute
	{
		private readonly Type _dbName;
		public string DbName
		{
			get
			{
				return _dbName.Name;
			}
		}
		public DbNameAttribute(Type dbName) => _dbName = dbName;
	}
}

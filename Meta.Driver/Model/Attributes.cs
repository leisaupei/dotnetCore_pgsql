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

	/// <summary>
	/// 数据库名称特性
	/// </summary>
	[AttributeUsage(AttributeTargets.Struct, Inherited = true)]
	public class DbNameAttribute : Attribute
	{
		/// <summary>
		/// 名称
		/// </summary>
		public string DbName { get; set; }

		/// <summary>
		/// 初始化
		/// </summary>
		/// <param name="dbName">数据库名称</param>
		public DbNameAttribute(string dbName) => DbName = dbName;
	}
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NpgsqlTypes;

namespace Common.CodeFactory
{
	public class GenerateModel
	{
		/// <summary>
		/// 数据库连接字符串
		/// </summary>
		public string ConnectionString { get; set; }
		/// <summary>
		/// 项目名称
		/// </summary>
		public string ProjectName { get; set; }
		/// <summary>
		/// 输出路径
		/// </summary>
		public string OutputPath { get; set; }
		/// <summary>
		/// 数据库
		/// </summary>
	}
	public class TableViewModel
	{
		public string Name { get; set; }
		public string Type { get; set; }
	}
	public class FieldInfo
	{
		public int Oid { get; set; }
		public string Field { get; set; }
		public int Length { get; set; }
		public string Comment { get; set; }
		public string RelType { get; set; }
		public string DbType { get; set; }
		public string DataType { get; set; }
		public bool IsIdentity { get; set; }
		public bool IsArray { get; set; }
		public bool IsEnum { get; set; }
		public bool IsNotNull { get; set; }
		public NpgsqlDbType PgDbType { get; set; }
		public string Typcategory { get; set; }
	}
	public class PrimarykeyInfo
	{
		public string Field { get; set; }
		public string TypeName { get; set; }
	}
	public class ConstraintMoreToOne
	{
		public string Conname { get; set; }
		public string Contype { get; set; }
		public string RefColumn { get; set; }
		public string TableName { get; set; }
		public string Nspname { get; set; }
	}
	public class ConstraintOneToMore
	{
		public string Conname { get; set; }
		public string Contype { get; set; }
		public string RefColumn { get; set; }
		public string TableName { get; set; }
		public string Nspname { get; set; }
		public bool IsOneToOne { get; set; }
	}
	public class ConstraintMoreToMore
	{
		/// <summary>
		/// 主要输出表
		/// </summary>
		public string MainTable { get; set; }
		/// <summary>
		/// 主要输出表的Schema名称
		/// </summary>
		public string MainNspname { get; set; }
		/// <summary>
		/// 主要输出表的主键
		/// </summary>
		public string MainField { get; set; }
		/// <summary>
		/// 当前表的字段名称
		/// </summary>
		public string MinorField { get; set; }
		/// <summary>
		/// 当前表的Schema名称
		/// </summary>
		public string MinorNspname { get; set; }
		/// <summary>
		/// 当前表名称
		/// </summary>
		public string MinorTable { get; set; }
		/// <summary>
		/// 中间表的Schema
		/// </summary>
		public string CenterNspname { get; set; }
		/// <summary>
		/// 中间表中主要输出表的主键字段名称
		/// </summary>
		public string CenterMainField { get; set; }
		/// <summary>
		/// 中间表中主要输出表的主键字段类型
		/// </summary>
		public string CenterMainType { get; set; }
		/// <summary>
		/// 中间表中当前表的主键字段名称
		/// </summary>
		public string CenterMinorField { get; set; }
		/// <summary>
		/// 中间表中当前表的主键字段类型
		/// </summary>
		public string CenterMinorType { get; set; }
		/// <summary>
		/// 中间表的表名
		/// </summary>
		public string CenterTable { get; set; }
	}
	public class EnumTypeInfo
	{
		public int Oid { get; set; }
		public string Typname { get; set; }
		public string Nspname { get; set; }
	}
}

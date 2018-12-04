using NpgsqlTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CodeFactory
{
	public class GenerateModel
	{
		/// <summary>
		/// 默认主库名称
		/// </summary>
		public const string MASTER_DATABASE_TYPE_NAME = "Master";
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
		/// 数据库(多库字段)
		/// </summary>
		public string TypeName { get; set; } = MASTER_DATABASE_TYPE_NAME;
	}

	public class TableViewModel
	{
		public string Name { get; set; }
		public string Type { get; set; }
	}
	public class FieldInfo
	{
		/// <summary>
		/// oid
		/// </summary>
		public int Oid { get; set; }
		/// <summary>
		/// 字段名称
		/// </summary>
		public string Field { get; set; }
		/// <summary>
		/// 字段数据库长度
		/// </summary>
		public int Length { get; set; }
		/// <summary>
		/// 标识
		/// </summary>
		public string Comment { get; set; }
		/// <summary>
		/// C#类型
		/// </summary>
		public string RelType { get; set; }
		/// <summary>
		/// 数据库类型
		/// </summary>
		public string DbType { get; set; }
		/// <summary>
		/// 数据类型
		/// </summary>
		public string DataType { get; set; }
		/// <summary>
		/// 是否自增
		/// </summary>
		public bool IsIdentity { get; set; }
		/// <summary>
		/// 是否数组
		/// </summary>
		public bool IsArray { get; set; }
		/// <summary>
		/// 是否枚举
		/// </summary>
		public bool IsEnum { get; set; }
		/// <summary>
		/// 是否非空
		/// </summary>
		public bool IsNotNull { get; set; }
		/// <summary>
		/// npgsql 数据库类型
		/// </summary>
		public NpgsqlDbType PgDbType { get; set; }
		/// <summary>
		/// 类型分类
		/// </summary>
		public string Typcategory { get; set; }
		/// <summary>
		/// 类型schema(命名空间)
		/// </summary>
		public string Nspname { get; set; }
		/// <summary>
		/// 是否唯一键
		/// </summary>
		public bool IsUnique { get; set; }
		/// <summary>
		/// 
		/// </summary>
		public string PgDbTypeString { get; set; }
		/// <summary>
		/// C#类型
		/// </summary>
		public string CSharpType { get; set; }
		/// <summary>
		/// 纬度
		/// </summary>
		public int Dimensions { get; set; }
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
	public class CompositeTypeInfo
	{
		public string Typname { get; set; }
		public string Nspname { get; set; }
	}
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NpgsqlTypes;

namespace DBHelper.CodeFactory
{
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
	}
	public class ConstraintMoreToMore
	{
		public string MainTable { get; set; }//主要输出表
		public string MainNspname { get; set; }//主要输出表的schema
		public string MainField { get; set; }//主要输出表的主键
		public string MinorField { get; set; }//当前表的字段名称
		public string MinorNspname { get; set; }//当前表的schema
		public string MinorTable { get; set; }//当前表名
		public string CenterNspname { get; set; }//中间表的schema
		public string CenterMainField { get; set; }//中间表中主要输出表的主键字段名
		public string CenterMainType { get; set; }//中间表中主要输出表的主键字段类型
		public string CenterMinorField { get; set; }//中间表中当前表的主键字段名
		public string CenterMinorType { get; set; }//中间表中当前表的主键字段类型
		public string CenterTable { get; set; }//中间表的表名
	}
	public class EnumTypeInfo
	{
		public int Oid { get; set; }
		public string Typname { get; set; }
		public string Nspname { get; set; }
	}
}

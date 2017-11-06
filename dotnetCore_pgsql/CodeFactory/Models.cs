using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NpgsqlTypes;

namespace dotnetCore_pgsql_DevVersion.CodeFactory
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
        public string Db_type { get; set; }
        public string Data_Type { get; set; }
        public bool Is_identity { get; set; }
        public bool Is_array { get; set; }
        public bool Is_enum { get; set; }
        public bool Is_not_null { get; set; }
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
        public string Ref_column { get; set; }
        public string Table_name { get; set; }
        public string Nspname { get; set; }
    }
    public class ConstraintOneToMore
    {
        public string Conname { get; set; }
        public string Contype { get; set; }
        public string Ref_column { get; set; }
        public string Table_name { get; set; }
        public string Nspname { get; set; }
    }
    public class ConstraintMoreToMore
    {
        //public string Conname { get; set; }
        //public string Contype { get; set; }
        //public string Ref_column { get; set; }
        //public string Table_name { get; set; }
        //public string Nspname { get; set; }

        public string Main_table { get; set; }//主要输出表
        public string Main_nspname { get; set; }//主要输出表的schema
        public string Main_field { get; set; }//主要输出表的主键
        public string Minor_field { get; set; }//当前表的字段名称
        public string Minor_nspname { get; set; }//当前表的schema
        public string Minor_table { get; set; }//当前表名
        public string Center_nspname { get; set; }//中间表的schema
        public string Center_main_field{ get; set; }//中间表中主要输出表的主键字段名
        public string Center_main_type { get; set; }//中间表中主要输出表的主键字段类型
        public string Center_minor_field { get; set; }//中间表中当前表的主键字段名
        public string Center_minor_type{ get; set; }//中间表中当前表的主键字段类型
        public string Center_table { get; set; }//中间表的表名
    }
    public class EnumTypeInfo
    {
        public int Oid { get; set; }
        public string Typname { get; set; }
        public string Nspname { get; set; }
    }
}

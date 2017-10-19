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
    }
    public class PrimarykeyInfo
    {
        public string Field { get; set; }
        public string TypeName { get; set; }
    }
    public class ConstraintInfo
    {
        public string Conname { get; set; }
        public string Contype { get; set; }
        public string Ref_column { get; set; }
        public string Table_name { get; set; }
        public string Nspname { get; set; }
    }
    public class EnumTypeInfo
    {
        public int Oid { get; set; }
        public string Typname { get; set; }
        public string Nspname { get; set; }
    }
}

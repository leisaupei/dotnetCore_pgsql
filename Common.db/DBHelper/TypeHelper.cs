using NpgsqlTypes;
using System;
namespace Common.db.DBHelper
{
    public class TypeHelper
    {
        public static NpgsqlDbType? GetDbType(Type type)
        {
            if (type.BaseType.Name.ToLower() == "enum")
                return NpgsqlDbType.Enum;
            string type_name = type.Name.ToLower();
            switch (type_name)
            {
                case "guid": return NpgsqlDbType.Uuid;
                case "string": return NpgsqlDbType.Varchar;
                case "short":
                case "int16": return NpgsqlDbType.Smallint;
                case "int":
                case "int32": return NpgsqlDbType.Integer;
                case "int64":
                case "long": return NpgsqlDbType.Bigint;
                case "float": return NpgsqlDbType.Real;
                case "double": return NpgsqlDbType.Double;
                case "decimal": return NpgsqlDbType.Numeric;
                case "datetime": return NpgsqlDbType.Timestamp;
                case "jtoken": return NpgsqlDbType.Jsonb;
                case "timespan": return NpgsqlDbType.Interval;
                case "byte[]": return NpgsqlDbType.Bytea;
                case "enum": return NpgsqlDbType.Enum;
                default: return null;
            }
        }
        public static string ConvertNpgsqlDbTypeToSystemType(NpgsqlDbType type)
        {
            switch (type)
            {
                case NpgsqlDbType.Array: return "";
                case NpgsqlDbType.Bigint: return "";
                case NpgsqlDbType.Boolean: return "";
                case NpgsqlDbType.Box: return "";
                case NpgsqlDbType.Bytea: return "";
                case NpgsqlDbType.Circle: return "";
                case NpgsqlDbType.Char: return "";
                case NpgsqlDbType.Date: return "";
                case NpgsqlDbType.Double: return "";
                case NpgsqlDbType.Integer: return "";
                case NpgsqlDbType.Line: return "";
                case NpgsqlDbType.LSeg: return "";
                case NpgsqlDbType.Money: return "";
                case NpgsqlDbType.Numeric: return "";
                case NpgsqlDbType.Path: return "";
                case NpgsqlDbType.Point: return "";
                case NpgsqlDbType.Polygon: return "";
                case NpgsqlDbType.Real: return "";
                case NpgsqlDbType.Smallint: return "";
                case NpgsqlDbType.Text: return "";
                case NpgsqlDbType.Time: return "";
                case NpgsqlDbType.Timestamp: return "";
                case NpgsqlDbType.Varchar: return "";
                case NpgsqlDbType.Refcursor: return "";
                case NpgsqlDbType.Inet: return "";
                case NpgsqlDbType.Bit: return "";
                case NpgsqlDbType.TimestampTZ: return "";
                case NpgsqlDbType.Uuid: return "";
                case NpgsqlDbType.Xml: return "";
                case NpgsqlDbType.Oidvector: return "";
                case NpgsqlDbType.Interval: return "";
                case NpgsqlDbType.TimeTZ: return "";
                case NpgsqlDbType.Name: return "";
                case NpgsqlDbType.MacAddr: return "";
                case NpgsqlDbType.Json: return "";
                case NpgsqlDbType.Jsonb: return "";
                case NpgsqlDbType.Hstore: return "";
                case NpgsqlDbType.InternalChar: return "";
                case NpgsqlDbType.Varbit: return "";
                case NpgsqlDbType.Unknown: return "";
                case NpgsqlDbType.Oid: return "";
                case NpgsqlDbType.Xid: return "";
                case NpgsqlDbType.Cid: return "";
                case NpgsqlDbType.Cidr: return "";
                case NpgsqlDbType.TsVector: return "";
                case NpgsqlDbType.TsQuery: return "";
                case NpgsqlDbType.Enum: return "";
                case NpgsqlDbType.Composite: return "";
                case NpgsqlDbType.Regtype: return "";
                case NpgsqlDbType.Geometry: return "";
                case NpgsqlDbType.Citext: return "";
                case NpgsqlDbType.Int2Vector: return "";
                case NpgsqlDbType.Tid: return "";
                case NpgsqlDbType.Range: return "";
                default: return "";
            }
        }



        // 数据库类型转化成C#类型String
        public static string PgDbTypeConvertToCSharpString(string dbType)
        {
            switch (dbType)
            {
                case "bit": return "byte";
                case "varbit": return "BitArray";

                case "bool": return "bool";
                case "box": return "NpgsqlBox";
                case "bytea": return "byte[]";
                case "circle": return "NpgsqlCircle";

                case "float4":
                case "float8":
                case "numeric":
                case "money":
                case "decimal": return "decimal";

                case "cidr":
                case "inet": return "IPAddress";

                case "serial2":
                case "int2": return "short";

                case "serial4":
                case "int4": return "int";

                case "serial8":
                case "int8": return "long";

                case "time":
                case "interval": return "TimeSpan";

                case "json":
                case "jsonb": return "JToken";

                case "line": return "NpgsqlLine";
                case "lseg": return "NpgsqlLSeg";
                case "macaddr": return "PhysicalAddress";
                case "path": return "NpgsqlPath";
                case "point": return "NpgsqlPoint";
                case "polygon": return "NpgsqlPolygon";

                case "xml":
                case "char":
                case "bpchar":
                case "varchar":
                case "text": return "string";

                case "date":
                case "timetz":
                case "timestamp":
                case "timestamptz": return "DateTime";

                case "tsquery": return "NpgsqlTsQuery";
                case "tsvector": return "NpgsqlTsVector";
                //case "txid_snapshot": return "";
                case "uuid": return "Guid";
                default: return dbType;
            }
        }
        //转化数据库字段未数据库字段NpgsqlDbType枚举
        public static NpgsqlDbType ConvertFromDbTypeToNpgsqlDbTypeEnum(string data_type, string db_type)
        {
            if (data_type == "e")
                return NpgsqlDbType.Enum;  //   _dbtype = item.Db_type.ToUpperPascal();
            switch (db_type)
            {
                case "int2":
                case "int4": return NpgsqlDbType.Integer;
                case "int8": return NpgsqlDbType.Bigint;
                case "bool": return NpgsqlDbType.Boolean;
                case "bpchar": return NpgsqlDbType.Varchar;
                default: return Enum.Parse<NpgsqlDbType>(db_type.ToUpperPascal());
            }

        }
        //排除生成whereor条件的字段类型
        public static bool MakeWhereOrExceptType(string type)
        {
            string[] arr = new string[] { "datetime", "geometry" };
            if (arr.Contains(f => f == type.Replace("?", "")))
                return false;
            return true;
        }
        //从数据库类型获取where条件字段类型
        public static string GetWhereTypeFromDbType(string type)// where 参数加不加文浩
        {
            string _type = PgDbTypeConvertToCSharpString(type).Replace("?", "");

            string brackets = type.Contains("[]") ? "" : "[]";
            switch (_type)
            {
                case "JToken": return _type;
                default: return "params " + _type + brackets;
            }
        }
        //从数据库类型获取设置的数据库类型
        public static string GetSetTypeFromDbType(string type, bool is_array)
        {
            string _type = PgDbTypeConvertToCSharpString(type);
            switch (_type)
            {
                case "JToken": return _type;
                default: return _type + "?" + (is_array ? "[]" : "");
            }
        }
        //根据数据库类型判断不生成模型的字段
        public static bool NotCreateModelFieldDbType(string dbType, string typcategory)
        {
            if (typcategory.ToLower() == "u" && dbType.Replace("?", "") == "geometry")
                return false;
            return true;
        }

    }
}

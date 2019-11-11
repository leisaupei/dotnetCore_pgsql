using CodeFactory.Extension;
using NpgsqlTypes;
using System;
using System.Linq;

namespace CodeFactory
{
	public static class Types
	{
		/// <summary>
		/// 数据库类型转化成C#类型String
		/// </summary>
		/// <param name="dbType"></param>
		/// <returns></returns>
		public static string ConvertPgDbTypeToCSharpType(string dataType, string dbType)
		{
			switch (dbType)
			{
				case "bit": return "byte[]";
				case "varbit": return "BitArray";

				case "bool": return "bool";
				case "box": return "NpgsqlBox";
				case "bytea": return "byte[]";
				case "circle": return "NpgsqlCircle";

				case "float4": return "float";
				case "float8": return "double";
				case "numeric":
				case "money":
				case "decimal": return "decimal";

				case "cidr": return "ValueTuple<IPAddress, int>";
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
				case "hstore": return "IDictionary<string, string>";

				case "xml":
				case "char":
				case "bpchar":
				case "varchar":
				case "text": return "string";
				case "oid":
				case "cid":
				case "xid": return "uint";

				case "timetz": return "DateTimeOffset";

				case "date":
				case "timestamp":
				case "timestamptz": return "DateTime";

				case "record": return "object[]";
				case "oidvector": return "uint[]";
				case "tsquery": return "NpgsqlTsQuery";

				case "tsvector": return "NpgsqlTsVector";

				//case "txid_snapshot": return "";
				case "uuid": return "Guid";

				default:
					if (dataType == "e" || dataType == "c")
						return dbType.ToUpperPascal();
					return dbType;
			}
		}

		/// <summary>
		/// 数据库类型转化成NpgsqlDbType String
		/// </summary>
		/// <param name="dbType"></param>
		/// <returns></returns>
		public static string ConvertDbTypeToNpgsqlDbTypeString(string dataType, string dbType, bool isArray)
		{
			var _type = string.Empty;
			switch (dbType)
			{
				case "bit": _type = "NpgsqlDbType.Bit"; break;
				case "varbit": _type = "NpgsqlDbType.Varbit"; break;

				case "bool": _type = "NpgsqlDbType.Boolean"; break;
				case "box": _type = "NpgsqlDbType.Box"; break;
				case "bytea": _type = "NpgsqlDbType.Bytea"; break;
				case "circle": _type = "NpgsqlDbType.Circle"; break;

				case "float4": _type = "NpgsqlDbType.Real"; break;
				case "float8": _type = "NpgsqlDbType.Double"; break;

				case "money": _type = "NpgsqlDbType.Money"; break;
				case "decimal":
				case "numeric": _type = "NpgsqlDbType.Numeric"; break;

				case "cid": _type = "NpgsqlDbType.Cid"; break;
				case "cidr": _type = "NpgsqlDbType.Cidr"; break;
				case "inet": _type = "NpgsqlDbType.Inet"; break;

				case "serial2":
				case "int2": _type = "NpgsqlDbType.Smallint"; break;

				case "serial4":
				case "int4": _type = "NpgsqlDbType.Integer"; break;

				case "serial8":
				case "int8": _type = "NpgsqlDbType.Bigint"; break;

				case "time": _type = "NpgsqlDbType.Time"; break;
				case "interval": _type = "NpgsqlDbType.Interval"; break;

				case "json": _type = "NpgsqlDbType.Json"; break;
				case "jsonb": _type = "NpgsqlDbType.Jsonb"; break;

				case "line": _type = "NpgsqlDbType.Line"; break;
				case "lseg": _type = "NpgsqlDbType.LSeg"; break;
				case "macaddr": _type = "NpgsqlDbType.MacAddr"; break;
				case "path": _type = "NpgsqlDbType.Path"; break;
				case "point": _type = "NpgsqlDbType.Point"; break;
				case "polygon": _type = "NpgsqlDbType.Polygon"; break;

				case "xml": _type = "NpgsqlDbType.Xml"; break;
				case "char": _type = "NpgsqlDbType.InternalChar"; break;
				case "bpchar": _type = "NpgsqlDbType.Char"; break;
				case "varchar": _type = "NpgsqlDbType.Varchar"; break;
				case "text": _type = "NpgsqlDbType.Text"; break;

				case "name": _type = "NpgsqlDbType.Name"; break;
				case "date": _type = "NpgsqlDbType.Date"; break;
				case "timetz": _type = "NpgsqlDbType.TimeTz"; break;
				case "timestamp": _type = "NpgsqlDbType.Timestamp"; break;
				case "timestamptz": _type = "NpgsqlDbType.TimestampTz"; break;

				case "tsquery": _type = "NpgsqlDbType.TsQuery"; break;
				case "tsvector": _type = "NpgsqlDbType.TsVector"; break;
				case "int2vector": _type = "NpgsqlDbType.Int2Vector"; break;

				case "macaddr8": _type = "NpgsqlDbType.MacAddr8"; break;
				case "uuid": _type = "NpgsqlDbType.Uuid"; break;
				case "oid": _type = "NpgsqlDbType.Oid"; break;
				case "oidvector": _type = "NpgsqlDbType.Oidvector"; break;
				case "refcursor": _type = "NpgsqlDbType.Refcursor"; break;
				case "regtype": _type = "NpgsqlDbType.Regtype"; break;
				case "tid": _type = "NpgsqlDbType.Tid"; break;
				case "xid": _type = "NpgsqlDbType.Xid"; break;
				default: _type = ""; break;
			}
			if (isArray)
			{
				//	var need = new string[] { "varchar", "bpchar", "date", "time" };
				if (_type.IsNotNullOrEmpty())
					_type += " | NpgsqlDbType.Array";
			}
			_type = _type.IsNotNullOrEmpty() ? ", " + _type : "";
			return _type;
		}
		/// <summary>
		/// 转化数据库字段为数据库字段NpgsqlDbType枚举
		/// </summary>
		/// <param name="dataType"></param>
		/// <param name="dbType"></param>
		/// <returns></returns>
		public static NpgsqlDbType ConvertDbTypeToNpgsqlDbType(string dataType, string dbType, bool isArray)
		{
			NpgsqlDbType _type = NpgsqlDbType.Unknown;
			if (dataType == "e" || dataType == "c")
				return NpgsqlDbType.Unknown;  //   _dbtype = item.Db_type.ToUpperPascal();

			//var _type = string.Empty;
			switch (dbType)
			{
				case "bit": _type = NpgsqlDbType.Bit; break;
				case "varbit": _type = NpgsqlDbType.Varbit; break;

				case "bool": _type = NpgsqlDbType.Boolean; break;
				case "box": _type = NpgsqlDbType.Box; break;
				case "bytea": _type = NpgsqlDbType.Bytea; break;
				case "circle": _type = NpgsqlDbType.Circle; break;

				case "float4": _type = NpgsqlDbType.Real; break;
				case "float8": _type = NpgsqlDbType.Double; break;

				case "money": _type = NpgsqlDbType.Money; break;
				case "decimal":
				case "numeric": _type = NpgsqlDbType.Numeric; break;

				case "cid": _type = NpgsqlDbType.Cid; break;
				case "cidr": _type = NpgsqlDbType.Cidr; break;
				case "inet": _type = NpgsqlDbType.Inet; break;

				case "serial2":
				case "int2": _type = NpgsqlDbType.Smallint; break;

				case "serial4":
				case "int4": _type = NpgsqlDbType.Integer; break;

				case "serial8":
				case "int8": _type = NpgsqlDbType.Bigint; break;

				case "time": _type = NpgsqlDbType.Time; break;
				case "interval": _type = NpgsqlDbType.Interval; break;

				case "json": _type = NpgsqlDbType.Json; break;
				case "jsonb": _type = NpgsqlDbType.Jsonb; break;

				case "line": _type = NpgsqlDbType.Line; break;
				case "lseg": _type = NpgsqlDbType.LSeg; break;
				case "macaddr": _type = NpgsqlDbType.MacAddr; break;
				case "path": _type = NpgsqlDbType.Path; break;
				case "point": _type = NpgsqlDbType.Point; break;
				case "polygon": _type = NpgsqlDbType.Polygon; break;

				case "xml": _type = NpgsqlDbType.Xml; break;
				case "char": _type = NpgsqlDbType.InternalChar; break;
				case "bpchar": _type = NpgsqlDbType.Char; break;
				case "varchar": _type = NpgsqlDbType.Varchar; break;
				case "text": _type = NpgsqlDbType.Text; break;

				case "name": _type = NpgsqlDbType.Name; break;
				case "date": _type = NpgsqlDbType.Date; break;
				case "timetz": _type = NpgsqlDbType.TimeTz; break;
				case "timestamp": _type = NpgsqlDbType.Timestamp; break;
				case "timestamptz": _type = NpgsqlDbType.TimestampTz; break;

				case "tsquery": _type = NpgsqlDbType.TsQuery; break;

				case "tsvector": _type = NpgsqlDbType.TsVector; break;
				case "int2vector": _type = NpgsqlDbType.Int2Vector; break;
				case "hstore": _type = NpgsqlDbType.Hstore; break;
				case "macaddr8": _type = NpgsqlDbType.MacAddr8; break;
				case "uuid": _type = NpgsqlDbType.Uuid; break;
				case "oid": _type = NpgsqlDbType.Oid; break;
				case "oidvector": _type = NpgsqlDbType.Oidvector; break;
				case "refcursor": _type = NpgsqlDbType.Refcursor; break;
				case "regtype": _type = NpgsqlDbType.Regtype; break;
				case "tid": _type = NpgsqlDbType.Tid; break;
				case "xid": _type = NpgsqlDbType.Xid; break;
			}
			if (isArray)
			{
				if (_type == NpgsqlDbType.Unknown)
					_type = NpgsqlDbType.Array;
				_type = _type | NpgsqlDbType.Array;
			}
			return _type;
		}
		/// <summary>
		/// 排除生成whereor条件的字段类型
		/// </summary>
		public static bool MakeWhereOrExceptType(string type)
		{
			string[] arr = { "datetime", "geometry", "jtoken", "byte[]" };
			if (arr.Contains(type.ToLower().Replace("?", "")))
				return false;
			return true;
		}
		/// <summary>
		/// 从数据库类型获取设置的数据库类型
		/// </summary>
		/// <param name="type"></param>
		/// <param name="isArray"></param>
		/// <returns></returns>
		public static string GetSetTypeFromDbType(string type, bool isArray)
		{
			switch (type.ToLower())
			{
				case "jtoken": return type;
				default: return type + "?" + (isArray ? "[]" : "");
			}
		}
		/// <summary>
		/// 根据数据库类型判断不生成模型的字段
		/// </summary>
		/// <param name="dbType"></param>
		/// <param name="typcategory"></param>
		/// <returns></returns>
		public static bool NotCreateModelFieldDbType(string dbType, string typcategory)
		{
			if (typcategory.ToLower() == "u" && dbType.Replace("?", "") == "geometry")
				return false;
			return true;
		}
		/// <summary>
		/// 去掉public前缀及命名
		/// </summary>
		/// <param name="schemaName"></param>
		/// <param name="tableName"></param>
		/// <param name="isTableName">如果false为格式</param>
		/// <returns></returns>
		public static string DeletePublic(string schemaName, string tableName, bool isTableName = false, bool isView = false)
		{
			if (isTableName)
				return schemaName.ToLower() == "public" ? tableName.ToUpperPascal() : schemaName.ToLower() + "." + tableName;
			tableName = ExceptUnderlineToUpper(tableName);
			if (isView)
				tableName += "View";
			return schemaName.ToLower() == "public" ? tableName.ToUpperPascal() : schemaName.ToUpperPascal() + tableName;
		}
		/// <summary>
		/// 去除下划线并首字母大写
		/// </summary>
		/// <param name="str"></param>
		/// <param name="len"></param>
		/// <returns></returns>
		public static string ExceptUnderlineToUpper(string str, int? len = null)
		{
			var strArr = str.Split('_');
			str = string.Empty;
			var index = 1;
			foreach (var item in strArr)
			{
				str = string.Concat(str, item.ToUpperPascal());
				if (len != null && len == index)
					break;
				index++;
			}
			return str;
		}
	}
}

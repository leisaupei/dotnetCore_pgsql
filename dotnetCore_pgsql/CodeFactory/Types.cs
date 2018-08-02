﻿using NpgsqlTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Common.CodeFactory
{
	public static class Types
	{
		/// <summary>
		/// 数据库类型转化成C#类型String
		/// </summary>
		/// <param name="dbType"></param>
		/// <returns></returns>
		public static string ConvertPgDbTypeToCSharpType(string dbType)
		{
			switch (dbType)
			{
				case "bit": return "byte";
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
				case "oid": return "object";
				default:
					if (dbType.StartsWith("et_", StringComparison.Ordinal))
						return dbType.ToUpperPascal();
					return dbType;
			}
		}
		/// <summary>
		/// 转化数据库字段为数据库字段NpgsqlDbType枚举
		/// </summary>
		/// <param name="dataType"></param>
		/// <param name="dbType"></param>
		/// <returns></returns>
		public static NpgsqlDbType ConvertDbTypeToNpgsqlDbTypeEnum(string dataType, string dbType)
		{
			if (dataType == "e")
				return NpgsqlDbType.Unknown;  //   _dbtype = item.Db_type.ToUpperPascal();
			switch (dbType)
			{
				case "int2": return NpgsqlDbType.Smallint;
				case "int4": return NpgsqlDbType.Integer;
				case "int8": return NpgsqlDbType.Bigint;
				case "bool": return NpgsqlDbType.Boolean;
				case "bpchar": return NpgsqlDbType.Varchar;
				case "float4": return NpgsqlDbType.Numeric;
				case "float8": return NpgsqlDbType.Double;
				case "timestamptz": return NpgsqlDbType.TimestampTz;
				case "timetz": return NpgsqlDbType.TimeTz;
				default: return Enum.Parse<NpgsqlDbType>(dbType.ToUpperPascal());
			}

		}
		/// <summary>
		/// 排除生成whereor条件的字段类型
		/// </summary>
		public static bool MakeWhereOrExceptType(string type)
		{
			string[] arr = { "datetime", "geometry", "jtoken" };
			if (arr.Contains(type.ToLower().Replace("?", "")))
				return false;
			return true;
		}
		/// <summary>
		/// 从数据库类型获取where条件字段类型
		/// </summary>
		/// <param name="type"></param>
		/// <param name="isNotNull"></param>
		/// <returns></returns>
		public static string GetWhereTypeFromDbType(string type, bool isNotNull)
		{
			string _type = ConvertPgDbTypeToCSharpType(type).Replace("?", "");
			string brackets = type.Contains("[]") ? "" : "[]";
			string ques = !isNotNull && !brackets.IsNullOrEmpty() ? "?" : "";
			switch (_type.ToLower())
			{
				case "jtoken": return _type;
				case "object":
				case "string": return "params " + _type + brackets;
				default: return "params " + _type + ques + brackets;
			}
		}
		/// <summary>
		/// 从数据库类型获取设置的数据库类型
		/// </summary>
		/// <param name="type"></param>
		/// <param name="isArray"></param>
		/// <returns></returns>
		public static string GetSetTypeFromDbType(string type, bool isArray)
		{
			string _type = ConvertPgDbTypeToCSharpType(type);
			switch (_type.ToLower())
			{
				case "jtoken": return _type;
				default: return _type + "?" + (isArray ? "[]" : "");
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
	}
}
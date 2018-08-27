using Npgsql;
using NpgsqlTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace DBHelper
{

	public class TypeHelper
	{
		public static string SqlToString(string sql, List<NpgsqlParameter> nps)
		{
			foreach (var p in nps)
			{
				var value = GetParamValue(p.Value);
				var key = string.Concat("@", p.ParameterName);
				if (value == null)
				{
					if (sql.Contains("=")) sql = Regex.Replace(sql, @"\s+=\s+\" + key, " IS NULL");
					if (sql.Contains("!=")) sql = Regex.Replace(sql, @"\s+!=\s+\" + key, " IS NOT NULL");
				}
				else if (Regex.IsMatch(value, @"(^(\-|\+)?\d+(\.\d+)?$)|(^SELECT\s.+\sFROM\s)|(true)|(false)",
					RegexOptions.IgnoreCase)) sql = sql.Replace(key, value);
				else sql = sql.Replace(key, $"'{value}'");
			}
			return sql.Replace("\r", " ").Replace("\n", " ");
		}
		public static string GetParamValue(object value)
		{
			Type type = value.GetType();
			if (type.BaseType.FullName == "System.Array")
			{
				var arr = value as object[];
				var arrStr = arr.Select(a => a.ToEmptyOrString());
				return $"{{{string.Join(",", arr)}}}";
			}
			return value.ToNullOrString();
		}
		public static NpgsqlDbType? GetDbType(Type type)
		{
			NpgsqlDbType? pgsqlDbType = null;
			string type_name = type.Name.ToLower();
			if (type_name.EndsWith("[]"))
				type_name = type_name.Trim(']', '[');
			switch (type_name)
			{
				case "guid": pgsqlDbType = NpgsqlDbType.Uuid; break;
				case "string": pgsqlDbType = NpgsqlDbType.Varchar; break;
				case "short":
				case "int16": pgsqlDbType = NpgsqlDbType.Smallint; break;
				case "int":
				case "int32": pgsqlDbType = NpgsqlDbType.Integer; break;
				case "int64":
				case "long": pgsqlDbType = NpgsqlDbType.Bigint; break;
				case "float": pgsqlDbType = NpgsqlDbType.Real; break;
				case "double": pgsqlDbType = NpgsqlDbType.Double; break;
				case "decimal": pgsqlDbType = NpgsqlDbType.Numeric; break;
				case "datetime": pgsqlDbType = NpgsqlDbType.Timestamp; break;
				case "jarray":
				case "jobject":
				case "jtoken": pgsqlDbType = NpgsqlDbType.Jsonb; break;
				case "timespan": pgsqlDbType = NpgsqlDbType.Interval; break;
				case "byte[]": pgsqlDbType = NpgsqlDbType.Bytea; break;
			}
			if (type.BaseType.Name.ToLower() == "array")
				if (pgsqlDbType == null) pgsqlDbType = NpgsqlDbType.Array;
				else pgsqlDbType = pgsqlDbType | NpgsqlDbType.Array;
			return pgsqlDbType;
		}
	}
}

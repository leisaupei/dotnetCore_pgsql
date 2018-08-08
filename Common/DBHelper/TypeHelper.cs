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
				default: return null;
			}
		}
	}
}

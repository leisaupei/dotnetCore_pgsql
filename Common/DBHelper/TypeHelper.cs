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
			NpgsqlDbType[] isString = { NpgsqlDbType.Char, NpgsqlDbType.Varchar, NpgsqlDbType.Text };
			foreach (var p in nps)
			{
				var value = GetParamValue(p);
				var key = string.Concat("@", p.ParameterName);
				if (value == null)
					sql = GetNullSql(sql, key);
				else if (Regex.IsMatch(value, @"(^(\-|\+)?\d+(\.\d+)?$)|(^SELECT\s.+\sFROM\s)|(true)|(false)",
					RegexOptions.IgnoreCase) && !isString.Contains(NpgsqlDbType.Varchar)) sql = sql.Replace(key, value);
				else sql = sql.Replace(key, $"'{value}'");
			}
			return sql.Replace("\r", " ").Replace("\n", " ");
		}
		public static string GetNullSql(string sql, string key)
		{
			var equalsReg = new Regex(@"=\s*" + key);
			var notEqualsReg = new Regex(@"(!=|<>)\s*" + key);
			if (notEqualsReg.IsMatch(sql))
				return notEqualsReg.Replace(sql, " IS NOT NULL");
			else return equalsReg.Replace(sql, " IS NULL");
		}
		
		public static string GetParamValue(object value)
		{
			Type type = value.GetType();
			if (type.BaseType.IsArray)
			{
				var arrStr = (value as object[]).Select(a => a.ToEmptyOrString());
				return $"{{{string.Join(",", arrStr)}}}";
			}
			return value.ToNullOrString();
		}
		public static NpgsqlDbType? GetDbType(Type type)
		{
			NpgsqlDbType? pgsqlDbType = null;
			string type_name = type.Name.ToLower();
			type_name = type_name.EndsWith("[]") ? type_name.TrimEnd(']', '[') : type_name;
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
			if (type.BaseType.IsArray)
				pgsqlDbType = pgsqlDbType == null ? NpgsqlDbType.Array : pgsqlDbType | NpgsqlDbType.Array;
			return pgsqlDbType;
		}
	}
}

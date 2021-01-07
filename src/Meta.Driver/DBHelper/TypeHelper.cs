using Npgsql;
using NpgsqlTypes;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text.RegularExpressions;

namespace Meta.Driver.DbHelper
{
	internal class TypeHelper
	{
		public static string SqlToString(string sql, List<DbParameter> nps)
		{
			NpgsqlDbType[] isString = { NpgsqlDbType.Char, NpgsqlDbType.Varchar, NpgsqlDbType.Text };
			foreach (NpgsqlParameter p in nps)
			{
				var value = GetParamValue(p.Value);
				var key = string.Concat("@", p.ParameterName);
				if (value == null)
					sql = GetNullSql(sql, key);

				else if (Regex.IsMatch(value, @"(^(\-|\+)?\d+(\.\d+)?$)|(^SELECT\s.+\sFROM\s)|(true)|(false)", RegexOptions.IgnoreCase) && !isString.Contains(p.NpgsqlDbType))
					sql = sql.Replace(key, value);

				else if (value.Contains("array"))
					sql = sql.Replace(key, value);

				else
					sql = sql.Replace(key, $"'{value}'");
			}
			return sql.Replace(Environment.NewLine, " ").Replace("\r", " ").Replace("\n", " ");
		}
		public static string GetNullSql(string sql, string key)
		{
			var equalsReg = new Regex(@"=\s*" + key);
			var notEqualsReg = new Regex(@"(!=|<>)\s*" + key);
			if (notEqualsReg.IsMatch(sql))
				return notEqualsReg.Replace(sql, " IS NOT NULL");
			else if (equalsReg.IsMatch(sql))
				return equalsReg.Replace(sql, " IS NULL");
			else
				return sql;
		}
		public static string GetParamValue(object value)
		{
			Type type = value.GetType();
			if (type.IsArray)
			{
				var arrStr = (value as object[]).Select(a => $"'{a?.ToString() ?? ""}'");
				return $"array[{string.Join(",", arrStr)}]";
			}
			return value?.ToString();
		}
	}
}

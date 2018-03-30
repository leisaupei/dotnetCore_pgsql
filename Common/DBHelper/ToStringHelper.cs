using Npgsql;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace DBHelper
{
	public class ToStringHelper
	{
		public static string SqlToString(string sql, List<NpgsqlParameter> nps)
		{
			foreach (var p in nps)
			{
				var value = p.Value.ToString();
				var key = string.Concat("@", p.ParameterName);
				if (value == null)
				{
					if (sql.Contains("="))
						sql = Regex.Replace(sql, @"\s+=\s+\" + key, " IS NULL");
					if (sql.Contains("!="))
						sql = Regex.Replace(sql, @"\s+!=\s+\" + key, " IS NOT NULL");
				}
				else if (Regex.IsMatch(value, @"^(\-|\+)?\d+(\.\d+)?$") ||
					Regex.IsMatch(value, @"^SELECT\s.+\FROM\s", RegexOptions.IgnoreCase))
					sql = sql.Replace(key, value);
				else
					sql = sql.Replace(key, $"'{value}'");
			}
			return sql.Replace("\r", " ").Replace("\n", " ");
		}
	}
}

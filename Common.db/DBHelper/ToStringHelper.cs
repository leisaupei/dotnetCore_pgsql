using Npgsql;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace Common.db.DBHelper
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
                    sql = Regex.Replace(sql, @"\s+=\s+\" + key, " IS NULL");
                else if (Regex.IsMatch(value, @"^(\-|\+)?\d+(\.\d+)?$") ||
                    Regex.IsMatch(value, @"^select\s.+\sfrom\s", RegexOptions.IgnoreCase))
                        sql = sql.Replace(key, value);
                else
                    sql = sql.Replace(key, $"'{value}'");
            }
            return sql.Replace("\r", " ").Replace("\n", " ");
        }
    }
}

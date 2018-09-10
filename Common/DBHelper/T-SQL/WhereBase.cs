using Npgsql;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace DBHelper
{
	public abstract class WhereBase<TSQL> : BuilderBase<TSQL> where TSQL : class, new()
	{
		TSQL _this => this as TSQL;
		protected WhereBase(string table, string alias) : base(table, alias) { }
		protected WhereBase(string table) : base(table) { }
		protected WhereBase() { }

		public TSQL Where(string where)
		{
			_where.Add($"({where})");
			return _this;
		}
		public TSQL WhereNotIn<T>(string field, SelectBuilder<T> selectBuilder) where T : class, new()
		{
			ThrowNullFieldException(selectBuilder);
			return WhereNotIn(field, selectBuilder.ToString());
		}
		public TSQL WhereNotIn<T>(string field, IEnumerable<T> arr) => WhereNotIn(field, arr.Join(", "));
		public TSQL WhereNotIn(string field, string sql) => Where($"{field} NOT IN ({sql})");
		public TSQL WhereIn<T>(string field, SelectBuilder<T> selectBuilder) where T : class, new()
		{
			ThrowNullFieldException(selectBuilder);
			return WhereIn(field, selectBuilder.ToString());
		}
		public TSQL WhereIn<T>(string field, IEnumerable<T> arr) => WhereIn(field, arr.Join(", "));
		public TSQL WhereIn(string field, string sql) => Where($"{field} IN ({sql})");
		public TSQL WhereExsit<T>(SelectBuilder<T> selectBuilder) where T : class, new()
		{
			SetExistsField(selectBuilder);
			return WhereExsit(selectBuilder.ToString().Replace("a.", "a1."));
		}
		public TSQL WhereExsit(string sql) => Where($"EXISTS ({sql})");
		public TSQL WhereNotExsit<T>(SelectBuilder<T> selectBuilder) where T : class, new()
		{
			SetExistsField(selectBuilder);
			return WhereNotExsit(selectBuilder.ToString().Replace("a.", "a1."));
		}
		public TSQL WhereNotExsit(string sql) => Where($"NOT EXISTS ({sql})");
		public TSQL WhereOr<T>(string filter, IEnumerable<T> val)
		{
			var typeT = typeof(T);
			if (val.Count() == 0) return _this;
			if (typeT == typeof(char)) return Where(filter, (object)val);
			if (typeT == typeof(object)) return Where(filter, val as object[]);
			if (val.Count() == 1) return Where(filter, val.ElementAt(0));
			string filters = string.Empty;
			for (int a = 0; a < val.Count(); a++)
				filters = string.Concat(filters, " OR ", string.Format(filter, "{" + a + "}"));
			object[] parms = new object[val.Count()];
			val.ToArray().CopyTo(parms, 0);
			return Where(filters.Substring(4), parms);
		}
		public TSQL Where(bool isAdd, string filter, params object[] val) => isAdd ? Where(filter, val) : _this;
		public TSQL Where<T1, T2>(string[] keys, IEnumerable<(T1, T2)> val) => WhereTuple(f =>
		{
			var item = val.ElementAt(f.Item2 / keys.Length);
			f.Item1.Add(item.Item1); f.Item1.Add(item.Item2);
		}, keys, val.Count());
		public TSQL Where<T1, T2, T3>(string[] keys, IEnumerable<(T1, T2, T3)> val) => WhereTuple(f =>
		{
			var item = val.ElementAt(f.Item2 / keys.Length);
			f.Item1.Add(item.Item1); f.Item1.Add(item.Item2);
			f.Item1.Add(item.Item3);
		}, keys, val.Count());
		public TSQL Where<T1, T2, T3, T4>(string[] keys, IEnumerable<(T1, T2, T3, T4)> val) => WhereTuple(f =>
		{
			var item = val.ElementAt(f.Item2 / keys.Length);
			f.Item1.Add(item.Item1); f.Item1.Add(item.Item2);
			f.Item1.Add(item.Item3); f.Item1.Add(item.Item4);
		}, keys, val.Count());
		public TSQL WhereArray<T>(string filter, T[] val) => Where(filter, new object[] { val });
		public TSQL Where(string filter, params object[] val)
		{
			if (val.IsNullOrEmpty()) filter = TypeHelper.GetNullSql(filter, @"\{\d\}");
			else
				for (int i = 0; i < val.Length; i++)
				{
					var index = $"{{{i}}}";
					if (filter.IndexOf(index, StringComparison.Ordinal) == -1) throw new ArgumentException("Where Argument Error");
					if (val[i] == null)
						filter = TypeHelper.GetNullSql(filter, index);
					else
					{
						var reg = new Regex(@"^SELECT\s.+\sFROM\s");
						if (reg.IsMatch(val[i].ToString()))
							filter = filter.Replace(index, $"({val[i].ToString()})");
						else
						{
							var paramsName = ParamsIndex;
							AddParameter(paramsName, val);
							filter = filter.Replace(index, "@" + paramsName);
						}
					}
				}
			Where($"{filter}");
			return _this;
		}
		protected TSQL WhereTuple(Action<(List<object>, int)> action, string[] keys, int arrLength)
		{
			var parms = new List<object>();
			var count = keys.Length;
			StringBuilder sb = new StringBuilder();
			for (int a = 0; a < arrLength * count; a += count)
			{
				if (a != 0) sb.Append(" OR ");
				sb.Append("(");
				for (int b = 0; b < count; b++)
				{
					sb.Append($"{keys[b]} = {{{a + b}}}");
					if (b != count - 1) sb.Append(" AND ");
				}
				sb.Append(")");
				action.Invoke((parms, a));
			}
			return Where(sb.ToString(), parms.ToArray());
		}
		private static void ThrowNullFieldException<T>(SelectBuilder<T> selectBuilder) where T : class, new()
		{
			Type type = typeof(T);
			var fields = type.GetProperty("_fields", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(selectBuilder).ToString();
			if (fields.IsNullOrEmpty()) throw new ArgumentNullException("_fields is null.");
		}
		private static void SetExistsField<T>(SelectBuilder<T> selectBuilder) where T : class, new()
		{
			Type type = typeof(TSQL);
			type.GetProperty("_mainAlias", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(selectBuilder, "a1");
			var fields = type.GetProperty("_fields", BindingFlags.NonPublic | BindingFlags.Instance);
			var fieldStr = fields.GetValue(selectBuilder).ToString();
			if (fieldStr.IsNullOrEmpty())
				fields.SetValue(selectBuilder, fieldStr.Split(',')[0]);
		}
	}
}

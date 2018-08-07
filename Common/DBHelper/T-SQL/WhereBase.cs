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

		#region Where Expression
		public TSQL Where(string where)
		{
			_where.Add($"({where})");
			return _this;
		}
		public TSQL WhereIn(string field, TSQL selectBuilder) => !_fields.IsNullOrEmpty() ? Where($"{field} IN ({selectBuilder})") : throw new ArgumentNullException("_fields is null.");
		public TSQL WhereIn(string field, string selectBuilder) => Where($"{field} IN ({selectBuilder})");
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
		public TSQL WhereExsit(TSQL selectBuilder)
		{
			Type type = typeof(TSQL);
			var alias = type.GetProperty("_mainAlias", BindingFlags.NonPublic | BindingFlags.Instance);
			alias.SetValue(selectBuilder, "a1");
			var fields = type.GetProperty("_fields", BindingFlags.NonPublic | BindingFlags.Instance);
			var fieldStr = fields.GetValue(selectBuilder);
			fields.SetValue(selectBuilder, fieldStr.ToString().Split(',')[0]);
			return WhereExsit(selectBuilder.ToString().Replace("a.", "a1."));
		}
		public TSQL WhereExsit(string sql) => Where($"EXISTS ({sql})");
		public TSQL Where(bool isAdd, string filter, params object[] val) => isAdd ? Where(filter, val) : _this;
		protected TSQL WhereTuple(Action<(List<object>, int)> action, string[] keys, int arrLength)
		{
			var parms = new List<object>();
			var count = keys.Length;
			string filters = string.Empty;
			for (int a = 0; a < arrLength * count; a += count)
			{
				if (a != 0) filters += " OR ";
				filters += "(";
				for (int b = 0; b < count; b++)
				{
					filters += $"{keys[b]} = {{{a + b}}}";
					if (b != count - 1) filters += " AND ";
				}
				filters += ")";
				action.Invoke((parms, a));
			}
			return Where(filters, parms.ToArray());
		}
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
		public TSQL Where(string filter, params object[] val)
		{
			if (val == null) val = new object[] { null };
			if (new Regex(@"\{\d\}").Matches(filter).Count < val.Length)//参数个数不匹配
				throw new ArgumentException("where 参数错误");
			if (val.IsNullOrEmpty())//参数不能为空
				throw new ArgumentException("where 参数错误");

			for (int i = 0; i < val.Length; i++)
			{
				var index = string.Concat("{", i, "}");
				if (filter.IndexOf(index, StringComparison.Ordinal) == -1) throw new ArgumentException("where 参数错误");
				if (val[i] == null) //支持 Where("id = {0}", null)与Where("id != {0}", null); 写法
				{
					if (filter.Contains("!=") || filter.Contains("<>"))
						filter = Regex.Replace(filter, @"\s+!=\s+\{" + i + @"\}", " IS NOT NULL");
					else if (filter.Contains("="))
						filter = Regex.Replace(filter, @"\s+=\s+\{" + i + @"\}", " IS NULL");
				}
				else
				{
					var paramsName = ParamsIndex;
					filter = filter.Replace(index, "@" + paramsName);
					AddParameter(paramsName, val[i]);
				}
			}
			Where($"{filter}");
			return _this;
		}
		#endregion
	}
}

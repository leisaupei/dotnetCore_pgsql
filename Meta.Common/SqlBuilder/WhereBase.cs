using Meta.Common.DbHelper;
using Meta.Common.Extensions;
using Meta.Common.Model;
using Npgsql;
using NpgsqlTypes;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace Meta.Common.SqlBuilder
{
	public abstract class WhereBase<TSQL> : BuilderBase<TSQL> where TSQL : class, new()
	{
		TSQL This => this as TSQL;
		#region Constructor
		protected WhereBase(string table, string alias) : base(table, alias) { }
		protected WhereBase(string table) : base(table) { }
		protected WhereBase() { }
		#endregion

		/// <summary>
		/// 字符串where语句
		/// </summary>
		/// <param name="where"></param>
		/// <returns></returns>
		public TSQL Where(string where)
		{
			base.WhereList.Add($"({where})");
			return This;
		}
		/// <summary>
		/// not in
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="field"></param>
		/// <param name="selectBuilder"></param>
		/// <example>WhereNotIn("a.id",xxx.SelectDiy("id").Where())</example>
		/// <returns></returns>
		public TSQL WhereNotIn<T>(string field, SelectBuilder<T> selectBuilder) where T : class, new()
		{
			ThrowNullFieldException(selectBuilder);
			return WhereNotIn(field, selectBuilder.ToString());
		}
		/// <summary>
		/// 
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="field"></param>
		/// <param name="value"></param>
		/// <example>WhereNotIn("a.id",new Guid[]{})</example>
		/// <exception cref="ArgumentNullException">values is null or length is zero</exception>
		/// <returns></returns>
		public TSQL WhereNotIn<T>(string field, IEnumerable<T> values)
		{
			if (values.IsNullOrEmpty())
				throw new ArgumentNullException(nameof(values));
			return WhereNotIn(field, string.Join(", ", values));
		}
		/// <summary>
		/// 
		/// </summary>
		/// <param name="field"></param>
		/// <param name="sql"></param>
		/// <exception cref="ArgumentNullException">sql is null or empty</exception>
		/// <returns></returns>
		public TSQL WhereNotIn(string field, string sql)
		{
			if (string.IsNullOrEmpty(sql))
				throw new ArgumentNullException(nameof(sql));
			return Where($"{field} NOT IN ({sql})");
		}
		/// <summary>
		/// 
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="field"></param>
		/// <param name="selectBuilder"></param>
		/// <returns></returns>
		public TSQL WhereIn<T>(string field, SelectBuilder<T> selectBuilder) where T : class, new()
		{
			ThrowNullFieldException(selectBuilder);
			return WhereIn(field, selectBuilder.ToString());
		}
		/// <summary>
		/// 
		/// </summary>
		/// <param name="field"></param>
		/// <param name="sql"></param>
		/// <exception cref="ArgumentNullException">value is null or empty</exception>
		/// <returns></returns>
		public TSQL WhereIn<T>(string field, IEnumerable<T> values)
		{
			if (values.IsNullOrEmpty())
				throw new ArgumentNullException(nameof(values));
			return WhereIn(field, string.Join(", ", values.Select(f => $"'{f}'")));
		}
		/// <summary>
		/// 
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="field"></param>
		/// <param name="values"></param>
		/// <returns></returns>
		public TSQL WhereInDefault<T>(string field, IEnumerable<T> values)
		{
			if (values.IsNullOrEmpty()) IsReturnDefault = true;
			else WhereIn(field, string.Join(", ", values.Select(f => $"'{f}'")));
			return This;
		}
		/// <summary>
		/// 
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="field"></param>
		/// <param name="values"></param>
		/// <returns></returns>
		public TSQL WhereAnyDefault<T>(string field, IEnumerable<T> values)
		{
			if (values.IsNullOrEmpty()) IsReturnDefault = true;
			else Where(field + " = any({0})", new object[] { values.ToArray() });
			return This;
		}
		/// <summary>
		/// 
		/// </summary>
		/// <param name="field"></param>
		/// <param name="sql"></param>
		/// <exception cref="ArgumentNullException">value is null or empty</exception>
		/// <returns></returns>
		public TSQL WhereIn(string field, string sql)
		{
			if (string.IsNullOrEmpty(sql))
				throw new ArgumentNullException(nameof(sql));
			return Where($"{field} IN ({sql})");
		}
		/// <summary>
		/// 
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="selectBuilder"></param>
		/// <returns></returns>
		public TSQL WhereExists<T>(SelectBuilder<T> selectBuilder) where T : class, new()
		{
			SetExistsField(selectBuilder);
			return WhereExists(selectBuilder.ToString());
		}
		/// <summary>
		/// 
		/// </summary>
		/// <param name="sql"></param>
		/// <exception cref="ArgumentNullException">value is null or empty</exception>
		/// <returns></returns>
		public TSQL WhereExists(string sql)
		{
			if (string.IsNullOrEmpty(sql))
				throw new ArgumentNullException(nameof(sql));
			return Where($"EXISTS ({sql})");

		}
		/// <summary>
		/// 
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="selectBuilder"></param>
		/// <returns></returns>
		public TSQL WhereNotExsit<T>(SelectBuilder<T> selectBuilder) where T : class, new()
		{
			SetExistsField(selectBuilder);
			return WhereNotExists(selectBuilder.ToString());
		}
		/// <summary>
		/// 
		/// </summary>
		/// <param name="sql"></param>
		/// <exception cref="ArgumentNullException">value is null or empty</exception>
		/// <returns></returns>
		public TSQL WhereNotExists(string sql)
		{
			if (string.IsNullOrEmpty(sql))
				throw new ArgumentNullException(nameof(sql));
			return Where($"NOT EXISTS ({sql})");
		}
		/// <summary>
		/// where or 如果val 是空或长度为0 直接返回空数据
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="filter"></param>
		/// <param name="val"></param>
		/// <param name="dbType"></param>
		/// <returns></returns>
		public TSQL WhereOrDefault<T>(string filter, IEnumerable<T> val, NpgsqlDbType? dbType = null)
		{
			if (val.IsNullOrEmpty()) IsReturnDefault = true;
			else WhereOr(filter, val, dbType);
			return This;
		}
		/// <summary>
		/// where or条件
		/// </summary>
		/// <typeparam name="T">数组类型</typeparam>
		/// <param name="filter">xxx={0}</param>
		/// <param name="val">{0}的数组</param>
		/// <param name="dbType">CLR类型</param>
		/// <example>WhereOr("xxx={0}",new[]{1,2},NpgsqlDbType.Integer)</example>
		/// <returns></returns>
		public TSQL WhereOr<T>(string filter, IEnumerable<T> val, NpgsqlDbType? dbType = null)
		{
			object[] _val = null;
			var typeT = typeof(T);
			if (val == null)
				return Where(filter, null);
			if (val.Count() == 0)
				return This;
			else if (typeT == typeof(char))
				_val = val.Select(a => new DbTypeValue(a, dbType)).ToArray<object>();
			else if (typeT == typeof(object))
				_val = dbType.HasValue ? val.Select(a => new DbTypeValue(a, dbType)).ToArray<object>() : val as object[];
			else if (typeT == typeof(DbTypeValue))
				_val = val as object[];
			else if (val.Count() == 1)
			{
				if (val.ElementAt(0) == null) return Where(filter, null);
				_val = dbType.HasValue ? new object[] { new DbTypeValue(val.ElementAt(0), dbType) } : new object[] { val.ElementAt(0) };
			}
			string filters = filter;
			if (_val == null)
			{
				for (int a = 1; a < val.Count(); a++)
					filters = string.Concat(filters, " OR ", string.Format(filter, "{" + a + "}"));
				_val = dbType.HasValue ? val.Select(a => new DbTypeValue(a, dbType)).ToArray<object>() : val.OfType<object>().ToArray();
			}
			return Where(filters, _val);
		}
		/// <summary>
		/// 
		/// </summary>
		/// <param name="isAdd"></param>
		/// <param name="filter"></param>
		/// <param name="val"></param>
		/// <returns></returns>
		public TSQL Where(bool isAdd, string filter, params object[] val) => isAdd ? Where(filter, val) : This;
		/// <summary>
		/// 是否添加func返回的where语句
		/// </summary>
		/// <param name="isAdd"></param>
		/// <param name="filter"></param>
		/// <example>Where(bool, () => $"xxx='{xxx}'")</example>
		/// <returns></returns>
		public TSQL Where(bool isAdd, Func<string> filter)
		{
			if (isAdd)
				Where(filter.Invoke());
			return This;
		}
		/// <summary>
		/// 是否添加func返回的where语句, format格式
		/// </summary>
		/// <param name="isAdd">是否添加</param>
		/// <param name="filter">返回Where(string,object) </param>
		/// <example>Where(bool, () => ("xxx={0}", value))</example>
		/// <returns></returns>
		public TSQL Where(bool isAdd, Func<(string, object)> filter)
		{
			if (isAdd)
			{
				var (sql, ps) = filter.Invoke();
				Where(sql, ps);
			}
			return This;
		}
		/// <summary>
		/// 双主键
		/// </summary>
		/// <typeparam name="T1"></typeparam>
		/// <typeparam name="T2"></typeparam>
		/// <param name="keys"></param>
		/// <param name="val"></param>
		/// <param name="dbTypes"></param>
		/// <returns></returns>
		public TSQL Where<T1, T2>(string[] keys, IEnumerable<(T1, T2)> val, NpgsqlDbType?[] dbTypes = null) => WhereTuple(f =>
		{
			var item = val.ElementAt(f.Item2 / keys.Length);
			if (dbTypes == null)
			{
				f.Item1.Add(item.Item1);
				f.Item1.Add(item.Item2);
			}
			else
			{
				f.Item1.Add(new DbTypeValue(item.Item1, dbTypes[0]));
				f.Item1.Add(new DbTypeValue(item.Item2, dbTypes[1]));
			}
		}, keys, val.Count());
		/// <summary>
		/// 三主键
		/// </summary>
		/// <typeparam name="T1"></typeparam>
		/// <typeparam name="T2"></typeparam>
		/// <typeparam name="T3"></typeparam>
		/// <param name="keys"></param>
		/// <param name="val"></param>
		/// <param name="dbTypes"></param>
		/// <returns></returns>
		public TSQL Where<T1, T2, T3>(string[] keys, IEnumerable<(T1, T2, T3)> val, NpgsqlDbType?[] dbTypes = null) => WhereTuple(f =>
		{
			var item = val.ElementAt(f.Item2 / keys.Length);
			if (dbTypes == null)
			{
				f.Item1.Add(item.Item1);
				f.Item1.Add(item.Item2);
				f.Item1.Add(item.Item3);
			}
			else
			{
				f.Item1.Add(new DbTypeValue(item.Item1, dbTypes[0]));
				f.Item1.Add(new DbTypeValue(item.Item2, dbTypes[1]));
				f.Item1.Add(new DbTypeValue(item.Item3, dbTypes[2]));
			}
		}, keys, val.Count());
		/// <summary>
		/// 四主键
		/// </summary>
		/// <typeparam name="T1"></typeparam>
		/// <typeparam name="T2"></typeparam>
		/// <typeparam name="T3"></typeparam>
		/// <typeparam name="T4"></typeparam>
		/// <param name="keys"></param>
		/// <param name="val"></param>
		/// <param name="dbTypes"></param>
		/// <returns></returns>
		public TSQL Where<T1, T2, T3, T4>(string[] keys, IEnumerable<(T1, T2, T3, T4)> val, NpgsqlDbType?[] dbTypes = null) => WhereTuple(f =>
		{
			var item = val.ElementAt(f.Item2 / keys.Length);
			if (dbTypes == null)
			{
				f.Item1.Add(item.Item1); f.Item1.Add(item.Item2);
				f.Item1.Add(item.Item3); f.Item1.Add(item.Item4);
			}
			else
			{
				f.Item1.Add(new DbTypeValue(item.Item1, dbTypes[0]));
				f.Item1.Add(new DbTypeValue(item.Item2, dbTypes[1]));
				f.Item1.Add(new DbTypeValue(item.Item3, dbTypes[2]));
				f.Item1.Add(new DbTypeValue(item.Item4, dbTypes[3]));
			}
		}, keys, val.Count());
		/// <summary>
		/// 
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="filter"></param>
		/// <param name="value"></param>
		/// <param name="dbType"></param>
		/// <exception cref="ArgumentNullException">value is null or empty</exception>
		/// <returns></returns>
		public TSQL WhereArray<T>(string filter, IEnumerable<T> value, NpgsqlDbType? dbType = null)
		{
			if (value == null)
				throw new ArgumentNullException(nameof(value));
			return dbType.HasValue ? Where(filter, new[] { new DbTypeValue(value, dbType) }) : Where(filter, new object[] { value });
		}
		/// <summary>
		/// 
		/// </summary>
		/// <param name="filter"></param>
		/// <param name="val"></param>
		/// <returns></returns>
		public TSQL Where(string filter, params object[] val)
		{
			if (val.IsNullOrEmpty())
				filter = TypeHelper.GetNullSql(filter, @"\{\d\}");
			else
			{
				for (int i = 0; i < val.Length; i++)
				{
					var index = string.Concat("{", i, "}");
					if (filter.IndexOf(index, StringComparison.Ordinal) == -1)
						throw new ArgumentException(nameof(filter));
					if (val[i] == null)
						filter = TypeHelper.GetNullSql(filter, index.Replace("{", @"\{").Replace("}", @"\}"));
					else
					{
						var reg = new Regex(@"^SELECT\s.+\sFROM\s");
						if (reg.IsMatch(val[i].ToString()))
							filter = filter.Replace(index, $"({val[i].ToString()})");
						else
						{
							var paramsName = ParamsIndex;
							if (val[i] is DbTypeValue _val)
								AddParameter(paramsName, _val);
							else
								AddParameter(paramsName, val[i]);
							filter = filter.Replace(index, "@" + paramsName);
						}
					}
				}
			}
			Where($"{filter}");
			return This;
		}
		/// <summary>
		/// 
		/// </summary>
		/// <param name="action"></param>
		/// <param name="keys"></param>
		/// <param name="arrLength"></param>
		/// <returns></returns>
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
		/// <summary>
		/// 
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="selectBuilder"></param>
		static void ThrowNullFieldException<T>(SelectBuilder<T> selectBuilder) where T : class, new()
		{
			Type type = typeof(T);
			var fields = type.GetProperty("Fields", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(selectBuilder).ToString();
			if (string.IsNullOrEmpty(fields))
				throw new ArgumentNullException(nameof(fields));
		}
		/// <summary>
		/// 
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="selectBuilder"></param>
		static void SetExistsField<T>(SelectBuilder<T> selectBuilder) where T : class, new()
		{
			Type type = selectBuilder.GetType();
			var property = type.GetProperty("MainAlias", BindingFlags.NonPublic | BindingFlags.Instance);
			var refValue = property.GetValue(selectBuilder).ToString();
			if (refValue == "a")
				property.SetValue(selectBuilder, "a1");
			var fields = type.GetProperty("Fields", BindingFlags.NonPublic | BindingFlags.Instance);
			var fieldStr = fields.GetValue(selectBuilder).ToString();
			if (!string.IsNullOrEmpty(fieldStr) && fieldStr.Contains(','))
				fields.SetValue(selectBuilder, fieldStr.Split(',')[0]);
		}
	}
}

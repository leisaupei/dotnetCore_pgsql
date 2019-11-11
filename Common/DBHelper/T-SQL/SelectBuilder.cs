using DBHelper;
using Npgsql;
using NpgsqlTypes;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace DBHelper
{
	public abstract class SelectBuilder<TSQL> : WhereBase<TSQL> where TSQL : class, new()
	{
		readonly List<Union> _listUnion = new List<Union>();
		string _groupBy = string.Empty;
		string _orderBy = string.Empty;
		string _limit = string.Empty;
		string _offset = string.Empty;
		string _having = string.Empty;
		string _union = string.Empty;
		string _tablesampleSystem = string.Empty;

		protected SelectBuilder(string fields, string alias)
		{
			Fields = fields;
			MainAlias = alias;
		}
		public SelectBuilder(string fields) => Fields = fields;
		public SelectBuilder() => Fields = "*";
		TSQL This => this as TSQL;
		public TSQL From(string table, string alias = "a")
		{
			MainAlias = alias;
			if (new Regex(@"^SELECT\s.+\sFROM\s").IsMatch(table))
				MainTable = $"({table})";
			else
				MainTable = table;
			return This;
		}
		public TSQL GroupBy(string s)
		{
			_groupBy = $"GROUP BY {s}";
			return This;
		}
		public TSQL OrderBy(string s)
		{
			_orderBy = $"ORDER BY {s}";
			return This;
		}
		public TSQL Having(string s)
		{
			_having = $"HAVING {s}";
			return This;
		}
		public TSQL Limit(int i)
		{
			_limit = $"LIMIT {i}";
			return This;
		}
		public TSQL Skip(int i)
		{
			_offset = $"OFFSET {i}";
			return This;
		}
		public TSQL Union(string view)
		{
			_union = $"UNION ({view})";
			return This;
		}
		public TSQL Union(TSQL selectBuilder)
		{
			_union = $"UNION ({selectBuilder})";
			return This;
		}
		public TSQL Page(int pageIndex, int pageSize)
		{
			Limit(pageSize); Skip(Math.Max(0, pageIndex - 1) * pageSize);
			return This;
		}

		public TSQL TableSampleSystem(double percent)
		{
			_tablesampleSystem = $" tablesample system({percent}) ";
			return This;
		}

		#region Union
		public TSQL InnerJoin<T>(SelectBuilder<T> selectBuilder, string alias, string on) where T : class, new()
			=> Join(UnionEnum.INNER_JOIN, $"({selectBuilder})", alias, on);
		public TSQL LeftJoin<T>(SelectBuilder<T> selectBuilder, string alias, string on) where T : class, new()
			=> Join(UnionEnum.LEFT_JOIN, $"({selectBuilder})", alias, on);
		public TSQL RightJoin<T>(SelectBuilder<T> selectBuilder, string alias, string on) where T : class, new()
			=> Join(UnionEnum.RIGHT_JOIN, $"({selectBuilder})", alias, on);
		public TSQL InnerJoin(string table, string alias, string on) => Join(UnionEnum.INNER_JOIN, table, alias, on);
		public TSQL LeftJoin(string table, string alias, string on) => Join(UnionEnum.LEFT_JOIN, table, alias, on);
		public TSQL RightJoin(string table, string alias, string on) => Join(UnionEnum.RIGHT_JOIN, table, alias, on);
		public TSQL InnerJoin<TTarget>(string alias, string on) => Join<TTarget>(UnionEnum.INNER_JOIN, alias, on);
		public TSQL LeftJoin<TTarget>(string alias, string on) => Join<TTarget>(UnionEnum.LEFT_JOIN, alias, on);
		public TSQL RightJoin<TTarget>(string alias, string on) => Join<TTarget>(UnionEnum.RIGHT_JOIN, alias, on);
		public TSQL Join<TTarget>(UnionEnum unionType, string alias, string on) => Join(unionType, MappingHelper.GetMapping(typeof(TTarget)), alias, on);
		public TSQL Join(UnionEnum unionType, string table, string aliasName, string on)
		{
			if (new Regex(@"\{\d\}").Matches(on).Count > 0)//参数个数不匹配
				throw new ArgumentException("on 参数不支持存在参数");
			_listUnion.Add(new Union(aliasName, table, on, unionType));
			return This;
		}
		#endregion
		/// <summary>
		/// 返回一行(管道)
		/// </summary>
		public TSQL ToListPipe<T>(string fields = null)
		{
			if (!string.IsNullOrEmpty(fields)) Fields = fields;
			return base.ToPipe<T>(true);
		}
		/// <summary>
		/// 返回列表
		/// </summary>
		public List<T> ToList<T>(string fields = null)
		{
			if (!string.IsNullOrEmpty(fields)) Fields = fields;
			if (_enumerableNullReturnDefault) return new List<T>();

			return base.ToList<T>();
		}
		/// <summary>
		/// 返回一行(管道)
		/// </summary>
		public TSQL ToOnePipe<T>(string fields = null)
		{
			_limit = "LIMIT 1";
			if (!string.IsNullOrEmpty(fields)) Fields = fields;
			return base.ToPipe<T>(false);
		}
		/// <summary>
		/// 返回一行
		/// </summary>
		public T ToOne<T>(string fields = null)
		{
			_limit = "LIMIT 1";
			if (!string.IsNullOrEmpty(fields)) Fields = fields;
			return base.ToOne<T>();
		}
		/// <summary>
		/// 返回第一个元素
		/// </summary>
		public TResult ToScalar<TResult>(string fields)
		{
			Fields = fields;
			return (TResult)ToScalar();
		}

		public long Count() => ToScalar<long>("COUNT(1)");
		public TResult Max<TResult>(string field, string coalesce = "0") => ToScalar<TResult>($"COALESCE(MAX({field}),{coalesce})");
		public TResult Min<TResult>(string field, string coalesce = "0") => ToScalar<TResult>($"COALESCE(MIN({field}),{coalesce})");
		public TResult Sum<TResult>(string field, string coalesce = "0") => ToScalar<TResult>($"COALESCE(SUM({field}),{coalesce})");
		public TResult Avg<TResult>(string field, string coalesce = "0") => ToScalar<TResult>($"COALESCE(AVG({field}),{coalesce})");

		#region Implicit
		public static implicit operator string(SelectBuilder<TSQL> selectBuilder) => selectBuilder.ToString();
		#endregion

		#region Override
		public override string ToString() => base.ToString();
		public new string ToString(string field) => base.ToString(field);
		public override string GetCommandTextString()
		{
			StringBuilder sqlText = new StringBuilder($"SELECT {Fields} FROM {MainTable} {MainAlias} {_tablesampleSystem}");
			foreach (var item in _listUnion)
				sqlText.AppendLine(string.Format("{0} {1} {2} ON {3}", item.UnionType.ToString().Replace("_", " "), item.Table, item.AliasName, item.Expression));
			// other
			if (WhereList?.Count() > 0) sqlText.AppendLine("WHERE " + string.Join(" AND ", WhereList));
			if (!string.IsNullOrEmpty(_groupBy)) sqlText.AppendLine(_groupBy);
			if (!string.IsNullOrEmpty(_groupBy) && !string.IsNullOrEmpty(_having)) sqlText.AppendLine(_having);
			if (!string.IsNullOrEmpty(_orderBy)) sqlText.AppendLine(_orderBy);
			if (!string.IsNullOrEmpty(_limit)) sqlText.AppendLine(_limit);
			if (!string.IsNullOrEmpty(_offset)) sqlText.AppendLine(_offset);
			if (!string.IsNullOrEmpty(_union)) sqlText.AppendLine(_union);
			return sqlText.ToString();
		}
		#endregion
	}
}

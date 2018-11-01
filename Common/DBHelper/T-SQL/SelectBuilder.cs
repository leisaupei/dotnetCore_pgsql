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
		List<Union> _listUnion { get; set; } = new List<Union>();
		string _groupBy = string.Empty;
		string _orderBy = string.Empty;
		string _limit = string.Empty;
		string _offset = string.Empty;
		string _having = string.Empty;
		string _union = string.Empty;
		protected SelectBuilder(string fields, string alias)
		{
			_fields = fields;
			_mainAlias = alias;
		}
		public SelectBuilder(string fields) => _fields = fields;
		public SelectBuilder() => _fields = "*";
		TSQL _this => this as TSQL;
		public TSQL From(string table, string alias = "a")
		{
			_mainAlias = alias;
			if (new Regex(@"^SELECT\s.+\sFROM\s").IsMatch(table))
				_mainTable = $"({table})";
			else
				_mainTable = table;
			return _this;
		}
		public TSQL GroupBy(string s)
		{
			_groupBy = $"GROUP BY {s}";
			return _this;
		}
		public TSQL OrderBy(string s)
		{
			_orderBy = $"ORDER BY {s}";
			return _this;
		}
		public TSQL Having(string s)
		{
			_having = $"HAVING {s}";
			return _this;
		}
		public TSQL Limit(int i)
		{
			_limit = $"LIMIT {i}";
			return _this;
		}
		public TSQL Skip(int i)
		{
			_offset = $"OFFSET {i}";
			return _this;
		}
		public TSQL Union(string view)
		{
			_union = $"UNION ({view})";
			return _this;
		}
		public TSQL Union(TSQL selectBuilder)
		{
			_union = $"UNION ({selectBuilder})";
			return _this;
		}
		public TSQL Page(int pageIndex, int pageSize)
		{
			Limit(pageSize); Skip(Math.Max(0, pageIndex - 1) * pageSize);
			return _this;
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
			return _this;
		}
		#endregion

		/// <summary>
		/// 返回列表
		/// </summary>
		public List<TResult> ToList<TResult>(string fields = null)
		{
			if (!fields.IsNullOrEmpty()) _fields = fields;
			return base.ToList<TResult>();
		}
		/// <summary>
		/// 返回一行
		/// </summary>
		public TResult ToOne<TResult>(string fields = null)
		{
			_limit = "LIMIT 1";
			if (!fields.IsNullOrEmpty()) _fields = fields;
			return base.ToOne<TResult>();
		}
		/// <summary>
		/// 返回第一个元素
		/// </summary>
		public TResult ToScalar<TResult>(string fields)
		{
			_fields = fields;
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
		protected override string SetCommandString()
		{
			StringBuilder sqlText = new StringBuilder($"SELECT {_fields} FROM {_mainTable} {_mainAlias} ");
			foreach (var item in _listUnion)
				sqlText.AppendLine(item.UnionType.ToString().Replace("_", " ") + " " + item.Table + " " + item.AliasName + " ON " + item.Expression);
			// other
			if (!_where.IsNullOrEmpty()) sqlText.AppendLine("WHERE " + _where.Join(" AND "));
			if (!_groupBy.IsNullOrEmpty()) sqlText.AppendLine(_groupBy);
			if (!_groupBy.IsNullOrEmpty() && !_having.IsNullOrEmpty()) sqlText.AppendLine(_having);
			if (!_orderBy.IsNullOrEmpty()) sqlText.AppendLine(_orderBy);
			if (!_limit.IsNullOrEmpty()) sqlText.AppendLine(_limit);
			if (!_offset.IsNullOrEmpty()) sqlText.AppendLine(_offset);
			if (!_union.IsNullOrEmpty()) sqlText.AppendLine(_union);
			return sqlText.ToString();
		}
		#endregion
	}
}

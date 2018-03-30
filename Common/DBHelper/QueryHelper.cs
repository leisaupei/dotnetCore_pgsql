using Npgsql;
using NpgsqlTypes;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using Newtonsoft.Json;
using System.Text.RegularExpressions;


namespace DBHelper
{
	public class QueryHelper<T>
	{
		#region 属性
		private int _paramsCount = 0;
		protected string ParamsIndex => "parameter_" + _paramsCount++;
		protected string GroupByText { get; set; }
		protected string OrderByText { get; set; }
		protected string LimitText { get; set; }
		protected string OffsetText { get; set; }
		protected string HavingText { get; set; }
		protected string CommandText { get; set; }
		protected string MasterAliasName { get; } = "a";
		protected string UnionAliasName { get; set; }
		protected string Field { get; set; }
		protected List<UnionModel> UnionList { get; set; } = new List<UnionModel>();
		protected List<NpgsqlParameter> CommandParams { get; set; } = new List<NpgsqlParameter>();
		protected List<string> WhereList { get; set; } = new List<string>();
		#endregion

		#region Method

		#region where
		protected QueryHelper<T> Where(string filter, Array values)
		{
			if (values == null) values = new object[] { null };
			if (values.Length == 0) return this;
			if (values.Length == 1) return Where(filter, values.GetValue(0));
			string filters = string.Empty;
			for (int a = 0; a < values.Length; a++) filters = string.Concat(filters, " OR ", string.Format(filter, "{" + a + "}"));
			object[] parms = new object[values.Length];
			values.CopyTo(parms, 0);
			return Where(filters.Substring(4), parms);
		}
		protected QueryHelper<T> Where(string filter, params object[] value)
		{
			if (value == null) value = new object[] { null };
			if (new Regex(@"\{\d\}").Matches(filter).Count != value.Length)//参数个数不匹配
				throw new Exception("where 参数错误");
			if (value.IsNullOrEmpty())//参数不能为空
				throw new Exception("where 参数错误");

			List<string> str_where = new List<string>();
			for (int i = 0; i < value.Length; i++)
			{
				var paramsName = ParamsIndex;
				var index = string.Concat("{", i, "}");
				if (filter.IndexOf(index, StringComparison.Ordinal) == -1) throw new Exception("where 参数错误");
				if (value[i] == null) //支持 Where("id = {0}", null)与Where("id != {0}", null); 写法
				{
					if (filter.Contains("="))
						filter = Regex.Replace(filter, @"\s+=\s+\{" + i + @"\}", " IS NULL");
					if (filter.Contains("!="))
						filter = Regex.Replace(filter, @"\s+!=\s+\{" + i + @"\}", " IS NOT NULL");
				}
				else
				{
					filter = filter.Replace(index, "@" + paramsName);
					AddSelectParameter(paramsName, value[i]);
				}
			}
			WhereList.Add(string.Concat("(", filter, ")"));
			return this;
		}
		#endregion

		#region union
		protected QueryHelper<T> Union<TModel>(UnionType unionType, string aliasName, string on)
		{
			if (new Regex(@"\{\d\}").Matches(on).Count > 0)//参数个数不匹配
				throw new ArgumentException("on 参数不支持存在参数");

			UnionModel us = new UnionModel
			{
				Model = typeof(TModel),
				Expression = on,
				UnionType = unionType,
				AliasName = aliasName
			};
			UnionList.Add(us);
			return this;
		}
		#endregion

		#region db 
		/// <summary>
		/// 修改返回第一行
		/// </summary>
		/// <param name="cmdText"></param>
		/// <returns></returns>
		protected T ExecuteNonQueryReader(string cmdText)
		{
			List<T> list = PgSqlHelper.ExecuteDataReaderList<T>(cmdText, CommandParams.ToArray());
			return list.Count > 0 ? list[0] : default(T);
		}
		/// <summary>
		/// 返回多行
		/// </summary>
		protected List<TResult> ExecuteReader<TResult>()
		{
			GetSqlString<TResult>();
			return PgSqlHelper.ExecuteDataReaderList<TResult>(CommandText, CommandParams.ToArray());
		}
		protected int ExecuteNonQuery(string cmdText) =>
			 PgSqlHelper.ExecuteNonQuery(CommandType.Text, cmdText, CommandParams.ToArray());

		#endregion
		/// <summary>
		/// 返回列表
		/// </summary>
		public List<TResult> ToList<TResult>(string fields = null)
		{
			if (!fields.IsNullOrEmpty())
				Field = fields;
			return ExecuteReader<TResult>();
		}

		/// <summary>
		/// 返回一行
		/// </summary>
		protected TResult ToOne<TResult>(string fields = null)
		{
			LimitText = "LIMIT 1";
			List<TResult> list = ToList<TResult>(fields);
			if (list.Count > 0)
				return list[0];
			return default(TResult);
		}

		/// <summary>
		/// 返回一个元素
		/// </summary>
		protected TResult ToScalar<TResult>(string fields)
		{
			Field = fields;
			GetSqlString<TResult>();
			object obj = PgSqlHelper.ExecuteScalar(CommandType.Text, CommandText, CommandParams.ToArray());
			return (TResult)obj;
		}

		/// <summary>
		/// 
		/// </summary>
		protected string GetSqlString<TResult>()
		{
			//get table name
			Type mastertype = typeof(TResult);
			if (mastertype != typeof(T))
				mastertype = typeof(T);
			string tableName = MappingHelper.GetMapping(mastertype);

			StringBuilder sqlText = new StringBuilder();
			sqlText.AppendLine($"SELECT {Field} FROM  {tableName} {MasterAliasName}");
			foreach (var item in UnionList)
			{
				string unionAliasName = item.Model == mastertype ? MasterAliasName : item.AliasName;
				string unionTableName = MappingHelper.GetMapping(item.Model);
				sqlText.AppendLine(item.UnionType.ToString().Replace("_", " ") + " " + unionTableName + " " + unionAliasName + " ON " + item.Expression);
			}

			// other
			if (WhereList.Count > 0)
				sqlText.AppendLine("WHERE " + string.Join("\nAND ", WhereList));
			if (!string.IsNullOrEmpty(GroupByText))
				sqlText.AppendLine(GroupByText);
			if (!string.IsNullOrEmpty(GroupByText) && !string.IsNullOrEmpty(HavingText))
				sqlText.AppendLine(HavingText);
			if (!string.IsNullOrEmpty(OrderByText))
				sqlText.AppendLine(OrderByText);
			if (!string.IsNullOrEmpty(LimitText))
				sqlText.AppendLine(LimitText);
			if (!string.IsNullOrEmpty(OffsetText))
				sqlText.AppendLine(OffsetText);
			CommandText = sqlText.ToString();
			return CommandText;
		}


		protected QueryHelper<T> AddParameter(string field, NpgsqlDbType dbType, object value, int size, Type specificType)
		{
			NpgsqlParameter p = new NpgsqlParameter(field, dbType, size);
			if (specificType != null)
				p.SpecificType = specificType;
			p.Value = value;
			CommandParams.Add(p);
			return this;
		}
		private void AddSelectParameter(string field, object value)
		{
			var value_type = value.GetType();
			Type specificType = null;
			var dbType = TypeHelper.GetDbType(value_type);
			if (dbType == NpgsqlDbType.Enum)
				specificType = value_type;
			NpgsqlParameter p = new NpgsqlParameter(field, value);
			if (dbType != null)
				p.NpgsqlDbType = dbType.Value;
			if (specificType != null)
				p.SpecificType = specificType;
			CommandParams.Add(p);
		}
		#endregion
	}
}

using Meta.Common.Model;
using Npgsql;
using NpgsqlTypes;
using System;
using System.Collections.Generic;


namespace Meta.Common.SqlBuilder
{
	public class InsertBuilder : BuilderBase<InsertBuilder>
	{
		/// <summary>
		/// 字段列表
		/// </summary>
		readonly List<string> _valueList = new List<string>();

		/// <summary>
		/// 参数化列表
		/// </summary>
		readonly List<string> _paramList = new List<string>();
		/// <summary>
		/// 是否返回实体类
		/// </summary>
		bool _isReturn = false;
		public InsertBuilder() { }
		public InsertBuilder(string table) : base(table) { }
		/// <summary>
		/// Init table and return fields
		/// </summary>
		/// <param name="table"></param>
		/// <param name="fields"></param>
		public InsertBuilder(string table, string fields) : base(table) => Fields = fields;
		/// <summary>
		/// 设置一个列表
		/// </summary>
		/// <param name="values"></param>
		/// <returns></returns>
		public InsertBuilder Set(Dictionary<string, object> values)
		{
			using (var e = values.GetEnumerator())
				while (e.MoveNext())
					Set(e.Current.Key, e.Current.Value);
			return this;
		}
		/// <summary>
		/// 设置一个字段
		/// </summary>
		/// <param name="field"></param>
		/// <param name="value"></param>
		/// <returns></returns>
		public InsertBuilder Set(string field, object value, int? size = null, NpgsqlDbType? dbType = null)
		{
			_valueList.Add(field);
			_paramList.Add($"@{field}");
			AddParameter(field, value, size, dbType);
			return this;
		}
		/// <summary>
		/// 默认设置方法
		/// </summary>
		/// <param name="field"></param>
		/// <param name="paramStr"></param>
		/// <param name="nps"></param>
		/// <returns></returns>
		public InsertBuilder Set(string field, string paramStr, List<NpgsqlParameter> nps)
		{
			_valueList.Add(field);
			_paramList.Add(paramStr);
			Params.AddRange(nps);
			return this;
		}
		/// <summary>
		/// 返回受影响行数
		/// </summary>
		/// <returns></returns>
		public int Commit() => ToRows();
		/// <summary>
		/// 返回受影响行数_选库
		/// </summary>
		/// <returns></returns>
		public int Commit(string databaseType)
		{
			Data(databaseType);
			return ToRows();
		}
		/// <summary>
		/// 插入数据库并返回数据
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <returns></returns>
		public T Commit<T>()
		{
			_isReturn = true;
			return ToOne<T>();
		}
		/// <summary>
		/// 插入数据库并返回数据_选库
		/// </summary>
		/// <returns></returns>
		public T Commit<T>(string databaseType)
		{
			_isReturn = true;
			Data(databaseType);
			return ToOne<T>();
		}
		#region Override
		public override string ToString() => base.ToString();

		public override string GetCommandTextString()
		{
			if ((_valueList?.Count ?? 0) == 0 || (_paramList?.Count ?? 0) == 0) throw new ArgumentNullException("Insert KeyValuePairs is null.");
			if (_valueList.Count != _paramList.Count) throw new ArgumentNullException("Insert KeyValuePairs length is not equal.");
			var vs = string.Join(", ", _valueList);
			var fs = string.IsNullOrEmpty(Fields) ? vs : Fields.Replace($"{MainAlias}.", "");
			var ret = _isReturn ? $"RETURNING {fs}" : "";
			return $"INSERT INTO {MainTable} ({vs}) VALUES({string.Join(", ", _paramList)}) {ret}";
		}
		#endregion
	}
}

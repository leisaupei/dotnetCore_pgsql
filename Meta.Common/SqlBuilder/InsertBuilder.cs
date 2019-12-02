using Meta.Common.Extensions;
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
		readonly Dictionary<string, string> _insertList = new Dictionary<string, string>();
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
			using var e = values.GetEnumerator();
			while (e.MoveNext())
				Set(e.Current.Key, e.Current.Value);
			return this;
		}
		/// <summary>
		/// 设置一个字段
		/// </summary>
		/// <param name="field"></param>
		/// <param name="value"></param>
		/// <param name="size"></param>
		/// <param name="dbType"></param>
		/// <returns></returns>
		public InsertBuilder Set(string field, object value, int? size = null, NpgsqlDbType? dbType = null)
		{
			_insertList[field] = $"@{field}";
			AddParameter(field, value, size, dbType);
			return this;
		}
		/// <summary>
		/// 设置语句
		/// </summary>
		/// <param name="field">字段名称</param>
		/// <param name="paramStr">可以传入一个带参/无参的sql语句或一个@参数</param>
		/// <param name="nps">参数</param>
		/// <returns></returns>
		public InsertBuilder Set(string field, string paramStr, params NpgsqlParameter[] nps)
		{
			_insertList[field] = paramStr;
			Params.AddRange(nps);
			return this;
		}
		/// <summary>
		/// 返回受影响行数
		/// </summary>
		/// <returns></returns>
		public new int ToRows() => base.ToRows();

		/// <summary>
		/// 插入数据库并返回数据
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <returns></returns>
		public int ToRows<T>(ref T info)
		{
			_isReturn = true;
			info = ToOne<T>();
			return info != null ? 1 : 0;
		}

		#region Override
		public override string ToString() => base.ToString();

		public override string GetCommandTextString()
		{
			if (_insertList.IsNullOrEmpty())
				throw new ArgumentNullException(nameof(_insertList));
			var vs = string.Join(", ", _insertList.Keys);
			var ret = _isReturn ? $"RETURNING {vs}" : "";
			return $"INSERT INTO {MainTable} ({vs}) VALUES({string.Join(", ", _insertList.Values)}) {ret}";
		}
		#endregion
	}
}

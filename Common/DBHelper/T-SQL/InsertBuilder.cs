using Npgsql;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace DBHelper
{
	public class InsertBuilder : BuilderBase<InsertBuilder>, IGetReturn
	{
		List<string> _valueList = new List<string>();
		List<string> _paramList = new List<string>();
		public bool IsReturn { get; set; } = false;
		public InsertBuilder() { }
		public InsertBuilder(string table) : base(table) { }
		/// <summary>
		/// Init table and return fields
		/// </summary>
		/// <param name="table"></param>
		/// <param name="fields"></param>
		public InsertBuilder(string table, string fields) : base(table) => _fields = fields;
		/// <summary>
		/// set a dictionary
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
		/// set a field
		/// </summary>
		/// <param name="field"></param>
		/// <param name="value"></param>
		/// <returns></returns>
		public InsertBuilder Set(string field, object value, int? size = null)
		{
			_valueList.Add(field);
			_paramList.Add($"@{field}");
			AddParameter(field, value, size);
			return this;
		}
		public InsertBuilder Set(string field, string paramStr, List<NpgsqlParameter> nps)
		{
			_valueList.Add(field);
			_paramList.Add(paramStr);
			_params.AddRange(nps);
			return this;
		}
		public int Commit() => ToRows();
		/// <summary>
		/// insert and return model
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <returns></returns>
		public T Commit<T>()
		{
			IsReturn = true;
			return ToOne<T>();
		}
		#region Override
		public override string ToString() => base.ToString();

		protected override string SetCommandString()
		{
			if (_valueList.IsNullOrEmpty() || _paramList.IsNullOrEmpty()) throw new ArgumentNullException("Insert KeyValuePairs is null.");
			if (_valueList.Count != _paramList.Count) throw new ArgumentNullException("Insert KeyValuePairs length is not equal.");
			var vs = _valueList.Join(", ");
			var fs = _fields.IsNullOrEmpty() ? vs : _fields.Replace("a.", "");
			var ret = IsReturn ? $"RETURNING {fs}" : "";
			return $"INSERT INTO {_mainTable} ({vs}) VALUES({_paramList.Join(", ")}) {ret}";
		}
		#endregion
	}
}

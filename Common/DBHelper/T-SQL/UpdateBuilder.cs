using Npgsql;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DBHelper
{
	public abstract class UpdateBuilder<TSQL> : WhereBase<TSQL> where TSQL : class, new()
	{
		protected List<string> _setList = new List<string>();
		public bool _isReturn = false;
		TSQL _this => this as TSQL;
		/// <summary>
		/// Initialize table
		/// </summary>
		/// <param name="table"></param>
		public UpdateBuilder(string table) : base(table) { }
		public UpdateBuilder(string table, string alias) : base(table, alias) { }
		public UpdateBuilder() { }
		/// <summary>
		/// Increment
		/// </summary>
		/// <param name="field">field name</param>
		/// <param name="increment">increment value</param>
		/// <param name="size"></param>
		/// <returns></returns>
		public TSQL SetIncrement(string field, object increment, int? size = null)
		{
			var param_name = ParamsIndex;
			if (increment is TimeSpan) // 时间类型加减
				return Set($"{field} = COALESCE({field} , now()) + @{param_name}", param_name, increment, size);
			return Set($"{field} = COALESCE({field} , 0) + @{param_name}", param_name, increment, size);

		}
		/// <summary>
		/// Add element to the array
		/// </summary>
		/// <param name="field">field name</param>
		/// <param name="value">value or array</param>
		/// <param name="size"></param>
		/// <returns></returns>
		public TSQL SetJoin(string field, object value, int? size = null)
		{
			var param_name = ParamsIndex;
			return Set($"{field} = {field} || @{param_name}", param_name, value, size);
		}
		/// <summary>
		/// geometry 
		/// </summary>
		/// <param name="field">field name</param>
		/// <param name="x">longitude</param>
		/// <param name="y">latitude</param>
		/// <param name="srid">Unique identification of space coordinate system</param>
		/// <returns></returns>
		protected TSQL SetGeometry(string field, float x, float y, int srid)
		{
			var pointName = ParamsIndex;
			var sridName = ParamsIndex;
			AddParameter(pointName, $"POINT({x} {y})", -1);
			AddParameter(sridName, srid, -1);
			_setList.Add($"{field} = ST_GeomFromText(@{pointName}, @{sridName})");
			return _this;
		}
		/// <summary>
		/// Remove value from array
		/// </summary>
		/// <param name="field">field name</param>
		/// <param name="value">value</param>
		/// <param name="size"></param>
		/// <returns></returns>
		public TSQL SetRemove(string field, object value, int? size = null)
		{
			var param_name = ParamsIndex;
			return Set($"{field} = array_remove({field}, @{param_name})", param_name, value, size);
		}
		/// <summary>
		/// set field
		/// </summary>
		/// <param name="exp">expression with @param</param>
		/// <param name="param">parameter name</param>
		/// <param name="value">value</param>
		/// <param name="size"></param>
		/// <returns></returns>
		public TSQL Set(string exp, string param, object value, int? size = null)
		{
			AddParameter(param, value, size);
			_setList.Add(exp);
			return _this;
		}
		/// <summary>
		///  set ffield = value (only once for same update sql)
		/// </summary>
		/// <param name="field">field name</param>
		/// <param name="value">value </param>
		/// <returns></returns>
		public TSQL Set(string field, object value, int? size = null)
		{
			var param_name = ParamsIndex;
			return Set($"{field} = @{param_name}", param_name, value, size);
		}
		/// <summary>
		/// set field = sql 
		/// </summary>
		/// <param name="columnName">field name</param>
		/// <param name="sqlStr">sql</param>
		/// <returns></returns>
		public TSQL Set(string columnName, string sqlStr)
		{
			_setList.Add($"{columnName} = ({sqlStr})");
			return _this;
		}
		/// <summary>
		/// return execute rows
		/// </summary>
		/// <returns></returns>
		public int Commit() => ToRows();
		/// <summary>
		/// return model (suggested return one row)
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <returns></returns>
		public T Commit<T>()
		{
			_isReturn = true;
			return ToOne<T>();
		}
		#region Override
		public override string ToString() => base.ToString();
		protected override string SetCommandString()
		{
			if (_where.Count < 1) throw new ArgumentNullException("where expression is null or empty");
			if (!_fields.IsNullOrEmpty())
			{
				if (_fields.IndexOf($"{_mainAlias}.update_time", StringComparison.Ordinal) > 0
					&& !_setList.Any(a => a.Contains($"{_mainAlias}.update_time")))
					Set("update_time", DateTime.Now);
			}
			else
				_fields = "*";
			var ret = _isReturn ? $"RETURNING {_fields}" : "";
			return $"UPDATE {_mainTable} {_mainAlias} SET {_setList.Join(",")} WHERE {_where.Join("\nAND")} {ret};";
		}
		#endregion
	}
}

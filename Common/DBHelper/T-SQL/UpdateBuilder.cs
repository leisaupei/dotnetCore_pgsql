using Npgsql;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DBHelper
{
	public abstract class UpdateBuilder<TSQL> : BuilderBase<TSQL>, IGetReturn where TSQL : class, new()
	{
		protected List<string> _setList = new List<string>();
		public bool IsReturn { get; set; } = false;
		TSQL _this => this as TSQL;
		/// <summary>
		/// 初始化Table
		/// </summary>
		/// <param name="table"></param>
		public UpdateBuilder(string table) : base(table) { }
		public UpdateBuilder(string table, string alias) : base(table, alias) { }
		public UpdateBuilder() { }
		/// <summary>
		/// 字段自增
		/// </summary>
		/// <param name="field">字段名称</param>
		/// <param name="increment">自增值</param>
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
		/// 添加元素到数组
		/// </summary>
		/// <param name="field">字段名称</param>
		/// <param name="value">值或数组</param>
		/// <param name="size"></param>
		/// <returns></returns>
		public TSQL SetJoin(string field, object value, int? size = null)
		{
			var param_name = ParamsIndex;
			return Set($"{field} = {field} || @{param_name}", param_name, value, size);
		}
		/// <summary>
		/// geometry字段
		/// </summary>
		/// <param name="field">字段名称</param>
		/// <param name="x">经度</param>
		/// <param name="y">纬度</param>
		/// <param name="srid">空间坐标系唯一标识</param>
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
		/// 从数组移除元素
		/// </summary>
		/// <param name="field">字段名称</param>
		/// <param name="value">需要移除的值</param>
		/// <param name="size"></param>
		/// <returns></returns>
		public TSQL SetRemove(string field, object value, int? size = null)
		{
			var param_name = ParamsIndex;
			return Set($"{field} = array_remove({field}, @{param_name})", param_name, value, size);
		}
		/// <summary>
		/// 设置字段
		/// </summary>
		/// <param name="exp">带@param的表达式</param>
		/// <param name="param">param名称</param>
		/// <param name="value">值</param>
		/// <param name="size"></param>
		/// <returns></returns>
		public TSQL Set(string exp, string param, object value, int? size = null)
		{
			AddParameter(param, value, size);
			_setList.Add(exp);
			return _this;
		}
		/// <summary>
		/// 设置字段等于value(同一个update语句不能调用置两次)
		/// </summary>
		/// <param name="field">字段名称</param>
		/// <param name="value">值</param>
		/// <returns></returns>
		public TSQL Set(string field, object value, int? size = null)
		{
			var param_name = ParamsIndex;
			return Set($"{field} = @{param_name}", param_name, value, size);
		}
		/// <summary>
		/// 设置字段等于SQL
		/// </summary>
		/// <param name="columnName">字段名字</param>
		/// <param name="sqlStr">SQL语句</param>
		/// <returns></returns>
		public TSQL Set(string columnName, string sqlStr)
		{
			_setList.Add($"{columnName} = ({sqlStr})");
			return _this;
		}
		/// <summary>
		/// 返回修改行数
		/// </summary>
		/// <returns></returns>
		public int Commit() => ToRows();
		/// <summary>
		/// 返回修改后的对象(建议返回一行)
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
			if (_where.Count < 1) throw new ArgumentException("update语句必须带where条件");
			if (!_fields.IsNullOrEmpty())
			{
				if (_fields.IndexOf($"{_mainAlias}.update_time", StringComparison.Ordinal) > 0
					&& !_setList.Any(a => a.Contains($"{_mainAlias}.update_time")))
					Set("update_time", DateTime.Now);
			}
			else
				_fields = "*";
			var ret = IsReturn ? $"RETURNING {_fields}" : "";
			return $"UPDATE {_mainTable} {_mainAlias} SET {_setList.Join(",")} WHERE {_where.Join("\nAND")} {ret};";
		}
		#endregion
	}
}

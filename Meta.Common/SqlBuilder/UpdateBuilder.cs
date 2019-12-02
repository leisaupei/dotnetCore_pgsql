using Meta.Common.DbHelper;
using Meta.Common.Model;
using Npgsql;
using NpgsqlTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
namespace Meta.Common.SqlBuilder
{
	public abstract class UpdateBuilder<TSQL, TModel> : WhereBase<TSQL> where TSQL : class, new()
	{
		/// <summary>
		/// 设置列表
		/// </summary>
		readonly List<string> _setList = new List<string>();

		static readonly Dictionary<NpgsqlDbType, string> _incrementDic = new Dictionary<NpgsqlDbType, string>
		{
			{ NpgsqlDbType.Date, "now()::date" }, { NpgsqlDbType.Interval, "'00:00:00'" }, { NpgsqlDbType.Time, "'00:00:00'" },
			{ NpgsqlDbType.Timestamp, "now()" }, { NpgsqlDbType.Money, "0::money" }
		};
		/// <summary>
		/// 是否返回实体类
		/// </summary>
		bool _isReturn = false;
		TSQL This => this as TSQL;
		#region Contructor
		public UpdateBuilder(string table) : base(table) { }
		public UpdateBuilder(string table, string alias) : base(table, alias) { }
		public UpdateBuilder()
		{
			MainTable = EntityHelper.GetTableName<TModel>();
			Fields = EntityHelper.GetModelTypeFieldsString<TModel>(MainAlias);
		}
		#endregion

		/// <summary>
		/// 字段自增
		/// </summary>
		/// <param name="field">字段名称</param>
		/// <param name="value">自增值</param>
		/// <param name="size"></param>
		/// <returns></returns>
		public TSQL SetIncrement(string field, object value, int? size = null, NpgsqlDbType? dbType = null)
		{
			var param_name = ParamsIndex;
			var coalesce = dbType.HasValue && _incrementDic.ContainsKey(dbType.Value) ? _incrementDic[dbType.Value] : "0";
			return SetExp(string.Format("{0} = COALESCE({0}, {1}) + @{2}", field, coalesce, param_name), param_name, value, size, value is TimeSpan ? NpgsqlDbType.Interval : dbType);
		}
		/// <summary>
		/// 添加元素到数组
		/// </summary>
		/// <param name="field">字段名称</param>
		/// <param name="value">值或数组</param>
		/// <param name="size"></param>
		/// <returns></returns>
		public TSQL SetJoin(string field, object value, int? size = null, NpgsqlDbType? dbType = null)
		{
			var param_name = ParamsIndex;
			return SetExp(string.Format("{0} = {0} || @{1}", field, param_name), param_name, value, size, dbType);
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
			return This;
		}
		/// <summary>
		/// 从数组移除元素
		/// </summary>
		/// <param name="field">字段名称</param>
		/// <param name="value">需要移除的值</param>
		/// <param name="size"></param>
		/// <returns></returns>
		public TSQL SetRemove(string field, object value, int? size = null, NpgsqlDbType? dbType = null)
		{
			var param_name = ParamsIndex;
			return SetExp(string.Format("{0} = array_remove({0}, @{1})", field, param_name), param_name, value, size, dbType);
		}
		/// <summary>
		/// 设置字段
		/// </summary>
		/// <param name="exp">带@param的表达式</param>
		/// <param name="param">param名称</param>
		/// <param name="value">值</param>
		/// <param name="size"></param>
		/// <returns></returns>
		public TSQL SetExp(string exp, string param, object value, int? size = null, NpgsqlDbType? dbType = null)
		{
			AddParameter(param, value, size, dbType);
			_setList.Add(exp);
			return This;
		}
		/// <summary>
		/// 是否添加set语句
		/// </summary>
		/// <param name="isAdd"></param>
		/// <param name="field"></param>
		/// <param name="value"></param>
		/// <param name="size"></param>
		/// <param name="dbType"></param>
		/// <returns></returns>
		public TSQL Set(bool isAdd, string field, object value, int? size = null, NpgsqlDbType? dbType = null)
		{
			if (!isAdd) return This;
			var param_name = ParamsIndex;
			return SetExp($"{field} = @{param_name}", param_name, value, size, dbType);
		}
		/// <summary>
		/// 设置字段等于value(同一个update语句不能调用置两次)
		/// </summary>
		/// <param name="field">字段名称</param>
		/// <param name="value">值</param>
		/// <returns></returns>
		public TSQL Set(string field, object value, int? size = null, NpgsqlDbType? dbType = null)
		{
			var param_name = ParamsIndex;
			return SetExp($"{field} = @{param_name}", param_name, value, size, dbType);
		}
		/// <summary>
		/// 设置字段等于SQL
		/// </summary>
		/// <param name="field">字段名字</param>
		/// <param name="sqlStr">SQL语句</param>
		/// <returns></returns>
		public TSQL Set(string field, string sqlStr)
		{
			_setList.Add($"{field} = ({sqlStr})");
			return This;
		}
		/// <summary>
		/// 设置字段等于SQL
		/// </summary>
		/// <param name="field">字段名字</param>
		/// <param name="sqlStr">SQL语句</param>
		/// <returns></returns>
		public TSQL Set(string field, TSQL selectBuilder)
		{
			Set(field, selectBuilder.ToString());
			return This;
		}
		/// <summary>
		/// 返回修改行数
		/// </summary>
		/// <returns></returns>
		public new int ToRows() => base.ToRows();
		/// <summary>
		/// 返回修改行数, 并且ref实体类(一行)
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="row"></param>
		/// <returns></returns>
		public int ToRows<T>(ref T refInfo)
		{
			_isReturn = true;
			var info = base.ToOne<T>();
			if (info == null) return 0;
			refInfo = info;
			return 1;
		}
		/// <summary>
		/// 返回修改行数, 并且ref列表(多行)
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="refInfo"></param>
		/// <returns></returns>
		public int ToRows<T>(ref List<T> refInfo)
		{
			_isReturn = true;
			var info = base.ToList<T>();
			if ((info?.Count ?? 0) == 0) return 0;
			refInfo = info;
			return info.Count;
		}
		/// <summary>
		/// 
		/// </summary>
		/// <returns></returns>
		public TSQL ToPipe() => base.ToPipe<int>(PipeReturnType.Rows);
		#region Override
		public override string ToString() => base.ToString();
		public override string GetCommandTextString()
		{
			if (WhereList.Count < 1)
				throw new ArgumentNullException(nameof(WhereList));
			if (!string.IsNullOrEmpty(Fields))
			{
				//if (_fields.IndexOf($"{_mainAlias}.update_time", StringComparison.Ordinal) > 0
				//	&& !_setList.Any(a => a.Contains($"{_mainAlias}.update_time")))
				//	Set("update_time", DateTime.Now);
			}
			else
				Fields = "*";
			var ret = _isReturn ? $"RETURNING {Fields}" : "";
			return $"UPDATE {MainTable} {MainAlias} SET {string.Join(",", _setList)} WHERE {string.Join("\nAND", WhereList)} {ret}";
		}
		#endregion
	}
}

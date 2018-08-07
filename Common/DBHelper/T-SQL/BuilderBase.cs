using Npgsql;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace DBHelper
{
	public abstract class BuilderBase<TSQL> where TSQL : class, new()
	{

		protected string _mainTable { get; set; }
		protected string _mainAlias { get; set; } = "a";
		protected string _fields { get; set; }
		protected List<string> _where { get; } = new List<string>();
		protected string ParamsIndex => "parameter_" + _paramsCount++;

		int _paramsCount = 0;
		protected List<NpgsqlParameter> _params { get; } = new List<NpgsqlParameter>();
		string _cmdStr => SetCommandString();
		TSQL _this => this as TSQL;

		protected BuilderBase(string table, string alias)
		{
			_mainTable = table;
			_mainAlias = alias;
		}
		protected BuilderBase(string table)
		{
			_mainTable = table;
		}
		protected BuilderBase() { }

		public virtual TSQL Table(string table, string alias)
		{
			_mainTable = table;
			_mainAlias = alias;
			return _this;
		}
		public virtual TSQL Table(string table)
		{
			_mainTable = table;
			return _this;
		}
		public virtual TSQL Alias(string alias)
		{
			_mainAlias = alias;
			return _this;
		}
		protected TSQL AddParameter(string field, object value, int? size = null)
		{
			NpgsqlParameter p = new NpgsqlParameter(field, value);
			if (size != null) p.Size = size.Value;
			var value_type = value.GetType();
			var dbType = TypeHelper.GetDbType(value_type);
			if (dbType != null) p.NpgsqlDbType = dbType.Value;
			_params.Add(p);
			return _this;
		}

		/// <summary>
		/// 返回第一个元素
		/// </summary>
		/// <returns></returns>
		public object ToScalar() => PgSqlHelper.ExecuteScalar(CommandType.Text, _cmdStr, _params.ToArray());
		/// <summary>
		/// 返回自定义类型
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <returns></returns>
		public T ToScalar<T>() => (T)PgSqlHelper.ExecuteScalar(CommandType.Text, _cmdStr, _params.ToArray());
		/// <summary>
		/// 返回List<Model>
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <returns></returns>
		public List<T> ToList<T>() => PgSqlHelper.ExecuteDataReaderList<T>(_cmdStr, _params.ToArray());
		public List<T> ToList<T>(Func<T, T> func = null) => PgSqlHelper.ExecuteDataReaderList<T>(_cmdStr, func, _params.ToArray());
		/// <summary>
		/// 返回一个Model
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <returns></returns>
		public T ToOne<T>() => PgSqlHelper.ExecuteDataReaderModel<T>(_cmdStr, _params.ToArray());
		public T ToOne<T>(Func<T, T> func = null) => PgSqlHelper.ExecuteDataReaderModel<T>(_cmdStr, func, _params.ToArray());
		/// <summary>
		/// 返回修改行数
		/// </summary>
		/// <param name="cmdText"></param>
		/// <returns></returns>
		public int ToRows() => PgSqlHelper.ExecuteNonQuery(CommandType.Text, _cmdStr, _params.ToArray());

		public override string ToString() => ToString(null);
		/// <summary>
		/// 调试或输出用
		/// </summary>
		/// <param name="field"></param>
		/// <returns></returns>
		public string ToString(string field)
		{
			if (!string.IsNullOrEmpty(field)) _fields = field;
			return TypeHelper.SqlToString(_cmdStr, _params);
		}
		protected abstract string SetCommandString();
	}

	public interface IGetReturn
	{
		/// <summary>
		/// 修改后是否返回
		/// </summary>
		bool IsReturn { get; set; }
	}
}

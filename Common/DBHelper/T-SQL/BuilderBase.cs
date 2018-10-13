using Npgsql;
using NpgsqlTypes;
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
		/// <summary>
		/// 参数计数器
		/// </summary>
		int _paramsCount = 0;
		/// <summary>
		/// 主表
		/// </summary>
		protected string _mainTable { get; set; }
		/// <summary>
		/// 主表别名, 默认为"a"
		/// </summary>
		protected string _mainAlias { get; set; } = "a";
		/// <summary>
		/// 查询字段
		/// </summary>
		protected string _fields { get; set; }
		/// <summary>
		/// where条件列表
		/// </summary>
		protected List<string> _where { get; } = new List<string>();
		/// <summary>
		/// 参数后缀
		/// </summary>
		protected string ParamsIndex => "parameter_" + _paramsCount++;
		/// <summary>
		/// 表的类型
		/// </summary>
		protected DatabaseType _type { get; set; } = DatabaseType.Master;
		/// <summary>
		/// 是否主库
		/// </summary>
		protected bool _isMaster => _type == DatabaseType.Master;
		/// <summary>
		/// 参数列表
		/// </summary>
		protected List<NpgsqlParameter> _params { get; } = new List<NpgsqlParameter>();
		/// <summary>
		/// 输出sql语句
		/// </summary>
		string _cmdStr => SetCommandString();
		/// <summary>
		/// 类型转换
		/// </summary>
		TSQL _this => this as TSQL;
		/// <summary>
		/// 初始化主表与别名
		/// </summary>
		/// <param name="table"></param>
		/// <param name="alias"></param>
		protected BuilderBase(string table, string alias)
		{
			_mainTable = table;
			_mainAlias = alias;
		}
		/// <summary>
		/// 初始化主表
		/// </summary>
		/// <param name="table"></param>
		protected BuilderBase(string table)
		{
			_mainTable = table;
		}
		/// <summary>
		/// 默认构造函数
		/// </summary>
		protected BuilderBase() { }
		/// <summary>
		/// 
		/// </summary>
		/// <param name="table">主表</param>
		/// <param name="alias">别名</param>
		/// <returns></returns>
		public virtual TSQL Table(string table, string alias)
		{
			_mainTable = table;
			_mainAlias = alias;
			return _this;
		}
		/// <summary>
		/// 
		/// </summary>
		/// <param name="table">主表</param>
		/// <returns></returns>
		public virtual TSQL Table(string table)
		{
			_mainTable = table;
			return _this;
		}
		/// <summary>
		/// 
		/// </summary>
		/// <param name="alias">别名</param>
		/// <returns></returns>
		public virtual TSQL Alias(string alias)
		{
			_mainAlias = alias;
			return _this;
		}
		/// <summary>
		/// 选择表的类型
		/// </summary>
		/// <param name="type"></param>
		/// <returns></returns>
		public TSQL Data(DatabaseType type)
		{
			if (type == DatabaseType.Slave && PgSqlHelper.SlaveExecute != null)
				_type = type;
			return _this;
		}
		/// <summary>
		/// 添加参数
		/// </summary>
		/// <param name="field"></param>
		/// <param name="value"></param>
		/// <param name="size"></param>
		/// <param name="dbType"></param>
		/// <returns></returns>
		protected TSQL AddParameter(string field, DbTypeValue val, int? size = null)
		{
			NpgsqlParameter p = new NpgsqlParameter(field, val.Value);
			if (size != null) p.Size = size.Value;
			if (val.DbType.HasValue) p.NpgsqlDbType = val.DbType.Value;
			_params.Add(p);
			return _this;
		}
		/// <summary>
		/// 添加参数
		/// </summary>
		/// <param name="field"></param>
		/// <param name="value"></param>
		/// <param name="size"></param>
		/// <returns></returns>
		protected TSQL AddParameter(string field, object val, int? size = null, NpgsqlDbType? dbType = null)
		{
			NpgsqlParameter p = new NpgsqlParameter(field, val);
			if (size != null) p.Size = size.Value;
			if (dbType.HasValue) p.NpgsqlDbType = dbType.Value;
			_params.Add(p);
			return _this;
		}

		/// <summary>
		/// 返回第一个元素
		/// </summary>
		/// <returns></returns>
		public object ToScalar() => _isMaster ?
			PgSqlHelper.ExecuteScalar(CommandType.Text, _cmdStr, _params.ToArray()) :
			PgSqlHelper.ExecuteScalarSlave(CommandType.Text, _cmdStr, _params.ToArray());
		/// <summary>
		/// 返回List<Model>
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <returns></returns>
		public List<T> ToList<T>() => _isMaster ?
			PgSqlHelper.ExecuteDataReaderList<T>(_cmdStr, _params.ToArray()) :
			PgSqlHelper.ExecuteDataReaderListSlave<T>(_cmdStr, _params.ToArray());
		public List<T> ToList<T>(Func<T, T> filter = null) => _type == DatabaseType.Master ?
			PgSqlHelper.ExecuteDataReaderList(_cmdStr, filter, _params.ToArray()) :
			PgSqlHelper.ExecuteDataReaderListSlave(_cmdStr, filter, _params.ToArray());
		/// <summary>
		/// 返回一个Model
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <returns></returns>
		public T ToOne<T>() => _isMaster ?
			PgSqlHelper.ExecuteDataReaderModel<T>(_cmdStr, _params.ToArray()) :
			PgSqlHelper.ExecuteDataReaderModelSlave<T>(_cmdStr, _params.ToArray());
		public T ToOne<T>(Func<T, T> filter = null) => _isMaster ?
			PgSqlHelper.ExecuteDataReaderModel(_cmdStr, filter, _params.ToArray()) :
			PgSqlHelper.ExecuteDataReaderModelSlave(_cmdStr, filter, _params.ToArray());
		/// <summary>
		/// 返回修改行数
		/// </summary>
		/// <param name="cmdText"></param>
		/// <returns></returns>
		public int ToRows() => _isMaster ?
			PgSqlHelper.ExecuteNonQuery(CommandType.Text, _cmdStr, _params.ToArray()) :
			PgSqlHelper.ExecuteNonQuerySlave(CommandType.Text, _cmdStr, _params.ToArray());
		/// <summary>
		/// Override ToString()
		/// </summary>
		/// <returns></returns>
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
		/// <summary>
		/// 设置sql语句
		/// </summary>
		/// <returns></returns>
		protected abstract string SetCommandString();
	}
}

using Meta.Common.DbHelper;
using Meta.Common.Extensions;
using Meta.Common.Interface;
using Meta.Common.Model;
using Npgsql;
using NpgsqlTypes;
using System;
using System.Collections.Generic;
using System.Data;

namespace Meta.Common.SqlBuilder
{
	public abstract class BuilderBase<TSQL> : ISqlBuilder where TSQL : class, new()
	{
		#region Identity
		/// <summary>
		/// 参数计数器
		/// </summary>
		int _paramsCount = 0;
		/// <summary>
		/// 主表
		/// </summary>
		protected string MainTable { get; set; }
		/// <summary>
		/// 主表别名, 默认为"a"
		/// </summary>
		protected string MainAlias { get; set; } = "a";
		/// <summary>
		/// 查询字段
		/// </summary>
		protected string Fields { get; set; }
		/// <summary>
		/// where条件列表
		/// </summary>
		protected List<string> WhereList { get; } = new List<string>();
		/// <summary>
		/// 设置默认数据库
		/// </summary>
		protected string DataType { get; set; } = "master";
		/// <summary>
		/// 是否返回默认值
		/// </summary>
		public bool IsReturnDefault { get; set; } = false;
		/// <summary>
		/// 返回实例类型
		/// </summary>
		public Type Type { get; set; }
		/// <summary>
		/// 返回类型
		/// </summary>
		public PipeReturnType ReturnType { get; set; }
		/// <summary>
		/// 参数列表
		/// </summary>
		public List<NpgsqlParameter> Params { get; } = new List<NpgsqlParameter>();
		#endregion

		/// <summary>
		/// 参数后缀
		/// </summary>
		protected string ParamsIndex => "p" + _paramsCount++.ToString().PadLeft(6, '0');

		#region Constructor
		/// <summary>
		/// 初始化主表与别名
		/// </summary>
		/// <param name="table"></param>
		/// <param name="alias"></param>
		protected BuilderBase(string table, string alias) : this(table)
		{
			MainAlias = alias;
		}
		/// <summary>
		/// 初始化主表
		/// </summary>
		/// <param name="table"></param>
		protected BuilderBase(string table) : this()
		{
			MainTable = table;
		}
		/// <summary>
		/// 默认构造函数
		/// </summary>
		protected BuilderBase()
		{
		}
		#endregion

		/// <summary>
		/// 
		/// </summary>
		/// <param name="table">主表</param>
		/// <param name="alias">别名</param>
		/// <returns></returns>
		public virtual TSQL Table(string table, string alias)
		{
			MainTable = table;
			MainAlias = alias;
			return This;
		}
		/// <summary>
		/// 
		/// </summary>
		/// <param name="table">主表</param>
		/// <returns></returns>
		public virtual TSQL Table(string table)
		{
			MainTable = table;
			return This;
		}
		/// <summary>
		/// 
		/// </summary>
		/// <param name="alias">别名</param>
		/// <returns></returns>
		public virtual TSQL Alias(string alias)
		{
			MainAlias = alias;
			return This;
		}
		/// <summary>
		/// 选择表的类型
		/// </summary>
		/// <param name="type"></param>
		/// <returns></returns>
		public TSQL Data(string type)
		{
			DataType = type;
			return This;
		}
		public TSQL BySlave() => Data("slave");
		public TSQL ByMaster() => Data("master");
		/// <summary>
		/// 添加参数
		/// </summary>
		/// <param name="field"></param>
		/// <param name="val"></param>
		/// <param name="size"></param>
		/// <returns></returns>
		public TSQL AddParameter(string field, DbTypeValue val, int? size = null) => AddParameter(field, val.Value, size, val.DbType);
		/// <summary>
		/// 添加参数
		/// </summary>
		/// <param name="field"></param>
		/// <param name="val"></param>
		/// <param name="size"></param>
		/// <param name="dbType"></param>
		public TSQL AddParameter(string field, object val, int? size = null, NpgsqlDbType? dbType = null)
		{
			NpgsqlParameter p = new NpgsqlParameter(field, val);
			if (size.HasValue) p.Size = size.Value;
			if (dbType.HasValue) p.NpgsqlDbType = dbType.Value;
			Params.Add(p);
			return This;
		}
		/// <summary>
		/// 添加参数
		/// </summary>
		/// <param name="p"></param>
		/// <returns></returns>
		public TSQL AddParameter(NpgsqlParameter p)
		{
			Params.Add(p);
			return This;
		}
		/// <summary>
		/// 添加参数
		/// </summary>
		/// <param name="ps"></param>
		/// <returns></returns>
		public TSQL AddParameter(IEnumerable<NpgsqlParameter> ps)
		{
			Params.AddRange(ps);
			return This;
		}
		/// <summary>
		/// 返回第一个元素
		/// </summary>
		/// <returns></returns>
		protected object ToScalar() => PgSqlHelper.ExecuteScalar(CommandType.Text, CmdStr, Params.ToArray(), DataType);
		/// <summary>
		/// 返回List<Model>
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <returns></returns>
		protected List<T> ToList<T>() => PgSqlHelper.ExecuteDataReaderList<T>(CmdStr, Params.ToArray(), DataType);

		/// <summary>
		/// 返回一个Model
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <returns></returns>
		protected T ToOne<T>() => PgSqlHelper.ExecuteDataReaderModel<T>(CmdStr, Params.ToArray(), DataType);
		/// <summary>
		/// 输出管道元素
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <returns></returns>
		protected TSQL ToPipe<T>(PipeReturnType returnType)
		{
			Type = typeof(T);
			ReturnType = returnType;
			return This;
		}

		/// <summary>
		/// 返回修改行数
		/// </summary>
		/// <param name="cmdText"></param>
		/// <returns></returns>
		protected int ToRows() => PgSqlHelper.ExecuteNonQuery(CommandType.Text, CmdStr, Params.ToArray(), DataType);
		/// <summary>
		/// Override ToString()
		/// </summary>
		/// <returns></returns>
		public override string ToString() => ToString(null);

		/// <summary>
		/// 输出sql语句
		/// </summary>
		string CmdStr => GetCommandTextString();
		/// <summary>
		/// 类型转换
		/// </summary>
		TSQL This => this as TSQL;

		/// <summary>
		/// 调试或输出用
		/// </summary>
		/// <param name="field"></param>
		/// <returns></returns>
		public string ToString(string field)
		{
			if (!string.IsNullOrEmpty(field)) Fields = field;
			return TypeHelper.SqlToString(CmdStr, Params);
		}
		/// <summary>
		/// 设置sql语句
		/// </summary>
		/// <returns></returns>
		public abstract string GetCommandTextString();

		#region Implicit
		public static implicit operator string(BuilderBase<TSQL> builder) => builder.ToString();
		#endregion
	}
}

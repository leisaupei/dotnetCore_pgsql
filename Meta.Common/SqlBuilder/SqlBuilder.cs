using Meta.Common.DbHelper;
using Meta.Common.Extensions;
using Meta.Common.Interface;
using Meta.Common.Model;
using Meta.Common.SqlBuilder.AnalysisExpression;
using Npgsql;
using NpgsqlTypes;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;

namespace Meta.Common.SqlBuilder
{
	public abstract class SqlBuilder<TSQL> : ISqlBuilder where TSQL : class
	{
		#region Identity
		/// <summary>
		/// 主表
		/// </summary>
		protected string MainTable { get; set; }
		/// <summary>
		/// 主表别名, 默认为"a"
		/// </summary>
		protected string MainAlias { get; set; } = "a";
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
		/// 参数列表
		/// </summary>
		public List<DbParameter> Params { get; } = new List<DbParameter>();
		/// <summary>
		/// 返回类型
		/// </summary>
		public PipeReturnType ReturnType { get; set; }
		/// <summary>
		/// 查询字段
		/// </summary>
		public string Fields { get; set; }
		#endregion

		#region Constructor
		/// <summary>
		/// 初始化主表与别名
		/// </summary>
		/// <param name="table"></param>
		/// <param name="alias"></param>
		protected SqlBuilder(string table, string alias) : this(table)
		{
			MainAlias = alias;
		}
		/// <summary>
		/// 初始化主表
		/// </summary>
		/// <param name="table"></param>
		protected SqlBuilder(string table) : this()
		{
			MainTable = table;
		}
		/// <summary>
		/// 默认构造函数
		/// </summary>
		protected SqlBuilder()
		{
		}
		#endregion

		/// <summary>
		/// 选择数据库
		/// </summary>
		/// <param name="type"></param>
		/// <returns></returns>
		public TSQL Data(string type)
		{
			DataType = type;
			return This;
		}
		/// <summary>
		/// 使用别名为master-slave数据库
		/// </summary>
		/// <returns></returns>
		public TSQL BySlave() => Data("master-slave");

		/// <summary>
		/// 使用别名为master的数据库
		/// </summary>
		/// <returns></returns>
		public TSQL ByMaster() => Data("master");

		/// <summary>
		/// 添加参数
		/// </summary>
		/// <param name="parameterName"></param>
		/// <param name="value"></param>
		/// <returns></returns>
		public TSQL AddParameter(string parameterName, object value)
			=> AddParameter(new NpgsqlParameter(parameterName, value));

		/// <summary>
		/// 添加参数
		/// </summary>
		/// <param name="value"></param>
		/// <param name="parameterName"></param>
		/// <returns></returns>
		public TSQL AddParameter(object value, out string parameterName)
		{
			parameterName = EntityHelper.ParamsIndex;
			return AddParameter(parameterName, value);
		}

		/// <summary>
		/// 添加参数
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="parameterName"></param>
		/// <param name="value"></param>
		/// <returns></returns>
		public TSQL AddParameterT<T>(string parameterName, T value)
			=> AddParameter(new NpgsqlParameter<T>(parameterName, value));

		/// <summary>
		/// 添加参数
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="value"></param>
		/// <param name="parameterName"></param>
		/// <returns></returns>
		public TSQL AddParameterT<T>(T value, out string parameterName)
		{
			parameterName = EntityHelper.ParamsIndex;
			return AddParameterT(parameterName, value);
		}

		/// <summary>
		/// 添加参数
		/// </summary>
		/// <param name="ps"></param>
		/// <returns></returns>
		public TSQL AddParameter(DbParameter ps)
		{
			Params.Add(ps);
			return This;
		}

		/// <summary>
		/// 添加参数
		/// </summary>
		/// <param name="ps"></param>
		/// <returns></returns>
		public TSQL AddParameters(IEnumerable<NpgsqlParameter> ps)
		{
			Params.AddRange(ps);
			return This;
		}

		/// <summary>
		/// 添加参数
		/// </summary>
		/// <param name="ps"></param>
		/// <returns></returns>
		public TSQL AddParameters(IEnumerable<DbParameter> ps)
		{
			Params.AddRange(ps);
			return This;
		}

		/// <summary>
		/// 返回第一个元素
		/// </summary>
		/// <returns></returns>
		protected object ToScalar() => PgsqlHelper.ExecuteScalar(CommandText, CommandType.Text, Params.ToArray(), DataType);

		/// <summary>
		/// 返回list 
		/// </summary>
		/// <typeparam name="T">model type</typeparam>
		/// <returns></returns>
		protected List<T> ToList<T>() => PgsqlHelper.ExecuteDataReaderList<T>(CommandText, CommandType.Text, Params.ToArray(), DataType);

		/// <summary>
		/// 返回一个Model
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <returns></returns>
		protected T ToOne<T>() => PgsqlHelper.ExecuteDataReaderModel<T>(CommandText, CommandType.Text, Params.ToArray(), DataType);

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
		/// 返回行数
		/// </summary>
		/// <returns></returns>
		protected int ToRows() => PgsqlHelper.ExecuteNonQuery(CommandText, CommandType.Text, Params.ToArray(), DataType);

		/// <summary>
		/// Override ToString()
		/// </summary>
		/// <returns></returns>
		public override string ToString() => ToString(null);

		/// <summary>
		/// 输出sql语句
		/// </summary>
		public string CommandText => GetCommandTextString();

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
			return TypeHelper.SqlToString(CommandText, Params);
		}

		/// <summary>
		/// 设置sql语句
		/// </summary>
		/// <returns></returns>
		public abstract string GetCommandTextString();


		#region Implicit
		public static implicit operator string(SqlBuilder<TSQL> builder) => builder.ToString();
		#endregion
	}
}

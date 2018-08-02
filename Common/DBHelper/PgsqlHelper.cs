using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.Logging;
using Npgsql;
using System.Data;
using System.Reflection;
using System.ComponentModel;
using System.Linq;
using Newtonsoft.Json.Linq;
using Microsoft.AspNetCore.Server.Kestrel.Internal.System;

namespace DBHelper
{
	public partial class PgSqlHelper
	{
		public static int DB_COUNT = 1;
		public partial class _execute : PgExecute
		{
			public _execute(int poolSize, string connectionString, ILogger logger) : base(poolSize, connectionString, logger)
			{
			}
		}
		static _execute _Execute;
		static PgExecute Execute => _Execute;
		static ILogger _logger;
		public static void InitDBConnection(int poolSize, string connectionString, ILogger logger)
		{
			if (connectionString.IsNullOrEmpty())
				throw new ArgumentNullException("Connection String is null");
			//mark: 日志 
			_logger = logger;
			_Execute = new _execute(poolSize, connectionString, logger);

		}

		public static object ExecuteScalar(CommandType commandType, string commandText, params NpgsqlParameter[] commandParameters) =>
			Execute.ExecuteScalar(commandType, commandText, commandParameters);
		public static int ExecuteNonQuery(CommandType commandType, string commandText, params NpgsqlParameter[] commandParameters) =>
			Execute.ExecuteNonQuery(commandType, commandText, commandParameters);
		public static void ExecuteDataReader(Action<NpgsqlDataReader> action, string commandText, params NpgsqlParameter[] commandParameters) =>
			Execute.ExecuteDataReader(action, CommandType.Text, commandText, commandParameters);
		public static int ExecuteNonQuery(string commandText) =>
			Execute.ExecuteNonQuery(CommandType.Text, commandText, null);
		/// <summary>
		/// 返回列表
		/// </summary>
		/// <typeparam name="T"></typeparam>
		public static List<T> ExecuteDataReaderList<T>(string commandText, params NpgsqlParameter[] commandParameters)
		{
			var list = new List<T>();
			Execute.ExecuteDataReader(dr =>
			{
				list.Add(ReaderToModel<T>(dr));
			}, CommandType.Text, commandText, commandParameters);
			return list;
		}
		/// <summary>
		/// 返回表的第一行
		/// </summary>
		public static T ExecuteDataReaderModel<T>(string commandText, params NpgsqlParameter[] commandParameters)
		{
			var list = ExecuteDataReaderList<T>(commandText, commandParameters);
			return list.Count > 0 ? list[0] : default(T);
		}
		public static List<T> ExecuteDataReaderList<T>(string commandText, Func<T, T> func, params NpgsqlParameter[] commandParameters)
		{
			var list = new List<T>();
			Execute.ExecuteDataReader(dr =>
			{
				var model = ReaderToModel<T>(dr);
				if (func != null)
				{
					model = func(model);
					if (model != null) list.Add(model);
				}
				else list.Add(model);
			}, CommandType.Text, commandText, commandParameters);
			return list;
		}
		public static T ExecuteDataReaderModel<T>(string commandText, Func<T, T> func, params NpgsqlParameter[] commandParameters)
		{
			var list = ExecuteDataReaderList<T>(commandText, func, commandParameters);
			return list.Count > 0 ? list[0] : default(T);
		}
		#region ToList
		private static TResult ReaderToModel<TResult>(IDataReader objReader)
		{
			//获取传入的数据类型
			Type modelType = typeof(TResult);
			bool isTuple = (modelType.Namespace == "System" && modelType.Name.StartsWith("ValueTuple`", StringComparison.Ordinal)); //判断是否元组类型
			bool isValue = (modelType.Namespace == "System" && modelType.Name.StartsWith("String", StringComparison.Ordinal) || typeof(TResult).BaseType == typeof(ValueType));//判断是否值类型或者string类型

			//默认值
			TResult model = default(TResult);
			//如果非值类型创建实例
			if (!isValue) model = Activator.CreateInstance<TResult>();
			FieldInfo[] fs = modelType.GetFields();
			Type[] type = new Type[fs.Length];
			object[] parms = new object[fs.Length];
			for (int i = 0; i < objReader.FieldCount; i++)
			{
				if (isTuple)
				{
					type[i] = fs[i].FieldType;
					parms[i] = objReader[i];
				}
				else if (!isValue)
				{
					//判断字段值是否为空或不存在的值
					if (!objReader[i].IsNullOrDBNull())
					{
						//匹配字段名
						PropertyInfo pi = modelType.GetProperty(objReader.GetName(i), BindingFlags.Default | BindingFlags.GetProperty | BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
						if (pi != null)
							//绑定实体对象中同名的字段  
							pi.SetValue(model, CheckType(objReader[i], pi.PropertyType), null);
					}
				}
			}
			if (isValue)
			{
				var value = objReader[objReader.GetName(0)];
				if (!value.IsNullOrDBNull()) model = (TResult)CheckType(value, typeof(TResult));
			}
			else if (isTuple)
			{
				ConstructorInfo constructor = modelType.GetConstructor(type);
				model = (TResult)constructor.Invoke(parms);
			}
			return model;
		}

		/// <summary>
		/// 对可空类型进行判断转换(*要不然会报错)
		/// </summary>
		/// <param name="value">DataReader字段的值</param>
		/// <param name="conversionType">该字段的类型</param>
		/// <returns></returns>
		private static object CheckType(object value, Type conversionType)
		{
			if (conversionType.IsGenericType && conversionType.GetGenericTypeDefinition().Equals(typeof(Nullable<>)))
			{
				if (value == null)
					return null;
				NullableConverter nullableConverter = new NullableConverter(conversionType);
				conversionType = nullableConverter.UnderlyingType;
			}
			if (conversionType.Namespace == "Newtonsoft.Json.Linq")
				return JToken.Parse(value.ToEmptyOrString());
			return Convert.ChangeType(value, conversionType);
		}
		#endregion

		/// <summary>
		/// 事务
		/// </summary>
		public static void Transaction(Action action)
		{
			try
			{
				Execute.BeginTransaction();
				action?.Invoke();
				Execute.CommitTransaction();
			}
			finally
			{
				Execute.Close(null, Execute._transaction.Connection);
			}
		}
	}
}

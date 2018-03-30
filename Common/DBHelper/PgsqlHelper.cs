using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.Logging;
using Npgsql;
using System.Data;
using System.Reflection;
using System.ComponentModel;
using System.Linq;

namespace DBHelper
{
	public partial class PgSqlHelper
	{
		public partial class _execute : PgExecute
		{
			public _execute() { }
		}
		private static PgExecute Execute => new _execute();
		private static ILogger _logger;
		public static void InitDBConnection(ILogger logger, string connectionString)
		{
			if (string.IsNullOrEmpty(connectionString))
				throw new ArgumentNullException("connectionString is null");
			//mark: 日志 
			_logger = logger;
			PgExecute._logger = _logger;
			DBConnection.ConnectionString = connectionString;
		}
		public static object ExecuteScalar(CommandType commandType, string commandText, params NpgsqlParameter[] commandParameters) =>
			Execute.ExecuteScalar(commandType, commandText, commandParameters);
		public static int ExecuteNonQuery(CommandType commandType, string commandText, params NpgsqlParameter[] commandParameters) =>
			Execute.ExecuteNonQuery(commandType, commandText, commandParameters);
		public static void ExecuteDataReader(Action<NpgsqlDataReader> action, string commandText, params NpgsqlParameter[] commandParameters) =>
			Execute.ExecuteDataReader(action, CommandType.Text, commandText, commandParameters);
		public static int ExecuteNonQuery(string commandText) =>
			Execute.ExecuteNonQuery(CommandType.Text, commandText, null);
		public static List<T> ExecuteDataReaderList<T>(string commandText, params NpgsqlParameter[] commandParameters)
		{
			var list = new List<T>();
			Execute.ExecuteDataReader(dr =>
			{
				list.Add(ReaderToModel<T>(dr));
			}, CommandType.Text, commandText, commandParameters);
			return list;
		}
		public static T ExecuteDataReaderModel<T>(string commandText, params NpgsqlParameter[] commandParameters)
		{
			var list = ExecuteDataReaderList<T>(commandText, commandParameters);
			return list.Count > 0 ? list[0] : default(T);
		}
		/// <summary>
		/// 返回一列
		/// </summary>
		/// <typeparam name="T">数据类型</typeparam>
		/// <returns></returns>
		public static List<T> ExecuteDataReaderSingle<T>(string commandText, params NpgsqlParameter[] commandParameters)
		{
			var list = new List<T>();
			Execute.ExecuteDataReader(dr =>
			{
				list.Add(ReaderToSingle<T>(dr));
			}, CommandType.Text, commandText, commandParameters);
			return list;
		}
		#region ToList
		//Tresult 必须是能实例化的类型  不支持基本数据类型  例如List<string> 用ToListSingle<TResult>()
		//public static List<TResult> ReaderToList<TResult>(IDataReader objReader)
		//{
		//	using (objReader)
		//	{
		//		List<TResult> list = new List<TResult>();

		//		//获取传入的数据类型
		//		Type modelType = typeof(TResult);
		//		bool isTuple = modelType.Namespace == "System" && modelType.Name.StartsWith("ValueTuple`", StringComparison.Ordinal);
		//		//遍历DataReader对象
		//		while (objReader.Read())
		//		{
		//			//if(modelType.Namespace == "System" && modelType.Name == "String)
		//			//使用与指定参数匹配最高的构造函数，来创建指定类型的实例
		//			TResult model = Activator.CreateInstance<TResult>();
		//			FieldInfo[] fs = modelType.GetFields();
		//			Type[] type = new Type[fs.Length];
		//			object[] parms = new object[fs.Length];
		//			for (int i = 0; i < objReader.FieldCount; i++)
		//			{
		//				if (isTuple)
		//				{
		//					type[i] = fs[i].FieldType;
		//					parms[i] = objReader[i];
		//				}
		//				else
		//				{
		//					//判断字段值是否为空或不存在的值
		//					if (!objReader[i].IsNullOrDBNull())
		//					{
		//						//匹配字段名
		//						PropertyInfo pi = modelType.GetProperty(objReader.GetName(i), BindingFlags.Default | BindingFlags.GetProperty | BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
		//						if (pi != null)
		//							//绑定实体对象中同名的字段  
		//							pi.SetValue(model, CheckType(objReader[i], pi.PropertyType), null);
		//					}
		//				}
		//			}
		//			if (isTuple)
		//			{
		//				ConstructorInfo constructor = modelType.GetConstructor(type.ToArray());
		//				model = (TResult)constructor.Invoke(parms);
		//			}
		//			list.Add(model);
		//		}
		//		if (objReader != null)
		//		{
		//			objReader.Close();
		//			objReader.Dispose();
		//		}
		//		return list;
		//	}
		//}

		public static TResult ReaderToModel<TResult>(IDataReader objReader)
		{
			//获取传入的数据类型
			Type modelType = typeof(TResult);
			bool isTuple = modelType.Namespace == "System" && modelType.Name.StartsWith("ValueTuple`", StringComparison.Ordinal);
			//遍历DataReader对象
			//if(modelType.Namespace == "System" && modelType.Name == "String)
			//使用与指定参数匹配最高的构造函数，来创建指定类型的实例
			TResult model = Activator.CreateInstance<TResult>();
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
				else
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
			if (isTuple)
			{
				ConstructorInfo constructor = modelType.GetConstructor(type.ToArray());
				model = (TResult)constructor.Invoke(parms);
			}
			return model;
		}
		//重写支持一列  支持List<string>  仅CodeFactory使用 接口可用ToTupleList()
		public static TResult ReaderToSingle<TResult>(IDataReader objReader)
		{
			TResult model = default(TResult);
			//判断字段值是否为空或不存在的值
			if (!objReader[objReader.GetName(0)].IsNullOrDBNull())
				model = (TResult)CheckType(objReader[objReader.GetName(0)], typeof(TResult));
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
				Execute.Close(null, Execute._connection);
			}
		}
	}
}

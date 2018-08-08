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
		///  Execute data reader and return list.
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
		/// Execute data reader and return the first row to Model.
		/// </summary>
		public static T ExecuteDataReaderModel<T>(string commandText, params NpgsqlParameter[] commandParameters)
		{
			var list = ExecuteDataReaderList<T>(commandText, commandParameters);
			return list.Count > 0 ? list[0] : default(T);
		}
		/// <summary>
		/// Execute data reader and return list with filter.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="commandText"></param>
		/// <param name="func"></param>
		/// <param name="commandParameters"></param>
		/// <returns></returns>
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
		/// <summary>
		/// Execute data reader and return first row to Model with filter.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="commandText"></param>
		/// <param name="func"></param>
		/// <param name="commandParameters"></param>
		/// <returns></returns>
		public static T ExecuteDataReaderModel<T>(string commandText, Func<T, T> func, params NpgsqlParameter[] commandParameters)
		{
			var list = ExecuteDataReaderList<T>(commandText, func, commandParameters);
			return list.Count > 0 ? list[0] : default(T);
		}
		#region ToList
		private static TResult ReaderToModel<TResult>(IDataReader objReader)
		{
			//Get type of TResult
			Type modelType = typeof(TResult);
			//is tuple type?
			bool isTuple = (modelType.Namespace == "System" && modelType.Name.StartsWith("ValueTuple`", StringComparison.Ordinal));
			//is value type or string?
			bool isValue = (modelType.Namespace == "System" && modelType.Name.StartsWith("String", StringComparison.Ordinal) || typeof(TResult).BaseType == typeof(ValueType));

			//default of TRsult
			TResult model = default(TResult);
			//create TResult model if it isn't value type.
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
					//if it is not null and not DBNull
					if (!objReader[i].IsNullOrDBNull())
					{
						//matching field name.
						PropertyInfo pi = modelType.GetProperty(objReader.GetName(i), BindingFlags.Default | BindingFlags.GetProperty | BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
						if (pi != null)
							//set value to the field of the model
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
		/// Convert Nullable type
		/// </summary>
		/// <param name="value">value of DataReader</param>
		/// <param name="conversionType">this field's type</param>
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
		/// Transaction
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

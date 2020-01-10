using Meta.Common.DbHelper;
using Meta.Common.Interface;
using Meta.Common.Model;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Npgsql.LegacyPostgis;
using NpgsqlTypes;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Text;


namespace Meta.Common.Extensions
{
	internal static class Extensions
	{
		/// <summary>
		/// 判断数组为空
		/// </summary>
		public static bool IsNullOrEmpty<T>(this IEnumerable<T> value) => value == null || value.Count() == 0;
		public static bool ExistsJsonPropertyAttribute(this PropertyInfo info)
		{
			return info.GetCustomAttribute<JsonPropertyAttribute>() != null;
		}

		/// <summary>
		///  将首字母转小写
		/// </summary>
		/// <param name="s"></param>
		/// <returns></returns>
		public static string ToLowerPascal(this string s) => string.IsNullOrEmpty(s) ? s : $"{s.Substring(0, 1).ToLower()}{s.Substring(1)}";
		#region IDataReader.To
		/// <summary>
		/// 返回实体模型
		/// </summary>
		/// <param name="objReader"></param>
		/// <param name="modelType"></param>
		/// <returns></returns>
		public static object ReaderToModel(this IDataReader objReader, Type modelType)
		{
			bool isValueOrString = modelType == typeof(string) || modelType.IsValueType;
			bool isEnum = modelType.IsEnum;
			object model;
			if (modelType.IsTuple())
			{
				int columnIndex = -1;
				model = GetValueTuple(modelType, objReader, ref columnIndex);
			}
			else if (isValueOrString || isEnum)
			{
				model = CheckType(objReader[0], modelType);
			}
			else
			{
				model = Activator.CreateInstance(modelType);

				bool isDictionary = modelType.Namespace == "System.Collections.Generic" && modelType.Name.StartsWith("Dictionary`2", StringComparison.Ordinal);//判断是否字典类型

				for (int i = 0; i < objReader.FieldCount; i++)
				{
					if (isDictionary)
						model.GetType().GetMethod("Add").Invoke(model, new[] { objReader.GetName(i), objReader[i].IsNullOrDBNull() ? null : objReader[i] });
					else
					{
						if (!objReader[i].IsNullOrDBNull())
							SetPropertyValue(modelType, objReader[i], model, objReader.GetName(i));
					}
				}
			}
			return model;
		}
		/// <summary>
		/// 泛型重写
		/// </summary>
		/// <typeparam name="TResult"></typeparam>
		/// <param name="objReader"></param>
		/// <returns></returns>
		public static TResult ReaderToModel<TResult>(this IDataReader objReader)
		{
			return (TResult)objReader.ReaderToModel(typeof(TResult));
		}
		/// <summary>
		/// 类型是否元组
		/// </summary>
		/// <param name="tupleType"></param>
		/// <returns></returns>
		public static bool IsTuple(this Type tupleType) => tupleType.Namespace == "System" && tupleType.Name.StartsWith("ValueTuple`", StringComparison.Ordinal);
		/// <summary>
		/// json的类型需要转化
		/// </summary>
		static readonly Type[] _jTypes = new[] { typeof(JToken), typeof(JObject), typeof(JArray) };
		/// <summary>
		/// 遍历元组类型
		/// </summary>
		/// <param name="objType"></param>
		/// <param name="dr"></param>
		/// <param name="columnIndex"></param>
		/// <returns></returns>
		static object GetValueTuple(Type objType, IDataReader dr, ref int columnIndex)
		{
			if (objType.IsTuple())
			{
				FieldInfo[] fs = objType.GetFields();
				Type[] types = new Type[fs.Length];
				object[] parameters = new object[fs.Length];
				for (int i = 0; i < fs.Length; i++)
				{
					types[i] = fs[i].FieldType;
					parameters[i] = GetValueTuple(types[i], dr, ref columnIndex);
				}
				ConstructorInfo info = objType.GetConstructor(types);
				return info.Invoke(parameters);
			}
			// 当元组里面含有实体类
			if (objType.IsClass && !objType.IsSealed && !objType.IsAbstract)
			{
				if (!objType.GetInterfaces().Any(f => f == typeof(IDbModel)))
					throw new NotSupportedException("only the generate models.");

				var model = Activator.CreateInstance(objType);
				var isSet = false; // 这个实体类是否有赋值 没赋值直接返回 default

				var fs = EntityHelper.GetFieldsFromStaticType(objType);
				for (int i = 0; i < fs.Length; i++)
				{
					++columnIndex;
					if (!dr[columnIndex].IsNullOrDBNull())
					{
						isSet = true;
						SetPropertyValue(objType, dr[columnIndex], model, fs[i]);
					}
				}
				return isSet ? model : default;
			}
			else
			{
				++columnIndex;
				return CheckType(dr[columnIndex], objType);
			}
		}

		private static void SetPropertyValue(Type objType, object value, object model, string fs)
		{
			var p = objType.GetProperty(fs, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
			if (p != null) p.SetValue(model, CheckType(value, p.PropertyType));
		}

		static bool IsNullOrDBNull(this object obj) => obj is DBNull || obj == null;
		/// <summary>
		/// 对可空类型转化
		/// </summary>
		/// <param name="value"></param>
		/// <param name="valueType"></param>
		/// <returns></returns>
		static object CheckType(object value, Type valueType)
		{
			if (value.IsNullOrDBNull()) return null;
			if (valueType.IsGenericType && valueType.GetGenericTypeDefinition().Equals(typeof(Nullable<>)))
				valueType = new NullableConverter(valueType).UnderlyingType;

			try
			{
				return valueType switch
				{
					// 如果c#是enum 数据库是整型
					var t when t.IsEnum && value.GetType().GetInterface(nameof(IFormattable)) != null => Enum.ToObject(t, value),
					// jsonb json 类型
					var t when _jTypes.Contains(t) => JToken.Parse(value?.ToString() ?? "{}"),

					var t when t == typeof(NpgsqlTsQuery) => NpgsqlTsQuery.Parse(value.ToString()),

					var t when t == typeof(BitArray) && value is bool b => new BitArray(1, b),

					_ => Convert.ChangeType(value, valueType),
				};
			}
			catch (Exception ex)
			{
				throw ex;
			}

		}
		#endregion
	}
}

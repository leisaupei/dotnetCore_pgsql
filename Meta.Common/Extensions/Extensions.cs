using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
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
			if (IsTuple(modelType))
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
						{
							PropertyInfo pi = modelType.GetProperty(objReader.GetName(i), BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
							if (pi != null) pi.SetValue(model, CheckType(objReader[i], pi.PropertyType));
						}
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
		static bool IsTuple(Type tupleType) => tupleType.Namespace == "System" && tupleType.Name.StartsWith("ValueTuple`", StringComparison.Ordinal);//判断是否元组类型
		static readonly Type[] _jTypes = new[] { typeof(JToken), typeof(JObject), typeof(JArray) };
		//遍历元组类型 为兼容8个以上元组
		static object GetValueTuple(Type objType, IDataReader dr, ref int columnIndex)
		{
			if (IsTuple(objType))
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

			if (objType.IsClass && !objType.IsSealed && !objType.IsAbstract)
			{
				var model = Activator.CreateInstance(objType);
				var isSet = false;
				var fs = objType.GetProperties().Where(f => f.ExistsJsonPropertyAttribute()).ToArray();
				for (int i = 0; i < fs.Length; i++)
				{
					++columnIndex;
					if (!dr[columnIndex].IsNullOrDBNull())
					{
						isSet = true;
						fs[i].SetValue(model, CheckType(dr[columnIndex], fs[i].PropertyType));
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
		static bool IsNullOrDBNull(this object obj) => obj is DBNull || obj == null;
		// 对可空类型进行判断转换(*要不然会报错)
		static object CheckType(object value, Type valueType)
		{
			if (value.IsNullOrDBNull()) return null;
			if (valueType.IsGenericType && valueType.GetGenericTypeDefinition().Equals(typeof(Nullable<>)))
				valueType = new NullableConverter(valueType).UnderlyingType;
			if (_jTypes.Contains(valueType))
				return JToken.Parse(value?.ToString() ?? "{}");
			return Convert.ChangeType(value, valueType);

		}
		#endregion
	}
}

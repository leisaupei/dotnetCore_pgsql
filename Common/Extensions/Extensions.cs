using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Reflection;
using System.Text;

namespace Common.Extension
{
	static class Extensions
	{
		#region IDataReader.To
		public static object ReaderToModel(this IDataReader objReader, Type modelType)
		{
			object model = null;
			bool isValue() => modelType.Namespace == "System" && modelType.Name.StartsWith("String", StringComparison.Ordinal) || modelType.BaseType == typeof(ValueType);//判断是否值类型或者string类型
			bool isDictionary() => modelType.Namespace == "System.Collections.Generic" && modelType.Name.StartsWith("Dictionary`2", StringComparison.Ordinal);//判断是否字典类型]
			bool isEnum() => modelType.IsEnum;
			if (IsTuple(modelType))
			{
				int columnIndex = -1;
				model = GetValueTuple(modelType, objReader, ref columnIndex);
			}
			else if (isValue() || isEnum())
			{
				var value = objReader[objReader.GetName(0)];
				if (!IsNullOrDBNull(value)) model = CheckType(value, modelType);
			}
			else
			{
				model = Activator.CreateInstance(modelType);
				for (int i = 0; i < objReader.FieldCount; i++)
					if (!IsNullOrDBNull(objReader[i]))
						if (isDictionary())
							model.GetType().GetMethod("Add").Invoke(model, new[] { objReader.GetName(i), objReader[i] });
						else
						{
							PropertyInfo pi = modelType.GetProperty(objReader.GetName(i), BindingFlags.Default | BindingFlags.GetProperty | BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
							if (pi != null) pi.SetValue(model, CheckType(objReader[i], pi.PropertyType), null);
						}
			}
			return model;
		}
		public static TResult ReaderToModel<TResult>(this IDataReader objReader)
		{
			return (TResult)objReader.ReaderToModel(typeof(TResult));
		}
		static bool IsTuple(Type tupleType) => tupleType.Namespace == "System" && tupleType.Name.StartsWith("ValueTuple`", StringComparison.Ordinal);//判断是否元组类型

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
			++columnIndex;
			object dbValue = dr[columnIndex];
			return IsNullOrDBNull(dbValue) ? null : dbValue;
		}
		static bool IsNullOrDBNull(object obj) => obj is DBNull || obj == null;
		// 对可空类型进行判断转换(*要不然会报错)
		static object CheckType(object value, Type conversionType)
		{
			if (value == null) return null;
			if (conversionType.IsGenericType && conversionType.GetGenericTypeDefinition().Equals(typeof(Nullable<>)))
				conversionType = new NullableConverter(conversionType).UnderlyingType;
			if (conversionType.Namespace == "Newtonsoft.Json.Linq")
				return JToken.Parse(value?.ToString() ?? "{}");
			return Convert.ChangeType(value, conversionType);

		}
		#endregion
	}
}

using Meta.Common.Extensions;
using Meta.Common.Model;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Meta.Common.DbHelper
{
	/// <summary>
	/// 数据库表特性帮助类
	/// </summary>
	internal static class EntityHelper
	{
		static Dictionary<string, string[]> _typeFieldsDict;
		const string _sysytemLoadSuffix = ".SystemLoad";

		static string[] GetFieldsFromStaticType(Type type)
		{
			InitStaticTypesFields(type);
			return _typeFieldsDict[string.Concat(type.Name, "Model", _sysytemLoadSuffix)];
		}
		/// <summary>
		/// 匹配生成模型
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <returns></returns>
		static string[] GetFieldsFromStaticType<T>()
		{
			return GetFieldsFromStaticType(typeof(T));
		}

		static void InitStaticTypesFields(Type t)
		{
			if (_typeFieldsDict != null) return;
			_typeFieldsDict = new Dictionary<string, string[]>();
			var types = t.Assembly.GetTypes().Where(f => !string.IsNullOrEmpty(f.Namespace) && f.Namespace.EndsWith(".Model"));
			foreach (var type in types)
			{
				var key = string.Concat(type.Name, _sysytemLoadSuffix);
				if (!_typeFieldsDict.ContainsKey(key))
					_typeFieldsDict[key] = GetAllFields("", type).ToArray();
			}
		}
		static void InitStaticTypesFields<T>()
		{
			InitStaticTypesFields(typeof(T));
		}

		/// <summary>
		/// 获取当前所有字段列表
		/// </summary>
		/// <param name="type"></param>
		/// <param name="alias"></param>
		/// <returns></returns>
		static List<string> GetAllFields(string alias, Type type)
		{
			List<string> list = new List<string>();
			alias = !string.IsNullOrEmpty(alias) ? alias + "." : "";
			GetAllFields(p => list.Add(alias + p.Name.ToLower()), type);
			return list;
		}
		/// <summary>
		/// 获取Mapping特性
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="action"></param>
		public static string GetTableName<T>()
		{
			var mapping = typeof(T).GetCustomAttribute<MappingAttribute>();
			if (mapping == null)
				throw new ArgumentNullException(nameof(MappingAttribute));
			return mapping.TableName;
		}

		/// <summary>
		/// 获取当前类字段的字符串
		/// </summary>
		/// <param name="type"></param>
		/// <param name="alias"></param>
		/// <returns></returns>
		public static string GetModelTypeFieldsString(string alias, Type type)
		{
			InitStaticTypesFields(type);
			return string.Join(", ", _typeFieldsDict[string.Concat(type.Name, _sysytemLoadSuffix)].Select(f => $"{alias}.{f}"));
		}
		/// <summary>
		/// 获取当前类字段的字符串
		/// </summary>
		/// <param name="type"></param>
		/// <param name="alias"></param>
		/// <returns></returns>
		public static string GetModelTypeFieldsString<T>(string alias)
		{
			return GetModelTypeFieldsString(alias, typeof(T));
		}

		public static string GetDALTypeFieldsString(string alias, Type type)
		{
			return string.Join(", ", GetFieldsFromStaticType(type).Select(f => $"{alias}.{f}"));
		}
		public static string GetDALTypeFieldsString<T>(string alias)
		{
			return GetDALTypeFieldsString(alias, typeof(T));
		}

		/// <summary>
		/// 遍历所有字段
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="action"></param>
		static void GetAllFields<T>(Action<PropertyInfo> action)
		{
			GetAllFields(action, typeof(T));
		}
		/// <summary>
		/// 遍历所有字段
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="action"></param>
		static void GetAllFields(Action<PropertyInfo> action, Type type)
		{
			PropertyInfo[] pi = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
			foreach (var p in pi)
			{
				if (p.ExistsJsonPropertyAttribute())
					action?.Invoke(p);
			}
		}
	}

}
using Meta.Driver.Extensions;
using Meta.Driver.Interface;
using Meta.Driver.Model;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Meta.Driver.DbHelper
{
	/// <summary>
	/// 数据库表特性帮助类
	/// </summary>
	internal static class EntityHelper
	{
		/// <summary>
		/// 参数计数器
		/// </summary>
		static int _paramsCount = 0;

		/// <summary>
		/// 参数后缀
		/// </summary>
		public static string ParamsIndex
		{
			get
			{
				if (_paramsCount == int.MaxValue)
					_paramsCount = 0;
				return "p" + _paramsCount++.ToString().PadLeft(6, '0');
			}
		}

		static Dictionary<string, (string[], string[])> _typeFieldsDict;
		const string _sysytemLoadSuffix = ".SystemLoad";
		/// <summary>
		/// 根据实体类获取所有字段数组, 有双引号
		/// </summary>
		/// <param name="type"></param>
		/// <returns></returns>
		public static string[] GetFieldsFromStaticType(Type type)
		{
			InitStaticTypesFields(type);
			return _typeFieldsDict[string.Concat(type.FullName, _sysytemLoadSuffix)].Item1;
		}

		/// <summary>
		/// 根据实体类获取所有字段数组, 有双引号
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <returns></returns>
		static string[] GetFieldsFromStaticType<T>() where T : IDbModel
		{
			return GetFieldsFromStaticType(typeof(T));
		}

		/// <summary>
		/// 根据实体类获取所有字段数组, 不包含双引号
		/// </summary>
		/// <param name="type"></param>
		/// <returns></returns>
		public static string[] GetFieldsFromStaticTypeNoSymbol(Type type)
		{
			InitStaticTypesFields(type);
			return _typeFieldsDict[string.Concat(type.FullName, _sysytemLoadSuffix)].Item2;
		}

		/// <summary>
		/// 根据实体类获取所有字段数组, 不包含双引号
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <returns></returns>
		static string[] GetFieldsFromStaticTypeNoSymbol<T>() where T : IDbModel
		{
			return GetFieldsFromStaticTypeNoSymbol(typeof(T));
		}

		/// <summary>
		/// 根据类型初始化, 实体类map
		/// </summary>
		/// <param name="t"></param>
		static void InitStaticTypesFields(Type t)
		{
			if (_typeFieldsDict != null) return;
			if (!t.GetInterfaces().Any(f => f == typeof(IDbModel))) return;
			_typeFieldsDict = new Dictionary<string, (string[], string[])>();
			var types = t.Assembly.GetTypes().Where(f => !string.IsNullOrEmpty(f.Namespace) && f.Namespace.Contains(".Model") && f.GetCustomAttribute<DbTableAttribute>() != null);
			foreach (var type in types)
			{
				var key = string.Concat(type.FullName, _sysytemLoadSuffix);
				if (!_typeFieldsDict.ContainsKey(key))
					_typeFieldsDict[key] = GetAllFields("", type);

			}
		}

		static void InitStaticTypesFields<T>() where T : IDbModel
		{
			InitStaticTypesFields(typeof(T));
		}

		/// <summary>
		/// 获取当前所有字段列表
		/// </summary>
		/// <param name="type"></param>
		/// <param name="alias"></param>
		/// <returns>(包含双引号,用于SQL语句,不包含双引号,用于反射)</returns>
		static (string[], string[]) GetAllFields(string alias, Type type)
		{
			List<string> list = new List<string>();
			List<string> list_u = new List<string>();
			alias = !string.IsNullOrEmpty(alias) ? alias + "." : "";
			GetAllFields(p =>
			{
				list.Add(alias + "\"" + p.Name.ToLower() + "\"");
				list_u.Add(alias + p.Name.ToLower());
			}, type);
			return (list.ToArray(), list_u.ToArray());
		}

		/// <summary>
		/// 获取Mapping特性
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <returns></returns>
		public static string GetTableName<T>() where T : IDbModel
		{
			return GetTableName(typeof(T));
		}

		/// <summary>
		/// 获取Mapping特性
		/// </summary>
		/// <returns></returns>
		public static string GetTableName(Type type)
		{
			var mapping = type.GetCustomAttribute<DbTableAttribute>();
			if (mapping == null)
				throw new ArgumentNullException(nameof(DbTableAttribute));
			return mapping.TableName;
		}

		/// <summary>
		/// 获取db别名 如果没有返回null
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <returns></returns>
		public static string GetDbName<T>()
		{
			var mapping = typeof(T).GetCustomAttribute<DbNameAttribute>();
			return mapping?.DbName;
		}

		/// <summary>
		/// 获取当前类字段的字符串, 包含双引号
		/// </summary>
		/// <param name="type"></param>
		/// <param name="alias"></param>
		/// <returns></returns>
		public static string GetModelTypeFieldsString(string alias, Type type)
		{
			InitStaticTypesFields(type);
			return string.Join(", ", _typeFieldsDict[string.Concat(type.FullName, _sysytemLoadSuffix)].Item1.Select(f => $"{alias}.{f}"));
		}

		/// <summary>
		/// 获取当前类字段的字符串, 包含双引号
		/// </summary>
		/// <param name="alias"></param>
		/// <returns></returns>
		public static string GetModelTypeFieldsString<T>(string alias) where T : IDbModel
		{
			return GetModelTypeFieldsString(alias, typeof(T));
		}
		/// <summary>
		/// 获取当前类字段的字符串, 不包含双引号
		/// </summary>
		/// <param name="type"></param>
		/// <param name="alias"></param>
		/// <returns></returns>
		public static string GetModelTypeFieldsStringNoSymbol(string alias, Type type)
		{
			InitStaticTypesFields(type);
			return string.Join(", ", _typeFieldsDict[string.Concat(type.FullName, _sysytemLoadSuffix)].Item2.Select(f => $"{alias}.{f}"));
		}

		/// <summary>
		/// 获取当前类字段的字符串, 不包含双引号
		/// </summary>
		/// <param name="alias"></param>
		/// <returns></returns>
		public static string GetModelTypeFieldsStringNoSymbol<T>(string alias) where T : IDbModel
		{
			return GetModelTypeFieldsString(alias, typeof(T));
		}

		/// <summary>
		/// 遍历所有字段
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="action"></param>
		static void GetAllFields<T>(Action<PropertyInfo> action) where T : IDbModel
		{
			GetAllFields(action, typeof(T));
		}

		/// <summary>
		/// 遍历所有字段
		/// </summary>
		/// <param name="action"></param>
		/// <param name="type"></param>
		static void GetAllFields(Action<PropertyInfo> action, Type type)
		{
			PropertyInfo[] pi = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
			foreach (var p in pi)
			{
				if (p.GetCustomAttribute<JsonPropertyAttribute>() != null)
					action?.Invoke(p);
			}
		}
	}
}
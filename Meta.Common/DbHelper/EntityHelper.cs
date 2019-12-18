using Meta.Common.Extensions;
using Meta.Common.Interface;
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
		static Dictionary<string, SortedList<string, DbFieldModel>> _typeFieldsDict;
		const string _sysytemLoadSuffix = ".SystemLoad";

		public static string[] GetFieldsFromStaticType(Type type)
		{
			InitStaticTypesFields(type);
			return _typeFieldsDict[string.Concat(type.Name, _sysytemLoadSuffix)].Keys.ToArray();
		}
		/// <summary>
		/// 匹配生成模型
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <returns></returns>
		static string[] GetFieldsFromStaticType<T>() where T : IDbModel
		{
			return GetFieldsFromStaticType(typeof(T));
		}

		static void InitStaticTypesFields(Type t)
		{
			if (_typeFieldsDict != null) return;
			if (!t.GetInterfaces().Any(f => f == typeof(IDbModel))) return;
			_typeFieldsDict = new Dictionary<string, SortedList<string, DbFieldModel>>();
			var types = t.Assembly.GetTypes().Where(f => !string.IsNullOrEmpty(f.Namespace) && f.Namespace.EndsWith(".Model") && f.GetCustomAttribute<DbTableAttribute>() != null);
			foreach (var type in types)
			{
				var key = string.Concat(type.Name, _sysytemLoadSuffix);
				if (!_typeFieldsDict.ContainsKey(key))
				{
					_typeFieldsDict[key] = new SortedList<string, DbFieldModel>();
					GetAllFields((p, dbFieldAttr) =>
					{
						_typeFieldsDict[key][p.Name.ToLower()] = dbFieldAttr.DbField;
					}, type);
				}
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
		/// <returns></returns>
		static List<string> GetAllFields(string alias, Type type)
		{
			List<string> list = new List<string>();
			alias = !string.IsNullOrEmpty(alias) ? alias + "." : "";
			GetAllFields((p, dbFieldAttr) => list.Add(alias + p.Name.ToLower()), type);
			return list;
		}
		/// <summary>
		/// 获取Mapping特性
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="action"></param>
		public static string GetTableName<T>() where T : IDbModel
		{
			return GetTableName(typeof(T));
		}
		/// <summary>
		/// 获取Mapping特性
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="action"></param>
		public static string GetTableName(Type type)
		{
			var mapping = type.GetCustomAttribute<DbTableAttribute>();
			if (mapping == null)
				throw new ArgumentNullException(nameof(DbTableAttribute));
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
			return string.Join(", ", _typeFieldsDict[string.Concat(type.Name, _sysytemLoadSuffix)].Keys.Select(f => $"{alias}.{f}"));
		}
		/// <summary>
		/// 获取当前类字段的字符串
		/// </summary>
		/// <param name="type"></param>
		/// <param name="alias"></param>
		/// <returns></returns>
		public static string GetModelTypeFieldsString<T>(string alias) where T : IDbModel
		{
			return GetModelTypeFieldsString(alias, typeof(T));
		}
		/// <summary>
		/// 遍历所有字段
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="action"></param>
		static void GetAllFields<T>(Action<PropertyInfo, DbFieldAttribute> action) where T : IDbModel
		{
			GetAllFields(action, typeof(T));
		}

		/// <summary>
		/// 遍历所有字段
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="action"></param>
		static void GetAllFields(Action<PropertyInfo, DbFieldAttribute> action, Type type)
		{
			PropertyInfo[] pi = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
			foreach (var p in pi)
			{
				if (p.GetCustomAttribute<DbFieldAttribute>() is DbFieldAttribute dbFieldAttr)
					action?.Invoke(p, dbFieldAttr);
			}
		}
	}

}
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace Meta.Common.Model
{
	/// <summary>
	/// 数据库表特性
	/// </summary>
	[AttributeUsage(AttributeTargets.Property | AttributeTargets.Class, Inherited = true)]
	public class MappingAttribute : Attribute
	{
		public string TableName { get; set; }
		public MappingAttribute(string tableName) => TableName = tableName;
	}
	public class MappingHelper
	{
		/// <summary>
		/// 当前类的表
		/// </summary>
		/// <param name="t"></param>
		/// <returns></returns>
		public static string GetMapping<T>()
		{
			string tableName = string.Empty;
			GetMappingAttr<T>(m => { tableName = m.TableName; });
			return tableName;
		}
		/// <summary>
		/// 获取Mapping特性
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="action"></param>
		static void GetMappingAttr<T>(Action<MappingAttribute> action)
		{
			TypeInfo typeInfo = typeof(T).GetTypeInfo();
			var mapping = typeInfo.GetCustomAttribute<MappingAttribute>();
			if (mapping is null) throw new NotSupportedException("找不到MappingAttribute特性, 请确认实体模型");

			action?.Invoke(mapping);
		}
	}
	public class EntityHelper
	{
		/// <summary>
		/// 获取当前所有字段列表
		/// </summary>
		/// <param name="type"></param>
		/// <param name="alias"></param>
		/// <returns></returns>
		public static List<string> GetAllFields<T>(string alias)
		{
			List<string> list = new List<string>();
			alias = !string.IsNullOrEmpty(alias) ? alias + "." : "";
			GetAllFields<T>(p => list.Add(alias + p.Name.ToLower()));
			return list;
		}
		/// <summary>
		/// 获取当前类字段的字符串
		/// </summary>
		/// <param name="type"></param>
		/// <param name="alias"></param>
		/// <returns></returns>
		public static string GetAllSelectFieldsString<T>(string alias)
		{
			StringBuilder ret = new StringBuilder();
			alias = !string.IsNullOrEmpty(alias) ? alias + "." : "";
			GetAllFields<T>(p => ret.Append(alias).Append(p.Name.ToLower()).Append(", "));
			return ret.ToString().TrimEnd(' ', ',');
		}
		/// <summary>
		/// 遍历所有字段
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="action"></param>
		public static void GetAllFields<T>(Action<PropertyInfo> action)
		{
			PropertyInfo[] pi = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);
			foreach (var p in pi)
			{
				if (p.GetCustomAttribute<JsonPropertyAttribute>() != null)
					action?.Invoke(p);
			}
		}
	}

}
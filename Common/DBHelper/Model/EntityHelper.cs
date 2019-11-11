using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace DBHelper
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
		public static string GetMapping(Type t)
		{
			string tableName = string.Empty;
			GetMappingAttr(t, m => { tableName = m.TableName; });
			return tableName;
		}
		static void GetMappingAttr(Type t, Action<MappingAttribute> action)
		{
			TypeInfo typeInfo = t.GetTypeInfo();
			if (typeInfo.GetCustomAttribute(typeof(MappingAttribute)) is MappingAttribute mapping)
				action?.Invoke(mapping);
			else
				throw new NotSupportedException("找不到MappingAttribute特性, 请确认实体模型");
		}
	}
	public class EntityHelper
	{
		/// <summary>
		/// 不输出的特性
		/// </summary>
		/// <param name="item"></param>
		/// <returns></returns>
		public static bool ToBsonAttribute(PropertyInfo item) => item.GetCustomAttribute(typeof(JsonPropertyAttribute)) != null;

		/// <summary>
		/// 获取当前所有字段列表
		/// </summary>
		/// <param name="type"></param>
		/// <param name="alias"></param>
		/// <returns></returns>
		public static List<string> GetAllFields(Type type, string alias)
		{
			List<string> list = new List<string>();
			alias = !string.IsNullOrEmpty(alias) ? alias + "." : "";
			GetAllFields(type, (p) =>
			{
				list.Add(alias + p.Name.ToLower());
			});
			return list;
		}
		/// <summary>
		/// 获取当前类字段的字符串
		/// </summary>
		/// <param name="type"></param>
		/// <param name="alias"></param>
		/// <returns></returns>
		public static string GetAllSelectFieldsString(Type type, string alias)
		{
			StringBuilder ret = new StringBuilder();
			alias = !string.IsNullOrEmpty(alias) ? alias + "." : "";
			GetAllFields(type, p => ret.Append(alias).Append(p.Name.ToLower()).Append(", "));
			return ret.ToString().TrimEnd(' ', ',');
		}

		public static void GetAllFields(Type type, Action<PropertyInfo> action)
		{
			PropertyInfo[] pi = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
			for (int i = 0; i < pi.Length; i++)
			{
				if (ToBsonAttribute(pi[i]))
					action?.Invoke(pi[i]);
			}
		}
	}

}
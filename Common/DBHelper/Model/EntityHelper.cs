using Microsoft.AspNetCore.Hosting;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection;
using System.Text;

namespace DBHelper
{
	/// <summary>
	/// Table attribute.
	/// </summary>
	[AttributeUsage(AttributeTargets.Property | AttributeTargets.Class, Inherited = true)]
	public class MappingAttribute : Attribute
	{
		public string TableName { get; set; }
		public MappingAttribute(string tableName) => TableName = tableName;
	}
	/// <summary>
	/// Mothod attribute.
	/// </summary>
	[AttributeUsage(AttributeTargets.Property, Inherited = true)]
	public class MethodPropertyAttribute : Attribute { }
	/// <summary>
	/// Primary key attribute
	/// </summary>
	[AttributeUsage(AttributeTargets.Property, Inherited = true)]
	public class PrimaryKeyAttribute : Attribute { }
	/// <summary>
	/// Foreign attribute.
	/// </summary>
	[AttributeUsage(AttributeTargets.Property, Inherited = true)]
	public class ForeignKeyPropertyAttribute : Attribute { }
	/// <summary>
	/// Field attribute.
	/// </summary>
	[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
	public class FieldAttribute : ColumnAttribute
	{
		public FieldAttribute(string name, string dbtype, int length) : base(name)
		{
			TypeName = dbtype;
			Order = length;
		}
	}

	public class MappingHelper
	{
		/// <summary>
		/// Table of current class.
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
				throw new NotSupportedException("找不到EntityMappingAttribute特性, 请确认实体模型");
		}
	}
	public class EntityHelper
	{
		/// <summary>
		/// Do not output attribute
		/// </summary>
		/// <param name="item"></param>
		/// <returns></returns>
		public static bool InspectionAttribute(PropertyInfo item)
		{
			if (item.GetCustomAttribute(typeof(MethodPropertyAttribute)) == null &&
				item.GetCustomAttribute(typeof(ForeignKeyPropertyAttribute)) == null)
				return true;
			return false;
		}
		/// <summary>
		/// Get all fields of this class.
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
		/// Get string of all fields of current class.
		/// </summary>
		/// <param name="type"></param>
		/// <param name="alias"></param>
		/// <returns></returns>
		public static string GetAllSelectFieldsString(Type type, string alias)
		{
			StringBuilder ret = new StringBuilder();
			alias = !string.IsNullOrEmpty(alias) ? alias + "." : "";
			GetAllFields(type, p => ret.Append(alias).Append(p.Name.ToLower()).Append(", "));
			return ret.Remove(ret.Length - 1, 1).ToString();
		}

		public static void GetAllFields(Type type, Action<PropertyInfo> action)
		{
			PropertyInfo[] pi = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
			for (int i = 0; i < pi.Length; i++)
			{
				if (InspectionAttribute(pi[i]))
					action?.Invoke(pi[i]);
			}
		}
	}

}
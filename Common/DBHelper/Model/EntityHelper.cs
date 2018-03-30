using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace DBHelper
{
	[AttributeUsage(AttributeTargets.Property | AttributeTargets.Class, Inherited = true)]
	public class EntityMappingAttribute : Attribute
	{
		public string TableName { get; set; }
	}
	[AttributeUsage(AttributeTargets.Property, Inherited = true)]
	public class MethodPropertyAttribute : Attribute
	{

	}
	[AttributeUsage(AttributeTargets.Property, Inherited = true)]
	public class ForeignKeyPropertyAttribute : Attribute
	{

	}

	[AttributeUsage(AttributeTargets.Property, Inherited = true)]
	public class NonDbColumnMappingAttribute : Attribute
	{

	}
	public class MappingHelper
	{
		public static string GetMapping(Type t)
		{
			string table = string.Empty;
			TypeInfo typeInfo = t.GetTypeInfo();
			if (typeInfo.GetCustomAttribute(typeof(EntityMappingAttribute)) is EntityMappingAttribute mapping)
				return mapping.TableName;
			else
				throw new NotSupportedException("找不到EntityMappingAttribute特性, 请确认实体模型");
		}
	}
	public class EntityHelper
	{
		public static bool InspectionAttribute(PropertyInfo item)
		{
			if (item.GetCustomAttribute(typeof(MethodPropertyAttribute)) == null &&
				item.GetCustomAttribute(typeof(ForeignKeyPropertyAttribute)) == null)
				return true;
			return false;
		}
		public static List<string> GetAllFields(Type type, string alias)
		{
			alias = !string.IsNullOrEmpty(alias) ? alias + "." : "";
			List<string> list = new List<string>();
			PropertyInfo[] ps = type.GetProperties();
			foreach (var item in ps)
			{
				//todo: 应该输出全部表的结果
				if (InspectionAttribute(item))
					list.Add(alias + item.Name.ToLower());
			}
			return list;
		}
	}

}
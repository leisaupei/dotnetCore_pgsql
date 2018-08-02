using DBHelper;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

public static class BsonExtension
{
	public static IDictionary ToBsonOne<TTarget>(this TTarget info, Func<TTarget, object> func = null) => GetBson(new[] { info }, func)[0];
	public static IDictionary[] ToBson<TTarget>(this TTarget[] info, Func<TTarget, object> func = null) => GetBson(info, func);
	public static IDictionary[] ToBson<TTarget>(this IEnumerable<TTarget> items, Func<TTarget, object> func = null) => GetBson(items, func);
	public static IDictionary[] GetBson(IEnumerable items, Delegate func = null)
	{
		List<IDictionary> ret = new List<IDictionary>();
		var ie = items.GetEnumerator();
		while (ie.MoveNext())
		{
			if (ie.Current == null) ret.Add(null);
			else if (func == null) AddList(ret, ie.Current);
			else
			{
				object obj = func.GetMethodInfo().Invoke(func.Target, new object[] { ie.Current });
				if (obj is IDictionary idict) ret.Add(idict);
				else AddList(ret, obj);
			}
		}
		return ret.ToArray();
	}
	private static void AddList(List<IDictionary> ret, object obj)
	{
		Hashtable ht = new Hashtable();
		var props = obj.GetType().GetProperties();
		foreach (var prop in props)
			if (EntityHelper.InspectionAttribute(prop))
				ht[prop.Name] = prop.GetValue(obj);
		ret.Add(ht);
	}
}

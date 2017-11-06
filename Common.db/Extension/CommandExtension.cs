using Common.db.Common;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace Common.db.Extension
{
    public static class CommandExtension
    {

        public static IDictionary ToBsonOne<TTarget>(this TTarget info, Func<TTarget, object> func = null)
        {
            List<TTarget> items = new List<TTarget>();
            items.Add(info);
            return GetBson(items, func)[0];
        }
        public static IDictionary[] ToBson<TTarget>(this TTarget[] info, Func<TTarget, object> func = null)
        {
            return GetBson(info, func);
        }
        public static IDictionary[] ToBson<TTarget>(this IEnumerable<TTarget> items, Func<TTarget, object> func = null) => GetBson(items, func);

        public static IDictionary[] GetBson(IEnumerable items, Delegate func = null)
        {
            List<IDictionary> ret = new List<IDictionary>();
            IEnumerator ie = items.GetEnumerator();
            while (ie.MoveNext())
            {
                if (ie.Current == null) ret.Add(null);
                else if (func == null)
                {
                    Hashtable ht = new Hashtable();
                    var props = ie.Current.GetType().GetProperties();
                    foreach (var prop in props)
                        if (EntityHelper.InspectionAttribute(prop)) ht[prop.Name] = prop.GetValue(ie.Current);

                    ret.Add(ht);
                }
                else
                {
                    object obj = func.GetMethodInfo().Invoke(func.Target, new object[] { ie.Current });
                    if (obj is IDictionary) ret.Add(obj as IDictionary);
                    else
                    {
                        Hashtable ht = new Hashtable();
                        var props = obj.GetType().GetProperties();
                        foreach (var prop in props)
                            if (EntityHelper.InspectionAttribute(prop)) ht[prop.Name] = prop.GetValue(obj);
                        ret.Add(ht);
                    }
                }
            }
            return ret.ToArray();
        }

    }
}

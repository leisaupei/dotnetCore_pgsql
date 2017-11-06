
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;


public static class GlobalExtension
{




    #region DB
   
    #endregion

    #region IDictionary
    /// <summary>
    /// 添加一个键值对并返回该idictionary
    /// </summary>
    /// <returns></returns>
    public static IDictionary AddKey(this IDictionary idict, string key, object value)
    {
        idict[key] = value;
        return idict;
    }
    #endregion

    public static bool IsNullOrEmpty(this Guid value) => value == null || value == Guid.Empty;

    /// <summary>
    /// object返回空字符串或者返回tostring()
    /// </summary>
    public static string NullToString(this object str)
    {
        if (str == null)
            return "";
        else return str.ToString();
    }
  

    #region IEnumerable
    /// <summary>
    /// 返回Array基本类型的默认值或者第一个元素
    /// </summary>
    public static T ToArrayFirst<T>(this IEnumerable<T> arr)
    {
        if (arr == null || arr.Count() == 0)
            return default(T);
        else return arr.First();
    }
    #endregion
    
    #region DateTime
    /// <summary>  
    /// 得到本周第一天(以星期天为第一天)  
    /// </summary>  
    /// <param name="datetime"></param>  
    /// <returns></returns>  
    public static DateTime GetWeekFirstDaySun(this DateTime datetime)
    {
        //星期天为第一天  
        int weeknow = Convert.ToInt32(datetime.DayOfWeek);
        int daydiff = (-1) * weeknow;

        //本周第一天  
        string FirstDay = datetime.AddDays(daydiff).ToString("yyyy-MM-dd");
        return Convert.ToDateTime(FirstDay);
    }

    /// <summary>  
    /// 得到本周第一天(以星期一为第一天)  
    /// </summary>  
    /// <param name="datetime"></param>  
    /// <returns></returns>  
    public static DateTime GetWeekFirstDayMon(this DateTime datetime)
    {
        //星期一为第一天  
        int weeknow = Convert.ToInt32(datetime.DayOfWeek);

        //因为是以星期一为第一天，所以要判断weeknow等于0时，要向前推6天。  
        weeknow = (weeknow == 0 ? (7 - 1) : (weeknow - 1));
        int daydiff = (-1) * weeknow;

        //本周第一天  
        string FirstDay = datetime.AddDays(daydiff).ToString("yyyy-MM-dd");
        return Convert.ToDateTime(FirstDay);
    }

    /// <summary>  
    /// 得到本周最后一天(以星期六为最后一天)  
    /// </summary>  
    /// <param name="datetime"></param>  
    /// <returns></returns>  
    public static DateTime GetWeekLastDaySat(this DateTime datetime)
    {
        //星期六为最后一天  
        int weeknow = Convert.ToInt32(datetime.DayOfWeek);
        int daydiff = (7 - weeknow) - 1;

        //本周最后一天  
        string LastDay = datetime.AddDays(daydiff).ToString("yyyy-MM-dd");
        return Convert.ToDateTime(LastDay);
    }

    /// <summary>  
    /// 得到本周最后一天(以星期天为最后一天)  
    /// </summary>  
    /// <param name="datetime"></param>  
    /// <returns></returns>  
    public static DateTime GetWeekLastDaySun(this DateTime datetime)
    {
        //星期天为最后一天  
        int weeknow = Convert.ToInt32(datetime.DayOfWeek);
        weeknow = (weeknow == 0 ? 7 : weeknow);
        int daydiff = (7 - weeknow);

        //本周最后一天  
        string LastDay = datetime.AddDays(daydiff).ToString("yyyy-MM-dd");
        return Convert.ToDateTime(LastDay);
    }
	#endregion

}

using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

/**
 * @ 强类型化对象扩展方法
 * */
public static class TypeExtension
{
	#region General
	/// <summary>
	///  将首字母转大写
	/// </summary>
	public static string ToUpperPascal(this string s) => s.IsNullOrEmpty() ? s : $"{ s.Substring(0, 1).ToUpper()}{s.Substring(1)}";

	/// <summary>
	///  将首字母转小写
	/// </summary>
	/// <param name="s"></param>
	/// <returns></returns>
	public static string ToLowerPascal(this string s) => s.IsNullOrEmpty() ? s : $"{ s.Substring(0, 1).ToLower()}{s.Substring(1)}";

	/// <summary>
	/// 判断是否null或dbnull
	/// </summary>
	/// <param name="obj"></param>
	/// <returns></returns>
	public static bool IsNullOrDBNull(this object obj) => obj == null || obj is DBNull;
	#endregion

	#region Identity
	// 本地时区 1970.1.1格林威治时间
	public static DateTime Greenwich_Mean_Time = TimeZoneInfo.ConvertTime(new DateTime(1970, 1, 1), TimeZoneInfo.Local);

	#endregion

	public static object Json(this Microsoft.AspNetCore.Mvc.Rendering.IHtmlHelper html, object obj)
	{
		string s = JsonConvert.SerializeObject(obj);
		if (!s.IsNullOrEmpty()) s = Regex.Replace(s, @"<(/?script[\s>])", "<\"+\"$1", RegexOptions.IgnoreCase);
		if (html == null) return s;
		return html.Raw(s);
	}

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

	#region IEnumerable.To
	/// <summary>
	/// 返回Array基本类型的默认值或者第一个元素
	/// </summary>
	public static T ToArrayFirst<T>(this IEnumerable<T> arr) => arr == null || arr.Count() == 0 ? default(T) : arr.First();
	/// <summary>
	/// 在数组中插入分隔符
	/// </summary>
	/// <param name="s">分隔符</param>
	/// <returns></returns>
	public static string Join<T>(this IEnumerable<T> arr, string s) => string.Join(s, arr);
	/// <summary>
	/// 连接字符串
	/// </summary>
	/// <param name="ps">字符串</param>
	/// <returns></returns>
	public static string Concats(this string s, params string[] ps) => s + string.Concat(ps);
	#endregion

	#region Validator
	/// <summary>
	/// 判断一个字符串是否int类型
	/// </summary>
	public static bool IsInt(this string s) => new Regex(@"\d").IsMatch(s);

	/// <summary>
	/// 判断数组是否不为空，并且长度等于 @len
	/// </summary>
	/// <param name="len">数组长度</param>
	public static bool IsNotNullAndEq<T>(this IEnumerable<T> array, int len) => array != null && array.Count() == len;

	/// <summary>
	/// 判断数组是否不为空，并且长度大于等于 @len
	/// </summary>
	/// <param name="len">数组长度</param>
	public static bool IsNotNullAndGtEq<T>(this IEnumerable<T> array, int len) => array != null && array.Count() >= len;

	/// <summary>
	/// 判断数组为空
	/// </summary>
	public static bool IsNullOrEmpty<T>(this IEnumerable<T> value) => value == null || value.Count() == 0;

	/// <summary>
	/// 判断数组不为空
	/// </summary>
	public static bool IsNotNullOrEmpty<T>(this IEnumerable<T> value) => value != null && value.Count() != 0;
	/// <summary>
	/// 检查值类型是否枚举类型
	/// </summary>
	/// <typeparam name="T">枚举类型</typeparam>
	/// <typeparam name="F">要检查的值类型</typeparam>
	/// <param name="value"></param>
	/// <returns></returns>
	public static bool IsEnum<T, F>(this F value) => Enum.IsDefined(typeof(T), value);
	/// <summary>
	/// 判断Guid是空值或者Guid.Empty
	/// </summary>
	public static bool IsNullOrEmpty(this Guid value) => value == null || value == Guid.Empty;
	#endregion

	#region Linq
	/// <summary>
	/// 排序规则
	/// </summary>
	/// <typeparam name="T"></typeparam>
	/// <param name="source"></param>
	/// <param name="sortId"></param>
	/// <param name="sortOrder"></param>
	/// <returns></returns>
	public static IQueryable<T> DataSorting<T>(this IQueryable<T> source, string sortId, string sortOrder)
	{
		if (source != null)
		{
			string sortingDir = string.Empty;
			if (sortOrder.ToUpper().Trim() == "ASC")
				sortingDir = "OrderBy";
			else if (sortOrder.ToUpper().Trim() == "DESC")
				sortingDir = "OrderByDescending";
			ParameterExpression param = Expression.Parameter(typeof(T), sortId);
			PropertyInfo pi = typeof(T).GetRuntimeProperty(sortId);
			Type[] types = new Type[2];
			types[0] = typeof(T);
			types[1] = pi.PropertyType;
			Expression expr = Expression.Call(typeof(Queryable), sortingDir, types, source.Expression, Expression.Lambda(Expression.Property(param, sortId), param));
			IQueryable<T> query = source.AsQueryable().Provider.CreateQuery<T>(expr);
			return query;
		}
		return source;
	}
	#endregion

	#region String.To

	public static byte[] ToBytes(this string s) => s.IsNullOrEmpty() ? null : Encoding.UTF8.GetBytes(s);
	public static int ToShortInt(this string s) => s.IsNullOrEmpty() ? 0 : (new Regex(@"^[0-9]+$").IsMatch(s) ? Convert.ToInt16(s) : 0);
	public static int ToInt(this string s) => s.IsNullOrEmpty() ? 0 : (new Regex(@"^[0-9]+$").IsMatch(s) ? Convert.ToInt32(s) : 0);
	public static long ToLong(this string s) => s.IsNullOrEmpty() ? 0 : (new Regex(@"^[0-9]+$").IsMatch(s) ? Convert.ToInt64(s) : 0);
	public static decimal ToDecimal(this string s) => s.IsNullOrEmpty() ? 0 : (new Regex(@"[0-9]\d*[\.]?\d*|-0\.\d*[0-9]\d*$").IsMatch(s) ? Convert.ToDecimal(s) : 0);
	public static Guid ToGuid(this string s)
	{
		Guid val = Guid.Empty;
		if (s.IsNullOrEmpty())
			return val;
		Guid.TryParse(s, out val);
		return val;
	}
	public static double ToDouble(this string s) => s.IsNullOrEmpty() ? 0 : (new Regex(@"[0-9]\d*[\.]?\d*|-0\.\d*[0-9]\d*$").IsMatch(s) ? Convert.ToDouble(s) : 0);
	public static double ToFloat(this string s) => (float)ToDecimal(s);
	public static DateTime ToDateTime(this string s)
	{
		DateTime val = DateTime.Parse("1970-1-1");
		if (s.IsNullOrEmpty()) return val;
		DateTime.TryParse(s, out val);
		return val;
	}
	public static bool? ToBooleanNull(this string s)
	{

		bool? val = null;
		try
		{
			val = Convert.ToBoolean(s);
		}
		catch { }
		return val;
	}
	public static bool ToBoolean(this string s) => s.IsNullOrEmpty() ? false : (new Regex(@"[0-9]\d*[\.]?\d*|-0\.\d*[0-9]\d*$").IsMatch(s) ? Convert.ToInt64(s) > 0 : (new Regex(@"true|false").IsMatch(s.ToLower()) ? Convert.ToBoolean(s) : false));

	/// <summary>
	/// 截取字符串 用符号省略
	/// </summary>
	/// <param name="s">字符串</param>
	/// <param name="length">长度</param>
	/// <param name="spl">省略符</param>
	/// <returns></returns>
	public static string ToEllipsis(this string s, int length, string spl = "...")
	{
		if (s.IsNullOrEmpty()) return "";
		if (s.Length > length) return string.Format("{0}{1}", s.Substring(0, length), spl);
		else return s;
	}

	/// <summary>
	/// 空格和制表位替换成空格
	/// </summary>
	/// <param name="s"></param>
	/// <returns></returns>
	public static string ToTrimSpace(this string s) => new Regex(@"[\u3000||\u0020|\t]{2,}").Replace(s, " ");
	/// <summary>
	/// 过滤emoji与obj方框
	/// </summary>
	/// <param name="s"></param>
	/// <returns></returns>
	public static string ToFilterEmojiObj(this string s) => Regex.Replace(s, @"\p{Cs}|￼|\s", "");

	/**
	 * @ 将位移编码的字母和数字进行解码
	 * */
	public static string DecodeText(this string s)
	{
		byte[] bytes = Encoding.ASCII.GetBytes(s);
		for (int i = 0; i < bytes.Length; i++)
		{
			bytes[i] = Convert.ToByte(bytes[i] + 10 - 7);
		}
		string str = Encoding.ASCII.GetString(bytes);
		return str;
	}

	/**
	 * @ 将字母和数字进行位移编码，sorry 不支持中文
	 * */
	public static string EncodeText(this string s)
	{
		byte[] bytes = Encoding.ASCII.GetBytes(s);
		for (int i = 0; i < bytes.Length; i++)
		{
			bytes[i] = Convert.ToByte(bytes[i] - 10 + 7);
		}
		string str = Encoding.ASCII.GetString(bytes);

		return str;
	}
	#endregion

	#region DateTime.To
	public static long DateDiff(this DateTime dt1, DateTime dt2, DateInterval di) => DateAndTime.DateDiff(di, dt1, dt2);

	public static long ToUnixDateTime(this DateTime dt)
	{
		long val = 0;
		try
		{
			// 除以10000，保持13位
			val = (dt.ToUniversalTime().Ticks - Greenwich_Mean_Time.Ticks) / 10000000;
		}
		catch { }
		return val;
	}

	public static long ToUnixDateTime(this DateTime? dt)
	{
		long val = 0;
		try
		{
			val = ToUnixDateTime(Greenwich_Mean_Time);
		}
		catch { }
		return val;
	}
	/// <summary>
	/// .ToString("yyyy-MM-dd")
	/// </summary>
	/// <param name="dt"></param>
	/// <returns></returns>
	public static string ToDateString(this DateTime dt) => dt.ToString("yyyy-MM-dd");
	/// <summary>
	/// .ToString("yyyy-MM-dd HH:mm:ss.fff")
	/// </summary>
	/// <param name="dt"></param>
	/// <returns></returns>
	public static string ToDateTimeString(this DateTime dt) => dt.ToString("yyyy-MM-dd HH:mm:ss.fff");
	/// <summary>
	/// 日期转星期
	/// </summary>
	/// <param name="dt"></param>
	/// <returns></returns>
	public static string DateToWeak_ZH(this DateTime dt) => "星期" + ("日一二三四五六".Substring(dt.DayOfWeek.GetHashCode(), 1));

	#endregion

	#region decimal/float/double/long.To
	public static int ToInt(this long val) => Convert.ToInt16(val);
	public static int ToInt(this decimal val) => Convert.ToInt16(val);
	public static int ToInt(this float val) => Convert.ToInt16(val);
	public static int ToInt(this double val) => Convert.ToInt16(val);

	public static DateTime FromUnixDateTime(this long val)
	{
		DateTime dt = Greenwich_Mean_Time;
		try
		{
			dt = dt.AddMilliseconds(val);
		}
		catch { }
		return dt;
	}
	#endregion

	#region Other.To
	public static int ToInt(this bool val) => Convert.ToInt32(val);
	public static int ToInt(this bool? val) => val.HasValue ? Convert.ToInt32(val) : 0;
	public static int ToInt(this Enum e) => Convert.ToInt32(e);
	/**
	 * @ 调用该方法前最好先调用IsEnum进行确认
	 * */
	public static T ToEnum<T>(this string s)
	{
		T val = default(T);
		try
		{
			val = (T)Enum.Parse(typeof(T), s);
		}
		catch
		{
			throw new ArgumentException("由于不确定因素，此异常必须抛出，建议调用该方法前最好先调用 IsEnum 进行确认");
		}
		return val;
	}
	public static T ToEnum<T>(this int val) => ToEnum<T>(val.ToString());
	public static Enum ToEnum(this string s) => ToEnum<Enum>(s);
	/// <summary>
	///  将一个字典使用指定的分隔符号进行格式化后输出
	/// </summary>
	/// <param name="value">字典</param>
	/// <returns></returns>
	public static string ToUrlParams(this IDictionary value)
	{
		if (value == null || value.Count == 0)
			return "";
		StringBuilder text = new StringBuilder();
		foreach (var key in value.Keys)
			text.Append($"{key}={value[key]}&");
		string val = text.ToString();
		val = val.Substring(0, val.Length - 1);
		return val;
	}
	#endregion

	#region Base64.To
	/// <summary>
	/// Base64解码
	/// </summary>
	/// <param name="s"></param>
	/// <returns></returns>
	public static string Base64Decode(this string s) => s.IsNullOrEmpty() ? null : Encoding.UTF8.GetString(Convert.FromBase64String(s));
	/// <summary>
	/// Base64编码
	/// </summary>
	/// <param name="s"></param>
	/// <returns></returns>
	public static string Base64Encode(this string s) => s.IsNullOrEmpty() ? null : Convert.ToBase64String(Encoding.UTF8.GetBytes(s));
	public static string ToBase64(this byte[] b) => b.IsNullOrEmpty() ? string.Empty : Convert.ToBase64String(b);
	public static byte[] FromBase64(this string s) => s.IsNullOrEmpty() ? null : Convert.FromBase64String(s);
	#endregion

	#region MD5/SHA256/SHA1
	public static string ToMD5(this string value)
	{
		MD5 md5 = MD5.Create();
		byte[] source = Encoding.UTF8.GetBytes(value);
		byte[] crypto = md5.ComputeHash(source);
		return BitConverter.ToString(crypto).Replace("-", "").ToLower();
	}
	public static string ToSHA256(this string value)
	{
		SHA256 sha256 = SHA256.Create();
		byte[] source = Encoding.UTF8.GetBytes(value);
		byte[] crypto = sha256.ComputeHash(source);
		return BitConverter.ToString(crypto).Replace("-", "").ToLower();
	}
	public static string ToSHA1(this string value)
	{
		SHA1 sha1 = SHA1.Create();
		byte[] source = Encoding.UTF8.GetBytes(value);
		byte[] crypto = sha1.ComputeHash(source);
		return BitConverter.ToString(crypto).Replace("-", "").ToLower();
	}
	#endregion

	#region Object.To
	public static int ToInt(this object val)
	{
		int result = 0;
		try
		{
			if (val != null)
				result = Convert.ToInt32(val);
		}
		catch { }
		return result;
	}
	public static decimal ToDecimal(this object val)
	{
		decimal result = 0;
		try
		{
			if (val != null)
				result = Convert.ToDecimal(val);
		}
		catch { }
		return result;
	}
	public static bool ToBoolean(this object val)
	{
		try
		{
			return Convert.ToBoolean(val);
		}
		catch { }
		return false;
	}
	public static DateTime ToDateTime(this object val)
	{
		DateTime dt = Greenwich_Mean_Time;
		try
		{
			if (val == null)
				return dt;
			if (val is long)
				dt = dt.AddMilliseconds(Convert.ToInt64(val));
			else
				dt = Convert.ToDateTime(val);
		}
		catch { }
		return dt;
	}
	public static long ToLong(this object value)
	{
		long val = 0;
		try
		{
			if (value == null)
				return val;
			if (value is DateTime)
			{
				DateTime dt = value.ToDateTime();
				val = dt.ToUnixDateTime();
			}
			else
				val = Convert.ToInt64(value);
		}
		catch { }
		return val;
	}
	/// <summary>
	///  在相同对象间复制值，仅支持 Public 类型的属性，用于修改值
	/// </summary>
	/// <typeparam name="T"></typeparam>
	/// <param name="TSource">源对象</param>
	/// <param name="TTarget">目标对象</param>
	/// <param name="filter">忽略复制的属性名称，该参数为 TSource 的属性名称</param>
	/// <returns></returns>
	public static T CopyTo<T>(this T TSource, T TTarget, params string[] filter) where T : class
	{
		IEnumerable<PropertyInfo> properties = TSource.GetType().GetRuntimeProperties();
		if (filter.IsNotNullOrEmpty())
		{
			foreach (PropertyInfo pi in properties)
			{
				if (filter.Contains(pi.Name)) continue;
				pi.SetValue(TTarget, pi.GetValue(TSource, null), null);
			}
		}
		else
		{
			foreach (PropertyInfo pi in properties)
			{
				pi.SetValue(TTarget, pi.GetValue(TSource, null), null);
			}
		}
		return TTarget;
	}
	/// <summary>
	/// object返回空字符串或者返回tostring()
	/// </summary>
	public static string ToEmptyOrString(this object s) => s == null ? "" : s.ToString();
	public static string ToNullOrString(this object s) => s == null ? null : s.ToString();
	#endregion

	#region GetDateTime
	/// <summary>  
	/// 得到本周第一天(以星期天为第一天)  
	/// </summary>  
	/// <param name="dt"></param>  
	/// <returns></returns>  
	public static DateTime GetWeekFirstDaySun(this DateTime dt)
	{
		//星期天为第一天  
		int weeknow = Convert.ToInt32(dt.DayOfWeek);
		int daydiff = (-1) * weeknow;

		//本周第一天  
		string FirstDay = dt.AddDays(daydiff).ToString("yyyy-MM-dd");
		return Convert.ToDateTime(FirstDay);
	}

	/// <summary>  
	/// 得到本周第一天(以星期一为第一天)  
	/// </summary>  
	/// <param name="dt"></param>  
	/// <returns></returns>  
	public static DateTime GetWeekFirstDayMon(this DateTime dt)
	{
		//星期一为第一天  
		int weeknow = Convert.ToInt32(dt.DayOfWeek);

		//因为是以星期一为第一天，所以要判断weeknow等于0时，要向前推6天。  
		weeknow = (weeknow == 0 ? (7 - 1) : (weeknow - 1));
		int daydiff = (-1) * weeknow;

		//本周第一天  
		string FirstDay = dt.AddDays(daydiff).ToString("yyyy-MM-dd");
		return Convert.ToDateTime(FirstDay);
	}

	/// <summary>  
	/// 得到本周最后一天(以星期六为最后一天)  
	/// </summary>  
	/// <param name="dt"></param>  
	/// <returns></returns>  
	public static DateTime GetWeekLastDaySat(this DateTime dt)
	{
		//星期六为最后一天  
		int weeknow = Convert.ToInt32(dt.DayOfWeek);
		int daydiff = (7 - weeknow) - 1;

		//本周最后一天  
		string LastDay = dt.AddDays(daydiff).ToString("yyyy-MM-dd");
		return Convert.ToDateTime(LastDay);
	}

	/// <summary>  
	/// 得到本周最后一天(以星期天为最后一天)  
	/// </summary>  
	/// <param name="dt"></param>  
	/// <returns></returns>  
	public static DateTime GetWeekLastDaySun(this DateTime dt)
	{
		//星期天为最后一天  
		int weeknow = Convert.ToInt32(dt.DayOfWeek);
		weeknow = (weeknow == 0 ? 7 : weeknow);
		int daydiff = (7 - weeknow);

		//本周最后一天  
		string LastDay = dt.AddDays(daydiff).ToString("yyyy-MM-dd");
		return Convert.ToDateTime(LastDay);
	}
	#endregion

}
#region DateAndTime
/// <summary>
///  时间段枚举
/// </summary>
public enum DateInterval
{
	Day,
	DayOfYear,
	Hour,
	Minute,
	Month,
	Quarter,
	Second,
	Weekday,
	WeekOfYear,
	Year
}

/// <summary>
///  定义日期和时间处理的业务逻辑
/// </summary>
public class DateAndTime
{
	/// <summary>
	///  取两个日期间的差值，与 SQL 里面的同名函数功能相同
	/// </summary>
	/// <param name="interval"></param>
	/// <param name="dt1"></param>
	/// <param name="dt2"></param>
	/// <returns></returns>
	public static long DateDiff(DateInterval interval, DateTime dt1, DateTime dt2)
	{
		return DateDiff(interval, dt1, dt2, System.Globalization.DateTimeFormatInfo.CurrentInfo.FirstDayOfWeek);
	}

	/// <summary>
	///  获取季度
	/// </summary>
	/// <param name="nMonth"></param>
	/// <returns></returns>
	private static int GetQuarter(int nMonth)
	{
		if (nMonth <= 3) return 1;
		if (nMonth <= 6) return 2;
		if (nMonth <= 9) return 3;
		return 4;
	}

	/// <summary>
	///  取两个日期间的差值，与 SQL 里面的同名函数功能相同
	/// </summary>
	/// <param name="interval"></param>
	/// <param name="dt1"></param>
	/// <param name="dt2"></param>
	/// <param name="eFirstDayOfWeek"></param>
	/// <returns></returns>
	public static long DateDiff(DateInterval interval, DateTime dt1, DateTime dt2, DayOfWeek eFirstDayOfWeek)
	{
		if (interval == DateInterval.Year)
			return dt2.Year - dt1.Year;

		if (interval == DateInterval.Month)
			return (dt2.Month - dt1.Month) + (12 * (dt2.Year - dt1.Year));

		TimeSpan ts = dt2 - dt1;

		if (interval == DateInterval.Day || interval == DateInterval.DayOfYear)
			return Round(ts.TotalDays);

		if (interval == DateInterval.Hour)
			return Round(ts.TotalHours);

		if (interval == DateInterval.Minute)
			return Round(ts.TotalMinutes);

		if (interval == DateInterval.Second)
			return Round(ts.TotalSeconds);

		if (interval == DateInterval.Weekday)
		{
			return Round(ts.TotalDays / 7.0);
		}

		if (interval == DateInterval.WeekOfYear)
		{
			while (dt2.DayOfWeek != eFirstDayOfWeek)
				dt2 = dt2.AddDays(-1);
			while (dt1.DayOfWeek != eFirstDayOfWeek)
				dt1 = dt1.AddDays(-1);
			ts = dt2 - dt1;
			return Round(ts.TotalDays / 7.0);
		}

		if (interval == DateInterval.Quarter)
		{
			double d1Quarter = GetQuarter(dt1.Month);
			double d2Quarter = GetQuarter(dt2.Month);
			double d1 = d2Quarter - d1Quarter;
			double d2 = (4 * (dt2.Year - dt1.Year));
			return Round(d1 + d2);
		}

		return 0;

	}

	/// <summary>
	///  获取一个数的最大/最小的整数值
	/// </summary>
	/// <param name="dVal"></param>
	/// <returns></returns>
	private static long Round(double dVal)
	{
		if (dVal >= 0)
			return (long)Math.Floor(dVal);
		return (long)Math.Ceiling(dVal);
	}
}
#endregion
//using Meta.Common.DbHelper;
//using Meta.Common.Interface;
//using Meta.Common.Model;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Linq.Expressions;
//using System.Text;


//namespace Meta.Common.SqlBuilder
//{
//	/// <summary>
//	/// Select
//	/// </summary>
//	/// <typeparam name="TSQL"></typeparam>
//	/// <typeparam name="TModel"></typeparam>
//	public class SelectExchange<TSQL, TModel> : SelectBuilder<TSQL> where TSQL : class, new() where TModel : IDbModel, new()
//	{
//		public SelectExchange(string fields, string alias) : base(fields, alias) => Mapping(true);
//		public SelectExchange(string fields) : base(fields) => Mapping(true);
//		public SelectExchange() => Mapping();
//		private void Mapping(bool hasField = false)
//		{
//			MainTable = EntityHelper.GetTableName<TModel>();
//			if (!hasField)
//				Fields = EntityHelper.GetModelTypeFieldsString<TModel>(MainAlias);
//		}

//		#region Overload
//		public TSQL OrderByDescending<TKey>(Expression<Func<TModel, TKey>> selector) => base.OrderByDescending(selector);
//		public TSQL OrderBy<TKey>(Expression<Func<TModel, TKey>> selector) => base.OrderBy(selector);
//		public new TSQL OrderBy<TSource, TKey>(Expression<Func<TSource, TKey>> selector) => base.OrderBy(selector);
//		public TSQL GroupBy<TKey>(Expression<Func<TModel, TKey>> selector) => base.GroupBy(selector);
//		public TSQL InnerJoin<TTarget>(Expression<Func<TModel, TTarget, bool>> predicate) where TTarget : IDbModel => base.InnerJoin(predicate);
//		public TKey Max<TKey>(Expression<Func<TModel, TKey>> selector, TKey defaultValue = default) => base.Max(selector, defaultValue);
//		public TKey Min<TKey>(Expression<Func<TModel, TKey>> selector, TKey defaultValue = default) => base.Min(selector, defaultValue);
//		public TKey Sum<TKey>(Expression<Func<TModel, TKey>> selector, TKey defaultValue = default) => base.Sum(selector, defaultValue);
//		public TKey Avg<TKey>(Expression<Func<TModel, TKey>> selector, TKey defaultValue = default) => base.Avg(selector, defaultValue);

//		public (TModel, T1) ToOneUnion<T1>() => base.ToOne<(TModel, T1)>();
//		public List<(TModel, T1)> ToListUnion<T1>() => base.ToList<(TModel, T1)>();
//		public TSQL ToOneUnionPipe<T1>() => base.ToOnePipe<(TModel, T1)>();
//		public TSQL ToListUnionPipe<T1>() => base.ToListPipe<(TModel, T1)>();
//		public (TModel, T1, T2) ToOneUnion<T1, T2>() => base.ToOne<(TModel, T1, T2)>();
//		public List<(TModel, T1, T2)> ToListUnion<T1, T2>() => base.ToList<(TModel, T1, T2)>();
//		public TSQL ToOneUnionPipe<T1, T2>() => base.ToOnePipe<(TModel, T1, T2)>();
//		public TSQL ToListUnionPipe<T1, T2>() => base.ToListPipe<(TModel, T1, T2)>();
//		public (TModel, T1, T2, T3) ToOneUnion<T1, T2, T3>() => base.ToOne<(TModel, T1, T2, T3)>();
//		public List<(TModel, T1, T2, T3)> ToListUnion<T1, T2, T3>() => base.ToList<(TModel, T1, T2, T3)>();
//		public TSQL ToOneUnionPipe<T1, T2, T3>() => base.ToOnePipe<(TModel, T1, T2, T3)>();
//		public TSQL ToListUnionPipe<T1, T2, T3>() => base.ToListPipe<(TModel, T1, T2, T3)>();
//		public TModel ToOne() => base.ToOne<TModel>();
//		public TSQL ToOnePipe() => ToOnePipe<TModel>();
//		public List<TModel> ToList() => ToList<TModel>();
//		public TSQL ToListPipe() => ToListPipe<TModel>();
//		public TSQL Abc(TModel model)
//		{
//			return this as TSQL;
//		}
//		#endregion

//		#region Protected Method
//		protected static int SetRedisCache<T>(string key, T model, int timeout, Func<int> func)
//		{
//			if (func == null)
//				throw new ArgumentNullException(nameof(func));
//			if (timeout == 0) return func.Invoke();
//			RedisHelper.Set(key, model, timeout);
//			int affrows;
//			try { affrows = func.Invoke(); }
//			catch (Exception ex)
//			{
//				RedisHelper.Del(key);
//				throw ex;
//			}
//			if (affrows == 0) RedisHelper.Del(key);
//			return affrows;
//		}
//		protected static T GetRedisCache<T>(string key, int timeout, Func<T> select)
//		{
//			if (select == null)
//				throw new ArgumentNullException(nameof(select));
//			if (timeout == 0) return select.Invoke();
//			var info = RedisHelper.Get<T>(key);
//			if (info == null)
//			{
//				info = select.Invoke();
//				RedisHelper.Set(key, info, timeout);
//			}
//			return info;
//		}
//		#endregion

//	}

//}

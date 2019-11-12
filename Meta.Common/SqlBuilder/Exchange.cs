using Meta.Common.Model;
using System;
using System.Collections.Generic;
using System.Text;


namespace Meta.Common.SqlBuilder
{
	/// <summary>
	/// Select
	/// </summary>
	/// <typeparam name="TDAL"></typeparam>
	/// <typeparam name="TModel"></typeparam>
	public class SelectExchange<TDAL, TModel> : SelectBuilder<TDAL> where TDAL : class, new()
	{
		public SelectExchange(string fields, string alias) : base(fields, alias) => Mapping(true);
		public SelectExchange(string fields) : base(fields) => Mapping(true);
		public SelectExchange() => Mapping();
		private void Mapping(bool hasField = false)
		{
			MainTable = MappingHelper.GetMapping<TModel>();
			if (!hasField)
				Fields = EntityHelper.GetAllSelectFieldsString<TModel>(MainAlias);
		}

		public TModel ToOne() => ToOne<TModel>();
		public TDAL ToOnePipe() => ToOnePipe<TModel>();
		public List<TModel> ToList() => ToList<TModel>();
		public TDAL ToListPipe() => ToListPipe<TModel>();
	}

}

using System;
using System.Collections.Generic;
using System.Text;

namespace DBHelper
{
	public abstract class SelectExchange<TDAL, TModel> : SelectBuilder<TDAL> where TDAL : class, new()
	{
		public SelectExchange()
		{
			Type type = typeof(TModel);
			_mainTable = MappingHelper.GetMapping(type);
			_fields = EntityHelper.GetAllSelectFieldsString(type, "a");
		}
		public TModel ToOne() => ToOne<TModel>();
		public List<TModel> ToList() => ToList<TModel>();
	}
	public class UpdateExchange<TDAL, TModel> : UpdateBuilder<TDAL> where TDAL : class, new()
	{
		public UpdateExchange()
		{
			Type type = typeof(TModel);
			_mainTable = MappingHelper.GetMapping(type);
			_fields = EntityHelper.GetAllSelectFieldsString(type, "a");
		}

	}
}

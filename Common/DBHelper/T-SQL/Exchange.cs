using System;
using System.Collections.Generic;
using System.Text;

namespace DBHelper
{
	public abstract class SelectExchange<TDAL, TModel> : SelectBuilder<TDAL> where TDAL : class, new()
	{
		public SelectExchange(string fields, string alias) : base(fields, alias) => Mapping();
		public SelectExchange(string fields) : base(fields) => Mapping();
		public SelectExchange() => Mapping(true);
		private void Mapping(bool hasField = false)
		{
			Type type = typeof(TModel);
			_mainTable = MappingHelper.GetMapping(type);
			if (!hasField)
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

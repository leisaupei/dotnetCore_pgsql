using System;
using System.Collections.Generic;
using System.Text;

namespace DBHelper
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
			Type type = typeof(TModel);
			_mainTable = MappingHelper.GetMapping(type);
			if (!hasField)
				_fields = EntityHelper.GetAllSelectFieldsString(type, "a");
		}

		public TModel ToOne() => ToOne<TModel>();
		public List<TModel> ToList() => ToList<TModel>();
	}
	/// <summary>
	/// Update
	/// </summary>
	/// <typeparam name="TDAL"></typeparam>
	/// <typeparam name="TModel"></typeparam>
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

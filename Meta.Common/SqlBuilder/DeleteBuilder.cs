using System;
using System.Collections.Generic;
using System.Text;
using Meta.Common.DbHelper;
using Meta.Common.Interface;

namespace Meta.Common.SqlBuilder
{
	public class DeleteBuilder<TModel> : WhereBuilder<DeleteBuilder<TModel>, TModel> where TModel : IDbModel, new()
	{
		public DeleteBuilder() => MainTable = EntityHelper.GetTableName<TModel>();
		public new int ToRows() => base.ToRows();

		#region Override
		public override string ToString() => base.ToString();
		public override string GetCommandTextString()
		{
			if (WhereList.Count == 0)
				throw new ArgumentNullException(nameof(WhereList));
			return $"DELETE FROM {MainTable} {MainAlias} WHERE {string.Join("\nAND", WhereList)}";
		}
		#endregion
	}
}

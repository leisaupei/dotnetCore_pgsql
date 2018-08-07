using System;
using System.Collections.Generic;
using System.Text;

namespace DBHelper
{
	public class DeleteBuilder : WhereBase<DeleteBuilder>
	{
		/// <summary>
		/// 初始化Table
		/// </summary>
		/// <param name="table"></param>
		public DeleteBuilder(string table) : base(table) { }
		public DeleteBuilder(string table, string alias) : base(table, alias) { }
		public DeleteBuilder() { }
		public int Commit() => ToRows();

		#region Override
		public override string ToString() => base.ToString();
		protected override string SetCommandString()
		{
			if (_where.Count < 1) throw new ArgumentException("delete语句必须带where条件");
			return $"DELETE FROM {_mainTable} {_mainAlias} WHERE {_where.Join("\nAND")}";
		}
		#endregion
	}
}

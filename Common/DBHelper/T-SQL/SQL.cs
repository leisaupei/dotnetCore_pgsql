using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace DBHelper
{
	public class SQL
	{
		public static SelectSQL Select() => new SelectSQL();
		public static SelectSQL Select(string fields) => new SelectSQL(fields);
		public static InsertSQL Insert() => new InsertSQL();
		public static InsertSQL Insert(string table) => new InsertSQL(table);
		public static DeleteSQL Delete() => new DeleteSQL();
		public static DeleteSQL Delete(string table) => new DeleteSQL(table);
		public static UpdateSQL Update() => new UpdateSQL();
		public static UpdateSQL Update(string table) => new UpdateSQL(table);
	}
	public class SelectSQL : SelectBuilder<SelectSQL>
	{
		public SelectSQL() { }
		public SelectSQL(string fields) : base(fields) { }
	}
	public class InsertSQL : InsertBuilder
	{
		public InsertSQL() { }
		public InsertSQL(string table) : base(table) { }
	}
	public class DeleteSQL : DeleteBuilder
	{
		public DeleteSQL() { }
		public DeleteSQL(string table) : base(table) { }
		public DeleteSQL(string table, string alias) : base(table, alias) { }
	}
	public class UpdateSQL : UpdateBuilder<UpdateSQL>
	{
		public UpdateSQL() { }
		public UpdateSQL(string table) : base(table) { }
		public UpdateSQL(string table, string alias) : base(table, alias) { }
	}
}

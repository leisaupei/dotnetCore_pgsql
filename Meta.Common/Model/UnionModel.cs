using Meta.Common.DbHelper;
using Meta.Common.Extensions;
using Npgsql;
using NpgsqlTypes;
using System;
using System.Linq;
using System.Reflection;

namespace Meta.Common.Model
{
	/// <summary>
	/// 联表实体类
	/// </summary>
	internal class UnionModel
	{
		/// <summary>
		/// 别名
		/// </summary>
		public string AliasName { get; set; }
		/// <summary>
		/// 标明
		/// </summary>
		public string Table { get; set; }
		/// <summary>
		/// on表达式
		/// </summary>
		public string Expression { get; set; }
		/// <summary>
		/// 联表类型
		/// </summary>
		private UnionEnum UnionType { get; set; }
		public bool IsReturn { get; set; }
		public string UnionTypeString => UnionType.ToString().Replace("_", " ");


		public string Fields { get; set; }
		protected UnionModel() { }
		public UnionModel(string aliasName, string table, string expression, UnionEnum unionType) : this(aliasName, table, expression, unionType, aliasName + ".*") { }
		public UnionModel(string aliasName, string table, string expression, UnionEnum unionType, string fields)
		{
			AliasName = aliasName;
			Table = table;
			Expression = expression;
			UnionType = unionType;
			Fields = fields;
		}
		public static UnionModel Create<T>(string aliasName, string expression, UnionEnum unionType, bool isReturn)
		{
			var info = new UnionModel
			{
				AliasName = aliasName,
				Table = EntityHelper.GetTableName<T>(),
				Expression = expression,
				UnionType = unionType,
				IsReturn = isReturn,
			};
			if (isReturn)
			{
				info.Fields = EntityHelper.GetDALTypeFieldsString<T>(aliasName);
			}
			return info;
		}
	}


	public partial class NpgsqlNameTranslator : INpgsqlNameTranslator
	{
		public string TranslateMemberName(string clrName) => clrName;
		public string TranslateTypeName(string clrName) => clrName;
	}
}


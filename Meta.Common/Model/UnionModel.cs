using Meta.Common.DbHelper;
using Meta.Common.Extensions;
using Meta.Common.Interface;
using Meta.Common.SqlBuilder.AnalysisExpression;
using Npgsql;
using NpgsqlTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Meta.Common.Model
{
	/// <summary>
	/// 联表实体类
	/// </summary>
	internal class UnionCollection
	{
		public List<UnionModel> List { get; } = new List<UnionModel>();
		private readonly string _mainAlias;
		public UnionCollection(string mainAlias)
		{
			_mainAlias = mainAlias;
		}
		public UnionCollection() { }

		public void Add<TSource, TTarget>(Expression<Func<TSource, TTarget, bool>> predicate, UnionEnum unionType, bool isReturn)
			where TSource : IDbModel, new() where TTarget : IDbModel, new()
		{
			var model = SqlExpressionVisitor.Instance.VisitUnion(predicate, List.Select(f => f.AliasName).Append(_mainAlias));
			var info = new UnionModel
			{
				AliasName = model.UnionAlias,
				Table = EntityHelper.GetTableName(model.UnionType),
				Expression = model.SqlText,
				UnionType = unionType,
				IsReturn = isReturn,
			};
			if (isReturn)
			{
				info.Fields = EntityHelper.GetModelTypeFieldsString(model.UnionAlias, model.UnionType);
			}
			List.Add(info);
		}
		public void Add(UnionModel info)
		{
			List.Add(info);
		}
	}

	internal class UnionModel
	{
		public UnionModel() { }
		public UnionModel(string aliasName, string table, string expression, UnionEnum unionType) : this(aliasName, table, expression, unionType, aliasName + ".*") { }
		public UnionModel(string aliasName, string table, string expression, UnionEnum unionType, string fields)
		{
			AliasName = aliasName;
			Table = table;
			Expression = expression;
			UnionType = unionType;
			Fields = fields;
		}
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
		public UnionEnum UnionType { get; set; }
		public bool IsReturn { get; set; }
		public string UnionTypeString => UnionType.ToString().Replace("_", " ");


		public string Fields { get; set; }
	}

	
}


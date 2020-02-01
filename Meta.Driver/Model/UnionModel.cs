using Meta.Driver.DbHelper;
using Meta.Driver.Extensions;
using Meta.Driver.Interface;
using Meta.Driver.SqlBuilder.AnalysisExpression;
using Npgsql;
using NpgsqlTypes;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Meta.Driver.Model
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

		public List<DbParameter> Add<TSource, TTarget>(Expression<Func<TSource, TTarget, bool>> predicate, UnionEnum unionType, bool isReturn)
			where TSource : IDbModel, new() where TTarget : IDbModel, new()
		{
			var model = SqlExpressionVisitor.Instance.VisitUnion(predicate, List.Select(f => f.AliasName).Append(_mainAlias));
			var info = new UnionModel(model.Alias, EntityHelper.GetTableName(model.UnionType), model.SqlText, unionType, isReturn);
			if (info.IsReturn)
				info.Fields = EntityHelper.GetModelTypeFieldsString(model.Alias, model.UnionType);
			List.Add(info);
			return model.Paras;
		}
		public void Add<TTarget>(UnionEnum unionType, string aliasName, string on, bool isReturn = false) where TTarget : IDbModel, new()
		{
			var info = new UnionModel(aliasName, EntityHelper.GetTableName<TTarget>(), on, unionType, isReturn);
			if (info.IsReturn)
				info.Fields = EntityHelper.GetModelTypeFieldsString<TTarget>(aliasName);
			List.Add(info);
		}
	}

	internal class UnionModel
	{
		public UnionModel(string aliasName, string table, string expression, UnionEnum unionType, bool isReturn) : this(aliasName, table, expression, unionType, isReturn, aliasName + ".*") { }
		public UnionModel(string aliasName, string table, string expression, UnionEnum unionType, bool isReturn, string fields)
		{
			AliasName = aliasName;
			Table = table;
			Expression = expression;
			UnionType = unionType;
			IsReturn = isReturn;
			if (IsReturn) { Fields = fields; }
		}
		/// <summary> 
		/// 别名
		/// </summary>
		public string AliasName { get; }
		/// <summary>
		/// 标明
		/// </summary>
		public string Table { get; }
		/// <summary>
		/// on表达式
		/// </summary>
		public string Expression { get; }
		/// <summary>
		/// 联表类型
		/// </summary>
		public UnionEnum UnionType { get; }
		/// <summary>
		/// 是否添加返回字段
		/// </summary>
		public bool IsReturn { get; }
		public string UnionTypeString => UnionType.ToString().Replace("_", " ");
		/// <summary>
		/// 字段
		/// </summary>
		public string Fields { get; set; }
	}


}


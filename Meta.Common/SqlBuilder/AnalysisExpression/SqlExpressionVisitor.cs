using Meta.Common.Model;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace Meta.Common.SqlBuilder.AnalysisExpression
{
	public class SqlExpressionVisitor : ExpressionVisitor
	{
		private static SqlExpressionVisitor _instance;
		public static SqlExpressionVisitor Instance
		{
			get
			{
				if (_instance == null)
					_instance = new SqlExpressionVisitor();
				return _instance;
			}
		}
		private SqlExpressionModel _exp;
		private string[] _currentAlias;
		private ExpressionExcutionType _type = ExpressionExcutionType.None;
		/// <summary>
		/// 表达式操作符转换集
		/// </summary>
		protected static Dictionary<ExpressionType, string> OPERATOR_SET = new Dictionary<ExpressionType, string>()
		{
			{ ExpressionType.And," & "},
			{ ExpressionType.AndAlso," AND "},
			{ ExpressionType.Equal," = "},
			{ ExpressionType.GreaterThan," > "},
			{ ExpressionType.GreaterThanOrEqual," >= "},
			{ ExpressionType.LessThan," < "},
			{ ExpressionType.LessThanOrEqual," <= "},
			{ ExpressionType.NotEqual," <> "},
			{ ExpressionType.OrElse," OR "},
			{ ExpressionType.Or," | "},
			{ ExpressionType.Add," + "},
			{ ExpressionType.Subtract," - "},
			{ ExpressionType.Divide," / "},
			{ ExpressionType.Multiply," * "},
			{ ExpressionType.Not," NOT "}

		};

		public SqlExpressionVisitor()
		{
		}
		public SqlExpressionModel VisitSingleUpdate(Expression node)
		{
			_type = ExpressionExcutionType.SingleUpdate;
			Visit(node);
			return _exp;
		}
		public SqlExpressionModel VisitSingle(Expression node)
		{
			_type = ExpressionExcutionType.Single;
			Visit(node);
			return _exp;
		}
		public SqlExpressionModel VisitUnion(Expression node, IEnumerable<string> currentAlias)
		{
			this._currentAlias = currentAlias.ToArray();
			_type = ExpressionExcutionType.Union;
			Visit(node);
			return _exp;
		}
		public Expression VisitWhere(Expression node)
		{
			_type = ExpressionExcutionType.Where;
			return Visit(node);
		}
		public new Expression Visit(Expression node)
		{
			_exp = new SqlExpressionModel();
			return base.Visit(node);
		}
		protected override Expression VisitBinary(BinaryExpression node)
		{
			_exp.SqlText += "(";
			base.Visit(node.Left);
			if (OPERATOR_SET.TryGetValue(node.NodeType, out string operat))
				_exp.SqlText += operat;
			else
				_exp.SqlText += node.NodeType.ToString();
			base.Visit(node.Right);
			_exp.SqlText += ")";
			return node;
		}

		protected override Expression VisitBlock(BlockExpression node)
		{
			return base.VisitBlock(node);
		}

		protected override CatchBlock VisitCatchBlock(CatchBlock node)
		{
			return base.VisitCatchBlock(node);
		}

		protected override Expression VisitConditional(ConditionalExpression node)
		{
			return base.VisitConditional(node);
		}

		protected override Expression VisitConstant(ConstantExpression node)
		{
			return base.VisitConstant(node);
		}

		protected override Expression VisitDebugInfo(DebugInfoExpression node)
		{
			return base.VisitDebugInfo(node);
		}

		protected override Expression VisitDefault(DefaultExpression node)
		{
			return base.VisitDefault(node);
		}

		protected override Expression VisitDynamic(DynamicExpression node)
		{
			return base.VisitDynamic(node);
		}

		protected override ElementInit VisitElementInit(ElementInit node)
		{
			return base.VisitElementInit(node);
		}

		protected override Expression VisitExtension(Expression node)
		{
			return base.VisitExtension(node);
		}

		protected override Expression VisitGoto(GotoExpression node)
		{
			return base.VisitGoto(node);
		}

		protected override Expression VisitIndex(IndexExpression node)
		{
			return base.VisitIndex(node);
		}

		protected override Expression VisitInvocation(InvocationExpression node)
		{
			return base.VisitInvocation(node);
		}

		protected override Expression VisitLabel(LabelExpression node)
		{
			return base.VisitLabel(node);
		}

		protected override LabelTarget VisitLabelTarget(LabelTarget node)
		{
			return base.VisitLabelTarget(node);
		}

		protected override Expression VisitLambda<T>(Expression<T> node)
		{
			return base.VisitLambda(node);
		}

		protected override Expression VisitListInit(ListInitExpression node)
		{
			return base.VisitListInit(node);
		}

		protected override Expression VisitLoop(LoopExpression node)
		{
			return base.VisitLoop(node);
		}

		protected override Expression VisitMember(MemberExpression node)
		{
			if (_type == ExpressionExcutionType.SingleUpdate)
				_exp.SqlText += node.Member.Name.ToLower();
			else
				_exp.SqlText += node.ToString().ToLower();
			return node;
		}

		protected override MemberAssignment VisitMemberAssignment(MemberAssignment node)
		{
			return base.VisitMemberAssignment(node);
		}

		protected override MemberBinding VisitMemberBinding(MemberBinding node)
		{
			return base.VisitMemberBinding(node);
		}

		protected override Expression VisitMemberInit(MemberInitExpression node)
		{
			return base.VisitMemberInit(node);
		}

		protected override MemberListBinding VisitMemberListBinding(MemberListBinding node)
		{
			return base.VisitMemberListBinding(node);
		}

		protected override MemberMemberBinding VisitMemberMemberBinding(MemberMemberBinding node)
		{
			return base.VisitMemberMemberBinding(node);
		}

		protected override Expression VisitMethodCall(MethodCallExpression node)
		{
			return base.VisitMethodCall(node);
		}

		protected override Expression VisitNew(NewExpression node)
		{
			return base.VisitNew(node);
		}

		protected override Expression VisitNewArray(NewArrayExpression node)
		{
			return base.VisitNewArray(node);
		}

		protected override Expression VisitParameter(ParameterExpression node)
		{
			if (_type == ExpressionExcutionType.Single)
				_exp.UnionAlias = node.Name;
			if (_type == ExpressionExcutionType.Union && !_currentAlias.Contains(node.Name))
			{
				_exp.UnionAlias = node.Name;
				_exp.UnionType = node.Type;
			}
			return node;
		}

		protected override Expression VisitRuntimeVariables(RuntimeVariablesExpression node)
		{
			return base.VisitRuntimeVariables(node);
		}

		protected override Expression VisitSwitch(SwitchExpression node)
		{
			return base.VisitSwitch(node);
		}

		protected override SwitchCase VisitSwitchCase(SwitchCase node)
		{
			return base.VisitSwitchCase(node);
		}

		protected override Expression VisitTry(TryExpression node)
		{
			return base.VisitTry(node);
		}

		protected override Expression VisitTypeBinary(TypeBinaryExpression node)
		{
			return base.VisitTypeBinary(node);
		}

		protected override Expression VisitUnary(UnaryExpression node)
		{
			return base.VisitUnary(node);
		}
	}
}

using Meta.Driver.DbHelper;
using Meta.Driver.Model;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.Common;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;

namespace Meta.Driver.SqlBuilder.AnalysisExpression
{
	public class SqlExpressionVisitor : ExpressionVisitor
	{
		private SqlExpressionVisitor() { }
		private static SqlExpressionVisitor _instance;
		/// <summary>
		/// Visitor静态实例
		/// </summary>
		public static SqlExpressionVisitor Instance
		{
			get
			{
				if (_instance == null)
					_instance = new SqlExpressionVisitor();
				return _instance;
			}
		}
		/// <summary>
		/// 输出对象
		/// </summary>
		private SqlExpressionModel _exp;
		/// <summary>
		/// 关联表已拥有别名
		/// </summary>
		private string[] _currentAlias;
		/// <summary>
		/// 输入解析类型
		/// </summary>
		private ExpressionExcutionType _type = ExpressionExcutionType.None;
		/// <summary>
		/// 当前lambda表达式的操作符
		/// </summary>
		private ExpressionType? _currentLambdaNodeType;

		/// <summary>
		/// String.Contains
		/// </summary>
		private string _methodStringContainsFormat = null;

		/// <summary>
		/// 表达式操作符转换集
		/// </summary>
		private readonly static Dictionary<ExpressionType, string> _dictOperator = new Dictionary<ExpressionType, string>()
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
		/// <summary>
		/// 判断是否需要添加括号
		/// </summary>
		/// <param name="nodeType"></param>
		/// <returns></returns>
		private bool IsAddBrackets(ExpressionType nodeType) => nodeType == ExpressionType.AndAlso || nodeType == ExpressionType.OrElse;

		/// <summary>
		/// 访问单个无别名字段
		/// </summary>
		/// <param name="node"></param>
		/// <returns></returns>
		public SqlExpressionModel VisitSingleForNoAlias(Expression node)
		{
			Initialize(ExpressionExcutionType.SingleForNoAlias);
			base.Visit(node);
			return _exp;
		}

		/// <summary>
		/// 访问单个有别名字段
		/// </summary>
		/// <param name="node"></param>
		/// <returns></returns>
		public SqlExpressionModel VisitSingle(Expression node)
		{
			Initialize(ExpressionExcutionType.Single);
			base.Visit(node);
			return _exp;
		}

		/// <summary>
		/// 访问关联查询表达式
		/// </summary>
		/// <param name="node"></param>
		/// <param name="currentAlias"></param>
		/// <returns></returns>
		public SqlExpressionModel VisitUnion(Expression node, IEnumerable<string> currentAlias)
		{
			Initialize(ExpressionExcutionType.Union);
			_currentAlias = currentAlias.ToArray();
			base.Visit(node);
			return _exp;
		}

		/// <summary>
		/// 访问条件表达式
		/// </summary>
		/// <param name="node"></param>
		/// <returns></returns>
		public SqlExpressionModel VisitCondition(Expression node)
		{
			Initialize(ExpressionExcutionType.Condition);
			base.Visit(node);
			return _exp;
		}

		/// <summary>
		/// 初始化
		/// </summary>
		/// <param name="type"></param>
		private void Initialize(ExpressionExcutionType type)
		{
			_type = type;
			_exp = new SqlExpressionModel();
			_methodStringContainsFormat = null;
			_currentLambdaNodeType = null;
		}

		#region OverrideVisitMethod
		protected override Expression VisitBinary(BinaryExpression node)
		{
			if (node.NodeType == ExpressionType.ArrayIndex)
			{
				if (node.Left is MemberExpression exp)
				{
					VisitMember(exp);
					_exp.SqlText += string.Concat("[", GetExpressionInvokeResult<int>(node.Right) + 1, "]");
				}
				else
					SetMemberValue(node, node.Type);

				return node;
			}
			_currentLambdaNodeType = node.NodeType;
			if (IsAddBrackets(node.NodeType))
				_exp.SqlText += "(";
			base.Visit(node.Left);
			if (_dictOperator.TryGetValue(node.NodeType, out string operat))
				_exp.SqlText += operat;
			else
				_exp.SqlText += node.NodeType.ToString();
			base.Visit(node.Right);
			if (IsAddBrackets(node.NodeType))
				_exp.SqlText += ")";
			return node;
		}

		protected override Expression VisitConstant(ConstantExpression node)
		{
			if (!ChecktAndSetNullValue(node.Value))
				SetParameter(node.Value);
			return base.VisitConstant(node);
		}

		protected override Expression VisitInvocation(InvocationExpression node)
		{
			SetMemberValue(node, node.Type);
			return node;
		}

		protected override Expression VisitLambda<T>(Expression<T> node)
		{
			return base.VisitLambda(node);
		}

		protected override Expression VisitMember(MemberExpression node)
		{
			switch (_type)
			{
				case ExpressionExcutionType.Union:
				case ExpressionExcutionType.Condition:
					switch (node.Member)
					{
						case FieldInfo fieldInfo:
							SetMemberValue(node, fieldInfo.FieldType);
							break;
						case PropertyInfo propertyInfo:
							if (IsDbMember(node, out MemberExpression dbMember))
								_exp.SqlText += dbMember.ToString().ToLower();
							else SetMemberValue(node, propertyInfo.PropertyType);
							break;
						default:
							_exp.SqlText += node.ToString().ToLower();
							break;
					}
					break;
				case ExpressionExcutionType.SingleForNoAlias:
					_exp.SqlText += node.Member.Name.ToLower();
					break;
				default:
					_exp.SqlText += node.ToString().ToLower();
					break;
			}

			return node;
		}

		protected override Expression VisitMethodCall(MethodCallExpression node)
		{
			switch (node.Method.Name)
			{
				case "Contains":
					if (!StringLikeCalling(node, "%{0}%"))
						MethodContaionsHandler(node);
					break;
				case "StartsWith":
					StringLikeCalling(node, "{0}%");
					break;
				case "EndsWith":
					StringLikeCalling(node, "%{0}");
					break;
				case "ToString" when node.Object.NodeType == ExpressionType.MemberAccess && IsDbMember(node.Object as MemberExpression, out MemberExpression dbMember):
					_exp.SqlText += string.Concat(dbMember.ToString().ToLower(), "::text");
					break;
				default:
					SetExpressionInvokeResultParameter(node);
					break;
			}
			return node;
		}
		protected override Expression VisitConditional(ConditionalExpression node)
		{
			SetExpressionInvokeResultParameter(node);
			return node;
		}


		protected override Expression VisitNew(NewExpression node)
		{
			SetMemberValue(node, node.Type);
			return node;
		}

		protected override Expression VisitNewArray(NewArrayExpression node)
		{
			SetMemberValue(node, node.Type);
			return node;
		}

		protected override Expression VisitParameter(ParameterExpression node)
		{
			switch (_type)
			{
				case ExpressionExcutionType.Single:
					_exp.Alias = node.Name;
					break;
				case ExpressionExcutionType.Union when !_currentAlias.Contains(node.Name):
					_exp.Alias = node.Name;
					_exp.UnionType = node.Type;
					break;
			}
			return node;
		}

		protected override Expression VisitUnary(UnaryExpression node)
		{
			if (node.NodeType == ExpressionType.ArrayLength)
			{
				_exp.SqlText += "array_length(";
				base.VisitUnary(node);
				_exp.SqlText += ", 1)";
				return node;
			}
			if (node.NodeType == ExpressionType.Not)
				_currentLambdaNodeType = node.NodeType;
			return base.VisitUnary(node);
		}

		#region Not Supported Temporarily
		protected override Expression VisitBlock(BlockExpression node)
		{
			throw new NotSupportedException(nameof(VisitBlock));
		}


		protected override CatchBlock VisitCatchBlock(CatchBlock node)
		{
			throw new NotSupportedException(nameof(VisitCatchBlock));
		}



		protected override Expression VisitDebugInfo(DebugInfoExpression node)
		{
			throw new NotSupportedException(nameof(VisitDebugInfo));
		}

		protected override Expression VisitDefault(DefaultExpression node)
		{
			throw new NotSupportedException(nameof(VisitDefault));
		}

		protected override Expression VisitDynamic(DynamicExpression node)
		{
			throw new NotSupportedException(nameof(VisitDynamic));
		}

		protected override ElementInit VisitElementInit(ElementInit node)
		{
			throw new NotSupportedException(nameof(VisitElementInit));
		}

		protected override Expression VisitExtension(Expression node)
		{
			throw new NotSupportedException(nameof(VisitExtension));
		}

		protected override Expression VisitGoto(GotoExpression node)
		{
			throw new NotSupportedException(nameof(VisitGoto));
		}

		protected override Expression VisitIndex(IndexExpression node)
		{
			throw new NotSupportedException(nameof(VisitIndex));
		}

		protected override Expression VisitLabel(LabelExpression node)
		{
			throw new NotSupportedException(nameof(VisitLabel));
		}

		protected override LabelTarget VisitLabelTarget(LabelTarget node)
		{
			throw new NotSupportedException(nameof(VisitLabelTarget));
		}

		protected override Expression VisitListInit(ListInitExpression node)
		{
			throw new NotSupportedException(nameof(VisitListInit));
		}

		protected override Expression VisitLoop(LoopExpression node)
		{
			throw new NotSupportedException(nameof(VisitLoop));
		}

		protected override MemberAssignment VisitMemberAssignment(MemberAssignment node)
		{
			throw new NotSupportedException(nameof(VisitMemberAssignment));
		}

		protected override MemberBinding VisitMemberBinding(MemberBinding node)
		{
			throw new NotSupportedException(nameof(VisitMemberBinding));
		}

		protected override Expression VisitMemberInit(MemberInitExpression node)
		{
			throw new NotSupportedException(nameof(VisitMemberInit));
		}

		protected override MemberListBinding VisitMemberListBinding(MemberListBinding node)
		{
			throw new NotSupportedException(nameof(VisitMemberListBinding));
		}

		protected override MemberMemberBinding VisitMemberMemberBinding(MemberMemberBinding node)
		{
			throw new NotSupportedException(nameof(VisitMemberMemberBinding));
		}

		protected override Expression VisitRuntimeVariables(RuntimeVariablesExpression node)
		{
			throw new NotSupportedException(nameof(VisitRuntimeVariables));
		}

		protected override Expression VisitSwitch(SwitchExpression node)
		{
			throw new NotSupportedException(nameof(VisitSwitch));
		}

		protected override SwitchCase VisitSwitchCase(SwitchCase node)
		{
			throw new NotSupportedException(nameof(VisitSwitchCase));
		}

		protected override Expression VisitTry(TryExpression node)
		{
			throw new NotSupportedException(nameof(VisitTry));
		}

		protected override Expression VisitTypeBinary(TypeBinaryExpression node)
		{
			throw new NotSupportedException(nameof(VisitTypeBinary));
		}
		#endregion

		#endregion

		#region Private Method
		private void SetExpressionInvokeResultParameter(Expression node)
		{
			var value = GetExpressionInvokeResultObject(node);
			if (!ChecktAndSetNullValue(value))
				SetParameter(value);
		}
		private void MethodContaionsHandler(MethodCallExpression node)
		{
			int support = 0;
			void AddOperator() => _exp.SqlText += _currentLambdaNodeType == ExpressionType.Not ? " <> " : " = ";
			bool AnalysisDbField(List<Expression> expression, int i)
			{
				if (expression[i] is MemberExpression me && me.Expression != null)
				{
					if (i == 0)
					{
						i++;
						AnalysisDbField(expression, i);
						AddOperator();
					}
					var isArray = me.Type.IsArray || me.Type.FullName.StartsWith("System.Collections.Generic.List`1");
					if (isArray) _exp.SqlText += _currentLambdaNodeType == ExpressionType.Not ? "ALL(" : "ANY(";

					VisitMember(me);
					if (isArray) { _exp.SqlText += ")"; support++; };
					return true;
				}
				base.Visit(expression[i]);
				return false;
			}
			var argList = node.Arguments.ToList();
			if (node.Object != null)
				argList.Insert(0, node.Object);
			if (!AnalysisDbField(argList, 0))
			{
				AddOperator();
				AnalysisDbField(argList, 1);
			}
			if (support == 0)
				throw new NotSupportedException("Contains method property only supported 'new T[]' and 'list<T>'");

		}
		/// <summary>
		/// string.contains/startswith/endswith
		/// </summary>
		/// <param name="node"></param>
		/// <param name="key"></param>
		/// <returns></returns>
		private bool StringLikeCalling(MethodCallExpression node, string key)
		{
			if (node.Object == null) return false;
			if (node.Object.Type == typeof(string))
			{
				_exp.SqlText += node.Object.ToString().ToLower();
				_exp.SqlText += _currentLambdaNodeType == ExpressionType.Not ? " NOT" : string.Empty;
				if (node.Arguments.Count == 2 && node.Arguments[1].ToString().EndsWith("IgnoreCase"))
					_exp.SqlText += " ILIKE ";
				else
					_exp.SqlText += " LIKE ";

				_methodStringContainsFormat = key;
				base.Visit(node.Arguments[0]);
				return true;
			}
			return false;
		}
		/// <summary>
		/// 检查并设置null值
		/// </summary>
		/// <param name="value"></param>
		/// <returns></returns>
		private bool ChecktAndSetNullValue(object value)
		{
			if (value != null) return false;
			if (_currentLambdaNodeType == ExpressionType.Equal)
				_exp.SqlText = string.Concat(_exp.SqlText.Trim().TrimEnd('='), "IS NULL");
			else if (_currentLambdaNodeType == ExpressionType.NotEqual)
				_exp.SqlText = string.Concat(_exp.SqlText.Trim().TrimEnd('='), "IS NOT NULL");
			return true;
		}
		/// <summary>
		/// 设置成员的值
		/// </summary>
		/// <param name="expression"></param>
		/// <param name="convertType"></param>
		private void SetMemberValue(Expression expression, Type convertType)
		{
			var obj = GetExpressionInvokeResultObject(expression);
			if (ChecktAndSetNullValue(obj)) return;
			var value = Convert.ChangeType(obj, convertType);
			SetParameter(value);
		}
		/// <summary>
		/// 输出表达式的值
		/// </summary>
		/// <param name="expression"></param>
		/// <returns></returns>
		private object GetExpressionInvokeResultObject(Expression expression)
		{
			return Expression.Lambda(expression).Compile().DynamicInvoke();
		}
		/// <summary>
		/// 输出表达式的值
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="expression"></param>
		/// <returns></returns>
		private T GetExpressionInvokeResult<T>(Expression expression)
		{
			return Expression.Lambda<Func<T>>(Expression.Convert(expression, typeof(T))).Compile().Invoke();
		}
		/// <summary>
		/// 设置参数
		/// </summary>
		/// <param name="value"></param>
		private void SetParameter(object value)
		{
			var index = EntityHelper.ParamsIndex;
			if (_methodStringContainsFormat != null)
				value = string.Format(_methodStringContainsFormat, value);
			_exp.Paras.Add(new NpgsqlParameter(index, value));
			_exp.SqlText += string.Concat("@", index);
		}
		/// <summary>
		/// 递归member表达式, 针对optional字段, 从 a.xxx.Value->a.xxx
		/// </summary>
		/// <param name="node"></param>
		/// <returns></returns>
		private MemberExpression MemberVisitor(MemberExpression node)
		{
			if (node.NodeType == ExpressionType.MemberAccess && node.Expression is MemberExpression me)
				return MemberVisitor(me);
			return node;
		}
		/// <summary>
		/// 是否数据库成员
		/// </summary>
		/// <param name="node"></param>
		/// <param name="dbMember">a.xxx成员</param>
		/// <returns></returns>
		private bool IsDbMember(MemberExpression node, out MemberExpression dbMember)
		{
			dbMember = MemberVisitor(node);
			return dbMember.Expression != null && dbMember.Expression.NodeType == ExpressionType.Parameter;
		}
		#endregion
	}
}

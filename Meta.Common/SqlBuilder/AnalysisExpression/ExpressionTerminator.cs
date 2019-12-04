using Npgsql;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

namespace Meta.Common.SqlBuilder.AnalysisExpression
{
	internal class ExpressionTerminator
	{
		readonly LambdaExpression _lambda;
		public string SqlString { get; private set; }
		public NpgsqlParameter Paras { get; set; }
		public ExpressionTerminator()
		{
		}
		public ExpressionTerminator(LambdaExpression lambda)
		{
			_lambda = lambda;
		}
		public string GetResult()
		{
			GetExpressionType(_lambda);
			return SqlString;
		}
		public void GetExpressionType(Expression exp)
		{
			switch (exp)
			{
				case UnaryExpression body:
					GetExpressionType(body.Operand);
					break;
				case LambdaExpression body:
					GetExpressionType(body.Body);
					break;
				case MemberExpression body:
					GetExpressionType(body.Expression);
					SqlString += body.Member.Name.ToLowerInvariant();
					break;
				case ParameterExpression body:
					SqlString += string.Concat(body.Name, ".");
					break;
				default:
					return;
			}
		}
	}
}

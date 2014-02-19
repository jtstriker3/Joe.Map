using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace Joe.Map
{
    public class PredicateRewriter
    {
        public static Expression Rewrite(Expression exp, ParameterExpression newParameterExpression)
        {
            var param = newParameterExpression;
            var newExpression = new PredicateRewriterVisitor(param).Visit(exp);

            return newExpression;
        }

        private class PredicateRewriterVisitor : ExpressionVisitor
        {
            private readonly ParameterExpression _parameterExpression;

            public PredicateRewriterVisitor(ParameterExpression parameterExpression)
            {
                _parameterExpression = parameterExpression;
            }

            protected override Expression VisitParameter(ParameterExpression node)
            {
                if (node.Type == _parameterExpression.Type)
                    return _parameterExpression;
                else
                    return base.VisitParameter(node);
            }
        }
    }

}

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

        public static Expression Rewrite(Expression exp, System.Linq.Expressions.MemberExpression newParameterExpression)
        {
            var param = newParameterExpression;
            var newExpression = new PredicateRewriterVisitor(param).Visit(exp);

            return newExpression;
        }

        private class PredicateRewriterVisitor : ExpressionVisitor
        {
            private readonly Expression _expression;

            public PredicateRewriterVisitor(ParameterExpression parameterExpression)
            {
                _expression = parameterExpression;
            }

            public PredicateRewriterVisitor(MemberExpression memberExpression)
            {
                _expression = memberExpression;
            }

            protected override Expression VisitParameter(ParameterExpression node)
            {
                if (node.Type == _expression.Type)
                    return _expression;
                else
                    return base.VisitParameter(node);
            }

            protected override Expression VisitMember(MemberExpression node)
            {
                if (node.Type == _expression.Type)
                    return _expression;
                else
                    return base.VisitMember(node);
            }
        }
    }

}

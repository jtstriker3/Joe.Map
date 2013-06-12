using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Joe.Map
{
    class FilterBuilder
    {
        private IEnumerable<Operation> Operations { get; set; }
        private Type ViewModel { get; set; }
        private Boolean LinqToSql { get; set; }
        ParameterExpression ParameterExpression { get; set; }
        private static class FilterOperators
        {
            public const String Equal = "=";
            public const String NotEqual = "!=";
            public const String GreaterThan = ">";
            public const String LessThan = "<";
            public const String GreaterThanEqualTo = ">=";
            public const String LessThanEqualTo = "<=";
            public const String Contains = "Contains";
            public const String Null = "null";
            public const String True = "true";
            public const String False = "false";
        }
        private static class OperationsOperators
        {
            public const String And = "and";
            public const String Or = "or";
        }

        internal static Expression BuildWhereExpressions(Expression right, Type viewModel, String filterString, Boolean linqToSql)
        {
            List<ViewFilterAttribute> filterList = viewModel.GetCustomAttributes(typeof(ViewFilterAttribute), true).Union(
                viewModel.GetInterfaces().SelectMany(interphase => interphase.GetCustomAttributes(typeof(ViewFilterAttribute), true))
                ).Cast<ViewFilterAttribute>().ToList();

            if (filterList.Count > 0 || filterString != null)
                right = Expression.Call(typeof(Queryable), "AsQueryable", new[] { viewModel }, right);

            if (filterString != null)
                right = Expression.Call(typeof(Queryable), "Where", new[] { viewModel }, right, new FilterBuilder(filterString, viewModel, linqToSql).BuildWhereClause());

            foreach (ViewFilterAttribute filter in filterList)
            {
                right = Expression.Call(typeof(Queryable), "Where", new[] { viewModel }, right, new FilterBuilder(filter.Where, viewModel, linqToSql).BuildWhereClause());
            }
            return right;
        }

        private class Operation
        {
            public Operation()
            {
                Filter = new Condition();
            }
            public String Operator { get; set; }
            public Condition Filter { get; private set; }

            public class Condition
            {
                public PropertyInfo Property { get; set; }
                public Expression PropertyExpression { get; set; }
                public String Operator { get; set; }
                public String Constant { get; set; }
            }
        }

        public FilterBuilder(String filterString, Type viewModel, Boolean linqToSql)
        {
            this.ViewModel = viewModel;
            ParameterExpression = Expression.Parameter(this.ViewModel, this.ViewModel.Name);
            Operations = this.BuildOperations(filterString);
        }

        private IEnumerable<Operation> BuildOperations(String filterString)
        {
            List<String> stringOperations = filterString.Split(':').ToList();

            for (int i = 0; i < stringOperations.Count; i = i + 1)
            {
                if (stringOperations.Count >= i + 3)
                {
                    Operation opp = new Operation();
                    if (i > 0)
                    {
                        opp.Operator = stringOperations[i++];
                    }
                    Expression propEx = null;
                    Type outModel = ViewModel;
                    opp.Filter.Property = ExpressionHelpers.ParseProperty(LinqToSql, ParameterExpression, ref propEx, ref  outModel, stringOperations[i++]);
                    opp.Filter.PropertyExpression = propEx;
                    opp.Filter.Operator = stringOperations[i++];
                    opp.Filter.Constant = stringOperations[i];
                    yield return opp;
                }
                else
                    throw new Exception("Invalid Filter");
            }
        }

        public LambdaExpression BuildWhereClause()
        {
            ParameterExpression = Expression.Parameter(ViewModel, ViewModel.Name);
            Expression previousExpression = null;
            foreach (Operation opp in Operations)
            {
                Expression ex = BuildFilterExpression(opp.Filter, ParameterExpression);
                if (opp.Operator != null)
                {
                    switch (opp.Operator.ToLower())
                    {
                        case OperationsOperators.And:
                            ex = Expression.And(previousExpression, ex);
                            break;
                        case OperationsOperators.Or:
                            ex = Expression.Or(previousExpression, ex);
                            break;
                    }
                }

                previousExpression = ex;
            }
            return Expression.Lambda(previousExpression, new ParameterExpression[] { ParameterExpression });
        }

        private Expression BuildFilterExpression(Operation.Condition cond, Expression parameterExpression)
        {

            Expression ex = null;
            switch (cond.Operator)
            {
                case FilterOperators.Equal:
                    switch (cond.Constant)
                    {
                        case FilterOperators.Null:
                            ex = Expression.Equal(cond.PropertyExpression, Expression.Constant(null, cond.Property.PropertyType));
                            break;
                        case FilterOperators.False:
                        case FilterOperators.True:
                            ex = Expression.Equal(cond.PropertyExpression, Expression.Constant(Convert.ToBoolean(cond.Constant)));
                            break;
                        default:
                            ex = Expression.Equal(cond.PropertyExpression, Expression.Constant(Convert.ChangeType(cond.Constant, cond.Property.PropertyType)));
                            break;
                    }
                    break;
                case FilterOperators.NotEqual:
                    switch (cond.Constant)
                    {
                        case FilterOperators.Null:
                            ex = Expression.NotEqual(cond.PropertyExpression, Expression.Constant(null, cond.Property.PropertyType));
                            break;
                        case FilterOperators.False:
                        case FilterOperators.True:
                            ex = Expression.NotEqual(cond.PropertyExpression, Expression.Constant(Convert.ToBoolean(cond.Constant)));
                            break;
                        default:
                            ex = Expression.NotEqual(cond.PropertyExpression, Expression.Constant(Convert.ChangeType(cond.Constant, cond.Property.PropertyType)));
                            break;
                    }
                    break;
                case FilterOperators.GreaterThan:
                    ex = Expression.GreaterThan(cond.PropertyExpression, Expression.Constant(Convert.ChangeType(cond.Constant, cond.Property.PropertyType)));
                    break;
                case FilterOperators.LessThan:
                    ex = Expression.LessThan(cond.PropertyExpression, Expression.Constant(Convert.ChangeType(cond.Constant, cond.Property.PropertyType)));
                    break;
                case FilterOperators.GreaterThanEqualTo:
                    ex = Expression.GreaterThanOrEqual(cond.PropertyExpression, Expression.Constant(Convert.ChangeType(cond.Constant, cond.Property.PropertyType)));
                    break;
                case FilterOperators.LessThanEqualTo:
                    ex = Expression.LessThanOrEqual(cond.PropertyExpression, Expression.Constant(Convert.ChangeType(cond.Constant, cond.Property.PropertyType)));
                    break;
                case FilterOperators.Contains:
                    ex = Expression.Call(cond.PropertyExpression, cond.Property.PropertyType.GetMethod("Contains"), Expression.Constant(Convert.ChangeType(cond.Constant, cond.Property.PropertyType)));
                    break;


            }

            return ex;
        }

    }
}

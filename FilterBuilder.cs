using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

namespace Joe.Map
{
    public class FilterBuilder
    {
        private OperationGroup Operations { get; set; }
        private Type ViewModel { get; set; }
        private Boolean LinqToSql { get; set; }
        ParameterExpression ParameterExpression { get; set; }
        Object FilterValues { get; set; }
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

        internal static Expression BuildWhereExpressions(Expression right, Type viewModel, String filterString, Boolean linqToSql, Object filters = null)
        {
            List<ViewFilterAttribute> filterList = viewModel.GetCustomAttributes(typeof(ViewFilterAttribute), true).Union(
                viewModel.GetInterfaces().SelectMany(interphase => interphase.GetCustomAttributes(typeof(ViewFilterAttribute), true))
                ).Cast<ViewFilterAttribute>().ToList();

            if (filterList.Count > 0 || filterString != null)
                right = Expression.Call(typeof(Queryable), "AsQueryable", new[] { viewModel }, right);

            if (filterString != null)
            {
                var filterExpression = new FilterBuilder(filterString, viewModel, linqToSql, filters).BuildWhereLambda();
                if (filterExpression != null)
                    right = Expression.Call(typeof(Queryable), "Where", new[] { viewModel }, right, filterExpression);
            }

            foreach (ViewFilterAttribute filter in filterList)
            {
                var filterExpression = new FilterBuilder(filter.Where, viewModel, linqToSql, filters).BuildWhereLambda();
                if (filterExpression != null)
                    right = Expression.Call(typeof(Queryable), "Where", new[] { viewModel }, right, filterExpression);
            }
            return right;
        }

        protected class OperationGroup
        {
            public OperationGroup()
            {
                Operations = new List<Operation>();
                SubGroups = new List<OperationSubGroup>();
            }


            public List<Operation> Operations { get; set; }
            public List<OperationSubGroup> SubGroups { get; set; }
        }

        protected class OperationSubGroup : OperationGroup
        {
            public String JoinOperator { get; set; }
        }

        protected class Operation
        {
            public Operation()
            {
                Filter = new Condition();
            }
            public String Operator { get; set; }
            public Condition Filter { get; private set; }

            public class Condition
            {
                //public PropertyInfo Property { get; set; }
                public Expression PropertyExpression { get; set; }
                public String Operator { get; set; }
                public String Constant { get; set; }
            }
        }

        public FilterBuilder(String filterString, Type viewModel, Boolean linqToSql, Object filters)
        {
            LinqToSql = linqToSql;
            this.ViewModel = viewModel;
            ParameterExpression = Expression.Parameter(this.ViewModel, this.ViewModel.Name);
            //Operations = this.BuildOperations(filterString);
            FilterValues = filters;
            Operations = this.BuildOperationGroups(filterString);
        }

        private OperationGroup BuildOperationGroups(String filter, OperationGroup startGroup = null)
        {
            startGroup = startGroup ?? new OperationGroup();

            var regEx = new Regex(@":(and|or):\(([^\)]+)\)*");

            var matches = regEx.Matches(filter);

            filter = regEx.Replace(filter, String.Empty);

            if (filter.StartsWith("("))
            {
                var startGroupRegEx = new Regex(@"\(([^\)]+)\)*");
                var matchString = startGroupRegEx.Match(filter).Value;
                var ungroupedFilter = startGroupRegEx.Replace(filter, String.Empty);

                if (String.IsNullOrEmpty(ungroupedFilter))
                {
                    filter = filter.Remove(0, 1);
                    filter = filter.Remove(filter.Length - 1);
                    startGroup.Operations.AddRange(this.BuildOperations(filter));
                }
                else
                {
                    var subGroup = new OperationSubGroup();
                    if (ungroupedFilter.StartsWith(":and:"))
                    {
                        subGroup.JoinOperator = OperationsOperators.And;
                        matchString = matchString.Remove(0, 1);
                        matchString = matchString.Remove(matchString.Length - 1);
                        ungroupedFilter = ungroupedFilter.Remove(0, 5);
                    }
                    else
                    {
                        subGroup.JoinOperator = OperationsOperators.Or;
                        matchString = matchString.Remove(0, 1);
                        matchString = matchString.Remove(matchString.Length - 1);
                        ungroupedFilter = ungroupedFilter.Remove(0, 4);
                    }

                    startGroup.SubGroups.Add((OperationSubGroup)this.BuildOperationGroups(matchString, subGroup));
                    startGroup.Operations.AddRange(this.BuildOperations(ungroupedFilter));
                }

            }
            else
                startGroup.Operations.AddRange(this.BuildOperations(filter));

            foreach (Match group in matches)
            {
                var subGroup = new OperationSubGroup();
                var matchString = group.ToString();

                if (matchString.StartsWith(":and:"))
                {
                    subGroup.JoinOperator = OperationsOperators.And;
                    matchString = matchString.Remove(0, 6);
                    matchString = matchString.Remove(matchString.Length - 1);
                }
                else
                {
                    subGroup.JoinOperator = OperationsOperators.Or;
                    matchString = matchString.Remove(0, 5);
                    matchString = matchString.Remove(matchString.Length - 1);
                }

                startGroup.SubGroups.Add((OperationSubGroup)this.BuildOperationGroups(matchString, subGroup));
            }

            return startGroup;
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
                    Type outModel = ViewModel;
                    var propertyString = stringOperations[i++];
                    ViewMappingHelper helper = new ViewMappingHelper(new ViewMappingAttribute(propertyString));
                    opp.Filter.PropertyExpression = ExpressionHelpers.ParseProperty(LinqToSql, ParameterExpression, ViewModel, typeof(Object), helper, 0, null, true);
                    opp.Filter.Operator = stringOperations[i++];
                    var constantString = stringOperations[i];
                    opp.Filter.Constant = constantString;
                    yield return opp;
                }
                else
                    throw new Exception("Invalid Filter");
            }
        }

        private Boolean IsFilter(String propertyString)
        {
            return propertyString.StartsWith("$");
        }

        private Boolean IgnoreFilter(Operation.Condition cond)
        {
            if (IsFilter(cond.Constant))
            {
                if (FilterValues != null)
                {
                    var filterType = FilterValues.GetType();
                    var useFilterInfo = Joe.Reflection.ReflectionHelper.TryGetEvalPropertyInfo(filterType, cond.Constant.Remove(0, 1) + "Active");
                    if (useFilterInfo != null)
                        return !(Boolean)Joe.Reflection.ReflectionHelper.GetEvalProperty(FilterValues, cond.Constant.Remove(0, 1) + "Active");
                }
                else
                    return true;
            }
            return false;
        }

        protected Expression BuildWhereClause(OperationGroup operationGroup)
        {
            Expression previousExpression = null;
            var validFilters = operationGroup.Operations.Where(opp => !IgnoreFilter(opp.Filter));
            if (validFilters.Count() == 0)
                return null;
            foreach (Operation opp in validFilters)
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

            foreach (OperationSubGroup subGroup in operationGroup.SubGroups)
            {

                var subGroupExpression = this.BuildWhereClause(subGroup);
                if (previousExpression != null)
                    switch (subGroup.JoinOperator)
                    {
                        case OperationsOperators.And:
                            previousExpression = Expression.And(previousExpression, subGroupExpression);
                            break;
                        case OperationsOperators.Or:
                            previousExpression = Expression.Or(previousExpression, subGroupExpression);
                            break;
                    }
                else
                    previousExpression = subGroupExpression;

            }

            return previousExpression;
        }

        public LambdaExpression BuildWhereLambda()
        {
            var expression = this.BuildWhereClause(Operations);
            return Expression.Lambda(expression, new ParameterExpression[] { ParameterExpression });
        }

        private Expression GetFilterExpression(Operation.Condition cond)
        {
            if (IsFilter(cond.Constant) && FilterValues != null)
            {
                var filterProp = cond.Constant.Remove(0, 1);
                ViewMappingHelper helper = new ViewMappingHelper(new ViewMappingAttribute(filterProp));
                return Expression.Constant(
                    Expression.Lambda(
                    Expression.Block(
                    ExpressionHelpers.ParseProperty(LinqToSql, Expression.Constant(FilterValues), FilterValues.GetType(), cond.PropertyExpression.Type, helper, 0, FilterValues, true))).Compile().DynamicInvoke()
                    );
            }
            else if (FilterValues == null && IsFilter(cond.Constant))
                throw new NullReferenceException("Filter Object cannot be null if the View Requires it");

            Type parameterType;
            if (cond.Operator == FilterOperators.Contains)
            {
                if (cond.PropertyExpression.Type == typeof(String))
                    parameterType = cond.PropertyExpression.Type;
                else
                    parameterType = cond.PropertyExpression.Type.GetGenericArguments().Single();
            }
            else
                parameterType = cond.PropertyExpression.Type;

            Object constant;
            if (typeof(Enum).IsAssignableFrom(parameterType))
            {
                var enumInt = 0;

                if (int.TryParse(cond.Constant, out enumInt))
                    constant = Enum.ToObject(parameterType, enumInt);
                else
                    constant = Enum.Parse(parameterType, cond.Constant);

            }
            else
                constant = cond.Constant;
            if (parameterType.IsNullable() && parameterType.IsSimpleType() && parameterType.IsValueType)
                return Expression.Constant(constant.ToString().ToNullable(parameterType), parameterType);
            else
                return Expression.Constant(Convert.ChangeType(constant, parameterType));

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
                            ex = Expression.Equal(cond.PropertyExpression, Expression.Constant(null, cond.PropertyExpression.Type));
                            break;
                        case FilterOperators.False:
                        case FilterOperators.True:
                            ex = Expression.Equal(cond.PropertyExpression, Expression.Constant(Convert.ToBoolean(cond.Constant)));
                            break;
                        default:
                            ex = Expression.Equal(cond.PropertyExpression, GetFilterExpression(cond));
                            break;
                    }
                    break;
                case FilterOperators.NotEqual:
                    switch (cond.Constant)
                    {
                        case FilterOperators.Null:
                            ex = Expression.NotEqual(cond.PropertyExpression, Expression.Constant(null, cond.PropertyExpression.Type));
                            break;
                        case FilterOperators.False:
                        case FilterOperators.True:
                            ex = Expression.NotEqual(cond.PropertyExpression, Expression.Constant(Convert.ToBoolean(cond.Constant)));
                            break;
                        default:
                            ex = Expression.NotEqual(cond.PropertyExpression, GetFilterExpression(cond));
                            break;
                    }
                    break;
                case FilterOperators.GreaterThan:
                    ex = Expression.GreaterThan(cond.PropertyExpression, GetFilterExpression(cond));
                    break;
                case FilterOperators.LessThan:
                    ex = Expression.LessThan(cond.PropertyExpression, GetFilterExpression(cond));
                    break;
                case FilterOperators.GreaterThanEqualTo:
                    ex = Expression.GreaterThanOrEqual(cond.PropertyExpression, GetFilterExpression(cond));
                    break;
                case FilterOperators.LessThanEqualTo:
                    ex = Expression.LessThanOrEqual(cond.PropertyExpression, GetFilterExpression(cond));
                    break;
                case FilterOperators.Contains:
                    if (cond.PropertyExpression.Type == typeof(String))
                        ex = Expression.Call(cond.PropertyExpression, cond.PropertyExpression.Type.GetMethod("Contains"), GetFilterExpression(cond));
                    else
                    {
                        var method = typeof(Enumerable).GetMethods().Single(meth => meth.Name == "Contains" && meth.GetParameters().Count() == 2).MakeGenericMethod(cond.PropertyExpression.Type.GetGenericArguments().Single());
                        ex = Expression.Call(method, cond.PropertyExpression, GetFilterExpression(cond));
                    }
                    break;
            }

            return ex;
        }

    }
}

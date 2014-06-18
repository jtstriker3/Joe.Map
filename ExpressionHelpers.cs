using Joe.Reflection;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.Entity;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Joe.Map
{
    public static class ExpressionHelpers
    {
        private const String MaxDepthErrorMessage = "You have hit the default max depth of 10. If you have unintentionally created a recursive view then please correct this. If this is a valid mapping that truly goes 10 levels deep then Set the MaxDepth property of the ViewMapping Attribute to the depth you need.  If this is ture recursive view i.e. some sort of Tree structure set the Max Depth to a depth that your business rules require. True Recursive Views i.e. objects that have a instance of themselves cannot be executed as an Entity Query and must be an Object Query.";
        internal const int MaxDepthDefault = 10;
        //internal static readonly Dictionary<String, LambdaExpression> CachedExpressions = new Dictionary<String, LambdaExpression>();
        private static MethodInfo _stringConvertMethod;
        public static MethodInfo StringConvertMethod
        {
            get
            {
                _stringConvertMethod = _stringConvertMethod ?? typeof(System.Data.Entity.SqlServer.SqlFunctions).GetMethod("StringConvert", new[] { typeof(Decimal?) });
                return _stringConvertMethod;
            }
            set
            {
                _stringConvertMethod = value;
            }
        }

        private static class MappingOperators
        {
            public const String Equal = "=";
            public const String NotEqual = "!=";
            public const String GreaterThan = ">";
            public const String LessThan = "<";
            public const String GreaterThanEqualTo = ">=";
            public const String LessThanEqualTo = "<=";
            public const String Plus = "+";
            public const String Minus = "-";

            private static String[] _operatorArray;
            public static String[] OperatorArray()
            {
                _operatorArray = _operatorArray ?? new String[] { Equal, NotEqual, GreaterThan, LessThan, GreaterThanEqualTo, LessThanEqualTo };
                return _operatorArray;
            }
        }

        public static IQueryable<TViewModel> BuildIncludeExpressions<TViewModel>(this IQueryable<TViewModel> query, Type model) where TViewModel : class
        {
            foreach (PropertyInfo info in typeof(TViewModel).IncludedMappings(model))
            {
                //ParameterExpression viewParamEx = Expression.Parameter(typeof(ViewModel), typeof(ViewModel).Name.ToLower());
                //var type = typeof(Func<,>).MakeGenericType(typeof(ViewModel), info.PropertyType);
                //MemberExpression propertyEx = Expression.Property(viewParamEx, info);
                //Expression propLamdaEx = Expression.Lambda(propertyEx, new ParameterExpression[] { viewParamEx });
                query.Include(info.Name);
            }

            return query;
        }

        public static Expression BuildIncludeExpressions(Expression right, Type viewModel, Type model)
        {

            foreach (PropertyInfo info in viewModel.IncludedMappings(model))
            {
                //ParameterExpression viewParamEx = Expression.Parameter(typeof(ViewModel), typeof(ViewModel).Name.ToLower());
                //var type = typeof(Func<,>).MakeGenericType(typeof(ViewModel), info.PropertyType);
                //MemberExpression propertyEx = Expression.Property(viewParamEx, info);
                //Expression propLamdaEx = Expression.Lambda(propertyEx, new ParameterExpression[] { viewParamEx });
                right = Expression.Call(typeof(Queryable), "AsQueryable", new[] { viewModel }, right);
                right = Expression.Call(typeof(QueryableExtensions), "Include", new Type[] { }, right, Expression.Constant(info.Name));
                right = Expression.Convert(right, typeof(IEnumerable<>).MakeGenericType(viewModel));
            }
            return right;
        }

        internal static Expression BuildGroupByExpressions(Expression right, Type viewModel, String groupByString, Boolean linqToSql)
        {
            if (!String.IsNullOrEmpty(groupByString))
                return new GroupByBuilder(groupByString, viewModel, linqToSql, right).BuildGroupByClause();
            return right;
        }

        internal static Expression BuildLinqFunction(Expression right, Type viewModel, ViewMappingHelper propAttrHelper)
        {
            if (propAttrHelper.HasLinqFunction)
            {
                var genericType = right.Type.ImplementsIEnumerable() ? right.Type.GetGenericArguments().Single() : right.Type;
                right = Expression.Call(typeof(Queryable), "AsQueryable", new[] { genericType }, right);
                var hasNonGeneric = typeof(Queryable).GetMethods().Where(func => !func.IsGenericMethod && func.Name == propAttrHelper.GetLinqFunction()).Count() > 0;

                var linqFunctions = propAttrHelper.GetLinqFunction().Split(',');

                foreach (var linqFunction in linqFunctions)
                {
                    if (hasNonGeneric)
                        right = Expression.Call(typeof(Queryable), linqFunction, new Type[] { }, right);
                    else
                        right = Expression.Call(typeof(Queryable), linqFunction, new Type[] { genericType }, right);
                }
            }
            return right;
        }

        internal static Expression BuildOfType(Expression right, ViewMappingHelper propAttrHelper)
        {
            if (propAttrHelper.HasOfType)
            {
                var genericType = right.Type.ImplementsIEnumerable() ? right.Type.GetGenericArguments().Single() : right.Type;
                right = Expression.Call(typeof(Queryable), "AsQueryable", new[] { genericType }, right);
                right = Expression.Call(typeof(Queryable), "OfType", new Type[] { propAttrHelper.GetOfType() }, right);
            }
            return right;
        }

        internal static LambdaExpression BuildExpression(Type model, Type viewModel, Boolean linqToSql, Object filters)
        {
            //LambdaExpression expression;

            var key = "buildExpression";
            Delegate buildExpressionDelegate = (Func<Type, Type, Boolean, int, Object, Expression>)((Type delegateModel, Type delegateViewModel, Boolean delegateLinqToSql, int delegateDepth, Object delegateFilters) =>
            {
                return BuildExpression(delegateModel, delegateViewModel, delegateLinqToSql, delegateDepth, delegateFilters);
            });

            //Should be able to cache all expressions with the new Hashing of Parameters
            //if (filters == null)
            return (LambdaExpression)Joe.Caching.Cache.Instance.GetOrAdd(
                key,
                TimeSpan.MaxValue,
                buildExpressionDelegate,
                model, viewModel, linqToSql, 0, filters);
            //else
            //expression = BuildExpression(model, viewModel, linqToSql, 0, filters);

            //return expression;
        }

        internal static LambdaExpression BuildExpression(Type model, Type viewModel, Boolean linqToSql, int depth, Object filters)
        {
            var modelEx = Expression.Parameter(model, model.Name.ToLower());
            Expression total = null;

            var block = BuildMemberBindings(model, viewModel, linqToSql, modelEx, depth, filters);
            total = Expression.MemberInit(Expression.New(viewModel), block.ToArray());

            return Expression.Lambda(total, new[] { modelEx });
        }

        public static LambdaExpression BuildExpression(Type model, Type viewModel, Object filters)
        {
            return BuildExpression(model, viewModel, true, filters);
        }

        internal static List<MemberBinding> BuildMemberBindings(Type model, Type viewModel, Boolean linqToSql, Expression right, int depth, Object filters)
        {
            List<MemberBinding> block = new List<MemberBinding>();
            Expression viewEx = Expression.New(viewModel);
            var parameter = right;
            foreach (PropertyInfo propInfo in viewModel.GetProperties().OrderBy(prop => prop.Name))
            {
                ViewMappingHelper propAttrHelper = new ViewMappingHelper(propInfo, model);

                if (propAttrHelper.ViewMapping != null && !propAttrHelper.ViewMapping.WriteOnly)
                {
                    try
                    {
                        //String EvalString = (String.IsNullOrEmpty(Prefix) ? String.Empty : Prefix + ".") + (propAttr.ColumnPropertyName.Contains('-') ? propAttr.ColumnPropertyName.Split('-')[0] : propAttr.ColumnPropertyName);
                        //String NestedPrefix = propAttr.ColumnPropertyName.Contains('-') ? propAttr.ColumnPropertyName.Substring(propAttr.ColumnPropertyName.IndexOf('-') + 1) : null;
                        ColumnPropHelper colPropHelper = new ColumnPropHelper(propAttrHelper.ViewMapping.ColumnPropertyName);

                        if (colPropHelper.IsSwitch)
                        {
                            List<SwitchCase> switchCases = new List<SwitchCase>();

                            Type modelPropertyType = model;
                            var compareInfo = ReflectionHelper.GetEvalPropertyInfo(model, colPropHelper.SwitchProperty);
                            Expression compareProperty = Parse(linqToSql, parameter, model, viewModel, compareInfo.PropertyType, new Queue<String>(colPropHelper.SwitchProperty.Split('-')), 0, filters, hasTransformAttr: true);
                            Expression currentCondtion = null;
                            foreach (ColumnPropHelper.Case c in colPropHelper.Cases)
                            {
                                propAttrHelper.ViewMapping.ColumnPropertyName = c.PropertyString;
                                right = BuildRight(parameter, model, viewModel, linqToSql, propInfo, depth, propAttrHelper, filters);

                                Expression test = Expression.Equal(compareProperty, Expression.Constant(Convert.ChangeType(c.Constant, compareProperty.Type)));

                                if (currentCondtion == null)
                                    currentCondtion = Expression.Condition(test, right, Expression.Default(propInfo.PropertyType));
                                else
                                    currentCondtion = Expression.Condition(test, right, currentCondtion);
                            }
                            right = currentCondtion;
                        }
                        else
                        {
                            right = BuildRight(parameter, model, viewModel, linqToSql, propInfo, depth, propAttrHelper, filters);
                        }
                        if (right != null)
                            block.Add(Expression.Bind(propInfo, right));

                    }
                    catch (Exception ex)
                    {
                        throw new Exception(String.Format("Mapping Error"
                            + Environment.NewLine
                            + "Column: {0}"
                            + Environment.NewLine
                            + "Mapping Attr: {1}", propInfo.Name, propAttrHelper.ViewMapping.ColumnPropertyName), ex);
                    }
                }

            }

            return block;
        }

        internal static Expression BuildRight(Expression right, Type model, Type viewModel, Boolean linqToSql, PropertyInfo propInfo, int depth, ViewMappingHelper propAttrHelper, Object filters)
        {
            if (depth == MaxDepthDefault)
                throw new Exception(MaxDepthErrorMessage);
            else if (propAttrHelper.ViewMapping.MaxDepth == 0 || depth < propAttrHelper.ViewMapping.MaxDepth || propInfo.PropertyType.IsSimpleType())
            {
                var viewModelProperty = propInfo.PropertyType.ImplementsIEnumerable() ? propInfo.PropertyType.GetGenericArguments().Single() : propInfo.PropertyType;
                if (viewModelProperty.IsGenericType && typeof(IGrouping<,>).IsAssignableFrom(viewModelProperty.GetGenericTypeDefinition()))
                    viewModelProperty = viewModelProperty.GetGenericArguments().Last();
                right = ExpressionHelpers.ParseProperty(linqToSql, right, model, viewModelProperty, propAttrHelper, depth, filters);

            }
            else
                right = Expression.TypeAs(Expression.Constant(null), propInfo.PropertyType);

            return right;
        }

        internal static MethodCallExpression SelectMany(Type inType, Type outType, Expression selectFrom, Expression mapExpression, ParameterExpression parameterExpression)
        {
            mapExpression = Expression.Lambda(mapExpression, parameterExpression);
            var selectExpression = Expression.Call(typeof(Enumerable), "SelectMany",
                                           new[] { inType, outType }, selectFrom, mapExpression);
            selectExpression = Expression.Call(typeof(Enumerable), "ToList",
                                          new[] { outType }, selectExpression);
            return selectExpression;

        }

        internal static MethodCallExpression Select(Type inType, Type outType, Expression selectFrom, Expression mapExpression, ParameterExpression parameterExpression, bool callToList)
        {
            selectFrom = Expression.Call(typeof(Enumerable), "Distinct",
                      new[] { inType }, selectFrom);
            mapExpression = Expression.Lambda(mapExpression, parameterExpression);
            var selectExpression = Expression.Call(typeof(Enumerable), "Select",
                                          new[] { inType, outType }, selectFrom, mapExpression);
            if (callToList)
                selectExpression = Expression.Call(typeof(Enumerable), "ToList",
                                              new[] { outType }, selectExpression);
            return selectExpression;

        }

        public static Expression ParseProperty(Boolean linqToSql, Expression right, Type modelPropertyType, Type viewModelPropertyType, ViewMappingHelper propAttrHelper, int depth, Object filters, Boolean returnEntityExpression = false)
        {
            if (!propAttrHelper.HasMapFunction || returnEntityExpression == true)
            {
                var enumerableQueue = new Queue<String>(propAttrHelper.ViewMapping.ColumnPropertyName.Split('-'));
                if (right == null)
                    right = Expression.Parameter(modelPropertyType, modelPropertyType.Name.ToLower());

                var destinationPropertyType = propAttrHelper.PropInfo != null ? propAttrHelper.PropInfo.PropertyType : typeof(Object);
                right = Parse(linqToSql, right, modelPropertyType, viewModelPropertyType, destinationPropertyType, enumerableQueue, depth, filters, returnEntityExpression, hasTransformAttr: propAttrHelper.HasTransform, propAttrHelper: propAttrHelper);

                return right;
            }
            else
            {
                var resourceType = propAttrHelper.ViewMapping.MapFunctionType ?? propAttrHelper.PropInfo.DeclaringType;
                var expressionBody = ((LambdaExpression)resourceType.GetMethod(propAttrHelper.ViewMapping.MapFunction).Invoke(null, new Object[] { linqToSql })).Body;
                var expression = PredicateRewriter.Rewrite(expressionBody, (ParameterExpression)right);

                if (expression.Type.ImplementsIEnumerable())
                {
                    var genericType = expression.Type.GetGenericArguments().FirstOrDefault();
                    if (genericType != null)
                        expression = FilterBuilder.BuildWhereExpressions(expression, genericType, propAttrHelper.ViewMapping.Where, linqToSql, false, filters);
                }

                return expression;

            }
        }

        internal static Expression Parse(Boolean linqToSql, Expression right, Type modelPropertyType, Type viewModelPropertyType, Type destinationPropertyType, Queue<String> evalQueue, int depth, Object filters, Boolean returnEntityExpression = false, Boolean nested = false, Boolean hasTransformAttr = false, ViewMappingHelper propAttrHelper = null)
        {
            var evalString = evalQueue.Dequeue();
            var count = 0;
            var mapOperations = MapOperater.GetMapOperations(evalString);
            var inExpression = right;

            foreach (var mapOperation in mapOperations)
            {
                var evalList = mapOperation.PropertyMapping.Split('.');
                var hasSelect = false;
                List<Expression> rightsTree = new List<Expression>();
                //if (depth > 0 && !destinationPropertyType.ImplementsIEnumerable())
                //    rightsTree.Add(right);
                foreach (String str in evalList)
                {
                    var modelInfo = modelPropertyType.GetProperty(str);
                    if (modelInfo == null)
                        throw new Exception("Invalid Property in: " + evalString);
                    else
                    {
                        right = Expression.Property(right, modelInfo);
                    }
                    var lastPropertyType = modelPropertyType;
                    modelPropertyType = modelInfo.PropertyType;

                    if (modelPropertyType.ImplementsIEnumerable())
                    {
                        var genericPropertyType = modelPropertyType.GetGenericArguments().Single();
                        var parameterExpression = Expression.Parameter(genericPropertyType, genericPropertyType.Name.ToLower());
                        Expression nestExpression = null;
                        if (evalQueue.Count > 0)
                        {
                            nestExpression = Parse(linqToSql, parameterExpression, genericPropertyType, viewModelPropertyType, destinationPropertyType, evalQueue, depth, filters, returnEntityExpression, true, hasTransformAttr, propAttrHelper);
                            if (nestExpression.Type.ImplementsIEnumerable())
                            {
                                rightsTree.Add(right);
                                right = SelectMany(genericPropertyType, nestExpression.Type.GetGenericArguments().First(), right, nestExpression, parameterExpression);

                                hasSelect = true;
                            }
                            else
                            {
                                var outType = nestExpression.Type;

                                if (!nestExpression.Type.IsSimpleType() && !returnEntityExpression)
                                {
                                    var block = BuildMemberBindings(nestExpression.Type, viewModelPropertyType, linqToSql, nestExpression, depth + 1, filters);
                                    nestExpression = Expression.MemberInit(Expression.New(viewModelPropertyType), block.ToArray());
                                    outType = nestExpression.Type;
                                }
                                if (propAttrHelper != null && propAttrHelper.HasLinqFunction && viewModelPropertyType.IsSimpleType())
                                    right = FilterBuilder.BuildWhereExpressions(right, genericPropertyType, propAttrHelper.ViewMapping.Where, linqToSql, true, filters);
                                if (propAttrHelper.HasModelWhere)
                                    right = FilterBuilder.BuildWhereExpressions(right, genericPropertyType, propAttrHelper.ViewMapping.ModelWhere, linqToSql, true, filters);
                                rightsTree.Add(right);
                                right = Select(genericPropertyType, outType, right, nestExpression, parameterExpression, !propAttrHelper.HasLinqFunction);

                                //This is need to be executed becasue it will be missed later
                                if (propAttrHelper.HasLinqFunction)
                                    right = BuildLinqFunction(right, genericPropertyType, propAttrHelper);

                                hasSelect = true;
                            }
                        }
                        else
                        {
                            if (propAttrHelper != null && propAttrHelper.HasOfType)
                            {
                                right = BuildOfType(right, propAttrHelper);
                                genericPropertyType = propAttrHelper.GetOfType();
                                parameterExpression = Expression.Parameter(genericPropertyType, genericPropertyType.Name.ToLower());
                            }

                            var outType = right.Type;
                            if ((!viewModelPropertyType.IsSimpleType() && !returnEntityExpression))
                            {
                                var block = BuildMemberBindings(genericPropertyType, viewModelPropertyType, linqToSql, parameterExpression, depth + 1, filters);
                                nestExpression = Expression.MemberInit(Expression.New(viewModelPropertyType), block.ToArray());
                                outType = nestExpression.Type;
                                if (propAttrHelper != null && propAttrHelper.HasLinqFunction && viewModelPropertyType.IsSimpleType())
                                    right = FilterBuilder.BuildWhereExpressions(right, genericPropertyType, propAttrHelper.ViewMapping.Where, linqToSql, true, filters);
                                if (propAttrHelper.HasModelWhere)
                                    right = FilterBuilder.BuildWhereExpressions(right, genericPropertyType, propAttrHelper.ViewMapping.ModelWhere, linqToSql, true, filters);
                                rightsTree.Add(right);
                                right = Select(genericPropertyType, outType, right, nestExpression, parameterExpression, !propAttrHelper.HasLinqFunction);
                                hasSelect = true;
                            }
                            //else if (!returnEntityExpression)
                            //{
                            //    nestExpression = Parse(linqToSql, parameterExpression, genericPropertyType, viewModelPropertyType, destinationPropertyType, evalQueue, depth, filters, returnEntityExpression, true, hasTransformAttr, propAttrHelper);
                            //    if (propAttrHelper != null && propAttrHelper.HasLinqFunction && viewModelPropertyType.IsSimpleType())
                            //        right = FilterBuilder.BuildWhereExpressions(right, genericPropertyType, propAttrHelper.ViewMapping.Where, linqToSql, true, filters);
                            //    if (propAttrHelper.HasModelWhere)
                            //        right = FilterBuilder.BuildWhereExpressions(right, genericPropertyType, propAttrHelper.ViewMapping.ModelWhere, linqToSql, true, filters);
                            //    rightsTree.Add(right);
                            //    right = Select(genericPropertyType, outType, right, nestExpression, parameterExpression);
                            //    hasSelect = true;
                            //}
                        }

                    }
                    else if (evalQueue.Count > 0 && (count == evalList.Count() - 1))
                    {
                        rightsTree.Add(right);
                        right = Parse(linqToSql, right, modelPropertyType, viewModelPropertyType, destinationPropertyType, evalQueue, depth, filters, returnEntityExpression, true, hasTransformAttr, propAttrHelper);
                    }
                    else if (destinationPropertyType.IsClass
                        && !typeof(string).IsAssignableFrom(destinationPropertyType)
                        && destinationPropertyType != typeof(Object)
                        && (count == evalList.Count() - 1))
                    {
                        var nestExpression = BuildMemberBindings(right.Type, destinationPropertyType, linqToSql, right, depth + 1, filters);
                        rightsTree.Add(right);
                        right = Expression.MemberInit(Expression.New(destinationPropertyType), nestExpression);
                    }
                    if (right.Type.IsSimpleType()
                        && right.Type != destinationPropertyType
                        && destinationPropertyType.IsSimpleType()
                        && !hasTransformAttr
                        && count == evalList.Count() - 1
                        && mapOperations.Count() == 1)
                    {
                        rightsTree.Add(right);
                        right = Expression.Convert(right, destinationPropertyType);
                    }

                    rightsTree.Add(right);
                    count++;
                }

                //To Boolean
                if (propAttrHelper != null && propAttrHelper.HasCompare && right.Type.IsSimpleType())
                {
                    var compareValue = propAttrHelper.GetCompareTrue() == "$NotNull" ? null : propAttrHelper.GetCompareTrue();
                    Boolean notNull = false;
                    if (compareValue == "$NotNull")
                    {
                        notNull = true;
                        compareValue = null;
                    }
                    var comparer = Expression.Convert(Expression.Constant(compareValue), right.Type);
                    if (notNull)
                        right = Expression.NotEqual(right, comparer);
                    else
                        right = Expression.Equal(right, comparer);
                }


                if (!nested && right.Type.ImplementsIEnumerable() && propAttrHelper != null)
                {
                    var viewModelProperty = right.Type.GetGenericArguments().Single();

                    if (propAttrHelper.HasLinqFunction && !hasSelect)
                    {
                        var outType = right.Type.GetGenericArguments().LastOrDefault();
                        if (outType != null)
                            right = FilterBuilder.BuildWhereExpressions(right, outType, propAttrHelper.ViewMapping.Where, linqToSql, false, filters);
                    }
                    else if (!viewModelProperty.IsSimpleType())
                        right = FilterBuilder.BuildWhereExpressions(right, viewModelProperty, propAttrHelper.ViewMapping.Where, linqToSql, false, filters);
                    if (!returnEntityExpression)
                    {
                        right = BuildIncludeExpressions(right, viewModelProperty, modelPropertyType);
                        right = OrderBy.BuildOrderByExpressions(right, viewModelProperty);
                        right = BuildGroupByExpressions(right, viewModelProperty, propAttrHelper.ViewMapping.GroupBy, linqToSql);
                        right = BuildLinqFunction(right, viewModelProperty, propAttrHelper);
                    }

                    if (right.Type.IsSimpleType()
                       && right.Type != destinationPropertyType
                       && destinationPropertyType.IsSimpleType()
                        && mapOperations.Count() == 1)
                    {
                        rightsTree.Add(right);
                        right = Expression.Convert(right, destinationPropertyType);
                    }
                }

                //Null Check
                if (!linqToSql)
                {
                    count = rightsTree.Count();
                    rightsTree.Reverse();
                    foreach (var expression in rightsTree)
                    {
                        if (count < rightsTree.Count())
                        {
                            if (expression.Type.IsClass)
                            {
                                Expression nullCondition = Expression.Equal(expression, Expression.Constant(null));
                                Expression nullExpression;
                                if (destinationPropertyType.ImplementsIEnumerable())
                                {
                                    var generticType = destinationPropertyType.GetGenericArguments().Single();
                                    var listType = typeof(List<>).MakeGenericType(generticType);
                                    if (!returnEntityExpression)
                                    {
                                        nullExpression = Expression.Convert(Expression.New(listType), destinationPropertyType);
                                        right = Expression.Convert(right, destinationPropertyType);
                                    }
                                    else
                                    {
                                        nullExpression = Expression.Default(modelPropertyType);
                                    }
                                }
                                else if (returnEntityExpression)
                                    nullExpression = Expression.Default(right.Type);
                                else
                                    nullExpression = Expression.Default(destinationPropertyType);

                                if (nullExpression.Type != right.Type && !returnEntityExpression)
                                    right = Expression.Convert(right, nullExpression.Type);

                                try
                                {
                                    right = Expression.Condition(nullCondition, nullExpression, right);
                                }
                                catch (Exception ex)
                                {
                                    throw new Exception("Error Building Null Checks", ex);
                                }
                            }
                        }
                        count--;
                    }

                }

                if (mapOperation.JoinOperation != null)
                {
                    switch (mapOperation.JoinOperation)
                    {
                        case MappingOperators.Equal:
                            right = Expression.Equal(inExpression, right);
                            break;
                        case MappingOperators.NotEqual:
                            right = Expression.NotEqual(inExpression, right);
                            break;
                        case MappingOperators.GreaterThan:
                            right = Expression.GreaterThan(inExpression, right);
                            break;
                        case MappingOperators.LessThan:
                            right = Expression.LessThan(inExpression, right);
                            break;
                        case MappingOperators.GreaterThanEqualTo:
                            right = Expression.GreaterThanOrEqual(inExpression, right);
                            break;
                        case MappingOperators.LessThanEqualTo:
                            right = Expression.LessThanOrEqual(inExpression, right);
                            break;
                        case MappingOperators.Plus:
                            var stringConcatMethodInfo = typeof(string).GetMethod("Concat", new[] { typeof(String), typeof(String) });
                            var convertToString = typeof(Convert).GetMethod("ChangeType", new[] { typeof(Object), typeof(Type) });
                            var trimMethodInfo = typeof(string).GetMethod("Trim", new Type[] { });
                            if (inExpression.Type == typeof(String) && right.Type != typeof(String))
                            {
                                //convert and concat
                                right = Expression.Convert(right, typeof(Decimal?));
                                if (linqToSql)
                                    right = Expression.Call(StringConvertMethod, right);
                                else
                                    right = Expression.Call(convertToString, right, Expression.Constant(typeof(String)));
                                right = Expression.Call(right, trimMethodInfo);
                                right = Expression.Add(inExpression, right, stringConcatMethodInfo);
                            }
                            else if (right.Type == typeof(String) && inExpression.Type != typeof(String))
                            {
                                //convert and concat
                                Expression tempexpression = Expression.Convert(inExpression, typeof(Decimal?));
                                if (linqToSql)
                                    tempexpression = Expression.Call(StringConvertMethod, tempexpression);
                                else
                                    tempexpression = Expression.Call(convertToString, tempexpression, Expression.Constant(typeof(String)));
                                tempexpression = Expression.Call(tempexpression, trimMethodInfo);
                                right = Expression.Add(tempexpression, right, stringConcatMethodInfo);
                            }
                            else if (right.Type == typeof(String) && inExpression.Type == typeof(String))
                            {
                                //String.Concat
                                right = Expression.Add(inExpression, right, stringConcatMethodInfo);
                            }
                            else
                                right = Expression.Add(inExpression, right);
                            break;
                        case MappingOperators.Minus:
                            right = Expression.Subtract(inExpression, right);
                            break;
                    }

                    if (right.Type.IsSimpleType()
                          && right.Type != destinationPropertyType
                          && destinationPropertyType.IsSimpleType())
                    {
                        rightsTree.Add(right);
                        inExpression = Expression.Convert(inExpression, destinationPropertyType);
                    }

                }
                var tempExpression = right;
                right = inExpression;
                inExpression = tempExpression;
                modelPropertyType = right.Type;
            }

            return inExpression;
        }

        public static TModel WhereVM<TModel>(this IQueryable<TModel> list, Object viewModel)
        {
            Type model = list.GetType().GetGenericArguments()[0];
            ParameterExpression modelEx = Expression.Parameter(model, model.Name.ToLower());
            Expression test = null;
            foreach (PropertyInfo propInfo in viewModel.GetType().GetProperties())
            {
                ViewMappingHelper attrHelper = new ViewMappingHelper(propInfo, model);
                ViewMappingAttribute propAttr = attrHelper.ViewMapping;
                if (propAttr != null)
                {
                    if (propAttr.Key)
                    {
                        Expression right = null;
                        Type modelPropertyType = model;

                        foreach (String str in propAttr.ColumnPropertyName.Split('.'))
                        {
                            PropertyInfo modelInfo = modelPropertyType.GetProperty(str);

                            if (right == null)
                                right = Expression.Property(modelEx, modelInfo);
                            else
                            {
                                right = right = Expression.Property(right, modelInfo);
                            }
                            modelPropertyType = modelInfo.PropertyType;
                        }
                        if (test == null)
                            test = Expression.Equal(right, Expression.Constant(propInfo.GetValue(viewModel, null)));
                        else
                            test = Expression.And(test, Expression.Equal(right, Expression.Constant(propInfo.GetValue(viewModel, null))));
                    }
                }

            }

            if (test == null)
                throw new Exception("No Key Defined in View");

            Expression<Func<TModel, Boolean>> lambda = (Expression<Func<TModel, Boolean>>)Expression.Lambda(test, new ParameterExpression[] { modelEx });
            return list.Where(lambda).SingleOrDefault();
        }

        public static TViewModel WhereVM<TModel, TViewModel>(this IEnumerable<TViewModel> list, TViewModel viewModel)
        {
            ParameterExpression lamdaParameter = Expression.Parameter(typeof(TViewModel), typeof(TViewModel).Name.ToLower());
            Expression test = null;
            foreach (PropertyInfo propInfo in viewModel.GetType().GetProperties())
            {
                ViewMappingHelper attrHelper = new ViewMappingHelper(propInfo, typeof(TModel));
                ViewMappingAttribute propAttr = attrHelper.ViewMapping;
                if (propAttr != null)
                {
                    if (propAttr.Key)
                    {
                        Expression right = Expression.Property(lamdaParameter, propInfo);
                        if (test == null)
                            test = Expression.Equal(right, Expression.Constant(propInfo.GetValue(viewModel, null)));
                        else
                            test = Expression.And(test,
                                                  Expression.Equal(right,
                                                                   Expression.Constant(propInfo.GetValue(viewModel, null))));
                    }
                }

            }

            if (test == null)
                throw new Exception("No Key Defined in View");

            var lambda = (Expression<Func<TViewModel, Boolean>>)Expression.Lambda(test, new ParameterExpression[] { lamdaParameter });
            return list.AsQueryable().SingleOrDefault(lambda);
        }

        public static Object WhereVM(this IEnumerable list, Object viewModel)
        {
            return list.WhereVM(viewModel, null);
        }

        public static Object WhereVM(this IEnumerable list, Object viewModel, Type model = null)
        {
            if (model == null && list.GetType().IsGenericType)
                model = list.GetType().GetGenericArguments().Last();

            if ((model == null || model == typeof(Object)))
                if (list.Cast<Object>().Count() > 0)
                    model = list.Cast<Object>().First().GetType();
                else
                    return null;

            ParameterExpression modelEx = Expression.Parameter(model, model.Name.ToLower());
            Expression test = null;
            foreach (PropertyInfo propInfo in viewModel.GetType().GetProperties())
            {
                ViewMappingHelper attrHelper = new ViewMappingHelper(propInfo, model);
                ViewMappingAttribute propAttr = attrHelper.ViewMapping;
                if (propAttr != null)
                {
                    if (propAttr.Key)
                    {
                        Expression right = null;
                        Type modelPropertyType = model;

                        foreach (String str in propAttr.ColumnPropertyName.Split('.'))
                        {
                            PropertyInfo modelInfo = modelPropertyType.GetProperty(str);

                            if (right == null)
                                right = Expression.Property(modelEx, modelInfo);
                            else
                            {
                                right = right = Expression.Property(right, modelInfo);
                            }
                            modelPropertyType = modelInfo.PropertyType;
                        }
                        if (test == null)
                            test = Expression.Equal(right, Expression.Constant(propInfo.GetValue(viewModel, null)));
                        else
                            test = Expression.And(test, Expression.Equal(right, Expression.Constant(propInfo.GetValue(viewModel, null))));
                    }
                }

            }

            if (test == null)
                throw new Exception("No Key Defined in View");

            if (list.GetType().IsGenericType)
            {
                var listGenericType = list.GetType().GetGenericArguments().Last();
                if (listGenericType != model)
                {
                    list = (IEnumerable)Expression.Lambda(Expression.Call(typeof(Enumerable), "Cast", new[] { model }, Expression.Constant(list))).Compile().DynamicInvoke();
                }
            }

            var filterExpression = Expression.Lambda(test, new ParameterExpression[] { modelEx });
            var singleOrDefaultExpression = Expression.Call(typeof(Enumerable), "SingleOrDefault", new[] { model }, Expression.Constant(list), filterExpression);
            var lamda = Expression.Lambda(singleOrDefaultExpression);

            return lamda.Compile().DynamicInvoke();
        }

        public static Object WhereModel(this IEnumerable list, Object model)
        {
            var objectlist = list.Cast<Object>().ToList();
            Type viewModel;
            if (objectlist.Count > 0)
                viewModel = objectlist[0].GetType();
            else
                return null;

            ParameterExpression viewModelEx = Expression.Parameter(typeof(object), viewModel.Name.ToLower());
            Expression test = null;
            foreach (PropertyInfo propInfo in viewModel.GetProperties())
            {
                ViewMappingHelper attrHelper = new ViewMappingHelper(propInfo, model.GetType());
                ViewMappingAttribute propAttr = attrHelper.ViewMapping;
                if (propAttr != null)
                {
                    if (propAttr.Key)
                    {
                        Expression right = null;
                        Type modelPropertyType = model.GetType();
                        PropertyInfo modelInfo = null;
                        foreach (String str in propAttr.ColumnPropertyName.Split('.'))
                        {
                            modelInfo = modelPropertyType.GetProperty(str);

                            if (right == null)
                                right = Expression.Property(Expression.Convert(viewModelEx, viewModel), propInfo);

                            modelPropertyType = modelInfo.PropertyType;
                        }
                        if (test == null)
                            test = Expression.Equal(right, Expression.Constant(modelInfo.GetValue(model, null)));
                        else
                            test = Expression.And(test, Expression.Equal(right, Expression.Constant(modelInfo.GetValue(model, null))));
                    }
                }

            }

            if (test == null)
                throw new Exception("No Key Defined in View");

            Expression<Func<Object, Boolean>> lambda = (Expression<Func<Object, Boolean>>)Expression.Lambda(test, new ParameterExpression[] { viewModelEx });
            return list.Cast<Object>().AsQueryable().Where<Object>(lambda).SingleOrDefault();
        }

        public static int NewKey<TModel, TViewModel>(this IQueryable<TModel> list)
        {
            Type model = list.GetType().GetGenericArguments()[0];
            ParameterExpression modelEx = Expression.Parameter(model, model.Name.ToLower());
            Expression right = null;
            foreach (PropertyInfo propInfo in typeof(TViewModel).GetProperties())
            {
                ViewMappingAttribute propAttr = new ViewMappingHelper(propInfo, model).ViewMapping;
                if (propAttr != null)
                {
                    if (propAttr.Key)
                    {
                        Type modelPropertyType = model;

                        foreach (String str in propAttr.ColumnPropertyName.Split('.'))
                        {
                            PropertyInfo modelInfo = modelPropertyType.GetProperty(str);

                            if (right == null)
                                right = Expression.Property(modelEx, modelInfo);
                            else
                            {
                                right = right = Expression.Property(right, modelInfo);
                            }
                            modelPropertyType = modelInfo.PropertyType;
                        }
                    }
                }

            }

            if (right == null)
                throw new Exception("No Key Defined in View");

            Expression<Func<TModel, int?>> lambda = (Expression<Func<TModel, int?>>)Expression.Lambda(Expression.Convert(right, typeof(int?)), new ParameterExpression[] { modelEx });
            var max = list.Max(lambda);
            return max.HasValue ? max.Value + 1 : 1;
        }

        internal static IEnumerable<PropertyInfo> IncludedMappings(this Type viewModel, Type model)
        {
            foreach (PropertyInfo info in viewModel.GetProperties())
            {
                var viewMapping = new ViewMappingHelper(info, model).ViewMapping;
                if (viewMapping != null && viewMapping.Include)
                    yield return info;
            }
        }

        public static TViewModel SetIDs<TViewModel>(this TViewModel viewModel, params object[] keyValues)
        {
            var keyInfoList = new List<PropertyInfo>();
            var keyValuesList = keyValues.ToList();
            foreach (PropertyInfo info in typeof(TViewModel).GetProperties())
            {
                var customAttr = new ViewMappingHelper(info, null).ViewMapping;
                if (customAttr != null && customAttr.Key)
                    keyInfoList.Add(info);
            }

            for (int i = 0; i < keyValuesList.Count; i++)
            {
                var info = keyInfoList[i];
                var value = Convert.ChangeType(keyValuesList[i], info.PropertyType);
                info.SetValue(viewModel, value, null);

            }

            return viewModel;
        }

        public static IEnumerable<Object> GetIDs<TViewModel>(this TViewModel viewModel)
        {
            foreach (PropertyInfo info in viewModel.GetType().GetProperties())
            {
                var customAttr = new ViewMappingHelper(info, null).ViewMapping;
                if (customAttr != null && customAttr.Key)
                    yield return info.GetValue(viewModel, null);
            }
        }

        public static IEnumerable<Object> GetModelIDs<TModel>(this TModel model, Type viewModel)
        {
            foreach (PropertyInfo info in viewModel.GetProperties())
            {
                var customAttr = new ViewMappingHelper(info, model.GetType()).ViewMapping;
                if (customAttr != null && customAttr.Key)
                    yield return ReflectionHelper.GetEvalProperty(model, customAttr.ColumnPropertyName);
            }
        }

        public static Boolean IsNullable(this Type type)
        {
            if (Nullable.GetUnderlyingType(type) != null || type.IsClass)
                return true;

            return false;
        }

        public static Boolean IsSimpleType(this Type type)
        {
            if (type.IsPrimitive || type.Equals(typeof(string)) || type.IsValueType)
                return true;
            return false;
        }

        public static Boolean ImplementsIEnumerable(this Type type)
        {
            if (type.IsGenericType)
                return type.GetInterfaces().Any(i => i == typeof(IEnumerable));
            return false;
        }

        public static Boolean ImplementsIQueryable(this Type type)
        {
            if (type.IsGenericType)
                return type.GetInterfaces().Any(i => i == typeof(IQueryable));
            return false;
        }

        public static Object ToNullable(this string s, Type nullableType)
        {
            nullableType = Nullable.GetUnderlyingType(nullableType) ?? nullableType;
            var nullableMethod = typeof(ExpressionHelpers).GetMethod("ToNullable", new[] { typeof(string) });
            nullableMethod = nullableMethod.MakeGenericMethod(nullableType);

            return nullableMethod.Invoke(null, new[] { s });
        }

        public static Nullable<T> ToNullable<T>(this string s) where T : struct
        {
            Nullable<T> result = new Nullable<T>();
            try
            {
                if (!string.IsNullOrEmpty(s) && s.Trim().Length > 0)
                {
                    TypeConverter conv = TypeDescriptor.GetConverter(typeof(T));
                    result = (T)conv.ConvertFrom(s);
                }
            }
            catch { }
            return result;
        }

        internal static PropertyInfo ParseProperty(Boolean linqToSql, ParameterExpression modelEx, ref Expression right, ref Type modelPropertyType, String evalString)
        {
            PropertyInfo modelInfo = null;

            foreach (String str in evalString.Split('.'))
            {
                modelInfo = modelPropertyType.GetProperty(str);

                if (modelInfo == null)
                    throw new Exception("Invalid Property in: " + evalString);
                else if (right == null)
                    right = Expression.Property(modelEx, modelInfo);
                else
                {
                    if (!linqToSql && modelPropertyType.IsClass)
                    {
                        Expression defaultExpression;
                        if (typeof(String).IsAssignableFrom(modelPropertyType))
                            defaultExpression = Expression.Default(modelPropertyType);
                        else
                            defaultExpression = Expression.New(modelPropertyType);

                        Expression test = Expression.Equal(right, Expression.Constant(null));
                        right = Expression.Condition(test, defaultExpression, right);
                        right = Expression.Property(right, modelInfo);
                    }
                    else
                        right = Expression.Property(right, modelInfo);

                }
                modelPropertyType = modelInfo.PropertyType;
            }
            return modelInfo;
        }

        private class MapOperater
        {
            public String PropertyMapping { get; set; }
            public String JoinOperation { get; set; }

            public static IEnumerable<MapOperater> GetMapOperations(String mapping)
            {
                var mappings = mapping.Split(':');

                for (int i = 0; i < mappings.Count(); i = i + 1)
                {
                    if (i == 0 | mappings.Count() >= i + 1)
                    {
                        MapOperater opp = new MapOperater();
                        if (i > 0)
                        {
                            opp.JoinOperation = mappings[i++];
                        }
                        opp.PropertyMapping = mappings[i];
                        yield return opp;
                    }
                }
            }
        }
    }
}

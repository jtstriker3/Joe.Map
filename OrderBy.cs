using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Joe.Map
{
    class OrderBy
    {
        public String Name { get; set; }
        public int Sequence { get; set; }
        public Type PropertyType { get; set; }
        public PropertyInfo Info { get; set; }
        public Boolean Descending { get; set; }

        internal static Expression BuildOrderByExpressions(Expression right, Type viewModel)
        {

            var count = 0;
            foreach (OrderBy order in viewModel.DefaultOrderByList(null))
            {
                String orderByFunction;
                if (count == 0)
                    orderByFunction = order.Descending ? "OrderByDescending" : "OrderBy";
                else
                    orderByFunction = order.Descending ? "ThenByDescending" : "ThenBy";

                ParameterExpression viewParamEx = Expression.Parameter(viewModel, viewModel.Name.ToLower());
                var type = typeof(Func<,>).MakeGenericType(viewModel, order.PropertyType);
                MemberExpression propertyEx = Expression.Property(viewParamEx, order.Info);
                Expression propLamdaEx = Expression.Lambda(propertyEx, new ParameterExpression[] { viewParamEx });
                if (orderByFunction == "OrderBy")
                {
                    right = Expression.Call(typeof(Queryable), "AsQueryable", new[] { viewModel }, right);
                }
                right = Expression.Call(typeof(Queryable), orderByFunction, new Type[] { viewModel, order.PropertyType }, right, propLamdaEx);
                //right = Expression.Convert(right, typeof(IEnumerable<>).MakeGenericType(viewModel));
                orderByFunction = "ThenBy";
                count++;
            }
            return right;
        }

    }
}

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Joe.Map
{
    public static class FilterExtensions
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="viewList">Must Be Generic List</param>
        /// <param name="filter"></param>
        /// <returns></returns>
        public static IEnumerable Filter(this IEnumerable viewList, String filter, Object filters = null)
        {
            var listExpression = Expression.Constant(viewList);
            var genericType = viewList.GetType().GetGenericArguments().First();

            return (IEnumerable)Expression.Lambda(FilterBuilder.BuildWhereExpressions(listExpression, genericType, filter, false, filters)).Compile().DynamicInvoke();
        }

        public static IQueryable<TViewModel> Filter<TViewModel>(this IQueryable<TViewModel> viewList, String filter, Object filters = null)
        {
            if (filter != null)
            {
                Expression<Func<TViewModel, Boolean>> filterEx = (Expression<Func<TViewModel, Boolean>>)new FilterBuilder(filter, typeof(TViewModel), true, filters).BuildWhereClause();
                if (filterEx != null)
                    viewList = viewList.Where(filterEx);
            }
            else
            {
                throw new Exception("Filter cannot be null");
            }

            return viewList;
        }

        public static IQueryable<TViewModel> FilterDBView<TViewModel>(this IQueryable<TViewModel> viewList, Object filters = null) where TViewModel : class
        {
            List<ViewFilterAttribute> filterList = typeof(TViewModel).GetCustomAttributes(typeof(ViewFilterAttribute), true).Union(
                typeof(TViewModel).GetInterfaces().SelectMany(interphase => interphase.GetCustomAttributes(typeof(ViewFilterAttribute), true))
                ).Cast<ViewFilterAttribute>().ToList();
            foreach (ViewFilterAttribute viewFilter in filterList)
            {
                Expression<Func<TViewModel, Boolean>> filterEx = (Expression<Func<TViewModel, Boolean>>)new FilterBuilder(viewFilter.Where, typeof(TViewModel), true, filters).BuildWhereClause();
                if (filterEx != null)
                    viewList = viewList.Where(filterEx);
            }

            return viewList;
        }
    }
}

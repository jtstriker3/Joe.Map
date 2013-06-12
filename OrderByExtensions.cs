using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Joe.Map
{
    public static class OrderByExtensions
    {
        public static IQueryable<TViewModel> OrderDBView<TViewModel>(this IEnumerable<TViewModel> viewList) where TViewModel : class
        {
            return viewList.AsQueryable().OrderDBView();
        }

        public static IQueryable<TViewModel> OrderDBView<TViewModel>(this IQueryable<TViewModel> viewList) where TViewModel : class
        {
            IEnumerable<OrderBy> orderByList = DefaultOrderByList(typeof(TViewModel), null);
            var count = 0;
            foreach (var orderBy in orderByList)
            {
                if (count == 0)
                    if (orderBy.Descending)
                        viewList = viewList.OrderByDescending(orderBy.Name);
                    else
                        viewList = viewList.OrderBy(orderBy.Name);
                else
                    if (orderBy.Descending)
                        viewList = ((IOrderedQueryable<TViewModel>)viewList).ThenByDescending(orderBy.Name);
                    else
                        viewList = ((IOrderedQueryable<TViewModel>)viewList).ThenBy(orderBy.Name);
                count++;
            }

            return viewList;
        }

        internal static IEnumerable<OrderBy> DefaultOrderByList(this Type viewModel, Type model)
        {
            var orderList = (from info in viewModel.GetProperties()
                             let propAttr = new ViewMappingHelper(info, model).ViewMapping
                             where propAttr != null
                             where propAttr.OrderBy
                             select new OrderBy()
                             {
                                 Name = info.Name,
                                 Sequence = propAttr.OrderBySequence,
                                 PropertyType = info.PropertyType,
                                 Info = info,
                                 Descending = propAttr.Descending
                             }).ToList();

            return orderList.OrderBy(o => o.Sequence).ToList();
        }
    }
}

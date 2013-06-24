using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Linq.Expressions;
using System.Reflection;
using Joe.Reflection;
using System.Collections;
using System.Data.Objects;
using System.Data.Entity;

namespace Joe.Map
{
    public static class MapExtensions
    {

        public static IQueryable<TViewModel> MapDBView<TModel, TViewModel>(this IEnumerable<TModel> modelList, Object filters = null) where TViewModel : class
        {
            //Expression<Func<Model, ViewModel>> expression = (Model m) => (ViewModel)Activator.CreateInstance<ViewModel>().LoadViewWithFocus(m);
            Expression<Func<TModel, TViewModel>> expression = (Expression<Func<TModel, TViewModel>>)ExpressionHelpers.BuildExpression(typeof(TModel), typeof(TViewModel), false, filters);


            IQueryable<TViewModel> query = modelList.AsQueryable().Select(expression);
            query = query.FilterDBView(filters);
            query = query.OrderDBView();
            return query.BuildIncludeExpressions(typeof(TModel));
        }

        public static IQueryable<TViewModel> MapDBView<TModel, TViewModel>(this IQueryable<TModel> modelList, Object filters = null) where TViewModel : class
        {
            Expression<Func<TModel, TViewModel>> expression = (Expression<Func<TModel, TViewModel>>)ExpressionHelpers.BuildExpression(typeof(TModel), typeof(TViewModel), true, filters);

            IQueryable<TViewModel> query = modelList.Select(expression);
            query = query.FilterDBView(filters);
            query = query.OrderDBView();
            return query.BuildIncludeExpressions(typeof(TModel));
        }

        public static TViewModel MapDBView<TModel, TViewModel>(this TModel model, Object filters = null) where TViewModel : class
        {
            Expression<Func<TModel, TViewModel>> expression = (Expression<Func<TModel, TViewModel>>)ExpressionHelpers.BuildExpression(typeof(TModel), typeof(TViewModel), false, filters);
            List<TModel> modelList = new List<TModel>() { model };

            IQueryable<TViewModel> query = modelList.AsQueryable().Select(expression).BuildIncludeExpressions(typeof(TModel));
            return query.Single();
        }

        #region Map Refactor
        public static IQueryable<TViewModel> Map<TModel, TViewModel>(this IQueryable<TModel> modelList, Object filters = null) where TViewModel : class
        {
            return MapDBView<TModel, TViewModel>(modelList, filters);
        }

        public static TViewModel Map<TModel, TViewModel>(this TModel model, Object filters = null) where TViewModel : class
        {
            return MapDBView<TModel, TViewModel>(model, filters);
        }

        public static IQueryable<TViewModel> Map<TModel, TViewModel>(this IEnumerable<TModel> modelList, Object filters = null) where TViewModel : class
        {
            return MapDBView<TModel, TViewModel>(modelList, filters);
        }
        #endregion

        public static IQueryable<TViewModel> FilterDBView<TViewModel>(this IEnumerable<TViewModel> viewList) where TViewModel : class
        {
            return viewList.AsQueryable().FilterDBView();
        }

    }


}

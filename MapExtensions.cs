using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Linq.Expressions;
using System.Reflection;
using Joe.Reflection;
using System.Collections;
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

        public static IQueryable Map(this IEnumerable modelList, Type viewModelType, Object filters = null)
        {
            if (modelList.GetType().IsGenericType)
            {
                var listTypeOf = modelList.GetType().GetGenericArguments().Single();
                return modelList.Map(listTypeOf, viewModelType, filters);
            }
            throw new Exception("modelList must be of IEnumerable<T> to generateMapping try using \"Map(Type modelType, Type viewModelType)");
        }

        public static IQueryable Map(this IEnumerable modelList, Type modelType, Type viewModelType, Object filters = null)
        {
            var selectMappingExpression = ExpressionHelpers.BuildExpression(modelType, viewModelType, filters);
            var selectExpression = Expression.Call(typeof(Enumerable), "Select", new Type[] { modelType, viewModelType }, Expression.Constant(modelList), selectMappingExpression);
            return ((IEnumerable)Expression.Lambda(selectExpression).Compile().DynamicInvoke()).AsQueryable();
        }
        #endregion

        public static IQueryable<TViewModel> FilterDBView<TViewModel>(this IEnumerable<TViewModel> viewList) where TViewModel : class
        {
            return viewList.AsQueryable().FilterDBView();
        }

    }


}

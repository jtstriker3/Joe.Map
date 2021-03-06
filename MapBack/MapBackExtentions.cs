﻿using Joe.Reflection;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Joe.Map;

namespace Joe.MapBack
{
    public static class MapBackExtentions
    {
        public static IDeleteManyToMany DeleteFromList { get; set; }

        #region With Out Context

        /// <summary>
        /// Use this Function to Map A View back to its Model
        /// Note this will not remove ManyToMany Relationships that are not present in the Collection
        /// </summary>
        /// <param name="model">The Model Object that is the Focus of the View</param>
        /// <param name="viewModel">The ViewModel Object that is to be Mapped to the Model</param>
        /// 
        public static void MapBack(this Object model, Object viewModel, Action beforeSaveIEnumerable = null)
        {
            model.MapBack(viewModel, null, beforeSaveIEnumerable);
        }

        /// <summary>
        /// Use this Function to Map A View back to its Model
        /// Note this will not remove ManyToMany Relationships that are not present in the Collection
        /// </summary>
        /// <typeparam name="TModel">The Model Type That the ViewModels are to be mapped back to</typeparam>
        /// <param name="modelList">The IEnumerable List of Model Object to search to find the model for a View Model</param>
        /// <param name="viewModelList">The list of ViewModels to map back to the Model List</param>
        /// <param name="getFocusModel">Delegate that select from the database to get the model Model Object</param>
        /// 
        /// <returns>List of Model Object that were updated</returns>
        public static List<TModel> MapBack<TModel>(this IEnumerable<TModel> modelList, IEnumerable viewModelList, Func<Object, TModel> getFocusModel, Action beforeSaveIEnumerable = null)
        {
            List<TModel> returnModelList = new List<TModel>();

            foreach (Object viewModel in viewModelList)
            {
                var focusModel = getFocusModel(viewModel);
                focusModel.MapBack(viewModel, beforeSaveIEnumerable);
                returnModelList.Add(focusModel);
            }

            return returnModelList;
        }

        /// <summary>
        /// Use this Function to Map A Views back to their Models.
        /// This will use the IQuerable Model List to build an expression the get the model row from the database.
        /// Note this will not remove ManyToMany Relationships that are not present in the Collection
        /// </summary>
        /// <typeparam name="TModel">The Model Type That the ViewModels are to be mapped back to</typeparam>
        /// <param name="modelList">The IEnumerable List of Model Object to search to find the model for a View Model</param>
        /// <param name="viewModelList">The list of ViewModels to map back to the Model List</param>
        /// 
        /// <returns>List of Model Object that were updated</returns>
        public static List<TModel> MapBack<TModel>(this IQueryable<TModel> modelList, IEnumerable viewModelList, Action beforeSaveIEnumerable = null)
        {
            List<TModel> returnModelList = new List<TModel>();

            foreach (Object viewModel in viewModelList)
            {
                var focusModel = modelList.WhereVM<TModel>(viewModel);
                focusModel.MapBack(viewModel, beforeSaveIEnumerable);
                returnModelList.Add(focusModel);
            }

            return returnModelList;
        }

        /// <summary>
        /// Use this Function to Map A Views back to their Models.
        /// This will use the IQuerable Model List to build an expression the get the model row from the database.
        /// Note this will not remove ManyToMany Relationships that are not present in the Collection
        /// </summary>
        /// <typeparam name="TModel">The Model Type That the ViewModels are to be mapped back to</typeparam>
        /// <param name="modelList">The IEnumerable List of Model Object to search to find the model for a View Model</param>
        /// <param name="viewModel">The View Model to locate the Focus Row For and Map back to the database</param>
        /// 
        /// <returns>List of Model Object that were updated</returns>
        public static TModel MapBack<TModel>(this IQueryable<TModel> modelList, Object viewModel, Action beforeSaveIEnumerable = null)
        {
            return modelList.MapBack(new List<Object>() { viewModel }, beforeSaveIEnumerable).SingleOrDefault();
        }

        /// <summary>
        /// Use this Function to Map A View back to its Model
        /// Note this will not remove ManyToMany Relationships that are not present in the Collection
        /// </summary>
        /// <typeparam name="TModel">The Model Type That the ViewModels are to be mapped back to</typeparam>
        /// <param name="modelList">The IEnumerable List of Model Object to search to find the model for a View Model</param>
        /// <param name="viewModelList">The list of ViewModels to map back to the Model List</param>
        /// <param name="getFocusModel">Delegate that select from the database to get the model Model Object</param>
        /// <param name="admin">Whether or not to Map Back Admin Properties</param>
        /// <returns>List of Model Object that were updated</returns>
        public static TModel MapBack<TModel>(this IEnumerable<TModel> modelList, Object viewModel, Func<Object, TModel> getFocusModel, Action beforeSaveIEnumerable = null)
        {
            return modelList.MapBack(new List<Object>() { viewModel }, getFocusModel, beforeSaveIEnumerable).SingleOrDefault();
        }

        #endregion

        #region With Context

        public static void MapBack(this Object model, Object viewModel, IDBViewContext context, Action beforeSaveIEnumerable = null)
        {
            var simplePropertiesAndClasses = viewModel.GetType().GetProperties().Where(prop => !prop.PropertyType.ImplementsIEnumerable());
            var ienumerableProperties = viewModel.GetType().GetProperties().Where(prop => prop.PropertyType.ImplementsIEnumerable());
            foreach (PropertyInfo propInfo in simplePropertiesAndClasses)
            {
                ViewMappingHelper attrHelper = new ViewMappingHelper(propInfo, model.GetType());
                ViewMappingAttribute propAttr = attrHelper.ViewMapping;

                if (propAttr != null)
                {
                    var mapBackPropertyName = propAttr.MapBackPropertyName ?? propAttr.ColumnPropertyName;
                    try
                    {
                        if (!propAttr.ReadOnly && !attrHelper.HasLinqFunction)
                        {
                            ColumnPropHelper columnPropHelper = new ColumnPropHelper(mapBackPropertyName);
                            var value = propInfo.GetValue(viewModel, null);
                            String columnProperty = columnPropHelper.IsSwitch ? columnPropHelper.GetSwitchProperty(model) : mapBackPropertyName;


                            if (propInfo.PropertyType.IsClass
                                 && !typeof(String).IsAssignableFrom(propInfo.PropertyType))
                            {
                                var nestedModel = ReflectionHelper.GetEvalProperty(model, columnProperty);

                                if (value != null)
                                {
                                    if (nestedModel == null && propAttr.CreateNew)
                                    {
                                        var info = Reflection.ReflectionHelper.GetEvalPropertyInfo(model, columnProperty);
                                        nestedModel = Activator.CreateInstance(info.PropertyType);
                                        if (context != null)
                                            context.GetIPersistenceSet(nestedModel.GetType()).InvokeAdd(nestedModel);
                                        Reflection.ReflectionHelper.SetEvalProperty(model, columnProperty, nestedModel);
                                    }
                                    nestedModel.MapBack(value, context, null);
                                }
                            }
                            else
                            {
                                var propValue = ReflectionHelper.GetEvalProperty(model, columnProperty);
                                if (model != null && viewModel != value && (!propAttr.Key ||
                                    (value is string ? propValue == null : !(value is int) || (int)propValue == 0)))
                                {
                                    if (attrHelper.HasCompare)
                                    {
                                        value = Convert.ToBoolean(value) ? attrHelper.GetCompareTrue() : attrHelper.GetCompareFalse();
                                    }

                                    ReflectionHelper.SetEvalProperty(model, columnProperty, value, (obj, parentObj, newObjInfo) =>
                                    {
                                        if (value != null && propAttr.CreateNew)
                                        {
                                            newObjInfo.SetValue(parentObj, obj, null);
                                            if (context != null)
                                                context.GetIPersistenceSet(obj.GetType()).InvokeAdd(obj);
                                        }
                                    });
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        throw new Exception(String.Format("Error Mapping Back. ViewProperty: {0} to ModelProperty: {1} where ID: {2}", propInfo.Name, mapBackPropertyName, viewModel.GetIDs().BuildIDString()), ex);
                    }
                }


            }

            if (beforeSaveIEnumerable != null)
                beforeSaveIEnumerable();

            foreach (PropertyInfo propInfo in ienumerableProperties)
            {
                ViewMappingHelper attrHelper = new ViewMappingHelper(propInfo, model.GetType());
                ViewMappingAttribute propAttr = attrHelper.ViewMapping;

                if (propAttr != null)
                {
                    var mapBackPropertyName = propAttr.MapBackPropertyName ?? propAttr.ColumnPropertyName;
                    try
                    {
                        if (!propAttr.ReadOnly && !attrHelper.HasLinqFunction)
                        {
                            ColumnPropHelper columnPropHelper = new ColumnPropHelper(mapBackPropertyName);
                            var value = propInfo.GetValue(viewModel, null);
                            String columnProperty = columnPropHelper.IsSwitch ? columnPropHelper.GetSwitchProperty(model) : mapBackPropertyName;

                            if (value != null)
                            {
                                var genericType = propInfo.PropertyType.GetGenericArguments().Single();

                                var modelType = model.GetType();
                                var viewModelType = viewModel.GetType();
                                var parameterExpression = Expression.Parameter(modelType, modelType.Name.ToLower());
                                var selectExpression = ExpressionHelpers.ParseProperty(false, parameterExpression, modelType, viewModelType, attrHelper, 0, null, true);
                                var modelEnumerable = Expression.Lambda(selectExpression, parameterExpression).Compile().DynamicInvoke(model) as IEnumerable;
                                var modelEnumerableGenericType = propAttr.OfType ?? modelEnumerable.GetType().GetGenericArguments().Single();
                                if (modelEnumerable != null && !modelEnumerableGenericType.IsSimpleType())
                                {
                                    if (!attrHelper.HasGroupBy)
                                    {
                                        CompareList(modelEnumerable, (IEnumerable)value, context, propAttr, genericType, modelEnumerableGenericType);
                                    }
                                    else
                                    {
                                        foreach (var group in (IEnumerable)value)
                                        {
                                            CompareList(modelEnumerable, (IEnumerable)group, context, propAttr, genericType, modelEnumerableGenericType);
                                        }
                                    }
                                }
                                else
                                {
                                    ReflectionHelper.SetEvalProperty(model, columnProperty, value);
                                }

                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        throw new Exception(String.Format("Error Mapping Back. ViewProperty: {0} to ModelProperty: {1} where ID: {2}", propInfo.Name, mapBackPropertyName, viewModel.GetIDs().BuildIDString()), ex);
                    }
                }


            }

        }

        /// <summary>
        /// Use this Function to Map A View back to its Model
        /// </summary>
        /// <typeparam name="TModel">The Model Type That the ViewModels are to be mapped back to</typeparam>
        /// <param name="modelList">The IEnumerable List of Model Object to search to find the model for a View Model</param>
        /// <param name="viewModelList">The list of ViewModels to map back to the Model List</param>
        /// <param name="getFocusModel">Delegate that select from the database to get the model Model Object</param>
        /// 
        /// <param name="context">context to use to handle ManyToMany Relationships</param>
        /// <returns>List of Model Object that were updated</returns>
        public static List<TModel> MapBack<TModel>(this IEnumerable<TModel> modelList, IEnumerable viewModelList, Func<Object, TModel> getFocusModel, IDBViewContext context, Action beforeSaveIEnumerable = null)
        {
            List<TModel> returnModelList = new List<TModel>();

            foreach (Object viewModel in viewModelList)
            {
                var focusModel = getFocusModel(viewModel);
                focusModel.MapBack(viewModel, context, beforeSaveIEnumerable);
                returnModelList.Add(focusModel);
            }

            return returnModelList;
        }

        /// <summary>
        /// Use this Function to Map A Views back to their Models.
        /// This will use the IQuerable Model List to build an expression the get the model row from the database.
        /// </summary>
        /// <typeparam name="TModel">The Model Type That the ViewModels are to be mapped back to</typeparam>
        /// <param name="modelList">The IEnumerable List of Model Object to search to find the model for a View Model</param>
        /// <param name="viewModelList">The list of ViewModels to map back to the Model List</param>
        /// 
        /// <param name="context">context to use to handle ManyToMany Relationships</param>
        /// <returns>List of Model Object that were updated</returns>
        public static List<TModel> MapBack<TModel>(this IQueryable<TModel> modelList, IEnumerable viewModelList, IDBViewContext context, Action beforeSaveIEnumerable = null)
        {
            List<TModel> returnModelList = new List<TModel>();

            foreach (Object viewModel in viewModelList)
            {
                var focusModel = modelList.WhereVM<TModel>(viewModel);
                focusModel.MapBack(viewModel, context, beforeSaveIEnumerable);
                returnModelList.Add(focusModel);
            }

            return returnModelList;
        }

        /// <summary>
        /// Use this Function to Map A Views back to their Models.
        /// This will use the IQuerable Model List to build an expression the get the model row from the database.
        /// </summary>
        /// <typeparam name="TModel">The Model Type That the ViewModels are to be mapped back to</typeparam>
        /// <param name="modelList">The IEnumerable List of Model Object to search to find the model for a View Model</param>
        /// <param name="viewModel">The View Model to locate the Focus Row For and Map back to the database</param>
        /// 
        /// <param name="context">context to use to handle ManyToMany Relationships</param>
        /// <returns>List of Model Object that were updated</returns>
        public static TModel MapBack<TModel>(this IQueryable<TModel> modelList, Object viewModel, IDBViewContext context, Action beforeSaveIEnumerable = null)
        {
            return modelList.MapBack(new List<Object>() { viewModel }, context, beforeSaveIEnumerable).SingleOrDefault();
        }

        /// <summary>
        /// Use this Function to Map A View back to its Model
        /// </summary>
        /// <typeparam name="TModel">The Model Type That the ViewModel is to be mapped back to</typeparam>
        /// <param name="modelList">The IEnumerable List of Model Object to search to find the model for a View Model</param>
        /// <param name="viewModel">The ViewModel to map back to the Model List</param>
        /// <param name="getFocusModel">Delegate that select from the database to get the model Model Object</param>
        /// 
        /// <param name="context">context to use to handle ManyToMany Relationships</param>
        /// <returns>List of Model Object that were updated</returns>
        public static TModel MapBack<TModel>(this IEnumerable<TModel> modelList, Object viewModel, Func<Object, TModel> getFocusModel, IDBViewContext context, Action beforeSaveIEnumerable = null)
        {
            return modelList.MapBack(new List<Object>() { viewModel }, getFocusModel, context, beforeSaveIEnumerable).SingleOrDefault();
        }

        private static void CompareList(IEnumerable modelEnumerable, IEnumerable value, IDBViewContext context, ViewMappingAttribute propAttr, Type viewModelType, Type modelType)
        {
            var modelEnumerableDistinct = modelEnumerable.Cast<Object>().Distinct(new ModelCompare<Object>(viewModelType));
            var valueDistinct = ((IEnumerable)value).Cast<Object>().Distinct(ViewModelComparer<Object>.ViewModelIEqualityComparer);
            if (propAttr.OfType != null)
                modelEnumerableDistinct = modelEnumerableDistinct.Where(item => propAttr.OfType.IsAssignableFrom(item.GetType()));
            var immutableModelList = modelEnumerableDistinct.ToList().AsReadOnly();
            //foreach (Object model in immutableModelList)
            //{
            //    var view = valueDistinct.WhereModel(model);
            //    var included = true;
            //    if (view != null)
            //    {
            //        var includedInfo = view.GetType().GetProperty("Included");
            //        if (includedInfo != null && includedInfo.PropertyType == typeof(Boolean))
            //            included = (Boolean)includedInfo.GetValue(view, null);


            //        if ( /*focusItem == null ||*/ !included)
            //        {
            //            if (DeleteManyToMany != null)
            //                DeleteManyToMany.Delete(model, context,
            //                                        propAttr.UseParentListForRelationships,
            //                                        modelEnumerable);
            //            else if (propAttr.UseParentListForRelationships == true)
            //                try
            //                {
            //                    modelEnumerable.InvokeRemove(model);
            //                }
            //                catch (Exception ex)
            //                {
            //                    throw new Exception("No Remove Method on Parent List", ex);
            //                }
            //            else if (context != null)
            //                context.GetIDbSet(model.GetType()).InvokeRemove(model);
            //        }
            //    }

            //}

            List<Object> modelsToAdd = new List<Object>();
            foreach (var view in valueDistinct)
            {
                if (view != null)
                {
                    Boolean included = true;
                    var includedInfo = view.GetType().GetProperty("Included");
                    if (includedInfo != null && includedInfo.PropertyType == typeof(Boolean))
                        included = (Boolean)includedInfo.GetValue(view, null);

                    object model = null;
                    //assume view is list of ids
                    if (viewModelType.IsSimpleType())
                        if (viewModelType == typeof(String))
                            model = immutableModelList.Find(modelType, ((String)view).Split(','));
                        else
                            model = immutableModelList.Find(modelType, view);
                    else
                        model = immutableModelList.WhereVM(view, modelType);

                    if (included)
                    {
                        //Type genericListmodelType = propAttr.OfType ??
                        //  modelEnumerable.GetType().GetGenericArguments().Single();

                        if (!modelType.IsAbstract)
                        {
                            Object defaultFocusItem = null;
                            if (context != null)
                                defaultFocusItem = context.GetIPersistenceSet(modelType).InvokeCreate();
                            else
                                defaultFocusItem = Activator.CreateInstance(modelType);

                            if (model.GetIDs().Equals(defaultFocusItem.GetIDs()))
                                model = null;

                            if (model == null)
                            {
                                if (context != null)
                                {
                                    if (propAttr.HowToHandleCollections == CollectionHandleType.ParentCollection)
                                    {
                                        if (viewModelType.IsSimpleType())
                                            if (viewModelType == typeof(String))
                                                model = context.GetGenericQueryable(modelType).Find(modelType, ((String)view).Split(','));
                                            else
                                                model = context.GetGenericQueryable(modelType).Find(modelType, view);
                                        else
                                            model = context.GetGenericQueryable(modelType).WhereVM(view, modelType);

                                        if (model == null)
                                        {
                                            model = defaultFocusItem;
                                        }
                                        modelsToAdd.Add(model);
                                    }
                                    else
                                    {
                                        model = defaultFocusItem;
                                        modelsToAdd.Add(model);
                                    }
                                }
                            }
                        }

                        if (model != null && propAttr.MapBackListData)
                            model.MapBack(view, context, null);
                    }
                    else if (model != null)
                    {
                        if (DeleteFromList != null)
                            DeleteFromList.Delete(model, context,
                                                    propAttr.HowToHandleCollections,
                                                    modelEnumerable);
                        else
                        {
                            switch (propAttr.HowToHandleCollections)
                            {
                                case CollectionHandleType.ParentCollection:
                                    InvokeRemoveFromParentCollection(modelEnumerable, model);
                                    break;
                                case CollectionHandleType.Context:
                                    ContextDeletion(context, model);
                                    break;
                                case CollectionHandleType.Both:
                                    InvokeRemoveFromParentCollection(modelEnumerable, model);
                                    ContextDeletion(context, model);
                                    break;
                            }
                        }

                    }
                }
            }

            if (propAttr.DeleteIfNotInList)
            {
                foreach (var model in immutableModelList)
                {
                    if (propAttr.OfType == null || propAttr.OfType.IsAssignableFrom(model.GetType()))
                    {
                        bool hasVm = false;
                        if (viewModelType.IsSimpleType())
                        {
                            var modelIDs = String.Join(",", model.GetEntityIDs().ToArray());
                            hasVm = valueDistinct.Select(id => id.ToString()).Contains(modelIDs);
                        }
                        else
                            hasVm = valueDistinct.WhereModel(model) != null;

                        if (!hasVm)
                            switch (propAttr.HowToHandleCollections)
                            {
                                case CollectionHandleType.ParentCollection:
                                    InvokeRemoveFromParentCollection(modelEnumerable, model);
                                    break;
                                case CollectionHandleType.Context:
                                    ContextDeletion(context, model);
                                    break;
                                case CollectionHandleType.Both:
                                    InvokeRemoveFromParentCollection(modelEnumerable, model);
                                    ContextDeletion(context, model);
                                    break;
                            }
                    }
                }
            }

            foreach (var model in modelsToAdd)
            {

                switch (propAttr.HowToHandleCollections)
                {
                    case CollectionHandleType.ParentCollection:
                        modelEnumerable.InvokeAdd(model);
                        break;
                    case CollectionHandleType.Context:
                        if (context != null)
                            context.GetIPersistenceSet(model.GetType()).InvokeAdd(model);
                        break;
                    case CollectionHandleType.Both:
                        modelEnumerable.InvokeAdd(model);
                        //context.GetIDbSet(model.GetType()).InvokeAdd(model);
                        break;
                }
            }
        }

        private static void ContextDeletion(IDBViewContext context, object model)
        {
            if (context != null)
            {
                var set = context.GetIPersistenceSet(model.GetType());
                if (set != null)
                    set.InvokeRemove(model);
            }
        }

        private static void InvokeRemoveFromParentCollection(IEnumerable modelEnumerable, object model)
        {
            try
            {
                modelEnumerable.InvokeRemove(model);
            }
            catch (Exception ex)
            {
                throw new Exception("No Remove Method on Parent List", ex);
            }
        }

        #endregion

        internal static String BuildIDString(this IEnumerable<Object> ids)
        {
            var idString = String.Empty;
            foreach (object id in ids)
                idString += id.ToString() + ":";
            return idString;
        }

        public class ViewModelComparer<T> : IEqualityComparer<T>
        {
            internal static ViewModelComparer<T> _viewModelComparer = new ViewModelComparer<T>();
            public static ViewModelComparer<T> ViewModelIEqualityComparer { get { return _viewModelComparer; } }

            public bool Equals(T x, T y)
            {
                if (x.GetType().IsSimpleType())
                {
                    return x.Equals(y);
                }
                else
                {
                    var xIDs = x.GetIDs();
                    var yIDs = y.GetIDs();
                    foreach (var key in xIDs)
                    {
                        var defaultValue = key.GetType().GetDefualtValue();
                        if (!yIDs.Contains(key) || key.Equals(defaultValue))
                            return false;
                    }
                }

                return true;
            }

            public int GetHashCode(T obj)
            {
                if (obj.GetType().IsSimpleType())
                    return obj.GetHashCode();
                else
                {
                    int hash = 0;
                    foreach (var id in obj.GetIDs())
                    {
                        var defaultValue = id.GetType().GetDefualtValue();

                        if (id != null)
                            if (!id.Equals(defaultValue))
                                hash = hash + id.GetHashCode();
                            else
                                hash += DateTime.Now.GetHashCode();

                    }
                    return hash;
                }
            }
        }

        public class ModelCompare<T> : IEqualityComparer<T>
        {
            internal Type ViewModelType { get; set; }

            public ModelCompare(Type viewModelType)
            {
                ViewModelType = viewModelType;
            }

            public bool Equals(T x, T y)
            {
                if (ViewModelType.IsSimpleType())
                {
                    var xIDs = x.GetEntityIDs();
                    var yIDs = y.GetEntityIDs();

                    foreach (var key in xIDs)
                        if (!yIDs.Contains(key))
                            return false;

                    return true;
                }
                else
                {
                    var xIDs = x.GetModelIDs(ViewModelType);
                    var yIDs = y.GetModelIDs(ViewModelType);
                    foreach (var key in xIDs)
                        if (!yIDs.Contains(key))
                            return false;

                    return true;
                }
            }

            public int GetHashCode(T obj)
            {
                if (ViewModelType.IsSimpleType())
                {
                    int hash = 0;
                    foreach (var id in obj.GetEntityIDs())
                    {
                        hash = hash + id.GetHashCode();
                    }
                    return hash;
                }
                else
                {
                    int hash = 0;
                    foreach (var id in obj.GetModelIDs(ViewModelType))
                    {
                        hash = hash + id.GetHashCode();
                    }
                    return hash;
                }
            }
        }

        internal static Object GetDefualtValue(this Type type)
        {
            if (type.IsValueType)
            {
                return Activator.CreateInstance(type);
            }
            else
            {
                return null;
            }
        }

        private static IEnumerable GetIPersistenceSet(this IDBViewContext context, Type modelType)
        {
            //modelType = System.Data.Entity.Core.Objects.ObjectContext.GetObjectType(modelType);
            //MethodInfo methodInfo = context.GetType().GetMethods().Where(method =>
            //            method.Name == "GetIDbSet"
            //            && method.IsGenericMethod
            //            && method.GetGenericArguments().Count() == 1).Single().MakeGenericMethod(modelType);

            return context.GetGenericQueryable(modelType); //(IEnumerable)methodInfo.Invoke(context, null);
        }

        private static void InvokeAdd(this IEnumerable list, Object model)
        {
            list.GetType().GetMethod("Add").Invoke(
                        list, new[] { model });
        }

        private static void InvokeRemove(this IEnumerable list, Object model)
        {
            list.GetType().GetMethod("Remove").Invoke(
                        list, new[] { model });
        }

        private static Object InvokeCreate(this IEnumerable list)
        {
            return list.GetType().GetMethods().Where(m => m.Name == "Create" && !m.IsGenericMethod).First().Invoke(list, null);
        }
    }
}

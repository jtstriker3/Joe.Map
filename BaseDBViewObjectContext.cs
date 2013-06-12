using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Reflection;
using Teradata.Reflection;
using System.Runtime.Serialization;
using System.ComponentModel.DataAnnotations;
using System.Data.Entity;
using System.Data.Objects;
using System.Linq.Expressions;
using System.Collections;

namespace Teradata.DBView.ObjContext
{
    [DataContract]
    public abstract class DBView : Teradata.DBView.ObjContext.IDBView
    {
        protected ObjectContext Context { get; set; }
        protected Boolean Admin { get; set; }

        public DBView(ObjectContext context)
        {
            Context = context;
            Admin = false;
        }

        public DBView(ObjectContext context, Boolean admin)
        {
            Context = context;
            Admin = admin;
        }

        /// <summary>
        /// Update Values to their Object Context. This uses Reflection based on a DBView Attribute to Update the value.
        /// </summary>
        /// <param name="Commit">True: Call ObjectContext.SaveChanges(). False: Do not Call ObjectContext.SaveChanges(). 
        /// You must manually call after you have finished you operations</param>
        public virtual void SaveViewChanges(Boolean Commit)
        {
            this.SaveViewChangesRecursive(Commit, this);
        }

        /// <summary>
        /// Update Values to their Object Context. This uses Reflection based on a DBView Attribute to Update the value.
        /// </summary>
        /// <param name="Commit">True: Call ObjectContext.SaveChanges(). False: Do not Call ObjectContext.SaveChanges(). 
        /// You must manually call after you have finished you operations</param>
        public virtual void SaveViewChanges(Boolean Commit, ObjectContext context)
        {
            this.Context = context;
            this.SaveViewChangesRecursive(Commit, this);
        }

        /// <summary>
        /// Recursivly loop through the object properties and and set the value of any items with the DBView attribute
        /// </summary>
        /// <param name="Commit">Save the Changes to the database</param>
        /// <param name="focus">Objec that is the focus of Reflection</param>
        protected virtual void SaveViewChangesRecursive(Boolean Commit, Object focus)
        {
            Object focusRow = null;
            if (typeof(IDBView).IsAssignableFrom(focus.GetType()))
                focusRow = ((IDBView)focus).GetFocusRow(this.Context);

            foreach (PropertyInfo propInfo in focus.GetType().GetProperties())
            {

                ViewMappingAttribute propAttr = (ViewMappingAttribute)propInfo.GetCustomAttributes(false).Where(ca => ca is ViewMappingAttribute).SingleOrDefault();
                if (propAttr != null)
                {
                    if (!propAttr.ReadOnly && (Admin || !propAttr.Admin))
                    {
                        var value = propInfo.GetValue(focus, null);
                        if (typeof(IDBView).IsAssignableFrom(propInfo.PropertyType))
                        {
                            IDBView view = ((IDBView)value);
                            view.SetContext(this.Context);
                            view.SaveViewChanges(false);
                        }
                        else if (typeof(IDBViewList).IsAssignableFrom(propInfo.PropertyType))
                        {
                            IDBViewList viewList = ((IDBViewList)value);
                            viewList.SetContext(this.Context);
                            viewList.SaveViews(false);
                        }
                        else if (typeof(System.Collections.IEnumerable).IsAssignableFrom(propInfo.PropertyType)
                            && !typeof(String).IsAssignableFrom(propInfo.PropertyType))
                        {
                            if (value != null)
                            {
                                foreach (var obj in (System.Collections.IEnumerable)value)
                                {
                                    if (obj is IDBView)
                                        ((IDBView)obj).SaveViewChanges(false);
                                    else
                                        this.SaveViewChangesRecursive(false, obj);
                                }
                            }
                        }
                        else
                        {
                            if (focusRow != null)
                                ReflectionHelper.SetEvalProperty(focusRow, propAttr.ColumnPropertyName, propInfo.GetValue(focus, null));
                        }
                    }

                }

            }

            if (Commit)
                this.Context.SaveChanges();
        }

        /// <summary>
        /// Update Values to their Object Context. This uses Reflection based on a DBView Attribute to Update the value.
        /// </summary>
        /// <param name="Commit">True: Call ObjectContext.SaveChanges(). False: Do not Call ObjectContext.SaveChanges(). 
        /// You must manually call after you have finished you operations</param>
        public virtual void DeleteView()
        {
            this.DeleteViewRecursive(this.GetFocusRow(), true);
        }

        /// <summary>
        /// Delete Values from their Object Context.
        /// </summary>       
        public virtual void DeleteView(ObjectContext context)
        {
            this.Context = context;
            this.DeleteViewRecursive(this.GetFocusRow(), true);
        }

        protected virtual void DeleteViewRecursive(Object focus, Boolean Commit)
        {
            if (focus != null)
            {
                foreach (PropertyInfo propInfo in focus.GetType().GetProperties())
                {
                    var value = propInfo.GetValue(focus, null);
                    DeleteAttribute deleteAttr = (DeleteAttribute)value.GetType().GetCustomAttributes(false).Where(a => a is DeleteAttribute).SingleOrDefault();

                    if (deleteAttr.Cascade)
                    {
                        if (Admin || !deleteAttr.Admin)
                        {
                            if (typeof(IDelete).IsAssignableFrom(value.GetType()))
                            {
                                this.DeleteViewRecursive(value, false);
                                ((IDelete)value).Delete(this.Context);
                            }
                            else
                                throw new Exception("Model does not implement IDelete");
                        }
                        else
                            throw new Exception("User does not have sufficient privileges to delete");
                    }


                }
                if (Commit)
                    this.Context.SaveChanges();
            }
            else
                throw new Exception("Unable to Find Item");
        }

        public void LoadView<Model, ViewModel>(Object parameters) where ViewModel : IDBView
        {
            Model focus = (Model)this.GetFocusRow(parameters);

            var LoadedView = DBViewExtentions.MapDBView<Model, ViewModel>(new List<Model>() { focus }).SingleOrDefault();

            Teradata.Reflection.ReflectionHelper.RefelectiveMap(LoadedView, this);

        }

        /// <summary>
        /// Get the focus row of the View to use as a start point for settings properties
        /// </summary>
        /// <returns></returns>
        public abstract Object GetFocusRow();

        /// <summary>
        /// Private method for setting the context when getting the focus row of nested views.
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public Object GetFocusRow(ObjectContext context)
        {
            this.Context = context;
            return this.GetFocusRow();
        }

        /// <summary>
        /// Get the focus row of the View to use as a start point for settings properties
        /// </summary>
        /// <returns></returns>
        public abstract Object GetFocusRow(Object parameters);

        /// <summary>
        /// Get the focus row of the View to use as a start point for settings properties
        /// </summary>
        /// <returns></returns>
        public Object GetFocusRow(Object parameters, ObjectContext context)
        {
            this.Context = context;
            return this.GetFocusRow(parameters);
        }

        public void SetAdmin(Boolean boo)
        {
            Admin = boo;
        }

        public void SetContext(ObjectContext context)
        {
            Context = context;
        }

        public ObjectContext GetContext()
        {
            return Context;
        }
    }

    public interface IDBView
    {
        void SetContext(ObjectContext context);
        ObjectContext GetContext();
        void DeleteView();
        void DeleteView(ObjectContext context);
        object GetFocusRow();
        Object GetFocusRow(Object parameters);
        Object GetFocusRow(Object parameters, ObjectContext context);
        Object GetFocusRow(ObjectContext context);
        void SaveViewChanges(bool Commit);
        void SaveViewChanges(bool Commit, ObjectContext context);
        void SetAdmin(bool boo);
        void LoadView<Model, ViewModel>(Object parameters) where ViewModel : IDBView;
    }

    public interface IDBViewList
    {
        void SaveViews(Boolean commit);
        void DeleteViews();
        void SetContext(ObjectContext context);
    }

    public interface IDelete
    {
        void Delete(ObjectContext context);
    }

    public class DBViewList<T> : List<T>, IDBViewList where T : IDBView
    {
        private ObjectContext context { get; set; }

        public void SaveViews(Boolean commit)
        {
            foreach (T view in this)
            {
                view.SaveViewChanges(false);
                if (commit)
                    this.context.SaveChanges();
            }
        }

        public void DeleteViews()
        {
            foreach (T view in this)
            {
                view.DeleteView();
            }
        }

        public void SetContext(ObjectContext context)
        {
            this.context = context;
            foreach (T view in this)
            {
                view.SetContext(context);
            }


        }

    }

    public static class Extensions
    {
        public static DBViewList<T> ToDBViewList<T>(this IEnumerable<T> ilist) where T : IDBView
        {
            DBViewList<T> list = new DBViewList<T>();
            list.AddRange(ilist);
            return list;
        }
    }
}

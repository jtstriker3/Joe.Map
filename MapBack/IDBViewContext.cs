using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Joe.MapBack
{
    public interface IDBViewContext : IDisposable
    {
        IPersistenceSet<TModel> GetIPersistenceSet<TModel>()
            where TModel : class;
        IPersistenceSet GetIPersistenceSet(Type TModel);
        IQueryable GetGenericQueryable(Type TModel);
        void Detach(Object obj);
        int SaveChanges();
    }
}

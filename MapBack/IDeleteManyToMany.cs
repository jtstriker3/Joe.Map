using Joe.Map;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Joe.MapBack
{
    public interface IDeleteManyToMany
    {
        void Delete(Object manyItem, IDBViewContext context, CollectionHandleType howToDelteFromCollection, IEnumerable parentList);
    }
}

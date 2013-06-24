using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Joe.Map.Test
{
    public class Record
    {
        public int ID { get; set; }
        public int Count { get; set; }
        public DateTime StartTime { get; set; }
        public int PersonID { get; set; }
        public virtual Person Person { get; set; }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Joe.Map.Test
{
    public class Person
    {
        public int ID { get; set; }
        public String Name { get; set; }
        public DateTime TimeEntered { get; set; }
        public DateTime TimeLeft { get; set; }
        public virtual List<Record> Records { get; set; }
    }

    [ViewFilter(Where = "TimeEntered:>:$StartTime")]
    public class PersonView
    {
        public int ID { get; set; }
        public String Name { get; set; }
        public DateTime TimeEntered { get; set; }
        public DateTime TimeLeft { get; set; }
        public IEnumerable<RecordView> Records { get; set; }
        [ViewMapping("Records", GroupBy = "Key")]
        public IEnumerable<IGrouping<Object, RecordGroup>> GroupedRecords { get; set; }
        //Filter
        public DateTime StartTime { get; set; }
    }

    public class RecordGroup
    {
        [ViewMapping("StartTime.Month")]
        public Object Key { get; set; }
        [ViewMapping("Count")]
        public Object Value { get; set; }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
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
        [ViewMapping(MapFunction = "NameAndIDMap", MapFunctionType = typeof(PersonCustomMaps))]
        public String NameAndID { get; set; }
        public DateTime TimeEntered { get; set; }
        public DateTime TimeLeft { get; set; }
        public IEnumerable<RecordView> Records { get; set; }
        [ViewMapping("Records", GroupBy = "Key")]
        public IEnumerable<IGrouping<Object, RecordGroup>> GroupedRecords { get; set; }
        //Filter
        public DateTime StartTime { get; set; }
    }

    public class PersonCustomMaps
    {
        public static LambdaExpression NameAndIDMap(Boolean queryDatabase)
        {
            if (queryDatabase)
            {
                Expression<Func<Person, String>> returnExpression = person => person.Name + System.Data.Entity.SqlServer.SqlFunctions.StringConvert((decimal)person.ID);
                return returnExpression;
            }
            else
            {
                Expression<Func<Person, String>> returnExpression = person => person.Name + System.Data.Entity.SqlServer.SqlFunctions.StringConvert((decimal)person.ID);
                return returnExpression;
            }
        }
    }

    public class RecordGroup
    {
        [ViewMapping("StartTime.Month")]
        public Object Key { get; set; }
        [ViewMapping("Count")]
        public Object Value { get; set; }
    }
}

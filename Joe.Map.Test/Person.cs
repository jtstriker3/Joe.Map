﻿using System;
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

        [ViewMapping("Records-Count", Where = "StartTime:>=:$StartTime", LinqFunction = "Sum")]
        public int? RecordSum { get; set; }
        //Filter
        public DateTime StartTime { get; set; }
    }
}
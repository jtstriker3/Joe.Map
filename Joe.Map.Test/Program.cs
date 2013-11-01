using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Joe.Map.Test
{
    class Program
    {
        static void Main(string[] args)
        {
            //FilterTest();
            //NullableFkTest();
            // NonGenericMappingTest();
            //GroupedFilterTest();
            FilterTest();
        }

        public static void FilterTest()
        {

            var filter = new PersonView() { StartTime = DateTime.Now.AddDays(-30) };
            var context = new TestContext();
            //var test = context.People.Select(p => new
            //{
            //    x = String.Concat(p.Name, System.Data.Entity.SqlServer.SqlFunctions.StringConvert((double)p.ID).Trim())
            //}).ToList();

            var people = context.People.Map<Person, PersonView>(filter).Filter("Name.Length:>:20:or:(Name:=:Brice Lambson:and:Records-Count:Contains:11)");

            foreach (var person in people)
            {
                Console.WriteLine(person.NameAndID);
                foreach (var record in person.GroupedRecords)
                {
                    Console.WriteLine("Sum Counts For Group: " + record.Sum(r => (int)r.Value));
                }
            }
            //Console.WriteLine("--No Filter--");
            //people = context.People.Map<Person, PersonView>();

            //foreach (var person in people)
            //{
            //    Console.WriteLine(person.Name);
            //    foreach (var record in person.GroupedRecords)
            //    {
            //        Console.WriteLine("Sum Counts For Group: " + record.Sum(r => (int)r.Value));
            //    }
            //}

            Console.ReadLine();
        }

        public static void NonGenericMappingTest()
        {
            var context = new TestContext();
            var people = (IEnumerable<PersonView>)context.People.Map(typeof(PersonView));

            foreach (var person in people)
                Console.WriteLine(person.Name);
        }
        //public static void NullableFkTest()
        //{
        //    var context = new TestContext();
        //    var records = context.Records.Map<Record, RecordView>().Filter("Person.Name:Contains:Brice");

        //    foreach (var record in records)
        //        Console.WriteLine(record.Person != null ? record.Person.Name : "No Person");
        //}

        public static void GroupedFilterTest()
        {
            var filterBuilder = new FilterBuilder("Name:=:Joe:and:(Age:=:27:or:Age:=:28:and:(NotOverride:=:true:or:Age:>:27)):and:(Name:=:Jim):and:Name:=:Joe", typeof(TempPerson), false, null).BuildWhereLambda();
        }

        public class TempPerson
        {
            public String Name { get; set; }
            public int Age { get; set; }
            public Boolean NotOverride { get; set; }
        }
    }
}

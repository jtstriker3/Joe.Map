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
            NonGenericMappingTest();
        }

        public static void FilterTest()
        {
            var filter = new PersonView() { StartTime = DateTime.Now.AddDays(-30) };
            var context = new TestContext();

            var people = context.People.Map<Person, PersonView>(filter).Filter("Records-Count:Contains:11");

            foreach (var person in people)
            {
                Console.WriteLine(person.Name + " Record Count: " + person.RecordSum);
            }
            Console.WriteLine("--No Filter--");
            people = context.People.Map<Person, PersonView>();

            foreach (var person in people)
            {
                Console.WriteLine(person.Name + " Record Count: " + person.RecordSum);
            }

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
    }
}

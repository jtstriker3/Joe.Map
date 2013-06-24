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
            FilterTest();
        }

        public static void FilterTest()
        {
            var filter = new PersonView() { StartTime = DateTime.Now.AddDays(-19) };
            var context = new TestContext();

            var people = context.People.Map<Person, PersonView>(filter);

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
    }
}

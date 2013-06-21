namespace Joe.Map.Test.Migrations
{
    using System;
    using System.Data.Entity;
    using System.Data.Entity.Migrations;
    using System.Linq;

    internal sealed class Configuration : DbMigrationsConfiguration<Joe.Map.Test.TestContext>
    {
        public Configuration()
        {
            AutomaticMigrationsEnabled = true;
        }

        protected override void Seed(Joe.Map.Test.TestContext context)
        {
            context.People.AddOrUpdate(
              p => p.ID,
              new Person { Name = "Andrew Peters", TimeEntered = DateTime.Now.AddDays(-20), TimeLeft = DateTime.Now.AddDays(-19) },
              new Person { Name = "Brice Lambson", TimeEntered = DateTime.Now.AddDays(-10), TimeLeft = DateTime.Now.AddDays(-9) },
              new Person { Name = "Rowan Miller", TimeEntered = DateTime.Now.AddDays(-5), TimeLeft = DateTime.Now.AddDays(-4) }
            );

        }
    }
}

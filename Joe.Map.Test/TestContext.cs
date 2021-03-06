﻿using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Joe.Map.Test
{
    public class TestContext : DbContext
    {
        public DbSet<Person> People { get; set; }
        public DbSet<Record> Records { get; set; }

        public TestContext()
        {
            Database.SetInitializer<TestContext>(new DropCreateDatabaseIfModelChanges<TestContext>());
        }
    }
}

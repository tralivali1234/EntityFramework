// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Data.SqlClient;
using System.Linq;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.EntityFrameworkCore.SqlServer.FunctionalTests.Utilities;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Microsoft.EntityFrameworkCore.SqlServer.FunctionalTests
{
    public class ConnectionSpecificationTest
    {
        [Fact]
        public void Can_specify_connection_string_in_OnConfiguring()
        {
            var serviceProvider
                = new ServiceCollection()
                    .AddDbContext<StringInOnConfiguringContext>()
                    .BuildServiceProvider();

            using (SqlServerTestStore.GetNorthwindStore())
            {
                using (var context = serviceProvider.GetRequiredService<StringInOnConfiguringContext>())
                {
                    Assert.True(context.Customers.Any());
                }
            }
        }

        [Fact]
        public void Can_specify_connection_string_in_OnConfiguring_with_default_service_provider()
        {
            using (SqlServerTestStore.GetNorthwindStore())
            {
                using (var context = new StringInOnConfiguringContext())
                {
                    Assert.True(context.Customers.Any());
                }
            }
        }

        private class StringInOnConfiguringContext : NorthwindContextBase
        {
            protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
                => optionsBuilder.UseSqlServer(SqlServerTestStore.NorthwindConnectionString, b => b.ApplyConfiguration());
        }

        [Fact]
        public void Can_specify_connection_in_OnConfiguring()
        {
            var serviceProvider
                = new ServiceCollection()
                    .AddScoped(p => new SqlConnection(SqlServerTestStore.NorthwindConnectionString))
                    .AddDbContext<ConnectionInOnConfiguringContext>().BuildServiceProvider();

            using (SqlServerTestStore.GetNorthwindStore())
            {
                using (var context = serviceProvider.GetRequiredService<ConnectionInOnConfiguringContext>())
                {
                    Assert.True(context.Customers.Any());
                }
            }
        }

        [Fact]
        public void Can_specify_connection_in_OnConfiguring_with_default_service_provider()
        {
            using (SqlServerTestStore.GetNorthwindStore())
            {
                using (var context = new ConnectionInOnConfiguringContext(new SqlConnection(SqlServerTestStore.NorthwindConnectionString)))
                {
                    Assert.True(context.Customers.Any());
                }
            }
        }

        private class ConnectionInOnConfiguringContext : NorthwindContextBase
        {
            private readonly SqlConnection _connection;

            public ConnectionInOnConfiguringContext(SqlConnection connection)
            {
                _connection = connection;
            }

            protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
                => optionsBuilder.UseSqlServer(_connection, b => b.ApplyConfiguration());

            public override void Dispose()
            {
                _connection.Dispose();
                base.Dispose();
            }
        }

        private class StringInConfigContext : NorthwindContextBase
        {
            protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
                => optionsBuilder.UseSqlServer("Database=Crunchie", b => b.ApplyConfiguration());
        }

        [Fact]
        public void Throws_if_no_connection_found_in_config_without_UseSqlServer()
        {
            var serviceProvider
                = new ServiceCollection()
                    .AddDbContext<NoUseSqlServerContext>().BuildServiceProvider();

            using (var context = serviceProvider.GetRequiredService<NoUseSqlServerContext>())
            {
                Assert.Equal(
                    CoreStrings.NoProviderConfigured,
                    Assert.Throws<InvalidOperationException>(() => context.Customers.Any()).Message);
            }
        }

        [Fact]
        public void Throws_if_no_config_without_UseSqlServer()
        {
            var serviceProvider
                = new ServiceCollection()
                    .AddDbContext<NoUseSqlServerContext>().BuildServiceProvider();

            using (var context = serviceProvider.GetRequiredService<NoUseSqlServerContext>())
            {
                Assert.Equal(
                    CoreStrings.NoProviderConfigured,
                    Assert.Throws<InvalidOperationException>(() => context.Customers.Any()).Message);
            }
        }

        private class NoUseSqlServerContext : NorthwindContextBase
        {
        }

        [Fact]
        public void Can_depend_on_DbContextOptions()
        {
            var serviceProvider
                = new ServiceCollection()
                    .AddScoped(p => new SqlConnection(SqlServerTestStore.NorthwindConnectionString))
                    .AddDbContext<OptionsContext>()
                    .BuildServiceProvider();

            using (SqlServerTestStore.GetNorthwindStore())
            {
                using (var context = serviceProvider.GetRequiredService<OptionsContext>())
                {
                    Assert.True(context.Customers.Any());
                }
            }
        }

        [Fact]
        public void Can_depend_on_DbContextOptions_with_default_service_provider()
        {
            using (SqlServerTestStore.GetNorthwindStore())
            {
                using (var context = new OptionsContext(
                    new DbContextOptions<OptionsContext>(),
                    new SqlConnection(SqlServerTestStore.NorthwindConnectionString)))
                {
                    Assert.True(context.Customers.Any());
                }
            }
        }

        private class OptionsContext : NorthwindContextBase
        {
            private readonly SqlConnection _connection;
            private readonly DbContextOptions<OptionsContext> _options;

            public OptionsContext(DbContextOptions<OptionsContext> options, SqlConnection connection)
                : base(options)
            {
                _options = options;
                _connection = connection;
            }

            protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
            {
                Assert.Same(_options, optionsBuilder.Options);

                optionsBuilder.UseSqlServer(_connection, b => b.ApplyConfiguration());

                Assert.NotSame(_options, optionsBuilder.Options);
            }

            public override void Dispose()
            {
                _connection.Dispose();
                base.Dispose();
            }
        }

        [Fact]
        public void Can_depend_on_non_generic_options_when_only_one_context()
        {
            var serviceProvider
                = new ServiceCollection()
                    .AddDbContext<NonGenericOptionsContext>()
                    .BuildServiceProvider();

            using (SqlServerTestStore.GetNorthwindStore())
            {
                using (var context = serviceProvider.GetRequiredService<NonGenericOptionsContext>())
                {
                    Assert.True(context.Customers.Any());
                }
            }
        }

        [Fact]
        public void Can_depend_on_non_generic_options_when_only_one_context_with_default_service_provider()
        {
            using (SqlServerTestStore.GetNorthwindStore())
            {
                using (var context = new NonGenericOptionsContext(new DbContextOptions<DbContext>()))
                {
                    Assert.True(context.Customers.Any());
                }
            }
        }

        private class NonGenericOptionsContext : NorthwindContextBase
        {
            private readonly DbContextOptions _options;

            public NonGenericOptionsContext(DbContextOptions options)
                : base(options)
            {
                _options = options;
            }

            protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
            {
                Assert.Same(_options, optionsBuilder.Options);

                optionsBuilder.UseSqlServer(SqlServerTestStore.NorthwindConnectionString, b => b.ApplyConfiguration());

                Assert.NotSame(_options, optionsBuilder.Options);
            }
        }

        private class NorthwindContextBase : DbContext
        {
            protected NorthwindContextBase()
            {
            }

            protected NorthwindContextBase(DbContextOptions options)
                : base(options)
            {
            }

            public DbSet<Customer> Customers { get; set; }

            protected override void OnModelCreating(ModelBuilder modelBuilder)
            {
                modelBuilder.Entity<Customer>(b =>
                    {
                        b.HasKey(c => c.CustomerID);
                        b.ForSqlServerToTable("Customers");
                    });
            }
        }

        private class Customer
        {
            public string CustomerID { get; set; }
            public string CompanyName { get; set; }
            public string Fax { get; set; }
        }
    }
}

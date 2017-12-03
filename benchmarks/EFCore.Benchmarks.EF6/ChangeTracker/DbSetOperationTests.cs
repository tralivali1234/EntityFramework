// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Data.Entity;
using BenchmarkDotNet.Attributes;
using Microsoft.EntityFrameworkCore.Benchmarks.EF6.Models.Orders;
using Microsoft.EntityFrameworkCore.Benchmarks.Models.Orders;

namespace Microsoft.EntityFrameworkCore.Benchmarks.EF6.ChangeTracker
{
    public class DbSetOperationTests
    {
        public abstract class Base
        {
            private readonly DbSetOperationFixture _fixture = new DbSetOperationFixture();

            protected List<Customer> _customersWithoutPk;
            protected List<Customer> _customersWithPk;
            protected OrdersContext _context;

            protected abstract bool AutoDetectChanges { get; }

            [GlobalSetup]
            public virtual void CreateCustomers()
            {
                _customersWithoutPk = _fixture.CreateCustomers(20000, setPrimaryKeys: false);
                _customersWithPk = _fixture.CreateCustomers(20000, setPrimaryKeys: true);
            }

            [IterationSetup]
            public virtual void InitializeContext()
            {
                _context = _fixture.CreateContext();
                _context.Configuration.AutoDetectChangesEnabled = AutoDetectChanges;
            }

            [IterationCleanup]
            public virtual void CleanupContext()
            {
                _context.Dispose();
            }
        }

        public abstract class AddDataVariations : Base
        {
            [Benchmark]
            public virtual void Add()
            {
                foreach (var customer in _customersWithoutPk)
                {
                    _context.Customers.Add(customer);
                }
            }

            [Benchmark]
            public virtual void AddRange()
            {
                _context.Customers.AddRange(_customersWithoutPk);
            }

            [Benchmark]
            public virtual void Attach()
            {
                foreach (var customer in _customersWithPk)
                {
                    _context.Customers.Attach(customer);
                }
            }

            // Note: AttachRange() not implemented because there is no
            //       API for bulk attach in EF6.x
        }

        public abstract class ExistingDataVariations : Base
        {
            [IterationSetup]
            public override void InitializeContext()
            {
                base.InitializeContext();
                _customersWithPk.ForEach(c => _context.Customers.Attach(c));
            }

            [Benchmark]
            public virtual void Remove()
            {
                foreach (var customer in _customersWithPk)
                {
                    _context.Customers.Remove(customer);
                }
            }

            [Benchmark]
            public virtual void RemoveRange()
            {
                _context.Customers.RemoveRange(_customersWithPk);
            }

            [Benchmark]
            public virtual void Update()
            {
                foreach (var customer in _customersWithPk)
                {
                    _context.Entry(customer).State = EntityState.Modified;
                }
            }

            // Note: UpdateRange() not implemented because there is no
            //       API for bulk update in EF6.x
        }

        [SingleRunBenchmarkJob]
        public class AddDataVariationsWithAutoDetectChangesOn : AddDataVariations
        {
            protected override bool AutoDetectChanges => true;
        }

        [SingleRunBenchmarkJob]
        public class ExistingDataVariationsWithAutoDetectChangesOn : ExistingDataVariations
        {
            protected override bool AutoDetectChanges => true;
        }

        [BenchmarkJob]
        [MemoryDiagnoser]
        public class AddDataVariationsWithAutoDetectChangesOff : AddDataVariations
        {
            protected override bool AutoDetectChanges => false;
        }

        [BenchmarkJob]
        [MemoryDiagnoser]
        public class ExistingDataVariationsWithAutoDetectChangesOff : ExistingDataVariations
        {
            protected override bool AutoDetectChanges => false;
        }

        public class DbSetOperationFixture : OrdersFixture
        {
            public DbSetOperationFixture()
                : base("Perf_ChangeTracker_DbSetOperation_EF6", 0, 0, 0, 0)
            {
            }
        }
    }
}

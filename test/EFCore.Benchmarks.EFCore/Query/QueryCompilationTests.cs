// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore.Benchmarks.EFCore.Models.Orders;
using Xunit;

namespace Microsoft.EntityFrameworkCore.Benchmarks.EFCore.Query
{
    internal class QueryCompilationTests : IClassFixture<QueryCompilationTests.QueryCompilationFixture>
    {
        private readonly QueryCompilationFixture _fixture;

        public QueryCompilationTests(QueryCompilationFixture fixture)
        {
            _fixture = fixture;
        }

        [Benchmark]
        [BenchmarkVariation("Default (10 queries)")]
        public void ToList(IMetricCollector collector)
        {
            using (var context = _fixture.CreateContext())
            {
                var query = context.Products
                    .AsNoTracking();

                using (collector.StartCollection())
                {
                    for (var i = 0; i < 10; i++)
                    {
                        query.ToList();
                    }
                }

                Assert.Equal(0, query.Count());
            }
        }

        [Benchmark]
        [BenchmarkVariation("Default (10 queries)")]
        public void FilterOrderProject(IMetricCollector collector)
        {
            using (var context = _fixture.CreateContext())
            {
                var query = context.Products
                    .AsNoTracking()
                    .Where(p => p.Retail < 1000)
                    .OrderBy(p => p.Name).ThenBy(p => p.Retail)
                    .Select(p => new
                    {
                        p.ProductId,
                        p.Name,
                        p.Description,
                        p.ActualStockLevel,
                        p.SKU,
                        Savings = p.Retail - p.CurrentPrice,
                        Surplus = p.ActualStockLevel - p.TargetStockLevel
                    });

                using (collector.StartCollection())
                {
                    for (var i = 0; i < 10; i++)
                    {
                        query.ToList();
                    }
                }

                Assert.Equal(0, query.Count());
            }
        }

        public class QueryCompilationFixture : OrdersFixture
        {
            private readonly IServiceProvider _noQueryCacheServiceProvider;

            public QueryCompilationFixture()
                : base("Perf_Query_Compilation", 0, 0, 0, 0)
            {
                _noQueryCacheServiceProvider = new ServiceCollection()
                    .AddEntityFrameworkSqlServer()
                    .AddSingleton<IMemoryCache, NonCachingMemoryCache>()
                    .BuildServiceProvider();
            }

            public override OrdersContext CreateContext() 
                => new OrdersContext(_noQueryCacheServiceProvider, ConnectionString);

            private class NonCachingMemoryCache : IMemoryCache
            {
                public bool TryGetValue(object key, out object value)
                {
                    value = null;
                    return false;
                }

                public ICacheEntry CreateEntry(object key) => null;

                public void Remove(object key)
                {
                }

                public void Dispose()
                {
                }
            }
        }
    }
}

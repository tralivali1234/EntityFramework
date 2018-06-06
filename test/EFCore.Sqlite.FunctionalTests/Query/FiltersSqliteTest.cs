﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Xunit.Abstractions;

namespace Microsoft.EntityFrameworkCore.Query
{
    public class FiltersSqliteTest : FiltersTestBase<NorthwindQuerySqliteFixture<NorthwindFiltersCustomizer>>
    {
        public FiltersSqliteTest(NorthwindQuerySqliteFixture<NorthwindFiltersCustomizer> fixture, ITestOutputHelper testOutputHelper)
            : base(fixture)
        {
            fixture.TestSqlLoggerFactory.Clear();
            //fixture.TestSqlLoggerFactory.SetTestOutputHelper(testOutputHelper);
        }

#if !Test20
        public override void Count_query()
        {
            base.Count_query();
            AssertSql(
                @"@__ef_filter__TenantPrefix_0='B' (Size = 1)

SELECT COUNT(*)
FROM ""Customers"" AS ""c""
WHERE (""c"".""CompanyName"" LIKE @__ef_filter__TenantPrefix_0 || '%' AND (substr(""c"".""CompanyName"", 1, length(@__ef_filter__TenantPrefix_0)) = @__ef_filter__TenantPrefix_0)) OR (@__ef_filter__TenantPrefix_0 = '')");
        }
#endif

        private void AssertSql(params string[] expected)
            => Fixture.TestSqlLoggerFactory.AssertBaseline(expected);

        private void AssertContainsSql(params string[] expected)
            => Fixture.TestSqlLoggerFactory.AssertBaseline(expected, assertOrder: false);
    }
}

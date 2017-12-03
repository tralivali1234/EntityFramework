// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.EntityFrameworkCore.TestUtilities;

namespace Microsoft.EntityFrameworkCore
{
    public class NotificationEntitiesOracleTest : NotificationEntitiesTestBase<NotificationEntitiesOracleTest.NotificationEntitiesOracleFixture>
    {
        public NotificationEntitiesOracleTest(NotificationEntitiesOracleFixture fixture)
            : base(fixture)
        {
        }

        public class NotificationEntitiesOracleFixture : NotificationEntitiesFixtureBase
        {
            protected override ITestStoreFactory TestStoreFactory => OracleTestStoreFactory.Instance;
        }
    }
}

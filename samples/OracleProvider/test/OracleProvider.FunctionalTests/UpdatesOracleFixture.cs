// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.EntityFrameworkCore.TestModels.UpdatesModel;
using Microsoft.EntityFrameworkCore.TestUtilities;

namespace Microsoft.EntityFrameworkCore
{
    public class UpdatesOracleFixture : UpdatesRelationalFixture
    {
        protected override ITestStoreFactory TestStoreFactory => OracleTestStoreFactory.Instance;

        protected override void OnModelCreating(ModelBuilder modelBuilder, DbContext context)
        {
            base.OnModelCreating(modelBuilder, context);

            modelBuilder.Entity<Product>()
                .Property(p => p.Price).HasColumnType("decimal(18,2)");
        }
    }
}

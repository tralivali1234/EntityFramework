// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.EntityFrameworkCore.Specification.Tests;
using Xunit;

namespace Microsoft.EntityFrameworkCore.InMemory.FunctionalTests
{
    public class FiltersInheritanceInMemoryTest : FiltersInheritanceTestBase<InheritanceInMemoryFixture>
    {
        public FiltersInheritanceInMemoryTest(InheritanceInMemoryFixture fixture)
            : base(fixture)
        {
        }
    }
}

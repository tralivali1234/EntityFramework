// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.EntityFrameworkCore.Sqlite.FunctionalTests;
using Xunit;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Microsoft.EntityFrameworkCore.Sqlite.Tests
{
    public class SqliteEventIdTest
    {
        [Fact]
        public void Every_eventId_has_a_logger_method_and_logs_when_level_enabled()
        {
            var entityType = new EntityType(typeof(object), new Model(new ConventionSet()), ConfigurationSource.Convention);

            var fakeFactories = new Dictionary<Type, Func<object>>
            {
                { typeof(string), () => "Fake" },
                { typeof(IEntityType), () => entityType },
                { typeof(ISequence), () => new FakeSequence() }
            };

            SqliteTestHelpers.Instance.TestEventLogging(
                typeof(SqliteEventId),
                typeof(SqliteLoggerExtensions),
                fakeFactories);
        }

        private class FakeSequence : ISequence
        {
            public string Name => "SequenceName";
            public string Schema => throw new NotImplementedException();
            public long StartValue => throw new NotImplementedException();
            public int IncrementBy => throw new NotImplementedException();
            public long? MinValue => throw new NotImplementedException();
            public long? MaxValue => throw new NotImplementedException();
            public Type ClrType => throw new NotImplementedException();
            public IModel Model => throw new NotImplementedException();
            public bool IsCyclic => throw new NotImplementedException();
        }
    }
}

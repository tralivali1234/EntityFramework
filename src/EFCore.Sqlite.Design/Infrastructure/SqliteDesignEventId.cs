﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Microsoft.EntityFrameworkCore.Infrastructure
{
    /// <summary>
    ///     <para>
    ///         Event IDs for relational design events that correspond to messages logged to an <see cref="ILogger" />
    ///         and events sent to a <see cref="DiagnosticSource" />.
    ///     </para>
    ///     <para>
    ///         These IDs are also used with <see cref="WarningsConfigurationBuilder" /> to configure the
    ///         behavior of warnings.
    ///     </para>
    /// </summary>
    public static class SqliteDesignEventId
    {
        // Warning: These values must not change between releases.
        // Only add new values to the end of sections, never in the middle.
        // Try to use <Noun><Verb> naming and be consistent with existing names.
        private enum Id
        {
            // Scaffolding events
            ColumnFound = CoreEventId.ProviderDesignBaseId,
            ForeignKeyColumnFound,
            SchemasNotSupportedWarning
        }

        private static readonly string _scaffoldingPrefix = LoggerCategory.Scaffolding.Name + ".";
        private static EventId MakeScaffoldingId(Id id) => new EventId((int)id, _scaffoldingPrefix + id);

        /// <summary>
        ///     A column was found.
        ///     This event is in the <see cref="LoggerCategory.Scaffolding" /> category.
        /// </summary>
        public static readonly EventId ColumnFound = MakeScaffoldingId(Id.ColumnFound);

        /// <summary>
        ///     A column of a foreign key was found.
        ///     This event is in the <see cref="LoggerCategory.Scaffolding" /> category.
        /// </summary>
        public static readonly EventId ForeignKeyColumnFound = MakeScaffoldingId(Id.ForeignKeyColumnFound);

        /// <summary>
        ///     SQLite does not support schemas.
        ///     This event is in the <see cref="LoggerCategory.Scaffolding" /> category.
        /// </summary>
        public static readonly EventId SchemasNotSupportedWarning = MakeScaffoldingId(Id.SchemasNotSupportedWarning);
    }
}

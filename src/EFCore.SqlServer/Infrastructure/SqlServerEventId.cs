// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Microsoft.EntityFrameworkCore.Infrastructure
{
    /// <summary>
    ///     <para>
    ///         Event IDs for SQL Server events that correspond to messages logged to an <see cref="ILogger" />
    ///         and events sent to a <see cref="DiagnosticSource" />.
    ///     </para>
    ///     <para>
    ///         These IDs are also used with <see cref="WarningsConfigurationBuilder" /> to configure the
    ///         behavior of warnings.
    ///     </para>
    /// </summary>
    public static class SqlServerEventId
    {
        // Warning: These values must not change between releases.
        // Only add new values to the end of sections, never in the middle.
        // Try to use <Noun><Verb> naming and be consistent with existing names.
        private enum Id
        {
            // Model validation events
            DecimalTypeDefaultWarning = CoreEventId.ProviderBaseId,
            ByteIdentityColumnWarning
        }

        private static readonly string _validationPrefix = LoggerCategory.Model.Validation.Name + ".";
        private static EventId MakeValidationId(Id id) => new EventId((int)id, _validationPrefix + id);

        /// <summary>
        ///     No explicit type for a decimal column.
        ///     This event is in the <see cref="LoggerCategory.Model.Validation" /> category.
        /// </summary>
        public static readonly EventId DecimalTypeDefaultWarning = MakeValidationId(Id.DecimalTypeDefaultWarning);

        /// <summary>
        ///     A byte property is set up to use a SQL Server identity column.
        ///     This event is in the <see cref="LoggerCategory.Model.Validation" /> category.
        /// </summary>
        public static readonly EventId ByteIdentityColumnWarning = MakeValidationId(Id.ByteIdentityColumnWarning);
    }
}

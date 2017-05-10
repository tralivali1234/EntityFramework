// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Diagnostics;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Microsoft.EntityFrameworkCore.Diagnostics
{
    /// <summary>
    ///     The <see cref="DiagnosticSource" /> event payload for
    ///     <see cref="RelationalEventId" /> migration scripting events.
    /// </summary>
    public class MigrationScriptingData : MigrationData
    {
        /// <summary>
        ///     Constructs the event payload.
        /// </summary>
        /// <param name="migrator">
        ///     The <see cref="IMigrator" /> in use.
        /// </param>
        /// <param name="migration">
        ///     The <see cref="Migration" /> being processed.
        /// </param>
        /// <param name="fromMigration">
        ///     The migration that scripting is starting from.
        /// </param>
        /// <param name="toMigration">
        ///     The migration that scripting is going to.
        /// </param>
        /// <param name="idempotent">
        ///     Indicates whether or not the script is idempotent.
        /// </param>
        public MigrationScriptingData(
            [NotNull] IMigrator migrator,
            [NotNull] Migration migration,
            [CanBeNull] string fromMigration,
            [CanBeNull] string toMigration,
            bool idempotent)
            : base(migrator, migration)
        {
            FromMigration = fromMigration;
            ToMigration = toMigration;
            Idempotent = idempotent;
        }

        /// <summary>
        ///     The migration that scripting is starting from.
        /// </summary>
        public virtual string FromMigration { get; }

        /// <summary>
        ///     The migration that scripting is going to.
        /// </summary>
        public virtual string ToMigration { get; }

        /// <summary>
        ///     Indicates whether or not the script is idempotent.
        /// </summary>
        public virtual bool Idempotent { get; }
    }
}

// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore.Query.Internal;

namespace Microsoft.EntityFrameworkCore.Storage
{
    /// <summary>
    ///     <para>
    ///         Represents a connection with a relational database.
    ///     </para>
    ///     <para>
    ///         This type is typically used by database providers (and other extensions). It is generally
    ///         not used in application code.
    ///     </para>
    /// </summary>
    public interface IRelationalConnection : IRelationalTransactionManager, IDisposable
    {
        /// <summary>
        ///     Gets the connection string for the database.
        /// </summary>
        string ConnectionString { get; }

        /// <summary>
        ///     Gets the underlying <see cref="System.Data.Common.DbConnection" /> used to connect to the database.
        /// </summary>
        DbConnection DbConnection { get; }

        /// <summary>
        ///     Gets the connection identifier.
        /// </summary>
        Guid ConnectionId { get; }

        /// <summary>
        ///     Gets the timeout for executing a command against the database.
        /// </summary>
        int? CommandTimeout { get; set; }

        /// <summary>
        ///     Opens the connection to the database.
        /// </summary>
        /// <returns> True if the underlying connection was actually opened; false otherwise. </returns>
        bool Open();

        /// <summary>
        ///     Asynchronously opens the connection to the database.
        /// </summary>
        /// <param name="cancellationToken">
        ///     A <see cref="CancellationToken" /> to observe while waiting for the task to complete.
        /// </param>
        /// <returns>
        ///     A task that represents the asynchronous operation, with a value of true if the connection
        ///     was actually opened.
        /// </returns>
        Task<bool> OpenAsync(CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        ///     Closes the connection to the database.
        /// </summary>
        /// <returns> True if the underlying connection was actually closed; false otherwise. </returns>
        bool Close();

        /// <summary>
        ///     Gets a value indicating whether the multiple active result sets feature is enabled.
        /// </summary>
        bool IsMultipleActiveResultSetsEnabled { get; }

        /// <summary>
        ///     Gets or sets the active cursor.
        /// </summary>
        IValueBufferCursor ActiveCursor { get; [param: CanBeNull] set; }

        /// <summary>
        ///     Gets the current transaction.
        /// </summary>
        new IDbContextTransaction CurrentTransaction { get; }
    }
}

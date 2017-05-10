using System;
using System.Data.Common;
using System.Diagnostics;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Microsoft.EntityFrameworkCore.Diagnostics
{
    /// <summary>
    ///     The <see cref="DiagnosticSource" /> event payload base class for
    ///     <see cref="RelationalEventId" /> transaction error events.
    /// </summary>
    public class TransactionErrorData : TransactionEndData
    {
        /// <summary>
        ///     Constructs the event payload.
        /// </summary>
        /// <param name="transaction">
        ///     The <see cref="DbTransaction" />.
        /// </param>
        /// <param name="transactionId">
        ///     A correlation ID that identifies the Entity Framework transaction being used.
        /// </param>
        /// <param name="connectionId">
        ///     A correlation ID that identifies the <see cref="DbConnection" /> instance being used.
        /// </param>
        /// <param name="action">
        ///     One of "Commit" or "Rollback".
        /// </param>
        /// <param name="exception">
        ///     The exception that was thrown when the transaction failed.
        /// </param>
        /// <param name="timestamp">
        ///     A timestamp from <see cref="Stopwatch.GetTimestamp" /> that can be used for timing.
        /// </param>
        /// <param name="duration">
        ///     The duration of execution as ticks from <see cref="Stopwatch.GetTimestamp" />.
        /// </param>
        public TransactionErrorData(
            [NotNull] DbTransaction transaction,
            Guid transactionId,
            Guid connectionId,
            [NotNull] string action,
            [NotNull] Exception exception,
            long timestamp,
            long duration)
            : base(transaction, transactionId, connectionId, timestamp, duration)
        {
            Action = action;
            Exception = exception;
        }

        /// <summary>
        ///     One of "Commit" or "Rollback".
        /// </summary>
        public virtual string Action { get; }

        /// <summary>
        ///     The exception that was thrown when the transaction failed.
        /// </summary>
        public virtual Exception Exception { get; }
    }
}
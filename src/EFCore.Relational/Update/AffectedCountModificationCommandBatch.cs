// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.EntityFrameworkCore.Storage;

namespace Microsoft.EntityFrameworkCore.Update
{
    /// <summary>
    ///     <para>
    ///         A <see cref="ReaderModificationCommandBatch" /> for providers which append an SQL query to find out
    ///         how many rows were affected (see <see cref="UpdateSqlGenerator.AppendSelectAffectedCountCommand" />).
    ///     </para>
    ///     <para>
    ///         This type is typically used by database providers; it is generally not used in application code.
    ///     </para>
    /// </summary>
    public abstract class AffectedCountModificationCommandBatch : ReaderModificationCommandBatch
    {
        /// <summary>
        ///     Creates a new <see cref="AffectedCountModificationCommandBatch" /> instance.
        /// </summary>
        /// <param name="commandBuilderFactory"> The builder to build commands. </param>
        /// <param name="sqlGenerationHelper"> A helper for SQL generation. </param>
        /// <param name="updateSqlGenerator"> A SQL generator for insert, update, and delete commands. </param>
        /// <param name="valueBufferFactoryFactory">
        ///     A factory for creating factories for creating <see cref="ValueBuffer" />s to be used when reading from the data reader.
        /// </param>
        protected AffectedCountModificationCommandBatch(
            [NotNull] IRelationalCommandBuilderFactory commandBuilderFactory,
            [NotNull] ISqlGenerationHelper sqlGenerationHelper,
            [NotNull] IUpdateSqlGenerator updateSqlGenerator,
            [NotNull] IRelationalValueBufferFactoryFactory valueBufferFactoryFactory)
            : base(commandBuilderFactory, sqlGenerationHelper, updateSqlGenerator, valueBufferFactoryFactory)
        {
        }

        /// <summary>
        ///     Consumes the data reader created by <see cref="ReaderModificationCommandBatch.Execute" />.
        /// </summary>
        /// <param name="reader"> The data reader. </param>
        protected override void Consume(RelationalDataReader reader)
        {
            Debug.Assert(CommandResultSet.Count == ModificationCommands.Count);
            var commandIndex = 0;

            try
            {
                var actualResultSetCount = 0;
                do
                {
                    while (commandIndex < CommandResultSet.Count
                           && CommandResultSet[commandIndex] == ResultSetMapping.NoResultSet)
                    {
                        commandIndex++;
                    }

                    if (commandIndex < CommandResultSet.Count)
                    {
                        commandIndex = ModificationCommands[commandIndex].RequiresResultPropagation
                            ? ConsumeResultSetWithPropagation(commandIndex, reader)
                            : ConsumeResultSetWithoutPropagation(commandIndex, reader);
                        actualResultSetCount++;
                    }
                }
                while (commandIndex < CommandResultSet.Count
                       && reader.DbDataReader.NextResult());

#if DEBUG
                while (commandIndex < CommandResultSet.Count
                       && CommandResultSet[commandIndex] == ResultSetMapping.NoResultSet)
                {
                    commandIndex++;
                }

                Debug.Assert(
                    commandIndex == ModificationCommands.Count,
                    "Expected " + ModificationCommands.Count + " results, got " + commandIndex);

                var expectedResultSetCount = CommandResultSet.Count(e => e == ResultSetMapping.LastInResultSet);

                Debug.Assert(
                    actualResultSetCount == expectedResultSetCount,
                    "Expected " + expectedResultSetCount + " result sets, got " + actualResultSetCount);
#endif
            }
            catch (Exception ex) when (!(ex is DbUpdateException))
            {
                throw new DbUpdateException(
                    RelationalStrings.UpdateStoreException,
                    ex,
                    ModificationCommands[commandIndex].Entries);
            }
        }

        /// <summary>
        ///     Consumes the data reader created by <see cref="ReaderModificationCommandBatch.ExecuteAsync" />.
        /// </summary>
        /// <param name="reader"> The data reader. </param>
        /// <param name="cancellationToken">A <see cref="CancellationToken" /> to observe while waiting for the task to complete.</param>
        /// <returns> A task that represents the asynchronous operation. </returns>
        protected override async Task ConsumeAsync(
            RelationalDataReader reader,
            CancellationToken cancellationToken = default)
        {
            Debug.Assert(CommandResultSet.Count == ModificationCommands.Count);
            var commandIndex = 0;

            try
            {
                var actualResultSetCount = 0;
                do
                {
                    while (commandIndex < CommandResultSet.Count
                           && CommandResultSet[commandIndex] == ResultSetMapping.NoResultSet)
                    {
                        commandIndex++;
                    }

                    if (commandIndex < CommandResultSet.Count)
                    {
                        commandIndex = ModificationCommands[commandIndex].RequiresResultPropagation
                            ? await ConsumeResultSetWithPropagationAsync(commandIndex, reader, cancellationToken)
                            : await ConsumeResultSetWithoutPropagationAsync(commandIndex, reader, cancellationToken);
                        actualResultSetCount++;
                    }
                }
                while (commandIndex < CommandResultSet.Count
                       && await reader.DbDataReader.NextResultAsync(cancellationToken));

#if DEBUG
                while (commandIndex < CommandResultSet.Count
                       && CommandResultSet[commandIndex] == ResultSetMapping.NoResultSet)
                {
                    commandIndex++;
                }

                Debug.Assert(
                    commandIndex == ModificationCommands.Count,
                    "Expected " + ModificationCommands.Count + " results, got " + commandIndex);

                var expectedResultSetCount = CommandResultSet.Count(e => e == ResultSetMapping.LastInResultSet);

                Debug.Assert(
                    actualResultSetCount == expectedResultSetCount,
                    "Expected " + expectedResultSetCount + " result sets, got " + actualResultSetCount);
#endif
            }
            catch (Exception ex) when (!(ex is DbUpdateException))
            {
                throw new DbUpdateException(
                    RelationalStrings.UpdateStoreException,
                    ex,
                    ModificationCommands[commandIndex].Entries);
            }
        }

        /// <summary>
        ///     Consumes the data reader created by <see cref="ReaderModificationCommandBatch.Execute" />,
        ///     propagating values back into the <see cref="ModificationCommand" />.
        /// </summary>
        /// <param name="commandIndex"> The ordinal of the command being consumed. </param>
        /// <param name="reader"> The data reader. </param>
        /// <returns> The ordinal of the next command that must be consumed. </returns>
        protected virtual int ConsumeResultSetWithPropagation(int commandIndex, [NotNull] RelationalDataReader reader)
        {
            var rowsAffected = 0;
            do
            {
                var tableModification = ModificationCommands[commandIndex];
                Debug.Assert(tableModification.RequiresResultPropagation);

                if (!reader.Read())
                {
                    var expectedRowsAffected = rowsAffected + 1;
                    while (++commandIndex < CommandResultSet.Count
                           && CommandResultSet[commandIndex - 1] == ResultSetMapping.NotLastInResultSet)
                    {
                        expectedRowsAffected++;
                    }

                    ThrowAggregateUpdateConcurrencyException(commandIndex, expectedRowsAffected, rowsAffected);
                }

                var valueBufferFactory = CreateValueBufferFactory(tableModification.ColumnModifications);

                tableModification.PropagateResults(valueBufferFactory.Create(reader.DbDataReader));
                rowsAffected++;
            }
            while (++commandIndex < CommandResultSet.Count
                   && CommandResultSet[commandIndex - 1] == ResultSetMapping.NotLastInResultSet);

            return commandIndex;
        }

        /// <summary>
        ///     Consumes the data reader created by <see cref="ReaderModificationCommandBatch.ExecuteAsync" />,
        ///     propagating values back into the <see cref="ModificationCommand" />.
        /// </summary>
        /// <param name="commandIndex"> The ordinal of the command being consumed. </param>
        /// <param name="reader"> The data reader. </param>
        /// <param name="cancellationToken">A <see cref="CancellationToken" /> to observe while waiting for the task to complete.</param>
        /// <returns>
        ///     A task that represents the asynchronous operation.
        ///     The task contains the ordinal of the next command that must be consumed.
        /// </returns>
        protected virtual async Task<int> ConsumeResultSetWithPropagationAsync(
            int commandIndex, [NotNull] RelationalDataReader reader, CancellationToken cancellationToken)
        {
            var rowsAffected = 0;
            do
            {
                var tableModification = ModificationCommands[commandIndex];
                Debug.Assert(tableModification.RequiresResultPropagation);

                if (!await reader.ReadAsync(cancellationToken))
                {
                    var expectedRowsAffected = rowsAffected + 1;
                    while (++commandIndex < CommandResultSet.Count
                           && CommandResultSet[commandIndex - 1] == ResultSetMapping.NotLastInResultSet)
                    {
                        expectedRowsAffected++;
                    }

                    ThrowAggregateUpdateConcurrencyException(commandIndex, expectedRowsAffected, rowsAffected);
                }

                var valueBufferFactory = CreateValueBufferFactory(tableModification.ColumnModifications);

                tableModification.PropagateResults(valueBufferFactory.Create(reader.DbDataReader));
                rowsAffected++;
            }
            while (++commandIndex < CommandResultSet.Count
                   && CommandResultSet[commandIndex - 1] == ResultSetMapping.NotLastInResultSet);

            return commandIndex;
        }

        /// <summary>
        ///     Consumes the data reader created by <see cref="ReaderModificationCommandBatch.Execute" />
        ///     without propagating values back into the <see cref="ModificationCommand" />.
        /// </summary>
        /// <param name="commandIndex"> The ordinal of the command being consumed. </param>
        /// <param name="reader"> The data reader. </param>
        /// <returns> The ordinal of the next command that must be consumed. </returns>
        protected virtual int ConsumeResultSetWithoutPropagation(int commandIndex, [NotNull] RelationalDataReader reader)
        {
            var expectedRowsAffected = 1;
            while (++commandIndex < CommandResultSet.Count
                   && CommandResultSet[commandIndex - 1] == ResultSetMapping.NotLastInResultSet)
            {
                Debug.Assert(!ModificationCommands[commandIndex].RequiresResultPropagation);

                expectedRowsAffected++;
            }

            if (reader.Read())
            {
                var rowsAffected = reader.DbDataReader.GetInt32(0);
                if (rowsAffected != expectedRowsAffected)
                {
                    ThrowAggregateUpdateConcurrencyException(commandIndex, expectedRowsAffected, rowsAffected);
                }
            }
            else
            {
                ThrowAggregateUpdateConcurrencyException(commandIndex, 1, 0);
            }

            return commandIndex;
        }

        /// <summary>
        ///     Consumes the data reader created by <see cref="ReaderModificationCommandBatch.ExecuteAsync" />
        ///     without propagating values back into the <see cref="ModificationCommand" />.
        /// </summary>
        /// <param name="commandIndex"> The ordinal of the command being consumed. </param>
        /// <param name="reader"> The data reader. </param>
        /// <param name="cancellationToken">A <see cref="CancellationToken" /> to observe while waiting for the task to complete.</param>
        /// <returns>
        ///     A task that represents the asynchronous operation.
        ///     The task contains the ordinal of the next command that must be consumed.
        /// </returns>
        protected virtual async Task<int> ConsumeResultSetWithoutPropagationAsync(
            int commandIndex, [NotNull] RelationalDataReader reader, CancellationToken cancellationToken)
        {
            var expectedRowsAffected = 1;
            while (++commandIndex < CommandResultSet.Count
                   && CommandResultSet[commandIndex - 1] == ResultSetMapping.NotLastInResultSet)
            {
                Debug.Assert(!ModificationCommands[commandIndex].RequiresResultPropagation);

                expectedRowsAffected++;
            }

            if (await reader.ReadAsync(cancellationToken))
            {
                var rowsAffected = reader.DbDataReader.GetInt32(0);
                if (rowsAffected != expectedRowsAffected)
                {
                    ThrowAggregateUpdateConcurrencyException(commandIndex, expectedRowsAffected, rowsAffected);
                }
            }
            else
            {
                ThrowAggregateUpdateConcurrencyException(commandIndex, 1, 0);
            }

            return commandIndex;
        }

        private IReadOnlyList<IUpdateEntry> AggregateEntries(int endIndex, int commandCount)
        {
            var entries = new List<IUpdateEntry>();
            for (var i = endIndex - commandCount; i < endIndex; i++)
            {
                entries.AddRange(ModificationCommands[i].Entries);
            }
            return entries;
        }

        /// <summary>
        ///     Throws an exception indicating the command affected an unexpected number of rows.
        /// </summary>
        /// <param name="commandIndex"> The ordinal of the command. </param>
        /// <param name="expectedRowsAffected"> The expected number of rows affected. </param>
        /// <param name="rowsAffected"> The actual number of rows affected. </param>
        protected virtual void ThrowAggregateUpdateConcurrencyException(
            int commandIndex,
            int expectedRowsAffected,
            int rowsAffected)
        {
            throw new DbUpdateConcurrencyException(
                RelationalStrings.UpdateConcurrencyException(expectedRowsAffected, rowsAffected),
                AggregateEntries(commandIndex, expectedRowsAffected));
        }
    }
}

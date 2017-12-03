// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore.ChangeTracking.Internal;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.EntityFrameworkCore.Storage;

namespace Microsoft.EntityFrameworkCore.Update.Internal
{
    /// <summary>
    ///     This API supports the Entity Framework Core infrastructure and is not intended to be used
    ///     directly from your code. This API may change or be removed in future releases.
    /// </summary>
    public class CommandBatchPreparer : ICommandBatchPreparer
    {
        private readonly IModificationCommandBatchFactory _modificationCommandBatchFactory;
        private readonly IParameterNameGeneratorFactory _parameterNameGeneratorFactory;
        private readonly IComparer<ModificationCommand> _modificationCommandComparer;
        private readonly IKeyValueIndexFactorySource _keyValueIndexFactorySource;
        private readonly int _minBatchSize;
        private IStateManager _stateManager;
        private readonly bool _sensitiveLoggingEnabled;

        private IReadOnlyDictionary<(string Schema, string Name), SharedTableEntryMapFactory<ModificationCommand>> _sharedTableEntryMapFactories;

        /// <summary>
        ///     This API supports the Entity Framework Core infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public CommandBatchPreparer([NotNull] CommandBatchPreparerDependencies dependencies)
        {
            _modificationCommandBatchFactory = dependencies.ModificationCommandBatchFactory;
            _parameterNameGeneratorFactory = dependencies.ParameterNameGeneratorFactory;
            _modificationCommandComparer = dependencies.ModificationCommandComparer;
            _keyValueIndexFactorySource = dependencies.KeyValueIndexFactorySource;
            _minBatchSize = dependencies.Options.Extensions.OfType<RelationalOptionsExtension>().FirstOrDefault()
                ?.MinBatchSize ?? 4;
            Dependencies = dependencies;

            if (dependencies.LoggingOptions.IsSensitiveDataLoggingEnabled)
            {
                _sensitiveLoggingEnabled = true;
            }
        }

        private CommandBatchPreparerDependencies Dependencies { get; }

        private IStateManager StateManager => _stateManager ?? (_stateManager = Dependencies.StateManager());

        /// <summary>
        ///     This API supports the Entity Framework Core infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public virtual IEnumerable<ModificationCommandBatch> BatchCommands(IReadOnlyList<IUpdateEntry> entries)
        {
            var parameterNameGenerator = _parameterNameGeneratorFactory.Create();
            var commands = CreateModificationCommands(entries, parameterNameGenerator.GenerateNext);
            var sortedCommandSets = TopologicalSort(commands);

            // TODO: Enable batching of dependent commands by passing through the dependency graph
            foreach (var independentCommandSet in sortedCommandSets)
            {
                independentCommandSet.Sort(_modificationCommandComparer);

                var batch = _modificationCommandBatchFactory.Create();
                foreach (var modificationCommand in independentCommandSet)
                {
                    Validate(modificationCommand);

                    if (!batch.AddCommand(modificationCommand))
                    {
                        if (batch.ModificationCommands.Count == 1
                            || batch.ModificationCommands.Count >= _minBatchSize)
                        {
                            yield return batch;
                        }
                        else
                        {
                            foreach (var command in batch.ModificationCommands)
                            {
                                yield return StartNewBatch(parameterNameGenerator, command);
                            }
                        }

                        batch = StartNewBatch(parameterNameGenerator, modificationCommand);
                    }
                }

                if (batch.ModificationCommands.Count == 1
                    || batch.ModificationCommands.Count >= _minBatchSize)
                {
                    yield return batch;
                }
                else
                {
                    foreach (var command in batch.ModificationCommands)
                    {
                        yield return StartNewBatch(parameterNameGenerator, command);
                    }
                }
            }
        }

        private ModificationCommandBatch StartNewBatch(ParameterNameGenerator parameterNameGenerator, ModificationCommand modificationCommand)
        {
            parameterNameGenerator.Reset();
            var batch = _modificationCommandBatchFactory.Create();
            batch.AddCommand(modificationCommand);
            return batch;
        }

        /// <summary>
        ///     This API supports the Entity Framework Core infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        protected virtual IEnumerable<ModificationCommand> CreateModificationCommands(
            [NotNull] IReadOnlyList<IUpdateEntry> entries,
            [NotNull] Func<string> generateParameterName)
        {
            var commands = new List<ModificationCommand>();
            if (_sharedTableEntryMapFactories == null)
            {
                _sharedTableEntryMapFactories = SharedTableEntryMap<ModificationCommand>.CreateSharedTableEntryMapFactories(
                    entries[0].EntityType.Model, StateManager);
            }

            Dictionary<(string Schema, string Name), SharedTableEntryMap<ModificationCommand>> sharedTablesCommandsMap = null;
            foreach (var entry in entries)
            {
                var entityType = entry.EntityType;
                var relationalExtensions = entityType.Relational();
                var table = relationalExtensions.TableName;
                var schema = relationalExtensions.Schema;
                var tableKey = (schema, table);

                ModificationCommand command;
                if (_sharedTableEntryMapFactories.TryGetValue(tableKey, out var commandIdentityMapFactory))
                {
                    if (sharedTablesCommandsMap == null)
                    {
                        sharedTablesCommandsMap = new Dictionary<(string Schema, string Name), SharedTableEntryMap<ModificationCommand>>();
                    }
                    if (!sharedTablesCommandsMap.TryGetValue(tableKey, out var sharedCommandsMap))
                    {
                        sharedCommandsMap = commandIdentityMapFactory((t, s, c) => new ModificationCommand(
                            t, s, generateParameterName, _sensitiveLoggingEnabled, c));
                        sharedTablesCommandsMap.Add((schema, table), sharedCommandsMap);
                    }

                    command = sharedCommandsMap.GetOrAddValue(entry);
                }
                else
                {
                    command = new ModificationCommand(table, schema, generateParameterName, _sensitiveLoggingEnabled, comparer: null);
                }

                command.AddEntry(entry);
                commands.Add(command);
            }

            if (sharedTablesCommandsMap != null)
            {
                Validate(sharedTablesCommandsMap);
            }

            return commands.Where(
                c => c.EntityState != EntityState.Modified
                     || c.ColumnModifications.Any(m => m.IsWrite));
        }

        private void Validate(Dictionary<(string Schema, string Name), SharedTableEntryMap<ModificationCommand>> sharedTablesCommandsMap)
        {
            foreach (var modificationCommandIdentityMap in sharedTablesCommandsMap.Values)
            {
                foreach (var command in modificationCommandIdentityMap.Values)
                {
                    if ((command.EntityState != EntityState.Added
                         && command.EntityState != EntityState.Deleted)
                        || (command.Entries.Any(e => modificationCommandIdentityMap.GetPrincipals(e.EntityType).Count == 0)
                            && command.Entries.Any(e => modificationCommandIdentityMap.GetDependents(e.EntityType).Count == 0)))
                    {
                        continue;
                    }

                    var tableName = (string.IsNullOrEmpty(command.Schema) ? "" : command.Schema + ".") + command.TableName;
                    foreach (var entry in command.Entries)
                    {
                        foreach (var principalEntityType in modificationCommandIdentityMap.GetPrincipals(entry.EntityType))
                        {
                            if (!command.Entries.Any(
                                principalEntry => principalEntry != entry
                                                  && principalEntityType.IsAssignableFrom(principalEntry.EntityType)))
                            {
                                if (_sensitiveLoggingEnabled)
                                {
                                    throw new InvalidOperationException(
                                        RelationalStrings.SharedRowEntryCountMismatchSensitive(
                                            entry.EntityType.DisplayName(),
                                            tableName,
                                            principalEntityType.DisplayName(),
                                            entry.BuildCurrentValuesString(entry.EntityType.FindPrimaryKey().Properties),
                                            command.EntityState));
                                }

                                throw new InvalidOperationException(
                                    RelationalStrings.SharedRowEntryCountMismatch(
                                        entry.EntityType.DisplayName(),
                                        tableName,
                                        principalEntityType.DisplayName(),
                                        command.EntityState));
                            }
                        }

                        foreach (var dependentEntityType in modificationCommandIdentityMap.GetDependents(entry.EntityType))
                        {
                            if (!command.Entries.Any(
                                dependentEntry => dependentEntry != entry
                                                  && dependentEntityType.IsAssignableFrom(dependentEntry.EntityType)))
                            {
                                if (_sensitiveLoggingEnabled)
                                {
                                    throw new InvalidOperationException(
                                        RelationalStrings.SharedRowEntryCountMismatchSensitive(
                                            entry.EntityType.DisplayName(),
                                            tableName,
                                            dependentEntityType.DisplayName(),
                                            entry.BuildCurrentValuesString(entry.EntityType.FindPrimaryKey().Properties),
                                            command.EntityState));
                                }

                                throw new InvalidOperationException(
                                    RelationalStrings.SharedRowEntryCountMismatch(
                                        entry.EntityType.DisplayName(),
                                        tableName,
                                        dependentEntityType.DisplayName(),
                                        command.EntityState));
                            }
                        }
                    }
                }
            }
        }

        // To avoid violating store constraints the modification commands must be sorted
        // according to these rules:
        //
        // 1. Commands adding rows or modifying the candidate key values (when supported) must precede
        //     commands adding or modifying rows that will be referencing the former
        // 2. Commands deleting rows or modifying the foreign key values must precede
        //     commands deleting rows or modifying the candidate key values (when supported) of rows
        //     that are currently being referenced by the former
        // 3. Commands deleting rows or modifying the foreign key values must precede
        //     commands adding or modifying the foreign key values to the same values
        //     if foreign key is unique
        /// <summary>
        ///     This API supports the Entity Framework Core infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        protected virtual IReadOnlyList<List<ModificationCommand>> TopologicalSort([NotNull] IEnumerable<ModificationCommand> commands)
        {
            var modificationCommandGraph = new Multigraph<ModificationCommand, IAnnotatable>();
            modificationCommandGraph.AddVertices(commands);

            // The predecessors map allows to populate the graph in linear time
            var predecessorsMap = CreateKeyValuePredecessorMap(modificationCommandGraph);
            AddForeignKeyEdges(modificationCommandGraph, predecessorsMap);

            AddUniqueValueEdges(modificationCommandGraph);

            var sortedCommands
                = modificationCommandGraph.BatchingTopologicalSort(data => { return string.Join(", ", data.Select(d => d.Item3.First())); });

            return sortedCommands;
        }

        // Builds a map from foreign key values to list of modification commands, with an entry for every command
        // that may need to precede some other command involving that foreign key value.
        private Dictionary<IKeyValueIndex, List<ModificationCommand>> CreateKeyValuePredecessorMap(Graph<ModificationCommand> commandGraph)
        {
            var predecessorsMap = new Dictionary<IKeyValueIndex, List<ModificationCommand>>();
            foreach (var command in commandGraph.Vertices)
            {
                var columnModifications = command.ColumnModifications;
                if (command.EntityState == EntityState.Modified
                    || command.EntityState == EntityState.Added)
                {
                    foreach (var entry in command.Entries)
                    {
                        // TODO: Perf: Consider only adding foreign keys defined on entity types involved in a modification
                        foreach (var foreignKey in entry.EntityType.GetReferencingForeignKeys())
                        {
                            var keyValueIndexFactory = _keyValueIndexFactorySource.GetKeyValueIndexFactory(foreignKey.PrincipalKey);

                            var candidateKeyValueColumnModifications = columnModifications.Where(
                                cm =>
                                    foreignKey.PrincipalKey.Properties.Contains(cm.Property) && (cm.IsWrite || cm.IsRead));

                            if (command.EntityState == EntityState.Added
                                || candidateKeyValueColumnModifications.Any())
                            {
                                var principalKeyValue = keyValueIndexFactory.CreatePrincipalKeyValue((InternalEntityEntry)entry, foreignKey);

                                if (principalKeyValue != null)
                                {
                                    if (!predecessorsMap.TryGetValue(principalKeyValue, out var predecessorCommands))
                                    {
                                        predecessorCommands = new List<ModificationCommand>();
                                        predecessorsMap.Add(principalKeyValue, predecessorCommands);
                                    }
                                    predecessorCommands.Add(command);
                                }
                            }
                        }
                    }
                }

                if (command.EntityState == EntityState.Modified
                    || command.EntityState == EntityState.Deleted)
                {
                    foreach (var entry in command.Entries)
                    {
                        foreach (var foreignKey in entry.EntityType.GetForeignKeys())
                        {
                            var keyValueIndexFactory = _keyValueIndexFactorySource.GetKeyValueIndexFactory(foreignKey.PrincipalKey);

                            var currentForeignKey = foreignKey;
                            var foreignKeyValueColumnModifications = columnModifications.Where(
                                cm =>
                                    currentForeignKey.Properties.Contains(cm.Property) && (cm.IsWrite || cm.IsRead));

                            if (command.EntityState == EntityState.Deleted
                                || foreignKeyValueColumnModifications.Any())
                            {
                                var dependentKeyValue = keyValueIndexFactory.CreateDependentKeyValueFromOriginalValues((InternalEntityEntry)entry, foreignKey);

                                if (dependentKeyValue != null)
                                {
                                    if (!predecessorsMap.TryGetValue(dependentKeyValue, out var predecessorCommands))
                                    {
                                        predecessorCommands = new List<ModificationCommand>();
                                        predecessorsMap.Add(dependentKeyValue, predecessorCommands);
                                    }
                                    predecessorCommands.Add(command);
                                }
                            }
                        }
                    }
                }
            }
            return predecessorsMap;
        }

        private void AddForeignKeyEdges(
            Multigraph<ModificationCommand, IAnnotatable> commandGraph,
            Dictionary<IKeyValueIndex, List<ModificationCommand>> predecessorsMap)
        {
            foreach (var command in commandGraph.Vertices)
            {
                if (command.EntityState == EntityState.Modified
                    || command.EntityState == EntityState.Added)
                {
                    foreach (var entry in command.Entries)
                    {
                        foreach (var foreignKey in entry.EntityType.GetForeignKeys())
                        {
                            var keyValueIndexFactory = _keyValueIndexFactorySource.GetKeyValueIndexFactory(foreignKey.PrincipalKey);
                            var dependentKeyValue = keyValueIndexFactory.CreateDependentKeyValue((InternalEntityEntry)entry, foreignKey);
                            if (dependentKeyValue == null)
                            {
                                continue;
                            }

                            AddMatchingPredecessorEdge(predecessorsMap, dependentKeyValue, commandGraph, command, foreignKey);
                        }
                    }
                }

                // TODO: also examine modified entities here when principal key modification is supported
                if (command.EntityState == EntityState.Deleted)
                {
                    foreach (var entry in command.Entries)
                    {
                        foreach (var foreignKey in entry.EntityType.GetReferencingForeignKeys())
                        {
                            var keyValueIndexFactory = _keyValueIndexFactorySource.GetKeyValueIndexFactory(foreignKey.PrincipalKey);
                            var principalKeyValue = keyValueIndexFactory.CreatePrincipalKeyValueFromOriginalValues((InternalEntityEntry)entry, foreignKey);
                            if (principalKeyValue != null)
                            {
                                AddMatchingPredecessorEdge(predecessorsMap, principalKeyValue, commandGraph, command, foreignKey);
                            }
                        }
                    }
                }
            }
        }

        private static void AddMatchingPredecessorEdge(
            Dictionary<IKeyValueIndex, List<ModificationCommand>> predecessorsMap,
            IKeyValueIndex dependentKeyValue,
            Multigraph<ModificationCommand, IAnnotatable> commandGraph,
            ModificationCommand command,
            IForeignKey foreignKey)
        {
            if (predecessorsMap.TryGetValue(dependentKeyValue, out var predecessorCommands))
            {
                foreach (var predecessor in predecessorCommands)
                {
                    if (predecessor != command)
                    {
                        commandGraph.AddEdge(predecessor, command, foreignKey);
                    }
                }
            }
        }

        private void AddUniqueValueEdges(Multigraph<ModificationCommand, IAnnotatable> commandGraph)
        {
            Dictionary<IIndex, Dictionary<object[], ModificationCommand>> predecessorsMap = null;
            foreach (var command in commandGraph.Vertices)
            {
                if (command.EntityState == EntityState.Modified
                    || command.EntityState == EntityState.Deleted)
                {
                    foreach (var entry in command.Entries)
                    {
                        foreach (var index in entry.EntityType.GetIndexes().Where(i => i.IsUnique))
                        {
                            var indexColumnModifications =
                                command.ColumnModifications.Where(
                                    cm =>
                                        index.Properties.Contains(cm.Property)
                                        && (cm.IsWrite || cm.IsRead));

                            if (command.EntityState == EntityState.Deleted
                                || indexColumnModifications.Any())
                            {
                                var valueFactory = index.GetNullableValueFactory<object[]>();
                                if (valueFactory.TryCreateFromOriginalValues((InternalEntityEntry)entry, out var indexValue))
                                {
                                    predecessorsMap = predecessorsMap ?? new Dictionary<IIndex, Dictionary<object[], ModificationCommand>>();
                                    if (!predecessorsMap.TryGetValue(index, out var predecessorCommands))
                                    {
                                        predecessorCommands = new Dictionary<object[], ModificationCommand>(valueFactory.EqualityComparer);
                                        predecessorsMap.Add(index, predecessorCommands);
                                    }
                                    predecessorCommands.Add(indexValue, command);
                                }
                            }
                        }
                    }
                }
            }

            if (predecessorsMap == null)
            {
                return;
            }

            foreach (var command in commandGraph.Vertices)
            {
                if (command.EntityState == EntityState.Modified
                    || command.EntityState == EntityState.Added)
                {
                    foreach (var entry in command.Entries)
                    {
                        foreach (var index in entry.EntityType.GetIndexes().Where(i => i.IsUnique))
                        {
                            var indexColumnModifications =
                                command.ColumnModifications.Where(
                                    cm =>
                                        index.Properties.Contains(cm.Property)
                                        && cm.IsWrite);

                            if (command.EntityState == EntityState.Added
                                || indexColumnModifications.Any())
                            {
                                var valueFactory = index.GetNullableValueFactory<object[]>();
                                if (valueFactory.TryCreateFromCurrentValues((InternalEntityEntry)entry, out var indexValue)
                                    && predecessorsMap.TryGetValue(index, out var predecessorCommands)
                                    && predecessorCommands.TryGetValue(indexValue, out var predecessor)
                                    && predecessor != command)
                                {
                                    commandGraph.AddEdge(predecessor, command, index);
                                }
                            }
                        }
                    }
                }
            }
        }

        private void Validate(ModificationCommand modificationCommand)
        {
            if (modificationCommand.EntityState == EntityState.Added)
            {
                foreach (var columnModification in modificationCommand.ColumnModifications)
                {
                    if (!columnModification.IsRead
                        && columnModification.Entry.HasTemporaryValue(columnModification.Property))
                    {
                        throw new InvalidOperationException(
                            CoreStrings.TempValue(columnModification.Property.Name, columnModification.Entry.EntityType.DisplayName()));
                    }
                }
            }
        }
    }
}

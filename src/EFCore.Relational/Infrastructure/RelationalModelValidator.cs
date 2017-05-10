﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.Utilities;

namespace Microsoft.EntityFrameworkCore.Infrastructure
{
    /// <summary>
    ///     The validator that enforces rules common for all relational providers.
    /// </summary>
    public class RelationalModelValidator : CoreModelValidator
    {
        /// <summary>
        ///     Creates a new instance of <see cref="RelationalModelValidator" />.
        /// </summary>
        /// <param name="dependencies"> Parameter object containing dependencies for this service. </param>
        /// <param name="relationalDependencies"> Parameter object containing relational dependencies for this service. </param>
        public RelationalModelValidator(
            [NotNull] ModelValidatorDependencies dependencies,
            [NotNull] RelationalModelValidatorDependencies relationalDependencies)
            : base(dependencies)
        {
            Check.NotNull(relationalDependencies, nameof(relationalDependencies));

            RelationalDependencies = relationalDependencies;
        }

        /// <summary>
        ///     Dependencies used to create a <see cref="ModelValidator" />
        /// </summary>
        protected virtual RelationalModelValidatorDependencies RelationalDependencies { get; }

        /// <summary>
        ///     Gets the relational annotation provider.
        /// </summary>
        protected virtual IRelationalAnnotationProvider RelationalExtensions => RelationalDependencies.RelationalExtensions;

        /// <summary>
        ///     Gets the type mapper.
        /// </summary>
        protected virtual IRelationalTypeMapper TypeMapper => RelationalDependencies.TypeMapper;

        /// <summary>
        ///     Validates a model, throwing an exception if any errors are found.
        /// </summary>
        /// <param name="model"> The model to validate. </param>
        public override void Validate(IModel model)
        {
            base.Validate(model);

            ValidateSharedTableCompatibility(model);
            ValidateInheritanceMapping(model);
            ValidateDataTypes(model);
            ValidateDefaultValuesOnKeys(model);
        }

        /// <summary>
        ///     This API supports the Entity Framework Core infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        protected virtual void ValidateDataTypes([NotNull] IModel model)
        {
            foreach (var entityType in model.GetEntityTypes())
            {
                foreach (var property in entityType.GetDeclaredProperties())
                {
                    var dataType = RelationalExtensions.For(property).ColumnType;
                    if (dataType != null)
                    {
                        TypeMapper.ValidateTypeName(dataType);
                    }
                }
            }
        }

        /// <summary>
        ///     This API supports the Entity Framework Core infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        protected virtual void ValidateDefaultValuesOnKeys([NotNull] IModel model)
        {
            foreach (var property in model.GetEntityTypes().SelectMany(
                t => t.GetDeclaredKeys().SelectMany(k => k.Properties))
                .Where(p => RelationalExtensions.For(p).DefaultValue != null))
            {
                Dependencies.Logger.ModelValidationKeyDefaultValueWarning(property);
            }
        }

        /// <summary>
        ///     This API supports the Entity Framework Core infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        protected virtual void ValidateSharedTableCompatibility([NotNull] IModel model)
        {
            var tables = new Dictionary<string, List<IEntityType>>();
            foreach (var entityType in model.GetEntityTypes())
            {
                var annotations = RelationalExtensions.For(entityType);
                var tableName = Format(annotations.Schema, annotations.TableName);

                if (tables.TryGetValue(tableName, out var mappedTypes))
                {
                    ValidateSharedTableCompatibility(entityType, mappedTypes, tableName);
                }
                else
                {
                    mappedTypes = new List<IEntityType>();
                    tables[tableName] = mappedTypes;
                }

                mappedTypes.Add(entityType);
            }

            foreach (var tableMapping in tables)
            {
                ValidateSharedColumnsCompatibility(tableMapping.Value, tableMapping.Key);
                ValidateSharedKeysCompatibility(tableMapping.Value, tableMapping.Key);
                ValidateSharedForeignKeysCompatibility(tableMapping.Value, tableMapping.Key);
                ValidateSharedIndexesCompatibility(tableMapping.Value, tableMapping.Key);
            }
        }

        /// <summary>
        ///     This API supports the Entity Framework Core infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        protected virtual void ValidateSharedTableCompatibility(
            [NotNull] IEntityType newEntityType, [NotNull] List<IEntityType> otherMappedTypes, [NotNull] string tableName)
        {
            var key = newEntityType.FindPrimaryKey();
            var identifyingForeignKeys = newEntityType.FindForeignKeys(key.Properties).ToList();
            var relationshipFound = false;
            foreach (var otherMappedType in otherMappedTypes)
            {
                var otherKey = otherMappedType.FindPrimaryKey();
                if (RelationalExtensions.For(key).Name != RelationalExtensions.For(otherKey).Name)
                {
                    ShowError(RelationalStrings.IncompatibleTableKeyNameMismatch(
                        tableName,
                        newEntityType.DisplayName(),
                        otherMappedType.DisplayName(),
                        RelationalExtensions.For(key).Name,
                        Property.Format(key.Properties),
                        RelationalExtensions.For(otherKey).Name,
                        Property.Format(otherKey.Properties)));
                }

                if (!relationshipFound
                    && (newEntityType.RootType() == otherMappedType.RootType()
                        || identifyingForeignKeys.Any(fk => fk.PrincipalEntityType == otherMappedType && fk.PrincipalKey == otherKey)
                        || otherMappedType.FindForeignKeys(otherKey.Properties)
                            .Any(fk => fk.PrincipalEntityType == newEntityType && fk.PrincipalKey == key)))
                {
                    relationshipFound = true;
                }
            }

            if (!relationshipFound)
            {
                ShowError(
                    RelationalStrings.IncompatibleTableNoRelationship(
                        tableName,
                        newEntityType.DisplayName(),
                        otherMappedTypes[0].DisplayName(),
                        Property.Format(key.Properties),
                        Property.Format(otherMappedTypes[0].FindPrimaryKey().Properties)));
            }
        }

        /// <summary>
        ///     This API supports the Entity Framework Core infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        protected virtual void ValidateSharedColumnsCompatibility(
            [NotNull] IReadOnlyList<IEntityType> mappedTypes, [NotNull] string tableName)
        {
            var propertyMappings = new Dictionary<string, IProperty>();

            foreach (var property in mappedTypes.SelectMany(et => et.GetDeclaredProperties()))
            {
                var propertyAnnotations = RelationalExtensions.For(property);
                var columnName = propertyAnnotations.ColumnName;
                if (propertyMappings.TryGetValue(columnName, out var duplicateProperty))
                {
                    var previousAnnotations = RelationalExtensions.For(duplicateProperty);
                    var currentTypeString = propertyAnnotations.ColumnType
                                            ?? TypeMapper.GetMapping(property).StoreType;
                    var previousTypeString = previousAnnotations.ColumnType
                                             ?? TypeMapper.GetMapping(duplicateProperty).StoreType;
                    if (!currentTypeString.Equals(previousTypeString, StringComparison.OrdinalIgnoreCase))
                    {
                        ShowError(RelationalStrings.DuplicateColumnNameDataTypeMismatch(
                            duplicateProperty.DeclaringEntityType.DisplayName(),
                            duplicateProperty.Name,
                            property.DeclaringEntityType.DisplayName(),
                            property.Name,
                            columnName,
                            tableName,
                            previousTypeString,
                            currentTypeString));
                    }

                    if (property.IsColumnNullable() != duplicateProperty.IsColumnNullable())
                    {
                        ShowError(RelationalStrings.DuplicateColumnNameNullabilityMismatch(
                            duplicateProperty.DeclaringEntityType.DisplayName(),
                            duplicateProperty.Name,
                            property.DeclaringEntityType.DisplayName(),
                            property.Name,
                            columnName,
                            tableName));
                    }

                    var currentComputedColumnSql = propertyAnnotations.ComputedColumnSql ?? "";
                    var previousComputedColumnSql = previousAnnotations.ComputedColumnSql ?? "";
                    if (!currentComputedColumnSql.Equals(previousComputedColumnSql, StringComparison.OrdinalIgnoreCase))
                    {
                        ShowError(RelationalStrings.DuplicateColumnNameComputedSqlMismatch(
                            duplicateProperty.DeclaringEntityType.DisplayName(),
                            duplicateProperty.Name,
                            property.DeclaringEntityType.DisplayName(),
                            property.Name,
                            columnName,
                            tableName,
                            previousComputedColumnSql,
                            currentComputedColumnSql));
                    }

                    var currentDefaultValue = propertyAnnotations.DefaultValue;
                    var previousDefaultValue = previousAnnotations.DefaultValue;
                    if (!Equals(currentDefaultValue, previousDefaultValue))
                    {
                        ShowError(RelationalStrings.DuplicateColumnNameDefaultSqlMismatch(
                            duplicateProperty.DeclaringEntityType.DisplayName(),
                            duplicateProperty.Name,
                            property.DeclaringEntityType.DisplayName(),
                            property.Name,
                            columnName,
                            tableName,
                            previousDefaultValue ?? "NULL",
                            currentDefaultValue ?? "NULL"));
                    }

                    var currentDefaultValueSql = propertyAnnotations.DefaultValueSql ?? "";
                    var previousDefaultValueSql = previousAnnotations.DefaultValueSql ?? "";
                    if (!currentDefaultValueSql.Equals(previousDefaultValueSql, StringComparison.OrdinalIgnoreCase))
                    {
                        ShowError(RelationalStrings.DuplicateColumnNameDefaultSqlMismatch(
                            duplicateProperty.DeclaringEntityType.DisplayName(),
                            duplicateProperty.Name,
                            property.DeclaringEntityType.DisplayName(),
                            property.Name,
                            columnName,
                            tableName,
                            previousDefaultValueSql,
                            currentDefaultValueSql));
                    }
                }
                else
                {
                    propertyMappings[columnName] = property;
                }
            }
        }

        /// <summary>
        ///     This API supports the Entity Framework Core infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        protected virtual void ValidateSharedForeignKeysCompatibility(
            [NotNull] IReadOnlyList<IEntityType> mappedTypes, [NotNull] string tableName)
        {
            var foreignKeyMappings = new Dictionary<string, IForeignKey>();

            foreach (var foreignKey in mappedTypes.SelectMany(et => et.GetDeclaredForeignKeys()))
            {
                var foreignKeyAnnotations = RelationalExtensions.For(foreignKey);
                var foreignKeyName = foreignKeyAnnotations.Name;

                if (!foreignKeyMappings.TryGetValue(foreignKeyName, out var duplicateForeignKey))
                {
                    foreignKeyMappings[foreignKeyName] = foreignKey;
                    continue;
                }

                var principalAnnotations = RelationalExtensions.For(foreignKey.PrincipalEntityType);
                var principalTable = Format(principalAnnotations.Schema, principalAnnotations.TableName);
                var duplicateAnnotations = RelationalExtensions.For(duplicateForeignKey.PrincipalEntityType);
                var duplicatePrincipalTable = Format(duplicateAnnotations.Schema, duplicateAnnotations.TableName);
                if (!string.Equals(principalTable, duplicatePrincipalTable, StringComparison.OrdinalIgnoreCase))
                {
                    ShowError(RelationalStrings.DuplicateForeignKeyPrincipalTableMismatch(
                        Property.Format(foreignKey.Properties),
                        foreignKey.DeclaringEntityType.DisplayName(),
                        Property.Format(duplicateForeignKey.Properties),
                        duplicateForeignKey.DeclaringEntityType.DisplayName(),
                        tableName,
                        foreignKeyName,
                        principalTable,
                        duplicatePrincipalTable));
                }

                if (!foreignKey.Properties.Select(p => RelationalExtensions.For(p).ColumnName)
                    .SequenceEqual(duplicateForeignKey.Properties.Select(p => RelationalExtensions.For(p).ColumnName)))
                {
                    ShowError(RelationalStrings.DuplicateForeignKeyColumnMismatch(
                        Property.Format(foreignKey.Properties),
                        foreignKey.DeclaringEntityType.DisplayName(),
                        Property.Format(duplicateForeignKey.Properties),
                        duplicateForeignKey.DeclaringEntityType.DisplayName(),
                        tableName,
                        foreignKeyName,
                        RelationalExtensions.FormatColumns(foreignKey.Properties),
                        RelationalExtensions.FormatColumns(duplicateForeignKey.Properties)));
                }

                if (!foreignKey.PrincipalKey.Properties
                    .Select(p => RelationalExtensions.For(p).ColumnName)
                    .SequenceEqual(duplicateForeignKey.PrincipalKey.Properties
                        .Select(p => RelationalExtensions.For(p).ColumnName)))
                {
                    ShowError(RelationalStrings.DuplicateForeignKeyPrincipalColumnMismatch(
                        Property.Format(foreignKey.Properties),
                        foreignKey.DeclaringEntityType.DisplayName(),
                        Property.Format(duplicateForeignKey.Properties),
                        duplicateForeignKey.DeclaringEntityType.DisplayName(),
                        tableName,
                        foreignKeyName,
                        RelationalExtensions.FormatColumns(foreignKey.PrincipalKey.Properties),
                        RelationalExtensions.FormatColumns(duplicateForeignKey.PrincipalKey.Properties)));
                }

                if (foreignKey.IsUnique != duplicateForeignKey.IsUnique)
                {
                    ShowError(RelationalStrings.DuplicateForeignKeyUniquenessMismatch(
                        Property.Format(foreignKey.Properties),
                        foreignKey.DeclaringEntityType.DisplayName(),
                        Property.Format(duplicateForeignKey.Properties),
                        duplicateForeignKey.DeclaringEntityType.DisplayName(),
                        tableName,
                        foreignKeyName));
                }

                if (foreignKey.DeleteBehavior != duplicateForeignKey.DeleteBehavior)
                {
                    ShowError(RelationalStrings.DuplicateForeignKeyDeleteBehaviorMismatch(
                        Property.Format(foreignKey.Properties),
                        foreignKey.DeclaringEntityType.DisplayName(),
                        Property.Format(duplicateForeignKey.Properties),
                        duplicateForeignKey.DeclaringEntityType.DisplayName(),
                        tableName,
                        foreignKeyName,
                        foreignKey.DeleteBehavior,
                        duplicateForeignKey.DeleteBehavior));
                }
            }
        }

        /// <summary>
        ///     This API supports the Entity Framework Core infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        protected virtual void ValidateSharedIndexesCompatibility(
            [NotNull] IReadOnlyList<IEntityType> mappedTypes, [NotNull] string tableName)
        {
            var indexMappings = new Dictionary<string, IIndex>();

            foreach (var index in mappedTypes.SelectMany(et => et.GetDeclaredIndexes()))
            {
                var indexAnnotations = RelationalExtensions.For(index);
                var indexName = indexAnnotations.Name;

                if (!indexMappings.TryGetValue(indexName, out var duplicateIndex))
                {
                    indexMappings[indexName] = index;
                    continue;
                }

                if (!index.Properties.Select(p => RelationalExtensions.For(p).ColumnName)
                    .SequenceEqual(duplicateIndex.Properties.Select(p => RelationalExtensions.For(p).ColumnName)))
                {
                    ShowError(RelationalStrings.DuplicateIndexColumnMismatch(
                        Property.Format(index.Properties),
                        index.DeclaringEntityType.DisplayName(),
                        Property.Format(duplicateIndex.Properties),
                        duplicateIndex.DeclaringEntityType.DisplayName(),
                        tableName,
                        indexName,
                        RelationalExtensions.FormatColumns(index.Properties),
                        RelationalExtensions.FormatColumns(duplicateIndex.Properties)));
                }

                if (index.IsUnique != duplicateIndex.IsUnique)
                {
                    ShowError(RelationalStrings.DuplicateIndexUniquenessMismatch(
                        Property.Format(index.Properties),
                        index.DeclaringEntityType.DisplayName(),
                        Property.Format(duplicateIndex.Properties),
                        duplicateIndex.DeclaringEntityType.DisplayName(),
                        tableName,
                        indexName));
                }
            }
        }

        /// <summary>
        ///     This API supports the Entity Framework Core infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        protected virtual void ValidateSharedKeysCompatibility(
            [NotNull] IReadOnlyList<IEntityType> mappedTypes, [NotNull] string tableName)
        {
            var keyMappings = new Dictionary<string, IKey>();

            foreach (var key in mappedTypes.SelectMany(et => et.GetDeclaredKeys()))
            {
                var keyAnnotations = RelationalExtensions.For(key);
                var keyName = keyAnnotations.Name;

                if (!keyMappings.TryGetValue(keyName, out var duplicateKey))
                {
                    keyMappings[keyName] = key;
                    continue;
                }

                if (!key.Properties.Select(p => RelationalExtensions.For(p).ColumnName)
                    .SequenceEqual(duplicateKey.Properties.Select(p => RelationalExtensions.For(p).ColumnName)))
                {
                    ShowError(RelationalStrings.DuplicateKeyColumnMismatch(
                        Property.Format(key.Properties),
                        key.DeclaringEntityType.DisplayName(),
                        Property.Format(duplicateKey.Properties),
                        duplicateKey.DeclaringEntityType.DisplayName(),
                        tableName,
                        keyName,
                        RelationalExtensions.FormatColumns(key.Properties),
                        RelationalExtensions.FormatColumns(duplicateKey.Properties)));
                }
            }
        }

        /// <summary>
        ///     This API supports the Entity Framework Core infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        protected virtual void ValidateInheritanceMapping([NotNull] IModel model)
        {
            foreach (var rootEntityType in model.GetRootEntityTypes())
            {
                ValidateDiscriminatorValues(rootEntityType);
            }
        }

        private void ValidateDiscriminator(IEntityType entityType)
        {
            var annotations = RelationalExtensions.For(entityType);
            if (annotations.DiscriminatorProperty == null)
            {
                ShowError(RelationalStrings.NoDiscriminatorProperty(entityType.DisplayName()));
            }
            if (annotations.DiscriminatorValue == null)
            {
                ShowError(RelationalStrings.NoDiscriminatorValue(entityType.DisplayName()));
            }
        }

        private void ValidateDiscriminatorValues(IEntityType rootEntityType)
        {
            var discriminatorValues = new Dictionary<object, IEntityType>();
            var derivedTypes = rootEntityType.GetDerivedTypesInclusive().ToList();
            if (derivedTypes.Count == 1)
            {
                return;
            }

            foreach (var derivedType in derivedTypes)
            {
                if (derivedType.ClrType?.IsInstantiable() != true)
                {
                    continue;
                }

                ValidateDiscriminator(derivedType);

                var discriminatorValue = RelationalExtensions.For(derivedType).DiscriminatorValue;
                if (discriminatorValues.TryGetValue(discriminatorValue, out var duplicateEntityType))
                {
                    ShowError(RelationalStrings.DuplicateDiscriminatorValue(
                        derivedType.DisplayName(), discriminatorValue, duplicateEntityType.DisplayName()));
                }
                discriminatorValues[discriminatorValue] = derivedType;
            }
        }

        private static string Format(string schema, string name)
            => (string.IsNullOrEmpty(schema) ? "" : schema + ".") + name;
    }
}

﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Internal;

namespace Microsoft.EntityFrameworkCore.Internal
{
    public class SqlServerModelValidator : RelationalModelValidator
    {
        public SqlServerModelValidator(
            [NotNull] ModelValidatorDependencies dependencies,
            [NotNull] RelationalModelValidatorDependencies relationalDependencies)
            : base(dependencies, relationalDependencies)
        {
        }

        public override void Validate(IModel model)
        {
            base.Validate(model);

            ValidateDefaultDecimalMapping(model);
            ValidateByteIdentityMapping(model);
            ValidateNonKeyValueGeneration(model);
        }

        protected virtual void ValidateDefaultDecimalMapping([NotNull] IModel model)
        {
            foreach (var property in model.GetEntityTypes()
                .SelectMany(t => t.GetDeclaredProperties())
                .Where(
                    p => p.ClrType.UnwrapNullableType() == typeof(decimal)
                         && p.SqlServer().ColumnType == null))
            {
                Dependencies.Logger.DecimalTypeDefaultWarning(property);
            }
        }

        protected virtual void ValidateByteIdentityMapping([NotNull] IModel model)
        {
            foreach (var property in model.GetEntityTypes()
                .SelectMany(t => t.GetDeclaredProperties())
                .Where(
                    p => p.ClrType.UnwrapNullableType() == typeof(byte)
                         && p.SqlServer().ValueGenerationStrategy == SqlServerValueGenerationStrategy.IdentityColumn))
            {
                Dependencies.Logger.ByteIdentityColumnWarning(property);
            }
        }

        protected virtual void ValidateNonKeyValueGeneration([NotNull] IModel model)
        {
            foreach (var property in model.GetEntityTypes()
                .SelectMany(t => t.GetDeclaredProperties())
                .Where(
                    p =>
                        (((SqlServerPropertyAnnotations)p.SqlServer()).GetSqlServerValueGenerationStrategy(fallbackToModel: false) == SqlServerValueGenerationStrategy.SequenceHiLo
                         || ((SqlServerPropertyAnnotations)p.SqlServer()).GetSqlServerValueGenerationStrategy(fallbackToModel: false) == SqlServerValueGenerationStrategy.IdentityColumn)
                        && !p.IsKey()))
            {
                ShowError(SqlServerStrings.NonKeyValueGeneration(property.Name, property.DeclaringEntityType.DisplayName()));
            }
        }

        protected override void ValidateSharedTableCompatibility(
            IEntityType newEntityType, List<IEntityType> otherMappedTypes, string tableName)
        {
            var isMemoryOptimized = newEntityType.SqlServer().IsMemoryOptimized;

            foreach (var otherMappedType in otherMappedTypes)
            {
                if (isMemoryOptimized != otherMappedType.SqlServer().IsMemoryOptimized)
                {
                    ShowError(SqlServerStrings.IncompatibleTableMemoryOptimizedMismatch(
                        tableName, newEntityType.DisplayName(), otherMappedType.DisplayName(),
                        isMemoryOptimized ? newEntityType.DisplayName() : otherMappedType.DisplayName(),
                        !isMemoryOptimized ? newEntityType.DisplayName() : otherMappedType.DisplayName()));
                }
            }

            base.ValidateSharedTableCompatibility(newEntityType, otherMappedTypes, tableName);
        }
    }
}

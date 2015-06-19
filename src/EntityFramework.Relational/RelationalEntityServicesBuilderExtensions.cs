// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Microsoft.Data.Entity.Infrastructure;
using Microsoft.Data.Entity.Internal;
using Microsoft.Data.Entity.Relational.Migrations;
using Microsoft.Data.Entity.Relational.Migrations.Infrastructure;
using Microsoft.Data.Entity.Relational.Query;
using Microsoft.Data.Entity.Relational.Update;
using Microsoft.Data.Entity.Relational.ValueGeneration;
using Microsoft.Data.Entity.Utilities;
using Microsoft.Framework.DependencyInjection;

// Intentionally in this namespace since this is for use by other relational providers rather than
// by top-level app developers.

namespace Microsoft.Data.Entity.Relational
{
    public static class RelationalEntityServicesBuilderExtensions
    {
        public static EntityFrameworkServicesBuilder AddRelational([NotNull] this EntityFrameworkServicesBuilder builder)
        {
            Check.NotNull(builder, nameof(builder));

            builder.GetService().TryAdd(new ServiceCollection()
                .AddSingleton<IParameterNameGeneratorFactory, ParameterNameGeneratorFactory>()
                .AddSingleton<IComparer<ModificationCommand>, ModificationCommandComparer>()
                .AddSingleton<IMigrationIdGenerator, MigrationIdGenerator>()
                .AddSingleton<SqlStatementExecutor>()
                .AddSingleton<UntypedValueBufferFactoryFactory>()
                .AddSingleton<TypedValueBufferFactoryFactory>()
                .AddSingleton<IMigrationModelFactory, MigrationModelFactory>()
                .AddSingleton<RelationalModelValidator>()
                .AddSingleton<MigrationAnnotationProvider>()
                .AddScoped<IMigrator, Migrator>()
                .AddScoped<IMigrationAssembly, MigrationAssembly>()
                .AddScoped<RelationalQueryContextFactory>()
                .AddScoped<BatchExecutor>()
                .AddScoped<ModelDiffer>()
                .AddScoped<RelationalValueGeneratorSelector>()
                .AddScoped<CommandBatchPreparer>()
                .AddScoped<IModelDiffer, ModelDiffer>()
                .AddScoped(p => GetProviderServices(p).SqlStatementExecutor)
                .AddScoped(p => GetProviderServices(p).CompositeMethodCallTranslator)
                .AddScoped(p => GetProviderServices(p).CompositeMemberTranslator)
                .AddScoped(p => GetProviderServices(p).MigrationAnnotationProvider)
                .AddScoped(p => GetProviderServices(p).HistoryRepository)
                .AddScoped(p => GetProviderServices(p).MigrationSqlGenerator)
                .AddScoped(p => GetProviderServices(p).RelationalConnection)
                .AddScoped(p => GetProviderServices(p).TypeMapper)
                .AddScoped(p => GetProviderServices(p).ModificationCommandBatchFactory)
                .AddScoped(p => GetProviderServices(p).CommandBatchPreparer)
                .AddScoped(p => GetProviderServices(p).BatchExecutor)
                .AddScoped(p => GetProviderServices(p).ValueBufferFactoryFactory)
                .AddScoped(p => GetProviderServices(p).RelationalDatabaseCreator)
                .AddScoped(p => GetProviderServices(p).SqlGenerator)
                .AddScoped(p => GetProviderServices(p).MetadataExtensionProvider));

            return builder;
        }

        private static IRelationalDatabaseProviderServices GetProviderServices(IServiceProvider serviceProvider)
        {
            var providerServices = serviceProvider.GetRequiredService<IDbContextServices>().DatabaseProviderServices 
                as IRelationalDatabaseProviderServices;

            if (providerServices == null)
            {
                throw new InvalidOperationException(Strings.RelationalNotInUse);
            }

            return providerServices;
        }
    }
}

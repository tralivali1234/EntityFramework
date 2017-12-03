// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Reflection;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore.Scaffolding.Internal;
using Microsoft.EntityFrameworkCore.Utilities;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.EntityFrameworkCore.Design.Internal
{
    /// <summary>
    ///     This API supports the Entity Framework Core infrastructure and is not intended to be used
    ///     directly from your code. This API may change or be removed in future releases.
    /// </summary>
    public class DatabaseOperations
    {
        private readonly IOperationReporter _reporter;
        private readonly string _projectDir;
        private readonly string _rootNamespace;
        private readonly string _language;
        private readonly DesignTimeServicesBuilder _servicesBuilder;

        /// <summary>
        ///     This API supports the Entity Framework Core infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public DatabaseOperations(
            [NotNull] IOperationReporter reporter,
            [NotNull] Assembly startupAssembly,
            [NotNull] string projectDir,
            [NotNull] string rootNamespace,
            [NotNull] string language)
        {
            Check.NotNull(reporter, nameof(reporter));
            Check.NotNull(startupAssembly, nameof(startupAssembly));
            Check.NotNull(projectDir, nameof(projectDir));
            Check.NotNull(rootNamespace, nameof(rootNamespace));
            Check.NotNull(language, nameof(language));

            _reporter = reporter;
            _projectDir = projectDir;
            _rootNamespace = rootNamespace;
            _language = language;

            _servicesBuilder = new DesignTimeServicesBuilder(startupAssembly, reporter);
        }

        /// <summary>
        ///     This API supports the Entity Framework Core infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public virtual ReverseEngineerFiles ScaffoldContext(
            [NotNull] string provider,
            [NotNull] string connectionString,
            [CanBeNull] string outputDir,
            [CanBeNull] string dbContextClassName,
            [NotNull] IEnumerable<string> schemas,
            [NotNull] IEnumerable<string> tables,
            bool useDataAnnotations,
            bool overwriteFiles,
            bool useDatabaseNames)
        {
            Check.NotEmpty(provider, nameof(provider));
            Check.NotEmpty(connectionString, nameof(connectionString));
            Check.NotNull(schemas, nameof(schemas));
            Check.NotNull(tables, nameof(tables));

            var services = _servicesBuilder.Build(provider);

            var generator = services.GetRequiredService<IReverseEngineerScaffolder>();

            var scaffoldedModel = generator.Generate(
                connectionString,
                tables,
                schemas,
                _projectDir,
                outputDir,
                _rootNamespace,
                _language,
                dbContextClassName,
                useDataAnnotations,
                useDatabaseNames);

            return generator.Save(
                scaffoldedModel,
                _projectDir,
                outputDir,
                overwriteFiles);
        }
    }
}

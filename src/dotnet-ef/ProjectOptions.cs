﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.DotNet.Cli.CommandLine;
using Microsoft.EntityFrameworkCore.Tools.Properties;

namespace Microsoft.EntityFrameworkCore.Tools
{
    internal class ProjectOptions
    {
        private CommandOption _project;
        private CommandOption _startupProject;
        private CommandOption _framework;
        private CommandOption _configuration;
        private CommandOption _msbuildprojectextensionspath;

        public CommandOption Project
            => _project;

        public CommandOption StartupProject
            => _startupProject;

        public CommandOption Framework
            => _framework;

        public CommandOption Configuration
            => _configuration;

        public CommandOption MSBuildProjectExtensionsPath
            => _msbuildprojectextensionspath;

        public void Configure(CommandLineApplication command)
        {
            _project = command.Option("-p|--project <PROJECT>", Resources.ProjectDescription);
            _startupProject = command.Option("-s|--startup-project <PROJECT>", Resources.StartupProjectDescription);
            _framework = command.Option("--framework <FRAMEWORK>", Resources.FrameworkDescription);
            _configuration = command.Option("--configuration <CONFIGURATION>", Resources.ConfigurationDescription);
            _msbuildprojectextensionspath = command.Option("--msbuildprojectextensionspath <PATH>", Resources.ProjectExtensionsDescription);
        }
    }
}

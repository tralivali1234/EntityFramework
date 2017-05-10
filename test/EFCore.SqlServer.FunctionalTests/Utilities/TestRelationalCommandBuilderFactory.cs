// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.Storage.Internal;

namespace Microsoft.EntityFrameworkCore.SqlServer.FunctionalTests.Utilities
{
    public class TestRelationalCommandBuilderFactory : IRelationalCommandBuilderFactory
    {
        private readonly IDiagnosticsLogger<LoggerCategory.Database.Sql> _logger;
        private readonly IDiagnosticsLogger<LoggerCategory.Database.DataReader> _readerLogger;
        private readonly IRelationalTypeMapper _typeMapper;

        public TestRelationalCommandBuilderFactory(
            IDiagnosticsLogger<LoggerCategory.Database.Sql> logger,
            IDiagnosticsLogger<LoggerCategory.Database.DataReader> readerLogger,
            IRelationalTypeMapper typeMapper)
        {
            _logger = logger;
            _readerLogger = readerLogger;
            _typeMapper = typeMapper;
        }

        public virtual IRelationalCommandBuilder Create()
            => new TestRelationalCommandBuilder(_logger, _readerLogger, _typeMapper);

        private class TestRelationalCommandBuilder : IRelationalCommandBuilder
        {
            private readonly IDiagnosticsLogger<LoggerCategory.Database.Sql> _logger;
            private readonly IDiagnosticsLogger<LoggerCategory.Database.DataReader> _readerLogger;

            public TestRelationalCommandBuilder(
                IDiagnosticsLogger<LoggerCategory.Database.Sql> logger,
                IDiagnosticsLogger<LoggerCategory.Database.DataReader> readerLogger,
                IRelationalTypeMapper typeMapper)
            {
                _logger = logger;
                _readerLogger = readerLogger;
                ParameterBuilder = new RelationalParameterBuilder(typeMapper);
            }

            IndentedStringBuilder IInfrastructure<IndentedStringBuilder>.Instance { get; } = new IndentedStringBuilder();

            public IRelationalParameterBuilder ParameterBuilder { get; }

            public IRelationalCommand Build()
                => new TestRelationalCommand(
                    _logger,
                    _readerLogger,
                    ((IInfrastructure<IndentedStringBuilder>)this).Instance.ToString(),
                    ParameterBuilder.Parameters);
        }

        private class TestRelationalCommand : IRelationalCommand
        {
            private readonly RelationalCommand _realRelationalCommand;

            public TestRelationalCommand(
                IDiagnosticsLogger<LoggerCategory.Database.Sql> logger,
                IDiagnosticsLogger<LoggerCategory.Database.DataReader> readerLogger,
                string commandText,
                IReadOnlyList<IRelationalParameter> parameters)
            {
                _realRelationalCommand = new RelationalCommand(logger, readerLogger, commandText, parameters);
            }

            public string CommandText => _realRelationalCommand.CommandText;

            public IReadOnlyList<IRelationalParameter> Parameters => _realRelationalCommand.Parameters;

            public int ExecuteNonQuery(
                IRelationalConnection connection, IReadOnlyDictionary<string, object> parameterValues)
            {
                var errorNumber = PreExecution(connection);

                var result = _realRelationalCommand.ExecuteNonQuery(connection, parameterValues);
                if (errorNumber.HasValue)
                {
                    connection.DbConnection.Close();
                    throw SqlExceptionFactory.CreateSqlException(errorNumber.Value);
                }
                return result;
            }

            int IRelationalCommand.ExecuteNonQuery(IRelationalConnection connection, IReadOnlyDictionary<string, object> parameterValues, bool manageConnection)
            {
                throw new System.NotImplementedException();
            }

            public Task<int> ExecuteNonQueryAsync(
                IRelationalConnection connection, IReadOnlyDictionary<string, object> parameterValues, CancellationToken cancellationToken = new CancellationToken())
            {
                var errorNumber = PreExecution(connection);

                var result = _realRelationalCommand.ExecuteNonQueryAsync(connection, parameterValues, cancellationToken);
                if (errorNumber.HasValue)
                {
                    connection.DbConnection.Close();
                    throw SqlExceptionFactory.CreateSqlException(errorNumber.Value);
                }
                return result;
            }

            Task<int> IRelationalCommand.ExecuteNonQueryAsync(IRelationalConnection connection, IReadOnlyDictionary<string, object> parameterValues, bool manageConnection, CancellationToken cancellationToken)
            {
                throw new System.NotImplementedException();
            }

            public object ExecuteScalar(
                IRelationalConnection connection, IReadOnlyDictionary<string, object> parameterValues)
            {
                var errorNumber = PreExecution(connection);

                var result = _realRelationalCommand.ExecuteScalar(connection, parameterValues);
                if (errorNumber.HasValue)
                {
                    connection.DbConnection.Close();
                    throw SqlExceptionFactory.CreateSqlException(errorNumber.Value);
                }
                return result;
            }

            object IRelationalCommand.ExecuteScalar(IRelationalConnection connection, IReadOnlyDictionary<string, object> parameterValues, bool manageConnection)
            {
                throw new System.NotImplementedException();
            }

            public async Task<object> ExecuteScalarAsync(
                IRelationalConnection connection, IReadOnlyDictionary<string, object> parameterValues, CancellationToken cancellationToken = new CancellationToken())
            {
                var errorNumber = PreExecution(connection);

                var result = await _realRelationalCommand.ExecuteScalarAsync(connection, parameterValues, cancellationToken);
                if (errorNumber.HasValue)
                {
                    connection.DbConnection.Close();
                    throw SqlExceptionFactory.CreateSqlException(errorNumber.Value);
                }
                return result;
            }

            Task<object> IRelationalCommand.ExecuteScalarAsync(IRelationalConnection connection, IReadOnlyDictionary<string, object> parameterValues, bool manageConnection, CancellationToken cancellationToken)
            {
                throw new System.NotImplementedException();
            }

            public RelationalDataReader ExecuteReader(
                IRelationalConnection connection, IReadOnlyDictionary<string, object> parameterValues)
            {
                var errorNumber = PreExecution(connection);

                var result = _realRelationalCommand.ExecuteReader(connection, parameterValues);
                if (errorNumber.HasValue)
                {
                    connection.DbConnection.Close();
                    throw SqlExceptionFactory.CreateSqlException(errorNumber.Value);
                }
                return result;
            }

            RelationalDataReader IRelationalCommand.ExecuteReader(IRelationalConnection connection, IReadOnlyDictionary<string, object> parameterValues, bool manageConnection)
            {
                throw new System.NotImplementedException();
            }

            public async Task<RelationalDataReader> ExecuteReaderAsync(
                IRelationalConnection connection, IReadOnlyDictionary<string, object> parameterValues, CancellationToken cancellationToken = new CancellationToken())
            {
                var errorNumber = PreExecution(connection);

                var result = await _realRelationalCommand.ExecuteReaderAsync(connection, parameterValues, cancellationToken);
                if (errorNumber.HasValue)
                {
                    connection.DbConnection.Close();
                    throw SqlExceptionFactory.CreateSqlException(errorNumber.Value);
                }
                return result;
            }

            Task<RelationalDataReader> IRelationalCommand.ExecuteReaderAsync(IRelationalConnection connection, IReadOnlyDictionary<string, object> parameterValues, bool manageConnection, CancellationToken cancellationToken)
            {
                throw new System.NotImplementedException();
            }

            private int? PreExecution(IRelationalConnection connection)
            {
                int? errorNumber = null;
                var testConnection = (TestSqlServerConnection)connection;

                testConnection.ExecutionCount++;
                if (testConnection.ExecutionFailures.Count > 0)
                {
                    var fail = testConnection.ExecutionFailures.Dequeue();
                    if (fail.HasValue)
                    {
                        if (fail.Value)
                        {
                            testConnection.DbConnection.Close();
                            throw SqlExceptionFactory.CreateSqlException(testConnection.ErrorNumber);
                        }
                        errorNumber = testConnection.ErrorNumber;
                    }
                }
                return errorNumber;
            }
        }
    }
}

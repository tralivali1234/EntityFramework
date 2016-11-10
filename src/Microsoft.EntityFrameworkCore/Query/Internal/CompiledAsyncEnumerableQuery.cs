// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using JetBrains.Annotations;

namespace Microsoft.EntityFrameworkCore.Query.Internal
{
    /// <summary>
    ///     This API supports the Entity Framework Core infrastructure and is not intended to be used
    ///     directly from your code. This API may change or be removed in future releases.
    /// </summary>
    public class CompiledAsyncEnumerableQuery<TContext, TResult> : CompiledQueryBase<TContext, IAsyncEnumerable<TResult>>
        where TContext : DbContext
    {
        /// <summary>
        ///     This API supports the Entity Framework Core infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public CompiledAsyncEnumerableQuery([NotNull] LambdaExpression queryExpression)
            : base(queryExpression)
        {
        }

        /// <summary>
        ///     This API supports the Entity Framework Core infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public virtual IAsyncEnumerable<TResult> Execute(
                [NotNull] TContext context)
            => ExecuteCore(context);

        /// <summary>
        ///     This API supports the Entity Framework Core infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public virtual IAsyncEnumerable<TResult> Execute<TParam1>(
                [NotNull] TContext context,
                [CanBeNull] TParam1 param1)
            => ExecuteCore(context, param1);

        /// <summary>
        ///     This API supports the Entity Framework Core infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public virtual IAsyncEnumerable<TResult> Execute<TParam1, TParam2>(
                [NotNull] TContext context,
                [CanBeNull] TParam1 param1,
                [CanBeNull] TParam2 param2)
            => ExecuteCore(context, param1, param2);

        /// <summary>
        ///     This API supports the Entity Framework Core infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public virtual IAsyncEnumerable<TResult> Execute<TParam1, TParam2, TParam3>(
                [NotNull] TContext context,
                [CanBeNull] TParam1 param1,
                [CanBeNull] TParam2 param2,
                [CanBeNull] TParam3 param3)
            => ExecuteCore(context, param1, param2, param3);

        /// <summary>
        ///     This API supports the Entity Framework Core infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public virtual IAsyncEnumerable<TResult> Execute<TParam1, TParam2, TParam3, TParam4>(
                [NotNull] TContext context,
                [CanBeNull] TParam1 param1,
                [CanBeNull] TParam2 param2,
                [CanBeNull] TParam3 param3,
                [CanBeNull] TParam4 param4)
            => ExecuteCore(context, param1, param2, param3, param4);

        /// <summary>
        ///     This API supports the Entity Framework Core infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public virtual IAsyncEnumerable<TResult> Execute<TParam1, TParam2, TParam3, TParam4, TParam5>(
                [NotNull] TContext context,
                [CanBeNull] TParam1 param1,
                [CanBeNull] TParam2 param2,
                [CanBeNull] TParam3 param3,
                [CanBeNull] TParam4 param4,
                [CanBeNull] TParam5 param5)
            => ExecuteCore(context, param1, param2, param3, param4, param5);

        /// <summary>
        ///     This API supports the Entity Framework Core infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        protected override Func<QueryContext, IAsyncEnumerable<TResult>> CreateCompiledQuery(
                IQueryCompiler queryCompiler, Expression expression)
            => queryCompiler.CreateCompiledAsyncEnumerableQuery<TResult>(expression);
    }
}

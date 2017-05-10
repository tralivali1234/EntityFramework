// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Linq.Expressions;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore.Extensions.Internal;
using Remotion.Linq.Clauses;
using Remotion.Linq.Clauses.Expressions;

namespace Microsoft.EntityFrameworkCore.Query.ExpressionVisitors.Internal
{
    /// <summary>
    ///     This API supports the Entity Framework Core infrastructure and is not intended to be used
    ///     directly from your code. This API may change or be removed in future releases.
    /// </summary>
    public class SubQueryMemberPushDownExpressionVisitor : ExpressionVisitorBase
    {
        private readonly QueryCompilationContext _queryCompilationContext;

        /// <summary>
        ///     This API supports the Entity Framework Core infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public SubQueryMemberPushDownExpressionVisitor([NotNull] QueryCompilationContext queryCompilationContext)
        {
            _queryCompilationContext = queryCompilationContext;
        }

        /// <summary>
        ///     This API supports the Entity Framework Core infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        protected override Expression VisitMember(MemberExpression memberExpression)
        {
            var newExpression = Visit(memberExpression.Expression);

            var subQueryExpression = newExpression as SubQueryExpression;
            var subSelector = subQueryExpression?.QueryModel.SelectClause.Selector;

            if (subSelector is QuerySourceReferenceExpression
                || subSelector is SubQueryExpression)
            {
                var querySourceMapping = new QuerySourceMapping();
                var subQueryModel = subQueryExpression.QueryModel.Clone(querySourceMapping);
                _queryCompilationContext.UpdateMapping(querySourceMapping);

                subQueryModel.SelectClause.Selector = VisitMember(memberExpression.Update(subQueryModel.SelectClause.Selector));
                subQueryModel.ResultTypeOverride = subQueryModel.SelectClause.Selector.Type;

                return new SubQueryExpression(subQueryModel);
            }

            return memberExpression.Update(newExpression);
        }

        /// <summary>
        ///     This API supports the Entity Framework Core infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        protected override Expression VisitMethodCall(MethodCallExpression methodCallExpression)
        {
            var newMethodCallExpression = (MethodCallExpression)base.VisitMethodCall(methodCallExpression);

            if (methodCallExpression.Method.IsEFPropertyMethod())
            {
                var subQueryExpression = newMethodCallExpression.Arguments[0] as SubQueryExpression;
                if (subQueryExpression?.QueryModel.SelectClause.Selector is QuerySourceReferenceExpression subSelector)
                {
                    var subQueryModel = subQueryExpression.QueryModel;

                    subQueryModel.SelectClause.Selector
                        = methodCallExpression
                            .Update(
                                null,
                                new[]
                                {
                                    subSelector,
                                    methodCallExpression.Arguments[1]
                                });

                    subQueryModel.ResultTypeOverride = subQueryModel.SelectClause.Selector.Type;

                    return new SubQueryExpression(subQueryModel);
                }
            }

            return newMethodCallExpression;
        }
    }
}

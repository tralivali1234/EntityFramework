// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore.Internal;

namespace Microsoft.EntityFrameworkCore.Metadata.Internal
{
    /// <summary>
    ///     Extension methods for <see cref="IProperty" /> for relational database metadata.
    /// </summary>
    public static class PropertyExtensions
    {
        /// <summary>
        ///     This API supports the Entity Framework Core infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public static IProperty FindSharedTablePrincipalPrimaryKeyProperty([NotNull] this IProperty property)
        {
            var linkingRelationship = property.FindSharedTableLink();
            return linkingRelationship?.PrincipalKey.Properties[linkingRelationship.Properties.IndexOf(property)];
        }

        /// <summary>
        ///     This API supports the Entity Framework Core infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public static IForeignKey FindSharedTableLink([NotNull] this IProperty property)
        {
            var pk = property.GetContainingPrimaryKey();
            if (pk == null)
            {
                return null;
            }

            var entityType = property.DeclaringEntityType;

            foreach (var fk in entityType.FindForeignKeys(pk.Properties))
            {
                if (!fk.PrincipalKey.IsPrimaryKey()
                    || fk.PrincipalEntityType == fk.DeclaringEntityType)
                {
                    continue;
                }

                var principalEntityType = fk.PrincipalEntityType;
                var entityTypeAnnotations = fk.DeclaringEntityType.Relational();
                var principalTypeAnnotations = principalEntityType.Relational();
                if (entityTypeAnnotations.TableName == principalTypeAnnotations.TableName
                    && entityTypeAnnotations.Schema == principalTypeAnnotations.Schema)
                {
                    return fk;
                }
            }

            return null;
        }
    }
}

// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.EntityFrameworkCore.Metadata
{
    /// <summary>
    ///     Properties for relational-specific annotations accessed through
    ///     <see cref="SqlServerMetadataExtensions.SqlServer(IIndex)" />.
    /// </summary>
    public interface ISqlServerIndexAnnotations : IRelationalIndexAnnotations
    {
        /// <summary>
        ///     Indicates whether or not the index is clustered, or <c>null</c> if clustering has not
        ///     been specified.
        /// </summary>
        bool? IsClustered { get; }
    }
}

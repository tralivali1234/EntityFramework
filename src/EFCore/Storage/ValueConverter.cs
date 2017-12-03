// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq.Expressions;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.EntityFrameworkCore.Storage.Internal;
using Microsoft.EntityFrameworkCore.Utilities;

namespace Microsoft.EntityFrameworkCore.Storage
{
    /// <summary>
    ///     Defines conversions from an object of one type in a model to an object of the same or
    ///     different type in the store.
    /// </summary>
    public abstract class ValueConverter
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="ValueConverter" /> class.
        /// </summary>
        /// <param name="convertToStore">
        ///     The function to convert objects when writing data to the store,
        ///     setup to handle nulls, boxing, and non-exact matches of simple types.
        /// </param>
        /// <param name="convertFromStore">
        ///     The function to convert objects when reading data from the store,
        ///     setup to handle nulls, boxing, and non-exact matches of simple types.
        /// </param>
        /// <param name="convertToStoreExpression">
        ///     The expression to convert objects when writing data to the store,
        ///     exactly as supplied and may not handle
        ///     nulls, boxing, and non-exact matches of simple types.
        /// </param>
        /// <param name="convertFromStoreExpression">
        ///     The expression to convert objects when reading data from the store,
        ///     exactly as supplied and may not handle
        ///     nulls, boxing, and non-exact matches of simple types.
        /// </param>
        protected ValueConverter(
            [NotNull] Func<object, object> convertToStore,
            [NotNull] Func<object, object> convertFromStore,
            [NotNull] LambdaExpression convertToStoreExpression,
            [NotNull] LambdaExpression convertFromStoreExpression)

        {
            Check.NotNull(convertToStore, nameof(convertToStore));
            Check.NotNull(convertFromStore, nameof(convertFromStore));
            Check.NotNull(convertToStoreExpression, nameof(convertToStoreExpression));
            Check.NotNull(convertFromStoreExpression, nameof(convertFromStoreExpression));

            ConvertToStore = convertToStore;
            ConvertFromStore = convertFromStore;
            ConvertToStoreExpression = convertToStoreExpression;
            ConvertFromStoreExpression = convertFromStoreExpression;
        }

        /// <summary>
        ///     Gets the function to convert objects when writing data to the store,
        ///     setup to handle nulls, boxing, and non-exact matches of simple types.
        /// </summary>
        public virtual Func<object, object> ConvertToStore { get; }

        /// <summary>
        ///     Gets the function to convert objects when reading data from the store,
        ///     setup to handle nulls, boxing, and non-exact matches of simple types.
        /// </summary>
        public virtual Func<object, object> ConvertFromStore { get; }

        /// <summary>
        ///     Gets the expression to convert objects when writing data to the store,
        ///     exactly as supplied and may not handle
        ///     nulls, boxing, and non-exact matches of simple types.
        /// </summary>
        public virtual LambdaExpression ConvertToStoreExpression { get; }

        /// <summary>
        ///     Gets the expression to convert objects when reading data from the store,
        ///     exactly as supplied and may not handle
        ///     nulls, boxing, and non-exact matches of simple types.
        /// </summary>
        public virtual LambdaExpression ConvertFromStoreExpression { get; }

        /// <summary>
        ///     The CLR type used in the EF model.
        /// </summary>
        public abstract Type ModelType { get; }

        /// <summary>
        ///     The CLR type used when reading and writing from the store.
        /// </summary>
        public abstract Type StoreType { get; }

        /// <summary>
        ///     Composes two <see cref="ValueConverter" /> instances together such that
        ///     the result of the first conversion is used as the input to the second conversion.
        /// </summary>
        /// <param name="firstConverter"> The first converter. </param>
        /// <param name="secondConverter"> The second converter. </param>
        /// <returns> The composed converter. </returns>
        public static ValueConverter Compose(
            [CanBeNull] ValueConverter firstConverter,
            [CanBeNull] ValueConverter secondConverter)
        {
            if (firstConverter == null
                || secondConverter == null)
            {
                return firstConverter ?? secondConverter;
            }

            if (firstConverter.StoreType != secondConverter.ModelType)
            {
                throw new ArgumentException(CoreStrings.ConvertersCannotBeComposed(
                    firstConverter.ModelType.ShortDisplayName(),
                    firstConverter.StoreType.ShortDisplayName(), 
                    secondConverter.ModelType.ShortDisplayName(),
                    secondConverter.StoreType.ShortDisplayName()));
            }

            return (ValueConverter)Activator.CreateInstance(
                typeof(ComposedValueConveter<,,>).MakeGenericType(
                    firstConverter.ModelType,
                    firstConverter.StoreType,
                    secondConverter.StoreType),
                firstConverter,
                secondConverter);
        }
    }
}

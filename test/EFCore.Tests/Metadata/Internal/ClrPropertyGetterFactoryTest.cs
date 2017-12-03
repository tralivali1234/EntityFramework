// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Xunit;

namespace Microsoft.EntityFrameworkCore.Metadata.Internal
{
    public class ClrPropertyGetterFactoryTest
    {
        [Fact]
        public void Property_is_returned_if_it_implements_IClrPropertyGetter()
        {
            var property = new FakeProperty();

            Assert.Same(property, new ClrPropertyGetterFactory().Create(property));
        }

        private class FakeProperty : IProperty, IClrPropertyGetter
        {
            public object GetClrValue(object instance) => throw new NotImplementedException();
            public object this[string name] => throw new NotImplementedException();
            public IAnnotation FindAnnotation(string name) => throw new NotImplementedException();
            public IEnumerable<IAnnotation> GetAnnotations() => throw new NotImplementedException();
            public string Name { get; }
            public ITypeBase DeclaringType { get; }
            public Type ClrType { get; }
            public bool IsShadowProperty { get; }
            public IEntityType DeclaringEntityType { get; }
            public bool IsNullable { get; }
            public PropertySaveBehavior BeforeSaveBehavior { get; }
            public PropertySaveBehavior AfterSaveBehavior { get; }
            public bool IsReadOnlyBeforeSave { get; }
            public bool IsReadOnlyAfterSave { get; }
            public bool IsStoreGeneratedAlways { get; }
            public ValueGenerated ValueGenerated { get; }
            public bool IsConcurrencyToken { get; }
            public PropertyInfo PropertyInfo { get; }
            public FieldInfo FieldInfo { get; }
        }

        [Fact]
        public void Delegate_getter_is_returned_for_IProperty_property()
        {
            var entityType = new Model().AddEntityType(typeof(Customer));
            var idProperty = entityType.AddProperty("Id", typeof(int));

            Assert.Equal(7, new ClrPropertyGetterFactory().Create(idProperty).GetClrValue(new Customer { Id = 7 }));
        }

        [Fact]
        public void Delegate_getter_is_returned_for_property_info()
        {
            Assert.Equal(7, new ClrPropertyGetterFactory().Create(typeof(Customer).GetAnyProperty("Id")).GetClrValue(new Customer { Id = 7 }));
        }

        private class Customer
        {
            internal int Id { get; set; }
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System;
using System.Collections;
using System.Collections.Generic;

namespace System.Windows.Markup
{
    [MarkupExtensionReturnType(typeof(object))]
    public class StaticExtension : MarkupExtension
    {
        public StaticExtension()
        {
        }

        public StaticExtension(string member)
        {
            ArgumentNullException.ThrowIfNull(member);
            Member = member;
        }

        [ConstructorArgument("member")]
        public string Member { get; set; }

        public Type MemberType { get; set; }

        public override object ProvideValue(IServiceProvider serviceProvider) => null;
    }

    [MarkupExtensionReturnType(typeof(Type))]
    public class TypeExtension : MarkupExtension
    {
        private string _typeName;
        private Type _type;

        public TypeExtension()
        {
        }

        public TypeExtension(string typeName)
        {
            ArgumentNullException.ThrowIfNull(typeName);
            _typeName = typeName;
        }

        public TypeExtension(Type type)
        {
            ArgumentNullException.ThrowIfNull(type);
            _type = type;
        }

        [ConstructorArgument("typeName")]
        public string TypeName
        {
            get => _typeName;
            set
            {
                ArgumentNullException.ThrowIfNull(value);
                _typeName = value;
                _type = null;
            }
        }

        [ConstructorArgument("type")]
        public Type Type
        {
            get => _type;
            set
            {
                ArgumentNullException.ThrowIfNull(value);
                _type = value;
                _typeName = null;
            }
        }

        public override object ProvideValue(IServiceProvider serviceProvider) => _type;
    }

    [MarkupExtensionReturnType(typeof(object))]
    public class NullExtension : MarkupExtension
    {
        public NullExtension()
        {
        }

        public override object ProvideValue(IServiceProvider serviceProvider) => null;
    }

    [ContentProperty("Items")]
    [MarkupExtensionReturnType(typeof(Array))]
    public class ArrayExtension : MarkupExtension
    {
        private readonly ArrayList _items = new ArrayList();

        public ArrayExtension()
        {
        }

        public ArrayExtension(Type arrayType)
        {
            ArgumentNullException.ThrowIfNull(arrayType);
            Type = arrayType;
        }

        public ArrayExtension(object[] elements)
        {
            ArgumentNullException.ThrowIfNull(elements);
            _items.AddRange(elements);
        }

        [ConstructorArgument("arrayType")]
        public Type Type { get; set; }

        public IList Items => _items;

        public void AddChild(object value) => _items.Add(value);

        public void AddText(string text)
        {
            if (text != null)
            {
                _items.Add(text);
            }
        }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            if (Type == null)
            {
                return _items.ToArray();
            }

            if (Type == typeof(string)) return ToTypedArray<string>();
            if (Type == typeof(int)) return ToTypedArray<int>();
            if (Type == typeof(double)) return ToTypedArray<double>();
            if (Type == typeof(bool)) return ToTypedArray<bool>();
            return _items.ToArray();
        }

        private T[] ToTypedArray<T>()
        {
            var arr = new T[_items.Count];
            for (int i = 0; i < _items.Count; i++)
            {
                arr[i] = _items[i] is T typed ? typed : default;
            }
            return arr;
        }
    }

    [ContentProperty("Name")]
    public class Reference : MarkupExtension
    {
        public Reference()
        {
        }

        public Reference(string name)
        {
            Name = name;
        }

        [ConstructorArgument("name")]
        public string Name { get; set; }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            if (string.IsNullOrEmpty(Name))
            {
                throw new InvalidOperationException("Reference.Name must be set.");
            }
            return null;
        }
    }
}

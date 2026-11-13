// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// Clean zero-reflection XAML contract interfaces & attributes preserved for WPF API compatibility.

#nullable disable

using System;
using System.Collections.Generic;

namespace System.Windows.Markup
{
    public interface INameScope
    {
        void RegisterName(string name, object scopedElement);
        void UnregisterName(string name);
        object FindName(string name);
    }

    public interface INameScopeDictionary : INameScope, IDictionary<string, object>
    {
    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public sealed class NameScopePropertyAttribute : Attribute
    {
        public NameScopePropertyAttribute(string name) => Name = name;
        public NameScopePropertyAttribute(string name, Type type) { Name = name; Type = type; }
        public string Name { get; }
        public Type Type { get; }
    }

    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Method | AttributeTargets.Event | AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public sealed class ContentPropertyAttribute : Attribute
    {
        public ContentPropertyAttribute() { }
        public ContentPropertyAttribute(string name) => Name = name;
        public string Name { get; }
    }

    public interface IProvideValueTarget
    {
        object TargetObject { get; }
        object TargetProperty { get; }
    }

    public interface IUriContext
    {
        Uri BaseUri { get; set; }
    }

    public abstract class MarkupExtension
    {
        public abstract object ProvideValue(IServiceProvider serviceProvider);
    }

    public interface IComponentConnector
    {
        void InitializeComponent();
        void Connect(int connectionId, object target);
    }

    public interface IAddChild
    {
        void AddChild(object value);
        void AddText(string text);
    }

    public interface IAddChild<in T> : IAddChild
    {
        void AddChild(T value);
    }

    public interface IQueryAmbient
    {
        bool IsAmbientPropertyAvailable(string propertyName);
    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method | AttributeTargets.Property, Inherited = true)]
    public sealed class AmbientAttribute : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public sealed class UsableDuringInitializationAttribute : Attribute
    {
        public UsableDuringInitializationAttribute(bool usable) => Usable = usable;
        public bool Usable { get; }
    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public sealed class XmlLangPropertyAttribute : Attribute
    {
        public XmlLangPropertyAttribute(string name) => Name = name;
        public string Name { get; }
    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public sealed class DictionaryKeyPropertyAttribute : Attribute
    {
        public DictionaryKeyPropertyAttribute(string name) => Name = name;
        public string Name { get; }
    }


    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
    public sealed class DependsOnAttribute : Attribute
    {
        public DependsOnAttribute(string name) => Name = name;
        public string Name { get; }
    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public sealed class MarkupExtensionReturnTypeAttribute : Attribute
    {
        public MarkupExtensionReturnTypeAttribute() { }
        public MarkupExtensionReturnTypeAttribute(Type returnType) => ReturnType = returnType;
        public Type ReturnType { get; }
    }

    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public sealed class TrimSurroundingWhitespaceAttribute : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public sealed class WhitespaceSignificantCollectionAttribute : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
    public sealed class ConstructorArgumentAttribute : Attribute
    {
        public ConstructorArgumentAttribute(string argumentName) => ArgumentName = argumentName;
        public string ArgumentName { get; }
    }

    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
    public sealed class XmlnsDefinitionAttribute : Attribute
    {
        public XmlnsDefinitionAttribute(string xmlNamespace, string clrNamespace)
        {
            ArgumentNullException.ThrowIfNull(xmlNamespace);
            ArgumentNullException.ThrowIfNull(clrNamespace);
            XmlNamespace = xmlNamespace;
            ClrNamespace = clrNamespace;
        }

        public string XmlNamespace { get; }
        public string ClrNamespace { get; }
        public string AssemblyName { get; set; }
    }

    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
    public sealed class XmlnsPrefixAttribute : Attribute
    {
        public XmlnsPrefixAttribute(string xmlNamespace, string prefix)
        {
            ArgumentNullException.ThrowIfNull(xmlNamespace);
            ArgumentNullException.ThrowIfNull(prefix);
            XmlNamespace = xmlNamespace;
            Prefix = prefix;
        }

        public string XmlNamespace { get; }
        public string Prefix { get; }
    }

    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
    public sealed class XmlnsCompatibleWithAttribute : Attribute
    {
        public XmlnsCompatibleWithAttribute(string oldNamespace, string newNamespace)
        {
            ArgumentNullException.ThrowIfNull(oldNamespace);
            ArgumentNullException.ThrowIfNull(newNamespace);
            OldNamespace = oldNamespace;
            NewNamespace = newNamespace;
        }

        public string OldNamespace { get; }
        public string NewNamespace { get; }
    }

    public class XamlParseException : SystemException
    {
        public XamlParseException() { }
        public XamlParseException(string message) : base(message) { }
        public XamlParseException(string message, Exception innerException) : base(message, innerException) { }
        public XamlParseException(string message, int lineNumber, int linePosition) : base(message)
        {
            LineNumber = lineNumber;
            LinePosition = linePosition;
        }
        public XamlParseException(string message, int lineNumber, int linePosition, Exception innerException) : base(message, innerException)
        {
            LineNumber = lineNumber;
            LinePosition = linePosition;
        }

        public int LineNumber { get; set; }
        public int LinePosition { get; set; }
        public object KeyContext { get; set; }
        public string UidContext { get; set; }
        public string NameContext { get; set; }
        public Uri BaseUri { get; set; }
    }
}

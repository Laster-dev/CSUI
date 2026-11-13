using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Media.Media3D;
using System.Windows.Media.Svg;
using System.Xml.Linq;

namespace CSUI.Markup
{
    public sealed class XamlLoadContext
    {
        private readonly Dictionary<string, object> _namedElements = new Dictionary<string, object>(StringComparer.Ordinal);
        private readonly Dictionary<string, object> _scopedResources = new Dictionary<string, object>(StringComparer.Ordinal);
        private readonly Dictionary<string, Func<Binding>> _bindingFactories = new Dictionary<string, Func<Binding>>(StringComparer.Ordinal);
        private readonly Dictionary<string, Delegate> _eventHandlers = new Dictionary<string, Delegate>(StringComparer.Ordinal);
        internal readonly NameScope NameScope = new NameScope();

        public object DataContext { get; set; }

        internal void RegisterResource(string key, object value)
        {
            if (!string.IsNullOrEmpty(key) && value != null)
            {
                _scopedResources[key] = value;
            }
        }

        internal object TryFindScopedResource(string key)
        {
            return !string.IsNullOrEmpty(key) && _scopedResources.TryGetValue(key, out object val) ? val : null;
        }

        public void RegisterName(string name, object instance)
        {
            if (!string.IsNullOrEmpty(name) && instance != null)
            {
                _namedElements[name] = instance;
                if (NameScope.FindName(name) == null)
                {
                    NameScope.RegisterName(name, instance);
                }
            }
        }

        public T FindName<T>(string name) where T : class
        {
            return _namedElements.TryGetValue(name, out object obj) ? obj as T : null;
        }

        public object FindName(string name)
        {
            return _namedElements.TryGetValue(name, out object obj) ? obj : null;
        }

        public XamlLoadContext RegisterBinding<TSource, TProp>(
            string path,
            Func<TSource, TProp> getter,
            Action<TSource, TProp> setter = null,
            TSource source = default,
            IValueConverter converter = null)
        {
            _bindingFactories[path] = () =>
            {
                Binding b = Binding.Create<TSource, TProp>(getter, setter, path);
                if (source != null)
                {
                    b.Source = source;
                }
                else if (DataContext != null)
                {
                    b.Source = DataContext;
                }
                if (converter != null)
                {
                    b.Converter = converter;
                }
                return b;
            };
            return this;
        }

        public XamlLoadContext RegisterEventHandler(string methodName, Delegate handler)
        {
            if (!string.IsNullOrEmpty(methodName) && handler != null)
            {
                _eventHandlers[methodName] = handler;
            }
            return this;
        }

        internal bool TryCreateBinding(string path, out Binding binding)
        {
            if (_bindingFactories.TryGetValue(path, out Func<Binding> factory))
            {
                binding = factory();
                return true;
            }
            binding = null;
            return false;
        }

        internal bool TryGetEventHandler(string methodName, out Delegate handler)
        {
            return _eventHandlers.TryGetValue(methodName, out handler);
        }
    }

    internal sealed class XamlTypeMetadata
    {
        public string XmlName { get; set; }
        public Type ClrType { get; set; }
        public Func<object> Factory { get; set; }
        public DependencyProperty ContentProperty { get; set; }
        public Action<object, object> AddChild { get; set; }
        public Dictionary<string, Action<object, string, XamlLoadContext>> ClrPropertySetters { get; } =
            new Dictionary<string, Action<object, string, XamlLoadContext>>(StringComparer.Ordinal);
        public Dictionary<string, Action<object, object>> ClrObjectPropertySetters { get; } =
            new Dictionary<string, Action<object, object>>(StringComparer.Ordinal);
        public Dictionary<string, Action<object, Delegate>> EventSubscribers { get; } =
            new Dictionary<string, Action<object, Delegate>>(StringComparer.Ordinal);
    }

    /// <summary>
    /// NativeAOT zero-reflection XAML loader and type/property registry.
    /// Supports both runtime XAML parsing without reflection and compile-time XAML-to-C# generation.
    /// </summary>
    public static class XamlMarkup
    {
        private static readonly Dictionary<string, XamlTypeMetadata> s_typesByName =
            new Dictionary<string, XamlTypeMetadata>(StringComparer.Ordinal);

        private static readonly Dictionary<Type, XamlTypeMetadata> s_typesByClr =
            new Dictionary<Type, XamlTypeMetadata>();

        private static readonly Dictionary<(Type owner, string name), DependencyProperty> s_dpByOwnerAndName =
            new Dictionary<(Type owner, string name), DependencyProperty>();

        private static readonly Dictionary<string, DependencyProperty> s_dpByShortName =
            new Dictionary<string, DependencyProperty>(StringComparer.Ordinal);

        static XamlMarkup()
        {
            RegisterCoreTypesAndProperties();
        }

        public static void RegisterType<T>(
            string xmlName = null,
            DependencyProperty contentProperty = null,
            Action<T, object> addChild = null)
            where T : new()
        {
            Type clrType = typeof(T);
            string name = xmlName ?? clrType.Name;

            if (typeof(DependencyObject).IsAssignableFrom(clrType))
            {
                FrameworkElementFactory.RegisterFactory(clrType, static () => (DependencyObject)(object)new T());
            }

            Action<object, object> untypedAdd = null;
            if (addChild != null)
            {
                untypedAdd = (parent, child) => addChild((T)parent, child);
            }
            else if (typeof(Panel).IsAssignableFrom(clrType))
            {
                untypedAdd = static (parent, child) =>
                {
                    if (child is UIElement uie)
                    {
                        ((Panel)parent).Children.Add(uie);
                    }
                };
            }
            else if (typeof(Decorator).IsAssignableFrom(clrType))
            {
                untypedAdd = static (parent, child) =>
                {
                    if (child is UIElement uie)
                    {
                        ((Decorator)parent).Child = uie;
                    }
                };
            }
            else if (typeof(ContentControl).IsAssignableFrom(clrType))
            {
                untypedAdd = static (parent, child) => ((ContentControl)parent).Content = child;
            }
            else if (typeof(ItemsControl).IsAssignableFrom(clrType))
            {
                untypedAdd = static (parent, child) => ((ItemsControl)parent).Items.Add(child);
            }
            else if (typeof(IAddChild).IsAssignableFrom(clrType))
            {
                untypedAdd = static (parent, child) => ((IAddChild)parent).AddChild(child);
            }

            if (!s_typesByClr.TryGetValue(clrType, out XamlTypeMetadata meta))
            {
                meta = new XamlTypeMetadata
                {
                    XmlName = name,
                    ClrType = clrType,
                    Factory = static () => new T(),
                    ContentProperty = contentProperty,
                    AddChild = untypedAdd
                };
                s_typesByClr[clrType] = meta;
            }
            else
            {
                meta.Factory = static () => new T();
                if (contentProperty != null) meta.ContentProperty = contentProperty;
                if (untypedAdd != null) meta.AddChild = untypedAdd;
            }

            s_typesByName[name] = meta;
        }

        public static void RegisterProperty(Type ownerType, string propertyName, DependencyProperty dp)
        {
            ArgumentNullException.ThrowIfNull(ownerType);
            ArgumentNullException.ThrowIfNull(propertyName);
            ArgumentNullException.ThrowIfNull(dp);

            s_dpByOwnerAndName[(ownerType, propertyName)] = dp;
            s_dpByShortName.TryAdd(propertyName, dp);
        }

        public static void RegisterClrProperty<T>(
            string propertyName,
            Action<T, string, XamlLoadContext> stringSetter,
            Action<T, object> objectSetter = null)
        {
            if (!s_typesByClr.TryGetValue(typeof(T), out XamlTypeMetadata meta))
            {
                meta = new XamlTypeMetadata { XmlName = typeof(T).Name, ClrType = typeof(T) };
                s_typesByClr[typeof(T)] = meta;
            }
            if (stringSetter != null)
            {
                meta.ClrPropertySetters[propertyName] = (target, raw, ctx) => stringSetter((T)target, raw, ctx);
            }
            if (objectSetter != null)
            {
                meta.ClrObjectPropertySetters[propertyName] = (target, val) => objectSetter((T)target, val);
            }
        }

        public static void RegisterClrProperty<T, TProp>(
            string propertyName,
            Action<T, TProp> setter,
            Func<T, TProp> getter = null)
        {
            ArgumentNullException.ThrowIfNull(propertyName);
            ArgumentNullException.ThrowIfNull(setter);

            RegisterClrProperty<T>(
                propertyName,
                (target, raw, ctx) =>
                {
                    object parsed = ConvertStringValue(raw, typeof(TProp));
                    if (parsed is TProp typed)
                    {
                        setter(target, typed);
                    }
                    else if (parsed == null && default(TProp) == null)
                    {
                        setter(target, default);
                    }
                },
                (target, obj) =>
                {
                    if (obj is TProp typed)
                    {
                        setter(target, typed);
                    }
                    else if (obj == null && default(TProp) == null)
                    {
                        setter(target, default);
                    }
                });

            if (getter != null)
            {
                PropertyAccessorRegistry.Register(propertyName, getter, setter);
            }
        }

        public static void RegisterEvent<T>(string eventName, Action<T, Delegate> subscribe)
        {
            if (!s_typesByClr.TryGetValue(typeof(T), out XamlTypeMetadata meta))
            {
                meta = new XamlTypeMetadata { XmlName = typeof(T).Name, ClrType = typeof(T) };
                s_typesByClr[typeof(T)] = meta;
            }
            meta.EventSubscribers[eventName] = (target, handler) => subscribe((T)target, handler);
        }

        public static T Load<T>(string xaml, out XamlLoadContext context, XamlLoadContext existingContext = null)
        {
            context = existingContext ?? new XamlLoadContext();
            XDocument doc = XDocument.Parse(xaml);
            if (doc.Root == null)
            {
                throw new XamlParseException("XAML document has no root element.");
            }
            object result = InstantiateElement(doc.Root, existingInstance: null, parentContextElement: null, context);
            return (T)result;
        }

        public static T Load<T>(string xaml, XamlLoadContext context = null)
        {
            return Load<T>(xaml, out _, context);
        }

        public static void LoadInto<T>(T rootInstance, string xaml, out XamlLoadContext context, XamlLoadContext existingContext = null)
            where T : class
        {
            ArgumentNullException.ThrowIfNull(rootInstance);
            context = existingContext ?? new XamlLoadContext();
            XDocument doc = XDocument.Parse(xaml);
            if (doc.Root == null)
            {
                throw new XamlParseException("XAML document has no root element.");
            }
            InstantiateElement(doc.Root, rootInstance, parentContextElement: null, context);
        }

        private static object InstantiateElement(
            XElement xmlElement,
            object existingInstance,
            FrameworkElement parentContextElement,
            XamlLoadContext context,
            Action<object> attachToParent = null)
        {
            string localName = xmlElement.Name.LocalName;

            if (localName == "ResourceDictionary" && existingInstance is ResourceDictionary existingDict)
            {
                PopulateResourceDictionary(xmlElement, existingDict, parentContextElement, context);
                return existingDict;
            }
            if (localName == "Style")
            {
                return ParseStyleElement(xmlElement, parentContextElement, context);
            }
            if (localName == "ControlTemplate")
            {
                return ParseControlTemplateElement(xmlElement, context);
            }
            if (localName == "Array" || localName == "ArrayExtension")
            {
                return ParseArrayElement(xmlElement, parentContextElement, context);
            }
            string xmlNs = xmlElement.Name.NamespaceName;
            if (xmlNs == "http://schemas.microsoft.com/winfx/2006/xaml" ||
                xmlNs.StartsWith("clr-namespace:System;", StringComparison.Ordinal) ||
                xmlNs == "clr-namespace:System")
            {
                if (localName == "String") return xmlElement.Value;
                if (localName == "Int32" && int.TryParse(xmlElement.Value.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int iVal)) return iVal;
                if (localName == "Double" && double.TryParse(xmlElement.Value.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out double dVal)) return dVal;
                if (localName == "Boolean" && bool.TryParse(xmlElement.Value.Trim(), out bool bVal)) return bVal;
            }

            if (!s_typesByName.TryGetValue(localName, out XamlTypeMetadata meta))
            {
                throw new XamlParseException($"Unregistered XAML element type '{localName}'. Call XamlMarkup.RegisterType<{localName}>() for custom types.");
            }

            object instance = existingInstance ?? meta.Factory();
            if (parentContextElement == null && instance is DependencyObject rootDo)
            {
                NameScope.SetNameScope(rootDo, context.NameScope);
                if (instance is FrameworkElement rootFe)
                {
                    if (context.DataContext != null && rootFe.DataContext == null)
                    {
                        rootFe.DataContext = context.DataContext;
                    }
                    else if (context.DataContext == null && rootFe.DataContext != null)
                    {
                        context.DataContext = rootFe.DataContext;
                    }
                }
            }
            attachToParent?.Invoke(instance);
            FrameworkElement currentFe = (instance as FrameworkElement) ?? parentContextElement;

            if (instance is ResourceDictionary dictInstance)
            {
                PopulateResourceDictionary(xmlElement, dictInstance, currentFe, context);
                return dictInstance;
            }

            // 1. Process XML Attributes
            foreach (XAttribute attr in xmlElement.Attributes())
            {
                if (attr.IsNamespaceDeclaration)
                {
                    continue;
                }

                string attrName = attr.Name.LocalName;
                string attrVal = attr.Value;

                if (attrName == "Class" || attrName == "DataType" || attrName == "Key" || attrName == "Shared")
                {
                    continue;
                }

                if (attrName == "Name")
                {
                    if (instance is FrameworkElement feName)
                    {
                        feName.Name = attrVal;
                    }
                    context.RegisterName(attrVal, instance);
                    continue;
                }

                ApplyAttribute(instance, meta, attrName, attrVal, currentFe, context);
            }

            // 2. Process Child XML Elements (Property Elements vs Content Children)
            foreach (XNode node in xmlElement.Nodes())
            {
                if (node is XText textNode)
                {
                    string trimmed = textNode.Value?.Trim();
                    if (!string.IsNullOrEmpty(trimmed))
                    {
                        if (meta.ContentProperty != null && instance is DependencyObject doTarget)
                        {
                            object parsedContent = ConvertStringValue(trimmed, meta.ContentProperty.PropertyType);
                            doTarget.SetValue(meta.ContentProperty, parsedContent);
                        }
                        else
                        {
                            meta.AddChild?.Invoke(instance, trimmed);
                        }
                    }
                    continue;
                }

                if (node is XElement childEl)
                {
                    string childName = childEl.Name.LocalName;
                    int dotIdx = childName.IndexOf('.');
                    if (dotIdx > 0)
                    {
                        // Property Element syntax: <Owner.PropertyName>
                        string ownerPart = childName.Substring(0, dotIdx);
                        string propPart = childName.Substring(dotIdx + 1);
                        ApplyPropertyElement(instance, meta, ownerPart, propPart, childEl, currentFe, context);
                    }
                    else
                    {
                        bool attachedEarly = false;
                        Action<object> earlyAttach = null;
                        if (meta.AddChild != null)
                        {
                            earlyAttach = child =>
                            {
                                if (child is UIElement || child is Visual3D)
                                {
                                    meta.AddChild(instance, child);
                                    attachedEarly = true;
                                }
                            };
                        }

                        object childObj = InstantiateElement(childEl, existingInstance: null, currentFe, context, earlyAttach);
                        if (!attachedEarly)
                        {
                            if (meta.AddChild != null)
                            {
                                meta.AddChild(instance, childObj);
                            }
                            else if (meta.ContentProperty != null && instance is DependencyObject doInstance)
                            {
                                doInstance.SetValue(meta.ContentProperty, childObj);
                            }
                        }
                    }
                }
            }

            return instance;
        }

        private static void ApplyAttribute(
            object instance,
            XamlTypeMetadata meta,
            string attrName,
            string rawValue,
            FrameworkElement contextFe,
            XamlLoadContext context)
        {
            // Check Event handlers first
            if (TrySubscribeEvent(instance, meta.ClrType, attrName, rawValue, context))
            {
                return;
            }

            // Check Attached Property: OwnerType.PropertyName
            int dotIdx = attrName.IndexOf('.');
            if (dotIdx > 0 && instance is DependencyObject attachedTarget)
            {
                string ownerName = attrName.Substring(0, dotIdx);
                string propName = attrName.Substring(dotIdx + 1);
                DependencyProperty attachedDp = ResolveDependencyProperty(ownerName, propName);
                if (attachedDp != null)
                {
                    ApplyDependencyPropertyValue(attachedTarget, attachedDp, rawValue, contextFe, context);
                    return;
                }
            }

            // Check DependencyProperty on instance hierarchy
            if (instance is DependencyObject doTarget)
            {
                DependencyProperty dp = ResolveDependencyPropertyForInstance(meta.ClrType, attrName);
                if (dp != null)
                {
                    ApplyDependencyPropertyValue(doTarget, dp, rawValue, contextFe, context);
                    return;
                }
            }

            // Check registered CLR property setter
            if (TryFindClrStringSetter(meta.ClrType, attrName, out Action<object, string, XamlLoadContext> clrSetter))
            {
                clrSetter(instance, rawValue, context);
                return;
            }

            throw new XamlParseException($"Unknown property or event '{attrName}' on type '{meta.ClrType.Name}'.");
        }

        private static void ApplyPropertyElement(
            object instance,
            XamlTypeMetadata meta,
            string ownerName,
            string propName,
            XElement propElement,
            FrameworkElement contextFe,
            XamlLoadContext context)
        {
            if (propName == "Resources" && instance is FrameworkElement feWithResources)
            {
                foreach (XElement child in propElement.Elements())
                {
                    if (child.Name.LocalName == "ResourceDictionary")
                    {
                        PopulateResourceDictionary(child, feWithResources.Resources, feWithResources, context);
                    }
                    else
                    {
                        string key = GetXamlKey(child);
                        object val = IsXamlNonShared(child)
                            ? ResourceDictionary.CreateNonShared(() => InstantiateElement(child, existingInstance: null, feWithResources, context))
                            : InstantiateElement(child, existingInstance: null, feWithResources, context);
                        if (key != null)
                        {
                            feWithResources.Resources[key] = val;
                            context.RegisterResource(key, val);
                        }
                    }
                }
                return;
            }

            if (propName == "Triggers" && instance is FrameworkElement feWithTriggers)
            {
                foreach (XElement child in propElement.Elements())
                {
                    if (InstantiateElement(child, existingInstance: null, contextFe, context) is TriggerBase tb)
                    {
                        feWithTriggers.Triggers.Add(tb);
                    }
                }
                return;
            }

            if (propName == "RowDefinitions" && instance is Grid gridRows)
            {
                foreach (XElement child in propElement.Elements())
                {
                    if (InstantiateElement(child, existingInstance: null, contextFe, context) is RowDefinition rd)
                    {
                        gridRows.RowDefinitions.Add(rd);
                    }
                }
                return;
            }

            if (propName == "ColumnDefinitions" && instance is Grid gridCols)
            {
                foreach (XElement child in propElement.Elements())
                {
                    if (InstantiateElement(child, existingInstance: null, contextFe, context) is ColumnDefinition cd)
                    {
                        gridCols.ColumnDefinitions.Add(cd);
                    }
                }
                return;
            }

            // Check registered CLR object setter (e.g. Viewport3D.Camera, LinearGradientBrush.GradientStops)
            if (TryFindClrObjectSetter(meta.ClrType, propName, out Action<object, object> objSetter))
            {
                foreach (XElement valueEl in propElement.Elements())
                {
                    object valueObj = InstantiateElement(valueEl, existingInstance: null, contextFe, context);
                    objSetter(instance, valueObj);
                }
                return;
            }

            if (instance is DependencyObject doTarget)
            {
                DependencyProperty dp = ResolveDependencyProperty(ownerName, propName)
                                        ?? ResolveDependencyPropertyForInstance(meta.ClrType, propName);
                if (dp != null)
                {
                    foreach (XElement valueEl in propElement.Elements())
                    {
                        string valLocal = valueEl.Name.LocalName;
                        if (valLocal is "Binding" or "MultiBinding" or "PriorityBinding")
                        {
                            BindingBase bindingBase = ParseBindingElement(valueEl, dp.PropertyType, contextFe, context);
                            BindingOperations.SetBinding(doTarget, dp, bindingBase);
                        }
                        else
                        {
                            object valueObj = InstantiateElement(valueEl, existingInstance: null, contextFe, context);
                            doTarget.SetValue(dp, valueObj);
                        }
                    }
                    return;
                }
            }

            throw new XamlParseException($"Unknown property element '{ownerName}.{propName}' on '{meta.ClrType.Name}'.");
        }

        private static void ApplyDependencyPropertyValue(
            DependencyObject target,
            DependencyProperty dp,
            string rawValue,
            FrameworkElement contextFe,
            XamlLoadContext context)
        {
            string trimmed = rawValue.Trim();
            if (trimmed.StartsWith('{') && trimmed.EndsWith('}'))
            {
                string inner = trimmed.Substring(1, trimmed.Length - 2).Trim();
                if (inner.StartsWith("DynamicResource ", StringComparison.Ordinal))
                {
                    string key = inner.Substring("DynamicResource ".Length).Trim();
                    if (target is FrameworkElement fe)
                    {
                        fe.SetResourceReference(dp, key);
                        return;
                    }
                }
                else if (inner.StartsWith("StaticResource ", StringComparison.Ordinal))
                {
                    string key = inner.Substring("StaticResource ".Length).Trim();
                    object found = contextFe?.TryFindResource(key);
                    if (found == null || found == DependencyProperty.UnsetValue)
                    {
                        found = context.TryFindScopedResource(key);
                    }
                    if (found != null && found != DependencyProperty.UnsetValue)
                    {
                        target.SetValue(dp, found);
                    }
                    return;
                }
                else if (inner.Equals("x:Null", StringComparison.Ordinal) || inner.Equals("Null", StringComparison.Ordinal))
                {
                    target.SetValue(dp, null);
                    return;
                }
                else if (inner.StartsWith("x:Reference ", StringComparison.Ordinal) || inner.StartsWith("Reference ", StringComparison.Ordinal))
                {
                    string refName = inner.StartsWith("x:Reference ", StringComparison.Ordinal)
                        ? inner.Substring("x:Reference ".Length).Trim()
                        : inner.Substring("Reference ".Length).Trim();
                    object found = context.NameScope.FindName(refName) ?? contextFe?.FindName(refName);
                    if (found != null)
                    {
                        target.SetValue(dp, found);
                    }
                    return;
                }
                else if (inner.StartsWith("Binding", StringComparison.Ordinal) || inner.StartsWith("x:Bind", StringComparison.Ordinal))
                {
                    Binding parsedBinding = ParseBindingMarkup(inner, dp.PropertyType, contextFe, context);
                    BindingOperations.SetBinding(target, dp, parsedBinding);
                    return;
                }
            }

            object converted = ConvertStringValue(rawValue, dp.PropertyType);
            target.SetValue(dp, converted);
        }

        private static void PopulateResourceDictionary(
            XElement dictElement,
            ResourceDictionary dict,
            FrameworkElement contextFe,
            XamlLoadContext context)
        {
            foreach (XElement itemEl in dictElement.Elements())
            {
                if (itemEl.Name.LocalName == "ResourceDictionary.MergedDictionaries")
                {
                    foreach (XElement mergedEl in itemEl.Elements())
                    {
                        var childDict = new ResourceDictionary();
                        PopulateResourceDictionary(mergedEl, childDict, contextFe, context);
                        dict.MergedDictionaries.Add(childDict);
                    }
                    continue;
                }

                string key = GetXamlKey(itemEl);
                object value = IsXamlNonShared(itemEl)
                    ? ResourceDictionary.CreateNonShared(() => InstantiateElement(itemEl, existingInstance: null, contextFe, context))
                    : InstantiateElement(itemEl, existingInstance: null, contextFe, context);
                if (!string.IsNullOrEmpty(key))
                {
                    dict[key] = value;
                    context.RegisterResource(key, value);
                }
                else if (value is Style styleVal && styleVal.TargetType != null)
                {
                    dict[styleVal.TargetType] = styleVal;
                }
            }
        }

        private static bool IsXamlNonShared(XElement el)
        {
            foreach (XAttribute attr in el.Attributes())
            {
                if (attr.Name.LocalName == "Shared" &&
                    string.Equals(attr.Value?.Trim(), "False", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
            return false;
        }

        private static object ParseArrayElement(XElement arrayEl, FrameworkElement contextFe, XamlLoadContext context)
        {
            string typeAttr = arrayEl.Attribute("Type")?.Value;
            Type elemType = ResolveTypeName(typeAttr);

            var items = new List<object>();
            foreach (XElement child in arrayEl.Elements())
            {
                items.Add(InstantiateElement(child, existingInstance: null, contextFe, context));
            }

            if (elemType == typeof(string))
            {
                var arr = new string[items.Count];
                for (int i = 0; i < items.Count; i++) arr[i] = items[i]?.ToString();
                return arr;
            }
            if (elemType == typeof(int))
            {
                var arr = new int[items.Count];
                for (int i = 0; i < items.Count; i++) arr[i] = items[i] is int v ? v : Convert.ToInt32(items[i], CultureInfo.InvariantCulture);
                return arr;
            }
            if (elemType == typeof(double))
            {
                var arr = new double[items.Count];
                for (int i = 0; i < items.Count; i++) arr[i] = items[i] is double v ? v : Convert.ToDouble(items[i], CultureInfo.InvariantCulture);
                return arr;
            }
            if (elemType == typeof(bool))
            {
                var arr = new bool[items.Count];
                for (int i = 0; i < items.Count; i++) arr[i] = items[i] is bool v ? v : Convert.ToBoolean(items[i], CultureInfo.InvariantCulture);
                return arr;
            }
            if (elemType == typeof(Brush))
            {
                var arr = new Brush[items.Count];
                for (int i = 0; i < items.Count; i++) arr[i] = items[i] as Brush;
                return arr;
            }
            if (elemType == typeof(UIElement))
            {
                var arr = new UIElement[items.Count];
                for (int i = 0; i < items.Count; i++) arr[i] = items[i] as UIElement;
                return arr;
            }
            return items.ToArray();
        }

        private static Style ParseStyleElement(XElement styleEl, FrameworkElement contextFe, XamlLoadContext context)
        {
            string targetTypeName = styleEl.Attribute("TargetType")?.Value;
            Type targetType = ResolveTypeName(targetTypeName) ?? typeof(FrameworkElement);
            Style style = new Style(targetType);

            string basedOnKey = styleEl.Attribute("BasedOn")?.Value;
            if (!string.IsNullOrEmpty(basedOnKey) && basedOnKey.StartsWith("{StaticResource ", StringComparison.Ordinal))
            {
                string key = basedOnKey.Substring("{StaticResource ".Length).TrimEnd('}').Trim();
                if (contextFe?.TryFindResource(key) is Style baseStyle)
                {
                    style.BasedOn = baseStyle;
                }
            }

            foreach (XElement child in styleEl.Elements())
            {
                string local = child.Name.LocalName;
                if (local == "Setter")
                {
                    Setter setter = ParseSetterElement(child, targetType, contextFe, context);
                    if (setter != null)
                    {
                        style.Setters.Add(setter);
                    }
                }
                else if (local == "Style.Setters")
                {
                    foreach (XElement sEl in child.Elements("Setter"))
                    {
                        Setter setter = ParseSetterElement(sEl, targetType, contextFe, context);
                        if (setter != null)
                        {
                            style.Setters.Add(setter);
                        }
                    }
                }
                else if (local == "Style.Triggers")
                {
                    foreach (XElement trigEl in child.Elements())
                    {
                        if (trigEl.Name.LocalName == "Trigger")
                        {
                            string propName = trigEl.Attribute("Property")?.Value;
                            string valAttr = trigEl.Attribute("Value")?.Value;
                            DependencyProperty trigDp = ResolveDependencyPropertyForInstance(targetType, propName);
                            if (trigDp != null)
                            {
                                var trigger = new Trigger
                                {
                                    Property = trigDp,
                                    Value = ConvertStringValue(valAttr, trigDp.PropertyType)
                                };
                                foreach (XElement sEl in trigEl.Elements())
                                {
                                    if (sEl.Name.LocalName == "Setter")
                                    {
                                        Setter s = ParseSetterElement(sEl, targetType, contextFe, context);
                                        if (s != null)
                                        {
                                            trigger.Setters.Add(s);
                                        }
                                    }
                                }
                                style.Triggers.Add(trigger);
                            }
                        }
                    }
                }
            }

            return style;
        }

        private static Setter ParseSetterElement(XElement setterEl, Type targetType, FrameworkElement contextFe, XamlLoadContext context)
        {
            string propAttr = setterEl.Attribute("Property")?.Value;
            string targetName = setterEl.Attribute("TargetName")?.Value;
            if (string.IsNullOrEmpty(propAttr))
            {
                return null;
            }

            DependencyProperty dp = ResolveDependencyPropertyForInstance(targetType, propAttr);
            if (dp == null)
            {
                int dot = propAttr.IndexOf('.');
                if (dot > 0)
                {
                    dp = ResolveDependencyProperty(propAttr.Substring(0, dot), propAttr.Substring(dot + 1));
                }
                dp ??= s_dpByShortName.TryGetValue(propAttr, out DependencyProperty shortDp) ? shortDp : null;
            }
            if (dp == null)
            {
                return null;
            }

            object valObj = null;
            XAttribute valAttr = setterEl.Attribute("Value");
            if (valAttr != null)
            {
                valObj = ConvertStringValue(valAttr.Value, dp.PropertyType);
            }
            else
            {
                foreach (XElement child in setterEl.Elements())
                {
                    if (child.Name.LocalName == "Setter.Value")
                    {
                        foreach (XElement inner in child.Elements())
                        {
                            valObj = InstantiateElement(inner, existingInstance: null, contextFe, context);
                        }
                    }
                    else
                    {
                        valObj = InstantiateElement(child, existingInstance: null, contextFe, context);
                    }
                }
            }

            var setter = new Setter(dp, valObj);
            if (!string.IsNullOrEmpty(targetName))
            {
                setter.TargetName = targetName;
            }
            return setter;
        }

        private static ControlTemplate ParseControlTemplateElement(XElement templateEl, XamlLoadContext context)
        {
            string targetTypeName = templateEl.Attribute("TargetType")?.Value;
            Type targetType = ResolveTypeName(targetTypeName) ?? typeof(Control);
            var controlTemplate = new ControlTemplate(targetType);

            foreach (XElement child in templateEl.Elements())
            {
                if (!child.Name.LocalName.Contains('.'))
                {
                    controlTemplate.VisualTree = BuildTemplateFactoryTree(child, targetType);
                    break;
                }
            }
            return controlTemplate;
        }

        private static FrameworkElementFactory BuildTemplateFactoryTree(XElement el, Type templatedControlType)
        {
            string localName = el.Name.LocalName;
            if (!s_typesByName.TryGetValue(localName, out XamlTypeMetadata meta))
            {
                throw new XamlParseException($"Unregistered template element '{localName}'.");
            }

            string elemName = GetXamlName(el);
            var factory = new FrameworkElementFactory(
                () => (DependencyObject)meta.Factory(),
                meta.ClrType,
                elemName);

            foreach (XAttribute attr in el.Attributes())
            {
                if (attr.IsNamespaceDeclaration) continue;
                string attrName = attr.Name.LocalName;
                if (attrName == "Name") continue;

                DependencyProperty dp = ResolveDependencyPropertyForInstance(meta.ClrType, attrName);
                if (dp == null) continue;

                string raw = attr.Value.Trim();
                if (raw.StartsWith("{TemplateBinding ", StringComparison.Ordinal) && raw.EndsWith('}'))
                {
                    string srcPropName = raw.Substring("{TemplateBinding ".Length, raw.Length - "{TemplateBinding ".Length - 1).Trim();
                    DependencyProperty srcDp = ResolveDependencyPropertyForInstance(templatedControlType, srcPropName)
                                               ?? ResolveDependencyPropertyForInstance(typeof(Control), srcPropName);
                    if (srcDp != null)
                    {
                        factory.SetValue(dp, new TemplateBindingExtension(srcDp));
                    }
                }
                else
                {
                    factory.SetValue(dp, ConvertStringValue(raw, dp.PropertyType));
                }
            }

            foreach (XElement childEl in el.Elements())
            {
                if (!childEl.Name.LocalName.Contains('.'))
                {
                    factory.AppendChild(BuildTemplateFactoryTree(childEl, templatedControlType));
                }
            }

            return factory;
        }

        // =====================================================================
        // Value Conversion & Helpers
        // =====================================================================

        internal static object ConvertStringValue(string raw, Type targetType)
        {
            if (raw == null) return null;
            Type underlying = Nullable.GetUnderlyingType(targetType);
            if (underlying != null)
            {
                if (string.Equals(raw, "{x:Null}", StringComparison.OrdinalIgnoreCase) || string.IsNullOrEmpty(raw))
                    return null;
                return ConvertStringValue(raw, underlying);
            }
            if (targetType == typeof(string) || targetType == typeof(object))
            {
                return raw;
            }
            if (targetType == typeof(double))
            {
                if (string.Equals(raw, "Auto", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(raw, "NaN", StringComparison.OrdinalIgnoreCase))
                {
                    return double.NaN;
                }
                return double.Parse(raw, CultureInfo.InvariantCulture);
            }
            if (targetType == typeof(double?))
            {
                return string.IsNullOrWhiteSpace(raw) ? null : double.Parse(raw, CultureInfo.InvariantCulture);
            }
            if (targetType == typeof(Color?))
            {
                return string.IsNullOrWhiteSpace(raw) ? null : ParseColor(raw);
            }
            if (targetType == typeof(Point?))
            {
                if (string.IsNullOrWhiteSpace(raw)) return null;
                double[] p = ParseDoubleList(raw);
                return new Point(p[0], p[1]);
            }
            if (targetType == typeof(Thickness?))
            {
                return string.IsNullOrWhiteSpace(raw) ? null : ParseThickness(raw);
            }
            if (targetType == typeof(Duration))
            {
                string dTrim = raw.Trim();
                if (string.Equals(dTrim, "Automatic", StringComparison.OrdinalIgnoreCase)) return Duration.Automatic;
                if (string.Equals(dTrim, "Forever", StringComparison.OrdinalIgnoreCase)) return Duration.Forever;
                return new Duration(TimeSpan.Parse(dTrim, CultureInfo.InvariantCulture));
            }
            if (targetType == typeof(TimeSpan) || targetType == typeof(TimeSpan?))
            {
                return string.IsNullOrWhiteSpace(raw) ? null : TimeSpan.Parse(raw.Trim(), CultureInfo.InvariantCulture);
            }
            if (targetType == typeof(System.Windows.Media.Animation.RepeatBehavior))
            {
                string rTrim = raw.Trim();
                if (string.Equals(rTrim, "Forever", StringComparison.OrdinalIgnoreCase)) return System.Windows.Media.Animation.RepeatBehavior.Forever;
                if (rTrim.EndsWith("x", StringComparison.OrdinalIgnoreCase) &&
                    double.TryParse(rTrim.Substring(0, rTrim.Length - 1), NumberStyles.Float, CultureInfo.InvariantCulture, out double rCount))
                {
                    return new System.Windows.Media.Animation.RepeatBehavior(rCount);
                }
                return new System.Windows.Media.Animation.RepeatBehavior(TimeSpan.Parse(rTrim, CultureInfo.InvariantCulture));
            }
            if (targetType == typeof(PropertyPath))
            {
                return new PropertyPath(raw.Trim());
            }
            if (targetType == typeof(int))
            {
                return int.Parse(raw, CultureInfo.InvariantCulture);
            }
            if (targetType == typeof(bool))
            {
                return bool.Parse(raw);
            }
            if (targetType == typeof(char))
            {
                return !string.IsNullOrEmpty(raw) ? raw[0] : '\0';
            }
            if (targetType == typeof(Color))
            {
                return ParseColor(raw);
            }
            if (typeof(Brush).IsAssignableFrom(targetType))
            {
                return new SolidColorBrush(ParseColor(raw));
            }
            if (targetType == typeof(Thickness))
            {
                return ParseThickness(raw);
            }
            if (targetType == typeof(CornerRadius))
            {
                double[] p = ParseDoubleList(raw);
                return p.Length switch
                {
                    1 => new CornerRadius(p[0]),
                    2 => new CornerRadius(p[0], p[1], p[0], p[1]),
                    4 => new CornerRadius(p[0], p[1], p[2], p[3]),
                    _ => new CornerRadius(p[0])
                };
            }
            if (targetType == typeof(System.Windows.Media.Animation.EasingMode))
            {
                return Enum.Parse<System.Windows.Media.Animation.EasingMode>(raw, ignoreCase: true);
            }
            if (targetType == typeof(Stretch))
            {
                return Enum.Parse<Stretch>(raw, ignoreCase: true);
            }
            if (targetType == typeof(StretchDirection))
            {
                return Enum.Parse<StretchDirection>(raw, ignoreCase: true);
            }
            if (targetType == typeof(Point))
            {
                double[] parts = ParseDoubleList(raw);
                return new Point(parts[0], parts.Length > 1 ? parts[1] : 0);
            }
            if (targetType == typeof(Size))
            {
                double[] parts = ParseDoubleList(raw);
                return new Size(parts[0], parts.Length > 1 ? parts[1] : parts[0]);
            }
            if (targetType == typeof(Rect))
            {
                double[] parts = ParseDoubleList(raw);
                return new Rect(parts[0], parts[1], parts[2], parts[3]);
            }
            if (targetType == typeof(Point3D))
            {
                double[] parts = ParseDoubleList(raw);
                return new Point3D(parts[0], parts[1], parts[2]);
            }
            if (targetType == typeof(Vector3D))
            {
                double[] parts = ParseDoubleList(raw);
                return new Vector3D(parts[0], parts[1], parts[2]);
            }
            if (targetType == typeof(Point3DCollection))
            {
                var col = new Point3DCollection();
                double[] vals = ParseDoubleList(raw);
                for (int i = 0; i + 2 < vals.Length; i += 3)
                {
                    col.Add(new Point3D(vals[i], vals[i + 1], vals[i + 2]));
                }
                return col;
            }
            if (targetType == typeof(Vector3DCollection))
            {
                var col = new Vector3DCollection();
                double[] vals = ParseDoubleList(raw);
                for (int i = 0; i + 2 < vals.Length; i += 3)
                {
                    col.Add(new Vector3D(vals[i], vals[i + 1], vals[i + 2]));
                }
                return col;
            }
            if (targetType == typeof(PointCollection))
            {
                var col = new PointCollection();
                double[] vals = ParseDoubleList(raw);
                for (int i = 0; i + 1 < vals.Length; i += 2)
                {
                    col.Add(new Point(vals[i], vals[i + 1]));
                }
                return col;
            }
            if (targetType == typeof(Int32Collection))
            {
                var col = new Int32Collection();
                string[] tokens = raw.Split(new[] { ',', ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                for (int i = 0; i < tokens.Length; i++)
                {
                    col.Add(int.Parse(tokens[i], CultureInfo.InvariantCulture));
                }
                return col;
            }
            if (targetType == typeof(HorizontalAlignment))
            {
                return Enum.Parse<HorizontalAlignment>(raw, ignoreCase: true);
            }
            if (targetType == typeof(VerticalAlignment))
            {
                return Enum.Parse<VerticalAlignment>(raw, ignoreCase: true);
            }
            if (targetType == typeof(Visibility))
            {
                return Enum.Parse<Visibility>(raw, ignoreCase: true);
            }
            if (targetType == typeof(Orientation))
            {
                return Enum.Parse<Orientation>(raw, ignoreCase: true);
            }
            if (targetType == typeof(ClickMode))
            {
                return Enum.Parse<ClickMode>(raw, ignoreCase: true);
            }
            if (targetType == typeof(TickPlacement))
            {
                return Enum.Parse<TickPlacement>(raw, ignoreCase: true);
            }
            if (targetType == typeof(WindowStartupLocation))
            {
                return Enum.Parse<WindowStartupLocation>(raw, ignoreCase: true);
            }
            if (targetType == typeof(WindowStyle))
            {
                return Enum.Parse<WindowStyle>(raw, ignoreCase: true);
            }
            if (targetType == typeof(WindowState))
            {
                return Enum.Parse<WindowState>(raw, ignoreCase: true);
            }
            if (targetType == typeof(ResizeMode))
            {
                return Enum.Parse<ResizeMode>(raw, ignoreCase: true);
            }
            if (targetType == typeof(SizeToContent))
            {
                return Enum.Parse<SizeToContent>(raw, ignoreCase: true);
            }
            if (targetType == typeof(PlacementMode))
            {
                return Enum.Parse<PlacementMode>(raw, ignoreCase: true);
            }
            if (targetType == typeof(PopupAnimation))
            {
                return Enum.Parse<PopupAnimation>(raw, ignoreCase: true);
            }
            if (targetType == typeof(System.Windows.Media.Animation.EasingMode))
            {
                return Enum.Parse<System.Windows.Media.Animation.EasingMode>(raw, ignoreCase: true);
            }
            if (targetType == typeof(Stretch))
            {
                return Enum.Parse<Stretch>(raw, ignoreCase: true);
            }
            if (targetType == typeof(GridLength))
            {
                return GridLength.Parse(raw);
            }
            if (targetType == typeof(Dock))
            {
                return Enum.Parse<Dock>(raw, ignoreCase: true);
            }
            if (targetType == typeof(FontWeight))
            {
                return raw.Equals("Bold", StringComparison.OrdinalIgnoreCase) ||
                       raw.Equals("SemiBold", StringComparison.OrdinalIgnoreCase)
                    ? FontWeights.Bold
                    : FontWeights.Normal;
            }
            if (targetType == typeof(FontFamily))
            {
                return new FontFamily(raw);
            }
            if (targetType == typeof(FontStyle))
            {
                return string.Equals(raw, "Italic", StringComparison.OrdinalIgnoreCase)
                    ? FontStyles.Italic
                    : string.Equals(raw, "Oblique", StringComparison.OrdinalIgnoreCase)
                        ? FontStyles.Oblique
                        : FontStyles.Normal;
            }
            if (targetType == typeof(ScrollBarVisibility))
            {
                return Enum.Parse<ScrollBarVisibility>(raw, ignoreCase: true);
            }
            if (targetType == typeof(TextWrapping))
            {
                return Enum.Parse<TextWrapping>(raw, ignoreCase: true);
            }
            if (targetType == typeof(TextTrimming))
            {
                return Enum.Parse<TextTrimming>(raw, ignoreCase: true);
            }
            if (targetType == typeof(TextAlignment))
            {
                return Enum.Parse<TextAlignment>(raw, ignoreCase: true);
            }

            return raw;
        }

        public static Color ParseColor(string raw)
        {
            string s = raw.Trim();
            if (s.StartsWith('#'))
            {
                string hex = s.Substring(1);
                if (hex.Length == 6)
                {
                    byte r = byte.Parse(hex.Substring(0, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
                    byte g = byte.Parse(hex.Substring(2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
                    byte b = byte.Parse(hex.Substring(4, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
                    return Color.FromRgb(r, g, b);
                }
                if (hex.Length == 8)
                {
                    byte a = byte.Parse(hex.Substring(0, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
                    byte r = byte.Parse(hex.Substring(2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
                    byte g = byte.Parse(hex.Substring(4, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
                    byte b = byte.Parse(hex.Substring(6, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
                    return Color.FromArgb(a, r, g, b);
                }
            }

            return s.ToLowerInvariant() switch
            {
                "white" => Colors.White,
                "black" => Colors.Black,
                "red" => Colors.Red,
                "green" => Colors.Green,
                "blue" => Colors.Blue,
                "yellow" => Colors.Yellow,
                "cyan" => Colors.Cyan,
                "magenta" => Colors.Magenta,
                "orange" => Colors.Orange,
                "gold" => Colors.Gold,
                "transparent" => Colors.Transparent,
                "cornflowerblue" => Colors.CornflowerBlue,
                "deepskyblue" => Colors.DeepSkyBlue,
                "limegreen" => Colors.LimeGreen,
                "hotpink" => Colors.HotPink,
                _ => Colors.White
            };
        }

        public static Thickness ParseThickness(string raw)
        {
            double[] parts = ParseDoubleList(raw);
            return parts.Length switch
            {
                1 => new Thickness(parts[0]),
                2 => new Thickness(parts[0], parts[1], parts[0], parts[1]),
                4 => new Thickness(parts[0], parts[1], parts[2], parts[3]),
                _ => new Thickness(0)
            };
        }

        private static double[] ParseDoubleList(string raw)
        {
            string[] tokens = raw.Split(new[] { ',', ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            double[] result = new double[tokens.Length];
            for (int i = 0; i < tokens.Length; i++)
            {
                result[i] = double.Parse(tokens[i], CultureInfo.InvariantCulture);
            }
            return result;
        }

        internal static string ExtractBindingPath(string innerMarkup)
        {
            string rest = innerMarkup;
            if (rest.StartsWith("Binding", StringComparison.Ordinal))
            {
                rest = rest.Substring("Binding".Length).Trim();
            }
            else if (rest.StartsWith("x:Bind", StringComparison.Ordinal))
            {
                rest = rest.Substring("x:Bind".Length).Trim();
            }

            List<string> clauses = SplitTopLevelClauses(rest);
            for (int i = 0; i < clauses.Count; i++)
            {
                string clause = clauses[i].Trim();
                if (clause.StartsWith("Path=", StringComparison.Ordinal))
                {
                    return clause.Substring("Path=".Length).Trim();
                }
                if (!clause.Contains('='))
                {
                    return clause;
                }
            }
            return string.Empty;
        }

        private static Binding ParseBindingMarkup(
            string innerMarkup,
            Type targetPropertyType,
            FrameworkElement contextFe,
            XamlLoadContext context)
        {
            string rest = innerMarkup;
            if (rest.StartsWith("Binding", StringComparison.Ordinal))
            {
                rest = rest.Substring("Binding".Length).Trim();
            }
            else if (rest.StartsWith("x:Bind", StringComparison.Ordinal))
            {
                rest = rest.Substring("x:Bind".Length).Trim();
            }

            string path = string.Empty;
            BindingMode? explicitMode = null;
            string elementName = null;
            RelativeSource relativeSource = null;
            IValueConverter converter = null;
            object converterParameter = null;
            string stringFormat = null;
            object fallbackValue = null;
            object targetNullValue = null;
            UpdateSourceTrigger? trigger = null;
            int delay = 0;
            bool validatesOnErrors = false;
            bool validatesOnNotify = true;
            bool validatesOnExceptions = false;

            List<string> clauses = SplitTopLevelClauses(rest);
            for (int i = 0; i < clauses.Count; i++)
            {
                string clause = clauses[i].Trim();
                int eq = clause.IndexOf('=');
                if (eq < 0)
                {
                    path = clause;
                    continue;
                }

                string key = clause.Substring(0, eq).Trim();
                string val = clause.Substring(eq + 1).Trim().Trim('\'');

                switch (key)
                {
                    case "Path":
                        path = val;
                        break;
                    case "Mode":
                        explicitMode = Enum.Parse<BindingMode>(val, ignoreCase: true);
                        break;
                    case "ElementName":
                        elementName = val;
                        break;
                    case "RelativeSource":
                        relativeSource = ParseRelativeSourceMarkup(val);
                        break;
                    case "Converter":
                        if (ResolveStaticResourceFromMarkup(val, contextFe, context) is IValueConverter conv)
                        {
                            converter = conv;
                        }
                        break;
                    case "ConverterParameter":
                        converterParameter = val;
                        break;
                    case "StringFormat":
                        stringFormat = val;
                        break;
                    case "FallbackValue":
                        fallbackValue = ConvertStringValue(val, targetPropertyType ?? typeof(object));
                        break;
                    case "TargetNullValue":
                        targetNullValue = ConvertStringValue(val, targetPropertyType ?? typeof(object));
                        break;
                    case "UpdateSourceTrigger":
                        trigger = Enum.Parse<UpdateSourceTrigger>(val, ignoreCase: true);
                        break;
                    case "Delay":
                        delay = int.Parse(val, CultureInfo.InvariantCulture);
                        break;
                    case "ValidatesOnDataErrors":
                        validatesOnErrors = bool.Parse(val);
                        break;
                    case "ValidatesOnNotifyDataErrors":
                        validatesOnNotify = bool.Parse(val);
                        break;
                    case "ValidatesOnExceptions":
                        validatesOnExceptions = bool.Parse(val);
                        break;
                }
            }

            if (!string.IsNullOrEmpty(path) &&
                string.IsNullOrEmpty(elementName) &&
                relativeSource == null &&
                context.TryCreateBinding(path, out Binding registeredBinding))
            {
                if (explicitMode.HasValue) registeredBinding.Mode = explicitMode.Value;
                if (converter != null)
                {
                    registeredBinding.Converter = converter;
                    registeredBinding.ConverterParameter = converterParameter;
                }
                if (!string.IsNullOrEmpty(stringFormat)) registeredBinding.StringFormat = stringFormat;
                if (fallbackValue != null) registeredBinding.FallbackValue = fallbackValue;
                if (targetNullValue != null) registeredBinding.TargetNullValue = targetNullValue;
                if (trigger.HasValue) registeredBinding.UpdateSourceTrigger = trigger.Value;
                if (delay > 0) registeredBinding.Delay = delay;
                registeredBinding.ValidatesOnDataErrors = validatesOnErrors;
                registeredBinding.ValidatesOnNotifyDataErrors = validatesOnNotify;
                registeredBinding.ValidatesOnExceptions = validatesOnExceptions;
                return registeredBinding;
            }

            object explicitSource = (string.IsNullOrEmpty(elementName) && relativeSource == null)
                ? context.DataContext
                : null;

            return FluentElementExtensions.CreateConfiguredBinding(
                path,
                explicitMode ?? BindingMode.Default,
                explicitSource,
                elementName,
                relativeSource,
                converter,
                converterParameter,
                stringFormat,
                fallbackValue,
                targetNullValue,
                trigger ?? UpdateSourceTrigger.Default,
                delay,
                validatesOnErrors,
                validatesOnNotify,
                validatesOnExceptions);
        }

        private static BindingBase ParseBindingElement(
            XElement bindingEl,
            Type targetPropertyType,
            FrameworkElement contextFe,
            XamlLoadContext context)
        {
            string local = bindingEl.Name.LocalName;
            if (local == "MultiBinding")
            {
                var multi = new MultiBinding();
                string convAttr = bindingEl.Attribute("Converter")?.Value;
                if (!string.IsNullOrEmpty(convAttr) &&
                    ResolveStaticResourceFromMarkup(convAttr, contextFe, context) is IMultiValueConverter mvc)
                {
                    multi.Converter = mvc;
                }
                string fmtAttr = bindingEl.Attribute("StringFormat")?.Value;
                if (!string.IsNullOrEmpty(fmtAttr))
                {
                    multi.StringFormat = fmtAttr;
                }
                string fbAttr = bindingEl.Attribute("FallbackValue")?.Value;
                if (!string.IsNullOrEmpty(fbAttr))
                {
                    multi.FallbackValue = ConvertStringValue(fbAttr, targetPropertyType ?? typeof(object));
                }

                foreach (XElement child in bindingEl.Elements())
                {
                    if (child.Name.LocalName == "Binding" &&
                        ParseBindingElement(child, typeof(object), contextFe, context) is Binding childBinding)
                    {
                        multi.Bindings.Add(childBinding);
                    }
                }
                return multi;
            }

            if (local == "PriorityBinding")
            {
                var priority = new PriorityBinding();
                foreach (XElement child in bindingEl.Elements())
                {
                    if (child.Name.LocalName == "Binding" &&
                        ParseBindingElement(child, targetPropertyType, contextFe, context) is Binding childBinding)
                    {
                        priority.Bindings.Add(childBinding);
                    }
                }
                return priority;
            }

            // <Binding Path="..." Mode="..." ... />
            var clauses = new List<string>();
            foreach (XAttribute attr in bindingEl.Attributes())
            {
                if (!attr.IsNamespaceDeclaration)
                {
                    clauses.Add($"{attr.Name.LocalName}={attr.Value}");
                }
            }
            return ParseBindingMarkup("Binding " + string.Join(", ", clauses), targetPropertyType, contextFe, context);
        }

        private static RelativeSource ParseRelativeSourceMarkup(string raw)
        {
            string s = raw.Trim();
            if (s.StartsWith('{') && s.EndsWith('}'))
            {
                s = s.Substring(1, s.Length - 2).Trim();
            }
            if (s.StartsWith("RelativeSource", StringComparison.Ordinal))
            {
                s = s.Substring("RelativeSource".Length).Trim();
            }

            RelativeSourceMode mode = RelativeSourceMode.Self;
            Type ancestorType = null;
            int ancestorLevel = 1;

            List<string> clauses = SplitTopLevelClauses(s);
            for (int i = 0; i < clauses.Count; i++)
            {
                string c = clauses[i].Trim();
                int eq = c.IndexOf('=');
                if (eq < 0)
                {
                    mode = Enum.Parse<RelativeSourceMode>(c, ignoreCase: true);
                    continue;
                }
                string k = c.Substring(0, eq).Trim();
                string v = c.Substring(eq + 1).Trim();
                if (k == "Mode")
                {
                    mode = Enum.Parse<RelativeSourceMode>(v, ignoreCase: true);
                }
                else if (k == "AncestorType")
                {
                    ancestorType = ResolveTypeName(v);
                    mode = RelativeSourceMode.FindAncestor;
                }
                else if (k == "AncestorLevel")
                {
                    ancestorLevel = int.Parse(v, CultureInfo.InvariantCulture);
                }
            }

            return mode switch
            {
                RelativeSourceMode.Self => RelativeSource.Self,
                RelativeSourceMode.TemplatedParent => RelativeSource.TemplatedParent,
                RelativeSourceMode.PreviousData => RelativeSource.PreviousData,
                RelativeSourceMode.FindAncestor => new RelativeSource(RelativeSourceMode.FindAncestor, ancestorType ?? typeof(FrameworkElement), ancestorLevel),
                _ => RelativeSource.Self
            };
        }

        private static object ResolveStaticResourceFromMarkup(string raw, FrameworkElement contextFe, XamlLoadContext context)
        {
            string s = raw.Trim();
            if (s.StartsWith('{') && s.EndsWith('}'))
            {
                s = s.Substring(1, s.Length - 2).Trim();
            }
            if (s.StartsWith("StaticResource ", StringComparison.Ordinal))
            {
                s = s.Substring("StaticResource ".Length).Trim();
            }
            object found = contextFe?.TryFindResource(s);
            if (found == null || found == DependencyProperty.UnsetValue)
            {
                found = context.TryFindScopedResource(s);
            }
            return found;
        }

        private static List<string> SplitTopLevelClauses(string input)
        {
            var list = new List<string>();
            if (string.IsNullOrEmpty(input)) return list;

            int depth = 0;
            bool inSingleQuotes = false;
            int start = 0;
            for (int i = 0; i < input.Length; i++)
            {
                char ch = input[i];
                if (ch == '\'')
                {
                    inSingleQuotes = !inSingleQuotes;
                }
                else if (!inSingleQuotes)
                {
                    if (ch == '{') depth++;
                    else if (ch == '}' && depth > 0) depth--;
                    else if (ch == ',' && depth == 0)
                    {
                        list.Add(input.Substring(start, i - start));
                        start = i + 1;
                    }
                }
            }
            if (start < input.Length)
            {
                list.Add(input.Substring(start));
            }
            return list;
        }

        internal static string GetXamlKey(XElement el)
        {
            foreach (XAttribute attr in el.Attributes())
            {
                if (!attr.IsNamespaceDeclaration && attr.Name.LocalName == "Key")
                {
                    return attr.Value;
                }
            }
            return null;
        }

        internal static string GetXamlName(XElement el)
        {
            foreach (XAttribute attr in el.Attributes())
            {
                if (!attr.IsNamespaceDeclaration && attr.Name.LocalName == "Name")
                {
                    return attr.Value;
                }
            }
            return null;
        }

        internal static Type ResolveTypeName(string rawTypeName)
        {
            if (string.IsNullOrEmpty(rawTypeName)) return null;
            string clean = rawTypeName.Trim();
            if (clean.StartsWith("{x:Type ", StringComparison.Ordinal) && clean.EndsWith('}'))
            {
                clean = clean.Substring("{x:Type ".Length, clean.Length - "{x:Type ".Length - 1).Trim();
            }
            int colon = clean.IndexOf(':');
            if (colon >= 0)
            {
                clean = clean.Substring(colon + 1);
            }
            if (s_typesByName.TryGetValue(clean, out XamlTypeMetadata meta))
            {
                return meta.ClrType;
            }
            return clean switch
            {
                "String" or "string" => typeof(string),
                "Int32" or "int" => typeof(int),
                "Double" or "double" => typeof(double),
                "Boolean" or "bool" => typeof(bool),
                "Object" or "object" => typeof(object),
                _ => null,
            };
        }

        internal static DependencyProperty ResolveDependencyProperty(string ownerTypeName, string propName)
        {
            if (s_typesByName.TryGetValue(ownerTypeName, out XamlTypeMetadata ownerMeta))
            {
                return ResolveDependencyPropertyForInstance(ownerMeta.ClrType, propName);
            }
            return s_dpByShortName.TryGetValue(propName, out DependencyProperty dp) ? dp : null;
        }

        internal static DependencyProperty ResolveDependencyPropertyForInstance(Type type, string propName)
        {
            if (string.IsNullOrEmpty(propName)) return null;
            int dot = propName.IndexOf('.');
            if (dot > 0)
            {
                return ResolveDependencyProperty(propName.Substring(0, dot), propName.Substring(dot + 1));
            }

            Type cur = type;
            while (cur != null)
            {
                if (s_dpByOwnerAndName.TryGetValue((cur, propName), out DependencyProperty dp))
                {
                    return dp;
                }
                cur = cur.BaseType;
            }
            return null;
        }

        private static bool TryFindClrStringSetter(Type type, string propName, out Action<object, string, XamlLoadContext> setter)
        {
            Type cur = type;
            while (cur != null)
            {
                if (s_typesByClr.TryGetValue(cur, out XamlTypeMetadata meta) &&
                    meta.ClrPropertySetters.TryGetValue(propName, out setter))
                {
                    return true;
                }
                cur = cur.BaseType;
            }
            setter = null;
            return false;
        }

        private static bool TryFindClrObjectSetter(Type type, string propName, out Action<object, object> setter)
        {
            Type cur = type;
            while (cur != null)
            {
                if (s_typesByClr.TryGetValue(cur, out XamlTypeMetadata meta) &&
                    meta.ClrObjectPropertySetters.TryGetValue(propName, out setter))
                {
                    return true;
                }
                cur = cur.BaseType;
            }
            setter = null;
            return false;
        }

        private static bool TrySubscribeEvent(object instance, Type type, string eventName, string handlerName, XamlLoadContext context)
        {
            if (!context.TryGetEventHandler(handlerName, out Delegate handler))
            {
                return false;
            }

            Type cur = type;
            while (cur != null)
            {
                if (s_typesByClr.TryGetValue(cur, out XamlTypeMetadata meta) &&
                    meta.EventSubscribers.TryGetValue(eventName, out Action<object, Delegate> sub))
                {
                    sub(instance, handler);
                    return true;
                }
                cur = cur.BaseType;
            }
            return false;
        }

        // =====================================================================
        // Built-In CSUI.Core Type & DependencyProperty Registration
        // =====================================================================

        private static void RegisterCoreTypesAndProperties()
        {
            // 1. Base Elements & Controls
            RegisterType<UIElement>();
            RegisterType<FrameworkElement>();
            RegisterType<Control>();
            RegisterType<ContentControl>(contentProperty: ContentControl.ContentProperty);
            RegisterType<HeaderedContentControl>(contentProperty: ContentControl.ContentProperty);
            RegisterType<ItemsControl>();
            RegisterType<HeaderedItemsControl>();
            RegisterType<ContentPresenter>(contentProperty: ContentPresenter.ContentProperty);
            RegisterType<ResourceDictionary>();

            // UIElement properties & events
            RegisterProperty(typeof(UIElement), nameof(UIElement.Opacity), UIElement.OpacityProperty);
            RegisterProperty(typeof(UIElement), nameof(UIElement.Visibility), UIElement.VisibilityProperty);
            RegisterProperty(typeof(UIElement), nameof(UIElement.IsEnabled), UIElement.IsEnabledProperty);
            RegisterProperty(typeof(UIElement), nameof(UIElement.Clip), UIElement.ClipProperty);
            RegisterProperty(typeof(UIElement), nameof(UIElement.RenderTransform), UIElement.RenderTransformProperty);
            RegisterProperty(typeof(UIElement), nameof(UIElement.RenderTransformOrigin), UIElement.RenderTransformOriginProperty);
            RegisterProperty(typeof(UIElement), nameof(UIElement.IsMouseOver), UIElement.IsMouseOverProperty);

            RegisterEvent<UIElement>(nameof(UIElement.MouseLeftButtonDown), static (el, d) => el.MouseLeftButtonDown += (MouseButtonEventHandler)d);
            RegisterEvent<UIElement>(nameof(UIElement.MouseLeftButtonUp), static (el, d) => el.MouseLeftButtonUp += (MouseButtonEventHandler)d);
            RegisterEvent<UIElement>(nameof(UIElement.MouseMove), static (el, d) => el.MouseMove += (MouseEventHandler)d);

            // FrameworkElement properties & events
            RegisterProperty(typeof(FrameworkElement), nameof(FrameworkElement.Width), FrameworkElement.WidthProperty);
            RegisterProperty(typeof(FrameworkElement), nameof(FrameworkElement.Height), FrameworkElement.HeightProperty);
            RegisterProperty(typeof(FrameworkElement), nameof(FrameworkElement.MinWidth), FrameworkElement.MinWidthProperty);
            RegisterProperty(typeof(FrameworkElement), nameof(FrameworkElement.MinHeight), FrameworkElement.MinHeightProperty);
            RegisterProperty(typeof(FrameworkElement), nameof(FrameworkElement.MaxWidth), FrameworkElement.MaxWidthProperty);
            RegisterProperty(typeof(FrameworkElement), nameof(FrameworkElement.MaxHeight), FrameworkElement.MaxHeightProperty);
            RegisterProperty(typeof(FrameworkElement), nameof(FrameworkElement.Margin), FrameworkElement.MarginProperty);
            RegisterProperty(typeof(FrameworkElement), nameof(FrameworkElement.HorizontalAlignment), FrameworkElement.HorizontalAlignmentProperty);
            RegisterProperty(typeof(FrameworkElement), nameof(FrameworkElement.VerticalAlignment), FrameworkElement.VerticalAlignmentProperty);
            RegisterProperty(typeof(FrameworkElement), nameof(FrameworkElement.Style), FrameworkElement.StyleProperty);
            RegisterProperty(typeof(FrameworkElement), nameof(FrameworkElement.Tag), FrameworkElement.TagProperty);
            RegisterEvent<FrameworkElement>(nameof(FrameworkElement.Loaded), static (el, d) => el.Loaded += (RoutedEventHandler)d);

            // Control properties
            RegisterProperty(typeof(Control), nameof(Control.Background), Control.BackgroundProperty);
            RegisterProperty(typeof(Control), nameof(Control.Foreground), Control.ForegroundProperty);
            RegisterProperty(typeof(Control), nameof(Control.BorderBrush), Control.BorderBrushProperty);
            RegisterProperty(typeof(Control), nameof(Control.BorderThickness), Control.BorderThicknessProperty);
            RegisterProperty(typeof(Control), nameof(Control.Padding), Control.PaddingProperty);
            RegisterProperty(typeof(Control), nameof(Control.FontSize), Control.FontSizeProperty);
            RegisterProperty(typeof(Control), nameof(Control.FontWeight), Control.FontWeightProperty);
            RegisterProperty(typeof(Control), nameof(Control.FontFamily), Control.FontFamilyProperty);
            RegisterProperty(typeof(Control), nameof(Control.Template), Control.TemplateProperty);
            RegisterProperty(typeof(Control), nameof(Control.HorizontalContentAlignment), Control.HorizontalContentAlignmentProperty);
            RegisterProperty(typeof(Control), nameof(Control.VerticalContentAlignment), Control.VerticalContentAlignmentProperty);

            // ContentControl / HeaderedContentControl / ContentPresenter
            RegisterProperty(typeof(ContentControl), nameof(ContentControl.Content), ContentControl.ContentProperty);
            RegisterProperty(typeof(HeaderedContentControl), nameof(HeaderedContentControl.Header), HeaderedContentControl.HeaderProperty);
            RegisterProperty(typeof(ContentPresenter), nameof(ContentPresenter.Content), ContentPresenter.ContentProperty);
            RegisterProperty(typeof(FrameworkElement), nameof(FrameworkElement.ToolTip), FrameworkElement.ToolTipProperty);
            RegisterProperty(typeof(ToolTipService), "ToolTip", ToolTipService.ToolTipProperty);

            // Window, Popup & ToolTip
            RegisterType<Window>(contentProperty: ContentControl.ContentProperty);
            RegisterProperty(typeof(Window), nameof(Window.Title), Window.TitleProperty);
            RegisterProperty(typeof(Window), nameof(Window.WindowStyle), Window.WindowStyleProperty);
            RegisterProperty(typeof(Window), nameof(Window.WindowState), Window.WindowStateProperty);
            RegisterProperty(typeof(Window), nameof(Window.ResizeMode), Window.ResizeModeProperty);
            RegisterProperty(typeof(Window), nameof(Window.SizeToContent), Window.SizeToContentProperty);
            RegisterProperty(typeof(Window), nameof(Window.Topmost), Window.TopmostProperty);
            RegisterProperty(typeof(Window), nameof(Window.ShowInTaskbar), Window.ShowInTaskbarProperty);
            RegisterProperty(typeof(Window), nameof(Window.AllowsTransparency), Window.AllowsTransparencyProperty);
            RegisterProperty(typeof(Window), nameof(Window.Left), Window.LeftProperty);
            RegisterProperty(typeof(Window), nameof(Window.Top), Window.TopProperty);
            RegisterClrProperty<Window, WindowStartupLocation>(
                nameof(Window.WindowStartupLocation),
                static (w, v) => w.WindowStartupLocation = v,
                static w => w.WindowStartupLocation);
            RegisterEvent<Window>(nameof(Window.SourceInitialized), static (w, d) => w.SourceInitialized += (EventHandler)d);
            RegisterEvent<Window>(nameof(Window.ContentRendered), static (w, d) => w.ContentRendered += (EventHandler)d);
            RegisterEvent<Window>(nameof(Window.Activated), static (w, d) => w.Activated += (EventHandler)d);
            RegisterEvent<Window>(nameof(Window.Deactivated), static (w, d) => w.Deactivated += (EventHandler)d);
            RegisterEvent<Window>(nameof(Window.Closing), static (w, d) => w.Closing += (System.ComponentModel.CancelEventHandler)d);
            RegisterEvent<Window>(nameof(Window.Closed), static (w, d) => w.Closed += (EventHandler)d);

            RegisterType<Popup>(contentProperty: Popup.ChildProperty, addChild: static (p, child) =>
            {
                if (child is UIElement uie) p.Child = uie;
            });
            RegisterProperty(typeof(Popup), nameof(Popup.Child), Popup.ChildProperty);
            RegisterProperty(typeof(Popup), nameof(Popup.IsOpen), Popup.IsOpenProperty);
            RegisterProperty(typeof(Popup), nameof(Popup.Placement), Popup.PlacementProperty);
            RegisterProperty(typeof(Popup), nameof(Popup.PlacementTarget), Popup.PlacementTargetProperty);
            RegisterProperty(typeof(Popup), nameof(Popup.HorizontalOffset), Popup.HorizontalOffsetProperty);
            RegisterProperty(typeof(Popup), nameof(Popup.VerticalOffset), Popup.VerticalOffsetProperty);
            RegisterProperty(typeof(Popup), nameof(Popup.StaysOpen), Popup.StaysOpenProperty);
            RegisterProperty(typeof(Popup), nameof(Popup.AllowsTransparency), Popup.AllowsTransparencyProperty);
            RegisterProperty(typeof(Popup), nameof(Popup.PopupAnimation), Popup.PopupAnimationProperty);
            RegisterEvent<Popup>(nameof(Popup.Opened), static (p, d) => p.Opened += (EventHandler)d);
            RegisterEvent<Popup>(nameof(Popup.Closed), static (p, d) => p.Closed += (EventHandler)d);

            RegisterType<ToolTip>(contentProperty: ContentControl.ContentProperty);
            RegisterProperty(typeof(ToolTip), nameof(ToolTip.IsOpen), ToolTip.IsOpenProperty);
            RegisterProperty(typeof(ToolTip), nameof(ToolTip.Placement), ToolTip.PlacementProperty);
            RegisterProperty(typeof(ToolTip), nameof(ToolTip.PlacementTarget), ToolTip.PlacementTargetProperty);
            RegisterProperty(typeof(ToolTip), nameof(ToolTip.HorizontalOffset), ToolTip.HorizontalOffsetProperty);
            RegisterProperty(typeof(ToolTip), nameof(ToolTip.VerticalOffset), ToolTip.VerticalOffsetProperty);
            RegisterProperty(typeof(ToolTip), nameof(ToolTip.StaysOpen), ToolTip.StaysOpenProperty);
            RegisterEvent<ToolTip>(nameof(ToolTip.Opened), static (t, d) => t.Opened += (RoutedEventHandler)d);
            RegisterEvent<ToolTip>(nameof(ToolTip.Closed), static (t, d) => t.Closed += (RoutedEventHandler)d);

            // SvgViewbox & SvgImage
            RegisterType<SvgViewbox>(contentProperty: null);
            RegisterProperty(typeof(SvgViewbox), nameof(SvgViewbox.Source), SvgViewbox.SourceProperty);
            RegisterProperty(typeof(SvgViewbox), nameof(SvgViewbox.Stretch), SvgViewbox.StretchProperty);
            RegisterProperty(typeof(SvgViewbox), nameof(SvgViewbox.Foreground), SvgViewbox.ForegroundProperty);

            RegisterType<SvgImage>();
            RegisterProperty(typeof(SvgImage), nameof(SvgImage.Source), SvgImage.SourceProperty);

            // Modern Direct2D Effects & Materials
            RegisterType<ModernElevationEffect>();
            RegisterProperty(typeof(ModernElevationEffect), nameof(ModernElevationEffect.Elevation), ModernElevationEffect.ElevationProperty);
            RegisterProperty(typeof(ModernElevationEffect), nameof(ModernElevationEffect.BlurRadius), ModernElevationEffect.BlurRadiusProperty);
            RegisterProperty(typeof(ModernElevationEffect), nameof(ModernElevationEffect.ShadowDepth), ModernElevationEffect.ShadowDepthProperty);
            RegisterProperty(typeof(ModernElevationEffect), nameof(ModernElevationEffect.Color), ModernElevationEffect.ColorProperty);
            RegisterProperty(typeof(ModernElevationEffect), nameof(ModernElevationEffect.Opacity), ModernElevationEffect.OpacityProperty);

            RegisterType<BlurEffect>();
            RegisterProperty(typeof(BlurEffect), nameof(BlurEffect.Radius), BlurEffect.RadiusProperty);

            RegisterType<DropShadowEffect>();
            RegisterProperty(typeof(DropShadowEffect), nameof(DropShadowEffect.BlurRadius), DropShadowEffect.BlurRadiusProperty);
            RegisterProperty(typeof(DropShadowEffect), nameof(DropShadowEffect.Color), DropShadowEffect.ColorProperty);
            RegisterProperty(typeof(DropShadowEffect), nameof(DropShadowEffect.Direction), DropShadowEffect.DirectionProperty);
            RegisterProperty(typeof(DropShadowEffect), nameof(DropShadowEffect.ShadowDepth), DropShadowEffect.ShadowDepthProperty);
            RegisterProperty(typeof(DropShadowEffect), nameof(DropShadowEffect.Opacity), DropShadowEffect.OpacityProperty);

            RegisterType<AcrylicBrush>();
            RegisterProperty(typeof(AcrylicBrush), nameof(AcrylicBrush.TintColor), AcrylicBrush.TintColorProperty);
            RegisterProperty(typeof(AcrylicBrush), nameof(AcrylicBrush.TintOpacity), AcrylicBrush.TintOpacityProperty);
            RegisterProperty(typeof(AcrylicBrush), nameof(AcrylicBrush.BlurRadius), AcrylicBrush.BlurRadiusProperty);
            RegisterProperty(typeof(AcrylicBrush), nameof(AcrylicBrush.FallbackColor), AcrylicBrush.FallbackColorProperty);
            RegisterProperty(typeof(UIElement), nameof(UIElement.Effect), UIElement.EffectProperty);

            // 1B. Standard Layout Panels: Grid, StackPanel, DockPanel, WrapPanel
            RegisterType<Grid>(addChild: static (g, child) =>
            {
                if (child is UIElement uie) g.Children.Add(uie);
                else if (child is RowDefinition rd) g.RowDefinitions.Add(rd);
                else if (child is ColumnDefinition cd) g.ColumnDefinitions.Add(cd);
            });
            RegisterProperty(typeof(Grid), nameof(Grid.Rows), Grid.RowsProperty);
            RegisterProperty(typeof(Grid), nameof(Grid.Columns), Grid.ColumnsProperty);
            RegisterProperty(typeof(Grid), "Row", Grid.RowProperty);
            RegisterProperty(typeof(Grid), "Column", Grid.ColumnProperty);
            RegisterProperty(typeof(Grid), "RowSpan", Grid.RowSpanProperty);
            RegisterProperty(typeof(Grid), "ColumnSpan", Grid.ColumnSpanProperty);

            RegisterType<RowDefinition>();
            RegisterProperty(typeof(RowDefinition), nameof(RowDefinition.Height), RowDefinition.HeightProperty);
            RegisterProperty(typeof(RowDefinition), nameof(RowDefinition.MinHeight), RowDefinition.MinHeightProperty);
            RegisterProperty(typeof(RowDefinition), nameof(RowDefinition.MaxHeight), RowDefinition.MaxHeightProperty);

            RegisterType<ColumnDefinition>();
            RegisterProperty(typeof(ColumnDefinition), nameof(ColumnDefinition.Width), ColumnDefinition.WidthProperty);
            RegisterProperty(typeof(ColumnDefinition), nameof(ColumnDefinition.MinWidth), ColumnDefinition.MinWidthProperty);
            RegisterProperty(typeof(ColumnDefinition), nameof(ColumnDefinition.MaxWidth), ColumnDefinition.MaxWidthProperty);

            RegisterType<StackPanel>(addChild: static (sp, child) =>
            {
                if (child is UIElement uie) sp.Children.Add(uie);
            });
            RegisterProperty(typeof(StackPanel), nameof(StackPanel.Orientation), StackPanel.OrientationProperty);
            RegisterProperty(typeof(StackPanel), nameof(StackPanel.Spacing), StackPanel.SpacingProperty);

            RegisterType<DockPanel>(addChild: static (dp, child) =>
            {
                if (child is UIElement uie) dp.Children.Add(uie);
            });
            RegisterProperty(typeof(DockPanel), nameof(DockPanel.LastChildFill), DockPanel.LastChildFillProperty);
            RegisterProperty(typeof(DockPanel), "Dock", DockPanel.DockProperty);

            RegisterType<WrapPanel>(addChild: static (wp, child) =>
            {
                if (child is UIElement uie) wp.Children.Add(uie);
            });
            RegisterProperty(typeof(WrapPanel), nameof(WrapPanel.Orientation), WrapPanel.OrientationProperty);
            RegisterProperty(typeof(WrapPanel), nameof(WrapPanel.ItemWidth), WrapPanel.ItemWidthProperty);
            RegisterProperty(typeof(WrapPanel), nameof(WrapPanel.ItemHeight), WrapPanel.ItemHeightProperty);

            // Canvas & UniformGrid Panels
            RegisterType<Canvas>(addChild: static (c, child) =>
            {
                if (child is UIElement uie) c.Children.Add(uie);
            });
            RegisterProperty(typeof(Canvas), "Left", Canvas.LeftProperty);
            RegisterProperty(typeof(Canvas), "Top", Canvas.TopProperty);
            RegisterProperty(typeof(Canvas), "Right", Canvas.RightProperty);
            RegisterProperty(typeof(Canvas), "Bottom", Canvas.BottomProperty);

            RegisterType<UniformGrid>(addChild: static (ug, child) =>
            {
                if (child is UIElement uie) ug.Children.Add(uie);
            });
            RegisterProperty(typeof(UniformGrid), nameof(UniformGrid.Rows), UniformGrid.RowsProperty);
            RegisterProperty(typeof(UniformGrid), nameof(UniformGrid.Columns), UniformGrid.ColumnsProperty);
            RegisterProperty(typeof(UniformGrid), nameof(UniformGrid.FirstColumn), UniformGrid.FirstColumnProperty);

            // Border & Viewbox Decorators
            RegisterType<Border>(addChild: static (b, child) =>
            {
                if (child is UIElement uie) b.Child = uie;
            });
            RegisterProperty(typeof(Border), nameof(Border.BorderThickness), Border.BorderThicknessProperty);
            RegisterProperty(typeof(Border), nameof(Border.Padding), Border.PaddingProperty);
            RegisterProperty(typeof(Border), nameof(Border.CornerRadius), Border.CornerRadiusProperty);
            RegisterProperty(typeof(Border), nameof(Border.BorderBrush), Border.BorderBrushProperty);
            RegisterProperty(typeof(Border), nameof(Border.Background), Border.BackgroundProperty);

            RegisterType<Viewbox>(addChild: static (vb, child) =>
            {
                if (child is UIElement uie) vb.Child = uie;
            });
            RegisterProperty(typeof(Viewbox), nameof(Viewbox.Stretch), Viewbox.StretchProperty);
            RegisterProperty(typeof(Viewbox), nameof(Viewbox.StretchDirection), Viewbox.StretchDirectionProperty);

            // Scroll & Viewport matrix
            RegisterType<ScrollViewer>(contentProperty: ContentControl.ContentProperty);
            RegisterProperty(typeof(ScrollViewer), nameof(ScrollViewer.HorizontalScrollBarVisibility), ScrollViewer.HorizontalScrollBarVisibilityProperty);
            RegisterProperty(typeof(ScrollViewer), nameof(ScrollViewer.VerticalScrollBarVisibility), ScrollViewer.VerticalScrollBarVisibilityProperty);
            RegisterProperty(typeof(ScrollViewer), nameof(ScrollViewer.CanContentScroll), ScrollViewer.CanContentScrollProperty);
            RegisterProperty(typeof(ScrollViewer), nameof(ScrollViewer.IsSmoothScrollingEnabled), ScrollViewer.IsSmoothScrollingEnabledProperty);
            RegisterProperty(typeof(ScrollViewer), nameof(ScrollViewer.Friction), ScrollViewer.FrictionProperty);

            RegisterType<ScrollBar>();
            RegisterProperty(typeof(ScrollBar), nameof(ScrollBar.Orientation), ScrollBar.OrientationProperty);
            RegisterProperty(typeof(ScrollBar), nameof(ScrollBar.Value), ScrollBar.ValueProperty);
            RegisterProperty(typeof(ScrollBar), nameof(ScrollBar.Minimum), ScrollBar.MinimumProperty);
            RegisterProperty(typeof(ScrollBar), nameof(ScrollBar.Maximum), ScrollBar.MaximumProperty);
            RegisterProperty(typeof(ScrollBar), nameof(ScrollBar.ViewportSize), ScrollBar.ViewportSizeProperty);

            RegisterType<Thumb>();

            RegisterType<VirtualizingStackPanel>(addChild: static (vsp, child) =>
            {
                if (child is UIElement uie) vsp.Children.Add(uie);
            });
            RegisterProperty(typeof(VirtualizingStackPanel), nameof(VirtualizingStackPanel.Orientation), VirtualizingStackPanel.OrientationProperty);
            RegisterProperty(typeof(VirtualizingStackPanel), nameof(VirtualizingStackPanel.Spacing), VirtualizingStackPanel.SpacingProperty);

            // High performance DirectWrite Text matrix
            RegisterType<TextBlock>(contentProperty: TextBlock.TextProperty);
            RegisterProperty(typeof(TextBlock), nameof(TextBlock.Text), TextBlock.TextProperty);
            RegisterProperty(typeof(TextBlock), nameof(TextBlock.FontFamily), TextBlock.FontFamilyProperty);
            RegisterProperty(typeof(TextBlock), nameof(TextBlock.FontSize), TextBlock.FontSizeProperty);
            RegisterProperty(typeof(TextBlock), nameof(TextBlock.FontWeight), TextBlock.FontWeightProperty);
            RegisterProperty(typeof(TextBlock), nameof(TextBlock.FontStyle), TextBlock.FontStyleProperty);
            RegisterProperty(typeof(TextBlock), nameof(TextBlock.Foreground), TextBlock.ForegroundProperty);
            RegisterProperty(typeof(TextBlock), nameof(TextBlock.Background), TextBlock.BackgroundProperty);
            RegisterProperty(typeof(TextBlock), nameof(TextBlock.TextWrapping), TextBlock.TextWrappingProperty);
            RegisterProperty(typeof(TextBlock), nameof(TextBlock.TextTrimming), TextBlock.TextTrimmingProperty);
            RegisterProperty(typeof(TextBlock), nameof(TextBlock.TextAlignment), TextBlock.TextAlignmentProperty);
            RegisterProperty(typeof(TextBlock), nameof(TextBlock.Padding), TextBlock.PaddingProperty);
            RegisterProperty(typeof(TextBlock), nameof(TextBlock.LineHeight), TextBlock.LineHeightProperty);

            RegisterType<TextFlowView>();
            RegisterProperty(typeof(TextFlowView), nameof(TextFlowView.FontFamily), TextFlowView.FontFamilyProperty);
            RegisterProperty(typeof(TextFlowView), nameof(TextFlowView.FontSize), TextFlowView.FontSizeProperty);
            RegisterProperty(typeof(TextFlowView), nameof(TextFlowView.Foreground), TextFlowView.ForegroundProperty);
            RegisterProperty(typeof(TextFlowView), nameof(TextFlowView.Padding), TextFlowView.PaddingProperty);
            RegisterProperty(typeof(TextFlowView), nameof(TextFlowView.LineSpacing), TextFlowView.LineSpacingProperty);

            // 1C. Standard Basic Controls: Button, Toggle, CheckBox, Radio, Range & TextBox
            RegisterType<Button>(contentProperty: ContentControl.ContentProperty);
            RegisterProperty(typeof(Button), nameof(Button.Command), ButtonBase.CommandProperty);
            RegisterProperty(typeof(Button), nameof(Button.CommandParameter), ButtonBase.CommandParameterProperty);
            RegisterProperty(typeof(Button), nameof(Button.ClickMode), ButtonBase.ClickModeProperty);
            RegisterProperty(typeof(Button), nameof(Button.IsDefault), Button.IsDefaultProperty);
            RegisterProperty(typeof(Button), nameof(Button.IsCancel), Button.IsCancelProperty);

            RegisterType<ToggleButton>(contentProperty: ContentControl.ContentProperty);
            RegisterProperty(typeof(ToggleButton), nameof(ToggleButton.IsChecked), ToggleButton.IsCheckedProperty);
            RegisterProperty(typeof(ToggleButton), nameof(ToggleButton.IsThreeState), ToggleButton.IsThreeStateProperty);

            RegisterType<CheckBox>(contentProperty: ContentControl.ContentProperty);
            RegisterProperty(typeof(CheckBox), nameof(CheckBox.IsChecked), ToggleButton.IsCheckedProperty);
            RegisterProperty(typeof(CheckBox), nameof(CheckBox.IsThreeState), ToggleButton.IsThreeStateProperty);

            RegisterType<RadioButton>(contentProperty: ContentControl.ContentProperty);
            RegisterProperty(typeof(RadioButton), nameof(RadioButton.IsChecked), ToggleButton.IsCheckedProperty);
            RegisterProperty(typeof(RadioButton), nameof(RadioButton.GroupName), RadioButton.GroupNameProperty);

            RegisterType<ProgressBar>();
            RegisterProperty(typeof(ProgressBar), nameof(ProgressBar.Minimum), RangeBase.MinimumProperty);
            RegisterProperty(typeof(ProgressBar), nameof(ProgressBar.Maximum), RangeBase.MaximumProperty);
            RegisterProperty(typeof(ProgressBar), nameof(ProgressBar.Value), RangeBase.ValueProperty);
            RegisterProperty(typeof(ProgressBar), nameof(ProgressBar.IsIndeterminate), ProgressBar.IsIndeterminateProperty);
            RegisterProperty(typeof(ProgressBar), nameof(ProgressBar.Orientation), ProgressBar.OrientationProperty);

            RegisterType<Slider>();
            RegisterProperty(typeof(Slider), nameof(Slider.Minimum), RangeBase.MinimumProperty);
            RegisterProperty(typeof(Slider), nameof(Slider.Maximum), RangeBase.MaximumProperty);
            RegisterProperty(typeof(Slider), nameof(Slider.Value), RangeBase.ValueProperty);
            RegisterProperty(typeof(Slider), nameof(Slider.SmallChange), RangeBase.SmallChangeProperty);
            RegisterProperty(typeof(Slider), nameof(Slider.LargeChange), RangeBase.LargeChangeProperty);
            RegisterProperty(typeof(Slider), nameof(Slider.Orientation), Slider.OrientationProperty);
            RegisterProperty(typeof(Slider), nameof(Slider.TickFrequency), Slider.TickFrequencyProperty);
            RegisterProperty(typeof(Slider), nameof(Slider.TickPlacement), Slider.TickPlacementProperty);
            RegisterProperty(typeof(Slider), nameof(Slider.IsSnapToTickEnabled), Slider.IsSnapToTickEnabledProperty);
            RegisterProperty(typeof(Slider), nameof(Slider.IsDirectionReversed), Slider.IsDirectionReversedProperty);

            RegisterType<TextBox>(contentProperty: TextBox.TextProperty);
            RegisterProperty(typeof(TextBox), nameof(TextBox.Text), TextBox.TextProperty);
            RegisterProperty(typeof(TextBox), nameof(TextBox.IsReadOnly), TextBoxBase.IsReadOnlyProperty);
            RegisterProperty(typeof(TextBox), nameof(TextBox.AcceptsReturn), TextBoxBase.AcceptsReturnProperty);
            RegisterProperty(typeof(TextBox), nameof(TextBox.AcceptsTab), TextBoxBase.AcceptsTabProperty);
            RegisterProperty(typeof(TextBox), nameof(TextBox.TextWrapping), TextBox.TextWrappingProperty);
            RegisterProperty(typeof(TextBox), nameof(TextBox.TextAlignment), TextBox.TextAlignmentProperty);
            RegisterProperty(typeof(TextBox), nameof(TextBox.MaxLength), TextBox.MaxLengthProperty);

            RegisterType<PasswordBox>();
            RegisterProperty(typeof(PasswordBox), nameof(PasswordBox.Password), PasswordBox.PasswordProperty);
            RegisterProperty(typeof(PasswordBox), nameof(PasswordBox.PasswordChar), PasswordBox.PasswordCharProperty);
            RegisterProperty(typeof(PasswordBox), nameof(PasswordBox.MaxLength), PasswordBox.MaxLengthProperty);

            // 2. 2D Brushes, Transforms & Geometries
            RegisterType<SolidColorBrush>();
            RegisterProperty(typeof(SolidColorBrush), nameof(SolidColorBrush.Color), SolidColorBrush.ColorProperty);

            RegisterType<GradientStop>();
            RegisterProperty(typeof(GradientStop), nameof(GradientStop.Color), GradientStop.ColorProperty);
            RegisterProperty(typeof(GradientStop), nameof(GradientStop.Offset), GradientStop.OffsetProperty);

            RegisterType<LinearGradientBrush>(addChild: static (b, child) =>
            {
                if (child is GradientStop gs) b.GradientStops.Add(gs);
            });
            RegisterProperty(typeof(LinearGradientBrush), nameof(LinearGradientBrush.StartPoint), LinearGradientBrush.StartPointProperty);
            RegisterProperty(typeof(LinearGradientBrush), nameof(LinearGradientBrush.EndPoint), LinearGradientBrush.EndPointProperty);

            RegisterType<RadialGradientBrush>(addChild: static (b, child) =>
            {
                if (child is GradientStop gs) b.GradientStops.Add(gs);
            });
            RegisterProperty(typeof(RadialGradientBrush), nameof(RadialGradientBrush.Center), RadialGradientBrush.CenterProperty);
            RegisterProperty(typeof(RadialGradientBrush), nameof(RadialGradientBrush.GradientOrigin), RadialGradientBrush.GradientOriginProperty);
            RegisterProperty(typeof(RadialGradientBrush), nameof(RadialGradientBrush.RadiusX), RadialGradientBrush.RadiusXProperty);
            RegisterProperty(typeof(RadialGradientBrush), nameof(RadialGradientBrush.RadiusY), RadialGradientBrush.RadiusYProperty);

            RegisterType<TranslateTransform>();
            RegisterProperty(typeof(TranslateTransform), nameof(TranslateTransform.X), TranslateTransform.XProperty);
            RegisterProperty(typeof(TranslateTransform), nameof(TranslateTransform.Y), TranslateTransform.YProperty);

            RegisterType<RotateTransform>();
            RegisterProperty(typeof(RotateTransform), nameof(RotateTransform.Angle), RotateTransform.AngleProperty);
            RegisterProperty(typeof(RotateTransform), nameof(RotateTransform.CenterX), RotateTransform.CenterXProperty);
            RegisterProperty(typeof(RotateTransform), nameof(RotateTransform.CenterY), RotateTransform.CenterYProperty);

            RegisterType<ScaleTransform>();
            RegisterProperty(typeof(ScaleTransform), nameof(ScaleTransform.ScaleX), ScaleTransform.ScaleXProperty);
            RegisterProperty(typeof(ScaleTransform), nameof(ScaleTransform.ScaleY), ScaleTransform.ScaleYProperty);

            // 3. Media3D Viewport3D, Cameras, Lights, Models & Transforms
            RegisterType<Viewport3D>(addChild: static (vp, child) =>
            {
                if (child is Visual3D v3d) vp.Children.Add(v3d);
            });
            RegisterProperty(typeof(Viewport3D), nameof(Viewport3D.Camera), Viewport3D.CameraProperty);

            RegisterType<PerspectiveCamera>();
            RegisterProperty(typeof(PerspectiveCamera), nameof(PerspectiveCamera.Position), ProjectionCamera.PositionProperty);
            RegisterProperty(typeof(PerspectiveCamera), nameof(PerspectiveCamera.LookDirection), ProjectionCamera.LookDirectionProperty);
            RegisterProperty(typeof(PerspectiveCamera), nameof(PerspectiveCamera.UpDirection), ProjectionCamera.UpDirectionProperty);
            RegisterProperty(typeof(PerspectiveCamera), nameof(PerspectiveCamera.FieldOfView), PerspectiveCamera.FieldOfViewProperty);
            RegisterProperty(typeof(PerspectiveCamera), nameof(PerspectiveCamera.NearPlaneDistance), ProjectionCamera.NearPlaneDistanceProperty);
            RegisterProperty(typeof(PerspectiveCamera), nameof(PerspectiveCamera.FarPlaneDistance), ProjectionCamera.FarPlaneDistanceProperty);

            RegisterType<OrthographicCamera>();
            RegisterProperty(typeof(OrthographicCamera), nameof(OrthographicCamera.Position), ProjectionCamera.PositionProperty);
            RegisterProperty(typeof(OrthographicCamera), nameof(OrthographicCamera.LookDirection), ProjectionCamera.LookDirectionProperty);
            RegisterProperty(typeof(OrthographicCamera), nameof(OrthographicCamera.UpDirection), ProjectionCamera.UpDirectionProperty);
            RegisterProperty(typeof(OrthographicCamera), nameof(OrthographicCamera.Width), OrthographicCamera.WidthProperty);

            RegisterType<ModelVisual3D>(addChild: static (mv, child) =>
            {
                if (child is Model3D m3d) mv.Content = m3d;
                else if (child is Visual3D v3d) mv.Children.Add(v3d);
            });
            RegisterProperty(typeof(ModelVisual3D), nameof(ModelVisual3D.Content), ModelVisual3D.ContentProperty);
            RegisterProperty(typeof(ModelVisual3D), nameof(ModelVisual3D.Transform), ModelVisual3D.TransformProperty);

            RegisterType<Model3DGroup>(addChild: static (grp, child) =>
            {
                if (child is Model3D m3d) grp.Children.Add(m3d);
            });
            RegisterProperty(typeof(Model3DGroup), nameof(Model3DGroup.Transform), Model3D.TransformProperty);

            RegisterType<AmbientLight>();
            RegisterProperty(typeof(AmbientLight), nameof(AmbientLight.Color), Light.ColorProperty);

            RegisterType<DirectionalLight>();
            RegisterProperty(typeof(DirectionalLight), nameof(DirectionalLight.Color), Light.ColorProperty);
            RegisterProperty(typeof(DirectionalLight), nameof(DirectionalLight.Direction), DirectionalLight.DirectionProperty);

            RegisterType<PointLight>();
            RegisterProperty(typeof(PointLight), nameof(PointLight.Color), Light.ColorProperty);
            RegisterProperty(typeof(PointLight), nameof(PointLight.Position), PointLightBase.PositionProperty);
            RegisterProperty(typeof(PointLight), nameof(PointLight.Range), PointLightBase.RangeProperty);

            RegisterType<SpotLight>();
            RegisterProperty(typeof(SpotLight), nameof(SpotLight.Color), Light.ColorProperty);
            RegisterProperty(typeof(SpotLight), nameof(SpotLight.Position), PointLightBase.PositionProperty);
            RegisterProperty(typeof(SpotLight), nameof(SpotLight.Direction), SpotLight.DirectionProperty);
            RegisterProperty(typeof(SpotLight), nameof(SpotLight.OuterConeAngle), SpotLight.OuterConeAngleProperty);
            RegisterProperty(typeof(SpotLight), nameof(SpotLight.InnerConeAngle), SpotLight.InnerConeAngleProperty);

            RegisterType<GeometryModel3D>();
            RegisterProperty(typeof(GeometryModel3D), nameof(GeometryModel3D.Geometry), GeometryModel3D.GeometryProperty);
            RegisterProperty(typeof(GeometryModel3D), nameof(GeometryModel3D.Material), GeometryModel3D.MaterialProperty);
            RegisterProperty(typeof(GeometryModel3D), nameof(GeometryModel3D.BackMaterial), GeometryModel3D.BackMaterialProperty);
            RegisterProperty(typeof(GeometryModel3D), nameof(GeometryModel3D.Transform), Model3D.TransformProperty);

            RegisterType<MeshGeometry3D>();
            RegisterProperty(typeof(MeshGeometry3D), nameof(MeshGeometry3D.Positions), MeshGeometry3D.PositionsProperty);
            RegisterProperty(typeof(MeshGeometry3D), nameof(MeshGeometry3D.Normals), MeshGeometry3D.NormalsProperty);
            RegisterProperty(typeof(MeshGeometry3D), nameof(MeshGeometry3D.TextureCoordinates), MeshGeometry3D.TextureCoordinatesProperty);
            RegisterProperty(typeof(MeshGeometry3D), nameof(MeshGeometry3D.TriangleIndices), MeshGeometry3D.TriangleIndicesProperty);

            RegisterType<DiffuseMaterial>();
            RegisterProperty(typeof(DiffuseMaterial), nameof(DiffuseMaterial.Brush), DiffuseMaterial.BrushProperty);
            RegisterProperty(typeof(DiffuseMaterial), nameof(DiffuseMaterial.Color), DiffuseMaterial.ColorProperty);

            RegisterType<SpecularMaterial>();
            RegisterProperty(typeof(SpecularMaterial), nameof(SpecularMaterial.Brush), SpecularMaterial.BrushProperty);
            RegisterProperty(typeof(SpecularMaterial), nameof(SpecularMaterial.SpecularPower), SpecularMaterial.SpecularPowerProperty);

            RegisterType<EmissiveMaterial>();
            RegisterProperty(typeof(EmissiveMaterial), nameof(EmissiveMaterial.Brush), EmissiveMaterial.BrushProperty);

            RegisterType<MaterialGroup>(addChild: static (mg, child) =>
            {
                if (child is Material m) mg.Children.Add(m);
            });

            RegisterType<ModelUIElement3D>(addChild: static (el, child) =>
            {
                if (child is Model3D m) el.Model = m;
            });
            RegisterProperty(typeof(ModelUIElement3D), nameof(ModelUIElement3D.Model), ModelUIElement3D.ModelProperty);
            RegisterProperty(typeof(ModelUIElement3D), nameof(ModelUIElement3D.Transform), Visual3D.TransformProperty);
            RegisterEvent<UIElement3D>(nameof(UIElement3D.MouseDown), static (el, d) => el.MouseDown += (MouseButtonEventHandler)d);

            RegisterType<ContainerUIElement3D>(addChild: static (c, child) =>
            {
                if (child is Visual3D v) c.Children.Add(v);
            });

            RegisterType<Viewport2DVisual3D>(addChild: static (vp, child) =>
            {
                if (child is Visual v) vp.Visual = v;
            });
            RegisterProperty(typeof(Viewport2DVisual3D), nameof(Viewport2DVisual3D.Geometry), Viewport2DVisual3D.GeometryProperty);
            RegisterProperty(typeof(Viewport2DVisual3D), nameof(Viewport2DVisual3D.Material), Viewport2DVisual3D.MaterialProperty);
            RegisterProperty(typeof(Viewport2DVisual3D), nameof(Viewport2DVisual3D.Visual), Viewport2DVisual3D.VisualProperty);
            RegisterProperty(typeof(Viewport2DVisual3D), "IsVisualHostMaterial", Viewport2DVisual3D.IsVisualHostMaterialProperty);
            RegisterProperty(typeof(Viewport2DVisual3D), nameof(Viewport2DVisual3D.Transform), Visual3D.TransformProperty);

            RegisterType<RotateTransform3D>();
            RegisterProperty(typeof(RotateTransform3D), nameof(RotateTransform3D.Rotation), RotateTransform3D.RotationProperty);

            RegisterType<AxisAngleRotation3D>();
            RegisterProperty(typeof(AxisAngleRotation3D), nameof(AxisAngleRotation3D.Axis), AxisAngleRotation3D.AxisProperty);
            RegisterProperty(typeof(AxisAngleRotation3D), nameof(AxisAngleRotation3D.Angle), AxisAngleRotation3D.AngleProperty);

            RegisterType<TranslateTransform3D>();
            RegisterProperty(typeof(TranslateTransform3D), nameof(TranslateTransform3D.OffsetX), TranslateTransform3D.OffsetXProperty);
            RegisterProperty(typeof(TranslateTransform3D), nameof(TranslateTransform3D.OffsetY), TranslateTransform3D.OffsetYProperty);
            RegisterProperty(typeof(TranslateTransform3D), nameof(TranslateTransform3D.OffsetZ), TranslateTransform3D.OffsetZProperty);

            RegisterType<ScaleTransform3D>();
            RegisterProperty(typeof(ScaleTransform3D), nameof(ScaleTransform3D.ScaleX), ScaleTransform3D.ScaleXProperty);
            RegisterProperty(typeof(ScaleTransform3D), nameof(ScaleTransform3D.ScaleY), ScaleTransform3D.ScaleYProperty);
            RegisterProperty(typeof(ScaleTransform3D), nameof(ScaleTransform3D.ScaleZ), ScaleTransform3D.ScaleZProperty);

            RegisterType<Transform3DGroup>(addChild: static (grp, child) =>
            {
                if (child is Transform3D t) grp.Children.Add(t);
            });

            // Storyboard, EventTrigger, BeginStoryboard & Animations
            RegisterType<EventTrigger>(
                addChild: static (et, child) =>
                {
                    if (child is TriggerAction act) et.Actions.Add(act);
                });
            RegisterClrProperty<EventTrigger>("RoutedEvent", static (et, val, _) =>
            {
                string trimmed = val.Trim();
                int dot = trimmed.IndexOf('.');
                string evName = dot > 0 ? trimmed.Substring(dot + 1) : trimmed;
                et.RoutedEvent = evName switch
                {
                    "Loaded" => FrameworkElement.LoadedEvent,
                    "Unloaded" => FrameworkElement.UnloadedEvent,
                    "MouseEnter" => UIElement.MouseEnterEvent,
                    "MouseLeave" => UIElement.MouseLeaveEvent,
                    "MouseDown" => UIElement.MouseDownEvent,
                    "MouseUp" => UIElement.MouseUpEvent,
                    "PreviewMouseDown" => UIElement.PreviewMouseDownEvent,
                    "PreviewMouseUp" => UIElement.PreviewMouseUpEvent,
                    "Click" => ButtonBase.ClickEvent,
                    "Checked" => ToggleButton.CheckedEvent,
                    "Unchecked" => ToggleButton.UncheckedEvent,
                    "GotFocus" => UIElement.GotFocusEvent,
                    "LostFocus" => UIElement.LostFocusEvent,
                    _ => throw new XamlParseException($"Unknown RoutedEvent '{val}' on EventTrigger.")
                };
            });
            RegisterClrProperty<EventTrigger>("SourceName", static (et, val, _) => et.SourceName = val);

            RegisterType<BeginStoryboard>(contentProperty: BeginStoryboard.StoryboardProperty);
            RegisterClrProperty<BeginStoryboard>("Name", static (bs, val, _) => bs.Name = val);

            RegisterType<System.Windows.Media.Animation.Storyboard>(
                addChild: static (sb, child) =>
                {
                    if (child is System.Windows.Media.Animation.Timeline tl) sb.Children.Add(tl);
                });
            RegisterProperty(typeof(System.Windows.Media.Animation.Storyboard), "TargetName", System.Windows.Media.Animation.Storyboard.TargetNameProperty);
            RegisterProperty(typeof(System.Windows.Media.Animation.Storyboard), "TargetProperty", System.Windows.Media.Animation.Storyboard.TargetPropertyProperty);
            RegisterProperty(typeof(System.Windows.Media.Animation.Storyboard), "Target", System.Windows.Media.Animation.Storyboard.TargetProperty);
            RegisterProperty(typeof(System.Windows.Media.Animation.Timeline), "Duration", System.Windows.Media.Animation.Timeline.DurationProperty);
            RegisterProperty(typeof(System.Windows.Media.Animation.Timeline), "BeginTime", System.Windows.Media.Animation.Timeline.BeginTimeProperty);
            RegisterProperty(typeof(System.Windows.Media.Animation.Timeline), "RepeatBehavior", System.Windows.Media.Animation.Timeline.RepeatBehaviorProperty);
            RegisterProperty(typeof(System.Windows.Media.Animation.Timeline), "AutoReverse", System.Windows.Media.Animation.Timeline.AutoReverseProperty);
            RegisterProperty(typeof(System.Windows.Media.Animation.Timeline), "SpeedRatio", System.Windows.Media.Animation.Timeline.SpeedRatioProperty);

            RegisterType<System.Windows.Media.Animation.DoubleAnimation>();
            RegisterProperty(typeof(System.Windows.Media.Animation.DoubleAnimation), "From", System.Windows.Media.Animation.DoubleAnimation.FromProperty);
            RegisterProperty(typeof(System.Windows.Media.Animation.DoubleAnimation), "To", System.Windows.Media.Animation.DoubleAnimation.ToProperty);
            RegisterProperty(typeof(System.Windows.Media.Animation.DoubleAnimation), "By", System.Windows.Media.Animation.DoubleAnimation.ByProperty);
            RegisterProperty(typeof(System.Windows.Media.Animation.DoubleAnimation), "EasingFunction", System.Windows.Media.Animation.DoubleAnimation.EasingFunctionProperty);

            RegisterType<System.Windows.Media.Animation.ColorAnimation>();
            RegisterProperty(typeof(System.Windows.Media.Animation.ColorAnimation), "From", System.Windows.Media.Animation.ColorAnimation.FromProperty);
            RegisterProperty(typeof(System.Windows.Media.Animation.ColorAnimation), "To", System.Windows.Media.Animation.ColorAnimation.ToProperty);
            RegisterProperty(typeof(System.Windows.Media.Animation.ColorAnimation), "By", System.Windows.Media.Animation.ColorAnimation.ByProperty);
            RegisterProperty(typeof(System.Windows.Media.Animation.ColorAnimation), "EasingFunction", System.Windows.Media.Animation.ColorAnimation.EasingFunctionProperty);

            RegisterType<System.Windows.Media.Animation.PointAnimation>();
            RegisterProperty(typeof(System.Windows.Media.Animation.PointAnimation), "From", System.Windows.Media.Animation.PointAnimation.FromProperty);
            RegisterProperty(typeof(System.Windows.Media.Animation.PointAnimation), "To", System.Windows.Media.Animation.PointAnimation.ToProperty);
            RegisterProperty(typeof(System.Windows.Media.Animation.PointAnimation), "By", System.Windows.Media.Animation.PointAnimation.ByProperty);
            RegisterProperty(typeof(System.Windows.Media.Animation.PointAnimation), "EasingFunction", System.Windows.Media.Animation.PointAnimation.EasingFunctionProperty);

            RegisterType<System.Windows.Media.Animation.CubicEase>();
            RegisterType<System.Windows.Media.Animation.QuadraticEase>();
            RegisterType<System.Windows.Media.Animation.SineEase>();
            RegisterType<System.Windows.Media.Animation.ExponentialEase>();
            RegisterType<System.Windows.Media.Animation.BounceEase>();
            RegisterType<System.Windows.Media.Animation.ElasticEase>();
            RegisterType<System.Windows.Media.Animation.BackEase>();
            RegisterType<System.Windows.Media.Animation.CircleEase>();
            RegisterType<System.Windows.Media.Animation.PowerEase>();
            RegisterProperty(typeof(System.Windows.Media.Animation.EasingFunctionBase), "EasingMode", System.Windows.Media.Animation.EasingFunctionBase.EasingModeProperty);
        }
    }
}

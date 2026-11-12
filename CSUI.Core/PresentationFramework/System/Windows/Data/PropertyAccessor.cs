#nullable enable

using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using MS.Internal.Data;

namespace System.Windows.Data
{
    /// <summary>
    /// Reflection-free property accessor contract for NativeAOT data binding and PropertyPath evaluation.
    /// </summary>
    public interface IPropertyAccessor
    {
        /// <summary>Gets the property name.</summary>
        string PropertyName { get; }

        /// <summary>Gets the declaring or target source type.</summary>
        Type SourceType { get; }

        /// <summary>Gets the property value type.</summary>
        Type PropertyType { get; }

        /// <summary>Gets whether the property cannot be written back to source.</summary>
        bool IsReadOnly { get; }

        /// <summary>Gets whether the property is static.</summary>
        bool IsStatic { get; }

        /// <summary>Gets the property value from <paramref name="item"/>.</summary>
        object? GetValue(object? item);

        /// <summary>Sets the property value on <paramref name="item"/>.</summary>
        void SetValue(object? item, object? value);
    }

    /// <summary>
    /// Strongly-typed reflection-free property accessor for NativeAOT data binding.
    /// </summary>
    public sealed class PropertyAccessor<TSource, TValue> : IPropertyAccessor
    {
        private readonly Func<TSource, TValue>? _getter;
        private readonly Action<TSource, TValue>? _setter;
        private readonly Func<TValue>? _staticGetter;
        private readonly Action<TValue>? _staticSetter;

        /// <summary>Initializes an instance property accessor.</summary>
        public PropertyAccessor(string propertyName, Func<TSource, TValue> getter, Action<TSource, TValue>? setter = null)
        {
            ArgumentException.ThrowIfNullOrEmpty(propertyName);
            ArgumentNullException.ThrowIfNull(getter);

            PropertyName = propertyName;
            _getter = getter;
            _setter = setter;
        }

        /// <summary>Initializes a static property accessor.</summary>
        public PropertyAccessor(string propertyName, Func<TValue> staticGetter, Action<TValue>? staticSetter = null)
        {
            ArgumentException.ThrowIfNullOrEmpty(propertyName);
            ArgumentNullException.ThrowIfNull(staticGetter);

            PropertyName = propertyName;
            _staticGetter = staticGetter;
            _staticSetter = staticSetter;
            IsStatic = true;
        }

        /// <inheritdoc />
        public string PropertyName { get; }

        /// <inheritdoc />
        public Type SourceType => typeof(TSource);

        /// <inheritdoc />
        public Type PropertyType => typeof(TValue);

        /// <inheritdoc />
        public bool IsReadOnly => IsStatic ? _staticSetter == null : _setter == null;

        /// <inheritdoc />
        public bool IsStatic { get; }

        /// <inheritdoc />
        public object? GetValue(object? item)
        {
            if (IsStatic)
            {
                return _staticGetter!();
            }

            if (item is TSource typed)
            {
                return _getter!(typed);
            }

            if (item is null)
            {
                throw new InvalidOperationException($"Cannot read property '{PropertyName}' from a null source of type '{typeof(TSource).Name}'.");
            }

            throw new InvalidCastException($"Cannot convert source of type '{item.GetType().Name}' to '{typeof(TSource).Name}' for property '{PropertyName}'.");
        }

        /// <inheritdoc />
        public void SetValue(object? item, object? value)
        {
            if (IsReadOnly)
            {
                throw new InvalidOperationException($"Property '{PropertyName}' on '{typeof(TSource).Name}' is read-only.");
            }

            TValue typedValue;
            if (value is TValue direct)
            {
                typedValue = direct;
            }
            else if (value is null && default(TValue) is null)
            {
                typedValue = default!;
            }
            else
            {
                throw new InvalidCastException($"Cannot convert value of type '{value?.GetType().Name ?? "null"}' to '{typeof(TValue).Name}' for property '{PropertyName}'.");
            }

            if (IsStatic)
            {
                _staticSetter!(typedValue);
                return;
            }

            if (item is TSource typedSource)
            {
                _setter!(typedSource, typedValue);
                return;
            }

            if (item is null)
            {
                throw new InvalidOperationException($"Cannot write property '{PropertyName}' to a null source of type '{typeof(TSource).Name}'.");
            }

            throw new InvalidCastException($"Cannot convert source of type '{item.GetType().Name}' to '{typeof(TSource).Name}' for property '{PropertyName}'.");
        }
    }

    /// <summary>
    /// Internal contract for NativeAOT indexer accessors (built-in collections and registered custom indexers).
    /// </summary>
    internal interface IIndexerAccessor
    {
        string IndexerName { get; }
        Type ElementType { get; }
        bool IsReadOnly { get; }
        bool TryMatchArgs(IndexerParameterInfo[] aryInfo, object?[] args);
        object? GetValue(object item, object?[] args);
        void SetValue(object item, object?[] args, object? value);
    }

    /// <summary>
    /// Strongly-typed reflection-free single-argument indexer accessor.
    /// </summary>
    public sealed class IndexerAccessor<TSource, TIndex, TValue> : IIndexerAccessor
    {
        private readonly Func<TSource, TIndex, TValue> _getter;
        private readonly Action<TSource, TIndex, TValue>? _setter;

        /// <summary>Initializes an indexer accessor.</summary>
        public IndexerAccessor(Func<TSource, TIndex, TValue> getter, Action<TSource, TIndex, TValue>? setter = null, string indexerName = "Item")
        {
            ArgumentNullException.ThrowIfNull(getter);
            ArgumentException.ThrowIfNullOrEmpty(indexerName);
            _getter = getter;
            _setter = setter;
            IndexerName = indexerName;
        }

        /// <summary>Gets the CLR indexer name (default "Item").</summary>
        public string IndexerName { get; }

        Type IIndexerAccessor.ElementType => typeof(TValue);
        bool IIndexerAccessor.IsReadOnly => _setter == null;

        bool IIndexerAccessor.TryMatchArgs(IndexerParameterInfo[] aryInfo, object?[] args)
        {
            if (aryInfo.Length != 1)
                return false;

            object? arg = null;
            IndexerParameterInfo pInfo = aryInfo[0];
            if (pInfo.type != null)
            {
                if (!typeof(TIndex).IsAssignableFrom(pInfo.type))
                    return false;
                arg = pInfo.value;
            }
            else if (pInfo.value is string s)
            {
                if (typeof(TIndex) == typeof(string) || typeof(TIndex) == typeof(object))
                {
                    arg = s;
                }
                else
                {
                    arg = TypeConverterRegistry.ConvertFromInvariantString(s, typeof(TIndex));
                    if (arg == null)
                        return false;
                }
            }
            else if (pInfo.value is TIndex)
            {
                arg = pInfo.value;
            }
            else
            {
                return false;
            }

            args[0] = arg;
            return true;
        }

        object? IIndexerAccessor.GetValue(object item, object?[] args)
        {
            if (item is not TSource typed || args.Length != 1 || args[0] is not TIndex index)
                return DependencyProperty.UnsetValue;
            return _getter(typed, index);
        }

        void IIndexerAccessor.SetValue(object item, object?[] args, object? value)
        {
            if (_setter == null)
                throw new InvalidOperationException($"Indexer '{IndexerName}' on '{typeof(TSource).Name}' is read-only.");
            if (item is not TSource typed || args.Length != 1 || args[0] is not TIndex index)
                throw new InvalidCastException();

            TValue typedValue = value is null && default(TValue) is null ? default! : (TValue)value!;
            _setter(typed, index, typedValue);
        }
    }

    internal sealed class IListIndexerAccessor : IIndexerAccessor
    {
        internal static readonly IListIndexerAccessor Instance = new IListIndexerAccessor();
        private IListIndexerAccessor() { }

        public string IndexerName => "Item";
        public Type ElementType => typeof(object);
        public bool IsReadOnly => false;

        public bool TryMatchArgs(IndexerParameterInfo[] aryInfo, object?[] args)
        {
            if (aryInfo.Length != 1)
                return false;

            IndexerParameterInfo pInfo = aryInfo[0];
            if (pInfo.type != null)
            {
                if (pInfo.type == typeof(int) && pInfo.value is int idx)
                {
                    args[0] = new PropertyPathWorker.IListIndexerArg(idx);
                    return true;
                }
                return false;
            }

            if (pInfo.value is string s &&
                int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsed))
            {
                args[0] = new PropertyPathWorker.IListIndexerArg(parsed);
                return true;
            }

            return false;
        }

        public object? GetValue(object item, object?[] args)
        {
            IList list = (IList)item;
            int index = args[0] is PropertyPathWorker.IListIndexerArg wrapper ? wrapper.Value : (int)args[0]!;
            if ((uint)index < (uint)list.Count)
            {
                return list[index];
            }
            return PropertyPathWorker.IListIndexOutOfRange;
        }

        public void SetValue(object item, object?[] args, object? value)
        {
            IList list = (IList)item;
            int index = args[0] is PropertyPathWorker.IListIndexerArg wrapper ? wrapper.Value : (int)args[0]!;
            list[index] = value;
        }
    }

    internal sealed class IDictionaryIndexerAccessor : IIndexerAccessor
    {
        internal static readonly IDictionaryIndexerAccessor Instance = new IDictionaryIndexerAccessor();
        private IDictionaryIndexerAccessor() { }

        public string IndexerName => "Item";
        public Type ElementType => typeof(object);
        public bool IsReadOnly => false;

        public bool TryMatchArgs(IndexerParameterInfo[] aryInfo, object?[] args)
        {
            if (aryInfo.Length != 1)
                return false;

            args[0] = aryInfo[0].value;
            return args[0] != null;
        }

        public object? GetValue(object item, object?[] args)
        {
            IDictionary dict = (IDictionary)item;
            object key = args[0]!;
            return dict.Contains(key) ? dict[key] : null;
        }

        public void SetValue(object item, object?[] args, object? value)
        {
            IDictionary dict = (IDictionary)item;
            dict[args[0]!] = value;
        }
    }

    internal sealed class StringCharIndexerAccessor : IIndexerAccessor
    {
        internal static readonly StringCharIndexerAccessor Instance = new StringCharIndexerAccessor();
        private StringCharIndexerAccessor() { }

        public string IndexerName => "Chars";
        public Type ElementType => typeof(char);
        public bool IsReadOnly => true;

        public bool TryMatchArgs(IndexerParameterInfo[] aryInfo, object?[] args)
        {
            if (aryInfo.Length != 1)
                return false;

            IndexerParameterInfo pInfo = aryInfo[0];
            if (pInfo.type == typeof(int) && pInfo.value is int idx)
            {
                args[0] = idx;
                return true;
            }

            if (pInfo.type == null && pInfo.value is string s &&
                int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsed))
            {
                args[0] = parsed;
                return true;
            }

            return false;
        }

        public object? GetValue(object item, object?[] args)
        {
            string s = (string)item;
            int index = (int)args[0]!;
            return (uint)index < (uint)s.Length ? s[index] : DependencyProperty.UnsetValue;
        }

        public void SetValue(object item, object?[] args, object? value)
        {
            throw new InvalidOperationException("String indexer is read-only.");
        }
    }

    /// <summary>
    /// Global NativeAOT registry mapping (sourceType, propertyName) pairs to reflection-free accessors.
    /// </summary>
    public static class PropertyAccessorRegistry
    {
        private readonly record struct AccessorKey(Type SourceType, string PropertyName);

        private static readonly ConcurrentDictionary<AccessorKey, IPropertyAccessor> s_accessors = new();
        private static readonly ConcurrentDictionary<AccessorKey, IIndexerAccessor> s_indexers = new();
        private static readonly ConcurrentDictionary<Type, byte> s_registeredInterfaces = new();
        private static readonly ConcurrentDictionary<Type, byte> s_registeredIndexerInterfaces = new();
        private static readonly ConcurrentDictionary<Type, Func<object?>> s_itemFactories = new();
        private static readonly ConcurrentDictionary<Type, Type> s_collectionItemTypes = new();

        static PropertyAccessorRegistry()
        {
            Register<ICollection, int>(nameof(ICollection.Count), static c => c.Count);
            Register<IList, int>(nameof(IList.Count), static l => l.Count);
            Register<Array, int>(nameof(Array.Length), static a => a.Length);
            Register<string, int>(nameof(string.Length), static s => s.Length);
            Register<CollectionView, int>(nameof(CollectionView.Count), static cv => cv.Count);
            Register<CollectionView, object>(nameof(CollectionView.CurrentItem), static cv => cv.CurrentItem);
            Register<CollectionView, int>(nameof(CollectionView.CurrentPosition), static cv => cv.CurrentPosition);
            Register<CollectionView, bool>(nameof(CollectionView.IsEmpty), static cv => cv.IsEmpty);
            Register<DictionaryEntry, object>(nameof(DictionaryEntry.Key), static de => de.Key);
            Register<DictionaryEntry, object?>(nameof(DictionaryEntry.Value), static de => de.Value);
        }

        /// <summary>Registers a reflection-free instance property accessor for <typeparamref name="TSource"/>.</summary>
        public static PropertyAccessor<TSource, TValue> Register<TSource, TValue>(
            string propertyName,
            Func<TSource, TValue> getter,
            Action<TSource, TValue>? setter = null)
        {
            var accessor = new PropertyAccessor<TSource, TValue>(propertyName, getter, setter);
            s_accessors[new AccessorKey(typeof(TSource), propertyName)] = accessor;
            if (typeof(TSource).IsInterface)
                s_registeredInterfaces.TryAdd(typeof(TSource), 0);
            return accessor;
        }

        /// <summary>Registers a reflection-free static property accessor on <typeparamref name="TOwner"/>.</summary>
        public static PropertyAccessor<TOwner, TValue> RegisterStatic<TOwner, TValue>(
            string propertyName,
            Func<TValue> staticGetter,
            Action<TValue>? staticSetter = null)
        {
            var accessor = new PropertyAccessor<TOwner, TValue>(propertyName, staticGetter, staticSetter);
            s_accessors[new AccessorKey(typeof(TOwner), propertyName)] = accessor;
            return accessor;
        }

        /// <summary>Registers a custom <see cref="IPropertyAccessor"/>.</summary>
        public static void Register(IPropertyAccessor accessor)
        {
            ArgumentNullException.ThrowIfNull(accessor);
            s_accessors[new AccessorKey(accessor.SourceType, accessor.PropertyName)] = accessor;
            if (accessor.SourceType.IsInterface)
                s_registeredInterfaces.TryAdd(accessor.SourceType, 0);
        }

        /// <summary>Registers a strongly-typed indexer accessor for <typeparamref name="TSource"/>.</summary>
        public static IndexerAccessor<TSource, TIndex, TValue> RegisterIndexer<TSource, TIndex, TValue>(
            Func<TSource, TIndex, TValue> getter,
            Action<TSource, TIndex, TValue>? setter = null,
            string indexerName = "Item")
        {
            var accessor = new IndexerAccessor<TSource, TIndex, TValue>(getter, setter, indexerName);
            s_indexers[new AccessorKey(typeof(TSource), indexerName)] = accessor;
            if (typeof(TSource).IsInterface)
                s_registeredIndexerInterfaces.TryAdd(typeof(TSource), 0);
            return accessor;
        }

        /// <summary>Registers a reflection-free item factory for <c>ListCollectionView.AddNew</c>.</summary>
        public static void RegisterItemFactory<T>(Func<T> factory)
        {
            ArgumentNullException.ThrowIfNull(factory);
            s_itemFactories[typeof(T)] = () => factory();
        }

        internal static Func<object?>? FindItemFactory(Type? itemType)
            => itemType != null && s_itemFactories.TryGetValue(itemType, out Func<object?>? factory) ? factory : null;

        /// <summary>Registers the item element type for a custom collection type.</summary>
        public static void RegisterCollectionItemType<TCollection, TItem>()
            => s_collectionItemTypes[typeof(TCollection)] = typeof(TItem);

        internal static Type? FindCollectionItemType(Type? collectionType)
            => collectionType != null && s_collectionItemTypes.TryGetValue(collectionType, out Type? itemType) ? itemType : null;

        /// <summary>Finds an accessor for <paramref name="sourceType"/> and <paramref name="propertyName"/>, walking base types and registered interfaces.</summary>
        public static IPropertyAccessor? Find(Type? sourceType, string propertyName)
        {
            if (sourceType == null || string.IsNullOrEmpty(propertyName))
                return null;

            for (Type? t = sourceType; t != null; t = t.BaseType)
            {
                if (s_accessors.TryGetValue(new AccessorKey(t, propertyName), out IPropertyAccessor? found))
                    return found;
            }

            foreach (Type iface in s_registeredInterfaces.Keys)
            {
                if (iface.IsAssignableFrom(sourceType) &&
                    s_accessors.TryGetValue(new AccessorKey(iface, propertyName), out IPropertyAccessor? found))
                {
                    return found;
                }
            }

            return null;
        }

        internal static IIndexerAccessor? FindIndexer(Type? sourceType, string? indexerName = null)
        {
            if (sourceType == null)
                return null;

            string keyName = string.IsNullOrEmpty(indexerName) ? "Item" : indexerName;
            for (Type? t = sourceType; t != null; t = t.BaseType)
            {
                if (s_indexers.TryGetValue(new AccessorKey(t, keyName), out IIndexerAccessor? found))
                    return found;
            }

            foreach (Type iface in s_registeredIndexerInterfaces.Keys)
            {
                if (iface.IsAssignableFrom(sourceType) &&
                    s_indexers.TryGetValue(new AccessorKey(iface, keyName), out IIndexerAccessor? found))
                {
                    return found;
                }
            }

            if (string.IsNullOrEmpty(indexerName) || indexerName == "Item")
            {
                if (typeof(IDictionary).IsAssignableFrom(sourceType))
                    return IDictionaryIndexerAccessor.Instance;
                if (typeof(IList).IsAssignableFrom(sourceType))
                    return IListIndexerAccessor.Instance;
            }

            if ((string.IsNullOrEmpty(indexerName) || indexerName == "Chars") && sourceType == typeof(string))
            {
                return StringCharIndexerAccessor.Instance;
            }

            return null;
        }
    }

    /// <summary>
    /// NativeAOT-compatible TypeConverter lookup registry avoiding TypeDescriptor reflection.
    /// </summary>
    public static class TypeConverterRegistry
    {
        private static readonly ConcurrentDictionary<Type, TypeConverter> s_converters = new();

        static TypeConverterRegistry()
        {
            Register<bool>(new BooleanConverter());
            Register<byte>(new ByteConverter());
            Register<sbyte>(new SByteConverter());
            Register<short>(new Int16Converter());
            Register<ushort>(new UInt16Converter());
            Register<int>(new Int32Converter());
            Register<uint>(new UInt32Converter());
            Register<long>(new Int64Converter());
            Register<ulong>(new UInt64Converter());
            Register<float>(new SingleConverter());
            Register<double>(new DoubleConverter());
            Register<decimal>(new DecimalConverter());
            Register<char>(new CharConverter());
            Register<string>(new StringConverter());
            Register<DateTime>(new DateTimeConverter());
            Register<DateTimeOffset>(new DateTimeOffsetConverter());
            Register<TimeSpan>(new TimeSpanConverter());
            Register<Guid>(new GuidConverter());
            Register<Uri>(new UriTypeConverter());
            Register<CultureInfo>(new CultureInfoConverter());
            Register<bool?>(new NullableBoolConverter());
            Register<Thickness>(new ThicknessConverter());
            Register<CornerRadius>(new CornerRadiusConverter());
            Register<GridLength>(new GridLengthConverter());
            Register<FigureLength>(new FigureLengthConverter());
            Register<VirtualizationCacheLength>(new VirtualizationCacheLengthConverter());
            Register<Key>(new KeyConverter());
            Register<ModifierKeys>(new ModifierKeysConverter());
            Register<KeyGesture>(new KeyGestureConverter());
            Register<ImageSource>(new ImageSourceConverter());
            Register<PixelFormat>(new PixelFormatConverter());
            Register<Point3D>(new Point3DConverter());
            Register<Vector3D>(new Vector3DConverter());
            Register<Size3D>(new Size3DConverter());
            Register<Rect3D>(new Rect3DConverter());
            Register<Point4D>(new Point4DConverter());
            Register<Matrix3D>(new Matrix3DConverter());
            Register<Quaternion>(new QuaternionConverter());
            Register<Point3DCollection>(new Point3DCollectionConverter());
            Register<Vector3DCollection>(new Vector3DCollectionConverter());
        }

        /// <summary>Registers a converter for <paramref name="type"/>.</summary>
        public static void Register(Type type, TypeConverter converter)
        {
            ArgumentNullException.ThrowIfNull(type);
            ArgumentNullException.ThrowIfNull(converter);
            s_converters[type] = converter;
        }

        /// <summary>Registers a converter for <typeparamref name="T"/>.</summary>
        public static void Register<T>(TypeConverter converter) => Register(typeof(T), converter);

        /// <summary>Gets a registered converter for <paramref name="type"/>, or null if none is registered.</summary>
        public static TypeConverter? GetConverter(Type? type)
        {
            if (type == null)
                return null;

            if (s_converters.TryGetValue(type, out TypeConverter? converter))
                return converter;

            Type? underlying = Nullable.GetUnderlyingType(type);
            if (underlying != null && s_converters.TryGetValue(underlying, out TypeConverter? innerConverter))
                return innerConverter;

            return null;
        }

        internal static object? ConvertFromInvariantString(string text, Type targetType)
            => TryParseKnownType(text, targetType, CultureInfo.InvariantCulture);

        internal static object? TryParseKnownType(string text, Type targetType, CultureInfo? culture)
        {
            Type? nonNullable = Nullable.GetUnderlyingType(targetType) ?? targetType;
            IFormatProvider provider = culture ?? CultureInfo.InvariantCulture;

            if (nonNullable == typeof(string))
                return text;

            if (nonNullable == typeof(int) && int.TryParse(text, NumberStyles.Any, provider, out int i32))
                return i32;
            if (nonNullable == typeof(double) && double.TryParse(text, NumberStyles.Any, provider, out double f64))
                return f64;
            if (nonNullable == typeof(float) && float.TryParse(text, NumberStyles.Any, provider, out float f32))
                return f32;
            if (nonNullable == typeof(long) && long.TryParse(text, NumberStyles.Any, provider, out long i64))
                return i64;
            if (nonNullable == typeof(decimal) && decimal.TryParse(text, NumberStyles.Any, provider, out decimal dec))
                return dec;
            if (nonNullable == typeof(short) && short.TryParse(text, NumberStyles.Any, provider, out short i16))
                return i16;
            if (nonNullable == typeof(byte) && byte.TryParse(text, NumberStyles.Any, provider, out byte u8))
                return u8;
            if (nonNullable == typeof(sbyte) && sbyte.TryParse(text, NumberStyles.Any, provider, out sbyte i8))
                return i8;
            if (nonNullable == typeof(ushort) && ushort.TryParse(text, NumberStyles.Any, provider, out ushort u16))
                return u16;
            if (nonNullable == typeof(uint) && uint.TryParse(text, NumberStyles.Any, provider, out uint u32))
                return u32;
            if (nonNullable == typeof(ulong) && ulong.TryParse(text, NumberStyles.Any, provider, out ulong u64))
                return u64;
            if (nonNullable == typeof(bool) && bool.TryParse(text, out bool b))
                return b;
            if (nonNullable == typeof(char) && text.Length == 1)
                return text[0];
            if (nonNullable == typeof(DateTime) && DateTime.TryParse(text, provider, DateTimeStyles.None, out DateTime dt))
                return dt;
            if (nonNullable == typeof(DateTimeOffset) && DateTimeOffset.TryParse(text, provider, DateTimeStyles.None, out DateTimeOffset dto))
                return dto;
            if (nonNullable == typeof(TimeSpan) && TimeSpan.TryParse(text, provider, out TimeSpan ts))
                return ts;
            if (nonNullable == typeof(Guid) && Guid.TryParse(text, out Guid guid))
                return guid;
            if (nonNullable == typeof(Uri) && Uri.TryCreate(text, UriKind.RelativeOrAbsolute, out Uri? uri))
                return uri;

            TypeConverter? tc = GetConverter(nonNullable);
            if (tc != null && tc.CanConvertFrom(typeof(string)))
            {
                try
                {
                    return tc.ConvertFrom(null, culture ?? CultureInfo.InvariantCulture, text);
                }
                catch
                {
                    return null;
                }
            }

            return null;
        }
    }
}

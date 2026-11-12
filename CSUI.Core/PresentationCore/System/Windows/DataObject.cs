// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Reflection.Metadata;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization.Metadata;
using System.Threading;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Ole;
using System.Windows.Threading;
using MS.Win32;
using SR = MS.Internal.PresentationCore.SR;

namespace System.Windows
{
    /// <summary>
    /// Provides a basic implementation of <see cref="IDataObject"/> and <see cref="ITypedDataObject"/>
    /// for data transfer via <see cref="Clipboard"/> and <see cref="DragDrop"/>.
    /// </summary>
    public sealed unsafe class DataObject : IDataObject, ITypedDataObject
    {
        #region Attached Routed Events

        /// <summary>Copying</summary>
        public static readonly RoutedEvent CopyingEvent = EventManager.RegisterRoutedEvent(
            "Copying",
            RoutingStrategy.Bubble,
            typeof(DataObjectCopyingEventHandler),
            typeof(DataObject));

        /// <summary>Adds a handler for the Copying attached event.</summary>
        public static void AddCopyingHandler(DependencyObject element, DataObjectCopyingEventHandler handler)
        {
            UIElement.AddHandler(element, CopyingEvent, handler);
        }

        /// <summary>Removes a handler for the Copying attached event.</summary>
        public static void RemoveCopyingHandler(DependencyObject element, DataObjectCopyingEventHandler handler)
        {
            UIElement.RemoveHandler(element, CopyingEvent, handler);
        }

        /// <summary>Pasting</summary>
        public static readonly RoutedEvent PastingEvent = EventManager.RegisterRoutedEvent(
            "Pasting",
            RoutingStrategy.Bubble,
            typeof(DataObjectPastingEventHandler),
            typeof(DataObject));

        /// <summary>Adds a handler for the Pasting attached event.</summary>
        public static void AddPastingHandler(DependencyObject element, DataObjectPastingEventHandler handler)
        {
            UIElement.AddHandler(element, PastingEvent, handler);
        }

        /// <summary>Removes a handler for the Pasting attached event.</summary>
        public static void RemovePastingHandler(DependencyObject element, DataObjectPastingEventHandler handler)
        {
            UIElement.RemoveHandler(element, PastingEvent, handler);
        }

        /// <summary>SettingData</summary>
        public static readonly RoutedEvent SettingDataEvent = EventManager.RegisterRoutedEvent(
            "SettingData",
            RoutingStrategy.Bubble,
            typeof(DataObjectSettingDataEventHandler),
            typeof(DataObject));

        /// <summary>Adds a handler for the SettingData attached event.</summary>
        public static void AddSettingDataHandler(DependencyObject element, DataObjectSettingDataEventHandler handler)
        {
            UIElement.AddHandler(element, SettingDataEvent, handler);
        }

        /// <summary>Removes a handler for the SettingData attached event.</summary>
        public static void RemoveSettingDataHandler(DependencyObject element, DataObjectSettingDataEventHandler handler)
        {
            UIElement.RemoveHandler(element, SettingDataEvent, handler);
        }

        #endregion

        #region Fields & Constructors

        private const string JsonPayloadPrefix = "{\"$csui_json\":true,\"payload\":";
        private static readonly string s_bitmapSourceTypeName = typeof(BitmapSource).FullName;

        private sealed class DataStoreEntry
        {
            public object Data;
            public bool AutoConvert;
            public byte[] JsonUtf8Payload;
        }

        private readonly Dictionary<string, DataStoreEntry> _store;
        private readonly IDataObject _externalManaged;
        private void* _nativeDataObject;
        private readonly Dispatcher _dispatcher;

        /// <summary>Initializes an empty <see cref="DataObject"/>.</summary>
        public DataObject()
        {
            _store = new Dictionary<string, DataStoreEntry>(StringComparer.Ordinal);
        }

        /// <summary>Initializes a <see cref="DataObject"/> containing the specified data.</summary>
        public DataObject(object data)
        {
            ArgumentNullException.ThrowIfNull(data);

            if (data is IDataObject dataObject && data.GetType() != typeof(DataObject))
            {
                _externalManaged = dataObject;
            }
            else
            {
                _store = new Dictionary<string, DataStoreEntry>(StringComparer.Ordinal);
                SetData(data);
            }
        }

        /// <summary>Initializes a <see cref="DataObject"/> containing data in the specified format.</summary>
        public DataObject(string format, object data)
            : this()
        {
            SetData(format, data);
        }

        /// <summary>Initializes a <see cref="DataObject"/> containing data of the specified type format.</summary>
        public DataObject(Type format, object data)
            : this()
        {
            SetData(format, data);
        }

        /// <summary>Initializes a <see cref="DataObject"/> with format, data, and auto-conversion setting.</summary>
        public DataObject(string format, object data, bool autoConvert)
            : this()
        {
            SetData(format, data, autoConvert);
        }

        internal DataObject(void* pNativeDataObject)
        {
            if (pNativeDataObject == null)
            {
                throw new ArgumentNullException(nameof(pNativeDataObject));
            }

            OleComBridge.AddRefCom(pNativeDataObject);
            _nativeDataObject = pNativeDataObject;
            _dispatcher = Dispatcher.FromThread(Thread.CurrentThread);
        }

        ~DataObject()
        {
            void* native;
            fixed (void** ppNative = &_nativeDataObject)
            {
                native = (void*)Interlocked.Exchange(ref *(nint*)ppNative, 0);
            }

            if (native != null)
            {
                Dispatcher dispatcher = _dispatcher;
                if (dispatcher != null && !dispatcher.HasShutdownStarted)
                {
                    dispatcher.BeginInvoke(DispatcherPriority.Normal, (Action<nint>)(static ptr =>
                    {
                        OleComBridge.ReleaseCom((void*)ptr);
                    }), (nint)native);
                }
                else
                {
                    OleComBridge.ReleaseCom(native);
                }
            }
        }

        #endregion

        #region COM Export Factory

        internal static void* CreateComDataObject(IDataObject dataObject)
        {
            ArgumentNullException.ThrowIfNull(dataObject);

            if (dataObject is DataObject d && d._nativeDataObject != null)
            {
                OleComBridge.AddRefCom(d._nativeDataObject);
                return d._nativeDataObject;
            }

            if (dataObject is DataObject d2 && d2._externalManaged != null)
            {
                dataObject = d2._externalManaged;
            }

            return OleComBridge.CreateDataObjectCom(new OleDataObjectAdapter(dataObject), dataObject);
        }

        #endregion

        #region IDataObject

        /// <inheritdoc/>
        public object GetData(string format) => GetData(format, autoConvert: true);

        /// <inheritdoc/>
        public object GetData(Type format)
        {
            ArgumentNullException.ThrowIfNull(format);
            return GetData(format.FullName, autoConvert: true);
        }

        /// <inheritdoc/>
        public object GetData(string format, bool autoConvert)
        {
            ValidateFormat(format);

            if (_externalManaged != null)
            {
                return _externalManaged.GetData(format, autoConvert);
            }

            if (_nativeDataObject != null)
            {
                return GetNativeData(format, autoConvert);
            }

            return GetManagedData(format, autoConvert);
        }

        /// <inheritdoc/>
        public bool GetDataPresent(string format) => GetDataPresent(format, autoConvert: true);

        /// <inheritdoc/>
        public bool GetDataPresent(Type format)
        {
            ArgumentNullException.ThrowIfNull(format);
            return GetDataPresent(format.FullName, autoConvert: true);
        }

        /// <inheritdoc/>
        public bool GetDataPresent(string format, bool autoConvert)
        {
            if (string.IsNullOrEmpty(format))
            {
                return false;
            }

            if (_externalManaged != null)
            {
                return _externalManaged.GetDataPresent(format, autoConvert);
            }

            if (_nativeDataObject != null)
            {
                return GetNativeDataPresent(format, autoConvert);
            }

            return GetManagedDataPresent(format, autoConvert);
        }

        /// <inheritdoc/>
        public string[] GetFormats() => GetFormats(autoConvert: true);

        /// <inheritdoc/>
        public string[] GetFormats(bool autoConvert)
        {
            if (_externalManaged != null)
            {
                return _externalManaged.GetFormats(autoConvert);
            }

            if (_nativeDataObject != null)
            {
                return GetNativeFormats(autoConvert);
            }

            return GetManagedFormats(autoConvert);
        }

        /// <inheritdoc/>
        public void SetData(object data)
        {
            ArgumentNullException.ThrowIfNull(data);

            if (data is IDataObject && _externalManaged == null && _store != null)
            {
                if (data is DataObject src && src._store != null)
                {
                    foreach (KeyValuePair<string, DataStoreEntry> kv in src._store)
                    {
                        _store[kv.Key] = new DataStoreEntry
                        {
                            Data = kv.Value.Data,
                            AutoConvert = kv.Value.AutoConvert,
                            JsonUtf8Payload = kv.Value.JsonUtf8Payload,
                        };
                    }
                    return;
                }
            }

            if (data is string)
            {
                SetData(DataFormats.UnicodeText, data, autoConvert: true);
            }
            else if (data is BitmapSource)
            {
                SetData(DataFormats.Bitmap, data, autoConvert: true);
            }
            else
            {
                SetData(data.GetType().FullName, data, autoConvert: true);
            }
        }

        /// <inheritdoc/>
        public void SetData(string format, object data)
            => SetData(format, data, autoConvert: true);

        /// <inheritdoc/>
        public void SetData(Type format, object data)
        {
            ArgumentNullException.ThrowIfNull(format);
            SetData(format.FullName, data, autoConvert: false);
        }

        /// <inheritdoc/>
        public void SetData(string format, object data, bool autoConvert)
        {
            ValidateFormat(format);
            ArgumentNullException.ThrowIfNull(data);

            if (_externalManaged != null)
            {
                _externalManaged.SetData(format, data, autoConvert);
                return;
            }

            if (_nativeDataObject != null)
            {
                throw new InvalidOperationException(SR.DataObject_CannotSetDataOnAFozenOLEDataDbject);
            }

            _store[format] = new DataStoreEntry
            {
                Data = data,
                AutoConvert = autoConvert,
            };
        }

        #endregion

        #region ITypedDataObject & JSON

        /// <inheritdoc/>
        public bool TryGetData<T>([NotNullWhen(true), MaybeNullWhen(false)] out T data)
        {
            if (typeof(T) == typeof(string))
            {
                return TryGetData(DataFormats.UnicodeText, resolver: null, autoConvert: true, out data);
            }

            if (typeof(T) == typeof(BitmapSource) || typeof(BitmapSource).IsAssignableFrom(typeof(T)))
            {
                return TryGetData(DataFormats.Bitmap, resolver: null, autoConvert: true, out data);
            }

            if (typeof(T) == typeof(StringCollection) || typeof(T) == typeof(string[]))
            {
                if (TryGetData(DataFormats.FileDrop, resolver: null, autoConvert: true, out data))
                {
                    return true;
                }
            }

            return TryGetData(typeof(T).FullName ?? typeof(T).Name, resolver: null, autoConvert: true, out data);
        }

        /// <inheritdoc/>
        public bool TryGetData<T>(string format, [NotNullWhen(true), MaybeNullWhen(false)] out T data)
            => TryGetData(format, resolver: null, autoConvert: true, out data);

        /// <inheritdoc/>
        public bool TryGetData<T>(string format, bool autoConvert, [NotNullWhen(true), MaybeNullWhen(false)] out T data)
            => TryGetData(format, resolver: null, autoConvert, out data);

        /// <inheritdoc/>
        public bool TryGetData<T>(
            string format,
            Func<TypeName, Type> resolver,
            bool autoConvert,
            [NotNullWhen(true), MaybeNullWhen(false)] out T data)
        {
            ValidateFormat(format);
            data = default;

            if (_externalManaged is ITypedDataObject externalTyped)
            {
                return externalTyped.TryGetData(format, resolver, autoConvert, out data);
            }

            if (_store != null && _store.TryGetValue(format, out DataStoreEntry entry) && entry.JsonUtf8Payload != null)
            {
                if (TryDeserializeBuiltInJson<T>(entry.JsonUtf8Payload, out data))
                {
                    return true;
                }
            }

            object raw = GetData(format, autoConvert);
            if (raw == null)
            {
                return false;
            }

            if (raw is T direct)
            {
                data = direct;
                return true;
            }

            if (typeof(T) == typeof(StringCollection) && raw is string[] fileArr)
            {
                StringCollection sc = new();
                sc.AddRange(fileArr);
                data = (T)(object)sc;
                return true;
            }

            if (typeof(T) == typeof(string[]) && raw is StringCollection scIn)
            {
                string[] arr = new string[scIn.Count];
                scIn.CopyTo(arr, 0);
                data = (T)(object)arr;
                return true;
            }

            if (raw is MemoryStream ms && TryDeserializeBuiltInJson<T>(ms.ToArray(), out data))
            {
                return true;
            }

            if (raw is byte[] bytes && TryDeserializeBuiltInJson<T>(bytes, out data))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Reads JSON data formatted with <see cref="SetDataAsJson{T}(string, T, JsonTypeInfo{T})"/> using NativeAOT source-generated metadata.
        /// </summary>
        public bool TryGetData<T>(string format, JsonTypeInfo<T> jsonTypeInfo, [NotNullWhen(true), MaybeNullWhen(false)] out T data)
        {
            ArgumentNullException.ThrowIfNull(jsonTypeInfo);
            ValidateFormat(format);
            data = default;

            if (_store != null && _store.TryGetValue(format, out DataStoreEntry entry))
            {
                if (entry.JsonUtf8Payload != null)
                {
                    return TryDeserializeWithTypeInfo(entry.JsonUtf8Payload, jsonTypeInfo, out data);
                }

                if (entry.Data is T direct)
                {
                    data = direct;
                    return true;
                }
            }

            object raw = GetData(format, autoConvert: false);
            if (raw is MemoryStream ms)
            {
                return TryDeserializeWithTypeInfo(ms.ToArray(), jsonTypeInfo, out data);
            }
            if (raw is byte[] bytes)
            {
                return TryDeserializeWithTypeInfo(bytes, jsonTypeInfo, out data);
            }
            if (raw is T typed)
            {
                data = typed;
                return true;
            }

            return false;
        }

        /// <inheritdoc cref="SetDataAsJson{T}(string, T)"/>
        public void SetDataAsJson<T>(T data)
        {
            ArgumentNullException.ThrowIfNull(data);
            SetDataAsJson(typeof(T).FullName ?? typeof(T).Name, data);
        }

        /// <summary>
        /// Stores JSON-serialized data for built-in primitive/JSON types without runtime reflection.
        /// For custom user types in NativeAOT, pass a source-generated <see cref="JsonTypeInfo{T}"/> overload.
        /// </summary>
        public void SetDataAsJson<T>(string format, T data)
        {
            ValidateFormat(format);
            ArgumentNullException.ThrowIfNull(data);

            if (_nativeDataObject != null)
            {
                throw new InvalidOperationException(SR.DataObject_CannotSetDataOnAFozenOLEDataDbject);
            }

            byte[] jsonBytes = SerializeBuiltInJson(data);
            MemoryStream payloadStream = new(jsonBytes, writable: false);
            _store[format] = new DataStoreEntry
            {
                Data = payloadStream,
                AutoConvert = false,
                JsonUtf8Payload = jsonBytes,
            };
        }

        /// <summary>
        /// Stores JSON-serialized data using an AOT-safe source-generated <see cref="JsonTypeInfo{T}"/>.
        /// </summary>
        public void SetDataAsJson<T>(string format, T data, JsonTypeInfo<T> jsonTypeInfo)
        {
            ValidateFormat(format);
            ArgumentNullException.ThrowIfNull(data);
            ArgumentNullException.ThrowIfNull(jsonTypeInfo);

            if (_nativeDataObject != null)
            {
                throw new InvalidOperationException(SR.DataObject_CannotSetDataOnAFozenOLEDataDbject);
            }

            byte[] rawPayload = JsonSerializer.SerializeToUtf8Bytes(data, jsonTypeInfo);
            byte[] prefix = Encoding.UTF8.GetBytes(JsonPayloadPrefix);
            byte[] wrapped = new byte[prefix.Length + rawPayload.Length + 1];
            prefix.CopyTo(wrapped, 0);
            rawPayload.CopyTo(wrapped, prefix.Length);
            wrapped[wrapped.Length - 1] = (byte)'}';

            _store[format] = new DataStoreEntry
            {
                Data = new MemoryStream(wrapped, writable: false),
                AutoConvert = false,
                JsonUtf8Payload = wrapped,
            };
        }

        #endregion

        #region Typed Format Convenience APIs

        /// <summary>Determines whether the data object contains WaveAudio data.</summary>
        public bool ContainsAudio() => GetDataPresent(DataFormats.WaveAudio, autoConvert: false);

        /// <summary>Determines whether the data object contains a FileDrop list.</summary>
        public bool ContainsFileDropList() => GetDataPresent(DataFormats.FileDrop, autoConvert: false);

        /// <summary>Determines whether the data object contains an image.</summary>
        public bool ContainsImage() => GetDataPresent(DataFormats.Bitmap, autoConvert: true);

        /// <summary>Determines whether the data object contains Unicode text.</summary>
        public bool ContainsText() => ContainsText(TextDataFormat.UnicodeText);

        /// <summary>Determines whether the data object contains text in the specified format.</summary>
        public bool ContainsText(TextDataFormat format)
        {
            if (!DataFormats.IsValidTextDataFormat(format))
            {
                throw new System.ComponentModel.InvalidEnumArgumentException(nameof(format), (int)format, typeof(TextDataFormat));
            }

            return GetDataPresent(DataFormats.ConvertToDataFormats(format), autoConvert: true);
        }

        /// <summary>Retrieves an audio stream from the data object.</summary>
        public Stream GetAudioStream() => GetData(DataFormats.WaveAudio, autoConvert: false) as Stream;

        /// <summary>Retrieves a collection of file names from the data object.</summary>
        public StringCollection GetFileDropList()
        {
            StringCollection list = new();
            object data = GetData(DataFormats.FileDrop, autoConvert: true);
            if (data is string[] files)
            {
                list.AddRange(files);
            }
            else if (data is StringCollection sc)
            {
                foreach (string s in sc)
                {
                    list.Add(s);
                }
            }
            return list;
        }

        /// <summary>Retrieves an image from the data object.</summary>
        public BitmapSource GetImage() => GetData(DataFormats.Bitmap, autoConvert: true) as BitmapSource;

        /// <summary>Retrieves Unicode text from the data object.</summary>
        public string GetText() => GetText(TextDataFormat.UnicodeText);

        /// <summary>Retrieves text in the specified format from the data object.</summary>
        public string GetText(TextDataFormat format)
        {
            if (!DataFormats.IsValidTextDataFormat(format))
            {
                throw new System.ComponentModel.InvalidEnumArgumentException(nameof(format), (int)format, typeof(TextDataFormat));
            }

            string text = GetData(DataFormats.ConvertToDataFormats(format), autoConvert: true) as string;
            return text ?? string.Empty;
        }

        /// <summary>Stores audio bytes into the data object.</summary>
        public void SetAudio(byte[] audioBytes)
        {
            ArgumentNullException.ThrowIfNull(audioBytes);
            SetAudio(new MemoryStream(audioBytes));
        }

        /// <summary>Stores an audio stream into the data object.</summary>
        public void SetAudio(Stream audioStream)
        {
            ArgumentNullException.ThrowIfNull(audioStream);
            SetData(DataFormats.WaveAudio, audioStream, autoConvert: false);
        }

        /// <summary>Stores a file drop list into the data object.</summary>
        public void SetFileDropList(StringCollection fileDropList)
        {
            ArgumentNullException.ThrowIfNull(fileDropList);
            if (fileDropList.Count == 0)
            {
                throw new ArgumentException(SR.Format(SR.DataObject_FileDropListIsEmpty, nameof(fileDropList)));
            }

            string[] files = new string[fileDropList.Count];
            for (int i = 0; i < fileDropList.Count; i++)
            {
                string path = fileDropList[i];
                if (string.IsNullOrEmpty(path))
                {
                    throw new ArgumentException(SR.Format(SR.DataObject_FileDropListHasInvalidFileDropPath, i));
                }
                files[i] = Path.GetFullPath(path);
            }

            SetData(DataFormats.FileDrop, files, autoConvert: true);
        }

        /// <summary>Stores a <see cref="BitmapSource"/> image into the data object.</summary>
        public void SetImage(BitmapSource image)
        {
            ArgumentNullException.ThrowIfNull(image);
            SetData(DataFormats.Bitmap, image, autoConvert: true);
        }

        /// <summary>Stores Unicode text into the data object.</summary>
        public void SetText(string textData) => SetText(textData, TextDataFormat.UnicodeText);

        /// <summary>Stores text in the specified format into the data object.</summary>
        public void SetText(string textData, TextDataFormat format)
        {
            ArgumentNullException.ThrowIfNull(textData);
            if (!DataFormats.IsValidTextDataFormat(format))
            {
                throw new System.ComponentModel.InvalidEnumArgumentException(nameof(format), (int)format, typeof(TextDataFormat));
            }

            SetData(DataFormats.ConvertToDataFormats(format), textData, autoConvert: true);
        }

        #endregion

        #region Managed Data Store Lookup & AutoConvert

        private object GetManagedData(string format, bool autoConvert)
        {
            if (_store.TryGetValue(format, out DataStoreEntry direct))
            {
                return direct.Data;
            }

            if (!autoConvert)
            {
                return null;
            }

            string[] mapped = GetMappedFormats(format);
            for (int i = 0; i < mapped.Length; i++)
            {
                if (_store.TryGetValue(mapped[i], out DataStoreEntry candidate) && candidate.AutoConvert)
                {
                    return candidate.Data;
                }
            }

            return null;
        }

        private bool GetManagedDataPresent(string format, bool autoConvert)
        {
            if (_store.ContainsKey(format))
            {
                return true;
            }

            if (!autoConvert)
            {
                return false;
            }

            string[] mapped = GetMappedFormats(format);
            for (int i = 0; i < mapped.Length; i++)
            {
                if (_store.TryGetValue(mapped[i], out DataStoreEntry candidate) && candidate.AutoConvert)
                {
                    return true;
                }
            }

            return false;
        }

        private string[] GetManagedFormats(bool autoConvert)
        {
            if (_store.Count == 0)
            {
                return Array.Empty<string>();
            }

            HashSet<string> result = new(_store.Count, StringComparer.Ordinal);
            foreach (KeyValuePair<string, DataStoreEntry> kv in _store)
            {
                result.Add(kv.Key);
                if (autoConvert && kv.Value.AutoConvert)
                {
                    string[] mapped = GetMappedFormats(kv.Key);
                    for (int i = 0; i < mapped.Length; i++)
                    {
                        result.Add(mapped[i]);
                    }
                }
            }

            string[] formats = new string[result.Count];
            result.CopyTo(formats);
            return formats;
        }

        private static string[] GetMappedFormats(string format)
        {
            if (string.Equals(format, DataFormats.Text, StringComparison.Ordinal) ||
                string.Equals(format, DataFormats.UnicodeText, StringComparison.Ordinal) ||
                string.Equals(format, DataFormats.OemText, StringComparison.Ordinal) ||
                string.Equals(format, DataFormats.StringFormat, StringComparison.Ordinal))
            {
                return new[]
                {
                    DataFormats.UnicodeText,
                    DataFormats.Text,
                    DataFormats.OemText,
                    DataFormats.StringFormat,
                };
            }

            if (string.Equals(format, DataFormats.Bitmap, StringComparison.Ordinal) ||
                string.Equals(format, DataFormats.Dib, StringComparison.Ordinal) ||
                string.Equals(format, s_bitmapSourceTypeName, StringComparison.Ordinal))
            {
                return new[]
                {
                    DataFormats.Bitmap,
                    DataFormats.Dib,
                    s_bitmapSourceTypeName,
                };
            }

            return Array.Empty<string>();
        }

        #endregion

        #region Foreign Native OLE IDataObject Client

        private bool GetNativeDataPresent(string format, bool autoConvert)
        {
            if (QueryNativeFormat(format))
            {
                return true;
            }

            if (!autoConvert)
            {
                return false;
            }

            string[] mapped = GetMappedFormats(format);
            for (int i = 0; i < mapped.Length; i++)
            {
                if (QueryNativeFormat(mapped[i]))
                {
                    return true;
                }
            }

            return false;
        }

        private bool QueryNativeFormat(string format)
        {
            DataFormat df = DataFormats.GetDataFormat(format);
            FORMATETC fmt = CreateFormatEtc(df.Id, format);
            return OleComBridge.QueryGetData(_nativeDataObject, &fmt) == OleComBridge.S_OK;
        }

        private string[] GetNativeFormats(bool autoConvert)
        {
            void* pEnum = null;
            int hr = OleComBridge.EnumFormatEtc(_nativeDataObject, (uint)DATADIR.DATADIR_GET, &pEnum);
            if (hr < 0 || pEnum == null)
            {
                return Array.Empty<string>();
            }

            try
            {
                OleComBridge.ResetFormatEtc(pEnum);
                HashSet<string> result = new(StringComparer.Ordinal);
                FORMATETC fmt = default;
                uint fetched = 0;
                while (OleComBridge.NextFormatEtc(pEnum, 1, &fmt, &fetched) == OleComBridge.S_OK && fetched == 1)
                {
                    string name = DataFormats.GetDataFormat(fmt.cfFormat).Name;
                    result.Add(name);
                    if (autoConvert)
                    {
                        string[] mapped = GetMappedFormats(name);
                        for (int i = 0; i < mapped.Length; i++)
                        {
                            result.Add(mapped[i]);
                        }
                    }
                    fetched = 0;
                }

                string[] arr = new string[result.Count];
                result.CopyTo(arr);
                return arr;
            }
            finally
            {
                OleComBridge.ReleaseCom(pEnum);
            }
        }

        private object GetNativeData(string format, bool autoConvert)
        {
            object data = TryGetNativeDataCore(format);
            if (data != null || !autoConvert)
            {
                return data;
            }

            string[] mapped = GetMappedFormats(format);
            for (int i = 0; i < mapped.Length; i++)
            {
                if (string.Equals(mapped[i], format, StringComparison.Ordinal))
                {
                    continue;
                }

                data = TryGetNativeDataCore(mapped[i]);
                if (data != null)
                {
                    return data;
                }
            }

            return null;
        }

        private object TryGetNativeDataCore(string format)
        {
            DataFormat df = DataFormats.GetDataFormat(format);
            FORMATETC fmt = CreateFormatEtc(df.Id, format);
            STGMEDIUM med = default;

            int hr = OleComBridge.GetData(_nativeDataObject, &fmt, &med);
            if (hr < 0)
            {
                return null;
            }

            try
            {
                if (med.tymed == (uint)TYMED.TYMED_GDI && med.unionValue != IntPtr.Zero)
                {
                    return Imaging.CreateBitmapSourceFromHBitmap(med.unionValue, IntPtr.Zero, Int32Rect.Empty, null);
                }

                if (med.tymed == (uint)TYMED.TYMED_HGLOBAL && med.unionValue != IntPtr.Zero)
                {
                    int id = df.Id & 0xFFFF;
                    if (id == NativeMethods.CF_UNICODETEXT)
                    {
                        return OleComBridge.ReadUnicodeStringFromHGlobal(med.unionValue);
                    }

                    if (id == NativeMethods.CF_TEXT || id == NativeMethods.CF_OEMTEXT)
                    {
                        return OleComBridge.ReadAnsiOrUtf8StringFromHGlobal(med.unionValue, Encoding.Latin1);
                    }

                    if (id == NativeMethods.CF_HDROP)
                    {
                        return OleComBridge.ReadFileDropListFromHDrop(med.unionValue);
                    }

                    if (id == NativeMethods.CF_DIB)
                    {
                        byte[] dibBytes = OleComBridge.ReadBytesFromHGlobal(med.unionValue);
                        return OleComBridge.DecodeBitmapSourceFromDibBytes(dibBytes);
                    }

                    if (string.Equals(format, DataFormats.Html, StringComparison.Ordinal) ||
                        string.Equals(format, DataFormats.Rtf, StringComparison.Ordinal) ||
                        string.Equals(format, DataFormats.CommaSeparatedValue, StringComparison.Ordinal) ||
                        string.Equals(format, DataFormats.Xaml, StringComparison.Ordinal))
                    {
                        return OleComBridge.ReadAnsiOrUtf8StringFromHGlobal(med.unionValue, Encoding.UTF8);
                    }

                    byte[] bytes = OleComBridge.ReadBytesFromHGlobal(med.unionValue);
                    return new MemoryStream(bytes);
                }

                if (med.tymed == (uint)TYMED.TYMED_ISTREAM && med.unionValue != IntPtr.Zero)
                {
                    byte[] bytes = OleComBridge.ReadBytesFromIStream((void*)med.unionValue);
                    if (string.Equals(format, DataFormats.Html, StringComparison.Ordinal) ||
                        string.Equals(format, DataFormats.Rtf, StringComparison.Ordinal) ||
                        string.Equals(format, DataFormats.CommaSeparatedValue, StringComparison.Ordinal) ||
                        string.Equals(format, DataFormats.Xaml, StringComparison.Ordinal))
                    {
                        return Encoding.UTF8.GetString(bytes);
                    }
                    return new MemoryStream(bytes);
                }

                return null;
            }
            finally
            {
                UnsafeNativeMethods.ReleaseStgMedium(&med);
            }
        }

        private static FORMATETC CreateFormatEtc(int formatId, string formatName)
        {
            uint tymed = (uint)(TYMED.TYMED_HGLOBAL | TYMED.TYMED_ISTREAM);
            if ((formatId & 0xFFFF) == NativeMethods.CF_BITMAP ||
                string.Equals(formatName, DataFormats.Bitmap, StringComparison.Ordinal))
            {
                tymed |= (uint)TYMED.TYMED_GDI;
            }

            return new FORMATETC
            {
                cfFormat = unchecked((ushort)(formatId & 0xFFFF)),
                ptd = 0,
                dwAspect = (uint)DVASPECT.DVASPECT_CONTENT,
                lindex = -1,
                tymed = tymed,
            };
        }

        #endregion

        #region NativeAOT Zero-Reflection JSON Helpers

        private static byte[] SerializeBuiltInJson<T>(T value)
        {
            using MemoryStream ms = new();
            using (Utf8JsonWriter writer = new(ms))
            {
                writer.WriteStartObject();
                writer.WriteBoolean("$csui_json", true);
                writer.WritePropertyName("payload");
                WriteJsonValue(writer, value);
                writer.WriteEndObject();
            }
            return ms.ToArray();
        }

        private static void WriteJsonValue<T>(Utf8JsonWriter writer, T value)
        {
            switch (value)
            {
                case null:
                    writer.WriteNullValue();
                    break;
                case string s:
                    writer.WriteStringValue(s);
                    break;
                case bool b:
                    writer.WriteBooleanValue(b);
                    break;
                case byte v:
                    writer.WriteNumberValue(v);
                    break;
                case sbyte v:
                    writer.WriteNumberValue(v);
                    break;
                case short v:
                    writer.WriteNumberValue(v);
                    break;
                case ushort v:
                    writer.WriteNumberValue(v);
                    break;
                case int v:
                    writer.WriteNumberValue(v);
                    break;
                case uint v:
                    writer.WriteNumberValue(v);
                    break;
                case long v:
                    writer.WriteNumberValue(v);
                    break;
                case ulong v:
                    writer.WriteNumberValue(v);
                    break;
                case float v:
                    writer.WriteNumberValue(v);
                    break;
                case double v:
                    writer.WriteNumberValue(v);
                    break;
                case decimal v:
                    writer.WriteNumberValue(v);
                    break;
                case DateTime dt:
                    writer.WriteStringValue(dt);
                    break;
                case DateTimeOffset dto:
                    writer.WriteStringValue(dto);
                    break;
                case Guid g:
                    writer.WriteStringValue(g);
                    break;
                case JsonElement el:
                    el.WriteTo(writer);
                    break;
                case JsonNode node:
                    node.WriteTo(writer);
                    break;
                case JsonDocument doc:
                    doc.RootElement.WriteTo(writer);
                    break;
                case string[] strings:
                    writer.WriteStartArray();
                    for (int i = 0; i < strings.Length; i++)
                    {
                        writer.WriteStringValue(strings[i]);
                    }
                    writer.WriteEndArray();
                    break;
                case int[] ints:
                    writer.WriteStartArray();
                    for (int i = 0; i < ints.Length; i++)
                    {
                        writer.WriteNumberValue(ints[i]);
                    }
                    writer.WriteEndArray();
                    break;
                default:
                    throw new NotSupportedException($"SetDataAsJson without JsonTypeInfo supports built-in primitive/JSON types; pass JsonTypeInfo<{typeof(T).Name}> for NativeAOT.");
            }
        }

        private static bool TryDeserializeBuiltInJson<T>(byte[] utf8Json, out T value)
        {
            value = default;
            if (utf8Json == null || utf8Json.Length == 0 || utf8Json[0] != (byte)'{')
            {
                return false;
            }

            try
            {
                using JsonDocument doc = JsonDocument.Parse(utf8Json);
                if (!doc.RootElement.TryGetProperty("$csui_json", out JsonElement marker) ||
                    marker.ValueKind != JsonValueKind.True ||
                    !doc.RootElement.TryGetProperty("payload", out JsonElement payload))
                {
                    return false;
                }

                if (typeof(T) == typeof(JsonElement))
                {
                    value = (T)(object)payload.Clone();
                    return true;
                }

                if (typeof(T) == typeof(string) && payload.ValueKind == JsonValueKind.String)
                {
                    value = (T)(object)payload.GetString();
                    return true;
                }

                if (typeof(T) == typeof(bool) && (payload.ValueKind == JsonValueKind.True || payload.ValueKind == JsonValueKind.False))
                {
                    value = (T)(object)payload.GetBoolean();
                    return true;
                }

                if (typeof(T) == typeof(int) && payload.TryGetInt32(out int i32))
                {
                    value = (T)(object)i32;
                    return true;
                }

                if (typeof(T) == typeof(long) && payload.TryGetInt64(out long i64))
                {
                    value = (T)(object)i64;
                    return true;
                }

                if (typeof(T) == typeof(double) && payload.TryGetDouble(out double dbl))
                {
                    value = (T)(object)dbl;
                    return true;
                }

                if (typeof(T) == typeof(decimal) && payload.TryGetDecimal(out decimal dec))
                {
                    value = (T)(object)dec;
                    return true;
                }

                if (typeof(T) == typeof(DateTime) && payload.TryGetDateTime(out DateTime dt))
                {
                    value = (T)(object)dt;
                    return true;
                }

                if (typeof(T) == typeof(DateTimeOffset) && payload.TryGetDateTimeOffset(out DateTimeOffset dto))
                {
                    value = (T)(object)dto;
                    return true;
                }

                if (typeof(T) == typeof(Guid) && payload.TryGetGuid(out Guid g))
                {
                    value = (T)(object)g;
                    return true;
                }

                if (typeof(T) == typeof(string[]) && payload.ValueKind == JsonValueKind.Array)
                {
                    string[] arr = new string[payload.GetArrayLength()];
                    int idx = 0;
                    foreach (JsonElement item in payload.EnumerateArray())
                    {
                        arr[idx++] = item.GetString();
                    }
                    value = (T)(object)arr;
                    return true;
                }

                if (typeof(T) == typeof(int[]) && payload.ValueKind == JsonValueKind.Array)
                {
                    int[] arr = new int[payload.GetArrayLength()];
                    int idx = 0;
                    foreach (JsonElement item in payload.EnumerateArray())
                    {
                        arr[idx++] = item.GetInt32();
                    }
                    value = (T)(object)arr;
                    return true;
                }
            }
            catch (JsonException)
            {
                return false;
            }

            return false;
        }

        private static bool TryDeserializeWithTypeInfo<T>(byte[] utf8Json, JsonTypeInfo<T> typeInfo, out T value)
        {
            value = default;
            if (utf8Json == null || utf8Json.Length == 0)
            {
                return false;
            }

            try
            {
                using JsonDocument doc = JsonDocument.Parse(utf8Json);
                if (doc.RootElement.ValueKind == JsonValueKind.Object &&
                    doc.RootElement.TryGetProperty("$csui_json", out JsonElement marker) &&
                    marker.ValueKind == JsonValueKind.True &&
                    doc.RootElement.TryGetProperty("payload", out JsonElement payload))
                {
                    value = JsonSerializer.Deserialize(payload.GetRawText(), typeInfo);
                    return value != null;
                }

                value = JsonSerializer.Deserialize(utf8Json, typeInfo);
                return value != null;
            }
            catch (JsonException)
            {
                return false;
            }
        }

        #endregion

        #region OLE Export COM Adapter

        private sealed class OleDataObjectAdapter : INativeOleDataObject
        {
            private readonly IDataObject _dataObject;

            public OleDataObjectAdapter(IDataObject dataObject)
            {
                _dataObject = dataObject;
            }

            public int EnumFormatEtc(uint direction, out void* ppenumFormatEtc)
            {
                ppenumFormatEtc = null;
                if (direction != (uint)DATADIR.DATADIR_GET)
                {
                    return OleComBridge.E_NOTIMPL;
                }

                string[] formats = _dataObject.GetFormats(autoConvert: true) ?? Array.Empty<string>();
                List<FORMATETC> list = new(formats.Length);
                HashSet<ushort> seen = new();

                for (int i = 0; i < formats.Length; i++)
                {
                    if (string.IsNullOrEmpty(formats[i]))
                    {
                        continue;
                    }

                    DataFormat df = DataFormats.GetDataFormat(formats[i]);
                    ushort cf = unchecked((ushort)(df.Id & 0xFFFF));
                    if (!seen.Add(cf))
                    {
                        continue;
                    }

                    list.Add(CreateFormatEtc(df.Id, formats[i]));
                }

                ppenumFormatEtc = OleComBridge.CreateEnumFormatEtcCom(list.ToArray());
                return OleComBridge.S_OK;
            }

            public int QueryGetData(ref FORMATETC format)
            {
                if (format.dwAspect != (uint)DVASPECT.DVASPECT_CONTENT)
                {
                    return OleComBridge.DV_E_DVASPECT;
                }

                if (format.lindex != -1)
                {
                    return OleComBridge.DV_E_LINDEX;
                }

                DataFormat df = DataFormats.GetDataFormat(format.cfFormat);
                if (!_dataObject.GetDataPresent(df.Name, autoConvert: true))
                {
                    return OleComBridge.DV_E_FORMATETC;
                }

                uint supportedTymed = (uint)(TYMED.TYMED_HGLOBAL | TYMED.TYMED_ISTREAM);
                if (format.cfFormat == NativeMethods.CF_BITMAP)
                {
                    supportedTymed |= (uint)TYMED.TYMED_GDI;
                }

                if ((format.tymed & supportedTymed) == 0)
                {
                    return OleComBridge.DV_E_TYMED;
                }

                return OleComBridge.S_OK;
            }

            public int GetCanonicalFormatEtc(ref FORMATETC formatIn, out FORMATETC formatOut)
            {
                formatOut = formatIn;
                formatOut.ptd = 0;
                return OleComBridge.E_NOTIMPL;
            }

            public int GetData(ref FORMATETC format, out STGMEDIUM medium)
            {
                medium = default;

                int query = QueryGetData(ref format);
                if (query != OleComBridge.S_OK)
                {
                    return query;
                }

                DataFormat df = DataFormats.GetDataFormat(format.cfFormat);
                object data = _dataObject.GetData(df.Name, autoConvert: true);
                if (data == null)
                {
                    return OleComBridge.DV_E_FORMATETC;
                }

                if (format.cfFormat == NativeMethods.CF_BITMAP &&
                    (format.tymed & (uint)TYMED.TYMED_GDI) != 0 &&
                    data is BitmapSource bmpSource)
                {
                    IntPtr hBitmap = OleComBridge.AllocHBitmapFromBitmapSource(bmpSource);
                    medium.tymed = (uint)TYMED.TYMED_GDI;
                    medium.unionValue = hBitmap;
                    medium.pUnkForRelease = null;
                    return OleComBridge.S_OK;
                }

                if ((format.tymed & (uint)TYMED.TYMED_ISTREAM) != 0 &&
                    (format.tymed & (uint)TYMED.TYMED_HGLOBAL) == 0 &&
                    data is Stream streamOnly)
                {
                    if (streamOnly.CanSeek)
                    {
                        streamOnly.Position = 0;
                    }
                    medium.tymed = (uint)TYMED.TYMED_ISTREAM;
                    medium.unionValue = StreamAsIStream.IStreamFrom(streamOnly);
                    medium.pUnkForRelease = null;
                    return OleComBridge.S_OK;
                }

                if ((format.tymed & (uint)TYMED.TYMED_HGLOBAL) != 0)
                {
                    IntPtr hMem = MarshalObjectToHGlobal(format.cfFormat, df.Name, data);
                    if (hMem != IntPtr.Zero)
                    {
                        medium.tymed = (uint)TYMED.TYMED_HGLOBAL;
                        medium.unionValue = hMem;
                        medium.pUnkForRelease = null;
                        return OleComBridge.S_OK;
                    }
                }

                return OleComBridge.DV_E_TYMED;
            }

            public int GetDataHere(ref FORMATETC format, ref STGMEDIUM medium)
            {
                return OleComBridge.E_NOTIMPL;
            }

            public int SetData(ref FORMATETC format, ref STGMEDIUM medium, bool fRelease)
            {
                return OleComBridge.E_NOTIMPL;
            }

            private static IntPtr MarshalObjectToHGlobal(ushort cfFormat, string formatName, object data)
            {
                if (cfFormat == NativeMethods.CF_UNICODETEXT && data is string unicodeStr)
                {
                    return OleComBridge.AllocHGlobalFromUnicodeString(unicodeStr);
                }

                if ((cfFormat == NativeMethods.CF_TEXT || cfFormat == NativeMethods.CF_OEMTEXT) && data is string ansiStr)
                {
                    return OleComBridge.AllocHGlobalFromBytesWithNull(Encoding.Latin1.GetBytes(ansiStr));
                }

                if (cfFormat == NativeMethods.CF_HDROP)
                {
                    StringCollection sc = new();
                    if (data is string[] arr)
                    {
                        sc.AddRange(arr);
                    }
                    else if (data is StringCollection existing)
                    {
                        foreach (string s in existing)
                        {
                            sc.Add(s);
                        }
                    }
                    return OleComBridge.AllocHGlobalFromFileDropList(sc);
                }

                if (cfFormat == NativeMethods.CF_DIB && data is BitmapSource dibBmp)
                {
                    return OleComBridge.AllocDibHGlobalFromBitmapSource(dibBmp);
                }

                if (data is string textPayload)
                {
                    if (string.Equals(formatName, DataFormats.StringFormat, StringComparison.Ordinal))
                    {
                        return OleComBridge.AllocHGlobalFromUnicodeString(textPayload);
                    }
                    return OleComBridge.AllocHGlobalFromBytesWithNull(Encoding.UTF8.GetBytes(textPayload));
                }

                if (data is byte[] bytePayload)
                {
                    return OleComBridge.AllocHGlobalFromBytes(bytePayload);
                }

                if (data is MemoryStream msPayload)
                {
                    return OleComBridge.AllocHGlobalFromBytes(msPayload.ToArray());
                }

                if (data is Stream streamPayload)
                {
                    if (streamPayload.CanSeek)
                    {
                        streamPayload.Position = 0;
                    }
                    using MemoryStream copy = new();
                    streamPayload.CopyTo(copy);
                    return OleComBridge.AllocHGlobalFromBytes(copy.ToArray());
                }

                if (data is int i32)
                {
                    Span<byte> buf = stackalloc byte[4];
                    Unsafe.WriteUnaligned(ref buf[0], i32);
                    return OleComBridge.AllocHGlobalFromBytes(buf);
                }

                return IntPtr.Zero;
            }
        }

        #endregion

        private static void ValidateFormat(string format)
        {
            ArgumentNullException.ThrowIfNull(format);
            if (format.Length == 0)
            {
                throw new ArgumentException(SR.DataObject_EmptyFormatNotAllowed, nameof(format));
            }
        }
    }
}

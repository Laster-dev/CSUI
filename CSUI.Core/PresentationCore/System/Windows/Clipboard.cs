// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Reflection.Metadata;
using System.Runtime.InteropServices;
using System.Text.Json.Serialization.Metadata;
using System.Threading;
using System.Windows.Media.Imaging;
using MS.Win32;
using SR = MS.Internal.PresentationCore.SR;

namespace System.Windows
{
    /// <summary>
    /// Provides static methods to transfer data to and from the Windows system clipboard.
    /// </summary>
    public static class Clipboard
    {
        private const int OleRetryCount = 10;
        private const int OleRetryDelayMilliseconds = 100;

        /// <summary>Clears any data from the system Clipboard.</summary>
        public static void Clear()
        {
            int retry = OleRetryCount;
            while (true)
            {
                int hr = OleServicesContext.CurrentOleServicesContext.OleSetClipboard(null);
                if (NativeMethods.Succeeded(hr))
                {
                    break;
                }

                if (--retry == 0)
                {
                    Marshal.ThrowExceptionForHR(hr);
                }

                Thread.Sleep(OleRetryDelayMilliseconds);
            }
        }

        /// <summary>Queries the Clipboard for the presence of WaveAudio data.</summary>
        public static bool ContainsAudio() => ContainsDataInternal(DataFormats.WaveAudio);

        /// <summary>Queries the Clipboard for the presence of data in the specified format.</summary>
        public static bool ContainsData(string format)
        {
            ArgumentNullException.ThrowIfNull(format);
            if (format.Length == 0)
            {
                throw new ArgumentException(SR.DataObject_EmptyFormatNotAllowed, nameof(format));
            }

            return ContainsDataInternal(format);
        }

        /// <summary>Queries the Clipboard for the presence of a FileDrop list.</summary>
        public static bool ContainsFileDropList() => ContainsDataInternal(DataFormats.FileDrop);

        /// <summary>Queries the Clipboard for the presence of image data.</summary>
        public static bool ContainsImage() => ContainsDataInternal(DataFormats.Bitmap);

        /// <summary>Queries the Clipboard for the presence of Unicode text.</summary>
        public static bool ContainsText() => ContainsText(TextDataFormat.UnicodeText);

        /// <summary>Queries the Clipboard for the presence of text in the specified format.</summary>
        public static bool ContainsText(TextDataFormat format)
        {
            if (!DataFormats.IsValidTextDataFormat(format))
            {
                throw new InvalidEnumArgumentException(nameof(format), (int)format, typeof(TextDataFormat));
            }

            return ContainsDataInternal(DataFormats.ConvertToDataFormats(format));
        }

        /// <summary>Renders clipboard data statically so it remains available after shutdown.</summary>
        public static void Flush()
        {
            int retry = OleRetryCount;
            while (true)
            {
                int hr = OleServicesContext.CurrentOleServicesContext.OleFlushClipboard();
                if (NativeMethods.Succeeded(hr))
                {
                    break;
                }

                if (--retry == 0)
                {
                    Marshal.ThrowExceptionForHR(hr);
                }

                Thread.Sleep(OleRetryDelayMilliseconds);
            }
        }

        /// <summary>Retrieves an audio stream from the Clipboard.</summary>
        public static Stream GetAudioStream() => GetDataInternal(DataFormats.WaveAudio) as Stream;

        /// <summary>Retrieves data in the specified format from the Clipboard.</summary>
        public static object GetData(string format)
        {
            ArgumentNullException.ThrowIfNull(format);
            if (format.Length == 0)
            {
                throw new ArgumentException(SR.DataObject_EmptyFormatNotAllowed, nameof(format));
            }

            return GetDataInternal(format);
        }

        /// <summary>Retrieves the <see cref="IDataObject"/> currently on the system Clipboard.</summary>
        public static IDataObject GetDataObject()
        {
            IDataObject dataObject = null;
            int retry = OleRetryCount;
            while (true)
            {
                int hr = OleServicesContext.CurrentOleServicesContext.OleGetClipboard(ref dataObject);
                if (NativeMethods.Succeeded(hr))
                {
                    break;
                }

                if (--retry == 0)
                {
                    Marshal.ThrowExceptionForHR(hr);
                }

                Thread.Sleep(OleRetryDelayMilliseconds);
            }

            return dataObject;
        }

        /// <summary>Retrieves a collection of dropped file paths from the Clipboard.</summary>
        public static StringCollection GetFileDropList()
        {
            StringCollection list = new();
            if (GetDataInternal(DataFormats.FileDrop) is string[] files)
            {
                list.AddRange(files);
            }
            return list;
        }

        /// <summary>Retrieves an image from the Clipboard.</summary>
        public static BitmapSource GetImage()
        {
            System.Runtime.CompilerServices.RuntimeHelpers.RunClassConstructor(typeof(BitmapSource).TypeHandle);
            return GetDataInternal(DataFormats.Bitmap) as BitmapSource;
        }

        /// <summary>Retrieves Unicode text from the Clipboard.</summary>
        public static string GetText() => GetText(TextDataFormat.UnicodeText);

        /// <summary>Retrieves text in the specified format from the Clipboard.</summary>
        public static string GetText(TextDataFormat format)
        {
            if (!DataFormats.IsValidTextDataFormat(format))
            {
                throw new InvalidEnumArgumentException(nameof(format), (int)format, typeof(TextDataFormat));
            }

            return GetDataInternal(DataFormats.ConvertToDataFormats(format)) as string ?? string.Empty;
        }

        /// <summary>Determines whether the specified data object is currently on the Clipboard.</summary>
        public static bool IsCurrent(IDataObject data)
        {
            ArgumentNullException.ThrowIfNull(data);
            return OleServicesContext.CurrentOleServicesContext.OleIsCurrentClipboard(data) == 0;
        }

        /// <summary>Places audio bytes onto the Clipboard.</summary>
        public static void SetAudio(byte[] audioBytes)
        {
            ArgumentNullException.ThrowIfNull(audioBytes);
            SetAudio(new MemoryStream(audioBytes));
        }

        /// <summary>Places an audio stream onto the Clipboard.</summary>
        public static void SetAudio(Stream audioStream)
        {
            ArgumentNullException.ThrowIfNull(audioStream);
            SetDataInternal(DataFormats.WaveAudio, audioStream);
        }

        /// <summary>Places data of the specified format onto the Clipboard.</summary>
        public static void SetData(string format, object data)
        {
            ArgumentNullException.ThrowIfNull(format);
            if (format.Length == 0)
            {
                throw new ArgumentException(SR.DataObject_EmptyFormatNotAllowed, nameof(format));
            }
            ArgumentNullException.ThrowIfNull(data);

            SetDataInternal(format, data);
        }

        /// <summary>Places JSON-serialized data onto the Clipboard.</summary>
        public static void SetDataAsJson<T>(string format, T data)
        {
            ArgumentNullException.ThrowIfNull(format);
            if (format.Length == 0)
            {
                throw new ArgumentException(SR.DataObject_EmptyFormatNotAllowed, nameof(format));
            }
            ArgumentNullException.ThrowIfNull(data);

            DataObject dataObject = new();
            dataObject.SetDataAsJson(format, data);
            SetDataObject(dataObject, copy: true);
        }

        /// <summary>Places JSON-serialized data onto the Clipboard using NativeAOT source-generated metadata.</summary>
        public static void SetDataAsJson<T>(string format, T data, JsonTypeInfo<T> jsonTypeInfo)
        {
            ArgumentNullException.ThrowIfNull(format);
            if (format.Length == 0)
            {
                throw new ArgumentException(SR.DataObject_EmptyFormatNotAllowed, nameof(format));
            }
            ArgumentNullException.ThrowIfNull(data);
            ArgumentNullException.ThrowIfNull(jsonTypeInfo);

            DataObject dataObject = new();
            dataObject.SetDataAsJson(format, data, jsonTypeInfo);
            SetDataObject(dataObject, copy: true);
        }

        /// <summary>Places the specified non-persistent data object onto the Clipboard.</summary>
        public static void SetDataObject(object data) => SetDataObject(data, copy: false);

        /// <summary>Places the specified data object onto the Clipboard.</summary>
        public static void SetDataObject(object data, bool copy)
        {
            ArgumentNullException.ThrowIfNull(data);

            IDataObject dataObject = data as IDataObject ?? new DataObject(data);

            int retry = OleRetryCount;
            while (true)
            {
                int hr = OleServicesContext.CurrentOleServicesContext.OleSetClipboard(dataObject);
                if (NativeMethods.Succeeded(hr))
                {
                    break;
                }

                if (--retry == 0)
                {
                    Marshal.ThrowExceptionForHR(hr);
                }

                Thread.Sleep(OleRetryDelayMilliseconds);
            }

            if (copy)
            {
                Thread.Sleep(10);
                Flush();
            }
        }

        /// <summary>Places a file drop list onto the Clipboard.</summary>
        public static void SetFileDropList(StringCollection fileDropList)
        {
            ArgumentNullException.ThrowIfNull(fileDropList);
            DataObject dataObject = new();
            dataObject.SetFileDropList(fileDropList);
            SetDataObject(dataObject, copy: true);
        }

        /// <summary>Places an image onto the Clipboard.</summary>
        public static void SetImage(BitmapSource image)
        {
            ArgumentNullException.ThrowIfNull(image);
            SetDataInternal(DataFormats.Bitmap, image);
        }

        /// <summary>Places Unicode text onto the Clipboard.</summary>
        public static void SetText(string text) => SetText(text, TextDataFormat.UnicodeText);

        /// <summary>Places text of the specified format onto the Clipboard.</summary>
        public static void SetText(string text, TextDataFormat format)
        {
            ArgumentNullException.ThrowIfNull(text);
            if (!DataFormats.IsValidTextDataFormat(format))
            {
                throw new InvalidEnumArgumentException(nameof(format), (int)format, typeof(TextDataFormat));
            }

            SetDataInternal(DataFormats.ConvertToDataFormats(format), text);
        }

        /// <summary>Attempts to retrieve data of type <typeparamref name="T"/> from the Clipboard.</summary>
        public static bool TryGetData<T>([NotNullWhen(true), MaybeNullWhen(false)] out T data)
        {
            if (GetDataObject() is IDataObject dataObject)
            {
                return dataObject.TryGetData(out data);
            }

            data = default;
            return false;
        }

        /// <summary>Attempts to retrieve data of type <typeparamref name="T"/> in the specified format from the Clipboard.</summary>
        public static bool TryGetData<T>(string format, [NotNullWhen(true), MaybeNullWhen(false)] out T data)
        {
            if (GetDataObject() is IDataObject dataObject)
            {
                return dataObject.TryGetData(format, out data);
            }

            data = default;
            return false;
        }

        /// <summary>Attempts to retrieve data of type <typeparamref name="T"/> in the specified format from the Clipboard.</summary>
        public static bool TryGetData<T>(string format, bool autoConvert, [NotNullWhen(true), MaybeNullWhen(false)] out T data)
        {
            if (GetDataObject() is IDataObject dataObject)
            {
                return dataObject.TryGetData(format, autoConvert, out data);
            }

            data = default;
            return false;
        }

        /// <summary>Attempts to retrieve data of type <typeparamref name="T"/> with an explicit type resolver.</summary>
        public static bool TryGetData<T>(
            string format,
            Func<TypeName, Type> resolver,
            bool autoConvert,
            [NotNullWhen(true), MaybeNullWhen(false)] out T data)
        {
            if (GetDataObject() is IDataObject dataObject)
            {
                return dataObject.TryGetData(format, resolver, autoConvert, out data);
            }

            data = default;
            return false;
        }

        /// <summary>Attempts to retrieve JSON data using a source-generated <see cref="JsonTypeInfo{T}"/>.</summary>
        public static bool TryGetData<T>(string format, JsonTypeInfo<T> jsonTypeInfo, [NotNullWhen(true), MaybeNullWhen(false)] out T data)
        {
            if (GetDataObject() is DataObject dataObject)
            {
                return dataObject.TryGetData(format, jsonTypeInfo, out data);
            }

            data = default;
            return false;
        }

        private static bool ContainsDataInternal(string format)
        {
            IDataObject dataObject = GetDataObject();
            return dataObject != null && dataObject.GetDataPresent(format, autoConvert: IsDataFormatAutoConvert(format));
        }

        private static object GetDataInternal(string format)
        {
            IDataObject dataObject = GetDataObject();
            return dataObject?.GetData(format, autoConvert: IsDataFormatAutoConvert(format));
        }

        private static void SetDataInternal(string format, object data)
        {
            DataObject dataObject = new();
            dataObject.SetData(format, data, autoConvert: IsDataFormatAutoConvert(format));
            SetDataObject(dataObject, copy: true);
        }

        private static bool IsDataFormatAutoConvert(string format)
        {
            return string.Equals(format, DataFormats.Text, StringComparison.Ordinal) ||
                   string.Equals(format, DataFormats.UnicodeText, StringComparison.Ordinal) ||
                   string.Equals(format, DataFormats.OemText, StringComparison.Ordinal) ||
                   string.Equals(format, DataFormats.StringFormat, StringComparison.Ordinal) ||
                   string.Equals(format, DataFormats.Bitmap, StringComparison.Ordinal) ||
                   string.Equals(format, DataFormats.Dib, StringComparison.Ordinal);
        }
    }
}

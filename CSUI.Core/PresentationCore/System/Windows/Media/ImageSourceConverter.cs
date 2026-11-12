// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Markup;
using System.Windows.Media.Imaging;
using MS.Internal;
using SR = MS.Internal.PresentationCore.SR;

namespace System.Windows.Media
{
    /// <summary>
    /// ImageSourceConverter (zero-reflection NativeAOT implementation)
    /// </summary>
    public class ImageSourceConverter : TypeConverter
    {
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            if (sourceType == typeof(string) || sourceType == typeof(Stream) || sourceType == typeof(Uri) || sourceType == typeof(byte[]))
            {
                return true;
            }

            return base.CanConvertFrom(context, sourceType);
        }

        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
        {
            if (destinationType == typeof(string))
            {
                if (context != null && context.Instance != null)
                {
                    if (!(context.Instance is ImageSource))
                    {
                        throw new ArgumentException(SR.Format(SR.General_Expected_Type, "ImageSource"), nameof(context));
                    }

                    ImageSource value = (ImageSource)context.Instance;
                    return value.CanSerializeToString();
                }

                return true;
            }

            return base.CanConvertTo(context, destinationType);
        }

        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            try
            {
                if (value == null)
                {
                    throw GetConvertFromException(value);
                }

                if (((value is string) && (!string.IsNullOrEmpty((string)value))) || (value is Uri))
                {
                    UriHolder uriHolder = TypeConverterHelper.GetUriFromUriContext(context, value);
                    return BitmapFrame.CreateFromUriOrStream(
                        uriHolder.BaseUri,
                        uriHolder.OriginalUri,
                        null,
                        BitmapCreateOptions.None,
                        BitmapCacheOption.Default,
                        null);
                }
                else if (value is byte[] bytes)
                {
                    if (bytes != null)
                    {
                        Stream memStream = GetBitmapStream(bytes) ?? new MemoryStream(bytes);

                        return BitmapFrame.Create(
                            memStream,
                            BitmapCreateOptions.None,
                            BitmapCacheOption.Default);
                    }
                }
                else if (value is Stream stream)
                {
                    return BitmapFrame.Create(
                        stream,
                        BitmapCreateOptions.None,
                        BitmapCacheOption.Default);
                }

                return base.ConvertFrom(context, culture, value);
            }
            catch (Exception e)
            {
                if (!CriticalExceptions.IsCriticalException(e))
                {
                    if (context == null && CoreAppContextSwitches.OverrideExceptionWithNullReferenceException)
                    {
                        throw new NullReferenceException();
                    }

                    IProvideValueTarget ipvt = context?.GetService(typeof(IProvideValueTarget)) as IProvideValueTarget;
                    if (ipvt != null)
                    {
                        IProvidePropertyFallback ippf = ipvt.TargetObject as IProvidePropertyFallback;
                        DependencyProperty dp = ipvt.TargetProperty as DependencyProperty;

                        if (ippf != null && dp != null && ippf.CanProvidePropertyFallback(dp.Name))
                        {
                            return ippf.ProvidePropertyFallback(dp.Name, e);
                        }
                    }
                }

                throw;
            }
        }

        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
        {
            if (destinationType != null && value is ImageSource instance)
            {
                if (destinationType == typeof(string))
                {
                    if (context != null && context.Instance != null)
                    {
                        if (!instance.CanSerializeToString())
                        {
                            throw new NotSupportedException(SR.Converter_ConvertToNotSupported);
                        }
                    }

                    return instance.ConvertToString(null, culture);
                }
            }

            return base.ConvertTo(context, culture, value, destinationType);
        }

        /// <summary>
        /// Try to get a bitmap out of a byte array encoded in the Access OLE OBJECTHEADER format.
        /// Uses direct unmanaged pointer read instead of reflection-based Marshal.PtrToStructure.
        /// </summary>
        private unsafe Stream GetBitmapStream(byte[] rawData)
        {
            Debug.Assert(rawData != null, "rawData is null.");
            if (rawData.Length < sizeof(OBJECTHEADER))
            {
                return null;
            }

            fixed (byte* pByte = rawData)
            {
                if (pByte == null)
                {
                    return null;
                }

                if (*(short*)pByte != 0x1c15)
                {
                    return null;
                }

                OBJECTHEADER header = *(OBJECTHEADER*)pByte;
                if (header.headersize < 0 || rawData.Length < header.headersize + 18)
                {
                    return null;
                }

                string strPBrush = Encoding.ASCII.GetString(rawData, header.headersize + 12, 6);
                if (strPBrush != "PBrush")
                {
                    return null;
                }

                int searchEnd = Math.Min(header.headersize + 510, rawData.Length - 1);
                for (int i = header.headersize + 18; i < searchEnd; i++)
                {
                    if (pByte[i] == (byte)'B' && pByte[i + 1] == (byte)'M')
                    {
                        return new MemoryStream(rawData, i, rawData.Length - i);
                    }
                }
            }

            return null;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct OBJECTHEADER
        {
            public short signature;
            public short headersize;
            public short objectType;
            public short nameLen;
            public short classLen;
            public short nameOffset;
            public short classOffset;
            public short width;
            public short height;
            public IntPtr pInfo;
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.Security;
using CSUI.Core.Interop;
using CSUI.Core.Interop.DirectX;
using MS.Internal;
using SR = MS.Internal.PresentationCore.SR;

namespace System.Windows.Media
{
    internal enum PixelFormatEnum
    {
        Default = 0,
        Extended = Default,
        Indexed1 = 1,
        Indexed2 = 2,
        Indexed4 = 3,
        Indexed8 = 4,
        BlackWhite = 5,
        Gray2 = 6,
        Gray4 = 7,
        Gray8 = 8,
        Bgr555 = 9,
        Bgr565 = 10,
        Gray16 = 11,
        Bgr24 = 12,
        Rgb24 = 13,
        Bgr32 = 14,
        Bgra32 = 15,
        Pbgra32 = 16,
        Gray32Float = 17,
        Bgr101010 = 18,
        Rgb48 = 19,
        Rgba64 = 20,
        Prgba64 = 21,
        Rgba128Float = 22,
        Prgba128Float = 23,
        Rgb128Float = 24,
        Cmyk32 = 25,
    }

    [Flags]
    internal enum PixelFormatFlags
    {
        BitsPerPixelUndefined = 0,
        BitsPerPixel1 = 1 << 0,
        BitsPerPixel2 = 1 << 1,
        BitsPerPixel4 = 1 << 2,
        BitsPerPixel8 = 1 << 3,
        BitsPerPixel16 = 1 << 4,
        BitsPerPixel24 = 1 << 5,
        BitsPerPixel32 = 1 << 6,
        BitsPerPixel48 = 1 << 7,
        BitsPerPixel64 = 1 << 8,
        BitsPerPixel96 = 1 << 9,
        BitsPerPixel128 = 1 << 10,
        BitsPerPixelMask = 0x7FF,

        IsGray = 1 << 11,
        IsCMYK = 1 << 12,
        IsSRGB = 1 << 13,
        IsScRGB = 1 << 14,
        Premultiplied = 1 << 15,
        ChannelOrderRGB = 1 << 16,
        ChannelOrderBGR = 1 << 17,
        Palettized = 1 << 18,
        NChannel = 1 << 19,
        ChannelOrderABGR = 1 << 20,
        ChannelOrderARGB = 1 << 21,
    }

    /// <summary>
    /// Defines a bit mask and shift for a specific channel in a pixel format.
    /// </summary>
    public struct PixelFormatChannelMask
    {
        internal PixelFormatChannelMask(byte[] mask)
        {
            _mask = mask;
        }

        public IList<byte> Mask => _mask != null ? new ReadOnlyCollection<byte>((byte[])_mask.Clone()) : null;

        public static bool operator ==(PixelFormatChannelMask left, PixelFormatChannelMask right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(PixelFormatChannelMask left, PixelFormatChannelMask right)
        {
            return !(left == right);
        }

        public static bool Equals(PixelFormatChannelMask left, PixelFormatChannelMask right)
        {
            int leftLength = left._mask != null ? left._mask.Length : 0;
            int rightLength = right._mask != null ? right._mask.Length : 0;

            if (leftLength != rightLength)
            {
                return false;
            }

            for (int i = 0; i < leftLength; i++)
            {
                if (left._mask[i] != right._mask[i])
                {
                    return false;
                }
            }

            return true;
        }

        public override bool Equals(object obj)
        {
            return obj is PixelFormatChannelMask other && Equals(this, other);
        }

        public override int GetHashCode()
        {
            int hash = 0;
            if (_mask != null)
            {
                for (int i = 0; i < _mask.Length; i++)
                {
                    hash = (hash * 31) ^ _mask[i];
                }
            }
            return hash;
        }

        private readonly byte[] _mask;
    }

    /// <summary>
    /// Defines a pixel format for images and bitmap-based surfaces.
    /// </summary>
    [Serializable]
    public unsafe struct PixelFormat : IEquatable<PixelFormat>
    {
        internal PixelFormat(Guid guidPixelFormat)
        {
            Guid dontCare = WICPixelFormatGUIDs.WICPixelFormatDontCare;
            byte[] guidBytes = guidPixelFormat.ToByteArray();
            byte[] dontCareBytes = dontCare.ToByteArray();

            bool matchesPrefix = true;
            for (int i = 0; i < 15; i++)
            {
                if (guidBytes[i] != dontCareBytes[i])
                {
                    matchesPrefix = false;
                    break;
                }
            }

            if (matchesPrefix && guidBytes[15] <= (byte)PixelFormatEnum.Cmyk32)
            {
                _format = GetPixelFormatEnumFromWicByte(guidBytes[15]);
                _guidFormat = new NonSerializedGuid(guidPixelFormat);
                _flags = CreatePixelFormatFlagsFromEnum(_format);
                _bitsPerPixel = GetBitsPerPixelFromEnum(_format);
            }
            else
            {
                _format = PixelFormatEnum.Extended;
                _guidFormat = new NonSerializedGuid(guidPixelFormat);
                _flags = PixelFormatFlags.BitsPerPixelUndefined;
                _bitsPerPixel = GetBitsPerPixelFromWicGuid(guidPixelFormat);
            }
        }

        internal PixelFormat(PixelFormatEnum format)
        {
            _format = format;
            _flags = CreatePixelFormatFlagsFromEnum(format);
            _bitsPerPixel = GetBitsPerPixelFromEnum(format);
            _guidFormat = new NonSerializedGuid(GetGuidFromFormat(format));
        }

        internal PixelFormat(string pixelFormatString)
        {
            ArgumentNullException.ThrowIfNull(pixelFormatString);

            PixelFormatEnum format = PixelFormatEnum.Default;
            string upperFormat = pixelFormatString.ToUpperInvariant();

            switch (upperFormat)
            {
                case "EXTENDED":
                case "DEFAULT":
                    format = PixelFormatEnum.Default;
                    break;
                case "INDEXED1":
                    format = PixelFormatEnum.Indexed1;
                    break;
                case "INDEXED2":
                    format = PixelFormatEnum.Indexed2;
                    break;
                case "INDEXED4":
                    format = PixelFormatEnum.Indexed4;
                    break;
                case "INDEXED8":
                    format = PixelFormatEnum.Indexed8;
                    break;
                case "BLACKWHITE":
                    format = PixelFormatEnum.BlackWhite;
                    break;
                case "GRAY2":
                    format = PixelFormatEnum.Gray2;
                    break;
                case "GRAY4":
                    format = PixelFormatEnum.Gray4;
                    break;
                case "GRAY8":
                    format = PixelFormatEnum.Gray8;
                    break;
                case "BGR555":
                    format = PixelFormatEnum.Bgr555;
                    break;
                case "BGR565":
                    format = PixelFormatEnum.Bgr565;
                    break;
                case "GRAY16":
                    format = PixelFormatEnum.Gray16;
                    break;
                case "BGR24":
                    format = PixelFormatEnum.Bgr24;
                    break;
                case "RGB24":
                    format = PixelFormatEnum.Rgb24;
                    break;
                case "BGR32":
                    format = PixelFormatEnum.Bgr32;
                    break;
                case "BGRA32":
                    format = PixelFormatEnum.Bgra32;
                    break;
                case "PBGRA32":
                    format = PixelFormatEnum.Pbgra32;
                    break;
                case "GRAY32FLOAT":
                    format = PixelFormatEnum.Gray32Float;
                    break;
                case "BGR101010":
                    format = PixelFormatEnum.Bgr101010;
                    break;
                case "RGB48":
                    format = PixelFormatEnum.Rgb48;
                    break;
                case "RGBA64":
                    format = PixelFormatEnum.Rgba64;
                    break;
                case "PRGBA64":
                    format = PixelFormatEnum.Prgba64;
                    break;
                case "RGBA128FLOAT":
                    format = PixelFormatEnum.Rgba128Float;
                    break;
                case "PRGBA128FLOAT":
                    format = PixelFormatEnum.Prgba128Float;
                    break;
                case "RGB128FLOAT":
                    format = PixelFormatEnum.Rgb128Float;
                    break;
                case "CMYK32":
                    format = PixelFormatEnum.Cmyk32;
                    break;
                default:
                {
                    int hr = WicNativeExports.WICMapShortNameToGuid(pixelFormatString, out Guid formatGuid);
                    if (hr < 0)
                    {
                        throw new ArgumentException(SR.Format(SR.Image_BadPixelFormat, pixelFormatString), nameof(pixelFormatString));
                    }

                    this = new PixelFormat(formatGuid);
                    return;
                }
            }

            _format = format;
            _flags = CreatePixelFormatFlagsFromEnum(format);
            _bitsPerPixel = GetBitsPerPixelFromEnum(format);
            _guidFormat = new NonSerializedGuid(GetGuidFromFormat(format));
        }

        public static bool operator ==(PixelFormat left, PixelFormat right)
        {
            return left.Guid == right.Guid;
        }

        public static bool operator !=(PixelFormat left, PixelFormat right)
        {
            return left.Guid != right.Guid;
        }

        public static bool Equals(PixelFormat left, PixelFormat right)
        {
            return left.Guid == right.Guid;
        }

        public bool Equals(PixelFormat pixelFormat)
        {
            return this == pixelFormat;
        }

        public override bool Equals(object obj)
        {
            return obj is PixelFormat other && this == other;
        }

        public override int GetHashCode()
        {
            return Guid.GetHashCode();
        }

        public int BitsPerPixel => InternalBitsPerPixel;

        public IList<PixelFormatChannelMask> Masks
        {
            get
            {
                using SafeMILHandle pixelFormatInfo = CreatePixelFormatInfo();
                void* pInfo = (void*)pixelFormatInfo.DangerousGetHandle();

                HRESULT.Check(IWICPixelFormatInfoVtbl.GetChannelCount(pInfo, out uint channelCount));
                var masks = new PixelFormatChannelMask[channelCount];

                for (uint i = 0; i < channelCount; i++)
                {
                    uint cbActual = 0;
                    int hr = IWICPixelFormatInfoVtbl.GetChannelMask(pInfo, i, 0, null, out cbActual);
                    if (hr != (int)WinCodecErrors.WINCODEC_ERR_INSUFFICIENTBUFFER && hr < 0)
                    {
                        HRESULT.Check(hr);
                    }

                    byte[] channelMask = cbActual > 0 ? new byte[cbActual] : Array.Empty<byte>();
                    if (cbActual > 0)
                    {
                        fixed (byte* pMask = channelMask)
                        {
                            HRESULT.Check(IWICPixelFormatInfoVtbl.GetChannelMask(pInfo, i, cbActual, pMask, out cbActual));
                        }
                    }

                    masks[i] = new PixelFormatChannelMask(channelMask);
                }

                return new ReadOnlyCollection<PixelFormatChannelMask>(masks);
            }
        }

        internal SafeMILHandle CreatePixelFormatInfo()
        {
            Guid guid = Guid;
            using var factoryMaker = new FactoryMaker();
            void* factoryPtr = (void*)factoryMaker.ImagingFactoryPtr;

            void* componentInfo = null;
            void* pixelFormatInfo = null;
            try
            {
                HRESULT.Check(IWICImagingFactoryExVtbl.CreateComponentInfo(factoryPtr, guid, &componentInfo));
                HRESULT.Check(ComPtr<byte>.QueryInterface(componentInfo, MILGuidData.IID_IWICPixelFormatInfo, &pixelFormatInfo));
                var result = new SafeMILHandle((IntPtr)pixelFormatInfo);
                pixelFormatInfo = null;
                return result;
            }
            finally
            {
                if (pixelFormatInfo != null) ComHelpers.Release(pixelFormatInfo);
                if (componentInfo != null) ComHelpers.Release(componentInfo);
            }
        }

        internal int InternalBitsPerPixel
        {
            get
            {
                if (_bitsPerPixel == 0)
                {
                    _bitsPerPixel = (uint)GetBitsPerPixelFromWicGuid(Guid);
                }

                return (int)_bitsPerPixel;
            }
        }

        internal bool HasAlpha
        {
            get
            {
                return _format == PixelFormatEnum.Bgra32 ||
                       _format == PixelFormatEnum.Pbgra32 ||
                       _format == PixelFormatEnum.Rgba64 ||
                       _format == PixelFormatEnum.Prgba64 ||
                       _format == PixelFormatEnum.Rgba128Float ||
                       _format == PixelFormatEnum.Prgba128Float ||
                       ((_flags & PixelFormatFlags.ChannelOrderARGB) != 0) ||
                       ((_flags & PixelFormatFlags.ChannelOrderABGR) != 0);
            }
        }

        internal bool Palettized => (_flags & PixelFormatFlags.Palettized) != 0;

        internal PixelFormatFlags FormatFlags => _flags;

        internal PixelFormatEnum Format => _format;

        internal Guid Guid
        {
            get
            {
                Guid guid = _guidFormat.Value;
                if (guid == Guid.Empty)
                {
                    guid = GetGuidFromFormat(_format);
                }
                return guid;
            }
        }

        public override string ToString()
        {
            if (_format != PixelFormatEnum.Extended)
            {
                return _format.ToString();
            }

            Guid guid = Guid;
            int hr = WicNativeExports.WICMapGuidToShortName(ref guid, out string shortName);
            if (hr >= 0 && !string.IsNullOrEmpty(shortName))
            {
                return shortName;
            }

            return guid.ToString();
        }

        internal static PixelFormat GetPixelFormat(Guid pixelFormatGuid)
        {
            return new PixelFormat(pixelFormatGuid);
        }

        internal static unsafe PixelFormat GetPixelFormat(SafeMILHandle bitmapSource)
        {
            if (bitmapSource == null || bitmapSource.IsInvalid)
            {
                return PixelFormats.Default;
            }

            Guid fmt = Guid.Empty;
            HRESULT.Check(IWICBitmapSourceVtbl.GetPixelFormat((void*)bitmapSource.DangerousGetHandle(), out fmt));
            return new PixelFormat(fmt);
        }

        private static uint GetBitsPerPixelFromWicGuid(Guid guid)
        {
            if (guid == Guid.Empty || guid == WICPixelFormatGUIDs.WICPixelFormatDontCare)
            {
                return 0;
            }

            using var factoryMaker = new FactoryMaker();
            void* factoryPtr = (void*)factoryMaker.ImagingFactoryPtr;
            void* componentInfo = null;
            void* pixelFormatInfo = null;
            try
            {
                if (IWICImagingFactoryExVtbl.CreateComponentInfo(factoryPtr, guid, &componentInfo) < 0 || componentInfo == null)
                {
                    return 0;
                }

                if (ComPtr<byte>.QueryInterface(componentInfo, MILGuidData.IID_IWICPixelFormatInfo, &pixelFormatInfo) < 0 || pixelFormatInfo == null)
                {
                    return 0;
                }

                HRESULT.Check(IWICPixelFormatInfoVtbl.GetBitsPerPixel(pixelFormatInfo, out uint bpp));
                return bpp;
            }
            finally
            {
                if (pixelFormatInfo != null) ComHelpers.Release(pixelFormatInfo);
                if (componentInfo != null) ComHelpers.Release(componentInfo);
            }
        }

        private static PixelFormatEnum GetPixelFormatEnumFromWicByte(byte lastByte)
        {
            return lastByte switch
            {
                0x00 => PixelFormatEnum.Default,
                0x01 => PixelFormatEnum.Indexed1,
                0x02 => PixelFormatEnum.Indexed2,
                0x03 => PixelFormatEnum.Indexed4,
                0x04 => PixelFormatEnum.Indexed8,
                0x05 => PixelFormatEnum.BlackWhite,
                0x06 => PixelFormatEnum.Gray2,
                0x07 => PixelFormatEnum.Gray4,
                0x08 => PixelFormatEnum.Gray8,
                0x09 => PixelFormatEnum.Bgr555,
                0x0a => PixelFormatEnum.Bgr565,
                0x0b => PixelFormatEnum.Gray16,
                0x0c => PixelFormatEnum.Bgr24,
                0x0d => PixelFormatEnum.Rgb24,
                0x0e => PixelFormatEnum.Bgr32,
                0x0f => PixelFormatEnum.Bgra32,
                0x10 => PixelFormatEnum.Pbgra32,
                0x11 => PixelFormatEnum.Gray32Float,
                0x14 => PixelFormatEnum.Bgr101010,
                0x15 => PixelFormatEnum.Rgb48,
                0x16 => PixelFormatEnum.Rgba64,
                0x17 => PixelFormatEnum.Prgba64,
                0x19 => PixelFormatEnum.Rgba128Float,
                0x1a => PixelFormatEnum.Prgba128Float,
                0x1b => PixelFormatEnum.Rgb128Float,
                0x1c => PixelFormatEnum.Cmyk32,
                _ => PixelFormatEnum.Extended,
            };
        }

        private static Guid GetGuidFromFormat(PixelFormatEnum format)
        {
            return format switch
            {
                PixelFormatEnum.Default => WICPixelFormatGUIDs.WICPixelFormatDontCare,
                PixelFormatEnum.Indexed1 => WICPixelFormatGUIDs.WICPixelFormat1bppIndexed,
                PixelFormatEnum.Indexed2 => WICPixelFormatGUIDs.WICPixelFormat2bppIndexed,
                PixelFormatEnum.Indexed4 => WICPixelFormatGUIDs.WICPixelFormat4bppIndexed,
                PixelFormatEnum.Indexed8 => WICPixelFormatGUIDs.WICPixelFormat8bppIndexed,
                PixelFormatEnum.BlackWhite => WICPixelFormatGUIDs.WICPixelFormatBlackWhite,
                PixelFormatEnum.Gray2 => WICPixelFormatGUIDs.WICPixelFormat2bppGray,
                PixelFormatEnum.Gray4 => WICPixelFormatGUIDs.WICPixelFormat4bppGray,
                PixelFormatEnum.Gray8 => WICPixelFormatGUIDs.WICPixelFormat8bppGray,
                PixelFormatEnum.Bgr555 => WICPixelFormatGUIDs.WICPixelFormat16bppBGR555,
                PixelFormatEnum.Bgr565 => WICPixelFormatGUIDs.WICPixelFormat16bppBGR565,
                PixelFormatEnum.Bgr24 => WICPixelFormatGUIDs.WICPixelFormat24bppBGR,
                PixelFormatEnum.Rgb24 => WICPixelFormatGUIDs.WICPixelFormat24bppRGB,
                PixelFormatEnum.Bgr101010 => WICPixelFormatGUIDs.WICPixelFormat32bppBGR101010,
                PixelFormatEnum.Bgr32 => WICPixelFormatGUIDs.WICPixelFormat32bppBGR,
                PixelFormatEnum.Bgra32 => WICPixelFormatGUIDs.WICPixelFormat32bppBGRA,
                PixelFormatEnum.Pbgra32 => WICPixelFormatGUIDs.WICPixelFormat32bppPBGRA,
                PixelFormatEnum.Rgb48 => WICPixelFormatGUIDs.WICPixelFormat48bppRGB,
                PixelFormatEnum.Rgba64 => WICPixelFormatGUIDs.WICPixelFormat64bppRGBA,
                PixelFormatEnum.Prgba64 => WICPixelFormatGUIDs.WICPixelFormat64bppPRGBA,
                PixelFormatEnum.Gray16 => WICPixelFormatGUIDs.WICPixelFormat16bppGray,
                PixelFormatEnum.Gray32Float => WICPixelFormatGUIDs.WICPixelFormat32bppGrayFloat,
                PixelFormatEnum.Rgb128Float => WICPixelFormatGUIDs.WICPixelFormat128bppRGBFloat,
                PixelFormatEnum.Rgba128Float => WICPixelFormatGUIDs.WICPixelFormat128bppRGBAFloat,
                PixelFormatEnum.Prgba128Float => WICPixelFormatGUIDs.WICPixelFormat128bppPRGBAFloat,
                PixelFormatEnum.Cmyk32 => WICPixelFormatGUIDs.WICPixelFormat32bppCMYK,
                _ => throw new ArgumentException(SR.Format(SR.Image_BadPixelFormat, format), nameof(format)),
            };
        }

        private static uint GetBitsPerPixelFromEnum(PixelFormatEnum format)
        {
            return format switch
            {
                PixelFormatEnum.Default => 0,
                PixelFormatEnum.Indexed1 => 1,
                PixelFormatEnum.Indexed2 => 2,
                PixelFormatEnum.Indexed4 => 4,
                PixelFormatEnum.Indexed8 => 8,
                PixelFormatEnum.BlackWhite => 1,
                PixelFormatEnum.Gray2 => 2,
                PixelFormatEnum.Gray4 => 4,
                PixelFormatEnum.Gray8 => 8,
                PixelFormatEnum.Bgr555 => 16,
                PixelFormatEnum.Bgr565 => 16,
                PixelFormatEnum.Gray16 => 16,
                PixelFormatEnum.Bgr24 => 24,
                PixelFormatEnum.Rgb24 => 24,
                PixelFormatEnum.Bgr32 => 32,
                PixelFormatEnum.Bgra32 => 32,
                PixelFormatEnum.Pbgra32 => 32,
                PixelFormatEnum.Gray32Float => 32,
                PixelFormatEnum.Bgr101010 => 32,
                PixelFormatEnum.Rgb48 => 48,
                PixelFormatEnum.Rgba64 => 64,
                PixelFormatEnum.Prgba64 => 64,
                PixelFormatEnum.Rgba128Float => 128,
                PixelFormatEnum.Prgba128Float => 128,
                PixelFormatEnum.Rgb128Float => 128,
                PixelFormatEnum.Cmyk32 => 32,
                _ => 0,
            };
        }

        private static PixelFormatFlags CreatePixelFormatFlagsFromEnum(PixelFormatEnum pixelFormatEnum)
        {
            return pixelFormatEnum switch
            {
                PixelFormatEnum.Default => PixelFormatFlags.BitsPerPixelUndefined,
                PixelFormatEnum.Indexed1 => PixelFormatFlags.BitsPerPixel1 | PixelFormatFlags.Palettized,
                PixelFormatEnum.Indexed2 => PixelFormatFlags.BitsPerPixel2 | PixelFormatFlags.Palettized,
                PixelFormatEnum.Indexed4 => PixelFormatFlags.BitsPerPixel4 | PixelFormatFlags.Palettized,
                PixelFormatEnum.Indexed8 => PixelFormatFlags.BitsPerPixel8 | PixelFormatFlags.Palettized,
                PixelFormatEnum.BlackWhite => PixelFormatFlags.BitsPerPixel1 | PixelFormatFlags.IsGray,
                PixelFormatEnum.Gray2 => PixelFormatFlags.BitsPerPixel2 | PixelFormatFlags.IsGray,
                PixelFormatEnum.Gray4 => PixelFormatFlags.BitsPerPixel4 | PixelFormatFlags.IsGray,
                PixelFormatEnum.Gray8 => PixelFormatFlags.BitsPerPixel8 | PixelFormatFlags.IsGray,
                PixelFormatEnum.Bgr555 => PixelFormatFlags.BitsPerPixel16 | PixelFormatFlags.IsSRGB | PixelFormatFlags.ChannelOrderBGR,
                PixelFormatEnum.Bgr565 => PixelFormatFlags.BitsPerPixel16 | PixelFormatFlags.IsSRGB | PixelFormatFlags.ChannelOrderBGR,
                PixelFormatEnum.Gray16 => PixelFormatFlags.BitsPerPixel16 | PixelFormatFlags.IsScRGB | PixelFormatFlags.IsGray,
                PixelFormatEnum.Bgr24 => PixelFormatFlags.BitsPerPixel24 | PixelFormatFlags.IsSRGB | PixelFormatFlags.ChannelOrderBGR,
                PixelFormatEnum.Rgb24 => PixelFormatFlags.BitsPerPixel24 | PixelFormatFlags.IsSRGB | PixelFormatFlags.ChannelOrderRGB,
                PixelFormatEnum.Bgr32 => PixelFormatFlags.BitsPerPixel32 | PixelFormatFlags.IsSRGB | PixelFormatFlags.ChannelOrderBGR,
                PixelFormatEnum.Bgra32 => PixelFormatFlags.BitsPerPixel32 | PixelFormatFlags.IsSRGB | PixelFormatFlags.ChannelOrderBGR,
                PixelFormatEnum.Pbgra32 => PixelFormatFlags.BitsPerPixel32 | PixelFormatFlags.IsSRGB | PixelFormatFlags.Premultiplied | PixelFormatFlags.ChannelOrderBGR,
                PixelFormatEnum.Gray32Float => PixelFormatFlags.BitsPerPixel32 | PixelFormatFlags.IsScRGB | PixelFormatFlags.IsGray,
                PixelFormatEnum.Bgr101010 => PixelFormatFlags.BitsPerPixel32 | PixelFormatFlags.IsSRGB | PixelFormatFlags.ChannelOrderBGR,
                PixelFormatEnum.Rgb48 => PixelFormatFlags.BitsPerPixel48 | PixelFormatFlags.IsScRGB | PixelFormatFlags.ChannelOrderRGB,
                PixelFormatEnum.Rgba64 => PixelFormatFlags.BitsPerPixel64 | PixelFormatFlags.IsScRGB | PixelFormatFlags.ChannelOrderRGB,
                PixelFormatEnum.Prgba64 => PixelFormatFlags.BitsPerPixel64 | PixelFormatFlags.IsScRGB | PixelFormatFlags.Premultiplied | PixelFormatFlags.ChannelOrderRGB,
                PixelFormatEnum.Rgba128Float => PixelFormatFlags.BitsPerPixel128 | PixelFormatFlags.IsScRGB | PixelFormatFlags.ChannelOrderRGB,
                PixelFormatEnum.Prgba128Float => PixelFormatFlags.BitsPerPixel128 | PixelFormatFlags.IsScRGB | PixelFormatFlags.Premultiplied | PixelFormatFlags.ChannelOrderRGB,
                PixelFormatEnum.Rgb128Float => PixelFormatFlags.BitsPerPixel128 | PixelFormatFlags.IsScRGB | PixelFormatFlags.ChannelOrderRGB,
                PixelFormatEnum.Cmyk32 => PixelFormatFlags.BitsPerPixel32 | PixelFormatFlags.IsCMYK,
                _ => PixelFormatFlags.BitsPerPixelUndefined,
            };
        }

        [NonSerialized]
        private readonly PixelFormatFlags _flags;
        private readonly PixelFormatEnum _format;

        [NonSerialized]
        private uint _bitsPerPixel;

        [NonSerialized]
        private NonSerializedGuid _guidFormat;
    }

    internal struct NonSerializedGuid
    {
        internal Guid Value;

        internal NonSerializedGuid(Guid value)
        {
            Value = value;
        }
    }
}

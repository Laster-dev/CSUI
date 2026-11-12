// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net.Http;
using CSUI.Core.Interop.DirectX;
using MS.Internal;
using SR = MS.Internal.PresentationCore.SR;

namespace System.Windows.Media
{
    internal enum StandardColorSpace : int
    {
        Unknown = 0,
        Srgb = 1,
        ScRgb = 2,
        Rgb = 3,
        Gray = 4,
        Cmyk = 5,
        Multichannel = 6,
    }

    /// <summary>
    /// ColorContext represents an International Color Consortium (ICC) or Image Color Management (ICM) profile
    /// associated with an image or bitmap source.
    /// </summary>
    public unsafe class ColorContext
    {
        private const int MaximumColorContextLength = 32 * 1024 * 1024;
        private static readonly HttpClient s_httpClient = new HttpClient();

        private static readonly uint[] _colorTypeFromChannels = new uint[]
        {
            WicConstEx.COLOR_UNDEFINED,
            WicConstEx.COLOR_GRAY,
            WicConstEx.COLOR_UNDEFINED,
            WicConstEx.COLOR_RGB,
            WicConstEx.COLOR_CMYK,
            WicConstEx.COLOR_5_CHANNEL,
            WicConstEx.COLOR_6_CHANNEL,
            WicConstEx.COLOR_7_CHANNEL,
            WicConstEx.COLOR_8_CHANNEL,
        };

        internal ColorContext(SafeMILHandle colorContextHandle)
        {
            _colorContextHandle = colorContextHandle;

            void* pCC = (void*)_colorContextHandle.DangerousGetHandle();
            HRESULT.Check(IWICColorContextVtbl.GetType(pCC, out uint rawType));
            WICColorContextType type = (WICColorContextType)rawType;

            if (type == WICColorContextType.WICColorContextProfile)
            {
                HRESULT.Check(IWICColorContextVtbl.GetProfileBytes(pCC, 0, null, out uint cbActual));
                if (cbActual != 0)
                {
                    byte[] profileData = new byte[cbActual];
                    HRESULT.Check(IWICColorContextVtbl.GetProfileBytes(pCC, cbActual, profileData, out cbActual));
                    FromRawBytes(profileData, (int)cbActual, dontThrowException: false);
                }
            }
            else if (type == WICColorContextType.WICColorContextExifColorSpace)
            {
                HRESULT.Check(IWICColorContextVtbl.GetExifColorSpace(pCC, out uint colorSpace));
                if (colorSpace == 1 || colorSpace == 65535)
                {
                    using var factoryMaker = new FactoryMaker();
                    void* newCC = null;
                    HRESULT.Check(IWICImagingFactoryExVtbl.CreateColorContext((void*)factoryMaker.ImagingFactoryPtr, &newCC));
                    _colorContextHandle = new SafeMILHandle((IntPtr)newCC);

                    Uri profileUri = TryGetStandardColorSpaceProfileUri();
                    if (profileUri != null)
                    {
                        byte[] rawBytes = File.ReadAllBytes(profileUri.LocalPath);
                        HRESULT.Check(IWICColorContextVtbl.InitializeFromMemory((void*)_colorContextHandle.DangerousGetHandle(), rawBytes, (uint)rawBytes.Length));
                        FromRawBytes(rawBytes, rawBytes.Length, dontThrowException: false);
                    }
                    else
                    {
                        HRESULT.Check(IWICColorContextVtbl.InitializeFromExifColorSpace((void*)_colorContextHandle.DangerousGetHandle(), 1));
                        _numChannels = 3;
                        _colorSpaceFamily = StandardColorSpace.Srgb;
                    }
                }
                else
                {
                    Debug.Fail($"Unexpected EXIF color space: {colorSpace}");
                }
            }
        }

        public ColorContext(PixelFormat pixelFormat)
        {
            switch (pixelFormat.Format)
            {
                case PixelFormatEnum.Default:
                case PixelFormatEnum.Indexed1:
                case PixelFormatEnum.Indexed2:
                case PixelFormatEnum.Indexed4:
                case PixelFormatEnum.Indexed8:
                case PixelFormatEnum.BlackWhite:
                case PixelFormatEnum.Gray2:
                case PixelFormatEnum.Gray4:
                case PixelFormatEnum.Gray8:
                case PixelFormatEnum.Bgr555:
                case PixelFormatEnum.Bgr565:
                case PixelFormatEnum.Bgr24:
                case PixelFormatEnum.Rgb24:
                case PixelFormatEnum.Bgr32:
                case PixelFormatEnum.Bgra32:
                case PixelFormatEnum.Pbgra32:
                default:
                    Initialize(TryGetStandardColorSpaceProfileUri(), isStandardProfileUriNotFromUser: true);
                    break;
            }
        }

        public ColorContext(Uri profileUri)
        {
            Initialize(profileUri, isStandardProfileUriNotFromUser: false);
        }

        public Uri ProfileUri
        {
            get
            {
                return _profileUri;
            }
        }

        public Stream OpenProfileStream()
        {
            if (_colorContextHelper.IsInvalid)
            {
                throw new NullReferenceException();
            }

            uint bufferSize = 0;
            _colorContextHelper.GetColorProfileFromHandle(null, ref bufferSize);
            byte[] profile = new byte[bufferSize];
            _colorContextHelper.GetColorProfileFromHandle(profile, ref bufferSize);
            return new MemoryStream(profile, writable: false);
        }

        internal SafeProfileHandle ProfileHandle => _colorContextHelper.ProfileHandle;

        internal SafeMILHandle ColorContextHandle => _colorContextHandle;

        internal int NumChannels
        {
            get
            {
                if (_colorContextHelper.IsInvalid)
                {
                    return 3;
                }

                return _numChannels;
            }
        }

        internal uint ColorType => _colorTypeFromChannels[Math.Clamp(NumChannels, 0, _colorTypeFromChannels.Length - 1)];

        internal StandardColorSpace ColorSpaceFamily
        {
            get
            {
                if (_colorContextHelper.IsInvalid)
                {
                    return StandardColorSpace.Srgb;
                }

                return _colorSpaceFamily;
            }
        }

        internal bool IsValid => !_colorContextHelper.IsInvalid;

        internal delegate int GetColorContextsDelegate(ref uint numContexts, IntPtr[] colorContextPtrs);

        internal static IList<ColorContext> GetColorContextsHelper(GetColorContextsDelegate getColorContexts)
        {
            uint numContexts = 0;
            List<ColorContext> colorContextsList = null;

            int hr = getColorContexts(ref numContexts, null);
            if (hr != (int)WinCodecErrors.WINCODEC_ERR_UNSUPPORTEDOPERATION)
            {
                HRESULT.Check(hr);
            }

            if (numContexts > 0)
            {
                SafeMILHandle[] colorContextHandles = new SafeMILHandle[numContexts];

                using (var factoryMaker = new FactoryMaker())
                {
                    void* factoryPtr = (void*)factoryMaker.ImagingFactoryPtr;
                    for (uint i = 0; i < numContexts; i++)
                    {
                        void* cc = null;
                        HRESULT.Check(IWICImagingFactoryExVtbl.CreateColorContext(factoryPtr, &cc));
                        colorContextHandles[i] = new SafeMILHandle((IntPtr)cc);
                    }
                }

                IntPtr[] colorContextPtrs = new IntPtr[numContexts];
                for (uint i = 0; i < numContexts; i++)
                {
                    colorContextPtrs[i] = colorContextHandles[i].DangerousGetHandle();
                }

                HRESULT.Check(getColorContexts(ref numContexts, colorContextPtrs));

                colorContextsList = new List<ColorContext>((int)numContexts);
                for (uint i = 0; i < numContexts; i++)
                {
                    colorContextsList.Add(new ColorContext(colorContextHandles[i]));
                }
            }

            return colorContextsList;
        }

        public override bool Equals(object obj)
        {
            return obj is ColorContext context && context == this;
        }

        public override int GetHashCode()
        {
            return (int)_profileHeader.phDateTime_2;
        }

        public static bool operator ==(ColorContext context1, ColorContext context2)
        {
            if (ReferenceEquals(context1, context2))
            {
                return true;
            }

            if (context1 is null || context2 is null)
            {
                return false;
            }

            return
                context1._profileHeader.phSize == context2._profileHeader.phSize &&
                context1._profileHeader.phCMMType == context2._profileHeader.phCMMType &&
                context1._profileHeader.phVersion == context2._profileHeader.phVersion &&
                context1._profileHeader.phClass == context2._profileHeader.phClass &&
                context1._profileHeader.phDataColorSpace == context2._profileHeader.phDataColorSpace &&
                context1._profileHeader.phConnectionSpace == context2._profileHeader.phConnectionSpace &&
                context1._profileHeader.phDateTime_0 == context2._profileHeader.phDateTime_0 &&
                context1._profileHeader.phDateTime_1 == context2._profileHeader.phDateTime_1 &&
                context1._profileHeader.phDateTime_2 == context2._profileHeader.phDateTime_2 &&
                context1._profileHeader.phSignature == context2._profileHeader.phSignature &&
                context1._profileHeader.phPlatform == context2._profileHeader.phPlatform &&
                context1._profileHeader.phProfileFlags == context2._profileHeader.phProfileFlags &&
                context1._profileHeader.phManufacturer == context2._profileHeader.phManufacturer &&
                context1._profileHeader.phModel == context2._profileHeader.phModel &&
                context1._profileHeader.phAttributes_0 == context2._profileHeader.phAttributes_0 &&
                context1._profileHeader.phAttributes_1 == context2._profileHeader.phAttributes_1 &&
                context1._profileHeader.phRenderingIntent == context2._profileHeader.phRenderingIntent &&
                context1._profileHeader.phIlluminant_0 == context2._profileHeader.phIlluminant_0 &&
                context1._profileHeader.phIlluminant_1 == context2._profileHeader.phIlluminant_1 &&
                context1._profileHeader.phIlluminant_2 == context2._profileHeader.phIlluminant_2 &&
                context1._profileHeader.phCreator == context2._profileHeader.phCreator;
        }

        public static bool operator !=(ColorContext context1, ColorContext context2)
        {
            return !(context1 == context2);
        }

        private void Initialize(Uri profileUri, bool isStandardProfileUriNotFromUser)
        {
            _isProfileUriNotFromUser = isStandardProfileUriNotFromUser;

            if (profileUri == null)
            {
                if (!isStandardProfileUriNotFromUser)
                {
                    throw new ArgumentNullException(nameof(profileUri));
                }

                // Headless/minimal system fallback without reflection resources: use EXIF sRGB context
                using var factoryMaker = new FactoryMaker();
                void* cc = null;
                HRESULT.Check(IWICImagingFactoryExVtbl.CreateColorContext((void*)factoryMaker.ImagingFactoryPtr, &cc));
                _colorContextHandle = new SafeMILHandle((IntPtr)cc);
                HRESULT.Check(IWICColorContextVtbl.InitializeFromExifColorSpace(cc, 1));
                _numChannels = 3;
                _colorSpaceFamily = StandardColorSpace.Srgb;
                return;
            }

            if (!profileUri.IsAbsoluteUri)
            {
                throw new ArgumentException(SR.UriNotAbsolute, nameof(profileUri));
            }

            _profileUri = profileUri;

            byte[] rawBytes;
            if (profileUri.IsFile)
            {
                using Stream stream = File.OpenRead(profileUri.LocalPath);
                rawBytes = ReadStreamBytes(stream);
            }
            else if (string.Equals(profileUri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(profileUri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
            {
                using Stream stream = s_httpClient.GetStreamAsync(profileUri).GetAwaiter().GetResult();
                rawBytes = ReadStreamBytes(stream);
            }
            else
            {
                throw new NotSupportedException(SR.Image_CantDealWithUri);
            }

            FromRawBytes(rawBytes, rawBytes.Length, dontThrowException: false);
        }

        private static byte[] ReadStreamBytes(Stream stream)
        {
            byte[] rawBytes = new byte[MaximumColorContextLength];
            int totalRead = 0;

            while (totalRead < rawBytes.Length)
            {
                int bytesRead = stream.Read(rawBytes, totalRead, rawBytes.Length - totalRead);
                if (bytesRead == 0)
                {
                    break;
                }

                totalRead += bytesRead;
            }

            if (totalRead == rawBytes.Length && stream.ReadByte() != -1)
            {
                throw new ArgumentException(SR.ColorContext_FileTooLarge);
            }

            if (totalRead < rawBytes.Length)
            {
                Array.Resize(ref rawBytes, totalRead);
            }

            return rawBytes;
        }

        private static Uri TryGetStandardColorSpaceProfileUri()
        {
            if (WicNativeExports.GetStandardColorSpaceProfile(WicConstEx.SPACE_sRGB, out string profileName) == 0 &&
                !string.IsNullOrEmpty(profileName))
            {
                if (!Path.IsPathRooted(profileName))
                {
                    if (WicNativeExports.GetColorDirectory(out string colorDir) == 0 && !string.IsNullOrEmpty(colorDir))
                    {
                        string combined = Path.Combine(colorDir, profileName);
                        if (File.Exists(combined))
                        {
                            return new Uri(combined, UriKind.Absolute);
                        }
                    }
                }
                else if (File.Exists(profileName))
                {
                    return new Uri(profileName, UriKind.Absolute);
                }
            }

            string defaultSystemSrgb = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.System),
                "spool",
                "drivers",
                "color",
                "sRGB Color Space Profile.icm");

            if (File.Exists(defaultSystemSrgb))
            {
                return new Uri(defaultSystemSrgb, UriKind.Absolute);
            }

            return null;
        }

        private void FromRawBytes(byte[] data, int dataLength, bool dontThrowException)
        {
            if (data == null || dataLength <= 0)
            {
                if (!dontThrowException)
                {
                    throw new ArgumentException(SR.Image_ColorContextInvalid);
                }
                return;
            }

            fixed (byte* pData = data)
            {
                var profile = new WicNativeExports.PROFILE
                {
                    dwType = WicConstEx.PROFILE_MEMBUFFER,
                    pProfileData = pData,
                    cbDataSize = (uint)dataLength
                };

                _colorContextHelper.OpenColorProfile(ref profile);
            }

            if (_colorContextHelper.IsInvalid)
            {
                if (!dontThrowException)
                {
                    throw new ArgumentException(SR.Image_ColorContextInvalid);
                }
                return;
            }

            if (!_colorContextHelper.GetColorProfileHeader(out _profileHeader))
            {
                _colorContextHelper.CloseProfileHandle();
                if (!dontThrowException)
                {
                    throw new ArgumentException(SR.Image_ColorContextInvalid);
                }
                return;
            }

            PopulateChannelAndColorSpaceInfo(_profileHeader.phDataColorSpace);

            if (_colorContextHandle == null || _colorContextHandle.IsInvalid)
            {
                using var factoryMaker = new FactoryMaker();
                void* cc = null;
                HRESULT.Check(IWICImagingFactoryExVtbl.CreateColorContext((void*)factoryMaker.ImagingFactoryPtr, &cc));
                _colorContextHandle = new SafeMILHandle((IntPtr)cc);
                HRESULT.Check(IWICColorContextVtbl.InitializeFromMemory(cc, data, (uint)dataLength));
            }
        }

        private void PopulateChannelAndColorSpaceInfo(uint colorSpace)
        {
            switch (colorSpace)
            {
                case WicConstEx.SPACE_RGB:
                    _numChannels = 3;
                    _colorSpaceFamily = StandardColorSpace.Rgb;
                    break;
                case WicConstEx.SPACE_GRAY:
                    _numChannels = 1;
                    _colorSpaceFamily = StandardColorSpace.Gray;
                    break;
                case WicConstEx.SPACE_CMYK:
                    _numChannels = 4;
                    _colorSpaceFamily = StandardColorSpace.Cmyk;
                    break;
                case WicConstEx.SPACE_2_CHANNEL:
                    _numChannels = 2;
                    _colorSpaceFamily = StandardColorSpace.Multichannel;
                    break;
                case WicConstEx.SPACE_XYZ:
                case WicConstEx.SPACE_Lab:
                case WicConstEx.SPACE_Luv:
                case WicConstEx.SPACE_YCbCr:
                case WicConstEx.SPACE_Yxy:
                case WicConstEx.SPACE_HSV:
                case WicConstEx.SPACE_HLS:
                case WicConstEx.SPACE_CMY:
                case WicConstEx.SPACE_3_CHANNEL:
                    _numChannels = 3;
                    _colorSpaceFamily = StandardColorSpace.Multichannel;
                    break;
                case WicConstEx.SPACE_4_CHANNEL:
                    _numChannels = 4;
                    _colorSpaceFamily = StandardColorSpace.Multichannel;
                    break;
                case WicConstEx.SPACE_5_CHANNEL:
                    _numChannels = 5;
                    _colorSpaceFamily = StandardColorSpace.Multichannel;
                    break;
                case WicConstEx.SPACE_6_CHANNEL:
                    _numChannels = 6;
                    _colorSpaceFamily = StandardColorSpace.Multichannel;
                    break;
                case WicConstEx.SPACE_7_CHANNEL:
                    _numChannels = 7;
                    _colorSpaceFamily = StandardColorSpace.Multichannel;
                    break;
                case WicConstEx.SPACE_8_CHANNEL:
                    _numChannels = 8;
                    _colorSpaceFamily = StandardColorSpace.Multichannel;
                    break;
                default:
                    _numChannels = 0;
                    _colorSpaceFamily = StandardColorSpace.Unknown;
                    break;
            }
        }

        private readonly ColorContextHelper _colorContextHelper = new ColorContextHelper();
        private SafeMILHandle _colorContextHandle;
        private WicNativeExports.PROFILEHEADER _profileHeader;
        private int _numChannels;
        private StandardColorSpace _colorSpaceFamily;
        private Uri _profileUri;
        private bool _isProfileUriNotFromUser;
    }
}

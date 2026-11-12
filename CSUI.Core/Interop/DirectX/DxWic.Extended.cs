// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Windows.Media;
using Microsoft.Win32.SafeHandles;
using MS.Internal;

namespace CSUI.Core.Interop.DirectX;

internal static class WicConstEx
{
    public const uint WICBitmapLockRead = 0x1;
    public const uint WICBitmapLockWrite = 0x2;

    public const uint WICBitmapTransformRotate0 = 0;
    public const uint WICBitmapTransformRotate90 = 1;
    public const uint WICBitmapTransformRotate180 = 2;
    public const uint WICBitmapTransformRotate270 = 3;
    public const uint WICBitmapTransformFlipHorizontal = 0x8;
    public const uint WICBitmapTransformFlipVertical = 0x10;

    public const uint WICBitmapInterpolationModeNearestNeighbor = 0;
    public const uint WICBitmapInterpolationModeLinear = 1;
    public const uint WICBitmapInterpolationModeCubic = 2;
    public const uint WICBitmapInterpolationModeFant = 3;
    public const uint WICBitmapInterpolationModeHighQualityCubic = 4;

    public const uint WICColorContextUninitialized = 0;
    public const uint WICColorContextProfile = 1;
    public const uint WICColorContextExifColorSpace = 2;

    public const uint PROFILE_READ = 1;
    public const uint PROFILE_FILENAME = 1;
    public const uint PROFILE_MEMBUFFER = 2;

    public const uint COLOR_GRAY = 1;
    public const uint COLOR_RGB = 2;
    public const uint COLOR_XYZ = 3;
    public const uint COLOR_Yxy = 4;
    public const uint COLOR_Lab = 5;
    public const uint COLOR_3_CHANNEL = 6;
    public const uint COLOR_CMYK = 7;
    public const uint COLOR_5_CHANNEL = 8;
    public const uint COLOR_6_CHANNEL = 9;
    public const uint COLOR_7_CHANNEL = 10;
    public const uint COLOR_8_CHANNEL = 11;
    public const uint COLOR_NAMED = 12;
    public const uint COLOR_UNDEFINED = 255;

    public const uint SPACE_XYZ = 0x58595A20;
    public const uint SPACE_Lab = 0x4C616220;
    public const uint SPACE_Luv = 0x4C757620;
    public const uint SPACE_YCbCr = 0x59436272;
    public const uint SPACE_Yxy = 0x59787920;
    public const uint SPACE_RGB = 0x52474220;
    public const uint SPACE_GRAY = 0x47524159;
    public const uint SPACE_HSV = 0x48535620;
    public const uint SPACE_HLS = 0x484C5320;
    public const uint SPACE_CMYK = 0x434D594B;
    public const uint SPACE_CMY = 0x434D5920;
    public const uint SPACE_2_CHANNEL = 0x32434C52;
    public const uint SPACE_3_CHANNEL = 0x33434C52;
    public const uint SPACE_4_CHANNEL = 0x34434C52;
    public const uint SPACE_5_CHANNEL = 0x35434C52;
    public const uint SPACE_6_CHANNEL = 0x36434C52;
    public const uint SPACE_7_CHANNEL = 0x37434C52;
    public const uint SPACE_8_CHANNEL = 0x38434C52;
    public const uint SPACE_9_CHANNEL = 0x39434C52;
    public const uint SPACE_A_CHANNEL = 0x41434C52;
    public const uint SPACE_B_CHANNEL = 0x42434C52;
    public const uint SPACE_C_CHANNEL = 0x43434C52;
    public const uint SPACE_D_CHANNEL = 0x44434C52;
    public const uint SPACE_E_CHANNEL = 0x45434C52;
    public const uint SPACE_F_CHANNEL = 0x46434C52;
    public const uint SPACE_sRGB = 0x73524742;

    public const uint FILE_SHARE_READ = 0x00000001;
    public const uint OPEN_EXISTING = 3;
    public const int MAX_PATH = 260;

    public const uint DUPLICATE_SAME_ACCESS = 0x00000002;
}

internal static class WicGuidsEx
{
    public static readonly Guid IID_IUnknown = new("00000000-0000-0000-C000-000000000046");
    public static readonly Guid IID_ISequentialStream = new("0c733a30-2a1c-11ce-ade5-00aa0044773d");
    public static readonly Guid IID_IStream = new("0000000c-0000-0000-C000-000000000046");
    public static readonly Guid IID_IEnumUnknown = new("00000100-0000-0000-C000-000000000046");
    public static readonly Guid IID_IEnumString = new("00000101-0000-0000-C000-000000000046");
    public static readonly Guid IID_IWICComponentFactory = MILGuidData.IID_IWICComponentFactory;
    public static readonly Guid IID_IWICMetadataBlockReader = MILGuidData.IID_IWICMetadataBlockReader;
    public static readonly Guid IID_IWICMetadataBlockWriter = MILGuidData.IID_IWICMetadataBlockWriter;
    public static readonly Guid IID_IWICMetadataQueryReader = MILGuidData.IID_IWICMetadataQueryReader;
    public static readonly Guid IID_IWICMetadataQueryWriter = MILGuidData.IID_IWICMetadataQueryWriter;
    public static readonly Guid IID_IWICMetadataReader = MILGuidData.IID_IWICMetadataReader;
    public static readonly Guid IID_IWICMetadataWriter = MILGuidData.IID_IWICMetadataWriter;
    public static readonly Guid IID_IWICPixelFormatInfo = MILGuidData.IID_IWICPixelFormatInfo;
    public static readonly Guid IID_IWICPixelFormatInfo2 = MILGuidData.IID_IWICPixelFormatInfo2;
}

internal static unsafe class WicNativeExports
{
    private static readonly nint s_windowscodecs = NativeModule.Load("windowscodecs.dll");
    private static readonly nint s_ole32 = NativeModule.Load("ole32.dll");
    private static readonly nint s_mscms = NativeModule.Load("mscms.dll");
    private static readonly nint s_kernel32 = NativeModule.Load("kernel32.dll");

    private static readonly delegate* unmanaged[Stdcall]<Guid*, void*, void**, int> s_wicConvertBitmapSource =
        (delegate* unmanaged[Stdcall]<Guid*, void*, void**, int>)NativeModule.GetExport(s_windowscodecs, "WICConvertBitmapSource");

    private static readonly delegate* unmanaged[Stdcall]<Guid*, uint, char*, uint*, int> s_wicMapGuidToShortName =
        (delegate* unmanaged[Stdcall]<Guid*, uint, char*, uint*, int>)NativeModule.GetExport(s_windowscodecs, "WICMapGuidToShortName");

    private static readonly delegate* unmanaged[Stdcall]<char*, Guid*, int> s_wicMapShortNameToGuid =
        (delegate* unmanaged[Stdcall]<char*, Guid*, int>)NativeModule.GetExport(s_windowscodecs, "WICMapShortNameToGuid");

    private static readonly delegate* unmanaged[Stdcall]<uint, uint, Guid*, nint, uint, uint, void**, int> s_wicCreateBitmapFromSection =
        (delegate* unmanaged[Stdcall]<uint, uint, Guid*, nint, uint, uint, void**, int>)NativeModule.GetExport(s_windowscodecs, "WICCreateBitmapFromSection");

    private static readonly delegate* unmanaged[Stdcall]<void*, uint, int> s_coInitializeEx =
        (delegate* unmanaged[Stdcall]<void*, uint, int>)NativeModule.GetExport(s_ole32, "CoInitializeEx");

    private static readonly delegate* unmanaged[Stdcall]<void> s_coUninitialize =
        (delegate* unmanaged[Stdcall]<void>)NativeModule.GetExport(s_ole32, "CoUninitialize");

    private static readonly delegate* unmanaged[Stdcall]<void*, int> s_propVariantClear =
        (delegate* unmanaged[Stdcall]<void*, int>)NativeModule.GetExport(s_ole32, "PropVariantClear");

    private static readonly delegate* unmanaged[Stdcall]<nuint, void*> s_coTaskMemAlloc =
        (delegate* unmanaged[Stdcall]<nuint, void*>)NativeModule.GetExport(s_ole32, "CoTaskMemAlloc");

    private static readonly delegate* unmanaged[Stdcall]<void*, void> s_coTaskMemFree =
        (delegate* unmanaged[Stdcall]<void*, void>)NativeModule.GetExport(s_ole32, "CoTaskMemFree");

    // mscms.dll
    private static readonly delegate* unmanaged[Stdcall]<PROFILE*, uint, uint, uint, nint> s_openColorProfileW =
        (delegate* unmanaged[Stdcall]<PROFILE*, uint, uint, uint, nint>)NativeModule.GetExport(s_mscms, "OpenColorProfileW");

    private static readonly delegate* unmanaged[Stdcall]<nint, int> s_closeColorProfile =
        (delegate* unmanaged[Stdcall]<nint, int>)NativeModule.GetExport(s_mscms, "CloseColorProfile");

    private static readonly delegate* unmanaged[Stdcall]<nint, PROFILEHEADER*, int> s_getColorProfileHeader =
        (delegate* unmanaged[Stdcall]<nint, PROFILEHEADER*, int>)NativeModule.GetExport(s_mscms, "GetColorProfileHeader");

    private static readonly delegate* unmanaged[Stdcall]<nint, byte*, uint*, int> s_getColorProfileFromHandle =
        (delegate* unmanaged[Stdcall]<nint, byte*, uint*, int>)NativeModule.GetExport(s_mscms, "GetColorProfileFromHandle");

    private static readonly delegate* unmanaged[Stdcall]<char*, char*, uint*, int> s_getColorDirectoryW =
        (delegate* unmanaged[Stdcall]<char*, char*, uint*, int>)NativeModule.GetExport(s_mscms, "GetColorDirectoryW");

    private static readonly delegate* unmanaged[Stdcall]<char*, uint, char*, uint*, int> s_getStandardColorSpaceProfileW =
        (delegate* unmanaged[Stdcall]<char*, uint, char*, uint*, int>)NativeModule.GetExport(s_mscms, "GetStandardColorSpaceProfileW");

    private static readonly delegate* unmanaged[Stdcall]<nint*, uint, uint*, uint, uint, uint, nint> s_createMultiProfileTransform =
        (delegate* unmanaged[Stdcall]<nint*, uint, uint*, uint, uint, uint, nint>)NativeModule.GetExport(s_mscms, "CreateMultiProfileTransform");

    private static readonly delegate* unmanaged[Stdcall]<nint, int> s_deleteColorTransform =
        (delegate* unmanaged[Stdcall]<nint, int>)NativeModule.GetExport(s_mscms, "DeleteColorTransform");

    private static readonly delegate* unmanaged[Stdcall]<nint, void*, uint, uint, void*, uint, int> s_translateColors =
        (delegate* unmanaged[Stdcall]<nint, void*, uint, uint, void*, uint, int>)NativeModule.GetExport(s_mscms, "TranslateColors");

    // kernel32.dll
    private static readonly delegate* unmanaged[Stdcall]<nint> s_getCurrentProcess =
        (delegate* unmanaged[Stdcall]<nint>)NativeModule.GetExport(s_kernel32, "GetCurrentProcess");

    private static readonly delegate* unmanaged[Stdcall]<nint, nint, nint, nint*, uint, int, uint, int> s_duplicateHandle =
        (delegate* unmanaged[Stdcall]<nint, nint, nint, nint*, uint, int, uint, int>)NativeModule.GetExport(s_kernel32, "DuplicateHandle");

    private static readonly delegate* unmanaged[Stdcall]<char*, char*, uint, char*, uint> s_getTempFileNameW =
        (delegate* unmanaged[Stdcall]<char*, char*, uint, char*, uint>)NativeModule.GetExport(s_kernel32, "GetTempFileNameW");

    public static int WICConvertBitmapSource(ref Guid dstPixelFormat, void* src, void** dst)
    {
        fixed (Guid* pFmt = &dstPixelFormat)
            return s_wicConvertBitmapSource(pFmt, src, dst);
    }

    public static int WICMapGuidToShortName(ref Guid guid, out string name)
    {
        Guid g = guid;
        uint len = 0;
        int hr = s_wicMapGuidToShortName(&g, 0, null, &len);
        if (hr < 0 && hr != (int)WinCodecErrors.WINCODEC_ERR_INSUFFICIENTBUFFER)
        {
            name = null;
            return hr;
        }
        if (len == 0)
        {
            name = string.Empty;
            return MS.Internal.HRESULT.S_OK;
        }
        char[] buf = new char[len];
        fixed (char* pBuf = buf)
        {
            hr = s_wicMapGuidToShortName(&g, len, pBuf, &len);
            name = hr >= 0 ? new string(pBuf).TrimEnd('\0') : null;
            return hr;
        }
    }

    public static int WICMapShortNameToGuid(string name, out Guid guid)
    {
        Guid g = Guid.Empty;
        fixed (char* pName = name)
        {
            int hr = s_wicMapShortNameToGuid(pName, &g);
            guid = g;
            return hr;
        }
    }

    public static int WICCreateBitmapFromSection(uint width, uint height, ref Guid pixelFormat, nint hSection, uint stride, uint offset, void** ppIBitmap)
    {
        Guid fmt = pixelFormat;
        return s_wicCreateBitmapFromSection(width, height, &fmt, hSection, stride, offset, ppIBitmap);
    }

    public static int CoInitializeSTA() => s_coInitializeEx(null, 0x2 /* COINIT_APARTMENTTHREADED */);
    public static void CoUninitialize() => s_coUninitialize();
    public static int PropVariantClear(void* pvar) => s_propVariantClear(pvar);

    public static nint StringToCoTaskMemUni(string s)
    {
        if (s == null) return 0;
        nuint bytes = (nuint)((s.Length + 1) * sizeof(char));
        char* mem = (char*)s_coTaskMemAlloc(bytes);
        if (mem == null) throw new OutOfMemoryException();
        s.AsSpan().CopyTo(new Span<char>(mem, s.Length));
        mem[s.Length] = '\0';
        return (nint)mem;
    }

    public static void FreeCoTaskMem(nint ptr)
    {
        if (ptr != 0) s_coTaskMemFree((void*)ptr);
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct PROFILEHEADER
    {
        public uint phSize;
        public uint phCMMType;
        public uint phVersion;
        public uint phClass;
        public uint phDataColorSpace;
        public uint phConnectionSpace;
        public uint phDateTime_0;
        public uint phDateTime_1;
        public uint phDateTime_2;
        public uint phSignature;
        public uint phPlatform;
        public uint phProfileFlags;
        public uint phManufacturer;
        public uint phModel;
        public uint phAttributes_0;
        public uint phAttributes_1;
        public uint phRenderingIntent;
        public uint phIlluminant_0;
        public uint phIlluminant_1;
        public uint phIlluminant_2;
        public uint phCreator;
        public fixed byte phReserved[44];
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct PROFILE
    {
        public uint dwType;
        public void* pProfileData;
        public uint cbDataSize;
    }

    public static nint OpenColorProfile(ref PROFILE profile, uint desiredAccess, uint shareMode, uint creationMode)
    {
        fixed (PROFILE* p = &profile)
            return s_openColorProfileW(p, desiredAccess, shareMode, creationMode);
    }

    public static bool CloseColorProfile(nint handle) => s_closeColorProfile(handle) != 0;

    public static bool GetColorProfileHeader(nint handle, out PROFILEHEADER header)
    {
        fixed (PROFILEHEADER* p = &header)
            return s_getColorProfileHeader(handle, p) != 0;
    }

    public static bool GetColorProfileFromHandle(nint handle, byte[] buffer, ref uint size)
    {
        fixed (byte* pBuf = buffer)
        fixed (uint* pSize = &size)
            return s_getColorProfileFromHandle(handle, pBuf, pSize) != 0;
    }

    public static int GetColorDirectory(out string directory)
    {
        Span<char> buf = stackalloc char[WicConstEx.MAX_PATH + 1];
        buf.Clear();
        uint byteSize = (uint)(buf.Length * sizeof(char));
        fixed (char* pBuf = buf)
        {
            if (s_getColorDirectoryW(null, pBuf, &byteSize) == 0)
            {
                directory = null;
                int hr = Marshal.GetHRForLastWin32Error();
                return hr < 0 ? hr : MS.Internal.HRESULT.E_FAIL;
            }
            directory = new string(pBuf);
            return 0;
        }
    }

    public static int GetStandardColorSpaceProfile(uint profileId, out string profilePath)
    {
        Span<char> buf = stackalloc char[WicConstEx.MAX_PATH + 1];
        buf.Clear();
        uint byteSize = (uint)(buf.Length * sizeof(char));
        fixed (char* pBuf = buf)
        {
            if (s_getStandardColorSpaceProfileW(null, profileId, pBuf, &byteSize) == 0)
            {
                profilePath = null;
                int hr = Marshal.GetHRForLastWin32Error();
                return hr < 0 ? hr : MS.Internal.HRESULT.E_FAIL;
            }
            profilePath = new string(pBuf);
            return 0;
        }
    }

    public static nint CreateMultiProfileTransform(nint[] profiles, uint[] intents, uint flags, uint preferredCmm)
    {
        fixed (nint* pProfiles = profiles)
        fixed (uint* pIntents = intents)
            return s_createMultiProfileTransform(pProfiles, (uint)profiles.Length, pIntents, (uint)intents.Length, flags, preferredCmm);
    }

    public static bool DeleteColorTransform(nint hTransform) => s_deleteColorTransform(hTransform) != 0;

    public static bool TranslateColors(nint hTransform, void* input, uint nColors, uint ctInput, void* output, uint ctOutput)
        => s_translateColors(hTransform, input, nColors, ctInput, output, ctOutput) != 0;

    public static nint GetCurrentProcess() => s_getCurrentProcess();

    public static bool DuplicateHandle(nint srcProc, nint srcHandle, nint dstProc, out nint dstHandle, uint access, bool inherit, uint options)
    {
        nint res = 0;
        int ok = s_duplicateHandle(srcProc, srcHandle, dstProc, &res, access, inherit ? 1 : 0, options);
        dstHandle = res;
        return ok != 0;
    }

    public static int SetEncoderFormat(void* sourceIn, void* palette, void* frameEncode, out void* sourceOut)
    {
        sourceOut = null;
        if (sourceIn == null || frameEncode == null)
        {
            return HRESULT.E_INVALIDARG;
        }

        int hr = IWICBitmapSourceVtbl.GetPixelFormat(sourceIn, out Guid srcFormat);
        if (hr < 0)
        {
            return hr;
        }

        Guid dstFormat = srcFormat;
        hr = IWICBitmapFrameEncodeVtbl.SetPixelFormat(frameEncode, ref dstFormat);
        if (hr < 0)
        {
            return hr;
        }

        if (dstFormat == srcFormat)
        {
            if (palette != null)
            {
                IWICBitmapFrameEncodeVtbl.SetPalette(frameEncode, palette);
            }
            else if (TryGetIndexedColorCount(dstFormat, out _))
            {
                using var factoryMaker = new FactoryMaker();
                void* tempPal = null;
                if (IWICImagingFactoryExVtbl.CreatePalette((void*)factoryMaker.ImagingFactoryPtr, &tempPal) >= 0 && tempPal != null)
                {
                    try
                    {
                        if (IWICBitmapSourceExVtbl.CopyPalette(sourceIn, tempPal) >= 0)
                        {
                            IWICBitmapFrameEncodeVtbl.SetPalette(frameEncode, tempPal);
                        }
                    }
                    finally
                    {
                        ComHelpers.Release(tempPal);
                    }
                }
            }

            ComHelpers.AddRef(sourceIn);
            sourceOut = sourceIn;
            return HRESULT.S_OK;
        }

        using var fm = new FactoryMaker();
        void* wicFactory = (void*)fm.ImagingFactoryPtr;

        void* converter = null;
        hr = IWICImagingFactoryVtbl.CreateFormatConverter(wicFactory, &converter);
        if (hr < 0 || converter == null)
        {
            return hr < 0 ? hr : HRESULT.E_FAIL;
        }

        void* ownedPalette = null;
        try
        {
            void* convPalette = palette;
            uint paletteTranslate = (uint)WICPaletteType.WICPaletteTypeCustom;

            if (convPalette == null && TryGetIndexedColorCount(dstFormat, out uint colorCount))
            {
                hr = IWICImagingFactoryExVtbl.CreatePalette(wicFactory, &ownedPalette);
                if (hr >= 0 && ownedPalette != null)
                {
                    if (IWICPaletteVtbl.InitializeFromBitmap(ownedPalette, sourceIn, colorCount, 1) >= 0)
                    {
                        convPalette = ownedPalette;
                        paletteTranslate = (uint)WICPaletteType.WICPaletteTypeOptimal;
                    }
                }
            }

            hr = IWICFormatConverterVtbl.Initialize(
                converter,
                sourceIn,
                in dstFormat,
                (uint)DitherType.DitherTypeNone,
                convPalette,
                0.0,
                paletteTranslate);
            if (hr < 0)
            {
                ComHelpers.Release(converter);
                return hr;
            }

            if (convPalette != null)
            {
                if (ownedPalette != null)
                {
                    IWICBitmapSourceExVtbl.CopyPalette(converter, ownedPalette);
                    IWICBitmapFrameEncodeVtbl.SetPalette(frameEncode, ownedPalette);
                }
                else
                {
                    IWICBitmapFrameEncodeVtbl.SetPalette(frameEncode, convPalette);
                }
            }

            sourceOut = converter;
            return HRESULT.S_OK;
        }
        finally
        {
            if (ownedPalette != null)
            {
                ComHelpers.Release(ownedPalette);
            }
        }
    }

    private static bool TryGetIndexedColorCount(in Guid pixelFormat, out uint colorCount)
    {
        if (pixelFormat == WICPixelFormatGUIDs.WICPixelFormat1bppIndexed ||
            pixelFormat == WICPixelFormatGUIDs.WICPixelFormatBlackWhite)
        {
            colorCount = 2;
            return true;
        }
        if (pixelFormat == WICPixelFormatGUIDs.WICPixelFormat2bppIndexed ||
            pixelFormat == WICPixelFormatGUIDs.WICPixelFormat2bppGray)
        {
            colorCount = 4;
            return true;
        }
        if (pixelFormat == WICPixelFormatGUIDs.WICPixelFormat4bppIndexed ||
            pixelFormat == WICPixelFormatGUIDs.WICPixelFormat4bppGray)
        {
            colorCount = 16;
            return true;
        }
        if (pixelFormat == WICPixelFormatGUIDs.WICPixelFormat8bppIndexed ||
            pixelFormat == WICPixelFormatGUIDs.WICPixelFormat8bppGray)
        {
            colorCount = 256;
            return true;
        }

        colorCount = 0;
        return false;
    }
}

internal sealed class SafeProfileHandle : SafeHandleZeroOrMinusOneIsInvalid
{
    internal SafeProfileHandle() : base(true) { }
    internal SafeProfileHandle(nint handle) : base(true) { SetHandle(handle); }

    protected override bool ReleaseHandle() => WicNativeExports.CloseColorProfile(handle);
}

internal sealed class ColorTransformHandle : SafeHandleZeroOrMinusOneIsInvalid
{
    internal ColorTransformHandle() : base(true) { }
    internal ColorTransformHandle(nint handle) : base(true) { SetHandle(handle); }

    protected override bool ReleaseHandle() => WicNativeExports.DeleteColorTransform(handle);
}

internal static unsafe class IWICImagingFactoryExVtbl
{
    public static int CreateDecoderFromFileHandle(void* self, nuint hFile, Guid* vendor, uint metadataOptions, void** decoder)
        => ((delegate* unmanaged[Stdcall]<void*, nuint, Guid*, uint, void**, int>)(*(void***)self)[5])(self, hFile, vendor, metadataOptions, decoder);

    public static int CreateComponentInfo(void* self, in Guid clsidComponent, void** info)
    {
        Guid id = clsidComponent;
        return ((delegate* unmanaged[Stdcall]<void*, Guid*, void**, int>)(*(void***)self)[6])(self, &id, info);
    }

    public static int CreateDecoder(void* self, in Guid containerFormat, Guid* vendor, void** decoder)
    {
        Guid cf = containerFormat;
        return ((delegate* unmanaged[Stdcall]<void*, Guid*, Guid*, void**, int>)(*(void***)self)[7])(self, &cf, vendor, decoder);
    }

    public static int CreateEncoder(void* self, in Guid containerFormat, Guid* vendor, void** encoder)
    {
        Guid cf = containerFormat;
        return ((delegate* unmanaged[Stdcall]<void*, Guid*, Guid*, void**, int>)(*(void***)self)[8])(self, &cf, vendor, encoder);
    }

    public static int CreatePalette(void* self, void** palette)
        => ((delegate* unmanaged[Stdcall]<void*, void**, int>)(*(void***)self)[9])(self, palette);

    public static int CreateBitmapScaler(void* self, void** scaler)
        => ((delegate* unmanaged[Stdcall]<void*, void**, int>)(*(void***)self)[11])(self, scaler);

    public static int CreateBitmapClipper(void* self, void** clipper)
        => ((delegate* unmanaged[Stdcall]<void*, void**, int>)(*(void***)self)[12])(self, clipper);

    public static int CreateBitmapFlipRotator(void* self, void** rotator)
        => ((delegate* unmanaged[Stdcall]<void*, void**, int>)(*(void***)self)[13])(self, rotator);

    public static int CreateColorContext(void* self, void** colorContext)
        => ((delegate* unmanaged[Stdcall]<void*, void**, int>)(*(void***)self)[15])(self, colorContext);

    public static int CreateColorTransformer(void* self, void** colorTransform)
        => ((delegate* unmanaged[Stdcall]<void*, void**, int>)(*(void***)self)[16])(self, colorTransform);

    public static int CreateBitmap(void* self, uint width, uint height, in Guid pixelFormat, uint option, void** bitmap)
    {
        Guid fmt = pixelFormat;
        return ((delegate* unmanaged[Stdcall]<void*, uint, uint, Guid*, uint, void**, int>)(*(void***)self)[17])(self, width, height, &fmt, option, bitmap);
    }

    public static int CreateBitmapFromSource(void* self, void* source, uint option, void** bitmap)
        => ((delegate* unmanaged[Stdcall]<void*, void*, uint, void**, int>)(*(void***)self)[18])(self, source, option, bitmap);

    public static int CreateBitmapFromSourceRect(void* self, void* source, uint x, uint y, uint width, uint height, void** bitmap)
        => ((delegate* unmanaged[Stdcall]<void*, void*, uint, uint, uint, uint, void**, int>)(*(void***)self)[19])(self, source, x, y, width, height, bitmap);

    public static int CreateBitmapFromHBITMAP(void* self, nint hBitmap, nint hPalette, uint alphaOption, void** bitmap)
        => ((delegate* unmanaged[Stdcall]<void*, nint, nint, uint, void**, int>)(*(void***)self)[21])(self, hBitmap, hPalette, alphaOption, bitmap);

    public static int CreateBitmapFromHICON(void* self, nint hIcon, void** bitmap)
        => ((delegate* unmanaged[Stdcall]<void*, nint, void**, int>)(*(void***)self)[22])(self, hIcon, bitmap);

    public static int CreateFastMetadataEncoderFromDecoder(void* self, void* decoder, void** fastEncoder)
        => ((delegate* unmanaged[Stdcall]<void*, void*, void**, int>)(*(void***)self)[24])(self, decoder, fastEncoder);

    public static int CreateFastMetadataEncoderFromFrameDecode(void* self, void* frameDecoder, void** fastEncoder)
        => ((delegate* unmanaged[Stdcall]<void*, void*, void**, int>)(*(void***)self)[25])(self, frameDecoder, fastEncoder);

    public static int CreateQueryWriter(void* self, in Guid metadataFormat, Guid* vendor, void** queryWriter)
    {
        Guid mf = metadataFormat;
        return ((delegate* unmanaged[Stdcall]<void*, Guid*, Guid*, void**, int>)(*(void***)self)[26])(self, &mf, vendor, queryWriter);
    }

    public static int CreateQueryWriterFromReader(void* self, void* queryReader, Guid* vendor, void** queryWriter)
        => ((delegate* unmanaged[Stdcall]<void*, void*, Guid*, void**, int>)(*(void***)self)[27])(self, queryReader, vendor, queryWriter);

    // IWICComponentFactory slots (requires QI for IID_IWICComponentFactory)
    public static int CreateMetadataWriterFromReader(void* self, void* reader, Guid* vendor, void** writer)
        => ((delegate* unmanaged[Stdcall]<void*, void*, Guid*, void**, int>)(*(void***)self)[31])(self, reader, vendor, writer);

    public static int CreateQueryWriterFromBlockWriter(void* self, void* blockWriter, void** queryWriter)
        => ((delegate* unmanaged[Stdcall]<void*, void*, void**, int>)(*(void***)self)[33])(self, blockWriter, queryWriter);
}

internal static unsafe class IWICStreamExVtbl
{
    public static int InitializeFromIStream(void* self, void* comStream)
        => ((delegate* unmanaged[Stdcall]<void*, void*, int>)(*(void***)self)[14])(self, comStream);
}

internal static unsafe class IWICBitmapSourceExVtbl
{
    public static int CopyPalette(void* self, void* palette)
        => ((delegate* unmanaged[Stdcall]<void*, void*, int>)(*(void***)self)[6])(self, palette);
}

internal static unsafe class IWICBitmapVtbl
{
    public static int Lock(void* self, WICRect* rect, uint flags, void** bitmapLock)
        => ((delegate* unmanaged[Stdcall]<void*, WICRect*, uint, void**, int>)(*(void***)self)[8])(self, rect, flags, bitmapLock);

    public static int SetPalette(void* self, void* palette)
        => ((delegate* unmanaged[Stdcall]<void*, void*, int>)(*(void***)self)[9])(self, palette);

    public static int SetResolution(void* self, double dpiX, double dpiY)
        => ((delegate* unmanaged[Stdcall]<void*, double, double, int>)(*(void***)self)[10])(self, dpiX, dpiY);
}

internal static unsafe class IWICBitmapLockVtbl
{
    public static int GetSize(void* self, out uint width, out uint height)
    {
        fixed (uint* pw = &width)
        fixed (uint* ph = &height)
            return ((delegate* unmanaged[Stdcall]<void*, uint*, uint*, int>)(*(void***)self)[3])(self, pw, ph);
    }

    public static int GetStride(void* self, out uint stride)
    {
        fixed (uint* ps = &stride)
            return ((delegate* unmanaged[Stdcall]<void*, uint*, int>)(*(void***)self)[4])(self, ps);
    }

    public static int GetDataPointer(void* self, out uint bufferSize, out byte* data)
    {
        uint sz = 0;
        byte* ptr = null;
        int hr = ((delegate* unmanaged[Stdcall]<void*, uint*, byte**, int>)(*(void***)self)[5])(self, &sz, &ptr);
        bufferSize = sz;
        data = ptr;
        return hr;
    }

    public static int GetPixelFormat(void* self, out Guid format)
    {
        Guid f = Guid.Empty;
        int hr = ((delegate* unmanaged[Stdcall]<void*, Guid*, int>)(*(void***)self)[6])(self, &f);
        format = f;
        return hr;
    }
}

internal static unsafe class IWICBitmapClipperVtbl
{
    public static int Initialize(void* self, void* source, WICRect* rect)
        => ((delegate* unmanaged[Stdcall]<void*, void*, WICRect*, int>)(*(void***)self)[8])(self, source, rect);
}

internal static unsafe class IWICBitmapFlipRotatorVtbl
{
    public static int Initialize(void* self, void* source, uint options)
        => ((delegate* unmanaged[Stdcall]<void*, void*, uint, int>)(*(void***)self)[8])(self, source, options);
}

internal static unsafe class IWICBitmapScalerVtbl
{
    public static int Initialize(void* self, void* source, uint width, uint height, uint mode)
        => ((delegate* unmanaged[Stdcall]<void*, void*, uint, uint, uint, int>)(*(void***)self)[8])(self, source, width, height, mode);
}

internal static unsafe class IWICPaletteVtbl
{
    public static int InitializePredefined(void* self, uint paletteType, int addTransparent)
        => ((delegate* unmanaged[Stdcall]<void*, uint, int, int>)(*(void***)self)[3])(self, paletteType, addTransparent);

    public static int InitializeCustom(void* self, uint* colors, uint count)
        => ((delegate* unmanaged[Stdcall]<void*, uint*, uint, int>)(*(void***)self)[4])(self, colors, count);

    public static int InitializeFromBitmap(void* self, void* surface, uint count, int addTransparent)
        => ((delegate* unmanaged[Stdcall]<void*, void*, uint, int, int>)(*(void***)self)[5])(self, surface, count, addTransparent);

    public static int InitializeFromPalette(void* self, void* sourcePalette)
        => ((delegate* unmanaged[Stdcall]<void*, void*, int>)(*(void***)self)[6])(self, sourcePalette);

    public static int GetType(void* self, out WICPaletteType paletteType)
    {
        uint t = 0;
        int hr = ((delegate* unmanaged[Stdcall]<void*, uint*, int>)(*(void***)self)[7])(self, &t);
        paletteType = (WICPaletteType)t;
        return hr;
    }

    public static int GetColorCount(void* self, out uint count)
    {
        uint c = 0;
        int hr = ((delegate* unmanaged[Stdcall]<void*, uint*, int>)(*(void***)self)[8])(self, &c);
        count = c;
        return hr;
    }

    public static int GetColors(void* self, uint count, uint* colors, out uint actualColors)
    {
        uint actual = 0;
        int hr = ((delegate* unmanaged[Stdcall]<void*, uint, uint*, uint*, int>)(*(void***)self)[9])(self, count, colors, &actual);
        actualColors = actual;
        return hr;
    }

    public static int IsBlackWhite(void* self, out bool value)
    {
        int v = 0;
        int hr = ((delegate* unmanaged[Stdcall]<void*, int*, int>)(*(void***)self)[10])(self, &v);
        value = v != 0;
        return hr;
    }

    public static int IsGrayscale(void* self, out bool value)
    {
        int v = 0;
        int hr = ((delegate* unmanaged[Stdcall]<void*, int*, int>)(*(void***)self)[11])(self, &v);
        value = v != 0;
        return hr;
    }

    public static int HasAlpha(void* self, out bool value)
    {
        int v = 0;
        int hr = ((delegate* unmanaged[Stdcall]<void*, int*, int>)(*(void***)self)[12])(self, &v);
        value = v != 0;
        return hr;
    }
}

internal static unsafe class IWICComponentInfoVtbl
{
    public static int GetCLSID(void* self, out Guid clsid)
    {
        Guid g = Guid.Empty;
        int hr = ((delegate* unmanaged[Stdcall]<void*, Guid*, int>)(*(void***)self)[4])(self, &g);
        clsid = g;
        return hr;
    }

    public static int GetAuthor(void* self, out string author)
        => ReadWicString(self, 6, out author);

    public static int GetVersion(void* self, out string version)
        => ReadWicString(self, 8, out version);

    public static int GetSpecVersion(void* self, out string specVersion)
        => ReadWicString(self, 9, out specVersion);

    public static int GetFriendlyName(void* self, out string friendlyName)
        => ReadWicString(self, 10, out friendlyName);

    internal static int ReadWicString(void* self, int slot, out string value)
    {
        uint len = 0;
        int hr = ((delegate* unmanaged[Stdcall]<void*, uint, char*, uint*, int>)(*(void***)self)[slot])(self, 0, null, &len);
        if (hr < 0)
        {
            value = string.Empty;
            return hr;
        }
        if (len == 0)
        {
            value = string.Empty;
            return 0;
        }
        char[] buf = new char[len];
        fixed (char* pBuf = buf)
        {
            hr = ((delegate* unmanaged[Stdcall]<void*, uint, char*, uint*, int>)(*(void***)self)[slot])(self, len, pBuf, &len);
            value = hr >= 0 ? new string(pBuf).TrimEnd('\0') : string.Empty;
            return hr;
        }
    }
}

internal static unsafe class IWICPixelFormatInfoVtbl
{
    public static int GetBitsPerPixel(void* self, out uint bpp)
    {
        uint b = 0;
        int hr = ((delegate* unmanaged[Stdcall]<void*, uint*, int>)(*(void***)self)[13])(self, &b);
        bpp = b;
        return hr;
    }

    public static int GetChannelCount(void* self, out uint count)
    {
        uint c = 0;
        int hr = ((delegate* unmanaged[Stdcall]<void*, uint*, int>)(*(void***)self)[14])(self, &c);
        count = c;
        return hr;
    }

    public static int GetChannelMask(void* self, uint channelIndex, uint cbMaskBuffer, byte* maskBuffer, out uint cbActual)
    {
        uint actual = 0;
        int hr = ((delegate* unmanaged[Stdcall]<void*, uint, uint, byte*, uint*, int>)(*(void***)self)[15])(self, channelIndex, cbMaskBuffer, maskBuffer, &actual);
        cbActual = actual;
        return hr;
    }
}

internal static unsafe class IWICBitmapCodecInfoVtbl
{
    public static int GetContainerFormat(void* self, out Guid format)
    {
        Guid g = Guid.Empty;
        int hr = ((delegate* unmanaged[Stdcall]<void*, Guid*, int>)(*(void***)self)[11])(self, &g);
        format = g;
        return hr;
    }

    public static int GetDeviceManufacturer(void* self, out string val)
        => IWICComponentInfoVtbl.ReadWicString(self, 14, out val);

    public static int GetDeviceModels(void* self, out string val)
        => IWICComponentInfoVtbl.ReadWicString(self, 15, out val);

    public static int GetMimeTypes(void* self, out string val)
        => IWICComponentInfoVtbl.ReadWicString(self, 16, out val);

    public static int GetFileExtensions(void* self, out string val)
        => IWICComponentInfoVtbl.ReadWicString(self, 17, out val);

    public static int DoesSupportAnimation(void* self, out bool val)
    {
        int b = 0;
        int hr = ((delegate* unmanaged[Stdcall]<void*, int*, int>)(*(void***)self)[18])(self, &b);
        val = b != 0;
        return hr;
    }

    public static int DoesSupportLossless(void* self, out bool val)
    {
        int b = 0;
        int hr = ((delegate* unmanaged[Stdcall]<void*, int*, int>)(*(void***)self)[20])(self, &b);
        val = b != 0;
        return hr;
    }

    public static int DoesSupportMultiframe(void* self, out bool val)
    {
        int b = 0;
        int hr = ((delegate* unmanaged[Stdcall]<void*, int*, int>)(*(void***)self)[21])(self, &b);
        val = b != 0;
        return hr;
    }
}

internal static unsafe class IWICBitmapDecoderExVtbl
{
    public static int Initialize(void* self, void* comStream, uint cacheOptions)
        => ((delegate* unmanaged[Stdcall]<void*, void*, uint, int>)(*(void***)self)[4])(self, comStream, cacheOptions);

    public static int GetDecoderInfo(void* self, void** decoderInfo)
        => ((delegate* unmanaged[Stdcall]<void*, void**, int>)(*(void***)self)[6])(self, decoderInfo);

    public static int CopyPalette(void* self, void* palette)
        => ((delegate* unmanaged[Stdcall]<void*, void*, int>)(*(void***)self)[7])(self, palette);

    public static int GetMetadataQueryReader(void* self, void** queryReader)
        => ((delegate* unmanaged[Stdcall]<void*, void**, int>)(*(void***)self)[8])(self, queryReader);

    public static int GetPreview(void* self, void** preview)
        => ((delegate* unmanaged[Stdcall]<void*, void**, int>)(*(void***)self)[9])(self, preview);

    public static int GetColorContexts(void* self, uint count, void** colorContexts, out uint actualCount)
    {
        uint actual = 0;
        int hr = ((delegate* unmanaged[Stdcall]<void*, uint, void**, uint*, int>)(*(void***)self)[10])(self, count, colorContexts, &actual);
        actualCount = actual;
        return hr;
    }

    public static int GetThumbnail(void* self, void** thumbnail)
        => ((delegate* unmanaged[Stdcall]<void*, void**, int>)(*(void***)self)[11])(self, thumbnail);
}

internal static unsafe class IWICBitmapFrameDecodeVtbl
{
    public static int GetMetadataQueryReader(void* self, void** queryReader)
        => ((delegate* unmanaged[Stdcall]<void*, void**, int>)(*(void***)self)[8])(self, queryReader);

    public static int GetColorContexts(void* self, uint count, void** colorContexts, out uint actualCount)
    {
        uint actual = 0;
        int hr = ((delegate* unmanaged[Stdcall]<void*, uint, void**, uint*, int>)(*(void***)self)[9])(self, count, colorContexts, &actual);
        actualCount = actual;
        return hr;
    }

    public static int GetThumbnail(void* self, void** thumbnail)
        => ((delegate* unmanaged[Stdcall]<void*, void**, int>)(*(void***)self)[10])(self, thumbnail);
}

internal static unsafe class IWICBitmapEncoderVtbl
{
    public static int Initialize(void* self, void* comStream, uint cacheOption)
        => ((delegate* unmanaged[Stdcall]<void*, void*, uint, int>)(*(void***)self)[3])(self, comStream, cacheOption);

    public static int GetContainerFormat(void* self, out Guid format)
    {
        Guid g = Guid.Empty;
        int hr = ((delegate* unmanaged[Stdcall]<void*, Guid*, int>)(*(void***)self)[4])(self, &g);
        format = g;
        return hr;
    }

    public static int GetEncoderInfo(void* self, void** encoderInfo)
        => ((delegate* unmanaged[Stdcall]<void*, void**, int>)(*(void***)self)[5])(self, encoderInfo);

    public static int SetColorContexts(void* self, uint count, void** colorContexts)
        => ((delegate* unmanaged[Stdcall]<void*, uint, void**, int>)(*(void***)self)[6])(self, count, colorContexts);

    public static int SetPalette(void* self, void* palette)
        => ((delegate* unmanaged[Stdcall]<void*, void*, int>)(*(void***)self)[7])(self, palette);

    public static int SetThumbnail(void* self, void* thumbnail)
        => ((delegate* unmanaged[Stdcall]<void*, void*, int>)(*(void***)self)[8])(self, thumbnail);

    public static int SetPreview(void* self, void* preview)
        => ((delegate* unmanaged[Stdcall]<void*, void*, int>)(*(void***)self)[9])(self, preview);

    public static int CreateNewFrame(void* self, void** frameEncode, void** encoderOptions)
        => ((delegate* unmanaged[Stdcall]<void*, void**, void**, int>)(*(void***)self)[10])(self, frameEncode, encoderOptions);

    public static int Commit(void* self)
        => ((delegate* unmanaged[Stdcall]<void*, int>)(*(void***)self)[11])(self);

    public static int GetMetadataQueryWriter(void* self, void** queryWriter)
        => ((delegate* unmanaged[Stdcall]<void*, void**, int>)(*(void***)self)[12])(self, queryWriter);
}

internal static unsafe class IWICBitmapFrameEncodeVtbl
{
    public static int Initialize(void* self, void* encoderOptions)
        => ((delegate* unmanaged[Stdcall]<void*, void*, int>)(*(void***)self)[3])(self, encoderOptions);

    public static int SetSize(void* self, uint width, uint height)
        => ((delegate* unmanaged[Stdcall]<void*, uint, uint, int>)(*(void***)self)[4])(self, width, height);

    public static int SetResolution(void* self, double dpiX, double dpiY)
        => ((delegate* unmanaged[Stdcall]<void*, double, double, int>)(*(void***)self)[5])(self, dpiX, dpiY);

    public static int SetPixelFormat(void* self, ref Guid pixelFormat)
    {
        Guid fmt = pixelFormat;
        int hr = ((delegate* unmanaged[Stdcall]<void*, Guid*, int>)(*(void***)self)[6])(self, &fmt);
        pixelFormat = fmt;
        return hr;
    }

    public static int SetColorContexts(void* self, uint count, void** colorContexts)
        => ((delegate* unmanaged[Stdcall]<void*, uint, void**, int>)(*(void***)self)[7])(self, count, colorContexts);

    public static int SetPalette(void* self, void* palette)
        => ((delegate* unmanaged[Stdcall]<void*, void*, int>)(*(void***)self)[8])(self, palette);

    public static int SetThumbnail(void* self, void* thumbnail)
        => ((delegate* unmanaged[Stdcall]<void*, void*, int>)(*(void***)self)[9])(self, thumbnail);

    public static int WriteSource(void* self, void* source, WICRect* rect)
        => ((delegate* unmanaged[Stdcall]<void*, void*, WICRect*, int>)(*(void***)self)[11])(self, source, rect);

    public static int Commit(void* self)
        => ((delegate* unmanaged[Stdcall]<void*, int>)(*(void***)self)[12])(self);

    public static int GetMetadataQueryWriter(void* self, void** queryWriter)
        => ((delegate* unmanaged[Stdcall]<void*, void**, int>)(*(void***)self)[13])(self, queryWriter);
}

internal static unsafe class IWICColorContextVtbl
{
    public static int InitializeFromFilename(void* self, string filename)
    {
        fixed (char* p = filename)
            return ((delegate* unmanaged[Stdcall]<void*, char*, int>)(*(void***)self)[3])(self, p);
    }

    public static int InitializeFromMemory(void* self, byte[] buffer, uint bufferSize)
    {
        fixed (byte* p = buffer)
            return ((delegate* unmanaged[Stdcall]<void*, byte*, uint, int>)(*(void***)self)[4])(self, p, bufferSize);
    }

    public static int InitializeFromExifColorSpace(void* self, uint value)
        => ((delegate* unmanaged[Stdcall]<void*, uint, int>)(*(void***)self)[5])(self, value);

    public static int GetType(void* self, out uint type)
    {
        uint t = 0;
        int hr = ((delegate* unmanaged[Stdcall]<void*, uint*, int>)(*(void***)self)[6])(self, &t);
        type = t;
        return hr;
    }

    public static int GetProfileBytes(void* self, uint cbBuffer, byte[] buffer, out uint cbActual)
    {
        uint actual = 0;
        fixed (byte* p = buffer)
        {
            int hr = ((delegate* unmanaged[Stdcall]<void*, uint, byte*, uint*, int>)(*(void***)self)[7])(self, cbBuffer, p, &actual);
            cbActual = actual;
            return hr;
        }
    }

    public static int GetExifColorSpace(void* self, out uint colorSpace)
    {
        uint cs = 0;
        int hr = ((delegate* unmanaged[Stdcall]<void*, uint*, int>)(*(void***)self)[8])(self, &cs);
        colorSpace = cs;
        return hr;
    }
}

internal static unsafe class IWICColorTransformVtbl
{
    public static int Initialize(void* self, void* source, void* sourceContext, void* destContext, in Guid destPixelFormat)
    {
        Guid fmt = destPixelFormat;
        return ((delegate* unmanaged[Stdcall]<void*, void*, void*, void*, Guid*, int>)(*(void***)self)[8])(self, source, sourceContext, destContext, &fmt);
    }
}

internal static unsafe class IWICMetadataQueryReaderVtbl
{
    public static int GetContainerFormat(void* self, out Guid format)
    {
        Guid g = Guid.Empty;
        int hr = ((delegate* unmanaged[Stdcall]<void*, Guid*, int>)(*(void***)self)[3])(self, &g);
        format = g;
        return hr;
    }

    public static int GetLocation(void* self, uint maxLength, char* wzNamespace, out uint actualLength)
    {
        uint actual = 0;
        int hr = ((delegate* unmanaged[Stdcall]<void*, uint, char*, uint*, int>)(*(void***)self)[4])(self, maxLength, wzNamespace, &actual);
        actualLength = actual;
        return hr;
    }

    public static int GetMetadataByName(void* self, string name, void* propVar)
    {
        fixed (char* pName = name)
            return ((delegate* unmanaged[Stdcall]<void*, char*, void*, int>)(*(void***)self)[5])(self, pName, propVar);
    }

    public static int GetEnumerator(void* self, void** enumString)
        => ((delegate* unmanaged[Stdcall]<void*, void**, int>)(*(void***)self)[6])(self, enumString);
}

internal static unsafe class IWICMetadataQueryWriterVtbl
{
    public static int SetMetadataByName(void* self, string name, void* propVar)
    {
        fixed (char* pName = name)
            return ((delegate* unmanaged[Stdcall]<void*, char*, void*, int>)(*(void***)self)[7])(self, pName, propVar);
    }

    public static int RemoveMetadataByName(void* self, string name)
    {
        fixed (char* pName = name)
            return ((delegate* unmanaged[Stdcall]<void*, char*, int>)(*(void***)self)[8])(self, pName);
    }
}

internal static unsafe class IWICMetadataBlockReaderVtbl
{
    public static int GetCount(void* self, out uint count)
    {
        uint c = 0;
        int hr = ((delegate* unmanaged[Stdcall]<void*, uint*, int>)(*(void***)self)[4])(self, &c);
        count = c;
        return hr;
    }

    public static int GetReaderByIndex(void* self, uint index, void** reader)
        => ((delegate* unmanaged[Stdcall]<void*, uint, void**, int>)(*(void***)self)[5])(self, index, reader);
}

internal static unsafe class IWICFastMetadataEncoderVtbl
{
    public static int Commit(void* self)
        => ((delegate* unmanaged[Stdcall]<void*, int>)(*(void***)self)[3])(self);

    public static int GetMetadataQueryWriter(void* self, void** queryWriter)
        => ((delegate* unmanaged[Stdcall]<void*, void**, int>)(*(void***)self)[4])(self, queryWriter);
}

internal static unsafe class IEnumStringVtbl
{
    public static int Next(void* self, uint celt, nint* rgelt, out uint pceltFetched)
    {
        uint fetched = 0;
        int hr = ((delegate* unmanaged[Stdcall]<void*, uint, nint*, uint*, int>)(*(void***)self)[3])(self, celt, rgelt, &fetched);
        pceltFetched = fetched;
        return hr;
    }

    public static int Reset(void* self)
        => ((delegate* unmanaged[Stdcall]<void*, int>)(*(void***)self)[5])(self);
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
internal struct PROPBAG2
{
    internal uint dwType;
    internal ushort vt;
    internal ushort cfType;
    internal nint dwHint;
    internal nint pstrName;
    internal Guid clsid;

    internal void Init(string name)
    {
        pstrName = WicNativeExports.StringToCoTaskMemUni(name);
    }

    internal void Clear()
    {
        WicNativeExports.FreeCoTaskMem(pstrName);
        pstrName = 0;
    }
}

internal static unsafe class IPropertyBag2Vtbl
{
    public static int Write(void* self, uint cProperties, ref PROPBAG2 propBag, void* propVar)
    {
        fixed (PROPBAG2* p = &propBag)
            return ((delegate* unmanaged[Stdcall]<void*, uint, PROPBAG2*, void*, int>)(*(void***)self)[4])(self, cProperties, p, propVar);
    }
}

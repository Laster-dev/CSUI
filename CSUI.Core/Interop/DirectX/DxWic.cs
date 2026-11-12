using System.Runtime.InteropServices;

namespace CSUI.Core.Interop.DirectX;

[StructLayout(LayoutKind.Sequential)]
internal struct WICRect
{
    public int X;
    public int Y;
    public int Width;
    public int Height;

    public WICRect(int x, int y, int width, int height)
    {
        X = x;
        Y = y;
        Width = width;
        Height = height;
    }
}

internal static class WicConst
{
    public const uint COINIT_MULTITHREADED = 0x0;
    public const uint COINIT_APARTMENTTHREADED = 0x2;
    public const uint CLSCTX_INPROC_SERVER = 0x1;
    public const int RPC_E_CHANGED_MODE = unchecked((int)0x80010106);

    public const uint GENERIC_READ = 0x80000000;

    public const uint WICDecodeMetadataCacheOnDemand = 0;
    public const uint WICDecodeMetadataCacheOnLoad = 1;

    public const uint WICBitmapNoCache = 0;
    public const uint WICBitmapCacheOnDemand = 1;
    public const uint WICBitmapCacheOnLoad = 2;

    public const uint WICBitmapDitherTypeNone = 0;
    public const uint WICBitmapPaletteTypeCustom = 0;
}

internal static class WicGuids
{
    public static readonly Guid CLSID_WICImagingFactory2 = new("317d06e8-5f24-433d-bdf7-79ce68d8abc2");
    public static readonly Guid CLSID_WICImagingFactory1 = new("cacaf262-9370-4615-a13b-9f5539da4c0a");
    public static readonly Guid IID_IWICImagingFactory = new("ec5ec8a9-c395-4314-9c77-54d7a935ff70");

    public static readonly Guid IID_IWICBitmapSource = new("00000120-a8f2-4877-ba0a-fd2b6645fb94");
    public static readonly Guid IID_IWICBitmap = new("00000121-a8f2-4877-ba0a-fd2b6645fb94");
    public static readonly Guid IID_IWICFormatConverter = new("00000301-a8f2-4877-ba0a-fd2b6645fb94");
    public static readonly Guid IID_IWICStream = new("135FF860-22B7-4ddf-B0F6-218F4F299A43");
    public static readonly Guid IID_IWICBitmapDecoder = new("9EDDE9E7-8DEE-47ea-99DF-E6FAF2ED44BF");
    public static readonly Guid IID_IWICBitmapFrameDecode = new("3b16811b-6a43-4ec9-a813-3d930c13b940");

    public static readonly Guid GUID_WICPixelFormat32bppBGRA = new("6fddc324-4e03-4bfe-b185-3d77768dc90f");
    public static readonly Guid GUID_WICPixelFormat32bppPBGRA = new("6fddc324-4e03-4bfe-b185-3d77768dc910");
    public static readonly Guid GUID_WICPixelFormat32bppRGBA = new("f5c7ad2d-6a8d-43dd-a7a8-a29935261ae9");
}

internal static unsafe class WicExports
{
    private static readonly nint _ole32 = NativeModule.Load("ole32.dll");

    private static readonly delegate* unmanaged[Stdcall]<void*, uint, int> _coInitializeEx =
        (delegate* unmanaged[Stdcall]<void*, uint, int>)NativeModule.GetExport(_ole32, "CoInitializeEx");

    private static readonly delegate* unmanaged[Stdcall]<Guid*, void*, uint, Guid*, void**, int> _coCreateInstance =
        (delegate* unmanaged[Stdcall]<Guid*, void*, uint, Guid*, void**, int>)NativeModule.GetExport(_ole32, "CoCreateInstance");

    public static void EnsureComInitialized()
    {
        int hr = _coInitializeEx(null, WicConst.COINIT_APARTMENTTHREADED);
        if (hr < 0 && hr != WicConst.RPC_E_CHANGED_MODE)
        {
            HResult.ThrowIfFailed(hr, "CoInitializeEx");
        }
    }

    public static ComPtr<byte> CreateImagingFactory()
    {
        EnsureComInitialized();

        void* raw = null;
        Guid clsid2 = WicGuids.CLSID_WICImagingFactory2;
        Guid iid = WicGuids.IID_IWICImagingFactory;
        int hr = _coCreateInstance(&clsid2, null, WicConst.CLSCTX_INPROC_SERVER, &iid, &raw);
        if (hr < 0 || raw == null)
        {
            Guid clsid1 = WicGuids.CLSID_WICImagingFactory1;
            hr = _coCreateInstance(&clsid1, null, WicConst.CLSCTX_INPROC_SERVER, &iid, &raw);
            HResult.ThrowIfFailed(hr, "CoCreateInstance(CLSID_WICImagingFactory)");
        }

        return new ComPtr<byte>((byte*)raw);
    }
}

internal unsafe struct IWICImagingFactoryVtbl
{
    public static int CreateDecoderFromFilename(
        void* self,
        string filename,
        Guid* vendor,
        uint desiredAccess,
        uint metadataOptions,
        void** decoder)
    {
        fixed (char* pFile = filename)
            return ((delegate* unmanaged[Stdcall]<void*, char*, Guid*, uint, uint, void**, int>)(*(void***)self)[3])(
                self, pFile, vendor, desiredAccess, metadataOptions, decoder);
    }

    public static int CreateDecoderFromStream(
        void* self,
        void* stream,
        Guid* vendor,
        uint metadataOptions,
        void** decoder)
        => ((delegate* unmanaged[Stdcall]<void*, void*, Guid*, uint, void**, int>)(*(void***)self)[4])(
            self, stream, vendor, metadataOptions, decoder);

    public static int CreateFormatConverter(void* self, void** formatConverter)
        => ((delegate* unmanaged[Stdcall]<void*, void**, int>)(*(void***)self)[10])(self, formatConverter);

    public static int CreateStream(void* self, void** wicStream)
        => ((delegate* unmanaged[Stdcall]<void*, void**, int>)(*(void***)self)[14])(self, wicStream);

    public static int CreateBitmapFromMemory(
        void* self,
        uint width,
        uint height,
        in Guid pixelFormat,
        uint stride,
        uint bufferSize,
        byte* buffer,
        void** bitmap)
    {
        fixed (Guid* pFmt = &pixelFormat)
            return ((delegate* unmanaged[Stdcall]<void*, uint, uint, Guid*, uint, uint, byte*, void**, int>)(*(void***)self)[20])(
                self, width, height, pFmt, stride, bufferSize, buffer, bitmap);
    }
}

internal unsafe struct IWICStreamVtbl
{
    public static int InitializeFromFilename(void* self, string filename, uint desiredAccess)
    {
        fixed (char* p = filename)
            return ((delegate* unmanaged[Stdcall]<void*, char*, uint, int>)(*(void***)self)[15])(self, p, desiredAccess);
    }

    public static int InitializeFromMemory(void* self, byte* buffer, uint bufferSize)
        => ((delegate* unmanaged[Stdcall]<void*, byte*, uint, int>)(*(void***)self)[16])(self, buffer, bufferSize);
}

internal unsafe struct IWICBitmapDecoderVtbl
{
    public static int GetFrameCount(void* self, out uint count)
    {
        fixed (uint* p = &count)
            return ((delegate* unmanaged[Stdcall]<void*, uint*, int>)(*(void***)self)[12])(self, p);
    }

    public static int GetFrame(void* self, uint index, void** frameDecode)
        => ((delegate* unmanaged[Stdcall]<void*, uint, void**, int>)(*(void***)self)[13])(self, index, frameDecode);
}

internal unsafe struct IWICBitmapSourceVtbl
{
    public static int GetSize(void* self, out uint width, out uint height)
    {
        fixed (uint* pw = &width)
        fixed (uint* ph = &height)
            return ((delegate* unmanaged[Stdcall]<void*, uint*, uint*, int>)(*(void***)self)[3])(self, pw, ph);
    }

    public static int GetPixelFormat(void* self, out Guid pixelFormat)
    {
        fixed (Guid* p = &pixelFormat)
            return ((delegate* unmanaged[Stdcall]<void*, Guid*, int>)(*(void***)self)[4])(self, p);
    }

    public static int GetResolution(void* self, out double dpiX, out double dpiY)
    {
        fixed (double* px = &dpiX)
        fixed (double* py = &dpiY)
            return ((delegate* unmanaged[Stdcall]<void*, double*, double*, int>)(*(void***)self)[5])(self, px, py);
    }

    public static int CopyPixels(void* self, WICRect* rect, uint stride, uint bufferSize, byte* buffer)
        => ((delegate* unmanaged[Stdcall]<void*, WICRect*, uint, uint, byte*, int>)(*(void***)self)[7])(
            self, rect, stride, bufferSize, buffer);
}

internal unsafe struct IWICFormatConverterVtbl
{
    public static int Initialize(
        void* self,
        void* source,
        in Guid dstFormat,
        uint dither,
        void* palette,
        double alphaThresholdPercent,
        uint paletteTranslate)
    {
        fixed (Guid* pFmt = &dstFormat)
            return ((delegate* unmanaged[Stdcall]<void*, void*, Guid*, uint, void*, double, uint, int>)(*(void***)self)[8])(
                self, source, pFmt, dither, palette, alphaThresholdPercent, paletteTranslate);
    }

    public static int CanConvert(void* self, in Guid srcPixelFormat, in Guid dstPixelFormat, out bool canConvert)
    {
        int b = 0;
        int hr;
        fixed (Guid* pSrc = &srcPixelFormat)
        fixed (Guid* pDst = &dstPixelFormat)
            hr = ((delegate* unmanaged[Stdcall]<void*, Guid*, Guid*, int*, int>)(*(void***)self)[9])(self, pSrc, pDst, &b);
        canConvert = b != 0;
        return hr;
    }
}

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace CSUI.Core.Interop.DirectX;

internal static unsafe class ComHelpers
{
    public static uint AddRef(void* ptr)
    {
        if (ptr != null)
        {
            return ((delegate* unmanaged[Stdcall]<void*, uint>)(*(void***)ptr)[1])(ptr);
        }

        return 0;
    }

    public static void Release(void* ptr)
    {
        if (ptr != null)
        {
            ((delegate* unmanaged[Stdcall]<void*, uint>)(*(void***)ptr)[2])(ptr);
        }
    }
}

internal unsafe struct IDXGIDeviceVtbl
{
    public static int GetAdapter(void* self, void** adapter)
        => ((delegate* unmanaged[Stdcall]<void*, void**, int>)(*(void***)self)[7])(self, adapter);
}

internal unsafe struct IDXGIAdapterVtbl
{
    public static int GetParent(void* self, in Guid riid, void** parent)
    {
        fixed (Guid* p = &riid)
            return ((delegate* unmanaged[Stdcall]<void*, Guid*, void**, int>)(*(void***)self)[6])(self, p, parent);
    }
}

internal unsafe struct IDXGIFactory2Vtbl
{
    public static int MakeWindowAssociation(void* self, nint hwnd, uint flags)
        => ((delegate* unmanaged[Stdcall]<void*, nint, uint, int>)(*(void***)self)[8])(self, hwnd, flags);

    public static int CreateSwapChainForHwnd(
        void* self,
        void* device,
        nint hwnd,
        in DXGI_SWAP_CHAIN_DESC1 desc,
        void* fullscreenDesc,
        void* restrictToOutput,
        void** swapChain)
    {
        fixed (DXGI_SWAP_CHAIN_DESC1* pDesc = &desc)
            return ((delegate* unmanaged[Stdcall]<void*, void*, nint, DXGI_SWAP_CHAIN_DESC1*, void*, void*, void**, int>)(*(void***)self)[15])(
                self, device, hwnd, pDesc, fullscreenDesc, restrictToOutput, swapChain);
    }

    public static int CreateSwapChainForComposition(
        void* self,
        void* device,
        in DXGI_SWAP_CHAIN_DESC1 desc,
        void* restrictToOutput,
        void** swapChain)
    {
        fixed (DXGI_SWAP_CHAIN_DESC1* pDesc = &desc)
            return ((delegate* unmanaged[Stdcall]<void*, void*, DXGI_SWAP_CHAIN_DESC1*, void*, void**, int>)(*(void***)self)[24])(
                self, device, pDesc, restrictToOutput, swapChain);
    }
}

internal unsafe struct IDXGISwapChain1Vtbl
{
    public static int Present(void* self, uint syncInterval, uint flags)
        => ((delegate* unmanaged[Stdcall]<void*, uint, uint, int>)(*(void***)self)[8])(self, syncInterval, flags);

    public static int GetBuffer(void* self, uint buffer, in Guid riid, void** surface)
    {
        fixed (Guid* p = &riid)
            return ((delegate* unmanaged[Stdcall]<void*, uint, Guid*, void**, int>)(*(void***)self)[9])(self, buffer, p, surface);
    }

    public static int ResizeBuffers(void* self, uint bufferCount, uint width, uint height, uint format, uint flags)
        => ((delegate* unmanaged[Stdcall]<void*, uint, uint, uint, uint, uint, int>)(*(void***)self)[13])(
            self, bufferCount, width, height, format, flags);

    public static int Present1(void* self, uint syncInterval, uint flags, DXGI_PRESENT_PARAMETERS* pParameters)
        => ((delegate* unmanaged[Stdcall]<void*, uint, uint, DXGI_PRESENT_PARAMETERS*, int>)(*(void***)self)[22])(
            self, syncInterval, flags, pParameters);
}

internal unsafe struct ID2D1Factory1Vtbl
{
    public static int CreateRoundedRectangleGeometry(void* self, in D2D1_ROUNDED_RECT roundedRect, void** geometry)
    {
        fixed (D2D1_ROUNDED_RECT* p = &roundedRect)
            return ((delegate* unmanaged[Stdcall]<void*, D2D1_ROUNDED_RECT*, void**, int>)(*(void***)self)[6])(self, p, geometry);
    }

    public static int CreateDevice(void* self, void* dxgiDevice, void** d2dDevice)
        => ((delegate* unmanaged[Stdcall]<void*, void*, void**, int>)(*(void***)self)[17])(self, dxgiDevice, d2dDevice);
}

internal unsafe struct ID2D1DeviceVtbl
{
    public static int CreateDeviceContext(void* self, uint options, void** deviceContext)
        => ((delegate* unmanaged[Stdcall]<void*, uint, void**, int>)(*(void***)self)[4])(self, options, deviceContext);
}

internal unsafe struct ID2D1DeviceContextVtbl
{
    // ID2D1RenderTarget slots (4..56)
    public static int CreateSolidColorBrush(void* self, in D2D1_COLOR_F color, void* brushProps, void** brush)
    {
        fixed (D2D1_COLOR_F* p = &color)
            return ((delegate* unmanaged[Stdcall]<void*, D2D1_COLOR_F*, void*, void**, int>)(*(void***)self)[8])(self, p, brushProps, brush);
    }

    public static int CreateGradientStopCollection(
        void* self,
        D2D1_GRADIENT_STOP* stops,
        uint count,
        uint gamma,
        uint extendMode,
        void** collection)
        => ((delegate* unmanaged[Stdcall]<void*, D2D1_GRADIENT_STOP*, uint, uint, uint, void**, int>)(*(void***)self)[9])(
            self, stops, count, gamma, extendMode, collection);

    public static int CreateLinearGradientBrush(
        void* self,
        in D2D1_LINEAR_GRADIENT_BRUSH_PROPERTIES props,
        void* brushProps,
        void* stopCollection,
        void** brush)
    {
        fixed (D2D1_LINEAR_GRADIENT_BRUSH_PROPERTIES* p = &props)
            return ((delegate* unmanaged[Stdcall]<void*, D2D1_LINEAR_GRADIENT_BRUSH_PROPERTIES*, void*, void*, void**, int>)(*(void***)self)[10])(
                self, p, brushProps, stopCollection, brush);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void DrawLine(void* self, D2D1_POINT_2F p0, D2D1_POINT_2F p1, void* brush, float strokeWidth, void* strokeStyle)
        => ((delegate* unmanaged[Stdcall]<void*, D2D1_POINT_2F, D2D1_POINT_2F, void*, float, void*, void>)(*(void***)self)[15])(
            self, p0, p1, brush, strokeWidth, strokeStyle);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void DrawRectangle(void* self, in D2D1_RECT_F rect, void* brush, float strokeWidth, void* strokeStyle)
    {
        fixed (D2D1_RECT_F* p = &rect)
            ((delegate* unmanaged[Stdcall]<void*, D2D1_RECT_F*, void*, float, void*, void>)(*(void***)self)[16])(
                self, p, brush, strokeWidth, strokeStyle);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void FillRectangle(void* self, in D2D1_RECT_F rect, void* brush)
    {
        fixed (D2D1_RECT_F* p = &rect)
            ((delegate* unmanaged[Stdcall]<void*, D2D1_RECT_F*, void*, void>)(*(void***)self)[17])(self, p, brush);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void DrawRoundedRectangle(void* self, in D2D1_ROUNDED_RECT rect, void* brush, float strokeWidth, void* strokeStyle)
    {
        fixed (D2D1_ROUNDED_RECT* p = &rect)
            ((delegate* unmanaged[Stdcall]<void*, D2D1_ROUNDED_RECT*, void*, float, void*, void>)(*(void***)self)[18])(
                self, p, brush, strokeWidth, strokeStyle);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void FillRoundedRectangle(void* self, in D2D1_ROUNDED_RECT rect, void* brush)
    {
        fixed (D2D1_ROUNDED_RECT* p = &rect)
            ((delegate* unmanaged[Stdcall]<void*, D2D1_ROUNDED_RECT*, void*, void>)(*(void***)self)[19])(self, p, brush);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void DrawEllipse(void* self, in D2D1_ELLIPSE ellipse, void* brush, float strokeWidth, void* strokeStyle)
    {
        fixed (D2D1_ELLIPSE* p = &ellipse)
            ((delegate* unmanaged[Stdcall]<void*, D2D1_ELLIPSE*, void*, float, void*, void>)(*(void***)self)[20])(
                self, p, brush, strokeWidth, strokeStyle);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void FillEllipse(void* self, in D2D1_ELLIPSE ellipse, void* brush)
    {
        fixed (D2D1_ELLIPSE* p = &ellipse)
            ((delegate* unmanaged[Stdcall]<void*, D2D1_ELLIPSE*, void*, void>)(*(void***)self)[21])(self, p, brush);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void DrawGeometry(void* self, void* geometry, void* brush, float strokeWidth, void* strokeStyle)
        => ((delegate* unmanaged[Stdcall]<void*, void*, void*, float, void*, void>)(*(void***)self)[22])(
            self, geometry, brush, strokeWidth, strokeStyle);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void FillGeometry(void* self, void* geometry, void* brush, void* opacityBrush)
        => ((delegate* unmanaged[Stdcall]<void*, void*, void*, void*, void>)(*(void***)self)[23])(
            self, geometry, brush, opacityBrush);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void DrawTextLayout(void* self, D2D1_POINT_2F origin, void* textLayout, void* brush, uint options)
        => ((delegate* unmanaged[Stdcall]<void*, D2D1_POINT_2F, void*, void*, uint, void>)(*(void***)self)[28])(
            self, origin, textLayout, brush, options);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void SetTransform(void* self, in D2D1_MATRIX_3X2_F transform)
    {
        fixed (D2D1_MATRIX_3X2_F* p = &transform)
            ((delegate* unmanaged[Stdcall]<void*, D2D1_MATRIX_3X2_F*, void>)(*(void***)self)[30])(self, p);
    }

    public static void SetTextAntialiasMode(void* self, uint mode)
        => ((delegate* unmanaged[Stdcall]<void*, uint, void>)(*(void***)self)[34])(self, mode);

    public static void PopLayer(void* self)
        => ((delegate* unmanaged[Stdcall]<void*, void>)(*(void***)self)[41])(self);

    public static void PushAxisAlignedClip(void* self, in D2D1_RECT_F clipRect, uint antialiasMode)
    {
        fixed (D2D1_RECT_F* p = &clipRect)
            ((delegate* unmanaged[Stdcall]<void*, D2D1_RECT_F*, uint, void>)(*(void***)self)[45])(self, p, antialiasMode);
    }

    public static void PopAxisAlignedClip(void* self)
        => ((delegate* unmanaged[Stdcall]<void*, void>)(*(void***)self)[46])(self);

    public static void Clear(void* self, in D2D1_COLOR_F color)
    {
        fixed (D2D1_COLOR_F* p = &color)
            ((delegate* unmanaged[Stdcall]<void*, D2D1_COLOR_F*, void>)(*(void***)self)[47])(self, p);
    }

    public static void BeginDraw(void* self)
        => ((delegate* unmanaged[Stdcall]<void*, void>)(*(void***)self)[48])(self);

    public static int EndDraw(void* self)
        => ((delegate* unmanaged[Stdcall]<void*, ulong*, ulong*, int>)(*(void***)self)[49])(self, null, null);

    public static void SetDpi(void* self, float dpiX, float dpiY)
        => ((delegate* unmanaged[Stdcall]<void*, float, float, void>)(*(void***)self)[51])(self, dpiX, dpiY);

    // ID2D1DeviceContext slots (57+)
    public static int CreateBitmapFromDxgiSurface(void* self, void* surface, in D2D1_BITMAP_PROPERTIES1 props, void** bitmap)
    {
        fixed (D2D1_BITMAP_PROPERTIES1* p = &props)
            return ((delegate* unmanaged[Stdcall]<void*, void*, D2D1_BITMAP_PROPERTIES1*, void**, int>)(*(void***)self)[62])(
                self, surface, p, bitmap);
    }

    public static void SetTarget(void* self, void* image)
        => ((delegate* unmanaged[Stdcall]<void*, void*, void>)(*(void***)self)[74])(self, image);

    public static void PushLayer(void* self, in D2D1_LAYER_PARAMETERS1 parameters, void* layer)
    {
        fixed (D2D1_LAYER_PARAMETERS1* p = &parameters)
            ((delegate* unmanaged[Stdcall]<void*, D2D1_LAYER_PARAMETERS1*, void*, void>)(*(void***)self)[86])(self, p, layer);
    }

    public static int CreateEffect(void* self, in Guid effectId, void** effect)
    {
        fixed (Guid* p = &effectId)
            return ((delegate* unmanaged[Stdcall]<void*, Guid*, void**, int>)(*(void***)self)[63])(self, p, effect);
    }

    public static int CreateCommandList(void* self, void** commandList)
        => ((delegate* unmanaged[Stdcall]<void*, void**, int>)(*(void***)self)[67])(self, commandList);

    public static void GetDevice(void* self, void** device)
        => ((delegate* unmanaged[Stdcall]<void*, void**, void>)(*(void***)self)[73])(self, device);

    public static void GetTarget(void* self, void** image)
        => ((delegate* unmanaged[Stdcall]<void*, void**, void>)(*(void***)self)[75])(self, image);

    public static void DrawImage(
        void* self,
        void* image,
        D2D1_POINT_2F* targetOffset,
        D2D1_RECT_F* imageRectangle,
        uint interpolationMode,
        uint compositeMode)
        => ((delegate* unmanaged[Stdcall]<void*, void*, D2D1_POINT_2F*, D2D1_RECT_F*, uint, uint, void>)(*(void***)self)[83])(
            self, image, targetOffset, imageRectangle, interpolationMode, compositeMode);
}

internal static class D2D1EffectGuids
{
    public static readonly Guid CLSID_D2D1GaussianBlur = new("1feb6d69-2fe6-4ac9-8c58-1d7f93e7a6a5");
    public static readonly Guid CLSID_D2D1Shadow = new("c67ea361-1863-4e69-89db-695d3e9a5b6b");
    public static readonly Guid CLSID_D2D12DAffineTransform = new("6aa97485-6354-4cfc-908c-e4a74f62c96c");
    public static readonly Guid CLSID_D2D1Composite = new("48fc9f51-f6ac-48f1-8b58-3b28ac46f76d");

    public const uint D2D1_PROPERTY_TYPE_FLOAT = 5;
    public const uint D2D1_PROPERTY_TYPE_VECTOR4 = 8;
    public const uint D2D1_PROPERTY_TYPE_BLOB = 9;
    public const uint D2D1_PROPERTY_TYPE_ENUM = 11;

    public const uint D2D1_GAUSSIANBLUR_PROP_STANDARD_DEVIATION = 0;
    public const uint D2D1_GAUSSIANBLUR_PROP_OPTIMIZATION = 1;
    public const uint D2D1_GAUSSIANBLUR_PROP_BORDER_MODE = 2;

    public const uint D2D1_SHADOW_PROP_BLUR_STANDARD_DEVIATION = 0;
    public const uint D2D1_SHADOW_PROP_COLOR = 1;
    public const uint D2D1_SHADOW_PROP_OPTIMIZATION = 2;

    public const uint D2D1_2DAFFINETRANSFORM_PROP_TRANSFORM_MATRIX = 2;

    public const uint D2D1_COMPOSITE_MODE_SOURCE_OVER = 0;
}

internal unsafe struct ID2D1EffectVtbl
{
    public static int SetValue(void* self, uint index, uint type, byte* data, uint dataSize)
        => ((delegate* unmanaged[Stdcall]<void*, uint, uint, byte*, uint, int>)(*(void***)self)[9])(self, index, type, data, dataSize);

    public static int SetFloat(void* self, uint index, float value)
        => SetValue(self, index, D2D1EffectGuids.D2D1_PROPERTY_TYPE_FLOAT, (byte*)&value, sizeof(float));

    public static int SetEnum(void* self, uint index, uint value)
        => SetValue(self, index, D2D1EffectGuids.D2D1_PROPERTY_TYPE_ENUM, (byte*)&value, sizeof(uint));

    public static int SetVector4(void* self, uint index, in D2D1_COLOR_F value)
    {
        fixed (D2D1_COLOR_F* p = &value)
            return SetValue(self, index, D2D1EffectGuids.D2D1_PROPERTY_TYPE_VECTOR4, (byte*)p, (uint)sizeof(D2D1_COLOR_F));
    }

    public static int SetMatrix3x2(void* self, uint index, in D2D1_MATRIX_3X2_F value)
    {
        fixed (D2D1_MATRIX_3X2_F* p = &value)
            return SetValue(self, index, D2D1EffectGuids.D2D1_PROPERTY_TYPE_BLOB, (byte*)p, (uint)sizeof(D2D1_MATRIX_3X2_F));
    }

    public static void SetInput(void* self, uint index, void* input, int invalidate = 1)
        => ((delegate* unmanaged[Stdcall]<void*, uint, void*, int, void>)(*(void***)self)[14])(self, index, input, invalidate);

    public static void GetOutput(void* self, void** outputImage)
        => ((delegate* unmanaged[Stdcall]<void*, void**, void>)(*(void***)self)[18])(self, outputImage);
}

internal unsafe struct ID2D1CommandListVtbl
{
    public static int Close(void* self)
        => ((delegate* unmanaged[Stdcall]<void*, int>)(*(void***)self)[5])(self);
}

internal unsafe struct IDWriteFactoryVtbl
{
    public static int CreateTextFormat(
        void* self,
        string fontFamily,
        void* fontCollection,
        uint weight,
        uint style,
        uint stretch,
        float fontSize,
        string localeName,
        void** textFormat)
    {
        fixed (char* pFont = fontFamily)
        fixed (char* pLocale = localeName)
        {
            return ((delegate* unmanaged[Stdcall]<void*, char*, void*, uint, uint, uint, float, char*, void**, int>)(*(void***)self)[15])(
                self, pFont, fontCollection, weight, style, stretch, fontSize, pLocale, textFormat);
        }
    }

    public static int CreateTextLayout(
        void* self,
        string text,
        void* textFormat,
        float maxWidth,
        float maxHeight,
        void** textLayout)
    {
        fixed (char* pText = text)
        {
            return ((delegate* unmanaged[Stdcall]<void*, char*, uint, void*, float, float, void**, int>)(*(void***)self)[18])(
                self, pText, (uint)text.Length, textFormat, maxWidth, maxHeight, textLayout);
        }
    }
}

internal unsafe struct IDWriteTextFormatVtbl
{
    public static int SetTextAlignment(void* self, uint alignment)
        => ((delegate* unmanaged[Stdcall]<void*, uint, int>)(*(void***)self)[3])(self, alignment);

    public static int SetParagraphAlignment(void* self, uint alignment)
        => ((delegate* unmanaged[Stdcall]<void*, uint, int>)(*(void***)self)[4])(self, alignment);

    public static int SetWordWrapping(void* self, uint wrapping)
        => ((delegate* unmanaged[Stdcall]<void*, uint, int>)(*(void***)self)[5])(self, wrapping);
}

internal unsafe struct IDWriteTextLayoutVtbl
{
    public static int SetMaxWidth(void* self, float maxWidth)
        => ((delegate* unmanaged[Stdcall]<void*, float, int>)(*(void***)self)[28])(self, maxWidth);

    public static int SetMaxHeight(void* self, float maxHeight)
        => ((delegate* unmanaged[Stdcall]<void*, float, int>)(*(void***)self)[29])(self, maxHeight);

    public static int GetMetrics(void* self, out DWRITE_TEXT_METRICS metrics)
    {
        fixed (DWRITE_TEXT_METRICS* p = &metrics)
            return ((delegate* unmanaged[Stdcall]<void*, DWRITE_TEXT_METRICS*, int>)(*(void***)self)[60])(self, p);
    }
}

[StructLayout(LayoutKind.Sequential)]
internal struct D2D1_RENDER_TARGET_PROPERTIES
{
    public uint Type;
    public D2D1_PIXEL_FORMAT PixelFormat;
    public float DpiX;
    public float DpiY;
    public uint Usage;
    public uint MinLevel;
}

internal unsafe struct ID2D1FactoryVtbl
{
    public static int CreateWicBitmapRenderTarget(
        void* self,
        void* wicBitmap,
        in D2D1_RENDER_TARGET_PROPERTIES renderTargetProperties,
        void** renderTarget)
    {
        fixed (D2D1_RENDER_TARGET_PROPERTIES* p = &renderTargetProperties)
            return ((delegate* unmanaged[Stdcall]<void*, void*, D2D1_RENDER_TARGET_PROPERTIES*, void**, int>)(*(void***)self)[13])(
                self, wicBitmap, p, renderTarget);
    }
}

internal unsafe struct ID2D1MultithreadVtbl
{
    public static int GetMultithreadProtected(void* self)
        => ((delegate* unmanaged[Stdcall]<void*, int>)(*(void***)self)[3])(self);

    public static void Enter(void* self)
        => ((delegate* unmanaged[Stdcall]<void*, void>)(*(void***)self)[4])(self);

    public static void Leave(void* self)
        => ((delegate* unmanaged[Stdcall]<void*, void>)(*(void***)self)[5])(self);
}

internal unsafe struct ID3D11MultithreadVtbl
{
    public static void Enter(void* self)
        => ((delegate* unmanaged[Stdcall]<void*, void>)(*(void***)self)[3])(self);

    public static void Leave(void* self)
        => ((delegate* unmanaged[Stdcall]<void*, void>)(*(void***)self)[4])(self);

    public static int SetMultithreadProtected(void* self, int bMTProtect)
        => ((delegate* unmanaged[Stdcall]<void*, int, int>)(*(void***)self)[5])(self, bMTProtect);

    public static int GetMultithreadProtected(void* self)
        => ((delegate* unmanaged[Stdcall]<void*, int>)(*(void***)self)[6])(self);
}

internal unsafe struct ID3D11DeviceBaseVtbl
{
    public static int CreateTexture2D(void* self, in D3D11_TEXTURE2D_DESC desc, D3D11_SUBRESOURCE_DATA* initialData, void** texture2D)
    {
        fixed (D3D11_TEXTURE2D_DESC* p = &desc)
            return ((delegate* unmanaged[Stdcall]<void*, D3D11_TEXTURE2D_DESC*, D3D11_SUBRESOURCE_DATA*, void**, int>)(*(void***)self)[5])(
                self, p, initialData, texture2D);
    }
}

internal unsafe struct ID3D11DeviceContextBaseVtbl
{
    public static int Map(void* self, void* resource, uint subresource, uint mapType, uint mapFlags, out D3D11_MAPPED_SUBRESOURCE mappedResource)
    {
        fixed (D3D11_MAPPED_SUBRESOURCE* p = &mappedResource)
            return ((delegate* unmanaged[Stdcall]<void*, void*, uint, uint, uint, D3D11_MAPPED_SUBRESOURCE*, int>)(*(void***)self)[14])(
                self, resource, subresource, mapType, mapFlags, p);
    }

    public static void Unmap(void* self, void* resource, uint subresource)
        => ((delegate* unmanaged[Stdcall]<void*, void*, uint, void>)(*(void***)self)[15])(self, resource, subresource);

    public static void CopyResource(void* self, void* dstResource, void* srcResource)
        => ((delegate* unmanaged[Stdcall]<void*, void*, void*, void>)(*(void***)self)[47])(self, dstResource, srcResource);

    public static void Flush(void* self)
        => ((delegate* unmanaged[Stdcall]<void*, void>)(*(void***)self)[111])(self);
}

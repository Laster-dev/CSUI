using System.Runtime.InteropServices;

namespace CSUI.Core.Interop.DirectX;

[StructLayout(LayoutKind.Sequential)]
public struct RECT
{
    public int left;
    public int top;
    public int right;
    public int bottom;

    public RECT(int left, int top, int right, int bottom)
    {
        this.left = left;
        this.top = top;
        this.right = right;
        this.bottom = bottom;
    }

    public int Width => right - left;
    public int Height => bottom - top;
}

[StructLayout(LayoutKind.Sequential)]
internal struct D2D1_COLOR_F
{
    public float R;
    public float G;
    public float B;
    public float A;

    public D2D1_COLOR_F(float r, float g, float b, float a = 1f)
    {
        R = r;
        G = g;
        B = b;
        A = a;
    }
}

[StructLayout(LayoutKind.Sequential)]
internal struct D2D1_POINT_2F
{
    public float X;
    public float Y;

    public D2D1_POINT_2F(float x, float y)
    {
        X = x;
        Y = y;
    }
}

[StructLayout(LayoutKind.Sequential)]
internal struct D2D1_RECT_F
{
    public float Left;
    public float Top;
    public float Right;
    public float Bottom;

    public D2D1_RECT_F(float left, float top, float right, float bottom)
    {
        Left = left;
        Top = top;
        Right = right;
        Bottom = bottom;
    }
}

[StructLayout(LayoutKind.Sequential)]
internal struct D2D1_ROUNDED_RECT
{
    public D2D1_RECT_F Rect;
    public float RadiusX;
    public float RadiusY;
}

[StructLayout(LayoutKind.Sequential)]
internal struct D2D1_ELLIPSE
{
    public D2D1_POINT_2F Point;
    public float RadiusX;
    public float RadiusY;
}

[StructLayout(LayoutKind.Sequential)]
internal struct D2D1_MATRIX_3X2_F
{
    public float M11;
    public float M12;
    public float M21;
    public float M22;
    public float M31;
    public float M32;

    public static D2D1_MATRIX_3X2_F Identity => new() { M11 = 1f, M22 = 1f };

    public static D2D1_MATRIX_3X2_F Translation(float dx, float dy)
        => new() { M11 = 1f, M22 = 1f, M31 = dx, M32 = dy };
}

[StructLayout(LayoutKind.Sequential)]
internal struct D2D1_BRUSH_PROPERTIES
{
    public float Opacity;
    public D2D1_MATRIX_3X2_F Transform;
}

[StructLayout(LayoutKind.Sequential)]
internal struct D2D1_GRADIENT_STOP
{
    public float Position;
    public D2D1_COLOR_F Color;
}

[StructLayout(LayoutKind.Sequential)]
internal struct D2D1_LINEAR_GRADIENT_BRUSH_PROPERTIES
{
    public D2D1_POINT_2F StartPoint;
    public D2D1_POINT_2F EndPoint;
}

[StructLayout(LayoutKind.Sequential)]
internal struct D2D1_PIXEL_FORMAT
{
    public uint Format;
    public uint AlphaMode;
}

[StructLayout(LayoutKind.Sequential)]
internal unsafe struct D2D1_BITMAP_PROPERTIES1
{
    public D2D1_PIXEL_FORMAT PixelFormat;
    public float DpiX;
    public float DpiY;
    public uint BitmapOptions;
    public void* ColorContext;
}

[StructLayout(LayoutKind.Sequential)]
internal unsafe struct D2D1_LAYER_PARAMETERS1
{
    public D2D1_RECT_F ContentBounds;
    public void* GeometricMask;
    public uint MaskAntialiasMode;
    public D2D1_MATRIX_3X2_F MaskTransform;
    public float Opacity;
    public void* OpacityBrush;
    public uint LayerOptions;
}

[StructLayout(LayoutKind.Sequential)]
internal struct D2D1_FACTORY_OPTIONS
{
    public uint DebugLevel;
}

[StructLayout(LayoutKind.Sequential)]
internal struct DXGI_SAMPLE_DESC
{
    public uint Count;
    public uint Quality;
}

[StructLayout(LayoutKind.Sequential)]
internal struct DXGI_SWAP_CHAIN_DESC1
{
    public uint Width;
    public uint Height;
    public uint Format;
    public int Stereo;
    public DXGI_SAMPLE_DESC SampleDesc;
    public uint BufferUsage;
    public uint BufferCount;
    public uint Scaling;
    public uint SwapEffect;
    public uint AlphaMode;
    public uint Flags;
}

[StructLayout(LayoutKind.Sequential)]
internal unsafe struct DXGI_PRESENT_PARAMETERS
{
    public uint DirtyRectsCount;
    public void* DirtyRects;
    public void* ScrollRect;
    public void* ScrollOffset;
}

[StructLayout(LayoutKind.Sequential)]
internal struct DWRITE_TEXT_METRICS
{
    public float Left;
    public float Top;
    public float Width;
    public float WidthIncludingTrailingWhitespace;
    public float Height;
    public float LayoutWidth;
    public float LayoutHeight;
    public uint MaxBidiReorderingDepth;
    public uint LineCount;
}

[StructLayout(LayoutKind.Sequential)]
internal unsafe struct D2D1_MAPPED_RECT
{
    public uint Pitch;
    public byte* Bits;
}

[StructLayout(LayoutKind.Sequential)]
internal struct D3D11_TEXTURE2D_DESC
{
    public uint Width;
    public uint Height;
    public uint MipLevels;
    public uint ArraySize;
    public uint Format;
    public DXGI_SAMPLE_DESC SampleDesc;
    public uint Usage;
    public uint BindFlags;
    public uint CPUAccessFlags;
    public uint MiscFlags;
}

[StructLayout(LayoutKind.Sequential)]
internal unsafe struct D3D11_SUBRESOURCE_DATA
{
    public void* PSysMem;
    public uint SysMemPitch;
    public uint SysMemSlicePitch;
}

[StructLayout(LayoutKind.Sequential)]
internal unsafe struct D3D11_MAPPED_SUBRESOURCE
{
    public void* PData;
    public uint RowPitch;
    public uint DepthPitch;
}

[StructLayout(LayoutKind.Sequential)]
internal struct D3D11_BOX
{
    public uint Left;
    public uint Top;
    public uint Front;
    public uint Right;
    public uint Bottom;
    public uint Back;
}

internal static class DxConst
{
    public const uint D3D_DRIVER_TYPE_HARDWARE = 1;
    public const uint D3D_DRIVER_TYPE_WARP = 5;
    public const uint D3D11_CREATE_DEVICE_BGRA_SUPPORT = 0x20;
    public const uint D3D11_SDK_VERSION = 7;

    public const uint D3D11_USAGE_DEFAULT = 0;
    public const uint D3D11_USAGE_IMMUTABLE = 1;
    public const uint D3D11_USAGE_DYNAMIC = 2;
    public const uint D3D11_USAGE_STAGING = 3;

    public const uint D3D11_BIND_VERTEX_BUFFER = 0x1;
    public const uint D3D11_BIND_INDEX_BUFFER = 0x2;
    public const uint D3D11_BIND_CONSTANT_BUFFER = 0x4;
    public const uint D3D11_BIND_SHADER_RESOURCE = 0x8;
    public const uint D3D11_BIND_RENDER_TARGET = 0x20;
    public const uint D3D11_BIND_DEPTH_STENCIL = 0x40;

    public const uint D3D11_CPU_ACCESS_WRITE = 0x10000;
    public const uint D3D11_CPU_ACCESS_READ = 0x20000;

    public const uint D3D11_MAP_READ = 1;
    public const uint D3D11_MAP_WRITE_DISCARD = 4;

    public const uint DXGI_FORMAT_UNKNOWN = 0;
    public const uint DXGI_FORMAT_R32G32B32A32_FLOAT = 2;
    public const uint DXGI_FORMAT_R32G32B32_FLOAT = 6;
    public const uint DXGI_FORMAT_R32G32_FLOAT = 16;
    public const uint DXGI_FORMAT_R32_UINT = 42;
    public const uint DXGI_FORMAT_D24_UNORM_S8_UINT = 45;
    public const uint DXGI_FORMAT_B8G8R8A8_UNORM = 87;
    public const uint DXGI_USAGE_RENDER_TARGET_OUTPUT = 0x20;
    public const uint DXGI_SCALING_STRETCH = 0;
    public const uint DXGI_SCALING_NONE = 1;
    public const uint DXGI_SWAP_EFFECT_FLIP_SEQUENTIAL = 3;
    public const uint DXGI_SWAP_EFFECT_FLIP_DISCARD = 4;
    public const uint DXGI_ALPHA_MODE_PREMULTIPLIED = 1;
    public const uint DXGI_ALPHA_MODE_IGNORE = 3;

    public const uint D2D1_FACTORY_TYPE_SINGLE_THREADED = 0;
    public const uint D2D1_FACTORY_TYPE_MULTI_THREADED = 1;
    public const uint D2D1_DEVICE_CONTEXT_OPTIONS_NONE = 0;
    public const uint D2D1_ALPHA_MODE_PREMULTIPLIED = 1;
    public const uint D2D1_ALPHA_MODE_IGNORE = 3;
    public const uint D2D1_BITMAP_OPTIONS_NONE = 0;
    public const uint D2D1_BITMAP_OPTIONS_TARGET = 0x1;
    public const uint D2D1_BITMAP_OPTIONS_CANNOT_DRAW = 0x2;
    public const uint D2D1_BITMAP_OPTIONS_CPU_READ = 0x4;
    public const uint D2D1_INTERPOLATION_MODE_NEAREST_NEIGHBOR = 0;
    public const uint D2D1_INTERPOLATION_MODE_LINEAR = 1;
    public const uint D2D1_COMPOSITE_MODE_SOURCE_OVER = 0;
    public const uint D2D1_MAP_OPTIONS_READ = 1;
    public const uint D2D1_ANTIALIAS_MODE_PER_PRIMITIVE = 0;
    public const uint D2D1_TEXT_ANTIALIAS_MODE_CLEARTYPE = 1;
    public const uint D2D1_TEXT_ANTIALIAS_MODE_GRAYSCALE = 2;
    public const uint D2D1_DRAW_TEXT_OPTIONS_CLIP = 0x2;
    public const uint D2D1_DRAW_TEXT_OPTIONS_ENABLE_COLOR_FONT = 0x4;
    public const uint D2D1_GAMMA_2_2 = 0;
    public const uint D2D1_EXTEND_MODE_CLAMP = 0;

    public const uint DWRITE_FACTORY_TYPE_SHARED = 0;
    public const uint DWRITE_FONT_STYLE_NORMAL = 0;
    public const uint DWRITE_FONT_STRETCH_NORMAL = 5;
}

internal static class DxGuids
{
    public static readonly Guid IID_IDXGIDevice = new("54ec77fa-1377-44e6-8c32-88fd5f44c84c");
    public static readonly Guid IID_IDXGIAdapter = new("2411e7e1-12ac-4ccf-bd14-9798e8534dc0");
    public static readonly Guid IID_IDXGIFactory2 = new("50c83a1c-e072-4c48-87b0-3630fa36a6d0");
    public static readonly Guid IID_IDXGISurface = new("cafcb56c-6ac3-4889-bf47-9e23bbd260ec");
    public static readonly Guid IID_ID2D1Factory1 = new("bb12d362-daee-4b9a-aa1d-14ba401cfa1f");
    public static readonly Guid IID_ID2D1Multithread = new("31e6e7bc-e0ff-4d46-8c64-a0a8c41c15d3");
    public static readonly Guid IID_ID3D11Texture2D = new("6f15aaf2-d208-4e89-9ab4-489535d34f9c");
    public static readonly Guid IID_ID3D11DeviceContext = new("c0bfa96c-e089-44fb-8eaf-26f8796190da");
    public static readonly Guid IID_ID3D11Multithread = new("9b7e4e00-342c-4106-a19f-4f2704f689f0");
    public static readonly Guid IID_IDWriteFactory = new("b859ee5a-d838-4b5b-a2e8-1adc7d93db48");
    public static readonly Guid IID_IDCompositionDevice = new("c37d2e25-e4bc-45d8-ab24-4238d2e6464f");
    public static readonly Guid IID_IDCompositionTarget = new("eacdd04c-117e-4e17-88f4-d1b12b0e3d89");
    public static readonly Guid IID_IDCompositionVisual = new("4d93059d-097b-4651-9a60-f0f25116e2f3");
}

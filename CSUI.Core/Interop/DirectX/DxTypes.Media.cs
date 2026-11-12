using System.Runtime.InteropServices;

namespace CSUI.Core.Interop.DirectX;

[StructLayout(LayoutKind.Sequential)]
internal struct D2D1_SIZE_F
{
    public float width;
    public float height;

    public D2D1_SIZE_F(float width, float height)
    {
        this.width = width;
        this.height = height;
    }
}

[StructLayout(LayoutKind.Sequential)]
internal struct D2D1_SIZE_U
{
    public uint width;
    public uint height;

    public D2D1_SIZE_U(uint width, uint height)
    {
        this.width = width;
        this.height = height;
    }
}

[StructLayout(LayoutKind.Sequential)]
internal struct D2D1_POINT_2U
{
    public uint x;
    public uint y;

    public D2D1_POINT_2U(uint x, uint y)
    {
        this.x = x;
        this.y = y;
    }
}

[StructLayout(LayoutKind.Sequential)]
internal struct D2D1_RECT_U
{
    public uint left;
    public uint top;
    public uint right;
    public uint bottom;

    public D2D1_RECT_U(uint left, uint top, uint right, uint bottom)
    {
        this.left = left;
        this.top = top;
        this.right = right;
        this.bottom = bottom;
    }
}

[StructLayout(LayoutKind.Sequential)]
internal struct D2D1_BEZIER_SEGMENT
{
    public D2D1_POINT_2F point1;
    public D2D1_POINT_2F point2;
    public D2D1_POINT_2F point3;
}

[StructLayout(LayoutKind.Sequential)]
internal struct D2D1_ARC_SEGMENT
{
    public D2D1_POINT_2F point;
    public D2D1_SIZE_F size;
    public float rotationAngle;
    public uint sweepDirection;
    public uint arcSize;
}

[StructLayout(LayoutKind.Sequential)]
internal struct D2D1_QUADRATIC_BEZIER_SEGMENT
{
    public D2D1_POINT_2F point1;
    public D2D1_POINT_2F point2;
}

[StructLayout(LayoutKind.Sequential)]
internal struct D2D1_STROKE_STYLE_PROPERTIES
{
    public uint startCap;
    public uint endCap;
    public uint dashCap;
    public uint lineJoin;
    public float miterLimit;
    public uint dashStyle;
    public float dashOffset;
}

[StructLayout(LayoutKind.Sequential)]
internal struct D2D1_STROKE_STYLE_PROPERTIES1
{
    public uint startCap;
    public uint endCap;
    public uint dashCap;
    public uint lineJoin;
    public float miterLimit;
    public uint dashStyle;
    public float dashOffset;
    public uint transformType;
}

[StructLayout(LayoutKind.Sequential)]
internal struct D2D1_RADIAL_GRADIENT_BRUSH_PROPERTIES
{
    public D2D1_POINT_2F center;
    public D2D1_POINT_2F gradientOriginOffset;
    public float radiusX;
    public float radiusY;
}

[StructLayout(LayoutKind.Sequential)]
internal struct D2D1_BITMAP_BRUSH_PROPERTIES
{
    public uint extendModeX;
    public uint extendModeY;
    public uint interpolationMode;
}

[StructLayout(LayoutKind.Sequential)]
internal struct D2D1_BITMAP_BRUSH_PROPERTIES1
{
    public uint extendModeX;
    public uint extendModeY;
    public uint interpolationMode;
}

[StructLayout(LayoutKind.Sequential)]
internal struct D2D1_BITMAP_PROPERTIES
{
    public D2D1_PIXEL_FORMAT pixelFormat;
    public float dpiX;
    public float dpiY;
}

[StructLayout(LayoutKind.Sequential)]
internal unsafe struct D2D1_LAYER_PARAMETERS
{
    public D2D1_RECT_F contentBounds;
    public void* geometricMask;
    public uint maskAntialiasMode;
    public D2D1_MATRIX_3X2_F maskTransform;
    public float opacity;
    public void* opacityBrush;
    public uint layerOptions;
}

[StructLayout(LayoutKind.Sequential)]
internal struct DWRITE_TEXT_RANGE
{
    public uint startPosition;
    public uint length;

    public DWRITE_TEXT_RANGE(uint startPosition, uint length)
    {
        this.startPosition = startPosition;
        this.length = length;
    }
}

[StructLayout(LayoutKind.Sequential)]
internal struct DWRITE_TRIMMING
{
    public uint granularity;
    public uint delimiter;
    public uint delimiterCount;
}

[StructLayout(LayoutKind.Sequential)]
internal struct DWRITE_LINE_METRICS
{
    public uint length;
    public uint trailingWhitespaceLength;
    public uint newlineLength;
    public float height;
    public float baseline;
    public int isTrimmed;
}

[StructLayout(LayoutKind.Sequential)]
internal struct DWRITE_CLUSTER_METRICS
{
    public float width;
    public ushort length;
    public ushort flags;

    public readonly bool CanWrapLineAfter => (flags & 0x0001) != 0;
    public readonly bool IsWhitespace => (flags & 0x0002) != 0;
    public readonly bool IsNewline => (flags & 0x0004) != 0;
    public readonly bool IsSoftHyphen => (flags & 0x0008) != 0;
    public readonly bool IsRightToLeft => (flags & 0x0010) != 0;
}

[StructLayout(LayoutKind.Sequential)]
internal struct DWRITE_OVERHANG_METRICS
{
    public float left;
    public float top;
    public float right;
    public float bottom;
}

[StructLayout(LayoutKind.Sequential)]
internal struct DWRITE_HIT_TEST_METRICS
{
    public uint textPosition;
    public uint length;
    public float left;
    public float top;
    public float width;
    public float height;
    public uint bidiLevel;
    public int isText;
    public int isTrimmed;
}

[StructLayout(LayoutKind.Sequential)]
internal struct DWRITE_FONT_METRICS
{
    public ushort designUnitsPerEm;
    public ushort ascent;
    public ushort descent;
    public short lineGap;
    public ushort capHeight;
    public ushort xHeight;
    public short underlinePosition;
    public ushort underlineThickness;
    public short strikethroughPosition;
    public ushort strikethroughThickness;
}

internal static class DxMediaConst
{
    public const float D2D1_DEFAULT_FLATTENING_TOLERANCE = 0.25f;

    public const uint D2D1_FILL_MODE_ALTERNATE = 0;
    public const uint D2D1_FILL_MODE_WINDING = 1;

    public const uint D2D1_PATH_SEGMENT_NONE = 0x00000000;
    public const uint D2D1_PATH_SEGMENT_FORCE_UNSTROKED = 0x00000001;
    public const uint D2D1_PATH_SEGMENT_FORCE_ROUND_LINE_JOIN = 0x00000002;

    public const uint D2D1_SWEEP_DIRECTION_COUNTER_CLOCKWISE = 0;
    public const uint D2D1_SWEEP_DIRECTION_CLOCKWISE = 1;

    public const uint D2D1_ARC_SIZE_SMALL = 0;
    public const uint D2D1_ARC_SIZE_LARGE = 1;

    public const uint D2D1_FIGURE_BEGIN_FILLED = 0;
    public const uint D2D1_FIGURE_BEGIN_HOLLOW = 1;

    public const uint D2D1_FIGURE_END_OPEN = 0;
    public const uint D2D1_FIGURE_END_CLOSED = 1;

    public const uint D2D1_CAP_STYLE_FLAT = 0;
    public const uint D2D1_CAP_STYLE_SQUARE = 1;
    public const uint D2D1_CAP_STYLE_ROUND = 2;
    public const uint D2D1_CAP_STYLE_TRIANGLE = 3;

    public const uint D2D1_DASH_STYLE_SOLID = 0;
    public const uint D2D1_DASH_STYLE_DASH = 1;
    public const uint D2D1_DASH_STYLE_DOT = 2;
    public const uint D2D1_DASH_STYLE_DASH_DOT = 3;
    public const uint D2D1_DASH_STYLE_DASH_DOT_DOT = 4;
    public const uint D2D1_DASH_STYLE_CUSTOM = 5;

    public const uint D2D1_LINE_JOIN_MITER = 0;
    public const uint D2D1_LINE_JOIN_BEVEL = 1;
    public const uint D2D1_LINE_JOIN_ROUND = 2;
    public const uint D2D1_LINE_JOIN_MITER_OR_BEVEL = 3;

    public const uint D2D1_STROKE_TRANSFORM_TYPE_NORMAL = 0;
    public const uint D2D1_STROKE_TRANSFORM_TYPE_FIXED = 1;
    public const uint D2D1_STROKE_TRANSFORM_TYPE_HAIRLINE = 2;

    public const uint D2D1_COMBINE_MODE_UNION = 0;
    public const uint D2D1_COMBINE_MODE_INTERSECT = 1;
    public const uint D2D1_COMBINE_MODE_XOR = 2;
    public const uint D2D1_COMBINE_MODE_EXCLUDE = 3;

    public const uint D2D1_GEOMETRY_RELATION_UNKNOWN = 0;
    public const uint D2D1_GEOMETRY_RELATION_DISJOINT = 1;
    public const uint D2D1_GEOMETRY_RELATION_IS_CONTAINED = 2;
    public const uint D2D1_GEOMETRY_RELATION_CONTAINS = 3;
    public const uint D2D1_GEOMETRY_RELATION_OVERLAP = 4;

    public const uint D2D1_GEOMETRY_SIMPLIFICATION_OPTION_CUBICS_AND_LINES = 0;
    public const uint D2D1_GEOMETRY_SIMPLIFICATION_OPTION_LINES = 1;

    public const uint D2D1_EXTEND_MODE_WRAP = 1;
    public const uint D2D1_EXTEND_MODE_MIRROR = 2;

    public const uint D2D1_GAMMA_1_0 = 1;

    public const uint D2D1_BITMAP_INTERPOLATION_MODE_NEAREST_NEIGHBOR = 0;
    public const uint D2D1_BITMAP_INTERPOLATION_MODE_LINEAR = 1;

    public const uint D2D1_INTERPOLATION_MODE_NEAREST_NEIGHBOR = 0;
    public const uint D2D1_INTERPOLATION_MODE_LINEAR = 1;
    public const uint D2D1_INTERPOLATION_MODE_CUBIC = 2;
    public const uint D2D1_INTERPOLATION_MODE_MULTI_SAMPLE_LINEAR = 3;
    public const uint D2D1_INTERPOLATION_MODE_ANISOTROPIC = 4;
    public const uint D2D1_INTERPOLATION_MODE_HIGH_QUALITY_CUBIC = 5;

    public const uint D2D1_ANTIALIAS_MODE_ALIASED = 1;

    public const uint D2D1_LAYER_OPTIONS_NONE = 0x00000000;
    public const uint D2D1_LAYER_OPTIONS_INITIALIZE_FOR_CLEARTYPE = 0x00000001;

    public const uint D2D1_LAYER_OPTIONS1_NONE = 0;
    public const uint D2D1_LAYER_OPTIONS1_INITIALIZE_FROM_BACKGROUND = 1;
    public const uint D2D1_LAYER_OPTIONS1_IGNORE_ALPHA = 2;

    public const uint D2D1_OPACITY_MASK_CONTENT_GRAPHICS = 0;
    public const uint D2D1_OPACITY_MASK_CONTENT_TEXT_NATURAL = 1;
    public const uint D2D1_OPACITY_MASK_CONTENT_TEXT_GDI_COMPATIBLE = 2;

    public const uint DWRITE_READING_DIRECTION_LEFT_TO_RIGHT = 0;
    public const uint DWRITE_READING_DIRECTION_RIGHT_TO_LEFT = 1;
    public const uint DWRITE_READING_DIRECTION_TOP_TO_BOTTOM = 2;
    public const uint DWRITE_READING_DIRECTION_BOTTOM_TO_TOP = 3;

    public const uint DWRITE_FLOW_DIRECTION_TOP_TO_BOTTOM = 0;
    public const uint DWRITE_FLOW_DIRECTION_BOTTOM_TO_TOP = 1;
    public const uint DWRITE_FLOW_DIRECTION_LEFT_TO_RIGHT = 2;
    public const uint DWRITE_FLOW_DIRECTION_RIGHT_TO_LEFT = 3;

    public const uint DWRITE_TEXT_ALIGNMENT_LEADING = 0;
    public const uint DWRITE_TEXT_ALIGNMENT_TRAILING = 1;
    public const uint DWRITE_TEXT_ALIGNMENT_CENTER = 2;
    public const uint DWRITE_TEXT_ALIGNMENT_JUSTIFIED = 3;

    public const uint DWRITE_PARAGRAPH_ALIGNMENT_NEAR = 0;
    public const uint DWRITE_PARAGRAPH_ALIGNMENT_FAR = 1;
    public const uint DWRITE_PARAGRAPH_ALIGNMENT_CENTER = 2;

    public const uint DWRITE_WORD_WRAPPING_WRAP = 0;
    public const uint DWRITE_WORD_WRAPPING_NO_WRAP = 1;
    public const uint DWRITE_WORD_WRAPPING_EMERGENCY_BREAK = 2;
    public const uint DWRITE_WORD_WRAPPING_WHOLE_WORD = 3;
    public const uint DWRITE_WORD_WRAPPING_CHARACTER = 4;

    public const uint DWRITE_LINE_SPACING_METHOD_DEFAULT = 0;
    public const uint DWRITE_LINE_SPACING_METHOD_UNIFORM = 1;
    public const uint DWRITE_LINE_SPACING_METHOD_PROPORTIONAL = 2;

    public const uint DWRITE_TRIMMING_GRANULARITY_NONE = 0;
    public const uint DWRITE_TRIMMING_GRANULARITY_CHARACTER = 1;
    public const uint DWRITE_TRIMMING_GRANULARITY_WORD = 2;

    public const uint DWRITE_MEASURING_MODE_NATURAL = 0;
    public const uint DWRITE_MEASURING_MODE_GDI_CLASSIC = 1;
    public const uint DWRITE_MEASURING_MODE_GDI_NATURAL = 2;

    public const uint DWRITE_FONT_STYLE_OBLIQUE = 1;
    public const uint DWRITE_FONT_STYLE_ITALIC = 2;

    public const uint DWRITE_FONT_STRETCH_UNDEFINED = 0;
    public const uint DWRITE_FONT_STRETCH_ULTRA_CONDENSED = 1;
    public const uint DWRITE_FONT_STRETCH_EXTRA_CONDENSED = 2;
    public const uint DWRITE_FONT_STRETCH_CONDENSED = 3;
    public const uint DWRITE_FONT_STRETCH_SEMI_CONDENSED = 4;
    public const uint DWRITE_FONT_STRETCH_MEDIUM = 5;
    public const uint DWRITE_FONT_STRETCH_SEMI_EXPANDED = 6;
    public const uint DWRITE_FONT_STRETCH_EXPANDED = 7;
    public const uint DWRITE_FONT_STRETCH_EXTRA_EXPANDED = 8;
    public const uint DWRITE_FONT_STRETCH_ULTRA_EXPANDED = 9;
}

internal static class DxMediaGuids
{
    public static readonly Guid IID_ID2D1Factory = new("06152247-6f50-465a-9245-118bfd3b6007");
    public static readonly Guid IID_ID2D1RenderTarget = new("2cd90694-12e2-11dc-9fed-001143a055f9");
    public static readonly Guid IID_ID2D1DeviceContext = new("e8f7fe7a-191c-466d-ad95-975678bda998");
    public static readonly Guid IID_ID2D1StrokeStyle = new("2cd9069d-12e2-11dc-9fed-001143a055f9");
    public static readonly Guid IID_ID2D1StrokeStyle1 = new("10a72a66-e91c-43f4-993f-ddf4b82b0b4a");
    public static readonly Guid IID_ID2D1Geometry = new("2cd906a1-12e2-11dc-9fed-001143a055f9");
    public static readonly Guid IID_ID2D1RectangleGeometry = new("2cd906a2-12e2-11dc-9fed-001143a055f9");
    public static readonly Guid IID_ID2D1RoundedRectangleGeometry = new("2cd906a3-12e2-11dc-9fed-001143a055f9");
    public static readonly Guid IID_ID2D1EllipseGeometry = new("2cd906a4-12e2-11dc-9fed-001143a055f9");
    public static readonly Guid IID_ID2D1PathGeometry = new("2cd906a5-12e2-11dc-9fed-001143a055f9");
    public static readonly Guid IID_ID2D1PathGeometry1 = new("62baa2d2-ab54-41b7-b872-787e0106a421");
    public static readonly Guid IID_ID2D1GeometryGroup = new("2cd906a6-12e2-11dc-9fed-001143a055f9");
    public static readonly Guid IID_ID2D1TransformedGeometry = new("2cd906bb-12e2-11dc-9fed-001143a055f9");
    public static readonly Guid IID_ID2D1SimplifiedGeometrySink = new("2cd9069e-12e2-11dc-9fed-001143a055f9");
    public static readonly Guid IID_ID2D1GeometrySink = new("2cd9069f-12e2-11dc-9fed-001143a055f9");
    public static readonly Guid IID_ID2D1Brush = new("2cd906a8-12e2-11dc-9fed-001143a055f9");
    public static readonly Guid IID_ID2D1SolidColorBrush = new("2cd906a9-12e2-11dc-9fed-001143a055f9");
    public static readonly Guid IID_ID2D1BitmapBrush = new("2cd906aa-12e2-11dc-9fed-001143a055f9");
    public static readonly Guid IID_ID2D1BitmapBrush1 = new("41343a53-e41a-49a2-91cd-21793bbb62e5");
    public static readonly Guid IID_ID2D1LinearGradientBrush = new("2cd906ab-12e2-11dc-9fed-001143a055f9");
    public static readonly Guid IID_ID2D1RadialGradientBrush = new("2cd906ac-12e2-11dc-9fed-001143a055f9");
    public static readonly Guid IID_ID2D1Bitmap = new("a2296057-ea42-4099-983b-539fb6505426");
    public static readonly Guid IID_ID2D1Bitmap1 = new("a898a84c-3873-4588-b08b-ebbf978df041");
    public static readonly Guid IID_IDWriteTextFormat = new("9c906818-31d7-4fd3-a151-7c5e225db55a");
    public static readonly Guid IID_IDWriteTextLayout = new("53737037-6d14-410b-9bfe-0b182bb70961");
    public static readonly Guid IID_IDWriteFontCollection = new("a84cee02-3eea-4eee-a827-87c1a02a0fcc");
    public static readonly Guid IID_IDWriteFontFamily = new("da20d8ef-812a-4c43-9802-62ec4abd7add");
    public static readonly Guid IID_IDWriteFont = new("acd16696-8c14-4f5d-877e-fe3fc1d32737");
    public static readonly Guid IID_IDWriteLocalizedStrings = new("08256209-099a-4b34-b86d-c22b110e7771");
}

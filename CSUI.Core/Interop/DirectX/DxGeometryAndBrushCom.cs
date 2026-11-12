namespace CSUI.Core.Interop.DirectX;

internal unsafe struct ID2D1FactoryExVtbl
{
    public static int CreateRectangleGeometry(void* self, in D2D1_RECT_F rectangle, void** geometry)
    {
        fixed (D2D1_RECT_F* p = &rectangle)
            return ((delegate* unmanaged[Stdcall]<void*, D2D1_RECT_F*, void**, int>)(*(void***)self)[5])(self, p, geometry);
    }

    public static int CreateRoundedRectangleGeometry(void* self, in D2D1_ROUNDED_RECT roundedRectangle, void** geometry)
    {
        fixed (D2D1_ROUNDED_RECT* p = &roundedRectangle)
            return ((delegate* unmanaged[Stdcall]<void*, D2D1_ROUNDED_RECT*, void**, int>)(*(void***)self)[6])(self, p, geometry);
    }

    public static int CreateEllipseGeometry(void* self, in D2D1_ELLIPSE ellipse, void** geometry)
    {
        fixed (D2D1_ELLIPSE* p = &ellipse)
            return ((delegate* unmanaged[Stdcall]<void*, D2D1_ELLIPSE*, void**, int>)(*(void***)self)[7])(self, p, geometry);
    }

    public static int CreateGeometryGroup(void* self, uint fillMode, void** geometries, uint geometriesCount, void** geometryGroup)
        => ((delegate* unmanaged[Stdcall]<void*, uint, void**, uint, void**, int>)(*(void***)self)[8])(
            self, fillMode, geometries, geometriesCount, geometryGroup);

    public static int CreateTransformedGeometry(void* self, void* sourceGeometry, in D2D1_MATRIX_3X2_F transform, void** transformedGeometry)
    {
        fixed (D2D1_MATRIX_3X2_F* p = &transform)
            return ((delegate* unmanaged[Stdcall]<void*, void*, D2D1_MATRIX_3X2_F*, void**, int>)(*(void***)self)[9])(
                self, sourceGeometry, p, transformedGeometry);
    }

    public static int CreatePathGeometry(void* self, void** pathGeometry)
        => ((delegate* unmanaged[Stdcall]<void*, void**, int>)(*(void***)self)[10])(self, pathGeometry);

    public static int CreateStrokeStyle(void* self, in D2D1_STROKE_STYLE_PROPERTIES props, float* dashes, uint dashesCount, void** strokeStyle)
    {
        fixed (D2D1_STROKE_STYLE_PROPERTIES* p = &props)
            return ((delegate* unmanaged[Stdcall]<void*, D2D1_STROKE_STYLE_PROPERTIES*, float*, uint, void**, int>)(*(void***)self)[11])(
                self, p, dashes, dashesCount, strokeStyle);
    }

    public static int CreateStrokeStyle1(void* self, in D2D1_STROKE_STYLE_PROPERTIES1 props, float* dashes, uint dashesCount, void** strokeStyle)
    {
        fixed (D2D1_STROKE_STYLE_PROPERTIES1* p = &props)
            return ((delegate* unmanaged[Stdcall]<void*, D2D1_STROKE_STYLE_PROPERTIES1*, float*, uint, void**, int>)(*(void***)self)[18])(
                self, p, dashes, dashesCount, strokeStyle);
    }

    public static int CreatePathGeometry1(void* self, void** pathGeometry)
        => ((delegate* unmanaged[Stdcall]<void*, void**, int>)(*(void***)self)[19])(self, pathGeometry);
}

internal unsafe struct ID2D1SimplifiedGeometrySinkVtbl
{
    public static void SetFillMode(void* self, uint fillMode)
        => ((delegate* unmanaged[Stdcall]<void*, uint, void>)(*(void***)self)[3])(self, fillMode);

    public static void SetSegmentFlags(void* self, uint vertexFlags)
        => ((delegate* unmanaged[Stdcall]<void*, uint, void>)(*(void***)self)[4])(self, vertexFlags);

    public static void BeginFigure(void* self, D2D1_POINT_2F startPoint, uint figureBegin)
        => ((delegate* unmanaged[Stdcall]<void*, D2D1_POINT_2F, uint, void>)(*(void***)self)[5])(self, startPoint, figureBegin);

    public static void AddLines(void* self, D2D1_POINT_2F* points, uint pointsCount)
        => ((delegate* unmanaged[Stdcall]<void*, D2D1_POINT_2F*, uint, void>)(*(void***)self)[6])(self, points, pointsCount);

    public static void AddBeziers(void* self, D2D1_BEZIER_SEGMENT* beziers, uint beziersCount)
        => ((delegate* unmanaged[Stdcall]<void*, D2D1_BEZIER_SEGMENT*, uint, void>)(*(void***)self)[7])(self, beziers, beziersCount);

    public static void EndFigure(void* self, uint figureEnd)
        => ((delegate* unmanaged[Stdcall]<void*, uint, void>)(*(void***)self)[8])(self, figureEnd);

    public static int Close(void* self)
        => ((delegate* unmanaged[Stdcall]<void*, int>)(*(void***)self)[9])(self);
}

internal unsafe struct ID2D1GeometrySinkVtbl
{
    public static void SetFillMode(void* self, uint fillMode) => ID2D1SimplifiedGeometrySinkVtbl.SetFillMode(self, fillMode);
    public static void SetSegmentFlags(void* self, uint vertexFlags) => ID2D1SimplifiedGeometrySinkVtbl.SetSegmentFlags(self, vertexFlags);
    public static void BeginFigure(void* self, D2D1_POINT_2F startPoint, uint figureBegin) => ID2D1SimplifiedGeometrySinkVtbl.BeginFigure(self, startPoint, figureBegin);
    public static void AddLines(void* self, D2D1_POINT_2F* points, uint pointsCount) => ID2D1SimplifiedGeometrySinkVtbl.AddLines(self, points, pointsCount);
    public static void AddBeziers(void* self, D2D1_BEZIER_SEGMENT* beziers, uint beziersCount) => ID2D1SimplifiedGeometrySinkVtbl.AddBeziers(self, beziers, beziersCount);
    public static void EndFigure(void* self, uint figureEnd) => ID2D1SimplifiedGeometrySinkVtbl.EndFigure(self, figureEnd);
    public static int Close(void* self) => ID2D1SimplifiedGeometrySinkVtbl.Close(self);

    public static void AddLine(void* self, D2D1_POINT_2F point)
        => ((delegate* unmanaged[Stdcall]<void*, D2D1_POINT_2F, void>)(*(void***)self)[10])(self, point);

    public static void AddBezier(void* self, in D2D1_BEZIER_SEGMENT bezier)
    {
        fixed (D2D1_BEZIER_SEGMENT* p = &bezier)
            ((delegate* unmanaged[Stdcall]<void*, D2D1_BEZIER_SEGMENT*, void>)(*(void***)self)[11])(self, p);
    }

    public static void AddQuadraticBezier(void* self, in D2D1_QUADRATIC_BEZIER_SEGMENT bezier)
    {
        fixed (D2D1_QUADRATIC_BEZIER_SEGMENT* p = &bezier)
            ((delegate* unmanaged[Stdcall]<void*, D2D1_QUADRATIC_BEZIER_SEGMENT*, void>)(*(void***)self)[12])(self, p);
    }

    public static void AddQuadraticBeziers(void* self, D2D1_QUADRATIC_BEZIER_SEGMENT* beziers, uint beziersCount)
        => ((delegate* unmanaged[Stdcall]<void*, D2D1_QUADRATIC_BEZIER_SEGMENT*, uint, void>)(*(void***)self)[13])(self, beziers, beziersCount);

    public static void AddArc(void* self, in D2D1_ARC_SEGMENT arc)
    {
        fixed (D2D1_ARC_SEGMENT* p = &arc)
            ((delegate* unmanaged[Stdcall]<void*, D2D1_ARC_SEGMENT*, void>)(*(void***)self)[14])(self, p);
    }
}

internal unsafe struct ID2D1GeometryVtbl
{
    public static int GetBounds(void* self, D2D1_MATRIX_3X2_F* worldTransform, out D2D1_RECT_F bounds)
    {
        fixed (D2D1_RECT_F* p = &bounds)
            return ((delegate* unmanaged[Stdcall]<void*, D2D1_MATRIX_3X2_F*, D2D1_RECT_F*, int>)(*(void***)self)[4])(self, worldTransform, p);
    }

    public static int GetWidenedBounds(void* self, float strokeWidth, void* strokeStyle, D2D1_MATRIX_3X2_F* worldTransform, float flatteningTolerance, out D2D1_RECT_F bounds)
    {
        fixed (D2D1_RECT_F* p = &bounds)
            return ((delegate* unmanaged[Stdcall]<void*, float, void*, D2D1_MATRIX_3X2_F*, float, D2D1_RECT_F*, int>)(*(void***)self)[5])(
                self, strokeWidth, strokeStyle, worldTransform, flatteningTolerance, p);
    }

    public static int StrokeContainsPoint(void* self, D2D1_POINT_2F point, float strokeWidth, void* strokeStyle, D2D1_MATRIX_3X2_F* worldTransform, float flatteningTolerance, out bool contains)
    {
        int b = 0;
        int hr = ((delegate* unmanaged[Stdcall]<void*, D2D1_POINT_2F, float, void*, D2D1_MATRIX_3X2_F*, float, int*, int>)(*(void***)self)[6])(
            self, point, strokeWidth, strokeStyle, worldTransform, flatteningTolerance, &b);
        contains = b != 0;
        return hr;
    }

    public static int FillContainsPoint(void* self, D2D1_POINT_2F point, D2D1_MATRIX_3X2_F* worldTransform, float flatteningTolerance, out bool contains)
    {
        int b = 0;
        int hr = ((delegate* unmanaged[Stdcall]<void*, D2D1_POINT_2F, D2D1_MATRIX_3X2_F*, float, int*, int>)(*(void***)self)[7])(
            self, point, worldTransform, flatteningTolerance, &b);
        contains = b != 0;
        return hr;
    }

    public static int CompareWithGeometry(void* self, void* inputGeometry, D2D1_MATRIX_3X2_F* inputGeometryTransform, float flatteningTolerance, out uint relation)
    {
        fixed (uint* p = &relation)
            return ((delegate* unmanaged[Stdcall]<void*, void*, D2D1_MATRIX_3X2_F*, float, uint*, int>)(*(void***)self)[8])(
                self, inputGeometry, inputGeometryTransform, flatteningTolerance, p);
    }

    public static int Simplify(void* self, uint simplificationOption, D2D1_MATRIX_3X2_F* worldTransform, float flatteningTolerance, void* geometrySink)
        => ((delegate* unmanaged[Stdcall]<void*, uint, D2D1_MATRIX_3X2_F*, float, void*, int>)(*(void***)self)[9])(
            self, simplificationOption, worldTransform, flatteningTolerance, geometrySink);

    public static int CombineWithGeometry(void* self, void* inputGeometry, uint combineMode, D2D1_MATRIX_3X2_F* inputGeometryTransform, float flatteningTolerance, void* geometrySink)
        => ((delegate* unmanaged[Stdcall]<void*, void*, uint, D2D1_MATRIX_3X2_F*, float, void*, int>)(*(void***)self)[11])(
            self, inputGeometry, combineMode, inputGeometryTransform, flatteningTolerance, geometrySink);

    public static int Outline(void* self, D2D1_MATRIX_3X2_F* worldTransform, float flatteningTolerance, void* geometrySink)
        => ((delegate* unmanaged[Stdcall]<void*, D2D1_MATRIX_3X2_F*, float, void*, int>)(*(void***)self)[12])(
            self, worldTransform, flatteningTolerance, geometrySink);

    public static int ComputeArea(void* self, D2D1_MATRIX_3X2_F* worldTransform, float flatteningTolerance, out float area)
    {
        fixed (float* p = &area)
            return ((delegate* unmanaged[Stdcall]<void*, D2D1_MATRIX_3X2_F*, float, float*, int>)(*(void***)self)[13])(
                self, worldTransform, flatteningTolerance, p);
    }

    public static int ComputeLength(void* self, D2D1_MATRIX_3X2_F* worldTransform, float flatteningTolerance, out float length)
    {
        fixed (float* p = &length)
            return ((delegate* unmanaged[Stdcall]<void*, D2D1_MATRIX_3X2_F*, float, float*, int>)(*(void***)self)[14])(
                self, worldTransform, flatteningTolerance, p);
    }

    public static int ComputePointAtLength(void* self, float length, D2D1_MATRIX_3X2_F* worldTransform, float flatteningTolerance, D2D1_POINT_2F* point, D2D1_POINT_2F* unitTangentVector)
        => ((delegate* unmanaged[Stdcall]<void*, float, D2D1_MATRIX_3X2_F*, float, D2D1_POINT_2F*, D2D1_POINT_2F*, int>)(*(void***)self)[15])(
            self, length, worldTransform, flatteningTolerance, point, unitTangentVector);

    public static int Widen(void* self, float strokeWidth, void* strokeStyle, D2D1_MATRIX_3X2_F* worldTransform, float flatteningTolerance, void* geometrySink)
        => ((delegate* unmanaged[Stdcall]<void*, float, void*, D2D1_MATRIX_3X2_F*, float, void*, int>)(*(void***)self)[16])(
            self, strokeWidth, strokeStyle, worldTransform, flatteningTolerance, geometrySink);
}

internal unsafe struct ID2D1PathGeometryVtbl
{
    public static int Open(void* self, void** geometrySink)
        => ((delegate* unmanaged[Stdcall]<void*, void**, int>)(*(void***)self)[17])(self, geometrySink);

    public static int Stream(void* self, void* geometrySink)
        => ((delegate* unmanaged[Stdcall]<void*, void*, int>)(*(void***)self)[18])(self, geometrySink);

    public static int GetSegmentCount(void* self, out uint count)
    {
        fixed (uint* p = &count)
            return ((delegate* unmanaged[Stdcall]<void*, uint*, int>)(*(void***)self)[19])(self, p);
    }

    public static int GetFigureCount(void* self, out uint count)
    {
        fixed (uint* p = &count)
            return ((delegate* unmanaged[Stdcall]<void*, uint*, int>)(*(void***)self)[20])(self, p);
    }
}

internal unsafe struct ID2D1GeometryGroupVtbl
{
    public static uint GetFillMode(void* self)
        => ((delegate* unmanaged[Stdcall]<void*, uint>)(*(void***)self)[17])(self);

    public static uint GetSourceGeometryCount(void* self)
        => ((delegate* unmanaged[Stdcall]<void*, uint>)(*(void***)self)[18])(self);

    public static void GetSourceGeometries(void* self, void** geometries, uint geometriesCount)
        => ((delegate* unmanaged[Stdcall]<void*, void**, uint, void>)(*(void***)self)[19])(self, geometries, geometriesCount);
}

internal unsafe struct ID2D1TransformedGeometryVtbl
{
    public static void GetSourceGeometry(void* self, void** sourceGeometry)
        => ((delegate* unmanaged[Stdcall]<void*, void**, void>)(*(void***)self)[17])(self, sourceGeometry);

    public static void GetTransform(void* self, out D2D1_MATRIX_3X2_F transform)
    {
        fixed (D2D1_MATRIX_3X2_F* p = &transform)
            ((delegate* unmanaged[Stdcall]<void*, D2D1_MATRIX_3X2_F*, void>)(*(void***)self)[18])(self, p);
    }
}

internal unsafe struct ID2D1BrushVtbl
{
    public static void SetOpacity(void* self, float opacity)
        => ((delegate* unmanaged[Stdcall]<void*, float, void>)(*(void***)self)[4])(self, opacity);

    public static void SetTransform(void* self, in D2D1_MATRIX_3X2_F transform)
    {
        fixed (D2D1_MATRIX_3X2_F* p = &transform)
            ((delegate* unmanaged[Stdcall]<void*, D2D1_MATRIX_3X2_F*, void>)(*(void***)self)[5])(self, p);
    }

    public static float GetOpacity(void* self)
        => ((delegate* unmanaged[Stdcall]<void*, float>)(*(void***)self)[6])(self);

    public static void GetTransform(void* self, out D2D1_MATRIX_3X2_F transform)
    {
        fixed (D2D1_MATRIX_3X2_F* p = &transform)
            ((delegate* unmanaged[Stdcall]<void*, D2D1_MATRIX_3X2_F*, void>)(*(void***)self)[7])(self, p);
    }
}

internal unsafe struct ID2D1SolidColorBrushVtbl
{
    public static void SetColor(void* self, in D2D1_COLOR_F color)
    {
        fixed (D2D1_COLOR_F* p = &color)
            ((delegate* unmanaged[Stdcall]<void*, D2D1_COLOR_F*, void>)(*(void***)self)[8])(self, p);
    }
}

internal unsafe struct ID2D1LinearGradientBrushVtbl
{
    public static void SetStartPoint(void* self, D2D1_POINT_2F startPoint)
        => ((delegate* unmanaged[Stdcall]<void*, D2D1_POINT_2F, void>)(*(void***)self)[8])(self, startPoint);

    public static void SetEndPoint(void* self, D2D1_POINT_2F endPoint)
        => ((delegate* unmanaged[Stdcall]<void*, D2D1_POINT_2F, void>)(*(void***)self)[9])(self, endPoint);
}

internal unsafe struct ID2D1RadialGradientBrushVtbl
{
    public static void SetCenter(void* self, D2D1_POINT_2F center)
        => ((delegate* unmanaged[Stdcall]<void*, D2D1_POINT_2F, void>)(*(void***)self)[8])(self, center);

    public static void SetGradientOriginOffset(void* self, D2D1_POINT_2F gradientOriginOffset)
        => ((delegate* unmanaged[Stdcall]<void*, D2D1_POINT_2F, void>)(*(void***)self)[9])(self, gradientOriginOffset);

    public static void SetRadiusX(void* self, float radiusX)
        => ((delegate* unmanaged[Stdcall]<void*, float, void>)(*(void***)self)[10])(self, radiusX);

    public static void SetRadiusY(void* self, float radiusY)
        => ((delegate* unmanaged[Stdcall]<void*, float, void>)(*(void***)self)[11])(self, radiusY);

    public static float GetRadiusX(void* self)
        => ((delegate* unmanaged[Stdcall]<void*, float>)(*(void***)self)[14])(self);

    public static float GetRadiusY(void* self)
        => ((delegate* unmanaged[Stdcall]<void*, float>)(*(void***)self)[15])(self);
}

internal unsafe struct ID2D1BitmapBrush1Vtbl
{
    public static void SetExtendModeX(void* self, uint extendModeX)
        => ((delegate* unmanaged[Stdcall]<void*, uint, void>)(*(void***)self)[8])(self, extendModeX);

    public static void SetExtendModeY(void* self, uint extendModeY)
        => ((delegate* unmanaged[Stdcall]<void*, uint, void>)(*(void***)self)[9])(self, extendModeY);

    public static void SetInterpolationMode(void* self, uint interpolationMode)
        => ((delegate* unmanaged[Stdcall]<void*, uint, void>)(*(void***)self)[10])(self, interpolationMode);

    public static void SetBitmap(void* self, void* bitmap)
        => ((delegate* unmanaged[Stdcall]<void*, void*, void>)(*(void***)self)[11])(self, bitmap);

    public static void SetInterpolationMode1(void* self, uint interpolationMode)
        => ((delegate* unmanaged[Stdcall]<void*, uint, void>)(*(void***)self)[16])(self, interpolationMode);
}

internal unsafe struct ID2D1BitmapVtbl
{
    public static void GetDpi(void* self, out float dpiX, out float dpiY)
    {
        fixed (float* px = &dpiX)
        fixed (float* py = &dpiY)
            ((delegate* unmanaged[Stdcall]<void*, float*, float*, void>)(*(void***)self)[7])(self, px, py);
    }

    public static int CopyFromBitmap(void* self, D2D1_POINT_2U* destPoint, void* bitmap, D2D1_RECT_U* srcRect)
        => ((delegate* unmanaged[Stdcall]<void*, D2D1_POINT_2U*, void*, D2D1_RECT_U*, int>)(*(void***)self)[8])(
            self, destPoint, bitmap, srcRect);

    public static int CopyFromMemory(void* self, D2D1_RECT_U* dstRect, void* srcData, uint pitch)
        => ((delegate* unmanaged[Stdcall]<void*, D2D1_RECT_U*, void*, uint, int>)(*(void***)self)[10])(
            self, dstRect, srcData, pitch);
}

internal unsafe struct ID2D1Bitmap1Vtbl
{
    public static int Map(void* self, uint options, out D2D1_MAPPED_RECT mappedRect)
    {
        fixed (D2D1_MAPPED_RECT* p = &mappedRect)
            return ((delegate* unmanaged[Stdcall]<void*, uint, D2D1_MAPPED_RECT*, int>)(*(void***)self)[14])(self, options, p);
    }

    public static int Unmap(void* self)
        => ((delegate* unmanaged[Stdcall]<void*, int>)(*(void***)self)[15])(self);
}

internal unsafe struct ID2D1DeviceContextExVtbl
{
    public static int CreateBitmap(void* self, D2D1_SIZE_U size, void* sourceData, uint pitch, in D2D1_BITMAP_PROPERTIES1 props, void** bitmap)
    {
        fixed (D2D1_BITMAP_PROPERTIES1* p = &props)
            return ((delegate* unmanaged[Stdcall]<void*, D2D1_SIZE_U, void*, uint, D2D1_BITMAP_PROPERTIES1*, void**, int>)(*(void***)self)[57])(
                self, size, sourceData, pitch, p, bitmap);
    }

    public static int CreateBitmapFromWicBitmap(void* self, void* wicBitmapSource, D2D1_BITMAP_PROPERTIES1* props, void** bitmap)
        => ((delegate* unmanaged[Stdcall]<void*, void*, D2D1_BITMAP_PROPERTIES1*, void**, int>)(*(void***)self)[58])(
            self, wicBitmapSource, props, bitmap);

    public static int CreateRadialGradientBrush(
        void* self,
        in D2D1_RADIAL_GRADIENT_BRUSH_PROPERTIES radialProps,
        D2D1_BRUSH_PROPERTIES* brushProps,
        void* stopCollection,
        void** radialBrush)
    {
        fixed (D2D1_RADIAL_GRADIENT_BRUSH_PROPERTIES* p = &radialProps)
            return ((delegate* unmanaged[Stdcall]<void*, D2D1_RADIAL_GRADIENT_BRUSH_PROPERTIES*, D2D1_BRUSH_PROPERTIES*, void*, void**, int>)(*(void***)self)[11])(
                self, p, brushProps, stopCollection, radialBrush);
    }

    public static int CreateLayer(void* self, D2D1_SIZE_F* size, void** layer)
        => ((delegate* unmanaged[Stdcall]<void*, D2D1_SIZE_F*, void**, int>)(*(void***)self)[13])(self, size, layer);

    public static int CreateBitmapBrush(
        void* self,
        void* bitmap,
        D2D1_BITMAP_BRUSH_PROPERTIES1* bitmapBrushProps,
        D2D1_BRUSH_PROPERTIES* brushProps,
        void** bitmapBrush)
        => ((delegate* unmanaged[Stdcall]<void*, void*, D2D1_BITMAP_BRUSH_PROPERTIES1*, D2D1_BRUSH_PROPERTIES*, void**, int>)(*(void***)self)[66])(
            self, bitmap, bitmapBrushProps, brushProps, bitmapBrush);

    public static void DrawBitmap(
        void* self,
        void* bitmap,
        D2D1_RECT_F* destinationRectangle,
        float opacity,
        uint interpolationMode,
        D2D1_RECT_F* sourceRectangle,
        void* perspectiveTransform = null)
        => ((delegate* unmanaged[Stdcall]<void*, void*, D2D1_RECT_F*, float, uint, D2D1_RECT_F*, void*, void>)(*(void***)self)[85])(
            self, bitmap, destinationRectangle, opacity, interpolationMode, sourceRectangle, perspectiveTransform);

    public static void DrawText(
        void* self,
        char* str,
        uint stringLength,
        void* textFormat,
        in D2D1_RECT_F layoutRect,
        void* defaultFillBrush,
        uint options,
        uint measuringMode)
    {
        fixed (D2D1_RECT_F* p = &layoutRect)
            ((delegate* unmanaged[Stdcall]<void*, char*, uint, void*, D2D1_RECT_F*, void*, uint, uint, void>)(*(void***)self)[27])(
                self, str, stringLength, textFormat, p, defaultFillBrush, options, measuringMode);
    }

    public static void FillOpacityMask(
        void* self,
        void* opacityMask,
        void* brush,
        D2D1_RECT_F* destinationRectangle,
        D2D1_RECT_F* sourceRectangle)
        => ((delegate* unmanaged[Stdcall]<void*, void*, void*, D2D1_RECT_F*, D2D1_RECT_F*, void>)(*(void***)self)[91])(
            self, opacityMask, brush, destinationRectangle, sourceRectangle);

    public static void GetTransform(void* self, out D2D1_MATRIX_3X2_F transform)
    {
        fixed (D2D1_MATRIX_3X2_F* p = &transform)
            ((delegate* unmanaged[Stdcall]<void*, D2D1_MATRIX_3X2_F*, void>)(*(void***)self)[31])(self, p);
    }

    public static void SetAntialiasMode(void* self, uint antialiasMode)
        => ((delegate* unmanaged[Stdcall]<void*, uint, void>)(*(void***)self)[32])(self, antialiasMode);

    public static uint GetAntialiasMode(void* self)
        => ((delegate* unmanaged[Stdcall]<void*, uint>)(*(void***)self)[33])(self);

    public static uint GetTextAntialiasMode(void* self)
        => ((delegate* unmanaged[Stdcall]<void*, uint>)(*(void***)self)[35])(self);

    public static void GetDpi(void* self, out float dpiX, out float dpiY)
    {
        fixed (float* px = &dpiX)
        fixed (float* py = &dpiY)
            ((delegate* unmanaged[Stdcall]<void*, float*, float*, void>)(*(void***)self)[52])(self, px, py);
    }
}

internal unsafe struct IDWriteFactoryExVtbl
{
    public static int GetSystemFontCollection(void* self, void** fontCollection, bool checkForUpdates = false)
        => ((delegate* unmanaged[Stdcall]<void*, void**, int, int>)(*(void***)self)[3])(
            self, fontCollection, checkForUpdates ? 1 : 0);

    public static int CreateEllipsisTrimmingSign(void* self, void* textFormat, void** trimmingSign)
        => ((delegate* unmanaged[Stdcall]<void*, void*, void**, int>)(*(void***)self)[20])(
            self, textFormat, trimmingSign);
}

internal unsafe struct IDWriteTextFormatExVtbl
{
    public static int SetReadingDirection(void* self, uint readingDirection)
        => ((delegate* unmanaged[Stdcall]<void*, uint, int>)(*(void***)self)[6])(self, readingDirection);

    public static int SetFlowDirection(void* self, uint flowDirection)
        => ((delegate* unmanaged[Stdcall]<void*, uint, int>)(*(void***)self)[7])(self, flowDirection);

    public static int SetIncrementalTabStop(void* self, float incrementalTabStop)
        => ((delegate* unmanaged[Stdcall]<void*, float, int>)(*(void***)self)[8])(self, incrementalTabStop);

    public static int SetTrimming(void* self, in DWRITE_TRIMMING trimmingOptions, void* trimmingSign)
    {
        fixed (DWRITE_TRIMMING* p = &trimmingOptions)
            return ((delegate* unmanaged[Stdcall]<void*, DWRITE_TRIMMING*, void*, int>)(*(void***)self)[9])(self, p, trimmingSign);
    }

    public static int SetLineSpacing(void* self, uint lineSpacingMethod, float lineSpacing, float baseline)
        => ((delegate* unmanaged[Stdcall]<void*, uint, float, float, int>)(*(void***)self)[10])(
            self, lineSpacingMethod, lineSpacing, baseline);

    public static float GetFontSize(void* self)
        => ((delegate* unmanaged[Stdcall]<void*, float>)(*(void***)self)[25])(self);
}

internal unsafe struct IDWriteTextLayoutExVtbl
{
    public static int SetFontCollection(void* self, void* fontCollection, DWRITE_TEXT_RANGE textRange)
        => ((delegate* unmanaged[Stdcall]<void*, void*, DWRITE_TEXT_RANGE, int>)(*(void***)self)[30])(self, fontCollection, textRange);

    public static int SetFontFamilyName(void* self, string fontFamilyName, DWRITE_TEXT_RANGE textRange)
    {
        fixed (char* p = fontFamilyName)
            return ((delegate* unmanaged[Stdcall]<void*, char*, DWRITE_TEXT_RANGE, int>)(*(void***)self)[31])(self, p, textRange);
    }

    public static int SetFontWeight(void* self, uint fontWeight, DWRITE_TEXT_RANGE textRange)
        => ((delegate* unmanaged[Stdcall]<void*, uint, DWRITE_TEXT_RANGE, int>)(*(void***)self)[32])(self, fontWeight, textRange);

    public static int SetFontStyle(void* self, uint fontStyle, DWRITE_TEXT_RANGE textRange)
        => ((delegate* unmanaged[Stdcall]<void*, uint, DWRITE_TEXT_RANGE, int>)(*(void***)self)[33])(self, fontStyle, textRange);

    public static int SetFontStretch(void* self, uint fontStretch, DWRITE_TEXT_RANGE textRange)
        => ((delegate* unmanaged[Stdcall]<void*, uint, DWRITE_TEXT_RANGE, int>)(*(void***)self)[34])(self, fontStretch, textRange);

    public static int SetFontSize(void* self, float fontSize, DWRITE_TEXT_RANGE textRange)
        => ((delegate* unmanaged[Stdcall]<void*, float, DWRITE_TEXT_RANGE, int>)(*(void***)self)[35])(self, fontSize, textRange);

    public static int SetUnderline(void* self, bool hasUnderline, DWRITE_TEXT_RANGE textRange)
        => ((delegate* unmanaged[Stdcall]<void*, int, DWRITE_TEXT_RANGE, int>)(*(void***)self)[36])(self, hasUnderline ? 1 : 0, textRange);

    public static int SetStrikethrough(void* self, bool hasStrikethrough, DWRITE_TEXT_RANGE textRange)
        => ((delegate* unmanaged[Stdcall]<void*, int, DWRITE_TEXT_RANGE, int>)(*(void***)self)[37])(self, hasStrikethrough ? 1 : 0, textRange);

    public static int SetDrawingEffect(void* self, void* drawingEffect, DWRITE_TEXT_RANGE textRange)
        => ((delegate* unmanaged[Stdcall]<void*, void*, DWRITE_TEXT_RANGE, int>)(*(void***)self)[38])(self, drawingEffect, textRange);

    public static int SetInlineObject(void* self, void* inlineObject, DWRITE_TEXT_RANGE textRange)
        => ((delegate* unmanaged[Stdcall]<void*, void*, DWRITE_TEXT_RANGE, int>)(*(void***)self)[39])(self, inlineObject, textRange);

    public static int SetTypography(void* self, void* typography, DWRITE_TEXT_RANGE textRange)
        => ((delegate* unmanaged[Stdcall]<void*, void*, DWRITE_TEXT_RANGE, int>)(*(void***)self)[40])(self, typography, textRange);

    public static int SetLocaleName(void* self, string localeName, DWRITE_TEXT_RANGE textRange)
    {
        fixed (char* p = localeName)
            return ((delegate* unmanaged[Stdcall]<void*, char*, DWRITE_TEXT_RANGE, int>)(*(void***)self)[41])(self, p, textRange);
    }

    public static float GetMaxWidth(void* self)
        => ((delegate* unmanaged[Stdcall]<void*, float>)(*(void***)self)[42])(self);

    public static float GetMaxHeight(void* self)
        => ((delegate* unmanaged[Stdcall]<void*, float>)(*(void***)self)[43])(self);

    public static int GetLineMetrics(void* self, DWRITE_LINE_METRICS* lineMetrics, uint maxLineCount, out uint actualLineCount)
    {
        fixed (uint* p = &actualLineCount)
            return ((delegate* unmanaged[Stdcall]<void*, DWRITE_LINE_METRICS*, uint, uint*, int>)(*(void***)self)[59])(
                self, lineMetrics, maxLineCount, p);
    }

    public static int GetOverhangMetrics(void* self, out DWRITE_OVERHANG_METRICS overhangs)
    {
        fixed (DWRITE_OVERHANG_METRICS* p = &overhangs)
            return ((delegate* unmanaged[Stdcall]<void*, DWRITE_OVERHANG_METRICS*, int>)(*(void***)self)[61])(self, p);
    }

    public static int GetClusterMetrics(void* self, DWRITE_CLUSTER_METRICS* clusterMetrics, uint maxClusterCount, out uint actualClusterCount)
    {
        fixed (uint* p = &actualClusterCount)
            return ((delegate* unmanaged[Stdcall]<void*, DWRITE_CLUSTER_METRICS*, uint, uint*, int>)(*(void***)self)[62])(
                self, clusterMetrics, maxClusterCount, p);
    }

    public static int DetermineMinWidth(void* self, out float minWidth)
    {
        fixed (float* p = &minWidth)
            return ((delegate* unmanaged[Stdcall]<void*, float*, int>)(*(void***)self)[63])(self, p);
    }

    public static int HitTestPoint(void* self, float pointX, float pointY, out bool isTrailingHit, out bool isInside, out DWRITE_HIT_TEST_METRICS metrics)
    {
        int trailing = 0, inside = 0;
        int hr;
        fixed (DWRITE_HIT_TEST_METRICS* p = &metrics)
            hr = ((delegate* unmanaged[Stdcall]<void*, float, float, int*, int*, DWRITE_HIT_TEST_METRICS*, int>)(*(void***)self)[64])(
                self, pointX, pointY, &trailing, &inside, p);
        isTrailingHit = trailing != 0;
        isInside = inside != 0;
        return hr;
    }

    public static int HitTestTextPosition(void* self, uint textPosition, bool isTrailingHit, out float pointX, out float pointY, out DWRITE_HIT_TEST_METRICS metrics)
    {
        fixed (float* px = &pointX)
        fixed (float* py = &pointY)
        fixed (DWRITE_HIT_TEST_METRICS* p = &metrics)
            return ((delegate* unmanaged[Stdcall]<void*, uint, int, float*, float*, DWRITE_HIT_TEST_METRICS*, int>)(*(void***)self)[65])(
                self, textPosition, isTrailingHit ? 1 : 0, px, py, p);
    }

    public static int HitTestTextRange(
        void* self,
        uint textPosition,
        uint textLength,
        float originX,
        float originY,
        DWRITE_HIT_TEST_METRICS* hitTestMetrics,
        uint maxHitTestMetricsCount,
        out uint actualHitTestMetricsCount)
    {
        fixed (uint* p = &actualHitTestMetricsCount)
            return ((delegate* unmanaged[Stdcall]<void*, uint, uint, float, float, DWRITE_HIT_TEST_METRICS*, uint, uint*, int>)(*(void***)self)[66])(
                self, textPosition, textLength, originX, originY, hitTestMetrics, maxHitTestMetricsCount, p);
    }
}

internal unsafe struct IDWriteFontCollectionVtbl
{
    public static uint GetFontFamilyCount(void* self)
        => ((delegate* unmanaged[Stdcall]<void*, uint>)(*(void***)self)[3])(self);

    public static int GetFontFamily(void* self, uint index, void** fontFamily)
        => ((delegate* unmanaged[Stdcall]<void*, uint, void**, int>)(*(void***)self)[4])(self, index, fontFamily);

    public static int FindFamilyName(void* self, string familyName, out uint index, out bool exists)
    {
        int b = 0;
        int hr;
        fixed (char* pName = familyName)
        fixed (uint* pIdx = &index)
            hr = ((delegate* unmanaged[Stdcall]<void*, char*, uint*, int*, int>)(*(void***)self)[5])(self, pName, pIdx, &b);
        exists = b != 0;
        return hr;
    }
}

internal unsafe struct IDWriteFontFamilyVtbl
{
    public static uint GetFontCount(void* self)
        => ((delegate* unmanaged[Stdcall]<void*, uint>)(*(void***)self)[4])(self);

    public static int GetFont(void* self, uint index, void** font)
        => ((delegate* unmanaged[Stdcall]<void*, uint, void**, int>)(*(void***)self)[5])(self, index, font);

    public static int GetFamilyNames(void* self, void** names)
        => ((delegate* unmanaged[Stdcall]<void*, void**, int>)(*(void***)self)[6])(self, names);

    public static int GetFirstMatchingFont(void* self, uint weight, uint stretch, uint style, void** matchingFont)
        => ((delegate* unmanaged[Stdcall]<void*, uint, uint, uint, void**, int>)(*(void***)self)[7])(
            self, weight, stretch, style, matchingFont);
}

internal unsafe struct IDWriteFontVtbl
{
    public static uint GetWeight(void* self)
        => ((delegate* unmanaged[Stdcall]<void*, uint>)(*(void***)self)[4])(self);

    public static uint GetStretch(void* self)
        => ((delegate* unmanaged[Stdcall]<void*, uint>)(*(void***)self)[5])(self);

    public static uint GetStyle(void* self)
        => ((delegate* unmanaged[Stdcall]<void*, uint>)(*(void***)self)[6])(self);

    public static bool IsSymbolFont(void* self)
        => ((delegate* unmanaged[Stdcall]<void*, int>)(*(void***)self)[7])(self) != 0;

    public static void GetMetrics(void* self, out DWRITE_FONT_METRICS metrics)
    {
        fixed (DWRITE_FONT_METRICS* p = &metrics)
            ((delegate* unmanaged[Stdcall]<void*, DWRITE_FONT_METRICS*, void>)(*(void***)self)[11])(self, p);
    }

    public static int HasCharacter(void* self, uint unicodeValue, out bool exists)
    {
        int b = 0;
        int hr = ((delegate* unmanaged[Stdcall]<void*, uint, int*, int>)(*(void***)self)[12])(self, unicodeValue, &b);
        exists = b != 0;
        return hr;
    }
}

internal unsafe struct IDWriteLocalizedStringsVtbl
{
    public static uint GetCount(void* self)
        => ((delegate* unmanaged[Stdcall]<void*, uint>)(*(void***)self)[3])(self);

    public static int FindLocaleName(void* self, string localeName, out uint index, out bool exists)
    {
        int b = 0;
        int hr;
        fixed (char* pLoc = localeName)
        fixed (uint* pIdx = &index)
            hr = ((delegate* unmanaged[Stdcall]<void*, char*, uint*, int*, int>)(*(void***)self)[4])(self, pLoc, pIdx, &b);
        exists = b != 0;
        return hr;
    }

    public static int GetStringLength(void* self, uint index, out uint length)
    {
        fixed (uint* p = &length)
            return ((delegate* unmanaged[Stdcall]<void*, uint, uint*, int>)(*(void***)self)[7])(self, index, p);
    }

    public static int GetString(void* self, uint index, char* stringBuffer, uint size)
        => ((delegate* unmanaged[Stdcall]<void*, uint, char*, uint, int>)(*(void***)self)[8])(self, index, stringBuffer, size);
}

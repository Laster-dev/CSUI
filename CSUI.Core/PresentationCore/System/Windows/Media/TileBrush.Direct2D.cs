// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using CSUI.Core.Interop.DirectX;

namespace System.Windows.Media
{
    internal static unsafe class TileBrushDirect2DHelper
    {
        internal static void* CreateTileBitmapBrush(
            TileBrush brush,
            void* d2dContext,
            in Rect bounds,
            in Rect contentBounds,
            Action<nint> renderContent)
        {
            if (brush == null)
            {
                return null;
            }

            brush.GetTileBrushMapping(bounds, out Matrix mapping);
            return CreateTileBitmapBrushCore(
                brush,
                brush.Viewbox,
                brush.ViewboxUnits,
                mapping,
                brush.TileMode,
                d2dContext,
                bounds,
                contentBounds,
                renderContent);
        }

        internal static void* CreateTileBitmapBrushCore(
            Brush brush,
            Rect viewbox,
            BrushMappingMode viewboxUnits,
            in Matrix mapping,
            TileMode tileMode,
            void* d2dContext,
            in Rect bounds,
            in Rect contentBounds,
            Action<nint> renderContent)
        {
            if (brush == null || d2dContext == null ||
                bounds.IsEmpty || bounds.Width <= 0.0 || bounds.Height <= 0.0 ||
                contentBounds.IsEmpty || contentBounds.Width <= 0.0 || contentBounds.Height <= 0.0)
            {
                return null;
            }

            if (viewboxUnits == BrushMappingMode.RelativeToBoundingBox)
            {
                viewbox = new Rect(
                    contentBounds.X + viewbox.X * contentBounds.Width,
                    contentBounds.Y + viewbox.Y * contentBounds.Height,
                    viewbox.Width * contentBounds.Width,
                    viewbox.Height * contentBounds.Height);
            }

            if (viewbox.IsEmpty || viewbox.Width <= 0.0 || viewbox.Height <= 0.0)
            {
                return null;
            }

            ID2D1DeviceContextExVtbl.GetTransform(d2dContext, out D2D1_MATRIX_3X2_F ctxXform);
            double pxX1 = mapping.M11 * ctxXform.M11 + mapping.M12 * ctxXform.M21;
            double pxY1 = mapping.M11 * ctxXform.M12 + mapping.M12 * ctxXform.M22;
            double pxX2 = mapping.M21 * ctxXform.M11 + mapping.M22 * ctxXform.M21;
            double pxY2 = mapping.M21 * ctxXform.M12 + mapping.M22 * ctxXform.M22;
            double scaleX = Math.Sqrt(pxX1 * pxX1 + pxY1 * pxY1);
            double scaleY = Math.Sqrt(pxX2 * pxX2 + pxY2 * pxY2);
            ID2D1DeviceContextExVtbl.GetDpi(d2dContext, out float dpiX, out float dpiY);
            double dpiScaleX = dpiX > 0.0f ? dpiX / 96.0 : 1.0;
            double dpiScaleY = dpiY > 0.0f ? dpiY / 96.0 : 1.0;

            uint innerWidth = (uint)Math.Clamp(Math.Ceiling(viewbox.Width * scaleX * dpiScaleX), 1.0, 2048.0);
            uint innerHeight = (uint)Math.Clamp(Math.Ceiling(viewbox.Height * scaleY * dpiScaleY), 1.0, 2048.0);

            uint borderPx = tileMode == TileMode.None ? 1u : 0u;
            uint bmpWidth = innerWidth + borderPx * 2u;
            uint bmpHeight = innerHeight + borderPx * 2u;

            void* d2dDevice = MediaSystem.EnsureSharedDevice().D2DDevice;
            if (d2dDevice == null)
            {
                return null;
            }

            void* scratchCtx = null;
            void* offscreenBmp = null;
            try
            {
                if (ID2D1DeviceVtbl.CreateDeviceContext(d2dDevice, 0, &scratchCtx) < 0 || scratchCtx == null)
                {
                    return null;
                }

                D2D1_BITMAP_PROPERTIES1 bmpProps = new D2D1_BITMAP_PROPERTIES1
                {
                    PixelFormat = new D2D1_PIXEL_FORMAT
                    {
                        Format = 87, // DXGI_FORMAT_B8G8R8A8_UNORM
                        AlphaMode = DxConst.D2D1_ALPHA_MODE_PREMULTIPLIED
                    },
                    DpiX = 96.0f,
                    DpiY = 96.0f,
                    BitmapOptions = DxConst.D2D1_BITMAP_OPTIONS_TARGET,
                    ColorContext = null
                };

                if (ID2D1DeviceContextExVtbl.CreateBitmap(scratchCtx, new D2D1_SIZE_U(bmpWidth, bmpHeight), null, 0, bmpProps, &offscreenBmp) < 0 || offscreenBmp == null)
                {
                    return null;
                }

                ID2D1DeviceContextVtbl.SetTarget(scratchCtx, offscreenBmp);
                ID2D1DeviceContextVtbl.BeginDraw(scratchCtx);
                try
                {
                    ID2D1DeviceContextVtbl.Clear(scratchCtx, new D2D1_COLOR_F(0, 0, 0, 0));

                    if (borderPx > 0)
                    {
                        ID2D1DeviceContextVtbl.PushAxisAlignedClip(
                            scratchCtx,
                            new D2D1_RECT_F
                            {
                                Left = borderPx,
                                Top = borderPx,
                                Right = borderPx + innerWidth,
                                Bottom = borderPx + innerHeight
                            },
                            DxConst.D2D1_ANTIALIAS_MODE_PER_PRIMITIVE);
                    }

                    double sx = innerWidth / viewbox.Width;
                    double sy = innerHeight / viewbox.Height;
                    Matrix drawTransform = Matrix.Identity;
                    drawTransform.Translate(-viewbox.X, -viewbox.Y);
                    drawTransform.Scale(sx, sy);
                    drawTransform.Translate(borderPx, borderPx);
                    ID2D1DeviceContextVtbl.SetTransform(scratchCtx, MILUtilities.ConvertToD2DMatrix(drawTransform));

                    renderContent((nint)scratchCtx);

                    if (borderPx > 0)
                    {
                        ID2D1DeviceContextVtbl.PopAxisAlignedClip(scratchCtx);
                    }
                }
                finally
                {
                    ID2D1DeviceContextVtbl.EndDraw(scratchCtx);
                    ID2D1DeviceContextVtbl.SetTarget(scratchCtx, null);
                }

                // Map offscreen bitmap DIPs [0..bmpWidth] x [0..bmpHeight] into target shape bounds.
                Matrix bmpToShape = Matrix.Identity;
                bmpToShape.Translate(-borderPx, -borderPx);
                bmpToShape.Scale(viewbox.Width / innerWidth, viewbox.Height / innerHeight);
                bmpToShape.Translate(viewbox.X, viewbox.Y);
                bmpToShape = Matrix.Multiply(bmpToShape, mapping);

                (uint extX, uint extY) = tileMode switch
                {
                    TileMode.Tile => (DxMediaConst.D2D1_EXTEND_MODE_WRAP, DxMediaConst.D2D1_EXTEND_MODE_WRAP),
                    TileMode.FlipX => (DxMediaConst.D2D1_EXTEND_MODE_MIRROR, DxMediaConst.D2D1_EXTEND_MODE_WRAP),
                    TileMode.FlipY => (DxMediaConst.D2D1_EXTEND_MODE_WRAP, DxMediaConst.D2D1_EXTEND_MODE_MIRROR),
                    TileMode.FlipXY => (DxMediaConst.D2D1_EXTEND_MODE_MIRROR, DxMediaConst.D2D1_EXTEND_MODE_MIRROR),
                    _ => (DxConst.D2D1_EXTEND_MODE_CLAMP, DxConst.D2D1_EXTEND_MODE_CLAMP),
                };

                D2D1_BITMAP_BRUSH_PROPERTIES1 bmpBrushProps = new D2D1_BITMAP_BRUSH_PROPERTIES1
                {
                    extendModeX = extX,
                    extendModeY = extY,
                    interpolationMode = DxMediaConst.D2D1_INTERPOLATION_MODE_LINEAR
                };

                D2D1_BRUSH_PROPERTIES brushProps = new D2D1_BRUSH_PROPERTIES
                {
                    Opacity = (float)Math.Clamp(brush.Opacity, 0.0, 1.0),
                    Transform = MILUtilities.ConvertToD2DMatrix(bmpToShape)
                };

                void* bitmapBrush = null;
                int hr = ID2D1DeviceContextExVtbl.CreateBitmapBrush(d2dContext, offscreenBmp, &bmpBrushProps, &brushProps, &bitmapBrush);
                return hr >= 0 ? bitmapBrush : null;
            }
            finally
            {
                if (offscreenBmp != null)
                {
                    ComHelpers.Release(offscreenBmp);
                }

                if (scratchCtx != null)
                {
                    ComHelpers.Release(scratchCtx);
                }
            }
        }
    }

    public sealed partial class DrawingBrush
    {
        internal override unsafe void* CreateD2DBrush(void* d2dContext, in Rect bounds)
        {
            Drawing drawing = Drawing;
            if (drawing == null)
            {
                return null;
            }

            GetContentBounds(out Rect contentBounds);
            return TileBrushDirect2DHelper.CreateTileBitmapBrush(
                this,
                d2dContext,
                bounds,
                contentBounds,
                ctx => Direct2DRenderWalker.Render(drawing, (void*)ctx));
        }
    }

    public sealed partial class VisualBrush
    {
        internal override unsafe void* CreateD2DBrush(void* d2dContext, in Rect bounds)
        {
            Visual visual = Visual;
            if (visual == null || !Enter())
            {
                return null;
            }

            try
            {
                GetContentBounds(out Rect contentBounds);
                return TileBrushDirect2DHelper.CreateTileBitmapBrush(
                    this,
                    d2dContext,
                    bounds,
                    contentBounds,
                    ctx =>
                    {
                        var rc = new RenderContext();
                        rc.Initialize((void*)ctx);
                        visual.RenderRecursive(rc);
                    });
            }
            finally
            {
                Exit();
            }
        }
    }

    public sealed partial class BitmapCacheBrush
    {
        internal override unsafe void* CreateD2DBrush(void* d2dContext, in Rect bounds)
        {
            Visual visual = InternalTarget;
            if (visual == null || !Enter())
            {
                return null;
            }

            try
            {
                Rect contentBounds = visual.CalculateSubgraphBoundsOuterSpace();
                if (contentBounds.IsEmpty || contentBounds.Width <= 0.0 || contentBounds.Height <= 0.0)
                {
                    return null;
                }

                Matrix mapping = Matrix.Identity;
                mapping.Translate(-contentBounds.X, -contentBounds.Y);
                mapping.Scale(bounds.Width / contentBounds.Width, bounds.Height / contentBounds.Height);
                mapping.Translate(bounds.X, bounds.Y);

                return TileBrushDirect2DHelper.CreateTileBitmapBrushCore(
                    this,
                    contentBounds,
                    BrushMappingMode.Absolute,
                    mapping,
                    TileMode.None,
                    d2dContext,
                    bounds,
                    contentBounds,
                    ctx =>
                    {
                        var rc = new RenderContext();
                        rc.Initialize((void*)ctx);
                        visual.RenderRecursive(rc);
                    });
            }
            finally
            {
                Exit();
            }
        }
    }
}

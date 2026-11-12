// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using CSUI.Core.Interop;
using CSUI.Core.Interop.DirectX;

#nullable disable

namespace System.Windows.Media
{
    internal static class BrushDirect2DHelpers
    {
        internal static D2D1_COLOR_F ToD2DColor(Color color, double opacity = 1.0)
        {
            float alphaScale = (float)Math.Clamp(opacity, 0.0, 1.0);
            return new D2D1_COLOR_F(
                color.R / 255f,
                color.G / 255f,
                color.B / 255f,
                color.ScA * alphaScale);
        }

        internal static D2D1_MATRIX_3X2_F ComputeBrushTransform(Brush brush, in Rect bounds)
        {
            Matrix matrix = Matrix.Identity;

            Transform.GetTransformValue(brush.RelativeTransform, out Matrix relativeTransform);
            if (!relativeTransform.IsIdentity && !bounds.IsEmpty && bounds.Width > 0 && bounds.Height > 0)
            {
                matrix.Translate(-bounds.X, -bounds.Y);
                matrix.Scale(1.0 / bounds.Width, 1.0 / bounds.Height);
                matrix *= relativeTransform;
                matrix.Scale(bounds.Width, bounds.Height);
                matrix.Translate(bounds.X, bounds.Y);
            }

            Transform.GetTransformValue(brush.Transform, out Matrix transform);
            if (!transform.IsIdentity)
            {
                matrix *= transform;
            }

            return MILUtilities.ConvertToD2DMatrix(in matrix);
        }

        internal static uint ToD2DGamma(ColorInterpolationMode mode)
            => mode == ColorInterpolationMode.ScRgbLinearInterpolation
                ? DxMediaConst.D2D1_GAMMA_1_0
                : DxConst.D2D1_GAMMA_2_2;

        internal static uint ToD2DExtendMode(GradientSpreadMethod spread) => spread switch
        {
            GradientSpreadMethod.Reflect => DxMediaConst.D2D1_EXTEND_MODE_MIRROR,
            GradientSpreadMethod.Repeat => DxMediaConst.D2D1_EXTEND_MODE_WRAP,
            _ => DxConst.D2D1_EXTEND_MODE_CLAMP,
        };

        internal static unsafe void* CreateGradientStopCollection(
            void* d2dContext,
            GradientBrush gradient)
        {
            GradientStopCollection stops = gradient.GradientStops;
            if (stops == null || stops.Count == 0)
            {
                return null;
            }

            int count = stops.Count;
            D2D1_GRADIENT_STOP[] heapStops = count > 32 ? new D2D1_GRADIENT_STOP[count] : null;
            D2D1_GRADIENT_STOP* pStops = stackalloc D2D1_GRADIENT_STOP[count <= 32 ? count : 1];

            fixed (D2D1_GRADIENT_STOP* pHeap = heapStops)
            {
                D2D1_GRADIENT_STOP* buffer = heapStops != null ? pHeap : pStops;
                for (int i = 0; i < count; i++)
                {
                    GradientStop stop = stops[i];
                    buffer[i] = new D2D1_GRADIENT_STOP
                    {
                        Position = (float)stop.Offset,
                        Color = ToD2DColor(stop.Color),
                    };
                }

                void* collection = null;
                int hr = ID2D1DeviceContextVtbl.CreateGradientStopCollection(
                    d2dContext,
                    buffer,
                    (uint)count,
                    ToD2DGamma(gradient.ColorInterpolationMode),
                    ToD2DExtendMode(gradient.SpreadMethod),
                    &collection);
                HResult.ThrowIfFailed(hr, "ID2D1DeviceContext.CreateGradientStopCollection");
                return collection;
            }
        }
    }

    public abstract partial class Brush
    {
        internal virtual unsafe void* CreateD2DBrush(void* d2dContext, in Rect bounds)
        {
            return null;
        }
    }

    public sealed partial class SolidColorBrush
    {
        internal override unsafe void* CreateD2DBrush(void* d2dContext, in Rect bounds)
        {
            if (d2dContext == null)
            {
                return null;
            }

            double opacity = Math.Clamp(Opacity, 0.0, 1.0);
            D2D1_COLOR_F color = BrushDirect2DHelpers.ToD2DColor(Color);
            var props = new D2D1_BRUSH_PROPERTIES
            {
                Opacity = (float)opacity,
                Transform = BrushDirect2DHelpers.ComputeBrushTransform(this, in bounds),
            };

            void* brush = null;
            int hr = ID2D1DeviceContextVtbl.CreateSolidColorBrush(d2dContext, in color, &props, &brush);
            HResult.ThrowIfFailed(hr, "ID2D1DeviceContext.CreateSolidColorBrush");
            return brush;
        }
    }

    public sealed partial class LinearGradientBrush
    {
        internal override unsafe void* CreateD2DBrush(void* d2dContext, in Rect bounds)
        {
            if (d2dContext == null)
            {
                return null;
            }

            using var stopCollection = new ComPtr<byte>(
                (byte*)BrushDirect2DHelpers.CreateGradientStopCollection(d2dContext, this));
            if (stopCollection.IsNull)
            {
                return null;
            }

            Point start = StartPoint;
            Point end = EndPoint;
            if (MappingMode == BrushMappingMode.RelativeToBoundingBox)
            {
                start = new Point(bounds.X + start.X * bounds.Width, bounds.Y + start.Y * bounds.Height);
                end = new Point(bounds.X + end.X * bounds.Width, bounds.Y + end.Y * bounds.Height);
            }

            var linearProps = new D2D1_LINEAR_GRADIENT_BRUSH_PROPERTIES
            {
                StartPoint = new D2D1_POINT_2F((float)start.X, (float)start.Y),
                EndPoint = new D2D1_POINT_2F((float)end.X, (float)end.Y),
            };

            var brushProps = new D2D1_BRUSH_PROPERTIES
            {
                Opacity = (float)Math.Clamp(Opacity, 0.0, 1.0),
                Transform = BrushDirect2DHelpers.ComputeBrushTransform(this, in bounds),
            };

            void* brush = null;
            int hr = ID2D1DeviceContextVtbl.CreateLinearGradientBrush(
                d2dContext,
                in linearProps,
                &brushProps,
                stopCollection.Ptr,
                &brush);
            HResult.ThrowIfFailed(hr, "ID2D1DeviceContext.CreateLinearGradientBrush");
            return brush;
        }
    }

    public sealed partial class RadialGradientBrush
    {
        internal override unsafe void* CreateD2DBrush(void* d2dContext, in Rect bounds)
        {
            if (d2dContext == null)
            {
                return null;
            }

            using var stopCollection = new ComPtr<byte>(
                (byte*)BrushDirect2DHelpers.CreateGradientStopCollection(d2dContext, this));
            if (stopCollection.IsNull)
            {
                return null;
            }

            Point center = Center;
            Point origin = GradientOrigin;
            double radiusX = RadiusX;
            double radiusY = RadiusY;

            if (MappingMode == BrushMappingMode.RelativeToBoundingBox)
            {
                center = new Point(bounds.X + center.X * bounds.Width, bounds.Y + center.Y * bounds.Height);
                origin = new Point(bounds.X + origin.X * bounds.Width, bounds.Y + origin.Y * bounds.Height);
                radiusX *= bounds.Width;
                radiusY *= bounds.Height;
            }

            var radialProps = new D2D1_RADIAL_GRADIENT_BRUSH_PROPERTIES
            {
                center = new D2D1_POINT_2F((float)center.X, (float)center.Y),
                gradientOriginOffset = new D2D1_POINT_2F((float)(origin.X - center.X), (float)(origin.Y - center.Y)),
                radiusX = (float)radiusX,
                radiusY = (float)radiusY,
            };

            var brushProps = new D2D1_BRUSH_PROPERTIES
            {
                Opacity = (float)Math.Clamp(Opacity, 0.0, 1.0),
                Transform = BrushDirect2DHelpers.ComputeBrushTransform(this, in bounds),
            };

            void* brush = null;
            int hr = ID2D1DeviceContextExVtbl.CreateRadialGradientBrush(
                d2dContext,
                in radialProps,
                &brushProps,
                stopCollection.Ptr,
                &brush);
            HResult.ThrowIfFailed(hr, "ID2D1DeviceContext.CreateRadialGradientBrush");
            return brush;
        }
    }
}

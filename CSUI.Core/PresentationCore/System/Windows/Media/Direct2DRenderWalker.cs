// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System.Collections.Generic;
using CSUI.Core.Interop.DirectX;

namespace System.Windows.Media
{
    /// <summary>
    /// DrawingContextWalker that renders Drawing / RenderData instructions onto a Direct2D ID2D1DeviceContext.
    /// </summary>
    internal sealed unsafe class Direct2DRenderWalker : DrawingContextWalker
    {
        private enum PushKind
        {
            Transform,
            AxisAlignedClip,
            Layer,
            NoOp
        }

        private struct PushState
        {
            public PushKind Kind;
            public D2D1_MATRIX_3X2_F SavedTransform;
        }

        [ThreadStatic]
        private static Direct2DRenderWalker t_pooledWalker;

        private void* _d2dContext;
        private void* _d2dFactory;
        private readonly Stack<PushState> _pushStack = new(8);

        private Direct2DRenderWalker(void* d2dContext, void* d2dFactory)
        {
            _d2dContext = d2dContext;
            _d2dFactory = d2dFactory;
        }

        private static Direct2DRenderWalker Acquire(void* d2dContext, void* d2dFactory)
        {
            Direct2DRenderWalker walker = t_pooledWalker;
            if (walker != null)
            {
                t_pooledWalker = null;
                walker._d2dContext = d2dContext;
                walker._d2dFactory = d2dFactory;
                walker._pushStack.Clear();
                return walker;
            }
            return new Direct2DRenderWalker(d2dContext, d2dFactory);
        }

        private static void Release(Direct2DRenderWalker walker)
        {
            walker._pushStack.Clear();
            walker._d2dContext = null;
            walker._d2dFactory = null;
            t_pooledWalker ??= walker;
        }

        internal static void Render(IDrawingContent content, void* d2dContext)
        {
            if (content == null || d2dContext == null)
            {
                return;
            }

            void* d2dFactory = MediaSystem.EnsureSharedDevice().D2DFactory;
            Direct2DRenderWalker walker = Acquire(d2dContext, d2dFactory);
            try
            {
                content.WalkContent(walker);
                walker.FlushUnpoppedStates();
            }
            finally
            {
                Release(walker);
            }
        }

        internal static void Render(Drawing drawing, void* d2dContext)
        {
            if (drawing == null || d2dContext == null)
            {
                return;
            }

            void* d2dFactory = MediaSystem.EnsureSharedDevice().D2DFactory;
            Direct2DRenderWalker walker = Acquire(d2dContext, d2dFactory);
            try
            {
                drawing.WalkCurrentValue(walker);
                walker.FlushUnpoppedStates();
            }
            finally
            {
                Release(walker);
            }
        }

        private void FlushUnpoppedStates()
        {
            while (_pushStack.Count > 0)
            {
                Pop();
            }
        }

        public override void DrawLine(Pen pen, Point point0, Point point1)
        {
            if (_d2dContext == null || pen == null || pen.Brush == null || pen.Thickness <= 0.0 || double.IsNaN(pen.Thickness))
            {
                return;
            }

            Point p0 = point0;
            Point p1 = point1;
            SnapLineForRendering(ref p0, ref p1, pen.Thickness);

            Rect bounds = new Rect(p0, p1);
            double inflate = Math.Max(1.0, pen.Thickness);
            bounds.Inflate(inflate, inflate);

            void* d2dBrush = pen.Brush.CreateD2DBrush(_d2dContext, bounds);
            if (d2dBrush == null)
            {
                return;
            }

            void* d2dStrokeStyle = _d2dFactory != null ? pen.CreateD2DStrokeStyle(_d2dFactory) : null;
            try
            {
                ID2D1DeviceContextVtbl.DrawLine(
                    _d2dContext,
                    new D2D1_POINT_2F((float)p0.X, (float)p0.Y),
                    new D2D1_POINT_2F((float)p1.X, (float)p1.Y),
                    d2dBrush,
                    (float)pen.Thickness,
                    d2dStrokeStyle);
            }
            finally
            {
                if (d2dStrokeStyle != null)
                {
                    ComHelpers.Release(d2dStrokeStyle);
                }

                ComHelpers.Release(d2dBrush);
            }
        }

        public override void DrawRectangle(Brush brush, Pen pen, Rect rectangle)
        {
            if (_d2dContext == null || rectangle.IsEmpty || (brush == null && pen == null))
            {
                return;
            }

            D2D1_RECT_F d2dRect = ToD2DRect(rectangle);

            if (brush != null)
            {
                void* fillBrush = brush.CreateD2DBrush(_d2dContext, rectangle);
                if (fillBrush != null)
                {
                    try
                    {
                        ID2D1DeviceContextVtbl.FillRectangle(_d2dContext, d2dRect, fillBrush);
                    }
                    finally
                    {
                        ComHelpers.Release(fillBrush);
                    }
                }
            }

            if (pen != null && pen.Brush != null && pen.Thickness > 0.0 && !double.IsNaN(pen.Thickness))
            {
                Rect penBounds = rectangle;
                penBounds.Inflate(pen.Thickness * 0.5, pen.Thickness * 0.5);
                void* strokeBrush = pen.Brush.CreateD2DBrush(_d2dContext, penBounds);
                if (strokeBrush != null)
                {
                    void* strokeStyle = _d2dFactory != null ? pen.CreateD2DStrokeStyle(_d2dFactory) : null;
                    D2D1_RECT_F strokeRect = SnapRectForStroke(rectangle, pen.Thickness);
                    try
                    {
                        ID2D1DeviceContextVtbl.DrawRectangle(_d2dContext, strokeRect, strokeBrush, (float)pen.Thickness, strokeStyle);
                    }
                    finally
                    {
                        if (strokeStyle != null)
                        {
                            ComHelpers.Release(strokeStyle);
                        }

                        ComHelpers.Release(strokeBrush);
                    }
                }
            }
        }

        public override void DrawRoundedRectangle(Brush brush, Pen pen, Rect rectangle, double radiusX, double radiusY)
        {
            if (_d2dContext == null || rectangle.IsEmpty || (brush == null && pen == null))
            {
                return;
            }

            D2D1_ROUNDED_RECT roundedRect = new D2D1_ROUNDED_RECT
            {
                Rect = ToD2DRect(rectangle),
                RadiusX = (float)Math.Abs(radiusX),
                RadiusY = (float)Math.Abs(radiusY)
            };

            if (brush != null)
            {
                void* fillBrush = brush.CreateD2DBrush(_d2dContext, rectangle);
                if (fillBrush != null)
                {
                    try
                    {
                        ID2D1DeviceContextVtbl.FillRoundedRectangle(_d2dContext, roundedRect, fillBrush);
                    }
                    finally
                    {
                        ComHelpers.Release(fillBrush);
                    }
                }
            }

            if (pen != null && pen.Brush != null && pen.Thickness > 0.0 && !double.IsNaN(pen.Thickness))
            {
                Rect penBounds = rectangle;
                penBounds.Inflate(pen.Thickness * 0.5, pen.Thickness * 0.5);
                void* strokeBrush = pen.Brush.CreateD2DBrush(_d2dContext, penBounds);
                if (strokeBrush != null)
                {
                    void* strokeStyle = _d2dFactory != null ? pen.CreateD2DStrokeStyle(_d2dFactory) : null;
                    try
                    {
                        ID2D1DeviceContextVtbl.DrawRoundedRectangle(_d2dContext, roundedRect, strokeBrush, (float)pen.Thickness, strokeStyle);
                    }
                    finally
                    {
                        if (strokeStyle != null)
                        {
                            ComHelpers.Release(strokeStyle);
                        }

                        ComHelpers.Release(strokeBrush);
                    }
                }
            }
        }

        public override void DrawEllipse(Brush brush, Pen pen, Point center, double radiusX, double radiusY)
        {
            if (_d2dContext == null || (brush == null && pen == null))
            {
                return;
            }

            double rx = Math.Abs(radiusX);
            double ry = Math.Abs(radiusY);
            D2D1_ELLIPSE ellipse = new D2D1_ELLIPSE
            {
                Point = new D2D1_POINT_2F((float)center.X, (float)center.Y),
                RadiusX = (float)rx,
                RadiusY = (float)ry
            };
            Rect bounds = new Rect(center.X - rx, center.Y - ry, rx * 2.0, ry * 2.0);

            if (brush != null)
            {
                void* fillBrush = brush.CreateD2DBrush(_d2dContext, bounds);
                if (fillBrush != null)
                {
                    try
                    {
                        ID2D1DeviceContextVtbl.FillEllipse(_d2dContext, ellipse, fillBrush);
                    }
                    finally
                    {
                        ComHelpers.Release(fillBrush);
                    }
                }
            }

            if (pen != null && pen.Brush != null && pen.Thickness > 0.0 && !double.IsNaN(pen.Thickness))
            {
                Rect penBounds = bounds;
                penBounds.Inflate(pen.Thickness * 0.5, pen.Thickness * 0.5);
                void* strokeBrush = pen.Brush.CreateD2DBrush(_d2dContext, penBounds);
                if (strokeBrush != null)
                {
                    void* strokeStyle = _d2dFactory != null ? pen.CreateD2DStrokeStyle(_d2dFactory) : null;
                    try
                    {
                        ID2D1DeviceContextVtbl.DrawEllipse(_d2dContext, ellipse, strokeBrush, (float)pen.Thickness, strokeStyle);
                    }
                    finally
                    {
                        if (strokeStyle != null)
                        {
                            ComHelpers.Release(strokeStyle);
                        }

                        ComHelpers.Release(strokeBrush);
                    }
                }
            }
        }

        public override void DrawGeometry(Brush brush, Pen pen, Geometry geometry)
        {
            if (_d2dContext == null || _d2dFactory == null || geometry == null || (brush == null && pen == null))
            {
                return;
            }

            void* d2dGeom = geometry.CreateD2DGeometry(_d2dFactory);
            if (d2dGeom == null)
            {
                return;
            }

            try
            {
                if (brush != null)
                {
                    Rect fillBounds = geometry.Bounds;
                    void* fillBrush = brush.CreateD2DBrush(_d2dContext, fillBounds);
                    if (fillBrush != null)
                    {
                        try
                        {
                            ID2D1DeviceContextVtbl.FillGeometry(_d2dContext, d2dGeom, fillBrush, null);
                        }
                        finally
                        {
                            ComHelpers.Release(fillBrush);
                        }
                    }
                }

                if (pen != null && pen.Brush != null && pen.Thickness > 0.0 && !double.IsNaN(pen.Thickness))
                {
                    Rect penBounds = geometry.GetRenderBounds(pen);
                    void* strokeBrush = pen.Brush.CreateD2DBrush(_d2dContext, penBounds);
                    if (strokeBrush != null)
                    {
                        void* strokeStyle = pen.CreateD2DStrokeStyle(_d2dFactory);
                        try
                        {
                            ID2D1DeviceContextVtbl.DrawGeometry(_d2dContext, d2dGeom, strokeBrush, (float)pen.Thickness, strokeStyle);
                        }
                        finally
                        {
                            if (strokeStyle != null)
                            {
                                ComHelpers.Release(strokeStyle);
                            }

                            ComHelpers.Release(strokeBrush);
                        }
                    }
                }
            }
            finally
            {
                ComHelpers.Release(d2dGeom);
            }
        }

        public override void DrawImage(ImageSource imageSource, Rect rectangle)
        {
            if (_d2dContext == null || imageSource == null || rectangle.IsEmpty)
            {
                return;
            }

            imageSource.DrawD2D(_d2dContext, rectangle);
        }

        public override void DrawGlyphRun(Brush foregroundBrush, GlyphRun glyphRun)
        {
            if (_d2dContext == null || foregroundBrush == null || glyphRun == null)
            {
                return;
            }

            glyphRun.DrawD2D(_d2dContext, foregroundBrush);
        }

        public override void DrawDrawing(Drawing drawing)
        {
            if (_d2dContext == null || drawing == null)
            {
                return;
            }

            drawing.WalkCurrentValue(this);
        }

        public override void PushClip(Geometry clipGeometry)
        {
            if (_d2dContext == null || clipGeometry == null)
            {
                _pushStack.Push(new PushState { Kind = PushKind.NoOp });
                return;
            }

            if (clipGeometry is RectangleGeometry rectGeom &&
                (rectGeom.Transform == null || rectGeom.Transform.IsIdentity) &&
                rectGeom.RadiusX == 0.0 &&
                rectGeom.RadiusY == 0.0)
            {
                Rect rect = rectGeom.Rect;
                if (!rect.IsEmpty)
                {
                    ID2D1DeviceContextVtbl.PushAxisAlignedClip(_d2dContext, ToD2DRect(rect), DxConst.D2D1_ANTIALIAS_MODE_PER_PRIMITIVE);
                    _pushStack.Push(new PushState { Kind = PushKind.AxisAlignedClip });
                    return;
                }
            }

            void* d2dGeom = _d2dFactory != null ? clipGeometry.CreateD2DGeometry(_d2dFactory) : null;
            if (d2dGeom == null)
            {
                _pushStack.Push(new PushState { Kind = PushKind.NoOp });
                return;
            }

            try
            {
                D2D1_LAYER_PARAMETERS1 layerParams = CreateDefaultLayerParameters();
                layerParams.GeometricMask = d2dGeom;
                ID2D1DeviceContextVtbl.PushLayer(_d2dContext, layerParams, null);
                _pushStack.Push(new PushState { Kind = PushKind.Layer });
            }
            finally
            {
                ComHelpers.Release(d2dGeom);
            }
        }

        public override void PushOpacityMask(Brush brush)
        {
            if (_d2dContext == null || brush == null)
            {
                _pushStack.Push(new PushState { Kind = PushKind.NoOp });
                return;
            }

            // Use a fallback target bounds of (0,0,1,1) unless viewport sizing is handled by Visual.
            void* d2dBrush = brush.CreateD2DBrush(_d2dContext, new Rect(0, 0, 1, 1));
            if (d2dBrush == null)
            {
                _pushStack.Push(new PushState { Kind = PushKind.NoOp });
                return;
            }

            try
            {
                D2D1_LAYER_PARAMETERS1 layerParams = CreateDefaultLayerParameters();
                layerParams.OpacityBrush = d2dBrush;
                ID2D1DeviceContextVtbl.PushLayer(_d2dContext, layerParams, null);
                _pushStack.Push(new PushState { Kind = PushKind.Layer });
            }
            finally
            {
                ComHelpers.Release(d2dBrush);
            }
        }

        public override void PushOpacity(double opacity)
        {
            if (_d2dContext == null)
            {
                _pushStack.Push(new PushState { Kind = PushKind.NoOp });
                return;
            }

            float clamped = (float)Math.Clamp(opacity, 0.0, 1.0);
            if (clamped >= 1.0f)
            {
                _pushStack.Push(new PushState { Kind = PushKind.NoOp });
                return;
            }

            D2D1_LAYER_PARAMETERS1 layerParams = CreateDefaultLayerParameters();
            layerParams.Opacity = clamped;
            ID2D1DeviceContextVtbl.PushLayer(_d2dContext, layerParams, null);
            _pushStack.Push(new PushState { Kind = PushKind.Layer });
        }

        public override void PushTransform(Transform transform)
        {
            if (_d2dContext == null || transform == null || transform.IsIdentity)
            {
                _pushStack.Push(new PushState { Kind = PushKind.NoOp });
                return;
            }

            ID2D1DeviceContextExVtbl.GetTransform(_d2dContext, out D2D1_MATRIX_3X2_F prevD2D);
            Matrix prevWpf = MILUtilities.ConvertFromD2DMatrix(prevD2D);
            Matrix combined = Matrix.Multiply(transform.Value, prevWpf);
            ID2D1DeviceContextVtbl.SetTransform(_d2dContext, MILUtilities.ConvertToD2DMatrix(combined));

            _pushStack.Push(new PushState
            {
                Kind = PushKind.Transform,
                SavedTransform = prevD2D
            });
        }

        public override void PushGuidelineSet(GuidelineSet guidelines)
        {
            _pushStack.Push(new PushState { Kind = PushKind.NoOp });
        }

        internal override void PushGuidelineY1(double coordinate)
        {
            _pushStack.Push(new PushState { Kind = PushKind.NoOp });
        }

        internal override void PushGuidelineY2(double leadingCoordinate, double offsetToDrivenCoordinate)
        {
            _pushStack.Push(new PushState { Kind = PushKind.NoOp });
        }

        public override void Pop()
        {
            if (_d2dContext == null || _pushStack.Count == 0)
            {
                return;
            }

            PushState state = _pushStack.Pop();
            switch (state.Kind)
            {
                case PushKind.Transform:
                    ID2D1DeviceContextVtbl.SetTransform(_d2dContext, state.SavedTransform);
                    break;
                case PushKind.AxisAlignedClip:
                    ID2D1DeviceContextVtbl.PopAxisAlignedClip(_d2dContext);
                    break;
                case PushKind.Layer:
                    ID2D1DeviceContextVtbl.PopLayer(_d2dContext);
                    break;
            }
        }

        internal static D2D1_RECT_F ToD2DRect(Rect rect)
        {
            return new D2D1_RECT_F
            {
                Left = (float)rect.Left,
                Top = (float)rect.Top,
                Right = (float)rect.Right,
                Bottom = (float)rect.Bottom
            };
        }

        private static void SnapLineForRendering(ref Point p0, ref Point p1, double thickness)
        {
            if (Math.Abs(p0.Y - p1.Y) < 0.001)
            {
                double y = p0.Y;
                y = IsOddThickness(thickness) ? (Math.Floor(y) + 0.5) : Math.Round(y);
                p0 = new Point(p0.X, y);
                p1 = new Point(p1.X, y);
            }
            else if (Math.Abs(p0.X - p1.X) < 0.001)
            {
                double x = p0.X;
                x = IsOddThickness(thickness) ? (Math.Floor(x) + 0.5) : Math.Round(x);
                p0 = new Point(x, p0.Y);
                p1 = new Point(x, p1.Y);
            }
        }

        private static D2D1_RECT_F SnapRectForStroke(Rect rect, double thickness)
        {
            if (IsOddThickness(thickness) && rect.Width >= 1.0 && rect.Height >= 1.0)
            {
                float left = (float)(Math.Floor(rect.Left) + 0.5);
                float top = (float)(Math.Floor(rect.Top) + 0.5);
                float right = (float)(Math.Floor(rect.Right) - 0.5);
                float bottom = (float)(Math.Floor(rect.Bottom) - 0.5);
                if (right >= left && bottom >= top)
                {
                    return new D2D1_RECT_F { Left = left, Top = top, Right = right, Bottom = bottom };
                }
            }
            return ToD2DRect(rect);
        }

        private static bool IsOddThickness(double thickness)
        {
            int intThick = (int)Math.Round(thickness);
            return Math.Abs(thickness - intThick) < 0.05 && (intThick % 2 != 0);
        }

        internal static D2D1_LAYER_PARAMETERS1 CreateDefaultLayerParameters()
        {
            return new D2D1_LAYER_PARAMETERS1
            {
                ContentBounds = new D2D1_RECT_F
                {
                    Left = float.NegativeInfinity,
                    Top = float.NegativeInfinity,
                    Right = float.PositiveInfinity,
                    Bottom = float.PositiveInfinity
                },
                GeometricMask = null,
                MaskAntialiasMode = DxConst.D2D1_ANTIALIAS_MODE_PER_PRIMITIVE,
                MaskTransform = new D2D1_MATRIX_3X2_F { M11 = 1.0f, M22 = 1.0f },
                Opacity = 1.0f,
                OpacityBrush = null,
                LayerOptions = 0
            };
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using CSUI.Core.Interop;
using CSUI.Core.Interop.DirectX;
using MS.Internal;

#nullable disable

namespace System.Windows.Media
{
    public sealed partial class RectangleGeometry : Geometry
    {
        #region Constructors

        public RectangleGeometry()
        {
        }

        public RectangleGeometry(Rect rect)
        {
            Rect = rect;
        }

        public RectangleGeometry(Rect rect, double radiusX, double radiusY)
            : this(rect)
        {
            RadiusX = radiusX;
            RadiusY = radiusY;
        }

        public RectangleGeometry(Rect rect, double radiusX, double radiusY, Transform transform)
            : this(rect, radiusX, radiusY)
        {
            Transform = transform;
        }

        #endregion

        #region Bounds

        public override Rect Bounds
        {
            get
            {
                ReadPreamble();

                Rect currentRect = Rect;
                Transform transform = Transform;
                if (currentRect.IsEmpty)
                {
                    return Rect.Empty;
                }

                if (transform == null || transform.IsIdentity)
                {
                    return currentRect;
                }

                return GetBoundsInternal(null, Matrix.Identity, StandardFlatteningTolerance, ToleranceType.Absolute);
            }
        }

        internal override bool AreClose(Geometry geometry)
        {
            if (geometry is RectangleGeometry rectGeometry2)
            {
                Rect rect1 = Rect;
                Rect rect2 = rectGeometry2.Rect;

                return
                    DoubleUtil.AreClose(rect1.X, rect2.X) &&
                    DoubleUtil.AreClose(rect1.Y, rect2.Y) &&
                    DoubleUtil.AreClose(rect1.Width, rect2.Width) &&
                    DoubleUtil.AreClose(rect1.Height, rect2.Height) &&
                    DoubleUtil.AreClose(RadiusX, rectGeometry2.RadiusX) &&
                    DoubleUtil.AreClose(RadiusY, rectGeometry2.RadiusY) &&
                    (Transform == rectGeometry2.Transform) &&
                    (IsFrozen == rectGeometry2.IsFrozen);
            }

            return base.AreClose(geometry);
        }

        internal override Rect GetBoundsInternal(Pen pen, Matrix worldMatrix, double tolerance, ToleranceType type)
        {
            Transform.GetTransformValue(Transform, out Matrix geometryMatrix);
            return GetBoundsHelper(pen, worldMatrix, Rect, RadiusX, RadiusY, geometryMatrix, tolerance, type);
        }

        internal static Rect GetBoundsHelper(
            Pen pen,
            Matrix worldMatrix,
            Rect rect,
            double radiusX,
            double radiusY,
            Matrix geometryMatrix,
            double tolerance,
            ToleranceType type)
        {
            if (rect.IsEmpty)
            {
                return Rect.Empty;
            }

            if ((pen == null || pen.DoesNotContainGaps) &&
                geometryMatrix.IsIdentity &&
                worldMatrix.IsIdentity)
            {
                double strokeThickness = 0.0;
                if (Pen.ContributesToBounds(pen))
                {
                    strokeThickness = Math.Abs(pen.Thickness);
                }

                return new Rect(
                    rect.X - 0.5 * strokeThickness,
                    rect.Y - 0.5 * strokeThickness,
                    rect.Width + strokeThickness,
                    rect.Height + strokeThickness);
            }

            RectangleGeometry temp = geometryMatrix.IsIdentity
                ? new RectangleGeometry(rect, radiusX, radiusY)
                : new RectangleGeometry(rect, radiusX, radiusY, new MatrixTransform(geometryMatrix));
            return GeometryDirect2DHelpers.GetBounds(temp, pen, worldMatrix, tolerance, type);
        }

        #endregion

        #region Direct2D

        internal override unsafe void* CreateD2DGeometry(void* d2dFactory)
        {
            Rect r = Rect;
            if (r.IsEmpty)
            {
                return null;
            }

            void* rawGeo = null;
            double rx = RadiusX;
            double ry = RadiusY;
            D2D1_RECT_F d2dRect = new D2D1_RECT_F((float)r.Left, (float)r.Top, (float)r.Right, (float)r.Bottom);

            int hr;
            if (IsRounded(rx, ry))
            {
                float clampedRx = (float)Math.Min(r.Width * 0.5, Math.Abs(rx));
                float clampedRy = (float)Math.Min(r.Height * 0.5, Math.Abs(ry));
                D2D1_ROUNDED_RECT rounded = new D2D1_ROUNDED_RECT
                {
                    Rect = d2dRect,
                    RadiusX = clampedRx,
                    RadiusY = clampedRy,
                };
                hr = ID2D1FactoryExVtbl.CreateRoundedRectangleGeometry(d2dFactory, in rounded, &rawGeo);
                HResult.ThrowIfFailed(hr, "ID2D1Factory.CreateRoundedRectangleGeometry");
            }
            else
            {
                hr = ID2D1FactoryExVtbl.CreateRectangleGeometry(d2dFactory, in d2dRect, &rawGeo);
                HResult.ThrowIfFailed(hr, "ID2D1Factory.CreateRectangleGeometry");
            }

            return GeometryDirect2DHelpers.WrapWithTransform(d2dFactory, rawGeo, Transform);
        }

        #endregion

        #region Area & Helpers

        public override double GetArea(double tolerance, ToleranceType type)
        {
            ReadPreamble();

            if (IsEmpty())
            {
                return 0.0;
            }

            double radiusX = RadiusX;
            double radiusY = RadiusY;
            Rect rect = Rect;

            double area = Math.Abs(rect.Width * rect.Height);
            if (IsRounded(radiusX, radiusY))
            {
                double rx = Math.Min(rect.Width * 0.5, Math.Abs(radiusX));
                double ry = Math.Min(rect.Height * 0.5, Math.Abs(radiusY));
                area -= rx * ry * (4.0 - Math.PI);
            }

            Transform transform = Transform;
            if (transform != null && !transform.IsIdentity)
            {
                area *= Math.Abs(transform.Value.Determinant);
            }

            return area;
        }

        internal override PathFigureCollection GetTransformedFigureCollection(Transform transform)
        {
            if (IsEmpty())
            {
                return null;
            }

            Matrix matrix = GetCombinedMatrix(transform);
            double radiusX = RadiusX;
            double radiusY = RadiusY;
            Rect rect = Rect;

            if (IsRounded(radiusX, radiusY))
            {
                Point[] points = GetPointList(rect, radiusX, radiusY);

                if (!matrix.IsIdentity)
                {
                    for (int i = 0; i < points.Length; i++)
                    {
                        points[i] *= matrix;
                    }
                }

                PathFigureCollection collection = new PathFigureCollection();
                collection.Add(
                    new PathFigure(
                        points[0],
                        new PathSegment[]
                        {
                            new BezierSegment(points[1], points[2], points[3], true, true),
                            new LineSegment(points[4], true, true),
                            new BezierSegment(points[5], points[6], points[7], true, true),
                            new LineSegment(points[8], true, true),
                            new BezierSegment(points[9], points[10], points[11], true, true),
                            new LineSegment(points[12], true, true),
                            new BezierSegment(points[13], points[14], points[15], true, true),
                        },
                        true));

                return collection;
            }
            else
            {
                PathFigureCollection collection = new PathFigureCollection();
                collection.Add(
                    new PathFigure(
                        rect.TopLeft * matrix,
                        new PathSegment[]
                        {
                            new PolyLineSegment(
                                new Point[]
                                {
                                    rect.TopRight * matrix,
                                    rect.BottomRight * matrix,
                                    rect.BottomLeft * matrix,
                                },
                                true),
                        },
                        true));

                return collection;
            }
        }

        internal static bool IsRounded(double radiusX, double radiusY)
        {
            return (radiusX != 0.0) && (radiusY != 0.0);
        }

        internal bool IsRounded()
        {
            return RadiusX != 0.0 && RadiusY != 0.0;
        }

        internal override PathGeometry GetAsPathGeometry()
        {
            PathStreamGeometryContext ctx = new PathStreamGeometryContext(FillRule.EvenOdd, Transform);
            Rect rect = Rect;
            if (!rect.IsEmpty)
            {
                double radiusX = RadiusX;
                double radiusY = RadiusY;
                if (IsRounded(radiusX, radiusY))
                {
                    Point[] points = GetPointList(rect, radiusX, radiusY);
                    ctx.BeginFigure(points[0], isFilled: true, isClosed: true);
                    ctx.BezierTo(points[1], points[2], points[3], isStroked: true, isSmoothJoin: false);
                    ctx.LineTo(points[4], isStroked: true, isSmoothJoin: false);
                    ctx.BezierTo(points[5], points[6], points[7], isStroked: true, isSmoothJoin: false);
                    ctx.LineTo(points[8], isStroked: true, isSmoothJoin: false);
                    ctx.BezierTo(points[9], points[10], points[11], isStroked: true, isSmoothJoin: false);
                    ctx.LineTo(points[12], isStroked: true, isSmoothJoin: false);
                    ctx.BezierTo(points[13], points[14], points[15], isStroked: true, isSmoothJoin: false);
                }
                else
                {
                    ctx.BeginFigure(rect.TopLeft, isFilled: true, isClosed: true);
                    ctx.LineTo(rect.TopRight, isStroked: true, isSmoothJoin: false);
                    ctx.LineTo(rect.BottomRight, isStroked: true, isSmoothJoin: false);
                    ctx.LineTo(rect.BottomLeft, isStroked: true, isSmoothJoin: false);
                }
            }
            return ctx.GetPathGeometry();
        }

        private static Point[] GetPointList(Rect rect, double radiusX, double radiusY)
        {
            Point[] points = new Point[17];

            radiusX = Math.Min(rect.Width * 0.5, Math.Abs(radiusX));
            radiusY = Math.Min(rect.Height * 0.5, Math.Abs(radiusY));

            double bezierX = (1.0 - EllipseGeometry.c_arcAsBezier) * radiusX;
            double bezierY = (1.0 - EllipseGeometry.c_arcAsBezier) * radiusY;

            points[1].X = points[0].X = points[15].X = points[14].X = rect.X;
            points[2].X = points[13].X = rect.X + bezierX;
            points[3].X = points[12].X = rect.X + radiusX;
            points[4].X = points[11].X = rect.Right - radiusX;
            points[5].X = points[10].X = rect.Right - bezierX;
            points[6].X = points[7].X = points[8].X = points[9].X = rect.Right;

            points[2].Y = points[3].Y = points[4].Y = points[5].Y = rect.Y;
            points[1].Y = points[6].Y = rect.Y + bezierY;
            points[0].Y = points[7].Y = rect.Y + radiusY;
            points[15].Y = points[8].Y = rect.Bottom - radiusY;
            points[14].Y = points[9].Y = rect.Bottom - bezierY;
            points[13].Y = points[12].Y = points[11].Y = points[10].Y = rect.Bottom;

            points[16] = points[0];
            return points;
        }

        public override bool IsEmpty() => Rect.IsEmpty;

        public override bool MayHaveCurves() => IsRounded();

        #endregion
    }
}

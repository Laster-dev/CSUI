// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using CSUI.Core.Interop;
using CSUI.Core.Interop.DirectX;

#nullable disable

namespace System.Windows.Media
{
    public sealed partial class EllipseGeometry : Geometry
    {
        #region Constructors

        public EllipseGeometry()
        {
        }

        public EllipseGeometry(Point center, double radiusX, double radiusY)
        {
            Center = center;
            RadiusX = radiusX;
            RadiusY = radiusY;
        }

        public EllipseGeometry(Point center, double radiusX, double radiusY, Transform transform)
            : this(center, radiusX, radiusY)
        {
            Transform = transform;
        }

        public EllipseGeometry(Rect rect)
        {
            if (rect.IsEmpty)
            {
                throw new System.ArgumentOutOfRangeException(nameof(rect), MS.Internal.PresentationCore.SR.Format(MS.Internal.PresentationCore.SR.Rect_Empty, "rect"));
            }

            RadiusX = (rect.Right - rect.X) * 0.5;
            RadiusY = (rect.Bottom - rect.Y) * 0.5;
            Center = new Point(rect.X + RadiusX, rect.Y + RadiusY);
        }

        #endregion

        #region Bounds

        public override Rect Bounds
        {
            get
            {
                ReadPreamble();

                Transform transform = Transform;
                if (transform == null || transform.IsIdentity)
                {
                    Point currentCenter = Center;
                    double currentRadiusX = Math.Abs(RadiusX);
                    double currentRadiusY = Math.Abs(RadiusY);

                    return new Rect(
                        currentCenter.X - currentRadiusX,
                        currentCenter.Y - currentRadiusY,
                        2.0 * currentRadiusX,
                        2.0 * currentRadiusY);
                }

                return GetBoundsInternal(null, Matrix.Identity, StandardFlatteningTolerance, ToleranceType.Absolute);
            }
        }

        internal override Rect GetBoundsInternal(Pen pen, Matrix worldMatrix, double tolerance, ToleranceType type)
        {
            Transform.GetTransformValue(Transform, out Matrix geometryMatrix);
            return GetBoundsHelper(pen, worldMatrix, Center, RadiusX, RadiusY, geometryMatrix, tolerance, type);
        }

        internal static Rect GetBoundsHelper(
            Pen pen,
            Matrix worldMatrix,
            Point center,
            double radiusX,
            double radiusY,
            Matrix geometryMatrix,
            double tolerance,
            ToleranceType type)
        {
            if ((pen == null || pen.DoesNotContainGaps) &&
                worldMatrix.IsIdentity &&
                geometryMatrix.IsIdentity)
            {
                double strokeThickness = 0.0;
                if (Pen.ContributesToBounds(pen))
                {
                    strokeThickness = Math.Abs(pen.Thickness);
                }

                double rx = Math.Abs(radiusX);
                double ry = Math.Abs(radiusY);

                return new Rect(
                    center.X - rx - 0.5 * strokeThickness,
                    center.Y - ry - 0.5 * strokeThickness,
                    2.0 * rx + strokeThickness,
                    2.0 * ry + strokeThickness);
            }

            EllipseGeometry temp = new EllipseGeometry(center, radiusX, radiusY);
            if (!geometryMatrix.IsIdentity)
            {
                temp.Transform = new MatrixTransform(geometryMatrix);
            }

            return GeometryDirect2DHelpers.GetBounds(temp, pen, worldMatrix, tolerance, type);
        }

        #endregion

        #region Direct2D

        internal override unsafe void* CreateD2DGeometry(void* d2dFactory)
        {
            Point center = Center;
            D2D1_ELLIPSE ellipse = new D2D1_ELLIPSE
            {
                Point = GeometryDirect2DHelpers.ToD2DPoint(center),
                RadiusX = (float)Math.Abs(RadiusX),
                RadiusY = (float)Math.Abs(RadiusY),
            };

            void* rawGeo = null;
            int hr = ID2D1FactoryExVtbl.CreateEllipseGeometry(d2dFactory, in ellipse, &rawGeo);
            HResult.ThrowIfFailed(hr, "ID2D1Factory.CreateEllipseGeometry");

            return GeometryDirect2DHelpers.WrapWithTransform(d2dFactory, rawGeo, Transform);
        }

        #endregion

        #region Public Methods

        public override bool IsEmpty() => false;

        public override bool MayHaveCurves() => true;

        public override double GetArea(double tolerance, ToleranceType type)
        {
            ReadPreamble();

            double area = Math.Abs(RadiusX * RadiusY) * Math.PI;
            Transform transform = Transform;
            if (transform != null && !transform.IsIdentity)
            {
                area *= Math.Abs(transform.Value.Determinant);
            }

            return area;
        }

        #endregion

        internal override PathFigureCollection GetTransformedFigureCollection(Transform transform)
        {
            Point[] points = GetPointList();

            Matrix matrix = GetCombinedMatrix(transform);
            if (!matrix.IsIdentity)
            {
                for (int i = 0; i < points.Length; i++)
                {
                    points[i] *= matrix;
                }
            }

            PathFigureCollection figureCollection = new PathFigureCollection();
            figureCollection.Add(
                new PathFigure(
                    points[0],
                    new PathSegment[]
                    {
                        new BezierSegment(points[1], points[2], points[3], true, true),
                        new BezierSegment(points[4], points[5], points[6], true, true),
                        new BezierSegment(points[7], points[8], points[9], true, true),
                        new BezierSegment(points[10], points[11], points[12], true, true),
                    },
                    true));

            return figureCollection;
        }

        internal override PathGeometry GetAsPathGeometry()
        {
            PathStreamGeometryContext ctx = new PathStreamGeometryContext(FillRule.EvenOdd, Transform);
            Point[] points = GetPointList();
            ctx.BeginFigure(points[0], isFilled: true, isClosed: true);
            for (int i = 0; i < 12; i += 3)
            {
                ctx.BezierTo(points[i + 1], points[i + 2], points[i + 3], isStroked: true, isSmoothJoin: true);
            }
            return ctx.GetPathGeometry();
        }

        private Point[] GetPointList()
        {
            Point[] points = new Point[13];
            Point center = Center;
            double radiusX = Math.Abs(RadiusX);
            double radiusY = Math.Abs(RadiusY);

            double midX = radiusX * c_arcAsBezier;
            points[0].X = points[1].X = points[11].X = points[12].X = center.X + radiusX;
            points[2].X = points[10].X = center.X + midX;
            points[3].X = points[9].X = center.X;
            points[4].X = points[8].X = center.X - midX;
            points[5].X = points[6].X = points[7].X = center.X - radiusX;

            double midY = radiusY * c_arcAsBezier;
            points[2].Y = points[3].Y = points[4].Y = center.Y + radiusY;
            points[1].Y = points[5].Y = center.Y + midY;
            points[0].Y = points[6].Y = points[12].Y = center.Y;
            points[7].Y = points[11].Y = center.Y - midY;
            points[8].Y = points[9].Y = points[10].Y = center.Y - radiusY;

            return points;
        }

        internal const double c_arcAsBezier = 0.5522847498307933984;
    }
}

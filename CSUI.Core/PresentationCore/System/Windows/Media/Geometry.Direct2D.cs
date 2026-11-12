// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using CSUI.Core.Interop;
using CSUI.Core.Interop.DirectX;
using MS.Internal;

#nullable disable

namespace System.Windows.Media
{
    public abstract partial class Geometry
    {
        internal virtual unsafe void* CreateD2DGeometry(void* d2dFactory)
        {
            return null;
        }
    }

    internal static unsafe class GeometryDirect2DHelpers
    {
        internal static uint ToD2DFillMode(FillRule fillRule)
            => fillRule == FillRule.Nonzero
                ? DxMediaConst.D2D1_FILL_MODE_WINDING
                : DxMediaConst.D2D1_FILL_MODE_ALTERNATE;

        internal static uint ToD2DCombineMode(GeometryCombineMode mode) => mode switch
        {
            GeometryCombineMode.Union => DxMediaConst.D2D1_COMBINE_MODE_UNION,
            GeometryCombineMode.Intersect => DxMediaConst.D2D1_COMBINE_MODE_INTERSECT,
            GeometryCombineMode.Xor => DxMediaConst.D2D1_COMBINE_MODE_XOR,
            GeometryCombineMode.Exclude => DxMediaConst.D2D1_COMBINE_MODE_EXCLUDE,
            _ => DxMediaConst.D2D1_COMBINE_MODE_UNION,
        };

        internal static IntersectionDetail FromD2DGeometryRelation(uint relation) => relation switch
        {
            DxMediaConst.D2D1_GEOMETRY_RELATION_DISJOINT => IntersectionDetail.Empty,
            DxMediaConst.D2D1_GEOMETRY_RELATION_IS_CONTAINED => IntersectionDetail.FullyInside,
            DxMediaConst.D2D1_GEOMETRY_RELATION_CONTAINS => IntersectionDetail.FullyContains,
            DxMediaConst.D2D1_GEOMETRY_RELATION_OVERLAP => IntersectionDetail.Intersects,
            _ => IntersectionDetail.NotCalculated,
        };

        internal static D2D1_POINT_2F ToD2DPoint(Point p)
            => new D2D1_POINT_2F((float)p.X, (float)p.Y);

        internal static Point ToWpfPoint(D2D1_POINT_2F p)
            => new Point(p.X, p.Y);

        internal static Rect FromD2DRect(in D2D1_RECT_F rect)
        {
            if (float.IsNaN(rect.Left) || float.IsNaN(rect.Top) || float.IsNaN(rect.Right) || float.IsNaN(rect.Bottom) ||
                rect.Right < rect.Left || rect.Bottom < rect.Top)
            {
                return Rect.Empty;
            }

            return new Rect(rect.Left, rect.Top, rect.Right - rect.Left, rect.Bottom - rect.Top);
        }

        internal static float ToD2DTolerance(double tolerance, ToleranceType type, void* d2dGeometry = null)
        {
            double absTol = double.IsNaN(tolerance) ? Geometry.StandardFlatteningTolerance : Math.Abs(tolerance);

            if (type == ToleranceType.Relative && d2dGeometry != null)
            {
                if (ID2D1GeometryVtbl.GetBounds(d2dGeometry, null, out D2D1_RECT_F bounds) >= 0 &&
                    bounds.Right >= bounds.Left && bounds.Bottom >= bounds.Top)
                {
                    float maxSpan = Math.Max(1.0f, Math.Max(bounds.Right - bounds.Left, bounds.Bottom - bounds.Top));
                    double scaled = absTol * maxSpan;
                    if (!double.IsNaN(scaled) && !double.IsInfinity(scaled))
                    {
                        return Math.Max(1e-4f, (float)scaled);
                    }
                }
            }

            if (double.IsInfinity(absTol))
            {
                return 0.25f;
            }

            return Math.Max(1e-4f, (float)absTol);
        }

        internal static void* WrapWithTransform(void* d2dFactory, void* rawGeometry, Transform transform)
        {
            if (rawGeometry == null)
            {
                return null;
            }

            if (transform == null || transform.IsIdentity)
            {
                return rawGeometry;
            }

            using var raw = new ComPtr<byte>((byte*)rawGeometry);
            D2D1_MATRIX_3X2_F d2dTransform = transform.ToD2DMatrix();
            void* transformed = null;
            int hr = ID2D1FactoryExVtbl.CreateTransformedGeometry(d2dFactory, rawGeometry, in d2dTransform, &transformed);
            HResult.ThrowIfFailed(hr, "ID2D1Factory.CreateTransformedGeometry");
            return transformed;
        }

        internal static void* CreateD2DPathFromFigures(void* d2dFactory, PathFigureCollection figures, FillRule fillRule, Transform transform)
        {
            if (figures == null || figures.Count == 0)
            {
                return null;
            }

            void* rawPath = null;
            int hr = ID2D1FactoryExVtbl.CreatePathGeometry(d2dFactory, &rawPath);
            HResult.ThrowIfFailed(hr, "ID2D1Factory.CreatePathGeometry");

            try
            {
                void* sink = null;
                hr = ID2D1PathGeometryVtbl.Open(rawPath, &sink);
                HResult.ThrowIfFailed(hr, "ID2D1PathGeometry.Open");

                try
                {
                    FeedFiguresToD2DSink(sink, figures, fillRule);
                    hr = ID2D1GeometrySinkVtbl.Close(sink);
                    HResult.ThrowIfFailed(hr, "ID2D1GeometrySink.Close");
                }
                finally
                {
                    using var sinkCom = new ComPtr<byte>((byte*)sink);
                }

                return WrapWithTransform(d2dFactory, rawPath, transform);
            }
            catch
            {
                using var pathCom = new ComPtr<byte>((byte*)rawPath);
                throw;
            }
        }

        internal static void FeedFiguresToD2DSink(void* sink, PathFigureCollection figures, FillRule fillRule)
        {
            ID2D1GeometrySinkVtbl.SetFillMode(sink, ToD2DFillMode(fillRule));

            if (figures == null || figures.Count == 0)
            {
                return;
            }

            uint currentFlags = uint.MaxValue;
            int figCount = figures.Count;
            for (int f = 0; f < figCount; f++)
            {
                PathFigure figure = figures.Internal_GetItem(f);
                uint figBegin = figure.IsFilled
                    ? DxMediaConst.D2D1_FIGURE_BEGIN_FILLED
                    : DxMediaConst.D2D1_FIGURE_BEGIN_HOLLOW;

                ID2D1GeometrySinkVtbl.BeginFigure(sink, ToD2DPoint(figure.StartPoint), figBegin);

                PathSegmentCollection segments = figure.Segments;
                if (segments != null)
                {
                    int segCount = segments.Count;
                    for (int s = 0; s < segCount; s++)
                    {
                        PathSegment segment = segments.Internal_GetItem(s);
                        uint segFlags = DxMediaConst.D2D1_PATH_SEGMENT_NONE;
                        if (!segment.IsStroked)
                        {
                            segFlags |= DxMediaConst.D2D1_PATH_SEGMENT_FORCE_UNSTROKED;
                        }
                        if (segment.IsSmoothJoin)
                        {
                            segFlags |= DxMediaConst.D2D1_PATH_SEGMENT_FORCE_ROUND_LINE_JOIN;
                        }

                        if (segFlags != currentFlags)
                        {
                            ID2D1GeometrySinkVtbl.SetSegmentFlags(sink, segFlags);
                            currentFlags = segFlags;
                        }

                        switch (segment)
                        {
                            case LineSegment line:
                                ID2D1GeometrySinkVtbl.AddLine(sink, ToD2DPoint(line.Point));
                                break;

                            case BezierSegment bezier:
                                ID2D1GeometrySinkVtbl.AddBezier(sink, new D2D1_BEZIER_SEGMENT
                                {
                                    point1 = ToD2DPoint(bezier.Point1),
                                    point2 = ToD2DPoint(bezier.Point2),
                                    point3 = ToD2DPoint(bezier.Point3),
                                });
                                break;

                            case QuadraticBezierSegment quad:
                                ID2D1GeometrySinkVtbl.AddQuadraticBezier(sink, new D2D1_QUADRATIC_BEZIER_SEGMENT
                                {
                                    point1 = ToD2DPoint(quad.Point1),
                                    point2 = ToD2DPoint(quad.Point2),
                                });
                                break;

                            case ArcSegment arc:
                            {
                                Size arcSize = arc.Size;
                                ID2D1GeometrySinkVtbl.AddArc(sink, new D2D1_ARC_SEGMENT
                                {
                                    point = ToD2DPoint(arc.Point),
                                    size = new D2D1_SIZE_F((float)Math.Abs(arcSize.Width), (float)Math.Abs(arcSize.Height)),
                                    rotationAngle = (float)arc.RotationAngle,
                                    sweepDirection = arc.SweepDirection == SweepDirection.Clockwise
                                        ? DxMediaConst.D2D1_SWEEP_DIRECTION_CLOCKWISE
                                        : DxMediaConst.D2D1_SWEEP_DIRECTION_COUNTER_CLOCKWISE,
                                    arcSize = arc.IsLargeArc
                                        ? DxMediaConst.D2D1_ARC_SIZE_LARGE
                                        : DxMediaConst.D2D1_ARC_SIZE_SMALL,
                                });
                                break;
                            }

                            case PolyLineSegment polyLine:
                            {
                                PointCollection pts = polyLine.Points;
                                if (pts != null)
                                {
                                    int count = pts.Count;
                                    for (int i = 0; i < count; i++)
                                    {
                                        ID2D1GeometrySinkVtbl.AddLine(sink, ToD2DPoint(pts.Internal_GetItem(i)));
                                    }
                                }
                                break;
                            }

                            case PolyBezierSegment polyBezier:
                            {
                                PointCollection pts = polyBezier.Points;
                                if (pts != null)
                                {
                                    int count = pts.Count;
                                    for (int i = 0; i + 2 < count; i += 3)
                                    {
                                        ID2D1GeometrySinkVtbl.AddBezier(sink, new D2D1_BEZIER_SEGMENT
                                        {
                                            point1 = ToD2DPoint(pts.Internal_GetItem(i)),
                                            point2 = ToD2DPoint(pts.Internal_GetItem(i + 1)),
                                            point3 = ToD2DPoint(pts.Internal_GetItem(i + 2)),
                                        });
                                    }
                                }
                                break;
                            }

                            case PolyQuadraticBezierSegment polyQuad:
                            {
                                PointCollection pts = polyQuad.Points;
                                if (pts != null)
                                {
                                    int count = pts.Count;
                                    for (int i = 0; i + 1 < count; i += 2)
                                    {
                                        ID2D1GeometrySinkVtbl.AddQuadraticBezier(sink, new D2D1_QUADRATIC_BEZIER_SEGMENT
                                        {
                                            point1 = ToD2DPoint(pts.Internal_GetItem(i)),
                                            point2 = ToD2DPoint(pts.Internal_GetItem(i + 1)),
                                        });
                                    }
                                }
                                break;
                            }
                        }
                    }
                }

                uint figEnd = figure.IsClosed
                    ? DxMediaConst.D2D1_FIGURE_END_CLOSED
                    : DxMediaConst.D2D1_FIGURE_END_OPEN;
                ID2D1GeometrySinkVtbl.EndFigure(sink, figEnd);
            }
        }

        internal static Rect GetBounds(Geometry geometry, Pen pen, Matrix worldMatrix, double tolerance, ToleranceType type)
        {
            if (geometry == null || geometry.IsObviouslyEmpty())
            {
                return Rect.Empty;
            }

            DxSharedDevice device = MediaSystem.EnsureSharedDevice();
            void* factory = device.D2DFactory;
            using var d2dGeo = new ComPtr<byte>((byte*)geometry.CreateD2DGeometry(factory));
            if (d2dGeo.IsNull)
            {
                return Rect.Empty;
            }

            D2D1_MATRIX_3X2_F d2dWorld = default;
            D2D1_MATRIX_3X2_F* pWorld = null;
            if (!worldMatrix.IsIdentity)
            {
                d2dWorld = MILUtilities.ConvertToD2DMatrix(in worldMatrix);
                pWorld = &d2dWorld;
            }

            D2D1_RECT_F rect;
            if (Pen.ContributesToBounds(pen))
            {
                using var strokeStyle = new ComPtr<byte>((byte*)pen.CreateD2DStrokeStyle(factory));
                float tol = ToD2DTolerance(tolerance, type, d2dGeo.Ptr);
                int hr = ID2D1GeometryVtbl.GetWidenedBounds(
                    d2dGeo.Ptr,
                    (float)Math.Abs(pen.Thickness),
                    strokeStyle.Ptr,
                    pWorld,
                    tol,
                    out rect);
                HResult.ThrowIfFailed(hr, "ID2D1Geometry.GetWidenedBounds");
            }
            else
            {
                int hr = ID2D1GeometryVtbl.GetBounds(d2dGeo.Ptr, pWorld, out rect);
                HResult.ThrowIfFailed(hr, "ID2D1Geometry.GetBounds");
            }

            return FromD2DRect(in rect);
        }

        internal static double GetArea(Geometry geometry, double tolerance, ToleranceType type)
        {
            if (geometry == null || geometry.IsObviouslyEmpty())
            {
                return 0.0;
            }

            DxSharedDevice device = MediaSystem.EnsureSharedDevice();
            void* factory = device.D2DFactory;
            using var d2dGeo = new ComPtr<byte>((byte*)geometry.CreateD2DGeometry(factory));
            if (d2dGeo.IsNull)
            {
                return 0.0;
            }

            float tol = ToD2DTolerance(tolerance, type, d2dGeo.Ptr);
            int hr = ID2D1GeometryVtbl.ComputeArea(d2dGeo.Ptr, null, tol, out float area);
            HResult.ThrowIfFailed(hr, "ID2D1Geometry.ComputeArea");
            return float.IsNaN(area) ? 0.0 : area;
        }

        internal static bool ContainsPoint(Geometry geometry, Pen pen, Point hitPoint, double tolerance, ToleranceType type)
        {
            if (geometry == null || geometry.IsObviouslyEmpty())
            {
                return false;
            }

            DxSharedDevice device = MediaSystem.EnsureSharedDevice();
            void* factory = device.D2DFactory;
            using var d2dGeo = new ComPtr<byte>((byte*)geometry.CreateD2DGeometry(factory));
            if (d2dGeo.IsNull)
            {
                return false;
            }

            float tol = ToD2DTolerance(tolerance, type, d2dGeo.Ptr);
            D2D1_POINT_2F pt = ToD2DPoint(hitPoint);
            bool contains;
            if (pen != null)
            {
                using var strokeStyle = new ComPtr<byte>((byte*)pen.CreateD2DStrokeStyle(factory));
                int hr = ID2D1GeometryVtbl.StrokeContainsPoint(
                    d2dGeo.Ptr,
                    pt,
                    (float)Math.Abs(pen.Thickness),
                    strokeStyle.Ptr,
                    null,
                    tol,
                    out contains);
                HResult.ThrowIfFailed(hr, "ID2D1Geometry.StrokeContainsPoint");
            }
            else
            {
                int hr = ID2D1GeometryVtbl.FillContainsPoint(d2dGeo.Ptr, pt, null, tol, out contains);
                HResult.ThrowIfFailed(hr, "ID2D1Geometry.FillContainsPoint");
            }

            return contains;
        }

        internal static IntersectionDetail CompareWithGeometry(Geometry target, Geometry hitGeometry, double tolerance, ToleranceType type)
        {
            if (target == null || target.IsObviouslyEmpty() || hitGeometry == null || hitGeometry.IsObviouslyEmpty())
            {
                return IntersectionDetail.Empty;
            }

            DxSharedDevice device = MediaSystem.EnsureSharedDevice();
            void* factory = device.D2DFactory;
            using var geo1 = new ComPtr<byte>((byte*)target.CreateD2DGeometry(factory));
            using var geo2 = new ComPtr<byte>((byte*)hitGeometry.CreateD2DGeometry(factory));
            if (geo1.IsNull || geo2.IsNull)
            {
                return IntersectionDetail.Empty;
            }

            float tol = ToD2DTolerance(tolerance, type, geo1.Ptr);
            int hr = ID2D1GeometryVtbl.CompareWithGeometry(geo1.Ptr, geo2.Ptr, null, tol, out uint relation);
            HResult.ThrowIfFailed(hr, "ID2D1Geometry.CompareWithGeometry");
            return FromD2DGeometryRelation(relation);
        }

        internal static PathGeometry Flatten(Geometry geometry, double tolerance, ToleranceType type)
        {
            if (geometry == null || geometry.IsObviouslyEmpty())
            {
                return new PathGeometry();
            }

            DxSharedDevice device = MediaSystem.EnsureSharedDevice();
            void* factory = device.D2DFactory;
            using var d2dGeo = new ComPtr<byte>((byte*)geometry.CreateD2DGeometry(factory));
            if (d2dGeo.IsNull)
            {
                return new PathGeometry();
            }

            float tol = ToD2DTolerance(tolerance, type, d2dGeo.Ptr);
            return ManagedD2DSimplifiedGeometrySink.ReadBack(nativeSink =>
                ID2D1GeometryVtbl.Simplify(
                    d2dGeo.Ptr,
                    DxMediaConst.D2D1_GEOMETRY_SIMPLIFICATION_OPTION_LINES,
                    null,
                    tol,
                    (void*)nativeSink));
        }

        internal static PathGeometry Widen(Geometry geometry, Pen pen, double tolerance, ToleranceType type)
        {
            if (geometry == null || pen == null || geometry.IsObviouslyEmpty())
            {
                return new PathGeometry();
            }

            DxSharedDevice device = MediaSystem.EnsureSharedDevice();
            void* factory = device.D2DFactory;
            using var d2dGeo = new ComPtr<byte>((byte*)geometry.CreateD2DGeometry(factory));
            if (d2dGeo.IsNull)
            {
                return new PathGeometry();
            }

            using var strokeStyle = new ComPtr<byte>((byte*)pen.CreateD2DStrokeStyle(factory));
            float tol = ToD2DTolerance(tolerance, type, d2dGeo.Ptr);
            return ManagedD2DSimplifiedGeometrySink.ReadBack(nativeSink =>
                ID2D1GeometryVtbl.Widen(
                    d2dGeo.Ptr,
                    (float)Math.Abs(pen.Thickness),
                    strokeStyle.Ptr,
                    null,
                    tol,
                    (void*)nativeSink));
        }

        internal static PathGeometry Outline(Geometry geometry, double tolerance, ToleranceType type)
        {
            if (geometry == null || geometry.IsObviouslyEmpty())
            {
                return new PathGeometry();
            }

            DxSharedDevice device = MediaSystem.EnsureSharedDevice();
            void* factory = device.D2DFactory;
            using var d2dGeo = new ComPtr<byte>((byte*)geometry.CreateD2DGeometry(factory));
            if (d2dGeo.IsNull)
            {
                return new PathGeometry();
            }

            float tol = ToD2DTolerance(tolerance, type, d2dGeo.Ptr);
            return ManagedD2DSimplifiedGeometrySink.ReadBack(nativeSink =>
                ID2D1GeometryVtbl.Outline(d2dGeo.Ptr, null, tol, (void*)nativeSink));
        }

        internal static PathGeometry Combine(
            Geometry geometry1,
            Geometry geometry2,
            GeometryCombineMode mode,
            Transform transform,
            double tolerance,
            ToleranceType type)
        {
            bool empty1 = geometry1 == null || geometry1.IsObviouslyEmpty();
            bool empty2 = geometry2 == null || geometry2.IsObviouslyEmpty();

            if (mode == GeometryCombineMode.Intersect && (empty1 || empty2))
            {
                return new PathGeometry();
            }
            if (mode == GeometryCombineMode.Exclude && empty1)
            {
                return new PathGeometry();
            }
            if (empty1 && empty2)
            {
                return new PathGeometry();
            }

            DxSharedDevice device = MediaSystem.EnsureSharedDevice();
            void* factory = device.D2DFactory;

            using var g1 = new ComPtr<byte>(empty1 ? null : (byte*)geometry1.CreateD2DGeometry(factory));
            using var g2 = new ComPtr<byte>(empty2 ? null : (byte*)geometry2.CreateD2DGeometry(factory));

            if (g1.IsNull && g2.IsNull)
            {
                return new PathGeometry();
            }

            PathGeometry result;
            if (g1.IsNull || g2.IsNull)
            {
                // Empty placeholder PathGeometry for Direct2D CombineWithGeometry semantics
                void* emptyPath = null;
                int hrEmpty = ID2D1FactoryExVtbl.CreatePathGeometry(factory, &emptyPath);
                HResult.ThrowIfFailed(hrEmpty, "ID2D1Factory.CreatePathGeometry");
                using var emptyCom = new ComPtr<byte>((byte*)emptyPath);
                void* lhs = g1.IsNull ? emptyPath : g1.Ptr;
                void* rhs = g2.IsNull ? emptyPath : g2.Ptr;
                float tolEmpty = ToD2DTolerance(tolerance, type, lhs);
                result = ManagedD2DSimplifiedGeometrySink.ReadBack(nativeSink =>
                    ID2D1GeometryVtbl.CombineWithGeometry(lhs, rhs, ToD2DCombineMode(mode), null, tolEmpty, (void*)nativeSink));
            }
            else
            {
                float tol = ToD2DTolerance(tolerance, type, g1.Ptr);
                result = ManagedD2DSimplifiedGeometrySink.ReadBack(nativeSink =>
                    ID2D1GeometryVtbl.CombineWithGeometry(g1.Ptr, g2.Ptr, ToD2DCombineMode(mode), null, tol, (void*)nativeSink));
            }

            if (transform != null && !transform.IsIdentity)
            {
                Matrix m = transform.Value;
                PathFigureCollection transformedFigures = new PathFigureCollection();
                PathFigureCollection src = result.Figures;
                for (int i = 0; i < src.Count; i++)
                {
                    transformedFigures.Add(src.Internal_GetItem(i).GetTransformedCopy(m));
                }
                result.Figures = transformedFigures;
            }

            return result;
        }

        internal static void GetPointAtFractionLength(
            PathGeometry geometry,
            double progress,
            out Point point,
            out Point tangent)
        {
            if (geometry == null || geometry.IsEmpty())
            {
                point = new Point();
                tangent = new Point();
                return;
            }

            DxSharedDevice device = MediaSystem.EnsureSharedDevice();
            void* factory = device.D2DFactory;
            using var d2dGeo = new ComPtr<byte>((byte*)geometry.CreateD2DGeometry(factory));
            if (d2dGeo.IsNull)
            {
                point = new Point();
                tangent = new Point();
                return;
            }

            float tol = DxMediaConst.D2D1_DEFAULT_FLATTENING_TOLERANCE;
            int hr = ID2D1GeometryVtbl.ComputeLength(d2dGeo.Ptr, null, tol, out float length);
            HResult.ThrowIfFailed(hr, "ID2D1Geometry.ComputeLength");

            double clamped = double.IsNaN(progress) ? 0.0 : Math.Clamp(progress, 0.0, 1.0);
            float dist = length * (float)clamped;

            D2D1_POINT_2F pt = default;
            D2D1_POINT_2F tan = default;
            hr = ID2D1GeometryVtbl.ComputePointAtLength(d2dGeo.Ptr, dist, null, tol, &pt, &tan);
            HResult.ThrowIfFailed(hr, "ID2D1Geometry.ComputePointAtLength");

            point = ToWpfPoint(pt);
            tangent = (length <= 0f || (tan.X == 0f && tan.Y == 0f))
                ? new Point(1.0, 0.0)
                : ToWpfPoint(tan);
        }

        internal static void ArcToBezier(
            Point startPoint,
            Size size,
            double rotationAngle,
            bool isLargeArc,
            SweepDirection sweepDirection,
            Point endPoint,
            in Matrix matrix,
            Span<Point> points,
            out int bezierCount)
        {
            double rx = Math.Abs(size.Width);
            double ry = Math.Abs(size.Height);

            if (DoubleUtil.AreClose(startPoint, endPoint) || DoubleUtil.IsZero(rx) || DoubleUtil.IsZero(ry))
            {
                points[0] = endPoint * matrix;
                bezierCount = 0;
                return;
            }

            double phi = rotationAngle * (Math.PI / 180.0);
            double cosPhi = Math.Cos(phi);
            double sinPhi = Math.Sin(phi);

            double dx2 = (startPoint.X - endPoint.X) * 0.5;
            double dy2 = (startPoint.Y - endPoint.Y) * 0.5;

            double x1p = cosPhi * dx2 + sinPhi * dy2;
            double y1p = -sinPhi * dx2 + cosPhi * dy2;

            double rxSq = rx * rx;
            double rySq = ry * ry;
            double x1pSq = x1p * x1p;
            double y1pSq = y1p * y1p;

            double lambda = x1pSq / rxSq + y1pSq / rySq;
            if (lambda > 1.0)
            {
                double scale = Math.Sqrt(lambda);
                rx *= scale;
                ry *= scale;
                rxSq = rx * rx;
                rySq = ry * ry;
            }

            double num = Math.Max(0.0, rxSq * rySq - rxSq * y1pSq - rySq * x1pSq);
            double den = rxSq * y1pSq + rySq * x1pSq;
            double factor = den > 0.0 ? Math.Sqrt(num / den) : 0.0;
            bool isClockwise = sweepDirection == SweepDirection.Clockwise;
            if (isLargeArc == isClockwise)
            {
                factor = -factor;
            }

            double cxp = factor * (rx * y1p) / ry;
            double cyp = factor * -(ry * x1p) / rx;

            double cx = cosPhi * cxp - sinPhi * cyp + (startPoint.X + endPoint.X) * 0.5;
            double cy = sinPhi * cxp + cosPhi * cyp + (startPoint.Y + endPoint.Y) * 0.5;

            double ux = (x1p - cxp) / rx;
            double uy = (y1p - cyp) / ry;
            double vx = (-x1p - cxp) / rx;
            double vy = (-y1p - cyp) / ry;

            double theta1 = VectorAngle(1.0, 0.0, ux, uy);
            double dtheta = VectorAngle(ux, uy, vx, vy);

            if (!isClockwise && dtheta > 0.0)
            {
                dtheta -= 2.0 * Math.PI;
            }
            else if (isClockwise && dtheta < 0.0)
            {
                dtheta += 2.0 * Math.PI;
            }

            int segments = (int)Math.Ceiling(Math.Abs(dtheta) / (Math.PI * 0.5));
            segments = Math.Clamp(segments, 1, 4);
            bezierCount = segments;

            double delta = dtheta / segments;
            double alpha = (4.0 / 3.0) * Math.Tan(delta * 0.25);

            for (int i = 0; i < segments; i++)
            {
                double t1 = theta1 + i * delta;
                double t2 = t1 + delta;

                double cos1 = Math.Cos(t1);
                double sin1 = Math.Sin(t1);
                double cos2 = Math.Cos(t2);
                double sin2 = Math.Sin(t2);

                double p1x = cos1 - alpha * sin1;
                double p1y = sin1 + alpha * cos1;
                double p2x = cos2 + alpha * sin2;
                double p2y = sin2 - alpha * cos2;

                points[3 * i] = MapUnitEllipsePoint(p1x, p1y, rx, ry, cosPhi, sinPhi, cx, cy) * matrix;
                points[3 * i + 1] = MapUnitEllipsePoint(p2x, p2y, rx, ry, cosPhi, sinPhi, cx, cy) * matrix;
                points[3 * i + 2] = (i == segments - 1)
                    ? endPoint * matrix
                    : MapUnitEllipsePoint(cos2, sin2, rx, ry, cosPhi, sinPhi, cx, cy) * matrix;
            }
        }

        private static double VectorAngle(double ux, double uy, double vx, double vy)
        {
            double dot = ux * vx + uy * vy;
            double len = Math.Sqrt((ux * ux + uy * uy) * (vx * vx + vy * vy));
            if (len == 0.0)
            {
                return 0.0;
            }

            double ratio = Math.Clamp(dot / len, -1.0, 1.0);
            double angle = Math.Acos(ratio);
            return (ux * vy - uy * vx < 0.0) ? -angle : angle;
        }

        private static Point MapUnitEllipsePoint(
            double x, double y,
            double rx, double ry,
            double cosPhi, double sinPhi,
            double cx, double cy)
        {
            double sx = x * rx;
            double sy = y * ry;
            return new Point(
                cosPhi * sx - sinPhi * sy + cx,
                sinPhi * sx + cosPhi * sy + cy);
        }

        internal sealed unsafe class ManagedD2DSimplifiedGeometrySink
        {
            private static readonly Guid IID_IUnknown = new("00000000-0000-0000-c000-000000000046");
            private static readonly void** s_vtbl;

            private enum RunKind
            {
                None,
                Line,
                Bezier,
            }

            [StructLayout(LayoutKind.Sequential)]
            private struct NativeSink
            {
                public void** Vtbl;
                public IntPtr Handle;
                public int RefCount;
            }

            static ManagedD2DSimplifiedGeometrySink()
            {
                s_vtbl = (void**)NativeMemory.AllocZeroed(10, (nuint)sizeof(void*));
                s_vtbl[0] = (delegate* unmanaged[Stdcall]<NativeSink*, Guid*, void**, int>)&QueryInterface;
                s_vtbl[1] = (delegate* unmanaged[Stdcall]<NativeSink*, uint>)&AddRef;
                s_vtbl[2] = (delegate* unmanaged[Stdcall]<NativeSink*, uint>)&Release;
                s_vtbl[3] = (delegate* unmanaged[Stdcall]<NativeSink*, uint, void>)&SetFillMode;
                s_vtbl[4] = (delegate* unmanaged[Stdcall]<NativeSink*, uint, void>)&SetSegmentFlags;
                s_vtbl[5] = (delegate* unmanaged[Stdcall]<NativeSink*, D2D1_POINT_2F, uint, void>)&BeginFigure;
                s_vtbl[6] = (delegate* unmanaged[Stdcall]<NativeSink*, D2D1_POINT_2F*, uint, void>)&AddLines;
                s_vtbl[7] = (delegate* unmanaged[Stdcall]<NativeSink*, D2D1_BEZIER_SEGMENT*, uint, void>)&AddBeziers;
                s_vtbl[8] = (delegate* unmanaged[Stdcall]<NativeSink*, uint, void>)&EndFigure;
                s_vtbl[9] = (delegate* unmanaged[Stdcall]<NativeSink*, int>)&Close;
            }

            private readonly PathFigureCollection _figures = new PathFigureCollection();
            private FillRule _fillRule = FillRule.EvenOdd;
            private PathFigure _currentFigure;
            private PathSegmentCollection _currentSegments;

            private bool _currentStroked = true;
            private bool _currentSmoothJoin;
            private RunKind _runKind = RunKind.None;
            private bool _runStroked = true;
            private bool _runSmoothJoin;
            private PointCollection _runPoints;

            internal static PathGeometry ReadBack(Func<nint, int> invokeD2D)
            {
                var managed = new ManagedD2DSimplifiedGeometrySink();
                NativeSink* native = (NativeSink*)NativeMemory.Alloc((nuint)sizeof(NativeSink));
                native->Vtbl = s_vtbl;
                native->Handle = GCHandle.ToIntPtr(GCHandle.Alloc(managed));
                native->RefCount = 1;

                try
                {
                    int hr = invokeD2D((nint)native);
                    HResult.ThrowIfFailed(hr, "Direct2D ID2D1SimplifiedGeometrySink operation");
                    managed.FlushRun();
                    return new PathGeometry(managed._figures, managed._fillRule, transform: null);
                }
                finally
                {
                    ReleaseCore(native);
                }
            }

            private static ManagedD2DSimplifiedGeometrySink Unwrap(NativeSink* self)
                => (ManagedD2DSimplifiedGeometrySink)GCHandle.FromIntPtr(self->Handle).Target!;

            private void FlushRun()
            {
                if (_runPoints == null || _runPoints.Count == 0 || _currentFigure == null)
                {
                    _runPoints = null;
                    _runKind = RunKind.None;
                    return;
                }

                _currentSegments ??= (_currentFigure.Segments = new PathSegmentCollection());

                PathSegment segment = null;
                if (_runKind == RunKind.Line)
                {
                    if (_runPoints.Count == 1)
                    {
                        segment = new LineSegment(_runPoints[0], _runStroked, _runSmoothJoin);
                    }
                    else
                    {
                        _runPoints.Freeze();
                        segment = new PolyLineSegment(_runPoints, _runStroked, _runSmoothJoin);
                    }
                }
                else if (_runKind == RunKind.Bezier)
                {
                    if (_runPoints.Count == 3)
                    {
                        segment = new BezierSegment(_runPoints[0], _runPoints[1], _runPoints[2], _runStroked, _runSmoothJoin);
                    }
                    else
                    {
                        _runPoints.Freeze();
                        segment = new PolyBezierSegment(_runPoints, _runStroked, _runSmoothJoin);
                    }
                }

                if (segment != null)
                {
                    segment.Freeze();
                    _currentSegments.Add(segment);
                }

                _runPoints = null;
                _runKind = RunKind.None;
            }

            private void PrepareRun(RunKind kind)
            {
                if (_runKind != kind || _runStroked != _currentStroked || _runSmoothJoin != _currentSmoothJoin)
                {
                    FlushRun();
                    _runKind = kind;
                    _runStroked = _currentStroked;
                    _runSmoothJoin = _currentSmoothJoin;
                }

                _runPoints ??= new PointCollection();
            }

            [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
            private static int QueryInterface(NativeSink* self, Guid* riid, void** ppv)
            {
                if (self == null || riid == null || ppv == null)
                {
                    return unchecked((int)0x80004003); // E_POINTER
                }

                if (*riid == IID_IUnknown || *riid == DxMediaGuids.IID_ID2D1SimplifiedGeometrySink)
                {
                    *ppv = self;
                    AddRefCore(self);
                    return HResult.S_OK;
                }

                *ppv = null;
                return unchecked((int)0x80004002); // E_NOINTERFACE
            }

            private static uint AddRefCore(NativeSink* self)
                => (uint)Interlocked.Increment(ref self->RefCount);

            private static uint ReleaseCore(NativeSink* self)
            {
                int remaining = Interlocked.Decrement(ref self->RefCount);
                if (remaining == 0)
                {
                    if (self->Handle != IntPtr.Zero)
                    {
                        GCHandle.FromIntPtr(self->Handle).Free();
                        self->Handle = IntPtr.Zero;
                    }
                    NativeMemory.Free(self);
                }
                return (uint)Math.Max(0, remaining);
            }

            [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
            private static uint AddRef(NativeSink* self) => AddRefCore(self);

            [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
            private static uint Release(NativeSink* self) => ReleaseCore(self);

            [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
            private static void SetFillMode(NativeSink* self, uint fillMode)
            {
                ManagedD2DSimplifiedGeometrySink sink = Unwrap(self);
                sink._fillRule = fillMode == DxMediaConst.D2D1_FILL_MODE_WINDING
                    ? FillRule.Nonzero
                    : FillRule.EvenOdd;
            }

            [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
            private static void SetSegmentFlags(NativeSink* self, uint vertexFlags)
            {
                ManagedD2DSimplifiedGeometrySink sink = Unwrap(self);
                sink._currentStroked = (vertexFlags & DxMediaConst.D2D1_PATH_SEGMENT_FORCE_UNSTROKED) == 0;
                sink._currentSmoothJoin = (vertexFlags & DxMediaConst.D2D1_PATH_SEGMENT_FORCE_ROUND_LINE_JOIN) != 0;
            }

            [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
            private static void BeginFigure(NativeSink* self, D2D1_POINT_2F startPoint, uint figureBegin)
            {
                ManagedD2DSimplifiedGeometrySink sink = Unwrap(self);
                sink.FlushRun();
                sink._currentSegments = new PathSegmentCollection();
                sink._currentFigure = new PathFigure
                {
                    StartPoint = ToWpfPoint(startPoint),
                    IsFilled = figureBegin == DxMediaConst.D2D1_FIGURE_BEGIN_FILLED,
                    Segments = sink._currentSegments,
                };
            }

            [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
            private static void AddLines(NativeSink* self, D2D1_POINT_2F* points, uint pointsCount)
            {
                if (points == null || pointsCount == 0)
                {
                    return;
                }

                ManagedD2DSimplifiedGeometrySink sink = Unwrap(self);
                if (sink._currentFigure == null)
                {
                    return;
                }

                sink.PrepareRun(RunKind.Line);
                for (uint i = 0; i < pointsCount; i++)
                {
                    sink._runPoints.Add(ToWpfPoint(points[i]));
                }
            }

            [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
            private static void AddBeziers(NativeSink* self, D2D1_BEZIER_SEGMENT* beziers, uint beziersCount)
            {
                if (beziers == null || beziersCount == 0)
                {
                    return;
                }

                ManagedD2DSimplifiedGeometrySink sink = Unwrap(self);
                if (sink._currentFigure == null)
                {
                    return;
                }

                sink.PrepareRun(RunKind.Bezier);
                for (uint i = 0; i < beziersCount; i++)
                {
                    sink._runPoints.Add(ToWpfPoint(beziers[i].point1));
                    sink._runPoints.Add(ToWpfPoint(beziers[i].point2));
                    sink._runPoints.Add(ToWpfPoint(beziers[i].point3));
                }
            }

            [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
            private static void EndFigure(NativeSink* self, uint figureEnd)
            {
                ManagedD2DSimplifiedGeometrySink sink = Unwrap(self);
                if (sink._currentFigure == null)
                {
                    return;
                }

                sink.FlushRun();
                sink._currentFigure.IsClosed = figureEnd == DxMediaConst.D2D1_FIGURE_END_CLOSED;
                sink._currentFigure.Freeze();
                sink._figures.Add(sink._currentFigure);
                sink._currentFigure = null;
                sink._currentSegments = null;
            }

            [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
            private static int Close(NativeSink* self)
            {
                ManagedD2DSimplifiedGeometrySink sink = Unwrap(self);
                sink.FlushRun();
                return HResult.S_OK;
            }
        }
    }
}

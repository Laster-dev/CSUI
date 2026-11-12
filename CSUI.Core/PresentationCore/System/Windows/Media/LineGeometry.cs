// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using CSUI.Core.Interop;
using CSUI.Core.Interop.DirectX;

#nullable disable

namespace System.Windows.Media
{
    public sealed partial class LineGeometry : Geometry
    {
        #region Constructors

        public LineGeometry()
        {
        }

        public LineGeometry(Point startPoint, Point endPoint)
        {
            StartPoint = startPoint;
            EndPoint = endPoint;
        }

        public LineGeometry(Point startPoint, Point endPoint, Transform transform)
            : this(startPoint, endPoint)
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

                Transform transform = Transform;
                if (transform == null || transform.IsIdentity)
                {
                    return new Rect(StartPoint, EndPoint);
                }

                return GetBoundsInternal(null, Matrix.Identity, StandardFlatteningTolerance, ToleranceType.Absolute);
            }
        }

        internal override Rect GetBoundsInternal(Pen pen, Matrix worldMatrix, double tolerance, ToleranceType type)
        {
            Transform.GetTransformValue(Transform, out Matrix geometryMatrix);
            return GetBoundsHelper(pen, worldMatrix, StartPoint, EndPoint, geometryMatrix, tolerance, type);
        }

        internal static Rect GetBoundsHelper(
            Pen pen,
            Matrix worldMatrix,
            Point pt1,
            Point pt2,
            Matrix geometryMatrix,
            double tolerance,
            ToleranceType type)
        {
            if (pen == null && worldMatrix.IsIdentity && geometryMatrix.IsIdentity)
            {
                return new Rect(pt1, pt2);
            }

            LineGeometry temp = geometryMatrix.IsIdentity
                ? new LineGeometry(pt1, pt2)
                : new LineGeometry(pt1, pt2, new MatrixTransform(geometryMatrix));
            return GeometryDirect2DHelpers.GetBounds(temp, pen, worldMatrix, tolerance, type);
        }

        #endregion

        #region Direct2D

        internal override unsafe void* CreateD2DGeometry(void* d2dFactory)
        {
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
                    ID2D1GeometrySinkVtbl.BeginFigure(
                        sink,
                        GeometryDirect2DHelpers.ToD2DPoint(StartPoint),
                        DxMediaConst.D2D1_FIGURE_BEGIN_FILLED);
                    ID2D1GeometrySinkVtbl.AddLine(sink, GeometryDirect2DHelpers.ToD2DPoint(EndPoint));
                    ID2D1GeometrySinkVtbl.EndFigure(sink, DxMediaConst.D2D1_FIGURE_END_OPEN);
                    hr = ID2D1GeometrySinkVtbl.Close(sink);
                    HResult.ThrowIfFailed(hr, "ID2D1GeometrySink.Close");
                }
                finally
                {
                    using var sinkCom = new ComPtr<byte>((byte*)sink);
                }

                return GeometryDirect2DHelpers.WrapWithTransform(d2dFactory, rawPath, Transform);
            }
            catch
            {
                using var pathCom = new ComPtr<byte>((byte*)rawPath);
                throw;
            }
        }

        #endregion

        #region Public Methods

        public override bool IsEmpty() => false;

        public override bool MayHaveCurves() => false;

        public override double GetArea(double tolerance, ToleranceType type) => 0.0;

        #endregion

        #region Path Conversion

        internal override PathGeometry GetAsPathGeometry()
        {
            PathStreamGeometryContext ctx = new PathStreamGeometryContext(FillRule.EvenOdd, Transform);
            ctx.BeginFigure(StartPoint, isFilled: true, isClosed: false);
            ctx.LineTo(EndPoint, isStroked: true, isSmoothJoin: false);
            return ctx.GetPathGeometry();
        }

        internal override PathFigureCollection GetTransformedFigureCollection(Transform transform)
        {
            Point startPoint = StartPoint;
            Point endPoint = EndPoint;

            Transform internalTransform = Transform;
            if (internalTransform != null && !internalTransform.IsIdentity)
            {
                Matrix matrix = internalTransform.Value;
                startPoint *= matrix;
                endPoint *= matrix;
            }

            if (transform != null && !transform.IsIdentity)
            {
                Matrix matrix = transform.Value;
                startPoint *= matrix;
                endPoint *= matrix;
            }

            PathFigureCollection collection = new PathFigureCollection();
            collection.Add(
                new PathFigure(
                    startPoint,
                    new PathSegment[] { new LineSegment(endPoint, true) },
                    false));

            return collection;
        }

        #endregion
    }
}

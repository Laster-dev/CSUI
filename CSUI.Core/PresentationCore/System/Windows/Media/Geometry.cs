// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Threading;
using System.Windows.Media.Animation;

#nullable disable

namespace System.Windows.Media
{
    public abstract partial class Geometry : Animatable
    {
        #region Constructors

        internal Geometry()
        {
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// Gets the empty Geometry
        /// </summary>
        public static Geometry Empty
        {
            get
            {
                if (s_empty == null)
                {
                    StreamGeometry empty = new StreamGeometry();
                    empty.Freeze();
                    Interlocked.CompareExchange(ref s_empty, empty, null);
                }

                return s_empty;
            }
        }

        /// <summary>
        /// Gets the bounds of this Geometry as an axis-aligned bounding box
        /// </summary>
        public virtual Rect Bounds
        {
            get
            {
                return GetBoundsInternal(null, Matrix.Identity, StandardFlatteningTolerance, ToleranceType.Absolute);
            }
        }

        /// <summary>
        /// StandardFlatteningTolerance - the standard tolerance used when flattening curve geometry.
        /// </summary>
        public const double StandardFlatteningTolerance = 0.25;

        #endregion

        #region Public Methods

        public Rect GetRenderBounds(Pen pen, double tolerance, ToleranceType type)
        {
            ReadPreamble();

            Matrix matrix = Matrix.Identity;
            return GetBoundsInternal(pen, matrix, tolerance, type);
        }

        public Rect GetRenderBounds(Pen pen)
        {
            return GetRenderBounds(pen, StandardFlatteningTolerance, ToleranceType.Absolute);
        }

        public virtual double GetArea(double tolerance, ToleranceType type)
        {
            ReadPreamble();
            return GeometryDirect2DHelpers.GetArea(this, tolerance, type);
        }

        public double GetArea()
        {
            return GetArea(StandardFlatteningTolerance, ToleranceType.Absolute);
        }

        public abstract bool IsEmpty();

        public abstract bool MayHaveCurves();

        #endregion

        #region Hit Testing

        public bool FillContains(Point hitPoint, double tolerance, ToleranceType type)
        {
            return ContainsInternal(null, hitPoint, tolerance, type);
        }

        public bool FillContains(Point hitPoint)
        {
            return ContainsInternal(null, hitPoint, StandardFlatteningTolerance, ToleranceType.Absolute);
        }

        public bool StrokeContains(Pen pen, Point hitPoint, double tolerance, ToleranceType type)
        {
            if (pen == null)
            {
                return false;
            }

            return ContainsInternal(pen, hitPoint, tolerance, type);
        }

        public bool StrokeContains(Pen pen, Point hitPoint)
        {
            return StrokeContains(pen, hitPoint, StandardFlatteningTolerance, ToleranceType.Absolute);
        }

        internal virtual bool ContainsInternal(Pen pen, Point hitPoint, double tolerance, ToleranceType type)
        {
            ReadPreamble();
            return GeometryDirect2DHelpers.ContainsPoint(this, pen, hitPoint, tolerance, type);
        }

        public bool FillContains(Geometry geometry, double tolerance, ToleranceType type)
        {
            IntersectionDetail detail = FillContainsWithDetail(geometry, tolerance, type);
            return (detail == IntersectionDetail.FullyContains);
        }

        public bool FillContains(Geometry geometry)
        {
            return FillContains(geometry, StandardFlatteningTolerance, ToleranceType.Absolute);
        }

        public virtual IntersectionDetail FillContainsWithDetail(Geometry geometry, double tolerance, ToleranceType type)
        {
            ReadPreamble();
            return GeometryDirect2DHelpers.CompareWithGeometry(this, geometry, tolerance, type);
        }

        public IntersectionDetail FillContainsWithDetail(Geometry geometry)
        {
            return FillContainsWithDetail(geometry, StandardFlatteningTolerance, ToleranceType.Absolute);
        }

        public IntersectionDetail StrokeContainsWithDetail(Pen pen, Geometry geometry, double tolerance, ToleranceType type)
        {
            if (IsObviouslyEmpty() || geometry == null || geometry.IsObviouslyEmpty() || pen == null)
            {
                return IntersectionDetail.Empty;
            }

            PathGeometry widened = GetWidenedPathGeometry(pen, tolerance, type);
            return widened.FillContainsWithDetail(geometry, tolerance, type);
        }

        public IntersectionDetail StrokeContainsWithDetail(Pen pen, Geometry geometry)
        {
            return StrokeContainsWithDetail(pen, geometry, StandardFlatteningTolerance, ToleranceType.Absolute);
        }

        #endregion

        #region Geometric Flatten / Widen / Outline / Combine

        public virtual PathGeometry GetFlattenedPathGeometry(double tolerance, ToleranceType type)
        {
            ReadPreamble();
            return GeometryDirect2DHelpers.Flatten(this, tolerance, type);
        }

        public PathGeometry GetFlattenedPathGeometry()
        {
            return GetFlattenedPathGeometry(StandardFlatteningTolerance, ToleranceType.Absolute);
        }

        public virtual PathGeometry GetWidenedPathGeometry(Pen pen, double tolerance, ToleranceType type)
        {
            ReadPreamble();
            ArgumentNullException.ThrowIfNull(pen);
            return GeometryDirect2DHelpers.Widen(this, pen, tolerance, type);
        }

        public PathGeometry GetWidenedPathGeometry(Pen pen)
        {
            return GetWidenedPathGeometry(pen, StandardFlatteningTolerance, ToleranceType.Absolute);
        }

        public static PathGeometry Combine(
            Geometry geometry1,
            Geometry geometry2,
            GeometryCombineMode mode,
            Transform transform,
            double tolerance,
            ToleranceType type)
        {
            return PathGeometry.InternalCombine(geometry1, geometry2, mode, transform, tolerance, type);
        }

        public static PathGeometry Combine(
            Geometry geometry1,
            Geometry geometry2,
            GeometryCombineMode mode,
            Transform transform)
        {
            return PathGeometry.InternalCombine(
                geometry1,
                geometry2,
                mode,
                transform,
                StandardFlatteningTolerance,
                ToleranceType.Absolute);
        }

        public virtual PathGeometry GetOutlinedPathGeometry(double tolerance, ToleranceType type)
        {
            ReadPreamble();
            return GeometryDirect2DHelpers.Outline(this, tolerance, type);
        }

        public PathGeometry GetOutlinedPathGeometry()
        {
            return GetOutlinedPathGeometry(StandardFlatteningTolerance, ToleranceType.Absolute);
        }

        #endregion

        #region Internal

        internal virtual void TransformPropertyChangedHook(DependencyPropertyChangedEventArgs e)
        {
        }

        internal Geometry GetTransformedCopy(Transform transform)
        {
            Geometry copy = Clone();
            Transform internalTransform = Transform;

            if (transform != null && !transform.IsIdentity)
            {
                if (internalTransform == null || internalTransform.IsIdentity)
                {
                    copy.Transform = transform;
                }
                else
                {
                    copy.Transform = new MatrixTransform(internalTransform.Value * transform.Value);
                }
            }

            return copy;
        }

        internal abstract PathGeometry GetAsPathGeometry();

        internal PathFigureCollection GetPathFigureCollection()
        {
            return GetTransformedFigureCollection(null);
        }

        internal Matrix GetCombinedMatrix(Transform transform)
        {
            Matrix matrix = Matrix.Identity;
            Transform internalTransform = Transform;

            if (internalTransform != null && !internalTransform.IsIdentity)
            {
                matrix = internalTransform.Value;

                if (transform != null && !transform.IsIdentity)
                {
                    matrix *= transform.Value;
                }
            }
            else if (transform != null && !transform.IsIdentity)
            {
                matrix = transform.Value;
            }

            return matrix;
        }

        internal abstract PathFigureCollection GetTransformedFigureCollection(Transform transform);

        internal virtual bool IsObviouslyEmpty()
        {
            return IsEmpty();
        }

        internal virtual bool CanSerializeToString()
        {
            return false;
        }

        internal virtual Rect GetBoundsInternal(Pen pen, Matrix worldMatrix, double tolerance, ToleranceType type)
        {
            return GeometryDirect2DHelpers.GetBounds(this, pen, worldMatrix, tolerance, type);
        }

        internal virtual Rect GetBoundsInternal(Pen pen, Matrix matrix)
        {
            return GetBoundsInternal(pen, matrix, StandardFlatteningTolerance, ToleranceType.Absolute);
        }

        internal virtual bool AreClose(Geometry geometry)
        {
            return false;
        }

        #endregion

        private static Geometry s_empty;
    }
}

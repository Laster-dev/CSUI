// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Windows.Markup;
using CSUI.Core.Interop;
using CSUI.Core.Interop.DirectX;

#nullable disable

namespace System.Windows.Media
{
    [Flags]
    internal enum PathGeometryInternalFlags
    {
        None = 0x0,
        Invalid = 0x1,
        Dirty = 0x2,
        BoundsValid = 0x4
    }

    #region PathGeometry
    [ContentProperty("Figures")]
    public sealed partial class PathGeometry : Geometry
    {
        #region Constructors

        public PathGeometry()
        {
        }

        public PathGeometry(IEnumerable<PathFigure> figures)
        {
            ArgumentNullException.ThrowIfNull(figures);

            foreach (PathFigure item in figures)
            {
                Figures.Add(item);
            }

            SetDirty();
        }

        public PathGeometry(
            IEnumerable<PathFigure> figures,
            FillRule fillRule,
            Transform transform)
        {
            ArgumentNullException.ThrowIfNull(figures);

            foreach (PathFigure item in figures)
            {
                Figures.Add(item);
            }

            FillRule = fillRule;
            Transform = transform;
            SetDirty();
        }

        internal PathGeometry(PathFigureCollection figures, FillRule fillRule, Transform transform)
        {
            Figures = figures;
            FillRule = fillRule;
            Transform = transform;
            SetDirty();
        }

        public static PathGeometry CreateFromGeometry(Geometry geometry)
        {
            if (geometry == null)
            {
                return null;
            }

            return geometry.GetAsPathGeometry();
        }

        #endregion Constructors

        protected override void OnChanged()
        {
            SetDirty();
            base.OnChanged();
        }

        #region Public Methods

        public void AddGeometry(Geometry geometry)
        {
            ArgumentNullException.ThrowIfNull(geometry);

            if (geometry.IsEmpty())
            {
                return;
            }

            PathFigureCollection figureCollection = geometry.GetPathFigureCollection();
            if (figureCollection == null)
            {
                return;
            }

            PathFigureCollection figures = Figures;
            if (figures == null)
            {
                figures = Figures = new PathFigureCollection();
            }

            for (int i = 0; i < figureCollection.Count; ++i)
            {
                figures.Add(figureCollection.Internal_GetItem(i));
            }
        }

        public void GetPointAtFractionLength(
            double progress,
            out Point point,
            out Point tangent)
        {
            GeometryDirect2DHelpers.GetPointAtFractionLength(this, progress, out point, out tangent);
        }

        public void Clear()
        {
            Figures?.Clear();
        }

        #endregion

        #region Bounds

        public override Rect Bounds
        {
            get
            {
                ReadPreamble();

                if (IsEmpty())
                {
                    return Rect.Empty;
                }

                if ((_flags & PathGeometryInternalFlags.BoundsValid) == 0)
                {
                    _bounds = GeometryDirect2DHelpers.GetBounds(
                        this,
                        null,
                        Matrix.Identity,
                        StandardFlatteningTolerance,
                        ToleranceType.Absolute);
                    _flags |= PathGeometryInternalFlags.BoundsValid;
                }

                return _bounds;
            }
        }

        #endregion

        #region IsEmpty / MayHaveCurves

        public override bool IsEmpty()
        {
            PathFigureCollection figures = Figures;
            return (figures == null) || (figures.Count <= 0);
        }

        public override bool MayHaveCurves()
        {
            PathFigureCollection figures = Figures;
            int count = (figures != null) ? figures.Count : 0;

            for (int i = 0; i < count; i++)
            {
                if (figures.Internal_GetItem(i).MayHaveCurves())
                {
                    return true;
                }
            }

            return false;
        }

        #endregion

        #region Internal Methods & Serialization

        internal override unsafe void* CreateD2DGeometry(void* d2dFactory)
        {
            return GeometryDirect2DHelpers.CreateD2DPathFromFigures(d2dFactory, Figures, FillRule, Transform);
        }

        internal static PathGeometry InternalCombine(
            Geometry geometry1,
            Geometry geometry2,
            GeometryCombineMode mode,
            Transform transform,
            double tolerance,
            ToleranceType type)
        {
            return GeometryDirect2DHelpers.Combine(geometry1, geometry2, mode, transform, tolerance, type);
        }

        internal override PathGeometry GetAsPathGeometry()
        {
            PathGeometry result = new PathGeometry
            {
                FillRule = FillRule,
                Transform = Transform,
            };

            PathFigureCollection figures = Figures;
            if (figures != null)
            {
                result.Figures = figures.Clone();
            }

            return result;
        }

        internal override PathFigureCollection GetTransformedFigureCollection(Transform transform)
        {
            Matrix matrix = GetCombinedMatrix(transform);
            PathFigureCollection result;

            if (matrix.IsIdentity)
            {
                result = Figures;
                if (result == null)
                {
                    result = new PathFigureCollection();
                }
            }
            else
            {
                result = new PathFigureCollection();
                PathFigureCollection figures = Figures;
                int count = figures != null ? figures.Count : 0;
                for (int i = 0; i < count; ++i)
                {
                    result.Add(figures.Internal_GetItem(i).GetTransformedCopy(matrix));
                }
            }

            return result;
        }

        internal override bool CanSerializeToString()
        {
            Transform transform = Transform;
            PathFigureCollection figures = Figures;

            return (((transform == null) || transform.IsIdentity) &&
                    ((figures == null) || figures.CanSerializeToString()));
        }

        internal override string ConvertToString(string format, IFormatProvider provider)
        {
            PathFigureCollection figures = Figures;
            FillRule fillRule = FillRule;

            string figuresString = string.Empty;
            if (figures != null)
            {
                figuresString = figures.ConvertToString(format, provider);
            }

            if (fillRule != FillRule.EvenOdd)
            {
                return "F1" + figuresString;
            }
            else
            {
                return figuresString;
            }
        }

        internal void SetDirty()
        {
            _flags = PathGeometryInternalFlags.Dirty;
        }

        internal override void TransformPropertyChangedHook(DependencyPropertyChangedEventArgs e)
        {
            if ((_flags & PathGeometryInternalFlags.BoundsValid) != 0)
            {
                SetDirty();
            }
        }

        internal void FiguresPropertyChangedHook(DependencyPropertyChangedEventArgs e)
        {
            SetDirty();
        }

        #endregion

        #region Data

        internal PathGeometryInternalFlags _flags = PathGeometryInternalFlags.None;
        internal Rect _bounds = Rect.Empty;

        #endregion
    }
    #endregion
}

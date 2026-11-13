// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

namespace System.Windows.Media
{
    #region StreamGeometry
    public sealed partial class StreamGeometry : Geometry
    {
        #region Constructors

        public StreamGeometry()
        {
        }

        #endregion

        #region Public Methods

        public StreamGeometryContext Open()
        {
            WritePreamble();
            return new StreamGeometryCallbackContext(this);
        }

        public void Clear()
        {
            WritePreamble();
            _figures = null;
            SetDirty();
            WritePostscript();
        }

        public override bool IsEmpty()
        {
            ReadPreamble();
            return (_figures == null) || (_figures.Count <= 0);
        }

        public override bool MayHaveCurves()
        {
            ReadPreamble();

            if (_figures == null)
            {
                return false;
            }

            for (int i = 0; i < _figures.Count; i++)
            {
                if (_figures.Internal_GetItem(i).MayHaveCurves())
                {
                    return true;
                }
            }

            return false;
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

                if (!_boundsValid)
                {
                    _bounds = GeometryDirect2DHelpers.GetBounds(
                        this,
                        null,
                        Matrix.Identity,
                        StandardFlatteningTolerance,
                        ToleranceType.Absolute);
                    _boundsValid = true;
                }

                return _bounds;
            }
        }

        #endregion

        #region Direct2D & Internal Helpers

        private nint _cachedD2DGeometry;
        private nint _cachedD2DFactory;
        private readonly object _d2dCacheLock = new();

        ~StreamGeometry()
        {
            ClearD2DCache();
        }

        internal override unsafe void* CreateD2DGeometry(void* d2dFactory)
        {
            if (d2dFactory == null)
            {
                return null;
            }

            lock (_d2dCacheLock)
            {
                if (_cachedD2DGeometry != 0 && (void*)_cachedD2DFactory == d2dFactory)
                {
                    void* p = (void*)_cachedD2DGeometry;
                    CSUI.Core.Interop.DirectX.ComHelpers.AddRef(p);
                    return p;
                }

                ClearD2DCacheNoLock();

                void* newGeom = GeometryDirect2DHelpers.CreateD2DPathFromFigures(d2dFactory, _figures, FillRule, Transform);
                if (newGeom != null)
                {
                    CSUI.Core.Interop.DirectX.ComHelpers.AddRef(newGeom);
                    _cachedD2DGeometry = (nint)newGeom;
                    _cachedD2DFactory = (nint)d2dFactory;
                }
                return newGeom;
            }
        }

        private unsafe void ClearD2DCacheNoLock()
        {
            if (_cachedD2DGeometry != 0)
            {
                void* old = (void*)_cachedD2DGeometry;
                _cachedD2DGeometry = 0;
                _cachedD2DFactory = 0;
                CSUI.Core.Interop.DirectX.ComHelpers.Release(old);
            }
        }

        private void ClearD2DCache()
        {
            lock (_d2dCacheLock)
            {
                ClearD2DCacheNoLock();
            }
        }

        internal void Close(PathFigureCollection figures)
        {
            if (figures != null && !figures.IsFrozen)
            {
                figures.Freeze();
            }

            _figures = figures;
            SetDirty();
            WritePostscript();
        }

        protected override void OnChanged()
        {
            SetDirty();
            base.OnChanged();
        }

        internal override void TransformPropertyChangedHook(DependencyPropertyChangedEventArgs e)
        {
            SetDirty();
        }

        internal void SetDirty()
        {
            _boundsValid = false;
            _bounds = Rect.Empty;
            ClearD2DCache();
        }

        internal override PathGeometry GetAsPathGeometry()
        {
            ReadPreamble();

            PathGeometry result = new PathGeometry
            {
                FillRule = FillRule,
                Transform = Transform,
            };

            if (_figures != null)
            {
                result.Figures = _figures.Clone();
            }

            return result;
        }

        internal override PathFigureCollection GetTransformedFigureCollection(Transform transform)
        {
            return GetAsPathGeometry().GetTransformedFigureCollection(transform);
        }

        private bool HasHollows()
        {
            if (_figures == null)
            {
                return false;
            }

            for (int i = 0; i < _figures.Count; i++)
            {
                if (!_figures.Internal_GetItem(i).IsFilled)
                {
                    return true;
                }
            }

            return false;
        }

        private bool HasGaps()
        {
            if (_figures == null)
            {
                return false;
            }

            for (int f = 0; f < _figures.Count; f++)
            {
                PathSegmentCollection segments = _figures.Internal_GetItem(f).Segments;
                if (segments != null)
                {
                    for (int s = 0; s < segments.Count; s++)
                    {
                        if (!segments.Internal_GetItem(s).IsStroked)
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        internal override bool CanSerializeToString()
        {
            Transform transform = Transform;
            return (((transform == null) || transform.IsIdentity) &&
                    !HasHollows() && !HasGaps());
        }

        internal override string ConvertToString(string format, IFormatProvider provider)
        {
            return GetAsPathGeometry().ConvertToString(format, provider);
        }

        protected override void CloneCore(Freezable source)
        {
            base.CloneCore(source);
            StreamGeometry sourceStream = (StreamGeometry)source;
            if (sourceStream._figures != null)
            {
                _figures = (PathFigureCollection)sourceStream._figures.GetAsFrozen();
            }
            SetDirty();
        }

        protected override void CloneCurrentValueCore(Freezable source)
        {
            base.CloneCurrentValueCore(source);
            StreamGeometry sourceStream = (StreamGeometry)source;
            if (sourceStream._figures != null)
            {
                _figures = (PathFigureCollection)sourceStream._figures.GetAsFrozen();
            }
            SetDirty();
        }

        protected override void GetAsFrozenCore(Freezable source)
        {
            base.GetAsFrozenCore(source);
            StreamGeometry sourceStream = (StreamGeometry)source;
            if (sourceStream._figures != null)
            {
                _figures = (PathFigureCollection)sourceStream._figures.GetAsFrozen();
            }
            SetDirty();
        }

        protected override void GetCurrentValueAsFrozenCore(Freezable source)
        {
            base.GetCurrentValueAsFrozenCore(source);
            StreamGeometry sourceStream = (StreamGeometry)source;
            if (sourceStream._figures != null)
            {
                _figures = (PathFigureCollection)sourceStream._figures.GetAsFrozen();
            }
            SetDirty();
        }

        #endregion

        #region Data

        private PathFigureCollection _figures;
        private bool _boundsValid;
        private Rect _bounds = Rect.Empty;

        #endregion
    }
    #endregion

    #region StreamGeometryCallbackContext
    internal sealed class StreamGeometryCallbackContext : PathStreamGeometryContext
    {
        private readonly StreamGeometry _owner;
        private bool _closed;

        internal StreamGeometryCallbackContext(StreamGeometry owner)
            : base(owner.FillRule, owner.Transform)
        {
            _owner = owner;
        }

        public override void Close()
        {
            VerifyAccess();
            DisposeCore();
            GC.SuppressFinalize(this);
        }

        internal override void DisposeCore()
        {
            if (!_closed)
            {
                _closed = true;
                PathGeometry path = GetPathGeometry();
                _owner.Close(path.Figures);
            }
        }
    }
    #endregion
}

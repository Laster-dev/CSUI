// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System.ComponentModel;
using System.Diagnostics;
using System.Windows.Media;
using MS.Internal;
using MS.Internal.PresentationFramework;

namespace System.Windows.Shapes
{
    /// <summary>
    /// Shape is a base class for shape elements
    /// </summary>
    [Localizability(LocalizationCategory.None, Readability = Readability.Unreadable)]
    public abstract class Shape : FrameworkElement
    {
        #region Constructors

        /// <summary>
        /// Shape Constructor
        /// </summary>
        protected Shape()
        {
        }

        #endregion

        #region Properties

        /// <summary>
        /// DependencyProperty for the Stretch property.
        /// </summary>
        public static readonly DependencyProperty StretchProperty
            = DependencyProperty.Register(
                "Stretch",
                typeof(Stretch),
                typeof(Shape),
                new FrameworkPropertyMetadata(Stretch.None, FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsArrange));

        /// <summary>
        /// The Stretch property determines how the shape may be stretched to accommodate shape size
        /// </summary>
        public Stretch Stretch
        {
            get { return (Stretch)GetValue(StretchProperty); }
            set { SetValue(StretchProperty, value); }
        }

        /// <summary>
        /// The RenderedGeometry property returns the final rendered geometry
        /// </summary>
        public virtual Geometry RenderedGeometry
        {
            get
            {
                EnsureRenderedGeometry();

                Geometry geometry = _renderedGeometry.CloneCurrentValue();
                if (geometry == null || geometry == Geometry.Empty)
                {
                    return Geometry.Empty;
                }

                // We need to return a frozen copy
                if (object.ReferenceEquals(geometry, _renderedGeometry))
                {
                    // geometry is a reference to _renderedGeometry, so we need to copy
                    geometry = geometry.Clone();
                    geometry.Freeze();
                }

                return geometry;
            }
        }

        /// <summary>
        /// Return the transformation applied to the geometry before rendering
        /// </summary>
        public virtual Transform GeometryTransform
        {
            get
            {
                BoxedMatrix stretchMatrix = StretchMatrixField.GetValue(this);

                if (stretchMatrix == null)
                {
                    return Transform.Identity;
                }
                else
                {
                    return new MatrixTransform(stretchMatrix.Value);
                }
            }
        }

        private static void OnPenChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            // Called when any of the Stroke properties is invalidated.
            // That means that the cached pen should be recalculated.
            ((Shape)d)._pen = null;
        }

        /// <summary>
        /// Fill property
        /// </summary>
        [CommonDependencyProperty]
        public static readonly DependencyProperty FillProperty =
                DependencyProperty.Register(
                        "Fill",
                        typeof(Brush),
                        typeof(Shape),
                        new FrameworkPropertyMetadata(
                                (Brush)null,
                                FrameworkPropertyMetadataOptions.AffectsRender |
                                FrameworkPropertyMetadataOptions.SubPropertiesDoNotAffectRender));

        /// <summary>
        /// Fill property
        /// </summary>
        public Brush Fill
        {
            get { return (Brush)GetValue(FillProperty); }
            set { SetValue(FillProperty, value); }
        }

        /// <summary>
        /// Stroke property
        /// </summary>
        [CommonDependencyProperty]
        public static readonly DependencyProperty StrokeProperty =
                DependencyProperty.Register(
                        "Stroke",
                        typeof(Brush),
                        typeof(Shape),
                        new FrameworkPropertyMetadata(
                                (Brush)null,
                                FrameworkPropertyMetadataOptions.AffectsMeasure |
                                FrameworkPropertyMetadataOptions.AffectsRender |
                                FrameworkPropertyMetadataOptions.SubPropertiesDoNotAffectRender,
                                new PropertyChangedCallback(OnPenChanged)));

        /// <summary>
        /// Stroke property
        /// </summary>
        public Brush Stroke
        {
            get { return (Brush)GetValue(StrokeProperty); }
            set { SetValue(StrokeProperty, value); }
        }

        /// <summary>
        /// StrokeThickness property
        /// </summary>
        [CommonDependencyProperty]
        public static readonly DependencyProperty StrokeThicknessProperty =
                DependencyProperty.Register(
                        "StrokeThickness",
                        typeof(double),
                        typeof(Shape),
                        new FrameworkPropertyMetadata(
                                1.0d,
                                FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender,
                                new PropertyChangedCallback(OnPenChanged)));

        /// <summary>
        /// StrokeThickness property
        /// </summary>
        [TypeConverter(typeof(LengthConverter))]
        public double StrokeThickness
        {
            get { return (double)GetValue(StrokeThicknessProperty); }
            set { SetValue(StrokeThicknessProperty, value); }
        }

        /// <summary>
        /// StrokeStartLineCap property
        /// </summary>
        public static readonly DependencyProperty StrokeStartLineCapProperty =
                DependencyProperty.Register(
                        "StrokeStartLineCap",
                        typeof(PenLineCap),
                        typeof(Shape),
                        new FrameworkPropertyMetadata(
                                PenLineCap.Flat,
                                FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender,
                                new PropertyChangedCallback(OnPenChanged)),
                        new ValidateValueCallback(System.Windows.Media.ValidateEnums.IsPenLineCapValid));

        /// <summary>
        /// StrokeStartLineCap property
        /// </summary>
        public PenLineCap StrokeStartLineCap
        {
            get { return (PenLineCap)GetValue(StrokeStartLineCapProperty); }
            set { SetValue(StrokeStartLineCapProperty, value); }
        }

        /// <summary>
        /// StrokeEndLineCap property
        /// </summary>
        public static readonly DependencyProperty StrokeEndLineCapProperty =
                DependencyProperty.Register(
                        "StrokeEndLineCap",
                        typeof(PenLineCap),
                        typeof(Shape),
                        new FrameworkPropertyMetadata(
                                PenLineCap.Flat,
                                FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender,
                                new PropertyChangedCallback(OnPenChanged)),
                        new ValidateValueCallback(System.Windows.Media.ValidateEnums.IsPenLineCapValid));

        /// <summary>
        /// StrokeEndLineCap property
        /// </summary>
        public PenLineCap StrokeEndLineCap
        {
            get { return (PenLineCap)GetValue(StrokeEndLineCapProperty); }
            set { SetValue(StrokeEndLineCapProperty, value); }
        }

        /// <summary>
        /// StrokeDashCap property
        /// </summary>
        public static readonly DependencyProperty StrokeDashCapProperty =
                DependencyProperty.Register(
                        "StrokeDashCap",
                        typeof(PenLineCap),
                        typeof(Shape),
                        new FrameworkPropertyMetadata(
                                PenLineCap.Flat,
                                FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender,
                                new PropertyChangedCallback(OnPenChanged)),
                        new ValidateValueCallback(System.Windows.Media.ValidateEnums.IsPenLineCapValid));

        /// <summary>
        /// StrokeDashCap property
        /// </summary>
        public PenLineCap StrokeDashCap
        {
            get { return (PenLineCap)GetValue(StrokeDashCapProperty); }
            set { SetValue(StrokeDashCapProperty, value); }
        }

        /// <summary>
        /// StrokeLineJoin property
        /// </summary>
        public static readonly DependencyProperty StrokeLineJoinProperty =
                DependencyProperty.Register(
                        "StrokeLineJoin",
                        typeof(PenLineJoin),
                        typeof(Shape),
                        new FrameworkPropertyMetadata(
                                PenLineJoin.Miter,
                                FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender,
                                new PropertyChangedCallback(OnPenChanged)),
                        new ValidateValueCallback(System.Windows.Media.ValidateEnums.IsPenLineJoinValid));

        /// <summary>
        /// StrokeLineJoin property
        /// </summary>
        public PenLineJoin StrokeLineJoin
        {
            get { return (PenLineJoin)GetValue(StrokeLineJoinProperty); }
            set { SetValue(StrokeLineJoinProperty, value); }
        }

        /// <summary>
        /// StrokeMiterLimit property
        /// </summary>
        public static readonly DependencyProperty StrokeMiterLimitProperty =
                DependencyProperty.Register(
                        "StrokeMiterLimit",
                        typeof(double),
                        typeof(Shape),
                        new FrameworkPropertyMetadata(
                                10.0,
                                FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender,
                                new PropertyChangedCallback(OnPenChanged)));

        /// <summary>
        /// StrokeMiterLimit property
        /// </summary>
        public double StrokeMiterLimit
        {
            get { return (double)GetValue(StrokeMiterLimitProperty); }
            set { SetValue(StrokeMiterLimitProperty, value); }
        }

        /// <summary>
        /// StrokeDashOffset property
        /// </summary>
        public static readonly DependencyProperty StrokeDashOffsetProperty =
                DependencyProperty.Register(
                        "StrokeDashOffset",
                        typeof(double),
                        typeof(Shape),
                        new FrameworkPropertyMetadata(
                                0.0,
                                FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender,
                                new PropertyChangedCallback(OnPenChanged)));

        /// <summary>
        /// StrokeDashOffset property
        /// </summary>
        public double StrokeDashOffset
        {
            get { return (double)GetValue(StrokeDashOffsetProperty); }
            set { SetValue(StrokeDashOffsetProperty, value); }
        }

        /// <summary>
        /// StrokeDashArray property
        /// </summary>
        public static readonly DependencyProperty StrokeDashArrayProperty =
                DependencyProperty.Register(
                        "StrokeDashArray",
                        typeof(DoubleCollection),
                        typeof(Shape),
                        new FrameworkPropertyMetadata(
                                new FreezableDefaultValueFactory(DoubleCollection.Empty),
                                FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender,
                                new PropertyChangedCallback(OnPenChanged)));

        /// <summary>
        /// StrokeDashArray property
        /// </summary>
        public DoubleCollection StrokeDashArray
        {
            get { return (DoubleCollection)GetValue(StrokeDashArrayProperty); }
            set { SetValue(StrokeDashArrayProperty, value); }
        }

        #endregion

        #region Protected Methods

        /// <summary>
        /// Updates DesiredSize of the shape. Called by parent UIElement during is the first pass of layout.
        /// </summary>
        protected override Size MeasureOverride(Size constraint)
        {
            CacheDefiningGeometry();

            Size newSize;

            Stretch mode = Stretch;

            if (mode == Stretch.None)
            {
                newSize = GetNaturalSize();
            }
            else
            {
                newSize = GetStretchedRenderSize(mode, GetStrokeThickness(), constraint, GetDefiningGeometryBounds());
            }

            if (SizeIsInvalidOrEmpty(newSize))
            {
                // We've encountered a numerical error. Don't draw anything.
                newSize = new Size(0, 0);
                _renderedGeometry = Geometry.Empty;
            }

            return newSize;
        }

        /// <summary>
        /// Compute the rendered geometry and the stretching transform.
        /// </summary>
        protected override Size ArrangeOverride(Size finalSize)
        {
            Size newSize;

            Stretch mode = Stretch;

            if (mode == Stretch.None)
            {
                StretchMatrixField.ClearValue(this);

                ResetRenderedGeometry();

                newSize = finalSize;
            }
            else
            {
                newSize = GetStretchedRenderSizeAndSetStretchMatrix(
                    mode, GetStrokeThickness(), finalSize, GetDefiningGeometryBounds());
            }

            if (SizeIsInvalidOrEmpty(newSize))
            {
                // We've encountered a numerical error. Don't draw anything.
                newSize = new Size(0, 0);
                _renderedGeometry = Geometry.Empty;
            }

            return newSize;
        }

        /// <summary>
        /// Render callback.
        /// </summary>
        protected override void OnRender(DrawingContext drawingContext)
        {
            EnsureRenderedGeometry();

            if (_renderedGeometry != Geometry.Empty)
            {
                drawingContext.DrawGeometry(Fill, GetPen(), _renderedGeometry);
            }
        }

        #endregion

        #region Protected Properties

        /// <summary>
        /// Get the geometry that defines this shape
        /// </summary>
        protected abstract Geometry DefiningGeometry
        {
            get;
        }

        #endregion Protected Properties

        #region Internal Methods

        internal bool SizeIsInvalidOrEmpty(Size size)
        {
            return (double.IsNaN(size.Width) ||
                    double.IsNaN(size.Height) ||
                    size.IsEmpty);
        }

        internal bool IsPenNoOp
        {
            get
            {
                double strokeThickness = StrokeThickness;
                return (Stroke == null) || double.IsNaN(strokeThickness) || DoubleUtil.IsZero(strokeThickness);
            }
        }

        internal double GetStrokeThickness()
        {
            if (IsPenNoOp)
            {
                return 0;
            }
            else
            {
                return Math.Abs(StrokeThickness);
            }
        }

        internal Pen GetPen()
        {
            if (IsPenNoOp)
            {
                return null;
            }

            if (_pen == null)
            {
                double strokeThickness = StrokeThickness;
                double thickness = Math.Abs(strokeThickness);

                // This pen is internal to the system and
                // must not participate in freezable treeness
                _pen = new Pen
                {
                    CanBeInheritanceContext = false,
                    Thickness = thickness,
                    Brush = Stroke,
                    StartLineCap = StrokeStartLineCap,
                    EndLineCap = StrokeEndLineCap,
                    DashCap = StrokeDashCap,
                    LineJoin = StrokeLineJoin,
                    MiterLimit = StrokeMiterLimit
                };

                DoubleCollection strokeDashArray = null;
                bool hasModifiers;
                if (GetValueSource(StrokeDashArrayProperty, null, out hasModifiers)
                    != BaseValueSourceInternal.Default || hasModifiers)
                {
                    strokeDashArray = StrokeDashArray;
                }

                // Avoid creating the DashStyle if we can
                double strokeDashOffset = StrokeDashOffset;
                if (strokeDashArray != null || strokeDashOffset != 0.0)
                {
                    _pen.DashStyle = new DashStyle(strokeDashArray, strokeDashOffset);
                }
            }

            return _pen;
        }

        internal static bool IsDoubleFiniteNonNegative(object o)
        {
            double d = (double)o;
            return !(double.IsInfinity(d) || double.IsNaN(d) || d < 0.0);
        }

        internal static bool IsDoubleFinite(object o)
        {
            double d = (double)o;
            return !(double.IsInfinity(d) || double.IsNaN(d));
        }

        internal static bool IsDoubleFiniteOrNaN(object o)
        {
            double d = (double)o;
            return !double.IsInfinity(d);
        }

        internal virtual void CacheDefiningGeometry() { }

        internal Size GetStretchedRenderSize(Stretch mode, double strokeThickness, Size availableSize, Rect geometryBounds)
        {
            double xScale, yScale, dX, dY;
            Size renderSize;

            GetStretchMetrics(mode, strokeThickness, availableSize, geometryBounds,
                out xScale, out yScale, out dX, out dY, out renderSize);

            return renderSize;
        }

        internal Size GetStretchedRenderSizeAndSetStretchMatrix(Stretch mode, double strokeThickness, Size availableSize, Rect geometryBounds)
        {
            double xScale, yScale, dX, dY;
            Size renderSize;

            GetStretchMetrics(mode, strokeThickness, availableSize, geometryBounds,
                out xScale, out yScale, out dX, out dY, out renderSize);

            Matrix stretchMatrix = Matrix.Identity;
            stretchMatrix.ScaleAt(xScale, yScale, geometryBounds.Location.X, geometryBounds.Location.Y);
            stretchMatrix.Translate(dX, dY);
            StretchMatrixField.SetValue(this, new BoxedMatrix(stretchMatrix));

            ResetRenderedGeometry();

            return renderSize;
        }

        internal void ResetRenderedGeometry()
        {
            _renderedGeometry = null;
        }

        internal void GetStretchMetrics(Stretch mode, double strokeThickness, Size availableSize, Rect geometryBounds,
                                             out double xScale, out double yScale, out double dX, out double dY, out Size stretchedSize)
        {
            if (!geometryBounds.IsEmpty)
            {
                double margin = strokeThickness / 2;
                bool hasThinDimension = false;

                xScale = Math.Max(availableSize.Width - strokeThickness, 0);
                yScale = Math.Max(availableSize.Height - strokeThickness, 0);
                dX = margin - geometryBounds.Left;
                dY = margin - geometryBounds.Top;

                if (geometryBounds.Width > xScale * double.Epsilon)
                {
                    xScale /= geometryBounds.Width;
                }
                else
                {
                    xScale = 1;
                    if (geometryBounds.Width == 0)
                    {
                        hasThinDimension = true;
                    }
                }

                if (geometryBounds.Height > yScale * double.Epsilon)
                {
                    yScale /= geometryBounds.Height;
                }
                else
                {
                    yScale = 1;
                    if (geometryBounds.Height == 0)
                    {
                        hasThinDimension = true;
                    }
                }

                Debug.Assert(mode != Stretch.None);

                if (mode != Stretch.Fill && !hasThinDimension)
                {
                    if (mode == Stretch.Uniform)
                    {
                        if (yScale > xScale)
                        {
                            yScale = xScale;
                        }
                        else
                        {
                            xScale = yScale;
                        }
                    }
                    else
                    {
                        Debug.Assert(mode == Stretch.UniformToFill);

                        if (xScale > yScale)
                        {
                            yScale = xScale;
                        }
                        else
                        {
                            xScale = yScale;
                        }
                    }
                }

                stretchedSize = new Size(geometryBounds.Width * xScale + strokeThickness, geometryBounds.Height * yScale + strokeThickness);
            }
            else
            {
                xScale = yScale = 1;
                dX = dY = 0;
                stretchedSize = new Size(0, 0);
            }
        }

        internal virtual Size GetNaturalSize()
        {
            Geometry geometry = DefiningGeometry;

            Debug.Assert(geometry != null);

            Pen pen = GetPen();
            DashStyle style = null;

            if (pen != null)
            {
                style = pen.DashStyle;

                if (style != null)
                {
                    pen.DashStyle = null;
                }
            }

            Rect bounds = geometry.GetRenderBounds(pen);

            if (style != null)
            {
                pen.DashStyle = style;
            }

            return new Size(Math.Max(bounds.Right, 0),
                Math.Max(bounds.Bottom, 0));
        }

        internal virtual Rect GetDefiningGeometryBounds()
        {
            Geometry geometry = DefiningGeometry;

            Debug.Assert(geometry != null);

            return geometry.Bounds;
        }

        internal void EnsureRenderedGeometry()
        {
            if (_renderedGeometry == null)
            {
                _renderedGeometry = DefiningGeometry;

                Debug.Assert(_renderedGeometry != null);

                if (Stretch != Stretch.None)
                {
                    Geometry currentValue = _renderedGeometry.CloneCurrentValue();
                    if (object.ReferenceEquals(_renderedGeometry, currentValue))
                    {
                        _renderedGeometry = currentValue.Clone();
                    }
                    else
                    {
                        _renderedGeometry = currentValue;
                    }

                    Transform renderedTransform = _renderedGeometry.Transform;

                    BoxedMatrix boxedStretchMatrix = StretchMatrixField.GetValue(this);
                    Matrix stretchMatrix = (boxedStretchMatrix == null) ? Matrix.Identity : boxedStretchMatrix.Value;
                    if (renderedTransform == null || renderedTransform.IsIdentity)
                    {
                        _renderedGeometry.Transform = new MatrixTransform(stretchMatrix);
                    }
                    else
                    {
                        _renderedGeometry.Transform = new MatrixTransform(renderedTransform.Value * stretchMatrix);
                    }
                }
            }
        }

        #endregion Internal Methods

        #region Private Fields

        private Pen _pen = null;

        private Geometry _renderedGeometry = Geometry.Empty;

        private static readonly UncommonField<BoxedMatrix> StretchMatrixField = new UncommonField<BoxedMatrix>(null);

        #endregion Private Fields
    }

    internal class BoxedMatrix
    {
        public BoxedMatrix(Matrix value)
        {
            Value = value;
        }

        public Matrix Value;
    }
}

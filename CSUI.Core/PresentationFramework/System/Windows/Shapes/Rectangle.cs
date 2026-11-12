// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System.ComponentModel;
using System.Windows.Media;

namespace System.Windows.Shapes
{
    /// <summary>
    /// The rectangle shape element
    /// </summary>
    public sealed class Rectangle : Shape
    {
        #region Constructors

        public Rectangle()
        {
        }

        static Rectangle()
        {
            StretchProperty.OverrideMetadata(typeof(Rectangle), new FrameworkPropertyMetadata(Stretch.Fill));
        }

        #endregion Constructors

        #region Dynamic Properties

        public static readonly DependencyProperty RadiusXProperty =
            DependencyProperty.Register("RadiusX", typeof(double), typeof(Rectangle),
                new FrameworkPropertyMetadata(0d, FrameworkPropertyMetadataOptions.AffectsRender));

        [TypeConverter(typeof(LengthConverter))]
        public double RadiusX
        {
            get { return (double)GetValue(RadiusXProperty); }
            set { SetValue(RadiusXProperty, value); }
        }

        public static readonly DependencyProperty RadiusYProperty =
            DependencyProperty.Register("RadiusY", typeof(double), typeof(Rectangle),
                new FrameworkPropertyMetadata(0d, FrameworkPropertyMetadataOptions.AffectsRender));

        [TypeConverter(typeof(LengthConverter))]
        public double RadiusY
        {
            get { return (double)GetValue(RadiusYProperty); }
            set { SetValue(RadiusYProperty, value); }
        }

        public override Geometry RenderedGeometry
        {
            get
            {
                return new RectangleGeometry(_rect, RadiusX, RadiusY);
            }
        }

        public override Transform GeometryTransform
        {
            get
            {
                return Transform.Identity;
            }
        }

        #endregion Dynamic Properties

        #region Protected

        protected override Size MeasureOverride(Size constraint)
        {
            if (Stretch == Stretch.UniformToFill)
            {
                double width = constraint.Width;
                double height = constraint.Height;

                if (double.IsInfinity(width) && double.IsInfinity(height))
                {
                    return GetNaturalSize();
                }
                else if (double.IsInfinity(width) || double.IsInfinity(height))
                {
                    width = Math.Min(width, height);
                }
                else
                {
                    width = Math.Max(width, height);
                }

                return new Size(width, width);
            }

            return GetNaturalSize();
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            double penThickness = GetStrokeThickness();
            double margin = penThickness / 2;

            _rect = new Rect(
                margin,
                margin,
                Math.Max(0, finalSize.Width - penThickness),
                Math.Max(0, finalSize.Height - penThickness));

            switch (Stretch)
            {
                case Stretch.None:
                    _rect.Width = _rect.Height = 0;
                    break;

                case Stretch.Fill:
                    break;

                case Stretch.Uniform:
                    if (_rect.Width > _rect.Height)
                    {
                        _rect.Width = _rect.Height;
                    }
                    else
                    {
                        _rect.Height = _rect.Width;
                    }
                    break;

                case Stretch.UniformToFill:
                    if (_rect.Width < _rect.Height)
                    {
                        _rect.Width = _rect.Height;
                    }
                    else
                    {
                        _rect.Height = _rect.Width;
                    }
                    break;
            }

            ResetRenderedGeometry();

            return finalSize;
        }

        protected override Geometry DefiningGeometry
        {
            get
            {
                return new RectangleGeometry(_rect, RadiusX, RadiusY);
            }
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            Pen pen = GetPen();
            drawingContext.DrawRoundedRectangle(Fill, pen, _rect, RadiusX, RadiusY);
        }

        #endregion Protected

        #region Internal Methods

        internal override void CacheDefiningGeometry()
        {
            double margin = GetStrokeThickness() / 2;

            _rect = new Rect(margin, margin, 0, 0);
        }

        internal override Size GetNaturalSize()
        {
            double strokeThickness = GetStrokeThickness();
            return new Size(strokeThickness, strokeThickness);
        }

        internal override Rect GetDefiningGeometryBounds()
        {
            return _rect;
        }

        internal override int EffectiveValuesInitialSize
        {
            get { return 19; }
        }

        #endregion Internal Methods

        #region Private Fields

        private Rect _rect = Rect.Empty;

        #endregion Private Fields
    }
}

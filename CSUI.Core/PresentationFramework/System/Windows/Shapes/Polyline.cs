// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System.Windows.Media;
using MS.Internal;

namespace System.Windows.Shapes
{
    /// <summary>
    /// The Polyline shape element
    /// </summary>
    public sealed class Polyline : Shape
    {
        #region Constructors

        public Polyline()
        {
        }

        #endregion Constructors

        #region Dynamic Properties

        public static readonly DependencyProperty PointsProperty = DependencyProperty.Register(
                "Points", typeof(PointCollection), typeof(Polyline),
                new FrameworkPropertyMetadata(new FreezableDefaultValueFactory(PointCollection.Empty), FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender));

        public PointCollection Points
        {
            get { return (PointCollection)GetValue(PointsProperty); }
            set { SetValue(PointsProperty, value); }
        }

        public static readonly DependencyProperty FillRuleProperty = DependencyProperty.Register(
            "FillRule",
            typeof(FillRule),
            typeof(Polyline),
            new FrameworkPropertyMetadata(
                FillRule.EvenOdd,
                FrameworkPropertyMetadataOptions.AffectsRender),
            new ValidateValueCallback(System.Windows.Media.ValidateEnums.IsFillRuleValid));

        public FillRule FillRule
        {
            get { return (FillRule)GetValue(FillRuleProperty); }
            set { SetValue(FillRuleProperty, value); }
        }

        #endregion Dynamic Properties

        #region Protected Methods and Properties

        protected override Geometry DefiningGeometry
        {
            get
            {
                return _polylineGeometry;
            }
        }

        #endregion

        #region Internal methods

        internal override void CacheDefiningGeometry()
        {
            PointCollection pointCollection = Points;
            PathFigure pathFigure = new PathFigure();

            if (pointCollection == null)
            {
                _polylineGeometry = Geometry.Empty;
                return;
            }

            if (pointCollection.Count > 0)
            {
                pathFigure.StartPoint = pointCollection[0];

                if (pointCollection.Count > 1)
                {
                    Point[] array = new Point[pointCollection.Count - 1];

                    for (int i = 1; i < pointCollection.Count; i++)
                    {
                        array[i - 1] = pointCollection[i];
                    }

                    pathFigure.Segments.Add(new PolyLineSegment(array, true));
                }
            }

            PathGeometry polylineGeometry = new PathGeometry();
            polylineGeometry.Figures.Add(pathFigure);

            polylineGeometry.FillRule = FillRule;

            if (polylineGeometry.Bounds == Rect.Empty)
            {
                _polylineGeometry = Geometry.Empty;
            }
            else
            {
                _polylineGeometry = polylineGeometry;
            }
        }

        #endregion Internal methods

        #region Private Methods and Members

        private Geometry _polylineGeometry;

        #endregion
    }
}

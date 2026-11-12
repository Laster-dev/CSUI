// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System.Windows.Media;
using MS.Internal;

namespace System.Windows.Shapes
{
    /// <summary>
    /// The polygon shape element
    /// </summary>
    public sealed class Polygon : Shape
    {
        #region Constructors

        public Polygon()
        {
        }

        #endregion Constructors

        #region Dynamic Properties

        public static readonly DependencyProperty PointsProperty = DependencyProperty.Register(
                "Points", typeof(PointCollection), typeof(Polygon),
                new FrameworkPropertyMetadata(new FreezableDefaultValueFactory(PointCollection.Empty), FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender));

        public PointCollection Points
        {
            get { return (PointCollection)GetValue(PointsProperty); }
            set { SetValue(PointsProperty, value); }
        }

        public static readonly DependencyProperty FillRuleProperty = DependencyProperty.Register(
            "FillRule",
            typeof(FillRule),
            typeof(Polygon),
            new FrameworkPropertyMetadata(
                FillRule.EvenOdd,
                FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender),
            new ValidateValueCallback(System.Windows.Media.ValidateEnums.IsFillRuleValid));

        public FillRule FillRule
        {
            get { return (FillRule)GetValue(FillRuleProperty); }
            set { SetValue(FillRuleProperty, value); }
        }

        #endregion Dynamic Properties

        #region Protected Methods and properties

        protected override Geometry DefiningGeometry
        {
            get
            {
                return _polygonGeometry;
            }
        }

        #endregion

        #region Internal Methods

        internal override void CacheDefiningGeometry()
        {
            PointCollection pointCollection = Points;
            PathFigure pathFigure = new PathFigure();

            if (pointCollection == null)
            {
                _polygonGeometry = Geometry.Empty;
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

                pathFigure.IsClosed = true;
            }

            PathGeometry polygonGeometry = new PathGeometry();
            polygonGeometry.Figures.Add(pathFigure);

            polygonGeometry.FillRule = FillRule;

            _polygonGeometry = polygonGeometry;
        }

        #endregion Internal Methods

        #region Private Methods and Members

        private Geometry _polygonGeometry;

        #endregion
    }
}

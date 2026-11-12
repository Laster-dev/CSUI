// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System.Windows.Media;
using MS.Internal.PresentationFramework;

namespace System.Windows.Shapes
{
    /// <summary>
    /// The Path shape element
    /// </summary>
    public sealed class Path : Shape
    {
        #region Constructors

        public Path()
        {
        }

        #endregion Constructors

        #region Dynamic Properties

        [CommonDependencyProperty]
        public static readonly DependencyProperty DataProperty = DependencyProperty.Register(
            "Data",
            typeof(Geometry),
            typeof(Path),
            new FrameworkPropertyMetadata(
                null,
                FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender),
            null);

        public Geometry Data
        {
            get { return (Geometry)GetValue(DataProperty); }
            set { SetValue(DataProperty, value); }
        }

        #endregion

        #region Protected Methods and Properties

        protected override Geometry DefiningGeometry
        {
            get
            {
                Geometry data = Data;

                if (data == null)
                {
                    data = Geometry.Empty;
                }

                return data;
            }
        }

        internal override int EffectiveValuesInitialSize
        {
            get { return 13; }
        }

        #endregion
    }
}

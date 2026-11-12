// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//
//
// Description: This file contains the implementation of RadialGradientBrush.
//              The RadialGradientBrush is a GradientBrush which defines its
//              Gradient as a radial interpolation within an Ellipse.
//
//



#nullable disable
using SR = MS.Internal.PresentationCore.SR;

namespace System.Windows.Media
{
    /// <summary>
    /// RadialGradientBrush - This GradientBrush defines its Gradient as an interpolation
    /// within an Ellipse.
    /// </summary>
    public sealed partial class RadialGradientBrush : GradientBrush
    {
        #region Constructors

        /// <summary>
        /// Default constructor for RadialGradientBrush.  The resulting brush has no content.
        /// </summary>
        public RadialGradientBrush() : base()
        {
        }

        /// <summary>
        /// RadialGradientBrush Constructor
        /// Constructs a RadialGradientBrush with two colors specified for GradientStops at
        /// offsets 0.0 and 1.0.
        /// </summary>
        /// <param name="startColor"> The Color at offset 0.0. </param>
        /// <param name="endColor"> The Color at offset 1.0. </param>        
        public RadialGradientBrush(Color startColor,
                                   Color endColor) : base()
        {
            GradientStops.Add(new GradientStop(startColor, 0.0));
            GradientStops.Add(new GradientStop(endColor, 1.0));
        }

        /// <summary>
        /// RadialGradientBrush Constructor
        /// Constructs a RadialGradientBrush with GradientStops set to the passed-in 
        /// collection.
        /// </summary>
        /// <param name="gradientStopCollection"> GradientStopCollection to set on this brush. </param>
        public RadialGradientBrush(GradientStopCollection gradientStopCollection) 
                                                   : base(gradientStopCollection)
        {            
        }
            

        #endregion Constructors

    }
}


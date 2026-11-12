// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//
//
// Description: This file contains the implementation of LinearGradientBrush.
//              The LinearGradientBrush is a GradientBrush which defines its
//              Gradient as a linear interpolation between two parallel lines.
//
//



#nullable disable
using SR = MS.Internal.PresentationCore.SR;

namespace System.Windows.Media
{
    /// <summary>
    /// LinearGradientBrush - This GradientBrush defines its Gradient as an interpolation
    /// between two parallel lines.
    /// </summary>
    public sealed partial class LinearGradientBrush : GradientBrush
    {
        #region Constructors

        /// <summary>
        /// Default constructor for LinearGradientBrush.  The resulting brush has no content.
        /// </summary>
        public LinearGradientBrush() : base()
        {
        }

        /// <summary>
        /// LinearGradientBrush Constructor
        /// Constructs a LinearGradientBrush with GradientStops specified at offset 0.0 and 
        /// 1.0. The StartPoint is set to (0,0) and the EndPoint is derived from the angle 
        /// such that 1) the line containing the StartPoint and EndPoint is 'angle' degrees 
        /// from the horizontal in the direction of positive Y, and 2) the EndPoint lies on 
        ///  the perimeter of the unit circle.
        /// </summary>
        /// <param name="startColor"> The Color at offset 0.0. </param>
        /// <param name="endColor"> The Color at offset 1.0. </param>
        /// <param name="angle"> The angle, in degrees, that the gradient will be away from horizontal. </param>
        public LinearGradientBrush(Color startColor,
                                   Color endColor,
                                   double angle) : base()
        {
            EndPoint = EndPointFromAngle(angle);

            GradientStops.Add(new GradientStop(startColor, 0.0));
            GradientStops.Add(new GradientStop(endColor, 1.0));
        }

        /// <summary>
        /// LinearGradientBrush Constructor
        /// Constructs a LinearGradientBrush with two colors at the specified start and end points.
        /// </summary>
        /// <param name="startColor"> The Color at offset 0.0. </param>
        /// <param name="endColor"> The Color at offset 1.0. </param>
        /// <param name="startPoint"> The start point</param>
        /// <param name="endPoint"> The end point</param>
        public LinearGradientBrush(Color startColor,
                                   Color endColor,
                                   Point startPoint,
                                   Point endPoint) : base()
        {
            StartPoint = startPoint;
            EndPoint = endPoint;

            GradientStops.Add(new GradientStop(startColor, 0.0));
            GradientStops.Add(new GradientStop(endColor, 1.0));
        }

        /// <summary>
        /// LinearGradientBrush Constructor
        /// Constructs a LinearGradientBrush with GradientStops set to the passed-in collection.
        /// </summary>
        /// <param name="gradientStopCollection"> GradientStopCollection to set on this brush. </param>
        public LinearGradientBrush(GradientStopCollection gradientStopCollection)
                                                : base (gradientStopCollection)                                                
        {
        }        

        /// <summary>
        /// LinearGradientBrush Constructor
        /// Constructs a LinearGradientBrush with GradientStops set to the passed-in collection.
        /// Constructs a LinearGradientBrush with GradientStops specified at offset 0.0 and 
        /// 1.0. The StartPoint is set to (0,0) and the EndPoint is derived from the angle 
        /// such that 1) the line containing the StartPoint and EndPoint is 'angle' degrees 
        /// from the horizontal in the direction of positive Y, and 2) the EndPoint lies on 
        ///  the perimeter of the unit circle.
        /// </summary>
        /// <param name="gradientStopCollection"> GradientStopCollection to set on this brush. </param>
        /// <param name="angle"> The angle, in degrees, that the gradient will be away from horizontal. </param>        
        public LinearGradientBrush(GradientStopCollection gradientStopCollection,
                                   double angle) : base (gradientStopCollection)
        {
            EndPoint = EndPointFromAngle(angle);
        }        

        /// <summary>
        /// LinearGradientBrush Constructor
        /// Constructs a LinearGradientBrush with GradientStops set to the passed-in collection.
        /// The StartPoint and EndPoint are set to the specified startPoint and endPoint.
        /// </summary>
        /// <param name="gradientStopCollection"> GradientStopCollection to set on this brush. </param>
        /// <param name="startPoint"> The start point</param>
        /// <param name="endPoint"> The end point</param>
        public LinearGradientBrush(GradientStopCollection gradientStopCollection,
                                   Point startPoint,
                                   Point endPoint) : base (gradientStopCollection)
        {
            StartPoint = startPoint;
            EndPoint = endPoint;
        }
        

        #endregion Constructors


        private Point EndPointFromAngle(double angle)
        {
            // Convert the angle from degrees to radians
            angle = angle * (1.0/180.0) * System.Math.PI;

            return (new Point(System.Math.Cos(angle), System.Math.Sin(angle)));
}
    }
}


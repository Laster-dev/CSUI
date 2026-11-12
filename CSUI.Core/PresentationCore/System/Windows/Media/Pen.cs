// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//
//
//
// Description: This file contains the implementation of Pen.
//              Pen is is the class which describes how to stroke a geometric
//              area.
//
//

using MS.Internal;
using System.Windows.Media.Animation;
using CSUI.Core.Interop;
using CSUI.Core.Interop.DirectX;

#nullable disable
using SR = MS.Internal.PresentationCore.SR;

namespace System.Windows.Media
{
    /// <summary>
    /// Pen - The pen class is used to describe how a shape is stroked.
    /// </summary>
    public sealed partial class Pen : Animatable
    {
        #region Constructors

        /// <summary>
        ///
        /// </summary>
        public Pen()
        {
        }

        /// <summary>
        /// Pen - Initializes the pen from the given Brush and thickness.
        /// All other values as set to default.
        /// </summary>
        /// <param name="brush"> The Brush for this Pen. </param>
        /// <param name="thickness"> The thickness of the Pen. </param>
        public Pen(Brush brush,
                   double thickness)
        {
            Brush = brush;
            Thickness = thickness;
        }

        /// <summary>
        /// Pen - Initializes the brush from the parameters.
        /// </summary>
        /// <param name="brush"> The Pen's Brush. </param>
        /// <param name="thickness"> The Pen's thickness. </param>
        /// <param name="startLineCap"> The PenLineCap which applies to the start of the stroke. </param>
        /// <param name="endLineCap"> The PenLineCap which applies to the end of the stroke. </param>
        /// <param name="dashCap"> The PenDashCap which applies to the ends of each dash. </param>
        /// <param name="lineJoin"> The PenLineJoin. </param>
        /// <param name="miterLimit"> The miter limit. </param>
        /// <param name="dashStyle"> The dash style. </param>
        internal Pen(
            Brush brush,
            double thickness,
            PenLineCap startLineCap,
            PenLineCap endLineCap,
            PenLineCap dashCap,
            PenLineJoin lineJoin,
            double miterLimit,
            DashStyle dashStyle)
        {
            Thickness = thickness;
            StartLineCap = startLineCap;
            EndLineCap = endLineCap;
            DashCap = dashCap;
            LineJoin = lineJoin;
            MiterLimit = miterLimit;

            Brush = brush;
            DashStyle = dashStyle;
        }

        #endregion Constructors



 
        internal static uint ToD2DCapStyle(PenLineCap cap) => cap switch
        {
            PenLineCap.Square => DxMediaConst.D2D1_CAP_STYLE_SQUARE,
            PenLineCap.Round => DxMediaConst.D2D1_CAP_STYLE_ROUND,
            PenLineCap.Triangle => DxMediaConst.D2D1_CAP_STYLE_TRIANGLE,
            _ => DxMediaConst.D2D1_CAP_STYLE_FLAT,
        };

        internal static uint ToD2DLineJoin(PenLineJoin join) => join switch
        {
            PenLineJoin.Bevel => DxMediaConst.D2D1_LINE_JOIN_BEVEL,
            PenLineJoin.Round => DxMediaConst.D2D1_LINE_JOIN_ROUND,
            _ => DxMediaConst.D2D1_LINE_JOIN_MITER,
        };

        internal unsafe void* CreateD2DStrokeStyle(void* d2dFactory)
        {
            if (d2dFactory == null)
            {
                return null;
            }

            float[] dashes = null;
            float dashOffset = 0f;
            DashStyle style = DashStyle;
            if (style != null)
            {
                dashes = style.GetDashArray(out dashOffset);
            }

            var props = new D2D1_STROKE_STYLE_PROPERTIES1
            {
                startCap = ToD2DCapStyle(StartLineCap),
                endCap = ToD2DCapStyle(EndLineCap),
                dashCap = ToD2DCapStyle(DashCap),
                lineJoin = ToD2DLineJoin(LineJoin),
                miterLimit = (float)MiterLimit,
                dashStyle = (dashes != null && dashes.Length > 0) ? DxMediaConst.D2D1_DASH_STYLE_CUSTOM : DxMediaConst.D2D1_DASH_STYLE_SOLID,
                dashOffset = dashOffset,
                transformType = DxMediaConst.D2D1_STROKE_TRANSFORM_TYPE_NORMAL,
            };

            void* strokeStyle = null;
            fixed (float* pDashes = dashes)
            {
                int hr = ID2D1FactoryExVtbl.CreateStrokeStyle1(
                    d2dFactory,
                    in props,
                    pDashes,
                    dashes != null ? (uint)dashes.Length : 0u,
                    &strokeStyle);
                HResult.ThrowIfFailed(hr, "ID2D1Factory1.CreateStrokeStyle1");
            }

            return strokeStyle;
        }

        internal bool DoesNotContainGaps
        {
            get
            {
                DashStyle style = DashStyle;

                if (style != null)
                {
                    DoubleCollection dashes = style.Dashes;

                    if ((dashes != null) && (dashes.Count > 0))
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        internal static bool ContributesToBounds(
            Pen pen)
        {
            return (pen != null) && (pen.Brush != null);
        }
}
}

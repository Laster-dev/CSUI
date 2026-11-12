// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//
//
//
// Description: This file contains the implementation of TileBrush.
//              The TileBrush is an abstract class of Brushes which describes
//              a way to fill a region by tiling.  The contents of the tiles
//              are described by classes derived from TileBrush.
//
//


using MS.Internal;

#nullable disable
using SR = MS.Internal.PresentationCore.SR;

namespace System.Windows.Media
{
    /// <summary>
    /// TileBrush
    /// The TileBrush is an abstract class of Brushes which describes
    /// a way to fill a region by tiling.  The contents of the tiles
    /// are described by classes derived from TileBrush.
    /// </summary>
    public abstract partial class TileBrush : Brush
    {
        #region Constructors
    
        /// <summary>
        /// Protected constructor for TileBrush.  
        /// Sets all values to their defaults.  
        /// To set property values, use the constructor which accepts paramters
        /// </summary>
        protected TileBrush()
        {
        }
   
        #endregion Constructors

        /// <summary>
        /// Obtains the current bounds of the brush's content
        /// </summary>
        /// <param name="contentBounds"> Output bounds of content </param>            
        protected abstract void GetContentBounds(out Rect contentBounds);

        /// <summary>
        /// Obtains a matrix that maps the TileBrush's content to the coordinate
        /// space of the shape it is filling.
        /// </summary>
        /// <param name="shapeFillBounds">
        ///     Fill-bounds of the shape this brush is stroking/filling
        /// </param>
        /// <param name="tileBrushMapping">
        ///     Output matrix that maps the TileBrush's content to the coordinate
        ///     space of the shape it is filling
        /// </param>
        internal void GetTileBrushMapping(
            Rect shapeFillBounds,
            out Matrix tileBrushMapping
            )
        {
            Rect contentBounds = Rect.Empty;
            if (ViewboxUnits == BrushMappingMode.RelativeToBoundingBox)
            {
                GetContentBounds(out contentBounds);
            }

            GetTileBrushMapping(shapeFillBounds, contentBounds, out tileBrushMapping);
        }

        internal void GetTileBrushMapping(
            Rect shapeFillBounds,
            Rect contentBounds,
            out Matrix tileBrushMapping
            )
        {
            BrushMappingMode viewboxUnits = ViewboxUnits;
            bool brushIsEmpty = false;

            tileBrushMapping = Matrix.Identity;

            if (viewboxUnits == BrushMappingMode.RelativeToBoundingBox)
            {
                if (contentBounds.IsEmpty || contentBounds.Width <= 0 || contentBounds.Height <= 0)
                {
                    brushIsEmpty = true;
                }
            }

            if (brushIsEmpty)
            {
                return;
            }

            BrushMappingMode viewportUnits = ViewportUnits;
            if (viewportUnits == BrushMappingMode.RelativeToBoundingBox &&
                (shapeFillBounds.IsEmpty || shapeFillBounds.Width <= 0 || shapeFillBounds.Height <= 0))
            {
                return;
            }

            Rect viewbox = Viewbox;
            if (viewboxUnits == BrushMappingMode.RelativeToBoundingBox)
            {
                viewbox = new Rect(
                    contentBounds.X + viewbox.X * contentBounds.Width,
                    contentBounds.Y + viewbox.Y * contentBounds.Height,
                    viewbox.Width * contentBounds.Width,
                    viewbox.Height * contentBounds.Height);
            }

            Rect viewport = Viewport;
            if (viewportUnits == BrushMappingMode.RelativeToBoundingBox)
            {
                viewport = new Rect(
                    shapeFillBounds.X + viewport.X * shapeFillBounds.Width,
                    shapeFillBounds.Y + viewport.Y * shapeFillBounds.Height,
                    viewport.Width * shapeFillBounds.Width,
                    viewport.Height * shapeFillBounds.Height);
            }

            if (viewbox.IsEmpty || DoubleUtil.IsZero(viewbox.Width) || DoubleUtil.IsZero(viewbox.Height) ||
                viewport.IsEmpty || DoubleUtil.IsZero(viewport.Width) || DoubleUtil.IsZero(viewport.Height))
            {
                return;
            }

            double scaleX = viewport.Width / viewbox.Width;
            double scaleY = viewport.Height / viewbox.Height;
            switch (Stretch)
            {
                case Stretch.None:
                    scaleX = 1.0;
                    scaleY = 1.0;
                    break;
                case Stretch.Uniform:
                    scaleX = scaleY = Math.Min(scaleX, scaleY);
                    break;
                case Stretch.UniformToFill:
                    scaleX = scaleY = Math.Max(scaleX, scaleY);
                    break;
                case Stretch.Fill:
                default:
                    break;
            }

            double alignXFactor = AlignmentX switch
            {
                AlignmentX.Left => 0.0,
                AlignmentX.Center => 0.5,
                AlignmentX.Right => 1.0,
                _ => 0.5,
            };
            double alignYFactor = AlignmentY switch
            {
                AlignmentY.Top => 0.0,
                AlignmentY.Center => 0.5,
                AlignmentY.Bottom => 1.0,
                _ => 0.5,
            };

            double offsetX = alignXFactor * (viewport.Width - viewbox.Width * scaleX);
            double offsetY = alignYFactor * (viewport.Height - viewbox.Height * scaleY);

            Matrix mapping = Matrix.Identity;
            mapping.Translate(-viewbox.X, -viewbox.Y);
            mapping.Scale(scaleX, scaleY);
            mapping.Translate(viewport.X + offsetX, viewport.Y + offsetY);

            Transform.GetTransformValue(RelativeTransform, out Matrix relativeTransformValue);
            if (!relativeTransformValue.IsIdentity && !shapeFillBounds.IsEmpty && shapeFillBounds.Width > 0 && shapeFillBounds.Height > 0)
            {
                Matrix rel = Matrix.Identity;
                rel.Translate(-shapeFillBounds.X, -shapeFillBounds.Y);
                rel.Scale(1.0 / shapeFillBounds.Width, 1.0 / shapeFillBounds.Height);
                rel *= relativeTransformValue;
                rel.Scale(shapeFillBounds.Width, shapeFillBounds.Height);
                rel.Translate(shapeFillBounds.X, shapeFillBounds.Y);
                mapping *= rel;
            }

            Transform.GetTransformValue(Transform, out Matrix transformValue);
            if (!transformValue.IsIdentity)
            {
                mapping *= transformValue;
            }

            tileBrushMapping = mapping;
        }
    }
}

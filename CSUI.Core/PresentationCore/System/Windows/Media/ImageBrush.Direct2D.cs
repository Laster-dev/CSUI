// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Windows.Media
{
    public sealed partial class ImageBrush
    {
        internal override unsafe void* CreateD2DBrush(void* d2dContext, in Rect bounds)
        {
            ImageSource image = ImageSource;
            if (image == null)
            {
                return null;
            }

            Rect contentBounds;
            if (image is DrawingImage)
            {
                GetContentBounds(out contentBounds);
            }
            else
            {
                Size imageSize = image.Size;
                if (imageSize.IsEmpty || imageSize.Width <= 0.0 || imageSize.Height <= 0.0)
                {
                    return null;
                }

                contentBounds = new Rect(0.0, 0.0, imageSize.Width, imageSize.Height);
            }

            if (contentBounds.IsEmpty || contentBounds.Width <= 0.0 || contentBounds.Height <= 0.0)
            {
                return null;
            }

            GetTileBrushMapping(bounds, contentBounds, out Matrix tileBrushMapping);

            return TileBrushDirect2DHelper.CreateTileBitmapBrushCore(
                this,
                Viewbox,
                ViewboxUnits,
                in tileBrushMapping,
                TileMode,
                d2dContext,
                in bounds,
                in contentBounds,
                ctx => image.DrawD2D((void*)ctx, contentBounds));
        }
    }
}

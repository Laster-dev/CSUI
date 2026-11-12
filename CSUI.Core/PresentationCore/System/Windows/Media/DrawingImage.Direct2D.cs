// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using CSUI.Core.Interop.DirectX;

namespace System.Windows.Media
{
    public sealed partial class DrawingImage
    {
        internal override unsafe void DrawD2D(void* d2dContext, Rect rectangle)
        {
            Drawing drawing = Drawing;
            if (drawing == null || d2dContext == null || rectangle.IsEmpty || rectangle.Width <= 0.0 || rectangle.Height <= 0.0)
            {
                return;
            }

            Rect contentBounds = drawing.Bounds;
            if (contentBounds.IsEmpty || contentBounds.Width <= 0.0 || contentBounds.Height <= 0.0)
            {
                return;
            }

            ID2D1DeviceContextExVtbl.GetTransform(d2dContext, out D2D1_MATRIX_3X2_F savedTransform);
            ID2D1DeviceContextVtbl.PushAxisAlignedClip(
                d2dContext,
                Direct2DRenderWalker.ToD2DRect(rectangle),
                DxConst.D2D1_ANTIALIAS_MODE_PER_PRIMITIVE);

            try
            {
                Matrix m = Matrix.Identity;
                m.Translate(-contentBounds.X, -contentBounds.Y);
                m.Scale(rectangle.Width / contentBounds.Width, rectangle.Height / contentBounds.Height);
                m.Translate(rectangle.X, rectangle.Y);

                Matrix prev = MILUtilities.ConvertFromD2DMatrix(savedTransform);
                ID2D1DeviceContextVtbl.SetTransform(d2dContext, MILUtilities.ConvertToD2DMatrix(Matrix.Multiply(m, prev)));

                Direct2DRenderWalker.Render(drawing, d2dContext);
            }
            finally
            {
                ID2D1DeviceContextVtbl.SetTransform(d2dContext, savedTransform);
                ID2D1DeviceContextVtbl.PopAxisAlignedClip(d2dContext);
            }
        }
    }
}

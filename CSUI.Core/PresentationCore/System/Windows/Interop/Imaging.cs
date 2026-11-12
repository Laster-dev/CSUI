// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using MS.Internal;
using System.Windows.Media.Imaging;

namespace System.Windows.Interop
{
    /// <summary>
    /// Managed/Unmanaged Interop for Imaging.
    /// </summary>
    public static class Imaging
    {
        /// <summary>
        /// Construct an Bitmap from a HBITMAP.
        /// </summary>
        public static unsafe BitmapSource CreateBitmapSourceFromHBitmap(
            IntPtr bitmap,
            IntPtr palette,
            Int32Rect sourceRect,
            BitmapSizeOptions sizeOptions)
        {
            return CriticalCreateBitmapSourceFromHBitmap(bitmap, palette, sourceRect, sizeOptions, WICBitmapAlphaChannelOption.WICBitmapUseAlpha);
        }

        internal static unsafe BitmapSource CriticalCreateBitmapSourceFromHBitmap(
            IntPtr bitmap,
            IntPtr palette,
            Int32Rect sourceRect,
            BitmapSizeOptions sizeOptions,
            WICBitmapAlphaChannelOption alphaOptions)
        {
            if (bitmap == IntPtr.Zero)
            {
                throw new ArgumentNullException(nameof(bitmap));
            }

            return new InteropBitmap(bitmap, palette, sourceRect, sizeOptions, alphaOptions);
        }

        /// <summary>
        /// Construct an Bitmap from a HICON.
        /// </summary>
        public static unsafe BitmapSource CreateBitmapSourceFromHIcon(
            IntPtr icon,
            Int32Rect sourceRect,
            BitmapSizeOptions sizeOptions)
        {
            if (icon == IntPtr.Zero)
            {
                throw new ArgumentNullException(nameof(icon));
            }

            return new InteropBitmap(icon, sourceRect, sizeOptions);
        }

        /// <summary>
        /// Construct an Bitmap from a section handle.
        /// </summary>
        public static unsafe BitmapSource CreateBitmapSourceFromMemorySection(
            IntPtr section,
            int pixelWidth,
            int pixelHeight,
            Media.PixelFormat format,
            int stride,
            int offset)
        {
            if (section == IntPtr.Zero)
            {
                throw new ArgumentNullException(nameof(section));
            }

            return new InteropBitmap(section, pixelWidth, pixelHeight, format, stride, offset);
        }
    }
}

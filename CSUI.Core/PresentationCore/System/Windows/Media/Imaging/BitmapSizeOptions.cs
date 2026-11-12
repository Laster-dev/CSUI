// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System.Diagnostics;
using MS.Internal;
using SR = MS.Internal.PresentationCore.SR;

namespace System.Windows.Media.Imaging
{
    /// <summary>
    /// Sizing options for a bitmap. The resulting bitmap
    /// will be scaled based on these options.
    /// </summary>
    public class BitmapSizeOptions
    {
        private BitmapSizeOptions()
        {
        }

        public bool PreservesAspectRatio => _preservesAspectRatio;

        public int PixelWidth => _pixelWidth;

        public int PixelHeight => _pixelHeight;

        public Rotation Rotation => _rotationAngle;

        public static BitmapSizeOptions FromEmptyOptions()
        {
            return new BitmapSizeOptions
            {
                _rotationAngle = Rotation.Rotate0,
                _preservesAspectRatio = true,
                _pixelHeight = 0,
                _pixelWidth = 0
            };
        }

        public static BitmapSizeOptions FromHeight(int pixelHeight)
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(pixelHeight);

            return new BitmapSizeOptions
            {
                _rotationAngle = Rotation.Rotate0,
                _preservesAspectRatio = true,
                _pixelHeight = pixelHeight,
                _pixelWidth = 0
            };
        }

        public static BitmapSizeOptions FromWidth(int pixelWidth)
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(pixelWidth);

            return new BitmapSizeOptions
            {
                _rotationAngle = Rotation.Rotate0,
                _preservesAspectRatio = true,
                _pixelWidth = pixelWidth,
                _pixelHeight = 0
            };
        }

        public static BitmapSizeOptions FromWidthAndHeight(int pixelWidth, int pixelHeight)
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(pixelWidth);
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(pixelHeight);

            return new BitmapSizeOptions
            {
                _rotationAngle = Rotation.Rotate0,
                _preservesAspectRatio = false,
                _pixelWidth = pixelWidth,
                _pixelHeight = pixelHeight
            };
        }

        public static BitmapSizeOptions FromRotation(Rotation rotation)
        {
            switch (rotation)
            {
                case Rotation.Rotate0:
                case Rotation.Rotate90:
                case Rotation.Rotate180:
                case Rotation.Rotate270:
                    break;
                default:
                    throw new ArgumentException(SR.Image_SizeOptionsAngle, nameof(rotation));
            }

            return new BitmapSizeOptions
            {
                _rotationAngle = rotation,
                _preservesAspectRatio = true,
                _pixelWidth = 0,
                _pixelHeight = 0
            };
        }

        internal void GetScaledWidthAndHeight(
            uint width,
            uint height,
            out uint newWidth,
            out uint newHeight)
        {
            if (_pixelWidth == 0 && _pixelHeight != 0)
            {
                Debug.Assert(_preservesAspectRatio);

                newWidth = (uint)((_pixelHeight * width) / height);
                newHeight = (uint)_pixelHeight;
            }
            else if (_pixelWidth != 0 && _pixelHeight == 0)
            {
                Debug.Assert(_preservesAspectRatio);

                newWidth = (uint)_pixelWidth;
                newHeight = (uint)((_pixelWidth * height) / width);
            }
            else if (_pixelWidth != 0 && _pixelHeight != 0)
            {
                Debug.Assert(!_preservesAspectRatio);

                newWidth = (uint)_pixelWidth;
                newHeight = (uint)_pixelHeight;
            }
            else
            {
                newWidth = width;
                newHeight = height;
            }
        }

        internal bool DoesScale => _pixelWidth != 0 || _pixelHeight != 0;

        internal WICBitmapTransformOptions WICTransformOptions
        {
            get
            {
                return _rotationAngle switch
                {
                    Rotation.Rotate0 => WICBitmapTransformOptions.WICBitmapTransformRotate0,
                    Rotation.Rotate90 => WICBitmapTransformOptions.WICBitmapTransformRotate90,
                    Rotation.Rotate180 => WICBitmapTransformOptions.WICBitmapTransformRotate180,
                    Rotation.Rotate270 => WICBitmapTransformOptions.WICBitmapTransformRotate270,
                    _ => WICBitmapTransformOptions.WICBitmapTransformRotate0
                };
            }
        }

        private bool _preservesAspectRatio;
        private int _pixelWidth;
        private int _pixelHeight;
        private Rotation _rotationAngle;
    }
}

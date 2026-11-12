// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System.Windows;
using SR = MS.Internal.PresentationFramework.SR;

namespace MS.Internal.KnownBoxes
{
    internal class SizeBox
    {
        internal SizeBox(double width, double height)
        {
            if (width < 0 || height < 0)
            {
                throw new System.ArgumentException(SR.Rect_WidthAndHeightCannotBeNegative);
            }

            _width = width;
            _height = height;
        }

        internal SizeBox(Size size) : this(size.Width, size.Height) { }

        internal double Width
        {
            get => _width;
            set
            {
                if (value < 0)
                {
                    throw new System.ArgumentException(SR.Rect_WidthAndHeightCannotBeNegative);
                }

                _width = value;
            }
        }

        internal double Height
        {
            get => _height;
            set
            {
                if (value < 0)
                {
                    throw new System.ArgumentException(SR.Rect_WidthAndHeightCannotBeNegative);
                }

                _height = value;
            }
        }

        private double _width;
        private double _height;
    }
}

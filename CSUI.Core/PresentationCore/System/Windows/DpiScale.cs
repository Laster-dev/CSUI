// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using MS.Internal;

namespace System.Windows
{
    public struct DpiScale
    {
        public DpiScale(double dpiScaleX, double dpiScaleY)
        {
            _dpiScaleX = dpiScaleX;
            _dpiScaleY = dpiScaleY;
        }

        public double DpiScaleX => _dpiScaleX;
        public double DpiScaleY => _dpiScaleY;
        public double PixelsPerDip => _dpiScaleY;
        public double PixelsPerInchX => DpiUtil.DefaultPixelsPerInch * _dpiScaleX;
        public double PixelsPerInchY => DpiUtil.DefaultPixelsPerInch * _dpiScaleY;

        internal bool Equals(DpiScale other) => _dpiScaleX == other._dpiScaleX && _dpiScaleY == other._dpiScaleY;

        private readonly double _dpiScaleX;
        private readonly double _dpiScaleY;
    }
}

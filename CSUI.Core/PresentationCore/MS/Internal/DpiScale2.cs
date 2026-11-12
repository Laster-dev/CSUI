// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System.Windows;

namespace MS.Internal
{
    internal class DpiScale2 : IEquatable<DpiScale2>, IEquatable<DpiScale>
    {
        private DpiScale dpiScale;

        internal DpiScale2(DpiScale dpiScale)
        {
            this.dpiScale = dpiScale;
        }

        internal DpiScale2(double dpiScaleX, double dpiScaleY)
            : this(new DpiScale(dpiScaleX, dpiScaleY))
        {
        }

        internal double DpiScaleX => this.dpiScale.DpiScaleX;
        internal double DpiScaleY => this.dpiScale.DpiScaleY;
        internal double PixelsPerDip => this.dpiScale.PixelsPerDip;
        internal double PixelsPerInchX => this.dpiScale.PixelsPerInchX;
        internal double PixelsPerInchY => this.dpiScale.PixelsPerInchY;

        public static implicit operator DpiScale(DpiScale2 dpiScale2) => dpiScale2.dpiScale;

        public static bool operator !=(DpiScale2 dpiScaleA, DpiScale2 dpiScaleB)
        {
            if ((object.ReferenceEquals(dpiScaleA, null) && !object.ReferenceEquals(dpiScaleB, null)) ||
                (!object.ReferenceEquals(dpiScaleA, null) && object.ReferenceEquals(dpiScaleB, null)))
            {
                return true;
            }

            if (object.ReferenceEquals(dpiScaleA, null))
            {
                return false;
            }

            return !dpiScaleA.Equals(dpiScaleB);
        }

        public static bool operator ==(DpiScale2 dpiScaleA, DpiScale2 dpiScaleB)
        {
            if (object.ReferenceEquals(dpiScaleA, null) && object.ReferenceEquals(dpiScaleB, null))
            {
                return true;
            }

            if (object.ReferenceEquals(dpiScaleA, null))
            {
                return false;
            }

            return dpiScaleA.Equals(dpiScaleB);
        }

        public bool Equals(DpiScale dpiScale)
        {
            return
                DoubleUtil.AreClose(this.DpiScaleX, dpiScale.DpiScaleX) &&
                DoubleUtil.AreClose(this.DpiScaleY, dpiScale.DpiScaleY);
        }

        public bool Equals(DpiScale2 dpiScale2)
        {
            if (object.ReferenceEquals(dpiScale2, null))
            {
                return false;
            }

            return this.Equals(dpiScale2.dpiScale);
        }

        public override bool Equals(object obj)
        {
            if (obj is DpiScale ds)
            {
                return this.Equals(ds);
            }
            if (obj is DpiScale2 ds2)
            {
                return this.Equals(ds2);
            }
            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return ((int)(this.PixelsPerInchX)).GetHashCode();
        }

        internal static DpiScale2 FromPixelsPerInch(double ppiX, double ppiY)
        {
            if (DoubleUtil.LessThanOrClose(ppiX, 0) || DoubleUtil.LessThanOrClose(ppiY, 0))
            {
                return null;
            }

            return new DpiScale2(ppiX / DpiUtil.DefaultPixelsPerInch, ppiY / DpiUtil.DefaultPixelsPerInch);
        }
    }
}

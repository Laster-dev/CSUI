// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using MS.Internal;

namespace System.Windows
{
    /// <summary>
    /// FontStretch structure describes relative change from the normal aspect ratio
    /// as specified by a font designer for the glyphs in a font.
    /// </summary>
    public struct FontStretch : IFormattable
    {
        internal FontStretch(int stretch)
        {
            Debug.Assert(1 <= stretch && stretch <= 9);

            // We want the default zero value of new FontStretch() to correspond to FontStretches.Normal.
            // Therefore, the _stretch value is shifted by 5 relative to the OpenType stretch value.
            _stretch = stretch - 5;
        }

        public static FontStretch FromOpenTypeStretch(int stretchValue)
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(stretchValue, 1);
            ArgumentOutOfRangeException.ThrowIfGreaterThan(stretchValue, 9);
            return new FontStretch(stretchValue);
        }

        public int ToOpenTypeStretch()
        {
            Debug.Assert(1 <= RealStretch && RealStretch <= 9);
            return RealStretch;
        }

        public static int Compare(FontStretch left, FontStretch right)
        {
            return left._stretch - right._stretch;
        }

        public static bool operator <(FontStretch left, FontStretch right)
        {
            return Compare(left, right) < 0;
        }

        public static bool operator <=(FontStretch left, FontStretch right)
        {
            return Compare(left, right) <= 0;
        }

        public static bool operator >(FontStretch left, FontStretch right)
        {
            return Compare(left, right) > 0;
        }

        public static bool operator >=(FontStretch left, FontStretch right)
        {
            return Compare(left, right) >= 0;
        }

        public static bool operator ==(FontStretch left, FontStretch right)
        {
            return Compare(left, right) == 0;
        }

        public static bool operator !=(FontStretch left, FontStretch right)
        {
            return !(left == right);
        }

        public bool Equals(FontStretch obj)
        {
            return this == obj;
        }

        public override bool Equals(object obj)
        {
            if (!(obj is FontStretch))
                return false;
            return this == (FontStretch)obj;
        }

        public override int GetHashCode()
        {
            return RealStretch;
        }

        public override string ToString()
        {
            return ConvertToString(null, null);
        }

        string IFormattable.ToString(string format, IFormatProvider provider)
        {
            return ConvertToString(format, provider);
        }

        private string ConvertToString(string format, IFormatProvider provider)
        {
            string convertedValue;
            if (!FontStretches.FontStretchToString(RealStretch, out convertedValue))
            {
                // This can happen only if _stretch member is corrupted.
                Invariant.Assert(false);
            }
            return convertedValue;
        }

        private int RealStretch
        {
            get
            {
                return _stretch + 5;
            }
        }

        private int _stretch;
    }
}

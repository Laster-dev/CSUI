// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

namespace System.Windows
{
    /// <summary>
    /// FontStyle structure describes the style of a font face, such as Normal, Italic or Oblique.
    /// </summary>
    public struct FontStyle : IFormattable
    {
        internal FontStyle(int style)
        {
            Debug.Assert(0 <= style && style <= 2);

            _style = style;
        }

        public static bool operator ==(FontStyle left, FontStyle right)
        {
            return left._style == right._style;
        }

        public static bool operator !=(FontStyle left, FontStyle right)
        {
            return !(left == right);
        }

        public bool Equals(FontStyle obj)
        {
            return this == obj;
        }

        public override bool Equals(object obj)
        {
            if (!(obj is FontStyle))
                return false;
            return this == (FontStyle)obj;
        }

        public override int GetHashCode()
        {
            return _style;
        }

        public override string ToString()
        {
            return ConvertToString(null, null);
        }

        string IFormattable.ToString(string format, IFormatProvider provider)
        {
            return ConvertToString(format, provider);
        }

        internal int GetStyleForInternalConstruction()
        {
            Debug.Assert(0 <= _style && _style <= 2);
            return _style;
        }

        private string ConvertToString(string format, IFormatProvider provider)
        {
            if (_style == 0)
                return "Normal";
            if (_style == 1)
                return "Oblique";
            Debug.Assert(_style == 2);
            return "Italic";
        }

        private int _style;
    }
}

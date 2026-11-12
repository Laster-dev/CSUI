// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

namespace System.Windows
{
    /// <summary>
    /// FontStyles contains predefined font style structures for common font styles.
    /// </summary>
    public static class FontStyles
    {
        public static FontStyle Normal  { get { return new FontStyle(0); } }
        public static FontStyle Oblique { get { return new FontStyle(1); } }
        public static FontStyle Italic  { get { return new FontStyle(2); } }

        internal static bool FontStyleStringToKnownStyle(string s, IFormatProvider provider, ref FontStyle fontStyle)
        {
            if (s.Equals("Normal", StringComparison.OrdinalIgnoreCase))
            {
                fontStyle = FontStyles.Normal;
                return true;
            }
            if (s.Equals("Italic", StringComparison.OrdinalIgnoreCase))
            {
                fontStyle = FontStyles.Italic;
                return true;
            }
            if (s.Equals("Oblique", StringComparison.OrdinalIgnoreCase))
            {
                fontStyle = FontStyles.Oblique;
                return true;
            }
            return false;
        }
    }
}

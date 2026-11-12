// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;

namespace MS.Internal.TextFormatting
{
    /// <summary>
    /// DigitMap maps unicode code points (from the backing store) to unicode code
    /// points (to be rendered) based on a specified digit culture.
    /// </summary>
    internal struct DigitMap
    {
        private readonly NumberFormatInfo? _format;
        private readonly string[]? _digits;

        internal DigitMap(CultureInfo? digitCulture)
        {
            if (digitCulture != null)
            {
                _format = digitCulture.NumberFormat;
                _digits = _format.NativeDigits;
            }
            else
            {
                _format = null;
                _digits = null;
            }
        }

        internal int this[int ch]
        {
            get
            {
                if (_format != null && _digits != null && IsDigitOrSymbol(ch))
                {
                    uint n = (uint)ch - '0';
                    if (n < 10)
                    {
                        ch = StringToScalar(_digits[n], ch);
                    }
                    else if (ch == '%')
                    {
                        ch = StringToScalar(_format.PercentSymbol, ch);
                    }
                    else if (ch == ',')
                    {
                        ch = StringToScalar(_format.NumberGroupSeparator, ch);
                    }
                    else
                    {
                        ch = StringToScalar(_format.NumberDecimalSeparator, ch);
                    }
                }

                return ch;
            }
        }

        /// <summary>
        /// In some cases, our first choice for a substituted code point is not present
        /// in many older fonts. To avoid displaying missing glyphs in such cases, this
        /// function returns the alternate character to fall back to if the specified
        /// substituted character does not exist in the font. The return value is zero
        /// if there is no fallback character.
        /// </summary>
        internal static int GetFallbackCharacter(int ch)
        {
            switch (ch)
            {
                case 0x066B: return (int)',';   // Arabic decimal point -> Western comma
                case 0x066C: return 0x060C;     // Arabic thousands separator -> Arabic comma
                case 0x0BE6: return (int)'0';   // Tamil zero -> Western zero
            }

            return 0;   // no fallback character
        }

        private static int StringToScalar(string s, int defaultValue)
        {
            if (s.Length == 1)
            {
                return (int)s[0];
            }
            else if (s.Length == 2 &&
                IsHighSurrogate((int)s[0]) &&
                IsLowSurrogate((int)s[1]))
            {
                return MakeUnicodeScalar((int)s[0], (int)s[1]);
            }
            else
            {
                return defaultValue;
            }
        }

        internal static bool IsHighSurrogate(int ch)
        {
            return ch >= 0xd800 && ch < 0xdc00;
        }

        internal static bool IsLowSurrogate(int ch)
        {
            return ch >= 0xdc00 && ch < 0xe000;
        }

        internal static bool IsSurrogate(int ch)
        {
            return IsHighSurrogate(ch) || IsLowSurrogate(ch);
        }

        internal static int MakeUnicodeScalar(int hi, int lo)
        {
            return ((hi & 0x03ff) << 10 | (lo & 0x03ff)) + 0x10000;
        }

        private static bool IsDigitOrSymbol(int ch)
        {
            // The code points we're interested in are in the range 0x25 - 0x39.
            const int first = 0x25; // percent
            const int last  = 0x39; // '9'

            if ((uint)(ch - first) <= (uint)(last - first))
            {
                const uint mask =
                    (1U << ('%' - first)) |  // U+0025
                    (1U << (',' - first)) |  // U+002C
                    (1U << ('.' - first)) |  // U+002E
                    (1U << ('0' - first)) |  // U+0030
                    (1U << ('1' - first)) |  // U+0031
                    (1U << ('2' - first)) |  // U+0032
                    (1U << ('3' - first)) |  // U+0033
                    (1U << ('4' - first)) |  // U+0034
                    (1U << ('5' - first)) |  // U+0035
                    (1U << ('6' - first)) |  // U+0036
                    (1U << ('7' - first)) |  // U+0037
                    (1U << ('8' - first)) |  // U+0038
                    (1U << ('9' - first));   // U+0039

                return ((mask >> (ch - first)) & 1) != 0;
            }
            else
            {
                return false;
            }
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//  Synopsis: Implements class Parsers for pure C# parsing (zero reflection / zero TypeConverter)

#nullable disable

using System.Windows.Media;
using SR = MS.Internal.PresentationCore.SR;

namespace MS.Internal
{
    internal static partial class Parsers
    {
        private const int s_zeroChar = (int)'0';
        private const int s_aLower   = (int)'a';
        private const int s_aUpper   = (int)'A';

        internal const string s_ContextColor = "ContextColor ";
        internal const string s_ContextColorNoSpace = "ContextColor";

        private static int ParseHexChar(char c)
        {
            int intChar = (int)c;

            if ((intChar >= s_zeroChar) && (intChar <= (s_zeroChar + 9)))
            {
                return (intChar - s_zeroChar);
            }

            if ((intChar >= s_aLower) && (intChar <= (s_aLower + 5)))
            {
                return (intChar - s_aLower + 10);
            }

            if ((intChar >= s_aUpper) && (intChar <= (s_aUpper + 5)))
            {
                return (intChar - s_aUpper + 10);
            }
            throw new FormatException(SR.Parsers_IllegalToken);
        }

        private static Color ParseHexColor(string trimmedColor)
        {
            int a, r, g, b;
            a = 255;

            if (trimmedColor.Length > 7)
            {
                a = ParseHexChar(trimmedColor[1]) * 16 + ParseHexChar(trimmedColor[2]);
                r = ParseHexChar(trimmedColor[3]) * 16 + ParseHexChar(trimmedColor[4]);
                g = ParseHexChar(trimmedColor[5]) * 16 + ParseHexChar(trimmedColor[6]);
                b = ParseHexChar(trimmedColor[7]) * 16 + ParseHexChar(trimmedColor[8]);
            }
            else if (trimmedColor.Length > 5)
            {
                r = ParseHexChar(trimmedColor[1]) * 16 + ParseHexChar(trimmedColor[2]);
                g = ParseHexChar(trimmedColor[3]) * 16 + ParseHexChar(trimmedColor[4]);
                b = ParseHexChar(trimmedColor[5]) * 16 + ParseHexChar(trimmedColor[6]);
            }
            else if (trimmedColor.Length > 4)
            {
                a = ParseHexChar(trimmedColor[1]);
                a += a * 16;
                r = ParseHexChar(trimmedColor[2]);
                r += r * 16;
                g = ParseHexChar(trimmedColor[3]);
                g += g * 16;
                b = ParseHexChar(trimmedColor[4]);
                b += b * 16;
            }
            else
            {
                r = ParseHexChar(trimmedColor[1]);
                r += r * 16;
                g = ParseHexChar(trimmedColor[2]);
                g += g * 16;
                b = ParseHexChar(trimmedColor[3]);
                b += b * 16;
            }

            return Color.FromArgb((byte)a, (byte)r, (byte)g, (byte)b);
        }

        private static Color ParseScRgbColor(string trimmedColor, IFormatProvider formatProvider)
        {
            if (!trimmedColor.StartsWith("sc#", StringComparison.Ordinal))
            {
                throw new FormatException(SR.Parsers_IllegalToken);
            }

            string tokens = trimmedColor.Substring(3, trimmedColor.Length - 3);

            TokenizerHelper th = new TokenizerHelper(tokens, formatProvider);
            float[] values = new float[4];

            for (int i = 0; i < 3; i++)
            {
                values[i] = Convert.ToSingle(th.NextTokenRequired(), formatProvider);
            }

            if (th.NextToken())
            {
                values[3] = Convert.ToSingle(th.GetCurrentToken(), formatProvider);

                if (th.NextToken())
                {
                    throw new FormatException(SR.Parsers_IllegalToken);
                }

                return Color.FromScRgb(values[0], values[1], values[2], values[3]);
            }
            else
            {
                return Color.FromScRgb(1.0f, values[0], values[1], values[2]);
            }
        }

        /// <summary>
        /// ParseColor
        /// <param name="color"> string with color description </param>
        /// <param name="formatProvider">IFormatProvider for processing string</param>
        /// </summary>
        internal static Color ParseColor(string color, IFormatProvider formatProvider)
        {
            bool isPossibleKnowColor;
            bool isNumericColor;
            bool isScRgbColor;
            bool isContextColor;
            string trimmedColor = KnownColors.MatchColor(color, out isPossibleKnowColor, out isNumericColor, out isContextColor, out isScRgbColor);

            if ((!isPossibleKnowColor) &&
                (!isNumericColor) &&
                (!isScRgbColor) &&
                (!isContextColor))
            {
                throw new FormatException(SR.Parsers_IllegalToken);
            }

            if (isNumericColor)
            {
                return ParseHexColor(trimmedColor);
            }
            else if (isContextColor)
            {
                throw new NotSupportedException(SR.Parsers_IllegalToken);
            }
            else if (isScRgbColor)
            {
                return ParseScRgbColor(trimmedColor, formatProvider);
            }
            else
            {
                KnownColor kc = KnownColors.ColorStringToKnownColor(trimmedColor);

                if (kc == KnownColor.UnknownColor)
                {
                    throw new FormatException(SR.Parsers_IllegalToken);
                }

                return Color.FromUInt32((uint)kc);
            }
        }
    }
}

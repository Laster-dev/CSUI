// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Windows.Media;

#nullable disable
using SR = MS.Internal.PresentationCore.SR;

namespace MS.Internal
{
    internal static partial class Parsers
    {
        /// <summary>
        /// ParseBrush - parse a Brush from a string (reproduces WPF SolidColorBrush string syntax without reflection).
        /// </summary>
        internal static Brush ParseBrush(
            string brush,
            IFormatProvider formatProvider)
        {
            string trimmedColor = KnownColors.MatchColor(
                brush,
                out bool isPossibleKnownColor,
                out bool isNumericColor,
                out bool isContextColor,
                out bool isScRgbColor);

            if (trimmedColor.Length == 0)
            {
                throw new FormatException(SR.Parser_Empty);
            }

            if (isNumericColor)
            {
                return new SolidColorBrush(ParseHexColor(trimmedColor));
            }

            if (isContextColor)
            {
                throw new NotSupportedException(SR.Parsers_IllegalToken);
            }

            if (isScRgbColor)
            {
                return new SolidColorBrush(ParseScRgbColor(trimmedColor, formatProvider));
            }

            if (isPossibleKnownColor)
            {
                SolidColorBrush scp = KnownColors.ColorStringToKnownBrush(trimmedColor);

                if (scp != null)
                {
                    return scp;
                }
            }

            // Not a color, so the token is illegal.
            throw new FormatException(SR.Parsers_IllegalToken);
        }
    }
}

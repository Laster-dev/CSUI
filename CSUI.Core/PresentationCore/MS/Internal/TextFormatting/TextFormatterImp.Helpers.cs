// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Windows.Media;

namespace MS.Internal.TextFormatting
{
    internal sealed partial class TextFormatterImp
    {
        internal static int CompareReal(double x, double y, double pixelsPerDip, TextFormattingMode mode)
        {
            double xDisplay = x;
            double yDisplay = y;

            if (mode == TextFormattingMode.Display)
            {
                xDisplay = RoundDipForDisplayMode(x, pixelsPerDip);
                yDisplay = RoundDipForDisplayMode(y, pixelsPerDip);
            }

            if (xDisplay > yDisplay)
            {
                return 1;
            }

            if (xDisplay < yDisplay)
            {
                return -1;
            }

            return 0;
        }

        internal static double RoundDip(double value, double pixelsPerDip, TextFormattingMode textFormattingMode)
        {
            if (TextFormattingMode.Display == textFormattingMode)
            {
                return RoundDipForDisplayMode(value, pixelsPerDip);
            }
            else
            {
                return value;
            }
        }

        internal static double RoundDipForDisplayMode(double value, double pixelsPerDip)
        {
            return RoundDipForDisplayMode(value, pixelsPerDip, MidpointRounding.ToEven);
        }

        private static double RoundDipForDisplayMode(double value, double pixelsPerDip, MidpointRounding midpointRounding)
        {
            return Math.Round(value * pixelsPerDip, midpointRounding) / pixelsPerDip;
        }

        /// <summary>
        /// Consistent rounding of the width of GlyphRuns for Display mode + Justified text.
        /// </summary>
        internal static double RoundDipForDisplayModeJustifiedText(double value, double pixelsPerDip)
        {
            return RoundDipForDisplayMode(value, pixelsPerDip, MidpointRounding.AwayFromZero);
        }

        /// <summary>
        /// Scale LS ideal resolution value to real value
        /// </summary>
        internal static double IdealToRealWithNoRounding(double i)
        {
            return i * Constants.DefaultIdealToReal;
        }

        /// <summary>
        /// Scale real value to LS ideal resolution
        /// </summary>
        internal static int RealToIdeal(double i)
        {
            int value = (int)Math.Round(i * ToIdeal);
            if (i > 0)
            {
                // Non-zero values should not be converted to 0 accidentally through rounding, ensure that at least the min value is returned.
                value = Math.Max(value, 1);
            }
            return value;
        }

        /// <summary>
        /// Scale the real value to LS ideal resolution using floor.
        /// </summary>
        internal static int RealToIdealFloor(double i)
        {
            int value = (int)Math.Floor(i * ToIdeal);
            if (i > 0)
            {
                value = Math.Max(value, 1);
            }
            return value;
        }

        /// <summary>
        /// Real to ideal value scaling factor
        /// </summary>
        internal static double ToIdeal
        {
            get { return Constants.DefaultRealToIdeal; }
        }
    }
}

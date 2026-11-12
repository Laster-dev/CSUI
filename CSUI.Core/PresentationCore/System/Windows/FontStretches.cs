// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System.Globalization;

namespace System.Windows
{
    /// <summary>
    /// FontStretches contains predefined font stretch structures for common font stretches.
    /// </summary>
    public static class FontStretches
    {
        public static FontStretch UltraCondensed { get { return new FontStretch(1); } }
        public static FontStretch ExtraCondensed { get { return new FontStretch(2); } }
        public static FontStretch Condensed      { get { return new FontStretch(3); } }
        public static FontStretch SemiCondensed  { get { return new FontStretch(4); } }
        public static FontStretch Normal         { get { return new FontStretch(5); } }
        public static FontStretch Medium         { get { return new FontStretch(5); } }
        public static FontStretch SemiExpanded   { get { return new FontStretch(6); } }
        public static FontStretch Expanded       { get { return new FontStretch(7); } }
        public static FontStretch ExtraExpanded  { get { return new FontStretch(8); } }
        public static FontStretch UltraExpanded  { get { return new FontStretch(9); } }

        internal static bool FontStretchStringToKnownStretch(string s, IFormatProvider provider, ref FontStretch fontStretch)
        {
            switch (s.Length)
            {
                case 6:
                    if (s.Equals("Normal", StringComparison.OrdinalIgnoreCase))
                    {
                        fontStretch = FontStretches.Normal;
                        return true;
                    }
                    if (s.Equals("Medium", StringComparison.OrdinalIgnoreCase))
                    {
                        fontStretch = FontStretches.Medium;
                        return true;
                    }
                    break;

                case 8:
                    if (s.Equals("Expanded", StringComparison.OrdinalIgnoreCase))
                    {
                        fontStretch = FontStretches.Expanded;
                        return true;
                    }
                    break;

                case 9:
                    if (s.Equals("Condensed", StringComparison.OrdinalIgnoreCase))
                    {
                        fontStretch = FontStretches.Condensed;
                        return true;
                    }
                    break;

                case 12:
                    if (s.Equals("SemiExpanded", StringComparison.OrdinalIgnoreCase))
                    {
                        fontStretch = FontStretches.SemiExpanded;
                        return true;
                    }
                    break;

                case 13:
                    if (s.Equals("SemiCondensed", StringComparison.OrdinalIgnoreCase))
                    {
                        fontStretch = FontStretches.SemiCondensed;
                        return true;
                    }
                    if (s.Equals("ExtraExpanded", StringComparison.OrdinalIgnoreCase))
                    {
                        fontStretch = FontStretches.ExtraExpanded;
                        return true;
                    }
                    if (s.Equals("UltraExpanded", StringComparison.OrdinalIgnoreCase))
                    {
                        fontStretch = FontStretches.UltraExpanded;
                        return true;
                    }
                    break;

                case 14:
                    if (s.Equals("UltraCondensed", StringComparison.OrdinalIgnoreCase))
                    {
                        fontStretch = FontStretches.UltraCondensed;
                        return true;
                    }
                    if (s.Equals("ExtraCondensed", StringComparison.OrdinalIgnoreCase))
                    {
                        fontStretch = FontStretches.ExtraCondensed;
                        return true;
                    }
                    break;
            }

            int stretchValue;
            if (int.TryParse(s, NumberStyles.Integer, provider, out stretchValue))
            {
                fontStretch = FontStretch.FromOpenTypeStretch(stretchValue);
                return true;
            }
            return false;
        }

        internal static bool FontStretchToString(int stretch, out string convertedValue)
        {
            switch (stretch)
            {
                case 1:
                    convertedValue = "UltraCondensed";
                    return true;
                case 2:
                    convertedValue = "ExtraCondensed";
                    return true;
                case 3:
                    convertedValue = "Condensed";
                    return true;
                case 4:
                    convertedValue = "SemiCondensed";
                    return true;
                case 5:
                    convertedValue = "Normal";
                    return true;
                case 6:
                    convertedValue = "SemiExpanded";
                    return true;
                case 7:
                    convertedValue = "Expanded";
                    return true;
                case 8:
                    convertedValue = "ExtraExpanded";
                    return true;
                case 9:
                    convertedValue = "UltraExpanded";
                    return true;
            }
            convertedValue = null;
            return false;
        }
    }
}

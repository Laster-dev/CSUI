// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System.Globalization;

namespace System.Windows
{
    /// <summary>
    /// FontWeights contains predefined font weight structures for common font weights.
    /// </summary>
    public static class FontWeights
    {
        public static FontWeight Thin       { get { return new FontWeight(100); } }
        public static FontWeight ExtraLight { get { return new FontWeight(200); } }
        public static FontWeight UltraLight { get { return new FontWeight(200); } }
        public static FontWeight Light      { get { return new FontWeight(300); } }
        public static FontWeight Normal     { get { return new FontWeight(400); } }
        public static FontWeight Regular    { get { return new FontWeight(400); } }
        public static FontWeight Medium     { get { return new FontWeight(500); } }
        public static FontWeight DemiBold   { get { return new FontWeight(600); } }
        public static FontWeight SemiBold   { get { return new FontWeight(600); } }
        public static FontWeight Bold       { get { return new FontWeight(700); } }
        public static FontWeight ExtraBold  { get { return new FontWeight(800); } }
        public static FontWeight UltraBold  { get { return new FontWeight(800); } }
        public static FontWeight Black      { get { return new FontWeight(900); } }
        public static FontWeight Heavy      { get { return new FontWeight(900); } }
        public static FontWeight ExtraBlack { get { return new FontWeight(950); } }
        public static FontWeight UltraBlack { get { return new FontWeight(950); } }

        internal static bool FontWeightStringToKnownWeight(string s, IFormatProvider provider, ref FontWeight fontWeight)
        {
            switch (s.Length)
            {
                case 4:
                    if (s.Equals("Bold", StringComparison.OrdinalIgnoreCase))
                    {
                        fontWeight = FontWeights.Bold;
                        return true;
                    }
                    if (s.Equals("Thin", StringComparison.OrdinalIgnoreCase))
                    {
                        fontWeight = FontWeights.Thin;
                        return true;
                    }
                    break;

                case 5:
                    if (s.Equals("Black", StringComparison.OrdinalIgnoreCase))
                    {
                        fontWeight = FontWeights.Black;
                        return true;
                    }
                    if (s.Equals("Light", StringComparison.OrdinalIgnoreCase))
                    {
                        fontWeight = FontWeights.Light;
                        return true;
                    }
                    if (s.Equals("Heavy", StringComparison.OrdinalIgnoreCase))
                    {
                        fontWeight = FontWeights.Heavy;
                        return true;
                    }
                    break;

                case 6:
                    if (s.Equals("Normal", StringComparison.OrdinalIgnoreCase))
                    {
                        fontWeight = FontWeights.Normal;
                        return true;
                    }
                    if (s.Equals("Medium", StringComparison.OrdinalIgnoreCase))
                    {
                        fontWeight = FontWeights.Medium;
                        return true;
                    }
                    break;

                case 7:
                    if (s.Equals("Regular", StringComparison.OrdinalIgnoreCase))
                    {
                        fontWeight = FontWeights.Regular;
                        return true;
                    }
                    break;

                case 8:
                    if (s.Equals("SemiBold", StringComparison.OrdinalIgnoreCase))
                    {
                        fontWeight = FontWeights.SemiBold;
                        return true;
                    }
                    if (s.Equals("DemiBold", StringComparison.OrdinalIgnoreCase))
                    {
                        fontWeight = FontWeights.DemiBold;
                        return true;
                    }
                    break;

                case 9:
                    if (s.Equals("ExtraBold", StringComparison.OrdinalIgnoreCase))
                    {
                        fontWeight = FontWeights.ExtraBold;
                        return true;
                    }
                    if (s.Equals("UltraBold", StringComparison.OrdinalIgnoreCase))
                    {
                        fontWeight = FontWeights.UltraBold;
                        return true;
                    }
                    break;

                case 10:
                    if (s.Equals("ExtraLight", StringComparison.OrdinalIgnoreCase))
                    {
                        fontWeight = FontWeights.ExtraLight;
                        return true;
                    }
                    if (s.Equals("UltraLight", StringComparison.OrdinalIgnoreCase))
                    {
                        fontWeight = FontWeights.UltraLight;
                        return true;
                    }
                    if (s.Equals("ExtraBlack", StringComparison.OrdinalIgnoreCase))
                    {
                        fontWeight = FontWeights.ExtraBlack;
                        return true;
                    }
                    if (s.Equals("UltraBlack", StringComparison.OrdinalIgnoreCase))
                    {
                        fontWeight = FontWeights.UltraBlack;
                        return true;
                    }
                    break;
            }

            int weightValue;
            if (int.TryParse(s, NumberStyles.Integer, provider, out weightValue))
            {
                fontWeight = FontWeight.FromOpenTypeWeight(weightValue);
                return true;
            }
            return false;
        }

        internal static bool FontWeightToString(int weight, out string convertedValue)
        {
            switch (weight)
            {
                case 100:
                    convertedValue = "Thin";
                    return true;
                case 200:
                    convertedValue = "ExtraLight";
                    return true;
                case 300:
                    convertedValue = "Light";
                    return true;
                case 400:
                    convertedValue = "Normal";
                    return true;
                case 500:
                    convertedValue = "Medium";
                    return true;
                case 600:
                    convertedValue = "SemiBold";
                    return true;
                case 700:
                    convertedValue = "Bold";
                    return true;
                case 800:
                    convertedValue = "ExtraBold";
                    return true;
                case 900:
                    convertedValue = "Black";
                    return true;
                case 950:
                    convertedValue = "ExtraBlack";
                    return true;
            }
            convertedValue = null;
            return false;
        }
    }
}

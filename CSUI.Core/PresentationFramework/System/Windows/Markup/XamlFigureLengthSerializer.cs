// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System;
using System.Globalization;

namespace System.Windows.Markup
{
    internal static class XamlFigureLengthSerializer
    {
        internal static void FromString(string s, CultureInfo cultureInfo, out double value, out FigureUnitType unit)
        {
            string trimmed = s?.Trim();
            if (string.IsNullOrEmpty(trimmed) || trimmed.Equals("auto", StringComparison.OrdinalIgnoreCase))
            {
                value = 1.0;
                unit = FigureUnitType.Auto;
                return;
            }

            if (TryTrimSuffix(trimmed, "columns", out string numPart) || TryTrimSuffix(trimmed, "column", out numPart))
            {
                unit = FigureUnitType.Column;
                value = string.IsNullOrEmpty(numPart) ? 1.0 : Convert.ToDouble(numPart, cultureInfo);
                return;
            }
            if (TryTrimSuffix(trimmed, "content", out numPart))
            {
                unit = FigureUnitType.Content;
                value = string.IsNullOrEmpty(numPart) ? 1.0 : Convert.ToDouble(numPart, cultureInfo);
                return;
            }
            if (TryTrimSuffix(trimmed, "page", out numPart))
            {
                unit = FigureUnitType.Page;
                value = string.IsNullOrEmpty(numPart) ? 1.0 : Convert.ToDouble(numPart, cultureInfo);
                return;
            }

            double factor = 1.0;
            if (TryTrimSuffix(trimmed, "px", out numPart))
            {
                factor = 1.0;
            }
            else if (TryTrimSuffix(trimmed, "in", out numPart))
            {
                factor = 96.0;
            }
            else if (TryTrimSuffix(trimmed, "cm", out numPart))
            {
                factor = 96.0 / 2.54;
            }
            else if (TryTrimSuffix(trimmed, "pt", out numPart))
            {
                factor = 96.0 / 72.0;
            }
            else
            {
                numPart = trimmed;
            }

            unit = FigureUnitType.Pixel;
            value = Convert.ToDouble(numPart, cultureInfo) * factor;
        }

        private static bool TryTrimSuffix(string input, string suffix, out string numberPart)
        {
            if (input.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
            {
                numberPart = input.Substring(0, input.Length - suffix.Length).Trim();
                return true;
            }
            numberPart = null;
            return false;
        }
    }
}

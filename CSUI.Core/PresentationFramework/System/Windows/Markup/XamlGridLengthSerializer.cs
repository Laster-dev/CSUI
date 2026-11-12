// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System;
using System.Globalization;

namespace System.Windows.Markup
{
    internal static class XamlGridLengthSerializer
    {
        internal static void FromString(string s, CultureInfo cultureInfo, out double value, out GridUnitType unit)
        {
            string trimmed = s?.Trim();
            if (string.IsNullOrEmpty(trimmed) || trimmed.Equals("auto", StringComparison.OrdinalIgnoreCase))
            {
                value = 1.0;
                unit = GridUnitType.Auto;
                return;
            }

            if (trimmed.EndsWith('*'))
            {
                unit = GridUnitType.Star;
                string numPart = trimmed.Substring(0, trimmed.Length - 1).Trim();
                value = numPart.Length == 0 ? 1.0 : Convert.ToDouble(numPart, cultureInfo);
                return;
            }

            double factor = 1.0;
            string num;
            if (trimmed.EndsWith("px", StringComparison.OrdinalIgnoreCase))
            {
                num = trimmed.Substring(0, trimmed.Length - 2).Trim();
            }
            else if (trimmed.EndsWith("in", StringComparison.OrdinalIgnoreCase))
            {
                num = trimmed.Substring(0, trimmed.Length - 2).Trim();
                factor = 96.0;
            }
            else if (trimmed.EndsWith("cm", StringComparison.OrdinalIgnoreCase))
            {
                num = trimmed.Substring(0, trimmed.Length - 2).Trim();
                factor = 96.0 / 2.54;
            }
            else if (trimmed.EndsWith("pt", StringComparison.OrdinalIgnoreCase))
            {
                num = trimmed.Substring(0, trimmed.Length - 2).Trim();
                factor = 96.0 / 72.0;
            }
            else
            {
                num = trimmed;
            }

            unit = GridUnitType.Pixel;
            value = Convert.ToDouble(num, cultureInfo) * factor;
        }
    }
}

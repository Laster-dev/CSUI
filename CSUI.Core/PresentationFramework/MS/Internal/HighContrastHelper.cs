// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System.Windows;

namespace MS.Internal
{
    internal static class HighContrastHelper
    {
        private static bool s_isHighContrast = SystemParameters.HighContrast;

        internal static void OnSettingChanged()
        {
            s_isHighContrast = SystemParameters.HighContrast;
        }

        internal static bool IsHighContrast => s_isHighContrast;
    }
}

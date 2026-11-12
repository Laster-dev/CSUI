// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

namespace System.Windows.Media
{
    internal static partial class KnownColors
    {
        /// <summary>
        /// Return the solid color brush from a color string. If there's no match, null.
        /// </summary>
        public static SolidColorBrush ColorStringToKnownBrush(string s)
        {
            if (null != s)
            {
                KnownColor result = ColorStringToKnownColor(s);

                // If the result is UnknownColor, that means this string wasn't found
                if (result != KnownColor.UnknownColor)
                {
                    // Otherwise, return the appropriate SolidColorBrush
                    return SolidColorBrushFromUint((uint)result);
                }
            }
            return null;
        }

        public static bool IsKnownSolidColorBrush(SolidColorBrush scp)
        {
            lock (s_solidColorBrushCache)
            {
                return s_solidColorBrushCache.ContainsValue(scp);
            }
        }

        public static SolidColorBrush SolidColorBrushFromUint(uint argb)
        {
            SolidColorBrush scp = null;

            lock (s_solidColorBrushCache)
            {
                // Attempt to retrieve the color. If it fails create it.
                if (!s_solidColorBrushCache.TryGetValue(argb, out scp))
                {
                    scp = new SolidColorBrush(Color.FromUInt32(argb));
                    scp.Freeze();
                    s_solidColorBrushCache[argb] = scp;
                }
#if DEBUG
                else
                {
                    s_count++;
                }
#endif
            }

            return scp;
        }

#if DEBUG
        private static int s_count = 0;
#endif

        private static readonly Dictionary<uint, SolidColorBrush> s_solidColorBrushCache = new Dictionary<uint, SolidColorBrush>();
    }
}

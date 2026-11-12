// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using MS.Internal;

namespace System.Windows.Media.Imaging
{
    /// <summary>
    /// Pre-defined palette types.
    /// </summary>
    public static class BitmapPalettes
    {
        public static BitmapPalette BlackAndWhite => FromMILPaletteType(WICPaletteType.WICPaletteTypeFixedBW, false);
        public static BitmapPalette BlackAndWhiteTransparent => FromMILPaletteType(WICPaletteType.WICPaletteTypeFixedBW, true);
        public static BitmapPalette Halftone8 => FromMILPaletteType(WICPaletteType.WICPaletteTypeFixedHalftone8, false);
        public static BitmapPalette Halftone8Transparent => FromMILPaletteType(WICPaletteType.WICPaletteTypeFixedHalftone8, true);
        public static BitmapPalette Halftone27 => FromMILPaletteType(WICPaletteType.WICPaletteTypeFixedHalftone27, false);
        public static BitmapPalette Halftone27Transparent => FromMILPaletteType(WICPaletteType.WICPaletteTypeFixedHalftone27, true);
        public static BitmapPalette Halftone64 => FromMILPaletteType(WICPaletteType.WICPaletteTypeFixedHalftone64, false);
        public static BitmapPalette Halftone64Transparent => FromMILPaletteType(WICPaletteType.WICPaletteTypeFixedHalftone64, true);
        public static BitmapPalette Halftone125 => FromMILPaletteType(WICPaletteType.WICPaletteTypeFixedHalftone125, false);
        public static BitmapPalette Halftone125Transparent => FromMILPaletteType(WICPaletteType.WICPaletteTypeFixedHalftone125, true);
        public static BitmapPalette Halftone216 => FromMILPaletteType(WICPaletteType.WICPaletteTypeFixedHalftone216, false);
        public static BitmapPalette Halftone216Transparent => FromMILPaletteType(WICPaletteType.WICPaletteTypeFixedHalftone216, true);
        public static BitmapPalette Halftone252 => FromMILPaletteType(WICPaletteType.WICPaletteTypeFixedHalftone252, false);
        public static BitmapPalette Halftone252Transparent => FromMILPaletteType(WICPaletteType.WICPaletteTypeFixedHalftone252, true);
        public static BitmapPalette Halftone256 => FromMILPaletteType(WICPaletteType.WICPaletteTypeFixedHalftone256, false);
        public static BitmapPalette Halftone256Transparent => FromMILPaletteType(WICPaletteType.WICPaletteTypeFixedHalftone256, true);
        public static BitmapPalette Gray4 => FromMILPaletteType(WICPaletteType.WICPaletteTypeFixedGray4, false);
        public static BitmapPalette Gray4Transparent => FromMILPaletteType(WICPaletteType.WICPaletteTypeFixedGray4, true);
        public static BitmapPalette Gray16 => FromMILPaletteType(WICPaletteType.WICPaletteTypeFixedGray16, false);
        public static BitmapPalette Gray16Transparent => FromMILPaletteType(WICPaletteType.WICPaletteTypeFixedGray16, true);
        public static BitmapPalette Gray256 => FromMILPaletteType(WICPaletteType.WICPaletteTypeFixedGray256, false);
        public static BitmapPalette Gray256Transparent => FromMILPaletteType(WICPaletteType.WICPaletteTypeFixedGray256, true);
        public static BitmapPalette WebPalette => FromMILPaletteType(WICPaletteType.WICPaletteTypeFixedHalftone216, false);
        public static BitmapPalette WebPaletteTransparent => FromMILPaletteType(WICPaletteType.WICPaletteTypeFixedHalftone216, true);

        internal static BitmapPalette FromMILPaletteType(WICPaletteType type, bool hasAlpha)
        {
            int key = (int)type;
            BitmapPalette[] palettes = hasAlpha ? TransparentPalettes : OpaquePalettes;
            BitmapPalette palette = palettes[key];

            if (palette == null)
            {
                lock (palettes)
                {
                    palette = palettes[key];
                    if (palette == null)
                    {
                        palette = new BitmapPalette(type, hasAlpha);
                        palettes[key] = palette;
                    }
                }
            }

            return palette;
        }

        private static BitmapPalette[] TransparentPalettes => s_transparentPalettes ??= new BitmapPalette[MaxPalettes];
        private static BitmapPalette[] OpaquePalettes => s_opaquePalettes ??= new BitmapPalette[MaxPalettes];

        private static BitmapPalette[] s_transparentPalettes;
        private static BitmapPalette[] s_opaquePalettes;
        private const int MaxPalettes = 64;
    }
}

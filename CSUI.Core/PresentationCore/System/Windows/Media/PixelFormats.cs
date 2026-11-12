// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

namespace System.Windows.Media
{
    /// <summary>
    /// PixelFormats - The collection of supported Pixel Formats.
    /// </summary>
    public static class PixelFormats
    {
        public static PixelFormat Default => new PixelFormat(PixelFormatEnum.Default);
        public static PixelFormat Indexed1 => new PixelFormat(PixelFormatEnum.Indexed1);
        public static PixelFormat Indexed2 => new PixelFormat(PixelFormatEnum.Indexed2);
        public static PixelFormat Indexed4 => new PixelFormat(PixelFormatEnum.Indexed4);
        public static PixelFormat Indexed8 => new PixelFormat(PixelFormatEnum.Indexed8);
        public static PixelFormat BlackWhite => new PixelFormat(PixelFormatEnum.BlackWhite);
        public static PixelFormat Gray2 => new PixelFormat(PixelFormatEnum.Gray2);
        public static PixelFormat Gray4 => new PixelFormat(PixelFormatEnum.Gray4);
        public static PixelFormat Gray8 => new PixelFormat(PixelFormatEnum.Gray8);
        public static PixelFormat Bgr555 => new PixelFormat(PixelFormatEnum.Bgr555);
        public static PixelFormat Bgr565 => new PixelFormat(PixelFormatEnum.Bgr565);
        public static PixelFormat Rgb128Float => new PixelFormat(PixelFormatEnum.Rgb128Float);
        public static PixelFormat Bgr24 => new PixelFormat(PixelFormatEnum.Bgr24);
        public static PixelFormat Rgb24 => new PixelFormat(PixelFormatEnum.Rgb24);
        public static PixelFormat Bgr101010 => new PixelFormat(PixelFormatEnum.Bgr101010);
        public static PixelFormat Bgr32 => new PixelFormat(PixelFormatEnum.Bgr32);
        public static PixelFormat Bgra32 => new PixelFormat(PixelFormatEnum.Bgra32);
        public static PixelFormat Pbgra32 => new PixelFormat(PixelFormatEnum.Pbgra32);
        public static PixelFormat Rgb48 => new PixelFormat(PixelFormatEnum.Rgb48);
        public static PixelFormat Rgba64 => new PixelFormat(PixelFormatEnum.Rgba64);
        public static PixelFormat Prgba64 => new PixelFormat(PixelFormatEnum.Prgba64);
        public static PixelFormat Gray16 => new PixelFormat(PixelFormatEnum.Gray16);
        public static PixelFormat Gray32Float => new PixelFormat(PixelFormatEnum.Gray32Float);
        public static PixelFormat Rgba128Float => new PixelFormat(PixelFormatEnum.Rgba128Float);
        public static PixelFormat Prgba128Float => new PixelFormat(PixelFormatEnum.Prgba128Float);
        public static PixelFormat Cmyk32 => new PixelFormat(PixelFormatEnum.Cmyk32);
    }
}

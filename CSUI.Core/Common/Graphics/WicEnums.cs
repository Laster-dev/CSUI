// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

namespace MS.Internal
{
    internal enum WICBitmapAlphaChannelOption
    {
        WICBitmapUseAlpha              = 0,
        WICBitmapUsePremultipliedAlpha = 1,
        WICBitmapIgnoreAlpha           = 2,
    }

    internal enum WICBitmapCreateCacheOptions
    {
        WICBitmapNoCache       = 0x00000000,
        WICBitmapCacheOnDemand = 0x00000001,
        WICBitmapCacheOnLoad   = 0x00000002,
    }

    internal enum WICBitmapEncodeCacheOption
    {
        WICBitmapEncodeCacheInMemory = 0x00000000,
        WICBitmapEncodeCacheTempFile = 0x00000001,
        WICBitmapEncodeNoCache       = 0x00000002,
    }

    internal enum WICMetadataCacheOptions
    {
        WICMetadataCacheOnDemand = 0x00000000,
        WICMetadataCacheOnLoad   = 0x00000001,
    }

    internal enum WICInterpolationMode
    {
        NearestNeighbor = 0,
        Linear          = 1,
        Cubic           = 2,
        Fant            = 3,
    }

    internal enum DitherType
    {
        DitherTypeNone          = 0,
        DitherTypeSolid         = 0,
        DitherTypeOrdered4x4    = 1,
        DitherTypeOrdered8x8    = 2,
        DitherTypeOrdered16x16  = 3,
        DitherTypeSpiral4x4     = 4,
        DitherTypeSpiral8x8     = 5,
        DitherTypeDualSpiral4x4 = 6,
        DitherTypeDualSpiral8x8 = 7,
        DitherTypeErrorDiffusion = 8,
    }

    internal enum WICPaletteType
    {
        WICPaletteTypeCustom           = 0,
        WICPaletteTypeOptimal          = 1,
        WICPaletteTypeFixedBW          = 2,
        WICPaletteTypeFixedHalftone8   = 3,
        WICPaletteTypeFixedHalftone27  = 4,
        WICPaletteTypeFixedHalftone64  = 5,
        WICPaletteTypeFixedHalftone125 = 6,
        WICPaletteTypeFixedHalftone216 = 7,
        WICPaletteTypeFixedWebPalette  = 7,
        WICPaletteTypeFixedHalftone252 = 8,
        WICPaletteTypeFixedHalftone256 = 9,
        WICPaletteTypeFixedGray4       = 10,
        WICPaletteTypeFixedGray16      = 11,
        WICPaletteTypeFixedGray256     = 12,
    }

    [Flags]
    internal enum WICBitmapTransformOptions
    {
        WICBitmapTransformRotate0        = 0x00000000,
        WICBitmapTransformRotate90       = 0x00000001,
        WICBitmapTransformRotate180      = 0x00000002,
        WICBitmapTransformRotate270      = 0x00000003,
        WICBitmapTransformFlipHorizontal = 0x00000008,
        WICBitmapTransformFlipVertical   = 0x00000010,
    }

    internal enum WICColorContextType
    {
        WICColorContextUninitialized  = 0x00000000,
        WICColorContextProfile        = 0x00000001,
        WICColorContextExifColorSpace = 0x00000002,
    }
}

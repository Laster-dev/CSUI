// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System.Windows.Media.Imaging;

namespace System.Windows.Media
{
    internal static partial class ValidateEnums
    {
        public static bool IsBitmapCacheOptionValid(object valueObject)
        {
            BitmapCacheOption value = (BitmapCacheOption)valueObject;
            return value == BitmapCacheOption.Default ||
                   value == BitmapCacheOption.OnLoad ||
                   value == BitmapCacheOption.None;
        }

        public static bool IsBitmapCreateOptionsValid(object valueObject)
        {
            BitmapCreateOptions value = (BitmapCreateOptions)valueObject;
            const BitmapCreateOptions ValidMask =
                BitmapCreateOptions.PreservePixelFormat |
                BitmapCreateOptions.DelayCreation |
                BitmapCreateOptions.IgnoreColorProfile |
                BitmapCreateOptions.IgnoreImageCache;
            return (value & ~ValidMask) == 0;
        }
    }
}

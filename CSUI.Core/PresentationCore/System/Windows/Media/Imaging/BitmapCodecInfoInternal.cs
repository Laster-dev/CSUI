// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

namespace System.Windows.Media.Imaging
{
    /// <summary>
    /// Internal codec info wrapper around a WIC component handle.
    /// </summary>
    internal class BitmapCodecInfoInternal : BitmapCodecInfo
    {
        private BitmapCodecInfoInternal()
        {
        }

        internal BitmapCodecInfoInternal(SafeMILHandle codecInfoHandle)
            : base(codecInfoHandle)
        {
        }
    }
}

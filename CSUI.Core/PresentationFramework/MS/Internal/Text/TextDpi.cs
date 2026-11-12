// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

namespace MS.Internal.Text
{
    internal static class TextDpi
    {
        internal const double DefaultRealToIdeal = 28800.0 / 96.0;
        internal const double DefaultIdealToReal = 1.0 / DefaultRealToIdeal;
        internal const double MinWidth = DefaultIdealToReal;
        internal const double MaxWidth = 0x3FFFFFFE * DefaultIdealToReal;
    }
}

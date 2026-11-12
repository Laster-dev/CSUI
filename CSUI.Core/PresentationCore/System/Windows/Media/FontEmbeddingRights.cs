// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

namespace System.Windows.Media
{
    /// <summary>
    /// The FontEmbeddingRight enumeration is used to describe font embedding permissions
    /// specified in an OpenType font file.
    /// </summary>
    public enum FontEmbeddingRight
    {
        Installable = 0,
        InstallableButNoSubsetting,
        InstallableButWithBitmapsOnly,
        InstallableButNoSubsettingAndWithBitmapsOnly,
        RestrictedLicense,
        PreviewAndPrint,
        PreviewAndPrintButNoSubsetting,
        PreviewAndPrintButWithBitmapsOnly,
        PreviewAndPrintButNoSubsettingAndWithBitmapsOnly,
        Editable,
        EditableButNoSubsetting,
        EditableButWithBitmapsOnly,
        EditableButNoSubsettingAndWithBitmapsOnly
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


#nullable disable
using SR = MS.Internal.WindowsBase.SR;

namespace System.Windows
{
    /// <summary>
    ///     Property value freeze callback
    /// </summary>
    internal delegate bool FreezeValueCallback(DependencyObject d, DependencyProperty dp, EntryIndex entryIndex, PropertyMetadata metadata, bool isChecking);
}


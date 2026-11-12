// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#define TRACE

//
// Description: Defines TraceShell class, for providing debugging information
//              for Shell integration
//

#nullable disable
using SR = MS.Internal.PresentationFramework.SR;

namespace MS.Internal
{
    /// <summary>
    /// Provides a central mechanism for providing debugging information
    /// to aid programmers in using Shell integration features.
    /// Helpers are defined here.
    /// The rest of the class is generated; see also: AvTraceMessage.txt and genTraceStrings.pl
    /// </summary>
    internal static partial class TraceShell
    {
        // Note: static ctor intentionally omitted while IsEnabled => false so accessing
        // TraceShell.IsEnabled preserves beforefieldinit in NativeAOT builds.
    }
}



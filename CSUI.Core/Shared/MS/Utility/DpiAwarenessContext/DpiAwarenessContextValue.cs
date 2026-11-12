// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

namespace MS.Utility
{
    internal enum DpiAwarenessContextValue : int
    {
        Invalid = 0,
        Unaware = -1,
        SystemAware = -2,
        PerMonitorAware = -3,
        PerMonitorAwareVersion2 = -4
    }
}

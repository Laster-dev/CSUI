// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System.Runtime.CompilerServices;
using System.Threading;

namespace MS.Utility
{
    /// <summary>
    /// Zero-reflection NativeAOT element ID tracker for ETW perf tracing stubs.
    /// </summary>
    internal static class PerfService
    {
        private static readonly ConditionalWeakTable<object, StrongBox<long>> s_perfElementIds = new();
        private static long s_nextId;

        internal static long GetPerfElementID2(object element, string extraData)
        {
            if (element == null)
            {
                return 0;
            }

            StrongBox<long> box = s_perfElementIds.GetValue(
                element,
                static _ => new StrongBox<long>(Interlocked.Increment(ref s_nextId)));
            return box.Value;
        }

        internal static long GetPerfElementID(object element)
        {
            return GetPerfElementID2(element, string.Empty);
        }
    }
}

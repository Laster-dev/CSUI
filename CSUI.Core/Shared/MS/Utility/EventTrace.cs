// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Zero-overhead NativeAOT ETW facade preserving all WPF EventTrace signatures and IDs.

#nullable disable

using System.Runtime.CompilerServices;

namespace MS.Utility
{
    internal static partial class EventTrace
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool IsEnabled(Keyword flag, Level level) => false;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void EasyTraceEvent(Keyword keywords, Event eventID) { }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void EasyTraceEvent(Keyword keywords, Level level, Event eventID) { }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void EasyTraceEvent<T1>(Keyword keywords, Event eventID, T1 param1) { }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void EasyTraceEvent<T1>(Keyword keywords, Level level, Event eventID, T1 param1) { }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void EasyTraceEvent<T1, T2>(Keyword keywords, Event eventID, T1 param1, T2 param2) { }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void EasyTraceEvent<T1, T2>(Keyword keywords, Level level, Event eventID, T1 param1, T2 param2) { }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void EasyTraceEvent<T1, T2, T3>(Keyword keywords, Event eventID, T1 param1, T2 param2, T3 param3) { }

        public enum LayoutSource : byte
        {
            LayoutManager,
            HwndSource_SetLayoutSize,
            HwndSource_WMSIZE
        }

        internal static class EventProvider
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal static bool IsEnabled(Keyword keywords, Level level) => false;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal static void TraceEvent(Event eventID, Keyword keywords, Level level) { }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal static void TraceEvent<T1>(Event eventID, Keyword keywords, Level level, T1 param1) { }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal static void TraceEvent<T1, T2>(Event eventID, Keyword keywords, Level level, T1 param1, T2 param2) { }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal static void TraceEvent<T1, T2, T3>(Event eventID, Keyword keywords, Level level, T1 param1, T2 param2, T3 param3) { }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal static void TraceEvent<T1, T2, T3, T4>(Event eventID, Keyword keywords, Level level, T1 param1, T2 param2, T3 param3, T4 param4) { }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal static void TraceEvent(Event eventID, Keyword keywords, Level level, params object[] args) { }
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using MS.Utility;
using MS.Win32;
using PROCESS_DPI_AWARENESS = MS.Win32.NativeMethods.PROCESS_DPI_AWARENESS;

namespace MS.Internal
{
    internal static partial class DpiUtil
    {
        private static class DpiAwarenessContextHelper
        {
            private static bool IsGetWindowDpiAwarenessContextMethodSupported { get; set; } = true;

            internal static DpiAwarenessContextHandle GetDpiAwarenessContext(IntPtr hWnd)
            {
                if (IsGetWindowDpiAwarenessContextMethodSupported)
                {
                    try
                    {
                        return GetWindowDpiAwarenessContext(hWnd);
                    }
                    catch (Exception e) when (e is EntryPointNotFoundException || e is MissingMethodException || e is DllNotFoundException)
                    {
                        IsGetWindowDpiAwarenessContextMethodSupported = false;
                    }
                }

                return GetProcessDpiAwarenessContext(hWnd);
            }

            private static DpiAwarenessContextHandle GetProcessDpiAwarenessContext(IntPtr hWnd)
            {
                PROCESS_DPI_AWARENESS processDpiAwareneess =
                    ProcessDpiAwarenessHelper.GetProcessDpiAwareness(hWnd);
                return GetProcessDpiAwarenessContext(processDpiAwareneess);
            }

            internal static DpiAwarenessContextHandle GetProcessDpiAwarenessContext(PROCESS_DPI_AWARENESS dpiAwareness)
            {
                switch (dpiAwareness)
                {
                    case PROCESS_DPI_AWARENESS.PROCESS_SYSTEM_DPI_AWARE:
                        return NativeMethods.DPI_AWARENESS_CONTEXT_SYSTEM_AWARE;

                    case PROCESS_DPI_AWARENESS.PROCESS_PER_MONITOR_DPI_AWARE:
                        return NativeMethods.DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE;

                    case PROCESS_DPI_AWARENESS.PROCESS_DPI_UNAWARE:
                    default:
                        return NativeMethods.DPI_AWARENESS_CONTEXT_UNAWARE;
                }
            }

            private static DpiAwarenessContextHandle GetWindowDpiAwarenessContext(IntPtr hWnd)
            {
                return SafeNativeMethods.GetWindowDpiAwarenessContext(hWnd);
            }
        }
    }
}

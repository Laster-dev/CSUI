// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using Microsoft.Win32.SafeHandles;
using MS.Win32;
using System.Runtime.InteropServices;
using PROCESS_DPI_AWARENESS = MS.Win32.NativeMethods.PROCESS_DPI_AWARENESS;

namespace MS.Internal
{
    internal static partial class DpiUtil
    {
        private static class ProcessDpiAwarenessHelper
        {
            private static bool IsGetProcessDpiAwarenessFunctionSupported { get; set; } = true;

            internal static PROCESS_DPI_AWARENESS GetLegacyProcessDpiAwareness()
            {
                return
                    UnsafeNativeMethods.IsProcessDPIAware()
                    ? PROCESS_DPI_AWARENESS.PROCESS_SYSTEM_DPI_AWARE
                    : PROCESS_DPI_AWARENESS.PROCESS_DPI_UNAWARE;
            }

            internal static PROCESS_DPI_AWARENESS GetProcessDpiAwareness(IntPtr hWnd)
            {
                if (IsGetProcessDpiAwarenessFunctionSupported)
                {
                    try
                    {
                        return GetProcessDpiAwarenessFromWindow(hWnd);
                    }
                    catch (Exception e) when (e is EntryPointNotFoundException || e is MissingMethodException || e is DllNotFoundException)
                    {
                        IsGetProcessDpiAwarenessFunctionSupported = false;
                    }
                    catch (Exception e) when (e is ArgumentException || e is UnauthorizedAccessException || e is COMException)
                    {
                    }
                }

                return GetLegacyProcessDpiAwareness();
            }

            private static PROCESS_DPI_AWARENESS GetProcessDpiAwarenessFromWindow(IntPtr hWnd)
            {
                int windowThreadProcessId = 0;
                if (hWnd != IntPtr.Zero)
                {
                    UnsafeNativeMethods.GetWindowThreadProcessId(new HandleRef(null, hWnd), out windowThreadProcessId);
                }
                else
                {
                    windowThreadProcessId = Environment.ProcessId;
                }

                Debug.Assert(windowThreadProcessId != 0, "GetWindowThreadProcessId failed");

                using (var hProcess = new SafeProcessHandle(UnsafeNativeMethods.OpenProcess(NativeMethods.PROCESS_ALL_ACCESS, false, windowThreadProcessId), true))
                {
                    return SafeNativeMethods.GetProcessDpiAwareness(new HandleRef(null, hProcess.DangerousGetHandle()));
                }
            }
        }
    }
}

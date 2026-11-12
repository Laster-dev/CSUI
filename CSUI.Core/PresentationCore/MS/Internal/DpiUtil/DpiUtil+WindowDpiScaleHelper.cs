// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using MS.Win32;
using System.Runtime.InteropServices;
using MONITOR_DPI_TYPE = MS.Win32.NativeMethods.MONITOR_DPI_TYPE;

namespace MS.Internal
{
    internal static partial class DpiUtil
    {
        private static class WindowDpiScaleHelper
        {
            private static bool IsGetDpiForWindowFunctionEnabled { get; set; } = true;

            internal static DpiScale2 GetWindowDpi(IntPtr hWnd, bool fallbackToNearestMonitorHeuristic)
            {
                if (IsGetDpiForWindowFunctionEnabled)
                {
                    try
                    {
                        return GetDpiForWindow(hWnd);
                    }
                    catch (Exception e) when (e is EntryPointNotFoundException || e is MissingMethodException || e is DllNotFoundException)
                    {
                        IsGetDpiForWindowFunctionEnabled = false;
                    }
                    catch (Exception e) when (e is COMException)
                    {
                    }
                }

                if (fallbackToNearestMonitorHeuristic)
                {
                    try
                    {
                        return GetDpiForWindowFromNearestMonitor(hWnd);
                    }
                    catch (Exception e) when (e is COMException)
                    {
                    }
                }

                return null;
            }

            private static DpiScale2 GetDpiForWindow(IntPtr hWnd)
            {
                uint dpi = SafeNativeMethods.GetDpiForWindow(new HandleRef(IntPtr.Zero, hWnd));
                return DpiScale2.FromPixelsPerInch(dpi, dpi);
            }

            private static DpiScale2 GetDpiForWindowFromNearestMonitor(IntPtr hWnd)
            {
                IntPtr hMon =
                    SafeNativeMethods.MonitorFromWindow(new HandleRef(IntPtr.Zero, hWnd), NativeMethods.MONITOR_DEFAULTTONEAREST);

                uint dpiX, dpiY;
                int hr = (int)UnsafeNativeMethods.GetDpiForMonitor(new HandleRef(IntPtr.Zero, hMon), MONITOR_DPI_TYPE.MDT_EFFECTIVE_DPI, out dpiX, out dpiY);
                Marshal.ThrowExceptionForHR(hr);

                return DpiScale2.FromPixelsPerInch(dpiX, dpiY);
            }
        }
    }
}

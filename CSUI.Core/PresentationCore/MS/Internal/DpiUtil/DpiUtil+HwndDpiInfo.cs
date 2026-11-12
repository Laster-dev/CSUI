// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using MS.Utility;
using MS.Win32;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace MS.Internal
{
    internal static partial class DpiUtil
    {
        public class HwndDpiInfo : Tuple<DpiAwarenessContextValue, DpiScale2>
        {
            internal HwndDpiInfo(IntPtr hWnd, bool fallbackToNearestMonitorHeuristic) : base(
                item1: (DpiAwarenessContextValue)DpiUtil.GetDpiAwarenessContext(hWnd),
                item2: DpiUtil.GetWindowDpi(hWnd, fallbackToNearestMonitorHeuristic))
            {
                ContainingMonitorScreenRect = NearestMonitorInfoFromWindow(hWnd).rcMonitor;
            }

            internal HwndDpiInfo(DpiAwarenessContextValue dpiAwarenessContextValue, DpiScale2 dpiScale)
                : base(dpiAwarenessContextValue, dpiScale)
            {
                ContainingMonitorScreenRect = NearestMonitorInfoFromWindow(IntPtr.Zero).rcMonitor;
            }

            private static NativeMethods.MONITORINFOEX NearestMonitorInfoFromWindow(IntPtr hwnd)
            {
                IntPtr hMonitor = SafeNativeMethods.MonitorFromWindow(new HandleRef(null, hwnd), NativeMethods.MONITOR_DEFAULTTONEAREST);
                if (hMonitor == IntPtr.Zero)
                {
                    throw new Win32Exception();
                }

                var monitorInfo = new NativeMethods.MONITORINFOEX();
                SafeNativeMethods.GetMonitorInfo(new HandleRef(null, hMonitor), monitorInfo);

                return monitorInfo;
            }

            internal NativeMethods.RECT ContainingMonitorScreenRect { get; }
            internal DpiAwarenessContextValue DpiAwarenessContextValue => Item1;
            internal DpiScale2 DpiScale => Item2;
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using MS.Utility;
using System.Windows;
using PROCESS_DPI_AWARENESS = MS.Win32.NativeMethods.PROCESS_DPI_AWARENESS;

namespace MS.Internal
{
    internal static partial class DpiUtil
    {
        internal const double DefaultPixelsPerInch = 96.0d;

        // Shared DPI scale cache (forwarded by UIElement in Stage C3).
        internal static readonly object DpiLock = new object();
        internal static readonly List<double> DpiScaleXValues = new List<double>();
        internal static readonly List<double> DpiScaleYValues = new List<double>();

        internal static DpiAwarenessContextHandle GetDpiAwarenessContext(IntPtr hWnd)
        {
            return DpiAwarenessContextHelper.GetDpiAwarenessContext(hWnd);
        }

        internal static PROCESS_DPI_AWARENESS GetProcessDpiAwareness(IntPtr hWnd)
        {
            return ProcessDpiAwarenessHelper.GetProcessDpiAwareness(hWnd);
        }

        internal static DpiAwarenessContextValue GetProcessDpiAwarenessContextValue(IntPtr hWnd)
        {
            var dpiAwarenessContext = ProcessDpiAwarenessHelper.GetProcessDpiAwareness(hWnd);
            return (DpiAwarenessContextValue)DpiAwarenessContextHelper.GetProcessDpiAwarenessContext(dpiAwarenessContext);
        }

        internal static PROCESS_DPI_AWARENESS GetLegacyProcessDpiAwareness()
        {
            return ProcessDpiAwarenessHelper.GetLegacyProcessDpiAwareness();
        }

        internal static DpiScale2 GetSystemDpi()
        {
            return SystemDpiHelper.GetSystemDpi();
        }

        internal static DpiScale2 GetSystemDpiFromUIElementCache()
        {
            return SystemDpiHelper.GetSystemDpiFromUIElementCache();
        }

        internal static void UpdateUIElementCacheForSystemDpi(DpiScale2 systemDpiScale)
        {
            SystemDpiHelper.UpdateUIElementCacheForSystemDpi(systemDpiScale);
        }

        internal static DpiScale2 GetWindowDpi(IntPtr hWnd, bool fallbackToNearestMonitorHeuristic)
        {
            return WindowDpiScaleHelper.GetWindowDpi(hWnd, fallbackToNearestMonitorHeuristic);
        }

        internal static HwndDpiInfo GetExtendedDpiInfoForWindow(IntPtr hWnd, bool fallbackToNearestMonitorHeuristic)
        {
            return new HwndDpiInfo(hWnd, fallbackToNearestMonitorHeuristic);
        }

        internal static HwndDpiInfo GetExtendedDpiInfoForWindow(IntPtr hWnd)
        {
            return GetExtendedDpiInfoForWindow(hWnd, true);
        }

        internal static IDisposable WithDpiAwarenessContext(DpiAwarenessContextValue dpiAwarenessContext)
        {
            return new DpiAwarenessScope(dpiAwarenessContext);
        }

        internal static DpiFlags UpdateDpiScalesAndGetIndex(double pixelsPerInchX, double pixelsPerInchY)
        {
            lock (DpiLock)
            {
                bool dpiScaleFlag1, dpiScaleFlag2;
                int index = 0;
                int sizeOfList = DpiScaleXValues.Count;
                for (index = 0; index < sizeOfList; index++)
                {
                    if (DpiScaleXValues[index] == pixelsPerInchX / DefaultPixelsPerInch &&
                        DpiScaleYValues[index] == pixelsPerInchY / DefaultPixelsPerInch)
                    {
                        break;
                    }
                }

                if (index == sizeOfList)
                {
                    DpiScaleXValues.Add(pixelsPerInchX / DefaultPixelsPerInch);
                    DpiScaleYValues.Add(pixelsPerInchY / DefaultPixelsPerInch);
                }

                if (index < 3)
                {
                    dpiScaleFlag1 = (index & 0x1) != 0;
                    dpiScaleFlag2 = (index & 0x2) != 0;
                }
                else
                {
                    dpiScaleFlag1 = dpiScaleFlag2 = true;
                }

                return new DpiFlags(dpiScaleFlag1, dpiScaleFlag2, index);
            }
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using MS.Win32;
using System.Runtime.InteropServices;

namespace MS.Internal
{
    internal static partial class DpiUtil
    {
        private static class SystemDpiHelper
        {
            private static bool IsGetDpiForSystemFunctionAvailable { get; set; } = true;

            internal static DpiScale2 GetSystemDpi()
            {
                if (IsGetDpiForSystemFunctionAvailable)
                {
                    try
                    {
                        return GetDpiForSystem();
                    }
                    catch (Exception e) when (e is EntryPointNotFoundException || e is MissingMethodException || e is DllNotFoundException)
                    {
                        IsGetDpiForSystemFunctionAvailable = false;
                    }
                }

                return GetSystemDpiFromDeviceCaps();
            }

            internal static DpiScale2 GetSystemDpiFromUIElementCache()
            {
                lock (DpiLock)
                {
                    if (DpiScaleXValues.Count == 0)
                    {
                        DpiScale2 dpi = GetSystemDpi() ?? new DpiScale2(1.0, 1.0);
                        DpiScaleXValues.Add(dpi.DpiScaleX);
                        DpiScaleYValues.Add(dpi.DpiScaleY);
                    }
                    return new DpiScale2(DpiScaleXValues[0], DpiScaleYValues[0]);
                }
            }

            internal static void UpdateUIElementCacheForSystemDpi(DpiScale2 systemDpiScale)
            {
                lock (DpiLock)
                {
                    DpiScaleXValues.Insert(0, systemDpiScale.DpiScaleX);
                    DpiScaleYValues.Insert(0, systemDpiScale.DpiScaleY);
                }
            }

            private static DpiScale2 GetDpiForSystem()
            {
                uint dpi = SafeNativeMethods.GetDpiForSystem();
                return DpiScale2.FromPixelsPerInch(dpi, dpi);
            }

            private static DpiScale2 GetSystemDpiFromDeviceCaps()
            {
                HandleRef hWndDesktop = new HandleRef(IntPtr.Zero, IntPtr.Zero);
                HandleRef hDC = new HandleRef(IntPtr.Zero, UnsafeNativeMethods.GetDC(hWndDesktop));
                if (hDC.Handle == IntPtr.Zero)
                {
                    return null;
                }

                try
                {
                    int ppiX = UnsafeNativeMethods.GetDeviceCaps(hDC, NativeMethods.LOGPIXELSX);
                    int ppiY = UnsafeNativeMethods.GetDeviceCaps(hDC, NativeMethods.LOGPIXELSY);

                    return DpiScale2.FromPixelsPerInch(ppiX, ppiY);
                }
                finally
                {
                    UnsafeNativeMethods.ReleaseDC(hWndDesktop, hDC);
                }
            }
        }
    }
}

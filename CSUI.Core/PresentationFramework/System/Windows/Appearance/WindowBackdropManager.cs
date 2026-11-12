// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Windows.Interop;
using System.Windows.Media;
using MS.Internal;
using Standard;
using HRESULT = Standard.HRESULT;

// ReSharper disable once CheckNamespace
#nullable disable
using SR = MS.Internal.PresentationFramework.SR;

namespace System.Windows.Appearance;

public static class WindowBackdropManager
{
    public static bool IsSupported(WindowBackdropType backdropType)
    {
        return backdropType switch
        {
            WindowBackdropType.Auto => Utility.IsOSWindows11OrNewer,
            WindowBackdropType.TabbedWindow => Utility.IsWindows11_22H2OrNewer,
            WindowBackdropType.MainWindow => Utility.IsOSWindows11OrNewer,
            WindowBackdropType.TransientWindow => Utility.IsOSWindows11OrNewer,
            WindowBackdropType.None => true,
            _ => false
        };
    }

    internal static bool SetBackdrop(Window window, WindowBackdropType backdropType)
    {
        if (window is null ||
            !IsSupported(backdropType) ||
            window.AllowsTransparency ||
            !IsBackdropEnabled)
        {
            return false;
        }

        var handle = new WindowInteropHelper(window).Handle;
        if (handle == IntPtr.Zero)
        {
            return false;
        }

        return SetBackdropCore(handle, backdropType);
    }

    #region Private Methods

    private static bool SetBackdropCore(IntPtr hwnd, WindowBackdropType backdropType)
    {
        if (hwnd == IntPtr.Zero)
        {
            return false;
        }

        if (backdropType == WindowBackdropType.None)
        {
            RestoreBackground(hwnd);
            return RemoveBackdrop(hwnd);
        }

        RemoveBackground(hwnd);
        return ApplyBackdrop(hwnd, backdropType);
    }

    private static bool ApplyBackdrop(IntPtr hwnd, WindowBackdropType backdropType)
    {
        UpdateGlassFrame(hwnd, backdropType);

        if (Utility.IsWindows11_22H2OrNewer)
        {
            var backdropPvAttribute = backdropType switch
            {
                WindowBackdropType.Auto => Standard.DWMSBT.DWMSBT_MAINWINDOW,
                WindowBackdropType.TabbedWindow => Standard.DWMSBT.DWMSBT_TABBEDWINDOW,
                WindowBackdropType.MainWindow => Standard.DWMSBT.DWMSBT_MAINWINDOW,
                WindowBackdropType.TransientWindow => Standard.DWMSBT.DWMSBT_TRANSIENTWINDOW,
                _ => Standard.DWMSBT.DWMSBT_NONE
            };

            var dwmResult = NativeMethods.DwmSetWindowAttributeSystemBackdropType(hwnd, backdropPvAttribute);
            NativeMethods.DwmSetWindowAttributeCaptionColor(hwnd, NativeMethods.DWMWA_COLOR_NONE);
            return dwmResult == HRESULT.S_OK;
        }
        else if (Utility.IsOSWindows11OrNewer)
        {
            bool enableMica = backdropType == WindowBackdropType.MainWindow || backdropType == WindowBackdropType.Auto;
            bool ok = NativeMethods.DwmSetWindowAttributeMica(hwnd, enableMica);
            NativeMethods.DwmSetWindowAttributeCaptionColor(hwnd, NativeMethods.DWMWA_COLOR_NONE);
            return ok;
        }

        return false;
    }

    private static bool RemoveBackdrop(IntPtr hwnd)
    {
        UpdateGlassFrame(hwnd, WindowBackdropType.None);

        if (Utility.IsWindows11_22H2OrNewer)
        {
            var backdropPvAttribute = Standard.DWMSBT.DWMSBT_NONE;
            var dwmResult = NativeMethods.DwmSetWindowAttributeSystemBackdropType(hwnd, backdropPvAttribute);
            NativeMethods.DwmSetWindowAttributeCaptionColor(hwnd, NativeMethods.DWMWA_COLOR_DEFAULT);
            return dwmResult == HRESULT.S_OK;
        }
        else if (Utility.IsOSWindows11OrNewer)
        {
            NativeMethods.DwmSetWindowAttributeMica(hwnd, false);
            NativeMethods.DwmSetWindowAttributeCaptionColor(hwnd, NativeMethods.DWMWA_COLOR_DEFAULT);
            return true;
        }

        return false;
    }

    private static bool RemoveBackground(IntPtr hwnd)
    {
        if (hwnd != IntPtr.Zero)
        {
            var windowSource = HwndSource.FromHwnd(hwnd);
            if (windowSource.CompositionTarget != null)
            {
                // TODO : Save the previous background color and reapply in RestoreBackground 
                windowSource.CompositionTarget.BackgroundColor = Colors.Transparent;
                return true;
            }
        }
        return false;
    }

    private static bool RestoreBackground(IntPtr hwnd)
    {
        if (hwnd != IntPtr.Zero)
        {
            var windowSource = HwndSource.FromHwnd(hwnd);
            if (windowSource?.Handle != IntPtr.Zero && windowSource.CompositionTarget != null)
            {
                windowSource.CompositionTarget.BackgroundColor = SystemColors.WindowColor;
                return true;
            }
        }
        return false;
    }

    private static bool UpdateGlassFrame(IntPtr hwnd, WindowBackdropType backdropType)
    {
        MARGINS margins = new MARGINS();
        if (backdropType != WindowBackdropType.None)
        {
            margins = new MARGINS { cxLeftWidth = -1, cxRightWidth = -1, cyTopHeight = -1, cyBottomHeight = -1 };
        }

        return NativeMethods.DwmExtendFrameIntoClientArea(hwnd, ref margins);
    }

    #endregion

    #region Internal Properties

    internal static bool IsBackdropEnabled => _isBackdropEnabled ??= Utility.IsOSWindows11OrNewer && 
                                                                        !FrameworkAppContextSwitches.DisableFluentThemeWindowBackdrop;

    private static bool? _isBackdropEnabled = null;

    #endregion

}

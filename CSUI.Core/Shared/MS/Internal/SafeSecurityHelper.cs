// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media;
using MS.Win32;

namespace MS.Internal
{
    internal static partial class SafeSecurityHelper
    {
        internal static void TransformLocalRectToScreen(HandleRef hwnd, ref NativeMethods.RECT rcWindowCoords)
        {
            int retval = UnsafeNativeMethods.MapWindowPoints(hwnd, new HandleRef(null, IntPtr.Zero), ref rcWindowCoords, 2, out int win32Err);
            if (retval == 0 && win32Err != 0)
            {
                throw new Win32Exception(win32Err);
            }
        }

        internal static bool IsConnectedToPresentationSource(Visual visual)
        {
            return PresentationSource.CriticalFromVisual(visual) != null;
        }
    }
}

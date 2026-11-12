// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using MS.Utility;
using MS.Win32;

namespace MS.Internal
{
    internal static partial class DpiUtil
    {
        internal class DpiAwarenessScope : IDisposable
        {
            public DpiAwarenessScope(DpiAwarenessContextValue dpiAwarenessContextEnumValue)
                : this(
                      dpiAwarenessContextEnumValue,
                      updateIfThreadInMixedHostingMode: false,
                      updateIfWindowIsSystemAwareOrUnaware: false,
                      hWnd: IntPtr.Zero)
            {
            }

            public DpiAwarenessScope(
                DpiAwarenessContextValue dpiAwarenessContextEnumValue, bool updateIfThreadInMixedHostingMode)
                : this(
                      dpiAwarenessContextEnumValue,
                      updateIfThreadInMixedHostingMode,
                      updateIfWindowIsSystemAwareOrUnaware: false,
                      hWnd: IntPtr.Zero)
            {
            }

            public DpiAwarenessScope(
                DpiAwarenessContextValue dpiAwarenessContextEnumValue, bool updateIfThreadInMixedHostingMode, IntPtr hWnd)
                : this(
                    dpiAwarenessContextEnumValue,
                    updateIfThreadInMixedHostingMode: updateIfThreadInMixedHostingMode,
                    updateIfWindowIsSystemAwareOrUnaware: true,
                    hWnd: hWnd)
            {
            }

            private DpiAwarenessScope(
                DpiAwarenessContextValue dpiAwarenessContextValue,
                bool updateIfThreadInMixedHostingMode,
                bool updateIfWindowIsSystemAwareOrUnaware,
                IntPtr hWnd)
            {
                if (dpiAwarenessContextValue == DpiAwarenessContextValue.Invalid)
                {
                    return;
                }

                if (!OperationSupported)
                {
                    return;
                }

                if (updateIfThreadInMixedHostingMode && !this.IsThreadInMixedHostingBehavior)
                {
                    return;
                }

                if (updateIfWindowIsSystemAwareOrUnaware &&
                    (hWnd == IntPtr.Zero || !this.IsWindowUnawareOrSystemAware(hWnd)))
                {
                    return;
                }

                try
                {
                    this.OldDpiAwarenessContext =
                        UnsafeNativeMethods.SetThreadDpiAwarenessContext(
                            new DpiAwarenessContextHandle(dpiAwarenessContextValue));
                }
                catch (Exception e) when (e is EntryPointNotFoundException || e is MissingMethodException || e is DllNotFoundException)
                {
                    OperationSupported = false;
                }
            }

            private static bool OperationSupported { get; set; } = true;

            private bool IsThreadInMixedHostingBehavior =>
                SafeNativeMethods.GetThreadDpiHostingBehavior() == NativeMethods.DPI_HOSTING_BEHAVIOR.DPI_HOSTING_BEHAVIOR_MIXED;

            private DpiAwarenessContextHandle OldDpiAwarenessContext { get; set; } = null;

            public void Dispose()
            {
                if (this.OldDpiAwarenessContext != null)
                {
                    UnsafeNativeMethods.SetThreadDpiAwarenessContext(this.OldDpiAwarenessContext);
                    this.OldDpiAwarenessContext = null;
                }
            }

            private bool IsWindowUnawareOrSystemAware(IntPtr hWnd)
            {
                var dpiAwarenessContext = GetDpiAwarenessContext(hWnd);

                return
                    dpiAwarenessContext.Equals(DpiAwarenessContextValue.Unaware) ||
                    dpiAwarenessContext.Equals(DpiAwarenessContextValue.SystemAware);
            }
        }
    }
}

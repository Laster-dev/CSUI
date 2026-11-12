// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// Adapted for zero-DllImport / NativeAOT (DirectX + NativeLibrary + delegate*).

#nullable disable

using System;
using MS.Win32;

namespace MS.Internal
{
    internal static class Utilities
    {
        private static readonly Version _osVersion = Environment.OSVersion.Version;

        internal static bool IsOSVistaOrNewer => _osVersion >= new Version(6, 0);
        internal static bool IsOSWindows7OrNewer => _osVersion >= new Version(6, 1);
        internal static bool IsOSWindows8OrNewer => _osVersion >= new Version(6, 2);

        internal static bool IsCompositionEnabled => IsOSVistaOrNewer && SafeNativeMethods.IsDwmCompositionEnabled();

        internal static void SafeDispose<T>(ref T disposable) where T : IDisposable
        {
            IDisposable t = disposable;
            disposable = default(T);
            t?.Dispose();
        }
    }
}

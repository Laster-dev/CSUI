// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

namespace MS.Internal
{
    /// <summary>
    /// NativeAOT zero-COM TSF stub; IME support routes via Win32 IMM32 APIs and WM_IME_* / WM_CHAR messages.
    /// </summary>
    internal sealed class TextServicesLoader
    {
        private TextServicesLoader() { }

        internal static bool ServicesInstalled => false;
    }
}

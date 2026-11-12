// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System;

namespace System.Windows.Interop
{
    /// <summary>
    /// Assists interoperation between WPF and Win32 code by exposing Window HWND handles.
    /// </summary>
    public sealed class WindowInteropHelper
    {
        public WindowInteropHelper(Window window)
        {
            ArgumentNullException.ThrowIfNull(window);
            _window = window;
        }

        public IntPtr Handle => _window.Handle;

        internal IntPtr CriticalHandle => _window.Handle;

        public IntPtr Owner
        {
            get => _window.OwnerHandle;
            set => _window.OwnerHandle = value;
        }

        public IntPtr EnsureHandle()
        {
            if (CriticalHandle == IntPtr.Zero)
            {
                _window.CreateSourceWindow(false /* duringShow */);
            }

            return CriticalHandle;
        }

        private readonly Window _window;
    }
}

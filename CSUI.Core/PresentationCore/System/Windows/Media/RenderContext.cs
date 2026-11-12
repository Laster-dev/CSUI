// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

namespace System.Windows.Media
{
    /// <summary>
    /// Tracks rendering context state (backed by Direct2D ID2D1DeviceContext) during a retained visual tree walk.
    /// </summary>
    internal sealed unsafe class RenderContext
    {
        private void* _d2dDeviceContext;

        internal RenderContext()
        {
        }

        internal void Initialize(void* d2dDeviceContext)
        {
            _d2dDeviceContext = d2dDeviceContext;
        }

        internal void Reset()
        {
            _d2dDeviceContext = null;
        }

        internal void* D2dDeviceContext => _d2dDeviceContext;
    }
}

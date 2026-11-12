// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Windows.Interop
{
    /// <summary>
    /// Internal render mode flags used for visual target initialization.
    /// </summary>
    internal enum RenderingMode
    {
        Default = 0,
        Software = 1,
        Hardware = 2,
        HardwareReference = 3,
        DisableMultimonDisplayClipping = 4,
        IsDisableMultimonDisplayClippingValid = 8,
        DisableDirtyRectangles = 16,
    }

    /// <summary>
    ///     Render mode preference.
    /// </summary>
    public enum RenderMode
    {
        /// <summary>
        /// The rendering layer should use the GPU and CPU as appropriate.
        /// </summary>
        Default,

        /// <summary>
        /// The rendering layer should only use the CPU.
        /// </summary>
        SoftwareOnly
    }
}

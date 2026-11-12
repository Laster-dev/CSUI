#nullable disable
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Description: Orientation types



namespace System.Windows.Automation
{
    /// <summary>
    /// Used to represent Orientation property enumerated types.
    /// </summary>
#if (INTERNAL_COMPILE)
    internal enum OrientationType
#else
    public enum OrientationType
#endif
    {
        /// <summary>No orientation.</summary>
        None,
        /// <summary>Horizontally oriented.</summary>
        Horizontal,
        /// <summary>Vertically oriented.</summary>
        Vertical,
    }
}

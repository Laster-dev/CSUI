#nullable disable
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Description: Dock pattern provider interface


namespace System.Windows.Automation.Provider
{
    /// <summary>
    /// Expose an element's ability to change its dock state at run time.
    /// </summary>
#if (NO_INTERNAL_COMPILE_BUG1080665)
    internal interface IDockProvider
#else
    public interface IDockProvider
#endif
    {
        /// <summary>
        /// Moves the window to be docked at the requested location.
        /// </summary>
        void SetDockPosition( DockPosition dockPosition);

        /// <summary>Is the DockPosition Top, Left, Right, Bottom, Fill, or None?</summary>
        DockPosition DockPosition
        {
            get;
        }

    }
}

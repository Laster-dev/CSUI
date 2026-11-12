#nullable disable
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Description: Toggle pattern provider interface


namespace System.Windows.Automation.Provider
{
    /// <summary>
    /// public interface that represents UI elements that have a multistate (toggleable) value.
    /// </summary>
#if (NO_INTERNAL_COMPILE_BUG1080665)
    internal interface IToggleProvider
#else
    public interface IToggleProvider
#endif
    {
        /// <summary>
        /// Request to change the state that this UI element is currently representing
        /// </summary>
        void Toggle( );


        /// <summary>Value of a toggleable control, as a ToggleState enum</summary>
        ToggleState ToggleState
        {
            get;
        }

    }
}

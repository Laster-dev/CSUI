// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Windows.Controls
{
    /// <summary>
    /// Specifies when the Click event of a button should be raised.
    /// </summary>
    public enum ClickMode
    {
        /// <summary>
        /// Specifies that the Click event should be raised when the button is clicked and released.
        /// </summary>
        Release = 0,

        /// <summary>
        /// Specifies that the Click event should be raised as soon as the button is pressed.
        /// </summary>
        Press = 1,

        /// <summary>
        /// Specifies that the Click event should be raised when the mouse hovers over the button.
        /// </summary>
        Hover = 2
    }
}

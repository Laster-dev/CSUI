// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Windows.Controls
{
    /// <summary>
    /// Specifies the visibility of a ScrollBar for scrollable content.
    /// </summary>
    public enum ScrollBarVisibility
    {
        /// <summary>
        /// A ScrollBar does not appear even when the display cannot show all of the content.
        /// The dimension does not scroll.
        /// </summary>
        Disabled = 0,

        /// <summary>
        /// A ScrollBar appears and the dimension is scrollable when the viewport cannot display
        /// all of the content.
        /// </summary>
        Auto = 1,

        /// <summary>
        /// A ScrollBar does not appear even when the display cannot show all of the content.
        /// The dimension is still scrollable programmatically or via touch/wheel.
        /// </summary>
        Hidden = 2,

        /// <summary>
        /// A ScrollBar always appears even when the viewport displays all of the content.
        /// </summary>
        Visible = 3
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Windows.Controls
{
    /// <summary>
    /// Describes how scaling applies to content and restricts scaling to named axis types.
    /// </summary>
    public enum StretchDirection
    {
        /// <summary>
        /// Only scales the content upward when the content is smaller than the parent.
        /// If the content is larger, no scaling down is performed.
        /// </summary>
        UpOnly,

        /// <summary>
        /// Only scales the content downward when the content is larger than the parent.
        /// If the content is smaller, no scaling up is performed.
        /// </summary>
        DownOnly,

        /// <summary>
        /// Always stretches to fit according to the Stretch mode.
        /// </summary>
        Both
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

namespace System.Windows.Media.Animation
{
    /// <summary>
    /// Represents an object that can provide linear time values as a TimeSpan.
    /// </summary>
    internal interface IClock
    {
        /// <summary>
        /// Gets the current time.
        /// </summary>
        TimeSpan CurrentTime
        {
            get;
        }
    }
}

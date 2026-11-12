// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

namespace System.Windows.Media.Animation
{
    /// <summary>
    /// Specifies the behavior of the <see cref="ClockController.Seek"/>
    /// method by defining the meaning of that method's offset parameter.
    /// </summary>
    public enum TimeSeekOrigin
    {
        /// <summary>
        /// The offset parameter specifies the new position of the timeline as a
        /// distance from time t=0
        /// </summary>
        BeginTime,

        /// <summary>
        /// The offset parameter specifies the new position of the timeline as a
        /// distance from the end of the simple duration. If the duration is not
        /// defined, this causes the method call to have no effect.
        /// </summary>
        Duration
    }

    /// <summary>
    /// Possible states the time manager may be in.
    /// </summary>
    internal enum TimeState
    {
        /// <summary>
        /// Time is stopped.
        /// </summary>
        Stopped,

        /// <summary>
        /// Time is paused.
        /// </summary>
        Paused,

        /// <summary>
        /// Time is running.
        /// </summary>
        Running,
    }
}

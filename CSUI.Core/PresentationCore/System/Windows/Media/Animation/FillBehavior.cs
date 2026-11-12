// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System.Windows.Media.Animation;

namespace System.Windows.Media.Animation
{
    /// <summary>
    /// The FillBehavior enumeration is used to indicate how a Timeline will behave
    /// when it is outside of its active period but its parent is inside its
    /// active period.
    /// </summary>
    public enum FillBehavior
    {
        /// <summary>
        /// Indicates that a Timeline will hold its progress between the period of
        /// time between the end of its active period and the end of its parents active and
        /// hold periods.
        /// </summary>
        HoldEnd,

        /// <summary>
        /// Indicates that a Timeline will stop if it's outside its active
        /// period while its parent is inside its active period.
        /// </summary>
        Stop,
    }
}

namespace MS.Internal
{
    internal static partial class TimeEnumHelper
    {
        private const int c_maxFillBehavior = (int)FillBehavior.Stop;

        internal static bool IsValidFillBehavior(FillBehavior value)
        {
            return (0 <= value && (int)value <= c_maxFillBehavior);
        }
    }
}

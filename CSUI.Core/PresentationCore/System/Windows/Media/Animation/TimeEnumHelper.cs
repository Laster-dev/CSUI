// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System.Windows.Media.Animation;

namespace MS.Internal
{
    /// <summary>
    /// A class for validating enumerated types.
    /// </summary>
    internal static partial class TimeEnumHelper
    {
        // IMPORTANT: These values must be kept current with enum definitions for validation to work

        // Enums declared in Enums.cs
        private const int _maxTimeSeekOrigin = (int)TimeSeekOrigin.Duration;

        // Enums declared in PathAnimationSource.cs
        private const byte _maxPathAnimationSource = (int)PathAnimationSource.Angle;

        internal static bool IsValidTimeSeekOrigin(TimeSeekOrigin value)
        {
            return (0 <= value && (int)value <= _maxTimeSeekOrigin);
        }

        internal static bool IsValidPathAnimationSource(PathAnimationSource value)
        {
            return (0 <= value && (byte)value <= _maxPathAnimationSource);
        }
    }
}

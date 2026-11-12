// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System.Windows.Media.Animation;

namespace System.Windows.Media
{
    /// <summary>
    /// The class definition for TextEffect
    /// </summary>
    public partial class TextEffect : Animatable
    {
        public TextEffect(
            Transform transform,
            Brush foreground,
            Geometry clip,
            int positionStart,
            int positionCount
            )
        {
            ArgumentOutOfRangeException.ThrowIfNegative(positionCount);

            Transform     = transform;
            Foreground    = foreground;
            Clip          = clip;
            PositionStart = positionStart;
            PositionCount = positionCount;
        }

        public TextEffect()
        {
        }

        private static bool OnPositionStartChanging(int value)
        {
            return (value >= 0);
        }

        private static bool OnPositionCountChanging(int value)
        {
            return (value >= 0);
        }
    }
}

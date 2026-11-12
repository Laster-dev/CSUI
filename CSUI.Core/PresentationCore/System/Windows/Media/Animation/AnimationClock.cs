// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

namespace System.Windows.Media.Animation
{
    public class AnimationClock : Clock
    {
        protected internal AnimationClock(AnimationTimeline animation)
            : base(animation)
        {
        }

        public new AnimationTimeline Timeline => (AnimationTimeline)base.Timeline;

        public object GetCurrentValue(object defaultOriginValue, object defaultDestinationValue)
        {
            return ((AnimationTimeline)base.Timeline).GetCurrentValue(defaultOriginValue, defaultDestinationValue, this);
        }

        internal override bool NeedsTicksWhenActive => true;
    }
}

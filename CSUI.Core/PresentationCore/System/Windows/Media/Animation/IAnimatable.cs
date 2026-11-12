// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

namespace System.Windows.Media.Animation
{
    /// <summary>
    /// This interface is supported by DependencyObjects whose properties are
    /// animatable.
    /// </summary>
    public interface IAnimatable
    {
        void ApplyAnimationClock(DependencyProperty dp, AnimationClock clock);

        void ApplyAnimationClock(DependencyProperty dp, AnimationClock clock, HandoffBehavior handoffBehavior);

        void BeginAnimation(DependencyProperty dp, AnimationTimeline animation);

        void BeginAnimation(DependencyProperty dp, AnimationTimeline animation, HandoffBehavior handoffBehavior);

        bool HasAnimatedProperties { get; }

        object GetAnimationBaseValue(DependencyProperty dp);
    }
}

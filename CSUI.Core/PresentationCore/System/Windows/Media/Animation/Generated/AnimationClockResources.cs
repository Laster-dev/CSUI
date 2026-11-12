// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

namespace System.Windows.Media.Animation
{
    internal class AnimationClockResource<T> : AnimationClockResource
    {
        protected AnimationClockResource(T baseValue, AnimationClock animationClock)
            : base(animationClock)
        {
            _baseValue = baseValue;
        }

        public T BaseValue => _baseValue;

        protected T _baseValue;
    }

    internal sealed class DoubleAnimationClockResource : AnimationClockResource<double>
    {
        public DoubleAnimationClockResource(double baseValue, AnimationClock animationClock)
            : base(baseValue, animationClock)
        {
        }

        public double CurrentValue => _animationClock != null
            ? ((DoubleAnimationBase)_animationClock.Timeline).GetCurrentValue(_baseValue, _baseValue, _animationClock)
            : _baseValue;
    }

    internal sealed class PointAnimationClockResource : AnimationClockResource<Point>
    {
        public PointAnimationClockResource(Point baseValue, AnimationClock animationClock)
            : base(baseValue, animationClock)
        {
        }

        public Point CurrentValue => _animationClock != null
            ? ((PointAnimationBase)_animationClock.Timeline).GetCurrentValue(_baseValue, _baseValue, _animationClock)
            : _baseValue;
    }

    internal sealed class SizeAnimationClockResource : AnimationClockResource<Size>
    {
        public SizeAnimationClockResource(Size baseValue, AnimationClock animationClock)
            : base(baseValue, animationClock)
        {
        }

        public Size CurrentValue => _animationClock != null
            ? ((SizeAnimationBase)_animationClock.Timeline).GetCurrentValue(_baseValue, _baseValue, _animationClock)
            : _baseValue;
    }

    internal sealed class RectAnimationClockResource : AnimationClockResource<Rect>
    {
        public RectAnimationClockResource(Rect baseValue, AnimationClock animationClock)
            : base(baseValue, animationClock)
        {
        }

        public Rect CurrentValue => _animationClock != null
            ? ((RectAnimationBase)_animationClock.Timeline).GetCurrentValue(_baseValue, _baseValue, _animationClock)
            : _baseValue;
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

namespace System.Windows.Media.Animation
{
    /// <summary>
    /// This is a wrapped exception designed to be thrown when we encounter an exception in
    /// the process of animating.
    /// </summary>
    public sealed class AnimationException : SystemException
    {
        internal AnimationException(
            AnimationClock clock, DependencyProperty property, IAnimatable target,
            string message, Exception innerException)
            : base(message, innerException)
        {
            _clock = clock;
            _property = property;
            _targetElement = target;
        }

        public AnimationClock Clock => _clock;

        public DependencyProperty Property => _property;

        public IAnimatable Target => _targetElement;

        private AnimationClock _clock;
        private DependencyProperty _property;
        private IAnimatable _targetElement;
    }
}

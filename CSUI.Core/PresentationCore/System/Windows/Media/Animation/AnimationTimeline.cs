// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

namespace System.Windows.Media.Animation
{
    public abstract class AnimationTimeline : Timeline
    {
        protected AnimationTimeline()
            : base()
        {
        }

        private static void AnimationTimeline_PropertyChangedFunction(
            DependencyObject d,
            DependencyPropertyChangedEventArgs e)
        {
            ((AnimationTimeline)d).PropertyChanged(e.Property);
        }

        public static readonly DependencyProperty IsAdditiveProperty =
            DependencyProperty.Register(
                "IsAdditive",
                typeof(bool),
                typeof(AnimationTimeline),
                new PropertyMetadata(
                    false,
                    new PropertyChangedCallback(AnimationTimeline_PropertyChangedFunction)));

        public static readonly DependencyProperty IsCumulativeProperty =
            DependencyProperty.Register(
                "IsCumulative",
                typeof(bool),
                typeof(AnimationTimeline),
                new PropertyMetadata(
                    false,
                    new PropertyChangedCallback(AnimationTimeline_PropertyChangedFunction)));

        public new AnimationTimeline Clone() => (AnimationTimeline)base.Clone();

        protected internal override Clock AllocateClock()
        {
            return new AnimationClock(this);
        }

        public new AnimationClock CreateClock() => (AnimationClock)base.CreateClock();

        public virtual object GetCurrentValue(
            object defaultOriginValue,
            object defaultDestinationValue,
            AnimationClock animationClock)
        {
            ReadPreamble();

            return defaultDestinationValue;
        }

        protected override Duration GetNaturalDurationCore(Clock clock)
        {
            return new TimeSpan(0, 0, 1);
        }

        public abstract Type TargetPropertyType { get; }

        public virtual bool IsDestinationDefault
        {
            get
            {
                ReadPreamble();

                return false;
            }
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using MS.Internal;
using System.ComponentModel;

namespace System.Windows.Media.Animation
{
    public partial class ParallelTimeline : TimelineGroup
    {
        public ParallelTimeline()
            : base()
        {
        }

        public ParallelTimeline(TimeSpan? beginTime)
            : base(beginTime)
        {
        }

        public ParallelTimeline(TimeSpan? beginTime, Duration duration)
            : base(beginTime, duration)
        {
        }

        public ParallelTimeline(TimeSpan? beginTime, Duration duration, RepeatBehavior repeatBehavior)
            : base(beginTime, duration, repeatBehavior)
        {
        }

        protected override Duration GetNaturalDurationCore(Clock clock)
        {
            Duration simpleDuration = TimeSpan.Zero;

            ClockGroup clockGroup = clock as ClockGroup;

            if (clockGroup != null)
            {
                List<Clock> children = clockGroup.InternalChildren;

                if (children != null)
                {
                    bool hasChildWithUnresolvedDuration = false;

                    for (int childIndex = 0; childIndex < children.Count; childIndex++)
                    {
                        Duration childEndOfActivePeriod = children[childIndex].EndOfActivePeriod;

                        if (childEndOfActivePeriod == Duration.Forever)
                        {
                            return Duration.Forever;
                        }
                        else if (childEndOfActivePeriod == Duration.Automatic)
                        {
                            hasChildWithUnresolvedDuration = true;
                        }
                        else if (childEndOfActivePeriod > simpleDuration)
                        {
                            simpleDuration = childEndOfActivePeriod;
                        }
                    }

                    if (hasChildWithUnresolvedDuration)
                    {
                        return Duration.Automatic;
                    }
                }
            }

            return simpleDuration;
        }

        public static readonly DependencyProperty SlipBehaviorProperty =
            DependencyProperty.Register(
                "SlipBehavior",
                typeof(SlipBehavior),
                typeof(ParallelTimeline),
                new PropertyMetadata(
                    SlipBehavior.Grow,
                    new PropertyChangedCallback(ParallelTimeline_PropertyChangedFunction)),
                new ValidateValueCallback(ValidateSlipBehavior));

        private static bool ValidateSlipBehavior(object value)
        {
            return TimeEnumHelper.IsValidSlipBehavior((SlipBehavior)value);
        }

        [DefaultValue(SlipBehavior.Grow)]
        public SlipBehavior SlipBehavior
        {
            get => (SlipBehavior)GetValue(SlipBehaviorProperty);
            set => SetValue(SlipBehaviorProperty, value);
        }

        internal static void ParallelTimeline_PropertyChangedFunction(
            DependencyObject d,
            DependencyPropertyChangedEventArgs e)
        {
            ((ParallelTimeline)d).PropertyChanged(e.Property);
        }
    }
}

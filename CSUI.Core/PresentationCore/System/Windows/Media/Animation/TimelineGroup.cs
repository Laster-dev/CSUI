// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System.ComponentModel;
using System.Windows.Markup;
using SR = MS.Internal.PresentationCore.SR;

namespace System.Windows.Media.Animation
{
    [ContentProperty("Children")]
    public abstract partial class TimelineGroup : Timeline, IAddChild
    {
        protected TimelineGroup()
            : base()
        {
        }

        protected TimelineGroup(TimeSpan? beginTime)
            : base(beginTime)
        {
        }

        protected TimelineGroup(TimeSpan? beginTime, Duration duration)
            : base(beginTime, duration)
        {
        }

        protected TimelineGroup(TimeSpan? beginTime, Duration duration, RepeatBehavior repeatBehavior)
            : base(beginTime, duration, repeatBehavior)
        {
        }

        protected internal override Clock AllocateClock()
        {
            return new ClockGroup(this);
        }

        public new ClockGroup CreateClock()
        {
            return (ClockGroup)base.CreateClock();
        }

        void IAddChild.AddChild(object child)
        {
            WritePreamble();

            ArgumentNullException.ThrowIfNull(child);

            AddChild(child);

            WritePostscript();
        }

        [EditorBrowsable(EditorBrowsableState.Advanced)]
        protected virtual void AddChild(object child)
        {
            Timeline timelineChild = child as Timeline;

            if (timelineChild == null)
            {
                throw new ArgumentException(SR.Timing_ChildMustBeTimeline, nameof(child));
            }
            else
            {
                Children.Add(timelineChild);
            }
        }

        void IAddChild.AddText(string childText)
        {
            WritePreamble();

            ArgumentNullException.ThrowIfNull(childText);

            AddText(childText);

            WritePostscript();
        }

        [EditorBrowsable(EditorBrowsableState.Advanced)]
        protected virtual void AddText(string childText)
        {
            throw new InvalidOperationException(SR.Timing_NoTextChildren);
        }
    }
}

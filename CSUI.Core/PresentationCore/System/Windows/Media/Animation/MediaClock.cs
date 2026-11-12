// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System;

namespace System.Windows.Media.Animation
{
    public class MediaTimeline : Timeline
    {
        public static readonly DependencyProperty SourceProperty =
            DependencyProperty.Register(
                nameof(Source),
                typeof(Uri),
                typeof(MediaTimeline),
                new FrameworkPropertyMetadata(null));

        public MediaTimeline()
        {
        }

        public MediaTimeline(Uri source)
        {
            Source = source;
        }

        public MediaTimeline(TimeSpan? beginTime) : base(beginTime)
        {
        }

        public MediaTimeline(TimeSpan? beginTime, Duration duration) : base(beginTime, duration)
        {
        }

        public MediaTimeline(TimeSpan? beginTime, Duration duration, RepeatBehavior repeatBehavior)
            : base(beginTime, duration, repeatBehavior)
        {
        }

        public Uri Source
        {
            get => (Uri)GetValue(SourceProperty);
            set => SetValue(SourceProperty, value);
        }

        protected override Freezable CreateInstanceCore() => new MediaTimeline();

        public new MediaClock CreateClock() => (MediaClock)base.CreateClock();

        protected internal override Clock AllocateClock() => new MediaClock(this);
    }

    public class MediaClock : Clock
    {
        protected internal MediaClock(MediaTimeline media) : base(media)
        {
        }

        public new MediaTimeline Timeline => (MediaTimeline)base.Timeline;
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

namespace System.Windows.Media.Animation
{
    public partial class ParallelTimeline : TimelineGroup
    {
        public new ParallelTimeline Clone() => (ParallelTimeline)base.Clone();

        public new ParallelTimeline CloneCurrentValue() => (ParallelTimeline)base.CloneCurrentValue();

        protected override Freezable CreateInstanceCore()
        {
            return new ParallelTimeline();
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using MS.Internal;

namespace System.Windows.Media.Animation
{
    public abstract partial class TimelineGroup : Timeline
    {
        public new TimelineGroup Clone() => (TimelineGroup)base.Clone();

        public new TimelineGroup CloneCurrentValue() => (TimelineGroup)base.CloneCurrentValue();

        public TimelineCollection Children
        {
            get => (TimelineCollection)GetValue(ChildrenProperty);
            set => SetValueInternal(ChildrenProperty, value);
        }

        public static readonly DependencyProperty ChildrenProperty;

        internal static TimelineCollection s_Children = TimelineCollection.Empty;

        static TimelineGroup()
        {
            Debug.Assert(s_Children == null || s_Children.IsFrozen,
                "Detected context bound default value TimelineGroup.s_Children.");

            Type typeofThis = typeof(TimelineGroup);
            ChildrenProperty =
                RegisterProperty(
                    "Children",
                    typeof(TimelineCollection),
                    typeofThis,
                    new FreezableDefaultValueFactory(TimelineCollection.Empty),
                    null,
                    null,
                    false,
                    null);
        }
    }
}

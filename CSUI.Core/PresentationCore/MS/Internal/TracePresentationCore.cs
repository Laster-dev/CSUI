// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System;
using System.Diagnostics;

namespace MS.Internal
{
    internal class AvTraceDetails
    {
        public AvTraceDetails(int id, string[] labels)
        {
            Id = id;
            Labels = labels;
        }

        public int Id { get; }
        public string[] Labels { get; }
        public virtual string Message => Labels != null && Labels.Length > 0 ? Labels[0] : string.Empty;
    }

    internal static partial class TraceRoutedEvent
    {
        public static AvTraceDetails RaiseEvent { get; } = new AvTraceDetails(1, new[] { "Raise RoutedEvent" });
        public static AvTraceDetails ReRaiseEventAs { get; } = new AvTraceDetails(2, new[] { "Raise RoutedEvent" });
        public static AvTraceDetails HandleEvent { get; } = new AvTraceDetails(3, new[] { "RoutedEvent has set Handled" });
        public static AvTraceDetails InvokeHandlers { get; } = new AvTraceDetails(4, new[] { "InvokeHandlers" });

        public static bool IsEnabled => false;
        public static bool IsEnabledOverride => false;
        public static void Refresh() { }
        public static void Trace(TraceEventType type, AvTraceDetails traceDetails, params ReadOnlySpan<object> parameters) { }
        public static void TraceActivityItem(AvTraceDetails traceDetails, params ReadOnlySpan<object> parameters) { }
    }

    internal static partial class TraceAnimation
    {
        public static AvTraceDetails StoryboardBegin { get; } = new AvTraceDetails(1, new[] { "Storyboard has begun" });
        public static AvTraceDetails StoryboardPause { get; } = new AvTraceDetails(2, new[] { "Storyboard has been paused" });
        public static AvTraceDetails StoryboardRemove { get; } = new AvTraceDetails(3, new[] { "Storyboard has been removed" });
        public static AvTraceDetails StoryboardResume { get; } = new AvTraceDetails(4, new[] { "Storyboard has been resumed" });
        public static AvTraceDetails StoryboardStop { get; } = new AvTraceDetails(5, new[] { "Storyboard has been stopped" });
        public static AvTraceDetails StoryboardNotApplied { get; } = new AvTraceDetails(6, new[] { "Unable to perform action because the specified Storyboard was never applied to this object for interactive control." });
        public static AvTraceDetails AnimateStorageValidationFailed { get; } = new AvTraceDetails(7, new[] { "Animated property failed validation. Animated value not set." });
        public static AvTraceDetails AnimateStorageValidationNoLongerFailing { get; } = new AvTraceDetails(8, new[] { "Animated property no longer failing validation." });

        public static bool IsEnabled => false;
        public static bool IsEnabledOverride => false;
        public static void Refresh() { }
        public static void Trace(TraceEventType type, AvTraceDetails traceDetails, params ReadOnlySpan<object> parameters) { }
        public static void TraceActivityItem(AvTraceDetails traceDetails, params ReadOnlySpan<object> parameters) { }
    }
}

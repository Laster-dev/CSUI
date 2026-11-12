// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

namespace MS.Internal
{
    internal static class TraceDependencyProperty
    {
        internal static bool IsEnabled => false;
        internal static object Register => null;
        internal static object UpdateEffectiveValueStart => null;
        internal static object UpdateEffectiveValueStop => null;
        internal static object ApplyTemplateContent => null;
        internal static void Trace(object details, params object[] args) { }
        internal static void TraceActivityItem(object details, params object[] args) { }
    }

    internal static class TraceFreezable
    {
        internal static bool IsEnabled => false;
        internal static object UnableToFreezeDispatcherObjectWithThreadAffinity => null;
        internal static object UnableToFreezeFreezableSubProperty => null;
        internal static object UnableToFreezeExpression => null;
        internal static object UnableToFreezeAnimatedProperties => null;
        internal static void Trace(object eventType, object details, params object[] args) { }
        internal static void TraceActivityItem(object details, params object[] args) { }
    }

    internal static class TraceNameScope
    {
        internal static bool IsEnabled => false;
        internal static object RegisterName => null;
        internal static object UnregisterName => null;
        internal static void TraceActivityItem(object details, params object[] args) { }
    }
}

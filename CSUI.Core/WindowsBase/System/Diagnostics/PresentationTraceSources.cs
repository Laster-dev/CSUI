// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System.Runtime.CompilerServices;
using System.Windows;

namespace System.Diagnostics
{
    public enum PresentationTraceLevel
    {
        None,
        Low,
        Medium,
        High,
    }

    public static partial class PresentationTraceSources
    {
        public static readonly DependencyProperty TraceLevelProperty =
            DependencyProperty.RegisterAttached(
                "TraceLevel",
                typeof(PresentationTraceLevel),
                typeof(PresentationTraceSources),
                new PropertyMetadata(PresentationTraceLevel.None));

        private static readonly ConditionalWeakTable<object, StrongBox<PresentationTraceLevel>> s_traceLevels = new();

        public static PresentationTraceLevel GetTraceLevel(object element)
        {
            if (element != null && s_traceLevels.TryGetValue(element, out StrongBox<PresentationTraceLevel> box))
            {
                return box.Value;
            }

            return PresentationTraceLevel.None;
        }

        public static void SetTraceLevel(object element, PresentationTraceLevel traceLevel)
        {
            if (element != null)
            {
                s_traceLevels.AddOrUpdate(element, new StrongBox<PresentationTraceLevel>(traceLevel));
            }
        }

        internal static object DataBindingSource => _DataBindingSource;
        internal static object _DataBindingSource;

        internal static object DocumentsSource => _DocumentsSource;
        internal static object _DocumentsSource;

        internal static object HwndHostSource => _HwndHostSource;
        internal static object _HwndHostSource;

        internal static object MarkupSource => _MarkupSource;
        internal static object _MarkupSource;

        internal static object ResourceDictionarySource => _ResourceDictionarySource;
        internal static object _ResourceDictionarySource;

        internal static object ShellSource => _ShellSource;
        internal static object _ShellSource;

        public static void Refresh() { }
    }
}

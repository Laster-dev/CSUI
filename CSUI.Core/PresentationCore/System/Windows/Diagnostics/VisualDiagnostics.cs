// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System.Runtime.CompilerServices;
using System.Windows.Media;
using MS.Internal;
using MS.Internal.PresentationCore;

namespace System.Windows.Diagnostics
{
    public static class VisualDiagnostics
    {
        private static readonly bool s_IsEnabled = !CoreAppContextSwitches.DisableDiagnostics;
        private static event EventHandler<VisualTreeChangeEventArgs> s_visualTreeChanged;
        private static bool s_HasVisualTreeChangedListeners;
        private static bool s_IsVisualTreeChangedInProgress;
        private static object s_ActivePresentationSource;
        private static readonly ConditionalWeakTable<object, XamlSourceInfo> s_xamlSourceInfo = new();

        public static event EventHandler<VisualTreeChangeEventArgs> VisualTreeChanged
        {
            add
            {
                if (EnableHelper.IsVisualTreeChangeEnabled)
                {
                    s_visualTreeChanged += value;
                    s_HasVisualTreeChangedListeners = true;
                }
            }
            remove
            {
                s_visualTreeChanged -= value;
                s_HasVisualTreeChangedListeners = s_visualTreeChanged != null;
            }
        }

        public static void EnableVisualTreeChanged()
        {
            EnableHelper.EnableVisualTreeChanged();
        }

        public static void DisableVisualTreeChanged()
        {
            EnableHelper.DisableVisualTreeChanged();
        }

        public static XamlSourceInfo GetXamlSourceInfo(object obj)
        {
            if (obj == null)
            {
                return null;
            }

            s_xamlSourceInfo.TryGetValue(obj, out XamlSourceInfo info);
            return info;
        }

        internal static void SetXamlSourceInfo(object obj, XamlSourceInfo info)
        {
            if (obj != null && info != null)
            {
                s_xamlSourceInfo.AddOrUpdate(obj, info);
            }
        }

        internal static void OnVisualChildChanged(DependencyObject parent, DependencyObject child, bool isAdded)
        {
            EventHandler<VisualTreeChangeEventArgs> visualTreeChanged = s_visualTreeChanged;
            if (visualTreeChanged != null && EnableHelper.IsVisualTreeChangeEnabled)
            {
                int index;
                VisualTreeChangeType changeType;
                if (isAdded)
                {
                    index = GetChildIndex(parent, child);
                    changeType = VisualTreeChangeType.Add;
                }
                else
                {
                    index = -1;
                    changeType = VisualTreeChangeType.Remove;
                }

                RaiseVisualTreeChangedEvent(visualTreeChanged, new VisualTreeChangeEventArgs(parent, child, index, changeType));
            }
        }

        private static void RaiseVisualTreeChangedEvent(
            EventHandler<VisualTreeChangeEventArgs> visualTreeChanged,
            VisualTreeChangeEventArgs args)
        {
            bool savedInProgress = s_IsVisualTreeChangedInProgress;
            object savedSource = s_ActivePresentationSource;
            try
            {
                s_IsVisualTreeChangedInProgress = true;
                if (args.ChangeType == VisualTreeChangeType.Add && VisualTreeHelper.GetParent(args.Parent) == null)
                {
                    s_ActivePresentationSource = Visual.PresentationSourceFromVisualHook?.Invoke(args.Parent);
                }

                visualTreeChanged(null, args);
            }
            finally
            {
                s_IsVisualTreeChangedInProgress = savedInProgress;
                s_ActivePresentationSource = savedSource;
            }
        }

        private static int GetChildIndex(DependencyObject parent, DependencyObject child)
        {
            int count = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < count; i++)
            {
                if (VisualTreeHelper.GetChild(parent, i) == child)
                {
                    return i;
                }
            }

            return -1;
        }

        internal static bool IsEnabled => s_IsEnabled;

        internal static void VerifyVisualTreeChange(DependencyObject d)
        {
            if (s_HasVisualTreeChangedListeners)
            {
                VerifyVisualTreeChangeCore(d);
            }
        }

        private static void VerifyVisualTreeChangeCore(DependencyObject d)
        {
            if (s_IsVisualTreeChangedInProgress)
            {
                if (!EnableHelper.AllowChangesDuringVisualTreeChanged(d))
                {
                    throw new InvalidOperationException(SR.Format(SR.ReentrantVisualTreeChangeError, nameof(VisualTreeChanged)));
                }
            }
        }

        internal static bool IsEnvironmentVariableSet(string value, string environmentVariable)
        {
            if (value != null)
            {
                return IsEnvironmentValueSet(value);
            }

            value = Environment.GetEnvironmentVariable(environmentVariable);
            return IsEnvironmentValueSet(value);
        }

        internal static bool IsEnvironmentValueSet(string value)
        {
            value = (value ?? string.Empty).Trim().ToLowerInvariant();
            return !(value == string.Empty || value == "0" || value == "false");
        }

        private static class EnableHelper
        {
            private const string c_enableVisualTreeNotificationsEnvironmentVariable = "ENABLE_XAML_DIAGNOSTICS_VISUAL_TREE_NOTIFICATIONS";
            private static readonly bool? s_IsEnableVisualTreeChangedAllowed = PrecomputeIsEnableVisualTreeChangedAllowed();
            private static bool s_IsVisualTreeChangedEnabled;
            private static bool? s_AllowChangesDuringVisualTreeChanged;

            internal static void EnableVisualTreeChanged()
            {
                if (!IsEnableVisualTreeChangedAllowed)
                {
                    throw new InvalidOperationException(SR.Format(SR.MethodCallNotAllowed, nameof(VisualDiagnostics.EnableVisualTreeChanged)));
                }

                s_IsVisualTreeChangedEnabled = true;
            }

            internal static void DisableVisualTreeChanged()
            {
                s_IsVisualTreeChangedEnabled = false;
            }

            internal static bool IsVisualTreeChangeEnabled =>
                IsEnabled && (s_IsVisualTreeChangedEnabled || System.Diagnostics.Debugger.IsAttached);

            internal static bool AllowChangesDuringVisualTreeChanged(DependencyObject d)
            {
                if (s_AllowChangesDuringVisualTreeChanged == null)
                {
                    if (IsChangePermitted(d))
                    {
                        return true;
                    }

                    s_AllowChangesDuringVisualTreeChanged = CoreAppContextSwitches.AllowChangesDuringVisualTreeChanged;

                    if (s_AllowChangesDuringVisualTreeChanged == true)
                    {
                        System.Diagnostics.Debug.WriteLine(SR.Format(SR.ReentrantVisualTreeChangeWarning, nameof(VisualTreeChanged)));
                    }

                    return s_AllowChangesDuringVisualTreeChanged == true;
                }

                return (s_AllowChangesDuringVisualTreeChanged == true) || IsChangePermitted(d);
            }

            private static bool IsChangePermitted(DependencyObject d)
            {
                return s_ActivePresentationSource != null
                    && d != null
                    && Visual.PresentationSourceFromVisualHook != null
                    && s_ActivePresentationSource != Visual.PresentationSourceFromVisualHook(d);
            }

            private static bool? PrecomputeIsEnableVisualTreeChangedAllowed()
            {
                if (!IsEnabled)
                {
                    return false;
                }

                if (IsEnvironmentVariableSet(null, c_enableVisualTreeNotificationsEnvironmentVariable))
                {
                    return true;
                }

                return null;
            }

            private static bool IsEnableVisualTreeChangedAllowed =>
                s_IsEnableVisualTreeChangedAllowed ?? System.Diagnostics.Debugger.IsAttached;
        }
    }
}

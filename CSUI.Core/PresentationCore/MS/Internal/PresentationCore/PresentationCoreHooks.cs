// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;

namespace MS.Internal.PresentationCore
{
    /// <summary>
    /// Wires forward-decoupling delegates on Visual and MediaContext once PresentationCore
    /// input, layout, PresentationSource, and HwndTarget infrastructure are present.
    /// </summary>
    internal static class PresentationCoreHooks
    {
        private sealed class CsuiFallbackComWrappers : System.Runtime.InteropServices.ComWrappers
        {
            protected override unsafe ComInterfaceEntry* ComputeVtables(
                object obj,
                System.Runtime.InteropServices.CreateComInterfaceFlags flags,
                out int count)
            {
                count = 0;
                return null;
            }

            protected override object CreateObject(
                IntPtr externalComObject,
                System.Runtime.InteropServices.CreateObjectFlags flags)
                => new object();

            protected override void ReleaseObjects(System.Collections.IEnumerable objects)
            {
            }
        }
#pragma warning disable CA2255
        [ModuleInitializer]
        internal static void Initialize()
#pragma warning restore CA2255
        {
            if (!RuntimeFeature.IsDynamicCodeSupported)
            {
                try
                {
                    System.Runtime.InteropServices.ComWrappers.RegisterForMarshalling(
                        new CsuiFallbackComWrappers());
                }
                catch (InvalidOperationException)
                {
                }
            }

            Visual.PropagateResumeLayoutHook = static (parent, child) =>
                UIElement.PropagateResumeLayout(parent, child);

            Visual.PropagateSuspendLayoutHook = static visual =>
                UIElement.PropagateSuspendLayout(visual);

            Visual.InvalidateForceInheritHook = static (visual, property) =>
                UIElement.InvalidateForceInheritPropertyOnChildren(visual, property);

            Visual.IsPerMonitorDpiAwareHook = static () =>
                HwndTarget.IsProcessPerMonitorDpiAware == true;

            Visual.IsPerMonitorDpiScalingEnabledHook = static () =>
                HwndTarget.IsPerMonitorDpiScalingEnabled;

            Visual.PresentationSourceFromVisualHook = static dependencyObject =>
                PresentationSource.CriticalFromVisual(dependencyObject);

            Visual.PointToScreenHook = static (point, source) =>
            {
                PresentationSource presentationSource = (PresentationSource)source;
                return PointUtil.ClientToScreen(
                    PointUtil.RootToClient(point, presentationSource),
                    presentationSource);
            };

            Visual.PointFromScreenHook = static (point, source) =>
            {
                PresentationSource presentationSource = (PresentationSource)source;
                return PointUtil.ClientToRoot(
                    PointUtil.ScreenToClient(point, presentationSource),
                    presentationSource);
            };

            Visual.ShouldAutoLayoutVisualHook = static (visual, internalTarget, _) =>
            {
                if (visual is UIElement element && !element.IsRootElement)
                {
                    DependencyObject parent = VisualTreeHelper.GetParent(element);
                    return parent == null
                        || parent is System.Windows.Media.Media3D.Viewport2DVisual3D
                        || (internalTarget != null && ReferenceEquals(parent, internalTarget));
                }

                return false;
            };

            Visual.SubscribeLayoutUpdatedHook = static (visual, handler) =>
            {
                if (visual is UIElement element && handler != null)
                {
                    element.LayoutUpdated += handler;
                }
            };

            Visual.UnsubscribeLayoutUpdatedHook = static (visual, handler) =>
            {
                if (visual is UIElement element && handler != null)
                {
                    element.LayoutUpdated -= handler;
                }
            };

            Visual.PerformAutoLayoutHook = static (visual, _) =>
            {
                if (visual is UIElement element)
                {
                    UIElement.PropagateResumeLayout(null, element);
                    element.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                    element.Arrange(new Rect(element.DesiredSize));
                }
            };

            Visual.NotifyHitTestInvalidatedHook = static () =>
                InputManager.SafeCurrentNotifyHitTestInvalidated();

            MediaContext.DefaultInvalidateInputDevicesHook = static () =>
                InputManager.UnsecureCurrent.InvalidateInputDevices();
        }
    }
}

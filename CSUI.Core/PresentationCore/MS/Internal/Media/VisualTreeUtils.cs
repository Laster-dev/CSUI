// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System.Diagnostics;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using MS.Internal.PresentationCore;

namespace MS.Internal.Media
{
    internal static class VisualTreeUtils
    {
        public const string BitmapEffectObsoleteMessage =
            "BitmapEffects are deprecated and no longer function.  Consider using Effects where appropriate instead.";

        internal static void PropagateFlags(
            DependencyObject element,
            VisualFlags flags,
            VisualProxyFlags proxyFlags)
        {
            if (element is Visual visual)
            {
                Visual.PropagateFlags(visual, flags, proxyFlags);
            }
            else if (element is Visual3D visual3D)
            {
                Visual3D.PropagateFlags(visual3D, flags, proxyFlags);
            }
        }

        internal static void SetFlagsToRoot(DependencyObject element, bool value, VisualFlags flags)
        {
            if (element is Visual visual)
            {
                visual.SetFlagsToRoot(value, flags);
            }
            else if (element is Visual3D visual3D)
            {
                visual3D.SetFlagsToRoot(value, flags);
            }
        }

        internal static DependencyObject FindFirstAncestorWithFlagsAnd(DependencyObject element, VisualFlags flags)
        {
            if (element is Visual visual)
            {
                return visual.FindFirstAncestorWithFlagsAnd(flags);
            }
            else if (element is Visual3D visual3D)
            {
                return visual3D.FindFirstAncestorWithFlagsAnd(flags);
            }

            return null;
        }

        internal static PointHitTestResult AsNearestPointHitTestResult(HitTestResult result)
        {
            if (result == null)
            {
                return null;
            }

            if (result is PointHitTestResult resultAsPoint)
            {
                return resultAsPoint;
            }

            Debug.Assert(false, string.Format("Unhandled HitTestResult type '{0}'", result.GetType().Name));
            return null;
        }

        internal static void EnsureNonNullVisual(DependencyObject element)
        {
            EnsureVisual(element, allowNull: false);
        }

        internal static void EnsureVisual(DependencyObject element)
        {
            EnsureVisual(element, allowNull: true);
        }

        internal static void EnsureVisual(DependencyObject element, bool allowNull = false)
        {
            if (element == null)
            {
                if (!allowNull)
                {
                    throw new ArgumentNullException(nameof(element));
                }

                return;
            }

            if (!(element is Visual || element is Visual3D))
            {
                throw new ArgumentException(SR.Visual_NotAVisual);
            }

            element.VerifyAccess();
        }

        internal static void AsNonNullVisual(DependencyObject element, out Visual visual)
        {
            ArgumentNullException.ThrowIfNull(element);
            AsVisual(element, out visual);
            Debug.Assert(visual != null);
        }

        internal static void AsNonNullVisual(DependencyObject element, out Visual visual, out Visual3D visual3D)
        {
            ArgumentNullException.ThrowIfNull(element);
            AsVisual(element, out visual, out visual3D);
            Debug.Assert((visual != null && visual3D == null) || (visual == null && visual3D != null));
        }

        internal static void AsVisual(DependencyObject element, out Visual visual)
        {
            bool castSucceeded = AsVisualHelper(element, out visual);

            if (element != null)
            {
                if (!castSucceeded)
                {
                    throw new InvalidOperationException(SR.Format(SR.Visual_NotAVisual, element.GetType()));
                }

                Debug.Assert(visual != null);
                element.VerifyAccess();
            }
        }

        internal static void AsVisual(DependencyObject element, out Visual visual, out Visual3D visual3D)
        {
            bool castSucceeded = AsVisualHelper(element, out visual, out visual3D);

            if (element != null)
            {
                if (!castSucceeded)
                {
                    throw new InvalidOperationException(SR.Format(SR.Visual_NotAVisual, element.GetType()));
                }

                Debug.Assert((visual != null && visual3D == null) || (visual == null && visual3D != null));
                element.VerifyAccess();
            }
        }

        internal static bool AsVisualInternal(DependencyObject element, out Visual visual)
        {
            bool castSucceeded = AsVisualHelper(element, out visual);

            if (!(castSucceeded || element == null))
            {
                Debug.Fail(string.Format(
                    "'{0}' is not a Visual. Caller is responsible for guaranteeing that element is a Visual type.",
                    element?.GetType()));
            }

            return castSucceeded;
        }

        internal static bool AsVisualInternal(DependencyObject element, out Visual visual, out Visual3D visual3D)
        {
            bool castSucceeded = AsVisualHelper(element, out visual, out visual3D);

            if (!(castSucceeded || element == null))
            {
                Debug.Fail(string.Format(
                    "'{0}' is not a Visual or Visual3D. Caller is responsible for guaranteeing that element is a Visual or Visual3D type.",
                    element?.GetType()));
            }

            return castSucceeded;
        }

        private static bool AsVisualHelper(DependencyObject element, out Visual visual)
        {
            if (element is Visual elementAsVisual)
            {
                visual = elementAsVisual;
                return true;
            }

            visual = null;
            return false;
        }

        private static bool AsVisualHelper(DependencyObject element, out Visual visual, out Visual3D visual3D)
        {
            if (element is Visual elementAsVisual)
            {
                visual = elementAsVisual;
                visual3D = null;
                return true;
            }

            if (element is Visual3D elementAsVisual3D)
            {
                visual = null;
                visual3D = elementAsVisual3D;
                return true;
            }

            visual = null;
            visual3D = null;
            return false;
        }
    }
}

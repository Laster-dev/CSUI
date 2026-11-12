// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using System.Windows.Input;
using System.Windows.Automation.Peers;

namespace MS.Internal
{
    internal static class UIElementHelper
    {
        internal static bool IsHitTestVisible(DependencyObject o)
        {
            Debug.Assert(o != null, "UIElementHelper.IsHitTestVisible called with null argument");

            if (o is UIElement oAsUIElement)
            {
                return oAsUIElement.IsHitTestVisible;
            }
            else if (o is UIElement3D oAsUIElement3D)
            {
                return oAsUIElement3D.IsHitTestVisible;
            }

            return false;
        }

        internal static bool IsVisible(DependencyObject o)
        {
            Debug.Assert(o != null, "UIElementHelper.IsVisible called with null argument");

            if (o is UIElement oAsUIElement)
            {
                return oAsUIElement.IsVisible;
            }
            else if (o is UIElement3D oAsUIElement3D)
            {
                return oAsUIElement3D.IsVisible;
            }

            return false;
        }

        internal static DependencyObject PredictFocus(DependencyObject o, FocusNavigationDirection direction)
        {
            Debug.Assert(o != null, "UIElementHelper.PredictFocus called with null argument");

            if (o is UIElement uie)
            {
                return uie.PredictFocus(direction);
            }
            else if (o is ContentElement ce)
            {
                return ce.PredictFocus(direction);
            }
            else if (o is UIElement3D uie3D)
            {
                return uie3D.PredictFocus(direction);
            }

            return null;
        }

        internal static UIElement GetContainingUIElement2D(DependencyObject reference)
        {
            UIElement element = null;

            while (reference != null)
            {
                element = reference as UIElement;

                if (element != null) break;

                reference = VisualTreeHelper.GetParent(reference);
            }

            return element;
        }

        internal static DependencyObject GetUIParent(DependencyObject child)
        {
            return GetUIParent(child, false);
        }

        internal static DependencyObject GetUIParent(DependencyObject child, bool continuePastVisualTree)
        {
            DependencyObject parent = null;
            DependencyObject myParent = null;

            // Try to find a UIElement parent in the visual ancestry.
            if (child is Visual visual)
            {
                myParent = visual.InternalVisualParent;
            }
            else if (child is Visual3D v3d)
            {
                myParent = v3d.InternalVisualParent;
            }

            parent = InputElement.GetContainingUIElement(myParent) as DependencyObject;

            // If there was no UIElement parent in the visual ancestry,
            // check along the logical branch.
            if (parent == null && continuePastVisualTree)
            {
                if (child is UIElement childAsUIElement)
                {
                    parent = InputElement.GetContainingInputElement(childAsUIElement.GetUIParentCore()) as DependencyObject;
                }
                else if (child is UIElement3D childAsUIElement3D)
                {
                    parent = InputElement.GetContainingInputElement(childAsUIElement3D.GetUIParentCore()) as DependencyObject;
                }
            }

            return parent;
        }

        internal static bool IsUIElementOrUIElement3D(DependencyObject o)
        {
            return o is UIElement || o is UIElement3D;
        }

        internal static void InvalidateAutomationAncestors(DependencyObject o)
        {
            Stack<DependencyObject> branchNodeStack = new Stack<DependencyObject>();
            bool continueInvalidation = true;

            while (o != null && continueInvalidation)
            {
                continueInvalidation &= InvalidateAutomationPeer(o, out UIElement e, out ContentElement ce, out UIElement3D e3d);

                // Invoke InvalidateAutomationAncestorsCore
                bool continuePastVisualTree = false;
                if (e != null)
                {
                    continueInvalidation &= e.InvalidateAutomationAncestorsCore(branchNodeStack, out continuePastVisualTree);
                    o = e.GetUIParent(continuePastVisualTree);
                }
                else if (ce != null)
                {
                    continueInvalidation &= ce.InvalidateAutomationAncestorsCore(branchNodeStack, out continuePastVisualTree);
                    o = (DependencyObject)ce.GetUIParent(continuePastVisualTree);
                }
                else if (e3d != null)
                {
                    continueInvalidation &= e3d.InvalidateAutomationAncestorsCore(branchNodeStack, out continuePastVisualTree);
                    o = e3d.GetUIParent(continuePastVisualTree);
                }
                else
                {
                    break;
                }
            }
        }

        internal static bool InvalidateAutomationPeer(
            DependencyObject o,
            out UIElement e,
            out ContentElement ce)
        {
            return InvalidateAutomationPeer(o, out e, out ce, out _);
        }

        internal static bool InvalidateAutomationPeer(
            DependencyObject o,
            out UIElement e,
            out ContentElement ce,
            out UIElement3D e3d)
        {
            e = null;
            ce = null;
            e3d = null;

            AutomationPeer ap = null;

            e = o as UIElement;
            if (e != null)
            {
                if (e.HasAutomationPeer)
                    ap = e.GetAutomationPeer();
            }
            else
            {
                ce = o as ContentElement;
                if (ce != null)
                {
                    if (ce.HasAutomationPeer)
                        ap = ce.GetAutomationPeer();
                }
                else
                {
                    e3d = o as UIElement3D;
                    if (e3d != null)
                    {
                        if (e3d.HasAutomationPeer)
                            ap = e3d.GetAutomationPeer();
                    }
                }
            }

            if (ap != null)
            {
                ap.InvalidateAncestorsRecursive();

                // Check for parent being non-null while stopping as we don't want to stop in between due to peers not connected to AT
                // those peers sometimes gets created to serve for various patterns.
                // e.g: ScrollViewAutomationPeer for Scroll Pattern in case of ListBox.
                if (ap.GetParent() != null)
                    return false;
            }

            return true;
        }
    }
}

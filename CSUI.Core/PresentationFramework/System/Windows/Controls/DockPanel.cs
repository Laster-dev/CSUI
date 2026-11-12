// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System;
using System.Windows;

namespace System.Windows.Controls
{
    /// <summary>
    /// Specifies the Dock position of a child element that is inside a DockPanel.
    /// </summary>
    public enum Dock
    {
        Left,
        Top,
        Right,
        Bottom
    }

    /// <summary>
    /// Defines an area where you can arrange child elements either horizontally or vertically, relative to each other.
    /// </summary>
    public class DockPanel : Panel
    {
        public static readonly DependencyProperty DockProperty =
            DependencyProperty.RegisterAttached(
                "Dock",
                typeof(Dock),
                typeof(DockPanel),
                new FrameworkPropertyMetadata(Dock.Left, FrameworkPropertyMetadataOptions.AffectsParentMeasure));

        public static readonly DependencyProperty LastChildFillProperty =
            DependencyProperty.Register(
                nameof(LastChildFill),
                typeof(bool),
                typeof(DockPanel),
                new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.AffectsMeasure));

        public static Dock GetDock(UIElement element)
        {
            ArgumentNullException.ThrowIfNull(element);
            return (Dock)element.GetValue(DockProperty);
        }

        public static void SetDock(UIElement element, Dock dock)
        {
            ArgumentNullException.ThrowIfNull(element);
            element.SetValue(DockProperty, dock);
        }

        internal static bool IsValidDock(object o)
        {
            Dock dock = (Dock)o;
            return dock == Dock.Left
                || dock == Dock.Top
                || dock == Dock.Right
                || dock == Dock.Bottom;
        }

        public bool LastChildFill
        {
            get => (bool)GetValue(LastChildFillProperty);
            set => SetValue(LastChildFillProperty, value);
        }

        public DockPanel()
        {
        }

        protected override Size MeasureOverride(Size constraint)
        {
            UIElementCollection children = InternalChildren;
            int count = children.Count;

            double parentWidth = 0.0;
            double parentHeight = 0.0;
            double accumulatedWidth = 0.0;
            double accumulatedHeight = 0.0;

            for (int i = 0; i < count; i++)
            {
                UIElement child = children[i];
                if (child == null || child.Visibility == Visibility.Collapsed) continue;

                Size childConstraint = new Size(
                    Math.Max(0.0, constraint.Width - accumulatedWidth),
                    Math.Max(0.0, constraint.Height - accumulatedHeight));

                child.Measure(childConstraint);
                Size childDesired = child.DesiredSize;

                Dock dock = GetDock(child);
                switch (dock)
                {
                    case Dock.Left:
                    case Dock.Right:
                        parentHeight = Math.Max(parentHeight, accumulatedHeight + childDesired.Height);
                        accumulatedWidth += childDesired.Width;
                        break;

                    case Dock.Top:
                    case Dock.Bottom:
                        parentWidth = Math.Max(parentWidth, accumulatedWidth + childDesired.Width);
                        accumulatedHeight += childDesired.Height;
                        break;
                }
            }

            parentWidth = Math.Max(parentWidth, accumulatedWidth);
            parentHeight = Math.Max(parentHeight, accumulatedHeight);

            return new Size(parentWidth, parentHeight);
        }

        protected override Size ArrangeOverride(Size arrangeSize)
        {
            UIElementCollection children = InternalChildren;
            int count = children.Count;
            int nonCollapsedCount = 0;
            for (int i = 0; i < count; i++)
            {
                if (children[i] != null && children[i].Visibility != Visibility.Collapsed)
                {
                    nonCollapsedCount++;
                }
            }

            double left = 0.0;
            double top = 0.0;
            double right = arrangeSize.Width;
            double bottom = arrangeSize.Height;

            int currentVisibleIndex = 0;
            for (int i = 0; i < count; i++)
            {
                UIElement child = children[i];
                if (child == null || child.Visibility == Visibility.Collapsed) continue;

                currentVisibleIndex++;
                bool isLastChild = currentVisibleIndex == nonCollapsedCount;

                if (isLastChild && LastChildFill)
                {
                    child.Arrange(new Rect(left, top, Math.Max(0.0, right - left), Math.Max(0.0, bottom - top)));
                    break;
                }

                Dock dock = GetDock(child);
                Size childDesired = child.DesiredSize;

                switch (dock)
                {
                    case Dock.Left:
                        child.Arrange(new Rect(left, top, childDesired.Width, Math.Max(0.0, bottom - top)));
                        left += childDesired.Width;
                        break;

                    case Dock.Top:
                        child.Arrange(new Rect(left, top, Math.Max(0.0, right - left), childDesired.Height));
                        top += childDesired.Height;
                        break;

                    case Dock.Right:
                        child.Arrange(new Rect(Math.Max(left, right - childDesired.Width), top, childDesired.Width, Math.Max(0.0, bottom - top)));
                        right -= childDesired.Width;
                        break;

                    case Dock.Bottom:
                        child.Arrange(new Rect(left, Math.Max(top, bottom - childDesired.Height), Math.Max(0.0, right - left), childDesired.Height));
                        bottom -= childDesired.Height;
                        break;
                }
            }

            return arrangeSize;
        }
    }
}

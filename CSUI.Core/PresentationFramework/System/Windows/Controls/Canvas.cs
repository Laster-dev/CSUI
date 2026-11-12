// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System;
using System.Windows;

namespace System.Windows.Controls
{
    /// <summary>
    /// Defines an area within which you can explicitly position child elements by using coordinates that are relative to the Canvas area.
    /// </summary>
    public class Canvas : Panel
    {
        public static readonly DependencyProperty LeftProperty =
            DependencyProperty.RegisterAttached(
                "Left",
                typeof(double),
                typeof(Canvas),
                new FrameworkPropertyMetadata(double.NaN, FrameworkPropertyMetadataOptions.AffectsParentArrange));

        public static readonly DependencyProperty TopProperty =
            DependencyProperty.RegisterAttached(
                "Top",
                typeof(double),
                typeof(Canvas),
                new FrameworkPropertyMetadata(double.NaN, FrameworkPropertyMetadataOptions.AffectsParentArrange));

        public static readonly DependencyProperty RightProperty =
            DependencyProperty.RegisterAttached(
                "Right",
                typeof(double),
                typeof(Canvas),
                new FrameworkPropertyMetadata(double.NaN, FrameworkPropertyMetadataOptions.AffectsParentArrange));

        public static readonly DependencyProperty BottomProperty =
            DependencyProperty.RegisterAttached(
                "Bottom",
                typeof(double),
                typeof(Canvas),
                new FrameworkPropertyMetadata(double.NaN, FrameworkPropertyMetadataOptions.AffectsParentArrange));

        public static double GetLeft(UIElement element)
        {
            ArgumentNullException.ThrowIfNull(element);
            return (double)element.GetValue(LeftProperty);
        }

        public static void SetLeft(UIElement element, double length)
        {
            ArgumentNullException.ThrowIfNull(element);
            element.SetValue(LeftProperty, length);
        }

        public static double GetTop(UIElement element)
        {
            ArgumentNullException.ThrowIfNull(element);
            return (double)element.GetValue(TopProperty);
        }

        public static void SetTop(UIElement element, double length)
        {
            ArgumentNullException.ThrowIfNull(element);
            element.SetValue(TopProperty, length);
        }

        public static double GetRight(UIElement element)
        {
            ArgumentNullException.ThrowIfNull(element);
            return (double)element.GetValue(RightProperty);
        }

        public static void SetRight(UIElement element, double length)
        {
            ArgumentNullException.ThrowIfNull(element);
            element.SetValue(RightProperty, length);
        }

        public static double GetBottom(UIElement element)
        {
            ArgumentNullException.ThrowIfNull(element);
            return (double)element.GetValue(BottomProperty);
        }

        public static void SetBottom(UIElement element, double length)
        {
            ArgumentNullException.ThrowIfNull(element);
            element.SetValue(BottomProperty, length);
        }

        public Canvas()
        {
        }

        protected override Size MeasureOverride(Size constraint)
        {
            Size childConstraint = new Size(double.PositiveInfinity, double.PositiveInfinity);
            UIElementCollection children = InternalChildren;
            for (int i = 0; i < children.Count; i++)
            {
                children[i]?.Measure(childConstraint);
            }

            return new Size();
        }

        protected override Size ArrangeOverride(Size arrangeSize)
        {
            UIElementCollection children = InternalChildren;
            for (int i = 0; i < children.Count; i++)
            {
                UIElement child = children[i];
                if (child == null) continue;

                double x = 0;
                double y = 0;

                double left = GetLeft(child);
                if (!double.IsNaN(left))
                {
                    x = left;
                }
                else
                {
                    double right = GetRight(child);
                    if (!double.IsNaN(right))
                    {
                        x = arrangeSize.Width - child.DesiredSize.Width - right;
                    }
                }

                double top = GetTop(child);
                if (!double.IsNaN(top))
                {
                    y = top;
                }
                else
                {
                    double bottom = GetBottom(child);
                    if (!double.IsNaN(bottom))
                    {
                        y = arrangeSize.Height - child.DesiredSize.Height - bottom;
                    }
                }

                child.Arrange(new Rect(new Point(x, y), child.DesiredSize));
            }

            return arrangeSize;
        }
    }
}

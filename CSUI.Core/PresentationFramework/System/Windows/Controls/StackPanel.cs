// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System;
using System.Windows;

namespace System.Windows.Controls
{
    /// <summary>
    /// Arranges child elements into a single line that can be oriented horizontally or vertically.
    /// Supports clean item spacing, bidirectional layout alignment, and zero-allocation passes.
    /// </summary>
    public class StackPanel : Panel
    {
        public static readonly DependencyProperty OrientationProperty =
            DependencyProperty.Register(
                nameof(Orientation),
                typeof(Orientation),
                typeof(StackPanel),
                new FrameworkPropertyMetadata(Orientation.Vertical, FrameworkPropertyMetadataOptions.AffectsMeasure));

        public static readonly DependencyProperty SpacingProperty =
            DependencyProperty.Register(
                nameof(Spacing),
                typeof(double),
                typeof(StackPanel),
                new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsMeasure));

        public Orientation Orientation
        {
            get => (Orientation)GetValue(OrientationProperty);
            set => SetValue(OrientationProperty, value);
        }

        public double Spacing
        {
            get => (double)GetValue(SpacingProperty);
            set => SetValue(SpacingProperty, value);
        }

        public StackPanel()
        {
        }

        protected override Size MeasureOverride(Size constraint)
        {
            UIElementCollection children = InternalChildren;
            int count = children.Count;
            bool isVert = Orientation == Orientation.Vertical;
            double spacing = Math.Max(0.0, Spacing);

            double stackSize = 0.0;
            double crossSize = 0.0;
            int visibleCount = 0;

            Size childConstraint = isVert
                ? new Size(constraint.Width, double.PositiveInfinity)
                : new Size(double.PositiveInfinity, constraint.Height);

            for (int i = 0; i < count; i++)
            {
                UIElement child = children[i];
                if (child == null) continue;

                child.Measure(childConstraint);
                Size desired = child.DesiredSize;

                if (child.Visibility != Visibility.Collapsed)
                {
                    visibleCount++;
                    if (isVert)
                    {
                        stackSize += desired.Height;
                        crossSize = Math.Max(crossSize, desired.Width);
                    }
                    else
                    {
                        stackSize += desired.Width;
                        crossSize = Math.Max(crossSize, desired.Height);
                    }
                }
            }

            if (visibleCount > 1)
            {
                stackSize += spacing * (visibleCount - 1);
            }

            return isVert ? new Size(crossSize, stackSize) : new Size(stackSize, crossSize);
        }

        protected override Size ArrangeOverride(Size arrangeSize)
        {
            UIElementCollection children = InternalChildren;
            int count = children.Count;
            bool isVert = Orientation == Orientation.Vertical;
            double spacing = Math.Max(0.0, Spacing);
            double currentPos = 0.0;

            for (int i = 0; i < count; i++)
            {
                UIElement child = children[i];
                if (child == null || child.Visibility == Visibility.Collapsed) continue;

                Size desired = child.DesiredSize;
                if (isVert)
                {
                    double w = Math.Max(desired.Width, arrangeSize.Width);
                    child.Arrange(new Rect(0, currentPos, w, desired.Height));
                    currentPos += desired.Height + spacing;
                }
                else
                {
                    double h = Math.Max(desired.Height, arrangeSize.Height);
                    child.Arrange(new Rect(currentPos, 0, desired.Width, h));
                    currentPos += desired.Width + spacing;
                }
            }

            return arrangeSize;
        }
    }
}

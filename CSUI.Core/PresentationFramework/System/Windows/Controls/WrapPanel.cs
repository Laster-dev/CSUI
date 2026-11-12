// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System;
using System.Windows;

namespace System.Windows.Controls
{
    /// <summary>
    /// Positions child elements in sequential position from left to right, breaking content to the next line at the edge of the containing box.
    /// </summary>
    public class WrapPanel : Panel
    {
        public static readonly DependencyProperty OrientationProperty =
            DependencyProperty.Register(
                nameof(Orientation),
                typeof(Orientation),
                typeof(WrapPanel),
                new FrameworkPropertyMetadata(Orientation.Horizontal, FrameworkPropertyMetadataOptions.AffectsMeasure));

        public static readonly DependencyProperty ItemWidthProperty =
            DependencyProperty.Register(
                nameof(ItemWidth),
                typeof(double),
                typeof(WrapPanel),
                new FrameworkPropertyMetadata(double.NaN, FrameworkPropertyMetadataOptions.AffectsMeasure));

        public static readonly DependencyProperty ItemHeightProperty =
            DependencyProperty.Register(
                nameof(ItemHeight),
                typeof(double),
                typeof(WrapPanel),
                new FrameworkPropertyMetadata(double.NaN, FrameworkPropertyMetadataOptions.AffectsMeasure));

        public Orientation Orientation
        {
            get => (Orientation)GetValue(OrientationProperty);
            set => SetValue(OrientationProperty, value);
        }

        public double ItemWidth
        {
            get => (double)GetValue(ItemWidthProperty);
            set => SetValue(ItemWidthProperty, value);
        }

        public double ItemHeight
        {
            get => (double)GetValue(ItemHeightProperty);
            set => SetValue(ItemHeightProperty, value);
        }

        public WrapPanel()
        {
        }

        protected override Size MeasureOverride(Size constraint)
        {
            UIElementCollection children = InternalChildren;
            int count = children.Count;
            bool isHorizontal = Orientation == Orientation.Horizontal;

            double itemW = ItemWidth;
            double itemH = ItemHeight;
            bool hasCustomW = !double.IsNaN(itemW);
            bool hasCustomH = !double.IsNaN(itemH);

            Size childConstraint = new Size(
                hasCustomW ? itemW : constraint.Width,
                hasCustomH ? itemH : constraint.Height);

            double curLineSize = 0.0;
            double curLineThickness = 0.0;
            double panelSize = 0.0;
            double panelThickness = 0.0;
            double maxLineSize = isHorizontal ? constraint.Width : constraint.Height;

            for (int i = 0; i < count; i++)
            {
                UIElement child = children[i];
                if (child == null || child.Visibility == Visibility.Collapsed) continue;

                child.Measure(childConstraint);
                Size desired = child.DesiredSize;
                double childSize = isHorizontal ? (hasCustomW ? itemW : desired.Width) : (hasCustomH ? itemH : desired.Height);
                double childThickness = isHorizontal ? (hasCustomH ? itemH : desired.Height) : (hasCustomW ? itemW : desired.Width);

                if (curLineSize + childSize > maxLineSize && curLineSize > 0.0)
                {
                    panelSize = Math.Max(panelSize, curLineSize);
                    panelThickness += curLineThickness;
                    curLineSize = childSize;
                    curLineThickness = childThickness;
                }
                else
                {
                    curLineSize += childSize;
                    curLineThickness = Math.Max(curLineThickness, childThickness);
                }
            }

            panelSize = Math.Max(panelSize, curLineSize);
            panelThickness += curLineThickness;

            return isHorizontal ? new Size(panelSize, panelThickness) : new Size(panelThickness, panelSize);
        }

        protected override Size ArrangeOverride(Size arrangeSize)
        {
            UIElementCollection children = InternalChildren;
            int count = children.Count;
            bool isHorizontal = Orientation == Orientation.Horizontal;

            double itemW = ItemWidth;
            double itemH = ItemHeight;
            bool hasCustomW = !double.IsNaN(itemW);
            bool hasCustomH = !double.IsNaN(itemH);

            double curLineSize = 0.0;
            double curLineThickness = 0.0;
            double curAccumulatedThickness = 0.0;
            double maxLineSize = isHorizontal ? arrangeSize.Width : arrangeSize.Height;
            int lineStartIndex = 0;

            for (int i = 0; i < count; i++)
            {
                UIElement child = children[i];
                if (child == null || child.Visibility == Visibility.Collapsed) continue;

                Size desired = child.DesiredSize;
                double childSize = isHorizontal ? (hasCustomW ? itemW : desired.Width) : (hasCustomH ? itemH : desired.Height);
                double childThickness = isHorizontal ? (hasCustomH ? itemH : desired.Height) : (hasCustomW ? itemW : desired.Width);

                if (curLineSize + childSize > maxLineSize && curLineSize > 0.0)
                {
                    ArrangeLine(lineStartIndex, i, curAccumulatedThickness, curLineThickness, isHorizontal, hasCustomW, hasCustomH, itemW, itemH);
                    curAccumulatedThickness += curLineThickness;
                    curLineSize = childSize;
                    curLineThickness = childThickness;
                    lineStartIndex = i;
                }
                else
                {
                    curLineSize += childSize;
                    curLineThickness = Math.Max(curLineThickness, childThickness);
                }
            }

            if (lineStartIndex < count)
            {
                ArrangeLine(lineStartIndex, count, curAccumulatedThickness, curLineThickness, isHorizontal, hasCustomW, hasCustomH, itemW, itemH);
            }

            return arrangeSize;
        }

        private void ArrangeLine(int start, int end, double lineOffset, double lineThickness, bool isHorizontal, bool hasCustomW, bool hasCustomH, double customW, double customH)
        {
            double pos = 0.0;
            UIElementCollection children = InternalChildren;

            for (int i = start; i < end; i++)
            {
                UIElement child = children[i];
                if (child == null || child.Visibility == Visibility.Collapsed) continue;

                Size desired = child.DesiredSize;
                double w = hasCustomW ? customW : desired.Width;
                double h = hasCustomH ? customH : desired.Height;

                if (isHorizontal)
                {
                    child.Arrange(new Rect(pos, lineOffset, w, lineThickness));
                    pos += w;
                }
                else
                {
                    child.Arrange(new Rect(lineOffset, pos, lineThickness, h));
                    pos += h;
                }
            }
        }
    }
}

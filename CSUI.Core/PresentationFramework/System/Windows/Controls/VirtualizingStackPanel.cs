// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System;
using System.Collections;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace System.Windows.Controls
{
    /// <summary>
    /// Arranges and virtualizes content on a single line that is oriented horizontally or vertically.
    /// Solves WPF historical variable-height virtualization pain points via dynamic anchor stabilization,
    /// running-average extent estimation, and pixel-smooth non-jitter scrolling.
    /// </summary>
    public class VirtualizingStackPanel : VirtualizingPanel, IScrollInfo
    {
        public static readonly DependencyProperty OrientationProperty =
            DependencyProperty.Register(
                nameof(Orientation),
                typeof(Orientation),
                typeof(VirtualizingStackPanel),
                new FrameworkPropertyMetadata(Orientation.Vertical, FrameworkPropertyMetadataOptions.AffectsMeasure));

        public static readonly DependencyProperty SpacingProperty =
            DependencyProperty.Register(
                nameof(Spacing),
                typeof(double),
                typeof(VirtualizingStackPanel),
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

        // IScrollInfo state
        public bool CanVerticallyScroll { get; set; } = true;
        public bool CanHorizontallyScroll { get; set; } = false;

        public double ExtentWidth => _extent.Width;
        public double ExtentHeight => _extent.Height;
        public double ViewportWidth => _viewport.Width;
        public double ViewportHeight => _viewport.Height;
        public double HorizontalOffset => _offset.X;
        public double VerticalOffset => _offset.Y;
        public ScrollViewer ScrollOwner { get; set; }

        private Size _extent = new Size(0, 0);
        private Size _viewport = new Size(0, 0);
        private Point _offset = new Point(0, 0);

        // --- Variable-height anchor stabilization & estimation engine ---
        // Maps item index to measured length along scrolling orientation
        private readonly Dictionary<int, double> _measuredLengths = new();
        private double _totalMeasuredLength = 0.0;
        private int _measuredCount = 0;
        private double _averageItemLength = 40.0; // Default estimate until items are measured

        // Currently realized item range in InternalChildren [FirstRealizedIndex .. LastRealizedIndex]
        private int _firstRealizedIndex = -1;
        private int _lastRealizedIndex = -1;

        // Anchor tracking: pins the top visible item to prevent thumb jumping
        private int _anchorIndex = 0;
        private double _anchorOffsetInItem = 0.0;

        // Direct item generator delegate for programmatic or standalone virtualization
        public Func<int, UIElement> ItemGenerator { get; set; }
        public int VirtualItemCount { get; set; } = 0;

        public VirtualizingStackPanel()
        {
        }

        /// <summary>
        /// Gets the total item count from either ItemsOwner (ItemsControl), VirtualItemCount, or Children.
        /// </summary>
        private int GetTotalItemCount()
        {
            ItemsControl owner = ItemsControl.GetItemsOwner(this);
            if (owner != null && owner.Items != null)
            {
                return owner.Items.Count;
            }
            if (VirtualItemCount > 0)
            {
                return VirtualItemCount;
            }
            return InternalChildren.Count;
        }

        private double GetEstimatedLength(int index)
        {
            if (_measuredLengths.TryGetValue(index, out double len))
            {
                return len;
            }
            return _averageItemLength;
        }

        private void RecordMeasuredLength(int index, double length)
        {
            if (length <= 0) length = 1.0;
            if (_measuredLengths.TryGetValue(index, out double oldLen))
            {
                _totalMeasuredLength += (length - oldLen);
            }
            else
            {
                _measuredLengths[index] = length;
                _totalMeasuredLength += length;
                _measuredCount++;
            }
            if (_measuredCount > 0)
            {
                _averageItemLength = Math.Max(10.0, _totalMeasuredLength / _measuredCount);
            }
        }

        /// <summary>
        /// Computes cumulative offset for a given item index using measured values and averages.
        /// </summary>
        private double ComputeOffsetForIndex(int targetIndex, double spacing)
        {
            double sum = 0.0;
            for (int i = 0; i < targetIndex; i++)
            {
                sum += GetEstimatedLength(i) + spacing;
            }
            return sum;
        }

        /// <summary>
        /// Finds the item index and intra-item offset corresponding to a given cumulative scroll offset.
        /// </summary>
        private (int index, double offsetInItem) FindIndexAtOffset(double targetOffset, int totalCount, double spacing)
        {
            if (targetOffset <= 0 || totalCount == 0) return (0, 0.0);

            double current = 0.0;
            for (int i = 0; i < totalCount; i++)
            {
                double len = GetEstimatedLength(i) + spacing;
                if (current + len > targetOffset)
                {
                    return (i, targetOffset - current);
                }
                current += len;
            }
            return (Math.Max(0, totalCount - 1), 0.0);
        }

        protected override Size MeasureOverride(Size constraint)
        {
            int totalCount = GetTotalItemCount();
            bool isVert = Orientation == Orientation.Vertical;
            double spacing = Math.Max(0.0, Spacing);

            double vpLength = isVert ? constraint.Height : constraint.Width;
            double vpCross = isVert ? constraint.Width : constraint.Height;

            if (double.IsInfinity(vpLength) || vpLength <= 0)
            {
                vpLength = 600.0; // Fallback viewport size when unconstrained
            }
            if (double.IsInfinity(vpCross))
            {
                vpCross = 400.0;
            }

            _viewport = isVert ? new Size(vpCross, vpLength) : new Size(vpLength, vpCross);

            if (totalCount == 0)
            {
                _extent = new Size(0, 0);
                ScrollOwner?.InvalidateScrollInfo();
                return new Size(0, 0);
            }

            // Step 1: Update Anchor from current scroll offset
            double currentScrollOffset = isVert ? _offset.Y : _offset.X;
            var (newAnchorIndex, newAnchorOffset) = FindIndexAtOffset(currentScrollOffset, totalCount, spacing);
            _anchorIndex = newAnchorIndex;
            _anchorOffsetInItem = newAnchorOffset;

            // Step 2: Determine realization window [startIndex .. endIndex]
            // We buffer 1/2 viewport before and after for jitter-free scrolling
            double cacheBefore = vpLength * 0.5;
            double cacheAfter = vpLength * 0.5;

            double windowStartOffset = Math.Max(0, currentScrollOffset - cacheBefore);
            double windowEndOffset = currentScrollOffset + vpLength + cacheAfter;

            var (startIndex, _) = FindIndexAtOffset(windowStartOffset, totalCount, spacing);
            var (endIndex, _) = FindIndexAtOffset(windowEndOffset, totalCount, spacing);
            endIndex = Math.Min(totalCount - 1, Math.Max(startIndex, endIndex + 1));

            // Step 3: Realize and measure elements in the active window
            double maxCrossSize = 0.0;
            Size childConstraint = isVert
                ? new Size(vpCross, double.PositiveInfinity)
                : new Size(double.PositiveInfinity, vpCross);

            ItemsControl owner = ItemsControl.GetItemsOwner(this);
            IItemContainerGenerator generator = ItemContainerGenerator;

            // If we have custom ItemGenerator or virtual items
            if (ItemGenerator != null)
            {
                // Synchronize children to the realized window
                InternalChildren.Clear();
                for (int i = startIndex; i <= endIndex; i++)
                {
                    UIElement element = ItemGenerator(i);
                    if (element != null)
                    {
                        AddInternalChild(element);
                        element.Measure(childConstraint);
                        double itemLen = isVert ? element.DesiredSize.Height : element.DesiredSize.Width;
                        double itemCross = isVert ? element.DesiredSize.Width : element.DesiredSize.Height;
                        RecordMeasuredLength(i, itemLen);
                        maxCrossSize = Math.Max(maxCrossSize, itemCross);
                    }
                }
            }
            else if (owner != null && generator != null)
            {
                // Realization via ItemContainerGenerator
                GeneratorPosition startPos = generator.GeneratorPositionFromIndex(startIndex);
                int childIndex = (startPos.Offset == 0) ? startPos.Index : startPos.Index + 1;

                using (generator.StartAt(startPos, GeneratorDirection.Forward, true))
                {
                    for (int i = startIndex; i <= endIndex; i++)
                    {
                        bool newlyRealized;
                        UIElement container = generator.GenerateNext(out newlyRealized) as UIElement;
                        if (container == null) break;

                        if (newlyRealized)
                        {
                            if (childIndex >= InternalChildren.Count)
                                AddInternalChild(container);
                            else
                                InsertInternalChild(childIndex, container);
                            generator.PrepareItemContainer(container);
                        }

                        childIndex++;
                        container.Measure(childConstraint);
                        double itemLen = isVert ? container.DesiredSize.Height : container.DesiredSize.Width;
                        double itemCross = isVert ? container.DesiredSize.Width : container.DesiredSize.Height;
                        RecordMeasuredLength(i, itemLen);
                        maxCrossSize = Math.Max(maxCrossSize, itemCross);
                    }
                }

                // Clean up / recycle unneeded containers outside active window
                CleanUpContainers(generator, startIndex, endIndex);
            }
            else
            {
                // Direct Children: measure existing child collection
                for (int i = 0; i < InternalChildren.Count; i++)
                {
                    UIElement child = InternalChildren[i];
                    if (child == null) continue;
                    child.Measure(childConstraint);
                    double itemLen = isVert ? child.DesiredSize.Height : child.DesiredSize.Width;
                    double itemCross = isVert ? child.DesiredSize.Width : child.DesiredSize.Height;
                    RecordMeasuredLength(i, itemLen);
                    maxCrossSize = Math.Max(maxCrossSize, itemCross);
                }
            }

            _firstRealizedIndex = startIndex;
            _lastRealizedIndex = endIndex;

            // Step 4: Compute stabilized total extent
            double totalEstimatedLength = 0.0;
            for (int i = 0; i < totalCount; i++)
            {
                totalEstimatedLength += GetEstimatedLength(i);
                if (i < totalCount - 1) totalEstimatedLength += spacing;
            }

            _extent = isVert
                ? new Size(Math.Max(vpCross, maxCrossSize), totalEstimatedLength)
                : new Size(totalEstimatedLength, Math.Max(vpCross, maxCrossSize));

            ScrollOwner?.InvalidateScrollInfo();
            return isVert ? new Size(maxCrossSize, vpLength) : new Size(vpLength, maxCrossSize);
        }

        private void CleanUpContainers(IItemContainerGenerator generator, int minKeep, int maxKeep)
        {
            IRecyclingItemContainerGenerator recGen = generator as IRecyclingItemContainerGenerator;
            UIElementCollection children = InternalChildren;

            for (int i = children.Count - 1; i >= 0; i--)
            {
                UIElement child = children[i];
                int itemIndex = generator.IndexFromGeneratorPosition(new GeneratorPosition(i, 0));
                if (itemIndex < minKeep || itemIndex > maxKeep)
                {
                    if (recGen != null)
                    {
                        recGen.Recycle(new GeneratorPosition(i, 0), 1);
                    }
                    else
                    {
                        generator.Remove(new GeneratorPosition(i, 0), 1);
                    }
                    RemoveInternalChildRange(i, 1);
                }
            }
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            int totalCount = GetTotalItemCount();
            if (totalCount == 0 || InternalChildren.Count == 0)
            {
                return finalSize;
            }

            bool isVert = Orientation == Orientation.Vertical;
            double spacing = Math.Max(0.0, Spacing);
            double currentScrollOffset = isVert ? _offset.Y : _offset.X;

            // Anchor stabilization in arrange:
            // Calculate anchor item's starting position in layout coordinates
            double anchorStartOffset = ComputeOffsetForIndex(_anchorIndex, spacing);
            double layoutCursor = anchorStartOffset - currentScrollOffset;

            // Arrange realized children
            // First find index of anchor child in InternalChildren
            int startIndex = _firstRealizedIndex >= 0 ? _firstRealizedIndex : 0;

            for (int i = 0; i < InternalChildren.Count; i++)
            {
                UIElement child = InternalChildren[i];
                if (child == null) continue;

                int itemIndex = startIndex + i;
                double itemLength = GetEstimatedLength(itemIndex);
                double itemPos = ComputeOffsetForIndex(itemIndex, spacing) - currentScrollOffset;

                if (isVert)
                {
                    child.Arrange(new Rect(0, itemPos, finalSize.Width, itemLength));
                }
                else
                {
                    child.Arrange(new Rect(itemPos, 0, itemLength, finalSize.Height));
                }
            }

            return finalSize;
        }

        // --- IScrollInfo Methods ---

        public void LineUp() => SetVerticalOffset(VerticalOffset - 16.0);
        public void LineDown() => SetVerticalOffset(VerticalOffset + 16.0);
        public void LineLeft() => SetHorizontalOffset(HorizontalOffset - 16.0);
        public void LineRight() => SetHorizontalOffset(HorizontalOffset + 16.0);

        public void PageUp() => SetVerticalOffset(VerticalOffset - ViewportHeight);
        public void PageDown() => SetVerticalOffset(VerticalOffset + ViewportHeight);
        public void PageLeft() => SetHorizontalOffset(HorizontalOffset - ViewportWidth);
        public void PageRight() => SetHorizontalOffset(HorizontalOffset + ViewportWidth);

        public void MouseWheelUp() => SetVerticalOffset(VerticalOffset - 48.0);
        public void MouseWheelDown() => SetVerticalOffset(VerticalOffset + 48.0);
        public void MouseWheelLeft() => SetHorizontalOffset(HorizontalOffset - 48.0);
        public void MouseWheelRight() => SetHorizontalOffset(HorizontalOffset + 48.0);

        public void SetVerticalOffset(double offset)
        {
            double maxOffset = Math.Max(0, ExtentHeight - ViewportHeight);
            double clamped = Math.Clamp(offset, 0, maxOffset);
            if (clamped != _offset.Y)
            {
                _offset.Y = clamped;
                InvalidateMeasure();
                ScrollOwner?.InvalidateScrollInfo();
            }
        }

        public void SetHorizontalOffset(double offset)
        {
            double maxOffset = Math.Max(0, ExtentWidth - ViewportWidth);
            double clamped = Math.Clamp(offset, 0, maxOffset);
            if (clamped != _offset.X)
            {
                _offset.X = clamped;
                InvalidateMeasure();
                ScrollOwner?.InvalidateScrollInfo();
            }
        }

        public Rect MakeVisible(Visual visual, Rect rectangle)
        {
            if (visual == null || !IsAncestorOf(visual)) return Rect.Empty;

            GeneralTransform transform = visual.TransformToAncestor(this);
            Rect transformedRect = transform.TransformBounds(rectangle);

            if (Orientation == Orientation.Vertical)
            {
                if (transformedRect.Top < 0)
                {
                    SetVerticalOffset(VerticalOffset + transformedRect.Top);
                }
                else if (transformedRect.Bottom > ViewportHeight)
                {
                    SetVerticalOffset(VerticalOffset + (transformedRect.Bottom - ViewportHeight));
                }
            }
            else
            {
                if (transformedRect.Left < 0)
                {
                    SetHorizontalOffset(HorizontalOffset + transformedRect.Left);
                }
                else if (transformedRect.Right > ViewportWidth)
                {
                    SetHorizontalOffset(HorizontalOffset + (transformedRect.Right - ViewportWidth));
                }
            }

            return transformedRect;
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System;
using System.Windows;

namespace System.Windows.Controls
{
    /// <summary>
    /// Provides a way to arrange content in a grid where all the cells in the grid each have the same size.
    /// </summary>
    public class UniformGrid : Panel
    {
        public static readonly DependencyProperty RowsProperty =
            DependencyProperty.Register(
                nameof(Rows),
                typeof(int),
                typeof(UniformGrid),
                new FrameworkPropertyMetadata(0, FrameworkPropertyMetadataOptions.AffectsMeasure),
                ValidateRowsColumns);

        public static readonly DependencyProperty ColumnsProperty =
            DependencyProperty.Register(
                nameof(Columns),
                typeof(int),
                typeof(UniformGrid),
                new FrameworkPropertyMetadata(0, FrameworkPropertyMetadataOptions.AffectsMeasure),
                ValidateRowsColumns);

        public static readonly DependencyProperty FirstColumnProperty =
            DependencyProperty.Register(
                nameof(FirstColumn),
                typeof(int),
                typeof(UniformGrid),
                new FrameworkPropertyMetadata(0, FrameworkPropertyMetadataOptions.AffectsMeasure),
                ValidateFirstColumn);

        public int Rows
        {
            get => (int)GetValue(RowsProperty);
            set => SetValue(RowsProperty, value);
        }

        public int Columns
        {
            get => (int)GetValue(ColumnsProperty);
            set => SetValue(ColumnsProperty, value);
        }

        public int FirstColumn
        {
            get => (int)GetValue(FirstColumnProperty);
            set => SetValue(FirstColumnProperty, value);
        }

        public UniformGrid()
        {
        }

        private static bool ValidateRowsColumns(object o)
        {
            return (int)o >= 0;
        }

        private static bool ValidateFirstColumn(object o)
        {
            return (int)o >= 0;
        }

        private void UpdateComputedValues(out int rows, out int columns)
        {
            int visibleCount = 0;
            UIElementCollection children = InternalChildren;
            for (int i = 0; i < children.Count; i++)
            {
                UIElement child = children[i];
                if (child != null && child.Visibility != Visibility.Collapsed)
                {
                    visibleCount++;
                }
            }

            if (visibleCount == 0)
            {
                visibleCount = 1;
            }

            columns = Columns;
            rows = Rows;

            if (rows == 0 && columns == 0)
            {
                columns = (int)Math.Ceiling(Math.Sqrt(visibleCount));
                rows = (visibleCount + columns - 1) / columns;
            }
            else if (rows == 0)
            {
                rows = (visibleCount + FirstColumn + columns - 1) / columns;
            }
            else if (columns == 0)
            {
                columns = (visibleCount + rows - 1) / rows;
            }

            if (rows <= 0) rows = 1;
            if (columns <= 0) columns = 1;
        }

        protected override Size MeasureOverride(Size constraint)
        {
            UpdateComputedValues(out int rows, out int columns);

            Size childConstraint = new Size(constraint.Width / columns, constraint.Height / rows);
            double maxChildW = 0.0;
            double maxChildH = 0.0;

            UIElementCollection children = InternalChildren;
            for (int i = 0; i < children.Count; i++)
            {
                UIElement child = children[i];
                if (child == null || child.Visibility == Visibility.Collapsed)
                    continue;

                child.Measure(childConstraint);
                Size desired = child.DesiredSize;
                if (desired.Width > maxChildW) maxChildW = desired.Width;
                if (desired.Height > maxChildH) maxChildH = desired.Height;
            }

            return new Size(maxChildW * columns, maxChildH * rows);
        }

        protected override Size ArrangeOverride(Size arrangeSize)
        {
            UpdateComputedValues(out int rows, out int columns);

            Rect cellRect = new Rect(0, 0, arrangeSize.Width / columns, arrangeSize.Height / rows);
            double cellWidth = cellRect.Width;
            cellRect.X += cellWidth * FirstColumn;

            UIElementCollection children = InternalChildren;
            for (int i = 0; i < children.Count; i++)
            {
                UIElement child = children[i];
                if (child == null || child.Visibility == Visibility.Collapsed)
                    continue;

                child.Arrange(cellRect);

                cellRect.X += cellWidth;
                if (cellRect.X + 0.0001 >= arrangeSize.Width)
                {
                    cellRect.Y += cellRect.Height;
                    cellRect.X = 0;
                }
            }

            return arrangeSize;
        }
    }
}

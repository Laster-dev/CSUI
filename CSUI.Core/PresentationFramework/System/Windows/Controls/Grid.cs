// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System;
using System.Collections.Generic;
using System.Windows;

namespace System.Windows.Controls
{
    /// <summary>
    /// Defines a flexible grid area consisting of columns and rows.
    /// Employs clean, high-performance compact matrix allocation and solves Star (*), Auto, Pixel,
    /// RowSpan/ColumnSpan, and Min/Max constraints without heap allocations during layout passes.
    /// </summary>
    public class Grid : Panel
    {
        #region Attached Properties

        public static readonly DependencyProperty RowProperty =
            DependencyProperty.RegisterAttached(
                "Row",
                typeof(int),
                typeof(Grid),
                new FrameworkPropertyMetadata(0, FrameworkPropertyMetadataOptions.AffectsParentMeasure));

        public static readonly DependencyProperty ColumnProperty =
            DependencyProperty.RegisterAttached(
                "Column",
                typeof(int),
                typeof(Grid),
                new FrameworkPropertyMetadata(0, FrameworkPropertyMetadataOptions.AffectsParentMeasure));

        public static readonly DependencyProperty RowSpanProperty =
            DependencyProperty.RegisterAttached(
                "RowSpan",
                typeof(int),
                typeof(Grid),
                new FrameworkPropertyMetadata(1, FrameworkPropertyMetadataOptions.AffectsParentMeasure));

        public static readonly DependencyProperty ColumnSpanProperty =
            DependencyProperty.RegisterAttached(
                "ColumnSpan",
                typeof(int),
                typeof(Grid),
                new FrameworkPropertyMetadata(1, FrameworkPropertyMetadataOptions.AffectsParentMeasure));

        public static readonly DependencyProperty IsSharedSizeScopeProperty =
            DependencyProperty.RegisterAttached(
                "IsSharedSizeScope",
                typeof(bool),
                typeof(Grid),
                new FrameworkPropertyMetadata(false));

        public static bool GetIsSharedSizeScope(DependencyObject element)
        {
            ArgumentNullException.ThrowIfNull(element);
            return (bool)element.GetValue(IsSharedSizeScopeProperty);
        }

        public static void SetIsSharedSizeScope(DependencyObject element, bool value)
        {
            ArgumentNullException.ThrowIfNull(element);
            element.SetValue(IsSharedSizeScopeProperty, value);
        }

        public static int GetRow(UIElement element)
        {
            ArgumentNullException.ThrowIfNull(element);
            return (int)element.GetValue(RowProperty);
        }

        public static void SetRow(UIElement element, int value)
        {
            ArgumentNullException.ThrowIfNull(element);
            element.SetValue(RowProperty, value);
        }

        public static int GetColumn(UIElement element)
        {
            ArgumentNullException.ThrowIfNull(element);
            return (int)element.GetValue(ColumnProperty);
        }

        public static void SetColumn(UIElement element, int value)
        {
            ArgumentNullException.ThrowIfNull(element);
            element.SetValue(ColumnProperty, value);
        }

        public static int GetRowSpan(UIElement element)
        {
            ArgumentNullException.ThrowIfNull(element);
            return (int)element.GetValue(RowSpanProperty);
        }

        public static void SetRowSpan(UIElement element, int value)
        {
            ArgumentNullException.ThrowIfNull(element);
            element.SetValue(RowSpanProperty, value);
        }

        public static int GetColumnSpan(UIElement element)
        {
            ArgumentNullException.ThrowIfNull(element);
            return (int)element.GetValue(ColumnSpanProperty);
        }

        public static void SetColumnSpan(UIElement element, int value)
        {
            ArgumentNullException.ThrowIfNull(element);
            element.SetValue(ColumnSpanProperty, value);
        }

        #endregion

        #region RowDefinitions & ColumnDefinitions

        private RowDefinitionCollection _rowDefinitions;
        private ColumnDefinitionCollection _columnDefinitions;

        public RowDefinitionCollection RowDefinitions => _rowDefinitions ??= new RowDefinitionCollection(this);
        public ColumnDefinitionCollection ColumnDefinitions => _columnDefinitions ??= new ColumnDefinitionCollection(this);

        public static readonly DependencyProperty RowsProperty =
            DependencyProperty.Register(
                nameof(Rows),
                typeof(string),
                typeof(Grid),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsMeasure, OnRowsStringChanged));

        public static readonly DependencyProperty ColumnsProperty =
            DependencyProperty.Register(
                nameof(Columns),
                typeof(string),
                typeof(Grid),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsMeasure, OnColumnsStringChanged));

        public string Rows
        {
            get => (string)GetValue(RowsProperty);
            set => SetValue(RowsProperty, value);
        }

        public string Columns
        {
            get => (string)GetValue(ColumnsProperty);
            set => SetValue(ColumnsProperty, value);
        }

        private static void OnRowsStringChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is Grid g && e.NewValue is string rowsStr)
            {
                g.RowDefinitions.Clear();
                string[] parts = rowsStr.Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (string p in parts)
                {
                    g.RowDefinitions.Add(p);
                }
            }
        }

        private static void OnColumnsStringChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is Grid g && e.NewValue is string colsStr)
            {
                g.ColumnDefinitions.Clear();
                string[] parts = colsStr.Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (string p in parts)
                {
                    g.ColumnDefinitions.Add(p);
                }
            }
        }

        #endregion

        public Grid()
        {
        }

        #region Measure & Arrange Engine

        protected override Size MeasureOverride(Size constraint)
        {
            bool hasCols = _columnDefinitions != null && _columnDefinitions.Count > 0;
            bool hasRows = _rowDefinitions != null && _rowDefinitions.Count > 0;

            // Fast path for single-cell default Grid (no ColumnDefinitions and no RowDefinitions):
            // behaves identically to 1 Star column + 1 Star row in stock WPF.
            if (!hasCols && !hasRows)
            {
                double maxW = 0.0;
                double maxH = 0.0;
                UIElementCollection singleChildren = InternalChildren;
                int singleCount = singleChildren.Count;
                for (int i = 0; i < singleCount; i++)
                {
                    UIElement child = singleChildren[i];
                    if (child == null) continue;
                    child.Measure(constraint);
                    Size desired = child.DesiredSize;
                    maxW = Math.Max(maxW, desired.Width);
                    maxH = Math.Max(maxH, desired.Height);
                }
                return new Size(maxW, maxH);
            }

            int rowCount = Math.Max(1, _rowDefinitions?.Count ?? 0);
            int colCount = Math.Max(1, _columnDefinitions?.Count ?? 0);

            Span<double> colSizes = colCount <= 32 ? stackalloc double[colCount] : new double[colCount];
            Span<double> rowSizes = rowCount <= 32 ? stackalloc double[rowCount] : new double[rowCount];

            Span<GridUnitType> colTypes = colCount <= 32 ? stackalloc GridUnitType[colCount] : new GridUnitType[colCount];
            Span<GridUnitType> rowTypes = rowCount <= 32 ? stackalloc GridUnitType[rowCount] : new GridUnitType[rowCount];

            Span<double> colWeights = colCount <= 32 ? stackalloc double[colCount] : new double[colCount];
            Span<double> rowWeights = rowCount <= 32 ? stackalloc double[rowCount] : new double[rowCount];

            Span<double> colMins = colCount <= 32 ? stackalloc double[colCount] : new double[colCount];
            Span<double> colMaxs = colCount <= 32 ? stackalloc double[colCount] : new double[colCount];
            Span<double> rowMins = rowCount <= 32 ? stackalloc double[rowCount] : new double[rowCount];
            Span<double> rowMaxs = rowCount <= 32 ? stackalloc double[rowCount] : new double[rowCount];

            InitDefinitions(colCount, rowCount, colSizes, rowSizes, colTypes, colWeights, colMins, colMaxs, rowTypes, rowWeights, rowMins, rowMaxs);

            UIElementCollection children = InternalChildren;
            int childCount = children.Count;

            // Pass 1: Measure non-spanning items, accumulate Auto and Star desired sizes
            for (int i = 0; i < childCount; i++)
            {
                UIElement child = children[i];
                if (child == null) continue;

                int row = Math.Clamp(GetRow(child), 0, rowCount - 1);
                int col = Math.Clamp(GetColumn(child), 0, colCount - 1);
                int rowSpan = Math.Max(1, GetRowSpan(child));
                int colSpan = Math.Max(1, GetColumnSpan(child));

                if (rowSpan == 1 && colSpan == 1)
                {
                    double availW = colTypes[col] == GridUnitType.Pixel ? Math.Clamp(colSizes[col], colMins[col], colMaxs[col]) : double.PositiveInfinity;
                    double availH = rowTypes[row] == GridUnitType.Pixel ? Math.Clamp(rowSizes[row], rowMins[row], rowMaxs[row]) : double.PositiveInfinity;
                    child.Measure(new Size(availW, availH));

                    Size desired = child.DesiredSize;
                    if (colTypes[col] == GridUnitType.Auto || colTypes[col] == GridUnitType.Star)
                    {
                        colSizes[col] = Math.Max(colSizes[col], desired.Width);
                    }
                    if (rowTypes[row] == GridUnitType.Auto || rowTypes[row] == GridUnitType.Star)
                    {
                        rowSizes[row] = Math.Max(rowSizes[row], desired.Height);
                    }
                }
            }

            // Pass 2: Measure spanning items and distribute deficit to Auto (or Star) tracks
            for (int i = 0; i < childCount; i++)
            {
                UIElement child = children[i];
                if (child == null) continue;

                int row = Math.Clamp(GetRow(child), 0, rowCount - 1);
                int col = Math.Clamp(GetColumn(child), 0, colCount - 1);
                int rowSpan = Math.Min(Math.Max(1, GetRowSpan(child)), rowCount - row);
                int colSpan = Math.Min(Math.Max(1, GetColumnSpan(child)), colCount - col);

                if (rowSpan > 1 || colSpan > 1)
                {
                    double availW = 0.0;
                    for (int c = 0; c < colSpan; c++)
                    {
                        availW += colTypes[col + c] == GridUnitType.Pixel
                            ? Math.Clamp(colSizes[col + c], colMins[col + c], colMaxs[col + c])
                            : double.PositiveInfinity;
                    }
                    double availH = 0.0;
                    for (int r = 0; r < rowSpan; r++)
                    {
                        availH += rowTypes[row + r] == GridUnitType.Pixel
                            ? Math.Clamp(rowSizes[row + r], rowMins[row + r], rowMaxs[row + r])
                            : double.PositiveInfinity;
                    }

                    child.Measure(new Size(availW, availH));
                    Size desired = child.DesiredSize;

                    DistributeSpanDeficit(col, colSpan, desired.Width, colTypes, colSizes);
                    DistributeSpanDeficit(row, rowSpan, desired.Height, rowTypes, rowSizes);
                }
            }

            // Clamp non-Star tracks with Min/Max
            for (int c = 0; c < colCount; c++)
            {
                if (colTypes[c] != GridUnitType.Star)
                {
                    colSizes[c] = Math.Clamp(colSizes[c], colMins[c], colMaxs[c]);
                }
            }
            for (int r = 0; r < rowCount; r++)
            {
                if (rowTypes[r] != GridUnitType.Star)
                {
                    rowSizes[r] = Math.Clamp(rowSizes[r], rowMins[r], rowMaxs[r]);
                }
            }

            // Preserve Star children's desired sizes before ResolveStars overwrites Star tracks under finite constraints
            Span<double> starDesiredCols = colCount <= 32 ? stackalloc double[colCount] : new double[colCount];
            Span<double> starDesiredRows = rowCount <= 32 ? stackalloc double[rowCount] : new double[rowCount];
            colSizes.CopyTo(starDesiredCols);
            rowSizes.CopyTo(starDesiredRows);

            // Pass 3: Resolve Star tracks against constraint
            ResolveStars(constraint.Width, colCount, colTypes, colWeights, colMins, colMaxs, colSizes);
            ResolveStars(constraint.Height, rowCount, rowTypes, rowWeights, rowMins, rowMaxs, rowSizes);

            // Pass 4: Re-measure children in Star tracks with resolved finite track sizes
            bool finiteW = !double.IsPositiveInfinity(constraint.Width);
            bool finiteH = !double.IsPositiveInfinity(constraint.Height);
            if (finiteW || finiteH)
            {
                bool autoColChanged = false;
                bool autoRowChanged = false;

                for (int i = 0; i < childCount; i++)
                {
                    UIElement child = children[i];
                    if (child == null) continue;

                    int row = Math.Clamp(GetRow(child), 0, rowCount - 1);
                    int col = Math.Clamp(GetColumn(child), 0, colCount - 1);
                    int rowSpan = Math.Min(Math.Max(1, GetRowSpan(child)), rowCount - row);
                    int colSpan = Math.Min(Math.Max(1, GetColumnSpan(child)), colCount - col);

                    bool hasStarCol = false;
                    for (int c = 0; c < colSpan; c++)
                    {
                        if (colTypes[col + c] == GridUnitType.Star) { hasStarCol = true; break; }
                    }
                    bool hasStarRow = false;
                    for (int r = 0; r < rowSpan; r++)
                    {
                        if (rowTypes[row + r] == GridUnitType.Star) { hasStarRow = true; break; }
                    }

                    if ((!hasStarCol || !finiteW) && (!hasStarRow || !finiteH))
                    {
                        continue;
                    }

                    double availW = 0.0;
                    if (hasStarCol && finiteW)
                    {
                        for (int c = 0; c < colSpan; c++) availW += colSizes[col + c];
                    }
                    else
                    {
                        for (int c = 0; c < colSpan; c++)
                        {
                            availW += colTypes[col + c] == GridUnitType.Pixel ? colSizes[col + c] : double.PositiveInfinity;
                        }
                    }

                    double availH = 0.0;
                    if (hasStarRow && finiteH)
                    {
                        for (int r = 0; r < rowSpan; r++) availH += rowSizes[row + r];
                    }
                    else
                    {
                        for (int r = 0; r < rowSpan; r++)
                        {
                            availH += rowTypes[row + r] == GridUnitType.Pixel ? rowSizes[row + r] : double.PositiveInfinity;
                        }
                    }

                    child.Measure(new Size(availW, availH));
                    Size desired = child.DesiredSize;

                    if (rowSpan == 1 && colSpan == 1)
                    {
                        if (colTypes[col] == GridUnitType.Auto)
                        {
                            double updated = Math.Clamp(Math.Max(colSizes[col], desired.Width), colMins[col], colMaxs[col]);
                            if (updated > colSizes[col])
                            {
                                colSizes[col] = updated;
                                starDesiredCols[col] = updated;
                                autoColChanged = true;
                            }
                        }
                        else if (colTypes[col] == GridUnitType.Star)
                        {
                            starDesiredCols[col] = Math.Max(starDesiredCols[col], desired.Width);
                        }

                        if (rowTypes[row] == GridUnitType.Auto)
                        {
                            double updated = Math.Clamp(Math.Max(rowSizes[row], desired.Height), rowMins[row], rowMaxs[row]);
                            if (updated > rowSizes[row])
                            {
                                rowSizes[row] = updated;
                                starDesiredRows[row] = updated;
                                autoRowChanged = true;
                            }
                        }
                        else if (rowTypes[row] == GridUnitType.Star)
                        {
                            starDesiredRows[row] = Math.Max(starDesiredRows[row], desired.Height);
                        }
                    }
                }

                if (autoColChanged)
                {
                    ResolveStars(constraint.Width, colCount, colTypes, colWeights, colMins, colMaxs, colSizes);
                }
                if (autoRowChanged)
                {
                    ResolveStars(constraint.Height, rowCount, rowTypes, rowWeights, rowMins, rowMaxs, rowSizes);
                }
            }

            // Compute desired size using infinity star sizing (stock WPF Grid MeasureOverride returns
            // content-desired size rather than stretching Star tracks to finite constraint).
            double measuredWidth = ResolveStars(double.PositiveInfinity, colCount, colTypes, colWeights, colMins, colMaxs, starDesiredCols);
            double measuredHeight = ResolveStars(double.PositiveInfinity, rowCount, rowTypes, rowWeights, rowMins, rowMaxs, starDesiredRows);

            // Commit calculated sizes to DefinitionBase
            CommitCalculatedSizes(colCount, rowCount, colSizes, rowSizes);

            return new Size(
                finiteW ? Math.Min(constraint.Width, measuredWidth) : measuredWidth,
                finiteH ? Math.Min(constraint.Height, measuredHeight) : measuredHeight);
        }

        protected override Size ArrangeOverride(Size arrangeSize)
        {
            bool hasCols = _columnDefinitions != null && _columnDefinitions.Count > 0;
            bool hasRows = _rowDefinitions != null && _rowDefinitions.Count > 0;

            if (!hasCols && !hasRows)
            {
                UIElementCollection singleChildren = InternalChildren;
                int singleCount = singleChildren.Count;
                Rect finalRect = new Rect(0.0, 0.0, Math.Max(0.0, arrangeSize.Width), Math.Max(0.0, arrangeSize.Height));
                for (int i = 0; i < singleCount; i++)
                {
                    UIElement child = singleChildren[i];
                    if (child == null) continue;
                    child.Arrange(finalRect);
                }
                return arrangeSize;
            }

            int rowCount = Math.Max(1, _rowDefinitions?.Count ?? 0);
            int colCount = Math.Max(1, _columnDefinitions?.Count ?? 0);

            Span<double> colSizes = colCount <= 32 ? stackalloc double[colCount] : new double[colCount];
            Span<double> rowSizes = rowCount <= 32 ? stackalloc double[rowCount] : new double[rowCount];

            Span<GridUnitType> colTypes = colCount <= 32 ? stackalloc GridUnitType[colCount] : new GridUnitType[colCount];
            Span<GridUnitType> rowTypes = rowCount <= 32 ? stackalloc GridUnitType[rowCount] : new GridUnitType[rowCount];

            Span<double> colWeights = colCount <= 32 ? stackalloc double[colCount] : new double[colCount];
            Span<double> rowWeights = rowCount <= 32 ? stackalloc double[rowCount] : new double[rowCount];

            Span<double> colMins = colCount <= 32 ? stackalloc double[colCount] : new double[colCount];
            Span<double> colMaxs = colCount <= 32 ? stackalloc double[colCount] : new double[colCount];
            Span<double> rowMins = rowCount <= 32 ? stackalloc double[rowCount] : new double[rowCount];
            Span<double> rowMaxs = rowCount <= 32 ? stackalloc double[rowCount] : new double[rowCount];

            InitDefinitions(colCount, rowCount, colSizes, rowSizes, colTypes, colWeights, colMins, colMaxs, rowTypes, rowWeights, rowMins, rowMaxs);

            // Copy measured Auto sizes into arrange
            for (int c = 0; c < colCount; c++)
            {
                if (colTypes[c] == GridUnitType.Auto && _columnDefinitions != null && c < _columnDefinitions.Count)
                {
                    colSizes[c] = _columnDefinitions[c].ActualCalculatedSize;
                }
            }
            for (int r = 0; r < rowCount; r++)
            {
                if (rowTypes[r] == GridUnitType.Auto && _rowDefinitions != null && r < _rowDefinitions.Count)
                {
                    rowSizes[r] = _rowDefinitions[r].ActualCalculatedSize;
                }
            }

            ResolveStars(arrangeSize.Width, colCount, colTypes, colWeights, colMins, colMaxs, colSizes);
            ResolveStars(arrangeSize.Height, rowCount, rowTypes, rowWeights, rowMins, rowMaxs, rowSizes);

            Span<double> colOffsets = colCount <= 32 ? stackalloc double[colCount + 1] : new double[colCount + 1];
            Span<double> rowOffsets = rowCount <= 32 ? stackalloc double[rowCount + 1] : new double[rowCount + 1];

            colOffsets[0] = 0.0;
            for (int c = 0; c < colCount; c++)
            {
                colOffsets[c + 1] = colOffsets[c] + colSizes[c];
                if (_columnDefinitions != null && c < _columnDefinitions.Count)
                {
                    _columnDefinitions[c].FinalOffset = colOffsets[c];
                    _columnDefinitions[c].ActualCalculatedSize = colSizes[c];
                }
            }

            rowOffsets[0] = 0.0;
            for (int r = 0; r < rowCount; r++)
            {
                rowOffsets[r + 1] = rowOffsets[r] + rowSizes[r];
                if (_rowDefinitions != null && r < _rowDefinitions.Count)
                {
                    _rowDefinitions[r].FinalOffset = rowOffsets[r];
                    _rowDefinitions[r].ActualCalculatedSize = rowSizes[r];
                }
            }

            UIElementCollection children = InternalChildren;
            int childCount = children.Count;

            for (int i = 0; i < childCount; i++)
            {
                UIElement child = children[i];
                if (child == null) continue;

                int row = Math.Clamp(GetRow(child), 0, rowCount - 1);
                int col = Math.Clamp(GetColumn(child), 0, colCount - 1);
                int rowSpan = Math.Min(GetRowSpan(child), rowCount - row);
                int colSpan = Math.Min(GetColumnSpan(child), colCount - col);

                double x = colOffsets[col];
                double y = rowOffsets[row];
                double w = colOffsets[col + colSpan] - x;
                double h = rowOffsets[row + rowSpan] - y;

                child.Arrange(new Rect(x, y, Math.Max(0.0, w), Math.Max(0.0, h)));
            }

            return arrangeSize;
        }

        private void InitDefinitions(
            int colCount, int rowCount,
            Span<double> colSizes, Span<double> rowSizes,
            Span<GridUnitType> colTypes, Span<double> colWeights, Span<double> colMins, Span<double> colMaxs,
            Span<GridUnitType> rowTypes, Span<double> rowWeights, Span<double> rowMins, Span<double> rowMaxs)
        {
            if (_columnDefinitions == null || _columnDefinitions.Count == 0)
            {
                colTypes[0] = GridUnitType.Star;
                colWeights[0] = 1.0;
                colMins[0] = 0.0;
                colMaxs[0] = double.PositiveInfinity;
                colSizes[0] = 0.0;
            }
            else
            {
                for (int c = 0; c < colCount; c++)
                {
                    ColumnDefinition def = _columnDefinitions[c];
                    GridLength len = def.Width;
                    colTypes[c] = len.GridUnitType;
                    colWeights[c] = len.Value;
                    colMins[c] = def.MinWidth;
                    colMaxs[c] = Math.Max(def.MinWidth, def.MaxWidth);
                    colSizes[c] = len.GridUnitType == GridUnitType.Pixel ? len.Value : 0.0;
                }
            }

            if (_rowDefinitions == null || _rowDefinitions.Count == 0)
            {
                rowTypes[0] = GridUnitType.Star;
                rowWeights[0] = 1.0;
                rowMins[0] = 0.0;
                rowMaxs[0] = double.PositiveInfinity;
                rowSizes[0] = 0.0;
            }
            else
            {
                for (int r = 0; r < rowCount; r++)
                {
                    RowDefinition def = _rowDefinitions[r];
                    GridLength len = def.Height;
                    rowTypes[r] = len.GridUnitType;
                    rowWeights[r] = len.Value;
                    rowMins[r] = def.MinHeight;
                    rowMaxs[r] = Math.Max(def.MinHeight, def.MaxHeight);
                    rowSizes[r] = len.GridUnitType == GridUnitType.Pixel ? len.Value : 0.0;
                }
            }
        }

        private static void DistributeSpanDeficit(int start, int span, double desired, Span<GridUnitType> types, Span<double> sizes)
        {
            double currentTotal = 0.0;
            int autoCount = 0;
            int starCount = 0;

            for (int i = 0; i < span; i++)
            {
                currentTotal += sizes[start + i];
                if (types[start + i] == GridUnitType.Auto) autoCount++;
                else if (types[start + i] == GridUnitType.Star) starCount++;
            }

            double deficit = desired - currentTotal;
            if (deficit <= 0.0)
            {
                return;
            }

            if (autoCount > 0)
            {
                double share = deficit / autoCount;
                for (int i = 0; i < span; i++)
                {
                    if (types[start + i] == GridUnitType.Auto)
                    {
                        sizes[start + i] += share;
                    }
                }
            }
            else if (starCount > 0)
            {
                double share = deficit / starCount;
                for (int i = 0; i < span; i++)
                {
                    if (types[start + i] == GridUnitType.Star)
                    {
                        sizes[start + i] += share;
                    }
                }
            }
        }

        private static double ResolveStars(
            double totalAvailable, int count,
            Span<GridUnitType> types, Span<double> weights, Span<double> mins, Span<double> maxs, Span<double> sizes)
        {
            double fixedTotal = 0.0;
            double starWeightTotal = 0.0;

            for (int i = 0; i < count; i++)
            {
                if (types[i] == GridUnitType.Star)
                {
                    starWeightTotal += weights[i];
                }
                else
                {
                    fixedTotal += sizes[i];
                }
            }

            if (double.IsPositiveInfinity(totalAvailable))
            {
                // Stock WPF infinity star sizing: scale all Star tracks proportionally by the
                // largest (clamped desired / weight) ratio so no Star track collapses to 0.
                double starScale = 0.0;
                for (int i = 0; i < count; i++)
                {
                    if (types[i] == GridUnitType.Star && weights[i] > 0.0)
                    {
                        double clamped = Math.Clamp(sizes[i], mins[i], maxs[i]);
                        starScale = Math.Max(starScale, clamped / weights[i]);
                    }
                }

                for (int i = 0; i < count; i++)
                {
                    if (types[i] == GridUnitType.Star)
                    {
                        double target = weights[i] > 0.0 ? starScale * weights[i] : 0.0;
                        sizes[i] = Math.Clamp(target, mins[i], maxs[i]);
                    }
                }
            }
            else if (starWeightTotal > 0.0)
            {
                double remaining = Math.Max(0.0, totalAvailable - fixedTotal);
                for (int i = 0; i < count; i++)
                {
                    if (types[i] == GridUnitType.Star)
                    {
                        double target = remaining * (weights[i] / starWeightTotal);
                        sizes[i] = Math.Clamp(target, mins[i], maxs[i]);
                    }
                }
            }
            else
            {
                for (int i = 0; i < count; i++)
                {
                    if (types[i] == GridUnitType.Star)
                    {
                        sizes[i] = Math.Clamp(0.0, mins[i], maxs[i]);
                    }
                }
            }

            double sum = 0.0;
            for (int i = 0; i < count; i++) sum += sizes[i];
            return sum;
        }

        private void CommitCalculatedSizes(int colCount, int rowCount, ReadOnlySpan<double> colSizes, ReadOnlySpan<double> rowSizes)
        {
            if (_columnDefinitions != null)
            {
                for (int c = 0; c < Math.Min(colCount, _columnDefinitions.Count); c++)
                {
                    _columnDefinitions[c].ActualCalculatedSize = colSizes[c];
                }
            }
            if (_rowDefinitions != null)
            {
                for (int r = 0; r < Math.Min(rowCount, _rowDefinitions.Count); r++)
                {
                    _rowDefinitions[r].ActualCalculatedSize = rowSizes[r];
                }
            }
        }

        #endregion
    }
}

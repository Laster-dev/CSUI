// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System;
using System.Collections.ObjectModel;
using System.Windows;

namespace System.Windows.Controls
{
    /// <summary>
    /// Defines column-specific properties for Grid elements.
    /// </summary>
    public sealed class ColumnDefinition : DefinitionBase
    {
        public static readonly DependencyProperty WidthProperty =
            DependencyProperty.Register(
                nameof(Width),
                typeof(GridLength),
                typeof(ColumnDefinition),
                new FrameworkPropertyMetadata(new GridLength(1.0, GridUnitType.Star), OnDefinitionPropertyChanged));

        public static readonly DependencyProperty MinWidthProperty =
            DependencyProperty.Register(
                nameof(MinWidth),
                typeof(double),
                typeof(ColumnDefinition),
                new FrameworkPropertyMetadata(0.0, OnDefinitionPropertyChanged));

        public static readonly DependencyProperty MaxWidthProperty =
            DependencyProperty.Register(
                nameof(MaxWidth),
                typeof(double),
                typeof(ColumnDefinition),
                new FrameworkPropertyMetadata(double.PositiveInfinity, OnDefinitionPropertyChanged));

        public GridLength Width
        {
            get => (GridLength)GetValue(WidthProperty);
            set => SetValue(WidthProperty, value);
        }

        public double MinWidth
        {
            get => (double)GetValue(MinWidthProperty);
            set => SetValue(MinWidthProperty, value);
        }

        public double MaxWidth
        {
            get => (double)GetValue(MaxWidthProperty);
            set => SetValue(MaxWidthProperty, value);
        }

        public double ActualWidth => ActualCalculatedSize;

        public ColumnDefinition()
        {
        }

        public ColumnDefinition(GridLength width)
        {
            Width = width;
        }

        public ColumnDefinition(string widthString)
        {
            Width = GridLength.Parse(widthString);
        }
    }

    /// <summary>
    /// Collection of ColumnDefinitions within a Grid.
    /// </summary>
    public sealed class ColumnDefinitionCollection : Collection<ColumnDefinition>
    {
        private readonly Grid _owner;

        internal ColumnDefinitionCollection(Grid owner)
        {
            _owner = owner;
        }

        protected override void InsertItem(int index, ColumnDefinition item)
        {
            ArgumentNullException.ThrowIfNull(item);
            item.ParentGrid = _owner;
            base.InsertItem(index, item);
            _owner?.InvalidateMeasure();
        }

        protected override void SetItem(int index, ColumnDefinition item)
        {
            ArgumentNullException.ThrowIfNull(item);
            if (this[index] != null) this[index].ParentGrid = null;
            item.ParentGrid = _owner;
            base.SetItem(index, item);
            _owner?.InvalidateMeasure();
        }

        protected override void RemoveItem(int index)
        {
            if (this[index] != null) this[index].ParentGrid = null;
            base.RemoveItem(index);
            _owner?.InvalidateMeasure();
        }

        protected override void ClearItems()
        {
            for (int i = 0; i < Count; i++)
            {
                if (this[i] != null) this[i].ParentGrid = null;
            }
            base.ClearItems();
            _owner?.InvalidateMeasure();
        }

        public void Add(GridLength length)
        {
            Add(new ColumnDefinition(length));
        }

        public void Add(string lengthString)
        {
            Add(new ColumnDefinition(lengthString));
        }
    }
}

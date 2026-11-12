// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;

namespace System.Windows.Controls
{
    /// <summary>
    /// Base class for RowDefinition and ColumnDefinition.
    /// </summary>
    public abstract class DefinitionBase : FrameworkContentElement
    {
        internal Grid ParentGrid { get; set; }

        internal double FinalOffset { get; set; }
        internal double ActualCalculatedSize { get; set; }
        internal double MinCalculatedSize { get; set; }
        internal double MaxCalculatedSize { get; set; }
        internal double PreferredCalculatedSize { get; set; }

        protected static void OnDefinitionPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DefinitionBase def && def.ParentGrid != null)
            {
                def.ParentGrid.InvalidateMeasure();
            }
        }
    }

    /// <summary>
    /// Defines row-specific properties for Grid elements.
    /// </summary>
    public sealed class RowDefinition : DefinitionBase
    {
        public static readonly DependencyProperty HeightProperty =
            DependencyProperty.Register(
                nameof(Height),
                typeof(GridLength),
                typeof(RowDefinition),
                new FrameworkPropertyMetadata(new GridLength(1.0, GridUnitType.Star), OnDefinitionPropertyChanged));

        public static readonly DependencyProperty MinHeightProperty =
            DependencyProperty.Register(
                nameof(MinHeight),
                typeof(double),
                typeof(RowDefinition),
                new FrameworkPropertyMetadata(0.0, OnDefinitionPropertyChanged));

        public static readonly DependencyProperty MaxHeightProperty =
            DependencyProperty.Register(
                nameof(MaxHeight),
                typeof(double),
                typeof(RowDefinition),
                new FrameworkPropertyMetadata(double.PositiveInfinity, OnDefinitionPropertyChanged));

        public GridLength Height
        {
            get => (GridLength)GetValue(HeightProperty);
            set => SetValue(HeightProperty, value);
        }

        public double MinHeight
        {
            get => (double)GetValue(MinHeightProperty);
            set => SetValue(MinHeightProperty, value);
        }

        public double MaxHeight
        {
            get => (double)GetValue(MaxHeightProperty);
            set => SetValue(MaxHeightProperty, value);
        }

        public double ActualHeight => ActualCalculatedSize;

        public RowDefinition()
        {
        }

        public RowDefinition(GridLength height)
        {
            Height = height;
        }

        public RowDefinition(string heightString)
        {
            Height = GridLength.Parse(heightString);
        }
    }

    /// <summary>
    /// Collection of RowDefinitions within a Grid.
    /// </summary>
    public sealed class RowDefinitionCollection : Collection<RowDefinition>
    {
        private readonly Grid _owner;

        internal RowDefinitionCollection(Grid owner)
        {
            _owner = owner;
        }

        protected override void InsertItem(int index, RowDefinition item)
        {
            ArgumentNullException.ThrowIfNull(item);
            item.ParentGrid = _owner;
            base.InsertItem(index, item);
            _owner?.InvalidateMeasure();
        }

        protected override void SetItem(int index, RowDefinition item)
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
            Add(new RowDefinition(length));
        }

        public void Add(string lengthString)
        {
            Add(new RowDefinition(lengthString));
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System;
using System.ComponentModel;
using System.Windows;

namespace System.Windows.Controls.Primitives
{
    /// <summary>
    /// Represents an element that has a value within a specific range.
    /// </summary>
    public abstract class RangeBase : Control
    {
        public static readonly DependencyProperty MinimumProperty =
            DependencyProperty.Register(
                nameof(Minimum),
                typeof(double),
                typeof(RangeBase),
                new FrameworkPropertyMetadata(0.0, OnMinimumChanged),
                ValidateDouble);

        public static readonly DependencyProperty MaximumProperty =
            DependencyProperty.Register(
                nameof(Maximum),
                typeof(double),
                typeof(RangeBase),
                new FrameworkPropertyMetadata(100.0, OnMaximumChanged, CoerceMaximum),
                ValidateDouble);

        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register(
                nameof(Value),
                typeof(double),
                typeof(RangeBase),
                new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault | FrameworkPropertyMetadataOptions.AffectsRender, OnValueChanged, CoerceValue),
                ValidateDouble);

        public static readonly DependencyProperty LargeChangeProperty =
            DependencyProperty.Register(
                nameof(LargeChange),
                typeof(double),
                typeof(RangeBase),
                new FrameworkPropertyMetadata(1.0),
                ValidateDouble);

        public static readonly DependencyProperty SmallChangeProperty =
            DependencyProperty.Register(
                nameof(SmallChange),
                typeof(double),
                typeof(RangeBase),
                new FrameworkPropertyMetadata(0.1),
                ValidateDouble);

        public static readonly RoutedEvent ValueChangedEvent =
            EventManager.RegisterRoutedEvent(
                nameof(ValueChanged),
                RoutingStrategy.Bubble,
                typeof(RoutedPropertyChangedEventHandler<double>),
                typeof(RangeBase));

        public double Minimum
        {
            get => (double)GetValue(MinimumProperty);
            set => SetValue(MinimumProperty, value);
        }

        public double Maximum
        {
            get => (double)GetValue(MaximumProperty);
            set => SetValue(MaximumProperty, value);
        }

        public double Value
        {
            get => (double)GetValue(ValueProperty);
            set => SetValue(ValueProperty, value);
        }

        public double LargeChange
        {
            get => (double)GetValue(LargeChangeProperty);
            set => SetValue(LargeChangeProperty, value);
        }

        public double SmallChange
        {
            get => (double)GetValue(SmallChangeProperty);
            set => SetValue(SmallChangeProperty, value);
        }

        public event RoutedPropertyChangedEventHandler<double> ValueChanged
        {
            add => AddHandler(ValueChangedEvent, value);
            remove => RemoveHandler(ValueChangedEvent, value);
        }

        protected RangeBase()
        {
        }

        private static bool ValidateDouble(object value)
        {
            double d = (double)value;
            return !double.IsNaN(d) && !double.IsInfinity(d);
        }

        private static void OnMinimumChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            RangeBase rb = (RangeBase)d;
            rb.CoerceValue(MaximumProperty);
            rb.CoerceValue(ValueProperty);
        }

        private static object CoerceMaximum(DependencyObject d, object baseValue)
        {
            RangeBase rb = (RangeBase)d;
            double max = (double)baseValue;
            double min = rb.Minimum;
            return Math.Max(min, max);
        }

        private static void OnMaximumChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            RangeBase rb = (RangeBase)d;
            rb.CoerceValue(ValueProperty);
        }

        private static object CoerceValue(DependencyObject d, object baseValue)
        {
            RangeBase rb = (RangeBase)d;
            double val = (double)baseValue;
            double min = rb.Minimum;
            double max = rb.Maximum;
            return Math.Clamp(val, min, max);
        }

        private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            RangeBase rb = (RangeBase)d;
            rb.OnValueChanged((double)e.OldValue, (double)e.NewValue);
        }

        protected virtual void OnValueChanged(double oldValue, double newValue)
        {
            RoutedPropertyChangedEventArgs<double> args = new(oldValue, newValue, ValueChangedEvent);
            RaiseEvent(args);
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace System.Windows.Controls
{
    /// <summary>
    /// Represents a control that lets the user select from a range of values by moving a Thumb control along a track.
    /// </summary>
    public class Slider : RangeBase
    {
        public static readonly DependencyProperty OrientationProperty =
            DependencyProperty.Register(
                nameof(Orientation),
                typeof(Orientation),
                typeof(Slider),
                new FrameworkPropertyMetadata(Orientation.Horizontal, FrameworkPropertyMetadataOptions.AffectsMeasure));

        public static readonly DependencyProperty IsDirectionReversedProperty =
            DependencyProperty.Register(
                nameof(IsDirectionReversed),
                typeof(bool),
                typeof(Slider),
                new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty TickFrequencyProperty =
            DependencyProperty.Register(
                nameof(TickFrequency),
                typeof(double),
                typeof(Slider),
                new FrameworkPropertyMetadata(1.0, FrameworkPropertyMetadataOptions.AffectsRender),
                ValidateTickFrequency);

        public static readonly DependencyProperty TickPlacementProperty =
            DependencyProperty.Register(
                nameof(TickPlacement),
                typeof(TickPlacement),
                typeof(Slider),
                new FrameworkPropertyMetadata(TickPlacement.None, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty IsSnapToTickEnabledProperty =
            DependencyProperty.Register(
                nameof(IsSnapToTickEnabled),
                typeof(bool),
                typeof(Slider),
                new FrameworkPropertyMetadata(false));

        public Orientation Orientation
        {
            get => (Orientation)GetValue(OrientationProperty);
            set => SetValue(OrientationProperty, value);
        }

        public bool IsDirectionReversed
        {
            get => (bool)GetValue(IsDirectionReversedProperty);
            set => SetValue(IsDirectionReversedProperty, value);
        }

        public double TickFrequency
        {
            get => (double)GetValue(TickFrequencyProperty);
            set => SetValue(TickFrequencyProperty, value);
        }

        public TickPlacement TickPlacement
        {
            get => (TickPlacement)GetValue(TickPlacementProperty);
            set => SetValue(TickPlacementProperty, value);
        }

        public bool IsSnapToTickEnabled
        {
            get => (bool)GetValue(IsSnapToTickEnabledProperty);
            set => SetValue(IsSnapToTickEnabledProperty, value);
        }

        static Slider()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(Slider), new FrameworkPropertyMetadata(typeof(Slider)));
            FocusableProperty.OverrideMetadata(typeof(Slider), new FrameworkPropertyMetadata(true));
        }

        public Slider()
        {
        }

        private static bool ValidateTickFrequency(object value)
        {
            double d = (double)value;
            return d > 0.0 && !double.IsNaN(d) && !double.IsInfinity(d);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);

            if (e.Handled) return;

            bool isHorizontal = Orientation == Orientation.Horizontal;
            Key decreaseKey = isHorizontal ? Key.Left : Key.Down;
            Key increaseKey = isHorizontal ? Key.Right : Key.Up;

            if (e.Key == decreaseKey)
            {
                ChangeValue(IsDirectionReversed ? SmallChange : -SmallChange);
                e.Handled = true;
            }
            else if (e.Key == increaseKey)
            {
                ChangeValue(IsDirectionReversed ? -SmallChange : SmallChange);
                e.Handled = true;
            }
            else if (e.Key == Key.PageDown)
            {
                ChangeValue(IsDirectionReversed ? LargeChange : -LargeChange);
                e.Handled = true;
            }
            else if (e.Key == Key.PageUp)
            {
                ChangeValue(IsDirectionReversed ? -LargeChange : LargeChange);
                e.Handled = true;
            }
            else if (e.Key == Key.Home)
            {
                Value = IsDirectionReversed ? Maximum : Minimum;
                e.Handled = true;
            }
            else if (e.Key == Key.End)
            {
                Value = IsDirectionReversed ? Minimum : Maximum;
                e.Handled = true;
            }
        }

        private void ChangeValue(double delta)
        {
            double newVal = Value + delta;
            if (IsSnapToTickEnabled && TickFrequency > 0.0)
            {
                double min = Minimum;
                double steps = Math.Round((newVal - min) / TickFrequency);
                newVal = min + (steps * TickFrequency);
            }
            Value = Math.Clamp(newVal, Minimum, Maximum);
        }
    }
}

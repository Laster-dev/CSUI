// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;

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

        protected override Size MeasureOverride(Size constraint)
        {
            if (Orientation == Orientation.Horizontal)
            {
                double w = double.IsInfinity(constraint.Width) ? 160.0 : Math.Max(60.0, constraint.Width);
                return new Size(w, 24.0);
            }
            else
            {
                double h = double.IsInfinity(constraint.Height) ? 160.0 : Math.Max(60.0, constraint.Height);
                return new Size(24.0, h);
            }
        }

        protected override void OnMouseEnter(MouseEventArgs e)
        {
            base.OnMouseEnter(e);
            InvalidateVisual();
        }

        protected override void OnMouseLeave(MouseEventArgs e)
        {
            base.OnMouseLeave(e);
            InvalidateVisual();
        }

        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonDown(e);
            if (!IsEnabled) return;
            Focus();
            CaptureMouse();
            UpdateValueFromMouse(e.GetPosition(this));
            e.Handled = true;
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (IsMouseCaptured)
            {
                UpdateValueFromMouse(e.GetPosition(this));
                e.Handled = true;
            }
        }

        protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonUp(e);
            if (IsMouseCaptured)
            {
                ReleaseMouseCapture();
                e.Handled = true;
            }
        }

        private void UpdateValueFromMouse(Point pt)
        {
            double min = Minimum;
            double max = Maximum;
            double range = max - min;
            if (range <= 0) return;

            const double thumbRadius = 8.0;
            double pct;

            if (Orientation == Orientation.Horizontal)
            {
                double trackW = Math.Max(1.0, RenderSize.Width - thumbRadius * 2);
                pct = Math.Clamp((pt.X - thumbRadius) / trackW, 0.0, 1.0);
            }
            else
            {
                double trackH = Math.Max(1.0, RenderSize.Height - thumbRadius * 2);
                pct = Math.Clamp((RenderSize.Height - thumbRadius - pt.Y) / trackH, 0.0, 1.0);
            }

            if (IsDirectionReversed) pct = 1.0 - pct;
            double rawVal = min + pct * range;
            if (IsSnapToTickEnabled && TickFrequency > 0.0)
            {
                rawVal = Math.Round((rawVal - min) / TickFrequency) * TickFrequency + min;
            }
            Value = Math.Clamp(rawVal, min, max);
        }

        protected override void OnRender(DrawingContext dc)
        {
            base.OnRender(dc);
            double w = RenderSize.Width;
            double h = RenderSize.Height;
            if (w <= 0 || h <= 0) return;

            dc.DrawRectangle(Brushes.Transparent, null, new Rect(0, 0, w, h));

            double min = Minimum;
            double max = Maximum;
            double range = max - min;
            double val = Value;
            double pct = range > 1e-6 ? Math.Clamp((val - min) / range, 0.0, 1.0) : 0.0;
            if (IsDirectionReversed) pct = 1.0 - pct;

            const double thumbRadius = 7.0;
            Brush trackBg = Background ?? new SolidColorBrush(Color.FromArgb(255, 55, 55, 55));
            Brush accentBrush = Foreground ?? new SolidColorBrush(Color.FromArgb(255, 0, 120, 215));

            if (Orientation == Orientation.Horizontal)
            {
                double trackY = (h - 4.0) * 0.5;
                double trackW = Math.Max(0.0, w - thumbRadius * 2);
                Rect trackRect = new Rect(thumbRadius, trackY, trackW, 4.0);

                // Full track background
                dc.DrawRoundedRectangle(trackBg, null, trackRect, 2.0, 2.0);

                // Active progress portion
                double fillW = trackW * pct;
                if (fillW > 0)
                {
                    dc.DrawRoundedRectangle(accentBrush, null, new Rect(thumbRadius, trackY, fillW, 4.0), 2.0, 2.0);
                }

                // Thumb handle
                Point thumbCenter = new Point(thumbRadius + fillW, h * 0.5);
                Brush thumbBg = IsMouseOver ? Brushes.White : new SolidColorBrush(Color.FromArgb(255, 230, 230, 230));
                Pen thumbBorder = new Pen(accentBrush, 2.0);
                dc.DrawEllipse(thumbBg, thumbBorder, thumbCenter, thumbRadius, thumbRadius);
            }
            else
            {
                double trackX = (w - 4.0) * 0.5;
                double trackH = Math.Max(0.0, h - thumbRadius * 2);
                Rect trackRect = new Rect(trackX, thumbRadius, 4.0, trackH);

                // Full track background
                dc.DrawRoundedRectangle(trackBg, null, trackRect, 2.0, 2.0);

                // Active progress portion (from bottom up)
                double fillH = trackH * pct;
                if (fillH > 0)
                {
                    dc.DrawRoundedRectangle(accentBrush, null, new Rect(trackX, thumbRadius + trackH - fillH, 4.0, fillH), 2.0, 2.0);
                }

                // Thumb handle
                Point thumbCenter = new Point(w * 0.5, thumbRadius + trackH - fillH);
                Brush thumbBg = IsMouseOver ? Brushes.White : new SolidColorBrush(Color.FromArgb(255, 230, 230, 230));
                Pen thumbBorder = new Pen(accentBrush, 2.0);
                dc.DrawEllipse(thumbBg, thumbBorder, thumbCenter, thumbRadius, thumbRadius);
            }
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

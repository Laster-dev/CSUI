// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;

namespace System.Windows.Controls
{
    /// <summary>
    /// Represents a control that lets the user select from a range of values by moving a Thumb control along a track.
    /// </summary>
    public partial class Slider : RangeBase
    {
        static partial void RegisterDefaultTheme();
        public static readonly DependencyProperty OrientationProperty =
            DependencyProperty.Register(
                nameof(Orientation),
                typeof(Orientation),
                typeof(Slider),
                new FrameworkPropertyMetadata(Orientation.Horizontal, FrameworkPropertyMetadataOptions.AffectsMeasure, OnSelectionRangePropertyChanged));

        public static readonly DependencyProperty IsDirectionReversedProperty =
            DependencyProperty.Register(
                nameof(IsDirectionReversed),
                typeof(bool),
                typeof(Slider),
                new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.AffectsRender, OnSelectionRangePropertyChanged));

        public static readonly DependencyProperty TickFrequencyProperty =
            DependencyProperty.Register(
                nameof(TickFrequency),
                typeof(double),
                typeof(Slider),
                new FrameworkPropertyMetadata(1.0, FrameworkPropertyMetadataOptions.AffectsRender),
                ValidateTickFrequency);

        private static DependencyProperty s_tickPlacementProperty;
        public static DependencyProperty TickPlacementProperty =>
            s_tickPlacementProperty ??= DependencyProperty.Register(
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

        private static DependencyProperty s_delayProperty;
        public static DependencyProperty DelayProperty =>
            s_delayProperty ??= RepeatButton.DelayProperty.AddOwner(typeof(Slider));

        private static DependencyProperty s_intervalProperty;
        public static DependencyProperty IntervalProperty =>
            s_intervalProperty ??= RepeatButton.IntervalProperty.AddOwner(typeof(Slider));

        private static DependencyProperty s_autoToolTipPlacementProperty;
        public static DependencyProperty AutoToolTipPlacementProperty =>
            s_autoToolTipPlacementProperty ??= DependencyProperty.Register(
                nameof(AutoToolTipPlacement),
                typeof(AutoToolTipPlacement),
                typeof(Slider),
                new FrameworkPropertyMetadata(AutoToolTipPlacement.None));

        private static DependencyProperty s_autoToolTipPrecisionProperty;
        public static DependencyProperty AutoToolTipPrecisionProperty =>
            s_autoToolTipPrecisionProperty ??= DependencyProperty.Register(
                nameof(AutoToolTipPrecision),
                typeof(int),
                typeof(Slider),
                new FrameworkPropertyMetadata(0));

        private static DependencyProperty s_isSelectionRangeEnabledProperty;
        public static DependencyProperty IsSelectionRangeEnabledProperty =>
            s_isSelectionRangeEnabledProperty ??= DependencyProperty.Register(
                nameof(IsSelectionRangeEnabled),
                typeof(bool),
                typeof(Slider),
                new FrameworkPropertyMetadata(false, OnSelectionRangePropertyChanged));

        private static DependencyProperty s_selectionStartProperty;
        public static DependencyProperty SelectionStartProperty =>
            s_selectionStartProperty ??= DependencyProperty.Register(
                nameof(SelectionStart),
                typeof(double),
                typeof(Slider),
                new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnSelectionRangePropertyChanged));

        private static DependencyProperty s_selectionEndProperty;
        public static DependencyProperty SelectionEndProperty =>
            s_selectionEndProperty ??= DependencyProperty.Register(
                nameof(SelectionEnd),
                typeof(double),
                typeof(Slider),
                new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnSelectionRangePropertyChanged));

        private static DependencyProperty s_ticksProperty;
        public static DependencyProperty TicksProperty =>
            s_ticksProperty ??= DependencyProperty.Register(
                nameof(Ticks),
                typeof(DoubleCollection),
                typeof(Slider),
                new FrameworkPropertyMetadata((DoubleCollection)null));

        private static RoutedCommand s_decreaseLarge;
        public static RoutedCommand DecreaseLarge => s_decreaseLarge ??= new RoutedCommand(nameof(DecreaseLarge), typeof(Slider));

        private static RoutedCommand s_increaseLarge;
        public static RoutedCommand IncreaseLarge => s_increaseLarge ??= new RoutedCommand(nameof(IncreaseLarge), typeof(Slider));

        private static RoutedCommand s_decreaseSmall;
        public static RoutedCommand DecreaseSmall => s_decreaseSmall ??= new RoutedCommand(nameof(DecreaseSmall), typeof(Slider));

        private static RoutedCommand s_increaseSmall;
        public static RoutedCommand IncreaseSmall => s_increaseSmall ??= new RoutedCommand(nameof(IncreaseSmall), typeof(Slider));

        private static RoutedCommand s_minimizeValue;
        public static RoutedCommand MinimizeValue => s_minimizeValue ??= new RoutedCommand(nameof(MinimizeValue), typeof(Slider));

        private static RoutedCommand s_maximizeValue;
        public static RoutedCommand MaximizeValue => s_maximizeValue ??= new RoutedCommand(nameof(MaximizeValue), typeof(Slider));

        private Track _track;
        private FrameworkElement _selectionRange;
        private ToolTip _autoToolTip;
        private double _dragUnsnappedValue;
        internal Track Track => _track;

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

        public int Delay
        {
            get => (int)GetValue(DelayProperty);
            set => SetValue(DelayProperty, value);
        }

        public int Interval
        {
            get => (int)GetValue(IntervalProperty);
            set => SetValue(IntervalProperty, value);
        }

        public AutoToolTipPlacement AutoToolTipPlacement
        {
            get => (AutoToolTipPlacement)GetValue(AutoToolTipPlacementProperty);
            set => SetValue(AutoToolTipPlacementProperty, value);
        }

        public int AutoToolTipPrecision
        {
            get => (int)GetValue(AutoToolTipPrecisionProperty);
            set => SetValue(AutoToolTipPrecisionProperty, value);
        }

        public bool IsSelectionRangeEnabled
        {
            get => (bool)GetValue(IsSelectionRangeEnabledProperty);
            set => SetValue(IsSelectionRangeEnabledProperty, value);
        }

        public double SelectionStart
        {
            get => (double)GetValue(SelectionStartProperty);
            set => SetValue(SelectionStartProperty, value);
        }

        public double SelectionEnd
        {
            get => (double)GetValue(SelectionEndProperty);
            set => SetValue(SelectionEndProperty, value);
        }

        public DoubleCollection Ticks
        {
            get => (DoubleCollection)GetValue(TicksProperty);
            set => SetValue(TicksProperty, value);
        }

        public override void OnApplyTemplate()
        {
            if (_track != null)
            {
                _track.SizeChanged -= OnTrackSizeChanged;
            }

            base.OnApplyTemplate();
            _track = GetTemplateChild("PART_Track") as Track;
            _selectionRange = GetTemplateChild("PART_SelectionRange") as FrameworkElement;

            if (_track != null)
            {
                _track.SizeChanged += OnTrackSizeChanged;
            }

            UpdateSelectionRangeElementPositionAndSize();
        }

        private void OnTrackSizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateSelectionRangeElementPositionAndSize();
        }

        private static void OnSelectionRangePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is Slider slider)
            {
                slider.UpdateSelectionRangeElementPositionAndSize();
            }
        }

        static Slider()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(Slider), new FrameworkPropertyMetadata(typeof(Slider)));
            FocusableProperty.OverrideMetadata(typeof(Slider), new FrameworkPropertyMetadata(true));
            MinimumProperty.OverrideMetadata(typeof(Slider), new FrameworkPropertyMetadata(0.0, OnSelectionRangePropertyChanged));
            MaximumProperty.OverrideMetadata(typeof(Slider), new FrameworkPropertyMetadata(10.0, OnSelectionRangePropertyChanged));
            EventManager.RegisterClassHandler(typeof(Slider), Thumb.DragStartedEvent, new DragStartedEventHandler(OnThumbDragStartedThunk));
            EventManager.RegisterClassHandler(typeof(Slider), Thumb.DragDeltaEvent, new DragDeltaEventHandler(OnThumbDragDeltaThunk));
            EventManager.RegisterClassHandler(typeof(Slider), Thumb.DragCompletedEvent, new DragCompletedEventHandler(OnThumbDragCompletedThunk));
            CommandManager.RegisterClassCommandBinding(typeof(Slider), new CommandBinding(DecreaseLarge, OnDecreaseLargeCommand));
            CommandManager.RegisterClassCommandBinding(typeof(Slider), new CommandBinding(IncreaseLarge, OnIncreaseLargeCommand));
            RegisterDefaultTheme();
        }

        private static void OnThumbDragStartedThunk(object sender, DragStartedEventArgs e)
        {
            if (sender is Slider slider)
            {
                slider.OnThumbDragStarted(e);
            }
        }

        private static void OnThumbDragDeltaThunk(object sender, DragDeltaEventArgs e)
        {
            if (sender is Slider slider && slider._track != null)
            {
                double delta = slider._track.ValueFromDistance(e.HorizontalChange, e.VerticalChange);
                if (!double.IsNaN(delta) && !double.IsInfinity(delta))
                {
                    slider.OnThumbDragDelta(delta);
                }
            }
        }

        private static void OnThumbDragCompletedThunk(object sender, DragCompletedEventArgs e)
        {
            if (sender is Slider slider)
            {
                slider.OnThumbDragCompleted(e);
            }
        }

        private void OnThumbDragStarted(DragStartedEventArgs e)
        {
            _dragUnsnappedValue = Value;

            if (AutoToolTipPlacement != AutoToolTipPlacement.None)
            {
                if (_autoToolTip == null)
                {
                    _autoToolTip = new ToolTip();
                }

                _autoToolTip.Placement = AutoToolTipPlacement == AutoToolTipPlacement.TopLeft
                    ? (Orientation == Orientation.Horizontal ? PlacementMode.Top : PlacementMode.Left)
                    : (Orientation == Orientation.Horizontal ? PlacementMode.Bottom : PlacementMode.Right);
                _autoToolTip.PlacementTarget = _track?.Thumb ?? (UIElement)this;
                _autoToolTip.Content = GetAutoToolTipNumber();
                _autoToolTip.IsOpen = true;
            }
        }

        private void OnThumbDragDelta(double delta)
        {
            if (delta != 0.0)
            {
                _dragUnsnappedValue = Math.Clamp(_dragUnsnappedValue + delta, Minimum, Maximum);
                UpdateValue(_dragUnsnappedValue);
            }

            if (_autoToolTip != null && _autoToolTip.IsOpen)
            {
                _autoToolTip.Content = GetAutoToolTipNumber();
            }
        }

        private void OnThumbDragCompleted(DragCompletedEventArgs e)
        {
            if (_autoToolTip != null)
            {
                _autoToolTip.IsOpen = false;
            }
        }

        private string GetAutoToolTipNumber()
        {
            int precision = Math.Max(0, AutoToolTipPrecision);
            return Value.ToString("F" + precision, CultureInfo.CurrentCulture);
        }

        private static void OnDecreaseLargeCommand(object sender, ExecutedRoutedEventArgs e)
        {
            if (sender is Slider slider)
            {
                slider.ChangeValue(-slider.LargeChange);
            }
        }

        private static void OnIncreaseLargeCommand(object sender, ExecutedRoutedEventArgs e)
        {
            if (sender is Slider slider)
            {
                slider.ChangeValue(slider.LargeChange);
            }
        }

        public Slider()
        {
        }

        protected override Size MeasureOverride(Size constraint)
        {
            if (Template != null)
            {
                return base.MeasureOverride(constraint);
            }
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

        protected override void OnPreviewMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            base.OnPreviewMouseLeftButtonDown(e);
            if (IsEnabled && Focusable && !IsKeyboardFocusWithin)
            {
                Focus();
            }
        }

        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonDown(e);
            if (_track != null || Template != null || !IsEnabled) return;
            Focus();
            CaptureMouse();
            UpdateValueFromMouse(e.GetPosition(this));
            e.Handled = true;
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (_track == null && IsMouseCaptured)
            {
                UpdateValueFromMouse(e.GetPosition(this));
                e.Handled = true;
            }
        }

        protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonUp(e);
            if (_track == null && IsMouseCaptured)
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
            if (Template != null) return;
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

        protected override Size ArrangeOverride(Size arrangeBounds)
        {
            Size arranged = base.ArrangeOverride(arrangeBounds);
            UpdateSelectionRangeElementPositionAndSize();
            return arranged;
        }

        private void UpdateSelectionRangeElementPositionAndSize()
        {
            if (_track == null || _selectionRange == null)
            {
                return;
            }

            if (!IsSelectionRangeEnabled)
            {
                if (Orientation == Orientation.Horizontal)
                {
                    _selectionRange.Width = 0.0;
                }
                else
                {
                    _selectionRange.Height = 0.0;
                }
                return;
            }

            double min = Minimum;
            double max = Maximum;
            double range = max - min;
            if (range <= 0.0)
            {
                return;
            }

            double start = Math.Clamp(SelectionStart, min, max);
            double end = Math.Clamp(SelectionEnd, min, max);
            if (end < start)
            {
                (start, end) = (end, start);
            }

            double thumbW = _track.Thumb != null ? _track.Thumb.ActualWidth : 0.0;
            double thumbH = _track.Thumb != null ? _track.Thumb.ActualHeight : 0.0;

            if (Orientation == Orientation.Horizontal)
            {
                double usableWidth = Math.Max(0.0, _track.ActualWidth - thumbW);
                double valueToSize = usableWidth / range;
                _selectionRange.Width = (end - start) * valueToSize;
                double left = IsDirectionReversed
                    ? (thumbW * 0.5) + ((max - end) * valueToSize)
                    : (thumbW * 0.5) + ((start - min) * valueToSize);
                Canvas.SetLeft(_selectionRange, left);
            }
            else
            {
                double usableHeight = Math.Max(0.0, _track.ActualHeight - thumbH);
                double valueToSize = usableHeight / range;
                _selectionRange.Height = (end - start) * valueToSize;
                double top = IsDirectionReversed
                    ? (thumbH * 0.5) + ((start - min) * valueToSize)
                    : (thumbH * 0.5) + ((max - end) * valueToSize);
                Canvas.SetTop(_selectionRange, top);
            }
        }

        private void ChangeValue(double delta)
        {
            UpdateValue(Value + delta);
        }

        private void UpdateValue(double value)
        {
            double snapped = SnapToTick(value);
            Value = Math.Clamp(snapped, Minimum, Maximum);
        }

        private double SnapToTick(double value)
        {
            if (!IsSnapToTickEnabled)
            {
                return value;
            }

            double previous = Minimum;
            double next = Maximum;

            DoubleCollection ticks = Ticks;
            if (ticks != null && ticks.Count > 0)
            {
                foreach (double tick in ticks)
                {
                    if (tick >= previous && tick <= value)
                    {
                        previous = tick;
                    }
                    if (tick <= next && tick >= value)
                    {
                        next = tick;
                    }
                }
            }
            else if (TickFrequency > 0.0)
            {
                double freq = TickFrequency;
                previous = Minimum + (Math.Floor((value - Minimum) / freq) * freq);
                next = Math.Min(Maximum, previous + freq);
            }

            return Math.Abs(value - previous) <= Math.Abs(next - value) ? previous : next;
        }
    }
}

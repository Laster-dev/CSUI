// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace System.Windows.Controls.Primitives
{
    /// <summary>
    /// Represents a control that provides a scroll bar that has a sliding Thumb whose position
    /// corresponds to a value.
    /// </summary>
    public class ScrollBar : Control
    {
        public static readonly DependencyProperty OrientationProperty =
            DependencyProperty.Register(
                nameof(Orientation),
                typeof(Orientation),
                typeof(ScrollBar),
                new FrameworkPropertyMetadata(Orientation.Vertical, FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsArrange));

        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register(
                nameof(Value),
                typeof(double),
                typeof(ScrollBar),
                new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnValueChanged, CoerceValue));

        public static readonly DependencyProperty MinimumProperty =
            DependencyProperty.Register(
                nameof(Minimum),
                typeof(double),
                typeof(ScrollBar),
                new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsArrange, OnMinimumChanged));

        public static readonly DependencyProperty MaximumProperty =
            DependencyProperty.Register(
                nameof(Maximum),
                typeof(double),
                typeof(ScrollBar),
                new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsArrange, OnMaximumChanged, CoerceMaximum));

        public static readonly DependencyProperty ViewportSizeProperty =
            DependencyProperty.Register(
                nameof(ViewportSize),
                typeof(double),
                typeof(ScrollBar),
                new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsArrange));

        public static readonly DependencyProperty SmallChangeProperty =
            DependencyProperty.Register(
                nameof(SmallChange),
                typeof(double),
                typeof(ScrollBar),
                new FrameworkPropertyMetadata(16.0));

        public static readonly DependencyProperty LargeChangeProperty =
            DependencyProperty.Register(
                nameof(LargeChange),
                typeof(double),
                typeof(ScrollBar),
                new FrameworkPropertyMetadata(100.0));

        public static readonly RoutedEvent ScrollEvent =
            EventManager.RegisterRoutedEvent(nameof(Scroll), RoutingStrategy.Bubble, typeof(ScrollEventHandler), typeof(ScrollBar));

        public Orientation Orientation
        {
            get => (Orientation)GetValue(OrientationProperty);
            set => SetValue(OrientationProperty, value);
        }

        public double Value
        {
            get => (double)GetValue(ValueProperty);
            set => SetValue(ValueProperty, value);
        }

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

        public double ViewportSize
        {
            get => (double)GetValue(ViewportSizeProperty);
            set => SetValue(ViewportSizeProperty, value);
        }

        public double SmallChange
        {
            get => (double)GetValue(SmallChangeProperty);
            set => SetValue(SmallChangeProperty, value);
        }

        public double LargeChange
        {
            get => (double)GetValue(LargeChangeProperty);
            set => SetValue(LargeChangeProperty, value);
        }

        public event ScrollEventHandler Scroll
        {
            add => AddHandler(ScrollEvent, value);
            remove => RemoveHandler(ScrollEvent, value);
        }

        private readonly Thumb _thumb;
        private Rect _thumbRect;

        public ScrollBar()
        {
            Focusable = false;
            _thumb = new Thumb();
            AddVisualChild(_thumb);
            _thumb.DragDelta += OnThumbDragDelta;
            _thumb.DragCompleted += OnThumbDragCompleted;
        }

        protected override int VisualChildrenCount => 1;

        protected override Visual GetVisualChild(int index)
        {
            if (index == 0) return _thumb;
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var sb = (ScrollBar)d;
            sb.InvalidateArrange();
        }

        private static object CoerceValue(DependencyObject d, object baseValue)
        {
            var sb = (ScrollBar)d;
            double val = (double)baseValue;
            if (val < sb.Minimum) val = sb.Minimum;
            if (val > sb.Maximum) val = sb.Maximum;
            return val;
        }

        private static void OnMinimumChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            d.CoerceValue(MaximumProperty);
            d.CoerceValue(ValueProperty);
        }

        private static void OnMaximumChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            d.CoerceValue(ValueProperty);
        }

        private static object CoerceMaximum(DependencyObject d, object baseValue)
        {
            var sb = (ScrollBar)d;
            double val = (double)baseValue;
            if (val < sb.Minimum) val = sb.Minimum;
            return val;
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            const double DefaultThickness = 10.0;
            if (Orientation == Orientation.Vertical)
            {
                double desiredW = double.IsNaN(Width) ? DefaultThickness : Width;
                _thumb.Measure(new Size(desiredW, availableSize.Height));
                return new Size(desiredW, double.IsInfinity(availableSize.Height) ? 0.0 : availableSize.Height);
            }
            else
            {
                double desiredH = double.IsNaN(Height) ? DefaultThickness : Height;
                _thumb.Measure(new Size(availableSize.Width, desiredH));
                return new Size(double.IsInfinity(availableSize.Width) ? 0.0 : availableSize.Width, desiredH);
            }
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            bool isVert = Orientation == Orientation.Vertical;
            double trackLength = isVert ? finalSize.Height : finalSize.Width;
            double crossLength = isVert ? finalSize.Width : finalSize.Height;
            double range = Maximum - Minimum;

            if (trackLength <= 0 || range <= 0)
            {
                _thumbRect = Rect.Empty;
                _thumb.Arrange(new Rect(0, 0, 0, 0));
                return finalSize;
            }

            double viewport = ViewportSize;
            double thumbLength;
            if (viewport > 0)
            {
                thumbLength = trackLength * (viewport / (range + viewport));
            }
            else
            {
                thumbLength = 30.0;
            }

            const double MinThumbLength = 18.0;
            thumbLength = Math.Max(MinThumbLength, Math.Min(trackLength, thumbLength));

            double availableTrack = trackLength - thumbLength;
            double offset = availableTrack > 0
                ? availableTrack * ((Value - Minimum) / range)
                : 0.0;

            if (isVert)
            {
                _thumbRect = new Rect(1, offset, Math.Max(0, crossLength - 2), thumbLength);
            }
            else
            {
                _thumbRect = new Rect(offset, 1, thumbLength, Math.Max(0, crossLength - 2));
            }

            _thumb.Arrange(_thumbRect);
            return finalSize;
        }

        private void OnThumbDragDelta(object sender, DragDeltaEventArgs e)
        {
            bool isVert = Orientation == Orientation.Vertical;
            double trackLength = isVert ? RenderSize.Height : RenderSize.Width;
            double thumbLength = isVert ? _thumbRect.Height : _thumbRect.Width;
            double availableTrack = trackLength - thumbLength;
            double range = Maximum - Minimum;

            if (availableTrack <= 0 || range <= 0) return;

            double change = isVert ? e.VerticalChange : e.HorizontalChange;
            double valDelta = change * (range / availableTrack);
            double newVal = Math.Clamp(Value + valDelta, Minimum, Maximum);

            if (newVal != Value)
            {
                Value = newVal;
                RaiseEvent(new ScrollEventArgs(ScrollEventType.ThumbTrack, newVal));
            }
        }

        private void OnThumbDragCompleted(object sender, DragCompletedEventArgs e)
        {
            RaiseEvent(new ScrollEventArgs(ScrollEventType.EndScroll, Value));
        }

        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonDown(e);
            if (e.Handled) return;

            Point pt = e.GetPosition(this);
            bool isVert = Orientation == Orientation.Vertical;
            double clickPos = isVert ? pt.Y : pt.X;
            double thumbStart = isVert ? _thumbRect.Top : _thumbRect.Left;
            double thumbEnd = isVert ? _thumbRect.Bottom : _thumbRect.Right;

            if (clickPos < thumbStart)
            {
                // Page up / large decrement
                double newVal = Math.Clamp(Value - LargeChange, Minimum, Maximum);
                Value = newVal;
                RaiseEvent(new ScrollEventArgs(ScrollEventType.LargeDecrement, newVal));
                e.Handled = true;
            }
            else if (clickPos > thumbEnd)
            {
                // Page down / large increment
                double newVal = Math.Clamp(Value + LargeChange, Minimum, Maximum);
                Value = newVal;
                RaiseEvent(new ScrollEventArgs(ScrollEventType.LargeIncrement, newVal));
                e.Handled = true;
            }
        }

        protected override void OnRender(DrawingContext dc)
        {
            // Subtle track background on mouse hover
            if (IsMouseOver)
            {
                var trackBrush = new SolidColorBrush(Color.FromArgb(25, 0, 0, 0));
                dc.DrawRoundedRectangle(trackBrush, null, new Rect(0, 0, RenderSize.Width, RenderSize.Height), 4, 4);
            }
        }
    }
}

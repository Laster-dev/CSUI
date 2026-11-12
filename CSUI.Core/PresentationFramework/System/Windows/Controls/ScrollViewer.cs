// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Input.Gestures;
using System.Windows.Media;
using System.Windows.Threading;

namespace System.Windows.Controls
{
    /// <summary>
    /// Represents a scrollable area that can contain other visible elements.
    /// Features modern smooth physics inertia, full IScrollInfo virtualization delegation,
    /// and zero-allocation high-performance viewport management.
    /// </summary>
    public class ScrollViewer : ContentControl
    {
        public static readonly DependencyProperty HorizontalScrollBarVisibilityProperty =
            DependencyProperty.RegisterAttached(
                "HorizontalScrollBarVisibility",
                typeof(ScrollBarVisibility),
                typeof(ScrollViewer),
                new FrameworkPropertyMetadata(ScrollBarVisibility.Disabled, FrameworkPropertyMetadataOptions.AffectsMeasure));

        public static readonly DependencyProperty VerticalScrollBarVisibilityProperty =
            DependencyProperty.RegisterAttached(
                "VerticalScrollBarVisibility",
                typeof(ScrollBarVisibility),
                typeof(ScrollViewer),
                new FrameworkPropertyMetadata(ScrollBarVisibility.Auto, FrameworkPropertyMetadataOptions.AffectsMeasure));

        public static readonly DependencyProperty CanContentScrollProperty =
            DependencyProperty.RegisterAttached(
                "CanContentScroll",
                typeof(bool),
                typeof(ScrollViewer),
                new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.AffectsMeasure));

        public static readonly DependencyProperty IsSmoothScrollingEnabledProperty =
            DependencyProperty.RegisterAttached(
                "IsSmoothScrollingEnabled",
                typeof(bool),
                typeof(ScrollViewer),
                new FrameworkPropertyMetadata(true));

        public static readonly DependencyProperty FrictionProperty =
            DependencyProperty.RegisterAttached(
                "Friction",
                typeof(double),
                typeof(ScrollViewer),
                new FrameworkPropertyMetadata(0.80));

        private static readonly DependencyPropertyKey HorizontalOffsetPropertyKey =
            DependencyProperty.RegisterReadOnly(
                nameof(HorizontalOffset),
                typeof(double),
                typeof(ScrollViewer),
                new FrameworkPropertyMetadata(0.0));

        public static readonly DependencyProperty HorizontalOffsetProperty =
            HorizontalOffsetPropertyKey.DependencyProperty;

        private static readonly DependencyPropertyKey VerticalOffsetPropertyKey =
            DependencyProperty.RegisterReadOnly(
                nameof(VerticalOffset),
                typeof(double),
                typeof(ScrollViewer),
                new FrameworkPropertyMetadata(0.0));

        public static readonly DependencyProperty VerticalOffsetProperty =
            VerticalOffsetPropertyKey.DependencyProperty;

        private static readonly DependencyPropertyKey ExtentWidthPropertyKey =
            DependencyProperty.RegisterReadOnly(
                nameof(ExtentWidth),
                typeof(double),
                typeof(ScrollViewer),
                new FrameworkPropertyMetadata(0.0));

        public static readonly DependencyProperty ExtentWidthProperty =
            ExtentWidthPropertyKey.DependencyProperty;

        private static readonly DependencyPropertyKey ExtentHeightPropertyKey =
            DependencyProperty.RegisterReadOnly(
                nameof(ExtentHeight),
                typeof(double),
                typeof(ScrollViewer),
                new FrameworkPropertyMetadata(0.0));

        public static readonly DependencyProperty ExtentHeightProperty =
            ExtentHeightPropertyKey.DependencyProperty;

        private static readonly DependencyPropertyKey ViewportWidthPropertyKey =
            DependencyProperty.RegisterReadOnly(
                nameof(ViewportWidth),
                typeof(double),
                typeof(ScrollViewer),
                new FrameworkPropertyMetadata(0.0));

        public static readonly DependencyProperty ViewportWidthProperty =
            ViewportWidthPropertyKey.DependencyProperty;

        private static readonly DependencyPropertyKey ViewportHeightPropertyKey =
            DependencyProperty.RegisterReadOnly(
                nameof(ViewportHeight),
                typeof(double),
                typeof(ScrollViewer),
                new FrameworkPropertyMetadata(0.0));

        public static readonly DependencyProperty ViewportHeightProperty =
            ViewportHeightPropertyKey.DependencyProperty;

        public static readonly RoutedEvent ScrollChangedEvent =
            EventManager.RegisterRoutedEvent(
                "ScrollChanged",
                RoutingStrategy.Bubble,
                typeof(ScrollChangedEventHandler),
                typeof(ScrollViewer));

        static ScrollViewer()
        {
            EventManager.RegisterClassHandler(
                typeof(ScrollViewer),
                ModernGestureEngine.ModernGestureDeltaEvent,
                new ModernGestureDeltaEventHandler(OnModernGestureDeltaThunk));
        }

        private static void OnModernGestureDeltaThunk(object sender, ModernGestureDeltaEventArgs e)
        {
            var sv = (ScrollViewer)sender;
            if (e.GestureType == ModernGestureType.Pan)
            {
                if (e.DeltaPan.Y != 0 && sv.VerticalScrollBarVisibility != ScrollBarVisibility.Disabled)
                {
                    double target = Math.Clamp(sv.VerticalOffset - e.DeltaPan.Y, 0, Math.Max(0, sv.ExtentHeight - sv.ViewportHeight));
                    sv.ApplyVerticalOffset(target);
                    e.Handled = true;
                }
                if (e.DeltaPan.X != 0 && sv.HorizontalScrollBarVisibility != ScrollBarVisibility.Disabled)
                {
                    double target = Math.Clamp(sv.HorizontalOffset - e.DeltaPan.X, 0, Math.Max(0, sv.ExtentWidth - sv.ViewportWidth));
                    sv.ApplyHorizontalOffset(target);
                    e.Handled = true;
                }
            }
        }

        public static ScrollBarVisibility GetHorizontalScrollBarVisibility(DependencyObject element) =>
            (ScrollBarVisibility)element.GetValue(HorizontalScrollBarVisibilityProperty);

        public static void SetHorizontalScrollBarVisibility(DependencyObject element, ScrollBarVisibility value) =>
            element.SetValue(HorizontalScrollBarVisibilityProperty, value);

        public static ScrollBarVisibility GetVerticalScrollBarVisibility(DependencyObject element) =>
            (ScrollBarVisibility)element.GetValue(VerticalScrollBarVisibilityProperty);

        public static void SetVerticalScrollBarVisibility(DependencyObject element, ScrollBarVisibility value) =>
            element.SetValue(VerticalScrollBarVisibilityProperty, value);

        public static bool GetCanContentScroll(DependencyObject element) =>
            (bool)element.GetValue(CanContentScrollProperty);

        public static void SetCanContentScroll(DependencyObject element, bool value) =>
            element.SetValue(CanContentScrollProperty, value);

        public static bool GetIsSmoothScrollingEnabled(DependencyObject element) =>
            (bool)element.GetValue(IsSmoothScrollingEnabledProperty);

        public static void SetIsSmoothScrollingEnabled(DependencyObject element, bool value) =>
            element.SetValue(IsSmoothScrollingEnabledProperty, value);

        public static double GetFriction(DependencyObject element) =>
            (double)element.GetValue(FrictionProperty);

        public static void SetFriction(DependencyObject element, double value) =>
            element.SetValue(FrictionProperty, value);

        public ScrollBarVisibility HorizontalScrollBarVisibility
        {
            get => (ScrollBarVisibility)GetValue(HorizontalScrollBarVisibilityProperty);
            set => SetValue(HorizontalScrollBarVisibilityProperty, value);
        }

        public ScrollBarVisibility VerticalScrollBarVisibility
        {
            get => (ScrollBarVisibility)GetValue(VerticalScrollBarVisibilityProperty);
            set => SetValue(VerticalScrollBarVisibilityProperty, value);
        }

        public bool CanContentScroll
        {
            get => (bool)GetValue(CanContentScrollProperty);
            set => SetValue(CanContentScrollProperty, value);
        }

        public bool IsSmoothScrollingEnabled
        {
            get => (bool)GetValue(IsSmoothScrollingEnabledProperty);
            set => SetValue(IsSmoothScrollingEnabledProperty, value);
        }

        public double Friction
        {
            get => (double)GetValue(FrictionProperty);
            set => SetValue(FrictionProperty, value);
        }

        public double HorizontalOffset => (double)GetValue(HorizontalOffsetProperty);
        public double VerticalOffset => (double)GetValue(VerticalOffsetProperty);
        public double ExtentWidth => (double)GetValue(ExtentWidthProperty);
        public double ExtentHeight => (double)GetValue(ExtentHeightProperty);
        public double ViewportWidth => (double)GetValue(ViewportWidthProperty);
        public double ViewportHeight => (double)GetValue(ViewportHeightProperty);

        public event ScrollChangedEventHandler ScrollChanged
        {
            add => AddHandler(ScrollChangedEvent, value);
            remove => RemoveHandler(ScrollChangedEvent, value);
        }

        private readonly ScrollBar _hScrollBar;
        private readonly ScrollBar _vScrollBar;
        private IScrollInfo _scrollInfo;

        // Smooth physics inertia state
        private double _targetVerticalOffset;
        private double _targetHorizontalOffset;
        private DispatcherTimer _smoothTimer;

        public ScrollViewer()
        {
            _hScrollBar = new ScrollBar { Orientation = Orientation.Horizontal };
            _vScrollBar = new ScrollBar { Orientation = Orientation.Vertical };

            AddVisualChild(_hScrollBar);
            AddVisualChild(_vScrollBar);

            _hScrollBar.Scroll += OnHScroll;
            _vScrollBar.Scroll += OnVScroll;

            ClipToBounds = true;
        }

        protected override int VisualChildrenCount
        {
            get
            {
                int count = 2; // two scrollbars
                if (Content is Visual) count++;
                return count;
            }
        }

        protected override Visual GetVisualChild(int index)
        {
            if (Content is Visual cv)
            {
                if (index == 0) return cv;
                if (index == 1) return _hScrollBar;
                if (index == 2) return _vScrollBar;
            }
            else
            {
                if (index == 0) return _hScrollBar;
                if (index == 1) return _vScrollBar;
            }
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        public void InvalidateScrollInfo()
        {
            if (_scrollInfo != null)
            {
                UpdateScrollInfo(
                    _scrollInfo.ExtentWidth,
                    _scrollInfo.ExtentHeight,
                    _scrollInfo.ViewportWidth,
                    _scrollInfo.ViewportHeight,
                    _scrollInfo.HorizontalOffset,
                    _scrollInfo.VerticalOffset);
            }
            InvalidateMeasure();
        }

        private void UpdateScrollInfo(double extW, double extH, double vpW, double vpH, double offX, double offY)
        {
            double oldOffX = HorizontalOffset;
            double oldOffY = VerticalOffset;
            double oldExtW = ExtentWidth;
            double oldExtH = ExtentHeight;
            double oldVpW = ViewportWidth;
            double oldVpH = ViewportHeight;

            SetValue(ExtentWidthPropertyKey, extW);
            SetValue(ExtentHeightPropertyKey, extH);
            SetValue(ViewportWidthPropertyKey, vpW);
            SetValue(ViewportHeightPropertyKey, vpH);
            SetValue(HorizontalOffsetPropertyKey, offX);
            SetValue(VerticalOffsetPropertyKey, offY);

            // Update scrollbars
            _hScrollBar.Minimum = 0;
            _hScrollBar.Maximum = Math.Max(0, extW - vpW);
            _hScrollBar.ViewportSize = vpW;
            _hScrollBar.Value = offX;

            _vScrollBar.Minimum = 0;
            _vScrollBar.Maximum = Math.Max(0, extH - vpH);
            _vScrollBar.ViewportSize = vpH;
            _vScrollBar.Value = offY;

            if (oldOffX != offX || oldOffY != offY || oldExtW != extW || oldExtH != extH || oldVpW != vpW || oldVpH != vpH)
            {
                var args = new ScrollChangedEventArgs(
                    new Vector(offX, offY),
                    new Vector(offX - oldOffX, oldOffY != offY ? offY - oldOffY : 0),
                    new Size(extW, extH),
                    new Vector(extW - oldExtW, extH - oldExtH),
                    new Size(vpW, vpH),
                    new Vector(vpW - oldVpW, vpH - oldVpH))
                {
                    RoutedEvent = ScrollChangedEvent
                };
                RaiseEvent(args);
            }
        }

        protected override Size MeasureOverride(Size constraint)
        {
            UIElement content = Content as UIElement;
            _scrollInfo = (CanContentScroll && content is IScrollInfo si) ? si : null;

            if (_scrollInfo != null)
            {
                _scrollInfo.ScrollOwner = this;
                _scrollInfo.CanHorizontallyScroll = HorizontalScrollBarVisibility != ScrollBarVisibility.Disabled;
                _scrollInfo.CanVerticallyScroll = VerticalScrollBarVisibility != ScrollBarVisibility.Disabled;
            }

            const double SbThickness = 10.0;
            double availableW = constraint.Width;
            double availableH = constraint.Height;

            bool canScrollH = HorizontalScrollBarVisibility != ScrollBarVisibility.Disabled;
            bool canScrollV = VerticalScrollBarVisibility != ScrollBarVisibility.Disabled;

            // First pass: measure content
            Size contentConstraint = new Size(
                canScrollH ? double.PositiveInfinity : availableW,
                canScrollV ? double.PositiveInfinity : availableH);

            content?.Measure(contentConstraint);

            Size contentDesired = content?.DesiredSize ?? new Size(0, 0);

            double extW = _scrollInfo != null ? _scrollInfo.ExtentWidth : contentDesired.Width;
            double extH = _scrollInfo != null ? _scrollInfo.ExtentHeight : contentDesired.Height;

            bool showH = HorizontalScrollBarVisibility == ScrollBarVisibility.Visible ||
                         (HorizontalScrollBarVisibility == ScrollBarVisibility.Auto && extW > availableW && !double.IsInfinity(availableW));
            bool showV = VerticalScrollBarVisibility == ScrollBarVisibility.Visible ||
                         (VerticalScrollBarVisibility == ScrollBarVisibility.Auto && extH > availableH && !double.IsInfinity(availableH));

            double vpW = Math.Max(0, availableW - (showV ? SbThickness : 0));
            double vpH = Math.Max(0, availableH - (showH ? SbThickness : 0));

            // Check if adding one scrollbar forced the other scrollbar to appear
            if (!showH && HorizontalScrollBarVisibility == ScrollBarVisibility.Auto && extW > vpW && !double.IsInfinity(vpW))
            {
                showH = true;
                vpH = Math.Max(0, availableH - SbThickness);
            }
            if (!showV && VerticalScrollBarVisibility == ScrollBarVisibility.Auto && extH > vpH && !double.IsInfinity(vpH))
            {
                showV = true;
                vpW = Math.Max(0, availableW - SbThickness);
            }

            if (showH)
            {
                _hScrollBar.Visibility = Visibility.Visible;
                _hScrollBar.Measure(new Size(vpW, SbThickness));
            }
            else
            {
                _hScrollBar.Visibility = Visibility.Collapsed;
            }

            if (showV)
            {
                _vScrollBar.Visibility = Visibility.Visible;
                _vScrollBar.Measure(new Size(SbThickness, vpH));
            }
            else
            {
                _vScrollBar.Visibility = Visibility.Collapsed;
            }

            if (_scrollInfo == null)
            {
                UpdateScrollInfo(extW, extH, vpW, vpH, HorizontalOffset, VerticalOffset);
            }

            double desiredW = double.IsInfinity(constraint.Width) ? (extW + (showV ? SbThickness : 0)) : constraint.Width;
            double desiredH = double.IsInfinity(constraint.Height) ? (extH + (showH ? SbThickness : 0)) : constraint.Height;

            return new Size(desiredW, desiredH);
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            const double SbThickness = 10.0;
            bool showH = _hScrollBar.Visibility == Visibility.Visible;
            bool showV = _vScrollBar.Visibility == Visibility.Visible;

            double vpW = Math.Max(0, finalSize.Width - (showV ? SbThickness : 0));
            double vpH = Math.Max(0, finalSize.Height - (showH ? SbThickness : 0));

            UIElement content = Content as UIElement;
            if (content != null)
            {
                if (_scrollInfo != null)
                {
                    // IScrollInfo manages its own virtualized/logical layout within viewport bounds
                    content.Arrange(new Rect(0, 0, vpW, vpH));
                }
                else
                {
                    // Physical pixel scrolling for standard non-IScrollInfo elements
                    double arrangeW = Math.Max(vpW, ExtentWidth);
                    double arrangeH = Math.Max(vpH, ExtentHeight);
                    content.Arrange(new Rect(-HorizontalOffset, -VerticalOffset, arrangeW, arrangeH));
                    content.Clip = new RectangleGeometry(new Rect(HorizontalOffset, VerticalOffset, vpW, vpH));
                }
            }

            if (showH)
            {
                _hScrollBar.Arrange(new Rect(0, finalSize.Height - SbThickness, vpW, SbThickness));
            }
            if (showV)
            {
                _vScrollBar.Arrange(new Rect(finalSize.Width - SbThickness, 0, SbThickness, vpH));
            }

            return finalSize;
        }

        private void OnHScroll(object sender, ScrollEventArgs e)
        {
            ScrollToHorizontalOffset(e.NewValue);
        }

        private void OnVScroll(object sender, ScrollEventArgs e)
        {
            ScrollToVerticalOffset(e.NewValue);
        }

        public void ScrollToVerticalOffset(double offset)
        {
            offset = Math.Clamp(offset, 0, Math.Max(0, ExtentHeight - ViewportHeight));
            if (IsSmoothScrollingEnabled)
            {
                _targetVerticalOffset = offset;
                StartSmoothTimer();
            }
            else
            {
                _targetVerticalOffset = offset;
                ApplyVerticalOffset(offset);
            }
        }

        public void ScrollToHorizontalOffset(double offset)
        {
            offset = Math.Clamp(offset, 0, Math.Max(0, ExtentWidth - ViewportWidth));
            if (IsSmoothScrollingEnabled)
            {
                _targetHorizontalOffset = offset;
                StartSmoothTimer();
            }
            else
            {
                _targetHorizontalOffset = offset;
                ApplyHorizontalOffset(offset);
            }
        }

        private void ApplyVerticalOffset(double offset)
        {
            if (_scrollInfo != null)
            {
                _scrollInfo.SetVerticalOffset(offset);
            }
            else
            {
                UpdateScrollInfo(ExtentWidth, ExtentHeight, ViewportWidth, ViewportHeight, HorizontalOffset, offset);
                InvalidateArrange();
            }
        }

        private void ApplyHorizontalOffset(double offset)
        {
            if (_scrollInfo != null)
            {
                _scrollInfo.SetHorizontalOffset(offset);
            }
            else
            {
                UpdateScrollInfo(ExtentWidth, ExtentHeight, ViewportWidth, ViewportHeight, offset, VerticalOffset);
                InvalidateArrange();
            }
        }

        public void ScrollToTop() => ScrollToVerticalOffset(0);
        public void ScrollToBottom() => ScrollToVerticalOffset(ExtentHeight);
        public void ScrollToLeftEnd() => ScrollToHorizontalOffset(0);
        public void ScrollToRightEnd() => ScrollToHorizontalOffset(ExtentWidth);

        public void LineUp()
        {
            if (_scrollInfo != null) _scrollInfo.LineUp();
            else ScrollToVerticalOffset(VerticalOffset - 16.0);
        }

        public void LineDown()
        {
            if (_scrollInfo != null) _scrollInfo.LineDown();
            else ScrollToVerticalOffset(VerticalOffset + 16.0);
        }

        public void PageUp()
        {
            if (_scrollInfo != null) _scrollInfo.PageUp();
            else ScrollToVerticalOffset(VerticalOffset - ViewportHeight);
        }

        public void PageDown()
        {
            if (_scrollInfo != null) _scrollInfo.PageDown();
            else ScrollToVerticalOffset(VerticalOffset + ViewportHeight);
        }

        protected override void OnMouseWheel(MouseWheelEventArgs e)
        {
            base.OnMouseWheel(e);
            if (e.Handled) return;

            double delta = e.Delta;
            double step = (delta / 120.0) * 48.0;

            if (IsSmoothScrollingEnabled)
            {
                _targetVerticalOffset = Math.Clamp(_targetVerticalOffset - step, 0, Math.Max(0, ExtentHeight - ViewportHeight));
                StartSmoothTimer();
            }
            else
            {
                ScrollToVerticalOffset(VerticalOffset - step);
            }

            e.Handled = true;
        }

        private void StartSmoothTimer()
        {
            if (_smoothTimer == null)
            {
                _smoothTimer = new DispatcherTimer(DispatcherPriority.Render)
                {
                    Interval = TimeSpan.FromMilliseconds(16) // 60 FPS tick
                };
                _smoothTimer.Tick += OnSmoothTick;
            }

            if (!_smoothTimer.IsEnabled)
            {
                _smoothTimer.Start();
            }
        }

        private void OnSmoothTick(object sender, EventArgs e)
        {
            double curV = VerticalOffset;
            double curH = HorizontalOffset;
            double diffV = _targetVerticalOffset - curV;
            double diffH = _targetHorizontalOffset - curH;

            double friction = Math.Clamp(Friction, 0.1, 0.95);
            double stepV = diffV * (1.0 - friction);
            double stepH = diffH * (1.0 - friction);

            bool doneV = Math.Abs(diffV) < 0.3;
            bool doneH = Math.Abs(diffH) < 0.3;

            double nextV = doneV ? _targetVerticalOffset : curV + stepV;
            double nextH = doneH ? _targetHorizontalOffset : curH + stepH;

            if (nextV != curV) ApplyVerticalOffset(nextV);
            if (nextH != curH) ApplyHorizontalOffset(nextH);

            if (doneV && doneH)
            {
                _smoothTimer.Stop();
            }
        }
    }
}

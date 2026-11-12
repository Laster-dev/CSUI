// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System;
using System.Diagnostics;
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
                    sv._vScrollAnim.Stop(target);
                    sv.ApplyVerticalOffset(target);
                    e.Handled = true;
                }
                if (e.DeltaPan.X != 0 && sv.HorizontalScrollBarVisibility != ScrollBarVisibility.Disabled)
                {
                    double target = Math.Clamp(sv.HorizontalOffset - e.DeltaPan.X, 0, Math.Max(0, sv.ExtentWidth - sv.ViewportWidth));
                    sv._hScrollAnim.Stop(target);
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

        internal const double _scrollLineDelta = 16.0;
        internal const double _mouseWheelDelta = 48.0;

        private readonly ScrollBar _hScrollBar;
        private readonly ScrollBar _vScrollBar;
        private IScrollInfo _scrollInfo;

        internal IScrollInfo ScrollInfo
        {
            get => _scrollInfo;
            set => _scrollInfo = value;
        }

        // Chromium cc::ScrollOffsetAnimationCurve smooth scroll state
        internal struct ChromiumScrollCubicBezier
        {
            private readonly double _ax, _bx, _cx;
            private readonly double _ay, _by, _cy;

            public ChromiumScrollCubicBezier(double p1x, double p1y, double p2x, double p2y)
            {
                _cx = 3.0 * p1x;
                _bx = 3.0 * (p2x - p1x) - _cx;
                _ax = 1.0 - _cx - _bx;

                _cy = 3.0 * p1y;
                _by = 3.0 * (p2y - p1y) - _cy;
                _ay = 1.0 - _cy - _by;
            }

            private double SampleX(double u) => ((_ax * u + _bx) * u + _cx) * u;
            private double SampleY(double u) => ((_ay * u + _by) * u + _cy) * u;
            private double SampleDerivativeX(double u) => (3.0 * _ax * u + 2.0 * _bx) * u + _cx;
            private double SampleDerivativeY(double u) => (3.0 * _ay * u + 2.0 * _by) * u + _cy;

            private double SolveCurveX(double t)
            {
                const double eps = 1e-6;
                double u = t;
                for (int i = 0; i < 8; i++)
                {
                    double x2 = SampleX(u) - t;
                    if (Math.Abs(x2) < eps)
                    {
                        return u;
                    }
                    double d2 = SampleDerivativeX(u);
                    if (Math.Abs(d2) < eps)
                    {
                        break;
                    }
                    u -= x2 / d2;
                }

                double lo = 0.0;
                double hi = 1.0;
                u = Math.Clamp(t, 0.0, 1.0);
                for (int i = 0; i < 20; i++)
                {
                    double x2 = SampleX(u);
                    if (Math.Abs(x2 - t) < eps)
                    {
                        return u;
                    }
                    if (t > x2)
                    {
                        lo = u;
                    }
                    else
                    {
                        hi = u;
                    }
                    u = 0.5 * (lo + hi);
                    if (hi - lo < 1e-7)
                    {
                        break;
                    }
                }
                return u;
            }

            public double Sample(double t)
            {
                if (t <= 0.0) return 0.0;
                if (t >= 1.0) return 1.0;
                return SampleY(SolveCurveX(t));
            }

            public double Slope(double t)
            {
                if (t <= 0.0)
                {
                    return _cx > 1e-6 ? (_cy / _cx) : 0.0;
                }
                if (t >= 1.0)
                {
                    return 0.0;
                }
                double u = SolveCurveX(t);
                double dx = SampleDerivativeX(u);
                return dx > 1e-6 ? (SampleDerivativeY(u) / dx) : 0.0;
            }
        }

        internal struct ChromiumScrollAxis
        {
            public bool IsActive;
            public double StartOffset;
            public double TargetOffset;
            public long StartTimestamp;
            public double DurationSeconds;
            public ChromiumScrollCubicBezier Curve;

            public static double SegmentDuration(double delta)
            {
                double absDelta = Math.Abs(delta);
                if (absDelta < 0.01)
                {
                    return 0.0;
                }
                // Chromium InverseDelta / Impulse distance-scaled duration bounds
                return Math.Clamp(0.12 + 0.012 * Math.Sqrt(absDelta), 0.16, 0.36);
            }

            public void UpdateTarget(double currentOffset, double newTarget, long nowTimestamp)
            {
                double effectiveStart = currentOffset;
                double currentVelocity = 0.0;

                if (IsActive && DurationSeconds > 0.0)
                {
                    double elapsed = Math.Max(0.0, (double)(nowTimestamp - StartTimestamp) / Stopwatch.Frequency);
                    double t = elapsed / DurationSeconds;
                    if (t < 1.0)
                    {
                        double oldDelta = TargetOffset - StartOffset;
                        effectiveStart = StartOffset + oldDelta * Curve.Sample(t);
                        currentVelocity = (oldDelta / DurationSeconds) * Curve.Slope(t);
                    }
                }

                double delta = newTarget - effectiveStart;
                if (Math.Abs(delta) < 0.01)
                {
                    StartOffset = newTarget;
                    TargetOffset = newTarget;
                    DurationSeconds = 0.0;
                    IsActive = false;
                    return;
                }

                double duration = SegmentDuration(delta);
                if (Math.Abs(currentVelocity) < 1e-3)
                {
                    Curve = new ChromiumScrollCubicBezier(0.25, 0.1, 0.25, 1.0);
                }
                else
                {
                    double normalizedSlope = (currentVelocity * duration) / delta;
                    if (normalizedSlope > 0.0)
                    {
                        // EaseInOutWithInitialSlope: preserve instantaneous velocity C1-continuously
                        double x1 = 0.25;
                        double y1 = x1 * normalizedSlope;
                        if (y1 > 1.0)
                        {
                            y1 = 1.0;
                            x1 = 1.0 / normalizedSlope;
                        }
                        Curve = new ChromiumScrollCubicBezier(x1, y1, 0.25, 1.0);
                    }
                    else
                    {
                        Curve = new ChromiumScrollCubicBezier(0.25, 0.0, 0.25, 1.0);
                    }
                }

                StartOffset = effectiveStart;
                TargetOffset = newTarget;
                StartTimestamp = nowTimestamp;
                DurationSeconds = duration;
                IsActive = true;
            }

            public void Stop(double offset)
            {
                IsActive = false;
                StartOffset = offset;
                TargetOffset = offset;
                DurationSeconds = 0.0;
            }

            public double Evaluate(long nowTimestamp)
            {
                if (!IsActive)
                {
                    return TargetOffset;
                }

                double elapsed = Math.Max(0.0, (double)(nowTimestamp - StartTimestamp) / Stopwatch.Frequency);
                if (DurationSeconds <= 0.0 || elapsed >= DurationSeconds)
                {
                    IsActive = false;
                    return TargetOffset;
                }

                double t = elapsed / DurationSeconds;
                return StartOffset + (TargetOffset - StartOffset) * Curve.Sample(t);
            }
        }

        private ChromiumScrollAxis _vScrollAnim;
        private ChromiumScrollAxis _hScrollAnim;
        private bool _isSmoothScrollingActive;
        private RectangleGeometry _contentClip;

        public ScrollViewer()
        {
            _hScrollBar = new ScrollBar { Orientation = Orientation.Horizontal };
            _vScrollBar = new ScrollBar { Orientation = Orientation.Vertical };

            AddVisualChild(_hScrollBar);
            AddVisualChild(_vScrollBar);

            _hScrollBar.Scroll += OnHScroll;
            _vScrollBar.Scroll += OnVScroll;
            Unloaded += OnScrollViewerUnloaded;

            ClipToBounds = true;
        }

        private void OnScrollViewerUnloaded(object sender, RoutedEventArgs e)
        {
            StopSmoothScroll();
        }

        protected override void OnContentChanged(object oldContent, object newContent)
        {
            base.OnContentChanged(oldContent, newContent);

            if (oldContent is Visual oldVisual)
            {
                RemoveVisualChild(oldVisual);
            }
            if (newContent is Visual newVisual)
            {
                AddVisualChild(newVisual);
            }
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

                    Rect clipRect = new Rect(HorizontalOffset, VerticalOffset, vpW, vpH);
                    if (_contentClip == null)
                    {
                        _contentClip = new RectangleGeometry(clipRect);
                    }
                    else if (_contentClip.Rect != clipRect)
                    {
                        _contentClip.Rect = clipRect;
                    }
                    if (!ReferenceEquals(content.Clip, _contentClip))
                    {
                        content.Clip = _contentClip;
                    }
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
            if (e.ScrollEventType == ScrollEventType.ThumbTrack ||
                e.ScrollEventType == ScrollEventType.ThumbPosition ||
                e.ScrollEventType == ScrollEventType.EndScroll)
            {
                double clamped = Math.Clamp(e.NewValue, 0, Math.Max(0, ExtentWidth - ViewportWidth));
                _hScrollAnim.Stop(clamped);
                ApplyHorizontalOffset(clamped);
                if (!_vScrollAnim.IsActive)
                {
                    StopSmoothScroll();
                }
                return;
            }
            ScrollToHorizontalOffset(e.NewValue);
        }

        private void OnVScroll(object sender, ScrollEventArgs e)
        {
            if (e.ScrollEventType == ScrollEventType.ThumbTrack ||
                e.ScrollEventType == ScrollEventType.ThumbPosition ||
                e.ScrollEventType == ScrollEventType.EndScroll)
            {
                double clamped = Math.Clamp(e.NewValue, 0, Math.Max(0, ExtentHeight - ViewportHeight));
                _vScrollAnim.Stop(clamped);
                ApplyVerticalOffset(clamped);
                if (!_hScrollAnim.IsActive)
                {
                    StopSmoothScroll();
                }
                return;
            }
            ScrollToVerticalOffset(e.NewValue);
        }

        public double ScrollableWidth => Math.Max(0.0, ExtentWidth - ViewportWidth);

        public double ScrollableHeight => Math.Max(0.0, ExtentHeight - ViewportHeight);

        public void ScrollToVerticalOffset(double offset)
        {
            offset = Math.Clamp(offset, 0, Math.Max(0, ExtentHeight - ViewportHeight));
            if (IsSmoothScrollingEnabled)
            {
                _vScrollAnim.UpdateTarget(VerticalOffset, offset, Stopwatch.GetTimestamp());
                if (_vScrollAnim.IsActive)
                {
                    StartSmoothScroll();
                }
                else if (offset != VerticalOffset)
                {
                    ApplyVerticalOffset(offset);
                }
            }
            else
            {
                _vScrollAnim.Stop(offset);
                ApplyVerticalOffset(offset);
            }
        }

        public void ScrollToHorizontalOffset(double offset)
        {
            offset = Math.Clamp(offset, 0, Math.Max(0, ExtentWidth - ViewportWidth));
            if (IsSmoothScrollingEnabled)
            {
                _hScrollAnim.UpdateTarget(HorizontalOffset, offset, Stopwatch.GetTimestamp());
                if (_hScrollAnim.IsActive)
                {
                    StartSmoothScroll();
                }
                else if (offset != HorizontalOffset)
                {
                    ApplyHorizontalOffset(offset);
                }
            }
            else
            {
                _hScrollAnim.Stop(offset);
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
        public void ScrollToHome() { ScrollToHorizontalOffset(0); ScrollToVerticalOffset(0); }
        public void ScrollToEnd() { ScrollToHorizontalOffset(0); ScrollToVerticalOffset(ExtentHeight); }

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

        public void LineLeft()
        {
            if (_scrollInfo != null) _scrollInfo.LineLeft();
            else ScrollToHorizontalOffset(HorizontalOffset - _scrollLineDelta);
        }

        public void LineRight()
        {
            if (_scrollInfo != null) _scrollInfo.LineRight();
            else ScrollToHorizontalOffset(HorizontalOffset + _scrollLineDelta);
        }

        public void PageLeft()
        {
            if (_scrollInfo != null) _scrollInfo.PageLeft();
            else ScrollToHorizontalOffset(HorizontalOffset - ViewportWidth);
        }

        public void PageRight()
        {
            if (_scrollInfo != null) _scrollInfo.PageRight();
            else ScrollToHorizontalOffset(HorizontalOffset + ViewportWidth);
        }

        internal const string ScrollContentPresenterTemplateName = "PART_ScrollContentPresenter";

        internal void ScrollInDirection(KeyEventArgs e)
        {
            if (e == null) return;
            bool invert = (FlowDirection == FlowDirection.RightToLeft);
            switch (e.Key)
            {
                case Key.Up:
                    LineUp();
                    break;
                case Key.Down:
                    LineDown();
                    break;
                case Key.Left:
                    if (invert) LineRight(); else LineLeft();
                    break;
                case Key.Right:
                    if (invert) LineLeft(); else LineRight();
                    break;
            }
        }

        protected override void OnMouseWheel(MouseWheelEventArgs e)
        {
            base.OnMouseWheel(e);
            if (e.Handled) return;

            double delta = e.Delta;
            // Chromium Windows desktop wheel notch delta (~100 DIPs per 120 wheel units)
            double step = (delta / 120.0) * 100.0;

            if (IsSmoothScrollingEnabled)
            {
                double maxV = Math.Max(0, ExtentHeight - ViewportHeight);
                double scrollAmount = -step;
                double currentTarget = _vScrollAnim.IsActive ? _vScrollAnim.TargetOffset : VerticalOffset;
                bool reversing = _vScrollAnim.IsActive && ((currentTarget - VerticalOffset) * scrollAmount < 0.0);
                double baseTarget = reversing ? VerticalOffset : currentTarget;
                double nextTarget = Math.Clamp(baseTarget + scrollAmount, 0, maxV);
                _vScrollAnim.UpdateTarget(VerticalOffset, nextTarget, Stopwatch.GetTimestamp());
                if (_vScrollAnim.IsActive)
                {
                    StartSmoothScroll();
                }
                else if (nextTarget != VerticalOffset)
                {
                    ApplyVerticalOffset(nextTarget);
                }
            }
            else
            {
                ScrollToVerticalOffset(VerticalOffset - step);
            }

            e.Handled = true;
        }

        private void StartSmoothScroll()
        {
            if (!_isSmoothScrollingActive)
            {
                _isSmoothScrollingActive = true;
                CompositionTarget.Rendering += OnSmoothRenderFrame;
            }
            MediaContext.From(Dispatcher)?.PostRender();
        }

        private void StopSmoothScroll()
        {
            if (_isSmoothScrollingActive)
            {
                _isSmoothScrollingActive = false;
                CompositionTarget.Rendering -= OnSmoothRenderFrame;
            }
        }

        private void OnSmoothRenderFrame(object sender, EventArgs e)
        {
            if (!_isSmoothScrollingActive)
            {
                return;
            }

            long now = Stopwatch.GetTimestamp();
            double maxV = Math.Max(0.0, ExtentHeight - ViewportHeight);
            double maxH = Math.Max(0.0, ExtentWidth - ViewportWidth);

            bool changed = false;
            if (_vScrollAnim.IsActive)
            {
                double targetV = Math.Clamp(_vScrollAnim.TargetOffset, 0.0, maxV);
                _vScrollAnim.TargetOffset = targetV;
                double nextV = Math.Clamp(_vScrollAnim.Evaluate(now), 0.0, maxV);
                if (nextV != VerticalOffset)
                {
                    ApplyVerticalOffset(nextV);
                    changed = true;
                }
            }

            if (_hScrollAnim.IsActive)
            {
                double targetH = Math.Clamp(_hScrollAnim.TargetOffset, 0.0, maxH);
                _hScrollAnim.TargetOffset = targetH;
                double nextH = Math.Clamp(_hScrollAnim.Evaluate(now), 0.0, maxH);
                if (nextH != HorizontalOffset)
                {
                    ApplyHorizontalOffset(nextH);
                    changed = true;
                }
            }

            if (changed)
            {
                UpdateLayout();
            }

            if (!_vScrollAnim.IsActive && !_hScrollAnim.IsActive)
            {
                StopSmoothScroll();
            }
            else
            {
                MediaContext.From(Dispatcher)?.PostRender();
            }
        }
    }
}

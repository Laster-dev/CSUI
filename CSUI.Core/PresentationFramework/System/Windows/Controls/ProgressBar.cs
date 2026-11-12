// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace System.Windows.Controls
{
    /// <summary>
    /// Indicates the progress of an operation.
    /// </summary>
    public partial class ProgressBar : RangeBase
    {
        static partial void RegisterDefaultTheme();

        public static readonly DependencyProperty IsIndeterminateProperty =
            DependencyProperty.Register(
                nameof(IsIndeterminate),
                typeof(bool),
                typeof(ProgressBar),
                new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.AffectsRender, OnIsIndeterminateChanged));

        public static readonly DependencyProperty OrientationProperty =
            DependencyProperty.Register(
                nameof(Orientation),
                typeof(Orientation),
                typeof(ProgressBar),
                new FrameworkPropertyMetadata(Orientation.Horizontal, FrameworkPropertyMetadataOptions.AffectsMeasure));

        private FrameworkElement _track;
        private FrameworkElement _indicator;
        private FrameworkElement _glow;

        public bool IsIndeterminate
        {
            get => (bool)GetValue(IsIndeterminateProperty);
            set => SetValue(IsIndeterminateProperty, value);
        }

        public Orientation Orientation
        {
            get => (Orientation)GetValue(OrientationProperty);
            set => SetValue(OrientationProperty, value);
        }

        static ProgressBar()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(ProgressBar), new FrameworkPropertyMetadata(typeof(ProgressBar)));
            FocusableProperty.OverrideMetadata(typeof(ProgressBar), new FrameworkPropertyMetadata(false));
            MinimumProperty.OverrideMetadata(typeof(ProgressBar), new FrameworkPropertyMetadata(0.0, OnMinimumOrMaximumChanged));
            MaximumProperty.OverrideMetadata(typeof(ProgressBar), new FrameworkPropertyMetadata(100.0, OnMinimumOrMaximumChanged));
            RegisterDefaultTheme();
        }

        public ProgressBar()
        {
        }

        private static void OnIsIndeterminateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ProgressBar pb)
            {
                pb.SetProgressBarIndicatorLength();
                pb.UpdateProgressVisualState(true);
            }
        }

        private static void OnMinimumOrMaximumChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ProgressBar pb)
            {
                pb.SetProgressBarIndicatorLength();
            }
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            _track = GetTemplateChild("PART_Track") as FrameworkElement;
            _indicator = GetTemplateChild("PART_Indicator") as FrameworkElement;
            _glow = GetTemplateChild("PART_GlowRect") as FrameworkElement;
            SetProgressBarIndicatorLength();
            UpdateProgressVisualState(false);
        }

        protected override void OnValueChanged(double oldValue, double newValue)
        {
            base.OnValueChanged(oldValue, newValue);
            SetProgressBarIndicatorLength();
        }

        private void SetProgressBarIndicatorLength()
        {
            if (_track == null || _indicator == null)
            {
                return;
            }

            double min = Minimum;
            double max = Maximum;
            double val = Value;
            double percent = (IsIndeterminate || max <= min) ? 1.0 : Math.Clamp((val - min) / (max - min), 0.0, 1.0);
            _indicator.Width = percent * _track.ActualWidth;
        }

        private void UpdateProgressVisualState(bool useTransitions)
        {
            VisualStateManager.GoToState(this, IsIndeterminate ? "Indeterminate" : "Determinate", useTransitions);
        }

        protected override Size MeasureOverride(Size constraint)
        {
            if (Template != null)
            {
                return base.MeasureOverride(constraint);
            }
            if (Orientation == Orientation.Horizontal)
            {
                double w = double.IsInfinity(constraint.Width) ? 160.0 : Math.Max(40.0, constraint.Width);
                return new Size(w, 6.0);
            }
            else
            {
                double h = double.IsInfinity(constraint.Height) ? 160.0 : Math.Max(40.0, constraint.Height);
                return new Size(6.0, h);
            }
        }

        protected override Size ArrangeOverride(Size arrangeBounds)
        {
            Size arranged = base.ArrangeOverride(arrangeBounds);
            SetProgressBarIndicatorLength();
            return arranged;
        }

        protected override System.Windows.Automation.Peers.AutomationPeer OnCreateAutomationPeer() =>
            new System.Windows.Automation.Peers.ProgressBarAutomationPeer(this);

        protected override void OnRender(DrawingContext dc)
        {
            base.OnRender(dc);
            if (Template != null)
            {
                return;
            }
            double w = RenderSize.Width;
            double h = RenderSize.Height;
            if (w <= 0 || h <= 0) return;

            // Track background
            Brush trackBg = Background ?? new SolidColorBrush(Color.FromArgb(255, 45, 45, 45));
            dc.DrawRoundedRectangle(trackBg, null, new Rect(0, 0, w, h), 3, 3);

            // Progress indicator
            double min = Minimum;
            double max = Maximum;
            double range = max - min;
            double val = Value;
            double pct = range > 1e-6 ? Math.Clamp((val - min) / range, 0.0, 1.0) : 0.0;

            Brush indBrush = Foreground ?? new SolidColorBrush(Color.FromArgb(255, 0, 120, 215));
            if (Orientation == Orientation.Horizontal)
            {
                double indW = w * pct;
                if (indW > 0)
                {
                    dc.DrawRoundedRectangle(indBrush, null, new Rect(0, 0, indW, h), 3, 3);
                }
            }
            else
            {
                double indH = h * pct;
                if (indH > 0)
                {
                    dc.DrawRoundedRectangle(indBrush, null, new Rect(0, h - indH, w, indH), 3, 3);
                }
            }
        }
    }
}

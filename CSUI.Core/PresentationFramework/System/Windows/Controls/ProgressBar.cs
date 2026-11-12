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
    public class ProgressBar : RangeBase
    {
        public static readonly DependencyProperty IsIndeterminateProperty =
            DependencyProperty.Register(
                nameof(IsIndeterminate),
                typeof(bool),
                typeof(ProgressBar),
                new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty OrientationProperty =
            DependencyProperty.Register(
                nameof(Orientation),
                typeof(Orientation),
                typeof(ProgressBar),
                new FrameworkPropertyMetadata(Orientation.Horizontal, FrameworkPropertyMetadataOptions.AffectsMeasure));

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
        }

        public ProgressBar()
        {
        }

        protected override Size MeasureOverride(Size constraint)
        {
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

        protected override System.Windows.Automation.Peers.AutomationPeer OnCreateAutomationPeer() =>
            new System.Windows.Automation.Peers.ProgressBarAutomationPeer(this);

        protected override void OnRender(DrawingContext dc)
        {
            base.OnRender(dc);
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

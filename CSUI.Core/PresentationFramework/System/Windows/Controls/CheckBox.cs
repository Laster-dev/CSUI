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
    /// Represents a control that a user can select and clear.
    /// </summary>
    public class CheckBox : ToggleButton
    {
        static CheckBox()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(CheckBox), new FrameworkPropertyMetadata(typeof(CheckBox)));
        }

        public CheckBox()
        {
        }

        protected override Size MeasureOverride(Size constraint)
        {
            Size contentSize = new Size(0, 0);
            if (VisualChildrenCount > 0 && GetVisualChild(0) is UIElement child)
            {
                child.Measure(new Size(Math.Max(0, constraint.Width - 26.0), constraint.Height));
                contentSize = child.DesiredSize;
            }

            double w = contentSize.Width + 26.0;
            double h = Math.Max(20.0, contentSize.Height);
            return new Size(w, h);
        }

        protected override Size ArrangeOverride(Size arrangeBounds)
        {
            if (VisualChildrenCount > 0 && GetVisualChild(0) is UIElement child)
            {
                double y = Math.Max(0, (arrangeBounds.Height - child.DesiredSize.Height) * 0.5);
                child.Arrange(new Rect(26.0, y, child.DesiredSize.Width, child.DesiredSize.Height));
            }
            return arrangeBounds;
        }

        protected override System.Windows.Automation.Peers.AutomationPeer OnCreateAutomationPeer() =>
            new System.Windows.Automation.Peers.CheckBoxAutomationPeer(this);

        protected override void OnRender(DrawingContext dc)
        {
            double w = RenderSize.Width;
            double h = RenderSize.Height;
            if (w <= 0 || h <= 0) return;

            dc.DrawRectangle(Brushes.Transparent, null, new Rect(0, 0, w, h));

            const double boxSize = 18.0;
            double boxY = Math.Max(0, (h - boxSize) * 0.5);
            Rect boxRect = new Rect(0, boxY, boxSize, boxSize);

            bool? isChecked = IsChecked;
            if (isChecked == true)
            {
                Brush bg = Background ?? new SolidColorBrush(Color.FromArgb(255, 0, 103, 192));
                Pen borderPen = new Pen(new SolidColorBrush(Color.FromArgb(255, 0, 120, 215)), 1.0);
                dc.DrawRoundedRectangle(bg, borderPen, boxRect, 4, 4);

                // Draw checkmark
                Pen checkPen = new Pen(Brushes.White, 2.0);
                dc.DrawLine(checkPen, new Point(3.5, boxY + 9.0), new Point(7.0, boxY + 12.5));
                dc.DrawLine(checkPen, new Point(7.0, boxY + 12.5), new Point(13.5, boxY + 5.0));
            }
            else if (isChecked == null)
            {
                Brush bg = Background ?? new SolidColorBrush(Color.FromArgb(255, 0, 103, 192));
                Pen borderPen = new Pen(new SolidColorBrush(Color.FromArgb(255, 0, 120, 215)), 1.0);
                dc.DrawRoundedRectangle(bg, borderPen, boxRect, 4, 4);

                // Draw indeterminate dash
                dc.DrawRectangle(Brushes.White, null, new Rect(4.5, boxY + 8.0, 9.0, 2.0));
            }
            else
            {
                Brush bg = Background ?? (IsMouseOver ? new SolidColorBrush(Color.FromArgb(255, 52, 52, 52)) : new SolidColorBrush(Color.FromArgb(255, 38, 38, 38)));
                Pen borderPen = new Pen(BorderBrush ?? new SolidColorBrush(Color.FromArgb(255, 120, 120, 120)), 1.0);
                dc.DrawRoundedRectangle(bg, borderPen, boxRect, 4, 4);
            }
        }
    }
}

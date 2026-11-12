// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System;
using System.Collections;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace System.Windows.Controls
{
    /// <summary>
    /// Represents a button that can be selected, but not cleared, by a user.
    /// The IsChecked property of a RadioButton can be set by clicking it, but can only be cleared programmatically.
    /// </summary>
    public class RadioButton : ToggleButton
    {
        public static readonly DependencyProperty GroupNameProperty =
            DependencyProperty.Register(
                nameof(GroupName),
                typeof(string),
                typeof(RadioButton),
                new FrameworkPropertyMetadata(string.Empty, OnGroupNameChanged));

        public string GroupName
        {
            get => (string)GetValue(GroupNameProperty);
            set => SetValue(GroupNameProperty, value);
        }

        static RadioButton()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(RadioButton), new FrameworkPropertyMetadata(typeof(RadioButton)));
        }

        public RadioButton()
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

        protected override void OnRender(DrawingContext dc)
        {
            double h = RenderSize.Height;
            if (h <= 0) return;

            const double radius = 9.0;
            Point center = new Point(radius, h * 0.5);

            if (IsChecked == true)
            {
                Brush bg = Background ?? new SolidColorBrush(Color.FromArgb(255, 38, 38, 38));
                Pen borderPen = new Pen(new SolidColorBrush(Color.FromArgb(255, 0, 120, 215)), 1.5);
                dc.DrawEllipse(bg, borderPen, center, radius, radius);

                // Inner dot
                Brush dotBrush = new SolidColorBrush(Color.FromArgb(255, 0, 120, 215));
                dc.DrawEllipse(dotBrush, null, center, 4.5, 4.5);
            }
            else
            {
                Brush bg = Background ?? (IsMouseOver ? new SolidColorBrush(Color.FromArgb(255, 52, 52, 52)) : new SolidColorBrush(Color.FromArgb(255, 38, 38, 38)));
                Pen borderPen = new Pen(BorderBrush ?? new SolidColorBrush(Color.FromArgb(255, 120, 120, 120)), 1.0);
                dc.DrawEllipse(bg, borderPen, center, radius, radius);
            }
        }

        protected internal override void OnToggle()
        {
            // RadioButton clicking can only turn ON (set to true)
            bool? isChecked = IsChecked;
            if (isChecked != true)
            {
                IsChecked = true;
            }
        }

        protected override void OnChecked(RoutedEventArgs e)
        {
            // Uncheck other radio buttons in the same group
            UpdateGroupMembers();

            base.OnChecked(e);
        }

        private static void OnGroupNameChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is RadioButton rb && rb.IsChecked == true)
            {
                rb.UpdateGroupMembers();
            }
        }

        private void UpdateGroupMembers()
        {
            string groupName = GroupName;
            DependencyObject parent = LogicalTreeHelper.GetParent(this) ?? VisualTreeHelper.GetParent(this);
            if (parent != null)
            {
                UncheckSiblings(parent, groupName);
            }
        }

        private void UncheckSiblings(DependencyObject root, string groupName)
        {
            // Traverse visual or logical tree under parent
            int count = VisualTreeHelper.GetChildrenCount(root);
            for (int i = 0; i < count; i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(root, i);
                if (child != null)
                {
                    if (child is RadioButton rb && rb != this && rb.GroupName == groupName)
                    {
                        if (rb.IsChecked == true)
                        {
                            rb.IsChecked = false;
                        }
                    }
                    else
                    {
                        UncheckSiblings(child, groupName);
                    }
                }
            }
        }
    }
}

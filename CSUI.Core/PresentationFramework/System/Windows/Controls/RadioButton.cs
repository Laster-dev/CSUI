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

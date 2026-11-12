// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System.Windows;
using System.Windows.Media;

namespace System.Windows.Controls.Primitives
{
    /// <summary>
    /// Base class for controls that can switch states, such as CheckBox.
    /// </summary>
    public partial class ToggleButton : ButtonBase
    {
        static partial void RegisterDefaultTheme();
        public static readonly DependencyProperty IsCheckedProperty =
            DependencyProperty.Register(
                nameof(IsChecked),
                typeof(bool?),
                typeof(ToggleButton),
                new FrameworkPropertyMetadata(
                    false,
                    FrameworkPropertyMetadataOptions.BindsTwoWayByDefault | FrameworkPropertyMetadataOptions.AffectsRender,
                    OnIsCheckedChanged));

        public static readonly DependencyProperty IsThreeStateProperty =
            DependencyProperty.Register(
                nameof(IsThreeState),
                typeof(bool),
                typeof(ToggleButton),
                new FrameworkPropertyMetadata(false));

        public static readonly RoutedEvent CheckedEvent =
            EventManager.RegisterRoutedEvent(
                nameof(Checked),
                RoutingStrategy.Bubble,
                typeof(RoutedEventHandler),
                typeof(ToggleButton));

        public static readonly RoutedEvent UncheckedEvent =
            EventManager.RegisterRoutedEvent(
                nameof(Unchecked),
                RoutingStrategy.Bubble,
                typeof(RoutedEventHandler),
                typeof(ToggleButton));

        public static readonly RoutedEvent IndeterminateEvent =
            EventManager.RegisterRoutedEvent(
                nameof(Indeterminate),
                RoutingStrategy.Bubble,
                typeof(RoutedEventHandler),
                typeof(ToggleButton));

        public bool? IsChecked
        {
            get => (bool?)GetValue(IsCheckedProperty);
            set => SetValue(IsCheckedProperty, value);
        }

        public bool IsThreeState
        {
            get => (bool)GetValue(IsThreeStateProperty);
            set => SetValue(IsThreeStateProperty, value);
        }

        public event RoutedEventHandler Checked
        {
            add => AddHandler(CheckedEvent, value);
            remove => RemoveHandler(CheckedEvent, value);
        }

        public event RoutedEventHandler Unchecked
        {
            add => AddHandler(UncheckedEvent, value);
            remove => RemoveHandler(UncheckedEvent, value);
        }

        public event RoutedEventHandler Indeterminate
        {
            add => AddHandler(IndeterminateEvent, value);
            remove => RemoveHandler(IndeterminateEvent, value);
        }

        static ToggleButton()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(ToggleButton), new FrameworkPropertyMetadata(typeof(ToggleButton)));
            RegisterDefaultTheme();
        }

        public ToggleButton()
        {
        }

        protected internal virtual void OnToggle()
        {
            bool? isChecked = IsChecked;
            if (isChecked == true)
            {
                IsChecked = IsThreeState ? null : false;
            }
            else
            {
                IsChecked = isChecked.HasValue;
            }
        }

        protected internal override void OnClick()
        {
            OnToggle();
            base.OnClick();
        }

        private static void OnIsCheckedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ToggleButton btn)
            {
                bool? newValue = (bool?)e.NewValue;
                if (newValue == true)
                {
                    btn.OnChecked(new RoutedEventArgs(CheckedEvent, btn));
                }
                else if (newValue == false)
                {
                    btn.OnUnchecked(new RoutedEventArgs(UncheckedEvent, btn));
                }
                else
                {
                    btn.OnIndeterminate(new RoutedEventArgs(IndeterminateEvent, btn));
                }

                btn.UpdateVisualState();
            }
        }

        internal override void ChangeVisualState(bool useTransitions)
        {
            base.ChangeVisualState(useTransitions);

            if (IsChecked == true)
            {
                VisualStateManager.GoToState(this, VisualStates.StateChecked, useTransitions);
            }
            else if (IsChecked == false)
            {
                VisualStateManager.GoToState(this, VisualStates.StateUnchecked, useTransitions);
            }
            else
            {
                VisualStates.GoToState(this, useTransitions, VisualStates.StateIndeterminate, VisualStates.StateUnchecked);
            }
        }

        protected virtual void OnChecked(RoutedEventArgs e)
        {
            RaiseEvent(e);
        }

        protected virtual void OnUnchecked(RoutedEventArgs e)
        {
            RaiseEvent(e);
        }

        protected override void OnRender(DrawingContext dc)
        {
            if (Template != null)
            {
                base.OnRender(dc);
                return;
            }
            if (Background == null && IsChecked == true)
            {
                Rect bounds = new Rect(0, 0, RenderSize.Width, RenderSize.Height);
                if (bounds.Width > 0 && bounds.Height > 0)
                {
                    Brush bg = IsPressed
                        ? new SolidColorBrush(Color.FromArgb(255, 0, 90, 158))
                        : (IsMouseOver ? new SolidColorBrush(Color.FromArgb(255, 0, 110, 190)) : new SolidColorBrush(Color.FromArgb(255, 0, 103, 192)));
                    Pen pen = new Pen(new SolidColorBrush(Color.FromArgb(255, 0, 120, 215)), 1.0);
                    dc.DrawRoundedRectangle(bg, pen, bounds, 4, 4);
                    return;
                }
            }

            base.OnRender(dc);
        }

        protected virtual void OnIndeterminate(RoutedEventArgs e)
        {
            RaiseEvent(e);
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace System.Windows.Controls
{
    /// <summary>
    /// Represents a Windows button control, which reacts to the Click event.
    /// </summary>
    public partial class Button : ButtonBase
    {
        static partial void RegisterDefaultTheme();
        public static readonly DependencyProperty IsDefaultProperty =
            DependencyProperty.Register(
                nameof(IsDefault),
                typeof(bool),
                typeof(Button),
                new FrameworkPropertyMetadata(false, OnIsDefaultChanged));

        public static readonly DependencyProperty IsCancelProperty =
            DependencyProperty.Register(
                nameof(IsCancel),
                typeof(bool),
                typeof(Button),
                new FrameworkPropertyMetadata(false, OnIsCancelChanged));

        private static readonly DependencyPropertyKey IsDefaultedPropertyKey =
            DependencyProperty.RegisterReadOnly(
                nameof(IsDefaulted),
                typeof(bool),
                typeof(Button),
                new FrameworkPropertyMetadata(false));

        public static readonly DependencyProperty IsDefaultedProperty =
            IsDefaultedPropertyKey.DependencyProperty;

        public bool IsDefault
        {
            get => (bool)GetValue(IsDefaultProperty);
            set => SetValue(IsDefaultProperty, value);
        }

        public bool IsCancel
        {
            get => (bool)GetValue(IsCancelProperty);
            set => SetValue(IsCancelProperty, value);
        }

        public bool IsDefaulted => (bool)GetValue(IsDefaultedProperty);

        static Button()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(Button), new FrameworkPropertyMetadata(typeof(Button)));
            RegisterDefaultTheme();
        }

        public Button()
        {
        }

        private static void OnIsDefaultChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is Button btn)
            {
                bool isDefault = (bool)e.NewValue;
                if (isDefault)
                {
                    AccessKeyManager.Register("\x000D", btn);
                    KeyboardNavigation.Current.FocusChanged += btn.OnFocusChanged;
                }
                else
                {
                    AccessKeyManager.Unregister("\x000D", btn);
                    KeyboardNavigation.Current.FocusChanged -= btn.OnFocusChanged;
                }

                btn.UpdateIsDefaulted();
            }
        }

        private static void OnIsCancelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is Button btn)
            {
                if ((bool)e.NewValue)
                {
                    AccessKeyManager.Register("\x001B", btn);
                }
                else
                {
                    AccessKeyManager.Unregister("\x001B", btn);
                }
            }
        }

        private void OnFocusChanged(object sender, KeyboardFocusChangedEventArgs e)
        {
            UpdateIsDefaulted();
        }

        private void UpdateIsDefaulted()
        {
            if (!IsDefault)
            {
                SetValue(IsDefaultedPropertyKey, false);
                return;
            }

            bool isDefaulted = true;
            if (Keyboard.FocusedElement is DependencyObject focused &&
                focused != this &&
                (bool)focused.GetValue(KeyboardNavigation.AcceptsReturnProperty))
            {
                isDefaulted = false;
            }

            SetValue(IsDefaultedPropertyKey, isDefaulted);
        }

        protected override void OnAccessKey(AccessKeyEventArgs e)
        {
            if ((e.Key == "\x000D" && IsDefault) || (e.Key == "\x001B" && IsCancel))
            {
                OnClick();
            }
            else
            {
                base.OnAccessKey(e);
            }
        }

        protected internal override void OnClick()
        {
            base.OnClick();

            // Support DialogResult if inside a Window and IsCancel is set
            if (IsCancel)
            {
                Window w = Window.GetWindow(this);
                if (w != null)
                {
                    w.DialogResult = false;
                }
            }
        }
    }
}

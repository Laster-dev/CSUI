// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System.Windows;
using System.Windows.Controls.Primitives;

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
                new FrameworkPropertyMetadata(false));

        public static readonly DependencyProperty IsCancelProperty =
            DependencyProperty.Register(
                nameof(IsCancel),
                typeof(bool),
                typeof(Button),
                new FrameworkPropertyMetadata(false));

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

        protected internal override void OnClick()
        {
            base.OnClick();

            // Support DialogResult if inside a Window and IsCancel/IsDefault is set
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

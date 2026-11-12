// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System;
using System.Globalization;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace System.Windows.Controls
{
    /// <summary>
    /// Represents a control designed for entering and handling passwords.
    /// </summary>
    public class PasswordBox : Control
    {
        public static readonly DependencyProperty PasswordProperty =
            DependencyProperty.Register(
                nameof(Password),
                typeof(string),
                typeof(PasswordBox),
                new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.AffectsRender, OnPasswordPropertyChanged));

        public static readonly DependencyProperty PasswordCharProperty =
            DependencyProperty.Register(
                nameof(PasswordChar),
                typeof(char),
                typeof(PasswordBox),
                new FrameworkPropertyMetadata('●', FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty MaxLengthProperty =
            DependencyProperty.Register(
                nameof(MaxLength),
                typeof(int),
                typeof(PasswordBox),
                new FrameworkPropertyMetadata(0));

        public static readonly RoutedEvent PasswordChangedEvent =
            EventManager.RegisterRoutedEvent(
                nameof(PasswordChanged),
                RoutingStrategy.Bubble,
                typeof(RoutedEventHandler),
                typeof(PasswordBox));

        public string Password
        {
            get => (string)GetValue(PasswordProperty) ?? string.Empty;
            set => SetValue(PasswordProperty, value ?? string.Empty);
        }

        public char PasswordChar
        {
            get => (char)GetValue(PasswordCharProperty);
            set => SetValue(PasswordCharProperty, value);
        }

        public int MaxLength
        {
            get => (int)GetValue(MaxLengthProperty);
            set => SetValue(MaxLengthProperty, value);
        }

        public event RoutedEventHandler PasswordChanged
        {
            add => AddHandler(PasswordChangedEvent, value);
            remove => RemoveHandler(PasswordChangedEvent, value);
        }

        static PasswordBox()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(PasswordBox), new FrameworkPropertyMetadata(typeof(PasswordBox)));
            FocusableProperty.OverrideMetadata(typeof(PasswordBox), new FrameworkPropertyMetadata(true));
        }

        public PasswordBox()
        {
        }

        public void Clear()
        {
            Password = string.Empty;
        }

        public void SelectAll()
        {
        }

        private static void OnPasswordPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is PasswordBox pb)
            {
                pb.RaiseEvent(new RoutedEventArgs(PasswordChangedEvent, pb));
            }
        }

        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonDown(e);
            Focus();
            e.Handled = true;
        }

        protected override void OnGotFocus(RoutedEventArgs e)
        {
            base.OnGotFocus(e);
            InvalidateVisual();
        }

        protected override void OnLostFocus(RoutedEventArgs e)
        {
            base.OnLostFocus(e);
            InvalidateVisual();
        }

        protected override void OnTextInput(TextCompositionEventArgs e)
        {
            base.OnTextInput(e);

            if (!e.Handled && !string.IsNullOrEmpty(e.Text))
            {
                if (MaxLength == 0 || Password.Length < MaxLength)
                {
                    Password += e.Text;
                    e.Handled = true;
                }
            }
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);

            if (e.Handled) return;

            if (e.Key == Key.Back && Password.Length > 0)
            {
                Password = Password.Substring(0, Password.Length - 1);
                e.Handled = true;
            }
        }

        protected override Size MeasureOverride(Size constraint)
        {
            double horizontalPadding = Padding.Left + Padding.Right + BorderThickness.Left + BorderThickness.Right;
            double verticalPadding = Padding.Top + Padding.Bottom + BorderThickness.Top + BorderThickness.Bottom;

            int len = Math.Max(1, Password.Length);
            string masked = new(PasswordChar, len);

            Typeface typeface = new(FontFamily ?? new FontFamily("Segoe UI"), FontStyle, FontWeight, FontStretch);
            FormattedText ft = new(
                masked,
                CultureInfo.CurrentUICulture,
                FlowDirection.LeftToRight,
                typeface,
                FontSize > 0 ? FontSize : 14.0,
                Foreground ?? Brushes.Black,
                null,
                TextFormattingMode.Ideal,
                1.0);

            return new Size(ft.Width + horizontalPadding, ft.Height + verticalPadding);
        }

        protected override void OnRender(DrawingContext dc)
        {
            base.OnRender(dc);

            Rect bounds = new(0, 0, RenderSize.Width, RenderSize.Height);
            if (bounds.Width <= 0 || bounds.Height <= 0) return;

            Brush bg = Background ?? Brushes.White;
            Brush borderBrush = BorderBrush ?? Brushes.Gray;
            Thickness borderThickness = BorderThickness;

            dc.DrawRectangle(bg, null, bounds);

            if (borderThickness.Left > 0 && borderBrush != null)
            {
                Pen pen = new(borderBrush, borderThickness.Left);
                dc.DrawRectangle(null, pen, new Rect(borderThickness.Left * 0.5, borderThickness.Top * 0.5, Math.Max(0, bounds.Width - borderThickness.Left), Math.Max(0, bounds.Height - borderThickness.Top)));
            }

            if (!string.IsNullOrEmpty(Password))
            {
                string masked = new(PasswordChar, Password.Length);
                Typeface typeface = new(FontFamily ?? new FontFamily("Segoe UI"), FontStyle, FontWeight, FontStretch);
                FormattedText ft = new(
                    masked,
                    CultureInfo.CurrentUICulture,
                    FlowDirection.LeftToRight,
                    typeface,
                    FontSize > 0 ? FontSize : 14.0,
                    Foreground ?? Brushes.Black,
                    null,
                    TextFormattingMode.Ideal,
                    1.0);

                double xStart = Padding.Left + borderThickness.Left;
                double yStart = Padding.Top + borderThickness.Top;
                dc.DrawText(ft, new Point(xStart, yStart));
            }
        }

        protected override System.Windows.Automation.Peers.AutomationPeer OnCreateAutomationPeer() =>
            new System.Windows.Automation.Peers.PasswordBoxAutomationPeer(this);
    }
}

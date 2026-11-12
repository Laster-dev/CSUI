// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace System.Windows.Controls.Primitives
{
    /// <summary>
    /// Represents the base class for all Button controls.
    /// </summary>
    public abstract class ButtonBase : ContentControl, ICommandSource
    {
        internal static readonly DependencyPropertyKey IsPressedPropertyKey =
            DependencyProperty.RegisterReadOnly(
                nameof(IsPressed),
                typeof(bool),
                typeof(ButtonBase),
                new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.AffectsRender, OnVisualStatePropertyChanged));

        public static readonly DependencyProperty IsPressedProperty =
            IsPressedPropertyKey.DependencyProperty;

        public static readonly DependencyProperty ClickModeProperty =
            DependencyProperty.Register(
                nameof(ClickMode),
                typeof(ClickMode),
                typeof(ButtonBase),
                new FrameworkPropertyMetadata(ClickMode.Release));

        public static readonly DependencyProperty CommandProperty =
            DependencyProperty.Register(
                nameof(Command),
                typeof(ICommand),
                typeof(ButtonBase),
                new FrameworkPropertyMetadata(null, OnCommandChanged));

        public static readonly DependencyProperty CommandParameterProperty =
            DependencyProperty.Register(
                nameof(CommandParameter),
                typeof(object),
                typeof(ButtonBase),
                new FrameworkPropertyMetadata(null, OnCommandParameterChanged));

        public static readonly DependencyProperty CommandTargetProperty =
            DependencyProperty.Register(
                nameof(CommandTarget),
                typeof(IInputElement),
                typeof(ButtonBase),
                new FrameworkPropertyMetadata(null));

        public static readonly RoutedEvent ClickEvent =
            EventManager.RegisterRoutedEvent(
                nameof(Click),
                RoutingStrategy.Bubble,
                typeof(RoutedEventHandler),
                typeof(ButtonBase));

        public bool IsPressed => (bool)GetValue(IsPressedProperty);

        public ClickMode ClickMode
        {
            get => (ClickMode)GetValue(ClickModeProperty);
            set => SetValue(ClickModeProperty, value);
        }

        public ICommand Command
        {
            get => (ICommand)GetValue(CommandProperty);
            set => SetValue(CommandProperty, value);
        }

        public object CommandParameter
        {
            get => GetValue(CommandParameterProperty);
            set => SetValue(CommandParameterProperty, value);
        }

        public IInputElement CommandTarget
        {
            get => (IInputElement)GetValue(CommandTargetProperty);
            set => SetValue(CommandTargetProperty, value);
        }

        public event RoutedEventHandler Click
        {
            add => AddHandler(ClickEvent, value);
            remove => RemoveHandler(ClickEvent, value);
        }

        private EventHandler _canExecuteChangedHandler;

        static ButtonBase()
        {
            FocusableProperty.OverrideMetadata(typeof(ButtonBase), new FrameworkPropertyMetadata(true));
            KeyboardNavigation.AcceptsReturnProperty.OverrideMetadata(typeof(ButtonBase), new FrameworkPropertyMetadata(true));
            IsEnabledProperty.OverrideMetadata(typeof(ButtonBase), new UIPropertyMetadata(new PropertyChangedCallback(OnVisualStatePropertyChanged)));
            IsMouseOverPropertyKey.OverrideMetadata(typeof(ButtonBase), new UIPropertyMetadata(new PropertyChangedCallback(OnVisualStatePropertyChanged)));
        }

        protected ButtonBase()
        {
        }

        protected internal void SetIsPressed(bool value)
        {
            SetValue(IsPressedPropertyKey, value);
        }

        protected internal virtual void OnClick()
        {
            RoutedEventArgs args = new RoutedEventArgs(ClickEvent, this);
            RaiseEvent(args);

            ExecuteCommand();
        }

        private void ExecuteCommand()
        {
            ICommand cmd = Command;
            if (cmd != null)
            {
                object param = CommandParameter;
                IInputElement target = CommandTarget;

                if (cmd is RoutedCommand routedCmd)
                {
                    if (routedCmd.CanExecute(param, target))
                    {
                        routedCmd.Execute(param, target);
                    }
                }
                else
                {
                    if (cmd.CanExecute(param))
                    {
                        cmd.Execute(param);
                    }
                }
            }
        }

        private static void OnCommandChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ButtonBase btn)
            {
                btn.HookCommand(e.OldValue as ICommand, e.NewValue as ICommand);
            }
        }

        private static void OnCommandParameterChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ButtonBase btn)
            {
                btn.UpdateCanExecute();
            }
        }

        private void HookCommand(ICommand oldCmd, ICommand newCmd)
        {
            if (oldCmd != null && _canExecuteChangedHandler != null)
            {
                oldCmd.CanExecuteChanged -= _canExecuteChangedHandler;
            }

            if (newCmd != null)
            {
                _canExecuteChangedHandler = (s, e) => UpdateCanExecute();
                newCmd.CanExecuteChanged += _canExecuteChangedHandler;
            }
            UpdateCanExecute();
        }

        private bool CanExecute
        {
            get => !ReadControlFlag(ControlBoolFlags.CommandDisabled);
            set
            {
                if (value != CanExecute)
                {
                    WriteControlFlag(ControlBoolFlags.CommandDisabled, !value);
                    CoerceValue(IsEnabledProperty);
                }
            }
        }

        protected override bool IsEnabledCore => base.IsEnabledCore && CanExecute;

        private void UpdateCanExecute()
        {
            ICommand cmd = Command;
            if (cmd != null)
            {
                object param = CommandParameter;
                bool canExecute = cmd is RoutedCommand routedCmd
                    ? routedCmd.CanExecute(param, CommandTarget)
                    : cmd.CanExecute(param);

                CanExecute = canExecute;
            }
            else
            {
                CanExecute = true;
            }
        }

        internal override void ChangeVisualState(bool useTransitions)
        {
            if (!IsEnabled)
            {
                VisualStateManager.GoToState(this, VisualStates.StateDisabled, useTransitions);
            }
            else if (IsPressed)
            {
                VisualStateManager.GoToState(this, VisualStates.StatePressed, useTransitions);
            }
            else if (IsMouseOver)
            {
                VisualStateManager.GoToState(this, VisualStates.StateMouseOver, useTransitions);
            }
            else
            {
                VisualStateManager.GoToState(this, VisualStates.StateNormal, useTransitions);
            }

            if (IsKeyboardFocused)
            {
                VisualStateManager.GoToState(this, VisualStates.StateFocused, useTransitions);
            }
            else
            {
                VisualStateManager.GoToState(this, VisualStates.StateUnfocused, useTransitions);
            }

            base.ChangeVisualState(useTransitions);
        }

        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            if (ClickMode != ClickMode.Hover)
            {
                e.Handled = true;
                Focus();
                CaptureMouse();

                if (IsMouseCaptured)
                {
                    SetIsPressed(true);

                    if (ClickMode == ClickMode.Press)
                    {
                        OnClick();
                    }
                }
            }

            base.OnMouseLeftButtonDown(e);
        }

        protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
        {
            if (ClickMode != ClickMode.Hover)
            {
                e.Handled = true;
                bool shouldClick = IsPressed && (ClickMode == ClickMode.Release);

                if (IsMouseCaptured)
                {
                    ReleaseMouseCapture();
                }

                SetIsPressed(false);

                if (shouldClick)
                {
                    OnClick();
                }
            }

            base.OnMouseLeftButtonUp(e);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);

            if (IsMouseCaptured && ClickMode != ClickMode.Hover)
            {
                Point pos = e.GetPosition(this);
                bool inside = pos.X >= 0.0 && pos.Y >= 0.0 && pos.X <= ActualWidth && pos.Y <= ActualHeight;
                SetIsPressed(inside);
            }
        }

        protected override void OnMouseEnter(MouseEventArgs e)
        {
            base.OnMouseEnter(e);
            InvalidateVisual();

            if (ClickMode == ClickMode.Hover)
            {
                SetIsPressed(true);
                OnClick();
            }
        }

        protected override void OnMouseLeave(MouseEventArgs e)
        {
            base.OnMouseLeave(e);
            InvalidateVisual();

            if (ClickMode == ClickMode.Hover)
            {
                SetIsPressed(false);
            }
            else if (IsMouseCaptured)
            {
                SetIsPressed(false);
            }
        }

        protected override void OnLostMouseCapture(MouseEventArgs e)
        {
            base.OnLostMouseCapture(e);
            SetIsPressed(false);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);

            if (ClickMode != ClickMode.Hover)
            {
                if (e.Key == Key.Space)
                {
                    SetIsPressed(true);
                    e.Handled = true;
                }
                else if (e.Key == Key.Enter)
                {
                    OnClick();
                    e.Handled = true;
                }
            }
        }

        protected override Size MeasureOverride(Size constraint)
        {
            if (Template != null)
            {
                return base.MeasureOverride(constraint);
            }
            Size baseSize = base.MeasureOverride(constraint);
            Thickness pad = Padding;
            double w = baseSize.Width + pad.Left + pad.Right;
            double h = baseSize.Height + pad.Top + pad.Bottom;
            return new Size(Math.Max(w, 24.0), Math.Max(h, 24.0));
        }

        protected override Size ArrangeOverride(Size arrangeBounds)
        {
            if (Template != null)
            {
                return base.ArrangeOverride(arrangeBounds);
            }
            Thickness pad = Padding;
            if (VisualChildrenCount > 0)
            {
                if (GetVisualChild(0) is UIElement child)
                {
                    double x = pad.Left;
                    double y = pad.Top;
                    double w = Math.Max(0, arrangeBounds.Width - pad.Left - pad.Right);
                    double h = Math.Max(0, arrangeBounds.Height - pad.Top - pad.Bottom);
                    child.Arrange(new Rect(x, y, w, h));
                }
            }
            return arrangeBounds;
        }

        protected override void OnRender(DrawingContext dc)
        {
            base.OnRender(dc);
            if (Template != null) return;

            Rect bounds = new Rect(0, 0, RenderSize.Width, RenderSize.Height);
            if (bounds.Width <= 0 || bounds.Height <= 0) return;

            Brush bg = Background;
            if (bg == null)
            {
                if (!IsEnabled)
                    bg = new SolidColorBrush(Color.FromArgb(255, 38, 38, 38));
                else if (IsPressed)
                    bg = new SolidColorBrush(Color.FromArgb(255, 68, 68, 68));
                else if (IsMouseOver)
                    bg = new SolidColorBrush(Color.FromArgb(255, 54, 54, 54));
                else
                    bg = new SolidColorBrush(Color.FromArgb(255, 44, 44, 44));
            }

            Brush borderBrush = BorderBrush ?? new SolidColorBrush(Color.FromArgb(255, 75, 75, 75));
            Thickness th = BorderThickness;
            double stroke = th.Left > 0 ? th.Left : 1.0;
            Pen pen = new Pen(borderBrush, stroke);

            dc.DrawRoundedRectangle(bg, pen, bounds, 4, 4);
        }

        protected override void OnKeyUp(KeyEventArgs e)
        {
            base.OnKeyUp(e);

            if (ClickMode != ClickMode.Hover && e.Key == Key.Space)
            {
                bool wasPressed = IsPressed;
                SetIsPressed(false);
                if (wasPressed)
                {
                    OnClick();
                }
                e.Handled = true;
            }
        }

        internal void AutomationButtonBaseClick() => OnClick();
    }
}

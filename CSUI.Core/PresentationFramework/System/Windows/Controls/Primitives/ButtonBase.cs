// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;

namespace System.Windows.Controls.Primitives
{
    /// <summary>
    /// Represents the base class for all Button controls.
    /// </summary>
    public abstract class ButtonBase : ContentControl, ICommandSource
    {
        private static readonly DependencyPropertyKey IsPressedPropertyKey =
            DependencyProperty.RegisterReadOnly(
                nameof(IsPressed),
                typeof(bool),
                typeof(ButtonBase),
                new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.AffectsRender));

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
                UpdateCanExecute();
            }
            else
            {
                CoerceValue(IsEnabledProperty);
            }
        }

        private void UpdateCanExecute()
        {
            ICommand cmd = Command;
            if (cmd != null)
            {
                object param = CommandParameter;
                bool canExecute;
                if (cmd is RoutedCommand routedCmd)
                {
                    canExecute = routedCmd.CanExecute(param, CommandTarget);
                }
                else
                {
                    canExecute = cmd.CanExecute(param);
                }

                IsEnabled = canExecute;
            }
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

            if (ClickMode == ClickMode.Hover)
            {
                SetIsPressed(true);
                OnClick();
            }
        }

        protected override void OnMouseLeave(MouseEventArgs e)
        {
            base.OnMouseLeave(e);

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
    }
}

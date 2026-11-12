// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System.ComponentModel;
using System.Windows.Automation.Peers;
using System.Windows.Input;
using System.Windows.Threading;
using MS.Internal.PresentationFramework;

namespace System.Windows.Controls.Primitives
{
    public class RepeatButton : ButtonBase
    {
        static RepeatButton()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(RepeatButton), new FrameworkPropertyMetadata(typeof(RepeatButton)));
            _dType = DependencyObjectType.FromSystemTypeInternal(typeof(RepeatButton));
            ClickModeProperty.OverrideMetadata(typeof(RepeatButton), new FrameworkPropertyMetadata(ClickMode.Press));
        }

        public RepeatButton() : base()
        {
        }

        public static readonly DependencyProperty DelayProperty =
            DependencyProperty.Register("Delay", typeof(int), typeof(RepeatButton),
                new FrameworkPropertyMetadata(GetKeyboardDelay()),
                new ValidateValueCallback(IsDelayValid));

        [Bindable(true), Category("Behavior")]
        public int Delay
        {
            get { return (int)GetValue(DelayProperty); }
            set { SetValue(DelayProperty, value); }
        }

        public static readonly DependencyProperty IntervalProperty =
            DependencyProperty.Register("Interval", typeof(int), typeof(RepeatButton),
                new FrameworkPropertyMetadata(GetKeyboardSpeed()),
                new ValidateValueCallback(IsIntervalValid));

        [Bindable(true), Category("Behavior")]
        public int Interval
        {
            get { return (int)GetValue(IntervalProperty); }
            set { SetValue(IntervalProperty, value); }
        }

        private static bool IsDelayValid(object value)
        {
            return ((int)value) >= 0;
        }

        private static bool IsIntervalValid(object value)
        {
            return ((int)value) > 0;
        }

        private void StartTimer()
        {
            if (_timer == null)
            {
                _timer = new DispatcherTimer();
                _timer.Tick += new EventHandler(OnTimeout);
            }
            else if (_timer.IsEnabled)
            {
                return;
            }

            _timer.Interval = TimeSpan.FromMilliseconds(Delay);
            _timer.Start();
        }

        private void StopTimer()
        {
            _timer?.Stop();
        }

        private void OnTimeout(object sender, EventArgs e)
        {
            TimeSpan interval = TimeSpan.FromMilliseconds(Interval);
            if (_timer.Interval != interval)
                _timer.Interval = interval;

            if (IsPressed)
            {
                OnClick();
            }
        }

        internal static int GetKeyboardDelay()
        {
            int delay = SystemParameters.KeyboardDelay;
            if (delay < 0 || delay > 3)
                delay = 0;
            return (delay + 1) * 250;
        }

        internal static int GetKeyboardSpeed()
        {
            int speed = SystemParameters.KeyboardSpeed;
            if (speed < 0 || speed > 31)
                speed = 31;
            return (31 - speed) * (400 - 1000 / 30) / 31 + 1000 / 30;
        }

        protected override AutomationPeer OnCreateAutomationPeer()
        {
            return new RepeatButtonAutomationPeer(this);
        }

        protected internal override void OnClick()
        {
            if (AutomationPeer.ListenerExists(AutomationEvents.InvokePatternOnInvoked))
            {
                AutomationPeer peer = UIElementAutomationPeer.CreatePeerForElement(this);
                peer?.RaiseAutomationEvent(AutomationEvents.InvokePatternOnInvoked);
            }

            base.OnClick();
        }

        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonDown(e);

            if (IsPressed && (ClickMode != ClickMode.Hover))
            {
                StartTimer();
            }
        }

        protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonUp(e);

            if (ClickMode != ClickMode.Hover)
            {
                StopTimer();
            }
        }

        protected override void OnLostMouseCapture(MouseEventArgs e)
        {
            base.OnLostMouseCapture(e);
            StopTimer();
        }

        protected override void OnMouseEnter(MouseEventArgs e)
        {
            base.OnMouseEnter(e);
            if (HandleIsMouseOverChanged())
            {
                e.Handled = true;
            }
        }

        protected override void OnMouseLeave(MouseEventArgs e)
        {
            base.OnMouseLeave(e);
            if (HandleIsMouseOverChanged())
            {
                e.Handled = true;
            }
        }

        private bool HandleIsMouseOverChanged()
        {
            if (ClickMode == ClickMode.Hover)
            {
                if (IsMouseOver)
                {
                    StartTimer();
                }
                else
                {
                    StopTimer();
                }

                return true;
            }

            return false;
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if ((e.Key == Key.Space) && (ClickMode != ClickMode.Hover))
            {
                StartTimer();
            }
        }

        protected override void OnKeyUp(KeyEventArgs e)
        {
            if ((e.Key == Key.Space) && (ClickMode != ClickMode.Hover))
            {
                StopTimer();
            }
            base.OnKeyUp(e);
        }

        private DispatcherTimer _timer;

        internal override DependencyObjectType DTypeThemeStyleKey
        {
            get { return _dType; }
        }

        private static DependencyObjectType _dType;
    }
}

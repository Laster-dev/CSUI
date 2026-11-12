// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System.ComponentModel;
using System.Windows.Automation.Peers;
using System.Windows.Controls.Primitives;
using MS.Internal.KnownBoxes;

namespace System.Windows.Controls
{
    public enum ExpandDirection
    {
        Down = 0,
        Up = 1,
        Left = 2,
        Right = 3,
    }

    [Localizability(LocalizationCategory.None, Readability = Readability.Unreadable)]
    public partial class Expander : HeaderedContentControl
    {
        static partial void RegisterDefaultTheme();
        static Expander()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(Expander), new FrameworkPropertyMetadata(typeof(Expander)));
            _dType = DependencyObjectType.FromSystemTypeInternal(typeof(Expander));

            IsTabStopProperty.OverrideMetadata(typeof(Expander), new FrameworkPropertyMetadata(BooleanBoxes.FalseBox));
            IsMouseOverPropertyKey.OverrideMetadata(typeof(Expander), new UIPropertyMetadata(new PropertyChangedCallback(OnVisualStatePropertyChanged)));
            IsEnabledProperty.OverrideMetadata(typeof(Expander), new UIPropertyMetadata(new PropertyChangedCallback(OnVisualStatePropertyChanged)));
            RegisterDefaultTheme();
        }

        public static readonly DependencyProperty ExpandDirectionProperty =
            DependencyProperty.Register(
                "ExpandDirection",
                typeof(ExpandDirection),
                typeof(Expander),
                new FrameworkPropertyMetadata(
                    ExpandDirection.Down,
                    FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                    new PropertyChangedCallback(OnVisualStatePropertyChanged)),
                new ValidateValueCallback(IsValidExpandDirection));

        [Bindable(true), Category("Behavior")]
        public ExpandDirection ExpandDirection
        {
            get { return (ExpandDirection)GetValue(ExpandDirectionProperty); }
            set { SetValue(ExpandDirectionProperty, value); }
        }

        private static bool IsValidExpandDirection(object o)
        {
            ExpandDirection value = (ExpandDirection)o;
            return value == ExpandDirection.Down
                || value == ExpandDirection.Left
                || value == ExpandDirection.Right
                || value == ExpandDirection.Up;
        }

        public static readonly DependencyProperty IsExpandedProperty =
            DependencyProperty.Register(
                "IsExpanded",
                typeof(bool),
                typeof(Expander),
                new FrameworkPropertyMetadata(
                    BooleanBoxes.FalseBox,
                    FrameworkPropertyMetadataOptions.BindsTwoWayByDefault | FrameworkPropertyMetadataOptions.Journal,
                    new PropertyChangedCallback(OnIsExpandedChanged)));

        private static void OnIsExpandedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            Expander ep = (Expander)d;
            bool newValue = (bool)e.NewValue;

            ExpanderAutomationPeer peer = UIElementAutomationPeer.FromElement(ep) as ExpanderAutomationPeer;
            peer?.RaiseExpandCollapseAutomationEvent(!newValue, newValue);

            if (newValue)
            {
                ep.OnExpanded();
            }
            else
            {
                ep.OnCollapsed();
            }

            ep.UpdateVisualState();
        }

        [Bindable(true), Category("Appearance")]
        public bool IsExpanded
        {
            get { return (bool)GetValue(IsExpandedProperty); }
            set { SetValue(IsExpandedProperty, BooleanBoxes.Box(value)); }
        }

        public static readonly RoutedEvent ExpandedEvent =
            EventManager.RegisterRoutedEvent<RoutedEventHandler>(
                "Expanded",
                RoutingStrategy.Bubble,
                typeof(Expander),
                static (h, s, e) => h(s, e));

        public event RoutedEventHandler Expanded
        {
            add { AddHandler(ExpandedEvent, value); }
            remove { RemoveHandler(ExpandedEvent, value); }
        }

        public static readonly RoutedEvent CollapsedEvent =
            EventManager.RegisterRoutedEvent<RoutedEventHandler>(
                "Collapsed",
                RoutingStrategy.Bubble,
                typeof(Expander),
                static (h, s, e) => h(s, e));

        public event RoutedEventHandler Collapsed
        {
            add { AddHandler(CollapsedEvent, value); }
            remove { RemoveHandler(CollapsedEvent, value); }
        }

        internal override void ChangeVisualState(bool useTransitions)
        {
            if (!IsEnabled)
            {
                VisualStates.GoToState(this, useTransitions, VisualStates.StateDisabled, VisualStates.StateNormal);
            }
            else if (IsMouseOver)
            {
                VisualStates.GoToState(this, useTransitions, VisualStates.StateMouseOver, VisualStates.StateNormal);
            }
            else
            {
                VisualStates.GoToState(this, useTransitions, VisualStates.StateNormal);
            }

            if (IsKeyboardFocused)
            {
                VisualStates.GoToState(this, useTransitions, VisualStates.StateFocused, VisualStates.StateUnfocused);
            }
            else
            {
                VisualStates.GoToState(this, useTransitions, VisualStates.StateUnfocused);
            }

            if (IsExpanded)
            {
                VisualStates.GoToState(this, useTransitions, VisualStates.StateExpanded);
            }
            else
            {
                VisualStates.GoToState(this, useTransitions, VisualStates.StateCollapsed);
            }

            switch (ExpandDirection)
            {
                case ExpandDirection.Down:
                    VisualStates.GoToState(this, useTransitions, VisualStates.StateExpandDown);
                    break;
                case ExpandDirection.Up:
                    VisualStates.GoToState(this, useTransitions, VisualStates.StateExpandUp);
                    break;
                case ExpandDirection.Left:
                    VisualStates.GoToState(this, useTransitions, VisualStates.StateExpandLeft);
                    break;
                default:
                    VisualStates.GoToState(this, useTransitions, VisualStates.StateExpandRight);
                    break;
            }

            base.ChangeVisualState(useTransitions);
        }

        internal bool IsExpanderToggleButtonFocused
        {
            get
            {
                ToggleButton expandSite = ExpanderToggleButton;
                return expandSite != null && expandSite.IsKeyboardFocused;
            }
        }

        internal ToggleButton ExpanderToggleButton
        {
            get
            {
                return _expanderToggleButton;
            }
        }

        protected virtual void OnExpanded()
        {
            RoutedEventArgs args = new RoutedEventArgs();
            args.RoutedEvent = Expander.ExpandedEvent;
            args.Source = this;
            RaiseEvent(args);
        }

        protected virtual void OnCollapsed()
        {
            RaiseEvent(new RoutedEventArgs(Expander.CollapsedEvent, this));
        }

        protected override AutomationPeer OnCreateAutomationPeer()
        {
            return new ExpanderAutomationPeer(this);
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            _expanderToggleButton = GetTemplateChild(ExpanderToggleButtonName) as ToggleButton;
        }

        private const string ExpanderToggleButtonName = "HeaderSite";
        private ToggleButton _expanderToggleButton;

        internal override DependencyObjectType DTypeThemeStyleKey
        {
            get { return _dType; }
        }

        private static DependencyObjectType _dType;
    }
}

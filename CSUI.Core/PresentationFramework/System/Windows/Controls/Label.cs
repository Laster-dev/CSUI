// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System.ComponentModel;
using System.Windows.Automation.Peers;
using System.Windows.Input;
using MS.Internal.Controls;
using MS.Internal.KnownBoxes;

namespace System.Windows.Controls
{
    [Localizability(LocalizationCategory.Label)]
    public class Label : ContentControl
    {
        static Label()
        {
            EventManager.RegisterClassHandler(typeof(Label), AccessKeyManager.AccessKeyPressedEvent, new AccessKeyPressedEventHandler(OnAccessKeyPressed));

            DefaultStyleKeyProperty.OverrideMetadata(typeof(Label), new FrameworkPropertyMetadata(typeof(Label)));
            _dType = DependencyObjectType.FromSystemTypeInternal(typeof(Label));

            IsTabStopProperty.OverrideMetadata(typeof(Label), new FrameworkPropertyMetadata(BooleanBoxes.FalseBox));
            FocusableProperty.OverrideMetadata(typeof(Label), new FrameworkPropertyMetadata(BooleanBoxes.FalseBox));

            ControlFoundationHooks.GetLabeledByHook = static d => GetLabeledBy(d);
        }

        public Label() : base()
        {
        }

        public static readonly DependencyProperty TargetProperty =
            DependencyProperty.Register(
                "Target",
                typeof(UIElement),
                typeof(Label),
                new FrameworkPropertyMetadata(
                    (UIElement)null,
                    new PropertyChangedCallback(OnTargetChanged)));

        public UIElement Target
        {
            get { return (UIElement)GetValue(TargetProperty); }
            set { SetValue(TargetProperty, value); }
        }

        private static void OnTargetChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            Label label = (Label)d;

            UIElement oldElement = (UIElement)e.OldValue;
            UIElement newElement = (UIElement)e.NewValue;

            if (oldElement != null)
            {
                object oldValue = oldElement.GetValue(LabeledByProperty);
                if (oldValue == label)
                {
                    oldElement.ClearValue(LabeledByProperty);
                }
            }

            if (newElement != null)
            {
                newElement.SetValue(LabeledByProperty, label);
            }
        }

        private static readonly DependencyProperty LabeledByProperty =
            DependencyProperty.RegisterAttached("LabeledBy", typeof(Label), typeof(Label),
                new FrameworkPropertyMetadata((Label)null));

        internal static Label GetLabeledBy(DependencyObject o)
        {
            ArgumentNullException.ThrowIfNull(o);
            return (Label)o.GetValue(LabeledByProperty);
        }

        protected override AutomationPeer OnCreateAutomationPeer()
        {
            return new LabelAutomationPeer(this);
        }

        private static void OnAccessKeyPressed(object sender, AccessKeyPressedEventArgs e)
        {
            Label label = sender as Label;
            if (!e.Handled && e.Scope == null && (e.Target == null || e.Target == label))
            {
                e.Target = label.Target;
            }
        }

        internal override DependencyObjectType DTypeThemeStyleKey
        {
            get { return _dType; }
        }

        private static DependencyObjectType _dType;
    }
}

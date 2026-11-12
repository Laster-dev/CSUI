// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System;
using System.ComponentModel;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using MS.Internal.Controls;
using MS.Internal.KnownBoxes;

namespace System.Windows.Controls
{
    public static class ToolTipService
    {
        public static readonly DependencyProperty ToolTipProperty =
            DependencyProperty.RegisterAttached(
                "ToolTip",
                typeof(object),
                typeof(ToolTipService),
                new FrameworkPropertyMetadata(null, OnToolTipServiceToolTipChanged));

        private static void OnToolTipServiceToolTipChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue != null)
            {
                PopupControlService.EnsureInitialized();
            }
        }

        public static object GetToolTip(DependencyObject element)
        {
            ArgumentNullException.ThrowIfNull(element);
            return element.GetValue(ToolTipProperty);
        }

        public static void SetToolTip(DependencyObject element, object value)
        {
            ArgumentNullException.ThrowIfNull(element);
            element.SetValue(ToolTipProperty, value);
        }

        public static readonly DependencyProperty HorizontalOffsetProperty =
            DependencyProperty.RegisterAttached(
                "HorizontalOffset",
                typeof(double),
                typeof(ToolTipService),
                new FrameworkPropertyMetadata(0.0));

        public static double GetHorizontalOffset(DependencyObject element)
        {
            ArgumentNullException.ThrowIfNull(element);
            return (double)element.GetValue(HorizontalOffsetProperty);
        }

        public static void SetHorizontalOffset(DependencyObject element, double value)
        {
            ArgumentNullException.ThrowIfNull(element);
            element.SetValue(HorizontalOffsetProperty, value);
        }

        public static readonly DependencyProperty VerticalOffsetProperty =
            DependencyProperty.RegisterAttached(
                "VerticalOffset",
                typeof(double),
                typeof(ToolTipService),
                new FrameworkPropertyMetadata(0.0));

        public static double GetVerticalOffset(DependencyObject element)
        {
            ArgumentNullException.ThrowIfNull(element);
            return (double)element.GetValue(VerticalOffsetProperty);
        }

        public static void SetVerticalOffset(DependencyObject element, double value)
        {
            ArgumentNullException.ThrowIfNull(element);
            element.SetValue(VerticalOffsetProperty, value);
        }

        public static readonly DependencyProperty PlacementTargetProperty =
            DependencyProperty.RegisterAttached(
                "PlacementTarget",
                typeof(UIElement),
                typeof(ToolTipService),
                new FrameworkPropertyMetadata(null));

        public static UIElement GetPlacementTarget(DependencyObject element)
        {
            ArgumentNullException.ThrowIfNull(element);
            return (UIElement)element.GetValue(PlacementTargetProperty);
        }

        public static void SetPlacementTarget(DependencyObject element, UIElement value)
        {
            ArgumentNullException.ThrowIfNull(element);
            element.SetValue(PlacementTargetProperty, value);
        }

        public static readonly DependencyProperty PlacementProperty =
            DependencyProperty.RegisterAttached(
                "Placement",
                typeof(PlacementMode),
                typeof(ToolTipService),
                new FrameworkPropertyMetadata(PlacementMode.Mouse));

        public static PlacementMode GetPlacement(DependencyObject element)
        {
            ArgumentNullException.ThrowIfNull(element);
            return (PlacementMode)element.GetValue(PlacementProperty);
        }

        public static void SetPlacement(DependencyObject element, PlacementMode value)
        {
            ArgumentNullException.ThrowIfNull(element);
            element.SetValue(PlacementProperty, value);
        }

        public static readonly DependencyProperty HasDropShadowProperty =
            DependencyProperty.RegisterAttached(
                "HasDropShadow",
                typeof(bool),
                typeof(ToolTipService),
                new FrameworkPropertyMetadata(BooleanBoxes.FalseBox));

        public static bool GetHasDropShadow(DependencyObject element)
        {
            ArgumentNullException.ThrowIfNull(element);
            return (bool)element.GetValue(HasDropShadowProperty);
        }

        public static void SetHasDropShadow(DependencyObject element, bool value)
        {
            ArgumentNullException.ThrowIfNull(element);
            element.SetValue(HasDropShadowProperty, BooleanBoxes.Box(value));
        }

        public static readonly DependencyProperty PlacementRectangleProperty =
            DependencyProperty.RegisterAttached(
                "PlacementRectangle",
                typeof(Rect),
                typeof(ToolTipService),
                new FrameworkPropertyMetadata(Rect.Empty));

        public static Rect GetPlacementRectangle(DependencyObject element)
        {
            ArgumentNullException.ThrowIfNull(element);
            return (Rect)element.GetValue(PlacementRectangleProperty);
        }

        public static void SetPlacementRectangle(DependencyObject element, Rect value)
        {
            ArgumentNullException.ThrowIfNull(element);
            element.SetValue(PlacementRectangleProperty, value);
        }

        public static readonly DependencyProperty ShowOnDisabledProperty =
            DependencyProperty.RegisterAttached(
                "ShowOnDisabled",
                typeof(bool),
                typeof(ToolTipService),
                new FrameworkPropertyMetadata(BooleanBoxes.FalseBox));

        public static bool GetShowOnDisabled(DependencyObject element)
        {
            ArgumentNullException.ThrowIfNull(element);
            return (bool)element.GetValue(ShowOnDisabledProperty);
        }

        public static void SetShowOnDisabled(DependencyObject element, bool value)
        {
            ArgumentNullException.ThrowIfNull(element);
            element.SetValue(ShowOnDisabledProperty, BooleanBoxes.Box(value));
        }

        private static readonly DependencyPropertyKey IsOpenPropertyKey =
            DependencyProperty.RegisterAttachedReadOnly(
                "IsOpen",
                typeof(bool),
                typeof(ToolTipService),
                new FrameworkPropertyMetadata(BooleanBoxes.FalseBox));

        public static readonly DependencyProperty IsOpenProperty = IsOpenPropertyKey.DependencyProperty;

        public static bool GetIsOpen(DependencyObject element)
        {
            ArgumentNullException.ThrowIfNull(element);
            return (bool)element.GetValue(IsOpenProperty);
        }

        internal static void SetIsOpen(DependencyObject element, bool value)
        {
            ArgumentNullException.ThrowIfNull(element);
            element.SetValue(IsOpenPropertyKey, BooleanBoxes.Box(value));
        }

        public static readonly DependencyProperty IsEnabledProperty =
            DependencyProperty.RegisterAttached(
                "IsEnabled",
                typeof(bool),
                typeof(ToolTipService),
                new FrameworkPropertyMetadata(BooleanBoxes.TrueBox));

        public static bool GetIsEnabled(DependencyObject element)
        {
            ArgumentNullException.ThrowIfNull(element);
            return (bool)element.GetValue(IsEnabledProperty);
        }

        public static void SetIsEnabled(DependencyObject element, bool value)
        {
            ArgumentNullException.ThrowIfNull(element);
            element.SetValue(IsEnabledProperty, BooleanBoxes.Box(value));
        }

        private static bool PositiveValueValidate(object o)
        {
            return ((int)o) >= 0;
        }

        public static readonly DependencyProperty ShowDurationProperty =
            DependencyProperty.RegisterAttached(
                "ShowDuration",
                typeof(int),
                typeof(ToolTipService),
                new FrameworkPropertyMetadata(5000),
                new ValidateValueCallback(PositiveValueValidate));

        public static int GetShowDuration(DependencyObject element)
        {
            ArgumentNullException.ThrowIfNull(element);
            return (int)element.GetValue(ShowDurationProperty);
        }

        public static void SetShowDuration(DependencyObject element, int value)
        {
            ArgumentNullException.ThrowIfNull(element);
            element.SetValue(ShowDurationProperty, value);
        }

        public static readonly DependencyProperty InitialShowDelayProperty =
            DependencyProperty.RegisterAttached(
                "InitialShowDelay",
                typeof(int),
                typeof(ToolTipService),
                new FrameworkPropertyMetadata(400),
                new ValidateValueCallback(PositiveValueValidate));

        public static int GetInitialShowDelay(DependencyObject element)
        {
            ArgumentNullException.ThrowIfNull(element);
            return (int)element.GetValue(InitialShowDelayProperty);
        }

        public static void SetInitialShowDelay(DependencyObject element, int value)
        {
            ArgumentNullException.ThrowIfNull(element);
            element.SetValue(InitialShowDelayProperty, value);
        }

        public static readonly DependencyProperty BetweenShowDelayProperty =
            DependencyProperty.RegisterAttached(
                "BetweenShowDelay",
                typeof(int),
                typeof(ToolTipService),
                new FrameworkPropertyMetadata(100),
                new ValidateValueCallback(PositiveValueValidate));

        public static int GetBetweenShowDelay(DependencyObject element)
        {
            ArgumentNullException.ThrowIfNull(element);
            return (int)element.GetValue(BetweenShowDelayProperty);
        }

        public static void SetBetweenShowDelay(DependencyObject element, int value)
        {
            ArgumentNullException.ThrowIfNull(element);
            element.SetValue(BetweenShowDelayProperty, value);
        }

        public static readonly DependencyProperty ShowsToolTipOnKeyboardFocusProperty =
            DependencyProperty.RegisterAttached(
                "ShowsToolTipOnKeyboardFocus",
                typeof(bool?),
                typeof(ToolTipService),
                new FrameworkPropertyMetadata((bool?)null));

        public static bool? GetShowsToolTipOnKeyboardFocus(DependencyObject element)
        {
            ArgumentNullException.ThrowIfNull(element);
            return (bool?)element.GetValue(ShowsToolTipOnKeyboardFocusProperty);
        }

        public static void SetShowsToolTipOnKeyboardFocus(DependencyObject element, bool? value)
        {
            ArgumentNullException.ThrowIfNull(element);
            element.SetValue(ShowsToolTipOnKeyboardFocusProperty, value);
        }

        public static readonly RoutedEvent ToolTipOpeningEvent =
            EventManager.RegisterRoutedEvent<ToolTipEventHandler>(
                "ToolTipOpening",
                RoutingStrategy.Direct,
                typeof(ToolTipService),
                static (h, s, e) => h(s, (ToolTipEventArgs)e));

        public static void AddToolTipOpeningHandler(DependencyObject element, ToolTipEventHandler handler)
        {
            UIElement.AddHandler(element, ToolTipOpeningEvent, handler);
        }

        public static void RemoveToolTipOpeningHandler(DependencyObject element, ToolTipEventHandler handler)
        {
            UIElement.RemoveHandler(element, ToolTipOpeningEvent, handler);
        }

        public static readonly RoutedEvent ToolTipClosingEvent =
            EventManager.RegisterRoutedEvent<ToolTipEventHandler>(
                "ToolTipClosing",
                RoutingStrategy.Direct,
                typeof(ToolTipService),
                static (h, s, e) => h(s, (ToolTipEventArgs)e));

        public static void AddToolTipClosingHandler(DependencyObject element, ToolTipEventHandler handler)
        {
            UIElement.AddHandler(element, ToolTipClosingEvent, handler);
        }

        public static void RemoveToolTipClosingHandler(DependencyObject element, ToolTipEventHandler handler)
        {
            UIElement.RemoveHandler(element, ToolTipClosingEvent, handler);
        }

        internal static readonly RoutedEvent FindToolTipEvent =
            EventManager.RegisterRoutedEvent<FindToolTipEventHandler>(
                "FindToolTip",
                RoutingStrategy.Bubble,
                typeof(ToolTipService),
                static (h, s, e) => h(s, (FindToolTipEventArgs)e));

        static ToolTipService()
        {
            ControlFoundationHooks.ToolTipPropertyHook = ToolTipProperty;
            ControlFoundationHooks.GetToolTipHook = static d => d?.GetValue(ToolTipProperty);
            EventManager.RegisterClassHandler(typeof(UIElement), FindToolTipEvent, new FindToolTipEventHandler(OnFindToolTip));
        }

        private static void OnFindToolTip(object sender, FindToolTipEventArgs e)
        {
            if (e.TargetElement == null && sender is DependencyObject o && ToolTipIsEnabled(o, e.TriggerAction))
            {
                e.TargetElement = o;
                e.Handled = true;
            }
        }

        internal static bool ToolTipIsEnabled(DependencyObject o, TriggerAction triggerAction)
        {
            object tooltipObject = GetToolTip(o);

            if ((tooltipObject != null) && GetIsEnabled(o))
            {
                bool enableOnKeyboardFocus = true;
                if (triggerAction == TriggerAction.KeyboardFocus)
                {
                    bool? propertyValue = GetShowsToolTipOnKeyboardFocus(o);

                    if (propertyValue == (bool?)null && tooltipObject is ToolTip tooltip)
                    {
                        propertyValue = tooltip.ShowsToolTipOnKeyboardFocus;
                    }

                    enableOnKeyboardFocus = (propertyValue != false);
                }

                if ((PopupControlService.IsElementEnabled(o) || GetShowOnDisabled(o)) && enableOnKeyboardFocus)
                {
                    return true;
                }
            }

            return false;
        }

        internal static bool IsFromKeyboard(TriggerAction triggerAction)
        {
            return triggerAction == TriggerAction.KeyboardFocus || triggerAction == TriggerAction.KeyboardShortcut;
        }

        internal enum TriggerAction
        {
            Mouse,
            KeyboardFocus,
            KeyboardShortcut
        }
    }

    public delegate void ToolTipEventHandler(object sender, ToolTipEventArgs e);

    public sealed class ToolTipEventArgs : RoutedEventArgs
    {
        internal ToolTipEventArgs(bool opening)
        {
            RoutedEvent = opening ? ToolTipService.ToolTipOpeningEvent : ToolTipService.ToolTipClosingEvent;
        }

        protected override void InvokeEventHandler(Delegate genericHandler, object genericTarget)
        {
            ((ToolTipEventHandler)genericHandler)(genericTarget, this);
        }
    }

    internal delegate void FindToolTipEventHandler(object sender, FindToolTipEventArgs e);

    internal sealed class FindToolTipEventArgs : RoutedEventArgs
    {
        internal FindToolTipEventArgs(ToolTipService.TriggerAction triggerAction)
        {
            RoutedEvent = ToolTipService.FindToolTipEvent;
            _triggerAction = triggerAction;
        }

        internal DependencyObject TargetElement
        {
            get => _targetElement;
            set => _targetElement = value;
        }

        internal ToolTipService.TriggerAction TriggerAction => _triggerAction;

        protected override void InvokeEventHandler(Delegate genericHandler, object genericTarget)
        {
            ((FindToolTipEventHandler)genericHandler)(genericTarget, this);
        }

        private DependencyObject _targetElement;
        private readonly ToolTipService.TriggerAction _triggerAction;
    }

    [DefaultEvent(nameof(Opened))]
    public class ToolTip : ContentControl
    {
        private Popup _parentPopup;

        static ToolTip()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(ToolTip), new FrameworkPropertyMetadata(typeof(ToolTip)));
            BackgroundProperty.OverrideMetadata(typeof(ToolTip), new FrameworkPropertyMetadata(new SolidColorBrush(Color.FromRgb(30, 41, 59))));
            ForegroundProperty.OverrideMetadata(typeof(ToolTip), new FrameworkPropertyMetadata(new SolidColorBrush(Color.FromRgb(241, 245, 249))));
            PaddingProperty.OverrideMetadata(typeof(ToolTip), new FrameworkPropertyMetadata(new Thickness(8.0, 4.0, 8.0, 4.0)));
            TemplateProperty.OverrideMetadata(typeof(ToolTip), new FrameworkPropertyMetadata(CreateDefaultToolTipTemplate()));
        }

        private static ControlTemplate CreateDefaultToolTipTemplate()
        {
            FrameworkElementFactory.RegisterFactory(typeof(WindowClientDecorator), static () => new WindowClientDecorator());
            FrameworkElementFactory.RegisterFactory(typeof(ContentPresenter), static () => new ContentPresenter());

            var borderFactory = new FrameworkElementFactory(typeof(WindowClientDecorator));
            borderFactory.SetValue(WindowClientDecorator.BackgroundProperty, new TemplateBindingExtension(BackgroundProperty));
            borderFactory.SetValue(WindowClientDecorator.PaddingProperty, new TemplateBindingExtension(PaddingProperty));

            var presenterFactory = new FrameworkElementFactory(typeof(ContentPresenter));
            borderFactory.AppendChild(presenterFactory);

            var template = new ControlTemplate(typeof(ToolTip))
            {
                VisualTree = borderFactory
            };
            template.Seal();
            return template;
        }

        public static readonly DependencyProperty IsOpenProperty =
            DependencyProperty.Register(
                nameof(IsOpen),
                typeof(bool),
                typeof(ToolTip),
                new FrameworkPropertyMetadata(
                    BooleanBoxes.FalseBox,
                    FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                    OnIsOpenChanged));

        public bool IsOpen
        {
            get => (bool)GetValue(IsOpenProperty);
            set => SetValue(IsOpenProperty, BooleanBoxes.Box(value));
        }

        private static void OnIsOpenChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var toolTip = (ToolTip)d;
            if ((bool)e.NewValue)
            {
                if (toolTip._parentPopup == null)
                {
                    toolTip.HookupParentPopup();
                }
                toolTip._parentPopup.IsOpen = true;
            }
            else if (toolTip._parentPopup != null)
            {
                toolTip._parentPopup.IsOpen = false;
            }
        }

        public static readonly DependencyProperty PlacementTargetProperty =
            ToolTipService.PlacementTargetProperty.AddOwner(typeof(ToolTip));

        public UIElement PlacementTarget
        {
            get => (UIElement)GetValue(PlacementTargetProperty);
            set => SetValue(PlacementTargetProperty, value);
        }

        public static readonly DependencyProperty PlacementProperty =
            ToolTipService.PlacementProperty.AddOwner(typeof(ToolTip));

        public PlacementMode Placement
        {
            get => (PlacementMode)GetValue(PlacementProperty);
            set => SetValue(PlacementProperty, value);
        }

        public static readonly DependencyProperty HorizontalOffsetProperty =
            ToolTipService.HorizontalOffsetProperty.AddOwner(typeof(ToolTip));

        public double HorizontalOffset
        {
            get => (double)GetValue(HorizontalOffsetProperty);
            set => SetValue(HorizontalOffsetProperty, value);
        }

        public static readonly DependencyProperty VerticalOffsetProperty =
            ToolTipService.VerticalOffsetProperty.AddOwner(typeof(ToolTip));

        public double VerticalOffset
        {
            get => (double)GetValue(VerticalOffsetProperty);
            set => SetValue(VerticalOffsetProperty, value);
        }

        public static readonly DependencyProperty StaysOpenProperty =
            Popup.StaysOpenProperty.AddOwner(typeof(ToolTip));

        public bool StaysOpen
        {
            get => (bool)GetValue(StaysOpenProperty);
            set => SetValue(StaysOpenProperty, BooleanBoxes.Box(value));
        }

        internal static readonly DependencyProperty FromKeyboardProperty =
            DependencyProperty.Register(
                nameof(FromKeyboard),
                typeof(bool),
                typeof(ToolTip),
                new FrameworkPropertyMetadata(BooleanBoxes.FalseBox));

        internal bool FromKeyboard
        {
            get => (bool)GetValue(FromKeyboardProperty);
            set => SetValue(FromKeyboardProperty, BooleanBoxes.Box(value));
        }

        public static readonly DependencyProperty HasDropShadowProperty =
            ToolTipService.HasDropShadowProperty.AddOwner(typeof(ToolTip));

        public bool HasDropShadow
        {
            get => (bool)GetValue(HasDropShadowProperty);
            set => SetValue(HasDropShadowProperty, BooleanBoxes.Box(value));
        }

        public static readonly DependencyProperty PlacementRectangleProperty =
            ToolTipService.PlacementRectangleProperty.AddOwner(typeof(ToolTip));

        public Rect PlacementRectangle
        {
            get => (Rect)GetValue(PlacementRectangleProperty);
            set => SetValue(PlacementRectangleProperty, value);
        }

        public static readonly DependencyProperty CustomPopupPlacementCallbackProperty =
            Popup.CustomPopupPlacementCallbackProperty.AddOwner(typeof(ToolTip));

        public CustomPopupPlacementCallback CustomPopupPlacementCallback
        {
            get => (CustomPopupPlacementCallback)GetValue(CustomPopupPlacementCallbackProperty);
            set => SetValue(CustomPopupPlacementCallbackProperty, value);
        }

        public static readonly DependencyProperty ShowsToolTipOnKeyboardFocusProperty =
            ToolTipService.ShowsToolTipOnKeyboardFocusProperty.AddOwner(typeof(ToolTip));

        public bool? ShowsToolTipOnKeyboardFocus
        {
            get => (bool?)GetValue(ShowsToolTipOnKeyboardFocusProperty);
            set => SetValue(ShowsToolTipOnKeyboardFocusProperty, value);
        }

        internal void ForceClose()
        {
            if (_parentPopup != null)
            {
                _parentPopup.IsOpen = false;
            }
        }

        internal Rect GetScreenRect()
        {
            if (_parentPopup?.PopupHwndSource == null)
            {
                return Rect.Empty;
            }

            Point origin = PointToScreen(new Point(0.0, 0.0));
            return new Rect(origin, RenderSize);
        }

        public static readonly RoutedEvent OpenedEvent =
            EventManager.RegisterRoutedEvent(
                nameof(Opened),
                RoutingStrategy.Bubble,
                typeof(RoutedEventHandler),
                typeof(ToolTip));

        public event RoutedEventHandler Opened
        {
            add => AddHandler(OpenedEvent, value);
            remove => RemoveHandler(OpenedEvent, value);
        }

        public static readonly RoutedEvent ClosedEvent =
            EventManager.RegisterRoutedEvent(
                nameof(Closed),
                RoutingStrategy.Bubble,
                typeof(RoutedEventHandler),
                typeof(ToolTip));

        public event RoutedEventHandler Closed
        {
            add => AddHandler(ClosedEvent, value);
            remove => RemoveHandler(ClosedEvent, value);
        }

        internal Popup ParentPopup => _parentPopup;

        private void HookupParentPopup()
        {
            _parentPopup = new Popup
            {
                AllowsTransparency = true,
                PlacementTarget = PlacementTarget,
                Placement = Placement,
                HorizontalOffset = HorizontalOffset,
                VerticalOffset = VerticalOffset,
                StaysOpen = StaysOpen,
                Child = this
            };
            _parentPopup.Opened += OnPopupOpened;
            _parentPopup.Closed += OnPopupClosed;
        }

        private void OnPopupOpened(object sender, EventArgs e)
        {
            OnOpened(new RoutedEventArgs(OpenedEvent, this));
        }

        private void OnPopupClosed(object sender, EventArgs e)
        {
            OnClosed(new RoutedEventArgs(ClosedEvent, this));
        }

        protected virtual void OnOpened(RoutedEventArgs e) => RaiseEvent(e);
        protected virtual void OnClosed(RoutedEventArgs e) => RaiseEvent(e);
    }
}

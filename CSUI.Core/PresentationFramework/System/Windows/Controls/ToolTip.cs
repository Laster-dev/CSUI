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
                new FrameworkPropertyMetadata(null));

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

        static ToolTipService()
        {
            ControlFoundationHooks.ToolTipPropertyHook = ToolTipProperty;
            ControlFoundationHooks.GetToolTipHook = static d => d?.GetValue(ToolTipProperty);
        }
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

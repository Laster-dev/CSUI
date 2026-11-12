// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System;
using System.Windows;
using System.Windows.Controls;

namespace System.Windows.Controls.Ribbon
{
    public enum KeyTipHorizontalPlacement
    {
        KeyTipLeftAtTargetLeft = 0,
        KeyTipLeftAtTargetCenter = 1,
        KeyTipLeftAtTargetRight = 2,
        KeyTipCenterAtTargetLeft = 3,
        KeyTipCenterAtTargetCenter = 4,
        KeyTipCenterAtTargetRight = 5,
        KeyTipRightAtTargetLeft = 6,
        KeyTipRightAtTargetCenter = 7,
        KeyTipRightAtTargetRight = 8
    }

    public enum KeyTipVerticalPlacement
    {
        KeyTipTopAtTargetTop = 0,
        KeyTipTopAtTargetCenter = 1,
        KeyTipTopAtTargetBottom = 2,
        KeyTipCenterAtTargetTop = 3,
        KeyTipCenterAtTargetCenter = 4,
        KeyTipCenterAtTargetBottom = 5,
        KeyTipBottomAtTargetTop = 6,
        KeyTipBottomAtTargetCenter = 7,
        KeyTipBottomAtTargetBottom = 8
    }

    public delegate void ActivatingKeyTipEventHandler(object sender, ActivatingKeyTipEventArgs e);

    public class ActivatingKeyTipEventArgs : RoutedEventArgs
    {
        public ActivatingKeyTipEventArgs()
        {
            RoutedEvent = KeyTipService.ActivatingKeyTipEvent;
            KeyTipVisibility = Visibility.Visible;
            KeyTipHorizontalPlacement = KeyTipHorizontalPlacement.KeyTipCenterAtTargetCenter;
            KeyTipVerticalPlacement = KeyTipVerticalPlacement.KeyTipCenterAtTargetBottom;
        }

        public KeyTipHorizontalPlacement KeyTipHorizontalPlacement { get; set; }
        public KeyTipVerticalPlacement KeyTipVerticalPlacement { get; set; }
        public double KeyTipHorizontalOffset { get; set; }
        public double KeyTipVerticalOffset { get; set; }
        public UIElement PlacementTarget { get; set; }
        public Visibility KeyTipVisibility { get; set; }
        internal DependencyObject Owner { get; set; }

        protected override void InvokeEventHandler(Delegate genericHandler, object genericTarget)
        {
            if (genericHandler is ActivatingKeyTipEventHandler handler)
            {
                handler(genericTarget, this);
            }
            else
            {
                base.InvokeEventHandler(genericHandler, genericTarget);
            }
        }
    }

    public delegate void KeyTipAccessedEventHandler(object sender, KeyTipAccessedEventArgs e);

    public class KeyTipAccessedEventArgs : RoutedEventArgs
    {
        public DependencyObject TargetKeyTipScope { get; set; }

        protected override void InvokeEventHandler(Delegate genericHandler, object genericTarget)
        {
            if (genericHandler is KeyTipAccessedEventHandler handler)
            {
                handler(genericTarget, this);
            }
            else
            {
                base.InvokeEventHandler(genericHandler, genericTarget);
            }
        }
    }

    public class KeyTipControl : Control
    {
        public static readonly DependencyProperty TextProperty =
            DependencyProperty.Register(nameof(Text), typeof(string), typeof(KeyTipControl),
                new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender));

        public string Text
        {
            get => (string)GetValue(TextProperty);
            set => SetValue(TextProperty, value);
        }
    }

    public static class KeyTipService
    {
        public static readonly DependencyProperty KeyTipProperty =
            DependencyProperty.RegisterAttached("KeyTip", typeof(string), typeof(KeyTipService), new FrameworkPropertyMetadata(null));

        public static readonly DependencyProperty IsKeyTipScopeProperty =
            DependencyProperty.RegisterAttached("IsKeyTipScope", typeof(bool), typeof(KeyTipService), new FrameworkPropertyMetadata(false));

        public static readonly DependencyProperty KeyTipStyleProperty =
            DependencyProperty.RegisterAttached("KeyTipStyle", typeof(Style), typeof(KeyTipService), new FrameworkPropertyMetadata(null));

        public static readonly RoutedEvent ActivatingKeyTipEvent =
            EventManager.RegisterRoutedEvent("ActivatingKeyTip", RoutingStrategy.Bubble, typeof(ActivatingKeyTipEventHandler), typeof(KeyTipService));

        public static readonly RoutedEvent PreviewKeyTipAccessedEvent =
            EventManager.RegisterRoutedEvent("PreviewKeyTipAccessed", RoutingStrategy.Tunnel, typeof(KeyTipAccessedEventHandler), typeof(KeyTipService));

        public static readonly RoutedEvent KeyTipAccessedEvent =
            EventManager.RegisterRoutedEvent("KeyTipAccessed", RoutingStrategy.Bubble, typeof(KeyTipAccessedEventHandler), typeof(KeyTipService));

        public static string GetKeyTip(DependencyObject element)
        {
            ArgumentNullException.ThrowIfNull(element);
            return (string)element.GetValue(KeyTipProperty);
        }

        public static void SetKeyTip(DependencyObject element, string value)
        {
            ArgumentNullException.ThrowIfNull(element);
            element.SetValue(KeyTipProperty, value);
        }

        public static bool GetIsKeyTipScope(DependencyObject element)
        {
            ArgumentNullException.ThrowIfNull(element);
            return (bool)element.GetValue(IsKeyTipScopeProperty);
        }

        public static void SetIsKeyTipScope(DependencyObject element, bool value)
        {
            ArgumentNullException.ThrowIfNull(element);
            element.SetValue(IsKeyTipScopeProperty, value);
        }

        public static Style GetKeyTipStyle(DependencyObject element)
        {
            ArgumentNullException.ThrowIfNull(element);
            return (Style)element.GetValue(KeyTipStyleProperty);
        }

        public static void SetKeyTipStyle(DependencyObject element, Style value)
        {
            ArgumentNullException.ThrowIfNull(element);
            element.SetValue(KeyTipStyleProperty, value);
        }

        public static void AddActivatingKeyTipHandler(DependencyObject element, ActivatingKeyTipEventHandler handler)
        {
            if (element is UIElement uie) uie.AddHandler(ActivatingKeyTipEvent, handler);
        }

        public static void RemoveActivatingKeyTipHandler(DependencyObject element, ActivatingKeyTipEventHandler handler)
        {
            if (element is UIElement uie) uie.RemoveHandler(ActivatingKeyTipEvent, handler);
        }

        public static void AddPreviewKeyTipAccessedHandler(DependencyObject element, KeyTipAccessedEventHandler handler)
        {
            if (element is UIElement uie) uie.AddHandler(PreviewKeyTipAccessedEvent, handler);
        }

        public static void RemovePreviewKeyTipAccessedHandler(DependencyObject element, KeyTipAccessedEventHandler handler)
        {
            if (element is UIElement uie) uie.RemoveHandler(PreviewKeyTipAccessedEvent, handler);
        }

        public static void AddKeyTipAccessedHandler(DependencyObject element, KeyTipAccessedEventHandler handler)
        {
            if (element is UIElement uie) uie.AddHandler(KeyTipAccessedEvent, handler);
        }

        public static void RemoveKeyTipAccessedHandler(DependencyObject element, KeyTipAccessedEventHandler handler)
        {
            if (element is UIElement uie) uie.RemoveHandler(KeyTipAccessedEvent, handler);
        }

        public static void DismissKeyTips()
        {
            Dismissed?.Invoke(null, EventArgs.Empty);
        }

        internal static event EventHandler Dismissed;
    }
}

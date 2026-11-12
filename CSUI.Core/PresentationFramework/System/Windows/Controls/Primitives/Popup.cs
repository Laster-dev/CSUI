// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Threading;
using MS.Internal.Controls;
using MS.Internal.KnownBoxes;
using MS.Win32;

namespace System.Windows.Controls.Primitives
{
    public enum PlacementMode
    {
        Absolute = 0,
        Relative = 1,
        Bottom = 2,
        Center = 3,
        Right = 4,
        AbsolutePoint = 5,
        RelativePoint = 6,
        Mouse = 7,
        MousePoint = 8,
        Left = 9,
        Top = 10,
        Custom = 11
    }

    public enum PopupPrimaryAxis
    {
        None = 0,
        Horizontal = 1,
        Vertical = 2
    }

    public struct CustomPopupPlacement : IEquatable<CustomPopupPlacement>
    {
        private Point _point;
        private PopupPrimaryAxis _primaryAxis;

        public CustomPopupPlacement(Point point, PopupPrimaryAxis primaryAxis)
        {
            _point = point;
            _primaryAxis = primaryAxis;
        }

        public Point Point
        {
            get => _point;
            set => _point = value;
        }

        public PopupPrimaryAxis PrimaryAxis
        {
            get => _primaryAxis;
            set => _primaryAxis = value;
        }

        public bool Equals(CustomPopupPlacement other) => _point == other._point && _primaryAxis == other._primaryAxis;
        public override bool Equals(object obj) => obj is CustomPopupPlacement other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(_point, _primaryAxis);
        public static bool operator ==(CustomPopupPlacement left, CustomPopupPlacement right) => left.Equals(right);
        public static bool operator !=(CustomPopupPlacement left, CustomPopupPlacement right) => !left.Equals(right);
    }

    public delegate CustomPopupPlacement[] CustomPopupPlacementCallback(Size popupSize, Size targetSize, Point offset);

    [ContentProperty(nameof(Child))]
    [DefaultEvent(nameof(Opened))]
    [DefaultProperty(nameof(Child))]
    public class Popup : FrameworkElement, IAddChild
    {
        private HwndSource _popupHwndSource;
        private PopupRoot _popupRoot;

        static Popup()
        {
            ControlFoundationHooks.IsPopupHook = static d => d is Popup;
            ControlFoundationHooks.GetPopupChildHook = static d => (d as Popup)?.Child;
            ControlFoundationHooks.GetPopupPlacementTargetHook = static d => (d as Popup)?.PlacementTarget as FrameworkElement;
            ControlFoundationHooks.CoercePopupHasDropShadowHook = static fe => fe?.CoerceValue(HasDropShadowProperty);
        }

        public Popup()
        {
        }

        public static readonly DependencyProperty ChildProperty =
            DependencyProperty.Register(
                nameof(Child),
                typeof(UIElement),
                typeof(Popup),
                new FrameworkPropertyMetadata(null, OnChildChanged));

        public UIElement Child
        {
            get => (UIElement)GetValue(ChildProperty);
            set => SetValue(ChildProperty, value);
        }

        private static void OnChildChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var popup = (Popup)d;
            if (e.OldValue is UIElement oldChild)
            {
                if (popup._popupRoot != null && ReferenceEquals(popup._popupRoot.Child, oldChild))
                {
                    popup._popupRoot.Child = null;
                }
                popup.RemoveLogicalChild(oldChild);
            }

            if (e.NewValue is UIElement newChild)
            {
                popup.AddLogicalChild(newChild);
                if (popup._popupRoot != null)
                {
                    popup._popupRoot.Child = newChild;
                    popup.UpdatePopupTransformAndPosition();
                }
            }
        }

        public static readonly DependencyProperty IsOpenProperty =
            DependencyProperty.Register(
                nameof(IsOpen),
                typeof(bool),
                typeof(Popup),
                new FrameworkPropertyMetadata(
                    BooleanBoxes.FalseBox,
                    FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                    OnIsOpenChanged));

        [Category("Appearance")]
        public bool IsOpen
        {
            get => (bool)GetValue(IsOpenProperty);
            set => SetValue(IsOpenProperty, BooleanBoxes.Box(value));
        }

        private static void OnIsOpenChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var popup = (Popup)d;
            if ((bool)e.NewValue)
            {
                popup.OpenPopupWindow();
            }
            else
            {
                popup.ClosePopupWindow();
            }
        }

        public static readonly DependencyProperty PlacementProperty =
            DependencyProperty.Register(
                nameof(Placement),
                typeof(PlacementMode),
                typeof(Popup),
                new FrameworkPropertyMetadata(PlacementMode.Bottom, OnPlacementChanged));

        public PlacementMode Placement
        {
            get => (PlacementMode)GetValue(PlacementProperty);
            set => SetValue(PlacementProperty, value);
        }

        public static readonly DependencyProperty PlacementTargetProperty =
            DependencyProperty.Register(
                nameof(PlacementTarget),
                typeof(UIElement),
                typeof(Popup),
                new FrameworkPropertyMetadata(null, OnPlacementChanged));

        public UIElement PlacementTarget
        {
            get => (UIElement)GetValue(PlacementTargetProperty);
            set => SetValue(PlacementTargetProperty, value);
        }

        public static readonly DependencyProperty PlacementRectangleProperty =
            DependencyProperty.Register(
                nameof(PlacementRectangle),
                typeof(Rect),
                typeof(Popup),
                new FrameworkPropertyMetadata(Rect.Empty, OnPlacementChanged));

        public Rect PlacementRectangle
        {
            get => (Rect)GetValue(PlacementRectangleProperty);
            set => SetValue(PlacementRectangleProperty, value);
        }

        public static readonly DependencyProperty HorizontalOffsetProperty =
            DependencyProperty.Register(
                nameof(HorizontalOffset),
                typeof(double),
                typeof(Popup),
                new FrameworkPropertyMetadata(0.0, OnPlacementChanged));

        public double HorizontalOffset
        {
            get => (double)GetValue(HorizontalOffsetProperty);
            set => SetValue(HorizontalOffsetProperty, value);
        }

        public static readonly DependencyProperty VerticalOffsetProperty =
            DependencyProperty.Register(
                nameof(VerticalOffset),
                typeof(double),
                typeof(Popup),
                new FrameworkPropertyMetadata(0.0, OnPlacementChanged));

        public double VerticalOffset
        {
            get => (double)GetValue(VerticalOffsetProperty);
            set => SetValue(VerticalOffsetProperty, value);
        }

        private static void OnPlacementChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var popup = (Popup)d;
            if (popup.IsOpen)
            {
                popup.UpdatePopupTransformAndPosition();
            }
        }

        public static readonly DependencyProperty StaysOpenProperty =
            DependencyProperty.Register(
                nameof(StaysOpen),
                typeof(bool),
                typeof(Popup),
                new FrameworkPropertyMetadata(BooleanBoxes.TrueBox));

        public bool StaysOpen
        {
            get => (bool)GetValue(StaysOpenProperty);
            set => SetValue(StaysOpenProperty, BooleanBoxes.Box(value));
        }

        public static readonly DependencyProperty AllowsTransparencyProperty =
            DependencyProperty.Register(
                nameof(AllowsTransparency),
                typeof(bool),
                typeof(Popup),
                new FrameworkPropertyMetadata(BooleanBoxes.FalseBox));

        public bool AllowsTransparency
        {
            get => (bool)GetValue(AllowsTransparencyProperty);
            set => SetValue(AllowsTransparencyProperty, BooleanBoxes.Box(value));
        }

        public static readonly DependencyProperty PopupAnimationProperty =
            DependencyProperty.Register(
                nameof(PopupAnimation),
                typeof(PopupAnimation),
                typeof(Popup),
                new FrameworkPropertyMetadata(PopupAnimation.None));

        public PopupAnimation PopupAnimation
        {
            get => (PopupAnimation)GetValue(PopupAnimationProperty);
            set => SetValue(PopupAnimationProperty, value);
        }

        private static readonly DependencyPropertyKey HasDropShadowPropertyKey =
            DependencyProperty.RegisterReadOnly(
                nameof(HasDropShadow),
                typeof(bool),
                typeof(Popup),
                new FrameworkPropertyMetadata(BooleanBoxes.FalseBox));

        public static readonly DependencyProperty HasDropShadowProperty =
            HasDropShadowPropertyKey.DependencyProperty;

        public bool HasDropShadow => (bool)GetValue(HasDropShadowProperty);

        public static readonly DependencyProperty CustomPopupPlacementCallbackProperty =
            DependencyProperty.Register(
                nameof(CustomPopupPlacementCallback),
                typeof(CustomPopupPlacementCallback),
                typeof(Popup),
                new FrameworkPropertyMetadata(null));

        public CustomPopupPlacementCallback CustomPopupPlacementCallback
        {
            get => (CustomPopupPlacementCallback)GetValue(CustomPopupPlacementCallbackProperty);
            set => SetValue(CustomPopupPlacementCallbackProperty, value);
        }

        public event EventHandler Opened;
        public event EventHandler Closed;
        internal event EventHandler PopupCouldClose;

        internal static readonly TimeSpan AnimationDelayTime = TimeSpan.FromMilliseconds(150);

        internal bool DropOpposite { get; set; }

        private static void BindPopupProperty(Popup popup, DependencyProperty targetProperty, DependencyObject source, DependencyProperty sourceProperty)
        {
            popup.SetBinding(targetProperty, new Binding
            {
                Path = new PropertyPath(sourceProperty),
                Mode = BindingMode.OneWay,
                Source = source
            });
        }

        public static void CreateRootPopup(Popup popup, UIElement child)
        {
            CreateRootPopupInternal(popup, child, false);
        }

        internal static void CreateRootPopupInternal(Popup popup, UIElement child, bool bindTreatMousePlacementAsBottomProperty)
        {
            ArgumentNullException.ThrowIfNull(popup);
            ArgumentNullException.ThrowIfNull(child);

            if (LogicalTreeHelper.GetParent(child) != null || VisualTreeHelper.GetParent(child) != null)
            {
                throw new InvalidOperationException("Child element already has a logical or visual parent.");
            }

            if (child is global::System.Windows.Controls.ContextMenu)
            {
                BindPopupProperty(popup, PlacementTargetProperty, child, global::System.Windows.Controls.ContextMenu.PlacementTargetProperty);
                popup.Child = child;
                BindPopupProperty(popup, VerticalOffsetProperty, child, global::System.Windows.Controls.ContextMenu.VerticalOffsetProperty);
                BindPopupProperty(popup, HorizontalOffsetProperty, child, global::System.Windows.Controls.ContextMenu.HorizontalOffsetProperty);
                BindPopupProperty(popup, PlacementRectangleProperty, child, global::System.Windows.Controls.ContextMenu.PlacementRectangleProperty);
                BindPopupProperty(popup, PlacementProperty, child, global::System.Windows.Controls.ContextMenu.PlacementProperty);
                BindPopupProperty(popup, StaysOpenProperty, child, global::System.Windows.Controls.ContextMenu.StaysOpenProperty);
                BindPopupProperty(popup, CustomPopupPlacementCallbackProperty, child, global::System.Windows.Controls.ContextMenu.CustomPopupPlacementCallbackProperty);
                BindPopupProperty(popup, IsOpenProperty, child, global::System.Windows.Controls.ContextMenu.IsOpenProperty);
            }
            else if (child is global::System.Windows.Controls.ToolTip)
            {
                BindPopupProperty(popup, PlacementTargetProperty, child, global::System.Windows.Controls.ToolTip.PlacementTargetProperty);
                popup.Child = child;
                BindPopupProperty(popup, VerticalOffsetProperty, child, global::System.Windows.Controls.ToolTip.VerticalOffsetProperty);
                BindPopupProperty(popup, HorizontalOffsetProperty, child, global::System.Windows.Controls.ToolTip.HorizontalOffsetProperty);
                BindPopupProperty(popup, PlacementRectangleProperty, child, global::System.Windows.Controls.ToolTip.PlacementRectangleProperty);
                BindPopupProperty(popup, PlacementProperty, child, global::System.Windows.Controls.ToolTip.PlacementProperty);
                BindPopupProperty(popup, StaysOpenProperty, child, global::System.Windows.Controls.ToolTip.StaysOpenProperty);
                BindPopupProperty(popup, CustomPopupPlacementCallbackProperty, child, global::System.Windows.Controls.ToolTip.CustomPopupPlacementCallbackProperty);
                BindPopupProperty(popup, IsOpenProperty, child, global::System.Windows.Controls.ToolTip.IsOpenProperty);
            }
            else
            {
                popup.Child = child;
            }
        }

        internal static bool IsRootedInPopup(Popup parentPopup, UIElement element)
        {
            object logicalParent = LogicalTreeHelper.GetParent(element);
            if (logicalParent == null && VisualTreeHelper.GetParent(element) != null)
            {
                return false;
            }

            return logicalParent == parentPopup;
        }

        internal HwndSource PopupHwndSource => _popupHwndSource;

        protected internal override IEnumerator LogicalChildren
        {
            get
            {
                UIElement child = Child;
                return child == null ? EmptyEnumerator.Instance : new SingleChildEnumerator(child);
            }
        }

        void IAddChild.AddChild(object value)
        {
            ArgumentNullException.ThrowIfNull(value);
            if (value is not UIElement uie)
            {
                throw new ArgumentException($"Popup Child must be a UIElement, got '{value.GetType().Name}'.", nameof(value));
            }
            if (Child != null)
            {
                throw new InvalidOperationException("Popup can only have a single Child.");
            }
            Child = uie;
        }

        void IAddChild.AddText(string text)
        {
            if (!string.IsNullOrWhiteSpace(text))
            {
                throw new ArgumentException("Popup does not support raw text content directly.", nameof(text));
            }
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            return new Size(0.0, 0.0);
        }

        private void OpenPopupWindow()
        {
            if (_popupHwndSource != null)
            {
                return;
            }

            UIElement child = Child;
            _popupRoot = new PopupRoot();
            if (child != null)
            {
                _popupRoot.Child = child;
            }

            Size desired = MeasurePopupChild();
            Point screenPos = CalculateScreenPosition(desired);

            int widthPx = Math.Max(1, (int)Math.Ceiling(desired.Width));
            int heightPx = Math.Max(1, (int)Math.Ceiling(desired.Height));
            int xPx = (int)Math.Round(screenPos.X);
            int yPx = (int)Math.Round(screenPos.Y);

            IntPtr ownerHwnd = IntPtr.Zero;
            UIElement target = PlacementTarget ?? (Parent as UIElement);
            if (target != null && PresentationSource.FromVisual(target) is HwndSource targetSource)
            {
                ownerHwnd = targetSource.Handle;
            }

            var parameters = new HwndSourceParameters("CSUI_Popup", widthPx, heightPx)
            {
                PositionX = xPx,
                PositionY = yPx,
                ParentWindow = ownerHwnd,
                UsesPerPixelOpacity = AllowsTransparency,
                WindowStyle = unchecked((int)(NativeMethods.WS_POPUP | NativeMethods.WS_CLIPSIBLINGS | NativeMethods.WS_VISIBLE)),
                ExtendedWindowStyle = NativeMethods.WS_EX_TOPMOST | NativeMethods.WS_EX_TOOLWINDOW | NativeMethods.WS_EX_NOACTIVATE
            };

            _popupHwndSource = new HwndSource(parameters)
            {
                RootVisual = _popupRoot
            };

            _popupRoot.Measure(desired);
            _popupRoot.Arrange(new Rect(0.0, 0.0, desired.Width, desired.Height));
            _popupHwndSource.CompositionTarget?.OnRenderFrame(false);

            OnOpened(EventArgs.Empty);
        }

        private void ClosePopupWindow()
        {
            if (_popupHwndSource == null)
            {
                return;
            }

            HwndSource src = _popupHwndSource;
            _popupHwndSource = null;

            if (_popupRoot != null)
            {
                _popupRoot.Child = null;
                _popupRoot = null;
            }

            src.RootVisual = null;
            src.Dispose();

            OnClosed(EventArgs.Empty);
        }

        private void UpdatePopupTransformAndPosition()
        {
            if (_popupHwndSource == null || _popupRoot == null)
            {
                return;
            }

            Size desired = MeasurePopupChild();
            Point screenPos = CalculateScreenPosition(desired);

            int widthPx = Math.Max(1, (int)Math.Ceiling(desired.Width));
            int heightPx = Math.Max(1, (int)Math.Ceiling(desired.Height));
            int xPx = (int)Math.Round(screenPos.X);
            int yPx = (int)Math.Round(screenPos.Y);

            UnsafeNativeMethods.SetWindowPos(
                new HandleRef(this, _popupHwndSource.Handle),
                new HandleRef(null, IntPtr.Zero),
                xPx,
                yPx,
                widthPx,
                heightPx,
                NativeMethods.SWP_NOZORDER | NativeMethods.SWP_NOACTIVATE);

            _popupRoot.Measure(desired);
            _popupRoot.Arrange(new Rect(0.0, 0.0, desired.Width, desired.Height));
            _popupHwndSource.CompositionTarget?.OnRenderFrame(false);
        }

        private Size MeasurePopupChild()
        {
            double explicitW = Width;
            double explicitH = Height;
            Size constraint = new Size(
                !double.IsNaN(explicitW) && explicitW > 0 ? explicitW : double.PositiveInfinity,
                !double.IsNaN(explicitH) && explicitH > 0 ? explicitH : double.PositiveInfinity);

            if (_popupRoot != null)
            {
                _popupRoot.Measure(constraint);
                Size desired = _popupRoot.DesiredSize;
                return new Size(
                    !double.IsNaN(explicitW) && explicitW > 0 ? explicitW : Math.Max(1.0, desired.Width),
                    !double.IsNaN(explicitH) && explicitH > 0 ? explicitH : Math.Max(1.0, desired.Height));
            }

            return new Size(
                !double.IsNaN(explicitW) && explicitW > 0 ? explicitW : 100.0,
                !double.IsNaN(explicitH) && explicitH > 0 ? explicitH : 40.0);
        }

        private Point CalculateScreenPosition(Size popupSize)
        {
            double dx = HorizontalOffset;
            double dy = VerticalOffset;

            UIElement target = PlacementTarget ?? (Parent as UIElement);
            Point targetOrigin = new Point(0.0, 0.0);
            Size targetSize = new Size(0.0, 0.0);

            if (target != null)
            {
                targetSize = target.RenderSize;
                if (PresentationSource.FromVisual(target) != null)
                {
                    try
                    {
                        targetOrigin = target.PointToScreen(new Point(0.0, 0.0));
                    }
                    catch
                    {
                        targetOrigin = new Point(0.0, 0.0);
                    }
                }
            }

            return Placement switch
            {
                PlacementMode.Bottom => new Point(targetOrigin.X + dx, targetOrigin.Y + targetSize.Height + dy),
                PlacementMode.Top => new Point(targetOrigin.X + dx, targetOrigin.Y - popupSize.Height + dy),
                PlacementMode.Right => new Point(targetOrigin.X + targetSize.Width + dx, targetOrigin.Y + dy),
                PlacementMode.Left => new Point(targetOrigin.X - popupSize.Width + dx, targetOrigin.Y + dy),
                PlacementMode.Center => new Point(
                    targetOrigin.X + (targetSize.Width - popupSize.Width) * 0.5 + dx,
                    targetOrigin.Y + (targetSize.Height - popupSize.Height) * 0.5 + dy),
                PlacementMode.Relative or PlacementMode.RelativePoint => new Point(targetOrigin.X + dx, targetOrigin.Y + dy),
                PlacementMode.Custom when CustomPopupPlacementCallback != null =>
                    ResolveCustomPlacement(popupSize, targetOrigin, targetSize, new Point(dx, dy)),
                _ => new Point(dx, dy)
            };
        }

        private Point ResolveCustomPlacement(Size popupSize, Point targetOrigin, Size targetSize, Point offset)
        {
            CustomPopupPlacement[] placements = CustomPopupPlacementCallback(popupSize, targetSize, offset);
            if (placements != null && placements.Length > 0)
            {
                return new Point(
                    targetOrigin.X + placements[0].Point.X + offset.X,
                    targetOrigin.Y + placements[0].Point.Y + offset.Y);
            }
            return new Point(targetOrigin.X + offset.X, targetOrigin.Y + offset.Y);
        }

        protected virtual void OnOpened(EventArgs e)
        {
            Opened?.Invoke(this, e);
        }

        protected virtual void OnClosed(EventArgs e)
        {
            Closed?.Invoke(this, e);
        }

        internal void OnPopupCouldClose()
        {
            PopupCouldClose?.Invoke(this, EventArgs.Empty);
        }
    }

    internal sealed class PopupRoot : Decorator
    {
        private readonly NonLogicalAdornerDecorator _adornerDecorator = new NonLogicalAdornerDecorator();

        public PopupRoot()
        {
            base.Child = _adornerDecorator;
        }

        public override UIElement Child
        {
            get => _adornerDecorator.Child;
            set => _adornerDecorator.Child = value;
        }

        protected override Automation.Peers.AutomationPeer OnCreateAutomationPeer() =>
            new Automation.Peers.PopupRootAutomationPeer(this);
    }
}

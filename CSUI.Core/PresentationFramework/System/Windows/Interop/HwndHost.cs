// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows.Automation.Peers;
using System.Windows.Input;
using System.Windows.Media;
using MS.Internal.Controls;
using MS.Internal.Interop;

namespace System.Windows.Interop
{
    public abstract class HwndHost : FrameworkElement, IDisposable, IWin32Window, IKeyboardInputSink
    {
        private HandleRef _hwnd;
        private IntPtr _parentHwnd;
        private List<HwndSourceHook> _hooks;
        private IKeyboardInputSite _keyboardInputSite;
        private bool _disposed;
        private bool _built;
        private bool _layoutHooked;
        private Rect _lastBoundingBox;

        public static readonly RoutedEvent DpiChangedEvent = Window.DpiChangedEvent.AddOwner(typeof(HwndHost));

        static HwndHost()
        {
            ControlFoundationHooks.IsHwndHostHook ??= static o => o is HwndHost;
            FocusableProperty.OverrideMetadata(typeof(HwndHost), new FrameworkPropertyMetadata(true));
        }

        protected HwndHost()
        {
            Loaded += OnHostLoaded;
            Unloaded += OnHostUnloaded;
            IsVisibleChanged += OnHostIsVisibleChanged;
        }

        ~HwndHost()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public IntPtr Handle => _hwnd.Handle;

        public event HwndSourceHook MessageHook
        {
            add => (_hooks ??= new List<HwndSourceHook>()).Add(value);
            remove => _hooks?.Remove(value);
        }

        public event DpiChangedEventHandler DpiChanged
        {
            add => AddHandler(DpiChangedEvent, value);
            remove => RemoveHandler(DpiChangedEvent, value);
        }

        public void UpdateWindowPos()
        {
            if (_hwnd.Handle != IntPtr.Zero)
            {
                Rect rc = ComputeBoundingBox(RenderSize);
                if (rc != _lastBoundingBox)
                {
                    _lastBoundingBox = rc;
                    OnWindowPositionChanged(rc);
                }
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            if (_layoutHooked)
            {
                LayoutUpdated -= OnHostLayoutUpdated;
                _layoutHooked = false;
            }

            if (disposing && _hwnd.Handle != IntPtr.Zero)
            {
                DestroyWindowCore(_hwnd);
            }

            _hwnd = new HandleRef(null, IntPtr.Zero);
            _disposed = true;
        }

        protected virtual void OnWindowPositionChanged(Rect rcBoundingBox)
        {
            if (_hwnd.Handle != IntPtr.Zero && _hwnd.Handle != _parentHwnd)
            {
                Standard.NativeMethods.SetWindowPos(
                    _hwnd.Handle,
                    IntPtr.Zero,
                    (int)Math.Round(rcBoundingBox.X),
                    (int)Math.Round(rcBoundingBox.Y),
                    Math.Max(0, (int)Math.Round(rcBoundingBox.Width)),
                    Math.Max(0, (int)Math.Round(rcBoundingBox.Height)),
                    Standard.SWP.NOZORDER | Standard.SWP.NOACTIVATE | Standard.SWP.ASYNCWINDOWPOS);
            }
        }

        protected override Size MeasureOverride(Size constraint)
        {
            EnsureBuilt();
            double w = double.IsInfinity(constraint.Width) ? 0.0 : constraint.Width;
            double h = double.IsInfinity(constraint.Height) ? 0.0 : constraint.Height;
            return new Size(w, h);
        }

        protected override Size ArrangeOverride(Size arrangeBounds)
        {
            EnsureBuilt();
            if (_hwnd.Handle != IntPtr.Zero)
            {
                Rect rc = ComputeBoundingBox(arrangeBounds);
                _lastBoundingBox = rc;
                OnWindowPositionChanged(rc);
            }
            return arrangeBounds;
        }

        protected override AutomationPeer OnCreateAutomationPeer() => new HwndHostAutomationPeer(this);

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
        }

        protected override void OnKeyUp(KeyEventArgs e)
        {
            base.OnKeyUp(e);
        }

        protected abstract HandleRef BuildWindowCore(HandleRef hwndParent);
        protected abstract void DestroyWindowCore(HandleRef hwnd);

        protected virtual IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (_hooks != null)
            {
                for (int i = 0; i < _hooks.Count; i++)
                {
                    IntPtr result = _hooks[i](hwnd, msg, wParam, lParam, ref handled);
                    if (handled)
                    {
                        return result;
                    }
                }
            }

            return IntPtr.Zero;
        }

        protected override void OnDpiChanged(DpiScale oldDpi, DpiScale newDpi)
        {
            RaiseEvent(new DpiChangedEventArgs(oldDpi, newDpi, DpiChangedEvent, this));
        }

        private void EnsureBuilt()
        {
            if (!_built && !_disposed)
            {
                _built = true;
                IntPtr parent = IntPtr.Zero;
                if (PresentationSource.FromVisual(this) is HwndSource hs)
                {
                    parent = hs.Handle;
                }

                _parentHwnd = parent;
                _hwnd = BuildWindowCore(new HandleRef(this, parent));
                if (!_layoutHooked)
                {
                    LayoutUpdated += OnHostLayoutUpdated;
                    _layoutHooked = true;
                }
            }
        }

        private void OnHostLoaded(object sender, RoutedEventArgs e)
        {
            EnsureBuilt();
            UpdateWindowPos();
        }

        private void OnHostUnloaded(object sender, RoutedEventArgs e)
        {
            if (_layoutHooked)
            {
                LayoutUpdated -= OnHostLayoutUpdated;
                _layoutHooked = false;
            }
            if (_hwnd.Handle != IntPtr.Zero && _hwnd.Handle != _parentHwnd)
            {
                Standard.NativeMethods.ShowWindow(_hwnd.Handle, Standard.SW.HIDE);
            }
        }

        private void OnHostIsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (_hwnd.Handle != IntPtr.Zero)
            {
                if ((bool)e.NewValue)
                {
                    if (_hwnd.Handle != _parentHwnd)
                    {
                        Standard.NativeMethods.ShowWindow(_hwnd.Handle, Standard.SW.SHOWNA);
                    }
                    UpdateWindowPos();
                }
                else if (_hwnd.Handle != _parentHwnd)
                {
                    Standard.NativeMethods.ShowWindow(_hwnd.Handle, Standard.SW.HIDE);
                }
            }
        }

        private void OnHostLayoutUpdated(object sender, EventArgs e)
        {
            UpdateWindowPos();
        }

        private Rect ComputeBoundingBox(Size size)
        {
            Rect local = new Rect(0, 0, Math.Max(0.0, size.Width), Math.Max(0.0, size.Height));
            if (PresentationSource.FromVisual(this) is HwndSource hs && hs.RootVisual != null)
            {
                try
                {
                    GeneralTransform gt = TransformToAncestor(hs.RootVisual);
                    Rect clientDip = gt.TransformBounds(local);
                    DpiScale dpi = VisualTreeHelper.GetDpi(this);
                    return new Rect(
                        clientDip.X * dpi.DpiScaleX,
                        clientDip.Y * dpi.DpiScaleY,
                        clientDip.Width * dpi.DpiScaleX,
                        clientDip.Height * dpi.DpiScaleY);
                }
                catch
                {
                }
            }
            return local;
        }

        protected virtual bool TranslateAcceleratorCore(ref MSG msg, ModifierKeys modifiers) => false;
        protected virtual bool TabIntoCore(TraversalRequest request) => false;
        protected virtual bool OnMnemonicCore(ref MSG msg, ModifierKeys modifiers) => false;
        protected virtual bool TranslateCharCore(ref MSG msg, ModifierKeys modifiers) => false;
        protected virtual bool HasFocusWithinCore() => IsKeyboardFocusWithin;

        IKeyboardInputSite IKeyboardInputSink.RegisterKeyboardInputSink(IKeyboardInputSink sink) =>
            throw new NotSupportedException();

        bool IKeyboardInputSink.TranslateAccelerator(ref MSG msg, ModifierKeys modifiers) =>
            TranslateAcceleratorCore(ref msg, modifiers);

        bool IKeyboardInputSink.TabInto(TraversalRequest request) =>
            TabIntoCore(request);

        IKeyboardInputSite IKeyboardInputSink.KeyboardInputSite
        {
            get => _keyboardInputSite;
            set => _keyboardInputSite = value;
        }

        bool IKeyboardInputSink.OnMnemonic(ref MSG msg, ModifierKeys modifiers) =>
            OnMnemonicCore(ref msg, modifiers);

        bool IKeyboardInputSink.TranslateChar(ref MSG msg, ModifierKeys modifiers) =>
            TranslateCharCore(ref msg, modifiers);

        bool IKeyboardInputSink.HasFocusWithin() =>
            HasFocusWithinCore();
    }
}

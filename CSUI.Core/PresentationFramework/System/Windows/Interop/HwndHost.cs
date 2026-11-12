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
        private List<HwndSourceHook> _hooks;
        private IKeyboardInputSite _keyboardInputSite;
        private bool _disposed;
        private bool _built;

        public static readonly RoutedEvent DpiChangedEvent = Window.DpiChangedEvent.AddOwner(typeof(HwndHost));

        static HwndHost()
        {
            ControlFoundationHooks.IsHwndHostHook ??= static o => o is HwndHost;
            FocusableProperty.OverrideMetadata(typeof(HwndHost), new FrameworkPropertyMetadata(true));
        }

        protected HwndHost()
        {
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
                OnWindowPositionChanged(new Rect(new Point(0, 0), RenderSize));
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
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
            OnWindowPositionChanged(new Rect(0, 0, arrangeBounds.Width, arrangeBounds.Height));
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
                _hwnd = BuildWindowCore(new HandleRef(this, parent));
            }
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

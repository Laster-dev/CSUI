// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Threading;
using System.Diagnostics.CodeAnalysis;

#nullable disable
using SR = MS.Internal.PresentationFramework.SR;

namespace Standard
{
    internal sealed unsafe class MessageWindow : CriticalFinalizerObject
    {
        private static readonly Dictionary<IntPtr, MessageWindow> s_windowLookup = new Dictionary<IntPtr, MessageWindow>();

        private WndProc _wndProcCallback;
        private string _className;
        private bool _isDisposed;
        private Dispatcher _dispatcher;

        public IntPtr Handle
        {
            get;
            private set;
        }

        public MessageWindow(CS classStyle, WS style, WS_EX exStyle, Rect location, string name, WndProc callback)
        {
            _wndProcCallback = callback;
            _className = $"MessageWindowClass+{Guid.NewGuid()}";

            fixed (char* pClassName = _className)
            fixed (char* pMenuName = "")
            {
                var wc = new WNDCLASSEX
                {
                    cbSize = sizeof(WNDCLASSEX),
                    style = classStyle,
                    lpfnWndProc = (IntPtr)(delegate* unmanaged[Stdcall]<IntPtr, WM, IntPtr, IntPtr, IntPtr>)&_NativeWndProc,
                    hInstance = NativeMethods.GetModuleHandle(null),
                    hbrBackground = NativeMethods.GetStockObject(StockObject.NULL_BRUSH),
                    lpszMenuName = pMenuName,
                    lpszClassName = pClassName,
                };

                NativeMethods.RegisterClassEx(ref wc);
            }

            GCHandle gcHandle = default(GCHandle);
            try
            {
                gcHandle = GCHandle.Alloc(this);
                IntPtr pinnedThisPtr = (IntPtr)gcHandle;

                Handle = NativeMethods.CreateWindowEx(
                    exStyle,
                    _className,
                    name,
                    style,
                    (int)location.X,
                    (int)location.Y,
                    (int)location.Width,
                    (int)location.Height,
                    IntPtr.Zero,
                    IntPtr.Zero,
                    IntPtr.Zero,
                    pinnedThisPtr);
            }
            finally
            {
                gcHandle.Free();
            }

            _dispatcher = Dispatcher.CurrentDispatcher;
        }

        ~MessageWindow()
        {
            _Dispose(false, false);
        }

        public void Release()
        {
            _Dispose(true, false);
            GC.SuppressFinalize(this);
        }

        [SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "disposing")]
        private void _Dispose(bool disposing, bool isHwndBeingDestroyed)
        {
            if (_isDisposed)
            {
                return;
            }

            _isDisposed = true;

            IntPtr hwnd = Handle;
            string className = _className;

            if (isHwndBeingDestroyed)
            {
                _dispatcher.BeginInvoke(DispatcherPriority.Normal, (DispatcherOperationCallback)_DestroyWindowCallback, new object[] { IntPtr.Zero, className });
            }
            else if (Handle != IntPtr.Zero)
            {
                if (_dispatcher.CheckAccess())
                {
                    _DestroyWindow(hwnd, className);
                }
                else
                {
                    _dispatcher.BeginInvoke(DispatcherPriority.Normal, (DispatcherOperationCallback)_DestroyWindowCallback, new object[] { hwnd, className });
                }
            }

            s_windowLookup.Remove(hwnd);

            _className = null;
            Handle = IntPtr.Zero;
        }

        private object _DestroyWindowCallback(object arg)
        {
            object[] args = (object[])arg;
            _DestroyWindow((IntPtr)args[0], (string)args[1]);
            return null;
        }

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
        private static IntPtr _NativeWndProc(IntPtr hwnd, WM msg, IntPtr wParam, IntPtr lParam)
        {
            return _WndProc(hwnd, msg, wParam, lParam);
        }

        [SuppressMessage("Microsoft.Usage", "CA1816:CallGCSuppressFinalizeCorrectly")]
        private static IntPtr _WndProc(IntPtr hwnd, WM msg, IntPtr wParam, IntPtr lParam)
        {
            IntPtr ret = IntPtr.Zero;
            MessageWindow hwndWrapper = null;

            if (msg == WM.CREATE)
            {
                // First field of Win32 CREATESTRUCTW is LPVOID lpCreateParams at offset 0.
                IntPtr lpCreateParams = *(IntPtr*)lParam;
                GCHandle gcHandle = GCHandle.FromIntPtr(lpCreateParams);
                hwndWrapper = (MessageWindow)gcHandle.Target;
                s_windowLookup.Add(hwnd, hwndWrapper);
            }
            else
            {
                if (!s_windowLookup.TryGetValue(hwnd, out hwndWrapper))
                {
                    return NativeMethods.DefWindowProc(hwnd, msg, wParam, lParam);
                }
            }
            Assert.IsNotNull(hwndWrapper);

            WndProc callback = hwndWrapper._wndProcCallback;
            if (callback != null)
            {
                ret = callback(hwnd, msg, wParam, lParam);
            }
            else
            {
                ret = NativeMethods.DefWindowProc(hwnd, msg, wParam, lParam);
            }

            if (msg == WM.NCDESTROY)
            {
                hwndWrapper._Dispose(true, true);
                GC.SuppressFinalize(hwndWrapper);
            }

            return ret;
        }

        private static void _DestroyWindow(IntPtr hwnd, string className)
        {
            Utility.SafeDestroyWindow(ref hwnd);
            NativeMethods.UnregisterClass(className, NativeMethods.GetModuleHandle(null));
        }
    }
}

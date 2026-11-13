using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using Microsoft.Web.WebView2.Core;

namespace Microsoft.Web.WebView2.Wpf
{
    /// <summary>
    /// Source-compiled <see cref="HwndHost"/>-based WebView2 control built directly on CSUI.Core
    /// and <see cref="Microsoft.Web.WebView2.Core"/>.
    /// </summary>
    public class WebView2 : HwndHost, IDisposable
    {
        private IntPtr _hostHwnd;
        private CoreWebView2Controller? _controller;
        private CoreWebView2? _coreWebView2;
        private Task? _initTask;
        private bool _disposed;

        public static readonly DependencyProperty SourceProperty =
            DependencyProperty.Register(
                nameof(Source),
                typeof(Uri),
                typeof(WebView2),
                new PropertyMetadata(null, OnSourceChanged));

        public static readonly DependencyProperty DefaultBackgroundColorProperty =
            DependencyProperty.Register(
                nameof(DefaultBackgroundColor),
                typeof(Color),
                typeof(WebView2),
                new PropertyMetadata(Color.White, OnDefaultBackgroundColorChanged));

        public static readonly DependencyProperty ZoomFactorProperty =
            DependencyProperty.Register(
                nameof(ZoomFactor),
                typeof(double),
                typeof(WebView2),
                new PropertyMetadata(1.0, OnZoomFactorChanged));

        public Uri? Source
        {
            get => (Uri?)GetValue(SourceProperty);
            set => SetValue(SourceProperty, value);
        }

        public Color DefaultBackgroundColor
        {
            get => (Color)GetValue(DefaultBackgroundColorProperty);
            set => SetValue(DefaultBackgroundColorProperty, value);
        }

        public double ZoomFactor
        {
            get => (double)GetValue(ZoomFactorProperty);
            set => SetValue(ZoomFactorProperty, value);
        }

        public CoreWebView2? CoreWebView2 => _coreWebView2;

        public bool CanGoBack => _coreWebView2?.CanGoBack ?? false;
        public bool CanGoForward => _coreWebView2?.CanGoForward ?? false;

        public event EventHandler<CoreWebView2NavigationCompletedEventArgs>? NavigationCompleted;
        public event EventHandler<CoreWebView2NavigationStartingEventArgs>? NavigationStarting;
        public event EventHandler<CoreWebView2InitializationCompletedEventArgs>? CoreWebView2InitializationCompleted;
        public event EventHandler<CoreWebView2SourceChangedEventArgs>? SourceChanged;

        public Task EnsureCoreWebView2Async(CoreWebView2Environment? environment = null)
        {
            if (_coreWebView2 != null)
            {
                return Task.CompletedTask;
            }

            return _initTask ??= InitializeCoreWebView2Async(environment);
        }

        private async Task InitializeCoreWebView2Async(CoreWebView2Environment? environment)
        {
            try
            {
                if (_hostHwnd == IntPtr.Zero)
                {
                    return;
                }

                CoreWebView2Environment env = environment ?? await CoreWebView2Environment.CreateAsync();
                _controller = await env.CreateCoreWebView2ControllerAsync(_hostHwnd);
                _controller.DefaultBackgroundColor = DefaultBackgroundColor;
                _controller.ZoomFactor = ZoomFactor;
                UpdateControllerBounds(RenderSize);

                _coreWebView2 = _controller.CoreWebView2;
                _coreWebView2.NavigationStarting += (s, e) => NavigationStarting?.Invoke(this, e);
                _coreWebView2.NavigationCompleted += (s, e) => NavigationCompleted?.Invoke(this, e);
                _coreWebView2.SourceChanged += (s, e) => SourceChanged?.Invoke(this, e);

                CoreWebView2InitializationCompleted?.Invoke(
                    this,
                    new CoreWebView2InitializationCompletedEventArgs());

                if (Source is Uri uri)
                {
                    _coreWebView2.Navigate(uri.AbsoluteUri);
                }
            }
            catch (Exception ex)
            {
                _initTask = null;
                CoreWebView2InitializationCompleted?.Invoke(
                    this,
                    new CoreWebView2InitializationCompletedEventArgs(ex));
            }
        }

        public async Task<string> ExecuteScriptAsync(string javaScript)
        {
            await EnsureCoreWebView2Async();
            if (_coreWebView2 == null)
            {
                return "null";
            }

            return await _coreWebView2.ExecuteScriptAsync(javaScript);
        }

        public void NavigateToString(string htmlContent)
        {
            _coreWebView2?.NavigateToString(htmlContent);
        }

        public void Reload() => _coreWebView2?.Reload();
        public void GoBack() => _coreWebView2?.GoBack();
        public void GoForward() => _coreWebView2?.GoForward();
        public void Stop() => _coreWebView2?.Stop();

        private static void OnSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is WebView2 webView && e.NewValue is Uri uri)
            {
                if (webView._coreWebView2 != null)
                {
                    webView._coreWebView2.Navigate(uri.AbsoluteUri);
                }
                else if (webView._hostHwnd != IntPtr.Zero)
                {
                    _ = webView.EnsureCoreWebView2Async();
                }
            }
        }

        private static void OnDefaultBackgroundColorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is WebView2 webView && webView._controller != null && e.NewValue is Color color)
            {
                webView._controller.DefaultBackgroundColor = color;
            }
        }

        private static void OnZoomFactorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is WebView2 webView && webView._controller != null && e.NewValue is double zoom)
            {
                webView._controller.ZoomFactor = zoom;
            }
        }

        protected override HandleRef BuildWindowCore(HandleRef hwndParent)
        {
            _hostHwnd = hwndParent.Handle;
            if (Source != null)
            {
                _ = EnsureCoreWebView2Async();
            }

            return new HandleRef(this, _hostHwnd);
        }

        protected override void DestroyWindowCore(HandleRef hwnd)
        {
            DisposeCore();
        }

        protected override void OnWindowPositionChanged(Rect rcBoundingBox)
        {
            base.OnWindowPositionChanged(rcBoundingBox);
            UpdateControllerBounds(rcBoundingBox.Size);
        }

        private void UpdateControllerBounds(System.Windows.Size size)
        {
            if (_controller == null)
            {
                return;
            }

            int w = Math.Max(0, (int)Math.Round(size.Width));
            int h = Math.Max(0, (int)Math.Round(size.Height));
            _controller.Bounds = new Rectangle(0, 0, w, h);
        }

        private void DisposeCore()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _controller?.Close();
            _controller = null;
            _coreWebView2 = null;
            _hostHwnd = IntPtr.Zero;
        }

        public new void Dispose()
        {
            DisposeCore();
            base.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}

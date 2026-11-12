// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Automation.Peers;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Navigation;

namespace System.Windows.Controls
{
    /// <summary>
    /// Managed WebBrowser control inheriting HwndHost without legacy IE4 shdocvw.dll ActiveX COM or dynamic IDispatch script baggage.
    /// </summary>
    public sealed class WebBrowser : HwndHost
    {
        private Uri _source;
        private readonly List<Uri> _history = new List<Uri>();
        private int _historyIndex = -1;
        private string _htmlContent;
        private object _document;
        private object _objectForScripting;

        public WebBrowser()
        {
        }

        public Uri Source
        {
            get => _source;
            set
            {
                VerifyAccess();
                Navigate(value);
            }
        }

        public bool CanGoBack => _historyIndex > 0;
        public bool CanGoForward => _historyIndex >= 0 && _historyIndex < _history.Count - 1;
        public object Document => _document;

        public object ObjectForScripting
        {
            get => _objectForScripting;
            set
            {
                VerifyAccess();
                _objectForScripting = value;
            }
        }

        public event NavigatingCancelEventHandler Navigating;
        public event NavigatedEventHandler Navigated;
        public event LoadCompletedEventHandler LoadCompleted;

        public void Navigate(Uri source) => Navigate(source, null, null, null);

        public void Navigate(string source) => Navigate(source, null, null, null);

        public void Navigate(Uri source, string targetFrameName, byte[] postData, string additionalHeaders)
        {
            VerifyAccess();
            var cancelArgs = new NavigatingCancelEventArgs(
                source, null, null, null, NavigationMode.New, null, postData, this, true);
            Navigating?.Invoke(this, cancelArgs);
            if (cancelArgs.Cancel)
            {
                return;
            }

            _source = source;
            if (source != null)
            {
                if (_historyIndex < _history.Count - 1)
                {
                    _history.RemoveRange(_historyIndex + 1, _history.Count - 1 - _historyIndex);
                }
                _history.Add(source);
                _historyIndex = _history.Count - 1;
            }

            var navArgs = new NavigationEventArgs(source, _document, null, null, this, true);
            Navigated?.Invoke(this, navArgs);
            LoadCompleted?.Invoke(this, navArgs);
            InvalidateVisual();
        }

        public void Navigate(string source, string targetFrameName, byte[] postData, string additionalHeaders)
        {
            Uri uri = string.IsNullOrEmpty(source) ? null : new Uri(source, UriKind.RelativeOrAbsolute);
            Navigate(uri, targetFrameName, postData, additionalHeaders);
        }

        public void NavigateToStream(Stream stream)
        {
            ArgumentNullException.ThrowIfNull(stream);
            VerifyAccess();
            using var reader = new StreamReader(stream, leaveOpen: true);
            _htmlContent = reader.ReadToEnd();
            _document = _htmlContent;
            Navigate((Uri)null);
        }

        public void NavigateToString(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                throw new ArgumentNullException(nameof(text));
            }

            VerifyAccess();
            _htmlContent = text;
            _document = text;
            Navigate((Uri)null);
        }

        public void GoBack()
        {
            VerifyAccess();
            if (!CanGoBack)
            {
                throw new InvalidOperationException("Cannot navigate back.");
            }

            _historyIndex--;
            _source = _history[_historyIndex];
            var navArgs = new NavigationEventArgs(_source, _document, null, null, this, true);
            Navigated?.Invoke(this, navArgs);
            LoadCompleted?.Invoke(this, navArgs);
            InvalidateVisual();
        }

        public void GoForward()
        {
            VerifyAccess();
            if (!CanGoForward)
            {
                throw new InvalidOperationException("Cannot navigate forward.");
            }

            _historyIndex++;
            _source = _history[_historyIndex];
            var navArgs = new NavigationEventArgs(_source, _document, null, null, this, true);
            Navigated?.Invoke(this, navArgs);
            LoadCompleted?.Invoke(this, navArgs);
            InvalidateVisual();
        }

        public void Refresh() => Refresh(noCache: false);

        public void Refresh(bool noCache)
        {
            VerifyAccess();
            var navArgs = new NavigationEventArgs(_source, _document, null, null, this, true);
            Navigated?.Invoke(this, navArgs);
            LoadCompleted?.Invoke(this, navArgs);
        }

        public object InvokeScript(string scriptName) => InvokeScript(scriptName, null);

        public object InvokeScript(string scriptName, params object[] args)
        {
            VerifyAccess();
            if (string.IsNullOrEmpty(scriptName))
            {
                throw new ArgumentNullException(nameof(scriptName));
            }

            return null;
        }

        protected override HandleRef BuildWindowCore(HandleRef hwndParent) => new HandleRef(this, IntPtr.Zero);

        protected override void DestroyWindowCore(HandleRef hwnd)
        {
        }
    }
}

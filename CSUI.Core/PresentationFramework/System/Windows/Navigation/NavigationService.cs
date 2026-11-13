// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System;
using System.Collections;
using System.Windows.Controls;
using MS.Internal.Controls;

namespace System.Windows.Navigation
{
    internal interface INavigator
    {
        Uri Source { get; set; }
        Uri CurrentSource { get; }
        object Content { get; set; }
        bool CanGoBack { get; }
        bool CanGoForward { get; }
        void GoBack();
        void GoForward();
        bool Navigate(Uri source);
        bool Navigate(Uri source, object extraData);
        bool Navigate(object content);
        bool Navigate(object content, object extraData);
        void Refresh();
        void StopLoading();
    }

    public sealed class NavigationService : INavigator
    {
        internal static readonly DependencyProperty NavigationServiceProperty =
            DependencyProperty.RegisterAttached(
                "NavigationService",
                typeof(NavigationService),
                typeof(NavigationService),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.Inherits));

        static NavigationService()
        {
            ControlFoundationHooks.NavigationServicePropertyHook = NavigationServiceProperty;
        }

        private readonly INavigationHost _host;
        private readonly JournalEntryStack _backStack = new JournalEntryStack();
        private readonly JournalEntryStack _forwardStack = new JournalEntryStack();
        private Uri _source;
        private Uri _currentSource;
        private object _content;

        internal NavigationService(INavigationHost host)
        {
            _host = host;
        }

        public static NavigationService GetNavigationService(DependencyObject dependencyObject)
        {
            ArgumentNullException.ThrowIfNull(dependencyObject);
            return dependencyObject.GetValue(NavigationServiceProperty) as NavigationService;
        }

        public Uri Source
        {
            get => _source;
            set => Navigate(value);
        }

        public Uri CurrentSource => _currentSource;

        public object Content
        {
            get => _content;
            set => Navigate(value);
        }

        public bool CanGoBack => _backStack.Count > 0;
        public bool CanGoForward => _forwardStack.Count > 0;

        internal IEnumerable BackStack => _backStack;
        internal IEnumerable ForwardStack => _forwardStack;

        public event NavigatingCancelEventHandler Navigating;
        public event NavigatedEventHandler Navigated;
        public event NavigationProgressEventHandler NavigationProgress;
        public event NavigationFailedEventHandler NavigationFailed;
        public event NavigationStoppedEventHandler NavigationStopped;
        public event LoadCompletedEventHandler LoadCompleted;
        public event FragmentNavigationEventHandler FragmentNavigation;

        public bool Navigate(Uri source) => Navigate(source, null);

        public bool Navigate(Uri source, object navigationState)
            => NavigateCore(source, null, navigationState, NavigationMode.New);

        public bool Navigate(object root) => Navigate(root, null);

        public bool Navigate(object root, object navigationState)
            => NavigateCore(null, root, navigationState, NavigationMode.New);

        public void GoBack()
        {
            if (!CanGoBack)
            {
                throw new InvalidOperationException("Cannot navigate back.");
            }

            JournalEntry previous = _backStack.Pop();
            if (_content != null || _currentSource != null)
            {
                _forwardStack.Push(new JournalEntry(_currentSource, null, _content, null));
            }

            NavigateCore(previous.Source, previous.Content, null, NavigationMode.Back, recordHistory: false);
        }

        public void GoForward()
        {
            if (!CanGoForward)
            {
                throw new InvalidOperationException("Cannot navigate forward.");
            }

            JournalEntry next = _forwardStack.Pop();
            if (_content != null || _currentSource != null)
            {
                _backStack.Push(new JournalEntry(_currentSource, null, _content, null));
            }

            NavigateCore(next.Source, next.Content, null, NavigationMode.Forward, recordHistory: false);
        }

        public JournalEntry RemoveBackEntry()
        {
            JournalEntry entry = _backStack.Pop();
            _host?.OnJournalChanged();
            return entry;
        }

        public void AddBackEntry(CustomContentState state)
        {
            ArgumentNullException.ThrowIfNull(state);
            _backStack.Push(new JournalEntry(_currentSource, state.JournalEntryName, _content, state));
            _forwardStack.Clear();
            _host?.OnJournalChanged();
        }

        public void Refresh()
        {
            NavigateCore(_currentSource, _content, null, NavigationMode.Refresh, recordHistory: false);
        }

        private object EventSender => (object)_host ?? this;

        public void StopLoading()
        {
            NavigationStopped?.Invoke(EventSender, new NavigationEventArgs(_source, _content, null, null, _host, true));
        }

        internal void RaiseNavigationProgress(long bytesRead, long maxBytes)
        {
            NavigationProgress?.Invoke(EventSender, new NavigationProgressEventArgs(_source, bytesRead, maxBytes, _host));
        }

        internal void RaiseNavigationFailed(Exception ex)
        {
            NavigationFailed?.Invoke(EventSender, new NavigationFailedEventArgs(_source, null, _host, null, null, ex));
        }

        private bool NavigateCore(Uri source, object content, object extraData, NavigationMode mode, bool recordHistory = true)
        {
            var cancelArgs = new NavigatingCancelEventArgs(
                source, content, null, null, mode, null, extraData, _host, true);
            Navigating?.Invoke(EventSender, cancelArgs);
            if (cancelArgs.Cancel)
            {
                return false;
            }

            if (recordHistory && (_content != null || _currentSource != null))
            {
                _backStack.Push(new JournalEntry(_currentSource, null, _content, null));
                _forwardStack.Clear();
            }

            _source = source;
            _currentSource = source;
            _content = content;

            if (_content is DependencyObject contentDo)
            {
                contentDo.SetValue(NavigationServiceProperty, this);
            }

            _host?.OnNavigatedContent(_content, _source);
            _host?.OnJournalChanged();

            var navArgs = new NavigationEventArgs(_currentSource, _content, extraData, null, _host, true);
            Navigated?.Invoke(EventSender, navArgs);
            LoadCompleted?.Invoke(EventSender, navArgs);

            if (source != null && !string.IsNullOrEmpty(source.OriginalString) && source.OriginalString.Contains('#'))
            {
                int idx = source.OriginalString.IndexOf('#');
                string fragment = source.OriginalString.Substring(idx + 1);
                FragmentNavigation?.Invoke(EventSender, new FragmentNavigationEventArgs(fragment, _host));
            }

            return true;
        }

        internal void SetContentSilently(object content)
        {
            _content = content;
            if (content is DependencyObject d)
            {
                d.SetValue(NavigationServiceProperty, this);
            }
        }
    }

    internal interface INavigationHost
    {
        void OnNavigatedContent(object content, Uri source);
        void OnJournalChanged();
    }
}

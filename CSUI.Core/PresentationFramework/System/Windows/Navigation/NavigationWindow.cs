// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System;
using System.Collections;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Markup;
using MS.Internal.Controls;

namespace System.Windows.Navigation
{
    [ContentProperty]
    [TemplatePart(Name = "PART_NavWinCP", Type = typeof(ContentPresenter))]
    public class NavigationWindow : Window, INavigator, INavigationHost, IUriContext
    {
        public static readonly DependencyProperty SourceProperty = Frame.SourceProperty.AddOwner(typeof(NavigationWindow), new FrameworkPropertyMetadata(null, OnSourceChanged));
        public static readonly DependencyProperty ShowsNavigationUIProperty = DependencyProperty.Register(nameof(ShowsNavigationUI), typeof(bool), typeof(NavigationWindow), new FrameworkPropertyMetadata(true));
        public static readonly DependencyProperty SandboxExternalContentProperty = Frame.SandboxExternalContentProperty.AddOwner(typeof(NavigationWindow));

        private static readonly DependencyPropertyKey CanGoBackPropertyKey =
            DependencyProperty.RegisterReadOnly(nameof(CanGoBack), typeof(bool), typeof(NavigationWindow), new FrameworkPropertyMetadata(false));

        public static readonly DependencyProperty CanGoBackProperty = CanGoBackPropertyKey.DependencyProperty;

        private static readonly DependencyPropertyKey CanGoForwardPropertyKey =
            DependencyProperty.RegisterReadOnly(nameof(CanGoForward), typeof(bool), typeof(NavigationWindow), new FrameworkPropertyMetadata(false));

        public static readonly DependencyProperty CanGoForwardProperty = CanGoForwardPropertyKey.DependencyProperty;

        private static readonly DependencyPropertyKey BackStackPropertyKey =
            DependencyProperty.RegisterReadOnly(nameof(BackStack), typeof(IEnumerable), typeof(NavigationWindow), new FrameworkPropertyMetadata(null));

        public static readonly DependencyProperty BackStackProperty = BackStackPropertyKey.DependencyProperty;

        private static readonly DependencyPropertyKey ForwardStackPropertyKey =
            DependencyProperty.RegisterReadOnly(nameof(ForwardStack), typeof(IEnumerable), typeof(NavigationWindow), new FrameworkPropertyMetadata(null));

        public static readonly DependencyProperty ForwardStackProperty = ForwardStackPropertyKey.DependencyProperty;

        private NavigationService _navigationService;
        private bool _updatingFromNavService;

        static NavigationWindow()
        {
            ControlFoundationHooks.IsNavigationWindowHook = static w => w is NavigationWindow;
            ControlFoundationHooks.NavigationWindowStyleKeyHook = typeof(NavigationWindow);
            DefaultStyleKeyProperty.OverrideMetadata(typeof(NavigationWindow), new FrameworkPropertyMetadata(typeof(NavigationWindow)));
        }

        public NavigationWindow()
        {
            _navigationService = new NavigationService(this);
            SetValue(NavigationService.NavigationServiceProperty, _navigationService);
            SetValue(BackStackPropertyKey, _navigationService.BackStack);
            SetValue(ForwardStackPropertyKey, _navigationService.ForwardStack);
        }

        public NavigationService NavigationService => _navigationService;

        public Uri Source
        {
            get => (Uri)GetValue(SourceProperty);
            set => SetValue(SourceProperty, value);
        }

        public Uri CurrentSource => _navigationService.CurrentSource;

        public bool ShowsNavigationUI
        {
            get => (bool)GetValue(ShowsNavigationUIProperty);
            set => SetValue(ShowsNavigationUIProperty, value);
        }

        public bool SandboxExternalContent
        {
            get => (bool)GetValue(SandboxExternalContentProperty);
            set => SetValue(SandboxExternalContentProperty, value);
        }

        public bool CanGoBack => (bool)GetValue(CanGoBackProperty);
        public bool CanGoForward => (bool)GetValue(CanGoForwardProperty);
        public IEnumerable BackStack => (IEnumerable)GetValue(BackStackProperty);
        public IEnumerable ForwardStack => (IEnumerable)GetValue(ForwardStackProperty);

        Uri IUriContext.BaseUri { get; set; }

        public event NavigatingCancelEventHandler Navigating
        {
            add => _navigationService.Navigating += value;
            remove => _navigationService.Navigating -= value;
        }

        public event NavigatedEventHandler Navigated
        {
            add => _navigationService.Navigated += value;
            remove => _navigationService.Navigated -= value;
        }

        public event NavigationProgressEventHandler NavigationProgress
        {
            add => _navigationService.NavigationProgress += value;
            remove => _navigationService.NavigationProgress -= value;
        }

        public event NavigationFailedEventHandler NavigationFailed
        {
            add => _navigationService.NavigationFailed += value;
            remove => _navigationService.NavigationFailed -= value;
        }

        public event NavigationStoppedEventHandler NavigationStopped
        {
            add => _navigationService.NavigationStopped += value;
            remove => _navigationService.NavigationStopped -= value;
        }

        public event LoadCompletedEventHandler LoadCompleted
        {
            add => _navigationService.LoadCompleted += value;
            remove => _navigationService.LoadCompleted -= value;
        }

        public event FragmentNavigationEventHandler FragmentNavigation
        {
            add => _navigationService.FragmentNavigation += value;
            remove => _navigationService.FragmentNavigation -= value;
        }

        public bool Navigate(Uri source) => _navigationService.Navigate(source);
        public bool Navigate(Uri source, object extraData) => _navigationService.Navigate(source, extraData);
        public bool Navigate(object content) => _navigationService.Navigate(content);
        public bool Navigate(object content, object extraData) => _navigationService.Navigate(content, extraData);

        public void GoBack() => _navigationService.GoBack();
        public void GoForward() => _navigationService.GoForward();
        public void AddBackEntry(CustomContentState state) => _navigationService.AddBackEntry(state);
        public JournalEntry RemoveBackEntry() => _navigationService.RemoveBackEntry();
        public void Refresh() => _navigationService.Refresh();
        public void StopLoading() => _navigationService.StopLoading();

        protected override AutomationPeer OnCreateAutomationPeer() => new NavigationWindowAutomationPeer(this);

        protected override void OnContentChanged(object oldContent, object newContent)
        {
            base.OnContentChanged(oldContent, newContent);
            if (!_updatingFromNavService)
            {
                _navigationService.SetContentSilently(newContent);
            }
        }

        private static void OnSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is NavigationWindow navWin && !navWin._updatingFromNavService)
            {
                navWin._navigationService.Navigate((Uri)e.NewValue);
            }
        }

        void INavigationHost.OnNavigatedContent(object content, Uri source)
        {
            _updatingFromNavService = true;
            try
            {
                Source = source;
                if (!ReferenceEquals(Content, content))
                {
                    Content = content;
                }
            }
            finally
            {
                _updatingFromNavService = false;
            }
        }

        void INavigationHost.OnJournalChanged()
        {
            SetValue(CanGoBackPropertyKey, _navigationService.CanGoBack);
            SetValue(CanGoForwardPropertyKey, _navigationService.CanGoForward);
        }
    }
}

namespace System.Windows.Automation.Peers
{
    public class NavigationWindowAutomationPeer : WindowAutomationPeer
    {
        public NavigationWindowAutomationPeer(Navigation.NavigationWindow owner) : base(owner)
        {
        }

        protected override string GetClassNameCore() => "NavigationWindow";
    }
}

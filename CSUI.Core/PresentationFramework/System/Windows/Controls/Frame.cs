// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System;
using System.Collections;
using System.ComponentModel;
using System.Windows.Automation.Peers;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Navigation;

namespace System.Windows.Controls
{
    [DefaultProperty(nameof(Source)), DefaultEvent(nameof(Navigated))]
    [ContentProperty]
    [TemplatePart(Name = "PART_FrameCP", Type = typeof(ContentPresenter))]
    public partial class Frame : ContentControl, INavigator, INavigationHost, IAddChild, IUriContext
    {
        static partial void RegisterDefaultTheme();
        public static readonly DependencyProperty SourceProperty =
            DependencyProperty.Register(
                nameof(Source),
                typeof(Uri),
                typeof(Frame),
                new FrameworkPropertyMetadata(null, OnSourceChanged));

        public static readonly DependencyProperty NavigationUIVisibilityProperty =
            DependencyProperty.Register(
                nameof(NavigationUIVisibility),
                typeof(NavigationUIVisibility),
                typeof(Frame),
                new FrameworkPropertyMetadata(NavigationUIVisibility.Automatic));

        public static readonly DependencyProperty SandboxExternalContentProperty =
            DependencyProperty.Register(
                nameof(SandboxExternalContent),
                typeof(bool),
                typeof(Frame),
                new FrameworkPropertyMetadata(false));

        public static readonly DependencyProperty JournalOwnershipProperty =
            DependencyProperty.Register(
                nameof(JournalOwnership),
                typeof(JournalOwnership),
                typeof(Frame),
                new FrameworkPropertyMetadata(JournalOwnership.Automatic));

        private static readonly DependencyPropertyKey CanGoBackPropertyKey =
            DependencyProperty.RegisterReadOnly(
                nameof(CanGoBack),
                typeof(bool),
                typeof(Frame),
                new FrameworkPropertyMetadata(false));

        public static readonly DependencyProperty CanGoBackProperty = CanGoBackPropertyKey.DependencyProperty;

        private static readonly DependencyPropertyKey CanGoForwardPropertyKey =
            DependencyProperty.RegisterReadOnly(
                nameof(CanGoForward),
                typeof(bool),
                typeof(Frame),
                new FrameworkPropertyMetadata(false));

        public static readonly DependencyProperty CanGoForwardProperty = CanGoForwardPropertyKey.DependencyProperty;

        private static readonly DependencyPropertyKey BackStackPropertyKey =
            DependencyProperty.RegisterReadOnly(
                nameof(BackStack),
                typeof(IEnumerable),
                typeof(Frame),
                new FrameworkPropertyMetadata(null));

        public static readonly DependencyProperty BackStackProperty = BackStackPropertyKey.DependencyProperty;

        private static readonly DependencyPropertyKey ForwardStackPropertyKey =
            DependencyProperty.RegisterReadOnly(
                nameof(ForwardStack),
                typeof(IEnumerable),
                typeof(Frame),
                new FrameworkPropertyMetadata(null));

        public static readonly DependencyProperty ForwardStackProperty = ForwardStackPropertyKey.DependencyProperty;

        private NavigationService _navigationService;
        private bool _updatingFromNavService;

        static Frame()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(Frame), new FrameworkPropertyMetadata(typeof(Frame)));
            KeyboardNavigation.TabNavigationProperty.OverrideMetadata(typeof(Frame), new FrameworkPropertyMetadata(KeyboardNavigationMode.Local));
            KeyboardNavigation.ControlTabNavigationProperty.OverrideMetadata(typeof(Frame), new FrameworkPropertyMetadata(KeyboardNavigationMode.Once));
            RegisterDefaultTheme();
        }

        public Frame()
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

        public NavigationUIVisibility NavigationUIVisibility
        {
            get => (NavigationUIVisibility)GetValue(NavigationUIVisibilityProperty);
            set => SetValue(NavigationUIVisibilityProperty, value);
        }

        public bool SandboxExternalContent
        {
            get => (bool)GetValue(SandboxExternalContentProperty);
            set => SetValue(SandboxExternalContentProperty, value);
        }

        public JournalOwnership JournalOwnership
        {
            get => (JournalOwnership)GetValue(JournalOwnershipProperty);
            set => SetValue(JournalOwnershipProperty, value);
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

        public event EventHandler ContentRendered;

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

        protected override void OnContentChanged(object oldContent, object newContent)
        {
            base.OnContentChanged(oldContent, newContent);
            if (!_updatingFromNavService)
            {
                _navigationService.SetContentSilently(newContent);
            }
            ContentRendered?.Invoke(this, EventArgs.Empty);
        }

        protected override AutomationPeer OnCreateAutomationPeer() => new FrameAutomationPeer(this);

        private static void OnSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is Frame frame && !frame._updatingFromNavService)
            {
                frame._navigationService.Navigate((Uri)e.NewValue);
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
    public class FrameAutomationPeer : FrameworkElementAutomationPeer
    {
        public FrameAutomationPeer(Controls.Frame owner) : base(owner)
        {
        }

        protected override string GetClassNameCore() => "Frame";

        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Pane;
    }
}

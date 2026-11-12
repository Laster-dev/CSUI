// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System;
using System.ComponentModel;
using System.Net;

namespace System.Windows.Navigation
{
    public enum NavigationMode : byte
    {
        New = 0,
        Back = 1,
        Forward = 2,
        Refresh = 3,
    }

    public delegate void NavigatingCancelEventHandler(object sender, NavigatingCancelEventArgs e);
    public delegate void NavigationProgressEventHandler(object sender, NavigationProgressEventArgs e);
    public delegate void NavigationFailedEventHandler(object sender, NavigationFailedEventArgs e);
    public delegate void NavigatedEventHandler(object sender, NavigationEventArgs e);
    public delegate void LoadCompletedEventHandler(object sender, NavigationEventArgs e);
    public delegate void NavigationStoppedEventHandler(object sender, NavigationEventArgs e);
    public delegate void FragmentNavigationEventHandler(object sender, FragmentNavigationEventArgs e);
    public delegate void RequestNavigateEventHandler(object sender, RequestNavigateEventArgs e);
    public delegate void ReturnEventHandler<T>(object sender, ReturnEventArgs<T> e);

    public class NavigationEventArgs : EventArgs
    {
        internal NavigationEventArgs(Uri uri, object content, object extraData, WebResponse webResponse, object navigator, bool isNavigationInitiator)
        {
            Uri = uri;
            Content = content;
            ExtraData = extraData;
            WebResponse = webResponse;
            Navigator = navigator;
            IsNavigationInitiator = isNavigationInitiator;
        }

        public Uri Uri { get; }
        public object Content { get; }
        public object ExtraData { get; }
        public WebResponse WebResponse { get; }
        public object Navigator { get; }
        public bool IsNavigationInitiator { get; }
    }

    public class NavigatingCancelEventArgs : CancelEventArgs
    {
        internal NavigatingCancelEventArgs(
            Uri uri,
            object content,
            CustomContentState contentStateToSave,
            CustomContentState targetContentState,
            NavigationMode navigationMode,
            WebRequest webRequest,
            object extraData,
            object navigator,
            bool isNavigationInitiator)
        {
            Uri = uri;
            Content = content;
            ContentStateToSave = contentStateToSave;
            TargetContentState = targetContentState;
            NavigationMode = navigationMode;
            WebRequest = webRequest;
            ExtraData = extraData;
            Navigator = navigator;
            IsNavigationInitiator = isNavigationInitiator;
        }

        public Uri Uri { get; }
        public object Content { get; }
        public CustomContentState ContentStateToSave { get; set; }
        public CustomContentState TargetContentState { get; }
        public NavigationMode NavigationMode { get; }
        public WebRequest WebRequest { get; }
        public object ExtraData { get; }
        public object Navigator { get; }
        public bool IsNavigationInitiator { get; }
    }

    public class NavigationFailedEventArgs : EventArgs
    {
        internal NavigationFailedEventArgs(Uri uri, object extraData, object navigator, WebRequest request, WebResponse response, Exception e)
        {
            Uri = uri;
            ExtraData = extraData;
            Navigator = navigator;
            WebRequest = request;
            WebResponse = response;
            Exception = e;
        }

        public Uri Uri { get; }
        public object ExtraData { get; }
        public object Navigator { get; }
        public WebRequest WebRequest { get; }
        public WebResponse WebResponse { get; }
        public Exception Exception { get; }
        public bool Handled { get; set; }
    }

    public class NavigationProgressEventArgs : EventArgs
    {
        internal NavigationProgressEventArgs(Uri uri, long bytesRead, long maxBytes, object navigator)
        {
            Uri = uri;
            BytesRead = bytesRead;
            MaxBytes = maxBytes;
            Navigator = navigator;
        }

        public Uri Uri { get; }
        public long BytesRead { get; }
        public long MaxBytes { get; }
        public object Navigator { get; }
    }

    public class FragmentNavigationEventArgs : EventArgs
    {
        internal FragmentNavigationEventArgs(string fragment, object navigator)
        {
            Fragment = fragment;
            Navigator = navigator;
        }

        public string Fragment { get; }
        public bool Handled { get; set; }
        public object Navigator { get; }
    }

    public class RequestNavigateEventArgs : RoutedEventArgs
    {
        protected RequestNavigateEventArgs()
        {
        }

        public RequestNavigateEventArgs(Uri uri, string target)
        {
            Uri = uri;
            Target = target;
        }

        public Uri Uri { get; }
        public string Target { get; }

        protected override void InvokeEventHandler(Delegate genericHandler, object genericTarget)
        {
            if (genericHandler is RequestNavigateEventHandler handler)
            {
                handler(genericTarget, this);
            }
            else
            {
                base.InvokeEventHandler(genericHandler, genericTarget);
            }
        }
    }

    public class ReturnEventArgs<T> : EventArgs
    {
        public ReturnEventArgs()
        {
        }

        public ReturnEventArgs(T result)
        {
            Result = result;
        }

        public T Result { get; set; }
    }

    [Serializable]
    public abstract class CustomContentState
    {
        public virtual string JournalEntryName => null;
        public abstract void Replay(NavigationService navigationService, NavigationMode mode);
    }
}

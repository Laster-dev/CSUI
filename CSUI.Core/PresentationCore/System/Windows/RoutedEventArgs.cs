// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System.Collections.Specialized;
using MS.Internal;
using SR = MS.Internal.PresentationCore.SR;

namespace System.Windows
{
    public class RoutedEventArgs : EventArgs
    {
        public RoutedEventArgs()
        {
        }

        public RoutedEventArgs(RoutedEvent routedEvent)
            : this(routedEvent, null)
        {
        }

        public RoutedEventArgs(RoutedEvent routedEvent, object source)
        {
            _routedEvent = routedEvent;
            _source = _originalSource = source;
        }

        public RoutedEvent RoutedEvent
        {
            get { return _routedEvent; }
            set
            {
                if (UserInitiated && InvokingHandler)
                    throw new InvalidOperationException(SR.RoutedEventCannotChangeWhileRouting);

                _routedEvent = value;
            }
        }

        internal void OverrideRoutedEvent(RoutedEvent newRoutedEvent)
        {
            _routedEvent = newRoutedEvent;
        }

        public bool Handled
        {
            get { return _flags[HandledIndex]; }
            set
            {
                if (_routedEvent == null)
                {
                    throw new InvalidOperationException(SR.RoutedEventArgsMustHaveRoutedEvent);
                }

                if (TraceRoutedEvent.IsEnabled)
                {
                    TraceRoutedEvent.TraceActivityItem(
                        TraceRoutedEvent.HandleEvent,
                        value,
                        RoutedEvent.OwnerType.Name,
                        RoutedEvent.Name,
                        this);
                }

                _flags[HandledIndex] = value;
            }
        }

        public object Source
        {
            get { return _source; }
            set
            {
                if (InvokingHandler && UserInitiated)
                    throw new InvalidOperationException(SR.RoutedEventCannotChangeWhileRouting);

                if (_routedEvent == null)
                {
                    throw new InvalidOperationException(SR.RoutedEventArgsMustHaveRoutedEvent);
                }

                object source = value;
                if (_source == null && _originalSource == null)
                {
                    _source = _originalSource = source;
                    OnSetSource(source);
                }
                else if (_source != source)
                {
                    _source = source;
                    OnSetSource(source);
                }
            }
        }

        internal void OverrideSource(object source)
        {
            _source = source;
        }

        public object OriginalSource => _originalSource;

        protected virtual void OnSetSource(object source)
        {
        }

        protected virtual void InvokeEventHandler(Delegate genericHandler, object genericTarget)
        {
            ArgumentNullException.ThrowIfNull(genericHandler);
            ArgumentNullException.ThrowIfNull(genericTarget);

            if (_routedEvent == null)
            {
                throw new InvalidOperationException(SR.RoutedEventArgsMustHaveRoutedEvent);
            }

            InvokingHandler = true;
            try
            {
                if (genericHandler is RoutedEventHandler routedHandler)
                {
                    routedHandler(genericTarget, this);
                }
                else if (_routedEvent.TryInvokeCustomHandler(genericHandler, genericTarget, this))
                {
                }
                else if (genericHandler is EventHandler clrHandler)
                {
                    clrHandler(genericTarget, this);
                }
                else
                {
                    throw new NotSupportedException(
                        $"Handler type '{genericHandler.GetType().Name}' requires either overriding InvokeEventHandler on '{GetType().Name}' or registering with EventManager.RegisterRoutedEvent<THandler> for zero-reflection NativeAOT dispatch.");
                }
            }
            finally
            {
                InvokingHandler = false;
            }
        }

        internal void InvokeHandler(Delegate handler, object target)
        {
            InvokingHandler = true;
            try
            {
                InvokeEventHandler(handler, target);
            }
            finally
            {
                InvokingHandler = false;
            }
        }

        internal bool UserInitiated => _flags[UserInitiatedIndex];

        internal void MarkAsUserInitiated()
        {
            _flags[UserInitiatedIndex] = true;
        }

        internal void ClearUserInitiated()
        {
            _flags[UserInitiatedIndex] = false;
        }

        private bool InvokingHandler
        {
            get { return _flags[InvokingHandlerIndex]; }
            set { _flags[InvokingHandlerIndex] = value; }
        }

        private RoutedEvent _routedEvent;
        private object _source;
        private object _originalSource;

        private BitVector32 _flags;

        private const int HandledIndex = 1;
        private const int UserInitiatedIndex = 2;
        private const int InvokingHandlerIndex = 4;
    }

    public delegate void RoutedEventHandler(object sender, RoutedEventArgs e);
}

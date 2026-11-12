// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

namespace System.Windows
{
    public struct RoutedEventHandlerInfo
    {
        internal RoutedEventHandlerInfo(Delegate handler, bool handledEventsToo)
        {
            _handler = handler;
            _handledEventsToo = handledEventsToo;
        }

        public Delegate Handler => _handler;
        public bool InvokeHandledEventsToo => _handledEventsToo;

        internal void InvokeHandler(object target, RoutedEventArgs routedEventArgs)
        {
            if ((!routedEventArgs.Handled) || (_handledEventsToo))
            {
                if (_handler is RoutedEventHandler reh)
                {
                    reh(target, routedEventArgs);
                }
                else
                {
                    routedEventArgs.InvokeHandler(_handler, target);
                }
            }
        }

        public override bool Equals(object obj)
        {
            if (obj is RoutedEventHandlerInfo info)
            {
                return Equals(info);
            }
            return false;
        }

        public bool Equals(RoutedEventHandlerInfo handlerInfo)
        {
            return _handler == handlerInfo._handler && _handledEventsToo == handlerInfo._handledEventsToo;
        }

        public override int GetHashCode() => base.GetHashCode();

        public static bool operator ==(RoutedEventHandlerInfo handlerInfo1, RoutedEventHandlerInfo handlerInfo2)
            => handlerInfo1.Equals(handlerInfo2);

        public static bool operator !=(RoutedEventHandlerInfo handlerInfo1, RoutedEventHandlerInfo handlerInfo2)
            => !handlerInfo1.Equals(handlerInfo2);

        private Delegate _handler;
        private bool _handledEventsToo;
    }
}

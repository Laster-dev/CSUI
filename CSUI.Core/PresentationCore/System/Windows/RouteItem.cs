// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

namespace System.Windows
{
    internal struct RouteItem
    {
        internal RouteItem(object target, RoutedEventHandlerInfo routedEventHandlerInfo)
        {
            _target = target;
            _routedEventHandlerInfo = routedEventHandlerInfo;
        }

        internal object Target => _target;

        internal void InvokeHandler(RoutedEventArgs routedEventArgs)
        {
            _routedEventHandlerInfo.InvokeHandler(_target, routedEventArgs);
        }

        public override bool Equals(object o)
        {
            return o is RouteItem item && Equals(item);
        }

        public bool Equals(RouteItem routeItem)
        {
            return routeItem._target == this._target &&
                   routeItem._routedEventHandlerInfo == this._routedEventHandlerInfo;
        }

        public override int GetHashCode() => base.GetHashCode();

        public static bool operator ==(RouteItem routeItem1, RouteItem routeItem2) => routeItem1.Equals(routeItem2);
        public static bool operator !=(RouteItem routeItem1, RouteItem routeItem2) => !routeItem1.Equals(routeItem2);

        private object _target;
        private RoutedEventHandlerInfo _routedEventHandlerInfo;
    }
}

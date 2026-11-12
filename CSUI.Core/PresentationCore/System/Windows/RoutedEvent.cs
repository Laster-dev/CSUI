// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System.Globalization;

namespace System.Windows
{
    public sealed class RoutedEvent
    {
        public RoutedEvent AddOwner(Type ownerType)
        {
            GlobalEventManager.AddOwner(this, ownerType);
            return this;
        }

        public string Name => _name;
        public RoutingStrategy RoutingStrategy => _routingStrategy;
        public Type HandlerType => _handlerType;
        public Type OwnerType => _ownerType;

        internal bool IsLegalHandler(Delegate handler)
        {
            Type handlerType = handler.GetType();
            return (handlerType == HandlerType) || (handlerType == typeof(RoutedEventHandler));
        }

        internal bool TryInvokeCustomHandler(Delegate genericHandler, object genericTarget, RoutedEventArgs args)
        {
            if (_customInvoker != null)
            {
                _customInvoker(genericHandler, genericTarget, args);
                return true;
            }
            return false;
        }

        public override string ToString()
        {
            return string.Format(CultureInfo.InvariantCulture, "{0}.{1}", _ownerType.Name, _name);
        }

        internal int GlobalIndex { get; }

        internal RoutedEvent(
            string name,
            RoutingStrategy routingStrategy,
            Type handlerType,
            Type ownerType,
            Action<Delegate, object, RoutedEventArgs> customInvoker = null)
        {
            _name = name;
            _routingStrategy = routingStrategy;
            _handlerType = handlerType;
            _ownerType = ownerType;
            _customInvoker = customInvoker;

            GlobalIndex = GlobalEventManager.GetNextAvailableGlobalIndex();
        }

        private string _name;
        private RoutingStrategy _routingStrategy;
        private Type _handlerType;
        private Type _ownerType;
        private Action<Delegate, object, RoutedEventArgs> _customInvoker;
    }
}

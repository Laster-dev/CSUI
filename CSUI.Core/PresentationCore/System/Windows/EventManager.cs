// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System.ComponentModel;
using SR = MS.Internal.PresentationCore.SR;

namespace System.Windows
{
    public static class EventManager
    {
        public static RoutedEvent RegisterRoutedEvent(
            string name,
            RoutingStrategy routingStrategy,
            Type handlerType,
            Type ownerType)
        {
            ArgumentNullException.ThrowIfNull(name);

            if (routingStrategy != RoutingStrategy.Tunnel &&
                routingStrategy != RoutingStrategy.Bubble &&
                routingStrategy != RoutingStrategy.Direct)
            {
                throw new InvalidEnumArgumentException(nameof(routingStrategy), (int)routingStrategy, typeof(RoutingStrategy));
            }

            ArgumentNullException.ThrowIfNull(handlerType);
            ArgumentNullException.ThrowIfNull(ownerType);

            if (GlobalEventManager.GetRoutedEventFromName(name, ownerType, false) != null)
            {
                throw new ArgumentException(SR.Format(SR.DuplicateEventName, name, ownerType));
            }

            return GlobalEventManager.RegisterRoutedEvent(name, routingStrategy, handlerType, ownerType);
        }

        /// <summary>
        /// Zero-reflection NativeAOT overload supplying a strongly typed custom handler invoker.
        /// </summary>
        public static RoutedEvent RegisterRoutedEvent<THandler>(
            string name,
            RoutingStrategy routingStrategy,
            Type ownerType,
            Action<THandler, object, RoutedEventArgs> invoker)
            where THandler : Delegate
        {
            ArgumentNullException.ThrowIfNull(name);
            ArgumentNullException.ThrowIfNull(ownerType);
            ArgumentNullException.ThrowIfNull(invoker);

            if (routingStrategy != RoutingStrategy.Tunnel &&
                routingStrategy != RoutingStrategy.Bubble &&
                routingStrategy != RoutingStrategy.Direct)
            {
                throw new InvalidEnumArgumentException(nameof(routingStrategy), (int)routingStrategy, typeof(RoutingStrategy));
            }

            if (GlobalEventManager.GetRoutedEventFromName(name, ownerType, false) != null)
            {
                throw new ArgumentException(SR.Format(SR.DuplicateEventName, name, ownerType));
            }

            return GlobalEventManager.RegisterRoutedEvent(
                name,
                routingStrategy,
                typeof(THandler),
                ownerType,
                (h, target, args) => invoker((THandler)h, target, args));
        }

        public static void RegisterClassHandler(
            Type classType,
            RoutedEvent routedEvent,
            Delegate handler)
        {
            RegisterClassHandler(classType, routedEvent, handler, false);
        }

        public static void RegisterClassHandler(
            Type classType,
            RoutedEvent routedEvent,
            Delegate handler,
            bool handledEventsToo)
        {
            ArgumentNullException.ThrowIfNull(classType);
            ArgumentNullException.ThrowIfNull(routedEvent);
            ArgumentNullException.ThrowIfNull(handler);

            // TODO(Stage C3): tighten check to UIElement/ContentElement once Stage C3 is ported.
            if (!typeof(DependencyObject).IsAssignableFrom(classType))
            {
                throw new ArgumentException(SR.ClassTypeIllegal);
            }

            if (!routedEvent.IsLegalHandler(handler))
            {
                throw new ArgumentException(SR.HandlerTypeIllegal);
            }

            GlobalEventManager.RegisterClassHandler(classType, routedEvent, handler, handledEventsToo);
        }

        public static RoutedEvent[] GetRoutedEvents()
        {
            return GlobalEventManager.GetRoutedEvents();
        }

        public static RoutedEvent[] GetRoutedEventsForOwner(Type ownerType)
        {
            ArgumentNullException.ThrowIfNull(ownerType);
            return GlobalEventManager.GetRoutedEventsForOwner(ownerType);
        }

        internal static RoutedEvent GetRoutedEventFromName(string name, Type ownerType)
        {
            ArgumentNullException.ThrowIfNull(name);
            ArgumentNullException.ThrowIfNull(ownerType);
            return GlobalEventManager.GetRoutedEventFromName(name, ownerType, true);
        }
    }
}

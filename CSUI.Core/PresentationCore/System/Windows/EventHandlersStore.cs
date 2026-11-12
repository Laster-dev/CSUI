// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using MS.Utility;
using SR = MS.Internal.PresentationCore.SR;

namespace System.Windows
{
    internal class EventHandlersStore
    {
        public EventHandlersStore()
        {
            _entries = new FrugalMap();
        }

        public EventHandlersStore(EventHandlersStore source)
        {
            _entries = source._entries;
        }

        public void Add(EventPrivateKey key, Delegate handler)
        {
            ArgumentNullException.ThrowIfNull(key);
            ArgumentNullException.ThrowIfNull(handler);

            Delegate existingDelegate = this[key];
            if (existingDelegate == null)
            {
                _entries[key.GlobalIndex] = handler;
            }
            else
            {
                _entries[key.GlobalIndex] = Delegate.Combine(existingDelegate, handler);
            }
        }

        public void Remove(EventPrivateKey key, Delegate handler)
        {
            ArgumentNullException.ThrowIfNull(key);
            ArgumentNullException.ThrowIfNull(handler);

            Delegate existingDelegate = this[key];
            if (existingDelegate != null)
            {
                existingDelegate = Delegate.Remove(existingDelegate, handler);
                if (existingDelegate == null)
                {
                    _entries[key.GlobalIndex] = DependencyProperty.UnsetValue;
                }
                else
                {
                    _entries[key.GlobalIndex] = existingDelegate;
                }
            }
        }

        public Delegate Get(EventPrivateKey key)
        {
            ArgumentNullException.ThrowIfNull(key);
            return this[key];
        }

        public void AddRoutedEventHandler(
            RoutedEvent routedEvent,
            Delegate handler,
            bool handledEventsToo)
        {
            ArgumentNullException.ThrowIfNull(routedEvent);
            ArgumentNullException.ThrowIfNull(handler);
            if (!routedEvent.IsLegalHandler(handler))
            {
                throw new ArgumentException(SR.HandlerTypeIllegal);
            }

            RoutedEventHandlerInfo routedEventHandlerInfo =
                new RoutedEventHandlerInfo(handler, handledEventsToo);

            FrugalObjectList<RoutedEventHandlerInfo> handlers = this[routedEvent];
            if (handlers == null)
            {
                _entries[routedEvent.GlobalIndex] = handlers = new FrugalObjectList<RoutedEventHandlerInfo>(1);
            }

            handlers.Add(routedEventHandlerInfo);
        }

        public void RemoveRoutedEventHandler(RoutedEvent routedEvent, Delegate handler)
        {
            ArgumentNullException.ThrowIfNull(routedEvent);
            ArgumentNullException.ThrowIfNull(handler);
            if (!routedEvent.IsLegalHandler(handler))
            {
                throw new ArgumentException(SR.HandlerTypeIllegal);
            }

            FrugalObjectList<RoutedEventHandlerInfo> handlers = this[routedEvent];
            if (handlers != null && handlers.Count > 0)
            {
                if ((handlers.Count == 1) && (handlers[0].Handler == handler))
                {
                    _entries[routedEvent.GlobalIndex] = DependencyProperty.UnsetValue;
                }
                else
                {
                    for (int i = 0; i < handlers.Count; i++)
                    {
                        if (handlers[i].Handler == handler)
                        {
                            handlers.RemoveAt(i);
                            break;
                        }
                    }
                }
            }
        }

        public bool Contains(RoutedEvent routedEvent)
        {
            ArgumentNullException.ThrowIfNull(routedEvent);
            FrugalObjectList<RoutedEventHandlerInfo> handlers = this[routedEvent];
            return handlers != null && handlers.Count != 0;
        }

        public RoutedEventHandlerInfo[] GetRoutedEventHandlers(RoutedEvent routedEvent)
        {
            ArgumentNullException.ThrowIfNull(routedEvent);
            FrugalObjectList<RoutedEventHandlerInfo> handlers = this[routedEvent];
            return handlers?.ToArray();
        }

        internal FrugalObjectList<RoutedEventHandlerInfo> this[RoutedEvent key]
        {
            get
            {
                Debug.Assert(key != null, "Search key cannot be null");
                object list = _entries[key.GlobalIndex];
                if (list == DependencyProperty.UnsetValue)
                {
                    return null;
                }
                return (FrugalObjectList<RoutedEventHandlerInfo>)list;
            }
        }

        internal Delegate this[EventPrivateKey key]
        {
            get
            {
                Debug.Assert(key != null, "Search key cannot be null");
                object existingDelegate = _entries[key.GlobalIndex];
                if (existingDelegate == DependencyProperty.UnsetValue)
                {
                    return null;
                }
                return (Delegate)existingDelegate;
            }
        }

        internal int Count => _entries.Count;

        private FrugalMap _entries;
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System.Collections;
using System.Threading;
using MS.Utility;
using SR = MS.Internal.PresentationCore.SR;

namespace System.Windows
{
    internal static class GlobalEventManager
    {
        internal static RoutedEvent RegisterRoutedEvent(
            string name,
            RoutingStrategy routingStrategy,
            Type handlerType,
            Type ownerType,
            Action<Delegate, object, RoutedEventArgs> customInvoker = null)
        {
            Debug.Assert(GetRoutedEventFromName(name, ownerType, false) == null,
                "RoutedEvent name must be unique within a given OwnerType");

            lock (Synchronized)
            {
                RoutedEvent routedEvent = new RoutedEvent(
                    name,
                    routingStrategy,
                    handlerType,
                    ownerType,
                    customInvoker);

                _countRoutedEvents++;
                AddOwner(routedEvent, ownerType);
                return routedEvent;
            }
        }

        internal static void RegisterClassHandler(
            Type classType,
            RoutedEvent routedEvent,
            Delegate handler,
            bool handledEventsToo)
        {
            // TODO(Stage C3): tighten check to UIElement/ContentElement once Stage C3 is ported.
            Debug.Assert(typeof(DependencyObject).IsAssignableFrom(classType),
                "Class Handlers can be registered only for UIElement/ContentElement and their sub types");
            Debug.Assert(routedEvent.IsLegalHandler(handler),
                "Handler Type mismatch");

            ClassHandlersStore classListenersLists;
            int index;

            DependencyObjectType dType = DependencyObjectType.FromSystemTypeInternal(classType);
            GetDTypedClassListeners(dType, routedEvent, out classListenersLists, out index);

            lock (Synchronized)
            {
                RoutedEventHandlerInfoList updatedClassListeners =
                    classListenersLists.AddToExistingHandlers(index, handler, handledEventsToo);

                ItemStructList<DependencyObjectType> keys = _dTypedClassListeners.ActiveDTypes;

                for (int i = 0; i < keys.Count; i++)
                {
                    if (keys.List[i].IsSubclassOf(dType))
                    {
                        classListenersLists = (ClassHandlersStore)_dTypedClassListeners[keys.List[i]];
                        classListenersLists.UpdateSubClassHandlers(routedEvent, updatedClassListeners);
                    }
                }
            }
        }

        internal static RoutedEvent[] GetRoutedEvents()
        {
            RoutedEvent[] routedEvents;

            lock (Synchronized)
            {
                routedEvents = new RoutedEvent[_countRoutedEvents];

                ItemStructList<DependencyObjectType> keys = _dTypedRoutedEventList.ActiveDTypes;

                int destIndex = 0;
                for (int i = 0; i < keys.Count; i++)
                {
                    FrugalObjectList<RoutedEvent> dTypedRoutedEventList = (FrugalObjectList<RoutedEvent>)_dTypedRoutedEventList[keys.List[i]];

                    for (int j = 0; j < dTypedRoutedEventList.Count; j++)
                    {
                        RoutedEvent routedEvent = dTypedRoutedEventList[j];

                        if (Array.IndexOf(routedEvents, routedEvent) < 0)
                        {
                            routedEvents[destIndex++] = routedEvent;
                        }
                    }
                }

                IDictionaryEnumerator htEnumerator = _ownerTypedRoutedEventList.GetEnumerator();

                while (htEnumerator.MoveNext())
                {
                    FrugalObjectList<RoutedEvent> ownerRoutedEventList = (FrugalObjectList<RoutedEvent>)htEnumerator.Value;

                    for (int j = 0; j < ownerRoutedEventList.Count; j++)
                    {
                        RoutedEvent routedEvent = ownerRoutedEventList[j];

                        if (Array.IndexOf(routedEvents, routedEvent) < 0)
                        {
                            routedEvents[destIndex++] = routedEvent;
                        }
                    }
                }
            }

            return routedEvents;
        }

        internal static void AddOwner(RoutedEvent routedEvent, Type ownerType)
        {
            if ((ownerType == typeof(DependencyObject)) || ownerType.IsSubclassOf(typeof(DependencyObject)))
            {
                DependencyObjectType dType = DependencyObjectType.FromSystemTypeInternal(ownerType);

                object ownerRoutedEventListObj = _dTypedRoutedEventList[dType];
                FrugalObjectList<RoutedEvent> ownerRoutedEventList;
                if (ownerRoutedEventListObj == null)
                {
                    ownerRoutedEventList = new FrugalObjectList<RoutedEvent>(1);
                    _dTypedRoutedEventList[dType] = ownerRoutedEventList;
                }
                else
                {
                    ownerRoutedEventList = (FrugalObjectList<RoutedEvent>)ownerRoutedEventListObj;
                }

                if (!ownerRoutedEventList.Contains(routedEvent))
                {
                    ownerRoutedEventList.Add(routedEvent);
                }
            }
            else
            {
                object ownerRoutedEventListObj = _ownerTypedRoutedEventList[ownerType];
                FrugalObjectList<RoutedEvent> ownerRoutedEventList;
                if (ownerRoutedEventListObj == null)
                {
                    ownerRoutedEventList = new FrugalObjectList<RoutedEvent>(1);
                    _ownerTypedRoutedEventList[ownerType] = ownerRoutedEventList;
                }
                else
                {
                    ownerRoutedEventList = (FrugalObjectList<RoutedEvent>)ownerRoutedEventListObj;
                }

                if (!ownerRoutedEventList.Contains(routedEvent))
                {
                    ownerRoutedEventList.Add(routedEvent);
                }
            }
        }

        internal static RoutedEvent[] GetRoutedEventsForOwner(Type ownerType)
        {
            if ((ownerType == typeof(DependencyObject)) || ownerType.IsSubclassOf(typeof(DependencyObject)))
            {
                DependencyObjectType dType = DependencyObjectType.FromSystemTypeInternal(ownerType);

                FrugalObjectList<RoutedEvent> ownerRoutedEventList = (FrugalObjectList<RoutedEvent>)_dTypedRoutedEventList[dType];
                if (ownerRoutedEventList != null)
                {
                    return ownerRoutedEventList.ToArray();
                }
            }
            else
            {
                FrugalObjectList<RoutedEvent> ownerRoutedEventList = (FrugalObjectList<RoutedEvent>)_ownerTypedRoutedEventList[ownerType];
                if (ownerRoutedEventList != null)
                {
                    return ownerRoutedEventList.ToArray();
                }
            }

            return null;
        }

        internal static RoutedEvent GetRoutedEventFromName(
            string name,
            Type ownerType,
            bool includeSupers)
        {
            if ((ownerType == typeof(DependencyObject)) || ownerType.IsSubclassOf(typeof(DependencyObject)))
            {
                DependencyObjectType dType = DependencyObjectType.FromSystemTypeInternal(ownerType);

                while (dType != null)
                {
                    FrugalObjectList<RoutedEvent> ownerRoutedEventList = (FrugalObjectList<RoutedEvent>)_dTypedRoutedEventList[dType];
                    if (ownerRoutedEventList != null)
                    {
                        for (int i = 0; i < ownerRoutedEventList.Count; i++)
                        {
                            RoutedEvent routedEvent = ownerRoutedEventList[i];
                            if (routedEvent.Name.Equals(name))
                            {
                                return routedEvent;
                            }
                        }
                    }

                    dType = includeSupers ? dType.BaseType : null;
                }
            }
            else
            {
                while (ownerType != null)
                {
                    FrugalObjectList<RoutedEvent> ownerRoutedEventList = (FrugalObjectList<RoutedEvent>)_ownerTypedRoutedEventList[ownerType];
                    if (ownerRoutedEventList != null)
                    {
                        for (int i = 0; i < ownerRoutedEventList.Count; i++)
                        {
                            RoutedEvent routedEvent = ownerRoutedEventList[i];
                            if (routedEvent.Name.Equals(name))
                            {
                                return routedEvent;
                            }
                        }
                    }

                    ownerType = includeSupers ? ownerType.BaseType : null;
                }
            }

            return null;
        }

        internal static RoutedEventHandlerInfoList GetDTypedClassListeners(
            DependencyObjectType dType,
            RoutedEvent routedEvent)
        {
            ClassHandlersStore classListenersLists;
            int index;
            return GetDTypedClassListeners(dType, routedEvent, out classListenersLists, out index);
        }

        internal static RoutedEventHandlerInfoList GetDTypedClassListeners(
            DependencyObjectType dType,
            RoutedEvent routedEvent,
            out ClassHandlersStore classListenersLists,
            out int index)
        {
            classListenersLists = (ClassHandlersStore)_dTypedClassListeners[dType];
            RoutedEventHandlerInfoList handlers;
            if (classListenersLists != null)
            {
                index = classListenersLists.GetHandlersIndex(routedEvent);
                if (index != -1)
                {
                    handlers = classListenersLists.GetExistingHandlers(index);
                    return handlers;
                }
            }

            lock (Synchronized)
            {
                handlers = GetUpdatedDTypedClassListeners(dType, routedEvent, out classListenersLists, out index);
            }

            return handlers;
        }

        private static RoutedEventHandlerInfoList GetUpdatedDTypedClassListeners(
            DependencyObjectType dType,
            RoutedEvent routedEvent,
            out ClassHandlersStore classListenersLists,
            out int index)
        {
            classListenersLists = (ClassHandlersStore)_dTypedClassListeners[dType];
            RoutedEventHandlerInfoList handlers;
            if (classListenersLists != null)
            {
                index = classListenersLists.GetHandlersIndex(routedEvent);
                if (index != -1)
                {
                    handlers = classListenersLists.GetExistingHandlers(index);
                    return handlers;
                }
            }

            DependencyObjectType tempDType = dType;
            ClassHandlersStore tempClassListenersLists = null;
            RoutedEventHandlerInfoList tempHandlers = null;
            int tempIndex = -1;
            while (tempIndex == -1 && tempDType.Id != _dependencyObjectType.Id)
            {
                tempDType = tempDType.BaseType;
                tempClassListenersLists = (ClassHandlersStore)_dTypedClassListeners[tempDType];
                if (tempClassListenersLists != null)
                {
                    tempIndex = tempClassListenersLists.GetHandlersIndex(routedEvent);
                    if (tempIndex != -1)
                    {
                        tempHandlers = tempClassListenersLists.GetExistingHandlers(tempIndex);
                    }
                }
            }

            if (classListenersLists == null)
            {
                // TODO(Stage C3): check UIElement / ContentElement specifically for 80-slot hint once ported.
                classListenersLists = new ClassHandlersStore(8);
                _dTypedClassListeners[dType] = classListenersLists;
            }

            index = classListenersLists.CreateHandlersLink(routedEvent, tempHandlers);
            return tempHandlers;
        }

        internal static int GetNextAvailableGlobalIndex()
        {
            uint newIndex = Interlocked.Increment(ref s_globalEventIndex);
            if (newIndex >= int.MaxValue)
                throw new InvalidOperationException(SR.TooManyRoutedEvents);

            return (int)newIndex;
        }

        private static uint s_globalEventIndex = uint.MinValue;

        private static DTypeMap _dTypedRoutedEventList = new DTypeMap(10);
        private static Hashtable _ownerTypedRoutedEventList = new Hashtable(10);
        private static int _countRoutedEvents = 0;
        private static DTypeMap _dTypedClassListeners = new DTypeMap(100);
        private static DependencyObjectType _dependencyObjectType = DependencyObjectType.FromSystemTypeInternal(typeof(DependencyObject));

        internal static readonly Lock Synchronized = new();
    }
}

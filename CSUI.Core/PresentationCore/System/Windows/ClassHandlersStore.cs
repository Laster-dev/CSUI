// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using MS.Utility;

namespace System.Windows
{
    internal class ClassHandlersStore
    {
        internal ClassHandlersStore(int size)
        {
            _eventHandlersList = new ItemStructList<ClassHandlers>(size);
        }

        internal RoutedEventHandlerInfoList AddToExistingHandlers(
            int index,
            Delegate handler,
            bool handledEventsToo)
        {
            Debug.Assert(index != -1, "There should exist a set of handlers for the given routedEvent");

            RoutedEventHandlerInfo routedEventHandlerInfo =
                new RoutedEventHandlerInfo(handler, handledEventsToo);

            RoutedEventHandlerInfoList handlers = _eventHandlersList.List[index].Handlers;
            if (handlers == null || !_eventHandlersList.List[index].HasSelfHandlers)
            {
                handlers = new RoutedEventHandlerInfoList
                {
                    Handlers = new RoutedEventHandlerInfo[1]
                };
                handlers.Handlers[0] = routedEventHandlerInfo;
                handlers.Next = _eventHandlersList.List[index].Handlers;
                _eventHandlersList.List[index].Handlers = handlers;
                _eventHandlersList.List[index].HasSelfHandlers = true;
            }
            else
            {
                int length = handlers.Handlers.Length;
                RoutedEventHandlerInfo[] mergedHandlers = new RoutedEventHandlerInfo[length + 1];
                Array.Copy(handlers.Handlers, 0, mergedHandlers, 0, length);
                mergedHandlers[length] = routedEventHandlerInfo;
                handlers.Handlers = mergedHandlers;
            }

            return handlers;
        }

        internal RoutedEventHandlerInfoList GetExistingHandlers(int index)
        {
            Debug.Assert(index != -1, "There should exist a set of handlers for the given index");
            return _eventHandlersList.List[index].Handlers;
        }

        internal int CreateHandlersLink(RoutedEvent routedEvent, RoutedEventHandlerInfoList handlers)
        {
            Debug.Assert(GetHandlersIndex(routedEvent) == -1, "There should not exist a set of handlers for the given routedEvent");

            ClassHandlers classHandlers = new ClassHandlers
            {
                RoutedEvent = routedEvent,
                Handlers = handlers,
                HasSelfHandlers = false
            };
            _eventHandlersList.Add(classHandlers);

            return _eventHandlersList.Count - 1;
        }

        internal void UpdateSubClassHandlers(
            RoutedEvent routedEvent,
            RoutedEventHandlerInfoList baseClassListeners)
        {
            Debug.Assert(baseClassListeners != null, "Update only when there are base class listeners to be updated");

            int index = GetHandlersIndex(routedEvent);
            if (index != -1)
            {
                bool hasSelfHandlers = _eventHandlersList.List[index].HasSelfHandlers;

                RoutedEventHandlerInfoList handlers = hasSelfHandlers
                    ? _eventHandlersList.List[index].Handlers.Next
                    : _eventHandlersList.List[index].Handlers;

                bool needToChange = false;

                if (handlers != null)
                {
                    if (baseClassListeners.Next != null && baseClassListeners.Next.Contains(handlers))
                    {
                        needToChange = true;
                    }
                }
                else
                {
                    needToChange = true;
                }

                if (needToChange)
                {
                    if (hasSelfHandlers)
                    {
                        _eventHandlersList.List[index].Handlers.Next = baseClassListeners;
                    }
                    else
                    {
                        _eventHandlersList.List[index].Handlers = baseClassListeners;
                    }
                }
            }
        }

        internal int GetHandlersIndex(RoutedEvent routedEvent)
        {
            for (int i = 0; i < _eventHandlersList.Count; i++)
            {
                if (_eventHandlersList.List[i].RoutedEvent == routedEvent)
                {
                    return i;
                }
            }

            return -1;
        }

        private ItemStructList<ClassHandlers> _eventHandlersList;
    }

    internal struct ClassHandlers
    {
        public override bool Equals(object o)
        {
            return o is ClassHandlers ch && Equals(ch);
        }

        public bool Equals(ClassHandlers classHandlers)
        {
            return classHandlers.RoutedEvent == this.RoutedEvent &&
                   classHandlers.Handlers == this.Handlers;
        }

        public override int GetHashCode() => base.GetHashCode();

        public static bool operator ==(ClassHandlers classHandlers1, ClassHandlers classHandlers2)
            => classHandlers1.Equals(classHandlers2);

        public static bool operator !=(ClassHandlers classHandlers1, ClassHandlers classHandlers2)
            => !classHandlers1.Equals(classHandlers2);

        internal RoutedEvent RoutedEvent;
        internal RoutedEventHandlerInfoList Handlers;
        internal bool HasSelfHandlers;
    }

    internal class RoutedEventHandlerInfoList
    {
        internal RoutedEventHandlerInfo[] Handlers;
        internal RoutedEventHandlerInfoList Next;

        internal bool Contains(RoutedEventHandlerInfoList handlers)
        {
            RoutedEventHandlerInfoList tempHandlers = this;
            while (tempHandlers != null)
            {
                if (tempHandlers == handlers)
                {
                    return true;
                }
                tempHandlers = tempHandlers.Next;
            }
            return false;
        }
    }
}

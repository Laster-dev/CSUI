// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using MS.Utility;
using MS.Internal;
using MS.Internal.KnownBoxes;
using SR = MS.Internal.PresentationCore.SR;

namespace System.Windows
{
    public sealed class EventRoute
    {
        public EventRoute(RoutedEvent routedEvent)
        {
            ArgumentNullException.ThrowIfNull(routedEvent);

            _routedEvent = routedEvent;
            _routeItemList = new FrugalStructList<RouteItem>(16);
            _sourceItemList = new FrugalStructList<SourceItem>(16);
        }

        public void Add(object target, Delegate handler, bool handledEventsToo)
        {
            ArgumentNullException.ThrowIfNull(target);
            ArgumentNullException.ThrowIfNull(handler);

            RouteItem routeItem = new RouteItem(target, new RoutedEventHandlerInfo(handler, handledEventsToo));
            _routeItemList.Add(routeItem);
        }

        internal void InvokeHandlers(object source, RoutedEventArgs args)
        {
            InvokeHandlersImpl(source, args, false);
        }

        internal void ReInvokeHandlers(object source, RoutedEventArgs args)
        {
            InvokeHandlersImpl(source, args, true);
        }

        private void InvokeHandlersImpl(object source, RoutedEventArgs args, bool reRaised)
        {
            ArgumentNullException.ThrowIfNull(source);
            ArgumentNullException.ThrowIfNull(args);

            if (args.Source == null)
            {
                throw new ArgumentException(SR.SourceNotSet);
            }

            if (args.RoutedEvent != _routedEvent)
            {
                throw new ArgumentException(SR.Mismatched_RoutedEvent);
            }

            if (args.RoutedEvent.RoutingStrategy == RoutingStrategy.Bubble ||
                args.RoutedEvent.RoutingStrategy == RoutingStrategy.Direct)
            {
                int endSourceChangeIndex = 0;

                for (int i = 0; i < _routeItemList.Count; i++)
                {
                    if (i >= endSourceChangeIndex)
                    {
                        object newSource = GetBubbleSource(i, out endSourceChangeIndex);
                        if (!reRaised)
                        {
                            args.Source = newSource ?? source;
                        }
                    }

                    bool traceRoutedEventIsEnabled = TraceRoutedEvent.IsEnabled;
                    if (traceRoutedEventIsEnabled)
                    {
                        TraceRoutedEvent.Trace(
                            TraceEventType.Start,
                            TraceRoutedEvent.InvokeHandlers,
                            _routeItemList[i].Target, args, BooleanBoxes.Box(args.Handled));
                    }

                    _routeItemList[i].InvokeHandler(args);

                    if (traceRoutedEventIsEnabled)
                    {
                        TraceRoutedEvent.Trace(
                            TraceEventType.Stop,
                            TraceRoutedEvent.InvokeHandlers,
                            _routeItemList[i].Target, args, BooleanBoxes.Box(args.Handled));
                    }
                }
            }
            else
            {
                int startSourceChangeIndex = _routeItemList.Count;
                int endTargetIndex = _routeItemList.Count - 1;
                int startTargetIndex;

                while (endTargetIndex >= 0)
                {
                    object currTarget = _routeItemList[endTargetIndex].Target;
                    for (startTargetIndex = endTargetIndex; startTargetIndex >= 0; startTargetIndex--)
                    {
                        if (_routeItemList[startTargetIndex].Target != currTarget)
                        {
                            break;
                        }
                    }

                    for (int i = startTargetIndex + 1; i <= endTargetIndex; i++)
                    {
                        if (i < startSourceChangeIndex)
                        {
                            object newSource = GetTunnelSource(i, out startSourceChangeIndex);
                            args.Source = newSource ?? source;
                        }

                        bool traceRoutedEventIsEnabled = TraceRoutedEvent.IsEnabled;
                        if (traceRoutedEventIsEnabled)
                        {
                            TraceRoutedEvent.Trace(
                                TraceEventType.Start,
                                TraceRoutedEvent.InvokeHandlers,
                                _routeItemList[i].Target, args, BooleanBoxes.Box(args.Handled));
                        }

                        _routeItemList[i].InvokeHandler(args);

                        if (traceRoutedEventIsEnabled)
                        {
                            TraceRoutedEvent.Trace(
                                TraceEventType.Stop,
                                TraceRoutedEvent.InvokeHandlers,
                                _routeItemList[i].Target, args, BooleanBoxes.Box(args.Handled));
                        }
                    }

                    endTargetIndex = startTargetIndex;
                }
            }
        }

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Advanced)]
        public void PushBranchNode(object node, object source)
        {
            BranchNode branchNode = new BranchNode
            {
                Node = node,
                Source = source
            };

            (_branchNodeStack ??= new Stack<BranchNode>(1)).Push(branchNode);
        }

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Advanced)]
        public object PopBranchNode()
        {
            if (_branchNodeStack is { Count: > 0 } stack)
            {
                return stack.Pop().Node;
            }
            return null;
        }

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Advanced)]
        public object PeekBranchNode()
        {
            if (_branchNodeStack is { Count: > 0 } stack)
            {
                return stack.Peek().Node;
            }
            return null;
        }

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Advanced)]
        public object PeekBranchSource()
        {
            if (_branchNodeStack is { Count: > 0 } stack)
            {
                return stack.Peek().Source;
            }
            return null;
        }

        internal RoutedEvent RoutedEvent
        {
            get { return _routedEvent; }
            set { _routedEvent = value; }
        }

        private struct BranchNode
        {
            public object Node;
            public object Source;
        }

        internal void AddSource(object source)
        {
            int startIndex = _routeItemList.Count;
            _sourceItemList.Add(new SourceItem(startIndex, source));
        }

        private object GetBubbleSource(int index, out int endIndex)
        {
            if (_sourceItemList.Count == 0)
            {
                endIndex = _routeItemList.Count;
                return null;
            }

            if (index < _sourceItemList[0].StartIndex)
            {
                endIndex = _sourceItemList[0].StartIndex;
                return null;
            }

            for (int i = 0; i < _sourceItemList.Count - 1; i++)
            {
                if (index >= _sourceItemList[i].StartIndex &&
                    index < _sourceItemList[i + 1].StartIndex)
                {
                    endIndex = _sourceItemList[i + 1].StartIndex;
                    return _sourceItemList[i].Source;
                }
            }

            endIndex = _routeItemList.Count;
            return _sourceItemList[_sourceItemList.Count - 1].Source;
        }

        private object GetTunnelSource(int index, out int startIndex)
        {
            if (_sourceItemList.Count == 0)
            {
                startIndex = 0;
                return null;
            }

            if (index < _sourceItemList[0].StartIndex)
            {
                startIndex = 0;
                return null;
            }

            for (int i = 0; i < _sourceItemList.Count - 1; i++)
            {
                if (index >= _sourceItemList[i].StartIndex &&
                    index < _sourceItemList[i + 1].StartIndex)
                {
                    startIndex = _sourceItemList[i].StartIndex;
                    return _sourceItemList[i].Source;
                }
            }

            startIndex = _sourceItemList[_sourceItemList.Count - 1].StartIndex;
            return _sourceItemList[_sourceItemList.Count - 1].Source;
        }

        internal void Clear()
        {
            _routedEvent = null;
            _routeItemList.Clear();
            _branchNodeStack?.Clear();
            _sourceItemList.Clear();
        }

        private RoutedEvent _routedEvent;
        private FrugalStructList<RouteItem> _routeItemList;
        private Stack<BranchNode> _branchNodeStack;
        private FrugalStructList<SourceItem> _sourceItemList;
    }
}

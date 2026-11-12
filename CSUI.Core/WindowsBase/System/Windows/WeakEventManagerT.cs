// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System;
using System.Collections.Generic;
using System.Windows.Threading;

namespace System.Windows
{
    /// <summary>
    /// Zero-reflection NativeAOT generic weak event manager using strongly-typed subscribe/unsubscribe callbacks.
    /// </summary>
    public class WeakEventManager<TEventSource, TEventArgs> : WeakEventManager
        where TEventSource : class
        where TEventArgs : EventArgs
    {
        private readonly Action<TEventSource, EventHandler<TEventArgs>> _subscribe;
        private readonly Action<TEventSource, EventHandler<TEventArgs>> _unsubscribe;
        private readonly EventHandler<TEventArgs> _handler;

        private WeakEventManager(
            Action<TEventSource, EventHandler<TEventArgs>> subscribe,
            Action<TEventSource, EventHandler<TEventArgs>> unsubscribe)
        {
            _subscribe = subscribe;
            _unsubscribe = unsubscribe;
            _handler = OnEvent;
        }

        public static void AddHandler(
            TEventSource source,
            string eventName,
            Action<TEventSource, EventHandler<TEventArgs>> subscribe,
            Action<TEventSource, EventHandler<TEventArgs>> unsubscribe,
            EventHandler<TEventArgs> handler)
        {
            ArgumentNullException.ThrowIfNull(handler);
            ArgumentNullException.ThrowIfNull(subscribe);
            ArgumentNullException.ThrowIfNull(unsubscribe);

            CurrentManager(eventName, subscribe, unsubscribe).ProtectedAddHandler(source, handler);
        }

        public static void RemoveHandler(
            TEventSource source,
            string eventName,
            EventHandler<TEventArgs> handler)
        {
            ArgumentNullException.ThrowIfNull(handler);

            var manager = GetCurrentManager(typeof(TEventSource), eventName) as WeakEventManager<TEventSource, TEventArgs>;
            manager?.ProtectedRemoveHandler(source, handler);
        }

        private void OnEvent(object sender, TEventArgs e)
        {
            DeliverEvent(sender, e);
        }

        protected override ListenerList NewListenerList() => new ListenerList<TEventArgs>();

        protected override void StartListening(object source)
        {
            if (source is TEventSource typedSource)
            {
                _subscribe(typedSource, _handler);
            }
        }

        protected override void StopListening(object source)
        {
            if (source is TEventSource typedSource)
            {
                _unsubscribe(typedSource, _handler);
            }
        }

        private static WeakEventManager<TEventSource, TEventArgs> CurrentManager(
            string eventName,
            Action<TEventSource, EventHandler<TEventArgs>> subscribe,
            Action<TEventSource, EventHandler<TEventArgs>> unsubscribe)
        {
            var manager = (WeakEventManager<TEventSource, TEventArgs>)GetCurrentManager(typeof(TEventSource), eventName);
            if (manager == null)
            {
                manager = new WeakEventManager<TEventSource, TEventArgs>(subscribe, unsubscribe);
                SetCurrentManager(typeof(TEventSource), eventName, manager);
            }
            return manager;
        }
    }
}

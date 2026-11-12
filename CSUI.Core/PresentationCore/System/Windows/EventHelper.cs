// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System.Threading;

namespace System.Windows
{
    internal static class EventHelper
    {
        public static void AddHandler<T>(ref Tuple<T, Delegate[]> field, T value) where T : Delegate
        {
            while (true)
            {
                Tuple<T, Delegate[]> oldTuple = field;
                T combinedDelegate = (T)Delegate.Combine(oldTuple?.Item1, value);
                Tuple<T, Delegate[]> newTuple = combinedDelegate != null ? Tuple.Create(combinedDelegate, combinedDelegate.GetInvocationList()) : null;
                if (Interlocked.CompareExchange(ref field, newTuple, oldTuple) == oldTuple)
                {
                    break;
                }
            }
        }

        public static void RemoveHandler<T>(ref Tuple<T, Delegate[]> field, T value) where T : Delegate
        {
            while (true)
            {
                Tuple<T, Delegate[]> oldTuple = field;
                T delegateWithRemoval = (T)Delegate.Remove(oldTuple?.Item1, value);
                Tuple<T, Delegate[]> newTuple = delegateWithRemoval != null ? Tuple.Create(delegateWithRemoval, delegateWithRemoval.GetInvocationList()) : null;
                if (Interlocked.CompareExchange(ref field, newTuple, oldTuple) == oldTuple)
                {
                    break;
                }
            }
        }
    }
}

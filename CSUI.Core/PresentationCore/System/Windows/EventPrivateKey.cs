// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

namespace System.Windows
{
    public class EventPrivateKey
    {
        internal int GlobalIndex { get; }

        public EventPrivateKey()
        {
            GlobalIndex = GlobalEventManager.GetNextAvailableGlobalIndex();
        }
    }
}

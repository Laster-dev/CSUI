// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System.Threading;

namespace System.Windows.Media
{
    internal class SafeMILHandleMemoryPressure
    {
        internal SafeMILHandleMemoryPressure(long gcPressure)
        {
            _gcPressure = gcPressure;
            _refCount = 0;
            GC.AddMemoryPressure(_gcPressure);
        }

        internal void AddRef()
        {
            Interlocked.Increment(ref _refCount);
        }

        internal void Release()
        {
            if (Interlocked.Decrement(ref _refCount) == 0 && _gcPressure > 0)
            {
                GC.RemoveMemoryPressure(_gcPressure);
                _gcPressure = 0;
            }
        }

        private long _gcPressure;
        private int _refCount;
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using CSUI.Core.Interop.DirectX;
using Microsoft.Win32.SafeHandles;

namespace System.Windows.Media
{
    internal unsafe class SafeMILHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        internal SafeMILHandle() : base(true)
        {
        }

        internal SafeMILHandle(IntPtr handle) : base(true)
        {
            SetHandle(handle);
        }

        internal void UpdateEstimatedSize(long estimatedSize)
        {
            _gcPressure?.Release();
            _gcPressure = null;

            if (estimatedSize > 0)
            {
                _gcPressure = new SafeMILHandleMemoryPressure(estimatedSize);
                _gcPressure.AddRef();
            }
        }

        internal void CopyMemoryPressure(SafeMILHandle original)
        {
            _gcPressure?.Release();
            _gcPressure = original?._gcPressure;
            _gcPressure?.AddRef();
        }

        internal nint DangerousGetHandleOrZero() => IsInvalid ? 0 : handle;

        protected override bool ReleaseHandle()
        {
            if (handle != IntPtr.Zero)
            {
                ComHelpers.Release((void*)handle);
                handle = IntPtr.Zero;
            }
            _gcPressure?.Release();
            _gcPressure = null;
            return true;
        }

        private SafeMILHandleMemoryPressure _gcPressure;
    }
}

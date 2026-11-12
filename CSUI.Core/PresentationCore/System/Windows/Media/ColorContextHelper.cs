// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System.Runtime.InteropServices;
using CSUI.Core.Interop.DirectX;
using MS.Internal;

namespace System.Windows.Media
{
    /// <summary>
    /// Helper for Win32 MSCms profile handles via NativeLibrary + delegate*.
    /// </summary>
    internal unsafe class ColorContextHelper
    {
        internal ColorContextHelper()
        {
        }

        internal void OpenColorProfile(ref WicNativeExports.PROFILE profile)
        {
            CloseProfileHandle();

            nint hProfile = WicNativeExports.OpenColorProfile(
                ref profile,
                WicConstEx.PROFILE_READ,
                WicConstEx.FILE_SHARE_READ,
                WicConstEx.OPEN_EXISTING);

            if (hProfile != 0 && hProfile != -1)
            {
                _profileHandle = new SafeProfileHandle(hProfile);
            }
            else
            {
                _profileHandle = null;
            }
        }

        internal bool GetColorProfileHeader(out WicNativeExports.PROFILEHEADER header)
        {
            header = default;
            if (IsInvalid)
            {
                return false;
            }

            bool refAdded = false;
            try
            {
                _profileHandle.DangerousAddRef(ref refAdded);
                return WicNativeExports.GetColorProfileHeader(_profileHandle.DangerousGetHandle(), out header);
            }
            finally
            {
                if (refAdded)
                {
                    _profileHandle.DangerousRelease();
                }
            }
        }

        internal void GetColorProfileFromHandle(byte[] buffer, ref uint bufferSize)
        {
            if (IsInvalid)
            {
                throw new InvalidOperationException();
            }

            bool refAdded = false;
            try
            {
                _profileHandle.DangerousAddRef(ref refAdded);
                if (!WicNativeExports.GetColorProfileFromHandle(_profileHandle.DangerousGetHandle(), buffer, ref bufferSize))
                {
                    int hr = Marshal.GetHRForLastWin32Error();
                    if (hr < 0)
                    {
                        HRESULT.Check(hr);
                    }
                    else
                    {
                        throw new InvalidOperationException();
                    }
                }
            }
            finally
            {
                if (refAdded)
                {
                    _profileHandle.DangerousRelease();
                }
            }
        }

        internal void CloseProfileHandle()
        {
            _profileHandle?.Dispose();
            _profileHandle = null;
        }

        internal bool IsInvalid => _profileHandle == null || _profileHandle.IsInvalid;

        internal SafeProfileHandle ProfileHandle => _profileHandle;

        private SafeProfileHandle _profileHandle;
    }
}

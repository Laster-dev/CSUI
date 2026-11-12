// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System;
using System.Runtime.InteropServices;
using CSUI.Core.Interop;

namespace System.Windows.Interop
{
    internal static class OSVersionHelper
    {
        #region Static Properties

        internal static bool IsOsWindows10RS5OrGreater { get; set; }
        internal static bool IsOsWindows10RS4OrGreater { get; set; }
        internal static bool IsOsWindows10RS3OrGreater { get; set; }
        internal static bool IsOsWindows10RS2OrGreater { get; set; }
        internal static bool IsOsWindows10RS1OrGreater { get; set; }
        internal static bool IsOsWindows10TH2OrGreater { get; set; }
        internal static bool IsOsWindows10TH1OrGreater { get; set; }
        internal static bool IsOsWindows10OrGreater { get; set; }
        internal static bool IsOsWindows8Point1OrGreater { get; set; }
        internal static bool IsOsWindows8OrGreater { get; set; }
        internal static bool IsOsWindows7SP1OrGreater { get; set; }
        internal static bool IsOsWindows7OrGreater { get; set; }
        internal static bool IsOsWindowsVistaSP2OrGreater { get; set; }
        internal static bool IsOsWindowsVistaSP1OrGreater { get; set; }
        internal static bool IsOsWindowsVistaOrGreater { get; set; }
        internal static bool IsOsWindowsXPSP3OrGreater { get; set; }
        internal static bool IsOsWindowsXPSP2OrGreater { get; set; }
        internal static bool IsOsWindowsXPSP1OrGreater { get; set; }
        internal static bool IsOsWindowsXPOrGreater { get; set; }
        internal static bool IsOsWindowsServer { get; set; }

        #endregion

        #region Native RtlGetVersion

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private unsafe struct RTL_OSVERSIONINFOEXW
        {
            public uint dwOSVersionInfoSize;
            public uint dwMajorVersion;
            public uint dwMinorVersion;
            public uint dwBuildNumber;
            public uint dwPlatformId;
            public fixed char szCSDVersion[128];
            public ushort wServicePackMajor;
            public ushort wServicePackMinor;
            public ushort wSuiteMask;
            public byte wProductType;
            public byte wReserved;
        }

        private const byte VER_NT_WORKSTATION = 1;

        private static unsafe bool TryGetOsVersion(
            out uint major,
            out uint minor,
            out uint build,
            out ushort spMajor,
            out byte productType)
        {
            try
            {
                nint ntdll = NativeModule.Load("ntdll.dll");
                if (NativeModule.TryGetExport(ntdll, "RtlGetVersion", out nint pRtlGetVersion))
                {
                    var rtlGetVersion = (delegate* unmanaged[Stdcall]<RTL_OSVERSIONINFOEXW*, int>)pRtlGetVersion;
                    RTL_OSVERSIONINFOEXW info = default;
                    info.dwOSVersionInfoSize = (uint)sizeof(RTL_OSVERSIONINFOEXW);
                    if (rtlGetVersion(&info) == 0)
                    {
                        major = info.dwMajorVersion;
                        minor = info.dwMinorVersion;
                        build = info.dwBuildNumber;
                        spMajor = info.wServicePackMajor;
                        productType = info.wProductType;
                        return true;
                    }
                }
            }
            catch
            {
            }

            Version v = Environment.OSVersion.Version;
            major = (uint)Math.Max(0, v.Major);
            minor = (uint)Math.Max(0, v.Minor);
            build = (uint)Math.Max(0, v.Build);
            spMajor = 0;
            productType = VER_NT_WORKSTATION;
            return false;
        }

        private static bool IsVersionOrBuildOrGreater(
            uint curMajor, uint curMinor, uint curBuild, ushort curSpMajor,
            uint reqMajor, uint reqMinor, uint reqBuild, ushort reqSpMajor)
        {
            if (curMajor != reqMajor)
                return curMajor > reqMajor;
            if (curMinor != reqMinor)
                return curMinor > reqMinor;
            if (reqBuild != 0)
                return curBuild >= reqBuild;
            return curSpMajor >= reqSpMajor;
        }

        static OSVersionHelper()
        {
            TryGetOsVersion(out uint major, out uint minor, out uint build, out ushort spMajor, out byte productType);

            IsOsWindows10RS5OrGreater = IsVersionOrBuildOrGreater(major, minor, build, spMajor, 10, 0, 17763, 0);
            IsOsWindows10RS4OrGreater = IsVersionOrBuildOrGreater(major, minor, build, spMajor, 10, 0, 17134, 0);
            IsOsWindows10RS3OrGreater = IsVersionOrBuildOrGreater(major, minor, build, spMajor, 10, 0, 16299, 0);
            IsOsWindows10RS2OrGreater = IsVersionOrBuildOrGreater(major, minor, build, spMajor, 10, 0, 15063, 0);
            IsOsWindows10RS1OrGreater = IsVersionOrBuildOrGreater(major, minor, build, spMajor, 10, 0, 14393, 0);
            IsOsWindows10TH2OrGreater = IsVersionOrBuildOrGreater(major, minor, build, spMajor, 10, 0, 10586, 0);
            IsOsWindows10TH1OrGreater = IsVersionOrBuildOrGreater(major, minor, build, spMajor, 10, 0, 10240, 0);
            IsOsWindows10OrGreater    = IsVersionOrBuildOrGreater(major, minor, build, spMajor, 10, 0, 0, 0);
            IsOsWindows8Point1OrGreater = IsVersionOrBuildOrGreater(major, minor, build, spMajor, 6, 3, 0, 0);
            IsOsWindows8OrGreater     = IsVersionOrBuildOrGreater(major, minor, build, spMajor, 6, 2, 0, 0);
            IsOsWindows7SP1OrGreater  = IsVersionOrBuildOrGreater(major, minor, build, spMajor, 6, 1, 0, 1);
            IsOsWindows7OrGreater     = IsVersionOrBuildOrGreater(major, minor, build, spMajor, 6, 1, 0, 0);
            IsOsWindowsVistaSP2OrGreater = IsVersionOrBuildOrGreater(major, minor, build, spMajor, 6, 0, 0, 2);
            IsOsWindowsVistaSP1OrGreater = IsVersionOrBuildOrGreater(major, minor, build, spMajor, 6, 0, 0, 1);
            IsOsWindowsVistaOrGreater = IsVersionOrBuildOrGreater(major, minor, build, spMajor, 6, 0, 0, 0);
            IsOsWindowsXPSP3OrGreater = IsVersionOrBuildOrGreater(major, minor, build, spMajor, 5, 1, 0, 3);
            IsOsWindowsXPSP2OrGreater = IsVersionOrBuildOrGreater(major, minor, build, spMajor, 5, 1, 0, 2);
            IsOsWindowsXPSP1OrGreater = IsVersionOrBuildOrGreater(major, minor, build, spMajor, 5, 1, 0, 1);
            IsOsWindowsXPOrGreater    = IsVersionOrBuildOrGreater(major, minor, build, spMajor, 5, 1, 0, 0);
            IsOsWindowsServer         = productType != VER_NT_WORKSTATION;
        }

        #endregion

        #region Helper Methods

        internal static bool IsOsVersionOrGreater(OperatingSystemVersion osVer)
        {
            switch (osVer)
            {
                case OperatingSystemVersion.Windows10RS5:
                    return IsOsWindows10RS5OrGreater;
                case OperatingSystemVersion.Windows10RS4:
                    return IsOsWindows10RS4OrGreater;
                case OperatingSystemVersion.Windows10RS3:
                    return IsOsWindows10RS3OrGreater;
                case OperatingSystemVersion.Windows10RS2:
                    return IsOsWindows10RS2OrGreater;
                case OperatingSystemVersion.Windows10RS1:
                    return IsOsWindows10RS1OrGreater;
                case OperatingSystemVersion.Windows10TH2:
                    return IsOsWindows10TH2OrGreater;
                case OperatingSystemVersion.Windows10:
                    return IsOsWindows10OrGreater;
                case OperatingSystemVersion.Windows8Point1:
                    return IsOsWindows8Point1OrGreater;
                case OperatingSystemVersion.Windows8:
                    return IsOsWindows8OrGreater;
                case OperatingSystemVersion.Windows7SP1:
                    return IsOsWindows7SP1OrGreater;
                case OperatingSystemVersion.Windows7:
                    return IsOsWindows7OrGreater;
                case OperatingSystemVersion.WindowsVistaSP2:
                    return IsOsWindowsVistaSP2OrGreater;
                case OperatingSystemVersion.WindowsVistaSP1:
                    return IsOsWindowsVistaSP1OrGreater;
                case OperatingSystemVersion.WindowsVista:
                    return IsOsWindowsVistaOrGreater;
                case OperatingSystemVersion.WindowsXPSP3:
                    return IsOsWindowsXPSP3OrGreater;
                case OperatingSystemVersion.WindowsXPSP2:
                    return IsOsWindowsXPSP2OrGreater;
            }

            return false;
        }

        internal static OperatingSystemVersion GetOsVersion()
        {
            if (IsOsWindows10RS5OrGreater)
                return OperatingSystemVersion.Windows10RS5;
            if (IsOsWindows10RS4OrGreater)
                return OperatingSystemVersion.Windows10RS4;
            if (IsOsWindows10RS3OrGreater)
                return OperatingSystemVersion.Windows10RS3;
            if (IsOsWindows10RS2OrGreater)
                return OperatingSystemVersion.Windows10RS2;
            if (IsOsWindows10RS1OrGreater)
                return OperatingSystemVersion.Windows10RS1;
            if (IsOsWindows10TH2OrGreater)
                return OperatingSystemVersion.Windows10TH2;
            if (IsOsWindows10OrGreater)
                return OperatingSystemVersion.Windows10;
            if (IsOsWindows8Point1OrGreater)
                return OperatingSystemVersion.Windows8Point1;
            if (IsOsWindows8OrGreater)
                return OperatingSystemVersion.Windows8;
            if (IsOsWindows7SP1OrGreater)
                return OperatingSystemVersion.Windows7SP1;
            if (IsOsWindows7OrGreater)
                return OperatingSystemVersion.Windows7;
            if (IsOsWindowsVistaSP2OrGreater)
                return OperatingSystemVersion.WindowsVistaSP2;
            if (IsOsWindowsVistaSP1OrGreater)
                return OperatingSystemVersion.WindowsVistaSP1;
            if (IsOsWindowsVistaOrGreater)
                return OperatingSystemVersion.WindowsVista;
            if (IsOsWindowsXPSP3OrGreater)
                return OperatingSystemVersion.WindowsXPSP3;
            if (IsOsWindowsXPSP2OrGreater)
                return OperatingSystemVersion.WindowsXPSP2;

            throw new Exception("OSVersionHelper.GetOsVersion: Could not get OS version");
        }

        #endregion
    }
}

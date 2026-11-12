// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System;
using System.Runtime.InteropServices;

namespace MS.Internal.WindowsRuntime
{
    namespace Windows.UI.ViewManagement
    {
        /// <summary>
        /// Zero-ComImport NativeAOT WinRT vtable bindings for invoking UISettings.
        /// </summary>
        internal static unsafe class UISettingsRCW
        {
            private static readonly Guid IID_IUISettings3 = new("03021BE4-5254-4781-8194-5168F7D06D7B");

            private static readonly delegate* unmanaged[Stdcall]<char*, int, IntPtr*, int> s_windowsCreateString =
                LoadWinRtString<char, IntPtr>("WindowsCreateString");

            private static readonly delegate* unmanaged[Stdcall]<IntPtr, int> s_windowsDeleteString =
                LoadWinRtStringDelete();

            private static readonly delegate* unmanaged[Stdcall]<IntPtr, IntPtr*, int> s_roActivateInstance =
                LoadRoActivateInstance();

            private static delegate* unmanaged[Stdcall]<char*, int, IntPtr*, int> LoadWinRtString<T1, T2>(string name)
            {
                if (!NativeLibrary.TryLoad("api-ms-win-core-winrt-string-l1-1-0.dll", out IntPtr mod))
                {
                    return null;
                }

                return NativeLibrary.TryGetExport(mod, name, out IntPtr addr)
                    ? (delegate* unmanaged[Stdcall]<char*, int, IntPtr*, int>)addr
                    : null;
            }

            private static delegate* unmanaged[Stdcall]<IntPtr, int> LoadWinRtStringDelete()
            {
                if (!NativeLibrary.TryLoad("api-ms-win-core-winrt-string-l1-1-0.dll", out IntPtr mod))
                {
                    return null;
                }

                return NativeLibrary.TryGetExport(mod, "WindowsDeleteString", out IntPtr addr)
                    ? (delegate* unmanaged[Stdcall]<IntPtr, int>)addr
                    : null;
            }

            private static delegate* unmanaged[Stdcall]<IntPtr, IntPtr*, int> LoadRoActivateInstance()
            {
                if (!NativeLibrary.TryLoad("api-ms-win-core-winrt-l1-1-0.dll", out IntPtr mod))
                {
                    return null;
                }

                return NativeLibrary.TryGetExport(mod, "RoActivateInstance", out IntPtr addr)
                    ? (delegate* unmanaged[Stdcall]<IntPtr, IntPtr*, int>)addr
                    : null;
            }

            public static IntPtr GetUISettings3Pointer()
            {
                if (s_windowsCreateString == null || s_windowsDeleteString == null || s_roActivateInstance == null)
                {
                    return IntPtr.Zero;
                }

                const string typeName = "Windows.UI.ViewManagement.UISettings";
                IntPtr hstring = IntPtr.Zero;
                int hr;
                fixed (char* pTypeName = typeName)
                {
                    hr = s_windowsCreateString(pTypeName, typeName.Length, &hstring);
                }

                if (hr < 0 || hstring == IntPtr.Zero)
                {
                    return IntPtr.Zero;
                }

                IntPtr inspectable = IntPtr.Zero;
                try
                {
                    hr = s_roActivateInstance(hstring, &inspectable);
                    if (hr < 0 || inspectable == IntPtr.Zero)
                    {
                        return IntPtr.Zero;
                    }

                    Guid iid = IID_IUISettings3;
                    void** vtbl = *(void***)inspectable;
                    var queryInterface = (delegate* unmanaged[Stdcall]<IntPtr, Guid*, IntPtr*, int>)vtbl[0];
                    IntPtr uiSettings3 = IntPtr.Zero;
                    hr = queryInterface(inspectable, &iid, &uiSettings3);
                    if (hr < 0)
                    {
                        return IntPtr.Zero;
                    }

                    return uiSettings3;
                }
                finally
                {
                    if (inspectable != IntPtr.Zero)
                    {
                        Release(inspectable);
                    }

                    _ = s_windowsDeleteString(hstring);
                }
            }

            public static int TryGetColorValue(IntPtr uiSettings3, UIColorType desiredColor, out UIColor color)
            {
                color = default;
                if (uiSettings3 == IntPtr.Zero)
                {
                    return unchecked((int)0x80004005); // E_FAIL
                }

                void** vtbl = *(void***)uiSettings3;
                // IUnknown (0..2), IInspectable (3..5), IUISettings3::GetColorValue (6)
                var getColorValue = (delegate* unmanaged[Stdcall]<IntPtr, UIColorType, UIColor*, int>)vtbl[6];
                fixed (UIColor* pColor = &color)
                {
                    return getColorValue(uiSettings3, desiredColor, pColor);
                }
            }

            public static void Release(IntPtr unk)
            {
                if (unk != IntPtr.Zero)
                {
                    void** vtbl = *(void***)unk;
                    var release = (delegate* unmanaged[Stdcall]<IntPtr, uint>)vtbl[2];
                    _ = release(unk);
                }
            }

            public enum UIColorType
            {
                Background = 0,
                Foreground = 1,
                AccentDark3 = 2,
                AccentDark2 = 3,
                AccentDark1 = 4,
                Accent = 5,
                AccentLight1 = 6,
                AccentLight2 = 7,
                AccentLight3 = 8,
                Complement = 9
            }

            [StructLayout(LayoutKind.Sequential)]
            internal readonly record struct UIColor(byte A, byte R, byte G, byte B);
        }
    }
}

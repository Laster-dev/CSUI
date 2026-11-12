// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using MS.Win32;

namespace MS.Utility
{
    internal partial class DpiAwarenessContextHandle
    {
        private static class DpiUtil
        {
            private static bool IsAreDpiAwarenessContextsEqualMethodSupported { get; set; } = true;

            internal static bool AreDpiAwarenessContextsEqual(IntPtr dpiContextA, IntPtr dpiContextB)
            {
                if (IsAreDpiAwarenessContextsEqualMethodSupported)
                {
                    try
                    {
                        return SafeNativeMethods.AreDpiAwarenessContextsEqual(dpiContextA, dpiContextB);
                    }
                    catch (Exception e) when (e is EntryPointNotFoundException || e is MissingMethodException || e is DllNotFoundException)
                    {
                        IsAreDpiAwarenessContextsEqualMethodSupported = false;
                    }
                }

                return AreDpiAwarenessContextsTriviallyEqual(dpiContextA, dpiContextB);
            }

            private static bool AreDpiAwarenessContextsTriviallyEqual(IntPtr dpiContextA, IntPtr dpiContextB)
            {
                return dpiContextA == dpiContextB;
            }
        }
    }
}

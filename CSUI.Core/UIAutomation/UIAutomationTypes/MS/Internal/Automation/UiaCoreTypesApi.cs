// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System;

namespace MS.Internal.Automation
{
    internal static class UiaCoreTypesApi
    {
        internal enum AutomationIdType
        {
            Property,
            Pattern,
            Event,
            ControlType,
            TextAttribute
        }

        internal const int UIA_E_ELEMENTNOTENABLED = unchecked((int)0x80040200);
        internal const int UIA_E_ELEMENTNOTAVAILABLE = unchecked((int)0x80040201);
        internal const int UIA_E_NOCLICKABLEPOINT = unchecked((int)0x80040202);
        internal const int UIA_E_PROXYASSEMBLYNOTLOADED = unchecked((int)0x80040203);

        private static readonly object s_notSupportedValue = new object();
        private static readonly object s_mixedAttributeValue = new object();

        internal static int UiaLookupId(AutomationIdType type, ref Guid guid)
        {
            return 0;
        }

        internal static object UiaGetReservedNotSupportedValue()
        {
            return s_notSupportedValue;
        }

        internal static object UiaGetReservedMixedAttributeValue()
        {
            return s_mixedAttributeValue;
        }

        internal static bool SupportsWin7Identifiers()
        {
            return true;
        }
    }
}

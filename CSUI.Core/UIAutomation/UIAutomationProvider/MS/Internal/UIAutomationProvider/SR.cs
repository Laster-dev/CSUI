// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System.Globalization;

namespace MS.Internal.UIAutomationProvider
{
    internal static class SR
    {
        internal const string GenericInvalidArgument = "The argument '{0}' was of an invalid type or had an invalid value.";
        internal const string HwndMustBeNonNULL = "hwnd must not be IntPtr.Zero (0) or null.";

        internal static string GetResourceString(string resourceKey, string defaultString = null)
        {
            return resourceKey switch
            {
                nameof(GenericInvalidArgument) => GenericInvalidArgument,
                nameof(HwndMustBeNonNULL) => HwndMustBeNonNULL,
                _ => defaultString ?? resourceKey ?? string.Empty,
            };
        }

        internal static string Format(string resourceFormat, params object[] args)
        {
            return args == null ? resourceFormat : string.Format(CultureInfo.CurrentCulture, resourceFormat, args);
        }
    }
}

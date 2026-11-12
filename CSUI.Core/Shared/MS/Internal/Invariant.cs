// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System;
using MS.Internal.WindowsBase;

namespace MS.Internal
{
    internal static class Invariant
    {
        static Invariant()
        {
            _strict = _strictDefaultValue;
        }

        internal static void Assert(bool condition)
        {
            if (!condition)
            {
                FailFast(message: null, detailMessage: null);
            }
        }

        internal static void AssertNotNull([System.Diagnostics.CodeAnalysis.NotNull] object value)
        {
            if (value is null)
            {
                FailFast("Value should not be null", null);
            }
        }

        internal static void Assert(bool condition, string invariantMessage)
        {
            if (!condition)
            {
                FailFast(invariantMessage, null);
            }
        }

        internal static void Assert(bool condition, string invariantMessage, string detailMessage)
        {
            if (!condition)
            {
                FailFast(invariantMessage, detailMessage);
            }
        }

        internal static bool Strict
        {
            get { return _strict; }
            set { _strict = value; }
        }

        [System.Diagnostics.CodeAnalysis.DoesNotReturn]
        private static void FailFast(string message, string detailMessage)
        {
            if (Debugger.IsAttached)
            {
                Debugger.Break();
            }

            Debug.Fail($"Invariant failure: {message}", detailMessage);
            Environment.FailFast(SR.InvariantFailure);
        }

        private static bool _strict;

        private const bool _strictDefaultValue
#if DEBUG
            = true;
#else
            = false;
#endif
    }
}

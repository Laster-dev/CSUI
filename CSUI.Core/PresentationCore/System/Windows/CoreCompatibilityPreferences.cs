// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using SR = MS.Internal.PresentationCore.SR;

namespace System.Windows
{
    public static class CoreCompatibilityPreferences
    {
        static CoreCompatibilityPreferences()
        {
        }

        internal static bool TargetsAtLeast_Desktop_V4_5 => true;

        private static bool _isAltKeyRequiredInAccessKeyDefaultScope = false;

        public static bool IsAltKeyRequiredInAccessKeyDefaultScope
        {
            get { return _isAltKeyRequiredInAccessKeyDefaultScope; }
            set
            {
                lock (_lockObject)
                {
                    if (_isSealed)
                    {
                        throw new InvalidOperationException(SR.Format(SR.CompatibilityPreferencesSealed, "IsAltKeyRequiredInAccessKeyDefaultScope", "CoreCompatibilityPreferences"));
                    }

                    _isAltKeyRequiredInAccessKeyDefaultScope = value;
                }
            }
        }

        internal static bool GetIsAltKeyRequiredInAccessKeyDefaultScope()
        {
            Seal();
            return IsAltKeyRequiredInAccessKeyDefaultScope;
        }

        private static bool _includeAllInkInBoundingBox = true;

        internal static bool IncludeAllInkInBoundingBox
        {
            get { return _includeAllInkInBoundingBox; }
            set
            {
                lock (_lockObject)
                {
                    if (_isSealed)
                    {
                        throw new InvalidOperationException(SR.Format(SR.CompatibilityPreferencesSealed, "IncludeAllInkInBoundingBox", "CoreCompatibilityPreferences"));
                    }

                    _includeAllInkInBoundingBox = value;
                }
            }
        }

        internal static bool GetIncludeAllInkInBoundingBox()
        {
            Seal();
            return IncludeAllInkInBoundingBox;
        }

        private static bool? _enableMultiMonitorDisplayClipping = null;

        public static bool? EnableMultiMonitorDisplayClipping
        {
            get { return GetEnableMultiMonitorDisplayClipping(); }
            set
            {
                lock (_lockObject)
                {
                    if (_isSealed)
                    {
                        throw new InvalidOperationException(SR.Format(SR.CompatibilityPreferencesSealed, "DisableMultimonDisplayClipping", "CoreCompatibilityPreferences"));
                    }

                    _enableMultiMonitorDisplayClipping = value;
                }
            }
        }

        internal static bool? GetEnableMultiMonitorDisplayClipping()
        {
            Seal();
            return _enableMultiMonitorDisplayClipping;
        }

        private static void Seal()
        {
            if (!_isSealed)
            {
                lock (_lockObject)
                {
                    _isSealed = true;
                }
            }
        }

        private static bool _isSealed;
        private static readonly object _lockObject = new object();
    }
}

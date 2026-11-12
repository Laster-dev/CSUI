// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System.Runtime.CompilerServices;
using System.Collections.Generic;
using System.Threading;

namespace System
{
    internal static class LocalAppContext
    {
        private static readonly Dictionary<string, bool> s_switchMap = new();
        private static readonly Lock s_syncLock = new();

        private static bool DisableCaching { get; set; }

        static LocalAppContext()
        {
            AppContextDefaultValues.PopulateDefaultValues();
            DisableCaching = IsSwitchEnabled(@"TestSwitch.LocalAppContext.DisableCaching");
        }

        public static bool IsSwitchEnabled(string switchName)
        {
            if (AppContext.TryGetSwitch(switchName, out bool isEnabledCentrally))
            {
                return isEnabledCentrally;
            }

            return IsSwitchEnabledLocal(switchName);
        }

        private static bool IsSwitchEnabledLocal(string switchName)
        {
            bool isEnabled, isPresent;
            lock (s_syncLock)
            {
                isPresent = s_switchMap.TryGetValue(switchName, out isEnabled);
            }

            return isPresent && isEnabled;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool GetCachedSwitchValue(string switchName, ref int switchValue)
        {
            if (switchValue < 0)
                return false;
            if (switchValue > 0)
                return true;

            return GetCachedSwitchValueInternal(switchName, ref switchValue);
        }

        private static bool GetCachedSwitchValueInternal(string switchName, ref int switchValue)
        {
            if (DisableCaching)
            {
                return IsSwitchEnabled(switchName);
            }

            bool isEnabled = IsSwitchEnabled(switchName);
            switchValue = isEnabled ? 1 : -1;
            return isEnabled;
        }

        internal static void DefineSwitchDefault(string switchName, bool initialValue)
        {
            s_switchMap[switchName] = initialValue;
        }
    }
}

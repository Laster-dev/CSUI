// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System;
using System.Windows.Automation;
using System.Windows.Automation.Provider;

namespace MS.Internal.Automation
{
    internal static class UiaCoreProviderApi
    {
        private const int UIA_E_ELEMENTNOTAVAILABLE = unchecked((int)0x80040201);

        internal static IntPtr UiaReturnRawElementProvider(IntPtr hwnd, IntPtr wParam, IntPtr lParam, IRawElementProviderSimple el)
        {
            return IntPtr.Zero;
        }

        internal static IRawElementProviderSimple UiaHostProviderFromHwnd(IntPtr hwnd)
        {
            return null;
        }

        internal static void UiaRaiseAutomationPropertyChangedEvent(IRawElementProviderSimple provider, int propertyId, object oldValue, object newValue)
        {
        }

        internal static void UiaRaiseAutomationEvent(IRawElementProviderSimple provider, int eventId)
        {
        }

        internal static void UiaRaiseStructureChangedEvent(IRawElementProviderSimple provider, StructureChangeType structureChangeType, int[] runtimeId)
        {
        }

        internal static void UiaRaiseAsyncContentLoadedEvent(IRawElementProviderSimple provider, AsyncContentLoadedState asyncContentLoadedState, double PercentComplete)
        {
        }

        internal static void UiaRaiseNotificationEvent(
            IRawElementProviderSimple provider,
            AutomationNotificationKind notificationKind,
            AutomationNotificationProcessing notificationProcessing,
            string displayString,
            string activityId)
        {
        }

        internal static void UiaRaiseActiveTextPositionChangedEvent(IRawElementProviderSimple provider, ITextRangeProvider textRange)
        {
        }

        internal static bool UiaClientsAreListening()
        {
            return false;
        }
    }
}

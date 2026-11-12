// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using MS.Internal;

namespace System
{
    internal static partial class AppContextDefaultValues
    {
        public static void PopulateDefaultValues()
        {
            // WindowsBase
            LocalAppContext.DefineSwitchDefault(BaseAppContextSwitches.SwitchDoNotUseCulturePreservingDispatcherOperations, false);
            LocalAppContext.DefineSwitchDefault(BaseAppContextSwitches.SwitchUseSha1AsDefaultHashAlgorithmForDigitalSignatures, false);
            LocalAppContext.DefineSwitchDefault(BaseAppContextSwitches.SwitchDoNotInvokeInWeakEventTableShutdownListener, false);
            LocalAppContext.DefineSwitchDefault(BaseAppContextSwitches.SwitchEnableCleanupSchedulingImprovements, false);
            LocalAppContext.DefineSwitchDefault(BaseAppContextSwitches.SwitchEnableWeakEventMemoryImprovements, false);

            // AccessibilitySwitches
            LocalAppContext.DefineSwitchDefault("Switch.UseLegacyAccessibilityFeatures", false);
            LocalAppContext.DefineSwitchDefault("Switch.UseLegacyAccessibilityFeatures.2", false);
            LocalAppContext.DefineSwitchDefault("Switch.UseLegacyAccessibilityFeatures.3", false);
            LocalAppContext.DefineSwitchDefault("Switch.UseLegacyToolTipDisplay", false);
            LocalAppContext.DefineSwitchDefault("Switch.System.Windows.Controls.ItemsControlDoesNotSupportAutomation", false);

            // PresentationCore
            LocalAppContext.DefineSwitchDefault(CoreAppContextSwitches.DoNotScaleForDpiChangesSwitchName, false);
            LocalAppContext.DefineSwitchDefault(CoreAppContextSwitches.OverrideExceptionWithNullReferenceExceptionName, false);
            LocalAppContext.DefineSwitchDefault(CoreAppContextSwitches.DoNotUsePresentationDpiCapabilityTier2OrGreaterSwitchName, false);
            LocalAppContext.DefineSwitchDefault(CoreAppContextSwitches.DisableStylusAndTouchSupportSwitchName, false);
            LocalAppContext.DefineSwitchDefault(CoreAppContextSwitches.EnablePointerSupportSwitchName, false);
            LocalAppContext.DefineSwitchDefault(CoreAppContextSwitches.DisableDiagnosticsSwitchName, false);
            LocalAppContext.DefineSwitchDefault(CoreAppContextSwitches.AllowChangesDuringVisualTreeChangedSwitchName, false);
            LocalAppContext.DefineSwitchDefault(CoreAppContextSwitches.DisableImplicitTouchKeyboardInvocationSwitchName, false);
            LocalAppContext.DefineSwitchDefault(CoreAppContextSwitches.ShouldRenderEvenWhenNoDisplayDevicesAreAvailableSwitchName, false);
            LocalAppContext.DefineSwitchDefault(CoreAppContextSwitches.ShouldNotRenderInNonInteractiveWindowStationSwitchName, false);
            LocalAppContext.DefineSwitchDefault(CoreAppContextSwitches.DoNotUsePresentationDpiCapabilityTier3OrGreaterSwitchName, false);
            LocalAppContext.DefineSwitchDefault(CoreAppContextSwitches.AllowExternalProcessToBlockAccessToTemporaryFilesSwitchName, false);
            LocalAppContext.DefineSwitchDefault(CoreAppContextSwitches.EnableHardwareAccelerationInRdpSwitchName, false);
            LocalAppContext.DefineSwitchDefault(CoreAppContextSwitches.DisableSpecialCharacterLigatureSwitchName, false);
            LocalAppContext.DefineSwitchDefault(CoreAppContextSwitches.DisableCmapAndSbitOverflowProtectionSwitchName, false);
            LocalAppContext.DefineSwitchDefault(CoreAppContextSwitches.DoNotApplyZoneCheckForDefaultCredentialsSwitchName, false);
            LocalAppContext.DefineSwitchDefault(CoreAppContextSwitches.DisableXpsPackageBoundaryRestrictionSwitchName, false);
            LocalAppContext.DefineSwitchDefault(CoreAppContextSwitches.DisableFontSecurityProtectionSwitchName, false);

            // PresentationFramework
            LocalAppContext.DefineSwitchDefault(FrameworkAppContextSwitches.UseAdornerForTextboxSelectionRenderingSwitchName, true);
            LocalAppContext.DefineSwitchDefault(FrameworkAppContextSwitches.DoNotApplyLayoutRoundingToMarginsAndBorderThicknessSwitchName, false);
            LocalAppContext.DefineSwitchDefault(FrameworkAppContextSwitches.GridStarDefinitionsCanExceedAvailableSpaceSwitchName, false);
            LocalAppContext.DefineSwitchDefault(FrameworkAppContextSwitches.SelectionPropertiesCanLagBehindSelectionChangedEventSwitchName, false);
            LocalAppContext.DefineSwitchDefault(FrameworkAppContextSwitches.DoNotUseFollowParentWhenBindingToADODataRelationSwitchName, false);
            LocalAppContext.DefineSwitchDefault(FrameworkAppContextSwitches.IListIndexerHidesCustomIndexerSwitchName, false);
            LocalAppContext.DefineSwitchDefault(FrameworkAppContextSwitches.AppendLocalAssemblyVersionForSourceUriSwitchName, false);
            LocalAppContext.DefineSwitchDefault(FrameworkAppContextSwitches.KeyboardNavigationFromHyperlinkInItemsControlIsNotRelativeToFocusedElementSwitchName, false);
            LocalAppContext.DefineSwitchDefault(FrameworkAppContextSwitches.ItemAutomationPeerKeepsItsItemAliveSwitchName, false);
            LocalAppContext.DefineSwitchDefault(FrameworkAppContextSwitches.DisableFluentThemeWindowBackdropSwitchName, false);
            LocalAppContext.DefineSwitchDefault(FrameworkAppContextSwitches.DisableDynamicResourceOptimizationSwitchName, false);
            LocalAppContext.DefineSwitchDefault(FrameworkAppContextSwitches.DisableDWMCrashContainmentSwitchName, false);
        }
    }
}

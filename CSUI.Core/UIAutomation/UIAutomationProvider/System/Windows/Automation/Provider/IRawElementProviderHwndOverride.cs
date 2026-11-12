#nullable disable
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Description: Provider interface that allows for overriding HWND-based UI



namespace System.Windows.Automation.Provider
{
    /// <summary>
    /// Implemented by providers which want to provide information about or want to
    /// reposition contained HWND-based elements.
    /// </summary>
#if (NO_INTERNAL_COMPILE_BUG1080665)
    internal interface IRawElementProviderHwndOverride : IRawElementProviderSimple
#else
    public interface IRawElementProviderHwndOverride : IRawElementProviderSimple
#endif
    {
        /// <summary>
        /// Request a provider for the specified component. The returned provider can supply additional
        /// properties or override properties of the specified component.
        /// </summary>
        /// <param name="hwnd">The window handle of the component</param>
        /// <returns>Return the provider for the specified component, or null if the component is not being overridden</returns>
        IRawElementProviderSimple GetOverrideProviderForHwnd( IntPtr hwnd );
    }
}

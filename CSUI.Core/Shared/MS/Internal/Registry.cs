// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using Microsoft.Win32;

namespace MS.Internal
{
    internal static class RegistryKeys
    {
        internal const string
            WPF = @"Software\Microsoft\.NETFramework\Windows Presentation Foundation",

            WPF_Features = WPF + "\\Features",
                value_AutomationWeakReferenceDisallow = "AutomationWeakReferenceDisallow",

            HKCU_XpsViewer = @"HKEY_CURRENT_USER\Software\Microsoft\XPSViewer",
                value_IsolatedStorageUserQuota = "IsolatedStorageUserQuota",
            HKLM_XpsViewerLocalServer32 = "HKEY_LOCAL_MACHINE\\SOFTWARE\\Classes\\CLSID\\{7DDA204B-2097-47C9-8323-C40BB840AE44}\\LocalServer32",

            HKLM_IetfLanguage = @"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Nls\IetfLanguage",
            FRAMEWORK_RegKey = @"Software\Microsoft\Net Framework Setup\NDP\v4\Client\",
            FRAMEWORK_RegKey_FullPath = @"HKEY_LOCAL_MACHINE\" + FRAMEWORK_RegKey,
            FRAMEWORK_InstallPath_RegValue = "InstallPath";

        internal static bool ReadLocalMachineBool(string key, string valueName)
        {
            if (!OperatingSystem.IsWindows())
            {
                return false;
            }

            string keyPath = "HKEY_LOCAL_MACHINE\\" + key;
            object value = Registry.GetValue(keyPath, valueName, null);
            return value is int intVal && intVal != 0;
        }
    }
}

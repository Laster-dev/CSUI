// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// ReSharper disable once CheckNamespace
#nullable disable
using SR = MS.Internal.PresentationFramework.SR;

namespace System.Windows;

/// <summary>
/// Specifies the Windows 11 DWM system backdrop material to render behind the window.
/// </summary>
public enum WindowBackdropType
{
    /// <summary>System chooses default based on system policy.</summary>
    Auto = 0,

    /// <summary>No backdrop material (standard opaque/classic window).</summary>
    None = 1,

    /// <summary>Windows 11 Mica material (ideal for long-lived top-level main windows).</summary>
    MainWindow = 2,

    /// <summary>Windows 11 Acrylic material (ideal for popups, context menus, transient dialogs).</summary>
    TransientWindow = 3,

    /// <summary>Windows 11 Mica Alt material (ideal for tabbed documents or content-heavy shells).</summary>
    TabbedWindow = 4,

    /// <summary>Alias for MainWindow (Mica material).</summary>
    Mica = MainWindow,

    /// <summary>Alias for TransientWindow (Acrylic material).</summary>
    Acrylic = TransientWindow,

    /// <summary>Alias for TabbedWindow (Mica Alt material).</summary>
    MicaAlt = TabbedWindow
}

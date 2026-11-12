// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Navigation;

namespace MS.Internal.PresentationFramework
{
    /// <summary>
    /// Wires PresentationFramework delegates into PresentationCore hooks and registers
    /// AOT-safe factory delegates for core WPF foundation elements used in templates.
    /// </summary>
    internal static class PresentationFrameworkHooks
    {
#pragma warning disable CA2255
        [ModuleInitializer]
        internal static void Initialize()
#pragma warning restore CA2255
        {
            BaseUriHelper.GetLogicalParentHook = static element =>
                element != null ? LogicalTreeHelper.GetParent(element) : null;

            FrameworkElementFactory.RegisterType<UIElement>();
            FrameworkElementFactory.RegisterType<FrameworkElement>();
            FrameworkElementFactory.RegisterType<Control>();
            FrameworkElementFactory.RegisterType<ContentControl>();
            FrameworkElementFactory.RegisterType<ContentPresenter>();
            FrameworkElementFactory.RegisterType<Decorator>();
            FrameworkElementFactory.RegisterType<AdornerDecorator>();
            FrameworkElementFactory.RegisterType<Window>();
        }
    }
}

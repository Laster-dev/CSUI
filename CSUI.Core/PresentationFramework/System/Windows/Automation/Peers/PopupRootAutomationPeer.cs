// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System.Windows.Controls.Primitives;

namespace System.Windows.Automation.Peers
{
    internal class PopupRootAutomationPeer : FrameworkElementAutomationPeer
    {
        public PopupRootAutomationPeer(PopupRoot owner) : base(owner)
        {
        }

        protected override string GetClassNameCore() => "Popup";

        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Window;
    }
}

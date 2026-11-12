// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System.Windows.Interop;

namespace System.Windows.Automation.Peers
{
    public class HwndHostAutomationPeer : FrameworkElementAutomationPeer
    {
        public HwndHostAutomationPeer(HwndHost owner) : base(owner)
        {
        }

        protected override string GetClassNameCore() => "HwndHost";

        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Pane;

        protected override bool IsControlElementCore() => true;
    }
}

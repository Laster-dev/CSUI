// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System.Windows.Controls;

namespace System.Windows.Automation.Peers
{
    public class InkPresenterAutomationPeer : FrameworkElementAutomationPeer
    {
        public InkPresenterAutomationPeer(InkPresenter owner) : base(owner)
        {
        }

        protected override string GetClassNameCore() => "InkPresenter";

        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Custom;
    }

    public class InkCanvasAutomationPeer : FrameworkElementAutomationPeer
    {
        public InkCanvasAutomationPeer(InkCanvas owner) : base(owner)
        {
        }

        protected override string GetClassNameCore() => "InkCanvas";

        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Custom;
    }
}

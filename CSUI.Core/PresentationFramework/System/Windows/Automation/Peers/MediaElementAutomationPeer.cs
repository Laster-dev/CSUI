// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System.Windows.Controls;

namespace System.Windows.Automation.Peers
{
    public class MediaElementAutomationPeer : FrameworkElementAutomationPeer
    {
        public MediaElementAutomationPeer(MediaElement owner) : base(owner)
        {
        }

        protected override string GetClassNameCore() => "MediaElement";

        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Custom;
    }
}

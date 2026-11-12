// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Windows.Controls;

#nullable disable

namespace System.Windows.Automation.Peers
{
    ///
    public class GroupBoxAutomationPeer : FrameworkElementAutomationPeer
    {
        ///
        public GroupBoxAutomationPeer(GroupBox owner): base(owner)
        {}
    
        ///
        protected override string GetClassNameCore()
        {
            return "GroupBox";
        }

        ///
        protected override AutomationControlType GetAutomationControlTypeCore()
        {
            return AutomationControlType.Group;
        }

        // Return the base without the AccessKey character
        ///
        protected override string GetNameCore()
        {
            string result = base.GetNameCore();
            if (!string.IsNullOrEmpty(result))
            {
                GroupBox groupBox = (GroupBox)Owner;
                if (groupBox.Header is string)
                {
                    return MS.Internal.Controls.ControlFoundationHooks.RemoveAccessKeyMarker(result);
                }
            }

            return result;
        }
    }
}


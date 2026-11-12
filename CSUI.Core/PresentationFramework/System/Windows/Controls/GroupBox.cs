// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System.Windows.Automation.Peers;
using System.Windows.Input;

namespace System.Windows.Controls
{
    [Localizability(LocalizationCategory.None, Readability = Readability.Unreadable)]
    public partial class GroupBox : HeaderedContentControl
    {
        static partial void RegisterDefaultTheme();

        static GroupBox()
        {
            FocusableProperty.OverrideMetadata(typeof(GroupBox), new FrameworkPropertyMetadata(MS.Internal.KnownBoxes.BooleanBoxes.FalseBox));
            IsTabStopProperty.OverrideMetadata(typeof(GroupBox), new FrameworkPropertyMetadata(MS.Internal.KnownBoxes.BooleanBoxes.FalseBox));
            DefaultStyleKeyProperty.OverrideMetadata(typeof(GroupBox), new FrameworkPropertyMetadata(typeof(GroupBox)));

            EventManager.RegisterClassHandler(typeof(GroupBox), AccessKeyManager.AccessKeyPressedEvent, new AccessKeyPressedEventHandler(OnAccessKeyPressed));
            RegisterDefaultTheme();
        }

        protected override AutomationPeer OnCreateAutomationPeer()
        {
            return new GroupBoxAutomationPeer(this);
        }

        protected override void OnAccessKey(AccessKeyEventArgs e)
        {
            MoveFocus(new TraversalRequest(FocusNavigationDirection.First));
        }

        private static void OnAccessKeyPressed(object sender, AccessKeyPressedEventArgs e)
        {
            if (!e.Handled && e.Scope == null && e.Target == null)
            {
                e.Target = sender as GroupBox;
            }
        }
    }
}

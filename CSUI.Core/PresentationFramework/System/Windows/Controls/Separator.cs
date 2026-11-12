// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System.Windows.Automation.Peers;

namespace System.Windows.Controls
{
    public class Separator : Control
    {
        static Separator()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(Separator), new FrameworkPropertyMetadata(typeof(Separator)));
            _dType = DependencyObjectType.FromSystemTypeInternal(typeof(Separator));

            IsEnabledProperty.OverrideMetadata(typeof(Separator), new FrameworkPropertyMetadata(MS.Internal.KnownBoxes.BooleanBoxes.FalseBox));
        }

        internal static void PrepareContainer(Control container)
        {
            if (container != null)
            {
                container.IsEnabled = false;
                container.HorizontalContentAlignment = HorizontalAlignment.Stretch;
            }
        }

        protected override AutomationPeer OnCreateAutomationPeer()
        {
            return new SeparatorAutomationPeer(this);
        }

        internal override DependencyObjectType DTypeThemeStyleKey
        {
            get { return _dType; }
        }

        private static DependencyObjectType _dType;
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System.Windows.Automation.Peers;
using System.Windows.Input;

namespace System.Windows.Controls
{
    public partial class UserControl : ContentControl
    {
        static partial void RegisterDefaultTheme();
        static UserControl()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(UserControl), new FrameworkPropertyMetadata(typeof(UserControl)));
            _dType = DependencyObjectType.FromSystemTypeInternal(typeof(UserControl));

            FocusableProperty.OverrideMetadata(typeof(UserControl), new FrameworkPropertyMetadata(MS.Internal.KnownBoxes.BooleanBoxes.FalseBox));
            KeyboardNavigation.IsTabStopProperty.OverrideMetadata(typeof(UserControl), new FrameworkPropertyMetadata(MS.Internal.KnownBoxes.BooleanBoxes.FalseBox));

            HorizontalContentAlignmentProperty.OverrideMetadata(typeof(UserControl), new FrameworkPropertyMetadata(HorizontalAlignment.Stretch));
            VerticalContentAlignmentProperty.OverrideMetadata(typeof(UserControl), new FrameworkPropertyMetadata(VerticalAlignment.Stretch));
            RegisterDefaultTheme();
        }

        public UserControl()
        {
        }

        protected override AutomationPeer OnCreateAutomationPeer()
        {
            return new UserControlAutomationPeer(this);
        }

        internal override void AdjustBranchSource(RoutedEventArgs e)
        {
            e.Source = this;
        }

        internal override DependencyObjectType DTypeThemeStyleKey
        {
            get { return _dType; }
        }

        private static DependencyObjectType _dType;
    }
}

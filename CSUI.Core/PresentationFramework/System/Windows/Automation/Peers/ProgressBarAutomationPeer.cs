// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System.Windows.Automation.Provider;
using System.Windows.Controls;

namespace System.Windows.Automation.Peers
{
    public class ProgressBarAutomationPeer : RangeBaseAutomationPeer, IRangeValueProvider
    {
        public ProgressBarAutomationPeer(ProgressBar owner) : base(owner)
        {
        }

        protected override string GetClassNameCore() => "ProgressBar";

        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.ProgressBar;

        public override object GetPattern(PatternInterface patternInterface)
        {
            if (patternInterface == PatternInterface.RangeValue && ((ProgressBar)Owner).IsIndeterminate)
            {
                return null;
            }

            return base.GetPattern(patternInterface);
        }

        void IRangeValueProvider.SetValue(double val)
        {
            throw new InvalidOperationException("ProgressBar is read-only.");
        }

        bool IRangeValueProvider.IsReadOnly => true;

        double IRangeValueProvider.LargeChange => double.NaN;

        double IRangeValueProvider.SmallChange => double.NaN;
    }
}

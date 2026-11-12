// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Windows.Automation.Provider;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

#nullable disable

namespace System.Windows.Automation.Peers
{
    public class ScrollBarAutomationPeer : FrameworkElementAutomationPeer, IRangeValueProvider
    {
        public ScrollBarAutomationPeer(ScrollBar owner) : base(owner)
        {
        }

        protected override string GetClassNameCore()
        {
            return "ScrollBar";
        }

        protected override AutomationControlType GetAutomationControlTypeCore()
        {
            return AutomationControlType.ScrollBar;
        }

        protected override bool IsContentElementCore()
        {
            return false;
        }

        protected override Point GetClickablePointCore()
        {
            return new Point(double.NaN, double.NaN);
        }

        protected override AutomationOrientation GetOrientationCore()
        {
            return ((ScrollBar)Owner).Orientation == Orientation.Horizontal ?
                AutomationOrientation.Horizontal :
                AutomationOrientation.Vertical;
        }

        public override object GetPattern(PatternInterface patternInterface)
        {
            if (patternInterface == PatternInterface.RangeValue)
            {
                return this;
            }
            return base.GetPattern(patternInterface);
        }

        internal virtual void SetValueCore(double val)
        {
            double horizontalPercent = -1;
            double verticalPercent = -1;
            ScrollBar sb = (ScrollBar)Owner;
            ScrollViewer sv = sb.TemplatedParent as ScrollViewer;
            if (sv == null)
            {
                ArgumentOutOfRangeException.ThrowIfLessThan(val, sb.Minimum);
                ArgumentOutOfRangeException.ThrowIfGreaterThan(val, sb.Maximum);
                sb.Value = val;
            }
            else
            {
                if (sb.Orientation == Orientation.Horizontal)
                {
                    horizontalPercent = (val / (sv.ExtentWidth - sv.ViewportWidth)) * 100;
                }
                else
                {
                    verticalPercent = (val / (sv.ExtentHeight - sv.ViewportHeight)) * 100;
                }

                ScrollViewerAutomationPeer svAP = UIElementAutomationPeer.FromElement(sv) as ScrollViewerAutomationPeer;
                IScrollProvider scrollProvider = svAP as IScrollProvider;
                scrollProvider?.SetScrollPercent(horizontalPercent, verticalPercent);
            }
        }

        void IRangeValueProvider.SetValue(double val)
        {
            if (!IsEnabled())
                throw new ElementNotEnabledException();

            SetValueCore(val);
        }

        double IRangeValueProvider.Value => ((ScrollBar)Owner).Value;
        bool IRangeValueProvider.IsReadOnly => !IsEnabled();
        double IRangeValueProvider.Maximum => ((ScrollBar)Owner).Maximum;
        double IRangeValueProvider.Minimum => ((ScrollBar)Owner).Minimum;
        double IRangeValueProvider.LargeChange => ((ScrollBar)Owner).LargeChange;
        double IRangeValueProvider.SmallChange => ((ScrollBar)Owner).SmallChange;
    }
}

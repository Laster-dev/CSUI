// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace System.Windows.Automation.Peers
{
    public abstract class TextAutomationPeer : FrameworkElementAutomationPeer
    {
        protected TextAutomationPeer(FrameworkElement owner) : base(owner)
        {
        }

        internal List<AutomationPeer> GetChildrenFromTextLinks() => new List<AutomationPeer>();
    }

    public abstract class ContentTextAutomationPeer : FrameworkContentElementAutomationPeer
    {
        protected ContentTextAutomationPeer(FrameworkContentElement owner) : base(owner)
        {
        }
    }

    public class TextElementAutomationPeer : ContentTextAutomationPeer
    {
        public TextElementAutomationPeer(TextElement owner) : base(owner)
        {
        }
    }

    public class HyperlinkAutomationPeer : TextElementAutomationPeer, IInvokeProvider
    {
        public HyperlinkAutomationPeer(Hyperlink owner) : base(owner)
        {
        }

        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Hyperlink;

        protected override string GetClassNameCore() => "Hyperlink";

        protected override string GetNameCore()
        {
            string name = base.GetNameCore();
            if (string.IsNullOrEmpty(name) && Owner is Hyperlink link)
            {
                var sb = new System.Text.StringBuilder();
                link.AppendPlainText(sb);
                name = sb.ToString();
            }

            return name ?? string.Empty;
        }

        public override object GetPattern(PatternInterface patternInterface)
        {
            if (patternInterface == PatternInterface.Invoke)
            {
                return this;
            }

            return base.GetPattern(patternInterface);
        }

        protected override bool IsControlElementCore() => true;

        void IInvokeProvider.Invoke()
        {
            if (!IsEnabled())
            {
                throw new ElementNotEnabledException();
            }

            ((Hyperlink)Owner).DoClick();
        }
    }

    public class TextBlockAutomationPeer : TextAutomationPeer
    {
        public TextBlockAutomationPeer(TextBlock owner) : base(owner)
        {
        }

        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Text;

        protected override string GetClassNameCore() => "TextBlock";

        protected override bool IsControlElementCore() => true;

        protected override string GetNameCore()
        {
            string name = base.GetNameCore();
            if (string.IsNullOrEmpty(name) && Owner is TextBlock tb)
            {
                name = tb.Text;
            }

            return name ?? string.Empty;
        }
    }

    public class TextBoxAutomationPeer : TextAutomationPeer, IValueProvider
    {
        public TextBoxAutomationPeer(TextBox owner) : base(owner)
        {
        }

        protected override string GetClassNameCore() => "TextBox";

        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Edit;

        public override object GetPattern(PatternInterface patternInterface)
        {
            if (patternInterface == PatternInterface.Value)
            {
                return this;
            }

            return base.GetPattern(patternInterface);
        }

        bool IValueProvider.IsReadOnly => ((TextBox)Owner).IsReadOnly;

        string IValueProvider.Value => ((TextBox)Owner).Text ?? string.Empty;

        void IValueProvider.SetValue(string value)
        {
            if (!IsEnabled())
            {
                throw new ElementNotEnabledException();
            }

            TextBox owner = (TextBox)Owner;
            if (owner.IsReadOnly)
            {
                throw new ElementNotEnabledException();
            }

            ArgumentNullException.ThrowIfNull(value);
            owner.Text = value;
        }
    }

    public class PasswordBoxAutomationPeer : TextAutomationPeer, IValueProvider
    {
        public PasswordBoxAutomationPeer(PasswordBox owner) : base(owner)
        {
        }

        protected override string GetClassNameCore() => "PasswordBox";

        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Edit;

        public override object GetPattern(PatternInterface patternInterface)
        {
            if (patternInterface == PatternInterface.Value)
            {
                return this;
            }

            return base.GetPattern(patternInterface);
        }

        protected override bool IsPasswordCore() => true;

        bool IValueProvider.IsReadOnly => false;

        string IValueProvider.Value => throw new InvalidOperationException();

        void IValueProvider.SetValue(string value)
        {
            if (!IsEnabled())
            {
                throw new ElementNotEnabledException();
            }

            ArgumentNullException.ThrowIfNull(value);
            ((PasswordBox)Owner).Password = value;
        }
    }
}

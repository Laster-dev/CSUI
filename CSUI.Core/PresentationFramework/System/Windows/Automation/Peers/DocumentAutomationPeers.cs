// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System.Windows.Automation.Provider;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;

namespace System.Windows.Automation.Peers
{
    public class DocumentAutomationPeer : ContentTextAutomationPeer
    {
        public DocumentAutomationPeer(FrameworkContentElement owner) : base(owner)
        {
        }

        protected override string GetClassNameCore() => "Document";

        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Document;

        protected override bool IsControlElementCore() => true;
    }

    public class DocumentPageViewAutomationPeer : FrameworkElementAutomationPeer
    {
        public DocumentPageViewAutomationPeer(DocumentPageView owner) : base(owner)
        {
        }

        protected override string GetClassNameCore() => "DocumentPageView";

        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Document;
    }

    public class DocumentViewerBaseAutomationPeer : FrameworkElementAutomationPeer
    {
        public DocumentViewerBaseAutomationPeer(DocumentViewerBase owner) : base(owner)
        {
        }

        protected override string GetClassNameCore() => "DocumentViewer";

        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Document;
    }

    public class DocumentViewerAutomationPeer : DocumentViewerBaseAutomationPeer
    {
        public DocumentViewerAutomationPeer(DocumentViewer owner) : base(owner)
        {
        }

        protected override string GetClassNameCore() => "DocumentViewer";
    }

    public class FlowDocumentPageViewerAutomationPeer : DocumentViewerBaseAutomationPeer
    {
        public FlowDocumentPageViewerAutomationPeer(FlowDocumentPageViewer owner) : base(owner)
        {
        }

        protected override string GetClassNameCore() => "FlowDocumentPageViewer";
    }

    public class FlowDocumentReaderAutomationPeer : FrameworkElementAutomationPeer
    {
        public FlowDocumentReaderAutomationPeer(FlowDocumentReader owner) : base(owner)
        {
        }

        protected override string GetClassNameCore() => "FlowDocumentReader";

        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Document;
    }

    public class FlowDocumentScrollViewerAutomationPeer : FrameworkElementAutomationPeer
    {
        public FlowDocumentScrollViewerAutomationPeer(FlowDocumentScrollViewer owner) : base(owner)
        {
        }

        protected override string GetClassNameCore() => "FlowDocumentScrollViewer";

        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Document;
    }

    public class FixedPageAutomationPeer : FrameworkElementAutomationPeer
    {
        public FixedPageAutomationPeer(FixedPage owner) : base(owner)
        {
        }

        protected override string GetClassNameCore() => "FixedPage";

        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Document;
    }

    public class RichTextBoxAutomationPeer : TextAutomationPeer, IValueProvider
    {
        public RichTextBoxAutomationPeer(RichTextBox owner) : base(owner)
        {
        }

        protected override string GetClassNameCore() => "RichTextBox";

        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Document;

        public override object GetPattern(PatternInterface patternInterface)
        {
            if (patternInterface == PatternInterface.Value)
            {
                return this;
            }
            return base.GetPattern(patternInterface);
        }

        bool IValueProvider.IsReadOnly => ((RichTextBox)Owner).IsReadOnly;

        string IValueProvider.Value => ((RichTextBox)Owner).Document?.GetPlainText() ?? string.Empty;

        void IValueProvider.SetValue(string value)
        {
            if (!IsEnabled()) throw new ElementNotEnabledException();
            RichTextBox owner = (RichTextBox)Owner;
            if (owner.IsReadOnly) throw new ElementNotEnabledException();
            owner.Document.SetPlainText(value ?? string.Empty);
        }
    }

    public class TableAutomationPeer : TextElementAutomationPeer, IGridProvider
    {
        public TableAutomationPeer(Table owner) : base(owner)
        {
        }

        protected override string GetClassNameCore() => "Table";

        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Table;

        protected override bool IsControlElementCore() => true;

        public override object GetPattern(PatternInterface patternInterface)
        {
            if (patternInterface == PatternInterface.Grid) return this;
            return base.GetPattern(patternInterface);
        }

        int IGridProvider.ColumnCount => ((Table)Owner).Columns.Count;

        int IGridProvider.RowCount
        {
            get
            {
                int rows = 0;
                Table t = (Table)Owner;
                for (int i = 0; i < t.RowGroups.Count; i++) rows += t.RowGroups[i].Rows.Count;
                return rows;
            }
        }

        IRawElementProviderSimple IGridProvider.GetItem(int row, int column) => null;
    }

    public class TableCellAutomationPeer : TextElementAutomationPeer, IGridItemProvider
    {
        public TableCellAutomationPeer(TableCell owner) : base(owner)
        {
        }

        protected override string GetClassNameCore() => "TableCell";

        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Custom;

        public override object GetPattern(PatternInterface patternInterface)
        {
            if (patternInterface == PatternInterface.GridItem) return this;
            return base.GetPattern(patternInterface);
        }

        int IGridItemProvider.Row => 0;
        int IGridItemProvider.Column => 0;
        int IGridItemProvider.RowSpan => ((TableCell)Owner).RowSpan;
        int IGridItemProvider.ColumnSpan => ((TableCell)Owner).ColumnSpan;
        IRawElementProviderSimple IGridItemProvider.ContainingGrid => null;
    }
}

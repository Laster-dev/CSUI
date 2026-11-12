// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Media;
using MS.Internal.Controls;

namespace System.Windows.Documents
{
    public enum TextDecorationLocation
    {
        Underline,
        OverLine,
        Strikethrough,
        Baseline
    }

    public abstract class Block : TextElement
    {
        public static readonly DependencyProperty TextAlignmentProperty =
            DependencyProperty.Register(nameof(TextAlignment), typeof(TextAlignment), typeof(Block),
                new FrameworkPropertyMetadata(TextAlignment.Left, FrameworkPropertyMetadataOptions.Inherits));

        public static readonly DependencyProperty LineHeightProperty =
            DependencyProperty.Register(nameof(LineHeight), typeof(double), typeof(Block),
                new FrameworkPropertyMetadata(double.NaN, FrameworkPropertyMetadataOptions.Inherits));

        public static readonly DependencyProperty MarginProperty =
            FrameworkElement.MarginProperty.AddOwner(typeof(Block),
                new FrameworkPropertyMetadata(new Thickness(0)));

        public static readonly DependencyProperty PaddingProperty =
            Control.PaddingProperty.AddOwner(typeof(Block),
                new FrameworkPropertyMetadata(new Thickness(0)));

        public static readonly DependencyProperty BorderBrushProperty =
            Control.BorderBrushProperty.AddOwner(typeof(Block));

        public static readonly DependencyProperty BorderThicknessProperty =
            Control.BorderThicknessProperty.AddOwner(typeof(Block));

        public static readonly DependencyProperty BreakPageBeforeProperty =
            DependencyProperty.Register(nameof(BreakPageBefore), typeof(bool), typeof(Block),
                new FrameworkPropertyMetadata(false));

        public static readonly DependencyProperty BreakColumnBeforeProperty =
            DependencyProperty.Register(nameof(BreakColumnBefore), typeof(bool), typeof(Block),
                new FrameworkPropertyMetadata(false));

        static Block()
        {
            ControlFoundationHooks.BlockLineHeightProperty = LineHeightProperty;
        }

        protected Block()
        {
        }

        public TextAlignment TextAlignment
        {
            get => (TextAlignment)GetValue(TextAlignmentProperty);
            set => SetValue(TextAlignmentProperty, value);
        }

        public double LineHeight
        {
            get => (double)GetValue(LineHeightProperty);
            set => SetValue(LineHeightProperty, value);
        }

        public Thickness Margin
        {
            get => (Thickness)GetValue(MarginProperty);
            set => SetValue(MarginProperty, value);
        }

        public Thickness Padding
        {
            get => (Thickness)GetValue(PaddingProperty);
            set => SetValue(PaddingProperty, value);
        }

        public Brush BorderBrush
        {
            get => (Brush)GetValue(BorderBrushProperty);
            set => SetValue(BorderBrushProperty, value);
        }

        public Thickness BorderThickness
        {
            get => (Thickness)GetValue(BorderThicknessProperty);
            set => SetValue(BorderThicknessProperty, value);
        }

        public bool BreakPageBefore
        {
            get => (bool)GetValue(BreakPageBeforeProperty);
            set => SetValue(BreakPageBeforeProperty, value);
        }

        public bool BreakColumnBefore
        {
            get => (bool)GetValue(BreakColumnBeforeProperty);
            set => SetValue(BreakColumnBeforeProperty, value);
        }

        internal virtual void AppendPlainText(StringBuilder sb)
        {
        }
    }

    public class BlockCollection : IList<Block>, IList
    {
        private readonly DependencyObject _owner;
        private readonly List<Block> _items = new List<Block>();

        internal BlockCollection(DependencyObject owner)
        {
            _owner = owner;
        }

        public Block FirstBlock => _items.Count > 0 ? _items[0] : null;
        public Block LastBlock => _items.Count > 0 ? _items[_items.Count - 1] : null;

        public int Count => _items.Count;
        public bool IsReadOnly => false;
        bool IList.IsFixedSize => false;
        bool ICollection.IsSynchronized => false;
        object ICollection.SyncRoot => ((ICollection)_items).SyncRoot;

        public Block this[int index]
        {
            get => _items[index];
            set
            {
                _items[index] = value;
                InvalidateOwner();
            }
        }

        object IList.this[int index]
        {
            get => this[index];
            set => this[index] = (Block)value;
        }

        public void Add(Block item)
        {
            ArgumentNullException.ThrowIfNull(item);
            _items.Add(item);
            InvalidateOwner();
        }

        public void AddRange(IEnumerable range)
        {
            ArgumentNullException.ThrowIfNull(range);
            foreach (object item in range)
            {
                if (item is Block block)
                {
                    _items.Add(block);
                }
            }
            InvalidateOwner();
        }

        public void InsertAfter(Block previousSibling, Block newItem)
        {
            ArgumentNullException.ThrowIfNull(newItem);
            int idx = previousSibling != null ? _items.IndexOf(previousSibling) : -1;
            if (idx >= 0) _items.Insert(idx + 1, newItem);
            else _items.Add(newItem);
            InvalidateOwner();
        }

        public void InsertBefore(Block nextSibling, Block newItem)
        {
            ArgumentNullException.ThrowIfNull(newItem);
            int idx = nextSibling != null ? _items.IndexOf(nextSibling) : 0;
            if (idx >= 0) _items.Insert(idx, newItem);
            else _items.Insert(0, newItem);
            InvalidateOwner();
        }

        public void Clear()
        {
            _items.Clear();
            InvalidateOwner();
        }

        public bool Contains(Block item) => _items.Contains(item);
        public void CopyTo(Block[] array, int arrayIndex) => _items.CopyTo(array, arrayIndex);
        public IEnumerator<Block> GetEnumerator() => _items.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => _items.GetEnumerator();
        public int IndexOf(Block item) => _items.IndexOf(item);
        public void Insert(int index, Block item)
        {
            ArgumentNullException.ThrowIfNull(item);
            _items.Insert(index, item);
            InvalidateOwner();
        }
        public bool Remove(Block item)
        {
            bool removed = _items.Remove(item);
            if (removed) InvalidateOwner();
            return removed;
        }
        public void RemoveAt(int index)
        {
            _items.RemoveAt(index);
            InvalidateOwner();
        }

        int IList.Add(object value) { Add((Block)value); return _items.Count - 1; }
        bool IList.Contains(object value) => value is Block b && Contains(b);
        int IList.IndexOf(object value) => value is Block b ? IndexOf(b) : -1;
        void IList.Insert(int index, object value) => Insert(index, (Block)value);
        void IList.Remove(object value) { if (value is Block b) Remove(b); }
        void ICollection.CopyTo(Array array, int index) => ((ICollection)_items).CopyTo(array, index);

        private void InvalidateOwner()
        {
            if (_owner is FlowDocument fd)
            {
                fd.NotifyContentChanged();
            }
        }
    }

    [ContentProperty(nameof(Inlines))]
    public class Paragraph : Block
    {
        public static readonly DependencyProperty KeepWithNextProperty =
            DependencyProperty.Register(nameof(KeepWithNext), typeof(bool), typeof(Paragraph), new FrameworkPropertyMetadata(false));

        public static readonly DependencyProperty KeepTogetherProperty =
            DependencyProperty.Register(nameof(KeepTogether), typeof(bool), typeof(Paragraph), new FrameworkPropertyMetadata(false));

        public static readonly DependencyProperty MinWidowLinesProperty =
            DependencyProperty.Register(nameof(MinWidowLines), typeof(int), typeof(Paragraph), new FrameworkPropertyMetadata(0));

        public static readonly DependencyProperty MinOrphanLinesProperty =
            DependencyProperty.Register(nameof(MinOrphanLines), typeof(int), typeof(Paragraph), new FrameworkPropertyMetadata(0));

        public static readonly DependencyProperty TextIndentProperty =
            DependencyProperty.Register(nameof(TextIndent), typeof(double), typeof(Paragraph), new FrameworkPropertyMetadata(0.0));

        private readonly InlineCollection _inlines;

        static Paragraph()
        {
            ControlFoundationHooks.GetParagraphKeepTogetherHook = static d => d is Paragraph p && p.KeepTogether;
            ControlFoundationHooks.GetParagraphKeepWithNextHook = static d => d is Paragraph p && p.KeepWithNext;
            ControlFoundationHooks.GetParagraphMinWidowLinesHook = static d => d is Paragraph p ? p.MinWidowLines : 0;
            ControlFoundationHooks.GetParagraphMinOrphanLinesHook = static d => d is Paragraph p ? p.MinOrphanLines : 0;
        }

        public Paragraph()
        {
            _inlines = new InlineCollection(this);
        }

        public Paragraph(Inline inline) : this()
        {
            ArgumentNullException.ThrowIfNull(inline);
            _inlines.Add(inline);
        }

        public InlineCollection Inlines => _inlines;

        public bool KeepWithNext
        {
            get => (bool)GetValue(KeepWithNextProperty);
            set => SetValue(KeepWithNextProperty, value);
        }

        public bool KeepTogether
        {
            get => (bool)GetValue(KeepTogetherProperty);
            set => SetValue(KeepTogetherProperty, value);
        }

        public int MinWidowLines
        {
            get => (int)GetValue(MinWidowLinesProperty);
            set => SetValue(MinWidowLinesProperty, value);
        }

        public int MinOrphanLines
        {
            get => (int)GetValue(MinOrphanLinesProperty);
            set => SetValue(MinOrphanLinesProperty, value);
        }

        public double TextIndent
        {
            get => (double)GetValue(TextIndentProperty);
            set => SetValue(TextIndentProperty, value);
        }

        internal override void AppendPlainText(StringBuilder sb)
        {
            for (int i = 0; i < _inlines.Count; i++)
            {
                _inlines[i].AppendPlainText(sb);
            }
        }
    }

    [ContentProperty(nameof(Blocks))]
    public class Section : Block
    {
        private readonly BlockCollection _blocks;

        public Section()
        {
            _blocks = new BlockCollection(this);
        }

        public Section(Block block) : this()
        {
            ArgumentNullException.ThrowIfNull(block);
            _blocks.Add(block);
        }

        public BlockCollection Blocks => _blocks;

        internal override void AppendPlainText(StringBuilder sb)
        {
            for (int i = 0; i < _blocks.Count; i++)
            {
                if (i > 0) sb.Append('\n');
                _blocks[i].AppendPlainText(sb);
            }
        }
    }

    [ContentProperty(nameof(Child))]
    public class BlockUIContainer : Block
    {
        public BlockUIContainer()
        {
        }

        public BlockUIContainer(UIElement uiElement)
        {
            Child = uiElement;
        }

        public UIElement Child { get; set; }
    }

    [ContentProperty(nameof(Blocks))]
    public class ListItem : TextElement
    {
        public ListItem()
        {
            Blocks = new BlockCollection(this);
        }

        public ListItem(Paragraph paragraph) : this()
        {
            ArgumentNullException.ThrowIfNull(paragraph);
            Blocks.Add(paragraph);
        }

        public BlockCollection Blocks { get; }
    }

    public class ListItemCollection : List<ListItem>
    {
    }

    [ContentProperty(nameof(ListItems))]
    public class List : Block
    {
        public static readonly DependencyProperty MarkerStyleProperty =
            DependencyProperty.Register(nameof(MarkerStyle), typeof(TextMarkerStyle), typeof(List), new FrameworkPropertyMetadata(TextMarkerStyle.Disc));

        public List()
        {
            ListItems = new ListItemCollection();
        }

        public List(ListItem listItem) : this()
        {
            ArgumentNullException.ThrowIfNull(listItem);
            ListItems.Add(listItem);
        }

        public ListItemCollection ListItems { get; }

        public TextMarkerStyle MarkerStyle
        {
            get => (TextMarkerStyle)GetValue(MarkerStyleProperty);
            set => SetValue(MarkerStyleProperty, value);
        }

        internal override void AppendPlainText(StringBuilder sb)
        {
            for (int i = 0; i < ListItems.Count; i++)
            {
                if (i > 0) sb.Append('\n');
                for (int b = 0; b < ListItems[i].Blocks.Count; b++)
                {
                    if (b > 0) sb.Append('\n');
                    ListItems[i].Blocks[b].AppendPlainText(sb);
                }
            }
        }
    }

    public enum TextMarkerStyle
    {
        None,
        Disc,
        Circle,
        Square,
        Box,
        LowerRoman,
        UpperRoman,
        LowerLatin,
        UpperLatin,
        Decimal
    }

    [ContentProperty(nameof(RowGroups))]
    public class Table : Block
    {
        public static readonly DependencyProperty CellSpacingProperty =
            DependencyProperty.Register(nameof(CellSpacing), typeof(double), typeof(Table), new FrameworkPropertyMetadata(2.0));

        public Table()
        {
            Columns = new TableColumnCollection();
            RowGroups = new TableRowGroupCollection();
        }

        public TableColumnCollection Columns { get; }
        public TableRowGroupCollection RowGroups { get; }

        public double CellSpacing
        {
            get => (double)GetValue(CellSpacingProperty);
            set => SetValue(CellSpacingProperty, value);
        }

        protected override AutomationPeer OnCreateAutomationPeer() => new TableAutomationPeer(this);

        internal override void AppendPlainText(StringBuilder sb)
        {
            for (int g = 0; g < RowGroups.Count; g++)
            {
                for (int r = 0; r < RowGroups[g].Rows.Count; r++)
                {
                    if (sb.Length > 0) sb.Append('\n');
                    var row = RowGroups[g].Rows[r];
                    for (int c = 0; c < row.Cells.Count; c++)
                    {
                        if (c > 0) sb.Append('\t');
                        row.Cells[c].AppendPlainText(sb);
                    }
                }
            }
        }
    }

    public class TableColumn : FrameworkContentElement
    {
        public static readonly DependencyProperty WidthProperty =
            DependencyProperty.Register(nameof(Width), typeof(GridLength), typeof(TableColumn), new FrameworkPropertyMetadata(GridLength.Auto));

        public GridLength Width
        {
            get => (GridLength)GetValue(WidthProperty);
            set => SetValue(WidthProperty, value);
        }
    }

    public class TableColumnCollection : List<TableColumn> { }

    [ContentProperty(nameof(Rows))]
    public class TableRowGroup : TextElement
    {
        public TableRowGroup()
        {
            Rows = new TableRowCollection();
        }

        public TableRowCollection Rows { get; }
    }

    public class TableRowGroupCollection : List<TableRowGroup> { }

    [ContentProperty(nameof(Cells))]
    public class TableRow : TextElement
    {
        public TableRow()
        {
            Cells = new TableCellCollection();
        }

        public TableCellCollection Cells { get; }
    }

    public class TableRowCollection : List<TableRow> { }

    [ContentProperty(nameof(Blocks))]
    public class TableCell : TextElement
    {
        public static readonly DependencyProperty ColumnSpanProperty =
            DependencyProperty.Register(nameof(ColumnSpan), typeof(int), typeof(TableCell), new FrameworkPropertyMetadata(1));

        public static readonly DependencyProperty RowSpanProperty =
            DependencyProperty.Register(nameof(RowSpan), typeof(int), typeof(TableCell), new FrameworkPropertyMetadata(1));

        public TableCell()
        {
            Blocks = new BlockCollection(this);
        }

        public TableCell(Block blockItem) : this()
        {
            ArgumentNullException.ThrowIfNull(blockItem);
            Blocks.Add(blockItem);
        }

        public BlockCollection Blocks { get; }

        public int ColumnSpan
        {
            get => (int)GetValue(ColumnSpanProperty);
            set => SetValue(ColumnSpanProperty, value);
        }

        public int RowSpan
        {
            get => (int)GetValue(RowSpanProperty);
            set => SetValue(RowSpanProperty, value);
        }

        protected override AutomationPeer OnCreateAutomationPeer() => new TableCellAutomationPeer(this);

        internal void AppendPlainText(StringBuilder sb)
        {
            for (int i = 0; i < Blocks.Count; i++)
            {
                if (i > 0) sb.Append('\n');
                Blocks[i].AppendPlainText(sb);
            }
        }
    }

    public class TableCellCollection : List<TableCell> { }

    public enum LogicalDirection
    {
        Backward = 0,
        Forward = 1
    }

    public class TextPointer
    {
        private readonly FlowDocument _document;
        private readonly int _offset;
        private readonly LogicalDirection _direction;

        internal TextPointer(FlowDocument document, int offset, LogicalDirection direction = LogicalDirection.Forward)
        {
            _document = document;
            _offset = Math.Max(0, offset);
            _direction = direction;
        }

        public FlowDocument Document => _document;
        public LogicalDirection LogicalDirection => _direction;
        public DependencyObject Parent => _document;
        public TextPointer DocumentStart => new TextPointer(_document, 0, LogicalDirection.Forward);
        public TextPointer DocumentEnd => new TextPointer(_document, _document?.GetPlainTextLength() ?? 0, LogicalDirection.Backward);

        internal int Offset => _offset;

        public int CompareTo(TextPointer position)
        {
            ArgumentNullException.ThrowIfNull(position);
            return _offset.CompareTo(position._offset);
        }

        public int GetOffsetToPosition(TextPointer position)
        {
            ArgumentNullException.ThrowIfNull(position);
            return position._offset - _offset;
        }

        public TextPointer GetPositionAtOffset(int offset) => GetPositionAtOffset(offset, _direction);

        public TextPointer GetPositionAtOffset(int offset, LogicalDirection direction)
        {
            int maxLen = _document?.GetPlainTextLength() ?? 0;
            int next = Math.Clamp(_offset + offset, 0, maxLen);
            return new TextPointer(_document, next, direction);
        }

        public string GetTextInRun(LogicalDirection direction)
        {
            string full = _document?.GetPlainText() ?? string.Empty;
            if (direction == LogicalDirection.Forward)
            {
                return _offset < full.Length ? full.Substring(_offset) : string.Empty;
            }
            return _offset > 0 ? full.Substring(0, Math.Min(_offset, full.Length)) : string.Empty;
        }

        public void InsertTextInRun(string textData)
        {
            _document?.SetPlainText((_document.GetPlainText() ?? string.Empty).Insert(Math.Min(_offset, _document.GetPlainTextLength()), textData ?? string.Empty));
        }
    }

    public class TextRange
    {
        private TextPointer _start;
        private TextPointer _end;

        public TextRange(TextPointer position1, TextPointer position2)
        {
            ArgumentNullException.ThrowIfNull(position1);
            ArgumentNullException.ThrowIfNull(position2);
            if (position1.CompareTo(position2) <= 0)
            {
                _start = position1;
                _end = position2;
            }
            else
            {
                _start = position2;
                _end = position1;
            }
        }

        public TextPointer Start => _start;
        public TextPointer End => _end;
        public bool IsEmpty => _start.Offset == _end.Offset;

        public virtual string Text
        {
            get
            {
                string full = _start.Document?.GetPlainText() ?? string.Empty;
                int s = Math.Clamp(_start.Offset, 0, full.Length);
                int e = Math.Clamp(_end.Offset, s, full.Length);
                return full.Substring(s, e - s);
            }
            set
            {
                string full = _start.Document?.GetPlainText() ?? string.Empty;
                int s = Math.Clamp(_start.Offset, 0, full.Length);
                int e = Math.Clamp(_end.Offset, s, full.Length);
                string replacement = value ?? string.Empty;
                string updated = full.Substring(0, s) + replacement + full.Substring(e);
                _start.Document?.SetPlainText(updated);
                _end = new TextPointer(_start.Document, s + replacement.Length);
            }
        }

        public void ApplyPropertyValue(DependencyProperty formattingProperty, object value)
        {
            ArgumentNullException.ThrowIfNull(formattingProperty);
            _start.Document?.SetValue(formattingProperty, value);
        }

        public object GetPropertyValue(DependencyProperty formattingProperty)
        {
            ArgumentNullException.ThrowIfNull(formattingProperty);
            return _start.Document != null ? _start.Document.GetValue(formattingProperty) : DependencyProperty.UnsetValue;
        }

        public void ClearAllProperties()
        {
        }

        public void Select(TextPointer position1, TextPointer position2)
        {
            ArgumentNullException.ThrowIfNull(position1);
            ArgumentNullException.ThrowIfNull(position2);
            _start = position1;
            _end = position2;
        }

        public void Save(Stream stream, string dataFormat)
        {
            ArgumentNullException.ThrowIfNull(stream);
            byte[] bytes = Encoding.UTF8.GetBytes(Text ?? string.Empty);
            stream.Write(bytes, 0, bytes.Length);
        }

        public void Load(Stream stream, string dataFormat)
        {
            ArgumentNullException.ThrowIfNull(stream);
            using var reader = new StreamReader(stream, Encoding.UTF8, leaveOpen: true);
            Text = reader.ReadToEnd();
        }
    }

    public sealed class TextSelection : TextRange
    {
        internal TextSelection(TextPointer start, TextPointer end) : base(start, end)
        {
        }
    }

    [ContentProperty(nameof(Blocks))]
    public class FlowDocument : FrameworkContentElement, IDocumentPaginatorSource, IAddChild
    {
        public static readonly DependencyProperty FontFamilyProperty = TextElement.FontFamilyProperty.AddOwner(typeof(FlowDocument));
        public static readonly DependencyProperty FontSizeProperty = TextElement.FontSizeProperty.AddOwner(typeof(FlowDocument));
        public static readonly DependencyProperty ForegroundProperty = TextElement.ForegroundProperty.AddOwner(typeof(FlowDocument));
        public static readonly DependencyProperty BackgroundProperty = TextElement.BackgroundProperty.AddOwner(typeof(FlowDocument));
        public static readonly DependencyProperty PageWidthProperty = DependencyProperty.Register(nameof(PageWidth), typeof(double), typeof(FlowDocument), new FrameworkPropertyMetadata(double.NaN));
        public static readonly DependencyProperty PageHeightProperty = DependencyProperty.Register(nameof(PageHeight), typeof(double), typeof(FlowDocument), new FrameworkPropertyMetadata(double.NaN));
        public static readonly DependencyProperty ColumnWidthProperty = DependencyProperty.Register(nameof(ColumnWidth), typeof(double), typeof(FlowDocument), new FrameworkPropertyMetadata(double.NaN));
        public static readonly DependencyProperty PagePaddingProperty = DependencyProperty.Register(nameof(PagePadding), typeof(Thickness), typeof(FlowDocument), new FrameworkPropertyMetadata(new Thickness(0)));
        public static readonly DependencyProperty IsOptimalParagraphEnabledProperty = DependencyProperty.Register(nameof(IsOptimalParagraphEnabled), typeof(bool), typeof(FlowDocument), new FrameworkPropertyMetadata(false));
        public static readonly DependencyProperty IsHyphenationEnabledProperty = DependencyProperty.Register(nameof(IsHyphenationEnabled), typeof(bool), typeof(FlowDocument), new FrameworkPropertyMetadata(false));

        private readonly BlockCollection _blocks;
        private DocumentPaginator _paginator;

        internal event EventHandler ContentChanged;

        public FlowDocument()
        {
            _blocks = new BlockCollection(this);
        }

        public FlowDocument(Block block) : this()
        {
            ArgumentNullException.ThrowIfNull(block);
            _blocks.Add(block);
        }

        public BlockCollection Blocks => _blocks;

        public FontFamily FontFamily
        {
            get => (FontFamily)GetValue(FontFamilyProperty);
            set => SetValue(FontFamilyProperty, value);
        }

        public double FontSize
        {
            get => (double)GetValue(FontSizeProperty);
            set => SetValue(FontSizeProperty, value);
        }

        public Brush Foreground
        {
            get => (Brush)GetValue(ForegroundProperty);
            set => SetValue(ForegroundProperty, value);
        }

        public Brush Background
        {
            get => (Brush)GetValue(BackgroundProperty);
            set => SetValue(BackgroundProperty, value);
        }

        public double PageWidth
        {
            get => (double)GetValue(PageWidthProperty);
            set => SetValue(PageWidthProperty, value);
        }

        public double PageHeight
        {
            get => (double)GetValue(PageHeightProperty);
            set => SetValue(PageHeightProperty, value);
        }

        public double ColumnWidth
        {
            get => (double)GetValue(ColumnWidthProperty);
            set => SetValue(ColumnWidthProperty, value);
        }

        public Thickness PagePadding
        {
            get => (Thickness)GetValue(PagePaddingProperty);
            set => SetValue(PagePaddingProperty, value);
        }

        public bool IsOptimalParagraphEnabled
        {
            get => (bool)GetValue(IsOptimalParagraphEnabledProperty);
            set => SetValue(IsOptimalParagraphEnabledProperty, value);
        }

        public bool IsHyphenationEnabled
        {
            get => (bool)GetValue(IsHyphenationEnabledProperty);
            set => SetValue(IsHyphenationEnabledProperty, value);
        }

        public TextPointer ContentStart => new TextPointer(this, 0, LogicalDirection.Forward);
        public TextPointer ContentEnd => new TextPointer(this, GetPlainTextLength(), LogicalDirection.Backward);

        public DocumentPaginator DocumentPaginator => _paginator ??= new FlowDocumentPaginator(this);

        DocumentPaginator IDocumentPaginatorSource.DocumentPaginator => DocumentPaginator;

        void IAddChild.AddChild(object value)
        {
            ArgumentNullException.ThrowIfNull(value);
            if (value is Block b)
            {
                Blocks.Add(b);
            }
            else if (value is Inline i)
            {
                Blocks.Add(new Paragraph(i));
            }
            else if (value is UIElement ui)
            {
                Blocks.Add(new BlockUIContainer(ui));
            }
            else
            {
                throw new ArgumentException("Unexpected child type for FlowDocument.", nameof(value));
            }
        }

        void IAddChild.AddText(string text)
        {
            if (!string.IsNullOrEmpty(text))
            {
                Blocks.Add(new Paragraph(new Run(text)));
            }
        }

        internal string GetPlainText()
        {
            var sb = new StringBuilder();
            for (int i = 0; i < _blocks.Count; i++)
            {
                if (i > 0) sb.Append('\n');
                _blocks[i].AppendPlainText(sb);
            }
            return sb.ToString();
        }

        internal int GetPlainTextLength() => GetPlainText().Length;

        internal void SetPlainText(string text)
        {
            _blocks.Clear();
            if (!string.IsNullOrEmpty(text))
            {
                string[] lines = text.Split('\n');
                for (int i = 0; i < lines.Length; i++)
                {
                    _blocks.Add(new Paragraph(new Run(lines[i])));
                }
            }
            else
            {
                _blocks.Add(new Paragraph());
            }
        }

        internal void NotifyContentChanged() => ContentChanged?.Invoke(this, EventArgs.Empty);

        protected override AutomationPeer OnCreateAutomationPeer() => new DocumentAutomationPeer(this);

        private sealed class FlowDocumentPaginator : DocumentPaginator
        {
            private readonly FlowDocument _doc;
            private Size _pageSize = new Size(816, 1056);

            public FlowDocumentPaginator(FlowDocument doc)
            {
                _doc = doc;
            }

            public override bool IsPageCountValid => true;
            public override int PageCount => 1;
            public override Size PageSize
            {
                get => _pageSize;
                set => _pageSize = value;
            }

            public override IDocumentPaginatorSource Source => _doc;

            public override DocumentPage GetPage(int pageNumber)
            {
                if (pageNumber != 0)
                {
                    return DocumentPage.Missing;
                }

                var tb = new TextBlock
                {
                    Text = _doc.GetPlainText(),
                    FontSize = _doc.FontSize > 0 ? _doc.FontSize : 14.0,
                    Foreground = _doc.Foreground ?? Brushes.Black,
                    TextWrapping = TextWrapping.Wrap,
                    Padding = new Thickness(24)
                };
                tb.Measure(_pageSize);
                tb.Arrange(new Rect(0, 0, _pageSize.Width, _pageSize.Height));
                return new DocumentPage(tb, _pageSize, new Rect(_pageSize), new Rect(_pageSize));
            }
        }
    }
}

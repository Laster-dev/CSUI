// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System;
using System.Collections;
using System.Globalization;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Input.Ime;
using System.Windows.Markup;
using System.Windows.Media;

namespace System.Windows.Controls
{
    /// <summary>
    /// Managed RichTextBox operating on FlowDocument with DirectWrite GPU rendering (zero PresentationNative C++ PTS dependency).
    /// </summary>
    [ContentProperty(nameof(Document))]
    public partial class RichTextBox : TextBoxBase, IAddChild
    {
        static partial void RegisterDefaultTheme();

        public static readonly DependencyProperty IsDocumentEnabledProperty =
            DependencyProperty.Register(nameof(IsDocumentEnabled), typeof(bool), typeof(RichTextBox), new FrameworkPropertyMetadata(false));

        private FlowDocument _document;
        private int _caretOffset;
        private int _selectionLength;
        private int _selectionAnchor;
        private bool _isSelecting;
        private TextSelection _selection;
        private TextContentHostView _contentHostView;
        private ScrollViewer _contentHostScrollViewer;

        static RichTextBox()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(RichTextBox), new FrameworkPropertyMetadata(typeof(RichTextBox)));
            KeyboardNavigation.AcceptsReturnProperty.OverrideMetadata(typeof(RichTextBox), new FrameworkPropertyMetadata(true));
            AcceptsReturnProperty.OverrideMetadata(typeof(RichTextBox), new FrameworkPropertyMetadata(true));
            RegisterDefaultTheme();
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            _contentHostScrollViewer = GetTemplateChild("PART_ContentHost") as ScrollViewer;
            if (_contentHostScrollViewer != null)
            {
                // FlowDocument content wraps to the viewport width by default
                _contentHostScrollViewer.HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled;
                _contentHostScrollViewer.VerticalScrollBarVisibility = VerticalScrollBarVisibility;
            }
            _contentHostView = AttachContentHost(
                _contentHostScrollViewer,
                _contentHostView,
                MeasureContentView,
                RenderContentView);
        }

        public RichTextBox() : this(null)
        {
        }

        public RichTextBox(FlowDocument document)
        {
            if (document == null)
            {
                document = new FlowDocument();
                document.Blocks.Add(new Paragraph());
            }

            Document = document;
        }

        public FlowDocument Document
        {
            get => _document;
            set
            {
                ArgumentNullException.ThrowIfNull(value);
                if (ReferenceEquals(_document, value))
                {
                    return;
                }

                if (_document != null)
                {
                    _document.ContentChanged -= OnDocumentContentChanged;
                }

                _document = value;
                _document.ContentChanged += OnDocumentContentChanged;
                _caretOffset = Math.Clamp(_caretOffset, 0, _document.GetPlainTextLength());
                _selectionLength = 0;
                _selection = new TextSelection(_document.ContentStart, _document.ContentStart);
                InvalidateMeasure();
                InvalidateVisual();
            }
        }

        public bool IsDocumentEnabled
        {
            get => (bool)GetValue(IsDocumentEnabledProperty);
            set => SetValue(IsDocumentEnabledProperty, value);
        }

        public TextPointer CaretPosition
        {
            get => new TextPointer(_document, _caretOffset, LogicalDirection.Forward);
            set
            {
                ArgumentNullException.ThrowIfNull(value);
                _caretOffset = Math.Clamp(value.GetOffsetToPosition(_document.ContentStart) * -1, 0, _document.GetPlainTextLength());
                _selectionLength = 0;
                InvalidateVisual();
            }
        }

        public TextSelection Selection => _selection ??= new TextSelection(CaretPosition, CaretPosition);

        public override int SelectionStart
        {
            get => _caretOffset;
            set
            {
                _caretOffset = Math.Clamp(value, 0, _document?.GetPlainTextLength() ?? 0);
                UpdateSelectionObject();
                InvalidateVisual();
            }
        }

        public override int SelectionLength
        {
            get => _selectionLength;
            set
            {
                int max = Math.Max(0, (_document?.GetPlainTextLength() ?? 0) - _caretOffset);
                _selectionLength = Math.Clamp(value, 0, max);
                UpdateSelectionObject();
                InvalidateVisual();
            }
        }

        public override string SelectedText
        {
            get => Selection.Text;
            set
            {
                Selection.Text = value ?? string.Empty;
                _caretOffset += (value ?? string.Empty).Length;
                _selectionLength = 0;
                UpdateSelectionObject();
                OnTextChanged(new TextChangedEventArgs(TextChangedEvent));
                InvalidateMeasure();
                InvalidateVisual();
            }
        }

        protected internal override IEnumerator LogicalChildren => new SingleChildEnumerator(_document);

        public override void Select(int start, int length)
        {
            _caretOffset = Math.Clamp(start, 0, _document?.GetPlainTextLength() ?? 0);
            _selectionLength = Math.Max(0, length);
            UpdateSelectionObject();
            OnSelectionChanged(new RoutedEventArgs(SelectionChangedEvent, this));
            InvalidateVisual();
        }

        public override void SelectAll()
        {
            _caretOffset = 0;
            _selectionLength = _document?.GetPlainTextLength() ?? 0;
            UpdateSelectionObject();
            OnSelectionChanged(new RoutedEventArgs(SelectionChangedEvent, this));
            InvalidateVisual();
        }

        public override void Clear()
        {
            _document?.SetPlainText(string.Empty);
            _caretOffset = 0;
            _selectionLength = 0;
            UpdateSelectionObject();
            OnTextChanged(new TextChangedEventArgs(TextChangedEvent));
            InvalidateMeasure();
            InvalidateVisual();
        }

        public override void AppendText(string textData)
        {
            if (string.IsNullOrEmpty(textData) || _document == null) return;
            string next = (_document.GetPlainText() ?? string.Empty) + textData;
            _document.SetPlainText(next);
            _caretOffset = next.Length;
            _selectionLength = 0;
            UpdateSelectionObject();
            OnTextChanged(new TextChangedEventArgs(TextChangedEvent));
            InvalidateMeasure();
            InvalidateVisual();
        }

        public TextPointer GetPositionFromPoint(Point point, bool snapToText)
        {
            return CaretPosition;
        }

        void IAddChild.AddChild(object value)
        {
            ArgumentNullException.ThrowIfNull(value);
            if (value is FlowDocument doc)
            {
                Document = doc;
            }
            else if (value is Block b)
            {
                Document.Blocks.Add(b);
            }
            else if (value is Inline i)
            {
                Document.Blocks.Add(new Paragraph(i));
            }
            else
            {
                throw new ArgumentException("Unexpected child for RichTextBox.", nameof(value));
            }
        }

        void IAddChild.AddText(string text)
        {
            if (!string.IsNullOrEmpty(text))
            {
                Document.Blocks.Add(new Paragraph(new Run(text)));
            }
        }

        protected override Size MeasureOverride(Size constraint)
        {
            if (Template != null)
            {
                _contentHostView?.InvalidateMeasure();
                return base.MeasureOverride(constraint);
            }
            string plain = _document?.GetPlainText() ?? string.Empty;
            Thickness pad = Padding;
            Thickness border = BorderThickness;
            double availW = double.IsInfinity(constraint.Width)
                ? double.PositiveInfinity
                : Math.Max(0.0, constraint.Width - pad.Left - pad.Right - border.Left - border.Right);

            FormattedText ft = CreateFormattedText(plain.Length > 0 ? plain : " ", availW);
            double w = ft.Width + pad.Left + pad.Right + border.Left + border.Right + 4.0;
            double h = ft.Height + pad.Top + pad.Bottom + border.Top + border.Bottom + 4.0;
            return new Size(
                double.IsInfinity(constraint.Width) ? Math.Max(120.0, w) : Math.Min(constraint.Width, Math.Max(40.0, w)),
                double.IsInfinity(constraint.Height) ? Math.Max(48.0, h) : Math.Min(constraint.Height, Math.Max(24.0, h)));
        }

        private double GetViewportWidth()
        {
            if (Template != null && _contentHostScrollViewer != null && _contentHostScrollViewer.ViewportWidth > 0)
                return _contentHostScrollViewer.ViewportWidth;
            if (_contentHostView != null && _contentHostView.RenderSize.Width > 0)
                return _contentHostView.RenderSize.Width;
            return Math.Max(0.0, RenderSize.Width - Padding.Left - Padding.Right - BorderThickness.Left - BorderThickness.Right);
        }

        private Rect GetCaretRect(string plain, double availW, double xOrigin, double yOrigin, int index)
        {
            double defaultH = FontSize > 0 ? FontSize * 1.2 : 18.0;
            if (string.IsNullOrEmpty(plain))
            {
                FormattedText ftSpace = CreateFormattedText(" ", availW);
                return new Rect(xOrigin, yOrigin, 1.5, Math.Max(ftSpace.Height, defaultH));
            }

            int idx = Math.Clamp(index, 0, plain.Length);
            if (idx < plain.Length && plain[idx] != '\r' && plain[idx] != '\n')
            {
                FormattedText ft = CreateFormattedText(plain, availW);
                Geometry g = ft.BuildHighlightGeometry(new Point(xOrigin, yOrigin), idx, 1);
                if (g != null && !g.Bounds.IsEmpty)
                    return new Rect(g.Bounds.Left, g.Bounds.Top, 1.5, Math.Max(g.Bounds.Height, defaultH));
            }
            else if (idx > 0 && plain[idx - 1] != '\r' && plain[idx - 1] != '\n')
            {
                FormattedText ft = CreateFormattedText(plain, availW);
                Geometry g = ft.BuildHighlightGeometry(new Point(xOrigin, yOrigin), idx - 1, 1);
                if (g != null && !g.Bounds.IsEmpty)
                    return new Rect(g.Bounds.Right, g.Bounds.Top, 1.5, Math.Max(g.Bounds.Height, defaultH));
            }
            else
            {
                string withSentinel = plain.Insert(idx, " ");
                FormattedText ftS = CreateFormattedText(withSentinel, availW);
                Geometry g = ftS.BuildHighlightGeometry(new Point(xOrigin, yOrigin), idx, 1);
                if (g != null && !g.Bounds.IsEmpty)
                    return new Rect(g.Bounds.Left, g.Bounds.Top, 1.5, Math.Max(g.Bounds.Height, defaultH));
            }

            return new Rect(xOrigin, yOrigin, 1.5, defaultH);
        }

        private void EnsureCaretVisible()
        {
            if (Template == null || _contentHostScrollViewer == null) return;
            string plain = _document?.GetPlainText() ?? string.Empty;
            double availW = GetViewportWidth();
            Rect caretRect = GetCaretRect(plain, availW > 0 ? availW : double.PositiveInfinity, 0.0, 0.0, _caretOffset);
            ScrollCaretIntoView(_contentHostScrollViewer, caretRect.X, caretRect.Y, caretRect.Height);
        }

        private int GetOffsetFromPoint(Point point)
        {
            string plain = _document?.GetPlainText() ?? string.Empty;
            if (string.IsNullOrEmpty(plain)) return 0;

            double hOff = _contentHostScrollViewer?.HorizontalOffset ?? 0.0;
            double vOff = _contentHostScrollViewer?.VerticalOffset ?? 0.0;
            double localX = point.X - BorderThickness.Left - Padding.Left + hOff;
            double localY = point.Y - BorderThickness.Top - Padding.Top + vOff;

            double vpW = GetViewportWidth();
            double availW = vpW > 0 ? vpW : double.PositiveInfinity;
            FormattedText ft = CreateFormattedText(plain, availW);

            int bestIdx = 0;
            double bestScore = double.PositiveInfinity;
            for (int i = 0; i <= plain.Length; i++)
            {
                Rect cr = GetCaretRect(plain, availW, 0.0, 0.0, i);
                double dy = 0.0;
                if (localY < cr.Top) dy = cr.Top - localY;
                else if (localY > cr.Bottom) dy = localY - cr.Bottom;
                double dx = Math.Abs(cr.X - localX);
                double score = dy * 10000.0 + dx;
                if (score < bestScore)
                {
                    bestScore = score;
                    bestIdx = i;
                }
            }
            return bestIdx;
        }

        private Size MeasureContentView(Size constraint)
        {
            string plain = _document?.GetPlainText() ?? string.Empty;
            double availW = double.IsInfinity(constraint.Width)
                ? double.PositiveInfinity
                : Math.Max(0.0, constraint.Width);
            FormattedText ft = CreateFormattedText(plain.Length > 0 ? plain : " ", availW);
            return new Size(Math.Max(10.0, ft.Width + 2.0), Math.Max(18.0, ft.Height));
        }

        private void RenderRichTextContent(DrawingContext dc, double xStart, double yStart, double availW)
        {
            string plain = _document?.GetPlainText() ?? string.Empty;
            FormattedText ft = CreateFormattedText(plain.Length > 0 ? plain : " ", availW);

            if (!string.IsNullOrEmpty(plain) && _selectionLength > 0 && _caretOffset < plain.Length)
            {
                int selLen = Math.Min(_selectionLength, plain.Length - _caretOffset);
                Geometry selGeo = ft.BuildHighlightGeometry(new Point(xStart, yStart), _caretOffset, selLen);
                if (selGeo != null && !selGeo.Bounds.IsEmpty)
                {
                    Brush selBrush = ResolveSelectionBrush(SelectionBrush, SelectionOpacity);
                    dc.DrawGeometry(selBrush, null, selGeo);
                }
                Brush selTextBrush = SelectionTextBrush ?? SystemColors.HighlightTextBrush;
                ft.SetForegroundBrush(selTextBrush, _caretOffset, selLen);
            }

            if (!string.IsNullOrEmpty(plain))
            {
                dc.DrawText(ft, new Point(xStart, yStart));
            }

            if (ShouldRenderCaret(IsReadOnly))
            {
                Rect caretRect = GetCaretRect(plain, availW, xStart, yStart, _caretOffset);
                Pen caretPen = new(CaretBrush ?? Foreground ?? Brushes.Black, 1.5);
                dc.DrawLine(caretPen, new Point(caretRect.X, caretRect.Y), new Point(caretRect.X, caretRect.Y + caretRect.Height));
            }
        }

        internal override void InvalidateCaretVisual()
        {
            _contentHostView?.InvalidateVisual();
            InvalidateVisual();
        }

        private void RenderContentView(DrawingContext dc, Size bounds)
        {
            if (bounds.Width <= 0 || bounds.Height <= 0) return;
            double vpW = GetViewportWidth();
            double availW = vpW > 0 ? vpW : bounds.Width;
            RenderRichTextContent(dc, 0.0, 0.0, availW);
        }

        protected override void OnRender(DrawingContext dc)
        {
            if (Template != null)
            {
                base.OnRender(dc);
                _contentHostView?.InvalidateVisual();
                return;
            }
            Rect bounds = new Rect(0, 0, RenderSize.Width, RenderSize.Height);
            Brush bg = Background ?? Brushes.White;
            dc.DrawRectangle(bg, null, bounds);

            Thickness border = BorderThickness;
            if (border.Left > 0 && BorderBrush != null)
            {
                dc.DrawRectangle(null, new Pen(BorderBrush, border.Left), bounds);
            }

            Thickness pad = Padding;
            double x = pad.Left + border.Left;
            double y = pad.Top + border.Top;
            double availW = Math.Max(0.0, RenderSize.Width - x - pad.Right - border.Right);
            RenderRichTextContent(dc, x, y, availW);
        }

        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonDown(e);
            Focus();
            CaptureMouse();
            _isSelecting = true;
            int offset = GetOffsetFromPoint(e.GetPosition(this));
            _selectionAnchor = offset;
            _caretOffset = offset;
            _selectionLength = 0;
            UpdateSelectionObject();
            EnsureCaretVisible();
            OnSelectionChanged(new RoutedEventArgs(SelectionChangedEvent, this));
            _contentHostView?.InvalidateVisual();
            InvalidateVisual();
            e.Handled = true;
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (_isSelecting && IsMouseCaptured)
            {
                int offset = GetOffsetFromPoint(e.GetPosition(this));
                _caretOffset = Math.Min(_selectionAnchor, offset);
                _selectionLength = Math.Abs(offset - _selectionAnchor);
                UpdateSelectionObject();
                EnsureCaretVisible();
                OnSelectionChanged(new RoutedEventArgs(SelectionChangedEvent, this));
                _contentHostView?.InvalidateVisual();
                InvalidateVisual();
            }
        }

        protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonUp(e);
            if (_isSelecting)
            {
                _isSelecting = false;
                if (IsMouseCaptured) ReleaseMouseCapture();
                e.Handled = true;
            }
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (e.Handled || _document == null) return;

            bool isCtrl = (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control;
            bool isShift = (Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift;
            string current = _document.GetPlainText() ?? string.Empty;

            if (isCtrl && e.Key == Key.A)
            {
                SelectAll();
                e.Handled = true;
                return;
            }

            if (e.Key == Key.Back)
            {
                if (!IsReadOnly)
                {
                    if (_selectionLength > 0)
                    {
                        int start = Math.Clamp(_caretOffset, 0, current.Length);
                        int len = Math.Min(_selectionLength, current.Length - start);
                        _caretOffset = start;
                        _selectionAnchor = start;
                        _selectionLength = 0;
                        _document.SetPlainText(current.Substring(0, start) + current.Substring(start + len));
                    }
                    else if (_caretOffset > 0 && current.Length > 0)
                    {
                        int oldOffset = Math.Min(_caretOffset, current.Length);
                        int prev = GraphemeClusterHelper.GetPreviousClusterIndex(current, oldOffset);
                        _caretOffset = prev;
                        _selectionAnchor = prev;
                        _document.SetPlainText(current.Substring(0, prev) + current.Substring(oldOffset));
                    }
                    EnsureCaretVisible();
                }
                e.Handled = true;
            }
            else if (e.Key == Key.Delete)
            {
                if (!IsReadOnly)
                {
                    if (_selectionLength > 0)
                    {
                        int start = Math.Clamp(_caretOffset, 0, current.Length);
                        int len = Math.Min(_selectionLength, current.Length - start);
                        _caretOffset = start;
                        _selectionAnchor = start;
                        _selectionLength = 0;
                        _document.SetPlainText(current.Substring(0, start) + current.Substring(start + len));
                    }
                    else if (_caretOffset < current.Length)
                    {
                        int next = GraphemeClusterHelper.GetNextClusterIndex(current, _caretOffset);
                        _document.SetPlainText(current.Substring(0, _caretOffset) + current.Substring(next));
                    }
                    EnsureCaretVisible();
                }
                e.Handled = true;
            }
            else if (e.Key == Key.Left)
            {
                if (isShift)
                {
                    if (_selectionLength == 0) _selectionAnchor = _caretOffset;
                    int pos = _caretOffset > 0 ? GraphemeClusterHelper.GetPreviousClusterIndex(current, _caretOffset) : 0;
                    _caretOffset = Math.Min(pos, _selectionAnchor);
                    _selectionLength = Math.Abs(_selectionAnchor - pos);
                }
                else
                {
                    int target = _selectionLength > 0 ? _caretOffset : (_caretOffset > 0 ? GraphemeClusterHelper.GetPreviousClusterIndex(current, _caretOffset) : 0);
                    _selectionLength = 0;
                    _caretOffset = target;
                    _selectionAnchor = target;
                }
                UpdateSelectionObject();
                EnsureCaretVisible();
                _contentHostView?.InvalidateVisual();
                InvalidateVisual();
                e.Handled = true;
            }
            else if (e.Key == Key.Right)
            {
                if (isShift)
                {
                    if (_selectionLength == 0) _selectionAnchor = _caretOffset;
                    int curEnd = _caretOffset + _selectionLength;
                    int pos = curEnd < current.Length ? GraphemeClusterHelper.GetNextClusterIndex(current, curEnd) : current.Length;
                    _caretOffset = Math.Min(pos, _selectionAnchor);
                    _selectionLength = Math.Abs(pos - _selectionAnchor);
                }
                else
                {
                    int target = _selectionLength > 0 ? _caretOffset + _selectionLength : (_caretOffset < current.Length ? GraphemeClusterHelper.GetNextClusterIndex(current, _caretOffset) : current.Length);
                    _selectionLength = 0;
                    _caretOffset = target;
                    _selectionAnchor = target;
                }
                UpdateSelectionObject();
                EnsureCaretVisible();
                _contentHostView?.InvalidateVisual();
                InvalidateVisual();
                e.Handled = true;
            }
            else if (e.Key == Key.Home)
            {
                _selectionLength = 0;
                _caretOffset = 0;
                _selectionAnchor = 0;
                UpdateSelectionObject();
                EnsureCaretVisible();
                _contentHostView?.InvalidateVisual();
                InvalidateVisual();
                e.Handled = true;
            }
            else if (e.Key == Key.End)
            {
                _selectionLength = 0;
                _caretOffset = current.Length;
                _selectionAnchor = current.Length;
                UpdateSelectionObject();
                EnsureCaretVisible();
                _contentHostView?.InvalidateVisual();
                InvalidateVisual();
                e.Handled = true;
            }
            else if (e.Key == Key.Enter && AcceptsReturn && !IsReadOnly)
            {
                int start = Math.Clamp(_caretOffset, 0, current.Length);
                int len = Math.Clamp(_selectionLength, 0, current.Length - start);
                _document.SetPlainText(current.Substring(0, start) + "\n" + current.Substring(start + len));
                _caretOffset = start + 1;
                _selectionAnchor = _caretOffset;
                _selectionLength = 0;
                EnsureCaretVisible();
                e.Handled = true;
            }
            else if (e.Key == Key.Tab && AcceptsTab && !IsReadOnly)
            {
                int start = Math.Clamp(_caretOffset, 0, current.Length);
                int len = Math.Clamp(_selectionLength, 0, current.Length - start);
                _document.SetPlainText(current.Substring(0, start) + "\t" + current.Substring(start + len));
                _caretOffset = start + 1;
                _selectionAnchor = _caretOffset;
                _selectionLength = 0;
                EnsureCaretVisible();
                e.Handled = true;
            }
        }

        protected override void OnTextInput(TextCompositionEventArgs e)
        {
            base.OnTextInput(e);
            if (IsReadOnly || string.IsNullOrEmpty(e.Text) || _document == null)
            {
                return;
            }

            string current = _document.GetPlainText() ?? string.Empty;
            int start = Math.Clamp(_caretOffset, 0, current.Length);
            int len = Math.Clamp(_selectionLength, 0, current.Length - start);
            string updated = current.Substring(0, start) + e.Text + current.Substring(start + len);
            _document.SetPlainText(updated);
            _caretOffset = start + e.Text.Length;
            _selectionLength = 0;
            UpdateSelectionObject();
            OnTextChanged(new TextChangedEventArgs(TextChangedEvent));
            InvalidateMeasure();
            InvalidateVisual();
            e.Handled = true;
        }

        protected override AutomationPeer OnCreateAutomationPeer() => new RichTextBoxAutomationPeer(this);

        private void OnDocumentContentChanged(object sender, EventArgs e)
        {
            UpdateSelectionObject();
            OnTextChanged(new TextChangedEventArgs(TextChangedEvent));
            InvalidateMeasure();
            InvalidateVisual();
        }

        private void UpdateSelectionObject()
        {
            if (_document != null)
            {
                var start = new TextPointer(_document, _caretOffset, LogicalDirection.Forward);
                var end = new TextPointer(_document, _caretOffset + _selectionLength, LogicalDirection.Backward);
                _selection = new TextSelection(start, end);
            }
        }

        private FormattedText CreateFormattedText(string text, double maxWidth)
        {
            var typeface = new Typeface(FontFamily ?? new FontFamily("Segoe UI"), FontStyle, FontWeight, FontStretch);
            var ft = new FormattedText(
                text ?? string.Empty,
                CultureInfo.CurrentUICulture,
                FlowDirection.LeftToRight,
                typeface,
                FontSize > 0 ? FontSize : 14.0,
                Foreground ?? Brushes.Black,
                null,
                TextFormattingMode.Ideal,
                1.0);
            if (!double.IsInfinity(maxWidth) && maxWidth > 0)
            {
                ft.MaxTextWidth = maxWidth;
            }
            return ft;
        }
    }
}

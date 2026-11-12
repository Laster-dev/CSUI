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
using System.Windows.Markup;
using System.Windows.Media;

namespace System.Windows.Controls
{
    /// <summary>
    /// Managed RichTextBox operating on FlowDocument with DirectWrite GPU rendering (zero PresentationNative C++ PTS dependency).
    /// </summary>
    [ContentProperty(nameof(Document))]
    public class RichTextBox : TextBoxBase, IAddChild
    {
        public static readonly DependencyProperty IsDocumentEnabledProperty =
            DependencyProperty.Register(nameof(IsDocumentEnabled), typeof(bool), typeof(RichTextBox), new FrameworkPropertyMetadata(false));

        private FlowDocument _document;
        private int _caretOffset;
        private int _selectionLength;
        private TextSelection _selection;

        static RichTextBox()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(RichTextBox), new FrameworkPropertyMetadata(typeof(RichTextBox)));
            KeyboardNavigation.AcceptsReturnProperty.OverrideMetadata(typeof(RichTextBox), new FrameworkPropertyMetadata(true));
            AcceptsReturnProperty.OverrideMetadata(typeof(RichTextBox), new FrameworkPropertyMetadata(true));
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

        protected override void OnRender(DrawingContext dc)
        {
            Rect bounds = new Rect(0, 0, RenderSize.Width, RenderSize.Height);
            Brush bg = Background ?? Brushes.White;
            dc.DrawRectangle(bg, null, bounds);

            Thickness border = BorderThickness;
            if (border.Left > 0 && BorderBrush != null)
            {
                dc.DrawRectangle(null, new Pen(BorderBrush, border.Left), bounds);
            }

            string plain = _document?.GetPlainText() ?? string.Empty;
            if (!string.IsNullOrEmpty(plain))
            {
                Thickness pad = Padding;
                double x = pad.Left + border.Left;
                double y = pad.Top + border.Top;
                double availW = Math.Max(0.0, RenderSize.Width - x - pad.Right - border.Right);
                FormattedText ft = CreateFormattedText(plain, availW);
                dc.DrawText(ft, new Point(x, y));
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

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Input.Ime;
using System.Windows.Media;

namespace System.Windows.Controls
{
    /// <summary>
    /// Represents a control that can be used to display or edit unformatted text.
    /// Integrated with ModernImeService and SmoothCaretTracker.
    /// </summary>
    public class TextBox : TextBoxBase
    {
        public static readonly DependencyProperty TextProperty =
            DependencyProperty.Register(
                nameof(Text),
                typeof(string),
                typeof(TextBox),
                new FrameworkPropertyMetadata(
                    string.Empty,
                    FrameworkPropertyMetadataOptions.BindsTwoWayByDefault | FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender,
                    OnTextPropertyChanged));

        public static readonly DependencyProperty TextWrappingProperty =
            DependencyProperty.Register(
                nameof(TextWrapping),
                typeof(TextWrapping),
                typeof(TextBox),
                new FrameworkPropertyMetadata(
                    TextWrapping.NoWrap,
                    FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty TextAlignmentProperty =
            DependencyProperty.Register(
                nameof(TextAlignment),
                typeof(TextAlignment),
                typeof(TextBox),
                new FrameworkPropertyMetadata(
                    TextAlignment.Left,
                    FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty MaxLengthProperty =
            DependencyProperty.Register(
                nameof(MaxLength),
                typeof(int),
                typeof(TextBox),
                new FrameworkPropertyMetadata(0));

        public static readonly DependencyProperty CaretIndexProperty =
            DependencyProperty.Register(
                nameof(CaretIndex),
                typeof(int),
                typeof(TextBox),
                new FrameworkPropertyMetadata(0, OnCaretIndexChanged));

        public string Text
        {
            get => (string)GetValue(TextProperty) ?? string.Empty;
            set => SetValue(TextProperty, value ?? string.Empty);
        }

        public TextWrapping TextWrapping
        {
            get => (TextWrapping)GetValue(TextWrappingProperty);
            set => SetValue(TextWrappingProperty, value);
        }

        public TextAlignment TextAlignment
        {
            get => (TextAlignment)GetValue(TextAlignmentProperty);
            set => SetValue(TextAlignmentProperty, value);
        }

        public int MaxLength
        {
            get => (int)GetValue(MaxLengthProperty);
            set => SetValue(MaxLengthProperty, value);
        }

        public int CaretIndex
        {
            get => (int)GetValue(CaretIndexProperty);
            set => SetValue(CaretIndexProperty, Math.Clamp(value, 0, Text.Length));
        }

        private int _selectionStart = 0;
        private int _selectionLength = 0;
        private readonly SmoothCaretTracker _caretTracker = new(new Point(0, 0));
        private bool _isSelecting = false;

        public override int SelectionStart
        {
            get => _selectionStart;
            set
            {
                int clamped = Math.Clamp(value, 0, Text.Length);
                if (_selectionStart != clamped)
                {
                    _selectionStart = clamped;
                    CaretIndex = clamped;
                    OnSelectionChanged(new RoutedEventArgs(SelectionChangedEvent, this));
                }
            }
        }

        public override int SelectionLength
        {
            get => _selectionLength;
            set
            {
                int maxLen = Text.Length - _selectionStart;
                int clamped = Math.Clamp(value, 0, Math.Max(0, maxLen));
                if (_selectionLength != clamped)
                {
                    _selectionLength = clamped;
                    OnSelectionChanged(new RoutedEventArgs(SelectionChangedEvent, this));
                }
            }
        }

        public override string SelectedText
        {
            get
            {
                string text = Text;
                if (_selectionLength > 0 && _selectionStart < text.Length)
                {
                    int len = Math.Min(_selectionLength, text.Length - _selectionStart);
                    return text.Substring(_selectionStart, len);
                }
                return string.Empty;
            }
            set
            {
                if (IsReadOnly) return;
                string replacement = value ?? string.Empty;
                string text = Text;
                if (_selectionLength > 0 && _selectionStart < text.Length)
                {
                    int len = Math.Min(_selectionLength, text.Length - _selectionStart);
                    string before = text.Substring(0, _selectionStart);
                    string after = text.Substring(_selectionStart + len);
                    Text = before + replacement + after;
                    CaretIndex = _selectionStart + replacement.Length;
                    SelectionLength = 0;
                }
                else
                {
                    InsertTextAtCaret(replacement);
                }
            }
        }

        public SmoothCaretTracker CaretTracker => _caretTracker;

        static TextBox()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(TextBox), new FrameworkPropertyMetadata(typeof(TextBox)));
        }

        public TextBox()
        {
            _caretTracker.TargetPosition = new Point(Padding.Left + BorderThickness.Left, Padding.Top + BorderThickness.Top);
        }

        public override void Select(int start, int length)
        {
            int textLen = Text.Length;
            _selectionStart = Math.Clamp(start, 0, textLen);
            _selectionLength = Math.Clamp(length, 0, textLen - _selectionStart);
            CaretIndex = _selectionStart + _selectionLength;
            OnSelectionChanged(new RoutedEventArgs(SelectionChangedEvent, this));
        }

        public override void SelectAll()
        {
            Select(0, Text.Length);
        }

        public override void Clear()
        {
            Text = string.Empty;
            _selectionStart = 0;
            _selectionLength = 0;
            CaretIndex = 0;
        }

        public override void AppendText(string textData)
        {
            if (string.IsNullOrEmpty(textData)) return;
            Text += textData;
            CaretIndex = Text.Length;
        }

        public void Copy()
        {
            string sel = SelectedText;
            if (!string.IsNullOrEmpty(sel))
            {
                Clipboard.SetText(sel);
            }
        }

        public void Cut()
        {
            if (IsReadOnly) return;
            Copy();
            SelectedText = string.Empty;
        }

        public void Paste()
        {
            if (IsReadOnly) return;
            string clip = Clipboard.GetText();
            if (!string.IsNullOrEmpty(clip))
            {
                SelectedText = clip;
            }
        }

        private static void OnTextPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is TextBox tb)
            {
                string newText = (string)e.NewValue ?? string.Empty;
                tb.CaretIndex = Math.Clamp(tb.CaretIndex, 0, newText.Length);
                tb._selectionStart = Math.Clamp(tb._selectionStart, 0, newText.Length);
                tb._selectionLength = Math.Clamp(tb._selectionLength, 0, newText.Length - tb._selectionStart);
                tb.UpdateCaretTarget();
                tb.OnTextChanged(new TextChangedEventArgs(TextChangedEvent));
            }
        }

        private static void OnCaretIndexChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is TextBox tb)
            {
                tb.UpdateCaretTarget();
            }
        }

        private void UpdateCaretTarget()
        {
            double xOffset = Padding.Left + BorderThickness.Left;
            double yOffset = Padding.Top + BorderThickness.Top;

            string text = Text;
            int idx = Math.Clamp(CaretIndex, 0, text.Length);

            if (idx > 0)
            {
                string sub = text.Substring(0, idx);
                FormattedText ft = CreateFormattedText(sub, double.PositiveInfinity);
                xOffset += ft.Width;
            }

            _caretTracker.TargetPosition = new Point(xOffset, yOffset);
        }

        private FormattedText CreateFormattedText(string text, double constraintWidth)
        {
            Typeface typeface = new(FontFamily ?? new FontFamily("Segoe UI"), FontStyle, FontWeight, FontStretch);
            FormattedText ft = new(
                text ?? string.Empty,
                CultureInfo.CurrentUICulture,
                FlowDirection.LeftToRight,
                typeface,
                FontSize > 0 ? FontSize : 14.0,
                Foreground ?? Brushes.Black,
                null,
                TextFormattingMode.Ideal,
                1.0);

            if (TextWrapping == TextWrapping.Wrap && !double.IsPositiveInfinity(constraintWidth))
            {
                ft.MaxTextWidth = Math.Max(0.0, constraintWidth);
            }

            return ft;
        }

        private void InsertTextAtCaret(string text)
        {
            if (IsReadOnly || string.IsNullOrEmpty(text)) return;

            if (MaxLength > 0 && Text.Length + text.Length > MaxLength)
            {
                int allowed = MaxLength - Text.Length;
                if (allowed <= 0) return;
                text = text.Substring(0, allowed);
            }

            string current = Text;
            int idx = Math.Clamp(CaretIndex, 0, current.Length);

            string before = current.Substring(0, idx);
            string after = current.Substring(idx);

            Text = before + text + after;
            CaretIndex = idx + text.Length;
            SelectionStart = CaretIndex;
            SelectionLength = 0;
        }

        protected override void OnTextInput(TextCompositionEventArgs e)
        {
            base.OnTextInput(e);

            if (!e.Handled && !string.IsNullOrEmpty(e.Text))
            {
                SelectedText = e.Text;
                e.Handled = true;
            }
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);

            if (e.Handled) return;

            bool isCtrl = (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control;
            bool isShift = (Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift;

            if (isCtrl)
            {
                if (e.Key == Key.A)
                {
                    SelectAll();
                    e.Handled = true;
                }
                else if (e.Key == Key.C)
                {
                    Copy();
                    e.Handled = true;
                }
                else if (e.Key == Key.V)
                {
                    Paste();
                    e.Handled = true;
                }
                else if (e.Key == Key.X)
                {
                    Cut();
                    e.Handled = true;
                }
                return;
            }

            if (e.Key == Key.Back)
            {
                if (!IsReadOnly)
                {
                    if (SelectionLength > 0)
                    {
                        SelectedText = string.Empty;
                    }
                    else if (CaretIndex > 0)
                    {
                        // Use GraphemeClusterHelper to safely remove complex emoji or combining marks
                        int prevCluster = GraphemeClusterHelper.GetPreviousClusterIndex(Text, CaretIndex);
                        int count = CaretIndex - prevCluster;
                        string cur = Text;
                        Text = cur.Substring(0, prevCluster) + cur.Substring(CaretIndex);
                        CaretIndex = prevCluster;
                    }
                }
                e.Handled = true;
            }
            else if (e.Key == Key.Delete)
            {
                if (!IsReadOnly)
                {
                    if (SelectionLength > 0)
                    {
                        SelectedText = string.Empty;
                    }
                    else if (CaretIndex < Text.Length)
                    {
                        int nextCluster = GraphemeClusterHelper.GetNextClusterIndex(Text, CaretIndex);
                        string cur = Text;
                        Text = cur.Substring(0, CaretIndex) + cur.Substring(nextCluster);
                    }
                }
                e.Handled = true;
            }
            else if (e.Key == Key.Left)
            {
                if (CaretIndex > 0)
                {
                    int prev = GraphemeClusterHelper.GetPreviousClusterIndex(Text, CaretIndex);
                    if (isShift)
                    {
                        int anchor = _selectionStart + _selectionLength;
                        CaretIndex = prev;
                        _selectionStart = Math.Min(prev, anchor);
                        _selectionLength = Math.Abs(anchor - prev);
                    }
                    else
                    {
                        CaretIndex = prev;
                        _selectionLength = 0;
                    }
                }
                e.Handled = true;
            }
            else if (e.Key == Key.Right)
            {
                if (CaretIndex < Text.Length)
                {
                    int next = GraphemeClusterHelper.GetNextClusterIndex(Text, CaretIndex);
                    if (isShift)
                    {
                        int anchor = _selectionStart;
                        CaretIndex = next;
                        _selectionStart = Math.Min(next, anchor);
                        _selectionLength = Math.Abs(next - anchor);
                    }
                    else
                    {
                        CaretIndex = next;
                        _selectionLength = 0;
                    }
                }
                e.Handled = true;
            }
            else if (e.Key == Key.Home)
            {
                CaretIndex = 0;
                if (!isShift) _selectionLength = 0;
                e.Handled = true;
            }
            else if (e.Key == Key.End)
            {
                CaretIndex = Text.Length;
                if (!isShift) _selectionLength = 0;
                e.Handled = true;
            }
            else if (e.Key == Key.Enter && AcceptsReturn)
            {
                SelectedText = "\n";
                e.Handled = true;
            }
            else if (e.Key == Key.Tab && AcceptsTab)
            {
                SelectedText = "\t";
                e.Handled = true;
            }
        }

        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonDown(e);

            Focus();
            CaptureMouse();
            _isSelecting = true;

            Point pt = e.GetPosition(this);
            double contentX = Math.Max(0.0, pt.X - Padding.Left - BorderThickness.Left);

            // Approximate character hit test
            string text = Text;
            int bestIdx = 0;
            double bestDist = double.PositiveInfinity;
            for (int i = 0; i <= text.Length; i++)
            {
                string sub = text.Substring(0, i);
                FormattedText ft = CreateFormattedText(sub, double.PositiveInfinity);
                double dist = Math.Abs(ft.Width - contentX);
                if (dist < bestDist)
                {
                    bestDist = dist;
                    bestIdx = i;
                }
            }

            CaretIndex = bestIdx;
            SelectionStart = bestIdx;
            SelectionLength = 0;
            e.Handled = true;
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);

            if (_isSelecting && IsMouseCaptured)
            {
                Point pt = e.GetPosition(this);
                double contentX = Math.Max(0.0, pt.X - Padding.Left - BorderThickness.Left);

                string text = Text;
                int bestIdx = 0;
                double bestDist = double.PositiveInfinity;
                for (int i = 0; i <= text.Length; i++)
                {
                    string sub = text.Substring(0, i);
                    FormattedText ft = CreateFormattedText(sub, double.PositiveInfinity);
                    double dist = Math.Abs(ft.Width - contentX);
                    if (dist < bestDist)
                    {
                        bestDist = dist;
                        bestIdx = i;
                    }
                }

                int start = Math.Min(_selectionStart, bestIdx);
                int len = Math.Abs(bestIdx - _selectionStart);
                _selectionLength = len;
                CaretIndex = bestIdx;
                OnSelectionChanged(new RoutedEventArgs(SelectionChangedEvent, this));
            }
        }

        protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonUp(e);

            if (_isSelecting)
            {
                _isSelecting = false;
                if (IsMouseCaptured)
                {
                    ReleaseMouseCapture();
                }
                e.Handled = true;
            }
        }

        protected override Size MeasureOverride(Size constraint)
        {
            double horizontalPadding = Padding.Left + Padding.Right + BorderThickness.Left + BorderThickness.Right;
            double verticalPadding = Padding.Top + Padding.Bottom + BorderThickness.Top + BorderThickness.Bottom;

            double availableW = Math.Max(0.0, constraint.Width - horizontalPadding);
            FormattedText ft = CreateFormattedText(string.IsNullOrEmpty(Text) ? " " : Text, availableW);

            double desiredW = ft.Width + horizontalPadding;
            double desiredH = ft.Height + verticalPadding;

            return new Size(desiredW, desiredH);
        }

        protected override void OnRender(DrawingContext dc)
        {
            base.OnRender(dc);

            Rect bounds = new(0, 0, RenderSize.Width, RenderSize.Height);
            if (bounds.Width <= 0 || bounds.Height <= 0) return;

            // 1. Background
            Brush bg = Background ?? Brushes.White;
            Brush borderBrush = BorderBrush ?? Brushes.Gray;
            Thickness borderThickness = BorderThickness;

            dc.DrawRectangle(bg, null, bounds);

            // 2. Border
            if (borderThickness.Left > 0 && borderBrush != null)
            {
                Pen pen = new(borderBrush, borderThickness.Left);
                dc.DrawRectangle(null, pen, new Rect(borderThickness.Left * 0.5, borderThickness.Top * 0.5, Math.Max(0, bounds.Width - borderThickness.Left), Math.Max(0, bounds.Height - borderThickness.Top)));
            }

            // 3. Selection Highlight
            double xStart = Padding.Left + borderThickness.Left;
            double yStart = Padding.Top + borderThickness.Top;

            if (_selectionLength > 0 && _selectionStart < Text.Length)
            {
                string before = Text.Substring(0, _selectionStart);
                string selected = SelectedText;
                FormattedText ftBefore = CreateFormattedText(before, double.PositiveInfinity);
                FormattedText ftSelected = CreateFormattedText(selected, double.PositiveInfinity);

                Rect selRect = new(xStart + ftBefore.Width, yStart, ftSelected.Width, ftSelected.Height);
                dc.DrawRectangle(new SolidColorBrush(Color.FromArgb(100, 0, 120, 215)), null, selRect);
            }

            // 4. Text
            if (!string.IsNullOrEmpty(Text))
            {
                FormattedText ft = CreateFormattedText(Text, Math.Max(0, bounds.Width - xStart - Padding.Right - borderThickness.Right));
                dc.DrawText(ft, new Point(xStart, yStart));
            }

            // 5. Smooth Caret
            if (IsFocused && !IsReadOnly)
            {
                Point caretPt = _caretTracker.TargetPosition;
                double h = FontSize > 0 ? FontSize * 1.2 : 18.0;
                Pen caretPen = new(Foreground ?? Brushes.Black, 1.5);
                dc.DrawLine(caretPen, caretPt, new Point(caretPt.X, caretPt.Y + h));
            }
        }
    }
}

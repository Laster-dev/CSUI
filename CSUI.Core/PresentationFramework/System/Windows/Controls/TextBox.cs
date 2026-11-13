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
    public partial class TextBox : TextBoxBase
    {
        static partial void RegisterDefaultTheme();
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
                    FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender,
                    OnTextWrappingChanged));

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

        public static readonly DependencyProperty MaxLinesProperty =
            DependencyProperty.Register(
                nameof(MaxLines),
                typeof(int),
                typeof(TextBox),
                new FrameworkPropertyMetadata(int.MaxValue));

        public static readonly DependencyProperty MinLinesProperty =
            DependencyProperty.Register(
                nameof(MinLines),
                typeof(int),
                typeof(TextBox),
                new FrameworkPropertyMetadata(1));

        public int MaxLines
        {
            get => (int)GetValue(MaxLinesProperty);
            set => SetValue(MaxLinesProperty, value);
        }

        public int MinLines
        {
            get => (int)GetValue(MinLinesProperty);
            set => SetValue(MinLinesProperty, value);
        }

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
        private int _selectionAnchor = 0;
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
                    _selectionAnchor = clamped;
                    CaretIndex = clamped;
                    OnSelectionChanged(new RoutedEventArgs(SelectionChangedEvent, this));
                    _contentHostView?.InvalidateVisual();
                    InvalidateVisual();
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
                    _contentHostView?.InvalidateVisual();
                    InvalidateVisual();
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
            RegisterDefaultTheme();
        }

        private TextContentHostView _contentHostView;
        private ScrollViewer _contentHostScrollViewer;

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            _contentHostScrollViewer = GetTemplateChild("PART_ContentHost") as ScrollViewer;
            SyncScrollViewerVisibility();
            _contentHostView = AttachContentHost(
                _contentHostScrollViewer,
                _contentHostView,
                MeasureContentView,
                RenderContentView);
        }

        private void SyncScrollViewerVisibility()
        {
            if (_contentHostScrollViewer != null)
            {
                _contentHostScrollViewer.HorizontalScrollBarVisibility =
                    TextWrapping != TextWrapping.NoWrap ? ScrollBarVisibility.Disabled : HorizontalScrollBarVisibility;
                _contentHostScrollViewer.VerticalScrollBarVisibility = VerticalScrollBarVisibility;
            }
        }

        private static void OnTextWrappingChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is TextBox tb)
            {
                tb.SyncScrollViewerVisibility();
                tb.UpdateCaretTarget();
            }
        }

        public TextBox()
        {
            _caretTracker.TargetPosition = new Point(Padding.Left + BorderThickness.Left, Padding.Top + BorderThickness.Top);
        }

        public override void Select(int start, int length)
        {
            int textLen = Text.Length;
            _selectionStart = Math.Clamp(start, 0, textLen);
            _selectionAnchor = _selectionStart;
            _selectionLength = Math.Clamp(length, 0, textLen - _selectionStart);
            CaretIndex = _selectionStart + _selectionLength;
            OnSelectionChanged(new RoutedEventArgs(SelectionChangedEvent, this));
            _contentHostView?.InvalidateVisual();
            InvalidateVisual();
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

        private double GetContentConstraintWidth()
        {
            if (TextWrapping == TextWrapping.NoWrap)
                return double.PositiveInfinity;
            if (Template != null && _contentHostScrollViewer != null && _contentHostScrollViewer.ViewportWidth > 0)
                return _contentHostScrollViewer.ViewportWidth;
            double w = RenderSize.Width - Padding.Left - Padding.Right - BorderThickness.Left - BorderThickness.Right;
            return w > 0 ? w : double.PositiveInfinity;
        }

        private double GetViewportWidth()
        {
            if (Template != null && _contentHostScrollViewer != null && _contentHostScrollViewer.ViewportWidth > 0)
                return _contentHostScrollViewer.ViewportWidth;
            if (_contentHostView != null && _contentHostView.RenderSize.Width > 0)
                return _contentHostView.RenderSize.Width;
            return Math.Max(0.0, RenderSize.Width - Padding.Left - Padding.Right - BorderThickness.Left - BorderThickness.Right);
        }

        private double GetViewportHeight()
        {
            if (Template != null && _contentHostScrollViewer != null && _contentHostScrollViewer.ViewportHeight > 0)
                return _contentHostScrollViewer.ViewportHeight;
            if (_contentHostView != null && _contentHostView.RenderSize.Height > 0)
                return _contentHostView.RenderSize.Height;
            return Math.Max(0.0, RenderSize.Height - Padding.Top - Padding.Bottom - BorderThickness.Top - BorderThickness.Bottom);
        }

        private double GetHorizontalTextOffset(FormattedText ft, double availW)
        {
            if (TextWrapping != TextWrapping.NoWrap || double.IsInfinity(availW) || availW <= ft.Width)
                return 0.0;

            if (TextAlignment == TextAlignment.Center || (TextAlignment == TextAlignment.Left && HorizontalContentAlignment == HorizontalAlignment.Center))
                return (availW - ft.Width) * 0.5;
            if (TextAlignment == TextAlignment.Right || (TextAlignment == TextAlignment.Left && HorizontalContentAlignment == HorizontalAlignment.Right))
                return availW - ft.Width;
            return 0.0;
        }

        private Rect GetCaretRect(string text, double availW, double xOrigin, double yOrigin, int index)
        {
            double defaultH = FontSize > 0 ? FontSize * 1.2 : 18.0;
            if (string.IsNullOrEmpty(text))
            {
                FormattedText ftSpace = CreateFormattedText(" ", availW);
                double cx = xOrigin;
                if (!double.IsInfinity(availW))
                {
                    if (TextAlignment == TextAlignment.Center || (TextAlignment == TextAlignment.Left && HorizontalContentAlignment == HorizontalAlignment.Center))
                        cx = xOrigin + availW * 0.5;
                    else if (TextAlignment == TextAlignment.Right || (TextAlignment == TextAlignment.Left && HorizontalContentAlignment == HorizontalAlignment.Right))
                        cx = xOrigin + availW;
                }
                return new Rect(cx, yOrigin, 1.5, Math.Max(ftSpace.Height, defaultH));
            }

            int idx = Math.Clamp(index, 0, text.Length);
            if (idx < text.Length && text[idx] != '\r' && text[idx] != '\n')
            {
                FormattedText ft = CreateFormattedText(text, availW);
                Geometry g = ft.BuildHighlightGeometry(new Point(xOrigin, yOrigin), idx, 1);
                if (g != null && !g.Bounds.IsEmpty)
                {
                    return new Rect(g.Bounds.Left, g.Bounds.Top, 1.5, Math.Max(g.Bounds.Height, defaultH));
                }
            }
            else if (idx > 0 && text[idx - 1] != '\r' && text[idx - 1] != '\n')
            {
                FormattedText ft = CreateFormattedText(text, availW);
                Geometry g = ft.BuildHighlightGeometry(new Point(xOrigin, yOrigin), idx - 1, 1);
                if (g != null && !g.Bounds.IsEmpty)
                {
                    return new Rect(g.Bounds.Right, g.Bounds.Top, 1.5, Math.Max(g.Bounds.Height, defaultH));
                }
            }
            else
            {
                string withSentinel = text.Insert(idx, " ");
                FormattedText ftS = CreateFormattedText(withSentinel, availW);
                Geometry g = ftS.BuildHighlightGeometry(new Point(xOrigin, yOrigin), idx, 1);
                if (g != null && !g.Bounds.IsEmpty)
                {
                    return new Rect(g.Bounds.Left, g.Bounds.Top, 1.5, Math.Max(g.Bounds.Height, defaultH));
                }
            }

            return new Rect(xOrigin, yOrigin, 1.5, defaultH);
        }

        private void UpdateCaretTarget()
        {
            string text = Text;
            double vpW = GetViewportWidth();
            double vpH = GetViewportHeight();
            double constraintW = GetContentConstraintWidth();
            FormattedText ft = CreateFormattedText(string.IsNullOrEmpty(text) ? " " : text, constraintW);

            double baseX = Template != null ? 0.0 : Padding.Left + BorderThickness.Left;
            double baseY = Template != null ? 0.0 : Padding.Top + BorderThickness.Top;
            double xOrigin = baseX + GetHorizontalTextOffset(ft, vpW);
            double yOrigin = baseY + GetVerticalAlignmentOffset(VerticalContentAlignment, vpH, ft.Height);

            Rect caretRect = GetCaretRect(text, constraintW, xOrigin, yOrigin, CaretIndex);
            _caretTracker.TargetPosition = new Point(caretRect.X, caretRect.Y);

            if (Template != null && _contentHostScrollViewer != null)
            {
                ScrollCaretIntoView(_contentHostScrollViewer, caretRect.X, caretRect.Y, caretRect.Height);
            }

            _contentHostView?.InvalidateVisual();
            InvalidateVisual();
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

            ft.TextAlignment = TextAlignment;

            if (TextWrapping != TextWrapping.NoWrap && !double.IsPositiveInfinity(constraintWidth))
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
                if (isShift)
                {
                    if (_selectionLength == 0) _selectionAnchor = CaretIndex;
                    if (CaretIndex > 0)
                    {
                        int prev = GraphemeClusterHelper.GetPreviousClusterIndex(Text, CaretIndex);
                        _selectionStart = Math.Min(prev, _selectionAnchor);
                        _selectionLength = Math.Abs(_selectionAnchor - prev);
                        CaretIndex = prev;
                        OnSelectionChanged(new RoutedEventArgs(SelectionChangedEvent, this));
                    }
                }
                else
                {
                    int target = _selectionLength > 0
                        ? _selectionStart
                        : (CaretIndex > 0 ? GraphemeClusterHelper.GetPreviousClusterIndex(Text, CaretIndex) : 0);
                    _selectionLength = 0;
                    _selectionStart = target;
                    _selectionAnchor = target;
                    CaretIndex = target;
                }
                e.Handled = true;
            }
            else if (e.Key == Key.Right)
            {
                if (isShift)
                {
                    if (_selectionLength == 0) _selectionAnchor = CaretIndex;
                    if (CaretIndex < Text.Length)
                    {
                        int next = GraphemeClusterHelper.GetNextClusterIndex(Text, CaretIndex);
                        _selectionStart = Math.Min(next, _selectionAnchor);
                        _selectionLength = Math.Abs(next - _selectionAnchor);
                        CaretIndex = next;
                        OnSelectionChanged(new RoutedEventArgs(SelectionChangedEvent, this));
                    }
                }
                else
                {
                    int target = _selectionLength > 0
                        ? _selectionStart + _selectionLength
                        : (CaretIndex < Text.Length ? GraphemeClusterHelper.GetNextClusterIndex(Text, CaretIndex) : Text.Length);
                    _selectionLength = 0;
                    _selectionStart = target;
                    _selectionAnchor = target;
                    CaretIndex = target;
                }
                e.Handled = true;
            }
            else if (e.Key == Key.Home)
            {
                if (isShift)
                {
                    if (_selectionLength == 0) _selectionAnchor = CaretIndex;
                    _selectionStart = 0;
                    _selectionLength = _selectionAnchor;
                    CaretIndex = 0;
                    OnSelectionChanged(new RoutedEventArgs(SelectionChangedEvent, this));
                }
                else
                {
                    _selectionLength = 0;
                    _selectionStart = 0;
                    _selectionAnchor = 0;
                    CaretIndex = 0;
                }
                e.Handled = true;
            }
            else if (e.Key == Key.End)
            {
                int endIdx = Text.Length;
                if (isShift)
                {
                    if (_selectionLength == 0) _selectionAnchor = CaretIndex;
                    _selectionStart = _selectionAnchor;
                    _selectionLength = endIdx - _selectionAnchor;
                    CaretIndex = endIdx;
                    OnSelectionChanged(new RoutedEventArgs(SelectionChangedEvent, this));
                }
                else
                {
                    _selectionLength = 0;
                    _selectionStart = endIdx;
                    _selectionAnchor = endIdx;
                    CaretIndex = endIdx;
                }
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
            int bestIdx = GetCharacterIndexFromPoint(pt, snapToText: true);

            _selectionAnchor = bestIdx;
            _selectionStart = bestIdx;
            _selectionLength = 0;
            CaretIndex = bestIdx;
            OnSelectionChanged(new RoutedEventArgs(SelectionChangedEvent, this));
            e.Handled = true;
        }

        public int GetCharacterIndexFromPoint(Point point, bool snapToText)
        {
            string text = Text;
            if (string.IsNullOrEmpty(text))
            {
                return snapToText ? 0 : -1;
            }

            double hOff = _contentHostScrollViewer?.HorizontalOffset ?? 0.0;
            double vOff = _contentHostScrollViewer?.VerticalOffset ?? 0.0;
            double localX = point.X - BorderThickness.Left - Padding.Left + hOff;
            double localY = point.Y - BorderThickness.Top - Padding.Top + vOff;

            double vpW = GetViewportWidth();
            double vpH = GetViewportHeight();
            double constraintW = GetContentConstraintWidth();
            FormattedText ft = CreateFormattedText(text, constraintW);

            double xOrigin = GetHorizontalTextOffset(ft, vpW);
            double yOrigin = GetVerticalAlignmentOffset(VerticalContentAlignment, vpH, ft.Height);

            if (!snapToText)
            {
                if (point.X < 0 || point.Y < 0 || point.X > RenderSize.Width || point.Y > RenderSize.Height ||
                    localX < xOrigin - 4.0 || localX > xOrigin + ft.Width + 4.0 ||
                    localY < yOrigin - 4.0 || localY > yOrigin + ft.Height + 4.0)
                {
                    return -1;
                }
            }

            int bestIdx = 0;
            double bestScore = double.PositiveInfinity;
            Point origin = new Point(xOrigin, yOrigin);

            for (int i = 0; i <= text.Length; i++)
            {
                Rect cr = GetCaretRectOnFormattedText(ft, text, origin, constraintW, i);
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

        private Rect GetCaretRectOnFormattedText(FormattedText ft, string text, Point origin, double availW, int index)
        {
            double defaultH = FontSize > 0 ? FontSize * 1.2 : 18.0;
            if (index < text.Length && text[index] != '\r' && text[index] != '\n')
            {
                Geometry g = ft.BuildHighlightGeometry(origin, index, 1);
                if (g != null && !g.Bounds.IsEmpty)
                {
                    return new Rect(g.Bounds.Left, g.Bounds.Top, 1.5, Math.Max(g.Bounds.Height, defaultH));
                }
            }
            else if (index > 0 && text[index - 1] != '\r' && text[index - 1] != '\n')
            {
                Geometry g = ft.BuildHighlightGeometry(origin, index - 1, 1);
                if (g != null && !g.Bounds.IsEmpty)
                {
                    return new Rect(g.Bounds.Right, g.Bounds.Top, 1.5, Math.Max(g.Bounds.Height, defaultH));
                }
            }
            return GetCaretRect(text, availW, origin.X, origin.Y, index);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);

            if (_isSelecting && IsMouseCaptured)
            {
                Point pt = e.GetPosition(this);
                int bestIdx = GetCharacterIndexFromPoint(pt, snapToText: true);

                _selectionStart = Math.Min(_selectionAnchor, bestIdx);
                _selectionLength = Math.Abs(bestIdx - _selectionAnchor);
                CaretIndex = bestIdx;
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
                if (IsMouseCaptured)
                {
                    ReleaseMouseCapture();
                }
                e.Handled = true;
            }
        }

        protected override Size MeasureOverride(Size constraint)
        {
            if (Template != null)
            {
                _contentHostView?.InvalidateMeasure();
                return base.MeasureOverride(constraint);
            }
            double horizontalPadding = Padding.Left + Padding.Right + BorderThickness.Left + BorderThickness.Right;
            double verticalPadding = Padding.Top + Padding.Bottom + BorderThickness.Top + BorderThickness.Bottom;

            double availableW = Math.Max(0.0, constraint.Width - horizontalPadding);
            FormattedText ft = CreateFormattedText(string.IsNullOrEmpty(Text) ? " " : Text, availableW);

            double desiredW = ft.Width + horizontalPadding;
            double desiredH = ft.Height + verticalPadding;

            return new Size(desiredW, desiredH);
        }

        private Size MeasureContentView(Size constraint)
        {
            double availableW = TextWrapping != TextWrapping.NoWrap ? Math.Max(0.0, constraint.Width) : double.PositiveInfinity;
            FormattedText ft = CreateFormattedText(string.IsNullOrEmpty(Text) ? " " : Text, availableW);
            return new Size(ft.Width + 2.0, ft.Height);
        }

        private void RenderTextContent(DrawingContext dc, double baseX, double baseY, double availW, double availH)
        {
            string text = Text;
            double constraintW = TextWrapping != TextWrapping.NoWrap ? availW : double.PositiveInfinity;
            FormattedText ft = CreateFormattedText(string.IsNullOrEmpty(text) ? " " : text, constraintW);

            double xStart = baseX + GetHorizontalTextOffset(ft, availW);
            double yStart = baseY + GetVerticalAlignmentOffset(VerticalContentAlignment, availH, ft.Height);

            if (!string.IsNullOrEmpty(text) && _selectionLength > 0 && _selectionStart < text.Length)
            {
                int selLen = Math.Min(_selectionLength, text.Length - _selectionStart);
                Geometry selGeo = ft.BuildHighlightGeometry(new Point(xStart, yStart), _selectionStart, selLen);
                if (selGeo != null && !selGeo.Bounds.IsEmpty)
                {
                    Brush selBrush = ResolveSelectionBrush(SelectionBrush, SelectionOpacity);
                    dc.DrawGeometry(selBrush, null, selGeo);
                }

                Brush selTextBrush = SelectionTextBrush ?? SystemColors.HighlightTextBrush;
                ft.SetForegroundBrush(selTextBrush, _selectionStart, selLen);
            }

            if (!string.IsNullOrEmpty(text))
            {
                dc.DrawText(ft, new Point(xStart, yStart));
            }

            if (ShouldRenderCaret(IsReadOnly))
            {
                Rect caretRect = GetCaretRect(text, constraintW, xStart, yStart, CaretIndex);
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
            double vpH = GetViewportHeight();
            double availW = vpW > 0 ? vpW : bounds.Width;
            double availH = vpH > 0 ? vpH : bounds.Height;

            RenderTextContent(dc, 0.0, 0.0, availW, availH);
        }

        protected override void OnRender(DrawingContext dc)
        {
            base.OnRender(dc);
            if (Template != null)
            {
                _contentHostView?.InvalidateVisual();
                return;
            }

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

            double xStart = Padding.Left + borderThickness.Left;
            double yStart = Padding.Top + borderThickness.Top;
            double availW = Math.Max(0, bounds.Width - xStart - Padding.Right - borderThickness.Right);
            double availH = Math.Max(0, bounds.Height - yStart - Padding.Bottom - borderThickness.Bottom);

            RenderTextContent(dc, xStart, yStart, availW, availH);
        }

        protected override System.Windows.Automation.Peers.AutomationPeer OnCreateAutomationPeer() =>
            new System.Windows.Automation.Peers.TextBoxAutomationPeer(this);
    }
}

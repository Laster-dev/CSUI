// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System;
using System.Globalization;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace System.Windows.Controls
{
    /// <summary>
    /// Represents a control designed for entering and handling passwords.
    /// </summary>
    public partial class PasswordBox : Control
    {
        static partial void RegisterDefaultTheme();
        public static readonly DependencyProperty PasswordProperty =
            DependencyProperty.Register(
                nameof(Password),
                typeof(string),
                typeof(PasswordBox),
                new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.AffectsRender, OnPasswordPropertyChanged));

        public static readonly DependencyProperty PasswordCharProperty =
            DependencyProperty.Register(
                nameof(PasswordChar),
                typeof(char),
                typeof(PasswordBox),
                new FrameworkPropertyMetadata('●', FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty MaxLengthProperty =
            DependencyProperty.Register(
                nameof(MaxLength),
                typeof(int),
                typeof(PasswordBox),
                new FrameworkPropertyMetadata(0));

        public static readonly DependencyProperty SelectionBrushProperty =
            DependencyProperty.Register(
                nameof(SelectionBrush),
                typeof(Brush),
                typeof(PasswordBox),
                new FrameworkPropertyMetadata(SystemColors.HighlightBrush, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty SelectionTextBrushProperty =
            DependencyProperty.Register(
                nameof(SelectionTextBrush),
                typeof(Brush),
                typeof(PasswordBox),
                new FrameworkPropertyMetadata(SystemColors.HighlightTextBrush, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty SelectionOpacityProperty =
            DependencyProperty.Register(
                nameof(SelectionOpacity),
                typeof(double),
                typeof(PasswordBox),
                new FrameworkPropertyMetadata(0.4, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty CaretBrushProperty =
            DependencyProperty.Register(
                nameof(CaretBrush),
                typeof(Brush),
                typeof(PasswordBox),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty IsInactiveSelectionHighlightEnabledProperty =
            DependencyProperty.Register(
                nameof(IsInactiveSelectionHighlightEnabled),
                typeof(bool),
                typeof(PasswordBox),
                new FrameworkPropertyMetadata(false));

        private static readonly DependencyPropertyKey IsSelectionActivePropertyKey =
            DependencyProperty.RegisterReadOnly(
                nameof(IsSelectionActive),
                typeof(bool),
                typeof(PasswordBox),
                new FrameworkPropertyMetadata(false));

        public static readonly DependencyProperty IsSelectionActiveProperty =
            IsSelectionActivePropertyKey.DependencyProperty;

        public static readonly RoutedEvent PasswordChangedEvent =
            EventManager.RegisterRoutedEvent(
                nameof(PasswordChanged),
                RoutingStrategy.Bubble,
                typeof(RoutedEventHandler),
                typeof(PasswordBox));

        public string Password
        {
            get => (string)GetValue(PasswordProperty) ?? string.Empty;
            set => SetValue(PasswordProperty, value ?? string.Empty);
        }

        public char PasswordChar
        {
            get => (char)GetValue(PasswordCharProperty);
            set => SetValue(PasswordCharProperty, value);
        }

        public int MaxLength
        {
            get => (int)GetValue(MaxLengthProperty);
            set => SetValue(MaxLengthProperty, value);
        }

        public Brush SelectionBrush
        {
            get => (Brush)GetValue(SelectionBrushProperty);
            set => SetValue(SelectionBrushProperty, value);
        }

        public Brush SelectionTextBrush
        {
            get => (Brush)GetValue(SelectionTextBrushProperty);
            set => SetValue(SelectionTextBrushProperty, value);
        }

        public double SelectionOpacity
        {
            get => (double)GetValue(SelectionOpacityProperty);
            set => SetValue(SelectionOpacityProperty, value);
        }

        public Brush CaretBrush
        {
            get => (Brush)GetValue(CaretBrushProperty);
            set => SetValue(CaretBrushProperty, value);
        }

        public bool IsInactiveSelectionHighlightEnabled
        {
            get => (bool)GetValue(IsInactiveSelectionHighlightEnabledProperty);
            set => SetValue(IsInactiveSelectionHighlightEnabledProperty, value);
        }

        public bool IsSelectionActive => (bool)GetValue(IsSelectionActiveProperty);

        public event RoutedEventHandler PasswordChanged
        {
            add => AddHandler(PasswordChangedEvent, value);
            remove => RemoveHandler(PasswordChangedEvent, value);
        }

        static PasswordBox()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(PasswordBox), new FrameworkPropertyMetadata(typeof(PasswordBox)));
            FocusableProperty.OverrideMetadata(typeof(PasswordBox), new FrameworkPropertyMetadata(true));
            PaddingProperty.OverrideMetadata(typeof(PasswordBox), new FrameworkPropertyMetadata(new Thickness(2, 0, 2, 0)));
            RegisterDefaultTheme();
        }

        private System.Windows.Controls.Primitives.TextBoxBase.TextContentHostView _contentHostView;
        private ScrollViewer _contentHostScrollViewer;
        private int _caretIndex;
        private int _selectionStart;
        private int _selectionLength;
        private int _selectionAnchor;
        private bool _isSelecting;

        public int CaretIndex
        {
            get => _caretIndex;
            set
            {
                int clamped = Math.Clamp(value, 0, Password.Length);
                if (_caretIndex != clamped)
                {
                    _caretIndex = clamped;
                    EnsureCaretVisible();
                    _contentHostView?.InvalidateVisual();
                    InvalidateVisual();
                }
            }
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            _contentHostScrollViewer = GetTemplateChild("PART_ContentHost") as ScrollViewer;
            _contentHostView = System.Windows.Controls.Primitives.TextBoxBase.AttachContentHost(
                _contentHostScrollViewer,
                _contentHostView,
                MeasureContentView,
                RenderContentView);
        }

        public PasswordBox()
        {
        }

        public void Clear()
        {
            _caretIndex = 0;
            _selectionStart = 0;
            _selectionLength = 0;
            _selectionAnchor = 0;
            Password = string.Empty;
        }

        public void SelectAll()
        {
            _selectionAnchor = 0;
            _selectionStart = 0;
            _selectionLength = Password.Length;
            _caretIndex = Password.Length;
            EnsureCaretVisible();
            _contentHostView?.InvalidateVisual();
            InvalidateVisual();
        }

        private static void OnPasswordPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is PasswordBox pb)
            {
                int len = pb.Password.Length;
                pb._caretIndex = Math.Clamp(pb._caretIndex, 0, len);
                pb._selectionStart = Math.Clamp(pb._selectionStart, 0, len);
                pb._selectionLength = Math.Clamp(pb._selectionLength, 0, len - pb._selectionStart);
                pb.EnsureCaretVisible();
                pb._contentHostView?.InvalidateMeasure();
                pb._contentHostView?.InvalidateVisual();
                pb.InvalidateVisual();
                pb.RaiseEvent(new RoutedEventArgs(PasswordChangedEvent, pb));
            }
        }

        protected override HitTestResult HitTestCore(PointHitTestParameters hitTestParameters)
        {
            Point pt = hitTestParameters.HitPoint;
            if (pt.X >= 0 && pt.X <= RenderSize.Width && pt.Y >= 0 && pt.Y <= RenderSize.Height)
            {
                return new PointHitTestResult(this, pt);
            }
            return base.HitTestCore(hitTestParameters);
        }

        private int GetCharacterIndexFromPoint(Point point)
        {
            string pwd = Password;
            if (string.IsNullOrEmpty(pwd)) return 0;

            double hOff = _contentHostScrollViewer?.HorizontalOffset ?? 0.0;
            double localX = Math.Max(0.0, point.X - BorderThickness.Left - Padding.Left + hOff);

            int bestIdx = 0;
            double bestDist = double.PositiveInfinity;
            for (int i = 0; i <= pwd.Length; i++)
            {
                double w = i == 0 ? 0.0 : CreateMaskedText(i).Width;
                double dist = Math.Abs(w - localX);
                if (dist < bestDist)
                {
                    bestDist = dist;
                    bestIdx = i;
                }
            }
            return bestIdx;
        }

        private void EnsureCaretVisible()
        {
            if (Template == null || _contentHostScrollViewer == null) return;
            double caretX = _caretIndex > 0 ? CreateMaskedText(_caretIndex).Width : 0.0;
            double lineH = FontSize > 0 ? FontSize * 1.2 : 18.0;
            System.Windows.Controls.Primitives.TextBoxBase.ScrollCaretIntoView(_contentHostScrollViewer, caretX, 0.0, lineH);
        }

        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonDown(e);
            Focus();
            CaptureMouse();
            _isSelecting = true;
            int idx = GetCharacterIndexFromPoint(e.GetPosition(this));
            _selectionAnchor = idx;
            _selectionStart = idx;
            _selectionLength = 0;
            _caretIndex = idx;
            EnsureCaretVisible();
            _contentHostView?.InvalidateVisual();
            InvalidateVisual();
            e.Handled = true;
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (_isSelecting && IsMouseCaptured)
            {
                int idx = GetCharacterIndexFromPoint(e.GetPosition(this));
                _selectionStart = Math.Min(_selectionAnchor, idx);
                _selectionLength = Math.Abs(idx - _selectionAnchor);
                _caretIndex = idx;
                EnsureCaretVisible();
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

        protected override void OnGotFocus(RoutedEventArgs e)
        {
            base.OnGotFocus(e);
            SetValue(IsSelectionActivePropertyKey, true);
            _contentHostView?.InvalidateVisual();
            InvalidateVisual();
        }

        protected override void OnLostFocus(RoutedEventArgs e)
        {
            base.OnLostFocus(e);
            if (!IsKeyboardFocused) SetValue(IsSelectionActivePropertyKey, false);
            _contentHostView?.InvalidateVisual();
            InvalidateVisual();
        }

        protected override void OnGotKeyboardFocus(KeyboardFocusChangedEventArgs e)
        {
            base.OnGotKeyboardFocus(e);
            SetValue(IsSelectionActivePropertyKey, true);
            _contentHostView?.InvalidateVisual();
            InvalidateVisual();
        }

        protected override void OnLostKeyboardFocus(KeyboardFocusChangedEventArgs e)
        {
            base.OnLostKeyboardFocus(e);
            if (!IsFocused) SetValue(IsSelectionActivePropertyKey, false);
            _contentHostView?.InvalidateVisual();
            InvalidateVisual();
        }

        protected override void OnTextInput(TextCompositionEventArgs e)
        {
            base.OnTextInput(e);

            if (!e.Handled && !string.IsNullOrEmpty(e.Text))
            {
                string current = Password;
                string insert = e.Text;
                int start = _selectionLength > 0 ? _selectionStart : Math.Clamp(_caretIndex, 0, current.Length);
                int len = _selectionLength > 0 ? Math.Min(_selectionLength, current.Length - start) : 0;

                if (MaxLength > 0)
                {
                    int remaining = MaxLength - (current.Length - len);
                    if (remaining <= 0)
                    {
                        e.Handled = true;
                        return;
                    }
                    if (insert.Length > remaining) insert = insert.Substring(0, remaining);
                }

                string next = current.Substring(0, start) + insert + current.Substring(start + len);
                _caretIndex = start + insert.Length;
                _selectionStart = _caretIndex;
                _selectionAnchor = _caretIndex;
                _selectionLength = 0;
                Password = next;
                e.Handled = true;
            }
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);

            if (e.Handled) return;

            bool isCtrl = (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control;
            bool isShift = (Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift;
            string current = Password;

            if (isCtrl && e.Key == Key.A)
            {
                SelectAll();
                e.Handled = true;
                return;
            }

            if (e.Key == Key.Back)
            {
                if (_selectionLength > 0)
                {
                    int start = Math.Clamp(_selectionStart, 0, current.Length);
                    int len = Math.Min(_selectionLength, current.Length - start);
                    _caretIndex = start;
                    _selectionLength = 0;
                    _selectionAnchor = start;
                    Password = current.Substring(0, start) + current.Substring(start + len);
                }
                else if (_caretIndex > 0 && current.Length > 0)
                {
                    int idx = Math.Min(_caretIndex, current.Length);
                    _caretIndex = idx - 1;
                    _selectionStart = _caretIndex;
                    _selectionAnchor = _caretIndex;
                    Password = current.Substring(0, idx - 1) + current.Substring(idx);
                }
                e.Handled = true;
            }
            else if (e.Key == Key.Delete)
            {
                if (_selectionLength > 0)
                {
                    int start = Math.Clamp(_selectionStart, 0, current.Length);
                    int len = Math.Min(_selectionLength, current.Length - start);
                    _caretIndex = start;
                    _selectionLength = 0;
                    _selectionAnchor = start;
                    Password = current.Substring(0, start) + current.Substring(start + len);
                }
                else if (_caretIndex < current.Length)
                {
                    Password = current.Substring(0, _caretIndex) + current.Substring(_caretIndex + 1);
                }
                e.Handled = true;
            }
            else if (e.Key == Key.Left)
            {
                if (isShift)
                {
                    if (_selectionLength == 0) _selectionAnchor = _caretIndex;
                    if (_caretIndex > 0)
                    {
                        _caretIndex--;
                        _selectionStart = Math.Min(_caretIndex, _selectionAnchor);
                        _selectionLength = Math.Abs(_selectionAnchor - _caretIndex);
                    }
                }
                else
                {
                    int target = _selectionLength > 0 ? _selectionStart : Math.Max(0, _caretIndex - 1);
                    _selectionLength = 0;
                    _selectionStart = target;
                    _selectionAnchor = target;
                    _caretIndex = target;
                }
                EnsureCaretVisible();
                _contentHostView?.InvalidateVisual();
                InvalidateVisual();
                e.Handled = true;
            }
            else if (e.Key == Key.Right)
            {
                if (isShift)
                {
                    if (_selectionLength == 0) _selectionAnchor = _caretIndex;
                    if (_caretIndex < current.Length)
                    {
                        _caretIndex++;
                        _selectionStart = Math.Min(_caretIndex, _selectionAnchor);
                        _selectionLength = Math.Abs(_caretIndex - _selectionAnchor);
                    }
                }
                else
                {
                    int target = _selectionLength > 0 ? _selectionStart + _selectionLength : Math.Min(current.Length, _caretIndex + 1);
                    _selectionLength = 0;
                    _selectionStart = target;
                    _selectionAnchor = target;
                    _caretIndex = target;
                }
                EnsureCaretVisible();
                _contentHostView?.InvalidateVisual();
                InvalidateVisual();
                e.Handled = true;
            }
            else if (e.Key == Key.Home)
            {
                if (isShift)
                {
                    if (_selectionLength == 0) _selectionAnchor = _caretIndex;
                    _selectionStart = 0;
                    _selectionLength = _selectionAnchor;
                    _caretIndex = 0;
                }
                else
                {
                    _selectionLength = 0;
                    _selectionStart = 0;
                    _selectionAnchor = 0;
                    _caretIndex = 0;
                }
                EnsureCaretVisible();
                _contentHostView?.InvalidateVisual();
                InvalidateVisual();
                e.Handled = true;
            }
            else if (e.Key == Key.End)
            {
                int endIdx = current.Length;
                if (isShift)
                {
                    if (_selectionLength == 0) _selectionAnchor = _caretIndex;
                    _selectionStart = _selectionAnchor;
                    _selectionLength = endIdx - _selectionAnchor;
                    _caretIndex = endIdx;
                }
                else
                {
                    _selectionLength = 0;
                    _selectionStart = endIdx;
                    _selectionAnchor = endIdx;
                    _caretIndex = endIdx;
                }
                EnsureCaretVisible();
                _contentHostView?.InvalidateVisual();
                InvalidateVisual();
                e.Handled = true;
            }
        }

        private FormattedText CreateMaskedText(int len)
        {
            string masked = new(PasswordChar, len);
            Typeface typeface = new(FontFamily ?? new FontFamily("Segoe UI"), FontStyle, FontWeight, FontStretch);
            return new(
                masked,
                CultureInfo.CurrentUICulture,
                FlowDirection.LeftToRight,
                typeface,
                FontSize > 0 ? FontSize : 14.0,
                Foreground ?? Brushes.Black,
                null,
                TextFormattingMode.Ideal,
                1.0);
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

            FormattedText ft = CreateMaskedText(Math.Max(1, Password.Length));
            return new Size(ft.Width + horizontalPadding, ft.Height + verticalPadding);
        }

        private Size MeasureContentView(Size constraint)
        {
            FormattedText ft = CreateMaskedText(Math.Max(1, Password.Length));
            return new Size(ft.Width + 2.0, ft.Height);
        }

        private void RenderPasswordContent(DrawingContext dc, double xStart, double yBase, double availH)
        {
            string pwd = Password;
            FormattedText ftMeasure = CreateMaskedText(Math.Max(1, pwd.Length));
            double yStart = yBase + System.Windows.Controls.Primitives.TextBoxBase.GetVerticalAlignmentOffset(VerticalContentAlignment, availH, ftMeasure.Height);

            if (!string.IsNullOrEmpty(pwd))
            {
                FormattedText ft = CreateMaskedText(pwd.Length);
                if (_selectionLength > 0 && _selectionStart < pwd.Length)
                {
                    int selLen = Math.Min(_selectionLength, pwd.Length - _selectionStart);
                    Geometry selGeo = ft.BuildHighlightGeometry(new Point(xStart, yStart), _selectionStart, selLen);
                    if (selGeo != null && !selGeo.Bounds.IsEmpty)
                    {
                        Brush selBrush = System.Windows.Controls.Primitives.TextBoxBase.ResolveSelectionBrush(SelectionBrush, SelectionOpacity);
                        dc.DrawGeometry(selBrush, null, selGeo);
                    }
                    Brush selTextBrush = SelectionTextBrush ?? SystemColors.HighlightTextBrush;
                    ft.SetForegroundBrush(selTextBrush, _selectionStart, selLen);
                }
                dc.DrawText(ft, new Point(xStart, yStart));
            }

            if (IsFocused || IsKeyboardFocused)
            {
                int caretIdx = Math.Clamp(_caretIndex, 0, pwd.Length);
                double caretX = xStart + (caretIdx > 0 ? CreateMaskedText(caretIdx).Width : 0.0);
                double caretH = ftMeasure.Height > 0 ? ftMeasure.Height : (FontSize > 0 ? FontSize * 1.2 : 18.0);
                Pen caretPen = new(CaretBrush ?? Foreground ?? Brushes.Black, 1.5);
                dc.DrawLine(caretPen, new Point(caretX, yStart), new Point(caretX, yStart + caretH));
            }
        }

        private void RenderContentView(DrawingContext dc, Size bounds)
        {
            if (bounds.Width <= 0 || bounds.Height <= 0) return;
            double vpH = (_contentHostScrollViewer != null && _contentHostScrollViewer.ViewportHeight > 0)
                ? _contentHostScrollViewer.ViewportHeight
                : bounds.Height;
            RenderPasswordContent(dc, 0.0, 0.0, vpH);
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

            Brush bg = Background ?? Brushes.White;
            Brush borderBrush = BorderBrush ?? Brushes.Gray;
            Thickness borderThickness = BorderThickness;

            dc.DrawRectangle(bg, null, bounds);

            if (borderThickness.Left > 0 && borderBrush != null)
            {
                Pen pen = new(borderBrush, borderThickness.Left);
                dc.DrawRectangle(null, pen, new Rect(borderThickness.Left * 0.5, borderThickness.Top * 0.5, Math.Max(0, bounds.Width - borderThickness.Left), Math.Max(0, bounds.Height - borderThickness.Top)));
            }

            double xStart = Padding.Left + borderThickness.Left;
            double yBase = Padding.Top + borderThickness.Top;
            double availH = Math.Max(0.0, bounds.Height - yBase - Padding.Bottom - borderThickness.Bottom);
            RenderPasswordContent(dc, xStart, yBase, availH);
        }

        protected override System.Windows.Automation.Peers.AutomationPeer OnCreateAutomationPeer() =>
            new System.Windows.Automation.Peers.PasswordBoxAutomationPeer(this);
    }
}

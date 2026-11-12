// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace System.Windows.Controls.Primitives
{
    /// <summary>
    /// An abstract base class for text box controls.
    /// </summary>
    public abstract partial class TextBoxBase : Control
    {
        static partial void RegisterDefaultTheme();
        public static readonly DependencyProperty IsReadOnlyProperty =
            DependencyProperty.Register(
                nameof(IsReadOnly),
                typeof(bool),
                typeof(TextBoxBase),
                new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty AcceptsReturnProperty =
            DependencyProperty.Register(
                nameof(AcceptsReturn),
                typeof(bool),
                typeof(TextBoxBase),
                new FrameworkPropertyMetadata(false));

        public static readonly DependencyProperty AcceptsTabProperty =
            DependencyProperty.Register(
                nameof(AcceptsTab),
                typeof(bool),
                typeof(TextBoxBase),
                new FrameworkPropertyMetadata(false));

        private static DependencyProperty s_horizontalScrollBarVisibilityProperty;
        public static DependencyProperty HorizontalScrollBarVisibilityProperty =>
            s_horizontalScrollBarVisibilityProperty ??= DependencyProperty.Register(
                nameof(HorizontalScrollBarVisibility),
                typeof(ScrollBarVisibility),
                typeof(TextBoxBase),
                new FrameworkPropertyMetadata(ScrollBarVisibility.Hidden));

        private static DependencyProperty s_verticalScrollBarVisibilityProperty;
        public static DependencyProperty VerticalScrollBarVisibilityProperty =>
            s_verticalScrollBarVisibilityProperty ??= DependencyProperty.Register(
                nameof(VerticalScrollBarVisibility),
                typeof(ScrollBarVisibility),
                typeof(TextBoxBase),
                new FrameworkPropertyMetadata(ScrollBarVisibility.Hidden));

        public static readonly RoutedEvent TextChangedEvent =
            EventManager.RegisterRoutedEvent(
                nameof(TextChanged),
                RoutingStrategy.Bubble,
                typeof(TextChangedEventHandler),
                typeof(TextBoxBase));

        public static readonly RoutedEvent SelectionChangedEvent =
            EventManager.RegisterRoutedEvent(
                nameof(SelectionChanged),
                RoutingStrategy.Bubble,
                typeof(RoutedEventHandler),
                typeof(TextBoxBase));

        public bool IsReadOnly
        {
            get => (bool)GetValue(IsReadOnlyProperty);
            set => SetValue(IsReadOnlyProperty, value);
        }

        public bool AcceptsReturn
        {
            get => (bool)GetValue(AcceptsReturnProperty);
            set => SetValue(AcceptsReturnProperty, value);
        }

        public bool AcceptsTab
        {
            get => (bool)GetValue(AcceptsTabProperty);
            set => SetValue(AcceptsTabProperty, value);
        }

        public ScrollBarVisibility HorizontalScrollBarVisibility
        {
            get => (ScrollBarVisibility)GetValue(HorizontalScrollBarVisibilityProperty);
            set => SetValue(HorizontalScrollBarVisibilityProperty, value);
        }

        public ScrollBarVisibility VerticalScrollBarVisibility
        {
            get => (ScrollBarVisibility)GetValue(VerticalScrollBarVisibilityProperty);
            set => SetValue(VerticalScrollBarVisibilityProperty, value);
        }

        public event TextChangedEventHandler TextChanged
        {
            add => AddHandler(TextChangedEvent, value);
            remove => RemoveHandler(TextChangedEvent, value);
        }

        public event RoutedEventHandler SelectionChanged
        {
            add => AddHandler(SelectionChangedEvent, value);
            remove => RemoveHandler(SelectionChangedEvent, value);
        }

        public abstract int SelectionStart { get; set; }
        public abstract int SelectionLength { get; set; }
        public abstract string SelectedText { get; set; }

        static TextBoxBase()
        {
            MS.Internal.Controls.ControlFoundationHooks.IsTextBoxBaseHook = static d => d is TextBoxBase;
            FocusableProperty.OverrideMetadata(typeof(TextBoxBase), new FrameworkPropertyMetadata(true));
            RegisterDefaultTheme();
        }

        private static DependencyProperty s_selectionBrushProperty;
        public static DependencyProperty SelectionBrushProperty =>
            s_selectionBrushProperty ??= DependencyProperty.Register(nameof(SelectionBrush), typeof(System.Windows.Media.Brush), typeof(TextBoxBase),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

        private static DependencyProperty s_selectionTextBrushProperty;
        public static DependencyProperty SelectionTextBrushProperty =>
            s_selectionTextBrushProperty ??= DependencyProperty.Register(nameof(SelectionTextBrush), typeof(System.Windows.Media.Brush), typeof(TextBoxBase),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

        private static DependencyProperty s_selectionOpacityProperty;
        public static DependencyProperty SelectionOpacityProperty =>
            s_selectionOpacityProperty ??= DependencyProperty.Register(nameof(SelectionOpacity), typeof(double), typeof(TextBoxBase),
                new FrameworkPropertyMetadata(0.4, FrameworkPropertyMetadataOptions.AffectsRender));

        private static DependencyProperty s_caretBrushProperty;
        public static DependencyProperty CaretBrushProperty =>
            s_caretBrushProperty ??= DependencyProperty.Register(nameof(CaretBrush), typeof(System.Windows.Media.Brush), typeof(TextBoxBase),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

        private static DependencyProperty s_isInactiveSelectionHighlightEnabledProperty;
        public static DependencyProperty IsInactiveSelectionHighlightEnabledProperty =>
            s_isInactiveSelectionHighlightEnabledProperty ??= DependencyProperty.Register(nameof(IsInactiveSelectionHighlightEnabled), typeof(bool), typeof(TextBoxBase),
                new FrameworkPropertyMetadata(false));

        private static DependencyPropertyKey s_isSelectionActivePropertyKey;
        public static DependencyProperty IsSelectionActiveProperty =>
            (s_isSelectionActivePropertyKey ??= DependencyProperty.RegisterReadOnly(nameof(IsSelectionActive), typeof(bool), typeof(TextBoxBase),
                new FrameworkPropertyMetadata(false))).DependencyProperty;

        private static DependencyProperty s_autoWordSelectionProperty;
        public static DependencyProperty AutoWordSelectionProperty =>
            s_autoWordSelectionProperty ??= DependencyProperty.Register(nameof(AutoWordSelection), typeof(bool), typeof(TextBoxBase),
                new FrameworkPropertyMetadata(false));

        private static DependencyProperty s_isReadOnlyCaretVisibleProperty;
        public static DependencyProperty IsReadOnlyCaretVisibleProperty =>
            s_isReadOnlyCaretVisibleProperty ??= DependencyProperty.Register(nameof(IsReadOnlyCaretVisible), typeof(bool), typeof(TextBoxBase),
                new FrameworkPropertyMetadata(false));

        public System.Windows.Media.Brush SelectionBrush
        {
            get => (System.Windows.Media.Brush)GetValue(SelectionBrushProperty);
            set => SetValue(SelectionBrushProperty, value);
        }

        public System.Windows.Media.Brush SelectionTextBrush
        {
            get => (System.Windows.Media.Brush)GetValue(SelectionTextBrushProperty);
            set => SetValue(SelectionTextBrushProperty, value);
        }

        public bool IsInactiveSelectionHighlightEnabled
        {
            get => (bool)GetValue(IsInactiveSelectionHighlightEnabledProperty);
            set => SetValue(IsInactiveSelectionHighlightEnabledProperty, value);
        }

        public bool IsSelectionActive => (bool)GetValue(IsSelectionActiveProperty);

        public double SelectionOpacity
        {
            get => (double)GetValue(SelectionOpacityProperty);
            set => SetValue(SelectionOpacityProperty, value);
        }

        public System.Windows.Media.Brush CaretBrush
        {
            get => (System.Windows.Media.Brush)GetValue(CaretBrushProperty);
            set => SetValue(CaretBrushProperty, value);
        }

        public bool AutoWordSelection
        {
            get => (bool)GetValue(AutoWordSelectionProperty);
            set => SetValue(AutoWordSelectionProperty, value);
        }

        public bool IsReadOnlyCaretVisible
        {
            get => (bool)GetValue(IsReadOnlyCaretVisibleProperty);
            set => SetValue(IsReadOnlyCaretVisibleProperty, value);
        }

        public SpellCheck SpellCheck => new SpellCheck(this);

        protected TextBoxBase()
        {
        }

        public abstract void Select(int start, int length);
        public abstract void SelectAll();
        public abstract void Clear();
        public abstract void AppendText(string textData);

        protected virtual void OnTextChanged(TextChangedEventArgs e)
        {
            RaiseEvent(e);
        }

        protected virtual void OnSelectionChanged(RoutedEventArgs e)
        {
            RaiseEvent(e);
        }

        internal static TextContentHostView AttachContentHost(
            object host,
            TextContentHostView existing,
            Func<Size, Size> measure,
            Action<DrawingContext, Size> render)
        {
            if (host == null) return existing;
            var view = existing ?? new TextContentHostView(measure, render);
            if (host is ContentControl cc)
            {
                if (!ReferenceEquals(cc.Content, view)) cc.Content = view;
            }
            else if (host is Decorator dec)
            {
                if (!ReferenceEquals(dec.Child, view)) dec.Child = view;
            }
            return view;
        }

        internal sealed class TextContentHostView : FrameworkElement
        {
            private readonly Func<Size, Size> _measure;
            private readonly Action<DrawingContext, Size> _render;

            internal TextContentHostView(Func<Size, Size> measure, Action<DrawingContext, Size> render)
            {
                _measure = measure;
                _render = render;
                IsHitTestVisible = false;
            }

            protected override Size MeasureOverride(Size availableSize) => _measure(availableSize);

            protected override void OnRender(DrawingContext drawingContext) => _render(drawingContext, RenderSize);
        }
    }
}

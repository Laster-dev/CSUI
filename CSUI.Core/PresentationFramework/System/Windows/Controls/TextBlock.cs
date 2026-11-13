// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;

namespace System.Windows.Controls
{
    /// <summary>
    /// Provides a lightweight, high-performance element for displaying small to large amounts of text.
    /// Direct2D DirectWrite hardware-accelerated text layout with sub-pixel and 1px snap alignment.
    /// </summary>
    public partial class TextBlock : FrameworkElement, MS.Internal.Controls.ITextDisplayElement
    {
        static partial void RegisterDefaultTheme();
        bool MS.Internal.Controls.ITextDisplayElement.IsContentPresenterContainer { get; set; }

        bool MS.Internal.Controls.ITextDisplayElement.TryAddInline(object inline)
        {
            return MS.Internal.Controls.ControlFoundationHooks.TryAddInlineHook?.Invoke(this, inline) ?? false;
        }
        public static readonly DependencyProperty TextProperty =
            DependencyProperty.Register(
                nameof(Text),
                typeof(string),
                typeof(TextBlock),
                new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender, OnFormattedTextInvalidated));

        public static readonly DependencyProperty FontFamilyProperty =
            TextElement.FontFamilyProperty.AddOwner(
                typeof(TextBlock),
                new FrameworkPropertyMetadata(
                    SystemFonts.MessageFontFamily,
                    FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.Inherits,
                    OnFormattedTextInvalidated));

        public static readonly DependencyProperty FontSizeProperty =
            TextElement.FontSizeProperty.AddOwner(
                typeof(TextBlock),
                new FrameworkPropertyMetadata(
                    SystemFonts.ThemeMessageFontSize,
                    FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.Inherits,
                    OnFormattedTextInvalidated));

        public static readonly DependencyProperty FontWeightProperty =
            TextElement.FontWeightProperty.AddOwner(
                typeof(TextBlock),
                new FrameworkPropertyMetadata(
                    SystemFonts.MessageFontWeight,
                    FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.Inherits,
                    OnFormattedTextInvalidated));

        public static readonly DependencyProperty FontStyleProperty =
            TextElement.FontStyleProperty.AddOwner(
                typeof(TextBlock),
                new FrameworkPropertyMetadata(
                    SystemFonts.MessageFontStyle,
                    FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.Inherits,
                    OnFormattedTextInvalidated));

        public static readonly DependencyProperty ForegroundProperty =
            TextElement.ForegroundProperty.AddOwner(
                typeof(TextBlock),
                new FrameworkPropertyMetadata(
                    Brushes.Black,
                    FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.SubPropertiesDoNotAffectRender | FrameworkPropertyMetadataOptions.Inherits,
                    OnFormattedTextInvalidated));

        public static readonly DependencyProperty BackgroundProperty =
            DependencyProperty.Register(
                nameof(Background),
                typeof(Brush),
                typeof(TextBlock),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty TextWrappingProperty =
            DependencyProperty.Register(
                nameof(TextWrapping),
                typeof(TextWrapping),
                typeof(TextBlock),
                new FrameworkPropertyMetadata(TextWrapping.NoWrap, FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender, OnFormattedTextInvalidated));

        public static readonly DependencyProperty TextTrimmingProperty =
            DependencyProperty.Register(
                nameof(TextTrimming),
                typeof(TextTrimming),
                typeof(TextBlock),
                new FrameworkPropertyMetadata(TextTrimming.None, FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender, OnFormattedTextInvalidated));

        public static readonly DependencyProperty TextAlignmentProperty =
            DependencyProperty.Register(
                nameof(TextAlignment),
                typeof(TextAlignment),
                typeof(TextBlock),
                new FrameworkPropertyMetadata(TextAlignment.Left, FrameworkPropertyMetadataOptions.AffectsRender, OnFormattedTextInvalidated));

        public static readonly DependencyProperty PaddingProperty =
            DependencyProperty.Register(
                nameof(Padding),
                typeof(Thickness),
                typeof(TextBlock),
                new FrameworkPropertyMetadata(new Thickness(0.0), FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender, OnFormattedTextInvalidated));

        private static DependencyProperty s_lineHeightProperty;
        public static DependencyProperty LineHeightProperty =>
            s_lineHeightProperty ??= DependencyProperty.Register(
                nameof(LineHeight),
                typeof(double),
                typeof(TextBlock),
                new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender, OnFormattedTextInvalidated));

        public string Text
        {
            get => (string)GetValue(TextProperty);
            set => SetValue(TextProperty, value);
        }

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

        public FontWeight FontWeight
        {
            get => (FontWeight)GetValue(FontWeightProperty);
            set => SetValue(FontWeightProperty, value);
        }

        public FontStyle FontStyle
        {
            get => (FontStyle)GetValue(FontStyleProperty);
            set => SetValue(FontStyleProperty, value);
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

        public TextWrapping TextWrapping
        {
            get => (TextWrapping)GetValue(TextWrappingProperty);
            set => SetValue(TextWrappingProperty, value);
        }

        public TextTrimming TextTrimming
        {
            get => (TextTrimming)GetValue(TextTrimmingProperty);
            set => SetValue(TextTrimmingProperty, value);
        }

        public TextAlignment TextAlignment
        {
            get => (TextAlignment)GetValue(TextAlignmentProperty);
            set => SetValue(TextAlignmentProperty, value);
        }

        public Thickness Padding
        {
            get => (Thickness)GetValue(PaddingProperty);
            set => SetValue(PaddingProperty, value);
        }

        public double LineHeight
        {
            get => (double)GetValue(LineHeightProperty);
            set => SetValue(LineHeightProperty, value);
        }

        private FormattedText _cachedFormattedText;
        private double _cachedConstraintWidth = -1;
        private object _inlinesStorage;
        private Func<object, string> _extractInlinesTextHook;
        private Action<object, List<UIElement>> _collectInlineChildrenHook;
        private List<UIElement> _inlineUiChildren;

        public InlineCollection Inlines => (InlineCollection)(_inlinesStorage ??= new InlineCollection(this));

        internal void ConfigureInlineStorage(
            Func<object, string> extractTextHook,
            Action<object, List<UIElement>> collectChildrenHook)
        {
            _extractInlinesTextHook = extractTextHook;
            _collectInlineChildrenHook = collectChildrenHook;
        }

        internal void OnInlinesChanged()
        {
            _cachedFormattedText = null;
            if (_inlineUiChildren != null)
            {
                foreach (UIElement child in _inlineUiChildren)
                {
                    RemoveVisualChild(child);
                }
                _inlineUiChildren.Clear();
            }

            if (_inlinesStorage != null && _collectInlineChildrenHook != null)
            {
                var collected = new List<UIElement>();
                _collectInlineChildrenHook(_inlinesStorage, collected);
                if (collected.Count > 0)
                {
                    _inlineUiChildren ??= new List<UIElement>(collected.Count);
                    foreach (UIElement child in collected)
                    {
                        _inlineUiChildren.Add(child);
                        AddVisualChild(child);
                    }
                }
            }

            InvalidateMeasure();
            InvalidateVisual();
        }

        protected override int VisualChildrenCount => _inlineUiChildren?.Count ?? 0;

        protected override Visual GetVisualChild(int index)
        {
            if (_inlineUiChildren == null || (uint)index >= (uint)_inlineUiChildren.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }
            return _inlineUiChildren[index];
        }

        private string GetEffectiveText()
        {
            string txt = Text;
            if (!string.IsNullOrEmpty(txt) || _inlinesStorage == null || _extractInlinesTextHook == null)
            {
                return txt ?? string.Empty;
            }

            return _extractInlinesTextHook(_inlinesStorage) ?? string.Empty;
        }

        static TextBlock()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(TextBlock), new FrameworkPropertyMetadata(typeof(TextBlock)));
            FrameworkElementFactory.RegisterFactory(typeof(TextBlock), static () => new TextBlock());
            RegisterDefaultTheme();
            MS.Internal.Controls.ControlFoundationHooks.RegisterTextDisplay(
                TextProperty,
                static () => new FrameworkElementFactory(typeof(TextBlock)),
                static () => new TextBlock());
        }

        public TextBlock()
        {
        }

        public TextBlock(string text) : this()
        {
            Text = text;
        }

        private static void OnFormattedTextInvalidated(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var tb = (TextBlock)d;
            tb._cachedFormattedText = null;
        }

        private FormattedText GetOrCreateFormattedText(double availableWidth)
        {
            string txt = GetEffectiveText();
            if (_cachedFormattedText != null && Math.Abs(_cachedConstraintWidth - availableWidth) < 0.001)
            {
                return _cachedFormattedText;
            }

            Typeface typeface = new Typeface(FontFamily, FontStyle, FontWeight, FontStretches.Normal);
            double emSize = Math.Max(1.0, FontSize);
            Brush fg = Foreground ?? Brushes.Black;

#pragma warning disable CS0618 // Type or member is obsolete
            FormattedText ft = new FormattedText(
                txt,
                CultureInfo.CurrentUICulture,
                FlowDirection.LeftToRight,
                typeface,
                emSize,
                fg);
#pragma warning restore CS0618

            if (TextWrapping != TextWrapping.NoWrap && !double.IsInfinity(availableWidth))
            {
                ft.MaxTextWidth = Math.Max(0.0, availableWidth);
            }

            ft.Trimming = TextTrimming;

            ft.TextAlignment = TextAlignment;

            if (s_lineHeightProperty != null)
            {
                double lineHeight = (double)GetValue(s_lineHeightProperty);
                if (lineHeight > 0)
                {
                    ft.LineHeight = lineHeight;
                }
            }

            _cachedFormattedText = ft;
            _cachedConstraintWidth = availableWidth;
            return ft;
        }

        protected override Size MeasureOverride(Size constraint)
        {
            string text = GetEffectiveText();
            Thickness pad = Padding;
            double horizontalPad = pad.Left + pad.Right;
            double verticalPad = pad.Top + pad.Bottom;

            double measuredW = horizontalPad;
            double measuredH = verticalPad;

            if (!string.IsNullOrEmpty(text))
            {
                double availableWidth = Math.Max(0.0, constraint.Width - horizontalPad);
                FormattedText ft = GetOrCreateFormattedText(availableWidth);
                measuredW += ft.WidthIncludingTrailingWhitespace;
                measuredH += ft.Height;
            }

            if (_inlineUiChildren != null && _inlineUiChildren.Count > 0)
            {
                double childW = 0.0;
                double childH = 0.0;
                Size childAvail = new Size(Math.Max(0.0, constraint.Width - horizontalPad), Math.Max(0.0, constraint.Height - verticalPad));
                foreach (UIElement child in _inlineUiChildren)
                {
                    child.Measure(childAvail);
                    childW += child.DesiredSize.Width;
                    if (child.DesiredSize.Height > childH) childH = child.DesiredSize.Height;
                }
                measuredW = Math.Max(measuredW, childW + horizontalPad);
                measuredH = Math.Max(measuredH, childH + verticalPad);
            }

            return new Size(measuredW, measuredH);
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            if (_inlineUiChildren != null && _inlineUiChildren.Count > 0)
            {
                Thickness pad = Padding;
                double x = pad.Left;
                double availH = Math.Max(0.0, finalSize.Height - pad.Top - pad.Bottom);
                foreach (UIElement child in _inlineUiChildren)
                {
                    double w = child.DesiredSize.Width;
                    child.Arrange(new Rect(x, pad.Top, w, availH));
                    x += w;
                }
            }
            return finalSize;
        }

        protected override void OnRender(DrawingContext dc)
        {
            Brush bg = Background;
            Thickness pad = Padding;

            if (bg != null)
            {
                dc.DrawRectangle(bg, null, new Rect(0, 0, RenderSize.Width, RenderSize.Height));
            }

            string text = GetEffectiveText();
            if (string.IsNullOrEmpty(text)) return;

            double availableWidth = Math.Max(0.0, RenderSize.Width - pad.Left - pad.Right);
            FormattedText ft = GetOrCreateFormattedText(availableWidth);

            Point origin = new Point(pad.Left, pad.Top);
            dc.DrawText(ft, origin);
        }

        protected override Automation.Peers.AutomationPeer OnCreateAutomationPeer() =>
            new Automation.Peers.TextBlockAutomationPeer(this);
    }
}

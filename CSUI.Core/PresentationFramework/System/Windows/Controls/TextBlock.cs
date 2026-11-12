// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System;
using System.Globalization;
using System.Windows;
using System.Windows.Media;

namespace System.Windows.Controls
{
    /// <summary>
    /// Provides a lightweight, high-performance element for displaying small to large amounts of text.
    /// Direct2D DirectWrite hardware-accelerated text layout with sub-pixel and 1px snap alignment.
    /// </summary>
    public class TextBlock : FrameworkElement
    {
        public static readonly DependencyProperty TextProperty =
            DependencyProperty.Register(
                nameof(Text),
                typeof(string),
                typeof(TextBlock),
                new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender, OnFormattedTextInvalidated));

        public static readonly DependencyProperty FontFamilyProperty =
            DependencyProperty.Register(
                nameof(FontFamily),
                typeof(FontFamily),
                typeof(TextBlock),
                new FrameworkPropertyMetadata(new FontFamily("Segoe UI"), FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender, OnFormattedTextInvalidated));

        public static readonly DependencyProperty FontSizeProperty =
            DependencyProperty.Register(
                nameof(FontSize),
                typeof(double),
                typeof(TextBlock),
                new FrameworkPropertyMetadata(12.0, FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender, OnFormattedTextInvalidated));

        public static readonly DependencyProperty FontWeightProperty =
            DependencyProperty.Register(
                nameof(FontWeight),
                typeof(FontWeight),
                typeof(TextBlock),
                new FrameworkPropertyMetadata(FontWeights.Normal, FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender, OnFormattedTextInvalidated));

        public static readonly DependencyProperty FontStyleProperty =
            DependencyProperty.Register(
                nameof(FontStyle),
                typeof(FontStyle),
                typeof(TextBlock),
                new FrameworkPropertyMetadata(FontStyles.Normal, FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender, OnFormattedTextInvalidated));

        public static readonly DependencyProperty ForegroundProperty =
            DependencyProperty.Register(
                nameof(Foreground),
                typeof(Brush),
                typeof(TextBlock),
                new FrameworkPropertyMetadata(Brushes.Black, FrameworkPropertyMetadataOptions.AffectsRender, OnFormattedTextInvalidated));

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

        public static readonly DependencyProperty LineHeightProperty =
            DependencyProperty.Register(
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
            string txt = Text ?? string.Empty;
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

            if (TextTrimming != TextTrimming.None)
            {
                ft.Trimming = TextTrimming == TextTrimming.CharacterEllipsis
                    ? TextTrimming.CharacterEllipsis
                    : TextTrimming.WordEllipsis;
            }

            ft.TextAlignment = TextAlignment;

            if (LineHeight > 0)
            {
                ft.LineHeight = LineHeight;
            }

            _cachedFormattedText = ft;
            _cachedConstraintWidth = availableWidth;
            return ft;
        }

        protected override Size MeasureOverride(Size constraint)
        {
            string text = Text;
            Thickness pad = Padding;
            double horizontalPad = pad.Left + pad.Right;
            double verticalPad = pad.Top + pad.Bottom;

            if (string.IsNullOrEmpty(text))
            {
                return new Size(horizontalPad, verticalPad);
            }

            double availableWidth = Math.Max(0.0, constraint.Width - horizontalPad);
            FormattedText ft = GetOrCreateFormattedText(availableWidth);

            double measuredW = ft.WidthIncludingTrailingWhitespace + horizontalPad;
            double measuredH = ft.Height + verticalPad;

            return new Size(measuredW, measuredH);
        }

        protected override void OnRender(DrawingContext dc)
        {
            Brush bg = Background;
            Thickness pad = Padding;

            if (bg != null)
            {
                dc.DrawRectangle(bg, null, new Rect(0, 0, RenderSize.Width, RenderSize.Height));
            }

            string text = Text;
            if (string.IsNullOrEmpty(text)) return;

            double availableWidth = Math.Max(0.0, RenderSize.Width - pad.Left - pad.Right);
            FormattedText ft = GetOrCreateFormattedText(availableWidth);

            Point origin = new Point(pad.Left, pad.Top);
            dc.DrawText(ft, origin);
        }
    }
}

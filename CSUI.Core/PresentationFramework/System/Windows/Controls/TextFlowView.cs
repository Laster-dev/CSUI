// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Media;

namespace System.Windows.Controls
{
    /// <summary>
    /// Represents a lightweight rich-text run within a TextFlowView.
    /// Eliminates the heavy DependencyObject overhead of WPF's FlowDocument/Run.
    /// </summary>
    public sealed class TextRunSpan
    {
        public string Text { get; set; }
        public FontWeight? FontWeight { get; set; }
        public FontStyle? FontStyle { get; set; }
        public Brush Foreground { get; set; }
        public Brush Background { get; set; }
        public double? FontSize { get; set; }
        public bool IsCode { get; set; }

        public TextRunSpan(string text, FontWeight? weight = null, FontStyle? style = null, Brush foreground = null, Brush background = null, double? fontSize = null, bool isCode = false)
        {
            Text = text ?? string.Empty;
            FontWeight = weight;
            FontStyle = style;
            Foreground = foreground;
            Background = background;
            FontSize = fontSize;
            IsCode = isCode;
        }
    }

    /// <summary>
    /// Lightweight DirectWrite rich text document stream viewer.
    /// Replaces WPF's bulky FlowDocument/RichTextBox by packing multi-style runs, code blocks,
    /// and paragraphs into a flat, zero-dependency-object layout with microsecond rendering speed.
    /// </summary>
    public class TextFlowView : FrameworkElement
    {
        public static readonly DependencyProperty FontFamilyProperty =
            DependencyProperty.Register(
                nameof(FontFamily),
                typeof(FontFamily),
                typeof(TextFlowView),
                new FrameworkPropertyMetadata(new FontFamily("Segoe UI"), FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender, OnFlowInvalidated));

        public static readonly DependencyProperty FontSizeProperty =
            DependencyProperty.Register(
                nameof(FontSize),
                typeof(double),
                typeof(TextFlowView),
                new FrameworkPropertyMetadata(14.0, FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender, OnFlowInvalidated));

        public static readonly DependencyProperty ForegroundProperty =
            DependencyProperty.Register(
                nameof(Foreground),
                typeof(Brush),
                typeof(TextFlowView),
                new FrameworkPropertyMetadata(Brushes.Black, FrameworkPropertyMetadataOptions.AffectsRender, OnFlowInvalidated));

        public static readonly DependencyProperty PaddingProperty =
            DependencyProperty.Register(
                nameof(Padding),
                typeof(Thickness),
                typeof(TextFlowView),
                new FrameworkPropertyMetadata(new Thickness(0.0), FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender, OnFlowInvalidated));

        public static readonly DependencyProperty LineSpacingProperty =
            DependencyProperty.Register(
                nameof(LineSpacing),
                typeof(double),
                typeof(TextFlowView),
                new FrameworkPropertyMetadata(4.0, FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender, OnFlowInvalidated));

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

        public Thickness Padding
        {
            get => (Thickness)GetValue(PaddingProperty);
            set => SetValue(PaddingProperty, value);
        }

        public double LineSpacing
        {
            get => (double)GetValue(LineSpacingProperty);
            set => SetValue(LineSpacingProperty, value);
        }

        private readonly List<TextRunSpan> _spans = new();
        public IList<TextRunSpan> Spans => _spans;

        // Cached laid-out visual lines
        private struct LaidOutSpan
        {
            public FormattedText Formatted;
            public Point Origin;
            public Rect Bounds;
            public Brush Background;
            public bool IsCode;
        }

        private readonly List<LaidOutSpan> _laidOutSpans = new();
        private double _cachedWidth = -1;

        public TextFlowView()
        {
        }

        public TextFlowView AddSpan(string text, FontWeight? weight = null, FontStyle? style = null, Brush foreground = null, Brush background = null, double? fontSize = null, bool isCode = false)
        {
            _spans.Add(new TextRunSpan(text, weight, style, foreground, background, fontSize, isCode));
            _cachedWidth = -1;
            InvalidateMeasure();
            return this;
        }

        public TextFlowView ClearSpans()
        {
            _spans.Clear();
            _cachedWidth = -1;
            InvalidateMeasure();
            return this;
        }

        private static void OnFlowInvalidated(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var view = (TextFlowView)d;
            view._cachedWidth = -1;
        }

        protected override Size MeasureOverride(Size constraint)
        {
            Thickness pad = Padding;
            double maxW = constraint.Width - (pad.Left + pad.Right);
            if (maxW <= 0 || double.IsInfinity(maxW)) maxW = 800.0;

            _laidOutSpans.Clear();
            _cachedWidth = maxW;

            double cursorX = pad.Left;
            double cursorY = pad.Top;
            double currentLineHeight = 0.0;
            double totalMaxWidth = 0.0;
            double baseFontSize = Math.Max(1.0, FontSize);
            Brush baseForeground = Foreground ?? Brushes.Black;
            double spacing = LineSpacing;

            for (int i = 0; i < _spans.Count; i++)
            {
                TextRunSpan span = _spans[i];
                if (string.IsNullOrEmpty(span.Text)) continue;

                // Handle newline
                if (span.Text == "\n" || span.Text == "\r\n")
                {
                    cursorX = pad.Left;
                    cursorY += (currentLineHeight > 0 ? currentLineHeight : baseFontSize * 1.2) + spacing;
                    currentLineHeight = 0;
                    continue;
                }

                double emSize = span.FontSize ?? baseFontSize;
                FontWeight weight = span.FontWeight ?? FontWeights.Normal;
                FontStyle style = span.FontStyle ?? FontStyles.Normal;
                Brush fg = span.Foreground ?? baseForeground;

                Typeface typeface = span.IsCode
                    ? new Typeface(new FontFamily("Consolas"), style, weight, FontStretches.Normal)
                    : new Typeface(FontFamily, style, weight, FontStretches.Normal);

#pragma warning disable CS0618
                FormattedText ft = new FormattedText(
                    span.Text,
                    CultureInfo.CurrentUICulture,
                    FlowDirection.LeftToRight,
                    typeface,
                    emSize,
                    fg);
#pragma warning restore CS0618

                double spanW = ft.WidthIncludingTrailingWhitespace;
                double spanH = ft.Height;

                // Simple wrap check
                if (cursorX + spanW > pad.Left + maxW && cursorX > pad.Left)
                {
                    // wrap to next line
                    cursorX = pad.Left;
                    cursorY += currentLineHeight + spacing;
                    currentLineHeight = 0.0;
                }

                _laidOutSpans.Add(new LaidOutSpan
                {
                    Formatted = ft,
                    Origin = new Point(cursorX, cursorY),
                    Bounds = new Rect(cursorX, cursorY, spanW, spanH),
                    Background = span.Background,
                    IsCode = span.IsCode
                });

                cursorX += spanW;
                currentLineHeight = Math.Max(currentLineHeight, spanH);
                totalMaxWidth = Math.Max(totalMaxWidth, cursorX);
            }

            double totalH = cursorY + currentLineHeight + pad.Bottom;
            double totalW = Math.Max(totalMaxWidth + pad.Right, pad.Left + pad.Right);

            return new Size(totalW, totalH);
        }

        protected override void OnRender(DrawingContext dc)
        {
            for (int i = 0; i < _laidOutSpans.Count; i++)
            {
                LaidOutSpan item = _laidOutSpans[i];

                if (item.Background != null)
                {
                    double radius = item.IsCode ? 3.0 : 0.0;
                    dc.DrawRoundedRectangle(item.Background, null, item.Bounds, radius, radius);
                }

                dc.DrawText(item.Formatted, item.Origin);
            }
        }
    }
}

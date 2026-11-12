// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System;
using System.IO;
using System.Windows.Media;
using System.Windows.Media.Svg;
using MS.Internal;

namespace System.Windows.Controls
{
    /// <summary>
    /// A high-performance vector SVG viewing element that renders resolution-independent SVG graphics
    /// using Direct2D retained DrawingGroup visual trees, with full support for Uniform/Fill stretching and currentColor theming.
    /// </summary>
    public class SvgViewbox : FrameworkElement
    {
        private SvgDocument _document;

        public static readonly DependencyProperty SourceProperty =
            DependencyProperty.Register(
                nameof(Source),
                typeof(string),
                typeof(SvgViewbox),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender, OnSourceChanged));

        public static readonly DependencyProperty StretchProperty =
            DependencyProperty.Register(
                nameof(Stretch),
                typeof(Stretch),
                typeof(SvgViewbox),
                new FrameworkPropertyMetadata(Stretch.Uniform, FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty ForegroundProperty =
            DependencyProperty.Register(
                nameof(Foreground),
                typeof(Brush),
                typeof(SvgViewbox),
                new FrameworkPropertyMetadata(Brushes.Black, FrameworkPropertyMetadataOptions.AffectsRender, OnForegroundChanged));

        public string Source
        {
            get => (string)GetValue(SourceProperty);
            set => SetValue(SourceProperty, value);
        }

        public Stretch Stretch
        {
            get => (Stretch)GetValue(StretchProperty);
            set => SetValue(StretchProperty, value);
        }

        public Brush Foreground
        {
            get => (Brush)GetValue(ForegroundProperty);
            set => SetValue(ForegroundProperty, value);
        }

        public SvgDocument Document => _document;

        public SvgViewbox()
        {
        }

        public SvgViewbox(string source)
        {
            Source = source;
        }

        private static void OnSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is SvgViewbox viewbox)
            {
                viewbox.Reload();
            }
        }

        private static void OnForegroundChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is SvgViewbox viewbox)
            {
                viewbox.Reload();
            }
        }

        private void Reload()
        {
            string src = Source;
            if (string.IsNullOrWhiteSpace(src))
            {
                _document = null;
                InvalidateMeasure();
                InvalidateVisual();
                return;
            }

            try
            {
                Brush fg = Foreground ?? Brushes.Black;
                if (src.TrimStart().StartsWith("<", StringComparison.Ordinal))
                {
                    _document = SvgDocument.Parse(src, fg);
                }
                else if (File.Exists(src))
                {
                    using var stream = File.OpenRead(src);
                    _document = SvgDocument.Load(stream, fg);
                }
                else
                {
                    _document = SvgDocument.Parse(src, fg);
                }
            }
            catch
            {
                _document = null;
            }

            InvalidateMeasure();
            InvalidateVisual();
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            if (_document == null)
            {
                return new Size(0, 0);
            }

            double w = _document.Width;
            double h = _document.Height;
            if (w <= 0.0 || h <= 0.0)
            {
                Rect vb = _document.ViewBox;
                w = vb.Width > 0 ? vb.Width : 24.0;
                h = vb.Height > 0 ? vb.Height : 24.0;
            }

            var contentSize = new Size(w, h);
            Vector scale = ComputeScaleFactor(availableSize, contentSize, Stretch);
            return new Size(contentSize.Width * scale.X, contentSize.Height * scale.Y);
        }

        protected override void OnRender(DrawingContext dc)
        {
            base.OnRender(dc);

            if (_document == null || _document.Drawing == null)
            {
                return;
            }

            Size renderSize = RenderSize;
            if (renderSize.Width <= 0.0 || renderSize.Height <= 0.0)
            {
                return;
            }

            Rect contentBounds = _document.ViewBox;
            if (contentBounds.IsEmpty || contentBounds.Width <= 0.0 || contentBounds.Height <= 0.0)
            {
                contentBounds = _document.Drawing.Bounds;
            }

            if (contentBounds.IsEmpty || contentBounds.Width <= 0.0 || contentBounds.Height <= 0.0)
            {
                contentBounds = new Rect(0, 0, Math.Max(1.0, _document.Width), Math.Max(1.0, _document.Height));
            }

            Vector scale = ComputeScaleFactor(renderSize, contentBounds.Size, Stretch);

            Matrix m = Matrix.Identity;
            m.Translate(-contentBounds.X, -contentBounds.Y);
            m.Scale(scale.X, scale.Y);

            // Center in viewport
            double scaledW = contentBounds.Width * scale.X;
            double scaledH = contentBounds.Height * scale.Y;
            double offsetX = (renderSize.Width - scaledW) * 0.5;
            double offsetY = (renderSize.Height - scaledH) * 0.5;
            m.Translate(offsetX, offsetY);

            dc.PushTransform(new MatrixTransform(m));
            try
            {
                dc.DrawDrawing(_document.Drawing);
            }
            finally
            {
                dc.Pop();
            }
        }

        private static Vector ComputeScaleFactor(Size availableSize, Size contentSize, Stretch stretch)
        {
            double scaleX = 1.0;
            double scaleY = 1.0;

            bool isConstrainedWidth = !double.IsPositiveInfinity(availableSize.Width);
            bool isConstrainedHeight = !double.IsPositiveInfinity(availableSize.Height);

            if ((stretch == Stretch.Uniform || stretch == Stretch.UniformToFill || stretch == Stretch.Fill)
                && (isConstrainedWidth || isConstrainedHeight))
            {
                scaleX = (DoubleUtil.IsZero(contentSize.Width)) ? 0.0 : availableSize.Width / contentSize.Width;
                scaleY = (DoubleUtil.IsZero(contentSize.Height)) ? 0.0 : availableSize.Height / contentSize.Height;

                if (!isConstrainedWidth)
                {
                    scaleX = scaleY;
                }
                else if (!isConstrainedHeight)
                {
                    scaleY = scaleX;
                }
                else
                {
                    switch (stretch)
                    {
                        case Stretch.Uniform:
                            double minscale = scaleX < scaleY ? scaleX : scaleY;
                            scaleX = scaleY = minscale;
                            break;

                        case Stretch.UniformToFill:
                            double maxscale = scaleX > scaleY ? scaleX : scaleY;
                            scaleX = scaleY = maxscale;
                            break;

                        case Stretch.Fill:
                            break;
                    }
                }
            }

            return new Vector(scaleX, scaleY);
        }
    }
}

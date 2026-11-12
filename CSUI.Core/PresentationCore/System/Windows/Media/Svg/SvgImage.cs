// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System;
using System.IO;

namespace System.Windows.Media.Svg
{
    /// <summary>
    /// An ImageSource that wraps an SVG vector graphic, enabling lossless rendering at any scale and DPI.
    /// </summary>
    public sealed class SvgImage : ImageSource
    {
        private SvgDocument _document;
        private DrawingImage _drawingImage;

        public static readonly DependencyProperty SourceProperty =
            DependencyProperty.Register(
                nameof(Source),
                typeof(string),
                typeof(SvgImage),
                new PropertyMetadata(null, OnSourceChanged));

        public string Source
        {
            get => (string)GetValue(SourceProperty);
            set => SetValue(SourceProperty, value);
        }

        public SvgDocument Document => _document;

        public SvgImage()
        {
        }

        public SvgImage(string svgContentOrPath)
        {
            Source = svgContentOrPath;
        }

        public SvgImage(SvgDocument document)
        {
            _document = document;
            _drawingImage = document != null ? document.ToDrawingImage() : null;
        }

        public static SvgImage Parse(string svgText, Brush defaultForeground = null)
        {
            var doc = SvgDocument.Parse(svgText, defaultForeground);
            return new SvgImage(doc);
        }

        private static void OnSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is SvgImage img)
            {
                img.ReloadFromSource((string)e.NewValue);
            }
        }

        private void ReloadFromSource(string source)
        {
            if (string.IsNullOrWhiteSpace(source))
            {
                _document = null;
                _drawingImage = null;
                return;
            }

            try
            {
                if (source.TrimStart().StartsWith("<", StringComparison.Ordinal))
                {
                    _document = SvgDocument.Parse(source);
                }
                else if (File.Exists(source))
                {
                    using var stream = File.OpenRead(source);
                    _document = SvgDocument.Load(stream);
                }
                else
                {
                    _document = SvgDocument.Parse(source);
                }

                _drawingImage = _document?.ToDrawingImage();
            }
            catch
            {
                _document = null;
                _drawingImage = null;
            }
        }

        public override double Width => _document?.Width ?? 0.0;

        public override double Height => _document?.Height ?? 0.0;

        public override ImageMetadata Metadata => null;

        protected override Freezable CreateInstanceCore() => new SvgImage();

        internal override unsafe void DrawD2D(void* d2dContext, Rect rectangle)
        {
            _drawingImage?.DrawD2D(d2dContext, rectangle);
        }
    }
}

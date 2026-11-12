// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System;
using System.IO;

namespace System.Windows.Media.Svg
{
    /// <summary>
    /// Represents a parsed vector SVG document containing a retained Direct2D-backed DrawingGroup visual tree.
    /// </summary>
    public sealed class SvgDocument
    {
        public Rect ViewBox { get; internal set; }
        public double Width { get; internal set; }
        public double Height { get; internal set; }
        public DrawingGroup Drawing { get; }

        public SvgDocument(DrawingGroup drawing, Rect viewBox, double width, double height)
        {
            Drawing = drawing ?? new DrawingGroup();
            ViewBox = viewBox;
            Width = width > 0.0 ? width : (viewBox.Width > 0.0 ? viewBox.Width : 24.0);
            Height = height > 0.0 ? height : (viewBox.Height > 0.0 ? viewBox.Height : 24.0);
        }

        public static SvgDocument Parse(string svgContent, Brush defaultForeground = null)
        {
            ArgumentNullException.ThrowIfNull(svgContent);
            return SvgParser.Parse(svgContent, defaultForeground);
        }

        public static SvgDocument Load(Stream stream, Brush defaultForeground = null)
        {
            ArgumentNullException.ThrowIfNull(stream);
            return SvgParser.Load(stream, defaultForeground);
        }

        public DrawingImage ToDrawingImage()
        {
            return new DrawingImage(Drawing);
        }
    }
}

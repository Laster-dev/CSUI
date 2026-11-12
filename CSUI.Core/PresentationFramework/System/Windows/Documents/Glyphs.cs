// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows.Markup;
using System.Windows.Media;

namespace System.Windows.Documents
{
    /// <summary>
    /// Represents the set of glyphs used for rendering fixed-format text (XPS / Low-level typography).
    /// </summary>
    public sealed class Glyphs : FrameworkElement, IUriContext
    {
        private GlyphRun _cachedGlyphRun;
        private bool _glyphRunDirty = true;

        static Glyphs()
        {
        }

        public static readonly DependencyProperty FillProperty =
            DependencyProperty.Register(
                nameof(Fill),
                typeof(Brush),
                typeof(Glyphs),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty IndicesProperty =
            DependencyProperty.Register(
                nameof(Indices),
                typeof(string),
                typeof(Glyphs),
                new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender, OnGlyphsPropertyChanged));

        public static readonly DependencyProperty UnicodeStringProperty =
            DependencyProperty.Register(
                nameof(UnicodeString),
                typeof(string),
                typeof(Glyphs),
                new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender, OnGlyphsPropertyChanged));

        public static readonly DependencyProperty CaretStopsProperty =
            DependencyProperty.Register(
                nameof(CaretStops),
                typeof(string),
                typeof(Glyphs),
                new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.AffectsRender, OnGlyphsPropertyChanged));

        public static readonly DependencyProperty FontRenderingEmSizeProperty =
            DependencyProperty.Register(
                nameof(FontRenderingEmSize),
                typeof(double),
                typeof(Glyphs),
                new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender, OnGlyphsPropertyChanged));

        public static readonly DependencyProperty OriginXProperty =
            DependencyProperty.Register(
                nameof(OriginX),
                typeof(double),
                typeof(Glyphs),
                new FrameworkPropertyMetadata(double.NaN, FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender, OnGlyphsPropertyChanged));

        public static readonly DependencyProperty OriginYProperty =
            DependencyProperty.Register(
                nameof(OriginY),
                typeof(double),
                typeof(Glyphs),
                new FrameworkPropertyMetadata(double.NaN, FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender, OnGlyphsPropertyChanged));

        public static readonly DependencyProperty IsSidewaysProperty =
            DependencyProperty.Register(
                nameof(IsSideways),
                typeof(bool),
                typeof(Glyphs),
                new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender, OnGlyphsPropertyChanged));

        public static readonly DependencyProperty BidiLevelProperty =
            DependencyProperty.Register(
                nameof(BidiLevel),
                typeof(int),
                typeof(Glyphs),
                new FrameworkPropertyMetadata(0, FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender, OnGlyphsPropertyChanged));

        public static readonly DependencyProperty DeviceFontNameProperty =
            DependencyProperty.Register(
                nameof(DeviceFontName),
                typeof(string),
                typeof(Glyphs),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender, OnGlyphsPropertyChanged));

        public static readonly DependencyProperty StyleSimulationsProperty =
            DependencyProperty.Register(
                nameof(StyleSimulations),
                typeof(StyleSimulations),
                typeof(Glyphs),
                new FrameworkPropertyMetadata(StyleSimulations.None, FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender, OnGlyphsPropertyChanged));

        public static readonly DependencyProperty FontUriProperty =
            DependencyProperty.Register(
                nameof(FontUri),
                typeof(Uri),
                typeof(Glyphs),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender, OnGlyphsPropertyChanged));

        public Brush Fill
        {
            get => (Brush)GetValue(FillProperty);
            set => SetValue(FillProperty, value);
        }

        public string Indices
        {
            get => (string)GetValue(IndicesProperty);
            set => SetValue(IndicesProperty, value);
        }

        public string UnicodeString
        {
            get => (string)GetValue(UnicodeStringProperty);
            set => SetValue(UnicodeStringProperty, value);
        }

        public string CaretStops
        {
            get => (string)GetValue(CaretStopsProperty);
            set => SetValue(CaretStopsProperty, value);
        }

        public double FontRenderingEmSize
        {
            get => (double)GetValue(FontRenderingEmSizeProperty);
            set => SetValue(FontRenderingEmSizeProperty, value);
        }

        public double OriginX
        {
            get => (double)GetValue(OriginXProperty);
            set => SetValue(OriginXProperty, value);
        }

        public double OriginY
        {
            get => (double)GetValue(OriginYProperty);
            set => SetValue(OriginYProperty, value);
        }

        public bool IsSideways
        {
            get => (bool)GetValue(IsSidewaysProperty);
            set => SetValue(IsSidewaysProperty, value);
        }

        public int BidiLevel
        {
            get => (int)GetValue(BidiLevelProperty);
            set => SetValue(BidiLevelProperty, value);
        }

        public string DeviceFontName
        {
            get => (string)GetValue(DeviceFontNameProperty);
            set => SetValue(DeviceFontNameProperty, value);
        }

        public StyleSimulations StyleSimulations
        {
            get => (StyleSimulations)GetValue(StyleSimulationsProperty);
            set => SetValue(StyleSimulationsProperty, value);
        }

        public Uri FontUri
        {
            get => (Uri)GetValue(FontUriProperty);
            set => SetValue(FontUriProperty, value);
        }

        Uri IUriContext.BaseUri { get; set; }

        private static void OnGlyphsPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((Glyphs)d)._glyphRunDirty = true;
        }

        public GlyphRun ToGlyphRun()
        {
            if (!_glyphRunDirty)
            {
                return _cachedGlyphRun;
            }

            _cachedGlyphRun = BuildGlyphRun();
            _glyphRunDirty = false;
            return _cachedGlyphRun;
        }

        protected override Size MeasureOverride(Size constraint)
        {
            GlyphRun run = TryBuildOrGetGlyphRun();
            if (run == null)
            {
                return Size.Empty;
            }

            Rect box = run.ComputeAlignmentBox();
            return new Size(Math.Max(0.0, box.Right), Math.Max(0.0, box.Bottom));
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            return finalSize;
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            if (drawingContext == null)
            {
                return;
            }

            GlyphRun run = TryBuildOrGetGlyphRun();
            Brush fill = Fill;
            if (run != null && fill != null)
            {
                drawingContext.DrawGlyphRun(fill, run);
            }
        }

        private GlyphRun TryBuildOrGetGlyphRun()
        {
            if (!_glyphRunDirty)
            {
                return _cachedGlyphRun;
            }

            if (FontUri == null || FontRenderingEmSize <= 0.0 ||
                (string.IsNullOrEmpty(UnicodeString) && string.IsNullOrEmpty(Indices)))
            {
                _cachedGlyphRun = null;
                _glyphRunDirty = false;
                return null;
            }

            return ToGlyphRun();
        }

        private GlyphRun BuildGlyphRun()
        {
            Uri fontUri = FontUri;
            if (fontUri == null)
            {
                throw new InvalidOperationException("FontUri must be specified on Glyphs.");
            }

            Uri baseUri = ((IUriContext)this).BaseUri;
            Uri resolvedUri = (!fontUri.IsAbsoluteUri && baseUri != null) ? new Uri(baseUri, fontUri) : fontUri;
            var typeface = new GlyphTypeface(resolvedUri, StyleSimulations);

            string unicode = UnicodeString ?? string.Empty;
            string indices = Indices ?? string.Empty;
            double emSize = FontRenderingEmSize;

            var glyphIndices = new List<ushort>();
            var advanceWidths = new List<double>();
            var glyphOffsets = new List<Point>();
            var clusterMap = unicode.Length > 0 ? new ushort[unicode.Length] : null;

            ParseIndices(typeface, emSize, unicode, indices, glyphIndices, advanceWidths, glyphOffsets, clusterMap);

            if (glyphIndices.Count == 0)
            {
                return null;
            }

            double originX = double.IsNaN(OriginX) ? 0.0 : OriginX;
            double originY = double.IsNaN(OriginY) ? emSize * typeface.Baseline : OriginY;
            Point origin = new Point(originX, originY);

            IList<bool> caretStops = ParseCaretStops(CaretStops, glyphIndices.Count);
            char[] chars = unicode.Length > 0 ? unicode.ToCharArray() : null;

            return new GlyphRun(
                typeface,
                BidiLevel,
                IsSideways,
                emSize,
                (float)VisualTreeHelper.GetDpi(this).PixelsPerDip,
                glyphIndices,
                origin,
                advanceWidths,
                glyphOffsets,
                chars,
                DeviceFontName,
                clusterMap,
                caretStops,
                Language);
        }

        private static void ParseIndices(
            GlyphTypeface typeface,
            double emSize,
            string unicode,
            string indices,
            List<ushort> glyphIndices,
            List<double> advanceWidths,
            List<Point> glyphOffsets,
            ushort[] clusterMap)
        {
            double emToDip = emSize / 100.0;
            string[] specs = string.IsNullOrEmpty(indices) ? Array.Empty<string>() : indices.Split(';');
            int specIndex = 0;
            int charIndex = 0;

            while (specIndex < specs.Length || charIndex < unicode.Length)
            {
                string spec = specIndex < specs.Length ? specs[specIndex].Trim() : string.Empty;
                specIndex++;

                int codeUnitCount = 1;
                int clusterGlyphCount = 1;

                if (spec.StartsWith("(", StringComparison.Ordinal))
                {
                    int close = spec.IndexOf(')');
                    if (close > 1)
                    {
                        string clusterSpec = spec.Substring(1, close - 1);
                        int colon = clusterSpec.IndexOf(':');
                        if (colon > 0)
                        {
                            int.TryParse(clusterSpec.Substring(0, colon), NumberStyles.Integer, CultureInfo.InvariantCulture, out codeUnitCount);
                            int.TryParse(clusterSpec.Substring(colon + 1), NumberStyles.Integer, CultureInfo.InvariantCulture, out clusterGlyphCount);
                        }
                        else
                        {
                            int.TryParse(clusterSpec, NumberStyles.Integer, CultureInfo.InvariantCulture, out codeUnitCount);
                        }
                        codeUnitCount = Math.Max(1, codeUnitCount);
                        clusterGlyphCount = Math.Max(1, clusterGlyphCount);
                        spec = spec.Substring(close + 1);
                    }
                }

                string[] fields = spec.Split(',');
                ushort glyphIndex;
                if (fields.Length > 0 && !string.IsNullOrWhiteSpace(fields[0]) &&
                    ushort.TryParse(fields[0].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out ushort parsedGlyph))
                {
                    glyphIndex = parsedGlyph;
                }
                else
                {
                    int codePoint = charIndex < unicode.Length ? unicode[charIndex] : ' ';
                    if (!typeface.CharacterToGlyphMap.TryGetValue(codePoint, out glyphIndex))
                    {
                        glyphIndex = 0;
                    }
                }

                double advance = (fields.Length > 1 && double.TryParse(fields[1].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out double parsedAdv))
                    ? parsedAdv * emToDip
                    : (typeface.AdvanceWidths.TryGetValue(glyphIndex, out double faceAdv) ? faceAdv * emSize : emSize * 0.5);

                double uOffset = (fields.Length > 2 && double.TryParse(fields[2].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out double parsedU))
                    ? parsedU * emToDip
                    : 0.0;

                double vOffset = (fields.Length > 3 && double.TryParse(fields[3].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out double parsedV))
                    ? parsedV * emToDip
                    : 0.0;

                ushort firstGlyphInCluster = (ushort)glyphIndices.Count;
                glyphIndices.Add(glyphIndex);
                advanceWidths.Add(advance);
                glyphOffsets.Add(new Point(uOffset, vOffset));

                if (clusterMap != null)
                {
                    for (int c = 0; c < codeUnitCount && charIndex + c < clusterMap.Length; c++)
                    {
                        clusterMap[charIndex + c] = firstGlyphInCluster;
                    }
                }
                charIndex += codeUnitCount;
            }
        }

        private static IList<bool> ParseCaretStops(string caretStops, int glyphCount)
        {
            if (string.IsNullOrWhiteSpace(caretStops))
            {
                return null;
            }

            var bits = new List<bool>(glyphCount + 1);
            foreach (char ch in caretStops)
            {
                int val;
                if (ch >= '0' && ch <= '9') val = ch - '0';
                else if (ch >= 'a' && ch <= 'f') val = ch - 'a' + 10;
                else if (ch >= 'A' && ch <= 'F') val = ch - 'A' + 10;
                else continue;

                bits.Add((val & 8) != 0);
                bits.Add((val & 4) != 0);
                bits.Add((val & 2) != 0);
                bits.Add((val & 1) != 0);
            }

            if (bits.Count < glyphCount + 1)
            {
                return null;
            }

            return bits.GetRange(0, glyphCount + 1).ToArray();
        }
    }
}

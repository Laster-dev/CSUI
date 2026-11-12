// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Xml;

namespace System.Windows.Media.Svg
{
    /// <summary>
    /// Zero-reflection, high-performance streaming SVG parser that converts SVG XML into a retained WPF/CSUI DrawingGroup.
    /// </summary>
    public static class SvgParser
    {
        private sealed class SvgContext
        {
            public Brush Fill { get; set; } = Brushes.Black;
            public Brush Stroke { get; set; } = null;
            public double StrokeThickness { get; set; } = 1.0;
            public PenLineCap StrokeStartLineCap { get; set; } = PenLineCap.Flat;
            public PenLineCap StrokeEndLineCap { get; set; } = PenLineCap.Flat;
            public PenLineJoin StrokeLineJoin { get; set; } = PenLineJoin.Miter;
            public double Opacity { get; set; } = 1.0;
            public Transform Transform { get; set; }

            public SvgContext Clone()
            {
                return new SvgContext
                {
                    Fill = Fill,
                    Stroke = Stroke,
                    StrokeThickness = StrokeThickness,
                    StrokeStartLineCap = StrokeStartLineCap,
                    StrokeEndLineCap = StrokeEndLineCap,
                    StrokeLineJoin = StrokeLineJoin,
                    Opacity = Opacity,
                    Transform = Transform
                };
            }
        }

        public static SvgDocument Parse(string svgContent, Brush defaultForeground = null)
        {
            if (string.IsNullOrWhiteSpace(svgContent))
            {
                return new SvgDocument(new DrawingGroup(), Rect.Empty, 0, 0);
            }

            using var stringReader = new StringReader(svgContent.Trim());
            return Load(stringReader, defaultForeground);
        }

        public static SvgDocument Load(Stream stream, Brush defaultForeground = null)
        {
            ArgumentNullException.ThrowIfNull(stream);
            using var textReader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
            return Load(textReader, defaultForeground);
        }

        public static SvgDocument Load(TextReader reader, Brush defaultForeground = null)
        {
            var settings = new XmlReaderSettings
            {
                DtdProcessing = DtdProcessing.Ignore,
                IgnoreComments = true,
                IgnoreWhitespace = true,
                IgnoreProcessingInstructions = true
            };

            using var xml = XmlReader.Create(reader, settings);

            var rootDrawing = new DrawingGroup();
            var defs = new Dictionary<string, Brush>(StringComparer.OrdinalIgnoreCase);
            var groupStack = new Stack<(DrawingGroup group, SvgContext context)>();

            var currentContext = new SvgContext();
            if (defaultForeground != null)
            {
                currentContext.Fill = defaultForeground;
            }

            groupStack.Push((rootDrawing, currentContext));

            Rect viewBox = Rect.Empty;
            double rootWidth = 0.0;
            double rootHeight = 0.0;

            while (xml.Read())
            {
                if (xml.NodeType == XmlNodeType.Element)
                {
                    string localName = xml.LocalName;
                    bool isEmpty = xml.IsEmptyElement;

                    if (string.Equals(localName, "svg", StringComparison.OrdinalIgnoreCase))
                    {
                        ParseSvgRootAttributes(xml, ref viewBox, ref rootWidth, ref rootHeight);
                        var newContext = groupStack.Peek().context.Clone();
                        ApplyElementStyles(xml, newContext, defs, defaultForeground);
                        if (newContext.Transform != null)
                        {
                            rootDrawing.Transform = newContext.Transform;
                        }
                    }
                    else if (string.Equals(localName, "defs", StringComparison.OrdinalIgnoreCase))
                    {
                        if (!isEmpty)
                        {
                            ParseDefs(xml.ReadSubtree(), defs, defaultForeground);
                        }
                    }
                    else if (string.Equals(localName, "linearGradient", StringComparison.OrdinalIgnoreCase) ||
                             string.Equals(localName, "radialGradient", StringComparison.OrdinalIgnoreCase))
                    {
                        ParseGradientElement(xml, defs, defaultForeground);
                    }
                    else if (string.Equals(localName, "g", StringComparison.OrdinalIgnoreCase))
                    {
                        var parent = groupStack.Peek();
                        var subGroup = new DrawingGroup();
                        var childContext = parent.context.Clone();
                        ApplyElementStyles(xml, childContext, defs, defaultForeground);

                        if (childContext.Transform != null)
                        {
                            subGroup.Transform = childContext.Transform;
                        }
                        if (childContext.Opacity < 1.0)
                        {
                            subGroup.Opacity = childContext.Opacity;
                        }

                        parent.group.Children.Add(subGroup);

                        if (!isEmpty)
                        {
                            groupStack.Push((subGroup, childContext));
                        }
                    }
                    else
                    {
                        var parent = groupStack.Peek();
                        var elemContext = parent.context.Clone();
                        ApplyElementStyles(xml, elemContext, defs, defaultForeground);

                        Geometry geometry = ParseGeometryElement(xml, localName);
                        if (geometry != null)
                        {
                            Pen pen = null;
                            if (elemContext.Stroke != null && elemContext.StrokeThickness > 0.0)
                            {
                                pen = new Pen(elemContext.Stroke, elemContext.StrokeThickness)
                                {
                                    StartLineCap = elemContext.StrokeStartLineCap,
                                    EndLineCap = elemContext.StrokeEndLineCap,
                                    LineJoin = elemContext.StrokeLineJoin
                                };
                            }

                            Brush fill = elemContext.Fill;
                            if (fill != null || pen != null)
                            {
                                var geomDrawing = new GeometryDrawing(fill, pen, geometry);
                                parent.group.Children.Add(geomDrawing);
                            }
                        }
                    }
                }
                else if (xml.NodeType == XmlNodeType.EndElement)
                {
                    string localName = xml.LocalName;
                    if (string.Equals(localName, "g", StringComparison.OrdinalIgnoreCase) && groupStack.Count > 1)
                    {
                        groupStack.Pop();
                    }
                }
            }

            if (viewBox.IsEmpty)
            {
                Rect contentBounds = rootDrawing.Bounds;
                if (!contentBounds.IsEmpty && contentBounds.Width > 0.0 && contentBounds.Height > 0.0)
                {
                    viewBox = contentBounds;
                }
                else if (rootWidth > 0.0 && rootHeight > 0.0)
                {
                    viewBox = new Rect(0, 0, rootWidth, rootHeight);
                }
            }

            if (rootWidth <= 0.0 && !viewBox.IsEmpty)
            {
                rootWidth = viewBox.Width;
            }
            if (rootHeight <= 0.0 && !viewBox.IsEmpty)
            {
                rootHeight = viewBox.Height;
            }

            return new SvgDocument(rootDrawing, viewBox, rootWidth, rootHeight);
        }

        private static void ParseSvgRootAttributes(XmlReader xml, ref Rect viewBox, ref double width, ref double height)
        {
            string vbStr = xml.GetAttribute("viewBox");
            if (!string.IsNullOrWhiteSpace(vbStr))
            {
                string[] parts = vbStr.Split(new[] { ' ', ',', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length == 4 &&
                    double.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out double minX) &&
                    double.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out double minY) &&
                    double.TryParse(parts[2], NumberStyles.Float, CultureInfo.InvariantCulture, out double vbW) &&
                    double.TryParse(parts[3], NumberStyles.Float, CultureInfo.InvariantCulture, out double vbH))
                {
                    viewBox = new Rect(minX, minY, vbW, vbH);
                }
            }

            string wStr = xml.GetAttribute("width");
            if (!string.IsNullOrWhiteSpace(wStr))
            {
                width = ParseUnit(wStr);
            }

            string hStr = xml.GetAttribute("height");
            if (!string.IsNullOrWhiteSpace(hStr))
            {
                height = ParseUnit(hStr);
            }
        }

        private static void ParseDefs(XmlReader xml, Dictionary<string, Brush> defs, Brush defaultForeground)
        {
            while (xml.Read())
            {
                if (xml.NodeType == XmlNodeType.Element)
                {
                    string name = xml.LocalName;
                    if (string.Equals(name, "linearGradient", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(name, "radialGradient", StringComparison.OrdinalIgnoreCase))
                    {
                        ParseGradientElement(xml, defs, defaultForeground);
                    }
                }
            }
        }

        private static void ParseGradientElement(XmlReader xml, Dictionary<string, Brush> defs, Brush defaultForeground)
        {
            string id = xml.GetAttribute("id");
            bool isLinear = string.Equals(xml.LocalName, "linearGradient", StringComparison.OrdinalIgnoreCase);

            Point startPoint = new Point(0, 0);
            Point endPoint = new Point(1, 0);
            Point center = new Point(0.5, 0.5);
            double radiusX = 0.5;
            double radiusY = 0.5;

            if (isLinear)
            {
                string x1Str = xml.GetAttribute("x1");
                string y1Str = xml.GetAttribute("y1");
                string x2Str = xml.GetAttribute("x2");
                string y2Str = xml.GetAttribute("y2");

                if (!string.IsNullOrWhiteSpace(x1Str)) startPoint.X = ParsePercentage(x1Str);
                if (!string.IsNullOrWhiteSpace(y1Str)) startPoint.Y = ParsePercentage(y1Str);
                if (!string.IsNullOrWhiteSpace(x2Str)) endPoint.X = ParsePercentage(x2Str);
                if (!string.IsNullOrWhiteSpace(y2Str)) endPoint.Y = ParsePercentage(y2Str);
            }
            else
            {
                string cxStr = xml.GetAttribute("cx");
                string cyStr = xml.GetAttribute("cy");
                string rStr = xml.GetAttribute("r");
                if (!string.IsNullOrWhiteSpace(cxStr)) center.X = ParsePercentage(cxStr);
                if (!string.IsNullOrWhiteSpace(cyStr)) center.Y = ParsePercentage(cyStr);
                if (!string.IsNullOrWhiteSpace(rStr)) radiusX = radiusY = ParsePercentage(rStr);
            }

            var stops = new GradientStopCollection();
            bool isEmpty = xml.IsEmptyElement;

            if (!isEmpty)
            {
                using var sub = xml.ReadSubtree();
                while (sub.Read())
                {
                    if (sub.NodeType == XmlNodeType.Element && string.Equals(sub.LocalName, "stop", StringComparison.OrdinalIgnoreCase))
                    {
                        string offsetStr = sub.GetAttribute("offset") ?? "0";
                        double offset = ParsePercentage(offsetStr);

                        Color color = Colors.Black;
                        string stopColor = sub.GetAttribute("stop-color");
                        if (!string.IsNullOrWhiteSpace(stopColor))
                        {
                            color = ParseColor(stopColor, defaultForeground);
                        }

                        string stopOpacity = sub.GetAttribute("stop-opacity");
                        if (!string.IsNullOrWhiteSpace(stopOpacity) &&
                            double.TryParse(stopOpacity, NumberStyles.Float, CultureInfo.InvariantCulture, out double op))
                        {
                            color.A = (byte)(Math.Clamp(op, 0.0, 1.0) * color.A);
                        }

                        stops.Add(new GradientStop(color, offset));
                    }
                }
            }

            Brush brush;
            if (isLinear)
            {
                brush = new LinearGradientBrush(stops, startPoint, endPoint);
            }
            else
            {
                brush = new RadialGradientBrush(stops)
                {
                    Center = center,
                    GradientOrigin = center,
                    RadiusX = radiusX,
                    RadiusY = radiusY
                };
            }

            if (!string.IsNullOrWhiteSpace(id))
            {
                defs[id.Trim()] = brush;
            }
        }

        private static Geometry ParseGeometryElement(XmlReader xml, string localName)
        {
            switch (localName.ToLowerInvariant())
            {
                case "path":
                    string d = xml.GetAttribute("d");
                    if (!string.IsNullOrWhiteSpace(d))
                    {
                        try
                        {
                            return Geometry.Parse(d);
                        }
                        catch
                        {
                            return null;
                        }
                    }
                    break;

                case "rect":
                    double x = ParseUnit(xml.GetAttribute("x") ?? "0");
                    double y = ParseUnit(xml.GetAttribute("y") ?? "0");
                    double w = ParseUnit(xml.GetAttribute("width") ?? "0");
                    double h = ParseUnit(xml.GetAttribute("height") ?? "0");
                    double rx = ParseUnit(xml.GetAttribute("rx") ?? "0");
                    double ry = ParseUnit(xml.GetAttribute("ry") ?? (rx > 0 ? rx.ToString(CultureInfo.InvariantCulture) : "0"));
                    if (w > 0 && h > 0)
                    {
                        return new RectangleGeometry(new Rect(x, y, w, h), rx, ry);
                    }
                    break;

                case "circle":
                    double cx = ParseUnit(xml.GetAttribute("cx") ?? "0");
                    double cy = ParseUnit(xml.GetAttribute("cy") ?? "0");
                    double r = ParseUnit(xml.GetAttribute("r") ?? "0");
                    if (r > 0)
                    {
                        return new EllipseGeometry(new Point(cx, cy), r, r);
                    }
                    break;

                case "ellipse":
                    double ecx = ParseUnit(xml.GetAttribute("cx") ?? "0");
                    double ecy = ParseUnit(xml.GetAttribute("cy") ?? "0");
                    double erx = ParseUnit(xml.GetAttribute("rx") ?? "0");
                    double ery = ParseUnit(xml.GetAttribute("ry") ?? "0");
                    if (erx > 0 && ery > 0)
                    {
                        return new EllipseGeometry(new Point(ecx, ecy), erx, ery);
                    }
                    break;

                case "line":
                    double x1 = ParseUnit(xml.GetAttribute("x1") ?? "0");
                    double y1 = ParseUnit(xml.GetAttribute("y1") ?? "0");
                    double x2 = ParseUnit(xml.GetAttribute("x2") ?? "0");
                    double y2 = ParseUnit(xml.GetAttribute("y2") ?? "0");
                    return new LineGeometry(new Point(x1, y1), new Point(x2, y2));

                case "polyline":
                case "polygon":
                    string ptsStr = xml.GetAttribute("points");
                    if (!string.IsNullOrWhiteSpace(ptsStr))
                    {
                        var points = ParsePointsList(ptsStr);
                        if (points.Count > 1)
                        {
                            var streamGeom = new StreamGeometry();
                            using (var ctx = streamGeom.Open())
                            {
                                bool isPolygon = string.Equals(localName, "polygon", StringComparison.OrdinalIgnoreCase);
                                ctx.BeginFigure(points[0], isFilled: isPolygon, isClosed: isPolygon);
                                for (int i = 1; i < points.Count; i++)
                                {
                                    ctx.LineTo(points[i], isStroked: true, isSmoothJoin: false);
                                }
                            }
                            streamGeom.Freeze();
                            return streamGeom;
                        }
                    }
                    break;
            }

            return null;
        }

        private static List<Point> ParsePointsList(string pointsStr)
        {
            var result = new List<Point>();
            string[] tokens = pointsStr.Split(new[] { ' ', ',', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i + 1 < tokens.Length; i += 2)
            {
                if (double.TryParse(tokens[i], NumberStyles.Float, CultureInfo.InvariantCulture, out double px) &&
                    double.TryParse(tokens[i + 1], NumberStyles.Float, CultureInfo.InvariantCulture, out double py))
                {
                    result.Add(new Point(px, py));
                }
            }
            return result;
        }

        private static void ApplyElementStyles(XmlReader xml, SvgContext context, Dictionary<string, Brush> defs, Brush defaultForeground)
        {
            string style = xml.GetAttribute("style");
            if (!string.IsNullOrWhiteSpace(style))
            {
                var dict = ParseInlineStyle(style);
                ApplyStyleDictionary(dict, context, defs, defaultForeground);
            }

            string fill = xml.GetAttribute("fill");
            if (!string.IsNullOrWhiteSpace(fill))
            {
                context.Fill = ResolveBrush(fill, defs, defaultForeground);
            }

            string stroke = xml.GetAttribute("stroke");
            if (!string.IsNullOrWhiteSpace(stroke))
            {
                context.Stroke = ResolveBrush(stroke, defs, defaultForeground);
            }

            string strokeWidth = xml.GetAttribute("stroke-width");
            if (!string.IsNullOrWhiteSpace(strokeWidth))
            {
                context.StrokeThickness = ParseUnit(strokeWidth);
            }

            string strokeLineCap = xml.GetAttribute("stroke-linecap");
            if (!string.IsNullOrWhiteSpace(strokeLineCap))
            {
                context.StrokeStartLineCap = context.StrokeEndLineCap = ParseLineCap(strokeLineCap);
            }

            string strokeLineJoin = xml.GetAttribute("stroke-linejoin");
            if (!string.IsNullOrWhiteSpace(strokeLineJoin))
            {
                context.StrokeLineJoin = ParseLineJoin(strokeLineJoin);
            }

            string opacity = xml.GetAttribute("opacity");
            if (!string.IsNullOrWhiteSpace(opacity) &&
                double.TryParse(opacity, NumberStyles.Float, CultureInfo.InvariantCulture, out double op))
            {
                context.Opacity = Math.Clamp(op, 0.0, 1.0);
            }

            string transform = xml.GetAttribute("transform");
            if (!string.IsNullOrWhiteSpace(transform))
            {
                var tr = ParseTransform(transform);
                if (tr != null)
                {
                    context.Transform = context.Transform != null ? new MatrixTransform(Matrix.Multiply(tr.Value, context.Transform.Value)) : tr;
                }
            }
        }

        private static Dictionary<string, string> ParseInlineStyle(string style)
        {
            var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            string[] declarations = style.Split(';', StringSplitOptions.RemoveEmptyEntries);
            foreach (var decl in declarations)
            {
                int colon = decl.IndexOf(':');
                if (colon > 0)
                {
                    string key = decl.Substring(0, colon).Trim();
                    string val = decl.Substring(colon + 1).Trim();
                    dict[key] = val;
                }
            }
            return dict;
        }

        private static void ApplyStyleDictionary(Dictionary<string, string> dict, SvgContext context, Dictionary<string, Brush> defs, Brush defaultForeground)
        {
            if (dict.TryGetValue("fill", out var fill))
            {
                context.Fill = ResolveBrush(fill, defs, defaultForeground);
            }
            if (dict.TryGetValue("stroke", out var stroke))
            {
                context.Stroke = ResolveBrush(stroke, defs, defaultForeground);
            }
            if (dict.TryGetValue("stroke-width", out var sw))
            {
                context.StrokeThickness = ParseUnit(sw);
            }
            if (dict.TryGetValue("stroke-linecap", out var lc))
            {
                context.StrokeStartLineCap = context.StrokeEndLineCap = ParseLineCap(lc);
            }
            if (dict.TryGetValue("stroke-linejoin", out var lj))
            {
                context.StrokeLineJoin = ParseLineJoin(lj);
            }
            if (dict.TryGetValue("opacity", out var opStr) &&
                double.TryParse(opStr, NumberStyles.Float, CultureInfo.InvariantCulture, out double op))
            {
                context.Opacity = Math.Clamp(op, 0.0, 1.0);
            }
        }

        private static Brush ResolveBrush(string value, Dictionary<string, Brush> defs, Brush defaultForeground)
        {
            if (string.IsNullOrWhiteSpace(value) || string.Equals(value, "none", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            value = value.Trim();

            if (string.Equals(value, "currentColor", StringComparison.OrdinalIgnoreCase))
            {
                return defaultForeground ?? Brushes.Black;
            }

            if (value.StartsWith("url(", StringComparison.OrdinalIgnoreCase) && value.EndsWith(")"))
            {
                string target = value.Substring(4, value.Length - 5).Trim();
                if (target.StartsWith("#"))
                {
                    target = target.Substring(1);
                }
                if (defs != null && defs.TryGetValue(target, out var gradBrush))
                {
                    return gradBrush;
                }
            }

            Color color = ParseColor(value, defaultForeground);
            return new SolidColorBrush(color);
        }

        private static Color ParseColor(string colorStr, Brush defaultForeground)
        {
            if (string.IsNullOrWhiteSpace(colorStr) || string.Equals(colorStr, "transparent", StringComparison.OrdinalIgnoreCase))
            {
                return Colors.Transparent;
            }

            colorStr = colorStr.Trim();

            if (string.Equals(colorStr, "currentColor", StringComparison.OrdinalIgnoreCase))
            {
                if (defaultForeground is SolidColorBrush scb)
                {
                    return scb.Color;
                }
                return Colors.Black;
            }

            if (colorStr.StartsWith("#"))
            {
                string hex = colorStr.Substring(1);
                if (hex.Length == 3)
                {
                    byte r = Convert.ToByte(new string(hex[0], 2), 16);
                    byte g = Convert.ToByte(new string(hex[1], 2), 16);
                    byte b = Convert.ToByte(new string(hex[2], 2), 16);
                    return Color.FromArgb(255, r, g, b);
                }
                if (hex.Length == 6)
                {
                    byte r = Convert.ToByte(hex.Substring(0, 2), 16);
                    byte g = Convert.ToByte(hex.Substring(2, 2), 16);
                    byte b = Convert.ToByte(hex.Substring(4, 2), 16);
                    return Color.FromArgb(255, r, g, b);
                }
                if (hex.Length == 8)
                {
                    byte a = Convert.ToByte(hex.Substring(0, 2), 16);
                    byte r = Convert.ToByte(hex.Substring(2, 2), 16);
                    byte g = Convert.ToByte(hex.Substring(4, 2), 16);
                    byte b = Convert.ToByte(hex.Substring(6, 2), 16);
                    return Color.FromArgb(a, r, g, b);
                }
            }

            if (colorStr.StartsWith("rgb(", StringComparison.OrdinalIgnoreCase) && colorStr.EndsWith(")"))
            {
                string inner = colorStr.Substring(4, colorStr.Length - 5);
                string[] parts = inner.Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length == 3)
                {
                    byte r = (byte)Math.Clamp(ParseUnit(parts[0]), 0, 255);
                    byte g = (byte)Math.Clamp(ParseUnit(parts[1]), 0, 255);
                    byte b = (byte)Math.Clamp(ParseUnit(parts[2]), 0, 255);
                    return Color.FromArgb(255, r, g, b);
                }
            }

            if (colorStr.StartsWith("rgba(", StringComparison.OrdinalIgnoreCase) && colorStr.EndsWith(")"))
            {
                string inner = colorStr.Substring(5, colorStr.Length - 6);
                string[] parts = inner.Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length == 4)
                {
                    byte r = (byte)Math.Clamp(ParseUnit(parts[0]), 0, 255);
                    byte g = (byte)Math.Clamp(ParseUnit(parts[1]), 0, 255);
                    byte b = (byte)Math.Clamp(ParseUnit(parts[2]), 0, 255);
                    double aDouble = double.Parse(parts[3], CultureInfo.InvariantCulture);
                    byte a = (byte)(Math.Clamp(aDouble, 0.0, 1.0) * 255.0);
                    return Color.FromArgb(a, r, g, b);
                }
            }

            return colorStr.ToLowerInvariant() switch
            {
                "black" => Colors.Black,
                "white" => Colors.White,
                "red" => Colors.Red,
                "green" => Colors.Green,
                "blue" => Colors.Blue,
                "yellow" => Colors.Yellow,
                "gray" or "grey" => Colors.Gray,
                "lightgray" or "lightgrey" => Colors.LightGray,
                "darkgray" or "darkgrey" => Colors.DarkGray,
                "cyan" => Colors.Cyan,
                "magenta" => Colors.Magenta,
                "orange" => Colors.Orange,
                "purple" => Colors.Purple,
                "dodgerblue" => Color.FromArgb(255, 30, 144, 255),
                "gold" => Colors.Gold,
                _ => Colors.Black
            };
        }

        private static PenLineCap ParseLineCap(string cap)
        {
            return cap.ToLowerInvariant() switch
            {
                "round" => PenLineCap.Round,
                "square" => PenLineCap.Square,
                _ => PenLineCap.Flat
            };
        }

        private static PenLineJoin ParseLineJoin(string join)
        {
            return join.ToLowerInvariant() switch
            {
                "round" => PenLineJoin.Round,
                "bevel" => PenLineJoin.Bevel,
                _ => PenLineJoin.Miter
            };
        }

        private static Transform ParseTransform(string transformStr)
        {
            if (string.IsNullOrWhiteSpace(transformStr)) return null;

            transformStr = transformStr.Trim();
            int openParen = transformStr.IndexOf('(');
            int closeParen = transformStr.IndexOf(')');
            if (openParen <= 0 || closeParen <= openParen) return null;

            string type = transformStr.Substring(0, openParen).Trim().ToLowerInvariant();
            string argsStr = transformStr.Substring(openParen + 1, closeParen - openParen - 1);
            string[] args = argsStr.Split(new[] { ' ', ',', '\t' }, StringSplitOptions.RemoveEmptyEntries);

            Matrix m = Matrix.Identity;
            switch (type)
            {
                case "translate":
                    if (args.Length >= 1 && double.TryParse(args[0], NumberStyles.Float, CultureInfo.InvariantCulture, out double tx))
                    {
                        double ty = (args.Length >= 2 && double.TryParse(args[1], NumberStyles.Float, CultureInfo.InvariantCulture, out double parsedTy)) ? parsedTy : 0.0;
                        m.Translate(tx, ty);
                    }
                    break;

                case "scale":
                    if (args.Length >= 1 && double.TryParse(args[0], NumberStyles.Float, CultureInfo.InvariantCulture, out double sx))
                    {
                        double sy = (args.Length >= 2 && double.TryParse(args[1], NumberStyles.Float, CultureInfo.InvariantCulture, out double parsedSy)) ? parsedSy : sx;
                        m.Scale(sx, sy);
                    }
                    break;

                case "rotate":
                    if (args.Length >= 1 && double.TryParse(args[0], NumberStyles.Float, CultureInfo.InvariantCulture, out double angle))
                    {
                        if (args.Length >= 3 &&
                            double.TryParse(args[1], NumberStyles.Float, CultureInfo.InvariantCulture, out double cx) &&
                            double.TryParse(args[2], NumberStyles.Float, CultureInfo.InvariantCulture, out double cy))
                        {
                            m.RotateAt(angle, cx, cy);
                        }
                        else
                        {
                            m.Rotate(angle);
                        }
                    }
                    break;

                case "matrix":
                    if (args.Length >= 6 &&
                        double.TryParse(args[0], NumberStyles.Float, CultureInfo.InvariantCulture, out double m11) &&
                        double.TryParse(args[1], NumberStyles.Float, CultureInfo.InvariantCulture, out double m12) &&
                        double.TryParse(args[2], NumberStyles.Float, CultureInfo.InvariantCulture, out double m21) &&
                        double.TryParse(args[3], NumberStyles.Float, CultureInfo.InvariantCulture, out double m22) &&
                        double.TryParse(args[4], NumberStyles.Float, CultureInfo.InvariantCulture, out double offsetX) &&
                        double.TryParse(args[5], NumberStyles.Float, CultureInfo.InvariantCulture, out double offsetY))
                    {
                        m = new Matrix(m11, m12, m21, m22, offsetX, offsetY);
                    }
                    break;
            }

            return m.IsIdentity ? null : new MatrixTransform(m);
        }

        private static double ParseUnit(string unitStr)
        {
            if (string.IsNullOrWhiteSpace(unitStr)) return 0.0;
            unitStr = unitStr.Trim();
            if (unitStr.EndsWith("px", StringComparison.OrdinalIgnoreCase))
            {
                unitStr = unitStr.Substring(0, unitStr.Length - 2).Trim();
            }
            return double.TryParse(unitStr, NumberStyles.Float, CultureInfo.InvariantCulture, out double val) ? val : 0.0;
        }

        private static double ParsePercentage(string percentStr)
        {
            if (string.IsNullOrWhiteSpace(percentStr)) return 0.0;
            percentStr = percentStr.Trim();
            if (percentStr.EndsWith("%"))
            {
                string num = percentStr.Substring(0, percentStr.Length - 1).Trim();
                if (double.TryParse(num, NumberStyles.Float, CultureInfo.InvariantCulture, out double val))
                {
                    return val / 100.0;
                }
            }
            return ParseUnit(percentStr);
        }
    }
}

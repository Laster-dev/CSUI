// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace System.Windows.Ink
{
    public enum StylusTip
    {
        Rectangle = 0,
        Ellipse = 1
    }

    public delegate void PropertyDataChangedEventHandler(object sender, PropertyDataChangedEventArgs e);
    public delegate void DrawingAttributesReplacedEventHandler(object sender, DrawingAttributesReplacedEventArgs e);
    public delegate void StylusPointsReplacedEventHandler(object sender, StylusPointsReplacedEventArgs e);
    public delegate void StrokeCollectionChangedEventHandler(object sender, StrokeCollectionChangedEventArgs e);
    public delegate void StrokeHitEventHandler(object sender, StrokeHitEventArgs e);
    public delegate void LassoSelectionChangedEventHandler(object sender, LassoSelectionChangedEventArgs e);

    public class PropertyDataChangedEventArgs : EventArgs
    {
        public PropertyDataChangedEventArgs(Guid propertyGuid, object newValue, object previousValue)
        {
            PropertyGuid = propertyGuid;
            NewValue = newValue;
            PreviousValue = previousValue;
        }

        public Guid PropertyGuid { get; }
        public object NewValue { get; }
        public object PreviousValue { get; }
    }

    public class DrawingAttributesReplacedEventArgs : EventArgs
    {
        public DrawingAttributesReplacedEventArgs(DrawingAttributes newDrawingAttributes, DrawingAttributes previousDrawingAttributes)
        {
            ArgumentNullException.ThrowIfNull(newDrawingAttributes);
            ArgumentNullException.ThrowIfNull(previousDrawingAttributes);
            NewDrawingAttributes = newDrawingAttributes;
            PreviousDrawingAttributes = previousDrawingAttributes;
        }

        public DrawingAttributes NewDrawingAttributes { get; }
        public DrawingAttributes PreviousDrawingAttributes { get; }
    }

    public class StylusPointsReplacedEventArgs : EventArgs
    {
        public StylusPointsReplacedEventArgs(StylusPointCollection newStylusPoints, StylusPointCollection previousStylusPoints)
        {
            ArgumentNullException.ThrowIfNull(newStylusPoints);
            ArgumentNullException.ThrowIfNull(previousStylusPoints);
            NewStylusPoints = newStylusPoints;
            PreviousStylusPoints = previousStylusPoints;
        }

        public StylusPointCollection NewStylusPoints { get; }
        public StylusPointCollection PreviousStylusPoints { get; }
    }

    public class DrawingAttributes : INotifyPropertyChanged
    {
        public static readonly double MinWidth = 0.003779527559055118;
        public static readonly double MaxWidth = 1623232.0;
        public static readonly double MinHeight = 0.003779527559055118;
        public static readonly double MaxHeight = 1623232.0;

        private static readonly Guid ColorGuid = new Guid("61B5B9C5-961B-4867-8F59-83878C1C6492");
        private static readonly Guid WidthGuid = new Guid("58E055A2-AD78-40C5-9E44-16483065649B");
        private static readonly Guid HeightGuid = new Guid("7F3E2852-7D8E-4B1E-8C26-66202348E02B");
        private static readonly Guid StylusTipGuid = new Guid("B271F9FA-7F8B-40CE-81D2-DCAD1F9AA379");
        private static readonly Guid FitToCurveGuid = new Guid("383C418A-79D8-4605-BB4F-C1E6D2E3E113");
        private static readonly Guid IgnorePressureGuid = new Guid("4A66DB6D-4FD5-4C12-90D2-2C9C9EE2DF25");
        private static readonly Guid IsHighlighterGuid = new Guid("522EEEC5-EC09-4C46-ABFE-5C4AE1B04628");
        private static readonly Guid StylusTipTransformGuid = new Guid("D6B7D4FE-19DE-44F2-B42F-2B26B8A39453");

        private Color _color = Colors.Black;
        private StylusTip _stylusTip = StylusTip.Ellipse;
        private double _width = 2.0031496062992127;
        private double _height = 2.0031496062992127;
        private bool _fitToCurve;
        private bool _ignorePressure;
        private bool _isHighlighter;
        private Matrix _stylusTipTransform = Matrix.Identity;
        private Dictionary<Guid, object> _customProperties;

        public event PropertyDataChangedEventHandler AttributeChanged;
        public event PropertyDataChangedEventHandler PropertyDataChanged;
        public event PropertyChangedEventHandler PropertyChanged;

        public DrawingAttributes()
        {
        }

        public Color Color
        {
            get => _color;
            set
            {
                if (_color != value)
                {
                    Color prev = _color;
                    _color = value;
                    OnAttributeChanged(ColorGuid, value, prev, nameof(Color));
                }
            }
        }

        public StylusTip StylusTip
        {
            get => _stylusTip;
            set
            {
                if (_stylusTip != value)
                {
                    StylusTip prev = _stylusTip;
                    _stylusTip = value;
                    OnAttributeChanged(StylusTipGuid, value, prev, nameof(StylusTip));
                }
            }
        }

        public Matrix StylusTipTransform
        {
            get => _stylusTipTransform;
            set
            {
                if (_stylusTipTransform != value)
                {
                    Matrix prev = _stylusTipTransform;
                    _stylusTipTransform = value;
                    OnAttributeChanged(StylusTipTransformGuid, value, prev, nameof(StylusTipTransform));
                }
            }
        }

        public double Width
        {
            get => _width;
            set
            {
                if (double.IsNaN(value) || value < MinWidth || value > MaxWidth)
                {
                    throw new ArgumentOutOfRangeException(nameof(Width));
                }

                if (_width != value)
                {
                    double prev = _width;
                    _width = value;
                    OnAttributeChanged(WidthGuid, value, prev, nameof(Width));
                }
            }
        }

        public double Height
        {
            get => _height;
            set
            {
                if (double.IsNaN(value) || value < MinHeight || value > MaxHeight)
                {
                    throw new ArgumentOutOfRangeException(nameof(Height));
                }

                if (_height != value)
                {
                    double prev = _height;
                    _height = value;
                    OnAttributeChanged(HeightGuid, value, prev, nameof(Height));
                }
            }
        }

        public bool FitToCurve
        {
            get => _fitToCurve;
            set
            {
                if (_fitToCurve != value)
                {
                    bool prev = _fitToCurve;
                    _fitToCurve = value;
                    OnAttributeChanged(FitToCurveGuid, value, prev, nameof(FitToCurve));
                }
            }
        }

        public bool IgnorePressure
        {
            get => _ignorePressure;
            set
            {
                if (_ignorePressure != value)
                {
                    bool prev = _ignorePressure;
                    _ignorePressure = value;
                    OnAttributeChanged(IgnorePressureGuid, value, prev, nameof(IgnorePressure));
                }
            }
        }

        public bool IsHighlighter
        {
            get => _isHighlighter;
            set
            {
                if (_isHighlighter != value)
                {
                    bool prev = _isHighlighter;
                    _isHighlighter = value;
                    OnAttributeChanged(IsHighlighterGuid, value, prev, nameof(IsHighlighter));
                }
            }
        }

        public void AddPropertyData(Guid propertyDataId, object propertyData)
        {
            ArgumentNullException.ThrowIfNull(propertyData);
            _customProperties ??= new Dictionary<Guid, object>();
            _customProperties.TryGetValue(propertyDataId, out object prev);
            _customProperties[propertyDataId] = propertyData;
            PropertyDataChanged?.Invoke(this, new PropertyDataChangedEventArgs(propertyDataId, propertyData, prev));
        }

        public void RemovePropertyData(Guid propertyDataId)
        {
            if (_customProperties != null && _customProperties.TryGetValue(propertyDataId, out object prev))
            {
                _customProperties.Remove(propertyDataId);
                PropertyDataChanged?.Invoke(this, new PropertyDataChangedEventArgs(propertyDataId, null, prev));
            }
        }

        public object GetPropertyData(Guid propertyDataId)
        {
            if (_customProperties != null && _customProperties.TryGetValue(propertyDataId, out object val))
            {
                return val;
            }

            throw new ArgumentException("Property data not found.", nameof(propertyDataId));
        }

        public bool ContainsPropertyData(Guid propertyDataId) => _customProperties != null && _customProperties.ContainsKey(propertyDataId);

        public Guid[] GetPropertyDataIds()
        {
            if (_customProperties == null || _customProperties.Count == 0)
            {
                return Array.Empty<Guid>();
            }

            var keys = new Guid[_customProperties.Count];
            _customProperties.Keys.CopyTo(keys, 0);
            return keys;
        }

        public virtual DrawingAttributes Clone()
        {
            var copy = new DrawingAttributes
            {
                _color = _color,
                _stylusTip = _stylusTip,
                _width = _width,
                _height = _height,
                _fitToCurve = _fitToCurve,
                _ignorePressure = _ignorePressure,
                _isHighlighter = _isHighlighter,
                _stylusTipTransform = _stylusTipTransform
            };
            if (_customProperties != null)
            {
                copy._customProperties = new Dictionary<Guid, object>(_customProperties);
            }
            return copy;
        }

        protected virtual void OnAttributeChanged(PropertyDataChangedEventArgs e)
            => AttributeChanged?.Invoke(this, e);

        protected virtual void OnPropertyChanged(PropertyChangedEventArgs e)
            => PropertyChanged?.Invoke(this, e);

        private void OnAttributeChanged(Guid id, object newValue, object oldValue, string propertyName)
        {
            OnAttributeChanged(new PropertyDataChangedEventArgs(id, newValue, oldValue));
            OnPropertyChanged(new PropertyChangedEventArgs(propertyName));
        }
    }

    public class Stroke : INotifyPropertyChanged
    {
        private StylusPointCollection _stylusPoints;
        private DrawingAttributes _drawingAttributes;
        private Dictionary<Guid, object> _customProperties;

        public event DrawingAttributesReplacedEventHandler DrawingAttributesReplaced;
        public event PropertyDataChangedEventHandler DrawingAttributesChanged;
        public event StylusPointsReplacedEventHandler StylusPointsReplaced;
        public event EventHandler StylusPointsChanged;
        public event PropertyDataChangedEventHandler PropertyDataChanged;
        public event EventHandler Invalidated;
        public event PropertyChangedEventHandler PropertyChanged;

        public Stroke(StylusPointCollection stylusPoints)
            : this(stylusPoints, new DrawingAttributes())
        {
        }

        public Stroke(StylusPointCollection stylusPoints, DrawingAttributes drawingAttributes)
        {
            ArgumentNullException.ThrowIfNull(stylusPoints);
            ArgumentNullException.ThrowIfNull(drawingAttributes);
            if (stylusPoints.Count == 0)
            {
                throw new ArgumentException("StylusPointCollection cannot be empty.", nameof(stylusPoints));
            }

            _stylusPoints = stylusPoints;
            _stylusPoints.Changed += OnStylusPointsChangedEvent;
            _drawingAttributes = drawingAttributes;
            _drawingAttributes.AttributeChanged += OnDrawingAttributesChangedEvent;
        }

        public StylusPointCollection StylusPoints
        {
            get => _stylusPoints;
            set
            {
                ArgumentNullException.ThrowIfNull(value);
                if (value.Count == 0)
                {
                    throw new ArgumentException("StylusPointCollection cannot be empty.", nameof(value));
                }

                if (!ReferenceEquals(_stylusPoints, value))
                {
                    StylusPointCollection old = _stylusPoints;
                    old.Changed -= OnStylusPointsChangedEvent;
                    _stylusPoints = value;
                    _stylusPoints.Changed += OnStylusPointsChangedEvent;
                    OnStylusPointsReplaced(new StylusPointsReplacedEventArgs(value, old));
                    OnInvalidated(EventArgs.Empty);
                    OnPropertyChanged(new PropertyChangedEventArgs(nameof(StylusPoints)));
                }
            }
        }

        public DrawingAttributes DrawingAttributes
        {
            get => _drawingAttributes;
            set
            {
                ArgumentNullException.ThrowIfNull(value);
                if (!ReferenceEquals(_drawingAttributes, value))
                {
                    DrawingAttributes old = _drawingAttributes;
                    old.AttributeChanged -= OnDrawingAttributesChangedEvent;
                    _drawingAttributes = value;
                    _drawingAttributes.AttributeChanged += OnDrawingAttributesChangedEvent;
                    OnDrawingAttributesReplaced(new DrawingAttributesReplacedEventArgs(value, old));
                    OnInvalidated(EventArgs.Empty);
                    OnPropertyChanged(new PropertyChangedEventArgs(nameof(DrawingAttributes)));
                }
            }
        }

        public virtual Stroke Clone()
        {
            var points = new StylusPointCollection(_stylusPoints.Count);
            for (int i = 0; i < _stylusPoints.Count; i++)
            {
                points.Add(_stylusPoints[i]);
            }

            var copy = new Stroke(points, _drawingAttributes.Clone());
            if (_customProperties != null)
            {
                copy._customProperties = new Dictionary<Guid, object>(_customProperties);
            }
            return copy;
        }

        public void Draw(DrawingContext context)
        {
            ArgumentNullException.ThrowIfNull(context);
            DrawCore(context, _drawingAttributes);
        }

        public void Draw(DrawingContext drawingContext, DrawingAttributes drawingAttributes)
        {
            ArgumentNullException.ThrowIfNull(drawingContext);
            ArgumentNullException.ThrowIfNull(drawingAttributes);
            DrawCore(drawingContext, drawingAttributes);
        }

        protected virtual void DrawCore(DrawingContext drawingContext, DrawingAttributes drawingAttributes)
        {
            if (_stylusPoints.Count == 0)
            {
                return;
            }

            Color c = drawingAttributes.Color;
            if (drawingAttributes.IsHighlighter)
            {
                c = Color.FromArgb((byte)(c.A / 2), c.R, c.G, c.B);
            }

            var brush = new SolidColorBrush(c);
            double thickness = Math.Max(0.5, drawingAttributes.Width);

            if (_stylusPoints.Count == 1)
            {
                var p = (Point)_stylusPoints[0];
                drawingContext.DrawEllipse(brush, null, p, thickness * 0.5, drawingAttributes.Height * 0.5);
                return;
            }

            var pen = new Pen(brush, thickness)
            {
                StartLineCap = drawingAttributes.StylusTip == StylusTip.Rectangle ? PenLineCap.Flat : PenLineCap.Round,
                EndLineCap = drawingAttributes.StylusTip == StylusTip.Rectangle ? PenLineCap.Flat : PenLineCap.Round,
                LineJoin = PenLineJoin.Round
            };

            for (int i = 1; i < _stylusPoints.Count; i++)
            {
                drawingContext.DrawLine(pen, (Point)_stylusPoints[i - 1], (Point)_stylusPoints[i]);
            }
        }

        public virtual Rect GetBounds()
        {
            if (_stylusPoints.Count == 0)
            {
                return Rect.Empty;
            }

            double minX = double.PositiveInfinity;
            double minY = double.PositiveInfinity;
            double maxX = double.NegativeInfinity;
            double maxY = double.NegativeInfinity;

            for (int i = 0; i < _stylusPoints.Count; i++)
            {
                StylusPoint pt = _stylusPoints[i];
                if (pt.X < minX) minX = pt.X;
                if (pt.Y < minY) minY = pt.Y;
                if (pt.X > maxX) maxX = pt.X;
                if (pt.Y > maxY) maxY = pt.Y;
            }

            double padX = _drawingAttributes.Width * 0.5;
            double padY = _drawingAttributes.Height * 0.5;
            return new Rect(minX - padX, minY - padY, (maxX - minX) + padX * 2, (maxY - minY) + padY * 2);
        }

        public Geometry GetGeometry() => GetGeometry(_drawingAttributes);

        public Geometry GetGeometry(DrawingAttributes drawingAttributes)
        {
            var group = new GeometryGroup();
            for (int i = 1; i < _stylusPoints.Count; i++)
            {
                group.Children.Add(new LineGeometry((Point)_stylusPoints[i - 1], (Point)_stylusPoints[i]));
            }

            if (_stylusPoints.Count == 1)
            {
                group.Children.Add(new EllipseGeometry((Point)_stylusPoints[0], drawingAttributes.Width * 0.5, drawingAttributes.Height * 0.5));
            }
            return group;
        }

        public virtual void Transform(Matrix transformMatrix, bool applyToStylusTip)
        {
            for (int i = 0; i < _stylusPoints.Count; i++)
            {
                StylusPoint sp = _stylusPoints[i];
                Point p = transformMatrix.Transform(new Point(sp.X, sp.Y));
                sp.X = p.X;
                sp.Y = p.Y;
                _stylusPoints[i] = sp;
            }

            if (applyToStylusTip)
            {
                _drawingAttributes.StylusTipTransform *= transformMatrix;
            }

            OnInvalidated(EventArgs.Empty);
        }

        public bool HitTest(Point point) => HitTest(point, 1.0);

        public bool HitTest(Point point, double diameter)
        {
            double radius = Math.Max(0.5, (diameter + _drawingAttributes.Width) * 0.5);
            double rSq = radius * radius;

            if (_stylusPoints.Count == 1)
            {
                var p = (Point)_stylusPoints[0];
                return ((p.X - point.X) * (p.X - point.X) + (p.Y - point.Y) * (p.Y - point.Y)) <= rSq;
            }

            for (int i = 1; i < _stylusPoints.Count; i++)
            {
                if (DistanceSqToSegment(point, (Point)_stylusPoints[i - 1], (Point)_stylusPoints[i]) <= rSq)
                {
                    return true;
                }
            }

            return false;
        }

        public bool HitTest(Rect bounds, int percentageWithinBounds)
        {
            if (_stylusPoints.Count == 0) return false;
            int inside = 0;
            for (int i = 0; i < _stylusPoints.Count; i++)
            {
                if (bounds.Contains((Point)_stylusPoints[i]))
                {
                    inside++;
                }
            }

            return (inside * 100.0 / _stylusPoints.Count) >= percentageWithinBounds;
        }

        public bool HitTest(IEnumerable<Point> lassoPoints, int percentageWithinLasso)
        {
            var poly = new List<Point>(lassoPoints);
            if (poly.Count < 3 || _stylusPoints.Count == 0)
            {
                return false;
            }

            int inside = 0;
            for (int i = 0; i < _stylusPoints.Count; i++)
            {
                if (PointInPolygon((Point)_stylusPoints[i], poly))
                {
                    inside++;
                }
            }

            return (inside * 100.0 / _stylusPoints.Count) >= percentageWithinLasso;
        }

        public StrokeCollection GetEraseResult(Rect bounds)
        {
            var result = new StrokeCollection();
            if (!HitTest(bounds.TopLeft) && !GetBounds().IntersectsWith(bounds))
            {
                result.Add(Clone());
                return result;
            }

            var currentSegment = new StylusPointCollection();
            for (int i = 0; i < _stylusPoints.Count; i++)
            {
                if (!bounds.Contains((Point)_stylusPoints[i]))
                {
                    currentSegment.Add(_stylusPoints[i]);
                }
                else if (currentSegment.Count > 0)
                {
                    result.Add(new Stroke(currentSegment, _drawingAttributes.Clone()));
                    currentSegment = new StylusPointCollection();
                }
            }

            if (currentSegment.Count > 0)
            {
                result.Add(new Stroke(currentSegment, _drawingAttributes.Clone()));
            }

            return result;
        }

        public void AddPropertyData(Guid propertyDataId, object propertyData)
        {
            ArgumentNullException.ThrowIfNull(propertyData);
            _customProperties ??= new Dictionary<Guid, object>();
            _customProperties.TryGetValue(propertyDataId, out object prev);
            _customProperties[propertyDataId] = propertyData;
            PropertyDataChanged?.Invoke(this, new PropertyDataChangedEventArgs(propertyDataId, propertyData, prev));
        }

        public void RemovePropertyData(Guid propertyDataId)
        {
            if (_customProperties != null && _customProperties.TryGetValue(propertyDataId, out object prev))
            {
                _customProperties.Remove(propertyDataId);
                PropertyDataChanged?.Invoke(this, new PropertyDataChangedEventArgs(propertyDataId, null, prev));
            }
        }

        public object GetPropertyData(Guid propertyDataId)
        {
            if (_customProperties != null && _customProperties.TryGetValue(propertyDataId, out object val))
            {
                return val;
            }

            throw new ArgumentException("Property data not found.", nameof(propertyDataId));
        }

        public bool ContainsPropertyData(Guid propertyDataId) => _customProperties != null && _customProperties.ContainsKey(propertyDataId);

        public Guid[] GetPropertyDataIds()
        {
            if (_customProperties == null || _customProperties.Count == 0)
            {
                return Array.Empty<Guid>();
            }

            var keys = new Guid[_customProperties.Count];
            _customProperties.Keys.CopyTo(keys, 0);
            return keys;
        }

        internal static double DistanceSqToSegment(Point p, Point a, Point b)
        {
            double dx = b.X - a.X;
            double dy = b.Y - a.Y;
            double lenSq = dx * dx + dy * dy;
            if (lenSq <= 1e-12)
            {
                return (p.X - a.X) * (p.X - a.X) + (p.Y - a.Y) * (p.Y - a.Y);
            }

            double t = Math.Clamp(((p.X - a.X) * dx + (p.Y - a.Y) * dy) / lenSq, 0.0, 1.0);
            double projX = a.X + t * dx;
            double projY = a.Y + t * dy;
            return (p.X - projX) * (p.X - projX) + (p.Y - projY) * (p.Y - projY);
        }

        internal static bool PointInPolygon(Point pt, IList<Point> poly)
        {
            bool inside = false;
            for (int i = 0, j = poly.Count - 1; i < poly.Count; j = i++)
            {
                if (((poly[i].Y > pt.Y) != (poly[j].Y > pt.Y)) &&
                    (pt.X < (poly[j].X - poly[i].X) * (pt.Y - poly[i].Y) / ((poly[j].Y - poly[i].Y) == 0 ? 1e-12 : (poly[j].Y - poly[i].Y)) + poly[i].X))
                {
                    inside = !inside;
                }
            }
            return inside;
        }

        protected virtual void OnDrawingAttributesChanged(PropertyDataChangedEventArgs e)
            => DrawingAttributesChanged?.Invoke(this, e);

        protected virtual void OnDrawingAttributesReplaced(DrawingAttributesReplacedEventArgs e)
            => DrawingAttributesReplaced?.Invoke(this, e);

        protected virtual void OnStylusPointsReplaced(StylusPointsReplacedEventArgs e)
            => StylusPointsReplaced?.Invoke(this, e);

        protected virtual void OnStylusPointsChanged(EventArgs e)
            => StylusPointsChanged?.Invoke(this, e);

        protected virtual void OnInvalidated(EventArgs e)
            => Invalidated?.Invoke(this, e);

        protected virtual void OnPropertyChanged(PropertyChangedEventArgs e)
            => PropertyChanged?.Invoke(this, e);

        private void OnStylusPointsChangedEvent(object sender, EventArgs e)
        {
            OnStylusPointsChanged(e);
            OnInvalidated(e);
        }

        private void OnDrawingAttributesChangedEvent(object sender, PropertyDataChangedEventArgs e)
        {
            OnDrawingAttributesChanged(e);
            OnInvalidated(EventArgs.Empty);
        }
    }

    public class StrokeCollectionChangedEventArgs : EventArgs
    {
        public StrokeCollectionChangedEventArgs(StrokeCollection added, StrokeCollection removed)
        {
            Added = added ?? new StrokeCollection();
            Removed = removed ?? new StrokeCollection();
        }

        public StrokeCollection Added { get; }
        public StrokeCollection Removed { get; }
    }

    [TypeConverter(typeof(StrokeCollectionConverter))]
    public class StrokeCollection : Collection<Stroke>, INotifyPropertyChanged
    {
        public event StrokeCollectionChangedEventHandler StrokesChanged;
        public event PropertyChangedEventHandler PropertyChanged;

        public StrokeCollection()
        {
        }

        public StrokeCollection(IEnumerable<Stroke> strokes)
        {
            ArgumentNullException.ThrowIfNull(strokes);
            foreach (Stroke s in strokes)
            {
                Add(s);
            }
        }

        public virtual StrokeCollection Clone()
        {
            var copy = new StrokeCollection();
            for (int i = 0; i < Count; i++)
            {
                copy.Add(this[i].Clone());
            }
            return copy;
        }

        public void Add(StrokeCollection strokes)
        {
            ArgumentNullException.ThrowIfNull(strokes);
            foreach (Stroke s in strokes)
            {
                Add(s);
            }
        }

        public void Remove(StrokeCollection strokes)
        {
            ArgumentNullException.ThrowIfNull(strokes);
            foreach (Stroke s in strokes)
            {
                Remove(s);
            }
        }

        public void Replace(Stroke strokeToReplace, StrokeCollection strokesToReplaceWith)
        {
            int idx = IndexOf(strokeToReplace);
            if (idx < 0)
            {
                throw new ArgumentException("Stroke not found.", nameof(strokeToReplace));
            }

            RemoveAt(idx);
            if (strokesToReplaceWith != null)
            {
                for (int i = 0; i < strokesToReplaceWith.Count; i++)
                {
                    Insert(idx + i, strokesToReplaceWith[i]);
                }
            }
        }

        public Rect GetBounds()
        {
            Rect union = Rect.Empty;
            for (int i = 0; i < Count; i++)
            {
                union.Union(this[i].GetBounds());
            }
            return union;
        }

        public void Draw(DrawingContext context)
        {
            ArgumentNullException.ThrowIfNull(context);
            for (int i = 0; i < Count; i++)
            {
                this[i].Draw(context);
            }
        }

        public void Transform(Matrix transformMatrix, bool applyToStylusTip)
        {
            for (int i = 0; i < Count; i++)
            {
                this[i].Transform(transformMatrix, applyToStylusTip);
            }
        }

        public StrokeCollection HitTest(Point point) => HitTest(point, 1.0);

        public StrokeCollection HitTest(Point point, double diameter)
        {
            var hits = new StrokeCollection();
            for (int i = 0; i < Count; i++)
            {
                if (this[i].HitTest(point, diameter))
                {
                    hits.Add(this[i]);
                }
            }
            return hits;
        }

        public StrokeCollection HitTest(Rect bounds, int percentageWithinBounds)
        {
            var hits = new StrokeCollection();
            for (int i = 0; i < Count; i++)
            {
                if (this[i].HitTest(bounds, percentageWithinBounds))
                {
                    hits.Add(this[i]);
                }
            }
            return hits;
        }

        public StrokeCollection HitTest(IEnumerable<Point> lassoPoints, int percentageWithinLasso)
        {
            var poly = new List<Point>(lassoPoints);
            var hits = new StrokeCollection();
            for (int i = 0; i < Count; i++)
            {
                if (this[i].HitTest(poly, percentageWithinLasso))
                {
                    hits.Add(this[i]);
                }
            }
            return hits;
        }

        public void Erase(Rect bounds)
        {
            for (int i = Count - 1; i >= 0; i--)
            {
                Stroke stroke = this[i];
                if (stroke.GetBounds().IntersectsWith(bounds))
                {
                    StrokeCollection fragments = stroke.GetEraseResult(bounds);
                    RemoveAt(i);
                    for (int j = 0; j < fragments.Count; j++)
                    {
                        Insert(i + j, fragments[j]);
                    }
                }
            }
        }

        public IncrementalLassoHitTester GetIncrementalLassoHitTester(int percentageWithinLasso)
            => new IncrementalLassoHitTester(this, percentageWithinLasso);

        public IncrementalStrokeHitTester GetIncrementalStrokeHitTester(StylusShape eraserShape)
            => new IncrementalStrokeHitTester(this, eraserShape);

        public void Save(Stream stream) => Save(stream, true);

        public virtual void Save(Stream stream, bool compress)
        {
            ArgumentNullException.ThrowIfNull(stream);
            using var writer = new BinaryWriter(stream, System.Text.Encoding.UTF8, leaveOpen: true);
            writer.Write(0x49534631); // "ISF1" clean marker
            writer.Write(Count);
            for (int i = 0; i < Count; i++)
            {
                Stroke s = this[i];
                writer.Write(s.DrawingAttributes.Color.A);
                writer.Write(s.DrawingAttributes.Color.R);
                writer.Write(s.DrawingAttributes.Color.G);
                writer.Write(s.DrawingAttributes.Color.B);
                writer.Write(s.DrawingAttributes.Width);
                writer.Write(s.DrawingAttributes.Height);
                writer.Write(s.StylusPoints.Count);
                for (int j = 0; j < s.StylusPoints.Count; j++)
                {
                    writer.Write(s.StylusPoints[j].X);
                    writer.Write(s.StylusPoints[j].Y);
                    writer.Write(s.StylusPoints[j].PressureFactor);
                }
            }
        }

        private StrokeCollection(bool _)
        {
        }

        private static StrokeCollection CreateSingleSilent(Stroke stroke)
        {
            var c = new StrokeCollection(true);
            c.Items.Add(stroke);
            return c;
        }

        private StrokeCollection SnapshotSilent()
        {
            var c = new StrokeCollection(true);
            for (int i = 0; i < Count; i++)
            {
                c.Items.Add(this[i]);
            }
            return c;
        }

        protected override void InsertItem(int index, Stroke item)
        {
            ArgumentNullException.ThrowIfNull(item);
            base.InsertItem(index, item);
            OnStrokesChanged(new StrokeCollectionChangedEventArgs(CreateSingleSilent(item), new StrokeCollection(true)));
        }

        protected override void RemoveItem(int index)
        {
            Stroke item = this[index];
            base.RemoveItem(index);
            OnStrokesChanged(new StrokeCollectionChangedEventArgs(new StrokeCollection(true), CreateSingleSilent(item)));
        }

        protected override void SetItem(int index, Stroke item)
        {
            ArgumentNullException.ThrowIfNull(item);
            Stroke old = this[index];
            base.SetItem(index, item);
            OnStrokesChanged(new StrokeCollectionChangedEventArgs(CreateSingleSilent(item), CreateSingleSilent(old)));
        }

        protected override void ClearItems()
        {
            if (Count == 0) return;
            StrokeCollection removed = SnapshotSilent();
            base.ClearItems();
            OnStrokesChanged(new StrokeCollectionChangedEventArgs(new StrokeCollection(true), removed));
        }

        protected virtual void OnStrokesChanged(StrokeCollectionChangedEventArgs e)
        {
            StrokesChanged?.Invoke(this, e);
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Count)));
        }
    }

    public class StrokeCollectionConverter : TypeConverter
    {
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
            => sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);

        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
            => destinationType == typeof(string) || base.CanConvertTo(context, destinationType);

        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            if (value is string s && string.IsNullOrWhiteSpace(s))
            {
                return new StrokeCollection();
            }
            return base.ConvertFrom(context, culture, value);
        }

        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
        {
            if (destinationType == typeof(string) && value is StrokeCollection strokes)
            {
                using var ms = new MemoryStream();
                strokes.Save(ms);
                return Convert.ToBase64String(ms.ToArray());
            }
            return base.ConvertTo(context, culture, value, destinationType);
        }
    }

    public abstract class StylusShape
    {
        protected StylusShape(double width, double height, double rotation)
        {
            Width = width;
            Height = height;
            Rotation = rotation;
        }

        public double Width { get; }
        public double Height { get; }
        public double Rotation { get; }
    }

    public sealed class EllipseStylusShape : StylusShape
    {
        public EllipseStylusShape(double width, double height) : base(width, height, 0) { }
        public EllipseStylusShape(double width, double height, double rotation) : base(width, height, rotation) { }
    }

    public sealed class RectangleStylusShape : StylusShape
    {
        public RectangleStylusShape(double width, double height) : base(width, height, 0) { }
        public RectangleStylusShape(double width, double height, double rotation) : base(width, height, rotation) { }
    }

    public class StrokeHitEventArgs : EventArgs
    {
        private readonly Stroke _hitStroke;
        private readonly StrokeCollection _eraseResult;

        internal StrokeHitEventArgs(Stroke hitStroke, StrokeCollection eraseResult)
        {
            _hitStroke = hitStroke;
            _eraseResult = eraseResult ?? new StrokeCollection();
        }

        public Stroke HitStroke => _hitStroke;
        public StrokeCollection GetPointEraseResults() => _eraseResult;
    }

    public class LassoSelectionChangedEventArgs : EventArgs
    {
        internal LassoSelectionChangedEventArgs(StrokeCollection selected, StrokeCollection deselected)
        {
            SelectedStrokes = selected ?? new StrokeCollection();
            DeselectedStrokes = deselected ?? new StrokeCollection();
        }

        public StrokeCollection SelectedStrokes { get; }
        public StrokeCollection DeselectedStrokes { get; }
    }

    public abstract class IncrementalHitTester
    {
        private bool _isValid = true;

        public bool IsValid => _isValid;

        public void EndHitTesting() => _isValid = false;

        public void AddPoint(Point point) => AddPointsCore(new[] { point });

        public void AddPoints(IEnumerable<Point> points)
        {
            ArgumentNullException.ThrowIfNull(points);
            AddPointsCore(points);
        }

        protected abstract void AddPointsCore(IEnumerable<Point> points);
    }

    public class IncrementalLassoHitTester : IncrementalHitTester
    {
        private readonly StrokeCollection _strokes;
        private readonly int _percentageWithinLasso;
        private readonly List<Point> _lasso = new List<Point>();

        public event LassoSelectionChangedEventHandler SelectionChanged;

        internal IncrementalLassoHitTester(StrokeCollection strokes, int percentageWithinLasso)
        {
            _strokes = strokes;
            _percentageWithinLasso = percentageWithinLasso;
        }

        protected override void AddPointsCore(IEnumerable<Point> points)
        {
            _lasso.AddRange(points);
            if (_lasso.Count >= 3)
            {
                StrokeCollection selected = _strokes.HitTest(_lasso, _percentageWithinLasso);
                SelectionChanged?.Invoke(this, new LassoSelectionChangedEventArgs(selected, null));
            }
        }
    }

    public class IncrementalStrokeHitTester : IncrementalHitTester
    {
        private readonly StrokeCollection _strokes;
        private readonly StylusShape _shape;

        public event StrokeHitEventHandler StrokeHit;

        internal IncrementalStrokeHitTester(StrokeCollection strokes, StylusShape shape)
        {
            _strokes = strokes;
            _shape = shape;
        }

        protected override void AddPointsCore(IEnumerable<Point> points)
        {
            double diameter = _shape != null ? Math.Max(_shape.Width, _shape.Height) : 4.0;
            foreach (Point pt in points)
            {
                for (int i = 0; i < _strokes.Count; i++)
                {
                    Stroke s = _strokes[i];
                    if (s.HitTest(pt, diameter))
                    {
                        var bounds = new Rect(pt.X - diameter * 0.5, pt.Y - diameter * 0.5, diameter, diameter);
                        StrokeHit?.Invoke(this, new StrokeHitEventArgs(s, s.GetEraseResult(bounds)));
                    }
                }
            }
        }
    }

    public enum ApplicationGesture
    {
        AllGestures = 0,
        NoGesture = 0xF000,
        ScratchOut = 0xF001,
        Triangle = 0xF002,
        Square = 0xF003,
        Star = 0xF004,
        Check = 0xF005,
        Curlicue = 0xF010,
        DoubleCurlicue = 0xF011,
        Circle = 0xF020,
        DoubleCircle = 0xF021,
        SemicircleLeft = 0xF028,
        SemicircleRight = 0xF029,
        ChevronUp = 0xF030,
        ChevronDown = 0xF031,
        ChevronLeft = 0xF032,
        ChevronRight = 0xF033,
        ArrowUp = 0xF038,
        ArrowDown = 0xF039,
        ArrowLeft = 0xF03A,
        ArrowRight = 0xF03B,
        Up = 0xF058,
        Down = 0xF059,
        Left = 0xF05A,
        Right = 0xF05B,
        UpDown = 0xF060,
        DownUp = 0xF061,
        LeftRight = 0xF062,
        RightLeft = 0xF063,
        UpLeftLong = 0xF064,
        UpRightLong = 0xF065,
        DownLeftLong = 0xF066,
        DownRightLong = 0xF067,
        UpLeft = 0xF068,
        UpRight = 0xF069,
        DownLeft = 0xF06A,
        DownRight = 0xF06B,
        LeftUp = 0xF06C,
        LeftDown = 0xF06D,
        RightUp = 0xF06E,
        RightDown = 0xF06F,
        Exclamation = 0xF0A4,
        Tap = 0xF0F0,
        DoubleTap = 0xF0F1
    }

    public enum RecognitionConfidence
    {
        Strong,
        Intermediate,
        Poor
    }

    public class GestureRecognitionResult
    {
        internal GestureRecognitionResult(RecognitionConfidence confidence, ApplicationGesture gesture)
        {
            RecognitionConfidence = confidence;
            ApplicationGesture = gesture;
        }

        public RecognitionConfidence RecognitionConfidence { get; }
        public ApplicationGesture ApplicationGesture { get; }
    }

    /// <summary>
    /// Pure managed geometric gesture recognizer (zero dependency on legacy mshwgst.dll COM recognizer).
    /// </summary>
    public class GestureRecognizer : DependencyObject, IDisposable
    {
        private List<ApplicationGesture> _enabledGestures;

        public GestureRecognizer()
        {
        }

        public GestureRecognizer(IEnumerable<ApplicationGesture> enabledApplicationGestures)
        {
            SetEnabledGestures(enabledApplicationGestures);
        }

        public bool IsRecognizerAvailable => true;

        public void SetEnabledGestures(IEnumerable<ApplicationGesture> applicationGestures)
        {
            ArgumentNullException.ThrowIfNull(applicationGestures);
            _enabledGestures = new List<ApplicationGesture>(applicationGestures);
        }

        public ReadOnlyCollection<ApplicationGesture> GetEnabledGestures()
        {
            return new ReadOnlyCollection<ApplicationGesture>(_enabledGestures ?? new List<ApplicationGesture> { ApplicationGesture.AllGestures });
        }

        public ReadOnlyCollection<GestureRecognitionResult> Recognize(StrokeCollection strokes)
        {
            ArgumentNullException.ThrowIfNull(strokes);
            var results = new List<GestureRecognitionResult>();
            if (strokes.Count == 0)
            {
                results.Add(new GestureRecognitionResult(RecognitionConfidence.Poor, ApplicationGesture.NoGesture));
                return results.AsReadOnly();
            }

            Stroke s = strokes[0];
            Rect bounds = s.GetBounds();
            if (bounds.Width < 6 && bounds.Height < 6)
            {
                results.Add(new GestureRecognitionResult(RecognitionConfidence.Strong, ApplicationGesture.Tap));
            }
            else if (s.StylusPoints.Count >= 2)
            {
                Point first = (Point)s.StylusPoints[0];
                Point last = (Point)s.StylusPoints[s.StylusPoints.Count - 1];
                double dx = last.X - first.X;
                double dy = last.Y - first.Y;
                ApplicationGesture g = Math.Abs(dx) >= Math.Abs(dy)
                    ? (dx >= 0 ? ApplicationGesture.Right : ApplicationGesture.Left)
                    : (dy >= 0 ? ApplicationGesture.Down : ApplicationGesture.Up);
                results.Add(new GestureRecognitionResult(RecognitionConfidence.Intermediate, g));
            }
            else
            {
                results.Add(new GestureRecognitionResult(RecognitionConfidence.Poor, ApplicationGesture.NoGesture));
            }

            return results.AsReadOnly();
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Automation.Peers;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;

namespace System.Windows.Controls
{
    public enum InkCanvasEditingMode
    {
        None = 0,
        Ink,
        GestureOnly,
        InkAndGesture,
        Select,
        EraseByPoint,
        EraseByStroke
    }

    public enum InkCanvasClipboardFormat
    {
        InkSerializedFormat = 0,
        TextInk,
        Xaml
    }

    public enum InkCanvasSelectionHitResult
    {
        None = 0,
        TopLeft,
        Top,
        TopRight,
        Right,
        BottomRight,
        Bottom,
        BottomLeft,
        Left,
        Selection
    }

    public delegate void InkCanvasStrokeCollectedEventHandler(object sender, InkCanvasStrokeCollectedEventArgs e);
    public delegate void InkCanvasStrokeErasingEventHandler(object sender, InkCanvasStrokeErasingEventArgs e);
    public delegate void InkCanvasSelectionChangingEventHandler(object sender, InkCanvasSelectionChangingEventArgs e);
    public delegate void InkCanvasSelectionEditingEventHandler(object sender, InkCanvasSelectionEditingEventArgs e);
    public delegate void InkCanvasGestureEventHandler(object sender, InkCanvasGestureEventArgs e);

    public class InkCanvasStrokeCollectedEventArgs : RoutedEventArgs
    {
        public InkCanvasStrokeCollectedEventArgs(Stroke stroke)
            : base(InkCanvas.StrokeCollectedEvent)
        {
            ArgumentNullException.ThrowIfNull(stroke);
            Stroke = stroke;
        }

        public Stroke Stroke { get; }
    }

    public class InkCanvasStrokeErasingEventArgs : CancelEventArgs
    {
        internal InkCanvasStrokeErasingEventArgs(Stroke stroke)
        {
            ArgumentNullException.ThrowIfNull(stroke);
            Stroke = stroke;
        }

        public Stroke Stroke { get; }
    }

    public class InkCanvasSelectionChangingEventArgs : CancelEventArgs
    {
        internal InkCanvasSelectionChangingEventArgs(StrokeCollection selectedStrokes, IList<UIElement> selectedElements)
        {
            _selectedStrokes = selectedStrokes ?? new StrokeCollection();
            _selectedElements = new List<UIElement>(selectedElements ?? Array.Empty<UIElement>());
        }

        private StrokeCollection _selectedStrokes;
        private IList<UIElement> _selectedElements;

        public void SetSelectedStrokes(StrokeCollection selectedStrokes)
        {
            ArgumentNullException.ThrowIfNull(selectedStrokes);
            _selectedStrokes = selectedStrokes;
        }

        public StrokeCollection GetSelectedStrokes() => _selectedStrokes.Clone();

        public void SetSelectedElements(IEnumerable<UIElement> selectedElements)
        {
            ArgumentNullException.ThrowIfNull(selectedElements);
            _selectedElements = new List<UIElement>(selectedElements);
        }

        public ReadOnlyCollection<UIElement> GetSelectedElements() => new ReadOnlyCollection<UIElement>(_selectedElements);
    }

    public class InkCanvasSelectionEditingEventArgs : CancelEventArgs
    {
        internal InkCanvasSelectionEditingEventArgs(Rect oldRectangle, Rect newRectangle)
        {
            OldRectangle = oldRectangle;
            NewRectangle = newRectangle;
        }

        public Rect OldRectangle { get; }
        public Rect NewRectangle { get; set; }
    }

    public class InkCanvasGestureEventArgs : RoutedEventArgs
    {
        private readonly List<GestureRecognitionResult> _results;

        public InkCanvasGestureEventArgs(StrokeCollection strokes, IEnumerable<GestureRecognitionResult> gestureRecognitionResults)
            : base(InkCanvas.GestureEvent)
        {
            ArgumentNullException.ThrowIfNull(strokes);
            ArgumentNullException.ThrowIfNull(gestureRecognitionResults);
            Strokes = strokes;
            _results = new List<GestureRecognitionResult>(gestureRecognitionResults);
        }

        public StrokeCollection Strokes { get; }
        public bool Cancel { get; set; }

        public ReadOnlyCollection<GestureRecognitionResult> GetGestureRecognitionResults() => _results.AsReadOnly();
    }

    [ContentProperty(nameof(Children))]
    public class InkCanvas : FrameworkElement, IAddChild
    {
        public static readonly RoutedUICommand DeselectCommand = new RoutedUICommand("Deselect", "Deselect", typeof(InkCanvas));

        public static readonly DependencyProperty BackgroundProperty =
            Panel.BackgroundProperty.AddOwner(typeof(InkCanvas), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty TopProperty =
            DependencyProperty.RegisterAttached("Top", typeof(double), typeof(InkCanvas), new FrameworkPropertyMetadata(double.NaN));

        public static readonly DependencyProperty LeftProperty =
            DependencyProperty.RegisterAttached("Left", typeof(double), typeof(InkCanvas), new FrameworkPropertyMetadata(double.NaN));

        public static readonly DependencyProperty BottomProperty =
            DependencyProperty.RegisterAttached("Bottom", typeof(double), typeof(InkCanvas), new FrameworkPropertyMetadata(double.NaN));

        public static readonly DependencyProperty RightProperty =
            DependencyProperty.RegisterAttached("Right", typeof(double), typeof(InkCanvas), new FrameworkPropertyMetadata(double.NaN));

        public static readonly DependencyProperty StrokesProperty =
            InkPresenter.StrokesProperty.AddOwner(typeof(InkCanvas), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender, OnStrokesChanged));

        public static readonly DependencyProperty EditingModeProperty =
            DependencyProperty.Register(nameof(EditingMode), typeof(InkCanvasEditingMode), typeof(InkCanvas),
                new FrameworkPropertyMetadata(InkCanvasEditingMode.Ink, OnEditingModeChanged));

        public static readonly DependencyProperty EditingModeInvertedProperty =
            DependencyProperty.Register(nameof(EditingModeInverted), typeof(InkCanvasEditingMode), typeof(InkCanvas),
                new FrameworkPropertyMetadata(InkCanvasEditingMode.EraseByStroke));

        private static readonly DependencyPropertyKey ActiveEditingModePropertyKey =
            DependencyProperty.RegisterReadOnly(nameof(ActiveEditingMode), typeof(InkCanvasEditingMode), typeof(InkCanvas),
                new FrameworkPropertyMetadata(InkCanvasEditingMode.Ink));

        public static readonly DependencyProperty ActiveEditingModeProperty = ActiveEditingModePropertyKey.DependencyProperty;

        public static readonly DependencyProperty UseCustomCursorProperty =
            DependencyProperty.Register(nameof(UseCustomCursor), typeof(bool), typeof(InkCanvas), new FrameworkPropertyMetadata(false));

        public static readonly DependencyProperty MoveEnabledProperty =
            DependencyProperty.Register(nameof(MoveEnabled), typeof(bool), typeof(InkCanvas), new FrameworkPropertyMetadata(true));

        public static readonly DependencyProperty ResizeEnabledProperty =
            DependencyProperty.Register(nameof(ResizeEnabled), typeof(bool), typeof(InkCanvas), new FrameworkPropertyMetadata(true));

        public static readonly DependencyProperty DefaultDrawingAttributesProperty =
            DependencyProperty.Register(nameof(DefaultDrawingAttributes), typeof(DrawingAttributes), typeof(InkCanvas),
                new FrameworkPropertyMetadata(null, OnDefaultDrawingAttributesChanged));

        public static readonly DependencyProperty EraserShapeProperty =
            DependencyProperty.Register(nameof(EraserShape), typeof(StylusShape), typeof(InkCanvas),
                new FrameworkPropertyMetadata(null));

        public static readonly RoutedEvent StrokeCollectedEvent =
            EventManager.RegisterRoutedEvent<InkCanvasStrokeCollectedEventHandler>(
                nameof(StrokeCollected),
                RoutingStrategy.Bubble,
                typeof(InkCanvas),
                static (handler, sender, args) => handler(sender, (InkCanvasStrokeCollectedEventArgs)args));

        public static readonly RoutedEvent GestureEvent =
            EventManager.RegisterRoutedEvent<InkCanvasGestureEventHandler>(
                nameof(Gesture),
                RoutingStrategy.Bubble,
                typeof(InkCanvas),
                static (handler, sender, args) => handler(sender, (InkCanvasGestureEventArgs)args));

        public static readonly RoutedEvent ActiveEditingModeChangedEvent =
            EventManager.RegisterRoutedEvent(nameof(ActiveEditingModeChanged), RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(InkCanvas));

        public static readonly RoutedEvent EditingModeChangedEvent =
            EventManager.RegisterRoutedEvent(nameof(EditingModeChanged), RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(InkCanvas));

        public static readonly RoutedEvent EditingModeInvertedChangedEvent =
            EventManager.RegisterRoutedEvent("EditingModeInvertedChanged", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(InkCanvas));

        private readonly InkPresenter _inkPresenter;
        private readonly Canvas _childrenCanvas;
        private readonly StrokeCollection _selectedStrokes = new StrokeCollection();
        private readonly List<UIElement> _selectedElements = new List<UIElement>();
        private StylusPointCollection _activeStrokePoints;
        private bool _isDrawing;

        static InkCanvas()
        {
            ClipToBoundsProperty.OverrideMetadata(typeof(InkCanvas), new FrameworkPropertyMetadata(true));
            FocusableProperty.OverrideMetadata(typeof(InkCanvas), new FrameworkPropertyMetadata(true));
        }

        public InkCanvas()
        {
            _childrenCanvas = new Canvas();
            _inkPresenter = new InkPresenter { Child = _childrenCanvas };
            AddVisualChild(_inkPresenter);
            AddLogicalChild(_inkPresenter);

            DefaultDrawingAttributes = new DrawingAttributes();
            EraserShape = new RectangleStylusShape(8, 8);
            Strokes = new StrokeCollection();
        }

        public static double GetTop(UIElement element) => (double)element.GetValue(TopProperty);
        public static void SetTop(UIElement element, double length) => element.SetValue(TopProperty, length);
        public static double GetLeft(UIElement element) => (double)element.GetValue(LeftProperty);
        public static void SetLeft(UIElement element, double length) => element.SetValue(LeftProperty, length);
        public static double GetBottom(UIElement element) => (double)element.GetValue(BottomProperty);
        public static void SetBottom(UIElement element, double length) => element.SetValue(BottomProperty, length);
        public static double GetRight(UIElement element) => (double)element.GetValue(RightProperty);
        public static void SetRight(UIElement element, double length) => element.SetValue(RightProperty, length);

        public Brush Background
        {
            get => (Brush)GetValue(BackgroundProperty);
            set => SetValue(BackgroundProperty, value);
        }

        public UIElementCollection Children => _childrenCanvas.Children;

        public StrokeCollection Strokes
        {
            get => (StrokeCollection)GetValue(StrokesProperty);
            set => SetValue(StrokesProperty, value);
        }

        public InkCanvasEditingMode EditingMode
        {
            get => (InkCanvasEditingMode)GetValue(EditingModeProperty);
            set => SetValue(EditingModeProperty, value);
        }

        public InkCanvasEditingMode EditingModeInverted
        {
            get => (InkCanvasEditingMode)GetValue(EditingModeInvertedProperty);
            set => SetValue(EditingModeInvertedProperty, value);
        }

        public InkCanvasEditingMode ActiveEditingMode => (InkCanvasEditingMode)GetValue(ActiveEditingModeProperty);

        public bool UseCustomCursor
        {
            get => (bool)GetValue(UseCustomCursorProperty);
            set => SetValue(UseCustomCursorProperty, value);
        }

        public bool MoveEnabled
        {
            get => (bool)GetValue(MoveEnabledProperty);
            set => SetValue(MoveEnabledProperty, value);
        }

        public bool ResizeEnabled
        {
            get => (bool)GetValue(ResizeEnabledProperty);
            set => SetValue(ResizeEnabledProperty, value);
        }

        public DrawingAttributes DefaultDrawingAttributes
        {
            get => (DrawingAttributes)GetValue(DefaultDrawingAttributesProperty);
            set => SetValue(DefaultDrawingAttributesProperty, value);
        }

        public StylusShape EraserShape
        {
            get => (StylusShape)GetValue(EraserShapeProperty);
            set => SetValue(EraserShapeProperty, value);
        }

        protected InkPresenter InkPresenter => _inkPresenter;

        public event InkCanvasStrokeCollectedEventHandler StrokeCollected
        {
            add => AddHandler(StrokeCollectedEvent, value);
            remove => RemoveHandler(StrokeCollectedEvent, value);
        }

        public event InkCanvasStrokeErasingEventHandler StrokeErasing;

        public event InkCanvasGestureEventHandler Gesture
        {
            add => AddHandler(GestureEvent, value);
            remove => RemoveHandler(GestureEvent, value);
        }

        public event RoutedEventHandler ActiveEditingModeChanged
        {
            add => AddHandler(ActiveEditingModeChangedEvent, value);
            remove => RemoveHandler(ActiveEditingModeChangedEvent, value);
        }

        public event RoutedEventHandler EditingModeChanged
        {
            add => AddHandler(EditingModeChangedEvent, value);
            remove => RemoveHandler(EditingModeChangedEvent, value);
        }

        public event RoutedEventHandler EditingModeInvertedChanged
        {
            add => AddHandler(EditingModeInvertedChangedEvent, value);
            remove => RemoveHandler(EditingModeInvertedChangedEvent, value);
        }

        public event StrokeCollectionChangedEventHandler StrokesReplaced;
        public event DrawingAttributesReplacedEventHandler DefaultDrawingAttributesReplaced;
        public event InkCanvasSelectionChangingEventHandler SelectionChanging;
        public event EventHandler SelectionChanged;
        public event InkCanvasSelectionEditingEventHandler SelectionMoving;
        public event EventHandler SelectionMoved;
        public event InkCanvasSelectionEditingEventHandler SelectionResizing;
        public event EventHandler SelectionResized;

        internal void RaiseSelectionMoveResize(Rect oldRect, Rect newRect, bool isResize)
        {
            var args = new InkCanvasSelectionEditingEventArgs(oldRect, newRect);
            if (isResize)
            {
                SelectionResizing?.Invoke(this, args);
                if (!args.Cancel) SelectionResized?.Invoke(this, EventArgs.Empty);
            }
            else
            {
                SelectionMoving?.Invoke(this, args);
                if (!args.Cancel) SelectionMoved?.Invoke(this, EventArgs.Empty);
            }
        }

        public StrokeCollection GetSelectedStrokes() => _selectedStrokes.Clone();

        public ReadOnlyCollection<UIElement> GetSelectedElements() => new ReadOnlyCollection<UIElement>(_selectedElements.ToArray());

        public void Select(StrokeCollection selectedStrokes) => Select(selectedStrokes, null);

        public void Select(IEnumerable<UIElement> selectedElements) => Select(null, selectedElements);

        public void Select(StrokeCollection selectedStrokes, IEnumerable<UIElement> selectedElements)
        {
            var args = new InkCanvasSelectionChangingEventArgs(
                selectedStrokes ?? new StrokeCollection(),
                selectedElements != null ? new List<UIElement>(selectedElements) : new List<UIElement>());
            SelectionChanging?.Invoke(this, args);
            if (args.Cancel) return;

            _selectedStrokes.Clear();
            _selectedStrokes.Add(args.GetSelectedStrokes());

            _selectedElements.Clear();
            _selectedElements.AddRange(args.GetSelectedElements());

            SelectionChanged?.Invoke(this, EventArgs.Empty);
            InvalidateVisual();
        }

        public Rect GetSelectionBounds()
        {
            Rect bounds = _selectedStrokes.GetBounds();
            foreach (UIElement el in _selectedElements)
            {
                double x = GetLeft(el);
                double y = GetTop(el);
                if (double.IsNaN(x)) x = 0;
                if (double.IsNaN(y)) y = 0;
                bounds.Union(new Rect(x, y, el.RenderSize.Width, el.RenderSize.Height));
            }
            return bounds;
        }

        public InkCanvasSelectionHitResult HitTestSelection(Point point)
        {
            Rect b = GetSelectionBounds();
            if (b.IsEmpty) return InkCanvasSelectionHitResult.None;
            return b.Contains(point) ? InkCanvasSelectionHitResult.Selection : InkCanvasSelectionHitResult.None;
        }

        public void CopySelection()
        {
        }

        public void CutSelection()
        {
            Strokes?.Remove(_selectedStrokes);
            _selectedStrokes.Clear();
            SelectionChanged?.Invoke(this, EventArgs.Empty);
        }

        public void Paste()
        {
        }

        void IAddChild.AddChild(object value)
        {
            ArgumentNullException.ThrowIfNull(value);
            if (value is UIElement ui)
            {
                Children.Add(ui);
            }
        }

        void IAddChild.AddText(string text)
        {
        }

        protected override int VisualChildrenCount => 1;

        protected override Visual GetVisualChild(int index)
        {
            if (index != 0) throw new ArgumentOutOfRangeException(nameof(index));
            return _inkPresenter;
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            _inkPresenter.Measure(availableSize);
            return _inkPresenter.DesiredSize;
        }

        protected override Size ArrangeOverride(Size arrangeSize)
        {
            _inkPresenter.Arrange(new Rect(new Point(0, 0), arrangeSize));
            return arrangeSize;
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            if (Background != null)
            {
                drawingContext.DrawRectangle(Background, null, new Rect(new Point(0, 0), RenderSize));
            }

            if (_activeStrokePoints != null && _activeStrokePoints.Count >= 2 && DefaultDrawingAttributes != null)
            {
                var tempStroke = new Stroke(_activeStrokePoints, DefaultDrawingAttributes);
                tempStroke.Draw(drawingContext);
            }
        }

        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonDown(e);
            Point pt = e.GetPosition(this);
            InkCanvasEditingMode mode = ActiveEditingMode;
            if (mode == InkCanvasEditingMode.Ink || mode == InkCanvasEditingMode.InkAndGesture || mode == InkCanvasEditingMode.GestureOnly)
            {
                _isDrawing = true;
                _activeStrokePoints = new StylusPointCollection();
                _activeStrokePoints.Add(new StylusPoint(pt.X, pt.Y));
                CaptureMouse();
                e.Handled = true;
            }
            else if (mode == InkCanvasEditingMode.EraseByStroke || mode == InkCanvasEditingMode.EraseByPoint)
            {
                EraseAtPoint(pt);
                e.Handled = true;
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (_isDrawing && _activeStrokePoints != null)
            {
                Point pt = e.GetPosition(this);
                _activeStrokePoints.Add(new StylusPoint(pt.X, pt.Y));
                InvalidateVisual();
            }
        }

        protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonUp(e);
            if (_isDrawing && _activeStrokePoints != null)
            {
                _isDrawing = false;
                ReleaseMouseCapture();
                var stroke = new Stroke(_activeStrokePoints, DefaultDrawingAttributes?.Clone() ?? new DrawingAttributes());
                _activeStrokePoints = null;

                if (ActiveEditingMode == InkCanvasEditingMode.GestureOnly)
                {
                    using var rec = new GestureRecognizer();
                    var strokes = new StrokeCollection { stroke };
                    RaiseEvent(new InkCanvasGestureEventArgs(strokes, rec.Recognize(strokes)));
                }
                else
                {
                    Strokes?.Add(stroke);
                    OnStrokeCollected(new InkCanvasStrokeCollectedEventArgs(stroke));
                }

                InvalidateVisual();
                e.Handled = true;
            }
        }

        private void EraseAtPoint(Point pt)
        {
            if (Strokes == null) return;
            double diameter = EraserShape != null ? Math.Max(EraserShape.Width, EraserShape.Height) : 8.0;
            StrokeCollection hits = Strokes.HitTest(pt, diameter);
            for (int i = 0; i < hits.Count; i++)
            {
                var erasingArgs = new InkCanvasStrokeErasingEventArgs(hits[i]);
                StrokeErasing?.Invoke(this, erasingArgs);
                if (!erasingArgs.Cancel)
                {
                    Strokes.Remove(hits[i]);
                }
            }
        }

        protected virtual void OnStrokeCollected(InkCanvasStrokeCollectedEventArgs e) => RaiseEvent(e);

        protected virtual void OnActiveEditingModeChanged(RoutedEventArgs e) => RaiseEvent(e);

        protected virtual void OnEditingModeChanged(RoutedEventArgs e) => RaiseEvent(e);

        protected override AutomationPeer OnCreateAutomationPeer() => new InkCanvasAutomationPeer(this);

        private static void OnStrokesChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is InkCanvas canvas)
            {
                canvas._inkPresenter.Strokes = e.NewValue as StrokeCollection;
                canvas.StrokesReplaced?.Invoke(canvas, new StrokeCollectionChangedEventArgs(
                    e.NewValue as StrokeCollection, e.OldValue as StrokeCollection));
            }
        }

        private static void OnEditingModeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is InkCanvas canvas)
            {
                canvas.SetValue(ActiveEditingModePropertyKey, (InkCanvasEditingMode)e.NewValue);
                canvas.OnEditingModeChanged(new RoutedEventArgs(EditingModeChangedEvent, canvas));
                canvas.OnActiveEditingModeChanged(new RoutedEventArgs(ActiveEditingModeChangedEvent, canvas));
            }
        }

        private static void OnDefaultDrawingAttributesChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is InkCanvas canvas && e.NewValue is DrawingAttributes newDa && e.OldValue is DrawingAttributes oldDa)
            {
                canvas.DefaultDrawingAttributesReplaced?.Invoke(canvas, new DrawingAttributesReplacedEventArgs(newDa, oldDa));
            }
        }
    }
}

using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace CSUI.SampleApp
{
    internal sealed class StackLayoutPanel : Panel
    {
        public Orientation Orientation { get; set; } = Orientation.Vertical;
        public double Spacing { get; set; } = 8.0;

        protected override Size MeasureOverride(Size availableSize)
        {
            double totalMain = 0;
            double maxCross = 0;
            int visibleCount = 0;

            for (int i = 0; i < InternalChildren.Count; i++)
            {
                UIElement child = InternalChildren[i];
                if (child == null || child.Visibility == Visibility.Collapsed)
                {
                    continue;
                }

                if (Orientation == Orientation.Vertical)
                {
                    child.Measure(new Size(availableSize.Width, double.PositiveInfinity));
                    totalMain += child.DesiredSize.Height;
                    maxCross = Math.Max(maxCross, child.DesiredSize.Width);
                }
                else
                {
                    child.Measure(new Size(double.PositiveInfinity, availableSize.Height));
                    totalMain += child.DesiredSize.Width;
                    maxCross = Math.Max(maxCross, child.DesiredSize.Height);
                }
                visibleCount++;
            }

            if (visibleCount > 1)
            {
                totalMain += (visibleCount - 1) * Spacing;
            }

            return Orientation == Orientation.Vertical
                ? new Size(maxCross, totalMain)
                : new Size(totalMain, maxCross);
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            double offset = 0;
            for (int i = 0; i < InternalChildren.Count; i++)
            {
                UIElement child = InternalChildren[i];
                if (child == null || child.Visibility == Visibility.Collapsed)
                {
                    continue;
                }

                if (Orientation == Orientation.Vertical)
                {
                    double h = child.DesiredSize.Height;
                    child.Arrange(new Rect(0, offset, finalSize.Width, h));
                    offset += h + Spacing;
                }
                else
                {
                    double w = child.DesiredSize.Width;
                    child.Arrange(new Rect(offset, 0, w, finalSize.Height));
                    offset += w + Spacing;
                }
            }
            return finalSize;
        }
    }

    internal sealed class CanvasPanel : Panel
    {
        public static readonly DependencyProperty LeftProperty =
            DependencyProperty.RegisterAttached(
                "Left",
                typeof(double),
                typeof(CanvasPanel),
                new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsParentArrange));

        public static readonly DependencyProperty TopProperty =
            DependencyProperty.RegisterAttached(
                "Top",
                typeof(double),
                typeof(CanvasPanel),
                new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsParentArrange));

        public static void SetPos(UIElement element, double left, double top)
        {
            element.SetValue(LeftProperty, left);
            element.SetValue(TopProperty, top);
        }

        protected override Size MeasureOverride(Size constraint)
        {
            Size childConstraint = new Size(double.PositiveInfinity, double.PositiveInfinity);
            for (int i = 0; i < InternalChildren.Count; i++)
            {
                InternalChildren[i]?.Measure(childConstraint);
            }
            return new Size(
                double.IsInfinity(constraint.Width) ? 0 : constraint.Width,
                double.IsInfinity(constraint.Height) ? 0 : constraint.Height);
        }

        protected override Size ArrangeOverride(Size arrangeSize)
        {
            for (int i = 0; i < InternalChildren.Count; i++)
            {
                UIElement child = InternalChildren[i];
                if (child == null)
                {
                    continue;
                }

                double x = (double)child.GetValue(LeftProperty);
                double y = (double)child.GetValue(TopProperty);
                child.Arrange(new Rect(new Point(x, y), child.DesiredSize));
            }
            return arrangeSize;
        }
    }

    internal sealed class CardDecorator : Decorator
    {
        public static readonly DependencyProperty BackgroundProperty =
            DependencyProperty.Register(
                nameof(Background),
                typeof(Brush),
                typeof(CardDecorator),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty BorderBrushProperty =
            DependencyProperty.Register(
                nameof(BorderBrush),
                typeof(Brush),
                typeof(CardDecorator),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty CornerRadiusProperty =
            DependencyProperty.Register(
                nameof(CornerRadius),
                typeof(double),
                typeof(CardDecorator),
                new FrameworkPropertyMetadata(10.0, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty PaddingProperty =
            DependencyProperty.Register(
                nameof(Padding),
                typeof(Thickness),
                typeof(CardDecorator),
                new FrameworkPropertyMetadata(new Thickness(14), FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsArrange));

        public Brush Background
        {
            get => (Brush)GetValue(BackgroundProperty);
            set => SetValue(BackgroundProperty, value);
        }

        public Brush BorderBrush
        {
            get => (Brush)GetValue(BorderBrushProperty);
            set => SetValue(BorderBrushProperty, value);
        }

        public double CornerRadius
        {
            get => (double)GetValue(CornerRadiusProperty);
            set => SetValue(CornerRadiusProperty, value);
        }

        public Thickness Padding
        {
            get => (Thickness)GetValue(PaddingProperty);
            set => SetValue(PaddingProperty, value);
        }

        protected override Size MeasureOverride(Size constraint)
        {
            Thickness pad = Padding;
            double padW = pad.Left + pad.Right;
            double padH = pad.Top + pad.Bottom;

            if (Child != null)
            {
                Size inner = new Size(
                    Math.Max(0, constraint.Width - padW),
                    Math.Max(0, constraint.Height - padH));
                Child.Measure(inner);
                return new Size(Child.DesiredSize.Width + padW, Child.DesiredSize.Height + padH);
            }
            return new Size(padW, padH);
        }

        protected override Size ArrangeOverride(Size arrangeSize)
        {
            if (Child != null)
            {
                Thickness pad = Padding;
                Rect childRect = new Rect(
                    pad.Left,
                    pad.Top,
                    Math.Max(0, arrangeSize.Width - pad.Left - pad.Right),
                    Math.Max(0, arrangeSize.Height - pad.Top - pad.Bottom));
                Child.Arrange(childRect);
            }
            return arrangeSize;
        }

        protected override void OnRender(DrawingContext dc)
        {
            Pen pen = BorderBrush != null ? new Pen(BorderBrush, 1.25) : null;
            double r = CornerRadius;
            dc.DrawRoundedRectangle(Background, pen, new Rect(0.5, 0.5, Math.Max(0, RenderSize.Width - 1), Math.Max(0, RenderSize.Height - 1)), r, r);
        }
    }

    internal sealed class DirectWriteLabel : FrameworkElement
    {
        private static readonly Typeface s_regularTypeface = new Typeface(new FontFamily("Segoe UI"), FontStyles.Normal, FontWeights.Normal, FontStretches.Normal);
        private static readonly Typeface s_boldTypeface = new Typeface(new FontFamily("Segoe UI"), FontStyles.Normal, FontWeights.SemiBold, FontStretches.Normal);

        public static readonly DependencyProperty TextProperty =
            DependencyProperty.Register(
                nameof(Text),
                typeof(string),
                typeof(DirectWriteLabel),
                new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty ForegroundProperty =
            DependencyProperty.Register(
                nameof(Foreground),
                typeof(Brush),
                typeof(DirectWriteLabel),
                new FrameworkPropertyMetadata(Brushes.White, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty FontSizeProperty =
            DependencyProperty.Register(
                nameof(FontSize),
                typeof(double),
                typeof(DirectWriteLabel),
                new FrameworkPropertyMetadata(13.0, FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty IsBoldProperty =
            DependencyProperty.Register(
                nameof(IsBold),
                typeof(bool),
                typeof(DirectWriteLabel),
                new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender));

        public string Text
        {
            get => (string)GetValue(TextProperty);
            set => SetValue(TextProperty, value);
        }

        public Brush Foreground
        {
            get => (Brush)GetValue(ForegroundProperty);
            set => SetValue(ForegroundProperty, value);
        }

        public double FontSize
        {
            get => (double)GetValue(FontSizeProperty);
            set => SetValue(FontSizeProperty, value);
        }

        public bool IsBold
        {
            get => (bool)GetValue(IsBoldProperty);
            set => SetValue(IsBoldProperty, value);
        }

        private FormattedText CreateFormattedText()
        {
            string str = string.IsNullOrEmpty(Text) ? " " : Text;
            Brush brush = Foreground ?? Brushes.White;
            Typeface tf = IsBold ? s_boldTypeface : s_regularTypeface;
            return new FormattedText(str, CultureInfo.InvariantCulture, FlowDirection.LeftToRight, tf, FontSize, brush, 1.0);
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            FormattedText ft = CreateFormattedText();
            if (!double.IsInfinity(availableSize.Width) && availableSize.Width > 8)
            {
                ft.MaxTextWidth = availableSize.Width;
            }
            return new Size(Math.Ceiling(ft.WidthIncludingTrailingWhitespace) + 2, Math.Ceiling(ft.Height) + 2);
        }

        protected override void OnRender(DrawingContext dc)
        {
            if (string.IsNullOrEmpty(Text))
            {
                return;
            }

            FormattedText ft = CreateFormattedText();
            if (RenderSize.Width > 8)
            {
                ft.MaxTextWidth = RenderSize.Width;
            }
            dc.DrawText(ft, new Point(0, 0));
        }
    }

    internal sealed class ShowcaseButton : ContentControl
    {
        public static readonly RoutedEvent ClickEvent =
            EventManager.RegisterRoutedEvent(
                nameof(Click),
                RoutingStrategy.Bubble,
                typeof(RoutedEventHandler),
                typeof(ShowcaseButton));

        public event RoutedEventHandler Click
        {
            add => AddHandler(ClickEvent, value);
            remove => RemoveHandler(ClickEvent, value);
        }

        private CardDecorator _templateChrome;
        private DirectWriteLabel _templateLabel;

        public string LabelText
        {
            get => _templateLabel != null ? _templateLabel.Text : (Content as string ?? string.Empty);
            set
            {
                Content = value;
                if (_templateLabel != null)
                {
                    _templateLabel.Text = value;
                }
            }
        }

        public ShowcaseButton(string text, double width = 148, double height = 34)
        {
            Width = width;
            Height = height;
            Content = text;
            Cursor = Cursors.Hand;

            Style btnStyle = new Style(typeof(ShowcaseButton));
            btnStyle.Setters.Add(new Setter(BackgroundProperty, new SolidColorBrush(Color.FromRgb(38, 94, 212))));
            btnStyle.Setters.Add(new Setter(BorderBrushProperty, new SolidColorBrush(Color.FromRgb(88, 148, 255))));
            btnStyle.Setters.Add(new Setter(ForegroundProperty, Brushes.White));

            Trigger hoverTrigger = new Trigger
            {
                Property = IsMouseOverProperty,
                Value = true
            };
            hoverTrigger.Setters.Add(new Setter(BackgroundProperty, new SolidColorBrush(Color.FromRgb(64, 126, 245))));
            hoverTrigger.Setters.Add(new Setter(BorderBrushProperty, new SolidColorBrush(Color.FromRgb(150, 200, 255))));
            btnStyle.Triggers.Add(hoverTrigger);
            Style = btnStyle;

            var template = new ControlTemplate(typeof(ShowcaseButton));
            FrameworkElementFactory chromeFactory = FrameworkElementFactory.Create<CardDecorator>("PART_Chrome");
            chromeFactory.SetValue(CardDecorator.CornerRadiusProperty, 7.0);
            chromeFactory.SetValue(CardDecorator.PaddingProperty, new Thickness(10, 6, 10, 6));

            FrameworkElementFactory lblFactory = FrameworkElementFactory.Create<DirectWriteLabel>("PART_Label");
            lblFactory.SetValue(DirectWriteLabel.FontSizeProperty, 12.5);
            lblFactory.SetValue(DirectWriteLabel.IsBoldProperty, true);
            lblFactory.SetValue(HorizontalAlignmentProperty, HorizontalAlignment.Center);
            lblFactory.SetValue(VerticalAlignmentProperty, VerticalAlignment.Center);
            chromeFactory.AppendChild(lblFactory);

            template.VisualTree = chromeFactory;
            Template = template;
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            _templateChrome = GetTemplateChild("PART_Chrome") as CardDecorator;
            _templateLabel = GetTemplateChild("PART_Label") as DirectWriteLabel;
            SyncVisualState();
        }

        protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(e);
            if (e.Property == BackgroundProperty || e.Property == BorderBrushProperty || e.Property == ForegroundProperty)
            {
                SyncVisualState();
            }
        }

        private void SyncVisualState()
        {
            if (_templateChrome != null)
            {
                _templateChrome.Background = Background;
                _templateChrome.BorderBrush = BorderBrush;
            }
            if (_templateLabel != null)
            {
                _templateLabel.Foreground = Foreground;
                if (Content is string s)
                {
                    _templateLabel.Text = s;
                }
            }
        }

        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonDown(e);
            e.Handled = true;
            RaiseEvent(new RoutedEventArgs(ClickEvent, this));
        }
    }

    internal sealed class InteractiveSliderBar : FrameworkElement
    {
        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register(
                nameof(Value),
                typeof(double),
                typeof(InteractiveSliderBar),
                new FrameworkPropertyMetadata(50.0, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault | FrameworkPropertyMetadataOptions.AffectsRender));

        public double Minimum { get; set; } = 0.0;
        public double Maximum { get; set; } = 100.0;
        public Brush AccentBrush { get; set; } = new SolidColorBrush(Color.FromRgb(56, 189, 248));

        public double Value
        {
            get => (double)GetValue(ValueProperty);
            set => SetValue(ValueProperty, Math.Clamp(value, Minimum, Maximum));
        }

        public InteractiveSliderBar()
        {
            Height = 22;
            Cursor = Cursors.Hand;
        }

        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonDown(e);
            CaptureMouse();
            UpdateFromPoint(e.GetPosition(this));
            e.Handled = true;
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (IsMouseCaptured && e.LeftButton == MouseButtonState.Pressed)
            {
                UpdateFromPoint(e.GetPosition(this));
            }
        }

        protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonUp(e);
            if (IsMouseCaptured)
            {
                ReleaseMouseCapture();
            }
        }

        private void UpdateFromPoint(Point pt)
        {
            double width = Math.Max(1.0, RenderSize.Width - 16.0);
            double ratio = Math.Clamp((pt.X - 8.0) / width, 0.0, 1.0);
            Value = Minimum + ratio * (Maximum - Minimum);
        }

        protected override void OnRender(DrawingContext dc)
        {
            double w = Math.Max(20, RenderSize.Width);
            double h = RenderSize.Height;
            double trackY = h * 0.5 - 3.5;
            double trackW = w - 16.0;
            double ratio = Math.Clamp((Value - Minimum) / Math.Max(0.001, Maximum - Minimum), 0.0, 1.0);
            double fillW = trackW * ratio;

            dc.DrawRoundedRectangle(
                new SolidColorBrush(Color.FromArgb(110, 80, 95, 125)),
                null,
                new Rect(8, trackY, trackW, 7),
                3.5,
                3.5);

            if (fillW > 1)
            {
                dc.DrawRoundedRectangle(
                    AccentBrush,
                    null,
                    new Rect(8, trackY, fillW, 7),
                    3.5,
                    3.5);
            }

            double thumbX = 8.0 + fillW;
            dc.DrawEllipse(
                Brushes.White,
                new Pen(AccentBrush, 2.2),
                new Point(thumbX, h * 0.5),
                7.5,
                7.5);
        }
    }

    internal sealed class VectorShowcaseCanvas : FrameworkElement
    {
        private readonly WriteableBitmap _wicBadgeBitmap;
        private double _phaseRadians;

        public VectorShowcaseCanvas()
        {
            Height = 148;
            _wicBadgeBitmap = CreateProceduralWicBitmap(56, 56);
        }

        public void AdvancePhase(double deltaRadians)
        {
            _phaseRadians = (_phaseRadians + deltaRadians) % (Math.PI * 2.0);
            InvalidateVisual();
        }

        private static WriteableBitmap CreateProceduralWicBitmap(int width, int height)
        {
            WriteableBitmap wb = new WriteableBitmap(width, height, 96, 96, PixelFormats.Bgra32, null);
            byte[] pixels = new byte[width * height * 4];
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int idx = (y * width + x) * 4;
                    double nx = (x - width * 0.5) / (width * 0.5);
                    double ny = (y - height * 0.5) / (height * 0.5);
                    double dist = Math.Sqrt(nx * nx + ny * ny);
                    bool checker = ((x / 7) + (y / 7)) % 2 == 0;

                    if (dist <= 0.92)
                    {
                        pixels[idx + 0] = (byte)(checker ? 255 : 140); // B
                        pixels[idx + 1] = (byte)(checker ? 210 : 60);  // G
                        pixels[idx + 2] = (byte)(checker ? 40 : 230);  // R
                        pixels[idx + 3] = 255;                         // A
                    }
                }
            }
            wb.WritePixels(new Int32Rect(0, 0, width, height), pixels, width * 4, 0);
            return wb;
        }

        protected override void OnRender(DrawingContext dc)
        {
            double w = Math.Max(260, RenderSize.Width);
            double h = Math.Max(120, RenderSize.Height);

            // 1. Subtle gradient backdrop
            var bgGrad = new LinearGradientBrush(
                Color.FromArgb(140, 14, 24, 48),
                Color.FromArgb(180, 28, 16, 58),
                new Point(0, 0),
                new Point(1, 1));
            dc.DrawRoundedRectangle(bgGrad, new Pen(new SolidColorBrush(Color.FromArgb(90, 96, 165, 250)), 1.0), new Rect(0, 0, w, h), 8, 8);

            // 2. RadialGradient pulsing energy orb
            double orbX = 68 + Math.Cos(_phaseRadians) * 10.0;
            double orbY = h * 0.5 + Math.Sin(_phaseRadians * 1.5) * 6.0;
            var radial = new RadialGradientBrush(Color.FromRgb(56, 239, 125), Color.FromArgb(15, 17, 153, 142))
            {
                Center = new Point(0.45, 0.45),
                GradientOrigin = new Point(0.35, 0.35),
                RadiusX = 0.5,
                RadiusY = 0.5
            };
            dc.DrawEllipse(radial, new Pen(new SolidColorBrush(Color.FromArgb(180, 110, 255, 190)), 1.5), new Point(orbX, orbY), 40, 40);

            // 3. StreamGeometry animated 8-point star
            double starCx = 178;
            double starCy = h * 0.5;
            StreamGeometry starGeo = new StreamGeometry();
            using (StreamGeometryContext sgc = starGeo.Open())
            {
                const int points = 16;
                for (int i = 0; i < points; i++)
                {
                    double angle = _phaseRadians * 0.8 + i * (Math.PI * 2.0 / points);
                    double radius = (i % 2 == 0) ? 38.0 : 16.0;
                    Point pt = new Point(starCx + Math.Cos(angle) * radius, starCy + Math.Sin(angle) * radius);
                    if (i == 0)
                    {
                        sgc.BeginFigure(pt, isFilled: true, isClosed: true);
                    }
                    else
                    {
                        sgc.LineTo(pt, isStroked: true, isSmoothJoin: false);
                    }
                }
            }
            var starBrush = new LinearGradientBrush(Color.FromRgb(251, 191, 36), Color.FromRgb(244, 63, 94), 45.0);
            dc.DrawGeometry(starBrush, new Pen(Brushes.White, 1.1), starGeo);

            // 4. WIC WriteableBitmap procedural badge
            double bmpX = w - 76;
            double bmpY = (h - 56) * 0.5;
            dc.DrawImage(_wicBadgeBitmap, new Rect(bmpX, bmpY, 56, 56));
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System;
using System.Collections;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;

namespace System.Windows.Controls
{
    public class DocumentViewer : DocumentViewerBase
    {
        public static readonly DependencyProperty ShowPageBordersProperty =
            DependencyProperty.Register(nameof(ShowPageBorders), typeof(bool), typeof(DocumentViewer),
                new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty ZoomProperty =
            DependencyProperty.Register(nameof(Zoom), typeof(double), typeof(DocumentViewer),
                new FrameworkPropertyMetadata(100.0, FrameworkPropertyMetadataOptions.AffectsMeasure));

        public static readonly DependencyProperty MaxPagesAcrossProperty =
            DependencyProperty.Register(nameof(MaxPagesAcross), typeof(int), typeof(DocumentViewer),
                new FrameworkPropertyMetadata(1));

        public static readonly DependencyProperty VerticalOffsetProperty =
            DependencyProperty.Register(nameof(VerticalOffset), typeof(double), typeof(DocumentViewer),
                new FrameworkPropertyMetadata(0.0));

        public static readonly DependencyProperty HorizontalOffsetProperty =
            DependencyProperty.Register(nameof(HorizontalOffset), typeof(double), typeof(DocumentViewer),
                new FrameworkPropertyMetadata(0.0));

        public bool ShowPageBorders
        {
            get => (bool)GetValue(ShowPageBordersProperty);
            set => SetValue(ShowPageBordersProperty, value);
        }

        public double Zoom
        {
            get => (double)GetValue(ZoomProperty);
            set => SetValue(ZoomProperty, Math.Clamp(value, 5.0, 5000.0));
        }

        public int MaxPagesAcross
        {
            get => (int)GetValue(MaxPagesAcrossProperty);
            set => SetValue(MaxPagesAcrossProperty, Math.Max(1, value));
        }

        public double VerticalOffset
        {
            get => (double)GetValue(VerticalOffsetProperty);
            set => SetValue(VerticalOffsetProperty, value);
        }

        public double HorizontalOffset
        {
            get => (double)GetValue(HorizontalOffsetProperty);
            set => SetValue(HorizontalOffsetProperty, value);
        }

        public bool CanIncreaseZoom => Zoom < 5000.0;
        public bool CanDecreaseZoom => Zoom > 5.0;

        public void IncreaseZoom() => Zoom = Math.Min(5000.0, Zoom * 1.25);
        public void DecreaseZoom() => Zoom = Math.Max(5.0, Zoom / 1.25);
        public void FitToWidth() => Zoom = 100.0;
        public void FitToHeight() => Zoom = 100.0;
        public void FitToMaxPagesAcross() => FitToMaxPagesAcross(1);
        public void FitToMaxPagesAcross(int pagesAcross) => MaxPagesAcross = pagesAcross;
        public void ViewThumbnails() => MaxPagesAcross = 4;

        protected override Size MeasureOverride(Size constraint)
        {
            if (PageViews.Count > 0)
            {
                PageViews[0].Measure(constraint);
                return PageViews[0].DesiredSize;
            }
            return base.MeasureOverride(constraint);
        }

        protected override AutomationPeer OnCreateAutomationPeer() => new DocumentViewerAutomationPeer(this);
    }

    public class FlowDocumentPageViewer : DocumentViewerBase
    {
        public static readonly DependencyProperty ZoomProperty =
            DependencyProperty.Register(nameof(Zoom), typeof(double), typeof(FlowDocumentPageViewer), new FrameworkPropertyMetadata(100.0));

        public static readonly DependencyProperty MaxZoomProperty =
            DependencyProperty.Register(nameof(MaxZoom), typeof(double), typeof(FlowDocumentPageViewer), new FrameworkPropertyMetadata(200.0));

        public static readonly DependencyProperty MinZoomProperty =
            DependencyProperty.Register(nameof(MinZoom), typeof(double), typeof(FlowDocumentPageViewer), new FrameworkPropertyMetadata(80.0));

        public static readonly DependencyProperty ZoomIncrementProperty =
            DependencyProperty.Register(nameof(ZoomIncrement), typeof(double), typeof(FlowDocumentPageViewer), new FrameworkPropertyMetadata(10.0));

        public double Zoom
        {
            get => (double)GetValue(ZoomProperty);
            set => SetValue(ZoomProperty, Math.Clamp(value, MinZoom, MaxZoom));
        }

        public double MaxZoom
        {
            get => (double)GetValue(MaxZoomProperty);
            set => SetValue(MaxZoomProperty, value);
        }

        public double MinZoom
        {
            get => (double)GetValue(MinZoomProperty);
            set => SetValue(MinZoomProperty, value);
        }

        public double ZoomIncrement
        {
            get => (double)GetValue(ZoomIncrementProperty);
            set => SetValue(ZoomIncrementProperty, value);
        }

        public bool CanIncreaseZoom => Zoom < MaxZoom;
        public bool CanDecreaseZoom => Zoom > MinZoom;
        public TextSelection Selection => Document is FlowDocument fd ? new TextSelection(fd.ContentStart, fd.ContentStart) : null;

        public void IncreaseZoom() => Zoom = Math.Min(MaxZoom, Zoom + ZoomIncrement);
        public void DecreaseZoom() => Zoom = Math.Max(MinZoom, Zoom - ZoomIncrement);

        protected override AutomationPeer OnCreateAutomationPeer() => new FlowDocumentPageViewerAutomationPeer(this);
    }

    [ContentProperty(nameof(Document))]
    public class FlowDocumentScrollViewer : Control, IAddChild, IServiceProvider
    {
        public static readonly DependencyProperty DocumentProperty =
            DependencyProperty.Register(nameof(Document), typeof(FlowDocument), typeof(FlowDocumentScrollViewer),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty ZoomProperty =
            DependencyProperty.Register(nameof(Zoom), typeof(double), typeof(FlowDocumentScrollViewer), new FrameworkPropertyMetadata(100.0));

        public static readonly DependencyProperty MaxZoomProperty =
            DependencyProperty.Register(nameof(MaxZoom), typeof(double), typeof(FlowDocumentScrollViewer), new FrameworkPropertyMetadata(200.0));

        public static readonly DependencyProperty MinZoomProperty =
            DependencyProperty.Register(nameof(MinZoom), typeof(double), typeof(FlowDocumentScrollViewer), new FrameworkPropertyMetadata(80.0));

        public static readonly DependencyProperty ZoomIncrementProperty =
            DependencyProperty.Register(nameof(ZoomIncrement), typeof(double), typeof(FlowDocumentScrollViewer), new FrameworkPropertyMetadata(10.0));

        public static readonly DependencyProperty IsToolBarVisibleProperty =
            DependencyProperty.Register(nameof(IsToolBarVisible), typeof(bool), typeof(FlowDocumentScrollViewer), new FrameworkPropertyMetadata(false));

        public static readonly DependencyProperty IsSelectionEnabledProperty =
            DependencyProperty.Register(nameof(IsSelectionEnabled), typeof(bool), typeof(FlowDocumentScrollViewer), new FrameworkPropertyMetadata(true));

        public static readonly DependencyProperty VerticalScrollBarVisibilityProperty =
            ScrollViewer.VerticalScrollBarVisibilityProperty.AddOwner(typeof(FlowDocumentScrollViewer));

        public static readonly DependencyProperty HorizontalScrollBarVisibilityProperty =
            ScrollViewer.HorizontalScrollBarVisibilityProperty.AddOwner(typeof(FlowDocumentScrollViewer));

        public FlowDocument Document
        {
            get => (FlowDocument)GetValue(DocumentProperty);
            set => SetValue(DocumentProperty, value);
        }

        public double Zoom
        {
            get => (double)GetValue(ZoomProperty);
            set => SetValue(ZoomProperty, Math.Clamp(value, MinZoom, MaxZoom));
        }

        public double MaxZoom
        {
            get => (double)GetValue(MaxZoomProperty);
            set => SetValue(MaxZoomProperty, value);
        }

        public double MinZoom
        {
            get => (double)GetValue(MinZoomProperty);
            set => SetValue(MinZoomProperty, value);
        }

        public double ZoomIncrement
        {
            get => (double)GetValue(ZoomIncrementProperty);
            set => SetValue(ZoomIncrementProperty, value);
        }

        public bool IsToolBarVisible
        {
            get => (bool)GetValue(IsToolBarVisibleProperty);
            set => SetValue(IsToolBarVisibleProperty, value);
        }

        public bool IsSelectionEnabled
        {
            get => (bool)GetValue(IsSelectionEnabledProperty);
            set => SetValue(IsSelectionEnabledProperty, value);
        }

        public ScrollBarVisibility VerticalScrollBarVisibility
        {
            get => (ScrollBarVisibility)GetValue(VerticalScrollBarVisibilityProperty);
            set => SetValue(VerticalScrollBarVisibilityProperty, value);
        }

        public ScrollBarVisibility HorizontalScrollBarVisibility
        {
            get => (ScrollBarVisibility)GetValue(HorizontalScrollBarVisibilityProperty);
            set => SetValue(HorizontalScrollBarVisibilityProperty, value);
        }

        public bool CanIncreaseZoom => Zoom < MaxZoom;
        public bool CanDecreaseZoom => Zoom > MinZoom;
        public TextSelection Selection => Document != null ? new TextSelection(Document.ContentStart, Document.ContentStart) : null;

        public void IncreaseZoom() => Zoom = Math.Min(MaxZoom, Zoom + ZoomIncrement);
        public void DecreaseZoom() => Zoom = Math.Max(MinZoom, Zoom - ZoomIncrement);

        public void Find() { }
        public void Print()
        {
            if (Document == null) return;
            var pd = new PrintDialog();
            if (pd.ShowDialog() == true)
            {
                pd.PrintDocument(Document.DocumentPaginator, "FlowDocument");
            }
        }
        public void CancelPrint() { }

        protected internal override IEnumerator LogicalChildren => new SingleChildEnumerator(Document);

        void IAddChild.AddChild(object value)
        {
            ArgumentNullException.ThrowIfNull(value);
            if (value is FlowDocument fd) Document = fd;
            else throw new ArgumentException("Child must be a FlowDocument.", nameof(value));
        }

        void IAddChild.AddText(string text) { }

        object IServiceProvider.GetService(Type serviceType) => null;

        protected override AutomationPeer OnCreateAutomationPeer() => new FlowDocumentScrollViewerAutomationPeer(this);
    }

    public enum FlowDocumentReaderViewingMode
    {
        Page,
        TwoPage,
        Scroll
    }

    [ContentProperty(nameof(Document))]
    public class FlowDocumentReader : Control, IAddChild
    {
        public static readonly DependencyProperty DocumentProperty =
            DependencyProperty.Register(nameof(Document), typeof(FlowDocument), typeof(FlowDocumentReader),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsMeasure));

        public static readonly DependencyProperty ViewingModeProperty =
            DependencyProperty.Register(nameof(ViewingMode), typeof(FlowDocumentReaderViewingMode), typeof(FlowDocumentReader),
                new FrameworkPropertyMetadata(FlowDocumentReaderViewingMode.Page));

        public static readonly DependencyProperty IsPageViewEnabledProperty =
            DependencyProperty.Register(nameof(IsPageViewEnabled), typeof(bool), typeof(FlowDocumentReader), new FrameworkPropertyMetadata(true));

        public static readonly DependencyProperty IsTwoPageViewEnabledProperty =
            DependencyProperty.Register(nameof(IsTwoPageViewEnabled), typeof(bool), typeof(FlowDocumentReader), new FrameworkPropertyMetadata(true));

        public static readonly DependencyProperty IsScrollViewEnabledProperty =
            DependencyProperty.Register(nameof(IsScrollViewEnabled), typeof(bool), typeof(FlowDocumentReader), new FrameworkPropertyMetadata(true));

        public static readonly DependencyProperty IsFindEnabledProperty =
            DependencyProperty.Register(nameof(IsFindEnabled), typeof(bool), typeof(FlowDocumentReader), new FrameworkPropertyMetadata(true));

        public static readonly DependencyProperty IsPrintEnabledProperty =
            DependencyProperty.Register(nameof(IsPrintEnabled), typeof(bool), typeof(FlowDocumentReader), new FrameworkPropertyMetadata(true));

        public static readonly DependencyProperty ZoomProperty =
            DependencyProperty.Register(nameof(Zoom), typeof(double), typeof(FlowDocumentReader), new FrameworkPropertyMetadata(100.0));

        public static readonly DependencyProperty MaxZoomProperty =
            DependencyProperty.Register(nameof(MaxZoom), typeof(double), typeof(FlowDocumentReader), new FrameworkPropertyMetadata(200.0));

        public static readonly DependencyProperty MinZoomProperty =
            DependencyProperty.Register(nameof(MinZoom), typeof(double), typeof(FlowDocumentReader), new FrameworkPropertyMetadata(80.0));

        public static readonly DependencyProperty ZoomIncrementProperty =
            DependencyProperty.Register(nameof(ZoomIncrement), typeof(double), typeof(FlowDocumentReader), new FrameworkPropertyMetadata(10.0));

        private int _pageNumber = 1;

        public FlowDocument Document
        {
            get => (FlowDocument)GetValue(DocumentProperty);
            set => SetValue(DocumentProperty, value);
        }

        public FlowDocumentReaderViewingMode ViewingMode
        {
            get => (FlowDocumentReaderViewingMode)GetValue(ViewingModeProperty);
            set => SetValue(ViewingModeProperty, value);
        }

        public bool IsPageViewEnabled
        {
            get => (bool)GetValue(IsPageViewEnabledProperty);
            set => SetValue(IsPageViewEnabledProperty, value);
        }

        public bool IsTwoPageViewEnabled
        {
            get => (bool)GetValue(IsTwoPageViewEnabledProperty);
            set => SetValue(IsTwoPageViewEnabledProperty, value);
        }

        public bool IsScrollViewEnabled
        {
            get => (bool)GetValue(IsScrollViewEnabledProperty);
            set => SetValue(IsScrollViewEnabledProperty, value);
        }

        public bool IsFindEnabled
        {
            get => (bool)GetValue(IsFindEnabledProperty);
            set => SetValue(IsFindEnabledProperty, value);
        }

        public bool IsPrintEnabled
        {
            get => (bool)GetValue(IsPrintEnabledProperty);
            set => SetValue(IsPrintEnabledProperty, value);
        }

        public double Zoom
        {
            get => (double)GetValue(ZoomProperty);
            set => SetValue(ZoomProperty, Math.Clamp(value, MinZoom, MaxZoom));
        }

        public double MaxZoom
        {
            get => (double)GetValue(MaxZoomProperty);
            set => SetValue(MaxZoomProperty, value);
        }

        public double MinZoom
        {
            get => (double)GetValue(MinZoomProperty);
            set => SetValue(MinZoomProperty, value);
        }

        public double ZoomIncrement
        {
            get => (double)GetValue(ZoomIncrementProperty);
            set => SetValue(ZoomIncrementProperty, value);
        }

        public int PageCount => Document != null ? Document.DocumentPaginator.PageCount : 0;
        public int PageNumber => Document != null ? _pageNumber : 0;
        public bool CanGoToPreviousPage => _pageNumber > 1;
        public bool CanGoToNextPage => _pageNumber < PageCount;
        public bool CanIncreaseZoom => Zoom < MaxZoom;
        public bool CanDecreaseZoom => Zoom > MinZoom;
        public TextSelection Selection => Document != null ? new TextSelection(Document.ContentStart, Document.ContentStart) : null;

        public void SwitchViewingMode(FlowDocumentReaderViewingMode viewingMode) => ViewingMode = viewingMode;
        public void IncreaseZoom() => Zoom = Math.Min(MaxZoom, Zoom + ZoomIncrement);
        public void DecreaseZoom() => Zoom = Math.Max(MinZoom, Zoom - ZoomIncrement);
        public void PreviousPage() { if (CanGoToPreviousPage) _pageNumber--; }
        public void NextPage() { if (CanGoToNextPage) _pageNumber++; }
        public void FirstPage() => _pageNumber = PageCount > 0 ? 1 : 0;
        public void LastPage() => _pageNumber = PageCount;
        public bool CanGoToPage(int pageNumber) => pageNumber >= 1 && pageNumber <= PageCount;
        public void Find() { }
        public void Print()
        {
            if (Document == null) return;
            var pd = new PrintDialog();
            if (pd.ShowDialog() == true)
            {
                pd.PrintDocument(Document.DocumentPaginator, "FlowDocument");
            }
        }
        public void CancelPrint() { }

        protected internal override IEnumerator LogicalChildren => new SingleChildEnumerator(Document);

        void IAddChild.AddChild(object value)
        {
            ArgumentNullException.ThrowIfNull(value);
            if (value is FlowDocument fd) Document = fd;
            else throw new ArgumentException("Child must be a FlowDocument.", nameof(value));
        }

        void IAddChild.AddText(string text) { }

        protected override AutomationPeer OnCreateAutomationPeer() => new FlowDocumentReaderAutomationPeer(this);
    }

    public enum StickyNoteType
    {
        Text,
        Ink
    }

    public class StickyNoteControl : Control
    {
        public static readonly RoutedCommand DeleteNoteCommand = new RoutedCommand(nameof(DeleteNoteCommand), typeof(StickyNoteControl));
        public static readonly RoutedCommand InkCommand = new RoutedCommand(nameof(InkCommand), typeof(StickyNoteControl));

        public static readonly DependencyProperty AuthorProperty =
            DependencyProperty.Register(nameof(Author), typeof(string), typeof(StickyNoteControl), new FrameworkPropertyMetadata(string.Empty));

        public static readonly DependencyProperty IsExpandedProperty =
            DependencyProperty.Register(nameof(IsExpanded), typeof(bool), typeof(StickyNoteControl), new FrameworkPropertyMetadata(true));

        public static readonly DependencyProperty IsActiveProperty =
            DependencyProperty.Register(nameof(IsActive), typeof(bool), typeof(StickyNoteControl), new FrameworkPropertyMetadata(false));

        public static readonly DependencyProperty IsMouseOverAnchorProperty =
            DependencyProperty.Register(nameof(IsMouseOverAnchor), typeof(bool), typeof(StickyNoteControl), new FrameworkPropertyMetadata(false));

        public static readonly DependencyProperty StickyNoteTypeProperty =
            DependencyProperty.Register(nameof(StickyNoteType), typeof(StickyNoteType), typeof(StickyNoteControl), new FrameworkPropertyMetadata(StickyNoteType.Text));

        public static readonly DependencyProperty CaptionFontFamilyProperty =
            DependencyProperty.Register(nameof(CaptionFontFamily), typeof(FontFamily), typeof(StickyNoteControl), new FrameworkPropertyMetadata(new FontFamily("Segoe UI")));

        public static readonly DependencyProperty CaptionFontSizeProperty =
            DependencyProperty.Register(nameof(CaptionFontSize), typeof(double), typeof(StickyNoteControl), new FrameworkPropertyMetadata(12.0));

        public static readonly DependencyProperty PenWidthProperty =
            DependencyProperty.Register(nameof(PenWidth), typeof(double), typeof(StickyNoteControl), new FrameworkPropertyMetadata(2.0));

        public string Author
        {
            get => (string)GetValue(AuthorProperty);
            set => SetValue(AuthorProperty, value);
        }

        public bool IsExpanded
        {
            get => (bool)GetValue(IsExpandedProperty);
            set => SetValue(IsExpandedProperty, value);
        }

        public bool IsActive => (bool)GetValue(IsActiveProperty);
        public bool IsMouseOverAnchor => (bool)GetValue(IsMouseOverAnchorProperty);
        public StickyNoteType StickyNoteType => (StickyNoteType)GetValue(StickyNoteTypeProperty);

        public FontFamily CaptionFontFamily
        {
            get => (FontFamily)GetValue(CaptionFontFamilyProperty);
            set => SetValue(CaptionFontFamilyProperty, value);
        }

        public double CaptionFontSize
        {
            get => (double)GetValue(CaptionFontSizeProperty);
            set => SetValue(CaptionFontSizeProperty, value);
        }

        public double PenWidth
        {
            get => (double)GetValue(PenWidthProperty);
            set => SetValue(PenWidthProperty, value);
        }
    }
}

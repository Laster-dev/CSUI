// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Media;

namespace System.Windows.Documents
{
    [ContentProperty(nameof(Children))]
    public class FixedPage : FrameworkElement, IAddChild
    {
        public static readonly DependencyProperty LeftProperty = Canvas.LeftProperty.AddOwner(typeof(FixedPage));
        public static readonly DependencyProperty TopProperty = Canvas.TopProperty.AddOwner(typeof(FixedPage));
        public static readonly DependencyProperty RightProperty = Canvas.RightProperty.AddOwner(typeof(FixedPage));
        public static readonly DependencyProperty BottomProperty = Canvas.BottomProperty.AddOwner(typeof(FixedPage));

        public static readonly DependencyProperty BackgroundProperty =
            Panel.BackgroundProperty.AddOwner(typeof(FixedPage), new FrameworkPropertyMetadata(Brushes.White, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty PrintTicketProperty =
            DependencyProperty.RegisterAttached("PrintTicket", typeof(object), typeof(FixedPage), new FrameworkPropertyMetadata(null));

        public static readonly DependencyProperty ContentBoxProperty =
            DependencyProperty.Register(nameof(ContentBox), typeof(Rect), typeof(FixedPage), new FrameworkPropertyMetadata(Rect.Empty));

        public static readonly DependencyProperty BleedBoxProperty =
            DependencyProperty.Register(nameof(BleedBox), typeof(Rect), typeof(FixedPage), new FrameworkPropertyMetadata(Rect.Empty));

        private readonly Canvas _canvas = new Canvas();

        static FixedPage()
        {
            WidthProperty.OverrideMetadata(typeof(FixedPage), new FrameworkPropertyMetadata(816.0));
            HeightProperty.OverrideMetadata(typeof(FixedPage), new FrameworkPropertyMetadata(1056.0));
        }

        public FixedPage()
        {
            AddVisualChild(_canvas);
            AddLogicalChild(_canvas);
        }

        public static double GetLeft(UIElement element) => Canvas.GetLeft(element);
        public static void SetLeft(UIElement element, double length) => Canvas.SetLeft(element, length);
        public static double GetTop(UIElement element) => Canvas.GetTop(element);
        public static void SetTop(UIElement element, double length) => Canvas.SetTop(element, length);
        public static double GetRight(UIElement element) => Canvas.GetRight(element);
        public static void SetRight(UIElement element, double length) => Canvas.SetRight(element, length);
        public static double GetBottom(UIElement element) => Canvas.GetBottom(element);
        public static void SetBottom(UIElement element, double length) => Canvas.SetBottom(element, length);

        public static object GetPrintTicket(DependencyObject element) => element.GetValue(PrintTicketProperty);
        public static void SetPrintTicket(DependencyObject element, object value) => element.SetValue(PrintTicketProperty, value);

        public UIElementCollection Children => _canvas.Children;

        public Brush Background
        {
            get => (Brush)GetValue(BackgroundProperty);
            set => SetValue(BackgroundProperty, value);
        }

        public Rect ContentBox
        {
            get => (Rect)GetValue(ContentBoxProperty);
            set => SetValue(ContentBoxProperty, value);
        }

        public Rect BleedBox
        {
            get => (Rect)GetValue(BleedBoxProperty);
            set => SetValue(BleedBoxProperty, value);
        }

        public object PrintTicket
        {
            get => GetValue(PrintTicketProperty);
            set => SetValue(PrintTicketProperty, value);
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
            return _canvas;
        }

        protected override Size MeasureOverride(Size constraint)
        {
            _canvas.Measure(constraint);
            double w = !double.IsNaN(Width) ? Width : _canvas.DesiredSize.Width;
            double h = !double.IsNaN(Height) ? Height : _canvas.DesiredSize.Height;
            return new Size(w, h);
        }

        protected override Size ArrangeOverride(Size arrangeBounds)
        {
            _canvas.Arrange(new Rect(new Point(0, 0), arrangeBounds));
            return arrangeBounds;
        }

        protected override void OnRender(DrawingContext dc)
        {
            if (Background != null && RenderSize.Width > 0 && RenderSize.Height > 0)
            {
                dc.DrawRectangle(Background, null, new Rect(new Point(0, 0), RenderSize));
            }
        }

        protected override AutomationPeer OnCreateAutomationPeer() => new FixedPageAutomationPeer(this);
    }

    [ContentProperty(nameof(Child))]
    public class PageContent : FrameworkElement, IAddChild
    {
        public static readonly DependencyProperty SourceProperty =
            DependencyProperty.Register(nameof(Source), typeof(Uri), typeof(PageContent), new FrameworkPropertyMetadata(null));

        public Uri Source
        {
            get => (Uri)GetValue(SourceProperty);
            set => SetValue(SourceProperty, value);
        }

        public FixedPage Child { get; set; }

        public FixedPage GetPageRoot(bool forceReload) => Child;

        void IAddChild.AddChild(object value)
        {
            ArgumentNullException.ThrowIfNull(value);
            if (value is FixedPage fp)
            {
                Child = fp;
            }
        }

        void IAddChild.AddText(string text)
        {
        }
    }

    public class PageContentCollection : List<PageContent>
    {
    }

    [ContentProperty(nameof(Pages))]
    public class FixedDocument : FrameworkContentElement, IDocumentPaginatorSource, IAddChild
    {
        private readonly PageContentCollection _pages = new PageContentCollection();
        private DocumentPaginator _paginator;

        public FixedDocument()
        {
        }

        public PageContentCollection Pages => _pages;
        public object PrintTicket { get; set; }

        public DocumentPaginator DocumentPaginator => _paginator ??= new FixedDocumentPaginator(this);

        DocumentPaginator IDocumentPaginatorSource.DocumentPaginator => DocumentPaginator;

        void IAddChild.AddChild(object value)
        {
            ArgumentNullException.ThrowIfNull(value);
            if (value is PageContent pc)
            {
                _pages.Add(pc);
            }
        }

        void IAddChild.AddText(string text)
        {
        }

        private sealed class FixedDocumentPaginator : DocumentPaginator
        {
            private readonly FixedDocument _doc;
            private Size _pageSize = new Size(816, 1056);

            public FixedDocumentPaginator(FixedDocument doc)
            {
                _doc = doc;
            }

            public override bool IsPageCountValid => true;
            public override int PageCount => _doc._pages.Count;
            public override Size PageSize
            {
                get => _pageSize;
                set => _pageSize = value;
            }
            public override IDocumentPaginatorSource Source => _doc;

            public override DocumentPage GetPage(int pageNumber)
            {
                if (pageNumber < 0 || pageNumber >= _doc._pages.Count)
                {
                    return DocumentPage.Missing;
                }

                FixedPage page = _doc._pages[pageNumber].Child;
                if (page == null)
                {
                    return DocumentPage.Missing;
                }

                page.Measure(_pageSize);
                Size sz = page.DesiredSize;
                page.Arrange(new Rect(0, 0, sz.Width, sz.Height));
                return new DocumentPage(page, sz, new Rect(sz), new Rect(sz));
            }
        }
    }

    public class DocumentReference : FrameworkElement
    {
        public static readonly DependencyProperty SourceProperty =
            DependencyProperty.Register(nameof(Source), typeof(Uri), typeof(DocumentReference), new FrameworkPropertyMetadata(null));

        public Uri Source
        {
            get => (Uri)GetValue(SourceProperty);
            set => SetValue(SourceProperty, value);
        }

        private FixedDocument _doc;

        public void SetDocument(FixedDocument doc) => _doc = doc;
        public FixedDocument GetDocument(bool forceReload) => _doc;
    }

    public class DocumentReferenceCollection : List<DocumentReference>
    {
    }

    [ContentProperty(nameof(References))]
    public class FixedDocumentSequence : FrameworkContentElement, IDocumentPaginatorSource, IAddChild
    {
        private readonly DocumentReferenceCollection _references = new DocumentReferenceCollection();
        private DocumentPaginator _paginator;

        public DocumentReferenceCollection References => _references;
        public object PrintTicket { get; set; }

        public DocumentPaginator DocumentPaginator => _paginator ??= new SequencePaginator(this);

        DocumentPaginator IDocumentPaginatorSource.DocumentPaginator => DocumentPaginator;

        void IAddChild.AddChild(object value)
        {
            if (value is DocumentReference dr)
            {
                _references.Add(dr);
            }
        }

        void IAddChild.AddText(string text)
        {
        }

        private sealed class SequencePaginator : DocumentPaginator
        {
            private readonly FixedDocumentSequence _seq;
            private Size _pageSize = new Size(816, 1056);

            public SequencePaginator(FixedDocumentSequence seq)
            {
                _seq = seq;
            }

            public override bool IsPageCountValid => true;

            public override int PageCount
            {
                get
                {
                    int total = 0;
                    for (int i = 0; i < _seq._references.Count; i++)
                    {
                        total += _seq._references[i].GetDocument(false)?.Pages.Count ?? 0;
                    }
                    return total;
                }
            }

            public override Size PageSize
            {
                get => _pageSize;
                set => _pageSize = value;
            }

            public override IDocumentPaginatorSource Source => _seq;

            public override DocumentPage GetPage(int pageNumber)
            {
                int remaining = pageNumber;
                for (int i = 0; i < _seq._references.Count; i++)
                {
                    FixedDocument doc = _seq._references[i].GetDocument(false);
                    if (doc == null) continue;
                    if (remaining < doc.Pages.Count)
                    {
                        return doc.DocumentPaginator.GetPage(remaining);
                    }
                    remaining -= doc.Pages.Count;
                }
                return DocumentPage.Missing;
            }
        }
    }
}

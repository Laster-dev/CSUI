// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Documents;
using System.Windows.Media;

namespace System.Windows.Controls.Primitives
{
    public class DocumentPageView : FrameworkElement
    {
        public static readonly DependencyProperty DocumentPaginatorProperty =
            DependencyProperty.Register(nameof(DocumentPaginator), typeof(DocumentPaginator), typeof(DocumentPageView),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsMeasure, OnPageViewPropertyChanged));

        public static readonly DependencyProperty PageNumberProperty =
            DependencyProperty.Register(nameof(PageNumber), typeof(int), typeof(DocumentPageView),
                new FrameworkPropertyMetadata(0, FrameworkPropertyMetadataOptions.AffectsMeasure, OnPageViewPropertyChanged));

        public static readonly DependencyProperty StretchProperty =
            Viewbox.StretchProperty.AddOwner(typeof(DocumentPageView));

        public static readonly DependencyProperty StretchDirectionProperty =
            Viewbox.StretchDirectionProperty.AddOwner(typeof(DocumentPageView));

        private DocumentPage _currentPage;
        private UIElement _pageVisual;

        public event EventHandler PageConnected;
        public event EventHandler PageDisconnected;

        public DocumentPaginator DocumentPaginator
        {
            get => (DocumentPaginator)GetValue(DocumentPaginatorProperty);
            set => SetValue(DocumentPaginatorProperty, value);
        }

        public int PageNumber
        {
            get => (int)GetValue(PageNumberProperty);
            set => SetValue(PageNumberProperty, value);
        }

        public Stretch Stretch
        {
            get => (Stretch)GetValue(StretchProperty);
            set => SetValue(StretchProperty, value);
        }

        public StretchDirection StretchDirection
        {
            get => (StretchDirection)GetValue(StretchDirectionProperty);
            set => SetValue(StretchDirectionProperty, value);
        }

        public DocumentPage DocumentPage => _currentPage ?? DocumentPage.Missing;

        protected override int VisualChildrenCount => _pageVisual != null ? 1 : 0;

        protected override Visual GetVisualChild(int index)
        {
            if (index != 0 || _pageVisual == null)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }
            return _pageVisual;
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            EnsurePageLoaded();
            if (_pageVisual != null)
            {
                _pageVisual.Measure(availableSize);
                return _pageVisual.DesiredSize;
            }
            return new Size(0, 0);
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            _pageVisual?.Arrange(new Rect(new Point(0, 0), finalSize));
            return finalSize;
        }

        protected override AutomationPeer OnCreateAutomationPeer() => new DocumentPageViewAutomationPeer(this);

        private void EnsurePageLoaded()
        {
            if (_pageVisual != null)
            {
                RemoveVisualChild(_pageVisual);
                _pageVisual = null;
                PageDisconnected?.Invoke(this, EventArgs.Empty);
            }

            _currentPage = null;
            if (DocumentPaginator != null && PageNumber >= 0 && PageNumber < DocumentPaginator.PageCount)
            {
                _currentPage = DocumentPaginator.GetPage(PageNumber);
                if (_currentPage?.Visual is UIElement ui)
                {
                    _pageVisual = ui;
                    AddVisualChild(ui);
                    PageConnected?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        private static void OnPageViewPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DocumentPageView view)
            {
                view.InvalidateMeasure();
            }
        }
    }
}

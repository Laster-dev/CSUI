// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Markup;

namespace System.Windows.Controls.Primitives
{
    [ContentProperty(nameof(Document))]
    public abstract class DocumentViewerBase : Control, IAddChild, IServiceProvider
    {
        public static readonly DependencyProperty DocumentProperty =
            DependencyProperty.Register(nameof(Document), typeof(IDocumentPaginatorSource), typeof(DocumentViewerBase),
                new FrameworkPropertyMetadata(null, OnDocumentChanged));

        public static readonly DependencyProperty IsMasterPageProperty =
            DependencyProperty.RegisterAttached("IsMasterPage", typeof(bool), typeof(DocumentViewerBase), new FrameworkPropertyMetadata(false));

        private static readonly DependencyPropertyKey PageCountPropertyKey =
            DependencyProperty.RegisterReadOnly(nameof(PageCount), typeof(int), typeof(DocumentViewerBase), new FrameworkPropertyMetadata(0));

        public static readonly DependencyProperty PageCountProperty = PageCountPropertyKey.DependencyProperty;

        private static readonly DependencyPropertyKey MasterPageNumberPropertyKey =
            DependencyProperty.RegisterReadOnly(nameof(MasterPageNumber), typeof(int), typeof(DocumentViewerBase), new FrameworkPropertyMetadata(0));

        public static readonly DependencyProperty MasterPageNumberProperty = MasterPageNumberPropertyKey.DependencyProperty;

        private static readonly DependencyPropertyKey CanGoToPreviousPagePropertyKey =
            DependencyProperty.RegisterReadOnly(nameof(CanGoToPreviousPage), typeof(bool), typeof(DocumentViewerBase), new FrameworkPropertyMetadata(false));

        public static readonly DependencyProperty CanGoToPreviousPageProperty = CanGoToPreviousPagePropertyKey.DependencyProperty;

        private static readonly DependencyPropertyKey CanGoToNextPagePropertyKey =
            DependencyProperty.RegisterReadOnly(nameof(CanGoToNextPage), typeof(bool), typeof(DocumentViewerBase), new FrameworkPropertyMetadata(false));

        public static readonly DependencyProperty CanGoToNextPageProperty = CanGoToNextPagePropertyKey.DependencyProperty;

        private ReadOnlyCollection<DocumentPageView> _pageViews = ReadOnlyCollection<DocumentPageView>.Empty;
        private DocumentPageView _defaultPageView;

        public event EventHandler PageViewsChanged;

        protected DocumentViewerBase()
        {
        }

        public static bool GetIsMasterPage(DependencyObject element) => (bool)element.GetValue(IsMasterPageProperty);
        public static void SetIsMasterPage(DependencyObject element, bool value) => element.SetValue(IsMasterPageProperty, value);

        public IDocumentPaginatorSource Document
        {
            get => (IDocumentPaginatorSource)GetValue(DocumentProperty);
            set => SetValue(DocumentProperty, value);
        }

        public int PageCount => (int)GetValue(PageCountProperty);
        public int MasterPageNumber => (int)GetValue(MasterPageNumberProperty);
        public bool CanGoToPreviousPage => (bool)GetValue(CanGoToPreviousPageProperty);
        public bool CanGoToNextPage => (bool)GetValue(CanGoToNextPageProperty);
        public ReadOnlyCollection<DocumentPageView> PageViews => _pageViews;

        protected internal override IEnumerator LogicalChildren => new SingleChildEnumerator(Document);

        public virtual void PreviousPage() => GoToPage(MasterPageNumber - 1);
        public virtual void NextPage() => GoToPage(MasterPageNumber + 1);
        public virtual void FirstPage() => GoToPage(1);
        public virtual void LastPage() => GoToPage(Math.Max(1, PageCount));

        public virtual bool CanGoToPage(int pageNumber) => pageNumber >= 1 && pageNumber <= PageCount;

        public virtual void GoToPage(int pageNumber)
        {
            if (!CanGoToPage(pageNumber))
            {
                return;
            }

            SetValue(MasterPageNumberPropertyKey, pageNumber);
            UpdateNavigationState();
            if (_defaultPageView != null)
            {
                _defaultPageView.PageNumber = pageNumber - 1;
            }
        }

        public void Print()
        {
            if (Document?.DocumentPaginator == null) return;
            var pd = new PrintDialog();
            if (pd.ShowDialog() == true)
            {
                pd.PrintDocument(Document.DocumentPaginator, "Document");
            }
        }

        public void CancelPrint()
        {
        }

        void IAddChild.AddChild(object value)
        {
            ArgumentNullException.ThrowIfNull(value);
            if (value is IDocumentPaginatorSource doc)
            {
                Document = doc;
            }
            else
            {
                throw new ArgumentException("Document must implement IDocumentPaginatorSource.", nameof(value));
            }
        }

        void IAddChild.AddText(string text)
        {
        }

        object IServiceProvider.GetService(Type serviceType) => null;

        protected virtual void OnDocumentChanged()
        {
            if (Document?.DocumentPaginator is DocumentPaginator paginator)
            {
                paginator.ComputePageCount();
                SetValue(PageCountPropertyKey, paginator.PageCount);
                SetValue(MasterPageNumberPropertyKey, paginator.PageCount > 0 ? 1 : 0);

                _defaultPageView ??= new DocumentPageView();
                _defaultPageView.DocumentPaginator = paginator;
                _defaultPageView.PageNumber = 0;
                _pageViews = new ReadOnlyCollection<DocumentPageView>(new[] { _defaultPageView });
            }
            else
            {
                SetValue(PageCountPropertyKey, 0);
                SetValue(MasterPageNumberPropertyKey, 0);
                _pageViews = ReadOnlyCollection<DocumentPageView>.Empty;
            }

            UpdateNavigationState();
            OnPageViewsChanged();
            InvalidateMeasure();
            InvalidateVisual();
        }

        protected virtual void OnPageViewsChanged()
        {
            PageViewsChanged?.Invoke(this, EventArgs.Empty);
        }

        protected override AutomationPeer OnCreateAutomationPeer() => new DocumentViewerBaseAutomationPeer(this);

        private void UpdateNavigationState()
        {
            SetValue(CanGoToPreviousPagePropertyKey, MasterPageNumber > 1);
            SetValue(CanGoToNextPagePropertyKey, MasterPageNumber > 0 && MasterPageNumber < PageCount);
        }

        private static void OnDocumentChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DocumentViewerBase viewer)
            {
                viewer.OnDocumentChanged();
            }
        }
    }
}

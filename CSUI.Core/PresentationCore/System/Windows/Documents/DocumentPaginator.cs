// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System;
using System.ComponentModel;
using System.Windows.Media;

namespace System.Windows.Documents
{
    public class DocumentPage : IDisposable
    {
        public static readonly DocumentPage Missing = new DocumentPage(null, Size.Empty, Rect.Empty, Rect.Empty);

        private Visual _visual;
        private Size _pageSize;
        private Rect _bleedBox;
        private Rect _contentBox;

        public DocumentPage(Visual visual)
            : this(visual, Size.Empty, Rect.Empty, Rect.Empty)
        {
        }

        public DocumentPage(Visual visual, Size pageSize, Rect bleedBox, Rect contentBox)
        {
            _visual = visual;
            _pageSize = pageSize;
            _bleedBox = bleedBox;
            _contentBox = contentBox;
        }

        public virtual Visual Visual => _visual;
        public virtual Size Size => _pageSize;
        public virtual Rect BleedBox => _bleedBox;
        public virtual Rect ContentBox => _contentBox;

        public event EventHandler PageDestroyed;

        public virtual void Dispose()
        {
            OnPageDestroyed(EventArgs.Empty);
            GC.SuppressFinalize(this);
        }

        protected void OnPageDestroyed(EventArgs e) => PageDestroyed?.Invoke(this, e);
        protected void SetBleedBox(Rect bleedBox) => _bleedBox = bleedBox;
        protected void SetContentBox(Rect contentBox) => _contentBox = contentBox;
        protected void SetSize(Size size) => _pageSize = size;
        protected void SetVisual(Visual visual) => _visual = visual;
    }

    public abstract class DocumentPaginator
    {
        public abstract bool IsPageCountValid { get; }
        public abstract int PageCount { get; }
        public abstract Size PageSize { get; set; }
        public abstract IDocumentPaginatorSource Source { get; }

        public abstract DocumentPage GetPage(int pageNumber);

        public virtual void GetPageAsync(int pageNumber) => GetPageAsync(pageNumber, null);

        public virtual void GetPageAsync(int pageNumber, object userState)
        {
            DocumentPage page = GetPage(pageNumber);
            OnGetPageCompleted(new GetPageCompletedEventArgs(page, pageNumber, null, false, userState));
        }

        public virtual void CancelAsync(object userState)
        {
        }

        public virtual void ComputePageCount()
        {
        }

        public virtual void ComputePageCountAsync() => ComputePageCountAsync(null);

        public virtual void ComputePageCountAsync(object userState)
        {
            ComputePageCount();
            OnComputePageCountCompleted(new AsyncCompletedEventArgs(null, false, userState));
        }

        public event GetPageCompletedEventHandler GetPageCompleted;
        public event AsyncCompletedEventHandler ComputePageCountCompleted;
        public event PagesChangedEventHandler PagesChanged;

        protected virtual void OnGetPageCompleted(GetPageCompletedEventArgs e) => GetPageCompleted?.Invoke(this, e);
        protected virtual void OnComputePageCountCompleted(AsyncCompletedEventArgs e) => ComputePageCountCompleted?.Invoke(this, e);
        protected virtual void OnPagesChanged(PagesChangedEventArgs e) => PagesChanged?.Invoke(this, e);
    }

    public abstract class DynamicDocumentPaginator : DocumentPaginator
    {
        public virtual bool IsBackgroundPaginationEnabled
        {
            get => true;
            set { }
        }

        public abstract ContentPosition GetObjectPosition(object value);
        public abstract int GetPageNumber(ContentPosition contentPosition);

        public virtual void GetPageNumberAsync(ContentPosition contentPosition) => GetPageNumberAsync(contentPosition, null);

        public virtual void GetPageNumberAsync(ContentPosition contentPosition, object userState)
        {
            int pageNumber = GetPageNumber(contentPosition);
            OnGetPageNumberCompleted(new GetPageNumberCompletedEventArgs(contentPosition, pageNumber, null, false, userState));
        }

        public abstract ContentPosition GetPagePosition(DocumentPage page);

        public event GetPageNumberCompletedEventHandler GetPageNumberCompleted;
        public event PaginationProgressEventHandler PaginationProgress;
        public event EventHandler PaginationCompleted;

        protected virtual void OnGetPageNumberCompleted(GetPageNumberCompletedEventArgs e) => GetPageNumberCompleted?.Invoke(this, e);
        protected virtual void OnPaginationProgress(PaginationProgressEventArgs e) => PaginationProgress?.Invoke(this, e);
        protected virtual void OnPaginationCompleted(EventArgs e) => PaginationCompleted?.Invoke(this, e);
    }

    public interface IDocumentPaginatorSource
    {
        DocumentPaginator DocumentPaginator { get; }
    }

    public abstract class ContentPosition
    {
        public static readonly ContentPosition Missing = new MissingContentPosition();

        private sealed class MissingContentPosition : ContentPosition
        {
        }
    }

    public delegate void GetPageCompletedEventHandler(object sender, GetPageCompletedEventArgs e);
    public delegate void GetPageNumberCompletedEventHandler(object sender, GetPageNumberCompletedEventArgs e);
    public delegate void PagesChangedEventHandler(object sender, PagesChangedEventArgs e);
    public delegate void PaginationProgressEventHandler(object sender, PaginationProgressEventArgs e);

    public class GetPageCompletedEventArgs : AsyncCompletedEventArgs
    {
        public GetPageCompletedEventArgs(DocumentPage page, int pageNumber, Exception error, bool cancelled, object userState)
            : base(error, cancelled, userState)
        {
            DocumentPage = page;
            PageNumber = pageNumber;
        }

        public DocumentPage DocumentPage { get; }
        public int PageNumber { get; }
    }

    public class GetPageNumberCompletedEventArgs : AsyncCompletedEventArgs
    {
        public GetPageNumberCompletedEventArgs(ContentPosition contentPosition, int pageNumber, Exception error, bool cancelled, object userState)
            : base(error, cancelled, userState)
        {
            ContentPosition = contentPosition;
            PageNumber = pageNumber;
        }

        public ContentPosition ContentPosition { get; }
        public int PageNumber { get; }
    }

    public class PagesChangedEventArgs : EventArgs
    {
        public PagesChangedEventArgs(int start, int count)
        {
            Start = start;
            Count = count;
        }

        public int Start { get; }
        public int Count { get; }
    }

    public class PaginationProgressEventArgs : EventArgs
    {
        public PaginationProgressEventArgs(int start, int count)
        {
            Start = start;
            Count = count;
        }

        public int Start { get; }
        public int Count { get; }
    }
}

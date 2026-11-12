// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System;
using System.Windows.Documents;
using System.Windows.Media;

namespace System.Windows.Controls
{
    public enum PageRangeSelection
    {
        AllPages,
        UserPages,
        CurrentPage,
        SelectedPages
    }

    public struct PageRange : IEquatable<PageRange>
    {
        public PageRange(int page)
        {
            PageFrom = page;
            PageTo = page;
        }

        public PageRange(int pageFrom, int pageTo)
        {
            PageFrom = pageFrom;
            PageTo = pageTo;
        }

        public int PageFrom { get; set; }
        public int PageTo { get; set; }

        public bool Equals(PageRange pageRange) => PageFrom == pageRange.PageFrom && PageTo == pageRange.PageTo;

        public override bool Equals(object obj) => obj is PageRange other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(PageFrom, PageTo);

        public static bool operator ==(PageRange pr1, PageRange pr2) => pr1.Equals(pr2);
        public static bool operator !=(PageRange pr1, PageRange pr2) => !pr1.Equals(pr2);
    }

    [Serializable]
    public class PrintDialogException : Exception
    {
        public PrintDialogException()
        {
        }

        public PrintDialogException(string message) : base(message)
        {
        }

        public PrintDialogException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }

    /// <summary>
    /// Managed PrintDialog implementation without legacy comdlg32 COM/System.Printing coupling.
    /// </summary>
    public class PrintDialog
    {
        private PageRangeSelection _pageRangeSelection = PageRangeSelection.AllPages;
        private PageRange _pageRange;
        private bool _userPageRangeEnabled;
        private bool _currentPageEnabled;
        private bool _selectedPagesEnabled;
        private uint _minPage = 1;
        private uint _maxPage = 9999;
        private double _printableAreaWidth = 816.0;   // 8.5in @ 96 DPI
        private double _printableAreaHeight = 1056.0; // 11.0in @ 96 DPI

        public PageRangeSelection PageRangeSelection
        {
            get => _pageRangeSelection;
            set => _pageRangeSelection = value;
        }

        public PageRange PageRange
        {
            get => _pageRange;
            set
            {
                if (value.PageFrom <= 0 || value.PageTo <= 0)
                {
                    throw new ArgumentException("Page range values must be positive.", nameof(PageRange));
                }

                _pageRange = value.PageFrom > value.PageTo
                    ? new PageRange(value.PageTo, value.PageFrom)
                    : value;
            }
        }

        public bool UserPageRangeEnabled
        {
            get => _userPageRangeEnabled;
            set => _userPageRangeEnabled = value;
        }

        public bool CurrentPageEnabled
        {
            get => _currentPageEnabled;
            set => _currentPageEnabled = value;
        }

        public bool SelectedPagesEnabled
        {
            get => _selectedPagesEnabled;
            set => _selectedPagesEnabled = value;
        }

        public uint MinPage
        {
            get => _minPage;
            set
            {
                if (value <= 0)
                {
                    throw new ArgumentException("MinPage must be positive.", nameof(MinPage));
                }

                _minPage = value;
            }
        }

        public uint MaxPage
        {
            get => _maxPage;
            set
            {
                if (value <= 0)
                {
                    throw new ArgumentException("MaxPage must be positive.", nameof(MaxPage));
                }

                _maxPage = value;
            }
        }

        public double PrintableAreaWidth => _printableAreaWidth;
        public double PrintableAreaHeight => _printableAreaHeight;

        public void SetPrintableAreaSize(double width, double height)
        {
            if (width <= 0 || height <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(width));
            }

            _printableAreaWidth = width;
            _printableAreaHeight = height;
        }

        public bool? ShowDialog() => false;

        public void PrintVisual(Visual visual, string description)
        {
            ArgumentNullException.ThrowIfNull(visual);
        }

        public void PrintDocument(DocumentPaginator documentPaginator, string description)
        {
            ArgumentNullException.ThrowIfNull(documentPaginator);

            documentPaginator.ComputePageCount();
            int count = documentPaginator.PageCount;
            for (int i = 0; i < count; i++)
            {
                DocumentPage page = documentPaginator.GetPage(i);
                page?.Dispose();
            }
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System;
using System.Runtime.InteropServices;
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

        public unsafe bool? ShowDialog()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return false;

            if (!NativeLibrary.TryLoad("comdlg32.dll", out IntPtr comdlg))
                return false;
            if (!NativeLibrary.TryGetExport(comdlg, "PrintDlgW", out IntPtr pPrintDlg))
                return false;

            var pd = new PRINTDLGW
            {
                lStructSize = (uint)sizeof(PRINTDLGW),
                hwndOwner = IntPtr.Zero,
                Flags = 0x00000100u | 0x00080000u, // PD_RETURNDC | PD_USEDEVMODECOPIESANDCOLLATE
                nFromPage = (ushort)Math.Clamp(_minPage, 1u, ushort.MaxValue),
                nToPage = (ushort)Math.Clamp(_maxPage, 1u, ushort.MaxValue),
                nMinPage = (ushort)Math.Clamp(_minPage, 1u, ushort.MaxValue),
                nMaxPage = (ushort)Math.Clamp(_maxPage, 1u, ushort.MaxValue),
                nCopies = 1
            };

            var fn = (delegate* unmanaged[Stdcall]<PRINTDLGW*, int>)pPrintDlg;
            int res = fn(&pd);
            if (res == 0 || pd.hDC == IntPtr.Zero)
                return false;

            if (_printerDc != IntPtr.Zero)
            {
                DeletePrinterDc(_printerDc);
            }
            _printerDc = pd.hDC;

            if (NativeLibrary.TryLoad("gdi32.dll", out IntPtr gdi32) &&
                NativeLibrary.TryGetExport(gdi32, "GetDeviceCaps", out IntPtr pGetDeviceCaps))
            {
                var getCaps = (delegate* unmanaged[Stdcall]<IntPtr, int, int>)pGetDeviceCaps;
                int pxW = getCaps(_printerDc, 8);   // HORZRES
                int pxH = getCaps(_printerDc, 10);  // VERTRES
                int dpiX = Math.Max(96, getCaps(_printerDc, 88)); // LOGPIXELSX
                int dpiY = Math.Max(96, getCaps(_printerDc, 90)); // LOGPIXELSY
                if (pxW > 0 && pxH > 0)
                {
                    _printableAreaWidth = pxW * 96.0 / dpiX;
                    _printableAreaHeight = pxH * 96.0 / dpiY;
                }
            }

            return true;
        }

        public void PrintVisual(Visual visual, string description)
        {
            ArgumentNullException.ThrowIfNull(visual);
            if (_printerDc == IntPtr.Zero) return;

            try
            {
                RenderVisualToPrinterDc(_printerDc, new[] { visual }, description ?? "CSUI Document", (int)Math.Ceiling(_printableAreaWidth), (int)Math.Ceiling(_printableAreaHeight));
            }
            finally
            {
                DeletePrinterDc(_printerDc);
                _printerDc = IntPtr.Zero;
            }
        }

        public void PrintDocument(DocumentPaginator documentPaginator, string description)
        {
            ArgumentNullException.ThrowIfNull(documentPaginator);

            documentPaginator.ComputePageCount();
            int count = documentPaginator.PageCount;
            var visuals = new System.Collections.Generic.List<Visual>(count);
            for (int i = 0; i < count; i++)
            {
                DocumentPage page = documentPaginator.GetPage(i);
                if (page?.Visual != null)
                {
                    visuals.Add(page.Visual);
                }
            }

            if (_printerDc != IntPtr.Zero && visuals.Count > 0)
            {
                try
                {
                    RenderVisualToPrinterDc(_printerDc, visuals, description ?? "CSUI Document", (int)Math.Ceiling(_printableAreaWidth), (int)Math.Ceiling(_printableAreaHeight));
                }
                finally
                {
                    DeletePrinterDc(_printerDc);
                    _printerDc = IntPtr.Zero;
                }
            }
        }

        private IntPtr _printerDc;

        private static unsafe void DeletePrinterDc(IntPtr hdc)
        {
            if (hdc != IntPtr.Zero &&
                NativeLibrary.TryLoad("gdi32.dll", out IntPtr gdi32) &&
                NativeLibrary.TryGetExport(gdi32, "DeleteDC", out IntPtr pDeleteDc))
            {
                ((delegate* unmanaged[Stdcall]<IntPtr, int>)pDeleteDc)(hdc);
            }
        }

        private static unsafe void RenderVisualToPrinterDc(IntPtr hdc, System.Collections.Generic.IReadOnlyList<Visual> pages, string docName, int widthDip, int heightDip)
        {
            if (!NativeLibrary.TryLoad("gdi32.dll", out IntPtr gdi32)) return;
            if (!NativeLibrary.TryGetExport(gdi32, "StartDocW", out IntPtr pStartDoc)) return;
            if (!NativeLibrary.TryGetExport(gdi32, "StartPage", out IntPtr pStartPage)) return;
            if (!NativeLibrary.TryGetExport(gdi32, "EndPage", out IntPtr pEndPage)) return;
            if (!NativeLibrary.TryGetExport(gdi32, "EndDoc", out IntPtr pEndDoc)) return;
            if (!NativeLibrary.TryGetExport(gdi32, "StretchDIBits", out IntPtr pStretchDIBits)) return;
            if (!NativeLibrary.TryGetExport(gdi32, "GetDeviceCaps", out IntPtr pGetDeviceCaps)) return;

            var startDoc = (delegate* unmanaged[Stdcall]<IntPtr, DOCINFOW*, int>)pStartDoc;
            var startPage = (delegate* unmanaged[Stdcall]<IntPtr, int>)pStartPage;
            var endPage = (delegate* unmanaged[Stdcall]<IntPtr, int>)pEndPage;
            var endDoc = (delegate* unmanaged[Stdcall]<IntPtr, int>)pEndDoc;
            var stretchDiBits = (delegate* unmanaged[Stdcall]<IntPtr, int, int, int, int, int, int, int, int, byte*, BITMAPINFOHEADER*, uint, uint, int>)pStretchDIBits;
            var getCaps = (delegate* unmanaged[Stdcall]<IntPtr, int, int>)pGetDeviceCaps;

            int devW = Math.Max(1, getCaps(hdc, 8));
            int devH = Math.Max(1, getCaps(hdc, 10));
            int w = Math.Max(1, widthDip);
            int h = Math.Max(1, heightDip);

            fixed (char* pName = docName)
            {
                var di = new DOCINFOW
                {
                    cbSize = sizeof(DOCINFOW),
                    lpszDocName = (IntPtr)pName
                };
                if (startDoc(hdc, &di) <= 0) return;

                try
                {
                    byte[] pixels = new byte[w * h * 4];
                    var bmi = new BITMAPINFOHEADER
                    {
                        biSize = (uint)sizeof(BITMAPINFOHEADER),
                        biWidth = w,
                        biHeight = -h, // top-down
                        biPlanes = 1,
                        biBitCount = 32,
                        biCompression = 0 // BI_RGB
                    };

                    for (int i = 0; i < pages.Count; i++)
                    {
                        var rtb = new System.Windows.Media.Imaging.RenderTargetBitmap(w, h, 96.0, 96.0, PixelFormats.Pbgra32);
                        rtb.Render(pages[i]);
                        rtb.CopyPixels(pixels, w * 4, 0);

                        if (startPage(hdc) > 0)
                        {
                            fixed (byte* pPx = pixels)
                            {
                                stretchDiBits(hdc, 0, 0, devW, devH, 0, 0, w, h, pPx, &bmi, 0, 0x00CC0020u /* SRCCOPY */);
                            }
                            endPage(hdc);
                        }
                    }
                }
                finally
                {
                    endDoc(hdc);
                }
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct PRINTDLGW
        {
            public uint lStructSize;
            public IntPtr hwndOwner;
            public IntPtr hDevMode;
            public IntPtr hDevNames;
            public IntPtr hDC;
            public uint Flags;
            public ushort nFromPage;
            public ushort nToPage;
            public ushort nMinPage;
            public ushort nMaxPage;
            public ushort nCopies;
            public IntPtr hInstance;
            public IntPtr lCustData;
            public IntPtr lpfnPrintHook;
            public IntPtr lpfnSetupHook;
            public IntPtr lpPrintTemplateName;
            public IntPtr lpSetupTemplateName;
            public IntPtr hPrintTemplate;
            public IntPtr hSetupTemplate;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct DOCINFOW
        {
            public int cbSize;
            public IntPtr lpszDocName;
            public IntPtr lpszOutput;
            public IntPtr lpszDatatype;
            public uint fwType;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct BITMAPINFOHEADER
        {
            public uint biSize;
            public int biWidth;
            public int biHeight;
            public ushort biPlanes;
            public ushort biBitCount;
            public uint biCompression;
            public uint biSizeImage;
            public int biXPelsPerMeter;
            public int biYPelsPerMeter;
            public uint biClrUsed;
            public uint biClrImportant;
        }
    }
}

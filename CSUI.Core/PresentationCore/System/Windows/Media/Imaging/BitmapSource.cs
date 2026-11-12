// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Interop;
using System.Windows.Ole;
using CSUI.Core.Interop;
using CSUI.Core.Interop.DirectX;
using MS.Internal;
using MS.Internal.PresentationCore;
using MS.Win32;

namespace System.Windows.Media.Imaging
{
    #region BitmapSource

    /// <summary>
    /// Interface for Bitmap Sources, included decoders and effects.
    /// </summary>
    public abstract partial class BitmapSource : ImageSource
    {
        #region Constructor

        static BitmapSource()
        {
            DataObject.IsBitmapSourceHook = static o => o is BitmapSource;
            DataObject.IsBitmapSourceTypeHook = static t => t == typeof(BitmapSource) || typeof(BitmapSource).IsAssignableFrom(t);
            DataObject.CreateBitmapSourceFromHBitmapHook = static hBmp => System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(hBmp, IntPtr.Zero, Int32Rect.Empty, null);
            DataObject.DecodeBitmapSourceFromDibBytesHook = static bytes => DecodeDibBytesForClipboard(bytes);
            DataObject.AllocHBitmapFromBitmapSourceHook = static o => AllocHBitmapForClipboard((BitmapSource)o);
            DataObject.AllocDibHGlobalFromBitmapSourceHook = static o => AllocDibHGlobalForClipboard((BitmapSource)o);
        }

        private static IntPtr AllocDibHGlobalForClipboard(BitmapSource source)
        {
            ArgumentNullException.ThrowIfNull(source);

            BmpBitmapEncoder encoder = new();
            encoder.Frames.Add(BitmapFrame.Create(source));
            using MemoryStream ms = new();
            encoder.Save(ms);

            byte[] bmpBytes = ms.ToArray();
            const int BitmapFileHeaderSize = 14;
            if (bmpBytes.Length <= BitmapFileHeaderSize)
            {
                throw new InvalidOperationException("Failed to encode bitmap as DIB.");
            }

            return OleComBridge.AllocHGlobalFromBytes(bmpBytes.AsSpan(BitmapFileHeaderSize));
        }

        private static unsafe IntPtr AllocHBitmapForClipboard(BitmapSource source)
        {
            ArgumentNullException.ThrowIfNull(source);

            BitmapSource bgraSource = source.Format == PixelFormats.Bgra32
                ? source
                : new FormatConvertedBitmap(source, PixelFormats.Bgra32, null, 0);

            int width = Math.Max(1, bgraSource.PixelWidth);
            int height = Math.Max(1, bgraSource.PixelHeight);

            BITMAPINFOHEADER bmi = new()
            {
                biSize = (uint)sizeof(BITMAPINFOHEADER),
                biWidth = width,
                biHeight = -height,
                biPlanes = 1,
                biBitCount = 32,
                biCompression = 0,
            };

            IntPtr screenDc = UnsafeNativeMethods.GetDC(new HandleRef(null, IntPtr.Zero));
            void* pBits = null;
            IntPtr hBitmap = UnsafeNativeMethods.CreateDIBSection(screenDc, &bmi, 0, &pBits, IntPtr.Zero, 0);
            if (screenDc != IntPtr.Zero)
            {
                UnsafeNativeMethods.ReleaseDC(new HandleRef(null, IntPtr.Zero), new HandleRef(null, screenDc));
            }

            if (hBitmap == IntPtr.Zero || pBits == null)
            {
                if (hBitmap != IntPtr.Zero)
                {
                    UnsafeNativeMethods.DeleteObject(hBitmap);
                }
                throw new OutOfMemoryException();
            }

            int stride = checked(width * 4);
            int bufferSize = checked(stride * height);
            bgraSource.CopyPixels(Int32Rect.Empty, (IntPtr)pBits, bufferSize, stride);
            return hBitmap;
        }

        private static unsafe BitmapSource DecodeDibBytesForClipboard(ReadOnlySpan<byte> dibBytes)
        {
            if (dibBytes.Length < sizeof(BITMAPINFOHEADER))
            {
                return null;
            }

            BITMAPINFOHEADER header = MemoryMarshal.Read<BITMAPINFOHEADER>(dibBytes);
            if (header.biSize < 12 || header.biSize > (uint)dibBytes.Length)
            {
                return null;
            }

            uint paletteCount = header.biClrUsed;
            if (paletteCount == 0 && header.biBitCount <= 8 && header.biBitCount > 0)
            {
                paletteCount = 1u << header.biBitCount;
            }

            uint maskBytes = (header.biSize == 40 && header.biCompression == 3) ? 12u : 0u;
            uint bfOffBits = 14u + header.biSize + maskBytes + (paletteCount * 4u);
            uint bfSize = 14u + (uint)dibBytes.Length;

            byte[] bmpData = new byte[14 + dibBytes.Length];
            bmpData[0] = (byte)'B';
            bmpData[1] = (byte)'M';
            Unsafe.WriteUnaligned(ref bmpData[2], bfSize);
            Unsafe.WriteUnaligned(ref bmpData[6], 0u);
            Unsafe.WriteUnaligned(ref bmpData[10], bfOffBits);
            dibBytes.CopyTo(bmpData.AsSpan(14));

            using MemoryStream ms = new(bmpData);
            BmpBitmapDecoder decoder = new(ms, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
            return decoder.Frames.Count > 0 ? decoder.Frames[0] : null;
        }

        /// <summary>
        /// Create a BitmapSource from an array of pixels.
        /// </summary>
        public static BitmapSource Create(
            int pixelWidth,
            int pixelHeight,
            double dpiX,
            double dpiY,
            PixelFormat pixelFormat,
            Imaging.BitmapPalette palette,
            System.Array pixels,
            int stride
            )
        {
            return new CachedBitmap(
                        pixelWidth, pixelHeight,
                        dpiX, dpiY,
                        pixelFormat, palette,
                        pixels, stride);
        }

        /// <summary>
        /// Create a BitmapSource from an array of pixels in unmanaged memory.
        /// </summary>
        public static unsafe BitmapSource Create(
            int pixelWidth,
            int pixelHeight,
            double dpiX,
            double dpiY,
            PixelFormat pixelFormat,
            Imaging.BitmapPalette palette,
            IntPtr buffer,
            int bufferSize,
            int stride
            )
        {
            return new CachedBitmap(
                        pixelWidth, pixelHeight,
                        dpiX, dpiY,
                        pixelFormat, palette,
                        buffer, bufferSize, stride);
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        protected BitmapSource()
        {
            // Synchronize for *this* object only by default.
            _syncObject = _bitmapInit;
            _isSourceCached = false;
        }

        /// <summary>
        /// Internal Constructor.
        /// useVirtuals: Should properties and methods like PixelWidth and CopyPixels use their "default" implementation.
        /// </summary>
        internal BitmapSource(bool useVirtuals)
        {
            _useVirtuals = true;
            _isSourceCached = false;

            // Synchronize for *this* object only by default.
            _syncObject = _bitmapInit;
        }

        /// <summary>
        /// Creates a copy of this object.
        /// </summary>
        public new BitmapSource Clone()
        {
            return (BitmapSource)base.Clone();
        }

        /// <summary>
        /// Shadows inherited CloneCurrentValue() with a strongly typed version for convenience.
        /// </summary>
        public new BitmapSource CloneCurrentValue()
        {
            return (BitmapSource)base.CloneCurrentValue();
        }

        #endregion Constructor

        #region Public properties and methods

        /// <summary>
        /// Native format of the bitmap's data.
        /// If the BitmapSource is directly readable, this is the format the
        /// pixels will be in when they are read.
        /// </summary>
        public virtual System.Windows.Media.PixelFormat Format
        {
            get
            {
                ReadPreamble();
                EnsureShouldUseVirtuals();
                _bitmapInit.EnsureInitializedComplete();
                CompleteDelayedCreation();

                return _format;
            }
        }

        /// <summary>
        /// Width, in pixels, of the bitmap.
        /// </summary>
        public virtual int PixelWidth
        {
            get
            {
                ReadPreamble();
                EnsureShouldUseVirtuals();
                _bitmapInit.EnsureInitializedComplete();
                CompleteDelayedCreation();

                return _pixelWidth;
            }
        }

        /// <summary>
        /// Height, in pixels, of the bitmap.
        /// </summary>
        public virtual int PixelHeight
        {
            get
            {
                ReadPreamble();
                EnsureShouldUseVirtuals();
                _bitmapInit.EnsureInitializedComplete();
                CompleteDelayedCreation();

                return _pixelHeight;
            }
        }

        /// <summary>
        /// Horizontal DPI of the bitmap.
        /// </summary>
        public virtual double DpiX
        {
            get
            {
                ReadPreamble();
                EnsureShouldUseVirtuals();
                _bitmapInit.EnsureInitializedComplete();
                CompleteDelayedCreation();

                return _dpiX;
            }
        }

        /// <summary>
        /// Vertical DPI of the bitmap.
        /// </summary>
        public virtual double DpiY
        {
            get
            {
                ReadPreamble();
                EnsureShouldUseVirtuals();
                _bitmapInit.EnsureInitializedComplete();
                CompleteDelayedCreation();

                return _dpiY;
            }
        }

        /// <summary>
        /// Retrieve the bitmap palette.
        /// </summary>
        public virtual Imaging.BitmapPalette Palette
        {
            get
            {
                ReadPreamble();
                EnsureShouldUseVirtuals();
                _bitmapInit.EnsureInitializedComplete();
                CompleteDelayedCreation();

                if (_palette == null)
                {
                    if (_format.Palettized)
                    {
                        _palette = Imaging.BitmapPalette.CreateFromBitmapSource(this);
                    }
                }

                return _palette;
            }
        }

        /// <summary>
        /// Returns true if the BitmapSource is downloading content.
        /// </summary>
        public virtual bool IsDownloading
        {
            get
            {
                ReadPreamble();
                return false;
            }
        }

        /// <summary>
        /// Raised when downloading content is done.
        /// </summary>
        public virtual event EventHandler DownloadCompleted
        {
            add
            {
                WritePreamble();
                _downloadEvent.AddEvent(value);
            }
            remove
            {
                WritePreamble();
                _downloadEvent.RemoveEvent(value);
            }
        }

        /// <summary>
        /// Raised when download has progressed.
        /// </summary>
        public virtual event EventHandler<DownloadProgressEventArgs> DownloadProgress
        {
            add
            {
                WritePreamble();
                _progressEvent.AddEvent(value);
            }
            remove
            {
                WritePreamble();
                _progressEvent.RemoveEvent(value);
            }
        }

        /// <summary>
        /// Raised when download has failed.
        /// </summary>
        public virtual event EventHandler<ExceptionEventArgs> DownloadFailed
        {
            add
            {
                WritePreamble();
                _failedEvent.AddEvent(value);
            }
            remove
            {
                WritePreamble();
                _failedEvent.RemoveEvent(value);
            }
        }

        /// <summary>
        /// Raised when decoding has failed.
        /// </summary>
        public virtual event EventHandler<ExceptionEventArgs> DecodeFailed
        {
            add
            {
                WritePreamble();
                EnsureShouldUseVirtuals();
                _decodeFailedEvent.AddEvent(value);
            }
            remove
            {
                WritePreamble();
                EnsureShouldUseVirtuals();
                _decodeFailedEvent.RemoveEvent(value);
            }
        }

        /// <summary>
        /// Copy the pixel data from the bitmap into the array of pixels that
        /// has the specified stride, starting at the offset.
        /// An empty rect (all 0s) will copy the entire bitmap.
        /// </summary>
        public virtual void CopyPixels(Int32Rect sourceRect, Array pixels, int stride, int offset)
        {
            EnsureShouldUseVirtuals();
            CheckIfSiteOfOrigin();
            CriticalCopyPixels(sourceRect, pixels, stride, offset);
        }

        /// <summary>
        /// Copy the pixel data from the bitmap into the array of pixels that
        /// has the specified stride, starting at the offset.
        /// </summary>
        public virtual void CopyPixels(Array pixels, int stride, int offset)
        {
            Int32Rect sourceRect = Int32Rect.Empty;
            EnsureShouldUseVirtuals();
            CheckIfSiteOfOrigin();
            CopyPixels(sourceRect, pixels, stride, offset);
        }

        /// <summary>
        /// Copy the pixel data from the bitmap into the unmanaged buffer that
        /// has the specified stride. An empty rect copies the entire bitmap.
        /// </summary>
        public virtual void CopyPixels(Int32Rect sourceRect, IntPtr buffer, int bufferSize, int stride)
        {
            ReadPreamble();
            EnsureShouldUseVirtuals();
            _bitmapInit.EnsureInitializedComplete();
            CompleteDelayedCreation();
            CheckIfSiteOfOrigin();

            CriticalCopyPixels(sourceRect, buffer, (uint)bufferSize, stride);
        }

        /// <summary>
        /// Get the width of the bitmap in measure units (96ths of an inch).
        /// </summary>
        public override double Width
        {
            get
            {
                ReadPreamble();
                return GetWidthInternal();
            }
        }

        /// <summary>
        /// Get the height of the bitmap in measure units (96ths of an inch).
        /// </summary>
        public override double Height
        {
            get
            {
                ReadPreamble();
                return GetHeightInternal();
            }
        }

        /// <summary>
        /// Get the Metadata of the bitmap.
        /// </summary>
        public override ImageMetadata Metadata
        {
            get
            {
                ReadPreamble();
                return null;
            }
        }

        #endregion Public properties and methods

        #region Internal, Protected and Private properties and methods

        private double GetWidthInternal()
        {
            return ImageSource.PixelsToDIPs(DpiX, PixelWidth);
        }

        private double GetHeightInternal()
        {
            return ImageSource.PixelsToDIPs(DpiY, PixelHeight);
        }

        /// <summary>
        /// Get the Size for the bitmap.
        /// </summary>
        internal override Size Size
        {
            get
            {
                ReadPreamble();
                return new Size(Math.Max(0, GetWidthInternal()),
                                Math.Max(0, GetHeightInternal()));
            }
        }

        internal virtual ReadOnlyCollection<ColorContext> InternalColorContexts => null;

        internal bool DelayCreation
        {
            get => _delayCreation;
            set
            {
                _delayCreation = value;
                if (_delayCreation)
                {
                    CreationCompleted = false;
                }
            }
        }

        internal bool CreationCompleted
        {
            get => _creationComplete;
            set => _creationComplete = value;
        }

        /// <summary>
        /// Demand that the bitmap should be created if it was delay-created.
        /// </summary>
        internal void CompleteDelayedCreation()
        {
            if (DelayCreation)
            {
                lock (_syncObject)
                {
                    if (DelayCreation)
                    {
                        EnsureShouldUseVirtuals();

                        DelayCreation = false;

                        try
                        {
                            FinalizeCreation();
                        }
                        catch
                        {
                            DelayCreation = true;
                            throw;
                        }

                        CreationCompleted = true;
                    }
                }
            }
        }

        internal virtual void FinalizeCreation()
        {
            throw new NotImplementedException();
        }

        private void EnsureShouldUseVirtuals()
        {
            if (!_useVirtuals)
            {
                throw new NotImplementedException();
            }
        }

        internal object SyncObject
        {
            get
            {
                Debug.Assert(_syncObject != null);
                return _syncObject;
            }
        }

        internal bool IsSourceCached
        {
            get => _isSourceCached;
            set => _isSourceCached = value;
        }

        internal unsafe BitmapSourceSafeMILHandle WicSourceHandle
        {
            get
            {
                CompleteDelayedCreation();
                if (_wicSource == null || _wicSource.IsInvalid)
                {
                    _wicSource = ManagedBitmapSource.CreateNativeHandle(this);
                }

                return _wicSource;
            }
            set
            {
                if (value != null)
                {
                    bool addRef = false;
                    value.DangerousAddRef(ref addRef);
                    try
                    {
                        void* wicSource = null;
                        Guid uuidWicBitmapSource = WicGuids.IID_IWICBitmapSource;
                        HRESULT.Check(ComPtr<byte>.QueryInterface(
                            (void*)value.DangerousGetHandle(),
                            in uuidWicBitmapSource,
                            &wicSource));

                        _wicSource = new BitmapSourceSafeMILHandle((IntPtr)wicSource, value);
                        UpdateCachedSettings();
                        InvalidateD2DSource();
                    }
                    finally
                    {
                        if (addRef)
                        {
                            value.DangerousRelease();
                        }
                    }
                }
                else
                {
                    _wicSource = null;
                    InvalidateD2DSource();
                }
            }
        }

        /// <summary>
        /// Update local variables from the unmanaged resource.
        /// </summary>
        internal virtual unsafe void UpdateCachedSettings()
        {
            EnsureShouldUseVirtuals();

            uint pw, ph;

            lock (_syncObject)
            {
                _format = PixelFormat.GetPixelFormat(_wicSource);

                bool addRef = false;
                _wicSource.DangerousAddRef(ref addRef);
                try
                {
                    void* pWic = (void*)_wicSource.DangerousGetHandle();
                    HRESULT.Check(IWICBitmapSourceVtbl.GetSize(pWic, out pw, out ph));
                    HRESULT.Check(IWICBitmapSourceVtbl.GetResolution(pWic, out _dpiX, out _dpiY));
                }
                finally
                {
                    if (addRef)
                    {
                        _wicSource.DangerousRelease();
                    }
                }
            }

            _pixelWidth = (int)pw;
            _pixelHeight = (int)ph;
        }

        internal unsafe void CriticalCopyPixels(Int32Rect sourceRect, Array pixels, int stride, int offset)
        {
            ReadPreamble();
            _bitmapInit.EnsureInitializedComplete();
            CompleteDelayedCreation();

            ArgumentNullException.ThrowIfNull(pixels);

            if (pixels.Rank != 1)
                throw new ArgumentException(SR.Collection_BadRank, nameof(pixels));

            if (offset < 0)
            {
                HRESULT.Check((int)WinCodecErrors.WINCODEC_ERR_VALUEOVERFLOW);
            }

            int elementSize = -1;

            if (pixels is byte[])
                elementSize = 1;
            else if (pixels is short[] || pixels is ushort[])
                elementSize = 2;
            else if (pixels is int[] || pixels is uint[] || pixels is float[])
                elementSize = 4;
            else if (pixels is double[])
                elementSize = 8;

            if (elementSize == -1)
                throw new ArgumentException(SR.Image_InvalidArrayForPixel);

            uint destBufferSize = checked((uint)elementSize * (uint)(pixels.Length - offset));

            if (offset >= pixels.Length)
                throw new IndexOutOfRangeException();

            fixed (byte* pixelArray = &Unsafe.AddByteOffset(ref MemoryMarshal.GetArrayDataReference(pixels), (nint)offset * elementSize))
                CriticalCopyPixels(sourceRect, (nint)pixelArray, destBufferSize, stride);
        }

        internal unsafe void CriticalCopyPixels(Int32Rect sourceRect, IntPtr buffer, uint bufferSize, int stride)
        {
            if (buffer == IntPtr.Zero)
                throw new ArgumentNullException(nameof(buffer));

            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(stride);

            if (sourceRect.Width <= 0)
                sourceRect.Width = PixelWidth;

            if (sourceRect.Height <= 0)
                sourceRect.Height = PixelHeight;

            ArgumentOutOfRangeException.ThrowIfGreaterThan(sourceRect.Width, PixelWidth, "sourceRect.Width");
            ArgumentOutOfRangeException.ThrowIfGreaterThan(sourceRect.Height, PixelHeight, "sourceRect.Height");

            int minStride = checked(((sourceRect.Width * Format.BitsPerPixel) + 7) / 8);
            ArgumentOutOfRangeException.ThrowIfLessThan(stride, minStride);

            uint minRequiredDestSize = checked(((uint)stride * (uint)(sourceRect.Height - 1)) + (uint)minStride);
            ArgumentOutOfRangeException.ThrowIfLessThan(bufferSize, minRequiredDestSize);

            WICRect wicRect = new WICRect(sourceRect.X, sourceRect.Y, sourceRect.Width, sourceRect.Height);

            lock (_syncObject)
            {
                SafeMILHandle wicHandle = WicSourceHandle;
                bool addRef = false;
                wicHandle.DangerousAddRef(ref addRef);
                try
                {
                    HRESULT.Check(IWICBitmapSourceVtbl.CopyPixels(
                        (void*)wicHandle.DangerousGetHandle(),
                        &wicRect,
                        (uint)stride,
                        bufferSize,
                        (byte*)buffer));
                }
                finally
                {
                    if (addRef)
                    {
                        wicHandle.DangerousRelease();
                    }
                }
            }
        }

        protected void CheckIfSiteOfOrigin()
        {
            string uri = null;

            if (CanSerializeToString())
            {
                uri = ConvertToString(null, null);
            }
        }

        internal void RecoverFromDecodeFailure(Exception e)
        {
            OnDecodeFailed(this, new ExceptionEventArgs(e));
        }

        /// <summary>
        /// Returns the closest format that is supported by the rendering engine.
        /// </summary>
        internal static PixelFormat GetClosestDUCEFormat(PixelFormat format, BitmapPalette palette)
        {
            int i = Array.IndexOf(s_supportedDUCEFormats, format);

            if (i != -1)
            {
                return s_supportedDUCEFormats[i];
            }

            int bitsPerPixel = format.InternalBitsPerPixel;

            if (bitsPerPixel == 1)
            {
                return PixelFormats.Indexed1;
            }
            else if (bitsPerPixel == 2)
            {
                return PixelFormats.Indexed2;
            }
            else if (bitsPerPixel <= 4)
            {
                return PixelFormats.Indexed4;
            }
            else if (bitsPerPixel <= 8)
            {
                return PixelFormats.Indexed8;
            }
            else if (bitsPerPixel <= 16 && format.Format != PixelFormatEnum.Gray16)
            {
                return PixelFormats.Bgr555;
            }
            else if (format.HasAlpha || BitmapPalette.DoesPaletteHaveAlpha(palette))
            {
                return PixelFormats.Pbgra32;
            }
            else
            {
                return PixelFormats.Bgr32;
            }
        }

        /// <summary>
        /// Creates a WIC cached bitmap.
        /// </summary>
        internal static unsafe BitmapSourceSafeMILHandle CreateCachedBitmap(
            BitmapSource frame,
            BitmapSourceSafeMILHandle wicSource,
            BitmapCreateOptions createOptions,
            BitmapCacheOption cacheOption,
            BitmapPalette palette
            )
        {
            BitmapSourceSafeMILHandle wicConverter = null;
            BitmapSourceSafeMILHandle wicConvertedSource = null;

            if (cacheOption == BitmapCacheOption.None)
            {
                return wicSource;
            }

            using (FactoryMaker factoryMaker = new FactoryMaker())
            {
                void* wicFactory = (void*)factoryMaker.ImagingFactoryPtr;
                bool changeFormat = false;
                PixelFormat originalFmt = PixelFormats.Pbgra32;

                WICBitmapCreateCacheOptions wicCache = WICBitmapCreateCacheOptions.WICBitmapCacheOnLoad;
                if (cacheOption == BitmapCacheOption.OnDemand)
                {
                    wicCache = WICBitmapCreateCacheOptions.WICBitmapCacheOnDemand;
                }

                originalFmt = PixelFormat.GetPixelFormat(wicSource);
                PixelFormat destFmt = originalFmt;

                if (0 == (createOptions & BitmapCreateOptions.PreservePixelFormat))
                {
                    if (!IsCompatibleFormat(originalFmt))
                        changeFormat = true;

                    destFmt = BitmapSource.GetClosestDUCEFormat(originalFmt, palette);
                }

                ReadOnlyCollection<ColorContext> colorContexts = frame?.InternalColorContexts;
                if (frame != null &&
                    (createOptions & BitmapCreateOptions.IgnoreColorProfile) == 0 &&
                    colorContexts != null &&
                    colorContexts.Count > 0 &&
                    colorContexts[0] != null &&
                    colorContexts[0].IsValid &&
                    !frame._isColorCorrected &&
                    PixelFormat.GetPixelFormat(wicSource).Format != PixelFormatEnum.Extended)
                {
                    ColorContext destinationColorContext;

                    try
                    {
                        destinationColorContext = new ColorContext(destFmt);
                    }
                    catch (NotSupportedException)
                    {
                        destinationColorContext = null;
                    }

                    if (destinationColorContext != null)
                    {
                        bool conversionSuccess = false;
                        bool badColorContext = false;

                        try
                        {
                            ColorConvertedBitmap colorConvertedBitmap = new ColorConvertedBitmap(
                                frame,
                                colorContexts[0],
                                destinationColorContext,
                                destFmt);

                            wicSource = colorConvertedBitmap.WicSourceHandle;
                            frame._isColorCorrected = true;
                            conversionSuccess = true;
                            changeFormat = false;
                        }
                        catch (NotSupportedException)
                        {
                        }
                        catch (FileFormatException)
                        {
                            badColorContext = true;
                        }

                        if (!conversionSuccess && changeFormat && !badColorContext)
                        {
                            changeFormat = false;

                            FormatConvertedBitmap formatConvertedBitmap = new FormatConvertedBitmap(frame, destFmt, null, 0.0);

                            ColorConvertedBitmap colorConvertedBitmap = new ColorConvertedBitmap(
                                formatConvertedBitmap,
                                colorContexts[0],
                                destinationColorContext,
                                destFmt);

                            wicSource = colorConvertedBitmap.WicSourceHandle;
                            frame._isColorCorrected = true;
                            Debug.Assert(destFmt == colorConvertedBitmap.Format);
                            changeFormat = false;
                        }
                    }
                }

                bool addRefWic = false;
                wicSource.DangerousAddRef(ref addRefWic);
                try
                {
                    void* pWicSource = (void*)wicSource.DangerousGetHandle();

                    if (changeFormat)
                    {
                        Guid fmtDestFmt = destFmt.Guid;
                        void* pConverter = null;
                        HRESULT.Check(WicNativeExports.WICConvertBitmapSource(
                            ref fmtDestFmt,
                            pWicSource,
                            &pConverter));
                        wicConverter = new BitmapSourceSafeMILHandle((IntPtr)pConverter);

                        bool addRefConv = false;
                        wicConverter.DangerousAddRef(ref addRefConv);
                        try
                        {
                            void* pConvertedSource = null;
                            HRESULT.Check(IWICImagingFactoryExVtbl.CreateBitmapFromSource(
                                wicFactory,
                                (void*)wicConverter.DangerousGetHandle(),
                                (uint)wicCache,
                                &pConvertedSource));
                            wicConvertedSource = new BitmapSourceSafeMILHandle((IntPtr)pConvertedSource);
                        }
                        finally
                        {
                            if (addRefConv)
                            {
                                wicConverter.DangerousRelease();
                            }
                        }
                    }
                    else
                    {
                        void* pConvertedSource = null;
                        HRESULT.Check(IWICImagingFactoryExVtbl.CreateBitmapFromSource(
                            wicFactory,
                            pWicSource,
                            (uint)wicCache,
                            &pConvertedSource));
                        wicConvertedSource = new BitmapSourceSafeMILHandle((IntPtr)pConvertedSource);
                    }
                }
                finally
                {
                    if (addRefWic)
                    {
                        wicSource.DangerousRelease();
                    }
                }

                wicConvertedSource.CalculateSize();
            }

            return wicConvertedSource;
        }

        private void OnDecodeFailed(object sender, ExceptionEventArgs e)
        {
            _decodeFailedEvent.InvokeEvents(this, e);
        }

        #region Event handlers for bitmap chains

        private void OnSourceDownloadCompleted(object sender, EventArgs e)
        {
            if (_weakBitmapSourceEventSink != null)
            {
                CleanUpWeakEventSink();

                if (_bitmapInit.IsInitAtLeastOnce &&
                    IsValidForFinalizeCreation(throwIfInvalid: false))
                {
                    try
                    {
                        FinalizeCreation();
                        _needsUpdate = true;
                        InvalidateD2DSource();
                    }
                    catch
                    {
                    }

                    _downloadEvent.InvokeEvents(this, e);
                }
            }
        }

        private void OnSourceDownloadFailed(object sender, ExceptionEventArgs e)
        {
            if (_weakBitmapSourceEventSink != null)
            {
                CleanUpWeakEventSink();
                _failedEvent.InvokeEvents(this, e);
            }
        }

        private void OnSourceDownloadProgress(object sender, DownloadProgressEventArgs e)
        {
            _progressEvent.InvokeEvents(this, e);
        }

        private void CleanUpWeakEventSink()
        {
            _weakBitmapSourceEventSink.EventSource = null;
            _weakBitmapSourceEventSink = null;
        }

        internal void RegisterDownloadEventSource(BitmapSource eventSource)
        {
            if (_weakBitmapSourceEventSink == null)
            {
                _weakBitmapSourceEventSink = new WeakBitmapSourceEventSink(this);
            }
            _weakBitmapSourceEventSink.EventSource = eventSource;
        }

        internal void UnregisterDownloadEventSource()
        {
            if (_weakBitmapSourceEventSink != null)
            {
                CleanUpWeakEventSink();
            }
        }

        internal virtual bool IsValidForFinalizeCreation(bool throwIfInvalid)
        {
            return true;
        }

        #endregion Event handlers for bitmap chains

        internal virtual bool ShouldCloneEventDelegates => true;

        #endregion Internal, Protected and Private properties and methods

        #region Animatable and Freezable

        protected override bool FreezeCore(bool isChecking)
        {
            if (!base.FreezeCore(isChecking))
            {
                return false;
            }

            if (IsDownloading)
            {
                return false;
            }

            return true;
        }

        private void CopyCommon(BitmapSource sourceBitmap)
        {
            _useVirtuals = sourceBitmap._useVirtuals;
            _delayCreation = sourceBitmap.DelayCreation;
            _creationComplete = sourceBitmap.CreationCompleted;
            WicSourceHandle = sourceBitmap.WicSourceHandle;
            _syncObject = sourceBitmap.SyncObject;
            IsSourceCached = sourceBitmap.IsSourceCached;

            if (ShouldCloneEventDelegates)
            {
                if (sourceBitmap._downloadEvent != null)
                {
                    _downloadEvent = sourceBitmap._downloadEvent.Clone();
                }

                if (sourceBitmap._progressEvent != null)
                {
                    _progressEvent = sourceBitmap._progressEvent.Clone();
                }

                if (sourceBitmap._failedEvent != null)
                {
                    _failedEvent = sourceBitmap._failedEvent.Clone();
                }

                if (sourceBitmap._decodeFailedEvent != null)
                {
                    _decodeFailedEvent = sourceBitmap._decodeFailedEvent.Clone();
                }
            }

            _format = sourceBitmap.Format;
            _pixelWidth = sourceBitmap.PixelWidth;
            _pixelHeight = sourceBitmap.PixelHeight;
            _dpiX = sourceBitmap.DpiX;
            _dpiY = sourceBitmap.DpiY;
            _palette = sourceBitmap.Palette;

            if (_weakBitmapSourceEventSink != null &&
                sourceBitmap._weakBitmapSourceEventSink != null)
            {
                sourceBitmap._weakBitmapSourceEventSink.DetachSourceDownloadHandlers(
                    _weakBitmapSourceEventSink.EventSource);
            }
        }

        protected override void CloneCore(Freezable sourceFreezable)
        {
            BitmapSource sourceBitmap = (BitmapSource)sourceFreezable;
            base.CloneCore(sourceFreezable);
            CopyCommon(sourceBitmap);
        }

        protected override void CloneCurrentValueCore(Freezable sourceFreezable)
        {
            BitmapSource sourceBitmap = (BitmapSource)sourceFreezable;
            base.CloneCurrentValueCore(sourceFreezable);
            CopyCommon(sourceBitmap);
        }

        protected override void GetAsFrozenCore(Freezable sourceFreezable)
        {
            BitmapSource sourceBitmap = (BitmapSource)sourceFreezable;
            base.GetAsFrozenCore(sourceFreezable);
            CopyCommon(sourceBitmap);
        }

        protected override void GetCurrentValueAsFrozenCore(Freezable sourceFreezable)
        {
            BitmapSource sourceBitmap = (BitmapSource)sourceFreezable;
            base.GetCurrentValueAsFrozenCore(sourceFreezable);
            CopyCommon(sourceBitmap);
        }

        #endregion Animatable and Freezable

        #region Data Members

        private bool _delayCreation = false;
        private bool _creationComplete = false;
        private bool _useVirtuals = false;
        internal BitmapInitialize _bitmapInit = new BitmapInitialize();

        internal BitmapSourceSafeMILHandle _wicSource = null;

        internal object _syncObject;
        internal bool _isSourceCached;

        internal bool _needsUpdate;
        internal bool _isColorCorrected;

        internal UniqueEventHelper _downloadEvent = new UniqueEventHelper();
        internal UniqueEventHelper<DownloadProgressEventArgs> _progressEvent = new UniqueEventHelper<DownloadProgressEventArgs>();
        internal UniqueEventHelper<ExceptionEventArgs> _failedEvent = new UniqueEventHelper<ExceptionEventArgs>();
        internal UniqueEventHelper<ExceptionEventArgs> _decodeFailedEvent = new UniqueEventHelper<ExceptionEventArgs>();

        internal PixelFormat _format = PixelFormats.Default;
        internal int _pixelWidth = 0;
        internal int _pixelHeight = 0;
        internal double _dpiX = 96.0;
        internal double _dpiY = 96.0;
        internal BitmapPalette _palette = null;

        internal bool UsableWithoutCache => HasCompatibleFormat && _isSourceCached;

        internal bool HasCompatibleFormat => IsCompatibleFormat(Format);

        internal static bool IsCompatibleFormat(PixelFormat format)
        {
            return (Array.IndexOf(s_supportedDUCEFormats, format) != -1);
        }

        private static readonly PixelFormat[] s_supportedDUCEFormats =
            new PixelFormat[13]
            {
                PixelFormats.Indexed1,
                PixelFormats.BlackWhite,
                PixelFormats.Indexed2,
                PixelFormats.Gray2,
                PixelFormats.Indexed4,
                PixelFormats.Gray4,
                PixelFormats.Indexed8,
                PixelFormats.Gray8,
                PixelFormats.Bgr555,
                PixelFormats.Bgr565,
                PixelFormats.Bgr32,
                PixelFormats.Bgra32,
                PixelFormats.Pbgra32
            };

        private WeakBitmapSourceEventSink _weakBitmapSourceEventSink = null;

        #endregion Data Members

        #region NativeAOT IWICBitmapSource CCW

        internal unsafe static class ManagedBitmapSource
        {
            [StructLayout(LayoutKind.Sequential)]
            private struct NativeManagedBitmapSource
            {
                public void** Vtbl;
                public uint RefCount;
                public IntPtr StateHandle;
            }

            private static readonly void** s_vtbl;

            static ManagedBitmapSource()
            {
                s_vtbl = (void**)NativeMemory.AllocZeroed(8, (nuint)sizeof(void*));
                s_vtbl[0] = (delegate* unmanaged[Stdcall]<NativeManagedBitmapSource*, Guid*, void**, int>)&ComQueryInterface;
                s_vtbl[1] = (delegate* unmanaged[Stdcall]<NativeManagedBitmapSource*, uint>)&ComAddRef;
                s_vtbl[2] = (delegate* unmanaged[Stdcall]<NativeManagedBitmapSource*, uint>)&ComRelease;
                s_vtbl[3] = (delegate* unmanaged[Stdcall]<NativeManagedBitmapSource*, uint*, uint*, int>)&ComGetSize;
                s_vtbl[4] = (delegate* unmanaged[Stdcall]<NativeManagedBitmapSource*, Guid*, int>)&ComGetPixelFormat;
                s_vtbl[5] = (delegate* unmanaged[Stdcall]<NativeManagedBitmapSource*, double*, double*, int>)&ComGetResolution;
                s_vtbl[6] = (delegate* unmanaged[Stdcall]<NativeManagedBitmapSource*, void*, int>)&ComCopyPalette;
                s_vtbl[7] = (delegate* unmanaged[Stdcall]<NativeManagedBitmapSource*, WICRect*, uint, uint, byte*, int>)&ComCopyPixels;
            }

            internal static BitmapSourceSafeMILHandle CreateNativeHandle(BitmapSource bitmapSource)
            {
                ArgumentNullException.ThrowIfNull(bitmapSource);

                WeakReference<BitmapSource> weakRef = new WeakReference<BitmapSource>(bitmapSource);
                GCHandle stateHandle = GCHandle.Alloc(weakRef);

                NativeManagedBitmapSource* native = (NativeManagedBitmapSource*)NativeMemory.AllocZeroed((nuint)sizeof(NativeManagedBitmapSource));
                native->Vtbl = s_vtbl;
                native->RefCount = 1;
                native->StateHandle = GCHandle.ToIntPtr(stateHandle);

                return new BitmapSourceSafeMILHandle((IntPtr)native);
            }

            private static bool TryGetBitmapSource(NativeManagedBitmapSource* self, out BitmapSource bitmapSource)
            {
                bitmapSource = null;
                if (self == null || self->StateHandle == IntPtr.Zero)
                {
                    return false;
                }

                WeakReference<BitmapSource> weakRef = GCHandle.FromIntPtr(self->StateHandle).Target as WeakReference<BitmapSource>;
                return weakRef != null && weakRef.TryGetTarget(out bitmapSource);
            }

            [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
            private static int ComQueryInterface(NativeManagedBitmapSource* self, Guid* riid, void** ppvObject)
            {
                if (ppvObject == null)
                {
                    return MS.Internal.Interop.HRESULT.E_POINTER.Code;
                }

                *ppvObject = null;
                if (riid == null)
                {
                    return HRESULT.E_INVALIDARG;
                }

                if (*riid == WicGuidsEx.IID_IUnknown || *riid == WicGuids.IID_IWICBitmapSource)
                {
                    *ppvObject = self;
                    Interlocked.Increment(ref self->RefCount);
                    return HRESULT.S_OK;
                }

                return MS.Internal.Interop.HRESULT.E_NOINTERFACE.Code;
            }

            [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
            private static uint ComAddRef(NativeManagedBitmapSource* self)
            {
                return Interlocked.Increment(ref self->RefCount);
            }

            [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
            private static uint ComRelease(NativeManagedBitmapSource* self)
            {
                uint count = Interlocked.Decrement(ref self->RefCount);
                if (count == 0)
                {
                    if (self->StateHandle != IntPtr.Zero)
                    {
                        GCHandle.FromIntPtr(self->StateHandle).Free();
                        self->StateHandle = IntPtr.Zero;
                    }
                    NativeMemory.Free(self);
                }
                return count;
            }

            [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
            private static int ComGetSize(NativeManagedBitmapSource* self, uint* puiWidth, uint* puiHeight)
            {
                if (puiWidth == null || puiHeight == null)
                {
                    return HRESULT.E_INVALIDARG;
                }

                try
                {
                    if (TryGetBitmapSource(self, out BitmapSource bitmapSource))
                    {
                        *puiWidth = (uint)bitmapSource.PixelWidth;
                        *puiHeight = (uint)bitmapSource.PixelHeight;
                        return HRESULT.S_OK;
                    }

                    *puiWidth = 0;
                    *puiHeight = 0;
                    return HRESULT.E_FAIL;
                }
                catch (Exception ex)
                {
                    return Marshal.GetHRForException(ex);
                }
            }

            [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
            private static int ComGetPixelFormat(NativeManagedBitmapSource* self, Guid* pPixelFormat)
            {
                if (pPixelFormat == null)
                {
                    return HRESULT.E_INVALIDARG;
                }

                try
                {
                    if (TryGetBitmapSource(self, out BitmapSource bitmapSource))
                    {
                        *pPixelFormat = bitmapSource.Format.Guid;
                        return HRESULT.S_OK;
                    }

                    *pPixelFormat = Guid.Empty;
                    return HRESULT.E_FAIL;
                }
                catch (Exception ex)
                {
                    return Marshal.GetHRForException(ex);
                }
            }

            [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
            private static int ComGetResolution(NativeManagedBitmapSource* self, double* pDpiX, double* pDpiY)
            {
                if (pDpiX == null || pDpiY == null)
                {
                    return HRESULT.E_INVALIDARG;
                }

                try
                {
                    if (TryGetBitmapSource(self, out BitmapSource bitmapSource))
                    {
                        *pDpiX = bitmapSource.DpiX;
                        *pDpiY = bitmapSource.DpiY;
                        return HRESULT.S_OK;
                    }

                    *pDpiX = 0.0;
                    *pDpiY = 0.0;
                    return HRESULT.E_FAIL;
                }
                catch (Exception ex)
                {
                    return Marshal.GetHRForException(ex);
                }
            }

            [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
            private static int ComCopyPalette(NativeManagedBitmapSource* self, void* pIPalette)
            {
                if (pIPalette == null)
                {
                    return HRESULT.E_INVALIDARG;
                }

                try
                {
                    if (!TryGetBitmapSource(self, out BitmapSource bitmapSource))
                    {
                        return HRESULT.E_FAIL;
                    }

                    BitmapPalette palette = bitmapSource.Palette;
                    if (palette == null || palette.InternalPalette == null || palette.InternalPalette.IsInvalid)
                    {
                        return (int)WinCodecErrors.WINCODEC_ERR_PALETTEUNAVAILABLE;
                    }

                    bool addRef = false;
                    palette.InternalPalette.DangerousAddRef(ref addRef);
                    try
                    {
                        HRESULT.Check(IWICPaletteVtbl.InitializeFromPalette(
                            pIPalette,
                            (void*)palette.InternalPalette.DangerousGetHandle()));
                    }
                    finally
                    {
                        if (addRef)
                        {
                            palette.InternalPalette.DangerousRelease();
                        }
                    }

                    return HRESULT.S_OK;
                }
                catch (Exception ex)
                {
                    return Marshal.GetHRForException(ex);
                }
            }

            [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
            private static int ComCopyPixels(
                NativeManagedBitmapSource* self,
                WICRect* prc,
                uint cbStride,
                uint cbBufferSize,
                byte* pbPixels)
            {
                if (pbPixels == null || cbStride > int.MaxValue || cbBufferSize > int.MaxValue)
                {
                    return HRESULT.E_INVALIDARG;
                }

                try
                {
                    if (!TryGetBitmapSource(self, out BitmapSource bitmapSource))
                    {
                        return HRESULT.E_FAIL;
                    }

                    Int32Rect rc = prc == null
                        ? new Int32Rect(0, 0, bitmapSource.PixelWidth, bitmapSource.PixelHeight)
                        : new Int32Rect(prc->X, prc->Y, prc->Width, prc->Height);

                    if (rc.Width < 1 || rc.Height < 1)
                    {
                        return HRESULT.E_INVALIDARG;
                    }

                    PixelFormat pfStruct = bitmapSource.Format;
                    if (pfStruct.Format == PixelFormatEnum.Default ||
                        pfStruct.Format == PixelFormatEnum.Extended)
                    {
                        return (int)WinCodecErrors.WINCODEC_ERR_UNSUPPORTEDPIXELFORMAT;
                    }

                    int rectRowSize = checked((rc.Width * pfStruct.InternalBitsPerPixel + 7) / 8);
                    int strideInt = (int)cbStride;

                    if (strideInt < rectRowSize)
                    {
                        return HRESULT.E_INVALIDARG;
                    }

                    if ((int)cbBufferSize < checked((rc.Height - 1) * strideInt + rectRowSize))
                    {
                        return (int)WinCodecErrors.WINCODEC_ERR_INSUFFICIENTBUFFER;
                    }

                    int arraySize = checked(rc.Height * rectRowSize);
                    byte[] managedArray = new byte[arraySize];

                    bitmapSource.CopyPixels(rc, managedArray, rectRowSize, 0);

                    fixed (byte* srcBase = managedArray)
                    {
                        for (int y = 0; y < rc.Height; y++)
                        {
                            Buffer.MemoryCopy(
                                srcBase + (nint)y * rectRowSize,
                                pbPixels + (nint)y * cbStride,
                                rectRowSize,
                                rectRowSize);
                        }
                    }

                    return HRESULT.S_OK;
                }
                catch (Exception ex)
                {
                    return Marshal.GetHRForException(ex);
                }
            }
        }

        #endregion NativeAOT IWICBitmapSource CCW

        #region WeakBitmapSourceEventSink

        private class WeakBitmapSourceEventSink : WeakReference
        {
            public WeakBitmapSourceEventSink(BitmapSource bitmapSource)
                : base(bitmapSource)
            {
            }

            public void OnSourceDownloadCompleted(object sender, EventArgs e)
            {
                BitmapSource bitmapSource = this.Target as BitmapSource;
                if (null != bitmapSource)
                {
                    bitmapSource.OnSourceDownloadCompleted(bitmapSource, e);
                }
                else
                {
                    DetachSourceDownloadHandlers(EventSource);
                }
            }

            public void OnSourceDownloadFailed(object sender, ExceptionEventArgs e)
            {
                BitmapSource bitmapSource = this.Target as BitmapSource;
                if (null != bitmapSource)
                {
                    bitmapSource.OnSourceDownloadFailed(bitmapSource, e);
                }
                else
                {
                    DetachSourceDownloadHandlers(EventSource);
                }
            }

            public void OnSourceDownloadProgress(object sender, DownloadProgressEventArgs e)
            {
                BitmapSource bitmapSource = this.Target as BitmapSource;
                if (null != bitmapSource)
                {
                    bitmapSource.OnSourceDownloadProgress(bitmapSource, e);
                }
                else
                {
                    DetachSourceDownloadHandlers(EventSource);
                }
            }

            public void DetachSourceDownloadHandlers(BitmapSource source)
            {
                if (source != null && !source.IsFrozen)
                {
                    source.DownloadCompleted -= OnSourceDownloadCompleted;
                    source.DownloadFailed -= OnSourceDownloadFailed;
                    source.DownloadProgress -= OnSourceDownloadProgress;
                }
            }

            public void AttachSourceDownloadHandlers()
            {
                if (_eventSource != null && !_eventSource.IsFrozen)
                {
                    _eventSource.DownloadCompleted += OnSourceDownloadCompleted;
                    _eventSource.DownloadFailed += OnSourceDownloadFailed;
                    _eventSource.DownloadProgress += OnSourceDownloadProgress;
                }
            }

            private BitmapSource _eventSource;
            public BitmapSource EventSource
            {
                get => _eventSource;
                set
                {
                    if (_eventSource != null)
                    {
                        DetachSourceDownloadHandlers(_eventSource);
                    }

                    _eventSource = value;

                    if (_eventSource != null)
                    {
                        AttachSourceDownloadHandlers();
                    }
                }
            }
        }

        internal override bool TryCopyIconPixelsBgra32(out int pixelWidth, out int pixelHeight, out byte[] bgra32Pixels)
        {
            if (PixelWidth <= 0 || PixelHeight <= 0)
            {
                pixelWidth = 0;
                pixelHeight = 0;
                bgra32Pixels = null;
                return false;
            }

            BitmapSource source = this;
            if (source.Format != PixelFormats.Bgra32 && source.Format != PixelFormats.Pbgra32)
            {
                source = new FormatConvertedBitmap(source, PixelFormats.Bgra32, null, 0);
            }

            pixelWidth = source.PixelWidth;
            pixelHeight = source.PixelHeight;
            int stride = pixelWidth * 4;
            bgra32Pixels = new byte[pixelHeight * stride];
            source.CopyPixels(bgra32Pixels, stride, 0);
            return true;
        }

        #endregion WeakBitmapSourceEventSink
    }

    #endregion BitmapSource
}

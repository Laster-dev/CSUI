// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System.Diagnostics;
using System.Runtime.InteropServices;
using CSUI.Core.Interop.DirectX;
using MS.Internal;
using MS.Internal.PresentationCore;

namespace System.Windows.Media.Imaging
{
    /// <summary>
    /// WriteableBitmap provides an efficient mechanism for updating a system-memory bitmap.
    /// </summary>
    public sealed unsafe class WriteableBitmap : BitmapSource
    {
        #region Constructors

        internal WriteableBitmap()
        {
        }

        public WriteableBitmap(BitmapSource source)
            : base(true)
        {
            InitFromBitmapSource(source);
        }

        public WriteableBitmap(
            int pixelWidth,
            int pixelHeight,
            double dpiX,
            double dpiY,
            PixelFormat pixelFormat,
            BitmapPalette palette
            )
            : base(true)
        {
            BeginInit();

            if (pixelFormat.Palettized && palette == null)
            {
                throw new InvalidOperationException(SR.Image_IndexedPixelFormatRequiresPalette);
            }

            if (pixelFormat.Format == PixelFormatEnum.Extended)
            {
                throw new ArgumentException(SR.Effect_PixelFormat, nameof(pixelFormat));
            }

            if (pixelWidth < 0)
            {
                HRESULT.Check((int)WinCodecErrors.WINCODEC_ERR_VALUEOVERFLOW);
            }

            if (pixelWidth == 0)
            {
                HRESULT.Check(HRESULT.E_INVALIDARG);
            }

            if (pixelHeight < 0)
            {
                HRESULT.Check((int)WinCodecErrors.WINCODEC_ERR_VALUEOVERFLOW);
            }

            if (pixelHeight == 0)
            {
                HRESULT.Check(HRESULT.E_INVALIDARG);
            }

            CreateWritableWicBitmap(pixelWidth, pixelHeight, dpiX, dpiY, pixelFormat, palette);

            // Momentarily lock to populate BackBuffer and BackBufferStride.
            _hasDirtyRects = false;
            Lock();
            Unlock();

            EndInit();
        }

        #endregion Constructors

        #region Public Methods

        public void AddDirtyRect(Int32Rect dirtyRect)
        {
            WritePreamble();

            if (_lockCount == 0)
            {
                throw new InvalidOperationException(SR.Image_MustBeLocked);
            }

            dirtyRect.ValidateForDirtyRect(nameof(dirtyRect), _pixelWidth, _pixelHeight);
            if (dirtyRect.HasArea)
            {
                _hasDirtyRects = true;
            }
        }

        public new WriteableBitmap Clone()
        {
            return (WriteableBitmap)base.Clone();
        }

        public new WriteableBitmap CloneCurrentValue()
        {
            return (WriteableBitmap)base.CloneCurrentValue();
        }

        public void Lock()
        {
            bool locked = TryLock(Duration.Forever);
            Debug.Assert(locked);
        }

        public bool TryLock(Duration timeout)
        {
            WritePreamble();

            TimeSpan timeoutSpan;
            if (timeout == Duration.Automatic)
            {
                throw new ArgumentOutOfRangeException(nameof(timeout));
            }
            else if (timeout == Duration.Forever)
            {
                timeoutSpan = TimeSpan.FromMilliseconds(-1);
            }
            else
            {
                timeoutSpan = timeout.TimeSpan;
            }

            if (_lockCount == uint.MaxValue)
            {
                throw new InvalidOperationException(SR.Image_LockCountLimit);
            }

            if (_lockCount == 0)
            {
                if (!AcquireBackBuffer(timeoutSpan, true))
                {
                    return false;
                }

                WICRect rect = new WICRect(0, 0, _pixelWidth, _pixelHeight);
                bool addRefWic = false;
                _pBackBuffer.DangerousAddRef(ref addRefWic);
                try
                {
                    void* pLock = null;
                    HRESULT.Check(IWICBitmapVtbl.Lock(
                        (void*)_pBackBuffer.DangerousGetHandle(),
                        &rect,
                        (uint)LockFlags.MIL_LOCK_WRITE,
                        &pLock));
                    _pBackBufferLock = new SafeMILHandle((IntPtr)pLock);
                }
                finally
                {
                    if (addRefWic)
                    {
                        _pBackBuffer.DangerousRelease();
                    }
                }

                if (_backBuffer == IntPtr.Zero)
                {
                    bool addRefLock = false;
                    _pBackBufferLock.DangerousAddRef(ref addRefLock);
                    try
                    {
                        void* pLock = (void*)_pBackBufferLock.DangerousGetHandle();
                        HRESULT.Check(IWICBitmapLockVtbl.GetDataPointer(
                            pLock,
                            out uint lockBufferSize,
                            out byte* tempBackBufferPointer));
                        _backBufferSize = lockBufferSize;
                        BackBuffer = (IntPtr)tempBackBufferPointer;

                        HRESULT.Check(IWICBitmapLockVtbl.GetStride(
                            pLock,
                            out uint lockBufferStride));
                        Invariant.Assert(lockBufferStride <= int.MaxValue);
                        _backBufferStride = (int)lockBufferStride;
                    }
                    finally
                    {
                        if (addRefLock)
                        {
                            _pBackBufferLock.DangerousRelease();
                        }
                    }
                }
            }

            _lockCount++;
            return true;
        }

        public void Unlock()
        {
            WritePreamble();

            if (_lockCount == 0)
            {
                throw new InvalidOperationException(SR.Image_MustBeLocked);
            }
            Invariant.Assert(_lockCount > 0, "Lock count should never be negative!");

            _lockCount--;
            if (_lockCount == 0)
            {
                _pBackBufferLock?.Dispose();
                _pBackBufferLock = null;

                if (_hasDirtyRects)
                {
                    InvalidateD2DSource();
                    _hasDirtyRects = false;
                    WritePostscript();
                }
            }
        }

        public void WritePixels(
            Int32Rect sourceRect,
            IntPtr sourceBuffer,
            int sourceBufferSize,
            int sourceBufferStride,
            int destinationX,
            int destinationY
            )
        {
            WritePreamble();

            WritePixelsImpl(sourceRect,
                            sourceBuffer,
                            (uint)sourceBufferSize,
                            sourceBufferStride,
                            destinationX,
                            destinationY,
                            backwardsCompat: false);
        }

        public void WritePixels(
            Int32Rect sourceRect,
            Array sourceBuffer,
            int sourceBufferStride,
            int destinationX,
            int destinationY
            )
        {
            WritePreamble();

            ValidateArrayAndGetInfo(sourceBuffer,
                                    backwardsCompat: false,
                                    out _,
                                    out uint sourceBufferSize,
                                    out bool isValueType);

            if (!isValueType)
            {
                throw new ArgumentException(SR.Image_InvalidArrayForPixel);
            }

            fixed (byte* buffer = &MemoryMarshal.GetArrayDataReference(sourceBuffer))
            {
                WritePixelsImpl(sourceRect,
                                (nint)buffer,
                                sourceBufferSize,
                                sourceBufferStride,
                                destinationX,
                                destinationY,
                                backwardsCompat: false);
            }
        }

        public void WritePixels(
            Int32Rect sourceRect,
            IntPtr buffer,
            int bufferSize,
            int stride
            )
        {
            WritePreamble();

            ArgumentOutOfRangeException.ThrowIfZero(bufferSize);
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(stride);

            if (sourceRect.IsEmpty || sourceRect.Width <= 0 || sourceRect.Height <= 0)
            {
                return;
            }

            int destinationX = sourceRect.X;
            int destinationY = sourceRect.Y;
            sourceRect.X = 0;
            sourceRect.Y = 0;

            WritePixelsImpl(sourceRect,
                            buffer,
                            (uint)bufferSize,
                            stride,
                            destinationX,
                            destinationY,
                            backwardsCompat: true);
        }

        public void WritePixels(
            Int32Rect sourceRect,
            Array pixels,
            int stride,
            int offset
            )
        {
            WritePreamble();

            if (sourceRect.IsEmpty || sourceRect.Width <= 0 || sourceRect.Height <= 0)
            {
                return;
            }

            ValidateArrayAndGetInfo(pixels,
                                    backwardsCompat: true,
                                    out int elementSize,
                                    out uint sourceBufferSize,
                                    out bool isValueType);

            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(stride);
            ArgumentOutOfRangeException.ThrowIfNegative(offset);

            if (!isValueType)
            {
                throw new ArgumentException(SR.Image_InvalidArrayForPixel);
            }

            checked
            {
                uint offsetInBytes = (uint)offset * (uint)elementSize;
                if (offsetInBytes >= sourceBufferSize)
                {
                    throw new IndexOutOfRangeException();
                }

                int destinationX = sourceRect.X;
                int destinationY = sourceRect.Y;
                sourceRect.X = 0;
                sourceRect.Y = 0;

                fixed (byte* buffer = &MemoryMarshal.GetArrayDataReference(pixels))
                {
                    nint adjustedBuffer = (nint)(buffer + offsetInBytes);
                    sourceBufferSize -= offsetInBytes;

                    WritePixelsImpl(sourceRect,
                                    adjustedBuffer,
                                    sourceBufferSize,
                                    stride,
                                    destinationX,
                                    destinationY,
                                    backwardsCompat: true);
                }
            }
        }

        #endregion Public Methods

        #region Protected Methods

        protected override Freezable CreateInstanceCore()
        {
            return new WriteableBitmap();
        }

        protected override void CloneCore(Freezable sourceFreezable)
        {
            WriteableBitmap sourceBitmap = (WriteableBitmap)sourceFreezable;
            base.CloneCore(sourceFreezable);
            CopyCommon(sourceBitmap);
        }

        protected override bool FreezeCore(bool isChecking)
        {
            bool canFreeze = (_lockCount == 0) && base.FreezeCore(isChecking);

            if (canFreeze && !isChecking)
            {
                Debug.Assert(_pBackBufferLock == null);

                if (_hasDirtyRects)
                {
                    InvalidateD2DSource();
                    _hasDirtyRects = false;
                }

                _needsUpdate = true;
            }

            return canFreeze;
        }

        protected override void CloneCurrentValueCore(Freezable sourceFreezable)
        {
            WriteableBitmap sourceBitmap = (WriteableBitmap)sourceFreezable;
            base.CloneCurrentValueCore(sourceFreezable);
            CopyCommon(sourceBitmap);
        }

        protected override void GetAsFrozenCore(Freezable sourceFreezable)
        {
            WriteableBitmap sourceBitmap = (WriteableBitmap)sourceFreezable;
            base.GetAsFrozenCore(sourceFreezable);
            CopyCommon(sourceBitmap);
        }

        protected override void GetCurrentValueAsFrozenCore(Freezable sourceFreezable)
        {
            WriteableBitmap sourceBitmap = (WriteableBitmap)sourceFreezable;
            base.GetCurrentValueAsFrozenCore(sourceFreezable);
            CopyCommon(sourceBitmap);
        }

        #endregion Protected Methods

        #region Private/Internal Methods

        private void CreateWritableWicBitmap(
            int pixelWidth,
            int pixelHeight,
            double dpiX,
            double dpiY,
            PixelFormat pixelFormat,
            BitmapPalette palette)
        {
            Guid formatGuid = pixelFormat.Guid;
            double effDpiX = dpiX < DoubleUtil.DBL_EPSILON ? 96.0 : dpiX;
            double effDpiY = dpiY < DoubleUtil.DBL_EPSILON ? 96.0 : dpiY;

            using (FactoryMaker factoryMaker = new FactoryMaker())
            {
                void* pWicBitmap = null;
                HRESULT.Check(IWICImagingFactoryExVtbl.CreateBitmap(
                    (void*)factoryMaker.ImagingFactoryPtr,
                    (uint)pixelWidth,
                    (uint)pixelHeight,
                    in formatGuid,
                    (uint)WICBitmapCreateCacheOptions.WICBitmapCacheOnLoad,
                    &pWicBitmap));

                BitmapSourceSafeMILHandle wicBitmap = new BitmapSourceSafeMILHandle((IntPtr)pWicBitmap);
                wicBitmap.CalculateSize();

                bool addRef = false;
                wicBitmap.DangerousAddRef(ref addRef);
                try
                {
                    void* pBmp = (void*)wicBitmap.DangerousGetHandle();
                    HRESULT.Check(IWICBitmapVtbl.SetResolution(pBmp, effDpiX, effDpiY));

                    if (pixelFormat.Palettized && palette != null)
                    {
                        SafeMILHandle internalPalette = palette.InternalPalette;
                        if (internalPalette != null && !internalPalette.IsInvalid)
                        {
                            bool addRefPal = false;
                            internalPalette.DangerousAddRef(ref addRefPal);
                            try
                            {
                                HRESULT.Check(IWICBitmapVtbl.SetPalette(
                                    pBmp,
                                    (void*)internalPalette.DangerousGetHandle()));
                            }
                            finally
                            {
                                if (addRefPal)
                                {
                                    internalPalette.DangerousRelease();
                                }
                            }
                        }
                    }
                }
                finally
                {
                    if (addRef)
                    {
                        wicBitmap.DangerousRelease();
                    }
                }

                _pBackBuffer = wicBitmap;
                WicSourceHandle = wicBitmap;
                _syncObject = WicSourceHandle;
            }
        }

        private void InitFromBitmapSource(BitmapSource source)
        {
            ArgumentNullException.ThrowIfNull(source);

            if (source.PixelWidth < 0)
            {
                HRESULT.Check((int)WinCodecErrors.WINCODEC_ERR_VALUEOVERFLOW);
            }

            if (source.PixelHeight < 0)
            {
                HRESULT.Check((int)WinCodecErrors.WINCODEC_ERR_VALUEOVERFLOW);
            }

            BeginInit();

            lock (source.SyncObject)
            {
                CreateWritableWicBitmap(
                    source.PixelWidth,
                    source.PixelHeight,
                    source.DpiX,
                    source.DpiY,
                    source.Format,
                    source.Format.Palettized ? source.Palette : null);

                _hasDirtyRects = false;
                Lock();
                try
                {
                    Int32Rect rcFull = new Int32Rect(0, 0, _pixelWidth, _pixelHeight);
                    uint bufferSize = checked((uint)_backBufferStride * (uint)_pixelHeight);
                    source.CriticalCopyPixels(rcFull, _backBuffer, bufferSize, _backBufferStride);
                    AddDirtyRect(rcFull);
                }
                finally
                {
                    Unlock();
                }
            }

            EndInit();
        }

        private void WritePixelsImpl(
            Int32Rect sourceRect,
            IntPtr sourceBuffer,
            uint sourceBufferSize,
            int sourceBufferStride,
            int destinationX,
            int destinationY,
            bool backwardsCompat
            )
        {
            Debug.Assert(!(backwardsCompat && (sourceRect.X < 0 || sourceRect.Y < 0 || sourceRect.Width < 0 || sourceRect.Height < 0)));
            ArgumentOutOfRangeException.ThrowIfNegative(sourceRect.X, nameof(sourceRect));
            ArgumentOutOfRangeException.ThrowIfNegative(sourceRect.Y, nameof(sourceRect));
            ArgumentOutOfRangeException.ThrowIfNegative(sourceRect.Width, nameof(sourceRect));
            ArgumentOutOfRangeException.ThrowIfNegative(sourceRect.Height, nameof(sourceRect));

            if (!backwardsCompat)
            {
                ArgumentOutOfRangeException.ThrowIfGreaterThan(sourceRect.Width, _pixelWidth, nameof(sourceRect));
                ArgumentOutOfRangeException.ThrowIfGreaterThan(sourceRect.Height, _pixelHeight, nameof(sourceRect));
                ArgumentOutOfRangeException.ThrowIfNegative(destinationX);
                ArgumentOutOfRangeException.ThrowIfNegative(destinationY);
                ArgumentOutOfRangeException.ThrowIfGreaterThan(destinationX, _pixelWidth - sourceRect.Width);
                ArgumentOutOfRangeException.ThrowIfGreaterThan(destinationY, _pixelHeight - sourceRect.Height);
            }
            else if (sourceRect.Width > _pixelWidth || sourceRect.Height > _pixelHeight || destinationX > _pixelWidth - sourceRect.Width || destinationY > _pixelHeight - sourceRect.Height)
            {
                HRESULT.Check(HRESULT.E_INVALIDARG);
            }
            else if (destinationX < 0 || destinationY < 0)
            {
                HRESULT.Check((int)WinCodecErrors.WINCODEC_ERR_VALUEOVERFLOW);
            }

            if (sourceBuffer == IntPtr.Zero)
            {
                throw new ArgumentNullException(backwardsCompat ? "buffer" : "sourceBuffer");
            }

            Debug.Assert(!(backwardsCompat && sourceBufferStride < 1));
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(sourceBufferStride);

            if (sourceRect.Width == 0 || sourceRect.Height == 0)
            {
                Debug.Assert(!backwardsCompat);
                return;
            }

            checked
            {
                uint finalRowWidthInBits = (uint)((sourceRect.X + sourceRect.Width) * _format.InternalBitsPerPixel);
                uint finalRowWidthInBytes = (finalRowWidthInBits + 7) / 8;
                uint requiredBufferSize = ((uint)(sourceRect.Y + sourceRect.Height - 1) * (uint)sourceBufferStride) + finalRowWidthInBytes;
                if (sourceBufferSize < requiredBufferSize)
                {
                    if (backwardsCompat)
                    {
                        HRESULT.Check((int)WinCodecErrors.WINCODEC_ERR_INSUFFICIENTBUFFER);
                    }
                    else
                    {
                        throw new ArgumentException(SR.Image_InsufficientBufferSize, nameof(sourceBufferSize));
                    }
                }

                uint copyWidthInBits = (uint)(sourceRect.Width * _format.InternalBitsPerPixel);

                uint sourceXbyteOffset = (uint)((sourceRect.X * _format.InternalBitsPerPixel) / 8);
                uint sourceBufferBitOffset = (uint)((sourceRect.X * _format.InternalBitsPerPixel) % 8);
                uint firstPixelByteOffet = (uint)((sourceRect.Y * sourceBufferStride) + sourceXbyteOffset);
                uint destXbyteOffset = (uint)((destinationX * _format.InternalBitsPerPixel) / 8);
                uint destBufferBitOffset = (uint)((destinationX * _format.InternalBitsPerPixel) % 8);

                Int32Rect destinationRect = sourceRect;
                destinationRect.X = destinationX;
                destinationRect.Y = destinationY;

                uint destOffset = ((uint)destinationY * (uint)_backBufferStride) + destXbyteOffset;
                byte* pDest = (byte*)_backBuffer.ToPointer() + destOffset;
                uint outputBufferSize = _backBufferSize - destOffset;

                byte* pSource = (byte*)sourceBuffer.ToPointer() + firstPixelByteOffet;
                uint inputBufferSize = sourceBufferSize - firstPixelByteOffet;

                Lock();
                try
                {
                    MILUtilities.MILCopyPixelBuffer(
                        pDest,
                        outputBufferSize,
                        (uint)_backBufferStride,
                        destBufferBitOffset,
                        pSource,
                        inputBufferSize,
                        (uint)sourceBufferStride,
                        sourceBufferBitOffset,
                        (uint)sourceRect.Height,
                        copyWidthInBits);
                    AddDirtyRect(destinationRect);
                }
                finally
                {
                    Unlock();
                }
            }
        }

        private bool AcquireBackBuffer(TimeSpan timeout, bool waitForCopy)
        {
            if (_pBackBuffer == null || _pBackBuffer.IsInvalid)
            {
                return false;
            }
            return true;
        }

        private void CopyCommon(WriteableBitmap sourceBitmap)
        {
            Animatable_IsResourceInvalidationNecessary = false;
            InitFromBitmapSource(sourceBitmap);
            Animatable_IsResourceInvalidationNecessary = true;
        }

        private void BeginInit()
        {
            _bitmapInit.BeginInit();
        }

        private void EndInit()
        {
            _bitmapInit.EndInit();
            FinalizeCreation();
        }

        internal override void FinalizeCreation()
        {
            IsSourceCached = true;
            CreationCompleted = true;
            UpdateCachedSettings();
        }

        private static void ValidateArrayAndGetInfo(
            Array sourceBuffer,
            bool backwardsCompat,
            out int elementSize,
            out uint sourceBufferSize,
            out bool isValueType)
        {
            if (sourceBuffer == null)
            {
                throw new ArgumentNullException(backwardsCompat ? "pixels" : "sourceBuffer");
            }

            int totalLength;
            if (sourceBuffer.Rank == 1)
            {
                totalLength = sourceBuffer.GetLength(0);
            }
            else if (sourceBuffer.Rank == 2)
            {
                totalLength = checked(sourceBuffer.GetLength(0) * sourceBuffer.GetLength(1));
            }
            else
            {
                throw new ArgumentException(SR.Collection_BadRank, backwardsCompat ? "pixels" : "sourceBuffer");
            }

            if (totalLength == 0)
            {
                if (backwardsCompat)
                {
                    elementSize = 1;
                    sourceBufferSize = 0;
                    isValueType = false;
                    return;
                }
                else
                {
                    throw new ArgumentException(SR.Image_InsufficientBuffer, nameof(sourceBuffer));
                }
            }

            elementSize = sourceBuffer switch
            {
                byte[] or byte[,] or sbyte[] or sbyte[,] or bool[] or bool[,] => 1,
                short[] or short[,] or ushort[] or ushort[,] or char[] or char[,] or Half[] or Half[,] => 2,
                int[] or int[,] or uint[] or uint[,] or float[] or float[,] => 4,
                long[] or long[,] or ulong[] or ulong[,] or double[] or double[,] => 8,
                nint[] or nint[,] => sizeof(nint),
                nuint[] or nuint[,] => sizeof(nuint),
                decimal[] or decimal[,] => sizeof(decimal),
                Guid[] or Guid[,] => sizeof(Guid),
                Int32Rect[] or Int32Rect[,] => sizeof(Int32Rect),
                Point[] or Point[,] => sizeof(Point),
                Size[] or Size[,] => sizeof(Size),
                Rect[] or Rect[,] => sizeof(Rect),
                Vector[] or Vector[,] => sizeof(Vector),
                Matrix[] or Matrix[,] => sizeof(Matrix),
                Color[] or Color[,] => sizeof(Color),
                _ => 0
            };

            if (elementSize <= 0)
            {
                elementSize = 0;
                sourceBufferSize = 0;
                isValueType = false;
                return;
            }

            sourceBufferSize = checked((uint)totalLength * (uint)elementSize);
            isValueType = true;
        }

        #endregion Private/Internal Methods

        #region Properties

        public IntPtr BackBuffer
        {
            get
            {
                ReadPreamble();
                return _backBuffer;
            }
            private set
            {
                _backBuffer = value;
            }
        }

        private IntPtr _backBuffer;
        private uint _backBufferSize;

        public int BackBufferStride
        {
            get
            {
                ReadPreamble();
                return _backBufferStride;
            }
        }

        private int _backBufferStride;

        #endregion Properties

        #region Fields

        private SafeMILHandle _pBackBufferLock;
        private BitmapSourceSafeMILHandle _pBackBuffer;
        private uint _lockCount = 0;
        private bool _hasDirtyRects = false;

        #endregion Fields
    }
}

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
    #region RenderTargetBitmap

    /// <summary>
    /// RenderTargetBitmap provides bitmap rendering for Visual subtrees via Direct2D.
    /// </summary>
    public sealed unsafe class RenderTargetBitmap : BitmapSource
    {
        public RenderTargetBitmap(
            int pixelWidth,
            int pixelHeight,
            double dpiX,
            double dpiY,
            PixelFormat pixelFormat
            ) : base(true)
        {
            if (pixelFormat.Format == PixelFormatEnum.Default)
            {
                pixelFormat = PixelFormats.Pbgra32;
            }
            else if (pixelFormat.Format != PixelFormatEnum.Pbgra32)
            {
                throw new ArgumentException(
                    SR.Format(SR.Effect_PixelFormat, pixelFormat),
                    nameof(pixelFormat));
            }

            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(pixelWidth);
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(pixelHeight);

            if (dpiX < DoubleUtil.DBL_EPSILON)
            {
                dpiX = 96.0;
            }

            if (dpiY < DoubleUtil.DBL_EPSILON)
            {
                dpiY = 96.0;
            }

            _bitmapInit.BeginInit();
            _pixelWidth = pixelWidth;
            _pixelHeight = pixelHeight;
            _dpiX = dpiX;
            _dpiY = dpiY;
            _format = pixelFormat;
            _bitmapInit.EndInit();
            FinalizeCreation();
        }

        internal RenderTargetBitmap() : base(true)
        {
        }

        protected override Freezable CreateInstanceCore()
        {
            return new RenderTargetBitmap();
        }

        private void CopyCommon(RenderTargetBitmap sourceBitmap)
        {
            _bitmapInit.BeginInit();
            _pixelWidth = sourceBitmap._pixelWidth;
            _pixelHeight = sourceBitmap._pixelHeight;
            _dpiX = sourceBitmap._dpiX;
            _dpiY = sourceBitmap._dpiY;
            _format = sourceBitmap._format;
            _isClear = sourceBitmap._isClear;

            BitmapSourceSafeMILHandle newBitmapHandle = CreateCachedBitmap(
                null,
                sourceBitmap.WicSourceHandle,
                BitmapCreateOptions.PreservePixelFormat,
                BitmapCacheOption.OnLoad,
                null);

            lock (_syncObject)
            {
                WicSourceHandle = newBitmapHandle;
                _isSourceCached = false;
            }

            _bitmapInit.EndInit();
        }

        protected override void CloneCore(Freezable sourceFreezable)
        {
            RenderTargetBitmap sourceBitmap = (RenderTargetBitmap)sourceFreezable;
            base.CloneCore(sourceFreezable);
            CopyCommon(sourceBitmap);
        }

        protected override void CloneCurrentValueCore(Freezable sourceFreezable)
        {
            RenderTargetBitmap sourceBitmap = (RenderTargetBitmap)sourceFreezable;
            base.CloneCurrentValueCore(sourceFreezable);
            CopyCommon(sourceBitmap);
        }

        protected override void GetAsFrozenCore(Freezable sourceFreezable)
        {
            RenderTargetBitmap sourceBitmap = (RenderTargetBitmap)sourceFreezable;
            base.GetAsFrozenCore(sourceFreezable);
            CopyCommon(sourceBitmap);
        }

        protected override void GetCurrentValueAsFrozenCore(Freezable sourceFreezable)
        {
            RenderTargetBitmap sourceBitmap = (RenderTargetBitmap)sourceFreezable;
            base.GetCurrentValueAsFrozenCore(sourceFreezable);
            CopyCommon(sourceBitmap);
        }

        /// <summary>
        /// Renders the specified Visual tree to the RenderTargetBitmap.
        /// </summary>
        public void Render(Visual visual)
        {
            BitmapVisualManager bmv = new BitmapVisualManager(this);
            bmv.Render(visual);
        }

        /// <summary>
        /// Clears the render target and sets every pixel to black transparent.
        /// </summary>
        public void Clear()
        {
            WritePreamble();

            lock (_syncObject)
            {
                BitmapSourceSafeMILHandle wicBitmap = WicSourceHandle;
                if (wicBitmap != null && !wicBitmap.IsInvalid)
                {
                    WICRect rect = new WICRect(0, 0, _pixelWidth, _pixelHeight);
                    bool addRef = false;
                    wicBitmap.DangerousAddRef(ref addRef);
                    try
                    {
                        void* pLock = null;
                        HRESULT.Check(IWICBitmapVtbl.Lock(
                            (void*)wicBitmap.DangerousGetHandle(),
                            &rect,
                            (uint)LockFlags.MIL_LOCK_WRITE,
                            &pLock));
                        try
                        {
                            HRESULT.Check(IWICBitmapLockVtbl.GetStride(pLock, out uint stride));
                            HRESULT.Check(IWICBitmapLockVtbl.GetDataPointer(pLock, out uint bufSize, out byte* data));

                            uint rowBytes = checked((uint)_pixelWidth * 4u);
                            if (data != null && stride >= rowBytes && (ulong)bufSize >= (ulong)stride * (uint)(_pixelHeight - 1) + rowBytes)
                            {
                                if (stride == rowBytes && (ulong)bufSize >= (ulong)_pixelHeight * stride)
                                {
                                    NativeMemory.Clear(data, (nuint)_pixelHeight * stride);
                                }
                                else
                                {
                                    for (int y = 0; y < _pixelHeight; y++)
                                    {
                                        NativeMemory.Clear(data + (nint)y * stride, rowBytes);
                                    }
                                }
                            }
                        }
                        finally
                        {
                            ComHelpers.Release(pLock);
                        }
                    }
                    finally
                    {
                        if (addRef)
                        {
                            wicBitmap.DangerousRelease();
                        }
                    }
                }

                _isClear = true;
            }

            RenderTargetContentsChanged();
        }

        internal void RenderTargetContentsChanged()
        {
            _isSourceCached = false;
            _needsUpdate = true;
            InvalidateD2DSource();
            FireChanged();
        }

        internal override void FinalizeCreation()
        {
            try
            {
                using (FactoryMaker myFactory = new FactoryMaker())
                {
                    Guid fmtGuid = _format.Guid;
                    void* pWicBitmap = null;
                    HRESULT.Check(IWICImagingFactoryExVtbl.CreateBitmap(
                        (void*)myFactory.ImagingFactoryPtr,
                        (uint)_pixelWidth,
                        (uint)_pixelHeight,
                        in fmtGuid,
                        (uint)WICBitmapCreateCacheOptions.WICBitmapCacheOnLoad,
                        &pWicBitmap));

                    BitmapSourceSafeMILHandle bitmapSource = new BitmapSourceSafeMILHandle((IntPtr)pWicBitmap);
                    Debug.Assert(!bitmapSource.IsInvalid);

                    bool addRef = false;
                    bitmapSource.DangerousAddRef(ref addRef);
                    try
                    {
                        HRESULT.Check(IWICBitmapVtbl.SetResolution(
                            (void*)bitmapSource.DangerousGetHandle(),
                            _dpiX,
                            _dpiY));
                    }
                    finally
                    {
                        if (addRef)
                        {
                            bitmapSource.DangerousRelease();
                        }
                    }

                    lock (_syncObject)
                    {
                        bitmapSource.CalculateSize();
                        _isClear = true;
                        WicSourceHandle = bitmapSource;
                        _isSourceCached = false;
                    }
                }

                CreationCompleted = true;
                UpdateCachedSettings();
            }
            catch
            {
                _bitmapInit.Reset();
                throw;
            }
        }

        internal bool _isClear = true;
    }

    #endregion RenderTargetBitmap
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System.Diagnostics;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using CSUI.Core.Interop.DirectX;
using MS.Internal;

namespace System.Windows.Interop
{
    #region InteropBitmap

    /// <summary>
    /// InteropBitmap provides Win32 HBITMAP/HICON/section interoperability as a BitmapSource.
    /// </summary>
    public sealed unsafe class InteropBitmap : BitmapSource
    {
        private InteropBitmap() : base(true)
        {
        }

        internal InteropBitmap(
            IntPtr hbitmap,
            IntPtr hpalette,
            Int32Rect sourceRect,
            BitmapSizeOptions sizeOptions,
            WICBitmapAlphaChannelOption alphaOptions)
            : base(true)
        {
            _bitmapInit.BeginInit();

            using (FactoryMaker myFactory = new FactoryMaker())
            {
                void* pBitmap = null;
                HRESULT.Check(IWICImagingFactoryExVtbl.CreateBitmapFromHBITMAP(
                    (void*)myFactory.ImagingFactoryPtr,
                    hbitmap,
                    hpalette,
                    (uint)alphaOptions,
                    &pBitmap));
                _unmanagedSource = new BitmapSourceSafeMILHandle((IntPtr)pBitmap);
                Debug.Assert(_unmanagedSource != null && !_unmanagedSource.IsInvalid);
            }

            _unmanagedSource.CalculateSize();
            _sizeOptions = sizeOptions;
            _sourceRect = sourceRect;
            _syncObject = _unmanagedSource;

            _bitmapInit.EndInit();

            FinalizeCreation();
        }

        internal InteropBitmap(IntPtr hicon, Int32Rect sourceRect, BitmapSizeOptions sizeOptions)
            : base(true)
        {
            _bitmapInit.BeginInit();

            using (FactoryMaker myFactory = new FactoryMaker())
            {
                void* pBitmap = null;
                HRESULT.Check(IWICImagingFactoryExVtbl.CreateBitmapFromHICON(
                    (void*)myFactory.ImagingFactoryPtr,
                    hicon,
                    &pBitmap));
                _unmanagedSource = new BitmapSourceSafeMILHandle((IntPtr)pBitmap);
                Debug.Assert(_unmanagedSource != null && !_unmanagedSource.IsInvalid);
            }

            _unmanagedSource.CalculateSize();
            _sourceRect = sourceRect;
            _sizeOptions = sizeOptions;
            _syncObject = _unmanagedSource;

            _bitmapInit.EndInit();

            FinalizeCreation();
        }

        internal InteropBitmap(
            IntPtr section,
            int pixelWidth,
            int pixelHeight,
            PixelFormat format,
            int stride,
            int offset)
            : base(true)
        {
            _bitmapInit.BeginInit();
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(pixelWidth);
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(pixelHeight);

            Guid formatGuid = format.Guid;

            void* pBitmap = null;
            HRESULT.Check(WicNativeExports.WICCreateBitmapFromSection(
                (uint)pixelWidth,
                (uint)pixelHeight,
                ref formatGuid,
                section,
                (uint)stride,
                (uint)offset,
                &pBitmap));
            _unmanagedSource = new BitmapSourceSafeMILHandle((IntPtr)pBitmap);
            Debug.Assert(_unmanagedSource != null && !_unmanagedSource.IsInvalid);

            _unmanagedSource.CalculateSize();
            _sourceRect = Int32Rect.Empty;
            _sizeOptions = null;
            _syncObject = _unmanagedSource;
            _bitmapInit.EndInit();

            FinalizeCreation();
        }

        protected override Freezable CreateInstanceCore()
        {
            return new InteropBitmap();
        }

        private void CopyCommon(InteropBitmap sourceBitmapSource)
        {
            Animatable_IsResourceInvalidationNecessary = false;
            _unmanagedSource = sourceBitmapSource._unmanagedSource;
            _sourceRect = sourceBitmapSource._sourceRect;
            _sizeOptions = sourceBitmapSource._sizeOptions;
            InitFromWICSource(sourceBitmapSource.WicSourceHandle);
            Animatable_IsResourceInvalidationNecessary = true;
        }

        protected override void CloneCore(Freezable sourceFreezable)
        {
            InteropBitmap sourceBitmapSource = (InteropBitmap)sourceFreezable;
            base.CloneCore(sourceFreezable);
            CopyCommon(sourceBitmapSource);
        }

        protected override void CloneCurrentValueCore(Freezable sourceFreezable)
        {
            InteropBitmap sourceBitmapSource = (InteropBitmap)sourceFreezable;
            base.CloneCurrentValueCore(sourceFreezable);
            CopyCommon(sourceBitmapSource);
        }

        protected override void GetAsFrozenCore(Freezable sourceFreezable)
        {
            InteropBitmap sourceBitmapSource = (InteropBitmap)sourceFreezable;
            base.GetAsFrozenCore(sourceFreezable);
            CopyCommon(sourceBitmapSource);
        }

        protected override void GetCurrentValueAsFrozenCore(Freezable sourceFreezable)
        {
            InteropBitmap sourceBitmapSource = (InteropBitmap)sourceFreezable;
            base.GetCurrentValueAsFrozenCore(sourceFreezable);
            CopyCommon(sourceBitmapSource);
        }

        private void InitFromWICSource(SafeMILHandle wicSource)
        {
            _bitmapInit.BeginInit();

            BitmapSourceSafeMILHandle bitmapSource = null;

            lock (_syncObject)
            {
                using (FactoryMaker factoryMaker = new FactoryMaker())
                {
                    bool addRef = false;
                    wicSource.DangerousAddRef(ref addRef);
                    try
                    {
                        void* pBitmap = null;
                        HRESULT.Check(IWICImagingFactoryExVtbl.CreateBitmapFromSource(
                            (void*)factoryMaker.ImagingFactoryPtr,
                            (void*)wicSource.DangerousGetHandle(),
                            (uint)WICBitmapCreateCacheOptions.WICBitmapCacheOnLoad,
                            &pBitmap));
                        bitmapSource = new BitmapSourceSafeMILHandle((IntPtr)pBitmap);
                    }
                    finally
                    {
                        if (addRef)
                        {
                            wicSource.DangerousRelease();
                        }
                    }
                }

                bitmapSource.CalculateSize();
            }

            WicSourceHandle = bitmapSource;
            _isSourceCached = true;
            _bitmapInit.EndInit();

            UpdateCachedSettings();
        }

        /// <summary>
        /// Invalidates the bitmap source.
        /// </summary>
        public void Invalidate()
        {
            Invalidate(null);
        }

        /// <summary>
        /// Invalidates the bitmap source.
        /// </summary>
        public void Invalidate(Int32Rect? dirtyRect)
        {
            if (dirtyRect.HasValue)
            {
                dirtyRect.Value.ValidateForDirtyRect(nameof(dirtyRect), _pixelWidth, _pixelHeight);

                if (!dirtyRect.Value.HasArea)
                {
                    return;
                }
            }

            WritePreamble();

            if (_unmanagedSource != null)
            {
                _needsUpdate = true;
                InvalidateD2DSource();
            }

            WritePostscript();
        }

        internal override void FinalizeCreation()
        {
            BitmapSourceSafeMILHandle wicClipper = null;
            BitmapSourceSafeMILHandle wicTransformer = null;
            BitmapSourceSafeMILHandle transformedSource = _unmanagedSource;

            bool addRefUnm = false;
            _unmanagedSource.DangerousAddRef(ref addRefUnm);
            try
            {
                HRESULT.Check(IWICBitmapVtbl.SetResolution(
                    (void*)_unmanagedSource.DangerousGetHandle(),
                    96.0,
                    96.0));
            }
            finally
            {
                if (addRefUnm)
                {
                    _unmanagedSource.DangerousRelease();
                }
            }

            try
            {
                using (FactoryMaker factoryMaker = new FactoryMaker())
                {
                    void* wicFactory = (void*)factoryMaker.ImagingFactoryPtr;

                    if (!_sourceRect.IsEmpty)
                    {
                        void* pClipper = null;
                        HRESULT.Check(IWICImagingFactoryExVtbl.CreateBitmapClipper(
                            wicFactory,
                            &pClipper));
                        wicClipper = new BitmapSourceSafeMILHandle((IntPtr)pClipper);

                        WICRect wicRect = new WICRect(_sourceRect.X, _sourceRect.Y, _sourceRect.Width, _sourceRect.Height);

                        lock (_syncObject)
                        {
                            bool addRefClip = false;
                            bool addRefSrc = false;
                            wicClipper.DangerousAddRef(ref addRefClip);
                            transformedSource.DangerousAddRef(ref addRefSrc);
                            try
                            {
                                HRESULT.Check(IWICBitmapClipperVtbl.Initialize(
                                    (void*)wicClipper.DangerousGetHandle(),
                                    (void*)transformedSource.DangerousGetHandle(),
                                    &wicRect));
                            }
                            finally
                            {
                                if (addRefSrc) transformedSource.DangerousRelease();
                                if (addRefClip) wicClipper.DangerousRelease();
                            }
                        }

                        transformedSource = wicClipper;
                    }

                    if (_sizeOptions != null)
                    {
                        if (_sizeOptions.DoesScale)
                        {
                            Debug.Assert(_sizeOptions.Rotation == Rotation.Rotate0);
                            _sizeOptions.GetScaledWidthAndHeight(
                                (uint)_sizeOptions.PixelWidth,
                                (uint)_sizeOptions.PixelHeight,
                                out uint width,
                                out uint height);

                            void* pScaler = null;
                            HRESULT.Check(IWICImagingFactoryExVtbl.CreateBitmapScaler(
                                wicFactory,
                                &pScaler));
                            wicTransformer = new BitmapSourceSafeMILHandle((IntPtr)pScaler);

                            lock (_syncObject)
                            {
                                bool addRefTrans = false;
                                bool addRefSrc = false;
                                wicTransformer.DangerousAddRef(ref addRefTrans);
                                transformedSource.DangerousAddRef(ref addRefSrc);
                                try
                                {
                                    HRESULT.Check(IWICBitmapScalerVtbl.Initialize(
                                        (void*)wicTransformer.DangerousGetHandle(),
                                        (void*)transformedSource.DangerousGetHandle(),
                                        width,
                                        height,
                                        (uint)WICInterpolationMode.Fant));
                                }
                                finally
                                {
                                    if (addRefSrc) transformedSource.DangerousRelease();
                                    if (addRefTrans) wicTransformer.DangerousRelease();
                                }
                            }
                        }
                        else if (_sizeOptions.Rotation != Rotation.Rotate0)
                        {
                            void* pRotator = null;
                            HRESULT.Check(IWICImagingFactoryExVtbl.CreateBitmapFlipRotator(
                                wicFactory,
                                &pRotator));
                            wicTransformer = new BitmapSourceSafeMILHandle((IntPtr)pRotator);

                            lock (_syncObject)
                            {
                                bool addRefTrans = false;
                                bool addRefSrc = false;
                                wicTransformer.DangerousAddRef(ref addRefTrans);
                                transformedSource.DangerousAddRef(ref addRefSrc);
                                try
                                {
                                    HRESULT.Check(IWICBitmapFlipRotatorVtbl.Initialize(
                                        (void*)wicTransformer.DangerousGetHandle(),
                                        (void*)transformedSource.DangerousGetHandle(),
                                        (uint)_sizeOptions.WICTransformOptions));
                                }
                                finally
                                {
                                    if (addRefSrc) transformedSource.DangerousRelease();
                                    if (addRefTrans) wicTransformer.DangerousRelease();
                                }
                            }
                        }

                        if (wicTransformer != null)
                        {
                            transformedSource = wicTransformer;
                        }
                    }

                    WicSourceHandle = transformedSource;
                    _isSourceCached = true;
                }
            }
            finally
            {
                wicTransformer?.Close();
                wicClipper?.Close();
            }

            CreationCompleted = true;
            UpdateCachedSettings();
        }

        private BitmapSourceSafeMILHandle _unmanagedSource = null;
        private Int32Rect _sourceRect = Int32Rect.Empty;
        private BitmapSizeOptions _sizeOptions = null;
    }

    #endregion InteropBitmap
}

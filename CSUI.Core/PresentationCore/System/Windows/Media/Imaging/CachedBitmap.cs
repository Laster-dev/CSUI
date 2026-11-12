// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System.Runtime.InteropServices;
using CSUI.Core.Interop.DirectX;
using MS.Internal;
using MS.Internal.PresentationCore;

namespace System.Windows.Media.Imaging
{
    #region CachedBitmap

    /// <summary>
    /// CachedBitmap provides caching functionality for a BitmapSource.
    /// </summary>
    public sealed unsafe class CachedBitmap : BitmapSource
    {
        /// <summary>
        /// Construct a CachedBitmap.
        /// </summary>
        public CachedBitmap(BitmapSource source, BitmapCreateOptions createOptions, BitmapCacheOption cacheOption)
            : base(true)
        {
            ArgumentNullException.ThrowIfNull(source);

            BeginInit();
            _source = source;
            RegisterDownloadEventSource(_source);
            _createOptions = createOptions;
            _cacheOption = cacheOption;
            _syncObject = source.SyncObject;

            EndInit();
        }

        internal CachedBitmap(
            int pixelWidth,
            int pixelHeight,
            double dpiX,
            double dpiY,
            PixelFormat pixelFormat,
            BitmapPalette palette,
            IntPtr buffer,
            int bufferSize,
            int stride
            )
            : base(true)
        {
            InitFromMemoryPtr(pixelWidth, pixelHeight, dpiX, dpiY,
                              pixelFormat, palette,
                              buffer, bufferSize, stride);
        }

        /// <summary>
        /// Creates a managed BitmapSource wrapper around a pre-existing unmanaged bitmap.
        /// </summary>
        internal CachedBitmap(BitmapSourceSafeMILHandle bitmap) : base(true)
        {
            ArgumentNullException.ThrowIfNull(bitmap);

            _bitmapInit.BeginInit();

            _source = null;
            _createOptions = BitmapCreateOptions.None;
            _cacheOption = BitmapCacheOption.OnLoad;

            bitmap.CalculateSize();
            WicSourceHandle = bitmap;
            _syncObject = WicSourceHandle;

            IsSourceCached = true;
            CreationCompleted = true;

            _bitmapInit.EndInit();
        }

        private CachedBitmap()
            : base(true)
        {
        }

        internal CachedBitmap(
            int pixelWidth,
            int pixelHeight,
            double dpiX,
            double dpiY,
            PixelFormat pixelFormat,
            BitmapPalette palette,
            System.Array pixels,
            int stride
            )
            : base(true)
        {
            ArgumentNullException.ThrowIfNull(pixels);

            if (pixels.Rank != 1)
                throw new ArgumentException(SR.Collection_BadRank, nameof(pixels));

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

            int destBufferSize = elementSize * pixels.Length;

            fixed (byte* pixelArray = &MemoryMarshal.GetArrayDataReference(pixels))
                InitFromMemoryPtr(pixelWidth, pixelHeight, dpiX, dpiY, pixelFormat, palette, (nint)pixelArray, destBufferSize, stride);
        }

        private void CopyCommon(CachedBitmap sourceBitmap)
        {
            Animatable_IsResourceInvalidationNecessary = false;

            if (sourceBitmap._source != null)
            {
                BeginInit();
                _syncObject = sourceBitmap._syncObject;
                _source = sourceBitmap._source;
                RegisterDownloadEventSource(_source);
                _createOptions = sourceBitmap._createOptions;
                _cacheOption = sourceBitmap._cacheOption;

                if (_cacheOption == BitmapCacheOption.OnDemand)
                    _cacheOption = BitmapCacheOption.OnLoad;
                EndInit();
            }
            else
            {
                InitFromWICSource(sourceBitmap.WicSourceHandle);
            }

            Animatable_IsResourceInvalidationNecessary = true;
        }

        private void BeginInit()
        {
            _bitmapInit.BeginInit();
        }

        private void EndInit()
        {
            _bitmapInit.EndInit();

            if (!DelayCreation)
                FinalizeCreation();
        }

        internal override void FinalizeCreation()
        {
            lock (_syncObject)
            {
                WicSourceHandle = CreateCachedBitmap(_source, _source.WicSourceHandle, _createOptions, _cacheOption, _source.Palette);
            }

            IsSourceCached = (_cacheOption != BitmapCacheOption.None);
            CreationCompleted = true;
            UpdateCachedSettings();
        }

        #region Public Methods

        public new CachedBitmap Clone()
        {
            return (CachedBitmap)base.Clone();
        }

        public new CachedBitmap CloneCurrentValue()
        {
            return (CachedBitmap)base.CloneCurrentValue();
        }

        #endregion Public Methods

        #region Protected Methods

        protected override Freezable CreateInstanceCore()
        {
            return new CachedBitmap();
        }

        protected override void CloneCore(Freezable sourceFreezable)
        {
            CachedBitmap sourceBitmap = (CachedBitmap)sourceFreezable;
            base.CloneCore(sourceFreezable);
            CopyCommon(sourceBitmap);
        }

        protected override void CloneCurrentValueCore(Freezable sourceFreezable)
        {
            CachedBitmap sourceBitmap = (CachedBitmap)sourceFreezable;
            base.CloneCurrentValueCore(sourceFreezable);
            CopyCommon(sourceBitmap);
        }

        protected override void GetAsFrozenCore(Freezable sourceFreezable)
        {
            CachedBitmap sourceBitmap = (CachedBitmap)sourceFreezable;
            base.GetAsFrozenCore(sourceFreezable);
            CopyCommon(sourceBitmap);
        }

        protected override void GetCurrentValueAsFrozenCore(Freezable sourceFreezable)
        {
            CachedBitmap sourceBitmap = (CachedBitmap)sourceFreezable;
            base.GetCurrentValueAsFrozenCore(sourceFreezable);
            CopyCommon(sourceBitmap);
        }

        #endregion Protected Methods

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

        private void InitFromMemoryPtr(
            int pixelWidth,
            int pixelHeight,
            double dpiX,
            double dpiY,
            PixelFormat pixelFormat,
            BitmapPalette palette,
            IntPtr buffer,
            int bufferSize,
            int stride
            )
        {
            if (pixelFormat.Palettized && palette == null)
                throw new InvalidOperationException(SR.Image_IndexedPixelFormatRequiresPalette);

            if (pixelFormat.Format == PixelFormatEnum.Default && pixelFormat.Guid == WICPixelFormatGUIDs.WICPixelFormatDontCare)
            {
                throw new ArgumentException(
                    SR.Format(SR.Effect_PixelFormat, pixelFormat),
                    nameof(pixelFormat));
            }

            _bitmapInit.BeginInit();

            try
            {
                BitmapSourceSafeMILHandle wicBitmap;
                Guid guidFmt = pixelFormat.Guid;

                using (FactoryMaker factoryMaker = new FactoryMaker())
                {
                    void* pWicBitmap = null;
                    HRESULT.Check(IWICImagingFactoryVtbl.CreateBitmapFromMemory(
                        (void*)factoryMaker.ImagingFactoryPtr,
                        (uint)pixelWidth,
                        (uint)pixelHeight,
                        in guidFmt,
                        (uint)stride,
                        (uint)bufferSize,
                        (byte*)buffer,
                        &pWicBitmap));

                    wicBitmap = new BitmapSourceSafeMILHandle((IntPtr)pWicBitmap);
                    wicBitmap.CalculateSize();
                }

                bool addRefBmp = false;
                wicBitmap.DangerousAddRef(ref addRefBmp);
                try
                {
                    void* pBitmap = (void*)wicBitmap.DangerousGetHandle();
                    HRESULT.Check(IWICBitmapVtbl.SetResolution(pBitmap, dpiX, dpiY));

                    if (pixelFormat.Palettized)
                    {
                        bool addRefPal = false;
                        palette.InternalPalette.DangerousAddRef(ref addRefPal);
                        try
                        {
                            HRESULT.Check(IWICBitmapVtbl.SetPalette(
                                pBitmap,
                                (void*)palette.InternalPalette.DangerousGetHandle()));
                        }
                        finally
                        {
                            if (addRefPal)
                            {
                                palette.InternalPalette.DangerousRelease();
                            }
                        }
                    }
                }
                finally
                {
                    if (addRefBmp)
                    {
                        wicBitmap.DangerousRelease();
                    }
                }

                WicSourceHandle = wicBitmap;
                _isSourceCached = true;
            }
            catch
            {
                _bitmapInit.Reset();
                throw;
            }

            _createOptions = BitmapCreateOptions.PreservePixelFormat;
            _cacheOption = BitmapCacheOption.OnLoad;
            _syncObject = WicSourceHandle;
            _bitmapInit.EndInit();

            UpdateCachedSettings();
        }

        private BitmapSource _source;
        private BitmapCreateOptions _createOptions = BitmapCreateOptions.None;
        private BitmapCacheOption _cacheOption = BitmapCacheOption.Default;
    }

    #endregion CachedBitmap
}

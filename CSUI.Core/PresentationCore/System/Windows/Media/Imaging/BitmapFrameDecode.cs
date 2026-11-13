// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using CSUI.Core.Interop.DirectX;
using MS.Internal;
using SR = MS.Internal.PresentationCore.SR;

namespace System.Windows.Media.Imaging
{
    #region BitmapFrameDecode

    /// <summary>
    /// BitmapFrameDecode class is a wrapper around a decoded frame.
    /// </summary>
    internal sealed unsafe class BitmapFrameDecode : BitmapFrame
    {
        #region Constructors

        internal BitmapFrameDecode(
            int frameNumber,
            BitmapCreateOptions createOptions,
            BitmapCacheOption cacheOption,
            LateBoundBitmapDecoder decoder)
            : base(true)
        {
            _bitmapInit.BeginInit();
            _frameNumber = frameNumber;
            _isThumbnailCached = false;
            _isMetadataCached = false;
            _frameSource = null;

            Debug.Assert(decoder != null);
            _decoder = decoder;
            _syncObject = decoder.SyncObject;

            _createOptions = createOptions;
            _cacheOption = cacheOption;
            _bitmapInit.EndInit();

            if (_decoder.IsDownloading)
            {
                _isDownloading = true;
                _decoder.DownloadCompleted += OnDownloadCompleted;
                _decoder.DownloadProgress += OnDownloadProgress;
                _decoder.DownloadFailed += OnDownloadFailed;
                _wicSource = CreatePlaceholderSource();
                UpdateCachedSettings();
            }
            else if ((_createOptions & BitmapCreateOptions.DelayCreation) == 0)
            {
                EnsureSource();
                FinalizeCreation();
            }
            else
            {
                DelayCreation = true;
            }
        }

        internal BitmapFrameDecode(
            int frameNumber,
            BitmapCreateOptions createOptions,
            BitmapCacheOption cacheOption,
            BitmapDecoder decoder)
            : base(true)
        {
            _bitmapInit.BeginInit();
            _frameNumber = frameNumber;
            _isThumbnailCached = false;
            _isMetadataCached = false;
            _frameSource = null;

            Debug.Assert(decoder != null);
            _decoder = decoder;
            _syncObject = decoder.SyncObject;

            _createOptions = createOptions;
            _cacheOption = cacheOption;
            _bitmapInit.EndInit();

            if ((_createOptions & BitmapCreateOptions.DelayCreation) != 0)
            {
                DelayCreation = true;
            }
            else
            {
                EnsureSource();
                FinalizeCreation();
            }
        }

        internal BitmapFrameDecode(
            int frameNumber,
            BitmapCreateOptions createOptions,
            BitmapCacheOption cacheOption,
            BitmapFrameDecode frameDecode)
            : base(true)
        {
            _bitmapInit.BeginInit();
            _frameNumber = frameNumber;
            _isThumbnailCached = frameDecode._isThumbnailCached;
            _isMetadataCached = frameDecode._isMetadataCached;
            _isColorContextCached = frameDecode._isColorContextCached;
            _frameSource = frameDecode._frameSource;
            _thumbnail = frameDecode._thumbnail;
            _metadata = frameDecode.InternalMetadata;
            _readOnlycolorContexts = frameDecode._readOnlycolorContexts;

            _decoder = frameDecode._decoder;
            _syncObject = _decoder.SyncObject;

            _createOptions = createOptions;
            _cacheOption = cacheOption;
            _bitmapInit.EndInit();

            if ((_createOptions & BitmapCreateOptions.DelayCreation) == 0 && _cacheOption == BitmapCacheOption.OnLoad)
            {
                EnsureSource();
                FinalizeCreation();
            }
            else if (_decoder == null || !_decoder.IsDownloading)
            {
                if (frameDecode._wicSource != null)
                {
                    _wicSource = frameDecode._wicSource;
                    _isSourceCached = frameDecode._isSourceCached;
                    CreationCompleted = frameDecode.CreationCompleted;
                    UpdateCachedSettings();
                }
                else if ((_createOptions & BitmapCreateOptions.DelayCreation) != 0)
                {
                    DelayCreation = true;
                }
                else
                {
                    EnsureSource();
                    FinalizeCreation();
                }
            }
        }

        private BitmapFrameDecode()
            : base(true)
        {
        }

        #endregion Constructors

        #region Public Properties

        public override Uri BaseUri
        {
            get
            {
                ReadPreamble();
                return _decoder._baseUri;
            }
            set
            {
                WritePreamble();
            }
        }

        public override BitmapSource Thumbnail
        {
            get
            {
                ReadPreamble();
                EnsureSource();

                if (_isDownloading)
                {
                    return null;
                }

                if (!_isThumbnailCached)
                {
                    IntPtr pThumbnail = IntPtr.Zero;

                    lock (_syncObject)
                    {
                        void* thumbPtr = null;
                        int hr = IWICBitmapFrameDecodeVtbl.GetThumbnail(
                            (void*)_frameSource.DangerousGetHandle(),
                            &thumbPtr);

                        if (hr != (int)WinCodecErrors.WINCODEC_ERR_CODECNOTHUMBNAIL)
                        {
                            HRESULT.Check(hr);
                        }
                        pThumbnail = (IntPtr)thumbPtr;
                    }

                    _isThumbnailCached = true;

                    if (pThumbnail != IntPtr.Zero)
                    {
                        BitmapSourceSafeMILHandle thumbHandle = new BitmapSourceSafeMILHandle(pThumbnail);
                        BitmapSourceSafeMILHandle cachedThumbnail = BitmapSource.CreateCachedBitmap(
                            null,
                            thumbHandle,
                            BitmapCreateOptions.PreservePixelFormat,
                            _cacheOption == BitmapCacheOption.None ? BitmapCacheOption.OnDemand : _cacheOption,
                            null);

                        _thumbnail = new UnmanagedBitmapWrapper(cachedThumbnail);
                        _thumbnail.Freeze();
                    }
                }

                return _thumbnail;
            }
        }

        public override ImageMetadata Metadata
        {
            get
            {
                ReadPreamble();
                return InternalMetadata;
            }
        }

        public override BitmapDecoder Decoder
        {
            get
            {
                ReadPreamble();
                return _decoder;
            }
        }

        private int GetColorContextsHelper(ref uint numContexts, IntPtr[] colorContextPtrs)
        {
            void* pFrame = (void*)_frameSource.DangerousGetHandle();
            if (colorContextPtrs == null)
            {
                return IWICBitmapFrameDecodeVtbl.GetColorContexts(pFrame, numContexts, null, out numContexts);
            }

            fixed (IntPtr* pPtrs = colorContextPtrs)
            {
                return IWICBitmapFrameDecodeVtbl.GetColorContexts(pFrame, numContexts, (void**)pPtrs, out numContexts);
            }
        }

        public override ReadOnlyCollection<ColorContext> ColorContexts
        {
            get
            {
                ReadPreamble();

                if (_isDownloading)
                {
                    return null;
                }

                if (!_isColorContextCached)
                {
                    EnsureSource();

                    lock (_syncObject)
                    {
                        IList<ColorContext> colorContextList = ColorContext.GetColorContextsHelper(GetColorContextsHelper);
                        if (colorContextList != null)
                        {
                            _readOnlycolorContexts = new ReadOnlyCollection<ColorContext>(colorContextList);
                        }

                        _isColorContextCached = true;
                    }
                }

                return _readOnlycolorContexts;
            }
        }

        public override bool IsDownloading
        {
            get
            {
                ReadPreamble();
                return _isDownloading;
            }
        }

        #endregion Public Properties

        #region Public Methods

        public override InPlaceBitmapMetadataWriter CreateInPlaceBitmapMetadataWriter()
        {
            ReadPreamble();
            EnsureSource();
            CheckIfSiteOfOrigin();
            return InPlaceBitmapMetadataWriter.CreateFromFrameDecode(_frameSource, _syncObject);
        }

        #endregion Public Methods

        #region Freezable Overrides

        protected override Freezable CreateInstanceCore()
        {
            return new BitmapFrameDecode();
        }

        private void CopyCommon(BitmapFrameDecode sourceFrameDecode)
        {
            _bitmapInit.BeginInit();
            _frameNumber = sourceFrameDecode._frameNumber;
            _isThumbnailCached = sourceFrameDecode._isThumbnailCached;
            _isMetadataCached = sourceFrameDecode._isMetadataCached;
            _isColorContextCached = sourceFrameDecode._isColorContextCached;
            _frameSource = sourceFrameDecode._frameSource;
            _thumbnail = sourceFrameDecode._thumbnail;
            _metadata = sourceFrameDecode.InternalMetadata;
            _readOnlycolorContexts = sourceFrameDecode._readOnlycolorContexts;
            _decoder = sourceFrameDecode._decoder;
            if (_decoder != null && _decoder.IsDownloading)
            {
                _isDownloading = true;
                _syncObject = _decoder.SyncObject;
                _decoder.DownloadCompleted += OnDownloadCompleted;
                _decoder.DownloadProgress += OnDownloadProgress;
                _decoder.DownloadFailed += OnDownloadFailed;
            }

            _bitmapInit.EndInit();
        }

        protected override void CloneCore(Freezable sourceFreezable)
        {
            BitmapFrameDecode sourceFrameDecode = (BitmapFrameDecode)sourceFreezable;
            base.CloneCore(sourceFreezable);
            CopyCommon(sourceFrameDecode);
        }

        protected override void CloneCurrentValueCore(Freezable sourceFreezable)
        {
            BitmapFrameDecode sourceFrameDecode = (BitmapFrameDecode)sourceFreezable;
            base.CloneCurrentValueCore(sourceFreezable);
            CopyCommon(sourceFrameDecode);
        }

        protected override void GetAsFrozenCore(Freezable sourceFreezable)
        {
            BitmapFrameDecode sourceFrameDecode = (BitmapFrameDecode)sourceFreezable;
            base.GetAsFrozenCore(sourceFreezable);
            CopyCommon(sourceFrameDecode);
        }

        protected override void GetCurrentValueAsFrozenCore(Freezable sourceFreezable)
        {
            BitmapFrameDecode sourceFrameDecode = (BitmapFrameDecode)sourceFreezable;
            base.GetCurrentValueAsFrozenCore(sourceFreezable);
            CopyCommon(sourceFrameDecode);
        }

        #endregion Freezable Overrides

        #region Internal Properties & Methods

        internal void UpdateDecoder(BitmapDecoder decoder)
        {
            _decoder = decoder;
            _syncObject = decoder.SyncObject;

            _needsUpdate = true;

            _decoder.DownloadCompleted += OnDownloadCompleted;
            _decoder.DownloadProgress += OnDownloadProgress;
            _decoder.DownloadFailed += OnDownloadFailed;

            EnsureSource();
        }

        internal override BitmapMetadata InternalMetadata
        {
            get
            {
                CheckIfSiteOfOrigin();
                EnsureSource();

                if (_isDownloading)
                {
                    return null;
                }

                if (!_isMetadataCached)
                {
                    void* pMetadata = null;
                    int hr;

                    lock (_syncObject)
                    {
                        hr = IWICBitmapFrameDecodeVtbl.GetMetadataQueryReader(
                            (void*)_frameSource.DangerousGetHandle(),
                            &pMetadata);
                    }

                    if (hr != (int)WinCodecErrors.WINCODEC_ERR_UNSUPPORTEDOPERATION)
                    {
                        HRESULT.Check(hr);
                    }

                    if (pMetadata != null)
                    {
                        SafeMILHandle metadataHandle = new SafeMILHandle((IntPtr)pMetadata);
                        _metadata = new BitmapMetadata(
                            metadataHandle,
                            readOnly: true,
                            fixedSize: _decoder != null && _decoder.InternalDecoder != null && _decoder.IsMetadataFixedSize,
                            _syncObject);
                    }

                    _isMetadataCached = true;
                }

                return _metadata;
            }
            set
            {
                CheckIfSiteOfOrigin();
                _metadata = value;
            }
        }

        internal override void FinalizeCreation()
        {
            EnsureSource();

            if (_frameSource != null)
            {
                _wicSource = _frameSource;
                _wicSource = CreateCachedBitmap(
                    this,
                    _frameSource,
                    _createOptions,
                    _cacheOption,
                    null);

                _isSourceCached = (_cacheOption != BitmapCacheOption.None);
            }

            CreationCompleted = true;
            UpdateCachedSettings();

            if ((_createOptions & BitmapCreateOptions.DelayCreation) == 0)
            {
                DelayCreation = false;
            }
        }

        internal override bool CanSerializeToString()
        {
            return (_decoder != null && _decoder._uri != null);
        }

        internal override string ConvertToString(string format, IFormatProvider provider)
        {
            if (_decoder != null && _decoder._uri != null)
            {
                if (_decoder._baseUri != null)
                {
                    return new Uri(_decoder._baseUri, _decoder._uri).OriginalString;
                }

                return _decoder._uri.OriginalString;
            }

            return base.ConvertToString(format, provider);
        }

        private void EnsureSource()
        {
            if (_frameSource == null)
            {
                if (_decoder == null)
                {
                    return;
                }

                if (_decoder.InternalDecoder == null)
                {
                    if (_isDownloading)
                    {
                        if (_frameSource == null)
                        {
                            _wicSource = CreatePlaceholderSource();
                        }
                        return;
                    }
                    else
                    {
                        return;
                    }
                }

                lock (_syncObject)
                {
                    if (_frameSource == null)
                    {
                        void* pFrameDecode = null;
                        HRESULT.Check(IWICBitmapDecoderVtbl.GetFrame(
                            (void*)_decoder.InternalDecoder.DangerousGetHandle(),
                            (uint)_frameNumber,
                            &pFrameDecode));

                        _frameSource = new BitmapSourceSafeMILHandle((IntPtr)pFrameDecode);
                        _frameSource.CalculateSize();
                    }
                }
            }
        }

        private static BitmapSourceSafeMILHandle CreatePlaceholderSource()
        {
            using var factoryMaker = new FactoryMaker();
            void* pBitmap = null;
            Guid fmt = WICPixelFormatGUIDs.WICPixelFormat32bppPBGRA;
            HRESULT.Check(IWICImagingFactoryExVtbl.CreateBitmap(
                (void*)factoryMaker.ImagingFactoryPtr,
                1,
                1,
                in fmt,
                (uint)WICBitmapCreateCacheOptions.WICBitmapCacheOnLoad,
                &pBitmap));

            return new BitmapSourceSafeMILHandle((IntPtr)pBitmap);
        }

        private void OnDownloadCompleted(object sender, EventArgs e)
        {
            if (sender is LateBoundBitmapDecoder decoder)
            {
                _decoder = decoder.Decoder;
                _syncObject = _decoder.SyncObject;
                _isDownloading = false;

                lock (_syncObject)
                {
                    _frameSource = null;
                    _wicSource = null;
                    _isThumbnailCached = false;
                    _isMetadataCached = false;
                    _isColorContextCached = false;
                    _needsUpdate = true;

                    EnsureSource();
                    FinalizeCreation();
                }

                UpdateCachedSettings();
                InvalidateD2DSource();

                _downloadEvent.InvokeEvents(this, e);
            }
        }

        private void OnDownloadProgress(object sender, DownloadProgressEventArgs e)
        {
            _progressEvent.InvokeEvents(this, e);
        }

        private void OnDownloadFailed(object sender, ExceptionEventArgs e)
        {
            _isDownloading = false;
            _failedEvent.InvokeEvents(this, e);
        }

        #endregion Internal Properties & Methods

        #region Data Members

        private BitmapSourceSafeMILHandle _frameSource;
        private int _frameNumber;
        private bool _isThumbnailCached;
        private bool _isMetadataCached;
        private bool _isColorContextCached;
        private BitmapDecoder _decoder;
        private BitmapCreateOptions _createOptions;
        private BitmapCacheOption _cacheOption;
        private bool _isDownloading;

        #endregion Data Members
    }

    #endregion BitmapFrameDecode
}

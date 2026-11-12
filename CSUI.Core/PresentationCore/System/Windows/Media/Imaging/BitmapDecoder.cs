// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Net.Cache;
using System.Reflection;
using System.Windows.Navigation;
using System.Windows.Threading;
using CSUI.Core.Interop.DirectX;
using Microsoft.Win32.SafeHandles;
using MS.Internal;
using MS.Internal.Utility;
using SR = MS.Internal.PresentationCore.SR;

namespace System.Windows.Media.Imaging
{
    #region BitmapDecoder

    /// <summary>
    /// BitmapDecoder represents a decoder for an image container format.
    /// </summary>
    public abstract unsafe class BitmapDecoder : DispatcherObject
    {
        #region Constructors & Static Create

        /// <summary>
        /// Create a BitmapDecoder from a Uri with the specified BitmapCreateOptions and BitmapCacheOption.
        /// </summary>
        public static BitmapDecoder Create(
            Uri bitmapUri,
            BitmapCreateOptions createOptions,
            BitmapCacheOption cacheOption)
        {
            return Create(bitmapUri, createOptions, cacheOption, null);
        }

        /// <summary>
        /// Create a BitmapDecoder from a Uri with the specified BitmapCreateOptions, BitmapCacheOption and RequestCachePolicy.
        /// </summary>
        public static BitmapDecoder Create(
            Uri bitmapUri,
            BitmapCreateOptions createOptions,
            BitmapCacheOption cacheOption,
            RequestCachePolicy uriCachePolicy)
        {
            ArgumentNullException.ThrowIfNull(bitmapUri);

            return CreateFromUriOrStream(
                null,
                bitmapUri,
                null,
                createOptions & ~BitmapCreateOptions.DelayCreation,
                cacheOption,
                uriCachePolicy,
                false);
        }

        /// <summary>
        /// Create a BitmapDecoder from a Stream with the specified BitmapCreateOptions and BitmapCacheOption.
        /// </summary>
        public static BitmapDecoder Create(
            Stream bitmapStream,
            BitmapCreateOptions createOptions,
            BitmapCacheOption cacheOption)
        {
            ArgumentNullException.ThrowIfNull(bitmapStream);

            return CreateFromUriOrStream(
                null,
                null,
                bitmapStream,
                createOptions & ~BitmapCreateOptions.DelayCreation,
                cacheOption,
                null,
                false);
        }

        internal static BitmapDecoder CreateFromUriOrStream(
            Uri baseUri,
            Uri uri,
            Stream stream,
            BitmapCreateOptions createOptions,
            BitmapCacheOption cacheOption,
            RequestCachePolicy uriCachePolicy,
            bool insertInDecoderCache)
        {
            Guid clsId = Guid.Empty;
            bool closeStream = false;
            SafeMILHandle decoderHandle = null;
            BitmapDecoder cachedDecoder = null;
            Uri finalUri = null;

            if (!ValidateEnums.IsBitmapCreateOptionsValid(createOptions))
            {
                throw new ArgumentException(SR.Format(SR.Enum_Invalid, "BitmapCreateOptions"), nameof(createOptions));
            }

            if (!ValidateEnums.IsBitmapCacheOptionValid(cacheOption))
            {
                throw new ArgumentException(SR.Format(SR.Enum_Invalid, "BitmapCacheOption"), nameof(cacheOption));
            }

            if (uri != null && stream != null)
            {
                throw new ArgumentException(SR.Format(SR.Image_NeitherArgument, "uri", "stream"));
            }

            if (uri == null && stream == null)
            {
                throw new ArgumentException(SR.Format(SR.Image_NeitherArgument, "uri", "stream"));
            }

            if (uri != null)
            {
                finalUri = BindUriHelper.GetResolvedUri(baseUri, uri);

                if (insertInDecoderCache &&
                    ((createOptions & BitmapCreateOptions.IgnoreImageCache) == 0) &&
                    cacheOption != BitmapCacheOption.None)
                {
                    WeakReference decoderRef = ImagingCache.CheckDecoderCache(finalUri) as WeakReference;
                    cachedDecoder = decoderRef?.Target as BitmapDecoder;

                    if (cachedDecoder != null && cachedDecoder.CheckAccess())
                    {
                        return cachedDecoder;
                    }

                    if (cachedDecoder == null && decoderRef != null)
                    {
                        ImagingCache.RemoveFromDecoderCache(finalUri);
                    }
                }
            }

            if (finalUri != null && string.Equals(finalUri.Scheme, "pack", StringComparison.OrdinalIgnoreCase))
            {
                stream = OpenPackUriStream(finalUri);
                closeStream = true;
                insertInDecoderCache = false;
            }
            else if (finalUri != null && !finalUri.IsFile)
            {
                if (!finalUri.IsAbsoluteUri)
                {
                    throw new ArgumentException(SR.Image_CantDealWithUri, nameof(uri));
                }

                if (string.Equals(finalUri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(finalUri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
                {
                    return ProcessHttpFiles(uri, baseUri, stream, createOptions, cacheOption, uriCachePolicy, insertInDecoderCache);
                }

                throw new NotSupportedException(SR.Image_CantDealWithUri);
            }
            else if (finalUri != null && finalUri.IsFile)
            {
                if (finalUri.IsUnc)
                {
                    return ProcessUncFiles(uri, baseUri, stream, createOptions, cacheOption, uriCachePolicy, insertInDecoderCache);
                }

                stream = new FileStream(finalUri.LocalPath, FileMode.Open, FileAccess.Read, FileShare.Read);
                closeStream = true;
            }

            Debug.Assert(stream != null);

            decoderHandle = SetupDecoderFromUriOrStream(
                finalUri,
                stream,
                cacheOption,
                out clsId,
                out bool isOriginalStreamSeekable,
                out Uri tempUri,
                out Stream tempStream,
                out object checkAccessObject);

            if (tempStream != null && !ReferenceEquals(tempStream, stream))
            {
                if (closeStream)
                {
                    stream.Dispose();
                }
                stream = tempStream;
                closeStream = true;
            }

            BitmapDecoder decoder = GetDecoderFromClsId(
                clsId,
                decoderHandle,
                null,
                baseUri,
                uri,
                stream,
                createOptions,
                cacheOption,
                insertInDecoderCache,
                closeStream);

            if (insertInDecoderCache &&
                finalUri != null &&
                cacheOption != BitmapCacheOption.None &&
                !decoder.IsDownloading)
            {
                ImagingCache.AddToDecoderCache(finalUri, new WeakReference(decoder));
            }

            return decoder;
        }

        private static BitmapDecoder ProcessUncFiles(
            Uri uri,
            Uri baseUri,
            Stream stream,
            BitmapCreateOptions createOptions,
            BitmapCacheOption cacheOption,
            RequestCachePolicy uriCachePolicy,
            bool insertInDecoderCache)
        {
            LateBoundBitmapDecoder decoder = new LateBoundBitmapDecoder(
                baseUri,
                uri,
                stream,
                createOptions,
                cacheOption,
                uriCachePolicy);

            if (insertInDecoderCache && uri != null && cacheOption != BitmapCacheOption.None)
            {
                Uri finalUri = BindUriHelper.GetResolvedUri(baseUri, uri);
                ImagingCache.AddToDecoderCache(finalUri, new WeakReference(decoder));
            }

            return decoder;
        }

        private static BitmapDecoder ProcessHttpFiles(
            Uri uri,
            Uri baseUri,
            Stream stream,
            BitmapCreateOptions createOptions,
            BitmapCacheOption cacheOption,
            RequestCachePolicy uriCachePolicy,
            bool insertInDecoderCache)
        {
            LateBoundBitmapDecoder decoder = new LateBoundBitmapDecoder(
                baseUri,
                uri,
                stream,
                createOptions,
                cacheOption,
                uriCachePolicy);

            if (insertInDecoderCache && uri != null && cacheOption != BitmapCacheOption.None)
            {
                Uri finalUri = BindUriHelper.GetResolvedUri(baseUri, uri);
                ImagingCache.AddToDecoderCache(finalUri, new WeakReference(decoder));
            }

            return decoder;
        }

        private static Stream OpenPackUriStream(Uri packUri)
        {
            string localPath = packUri.AbsolutePath ?? string.Empty;
            int componentMarker = localPath.IndexOf(";component/", StringComparison.OrdinalIgnoreCase);
            if (componentMarker >= 0)
            {
                localPath = localPath.Substring(componentMarker + ";component/".Length);
            }
            else
            {
                localPath = localPath.TrimStart('/');
            }

            if (string.IsNullOrEmpty(localPath))
            {
                throw new FileNotFoundException(SR.Image_CantDealWithUri);
            }

            string diskPath = Path.Combine(
                AppContext.BaseDirectory,
                localPath.Replace('/', Path.DirectorySeparatorChar));

            if (File.Exists(diskPath))
            {
                return new FileStream(diskPath, FileMode.Open, FileAccess.Read, FileShare.Read);
            }

            Assembly resourceAssembly = BaseUriHelper.ResourceAssembly ?? Assembly.GetEntryAssembly();
            if (resourceAssembly != null)
            {
                Stream manifestStream = resourceAssembly.GetManifestResourceStream(localPath)
                    ?? resourceAssembly.GetManifestResourceStream(localPath.Replace('/', '.'));

                if (manifestStream != null)
                {
                    return manifestStream;
                }

                string dotPath = "." + localPath.Replace('/', '.');
                foreach (string resourceName in resourceAssembly.GetManifestResourceNames())
                {
                    if (resourceName.EndsWith(dotPath, StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(resourceName, localPath, StringComparison.OrdinalIgnoreCase))
                    {
                        Stream matched = resourceAssembly.GetManifestResourceStream(resourceName);
                        if (matched != null)
                        {
                            return matched;
                        }
                    }
                }
            }

            throw new FileNotFoundException(SR.Image_CantDealWithUri, diskPath);
        }

        private static BitmapDecoder GetDecoderFromClsId(
            Guid clsId,
            SafeMILHandle decoderHandle,
            BitmapDecoder decoder,
            Uri baseUri,
            Uri uri,
            Stream stream,
            BitmapCreateOptions createOptions,
            BitmapCacheOption cacheOption,
            bool insertInDecoderCache,
            bool closeStream)
        {
            if (clsId == MILGuidData.GUID_ContainerFormatBmp)
            {
                return new BmpBitmapDecoder(decoderHandle, decoder, baseUri, uri, stream, createOptions, cacheOption, insertInDecoderCache, closeStream);
            }
            if (clsId == MILGuidData.GUID_ContainerFormatGif)
            {
                return new GifBitmapDecoder(decoderHandle, decoder, baseUri, uri, stream, createOptions, cacheOption, insertInDecoderCache, closeStream);
            }
            if (clsId == MILGuidData.GUID_ContainerFormatIco)
            {
                return new IconBitmapDecoder(decoderHandle, decoder, baseUri, uri, stream, createOptions, cacheOption, insertInDecoderCache, closeStream);
            }
            if (clsId == MILGuidData.GUID_ContainerFormatJpeg)
            {
                return new JpegBitmapDecoder(decoderHandle, decoder, baseUri, uri, stream, createOptions, cacheOption, insertInDecoderCache, closeStream);
            }
            if (clsId == MILGuidData.GUID_ContainerFormatPng)
            {
                return new PngBitmapDecoder(decoderHandle, decoder, baseUri, uri, stream, createOptions, cacheOption, insertInDecoderCache, closeStream);
            }
            if (clsId == MILGuidData.GUID_ContainerFormatTiff)
            {
                return new TiffBitmapDecoder(decoderHandle, decoder, baseUri, uri, stream, createOptions, cacheOption, insertInDecoderCache, closeStream);
            }
            if (clsId == MILGuidData.GUID_ContainerFormatWmp)
            {
                return new WmpBitmapDecoder(decoderHandle, decoder, baseUri, uri, stream, createOptions, cacheOption, insertInDecoderCache, closeStream);
            }

            return new UnknownBitmapDecoder(decoderHandle, decoder, baseUri, uri, stream, createOptions, cacheOption, insertInDecoderCache, closeStream);
        }

        protected BitmapDecoder()
        {
            _isBuiltInDecoder = false;
        }

        internal BitmapDecoder(bool isBuiltInDecoder)
        {
            _isBuiltInDecoder = isBuiltInDecoder;
        }

        internal BitmapDecoder(
            Uri bitmapUri,
            BitmapCreateOptions createOptions,
            BitmapCacheOption cacheOption,
            Guid expectedClsId)
        {
            ArgumentNullException.ThrowIfNull(bitmapUri);

            if ((createOptions & BitmapCreateOptions.DelayCreation) != 0)
            {
                throw new ArgumentException(SR.Format(SR.Enum_Invalid, "BitmapCreateOptions"), nameof(createOptions));
            }

            _createOptions = createOptions;
            _cacheOption = cacheOption;
            _uri = bitmapUri;
            _isBuiltInDecoder = true;

            Initialize(expectedClsId, uri: bitmapUri, stream: null, closeStreamOnExit: true);
        }

        internal BitmapDecoder(
            Stream bitmapStream,
            BitmapCreateOptions createOptions,
            BitmapCacheOption cacheOption,
            Guid expectedClsId)
        {
            ArgumentNullException.ThrowIfNull(bitmapStream);

            if ((createOptions & BitmapCreateOptions.DelayCreation) != 0)
            {
                throw new ArgumentException(SR.Format(SR.Enum_Invalid, "BitmapCreateOptions"), nameof(createOptions));
            }

            _createOptions = createOptions;
            _cacheOption = cacheOption;
            _stream = bitmapStream;
            _isBuiltInDecoder = true;

            Initialize(expectedClsId, uri: null, stream: bitmapStream, closeStreamOnExit: false);
        }

        internal BitmapDecoder(
            SafeMILHandle decoderHandle,
            BitmapDecoder decoder,
            Uri baseUri,
            Uri uri,
            Stream stream,
            BitmapCreateOptions createOptions,
            BitmapCacheOption cacheOption,
            bool insertInDecoderCache,
            bool closeStream)
        {
            _isBuiltInDecoder = true;
            _decoderHandle = decoderHandle;
            _baseUri = baseUri;
            _uri = uri;
            _stream = stream;
            _createOptions = createOptions;
            _cacheOption = cacheOption;

            if (decoderHandle != null)
            {
                Initialize(this);
            }

            if (closeStream && _stream != null && _cacheOption == BitmapCacheOption.OnLoad)
            {
                _stream.Dispose();
                _stream = null;
            }
        }

        #endregion Constructors & Static Create

        #region Properties

        public virtual BitmapPalette Palette
        {
            get
            {
                VerifyAccess();
                EnsureBuiltInDecoder();

                if (!_isPaletteCached)
                {
                    SafeMILHandle unmanagedPalette = BitmapPalette.CreateInternalPalette();
                    int hr;
                    lock (_syncObject)
                    {
                        hr = IWICBitmapDecoderExVtbl.CopyPalette(
                            (void*)_decoderHandle.DangerousGetHandle(),
                            (void*)unmanagedPalette.DangerousGetHandle());
                    }

                    if (hr == HRESULT.S_OK)
                    {
                        void* pPalette = (void*)unmanagedPalette.DangerousGetHandle();
                        HRESULT.Check(IWICPaletteVtbl.GetType(pPalette, out WICPaletteType paletteType));
                        HRESULT.Check(IWICPaletteVtbl.HasAlpha(pPalette, out bool hasAlpha));

                        if (paletteType == WICPaletteType.WICPaletteTypeCustom ||
                            paletteType == WICPaletteType.WICPaletteTypeOptimal)
                        {
                            _palette = new BitmapPalette(unmanagedPalette);
                        }
                        else
                        {
                            _palette = BitmapPalettes.FromMILPaletteType(paletteType, hasAlpha);
                        }
                    }

                    _isPaletteCached = true;
                }

                return _palette;
            }
        }

        private int GetColorContextsHelper(ref uint numContexts, IntPtr[] colorContextPtrs)
        {
            void* pDecoder = (void*)_decoderHandle.DangerousGetHandle();
            if (colorContextPtrs == null)
            {
                return IWICBitmapDecoderExVtbl.GetColorContexts(pDecoder, numContexts, null, out numContexts);
            }

            fixed (IntPtr* pPtrs = colorContextPtrs)
            {
                return IWICBitmapDecoderExVtbl.GetColorContexts(pDecoder, numContexts, (void**)pPtrs, out numContexts);
            }
        }

        public virtual ReadOnlyCollection<ColorContext> ColorContexts
        {
            get
            {
                VerifyAccess();
                EnsureBuiltInDecoder();

                if (!_isColorContextCached)
                {
                    lock (_syncObject)
                    {
                        IList<ColorContext> list = ColorContext.GetColorContextsHelper(GetColorContextsHelper);
                        if (list != null)
                        {
                            _readOnlyColorContexts = new ReadOnlyCollection<ColorContext>(list);
                        }

                        _isColorContextCached = true;
                    }
                }

                return _readOnlyColorContexts;
            }
        }

        public virtual BitmapSource Thumbnail
        {
            get
            {
                VerifyAccess();
                EnsureBuiltInDecoder();

                if (!_isThumbnailCached)
                {
                    void* pThumbnail = null;
                    int hr;
                    lock (_syncObject)
                    {
                        hr = IWICBitmapDecoderExVtbl.GetThumbnail(
                            (void*)_decoderHandle.DangerousGetHandle(),
                            &pThumbnail);
                    }

                    if (hr != (int)WinCodecErrors.WINCODEC_ERR_CODECNOTHUMBNAIL &&
                        hr != (int)WinCodecErrors.WINCODEC_ERR_UNSUPPORTEDOPERATION)
                    {
                        HRESULT.Check(hr);
                    }

                    if (pThumbnail != null)
                    {
                        BitmapSourceSafeMILHandle thumbHandle = new BitmapSourceSafeMILHandle((IntPtr)pThumbnail);
                        BitmapSourceSafeMILHandle cachedThumb = BitmapSource.CreateCachedBitmap(
                            null,
                            thumbHandle,
                            BitmapCreateOptions.PreservePixelFormat,
                            _cacheOption == BitmapCacheOption.None ? BitmapCacheOption.OnDemand : _cacheOption,
                            null);

                        _thumbnail = new UnmanagedBitmapWrapper(cachedThumb);
                        _thumbnail.Freeze();
                    }

                    _isThumbnailCached = true;
                }

                return _thumbnail;
            }
        }

        public virtual BitmapMetadata Metadata
        {
            get
            {
                VerifyAccess();
                EnsureBuiltInDecoder();
                CheckIfSiteOfOrigin();

                if (!_isMetadataCached)
                {
                    void* pMetadata = null;
                    int hr;
                    lock (_syncObject)
                    {
                        hr = IWICBitmapDecoderExVtbl.GetMetadataQueryReader(
                            (void*)_decoderHandle.DangerousGetHandle(),
                            &pMetadata);
                    }

                    if (hr != (int)WinCodecErrors.WINCODEC_ERR_UNSUPPORTEDOPERATION)
                    {
                        HRESULT.Check(hr);
                    }

                    if (pMetadata != null)
                    {
                        SafeMILHandle metadataHandle = new SafeMILHandle((IntPtr)pMetadata);
                        _metadata = new BitmapMetadata(metadataHandle, readOnly: true, fixedSize: IsMetadataFixedSize, _syncObject);
                    }

                    _isMetadataCached = true;
                }

                return _metadata;
            }
        }

        public virtual BitmapCodecInfo CodecInfo
        {
            get
            {
                VerifyAccess();
                EnsureBuiltInDecoder();

                if (_codecInfo == null)
                {
                    void* pCodecInfo = null;
                    lock (_syncObject)
                    {
                        HRESULT.Check(IWICBitmapDecoderExVtbl.GetDecoderInfo(
                            (void*)_decoderHandle.DangerousGetHandle(),
                            &pCodecInfo));
                    }

                    SafeMILHandle codecHandle = new SafeMILHandle((IntPtr)pCodecInfo);
                    _codecInfo = new BitmapCodecInfoInternal(codecHandle);
                }

                return _codecInfo;
            }
        }

        public virtual ReadOnlyCollection<BitmapFrame> Frames
        {
            get
            {
                VerifyAccess();
                EnsureBuiltInDecoder();

                if (_readOnlyFrames == null)
                {
                    EnsureFrames();
                }

                return _readOnlyFrames;
            }
        }

        public virtual BitmapSource Preview
        {
            get
            {
                VerifyAccess();
                EnsureBuiltInDecoder();

                if (!_isPreviewCached)
                {
                    void* pPreview = null;
                    int hr;
                    lock (_syncObject)
                    {
                        hr = IWICBitmapDecoderExVtbl.GetPreview(
                            (void*)_decoderHandle.DangerousGetHandle(),
                            &pPreview);
                    }

                    if (hr != (int)WinCodecErrors.WINCODEC_ERR_UNSUPPORTEDOPERATION)
                    {
                        HRESULT.Check(hr);
                    }

                    if (pPreview != null)
                    {
                        BitmapSourceSafeMILHandle previewHandle = new BitmapSourceSafeMILHandle((IntPtr)pPreview);
                        BitmapSourceSafeMILHandle cachedPreview = BitmapSource.CreateCachedBitmap(
                            null,
                            previewHandle,
                            BitmapCreateOptions.PreservePixelFormat,
                            _cacheOption == BitmapCacheOption.None ? BitmapCacheOption.OnDemand : _cacheOption,
                            null);

                        _preview = new UnmanagedBitmapWrapper(cachedPreview);
                        _preview.Freeze();
                    }

                    _isPreviewCached = true;
                }

                return _preview;
            }
        }

        public virtual bool IsDownloading
        {
            get
            {
                VerifyAccess();
                EnsureBuiltInDecoder();
                return false;
            }
        }

        #endregion Properties

        #region Events

        public virtual event EventHandler DownloadCompleted
        {
            add
            {
                VerifyAccess();
                EnsureBuiltInDecoder();
                _downloadEvent.AddEvent(value);
            }
            remove
            {
                VerifyAccess();
                EnsureBuiltInDecoder();
                _downloadEvent.RemoveEvent(value);
            }
        }

        public virtual event EventHandler<DownloadProgressEventArgs> DownloadProgress
        {
            add
            {
                VerifyAccess();
                EnsureBuiltInDecoder();
                _progressEvent.AddEvent(value);
            }
            remove
            {
                VerifyAccess();
                EnsureBuiltInDecoder();
                _progressEvent.RemoveEvent(value);
            }
        }

        public virtual event EventHandler<ExceptionEventArgs> DownloadFailed
        {
            add
            {
                VerifyAccess();
                EnsureBuiltInDecoder();
                _failedEvent.AddEvent(value);
            }
            remove
            {
                VerifyAccess();
                EnsureBuiltInDecoder();
                _failedEvent.RemoveEvent(value);
            }
        }

        #endregion Events

        #region Public Methods

        public virtual InPlaceBitmapMetadataWriter CreateInPlaceBitmapMetadataWriter()
        {
            VerifyAccess();
            EnsureBuiltInDecoder();
            CheckIfSiteOfOrigin();
            return InPlaceBitmapMetadataWriter.CreateFromDecoder(_decoderHandle, _syncObject);
        }

        public override string ToString()
        {
            VerifyAccess();

            if (!_isBuiltInDecoder)
            {
                return base.ToString();
            }

            if (_uri != null)
            {
                if (_baseUri != null)
                {
                    return new Uri(_baseUri, _uri).OriginalString;
                }

                return _uri.OriginalString;
            }

            return "image";
        }

        #endregion Public Methods

        #region Internal Methods & Setup

        internal SafeMILHandle InternalDecoder => _decoderHandle;

        internal virtual bool IsMetadataFixedSize => false;

        internal object SyncObject
        {
            get
            {
                Debug.Assert(_syncObject != null);
                return _syncObject;
            }
        }

        internal void CheckIfSiteOfOrigin()
        {
            // No CAS sandbox in NativeAOT CSUI; hook retained for WPF behavioral parity.
        }

        internal virtual void SealObject()
        {
            throw new NotImplementedException();
        }

        private void EnsureBuiltInDecoder()
        {
            if (!_isBuiltInDecoder)
            {
                throw new NotImplementedException();
            }
        }

        private void Initialize(Guid expectedClsId, Uri uri, Stream stream, bool closeStreamOnExit)
        {
            bool ownsStream = false;
            try
            {
                if (uri != null)
                {
                    Uri resolved = BindUriHelper.GetResolvedUri(null, uri);
                    if (string.Equals(resolved.Scheme, "pack", StringComparison.OrdinalIgnoreCase))
                    {
                        stream = OpenPackUriStream(resolved);
                        ownsStream = true;
                    }
                    else if (resolved.IsFile)
                    {
                        stream = new FileStream(resolved.LocalPath, FileMode.Open, FileAccess.Read, FileShare.Read);
                        ownsStream = true;
                    }
                    else
                    {
                        throw new NotSupportedException(SR.Image_CantDealWithUri);
                    }
                }

                _decoderHandle = SetupDecoderFromUriOrStream(
                    uri,
                    stream,
                    _cacheOption,
                    out Guid actualClsId,
                    out _,
                    out _,
                    out Stream tempStream,
                    out _);

                if (tempStream != null && !ReferenceEquals(tempStream, stream))
                {
                    if (ownsStream)
                    {
                        stream.Dispose();
                    }
                    stream = tempStream;
                    ownsStream = true;
                }

                if (expectedClsId != Guid.Empty && actualClsId != expectedClsId)
                {
                    throw new FileFormatException(uri, SR.Image_ContentTypeDoesNotMatchDecoder);
                }

                _stream = stream;
                Initialize(this);

                if ((ownsStream || closeStreamOnExit) && _stream != null && _cacheOption == BitmapCacheOption.OnLoad)
                {
                    _stream.Dispose();
                    _stream = null;
                }
            }
            catch
            {
                if (ownsStream)
                {
                    stream?.Dispose();
                }
                throw;
            }
        }

        internal static void Initialize(BitmapDecoder decoder)
        {
            decoder.EnsureFrames();
        }

        private void EnsureFrames()
        {
            if (_readOnlyFrames != null)
            {
                return;
            }

            uint frameCount = 0;
            lock (_syncObject)
            {
                HRESULT.Check(IWICBitmapDecoderVtbl.GetFrameCount(
                    (void*)_decoderHandle.DangerousGetHandle(),
                    out frameCount));
            }

            if (frameCount == 0)
            {
                throw new FileFormatException(_uri, SR.Image_NoDecodeFrames);
            }

            _frames = new List<BitmapFrame>((int)frameCount);
            for (int i = 0; i < (int)frameCount; i++)
            {
                _frames.Add(new BitmapFrameDecode(i, _createOptions, _cacheOption, this));
            }

            _readOnlyFrames = new ReadOnlyCollection<BitmapFrame>(_frames);
        }

        internal static SafeMILHandle SetupDecoderFromUriOrStream(
            Uri uri,
            Stream stream,
            BitmapCacheOption cacheOption,
            out Guid clsId,
            out bool isOriginalWritable,
            out Uri uriToDecode,
            out Stream streamToDecode,
            out object checkAccessObject)
        {
            isOriginalWritable = false;
            uriToDecode = uri;
            streamToDecode = stream;
            checkAccessObject = null;
            clsId = Guid.Empty;

            if (stream == null)
            {
                throw new ArgumentException(SR.Image_CantDealWithStream);
            }

            if (!stream.CanSeek)
            {
                MemoryStream buffered = new MemoryStream();
                stream.CopyTo(buffered);
                buffered.Seek(0, SeekOrigin.Begin);
                streamToDecode = buffered;
                stream = buffered;
            }

            int coInitHr = WicNativeExports.CoInitializeSTA();
            try
            {
                using FactoryMaker factoryMaker = new FactoryMaker();
                void* pFactory = (void*)factoryMaker.ImagingFactoryPtr;
                void* pDecoder = null;

                WICMetadataCacheOptions metadataOptions = cacheOption == BitmapCacheOption.OnLoad
                    ? WICMetadataCacheOptions.WICMetadataCacheOnLoad
                    : WICMetadataCacheOptions.WICMetadataCacheOnDemand;

                if (stream is FileStream fs)
                {
                    SafeFileHandle safeHandle = fs.SafeFileHandle;
                    bool addRef = false;
                    safeHandle.DangerousAddRef(ref addRef);
                    try
                    {
                        int hr = IWICImagingFactoryExVtbl.CreateDecoderFromFileHandle(
                            pFactory,
                            (nuint)safeHandle.DangerousGetHandle(),
                            null,
                            (uint)metadataOptions,
                            &pDecoder);

                        HRESULT.Check(hr);
                    }
                    finally
                    {
                        if (addRef)
                        {
                            safeHandle.DangerousRelease();
                        }
                    }
                }
                else
                {
                    IntPtr comStream = StreamAsIStream.IStreamFrom(stream);
                    try
                    {
                        HRESULT.Check(IWICImagingFactoryVtbl.CreateDecoderFromStream(
                            pFactory,
                            (void*)comStream,
                            null,
                            (uint)metadataOptions,
                            &pDecoder));
                    }
                    finally
                    {
                        if (comStream != IntPtr.Zero)
                        {
                            ComHelpers.Release((void*)comStream);
                        }
                    }
                }

                SafeMILHandle decoderHandle = new SafeMILHandle((IntPtr)pDecoder);
                clsId = GetCLSIDFromDecoder(decoderHandle, out _);
                return decoderHandle;
            }
            finally
            {
                if (coInitHr >= 0)
                {
                    WicNativeExports.CoUninitialize();
                }
            }
        }

        private static Guid GetCLSIDFromDecoder(SafeMILHandle decoderHandle, out string decoderMimeTypes)
        {
            decoderMimeTypes = string.Empty;
            void* pDecoderInfo = null;
            try
            {
                int hr = IWICBitmapDecoderExVtbl.GetDecoderInfo(
                    (void*)decoderHandle.DangerousGetHandle(),
                    &pDecoderInfo);

                if (HRESULT.Failed(hr) || pDecoderInfo == null)
                {
                    return Guid.Empty;
                }

                HRESULT.Check(IWICBitmapCodecInfoVtbl.GetContainerFormat(pDecoderInfo, out Guid clsId));
                int mimeHr = IWICBitmapCodecInfoVtbl.GetMimeTypes(pDecoderInfo, out string mimeTypes);
                if (HRESULT.Succeeded(mimeHr) && mimeTypes != null)
                {
                    decoderMimeTypes = mimeTypes;
                }

                return clsId;
            }
            finally
            {
                if (pDecoderInfo != null)
                {
                    ComHelpers.Release(pDecoderInfo);
                }
            }
        }

        #endregion Internal Methods & Setup

        #region Data Members

        private readonly bool _isBuiltInDecoder;
        private bool _isPaletteCached;
        private bool _isThumbnailCached;
        private bool _isMetadataCached;
        private bool _isColorContextCached;
        private bool _isPreviewCached;

        internal object _syncObject = new object();
        internal SafeMILHandle _decoderHandle;
        internal Uri _uri;
        internal Uri _baseUri;
        internal Stream _stream;
        internal BitmapCreateOptions _createOptions;
        internal BitmapCacheOption _cacheOption;

        internal BitmapPalette _palette;
        internal ReadOnlyCollection<ColorContext> _readOnlyColorContexts;
        internal BitmapSource _thumbnail;
        internal BitmapMetadata _metadata;
        internal BitmapCodecInfo _codecInfo;
        internal List<BitmapFrame> _frames;
        internal ReadOnlyCollection<BitmapFrame> _readOnlyFrames;
        internal BitmapSource _preview;

        internal UniqueEventHelper _downloadEvent = new UniqueEventHelper();
        internal UniqueEventHelper<DownloadProgressEventArgs> _progressEvent = new UniqueEventHelper<DownloadProgressEventArgs>();
        internal UniqueEventHelper<ExceptionEventArgs> _failedEvent = new UniqueEventHelper<ExceptionEventArgs>();

        #endregion Data Members
    }

    #endregion BitmapDecoder
}

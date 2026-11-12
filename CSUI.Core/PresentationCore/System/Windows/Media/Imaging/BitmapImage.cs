// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Net.Cache;
using System.Windows.Markup;
using CSUI.Core.Interop.DirectX;
using MS.Internal;
using MS.Internal.Utility;
using SR = MS.Internal.PresentationCore.SR;

namespace System.Windows.Media.Imaging
{
    #region BitmapImage

    /// <summary>
    /// BitmapImage provides a BitmapSource implementation that supports XAML initialization,
    /// progressive Uri download, cropping, scaling, and rotation during decoding.
    /// </summary>
    public sealed unsafe partial class BitmapImage : BitmapSource, ISupportInitialize, IUriContext
    {
        #region Constructors

        public BitmapImage()
            : base(true)
        {
        }

        public BitmapImage(Uri uriSource)
            : this(uriSource, null)
        {
        }

        public BitmapImage(Uri uriSource, RequestCachePolicy uriCachePolicy)
            : base(true)
        {
            ArgumentNullException.ThrowIfNull(uriSource);

            BeginInit();
            UriSource = uriSource;
            UriCachePolicy = uriCachePolicy;
            EndInit();
        }

        #endregion Constructors

        #region ISupportInitialize & IUriContext

        public void BeginInit()
        {
            WritePreamble();
            _bitmapInit.BeginInit();
        }

        public void EndInit()
        {
            WritePreamble();
            _bitmapInit.EndInit();

            if (UriSource == null && StreamSource == null)
            {
                throw new InvalidOperationException(SR.Format(SR.Image_NeitherArgument, "UriSource", "StreamSource"));
            }

            if (UriSource != null && !UriSource.IsAbsoluteUri && CacheOption != BitmapCacheOption.OnLoad)
            {
                DelayCreation = true;
            }

            FinalizeCreation();
        }

        Uri IUriContext.BaseUri
        {
            get => BaseUri;
            set => BaseUri = value;
        }

        public Uri BaseUri
        {
            get
            {
                ReadPreamble();
                return _baseUri;
            }
            set
            {
                WritePreamble();
                if (!IsFrozen && _baseUri != value)
                {
                    _baseUri = value;
                    WritePostscript();
                }
            }
        }

        #endregion ISupportInitialize & IUriContext

        #region Overrides

        public override bool IsDownloading
        {
            get
            {
                ReadPreamble();
                return _isDownloading;
            }
        }

        public override ImageMetadata Metadata => throw new NotSupportedException(SR.Image_MetadataNotSupported);

        internal override bool IsValidForFinalizeCreation(bool throwIfInvalid)
        {
            if (UriSource == null && StreamSource == null)
            {
                if (throwIfInvalid)
                {
                    throw new InvalidOperationException(SR.Format(SR.Image_NeitherArgument, "UriSource", "StreamSource"));
                }
                return false;
            }

            return true;
        }

        internal override bool CanSerializeToString()
        {
            ReadPreamble();
            return UriSource != null;
        }

        internal override string ConvertToString(string format, IFormatProvider provider)
        {
            ReadPreamble();
            if (UriSource != null)
            {
                if (_baseUri != null)
                {
                    return new Uri(_baseUri, UriSource).OriginalString;
                }

                return UriSource.OriginalString;
            }

            return base.ConvertToString(format, provider);
        }

        internal override void FinalizeCreation()
        {
            _bitmapInit.EnsureInitializedComplete();

            Uri uriToDecode = null;
            BitmapDecoder decoder = null;

            CheckIfSiteOfOrigin();

            if ((CreateOptions & BitmapCreateOptions.DelayCreation) != 0 && DelayCreation)
            {
                return;
            }

            if (UriSource != null)
            {
                uriToDecode = BindUriHelper.GetResolvedUri(_baseUri, UriSource);
            }

            // Look up cached decoded WIC handle if possible (when no transform options distort identity)
            if (uriToDecode != null &&
                StreamSource == null &&
                (CreateOptions & BitmapCreateOptions.IgnoreImageCache) == 0 &&
                CacheOption != BitmapCacheOption.None &&
                DecodePixelWidth == 0 &&
                DecodePixelHeight == 0 &&
                Rotation == Rotation.Rotate0 &&
                SourceRect.IsEmpty)
            {
                WeakReference cachedRef = ImagingCache.CheckImageCache(uriToDecode) as WeakReference;
                if (cachedRef?.Target is BitmapSourceSafeMILHandle cachedHandle && !cachedHandle.IsInvalid)
                {
                    lock (_syncObject)
                    {
                        WicSourceHandle = cachedHandle;
                        _isSourceCached = true;
                        CreationCompleted = true;
                        UpdateCachedSettings();
                        InvalidateD2DSource();
                        return;
                    }
                }
            }

            decoder = _cachedDecoder;
            if (decoder == null)
            {
                if (uriToDecode != null)
                {
                    decoder = BitmapDecoder.CreateFromUriOrStream(
                        _baseUri,
                        UriSource,
                        null,
                        CreateOptions & ~BitmapCreateOptions.DelayCreation,
                        CacheOption,
                        UriCachePolicy,
                        insertInDecoderCache: false);
                }
                else
                {
                    decoder = BitmapDecoder.CreateFromUriOrStream(
                        null,
                        null,
                        StreamSource,
                        CreateOptions & ~BitmapCreateOptions.DelayCreation,
                        CacheOption,
                        UriCachePolicy,
                        insertInDecoderCache: false);
                }
            }

            if (decoder.IsDownloading)
            {
                _isDownloading = true;
                _cachedDecoder = decoder;

                BitmapSource placeholder = CreatePlaceholderSource();
                lock (_syncObject)
                {
                    _wicSource = placeholder.WicSourceHandle;
                    _isSourceCached = true;
                    UpdateCachedSettings();
                }

                decoder.DownloadProgress += OnDownloadProgress;
                decoder.DownloadCompleted += OnDownloadCompleted;
                decoder.DownloadFailed += OnDownloadFailed;
                return;
            }

            BuildDecodedBitmap(decoder, uriToDecode);
        }

        private void BuildDecodedBitmap(BitmapDecoder decoder, Uri resolvedUri)
        {
            BitmapFrame frame = decoder.Frames[0];
            Int32Rect sourceRect = SourceRect;

            if (sourceRect.X == 0 && sourceRect.Y == 0 && sourceRect.Width == 0 && sourceRect.Height == 0)
            {
                sourceRect = Int32Rect.Empty;
            }

            BitmapSourceSafeMILHandle wicTransformer = frame.WicSourceHandle;
            BitmapSourceSafeMILHandle clipperHandle = null;
            BitmapSourceSafeMILHandle scalerHandle = null;
            BitmapSourceSafeMILHandle rotatorHandle = null;

            using (FactoryMaker factoryMaker = new FactoryMaker())
            {
                void* wicFactory = (void*)factoryMaker.ImagingFactoryPtr;

                if (!sourceRect.IsEmpty)
                {
                    void* pClipper = null;
                    HRESULT.Check(IWICImagingFactoryExVtbl.CreateBitmapClipper(wicFactory, &pClipper));
                    clipperHandle = new BitmapSourceSafeMILHandle((IntPtr)pClipper);

                    WICRect wicRect = new WICRect(sourceRect.X, sourceRect.Y, sourceRect.Width, sourceRect.Height);
                    lock (_syncObject)
                    {
                        bool addRefClip = false;
                        bool addRefSrc = false;
                        clipperHandle.DangerousAddRef(ref addRefClip);
                        wicTransformer.DangerousAddRef(ref addRefSrc);
                        try
                        {
                            HRESULT.Check(IWICBitmapClipperVtbl.Initialize(
                                (void*)clipperHandle.DangerousGetHandle(),
                                (void*)wicTransformer.DangerousGetHandle(),
                                &wicRect));
                        }
                        finally
                        {
                            if (addRefSrc) wicTransformer.DangerousRelease();
                            if (addRefClip) clipperHandle.DangerousRelease();
                        }
                    }

                    wicTransformer = clipperHandle;
                }

                int decodeWidth = DecodePixelWidth;
                int decodeHeight = DecodePixelHeight;

                if (decodeWidth != 0 || decodeHeight != 0)
                {
                    int srcWidth = sourceRect.IsEmpty ? frame.PixelWidth : sourceRect.Width;
                    int srcHeight = sourceRect.IsEmpty ? frame.PixelHeight : sourceRect.Height;

                    if (decodeWidth == 0 && srcHeight > 0)
                    {
                        decodeWidth = Math.Max(1, (int)Math.Round(srcWidth * (double)decodeHeight / srcHeight));
                    }
                    else if (decodeHeight == 0 && srcWidth > 0)
                    {
                        decodeHeight = Math.Max(1, (int)Math.Round(srcHeight * (double)decodeWidth / srcWidth));
                    }

                    void* pScaler = null;
                    HRESULT.Check(IWICImagingFactoryExVtbl.CreateBitmapScaler(wicFactory, &pScaler));
                    scalerHandle = new BitmapSourceSafeMILHandle((IntPtr)pScaler);

                    lock (_syncObject)
                    {
                        bool addRefScaler = false;
                        bool addRefSrc = false;
                        scalerHandle.DangerousAddRef(ref addRefScaler);
                        wicTransformer.DangerousAddRef(ref addRefSrc);
                        try
                        {
                            HRESULT.Check(IWICBitmapScalerVtbl.Initialize(
                                (void*)scalerHandle.DangerousGetHandle(),
                                (void*)wicTransformer.DangerousGetHandle(),
                                (uint)Math.Max(1, decodeWidth),
                                (uint)Math.Max(1, decodeHeight),
                                (uint)WICInterpolationMode.Fant));
                        }
                        finally
                        {
                            if (addRefSrc) wicTransformer.DangerousRelease();
                            if (addRefScaler) scalerHandle.DangerousRelease();
                        }
                    }

                    wicTransformer = scalerHandle;
                }

                WICBitmapTransformOptions transformOptions = RotationToWicTransform(Rotation);
                if (transformOptions != WICBitmapTransformOptions.WICBitmapTransformRotate0)
                {
                    BitmapSourceSafeMILHandle intermediate = CreateCachedBitmap(
                        frame,
                        wicTransformer,
                        CreateOptions | BitmapCreateOptions.PreservePixelFormat,
                        BitmapCacheOption.Default,
                        decoder.Palette);

                    void* pRotator = null;
                    HRESULT.Check(IWICImagingFactoryExVtbl.CreateBitmapFlipRotator(wicFactory, &pRotator));
                    rotatorHandle = new BitmapSourceSafeMILHandle((IntPtr)pRotator);

                    lock (_syncObject)
                    {
                        bool addRefRot = false;
                        bool addRefSrc = false;
                        rotatorHandle.DangerousAddRef(ref addRefRot);
                        intermediate.DangerousAddRef(ref addRefSrc);
                        try
                        {
                            HRESULT.Check(IWICBitmapFlipRotatorVtbl.Initialize(
                                (void*)rotatorHandle.DangerousGetHandle(),
                                (void*)intermediate.DangerousGetHandle(),
                                (uint)transformOptions));
                        }
                        finally
                        {
                            if (addRefSrc) intermediate.DangerousRelease();
                            if (addRefRot) rotatorHandle.DangerousRelease();
                        }
                    }

                    wicTransformer = rotatorHandle;
                }
            }

            BitmapSourceSafeMILHandle finalHandle = CreateCachedBitmap(
                frame,
                wicTransformer,
                CreateOptions,
                CacheOption,
                decoder.Palette);

            lock (_syncObject)
            {
                WicSourceHandle = finalHandle;
                _isSourceCached = (CacheOption != BitmapCacheOption.None);
                CreationCompleted = true;
                UpdateCachedSettings();
                InvalidateD2DSource();
            }

            if (resolvedUri != null &&
                StreamSource == null &&
                (CreateOptions & BitmapCreateOptions.IgnoreImageCache) == 0 &&
                CacheOption != BitmapCacheOption.None &&
                DecodePixelWidth == 0 &&
                DecodePixelHeight == 0 &&
                Rotation == Rotation.Rotate0 &&
                sourceRect.IsEmpty)
            {
                ImagingCache.AddToImageCache(resolvedUri, new WeakReference(finalHandle));
            }
        }

        private static WICBitmapTransformOptions RotationToWicTransform(Rotation rotation)
        {
            return rotation switch
            {
                Rotation.Rotate90 => WICBitmapTransformOptions.WICBitmapTransformRotate90,
                Rotation.Rotate180 => WICBitmapTransformOptions.WICBitmapTransformRotate180,
                Rotation.Rotate270 => WICBitmapTransformOptions.WICBitmapTransformRotate270,
                _ => WICBitmapTransformOptions.WICBitmapTransformRotate0,
            };
        }

        private static BitmapSource CreatePlaceholderSource()
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

            return new UnmanagedBitmapWrapper(new BitmapSourceSafeMILHandle((IntPtr)pBitmap));
        }

        #endregion Overrides

        #region Download Event Handlers

        private void OnDownloadCompleted(object sender, EventArgs e)
        {
            _isDownloading = false;

            if (_cachedDecoder != null)
            {
                _cachedDecoder.DownloadProgress -= OnDownloadProgress;
                _cachedDecoder.DownloadCompleted -= OnDownloadCompleted;
                _cachedDecoder.DownloadFailed -= OnDownloadFailed;
            }

            try
            {
                Uri resolvedUri = UriSource != null ? BindUriHelper.GetResolvedUri(_baseUri, UriSource) : null;
                BitmapDecoder decoder = (_cachedDecoder is LateBoundBitmapDecoder lateDecoder)
                    ? lateDecoder.Decoder
                    : _cachedDecoder;

                if (decoder != null)
                {
                    BuildDecodedBitmap(decoder, resolvedUri);
                }

                _cachedDecoder = null;
                _downloadEvent.InvokeEvents(this, e ?? EventArgs.Empty);
            }
            catch (Exception ex)
            {
                _cachedDecoder = null;
                RecoverFromDecodeFailure(ex);
            }
        }

        private void OnDownloadProgress(object sender, DownloadProgressEventArgs e)
        {
            _progressEvent.InvokeEvents(this, e);
        }

        private void OnDownloadFailed(object sender, ExceptionEventArgs e)
        {
            _isDownloading = false;

            if (_cachedDecoder != null)
            {
                _cachedDecoder.DownloadProgress -= OnDownloadProgress;
                _cachedDecoder.DownloadCompleted -= OnDownloadCompleted;
                _cachedDecoder.DownloadFailed -= OnDownloadFailed;
                _cachedDecoder = null;
            }

            _failedEvent.InvokeEvents(this, e);
        }

        #endregion Download Event Handlers

        #region Property Hooks & Coercion

        private void ClonePrequel(BitmapImage otherBitmapImage)
        {
            _bitmapInit.BeginInit();
            _isDownloading = otherBitmapImage._isDownloading;
            _cachedDecoder = otherBitmapImage._cachedDecoder;
            _baseUri = otherBitmapImage._baseUri;
        }

        private void ClonePostscript(BitmapImage otherBitmapImage)
        {
            if (_isDownloading && _cachedDecoder != null)
            {
                _cachedDecoder.DownloadCompleted += OnDownloadCompleted;
                _cachedDecoder.DownloadProgress += OnDownloadProgress;
                _cachedDecoder.DownloadFailed += OnDownloadFailed;
            }

            _bitmapInit.EndInit();
        }

        private void UriCachePolicyPropertyChangedHook(DependencyPropertyChangedEventArgs e)
        {
            if (!e.IsASubPropertyChange)
            {
                _uriCachePolicy = e.NewValue as RequestCachePolicy;
            }
        }

        private void UriSourcePropertyChangedHook(DependencyPropertyChangedEventArgs e)
        {
            if (!e.IsASubPropertyChange)
            {
                _uriSource = e.NewValue as Uri;
            }
        }

        private void StreamSourcePropertyChangedHook(DependencyPropertyChangedEventArgs e)
        {
            if (!e.IsASubPropertyChange)
            {
                _streamSource = e.NewValue as Stream;
            }
        }

        private void DecodePixelWidthPropertyChangedHook(DependencyPropertyChangedEventArgs e)
        {
            if (!e.IsASubPropertyChange)
            {
                _decodePixelWidth = (int)e.NewValue;
            }
        }

        private void DecodePixelHeightPropertyChangedHook(DependencyPropertyChangedEventArgs e)
        {
            if (!e.IsASubPropertyChange)
            {
                _decodePixelHeight = (int)e.NewValue;
            }
        }

        private void RotationPropertyChangedHook(DependencyPropertyChangedEventArgs e)
        {
            if (!e.IsASubPropertyChange)
            {
                _rotation = (Rotation)e.NewValue;
            }
        }

        private void SourceRectPropertyChangedHook(DependencyPropertyChangedEventArgs e)
        {
            if (!e.IsASubPropertyChange)
            {
                _sourceRect = (Int32Rect)e.NewValue;
            }
        }

        private void CreateOptionsPropertyChangedHook(DependencyPropertyChangedEventArgs e)
        {
            if (!e.IsASubPropertyChange)
            {
                _createOptions = (BitmapCreateOptions)e.NewValue;
                DelayCreation = ((_createOptions & BitmapCreateOptions.DelayCreation) != 0);
            }
        }

        private void CacheOptionPropertyChangedHook(DependencyPropertyChangedEventArgs e)
        {
            if (!e.IsASubPropertyChange)
            {
                _cacheOption = (BitmapCacheOption)e.NewValue;
            }
        }

        private static object CoerceUriCachePolicy(DependencyObject d, object value)
        {
            BitmapImage image = (BitmapImage)d;
            return !image._bitmapInit.IsInInit ? image._uriCachePolicy : value;
        }

        private static object CoerceUriSource(DependencyObject d, object value)
        {
            BitmapImage image = (BitmapImage)d;
            return !image._bitmapInit.IsInInit ? image._uriSource : value;
        }

        private static object CoerceStreamSource(DependencyObject d, object value)
        {
            BitmapImage image = (BitmapImage)d;
            return !image._bitmapInit.IsInInit ? image._streamSource : value;
        }

        private static object CoerceDecodePixelWidth(DependencyObject d, object value)
        {
            BitmapImage image = (BitmapImage)d;
            return !image._bitmapInit.IsInInit ? image._decodePixelWidth : value;
        }

        private static object CoerceDecodePixelHeight(DependencyObject d, object value)
        {
            BitmapImage image = (BitmapImage)d;
            return !image._bitmapInit.IsInInit ? image._decodePixelHeight : value;
        }

        private static object CoerceRotation(DependencyObject d, object value)
        {
            BitmapImage image = (BitmapImage)d;
            return !image._bitmapInit.IsInInit ? image._rotation : value;
        }

        private static object CoerceSourceRect(DependencyObject d, object value)
        {
            BitmapImage image = (BitmapImage)d;
            return !image._bitmapInit.IsInInit ? image._sourceRect : value;
        }

        private static object CoerceCreateOptions(DependencyObject d, object value)
        {
            BitmapImage image = (BitmapImage)d;
            return !image._bitmapInit.IsInInit ? image._createOptions : value;
        }

        private static object CoerceCacheOption(DependencyObject d, object value)
        {
            BitmapImage image = (BitmapImage)d;
            return !image._bitmapInit.IsInInit ? image._cacheOption : value;
        }

        #endregion Property Hooks & Coercion

        #region Data Members

        private Uri _baseUri;
        private bool _isDownloading;
        private BitmapDecoder _cachedDecoder;
        private RequestCachePolicy _uriCachePolicy;
        private Uri _uriSource;
        private Stream _streamSource;
        private int _decodePixelWidth;
        private int _decodePixelHeight;
        private Rotation _rotation;
        private Int32Rect _sourceRect = Int32Rect.Empty;
        private BitmapCreateOptions _createOptions;
        private BitmapCacheOption _cacheOption;

        #endregion Data Members
    }

    #endregion BitmapImage
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System.Collections.ObjectModel;
using System.IO;
using System.Net.Cache;
using MS.Internal;
using SR = MS.Internal.PresentationCore.SR;

namespace System.Windows.Media.Imaging
{
    #region LateBoundBitmapDecoder

    /// <summary>
    /// LateBoundBitmapDecoder is a decoder that delays decoding of an image until download completes.
    /// </summary>
    public sealed class LateBoundBitmapDecoder : BitmapDecoder
    {
        #region Constructors

        internal LateBoundBitmapDecoder(
            Uri baseUri,
            Uri uri,
            Stream stream,
            BitmapCreateOptions createOptions,
            BitmapCacheOption cacheOption,
            RequestCachePolicy requestCachePolicy)
            : base(true)
        {
            _baseUri = baseUri;
            _uri = uri;
            _stream = stream;
            _createOptions = createOptions;
            _cacheOption = cacheOption;
            _requestCachePolicy = requestCachePolicy;

            Uri resolvedUri = (_baseUri != null) ? new Uri(_baseUri, _uri) : _uri;

            _isDownloading = true;
            BitmapDownload.BeginDownload(
                this,
                resolvedUri,
                _requestCachePolicy,
                _stream);
        }

        #endregion Constructors

        #region Properties

        public override BitmapPalette Palette
        {
            get
            {
                VerifyAccess();

                if (_isDownloading)
                {
                    return null;
                }

                return _realDecoder.Palette;
            }
        }

        public override ReadOnlyCollection<ColorContext> ColorContexts
        {
            get
            {
                VerifyAccess();

                if (_isDownloading)
                {
                    return null;
                }

                return _realDecoder.ColorContexts;
            }
        }

        public override BitmapSource Thumbnail
        {
            get
            {
                VerifyAccess();

                if (_isDownloading)
                {
                    return null;
                }

                return _realDecoder.Thumbnail;
            }
        }

        public override BitmapMetadata Metadata
        {
            get
            {
                VerifyAccess();

                if (_isDownloading)
                {
                    return null;
                }

                return _realDecoder.Metadata;
            }
        }

        public override BitmapCodecInfo CodecInfo
        {
            get
            {
                VerifyAccess();

                if (_isDownloading)
                {
                    return null;
                }

                return _realDecoder.CodecInfo;
            }
        }

        public override ReadOnlyCollection<BitmapFrame> Frames
        {
            get
            {
                VerifyAccess();

                if (_isDownloading)
                {
                    if (_readOnlyFrames == null)
                    {
                        _frames = new System.Collections.Generic.List<BitmapFrame>(1);
                        _frames.Add(new BitmapFrameDecode(0, _createOptions, _cacheOption, this));
                        _readOnlyFrames = new ReadOnlyCollection<BitmapFrame>(_frames);
                    }

                    return _readOnlyFrames;
                }

                return _realDecoder.Frames;
            }
        }

        public override BitmapSource Preview
        {
            get
            {
                VerifyAccess();

                if (_isDownloading)
                {
                    return null;
                }

                return _realDecoder.Preview;
            }
        }

        public BitmapDecoder Decoder
        {
            get
            {
                VerifyAccess();
                return _realDecoder;
            }
        }

        public override bool IsDownloading
        {
            get
            {
                VerifyAccess();
                return _isDownloading;
            }
        }

        #endregion Properties

        #region Internal Methods

        internal override void SealObject()
        {
            throw new NotImplementedException();
        }

        internal object DownloadCallback(object arg)
        {
            Stream stream = (Stream)arg;
            return DownloadCallback(stream, null, null);
        }

        internal object DownloadCallback(Stream stream, string filePath, string contentType)
        {
            _stream = stream;

            if ((_createOptions & BitmapCreateOptions.IgnoreImageCache) != 0)
            {
                Uri resolvedUri = (_baseUri != null) ? new Uri(_baseUri, _uri) : _uri;
                ImagingCache.RemoveFromDecoderCache(resolvedUri);
            }

            try
            {
                _realDecoder = CreateFromUriOrStream(
                    _baseUri,
                    _uri,
                    _stream,
                    _createOptions & ~BitmapCreateOptions.DelayCreation,
                    _cacheOption,
                    _requestCachePolicy,
                    true);

                _isDownloading = false;

                if (_frames != null)
                {
                    for (int i = 0; i < _frames.Count; i++)
                    {
                        BitmapFrameDecode frame = _frames[i] as BitmapFrameDecode;
                        frame?.UpdateDecoder(_realDecoder);
                    }
                }

                _downloadEvent.InvokeEvents(this, null);
            }
            catch (Exception e)
            {
                ExceptionCallback(e);
            }

            return null;
        }

        internal object ProgressCallback(object arg)
        {
            int percentComplete = (int)arg;
            _progressEvent.InvokeEvents(this, new DownloadProgressEventArgs(percentComplete));
            return null;
        }

        internal object ExceptionCallback(object arg)
        {
            _isDownloading = false;

            if (_uri != null)
            {
                Uri resolvedUri = (_baseUri != null) ? new Uri(_baseUri, _uri) : _uri;
                ImagingCache.RemoveFromDecoderCache(resolvedUri);
            }

            _failedEvent.InvokeEvents(this, new ExceptionEventArgs((Exception)arg));
            return null;
        }

        #endregion Internal Methods

        #region Data Members

        private bool _isDownloading;
        private BitmapDecoder _realDecoder;
        private RequestCachePolicy _requestCachePolicy;

        #endregion Data Members
    }

    #endregion LateBoundBitmapDecoder
}

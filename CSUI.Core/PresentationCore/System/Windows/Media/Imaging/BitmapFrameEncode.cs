// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System.Collections.ObjectModel;
using System.Diagnostics;
using SR = MS.Internal.PresentationCore.SR;

namespace System.Windows.Media.Imaging
{
    #region BitmapFrameEncode

    /// <summary>
    /// BitmapFrameEncode class is a wrapper around a BitmapSource for encoding.
    /// </summary>
    internal sealed class BitmapFrameEncode : BitmapFrame
    {
        #region Constructors

        internal BitmapFrameEncode(
            BitmapSource source,
            BitmapSource thumbnail,
            BitmapMetadata metadata,
            ReadOnlyCollection<ColorContext> colorContexts)
            : base(true)
        {
            _bitmapInit.BeginInit();
            _source = source;

            // Create a cached copy if the source already is cached or if Virtuals are not used
            _syncObject = source.SyncObject;
            _isSourceCached = source.IsSourceCached;

            if (((BitmapSource)_source).ShouldCloneEventDelegates)
            {
                if (_source._downloadEvent != null)
                {
                    _downloadEvent = _source._downloadEvent.Clone();
                }

                if (_source._progressEvent != null)
                {
                    _progressEvent = _source._progressEvent.Clone();
                }

                if (_source._failedEvent != null)
                {
                    _failedEvent = _source._failedEvent.Clone();
                }

                if (_source._decodeFailedEvent != null)
                {
                    _decodeFailedEvent = _source._decodeFailedEvent.Clone();
                }
            }

            _wicSource = CreateCachedBitmap(
                null,
                _source.WicSourceHandle,
                BitmapCreateOptions.PreservePixelFormat,
                BitmapCacheOption.Default,
                _source.Palette);

            _isSourceCached = true;
            CreationCompleted = true;
            UpdateCachedSettings();

            _thumbnail = thumbnail;
            _readOnlycolorContexts = colorContexts;
            InternalMetadata = metadata;
            _needsUpdate = true;
            _bitmapInit.EndInit();
        }

        private BitmapFrameEncode()
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
                return null;
            }
            set
            {
                WritePreamble();
            }
        }

        public override BitmapSource Thumbnail => _thumbnail;

        public override ImageMetadata Metadata
        {
            get
            {
                ReadPreamble();
                return InternalMetadata;
            }
        }

        public override BitmapDecoder Decoder => null;

        public override ReadOnlyCollection<ColorContext> ColorContexts
        {
            get
            {
                ReadPreamble();
                return _readOnlycolorContexts;
            }
        }

        #endregion Public Properties

        #region Public Methods

        public override InPlaceBitmapMetadataWriter CreateInPlaceBitmapMetadataWriter()
        {
            ReadPreamble();
            CheckIfSiteOfOrigin();

            throw new InvalidOperationException(SR.Image_InplaceMetadataNoCopy);
        }

        #endregion Public Methods

        #region Freezable Overrides

        protected override Freezable CreateInstanceCore()
        {
            return new BitmapFrameEncode();
        }

        private void CopyCommon(BitmapFrameEncode sourceFrameEncode)
        {
            _bitmapInit.BeginInit();
            _source = sourceFrameEncode._source;
            _thumbnail = sourceFrameEncode._thumbnail;
            _readOnlycolorContexts = sourceFrameEncode._readOnlycolorContexts;
            if (sourceFrameEncode.InternalMetadata != null)
            {
                InternalMetadata = sourceFrameEncode.InternalMetadata.Clone();
            }
            _bitmapInit.EndInit();
        }

        protected override void CloneCore(Freezable sourceFreezable)
        {
            BitmapFrameEncode sourceFrameEncode = (BitmapFrameEncode)sourceFreezable;
            base.CloneCore(sourceFreezable);
            CopyCommon(sourceFrameEncode);
        }

        protected override void CloneCurrentValueCore(Freezable sourceFreezable)
        {
            BitmapFrameEncode sourceFrameEncode = (BitmapFrameEncode)sourceFreezable;
            base.CloneCurrentValueCore(sourceFreezable);
            CopyCommon(sourceFrameEncode);
        }

        protected override void GetAsFrozenCore(Freezable sourceFreezable)
        {
            BitmapFrameEncode sourceFrameEncode = (BitmapFrameEncode)sourceFreezable;
            base.GetAsFrozenCore(sourceFreezable);
            CopyCommon(sourceFrameEncode);
        }

        protected override void GetCurrentValueAsFrozenCore(Freezable sourceFreezable)
        {
            BitmapFrameEncode sourceFrameEncode = (BitmapFrameEncode)sourceFreezable;
            base.GetCurrentValueAsFrozenCore(sourceFreezable);
            CopyCommon(sourceFrameEncode);
        }

        #endregion Freezable Overrides

        #region Internal Properties

        internal override BitmapMetadata InternalMetadata
        {
            get
            {
                CheckIfSiteOfOrigin();
                return _metadata;
            }
            set
            {
                CheckIfSiteOfOrigin();
                _metadata = value;
            }
        }

        #endregion Internal Properties

        #region Data Members

        private BitmapSource _source;

        #endregion Data Members
    }

    #endregion BitmapFrameEncode
}

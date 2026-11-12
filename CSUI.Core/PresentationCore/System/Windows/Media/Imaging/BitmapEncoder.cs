// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Windows.Threading;
using CSUI.Core.Interop.DirectX;
using MS.Internal;
using PROPBAG2 = CSUI.Core.Interop.DirectX.PROPBAG2;
using SR = MS.Internal.PresentationCore.SR;

namespace System.Windows.Media.Imaging
{
    #region BitmapEncoder

    /// <summary>
    /// BitmapEncoder encodes a collection of BitmapFrame objects to an image stream.
    /// </summary>
    public abstract unsafe class BitmapEncoder : DispatcherObject
    {
        #region Constructors & Static Create

        internal static BitmapEncoder Create(Guid containerFormat)
        {
            if (containerFormat == Guid.Empty)
            {
                throw new ArgumentException(SR.Format(SR.Image_GuidEmpty, nameof(containerFormat)), nameof(containerFormat));
            }

            if (containerFormat == MILGuidData.GUID_ContainerFormatBmp)
            {
                return new BmpBitmapEncoder();
            }

            if (containerFormat == MILGuidData.GUID_ContainerFormatGif)
            {
                return new GifBitmapEncoder();
            }

            if (containerFormat == MILGuidData.GUID_ContainerFormatJpeg)
            {
                return new JpegBitmapEncoder();
            }

            if (containerFormat == MILGuidData.GUID_ContainerFormatPng)
            {
                return new PngBitmapEncoder();
            }

            if (containerFormat == MILGuidData.GUID_ContainerFormatTiff)
            {
                return new TiffBitmapEncoder();
            }

            if (containerFormat == MILGuidData.GUID_ContainerFormatWmp)
            {
                return new WmpBitmapEncoder();
            }

            return new UnknownBitmapEncoder(containerFormat);
        }

        protected BitmapEncoder()
        {
            _isBuiltInEncoder = false;
        }

        internal BitmapEncoder(bool isBuiltIn)
        {
            _isBuiltInEncoder = isBuiltIn;
        }

        #endregion Constructors & Static Create

        #region Public Properties

        public virtual BitmapPalette Palette
        {
            get
            {
                VerifyAccess();
                EnsureBuiltInEncoder();
                return _palette;
            }
            set
            {
                VerifyAccess();
                EnsureBuiltInEncoder();
                ArgumentNullException.ThrowIfNull(value);
                _palette = value;
            }
        }

        public virtual ReadOnlyCollection<ColorContext> ColorContexts
        {
            get
            {
                VerifyAccess();
                EnsureBuiltInEncoder();
                return _readOnlyColorContexts;
            }
            set
            {
                VerifyAccess();
                EnsureBuiltInEncoder();
                ArgumentNullException.ThrowIfNull(value);
                if (!SupportsColorContext)
                {
                    throw new InvalidOperationException(SR.Image_EncoderNoColorContext);
                }
                _readOnlyColorContexts = value;
            }
        }

        public virtual BitmapSource Thumbnail
        {
            get
            {
                VerifyAccess();
                EnsureBuiltInEncoder();
                return _thumbnail;
            }
            set
            {
                VerifyAccess();
                EnsureBuiltInEncoder();
                ArgumentNullException.ThrowIfNull(value);
                if (!SupportsGlobalThumbnail)
                {
                    throw new InvalidOperationException(SR.Image_EncoderNoGlobalThumbnail);
                }
                _thumbnail = value;
            }
        }

        public virtual BitmapMetadata Metadata
        {
            get
            {
                VerifyAccess();
                EnsureBuiltInEncoder();
                if (!SupportsGlobalMetadata)
                {
                    throw new InvalidOperationException(SR.Image_EncoderNoGlobalMetadata);
                }
                if (_metadata == null)
                {
                    _metadata = new BitmapMetadata(ContainerFormat);
                }
                return _metadata;
            }
            set
            {
                VerifyAccess();
                EnsureBuiltInEncoder();
                ArgumentNullException.ThrowIfNull(value);
                if (!SupportsGlobalMetadata)
                {
                    throw new InvalidOperationException(SR.Image_EncoderNoGlobalMetadata);
                }
                if (value.GuidFormat != ContainerFormat)
                {
                    throw new InvalidOperationException(SR.Image_MetadataNotCompatible);
                }
                _metadata = value;
            }
        }

        public virtual BitmapCodecInfo CodecInfo
        {
            get
            {
                VerifyAccess();
                EnsureBuiltInEncoder();
                EnsureUnmanagedEncoder();

                if (_codecInfo == null)
                {
                    void* pCodecInfo = null;
                    HRESULT.Check(IWICBitmapEncoderVtbl.GetEncoderInfo(
                        (void*)_encoderHandle.DangerousGetHandle(),
                        &pCodecInfo));

                    _codecInfo = new BitmapCodecInfoInternal(new SafeMILHandle((IntPtr)pCodecInfo));
                }

                return _codecInfo;
            }
        }

        public virtual IList<BitmapFrame> Frames
        {
            get
            {
                VerifyAccess();
                EnsureBuiltInEncoder();
                if (_frames == null)
                {
                    _frames = new List<BitmapFrame>(0);
                }
                return _frames;
            }
            set
            {
                VerifyAccess();
                EnsureBuiltInEncoder();
                ArgumentNullException.ThrowIfNull(value);
                _frames = value;
            }
        }

        public virtual BitmapSource Preview
        {
            get
            {
                VerifyAccess();
                EnsureBuiltInEncoder();
                return _preview;
            }
            set
            {
                VerifyAccess();
                EnsureBuiltInEncoder();
                ArgumentNullException.ThrowIfNull(value);
                if (!SupportsPreview)
                {
                    throw new InvalidOperationException(SR.Image_EncoderNoPreview);
                }
                _preview = value;
            }
        }

        #endregion Public Properties

        #region Save Methods

        public virtual void Save(Stream stream)
        {
            VerifyAccess();
            EnsureBuiltInEncoder();
            EnsureUnmanagedEncoder();

            if (_saveState != SaveState.Created)
            {
                throw new InvalidOperationException(SR.Image_OnlyOneSave);
            }

            ArgumentNullException.ThrowIfNull(stream);

            if (_frames == null || _frames.Count == 0)
            {
                throw new InvalidOperationException(SR.Image_NoFrames);
            }

            if (_frames.Count > 1 && !SupportsMultipleFrames)
            {
                throw new InvalidOperationException(SR.Image_NoFrames);
            }

            IntPtr comStream = IntPtr.Zero;
            try
            {
                _saveState = SaveState.Saving;
                void* pEncoder = (void*)_encoderHandle.DangerousGetHandle();

                comStream = StreamAsIStream.IStreamFrom(stream);
                HRESULT.Check(IWICBitmapEncoderVtbl.Initialize(
                    pEncoder,
                    (void*)comStream,
                    (uint)WICBitmapEncodeCacheOption.WICBitmapEncodeNoCache));

                if (SupportsPreview && _preview != null)
                {
                    BitmapSourceSafeMILHandle previewHandle = _preview.WicSourceHandle;
                    lock (_preview.SyncObject)
                    {
                        HRESULT.Check(IWICBitmapEncoderVtbl.SetPreview(
                            pEncoder,
                            (void*)previewHandle.DangerousGetHandle()));
                    }
                }

                if (SupportsGlobalThumbnail && _thumbnail != null)
                {
                    BitmapSourceSafeMILHandle thumbHandle = _thumbnail.WicSourceHandle;
                    lock (_thumbnail.SyncObject)
                    {
                        HRESULT.Check(IWICBitmapEncoderVtbl.SetThumbnail(
                            pEncoder,
                            (void*)thumbHandle.DangerousGetHandle()));
                    }
                }

                if (_palette != null && _palette.Colors != null && _palette.Colors.Count > 0)
                {
                    SafeMILHandle palHandle = _palette.InternalPalette;
                    HRESULT.Check(IWICBitmapEncoderVtbl.SetPalette(
                        pEncoder,
                        (void*)palHandle.DangerousGetHandle()));
                }

                if (SupportsColorContext && _readOnlyColorContexts != null && _readOnlyColorContexts.Count > 0)
                {
                    IntPtr[] ctxPtrs = new IntPtr[_readOnlyColorContexts.Count];
                    for (int i = 0; i < _readOnlyColorContexts.Count; i++)
                    {
                        ctxPtrs[i] = _readOnlyColorContexts[i].ColorContextHandle.DangerousGetHandle();
                    }

                    fixed (IntPtr* pCtx = ctxPtrs)
                    {
                        int hr = IWICBitmapEncoderVtbl.SetColorContexts(pEncoder, (uint)ctxPtrs.Length, (void**)pCtx);
                        if (hr != (int)WinCodecErrors.WINCODEC_ERR_UNSUPPORTEDOPERATION)
                        {
                            HRESULT.Check(hr);
                        }
                    }
                }

                if (SupportsGlobalMetadata && _metadata != null && _metadata.InternalMetadataHandle != null)
                {
                    void* pQueryWriter = null;
                    int hr = IWICBitmapEncoderVtbl.GetMetadataQueryWriter(pEncoder, &pQueryWriter);
                    if (hr != (int)WinCodecErrors.WINCODEC_ERR_UNSUPPORTEDOPERATION)
                    {
                        HRESULT.Check(hr);
                        if (pQueryWriter != null)
                        {
                            SafeMILHandle writerHandle = new SafeMILHandle((IntPtr)pQueryWriter);
                            BitmapMetadata targetMetadata = new BitmapMetadata(writerHandle, readOnly: false, fixedSize: false, writerHandle);
                            targetMetadata.SetQuery("/", _metadata);
                        }
                    }
                }

                int count = SupportsMultipleFrames ? _frames.Count : 1;
                for (int i = 0; i < count; i++)
                {
                    BitmapFrame frame = _frames[i];
                    if (frame == null)
                    {
                        continue;
                    }

                    void* pFrameEncode = null;
                    void* pEncoderOptions = null;
                    HRESULT.Check(IWICBitmapEncoderVtbl.CreateNewFrame(
                        pEncoder,
                        &pFrameEncode,
                        &pEncoderOptions));

                    using SafeMILHandle frameEncodeHandle = new SafeMILHandle((IntPtr)pFrameEncode);
                    using SafeMILHandle encoderOptionsHandle = pEncoderOptions != null
                        ? new SafeMILHandle((IntPtr)pEncoderOptions)
                        : null;

                    SaveFrame(frameEncodeHandle, encoderOptionsHandle, frame);
                }

                HRESULT.Check(IWICBitmapEncoderVtbl.Commit(pEncoder));
                _saveState = SaveState.Saved;
            }
            catch
            {
                _saveState = SaveState.Created;
                throw;
            }
            finally
            {
                if (comStream != IntPtr.Zero)
                {
                    ComHelpers.Release((void*)comStream);
                }
            }
        }

        internal void SaveFrame(SafeMILHandle frameEncodeHandle, SafeMILHandle encoderOptions, BitmapFrame frame)
        {
            SetupFrame(frameEncodeHandle, encoderOptions);

            void* pFrameEncode = (void*)frameEncodeHandle.DangerousGetHandle();
            HRESULT.Check(IWICBitmapFrameEncodeVtbl.SetSize(
                pFrameEncode,
                (uint)Math.Max(1, frame.PixelWidth),
                (uint)Math.Max(1, frame.PixelHeight)));

            double dpiX = frame.DpiX > 0 ? frame.DpiX : 96.0;
            double dpiY = frame.DpiY > 0 ? frame.DpiY : 96.0;
            HRESULT.Check(IWICBitmapFrameEncodeVtbl.SetResolution(pFrameEncode, dpiX, dpiY));

            if (SupportsColorContext)
            {
                ReadOnlyCollection<ColorContext> frameContexts = frame.ColorContexts;
                if (frameContexts != null && frameContexts.Count > 0)
                {
                    IntPtr[] ctxPtrs = new IntPtr[frameContexts.Count];
                    for (int i = 0; i < frameContexts.Count; i++)
                    {
                        ctxPtrs[i] = frameContexts[i].ColorContextHandle.DangerousGetHandle();
                    }

                    fixed (IntPtr* pCtx = ctxPtrs)
                    {
                        int ctxHr = IWICBitmapFrameEncodeVtbl.SetColorContexts(pFrameEncode, (uint)ctxPtrs.Length, (void**)pCtx);
                        if (ctxHr != (int)WinCodecErrors.WINCODEC_ERR_UNSUPPORTEDOPERATION)
                        {
                            HRESULT.Check(ctxHr);
                        }
                    }
                }
            }

            if (SupportsFrameThumbnails && frame.Thumbnail != null)
            {
                BitmapSource thumb = frame.Thumbnail;
                BitmapSourceSafeMILHandle thumbHandle = thumb.WicSourceHandle;
                lock (thumb.SyncObject)
                {
                    int thumbHr = IWICBitmapFrameEncodeVtbl.SetThumbnail(
                        pFrameEncode,
                        (void*)thumbHandle.DangerousGetHandle());
                    if (thumbHr != (int)WinCodecErrors.WINCODEC_ERR_UNSUPPORTEDOPERATION)
                    {
                        HRESULT.Check(thumbHr);
                    }
                }
            }

            BitmapPalette encodePalette = frame.Palette ?? _palette;
            void* palettePtr = (encodePalette != null && encodePalette.Colors != null && encodePalette.Colors.Count > 0)
                ? (void*)encodePalette.InternalPalette.DangerousGetHandle()
                : null;

            BitmapSourceSafeMILHandle frameSourceHandle = frame.WicSourceHandle;
            void* convertedSource = null;

            lock (frame.SyncObject)
            {
                bool addRefSrc = false;
                frameSourceHandle.DangerousAddRef(ref addRefSrc);
                try
                {
                    HRESULT.Check(WicNativeExports.SetEncoderFormat(
                        (void*)frameSourceHandle.DangerousGetHandle(),
                        palettePtr,
                        pFrameEncode,
                        out convertedSource));

                    if (SupportsFrameMetadata)
                    {
                        BitmapMetadata frameMetadata = frame.InternalMetadata;
                        if (frameMetadata != null &&
                            frameMetadata.InternalMetadataHandle != null &&
                            !frameMetadata.InternalMetadataHandle.IsInvalid)
                        {
                            void* pQueryWriter = null;
                            int metaHr = IWICBitmapFrameEncodeVtbl.GetMetadataQueryWriter(pFrameEncode, &pQueryWriter);
                            if (metaHr != (int)WinCodecErrors.WINCODEC_ERR_UNSUPPORTEDOPERATION)
                            {
                                HRESULT.Check(metaHr);
                                if (pQueryWriter != null)
                                {
                                    SafeMILHandle writerHandle = new SafeMILHandle((IntPtr)pQueryWriter);
                                    BitmapMetadata targetMetadata = new BitmapMetadata(writerHandle, readOnly: false, fixedSize: false, writerHandle);
                                    targetMetadata.SetQuery("/", frameMetadata);
                                }
                            }
                        }
                    }

                    // Pass null rect so WIC encodes the entire frame surface without triggering E_INVALIDARG.
                    HRESULT.Check(IWICBitmapFrameEncodeVtbl.WriteSource(
                        pFrameEncode,
                        convertedSource,
                        null));
                }
                finally
                {
                    if (convertedSource != null)
                    {
                        ComHelpers.Release(convertedSource);
                    }

                    if (addRefSrc)
                    {
                        frameSourceHandle.DangerousRelease();
                    }
                }
            }

            HRESULT.Check(IWICBitmapFrameEncodeVtbl.Commit(pFrameEncode));
        }

        #endregion Save Methods

        #region Internal Helpers

        internal virtual Guid ContainerFormat => Guid.Empty;
        internal virtual bool SupportsPreview => true;
        internal virtual bool SupportsGlobalThumbnail => true;
        internal virtual bool SupportsGlobalMetadata => true;
        internal virtual bool SupportsFrameThumbnails => true;
        internal virtual bool SupportsFrameMetadata => true;
        internal virtual bool SupportsMultipleFrames => true;
        internal virtual bool SupportsColorContext => true;

        internal virtual void SetupFrame(SafeMILHandle frameEncodeHandle, SafeMILHandle encoderOptions)
        {
            HRESULT.Check(IWICBitmapFrameEncodeVtbl.Initialize(
                (void*)frameEncodeHandle.DangerousGetHandle(),
                null));
        }

        internal virtual void SealObject()
        {
            throw new NotImplementedException();
        }

        internal static void WritePropertyBagOption(SafeMILHandle encoderOptions, string name, object value)
        {
            if (encoderOptions == null || encoderOptions.IsInvalid)
            {
                return;
            }

            PROPBAG2 propBag = default;
            PROPVARIANT propValue = default;
            try
            {
                propBag.Init(name);
                propValue.Init(value);
                HRESULT.Check(IPropertyBag2Vtbl.Write(
                    (void*)encoderOptions.DangerousGetHandle(),
                    1,
                    ref propBag,
                    &propValue));
            }
            finally
            {
                propBag.Clear();
                propValue.Clear();
            }
        }

        private void EnsureBuiltInEncoder()
        {
            if (!_isBuiltInEncoder)
            {
                throw new NotImplementedException();
            }
        }

        internal void EnsureUnmanagedEncoder()
        {
            if (_encoderHandle != null && !_encoderHandle.IsInvalid)
            {
                return;
            }

            using FactoryMaker factoryMaker = new FactoryMaker();
            void* pEncoder = null;
            Guid containerFormat = ContainerFormat;
            Guid vendorMicrosoft = MILGuidData.GUID_VendorMicrosoftGuid;

            int hr = IWICImagingFactoryExVtbl.CreateEncoder(
                (void*)factoryMaker.ImagingFactoryPtr,
                in containerFormat,
                &vendorMicrosoft,
                &pEncoder);

            if (HRESULT.Failed(hr))
            {
                hr = IWICImagingFactoryExVtbl.CreateEncoder(
                    (void*)factoryMaker.ImagingFactoryPtr,
                    in containerFormat,
                    null,
                    &pEncoder);
            }

            HRESULT.Check(hr);
            _encoderHandle = new SafeMILHandle((IntPtr)pEncoder);
        }

        #endregion Internal Helpers

        #region Data Members

        private enum SaveState : int
        {
            Created = 0,
            Saving = 1,
            Saved = 2
        }

        private readonly bool _isBuiltInEncoder;
        private SaveState _saveState = SaveState.Created;
        internal SafeMILHandle _encoderHandle;
        internal BitmapPalette _palette;
        internal ReadOnlyCollection<ColorContext> _readOnlyColorContexts;
        internal BitmapSource _thumbnail;
        internal BitmapMetadata _metadata;
        internal BitmapCodecInfo _codecInfo;
        internal IList<BitmapFrame> _frames;
        internal BitmapSource _preview;

        #endregion Data Members
    }

    #endregion BitmapEncoder
}

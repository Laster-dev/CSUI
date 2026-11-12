// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using CSUI.Core.Interop.DirectX;
using MS.Internal;

namespace System.Windows.Media.Imaging
{
    #region TiffCompressOption

    /// <summary>
    /// Compression options for TIFF encoding.
    /// </summary>
    public enum TiffCompressOption
    {
        Default = 0,
        None = 1,
        Ccitt3 = 2,
        Ccitt4 = 3,
        Lzw = 4,
        Rle = 5,
        Zip = 6
    }

    #endregion TiffCompressOption

    #region TiffBitmapEncoder

    /// <summary>
    /// TiffBitmapEncoder encodes BitmapFrames into the TIFF image format.
    /// </summary>
    public sealed unsafe class TiffBitmapEncoder : BitmapEncoder
    {
        public TiffBitmapEncoder()
            : base(true)
        {
            _supportsPreview = false;
            _supportsGlobalThumbnail = false;
            _supportsGlobalMetadata = false;
            _supportsFrameThumbnails = false;
            _supportsFrameMetadata = true;
            _supportsMultipleFrames = true;
            _supportsColorContext = false;
        }

        /// <summary>
        /// Compression algorithm used for TIFF encoding.
        /// </summary>
        public TiffCompressOption Compression
        {
            get => _compression;
            set => _compression = value;
        }

        internal override Guid ContainerFormat => _containerFormat;
        internal override bool SupportsPreview => _supportsPreview;
        internal override bool SupportsGlobalThumbnail => _supportsGlobalThumbnail;
        internal override bool SupportsGlobalMetadata => _supportsGlobalMetadata;
        internal override bool SupportsFrameThumbnails => _supportsFrameThumbnails;
        internal override bool SupportsFrameMetadata => _supportsFrameMetadata;
        internal override bool SupportsMultipleFrames => _supportsMultipleFrames;
        internal override bool SupportsColorContext => _supportsColorContext;

        internal override void SetupFrame(SafeMILHandle frameEncodeHandle, SafeMILHandle encoderOptions)
        {
            if (encoderOptions != null && !encoderOptions.IsInvalid && _compression != c_defaultCompression)
            {
                WritePropertyBagOption(encoderOptions, "TiffCompressionMethod", (byte)_compression);
            }

            HRESULT.Check(IWICBitmapFrameEncodeVtbl.Initialize(
                (void*)frameEncodeHandle.DangerousGetHandle(),
                encoderOptions != null ? (void*)encoderOptions.DangerousGetHandle() : null));
        }

        internal override void SealObject()
        {
            throw new NotImplementedException();
        }

        private const TiffCompressOption c_defaultCompression = TiffCompressOption.Default;

        private readonly Guid _containerFormat = MILGuidData.GUID_ContainerFormatTiff;
        private readonly bool _supportsPreview;
        private readonly bool _supportsGlobalThumbnail;
        private readonly bool _supportsGlobalMetadata;
        private readonly bool _supportsFrameThumbnails;
        private readonly bool _supportsFrameMetadata;
        private readonly bool _supportsMultipleFrames;
        private readonly bool _supportsColorContext;
        private TiffCompressOption _compression = c_defaultCompression;
    }

    #endregion TiffBitmapEncoder
}

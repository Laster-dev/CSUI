// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using CSUI.Core.Interop.DirectX;
using MS.Internal;

namespace System.Windows.Media.Imaging
{
    #region BmpBitmapEncoder

    /// <summary>
    /// BmpBitmapEncoder encodes BitmapFrames into the BMP image format.
    /// </summary>
    public sealed unsafe class BmpBitmapEncoder : BitmapEncoder
    {
        public BmpBitmapEncoder()
            : base(true)
        {
            _supportsPreview = false;
            _supportsGlobalThumbnail = false;
            _supportsGlobalMetadata = false;
            _supportsFrameThumbnails = false;
            _supportsFrameMetadata = false;
            _supportsMultipleFrames = false;
            _supportsColorContext = false;
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
            HRESULT.Check(IWICBitmapFrameEncodeVtbl.Initialize(
                (void*)frameEncodeHandle.DangerousGetHandle(),
                encoderOptions != null ? (void*)encoderOptions.DangerousGetHandle() : null));
        }

        internal override void SealObject()
        {
            throw new NotImplementedException();
        }

        private readonly Guid _containerFormat = MILGuidData.GUID_ContainerFormatBmp;
        private readonly bool _supportsPreview;
        private readonly bool _supportsGlobalThumbnail;
        private readonly bool _supportsGlobalMetadata;
        private readonly bool _supportsFrameThumbnails;
        private readonly bool _supportsFrameMetadata;
        private readonly bool _supportsMultipleFrames;
        private readonly bool _supportsColorContext;
    }

    #endregion BmpBitmapEncoder
}

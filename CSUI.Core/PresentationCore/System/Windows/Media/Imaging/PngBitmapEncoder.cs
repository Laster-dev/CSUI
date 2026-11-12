// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using CSUI.Core.Interop.DirectX;
using MS.Internal;

namespace System.Windows.Media.Imaging
{
    #region PngInterlaceOption

    /// <summary>
    /// Interlacing options for PNG encoding.
    /// </summary>
    public enum PngInterlaceOption
    {
        /// <summary>Use default interlacing setting.</summary>
        Default = 0,
        /// <summary>Use Adam7 interlacing.</summary>
        On = 1,
        /// <summary>Do not interlace.</summary>
        Off = 2
    }

    #endregion PngInterlaceOption

    #region PngBitmapEncoder

    /// <summary>
    /// PngBitmapEncoder encodes BitmapFrames into the PNG image format.
    /// </summary>
    public sealed unsafe class PngBitmapEncoder : BitmapEncoder
    {
        public PngBitmapEncoder()
            : base(true)
        {
            _supportsPreview = false;
            _supportsGlobalThumbnail = false;
            _supportsGlobalMetadata = false;
            _supportsFrameThumbnails = false;
            _supportsFrameMetadata = true;
            _supportsMultipleFrames = false;
            _supportsColorContext = true;
        }

        /// <summary>
        /// Interlace setting for PNG encoding.
        /// </summary>
        public PngInterlaceOption Interlace
        {
            get => _interlaceOption;
            set => _interlaceOption = value;
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
            if (encoderOptions != null && !encoderOptions.IsInvalid && _interlaceOption != PngInterlaceOption.Default)
            {
                WritePropertyBagOption(encoderOptions, "InterlaceOption", _interlaceOption == PngInterlaceOption.On);
            }

            HRESULT.Check(IWICBitmapFrameEncodeVtbl.Initialize(
                (void*)frameEncodeHandle.DangerousGetHandle(),
                encoderOptions != null ? (void*)encoderOptions.DangerousGetHandle() : null));
        }

        internal override void SealObject()
        {
            throw new NotImplementedException();
        }

        private const PngInterlaceOption c_defaultInterlaceOption = PngInterlaceOption.Default;

        private readonly Guid _containerFormat = MILGuidData.GUID_ContainerFormatPng;
        private readonly bool _supportsPreview;
        private readonly bool _supportsGlobalThumbnail;
        private readonly bool _supportsGlobalMetadata;
        private readonly bool _supportsFrameThumbnails;
        private readonly bool _supportsFrameMetadata;
        private readonly bool _supportsMultipleFrames;
        private readonly bool _supportsColorContext;
        private PngInterlaceOption _interlaceOption = c_defaultInterlaceOption;
    }

    #endregion PngBitmapEncoder
}

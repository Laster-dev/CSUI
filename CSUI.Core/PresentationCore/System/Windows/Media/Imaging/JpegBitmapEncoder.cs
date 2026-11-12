// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using CSUI.Core.Interop.DirectX;
using MS.Internal;
using SR = MS.Internal.PresentationCore.SR;

namespace System.Windows.Media.Imaging
{
    #region JpegBitmapEncoder

    /// <summary>
    /// JpegBitmapEncoder encodes BitmapFrames into the JPEG image format.
    /// </summary>
    public sealed unsafe class JpegBitmapEncoder : BitmapEncoder
    {
        public JpegBitmapEncoder()
            : base(true)
        {
            _supportsPreview = false;
            _supportsGlobalThumbnail = false;
            _supportsGlobalMetadata = false;
            _supportsFrameThumbnails = true;
            _supportsFrameMetadata = true;
            _supportsMultipleFrames = false;
            _supportsColorContext = false;
        }

        /// <summary>
        /// Quality level between 1 and 100 inclusive (default 75).
        /// </summary>
        public int QualityLevel
        {
            get => _qualityLevel;
            set
            {
                ArgumentOutOfRangeException.ThrowIfLessThan(value, 1);
                ArgumentOutOfRangeException.ThrowIfGreaterThan(value, 100);
                _qualityLevel = value;
            }
        }

        /// <summary>
        /// Horizontal flip flag during encoding.
        /// </summary>
        public bool FlipHorizontal
        {
            get => Convert.ToBoolean((int)_transformation & (int)WICBitmapTransformOptions.WICBitmapTransformFlipHorizontal);
            set
            {
                if (value != FlipHorizontal)
                {
                    if (value)
                    {
                        _transformation |= WICBitmapTransformOptions.WICBitmapTransformFlipHorizontal;
                    }
                    else
                    {
                        _transformation &= ~WICBitmapTransformOptions.WICBitmapTransformFlipHorizontal;
                    }
                }
            }
        }

        /// <summary>
        /// Vertical flip flag during encoding.
        /// </summary>
        public bool FlipVertical
        {
            get => Convert.ToBoolean((int)_transformation & (int)WICBitmapTransformOptions.WICBitmapTransformFlipVertical);
            set
            {
                if (value != FlipVertical)
                {
                    if (value)
                    {
                        _transformation |= WICBitmapTransformOptions.WICBitmapTransformFlipVertical;
                    }
                    else
                    {
                        _transformation &= ~WICBitmapTransformOptions.WICBitmapTransformFlipVertical;
                    }
                }
            }
        }

        /// <summary>
        /// Lossless orthogonal rotation applied during encoding.
        /// </summary>
        public Rotation Rotation
        {
            get
            {
                if (Rotate90)
                {
                    return Rotation.Rotate90;
                }
                if (Rotate180)
                {
                    return Rotation.Rotate180;
                }
                if (Rotate270)
                {
                    return Rotation.Rotate270;
                }
                return Rotation.Rotate0;
            }
            set
            {
                bool flipH = FlipHorizontal;
                bool flipV = FlipVertical;
                _transformation = value switch
                {
                    Rotation.Rotate0 => WICBitmapTransformOptions.WICBitmapTransformRotate0,
                    Rotation.Rotate90 => WICBitmapTransformOptions.WICBitmapTransformRotate90,
                    Rotation.Rotate180 => WICBitmapTransformOptions.WICBitmapTransformRotate180,
                    Rotation.Rotate270 => WICBitmapTransformOptions.WICBitmapTransformRotate270,
                    _ => throw new ArgumentException(SR.Format(SR.Enum_Invalid, nameof(Rotation)), nameof(value)),
                };
                FlipHorizontal = flipH;
                FlipVertical = flipV;
            }
        }

        private bool Rotate90 =>
            Convert.ToBoolean((int)_transformation & (int)WICBitmapTransformOptions.WICBitmapTransformRotate90) && !Rotate270;

        private bool Rotate180 =>
            Convert.ToBoolean((int)_transformation & (int)WICBitmapTransformOptions.WICBitmapTransformRotate180) && !Rotate270;

        private bool Rotate270 =>
            ((int)_transformation & (int)WICBitmapTransformOptions.WICBitmapTransformRotate270) == (int)WICBitmapTransformOptions.WICBitmapTransformRotate270;

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
            if (encoderOptions != null && !encoderOptions.IsInvalid)
            {
                if (_qualityLevel != c_defaultQualityLevel)
                {
                    WritePropertyBagOption(encoderOptions, "ImageQuality", _qualityLevel / 100.0f);
                }

                if (_transformation != c_defaultTransformation)
                {
                    WritePropertyBagOption(encoderOptions, "BitmapTransform", (byte)_transformation);
                }
            }

            HRESULT.Check(IWICBitmapFrameEncodeVtbl.Initialize(
                (void*)frameEncodeHandle.DangerousGetHandle(),
                encoderOptions != null ? (void*)encoderOptions.DangerousGetHandle() : null));
        }

        internal override void SealObject()
        {
            throw new NotImplementedException();
        }

        private const int c_defaultQualityLevel = 75;
        private const WICBitmapTransformOptions c_defaultTransformation = WICBitmapTransformOptions.WICBitmapTransformRotate0;

        private readonly Guid _containerFormat = MILGuidData.GUID_ContainerFormatJpeg;
        private readonly bool _supportsPreview;
        private readonly bool _supportsGlobalThumbnail;
        private readonly bool _supportsGlobalMetadata;
        private readonly bool _supportsFrameThumbnails;
        private readonly bool _supportsFrameMetadata;
        private readonly bool _supportsMultipleFrames;
        private readonly bool _supportsColorContext;
        private int _qualityLevel = c_defaultQualityLevel;
        private WICBitmapTransformOptions _transformation = c_defaultTransformation;
    }

    #endregion JpegBitmapEncoder
}

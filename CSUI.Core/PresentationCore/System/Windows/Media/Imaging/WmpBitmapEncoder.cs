// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using CSUI.Core.Interop.DirectX;
using MS.Internal;
using SR = MS.Internal.PresentationCore.SR;

namespace System.Windows.Media.Imaging
{
    #region WmpBitmapEncoder

    /// <summary>
    /// WmpBitmapEncoder encodes BitmapFrames into Windows Media Photo / JPEG XR format.
    /// </summary>
    public sealed unsafe class WmpBitmapEncoder : BitmapEncoder
    {
        public WmpBitmapEncoder()
            : base(true)
        {
            _supportsPreview = false;
            _supportsGlobalThumbnail = false;
            _supportsGlobalMetadata = false;
            _supportsFrameThumbnails = true;
            _supportsFrameMetadata = true;
            _supportsMultipleFrames = false;
            _supportsColorContext = true;
        }

        public float ImageQualityLevel
        {
            get => _imagequalitylevel;
            set
            {
                ArgumentOutOfRangeException.ThrowIfNegative(value);
                ArgumentOutOfRangeException.ThrowIfGreaterThan(value, 1.0f);
                _imagequalitylevel = value;
            }
        }

        public bool Lossless
        {
            get => _lossless;
            set => _lossless = value;
        }

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

        public bool UseCodecOptions
        {
            get => _usecodecoptions;
            set => _usecodecoptions = value;
        }

        public byte QualityLevel
        {
            get => _qualitylevel;
            set
            {
                ArgumentOutOfRangeException.ThrowIfZero(value);
                _qualitylevel = value;
            }
        }

        public byte SubsamplingLevel
        {
            get => _subsamplinglevel;
            set
            {
                ArgumentOutOfRangeException.ThrowIfGreaterThan(value, 3);
                _subsamplinglevel = value;
            }
        }

        public byte OverlapLevel
        {
            get => _overlaplevel;
            set
            {
                ArgumentOutOfRangeException.ThrowIfGreaterThan(value, 2);
                _overlaplevel = value;
            }
        }

        public short HorizontalTileSlices
        {
            get => _horizontaltileslices;
            set
            {
                ArgumentOutOfRangeException.ThrowIfNegative(value);
                ArgumentOutOfRangeException.ThrowIfGreaterThan(value, 4096);
                _horizontaltileslices = value;
            }
        }

        public short VerticalTileSlices
        {
            get => _verticaltileslices;
            set
            {
                ArgumentOutOfRangeException.ThrowIfNegative(value);
                ArgumentOutOfRangeException.ThrowIfGreaterThan(value, 4096);
                _verticaltileslices = value;
            }
        }

        public bool FrequencyOrder
        {
            get => _frequencyorder;
            set => _frequencyorder = value;
        }

        public bool InterleavedAlpha
        {
            get => _interleavedalpha;
            set => _interleavedalpha = value;
        }

        public byte AlphaQualityLevel
        {
            get => _alphaqualitylevel;
            set => _alphaqualitylevel = value;
        }

        public bool CompressedDomainTranscode
        {
            get => _compresseddomaintranscode;
            set => _compresseddomaintranscode = value;
        }

        public bool IgnoreOverlap
        {
            get => _ignoreoverlap;
            set => _ignoreoverlap = value;
        }

        public byte ImageDataDiscard
        {
            get => _imagedatadiscard;
            set
            {
                ArgumentOutOfRangeException.ThrowIfGreaterThan(value, 3);
                _imagedatadiscard = value;
            }
        }

        public byte AlphaDataDiscard
        {
            get => _alphadatadiscard;
            set
            {
                ArgumentOutOfRangeException.ThrowIfGreaterThan(value, 4);
                _alphadatadiscard = value;
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
                if (_imagequalitylevel != c_defaultImageQualityLevel)
                {
                    WritePropertyBagOption(encoderOptions, "ImageQuality", _imagequalitylevel);
                }

                if (_lossless != c_defaultLossless)
                {
                    WritePropertyBagOption(encoderOptions, "Lossless", _lossless);
                }

                if (_transformation != c_defaultTransformation)
                {
                    WritePropertyBagOption(encoderOptions, "BitmapTransform", (byte)_transformation);
                }

                if (_usecodecoptions != c_defaultUseCodecOptions)
                {
                    WritePropertyBagOption(encoderOptions, "UseCodecOptions", _usecodecoptions);
                }

                if (_qualitylevel != c_defaultQualityLevel)
                {
                    WritePropertyBagOption(encoderOptions, "Quality", _qualitylevel);
                }

                if (_subsamplinglevel != c_defaultSubsamplingLevel)
                {
                    WritePropertyBagOption(encoderOptions, "Subsampling", _subsamplinglevel);
                }

                if (_overlaplevel != c_defaultOverlapLevel)
                {
                    WritePropertyBagOption(encoderOptions, "Overlap", _overlaplevel);
                }

                if (_horizontaltileslices != c_defaultHorizontalTileSlices)
                {
                    WritePropertyBagOption(encoderOptions, "HorizontalTileSlices", (ushort)_horizontaltileslices);
                }

                if (_verticaltileslices != c_defaultVerticalTileSlices)
                {
                    WritePropertyBagOption(encoderOptions, "VerticalTileSlices", (ushort)_verticaltileslices);
                }

                if (_frequencyorder != c_defaultFrequencyOrder)
                {
                    WritePropertyBagOption(encoderOptions, "FrequencyOrder", _frequencyorder);
                }

                if (_interleavedalpha != c_defaultInterleavedAlpha)
                {
                    WritePropertyBagOption(encoderOptions, "InterleavedAlpha", _interleavedalpha);
                }

                if (_alphaqualitylevel != c_defaultAlphaQualityLevel)
                {
                    WritePropertyBagOption(encoderOptions, "AlphaQuality", _alphaqualitylevel);
                }

                if (_compresseddomaintranscode != c_defaultCompressedDomainTranscode)
                {
                    WritePropertyBagOption(encoderOptions, "CompressedDomainTranscode", _compresseddomaintranscode);
                }

                if (_ignoreoverlap != c_defaultIgnoreOverlap)
                {
                    WritePropertyBagOption(encoderOptions, "IgnoreOverlap", _ignoreoverlap);
                }

                if (_imagedatadiscard != c_defaultImageDataDiscard)
                {
                    WritePropertyBagOption(encoderOptions, "ImageDataDiscard", _imagedatadiscard);
                }

                if (_alphadatadiscard != c_defaultAlphaDataDiscard)
                {
                    WritePropertyBagOption(encoderOptions, "AlphaDataDiscard", _alphadatadiscard);
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

        private const float c_defaultImageQualityLevel = 0.9f;
        private const bool c_defaultLossless = false;
        private const WICBitmapTransformOptions c_defaultTransformation = WICBitmapTransformOptions.WICBitmapTransformRotate0;
        private const bool c_defaultUseCodecOptions = false;
        private const byte c_defaultQualityLevel = 10;
        private const byte c_defaultSubsamplingLevel = 3;
        private const byte c_defaultOverlapLevel = 1;
        private const short c_defaultHorizontalTileSlices = 0;
        private const short c_defaultVerticalTileSlices = 0;
        private const bool c_defaultFrequencyOrder = true;
        private const bool c_defaultInterleavedAlpha = false;
        private const byte c_defaultAlphaQualityLevel = 1;
        private const bool c_defaultCompressedDomainTranscode = true;
        private const bool c_defaultIgnoreOverlap = false;
        private const byte c_defaultImageDataDiscard = 0;
        private const byte c_defaultAlphaDataDiscard = 0;

        private readonly Guid _containerFormat = MILGuidData.GUID_ContainerFormatWmp;
        private readonly bool _supportsPreview;
        private readonly bool _supportsGlobalThumbnail;
        private readonly bool _supportsGlobalMetadata;
        private readonly bool _supportsFrameThumbnails;
        private readonly bool _supportsFrameMetadata;
        private readonly bool _supportsMultipleFrames;
        private readonly bool _supportsColorContext;

        private float _imagequalitylevel = c_defaultImageQualityLevel;
        private bool _lossless = c_defaultLossless;
        private WICBitmapTransformOptions _transformation = c_defaultTransformation;
        private bool _usecodecoptions = c_defaultUseCodecOptions;
        private byte _qualitylevel = c_defaultQualityLevel;
        private byte _subsamplinglevel = c_defaultSubsamplingLevel;
        private byte _overlaplevel = c_defaultOverlapLevel;
        private short _horizontaltileslices = c_defaultHorizontalTileSlices;
        private short _verticaltileslices = c_defaultVerticalTileSlices;
        private bool _frequencyorder = c_defaultFrequencyOrder;
        private bool _interleavedalpha = c_defaultInterleavedAlpha;
        private byte _alphaqualitylevel = c_defaultAlphaQualityLevel;
        private bool _compresseddomaintranscode = c_defaultCompressedDomainTranscode;
        private bool _ignoreoverlap = c_defaultIgnoreOverlap;
        private byte _imagedatadiscard = c_defaultImageDataDiscard;
        private byte _alphadatadiscard = c_defaultAlphaDataDiscard;
    }

    #endregion WmpBitmapEncoder
}

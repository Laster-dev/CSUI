// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using CSUI.Core.Interop.DirectX;
using MS.Internal;

namespace System.Windows.Media.Imaging
{
    /// <summary>
    /// Codec info for a given Encoder/Decoder.
    /// </summary>
    public abstract unsafe class BitmapCodecInfo
    {
        protected BitmapCodecInfo()
        {
        }

        internal BitmapCodecInfo(SafeMILHandle codecInfoHandle)
        {
            ArgumentNullException.ThrowIfNull(codecInfoHandle);
            _isBuiltIn = true;
            _codecInfoHandle = codecInfoHandle;
        }

        public virtual Guid ContainerFormat
        {
            get
            {
                EnsureBuiltIn();
                HRESULT.Check(IWICBitmapCodecInfoVtbl.GetContainerFormat(GetPtr(), out Guid containerFormat));
                return containerFormat;
            }
        }

        public virtual string Author
        {
            get
            {
                EnsureBuiltIn();
                HRESULT.Check(IWICComponentInfoVtbl.GetAuthor(GetPtr(), out string author));
                return author ?? string.Empty;
            }
        }

        public virtual Version Version
        {
            get
            {
                EnsureBuiltIn();
                HRESULT.Check(IWICComponentInfoVtbl.GetVersion(GetPtr(), out string version));
                return !string.IsNullOrEmpty(version) && Version.TryParse(version, out Version parsed) ? parsed : new Version();
            }
        }

        public virtual Version SpecificationVersion
        {
            get
            {
                EnsureBuiltIn();
                HRESULT.Check(IWICComponentInfoVtbl.GetSpecVersion(GetPtr(), out string specVersion));
                return !string.IsNullOrEmpty(specVersion) && Version.TryParse(specVersion, out Version parsed) ? parsed : new Version();
            }
        }

        public virtual string FriendlyName
        {
            get
            {
                EnsureBuiltIn();
                HRESULT.Check(IWICComponentInfoVtbl.GetFriendlyName(GetPtr(), out string friendlyName));
                return friendlyName ?? string.Empty;
            }
        }

        public virtual string DeviceManufacturer
        {
            get
            {
                EnsureBuiltIn();
                HRESULT.Check(IWICBitmapCodecInfoVtbl.GetDeviceManufacturer(GetPtr(), out string deviceManufacturer));
                return deviceManufacturer ?? string.Empty;
            }
        }

        public virtual string DeviceModels
        {
            get
            {
                EnsureBuiltIn();
                HRESULT.Check(IWICBitmapCodecInfoVtbl.GetDeviceModels(GetPtr(), out string deviceModels));
                return deviceModels ?? string.Empty;
            }
        }

        public virtual string MimeTypes
        {
            get
            {
                EnsureBuiltIn();
                HRESULT.Check(IWICBitmapCodecInfoVtbl.GetMimeTypes(GetPtr(), out string mimeTypes));
                return mimeTypes ?? string.Empty;
            }
        }

        public virtual string FileExtensions
        {
            get
            {
                EnsureBuiltIn();
                HRESULT.Check(IWICBitmapCodecInfoVtbl.GetFileExtensions(GetPtr(), out string fileExtensions));
                return fileExtensions ?? string.Empty;
            }
        }

        public virtual bool SupportsAnimation
        {
            get
            {
                EnsureBuiltIn();
                HRESULT.Check(IWICBitmapCodecInfoVtbl.DoesSupportAnimation(GetPtr(), out bool supportsAnimation));
                return supportsAnimation;
            }
        }

        public virtual bool SupportsLossless
        {
            get
            {
                EnsureBuiltIn();
                HRESULT.Check(IWICBitmapCodecInfoVtbl.DoesSupportLossless(GetPtr(), out bool supportsLossless));
                return supportsLossless;
            }
        }

        public virtual bool SupportsMultipleFrames
        {
            get
            {
                EnsureBuiltIn();
                HRESULT.Check(IWICBitmapCodecInfoVtbl.DoesSupportMultiframe(GetPtr(), out bool supportsMultiFrame));
                return supportsMultiFrame;
            }
        }

        private void* GetPtr() => (void*)_codecInfoHandle.DangerousGetHandle();

        private void EnsureBuiltIn()
        {
            if (!_isBuiltIn)
            {
                throw new NotImplementedException();
            }
        }

        private readonly bool _isBuiltIn;
        private readonly SafeMILHandle _codecInfoHandle;
    }
}

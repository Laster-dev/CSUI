// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using CSUI.Core.Interop.DirectX;
using MS.Internal;
using SR = MS.Internal.PresentationCore.SR;

namespace System.Windows.Media.Imaging
{
    /// <summary>
    /// Fast in-place metadata writer backed by IWICFastMetadataEncoder.
    /// </summary>
    public sealed unsafe partial class InPlaceBitmapMetadataWriter : BitmapMetadata
    {
        private InPlaceBitmapMetadataWriter()
        {
        }

        internal InPlaceBitmapMetadataWriter(
            SafeMILHandle fmeHandle,
            SafeMILHandle metadataHandle,
            object syncObject)
            : base(metadataHandle, readOnly: false, fixedSize: false, syncObject)
        {
            _fmeHandle = fmeHandle;
        }

        internal static InPlaceBitmapMetadataWriter CreateFromFrameDecode(BitmapSourceSafeMILHandle frameHandle, object syncObject)
        {
            Invariant.Assert(frameHandle != null);

            void* fmePtr = null;
            void* metadataPtr = null;

            using (var factoryMaker = new FactoryMaker())
            {
                lock (syncObject)
                {
                    HRESULT.Check(IWICImagingFactoryExVtbl.CreateFastMetadataEncoderFromFrameDecode(
                        (void*)factoryMaker.ImagingFactoryPtr,
                        (void*)frameHandle.DangerousGetHandle(),
                        &fmePtr));
                }
            }

            var fmeHandle = new SafeMILHandle((IntPtr)fmePtr);
            HRESULT.Check(IWICFastMetadataEncoderVtbl.GetMetadataQueryWriter(fmePtr, &metadataPtr));
            var metadataHandle = new SafeMILHandle((IntPtr)metadataPtr);

            return new InPlaceBitmapMetadataWriter(fmeHandle, metadataHandle, syncObject);
        }

        internal static InPlaceBitmapMetadataWriter CreateFromDecoder(SafeMILHandle decoderHandle, object syncObject)
        {
            Invariant.Assert(decoderHandle != null);

            void* fmePtr = null;
            void* metadataPtr = null;

            using (var factoryMaker = new FactoryMaker())
            {
                lock (syncObject)
                {
                    HRESULT.Check(IWICImagingFactoryExVtbl.CreateFastMetadataEncoderFromDecoder(
                        (void*)factoryMaker.ImagingFactoryPtr,
                        (void*)decoderHandle.DangerousGetHandle(),
                        &fmePtr));
                }
            }

            var fmeHandle = new SafeMILHandle((IntPtr)fmePtr);
            HRESULT.Check(IWICFastMetadataEncoderVtbl.GetMetadataQueryWriter(fmePtr, &metadataPtr));
            var metadataHandle = new SafeMILHandle((IntPtr)metadataPtr);

            return new InPlaceBitmapMetadataWriter(fmeHandle, metadataHandle, syncObject);
        }

        public bool TrySave()
        {
            Invariant.Assert(_fmeHandle != null);

            int hr;
            lock (SyncObject)
            {
                hr = IWICFastMetadataEncoderVtbl.Commit((void*)_fmeHandle.DangerousGetHandle());
            }

            return HRESULT.Succeeded(hr);
        }

        public new InPlaceBitmapMetadataWriter Clone()
        {
            return (InPlaceBitmapMetadataWriter)base.Clone();
        }

        protected override Freezable CreateInstanceCore()
        {
            throw new InvalidOperationException(SR.Image_InplaceMetadataNoCopy);
        }

        protected override void CloneCore(Freezable sourceFreezable)
        {
            throw new InvalidOperationException(SR.Image_InplaceMetadataNoCopy);
        }

        protected override void CloneCurrentValueCore(Freezable sourceFreezable)
        {
            throw new InvalidOperationException(SR.Image_InplaceMetadataNoCopy);
        }

        protected override void GetAsFrozenCore(Freezable sourceFreezable)
        {
            throw new InvalidOperationException(SR.Image_InplaceMetadataNoCopy);
        }

        protected override void GetCurrentValueAsFrozenCore(Freezable sourceFreezable)
        {
            throw new InvalidOperationException(SR.Image_InplaceMetadataNoCopy);
        }

        private readonly SafeMILHandle _fmeHandle;
    }
}

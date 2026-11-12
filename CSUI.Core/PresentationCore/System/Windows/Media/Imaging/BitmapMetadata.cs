// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Threading;
using CSUI.Core.Interop;
using CSUI.Core.Interop.DirectX;
using MS.Internal;
using SR = MS.Internal.PresentationCore.SR;

namespace System.Windows.Media.Imaging
{
    /// <summary>
    /// Metadata Class for BitmapSource with NativeAOT [UnmanagedCallersOnly] COM CCWs
    /// (zero [ComImport] / zero Marshal.GetComInterfaceForObject).
    /// </summary>
    public unsafe partial class BitmapMetadata : ImageMetadata, IEnumerable, IEnumerable<string>
    {
        private const int S_OK = 0;
        private const int S_FALSE = 1;
        private const int E_NOINTERFACE = unchecked((int)0x80004002);
        private const int E_POINTER = unchecked((int)0x80004003);
        private const int E_FAIL = unchecked((int)0x80004005);

        #region BitmapMetadataBlockWriter

        internal sealed class BitmapMetadataBlockWriter
        {
            [StructLayout(LayoutKind.Sequential)]
            private struct NativeBlockWriter
            {
                public void** Vtbl;
                public nint Handle;
                public int RefCount;
            }

            private static readonly void** s_vtbl;

            static BitmapMetadataBlockWriter()
            {
                s_vtbl = (void**)NativeMemory.AllocZeroed(12, (nuint)sizeof(void*));
                s_vtbl[0] = (delegate* unmanaged[Stdcall]<NativeBlockWriter*, Guid*, void**, int>)&ComQueryInterface;
                s_vtbl[1] = (delegate* unmanaged[Stdcall]<NativeBlockWriter*, uint>)&ComAddRef;
                s_vtbl[2] = (delegate* unmanaged[Stdcall]<NativeBlockWriter*, uint>)&ComRelease;
                s_vtbl[3] = (delegate* unmanaged[Stdcall]<NativeBlockWriter*, Guid*, int>)&ComGetContainerFormat;
                s_vtbl[4] = (delegate* unmanaged[Stdcall]<NativeBlockWriter*, uint*, int>)&ComGetCount;
                s_vtbl[5] = (delegate* unmanaged[Stdcall]<NativeBlockWriter*, uint, void**, int>)&ComGetReaderByIndex;
                s_vtbl[6] = (delegate* unmanaged[Stdcall]<NativeBlockWriter*, void**, int>)&ComGetEnumerator;
                s_vtbl[7] = (delegate* unmanaged[Stdcall]<NativeBlockWriter*, void*, int>)&ComInitializeFromBlockReader;
                s_vtbl[8] = (delegate* unmanaged[Stdcall]<NativeBlockWriter*, uint, void**, int>)&ComGetWriterByIndex;
                s_vtbl[9] = (delegate* unmanaged[Stdcall]<NativeBlockWriter*, void*, int>)&ComAddWriter;
                s_vtbl[10] = (delegate* unmanaged[Stdcall]<NativeBlockWriter*, uint, void*, int>)&ComSetWriterByIndex;
                s_vtbl[11] = (delegate* unmanaged[Stdcall]<NativeBlockWriter*, uint, int>)&ComRemoveWriterByIndex;
            }

            internal BitmapMetadataBlockWriter(Guid containerFormat, bool fixedSize)
            {
                _fixedSize = fixedSize;
                _containerFormat = containerFormat;
                _metadataBlocks = new List<SafeMILHandle>();
            }

            internal BitmapMetadataBlockWriter(BitmapMetadataBlockWriter blockWriter, object syncObject)
            {
                Guid guidVendor = MILGuidData.GUID_VendorMicrosoftGuid;
                _fixedSize = blockWriter._fixedSize;
                _containerFormat = blockWriter._containerFormat;
                _metadataBlocks = new List<SafeMILHandle>();

                List<SafeMILHandle> metadataBlocks = blockWriter.MetadataBlocks;

                using var factoryMaker = new FactoryMaker();
                void* componentFactory = null;
                try
                {
                    HRESULT.Check(ComPtr<byte>.QueryInterface(
                        (void*)factoryMaker.ImagingFactoryPtr,
                        MILGuidData.IID_IWICComponentFactory,
                        &componentFactory));

                    foreach (SafeMILHandle metadataHandle in metadataBlocks)
                    {
                        void* pIMetadataWriter = null;
                        try
                        {
                            lock (syncObject)
                            {
                                HRESULT.Check(IWICImagingFactoryExVtbl.CreateMetadataWriterFromReader(
                                    componentFactory,
                                    (void*)metadataHandle.DangerousGetHandle(),
                                    &guidVendor,
                                    &pIMetadataWriter));
                            }

                            _metadataBlocks.Add(new SafeMILHandle((IntPtr)pIMetadataWriter));
                            pIMetadataWriter = null;
                        }
                        finally
                        {
                            if (pIMetadataWriter != null)
                            {
                                ComHelpers.Release(pIMetadataWriter);
                            }
                        }
                    }
                }
                finally
                {
                    if (componentFactory != null)
                    {
                        ComHelpers.Release(componentFactory);
                    }
                }
            }

            internal void* CreateComWrapper()
            {
                NativeBlockWriter* native = (NativeBlockWriter*)NativeMemory.Alloc((nuint)sizeof(NativeBlockWriter));
                native->Vtbl = s_vtbl;
                native->Handle = GCHandle.ToIntPtr(GCHandle.Alloc(this));
                native->RefCount = 1;
                return native;
            }

            private static BitmapMetadataBlockWriter Unwrap(NativeBlockWriter* self)
            {
                if (self == null || self->Handle == 0)
                {
                    return null;
                }

                return GCHandle.FromIntPtr(self->Handle).Target as BitmapMetadataBlockWriter;
            }

            internal List<SafeMILHandle> MetadataBlocks => _metadataBlocks;

            [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
            private static int ComQueryInterface(NativeBlockWriter* self, Guid* riid, void** ppvObject)
            {
                if (riid == null || ppvObject == null)
                {
                    return E_POINTER;
                }

                if (*riid == WicGuidsEx.IID_IUnknown ||
                    *riid == WicGuidsEx.IID_IWICMetadataBlockReader ||
                    *riid == WicGuidsEx.IID_IWICMetadataBlockWriter)
                {
                    *ppvObject = self;
                    Interlocked.Increment(ref self->RefCount);
                    return S_OK;
                }

                *ppvObject = null;
                return E_NOINTERFACE;
            }

            [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
            private static uint ComAddRef(NativeBlockWriter* self)
            {
                return self == null ? 0 : (uint)Interlocked.Increment(ref self->RefCount);
            }

            [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
            private static uint ComRelease(NativeBlockWriter* self)
            {
                if (self == null)
                {
                    return 0;
                }

                int remaining = Interlocked.Decrement(ref self->RefCount);
                if (remaining == 0)
                {
                    if (self->Handle != 0)
                    {
                        GCHandle.FromIntPtr(self->Handle).Free();
                        self->Handle = 0;
                    }

                    NativeMemory.Free(self);
                    return 0;
                }

                return (uint)remaining;
            }

            [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
            private static int ComGetContainerFormat(NativeBlockWriter* self, Guid* pguidContainerFormat)
            {
                if (pguidContainerFormat == null) return E_POINTER;
                var writer = Unwrap(self);
                if (writer == null) return E_FAIL;

                *pguidContainerFormat = writer._containerFormat;
                return S_OK;
            }

            [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
            private static int ComGetCount(NativeBlockWriter* self, uint* pcCount)
            {
                if (pcCount == null) return E_POINTER;
                var writer = Unwrap(self);
                if (writer == null) return E_FAIL;

                *pcCount = (uint)writer._metadataBlocks.Count;
                return S_OK;
            }

            [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
            private static int ComGetReaderByIndex(NativeBlockWriter* self, uint nIndex, void** ppIMetadataReader)
            {
                if (ppIMetadataReader == null) return E_POINTER;
                *ppIMetadataReader = null;

                var writer = Unwrap(self);
                if (writer == null) return E_FAIL;

                if (nIndex >= writer._metadataBlocks.Count)
                {
                    return (int)WinCodecErrors.WINCODEC_ERR_PROPERTYNOTFOUND;
                }

                SafeMILHandle metadataReader = writer._metadataBlocks[(int)nIndex];
                return ComPtr<byte>.QueryInterface(
                    (void*)metadataReader.DangerousGetHandle(),
                    MILGuidData.IID_IWICMetadataReader,
                    ppIMetadataReader);
            }

            [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
            private static int ComGetEnumerator(NativeBlockWriter* self, void** ppIEnumMetadata)
            {
                if (ppIEnumMetadata == null) return E_POINTER;
                *ppIEnumMetadata = null;

                var writer = Unwrap(self);
                if (writer == null) return E_FAIL;

                try
                {
                    var enumerator = new BitmapMetadataBlockWriterEnumerator(writer);
                    *ppIEnumMetadata = enumerator.CreateComWrapper();
                    return S_OK;
                }
                catch (Exception e)
                {
                    return e.HResult != 0 ? e.HResult : E_FAIL;
                }
            }

            [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
            private static int ComInitializeFromBlockReader(NativeBlockWriter* self, void* pIBlockReader)
            {
                var writer = Unwrap(self);
                if (writer == null) return E_FAIL;
                if (pIBlockReader == null) return (int)WinCodecErrors.WINCODEC_ERR_INVALIDPARAMETER;

                try
                {
                    uint count = 0;
                    Guid guidVendor = MILGuidData.GUID_VendorMicrosoftGuid;
                    var metadataBlocks = new List<SafeMILHandle>();

                    int hr = IWICMetadataBlockReaderVtbl.GetCount(pIBlockReader, out count);
                    if (HRESULT.Succeeded(hr) && count > 0)
                    {
                        using var factoryMaker = new FactoryMaker();
                        void* componentFactory = null;
                        try
                        {
                            hr = ComPtr<byte>.QueryInterface(
                                (void*)factoryMaker.ImagingFactoryPtr,
                                MILGuidData.IID_IWICComponentFactory,
                                &componentFactory);

                            for (uint i = 0; i < count && HRESULT.Succeeded(hr); i++)
                            {
                                void* pIMetadataReader = null;
                                void* pIMetadataWriter = null;
                                try
                                {
                                    hr = IWICMetadataBlockReaderVtbl.GetReaderByIndex(pIBlockReader, i, &pIMetadataReader);
                                    if (HRESULT.Succeeded(hr))
                                    {
                                        hr = IWICImagingFactoryExVtbl.CreateMetadataWriterFromReader(
                                            componentFactory,
                                            pIMetadataReader,
                                            &guidVendor,
                                            &pIMetadataWriter);
                                        if (HRESULT.Succeeded(hr))
                                        {
                                            metadataBlocks.Add(new SafeMILHandle((IntPtr)pIMetadataWriter));
                                            pIMetadataWriter = null;
                                        }
                                    }
                                }
                                finally
                                {
                                    if (pIMetadataReader != null) ComHelpers.Release(pIMetadataReader);
                                    if (pIMetadataWriter != null) ComHelpers.Release(pIMetadataWriter);
                                }
                            }
                        }
                        finally
                        {
                            if (componentFactory != null) ComHelpers.Release(componentFactory);
                        }
                    }

                    if (HRESULT.Succeeded(hr))
                    {
                        writer._metadataBlocks = metadataBlocks;
                    }

                    return hr;
                }
                catch (Exception e)
                {
                    return e.HResult != 0 ? e.HResult : E_FAIL;
                }
            }

            [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
            private static int ComGetWriterByIndex(NativeBlockWriter* self, uint index, void** ppIMetadataWriter)
            {
                if (ppIMetadataWriter == null) return E_POINTER;
                *ppIMetadataWriter = null;

                var writer = Unwrap(self);
                if (writer == null) return E_FAIL;

                if (index >= writer._metadataBlocks.Count)
                {
                    return (int)WinCodecErrors.WINCODEC_ERR_PROPERTYNOTFOUND;
                }

                SafeMILHandle metadataWriter = writer._metadataBlocks[(int)index];
                return ComPtr<byte>.QueryInterface(
                    (void*)metadataWriter.DangerousGetHandle(),
                    MILGuidData.IID_IWICMetadataWriter,
                    ppIMetadataWriter);
            }

            [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
            private static int ComAddWriter(NativeBlockWriter* self, void* pIMetadataWriter)
            {
                var writer = Unwrap(self);
                if (writer == null) return E_FAIL;
                if (pIMetadataWriter == null) return (int)WinCodecErrors.WINCODEC_ERR_INVALIDPARAMETER;

                if (writer._fixedSize && writer._metadataBlocks.Count > 0)
                {
                    return (int)WinCodecErrors.WINCODEC_ERR_UNSUPPORTEDOPERATION;
                }

                ComHelpers.AddRef(pIMetadataWriter);
                writer._metadataBlocks.Add(new SafeMILHandle((IntPtr)pIMetadataWriter));
                return S_OK;
            }

            [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
            private static int ComSetWriterByIndex(NativeBlockWriter* self, uint index, void* pIMetadataWriter)
            {
                var writer = Unwrap(self);
                if (writer == null) return E_FAIL;

                if (index >= writer._metadataBlocks.Count)
                {
                    return (int)WinCodecErrors.WINCODEC_ERR_PROPERTYNOTFOUND;
                }

                if (pIMetadataWriter == null)
                {
                    return (int)WinCodecErrors.WINCODEC_ERR_INVALIDPARAMETER;
                }

                if (writer._fixedSize)
                {
                    return (int)WinCodecErrors.WINCODEC_ERR_UNSUPPORTEDOPERATION;
                }

                ComHelpers.AddRef(pIMetadataWriter);
                writer._metadataBlocks[(int)index] = new SafeMILHandle((IntPtr)pIMetadataWriter);
                return S_OK;
            }

            [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
            private static int ComRemoveWriterByIndex(NativeBlockWriter* self, uint index)
            {
                var writer = Unwrap(self);
                if (writer == null) return E_FAIL;

                if (index >= writer._metadataBlocks.Count)
                {
                    return (int)WinCodecErrors.WINCODEC_ERR_PROPERTYNOTFOUND;
                }

                if (writer._fixedSize)
                {
                    return (int)WinCodecErrors.WINCODEC_ERR_UNSUPPORTEDOPERATION;
                }

                writer._metadataBlocks.RemoveAt((int)index);
                return S_OK;
            }

            private readonly bool _fixedSize;
            private readonly Guid _containerFormat;
            private List<SafeMILHandle> _metadataBlocks;
        }

        internal sealed class BitmapMetadataBlockWriterEnumerator
        {
            [StructLayout(LayoutKind.Sequential)]
            private struct NativeEnumUnknown
            {
                public void** Vtbl;
                public nint Handle;
                public int RefCount;
            }

            private static readonly void** s_vtbl;

            static BitmapMetadataBlockWriterEnumerator()
            {
                s_vtbl = (void**)NativeMemory.AllocZeroed(7, (nuint)sizeof(void*));
                s_vtbl[0] = (delegate* unmanaged[Stdcall]<NativeEnumUnknown*, Guid*, void**, int>)&ComQueryInterface;
                s_vtbl[1] = (delegate* unmanaged[Stdcall]<NativeEnumUnknown*, uint>)&ComAddRef;
                s_vtbl[2] = (delegate* unmanaged[Stdcall]<NativeEnumUnknown*, uint>)&ComRelease;
                s_vtbl[3] = (delegate* unmanaged[Stdcall]<NativeEnumUnknown*, uint, void**, uint*, int>)&ComNext;
                s_vtbl[4] = (delegate* unmanaged[Stdcall]<NativeEnumUnknown*, uint, int>)&ComSkip;
                s_vtbl[5] = (delegate* unmanaged[Stdcall]<NativeEnumUnknown*, int>)&ComReset;
                s_vtbl[6] = (delegate* unmanaged[Stdcall]<NativeEnumUnknown*, void**, int>)&ComClone;
            }

            internal BitmapMetadataBlockWriterEnumerator(BitmapMetadataBlockWriter blockWriter)
            {
                Debug.Assert(blockWriter != null);
                _metadataBlocks = blockWriter.MetadataBlocks;
                _index = 0;
            }

            internal void* CreateComWrapper()
            {
                NativeEnumUnknown* native = (NativeEnumUnknown*)NativeMemory.Alloc((nuint)sizeof(NativeEnumUnknown));
                native->Vtbl = s_vtbl;
                native->Handle = GCHandle.ToIntPtr(GCHandle.Alloc(this));
                native->RefCount = 1;
                return native;
            }

            private static BitmapMetadataBlockWriterEnumerator Unwrap(NativeEnumUnknown* self)
            {
                if (self == null || self->Handle == 0)
                {
                    return null;
                }

                return GCHandle.FromIntPtr(self->Handle).Target as BitmapMetadataBlockWriterEnumerator;
            }

            [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
            private static int ComQueryInterface(NativeEnumUnknown* self, Guid* riid, void** ppvObject)
            {
                if (riid == null || ppvObject == null) return E_POINTER;

                if (*riid == WicGuidsEx.IID_IUnknown || *riid == WicGuidsEx.IID_IEnumUnknown)
                {
                    *ppvObject = self;
                    Interlocked.Increment(ref self->RefCount);
                    return S_OK;
                }

                *ppvObject = null;
                return E_NOINTERFACE;
            }

            [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
            private static uint ComAddRef(NativeEnumUnknown* self)
            {
                return self == null ? 0 : (uint)Interlocked.Increment(ref self->RefCount);
            }

            [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
            private static uint ComRelease(NativeEnumUnknown* self)
            {
                if (self == null) return 0;

                int remaining = Interlocked.Decrement(ref self->RefCount);
                if (remaining == 0)
                {
                    if (self->Handle != 0)
                    {
                        GCHandle.FromIntPtr(self->Handle).Free();
                        self->Handle = 0;
                    }
                    NativeMemory.Free(self);
                    return 0;
                }

                return (uint)remaining;
            }

            [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
            private static int ComNext(NativeEnumUnknown* self, uint celt, void** rgelt, uint* pceltFetched)
            {
                if (rgelt == null) return E_POINTER;

                var e = Unwrap(self);
                if (e == null) return E_FAIL;

                if (celt > 1)
                {
                    *rgelt = null;
                    if (pceltFetched != null) *pceltFetched = 0;
                    return (int)WinCodecErrors.WINCODEC_ERR_UNSUPPORTEDOPERATION;
                }

                if (e._index >= e._metadataBlocks.Count || celt == 0)
                {
                    *rgelt = null;
                    if (pceltFetched != null) *pceltFetched = 0;
                    return S_FALSE;
                }

                SafeMILHandle metadataHandle = e._metadataBlocks[(int)e._index];
                void* item = null;
                int hr = ComPtr<byte>.QueryInterface(
                    (void*)metadataHandle.DangerousGetHandle(),
                    MILGuidData.IID_IWICMetadataReader,
                    &item);

                *rgelt = item;
                if (HRESULT.Succeeded(hr))
                {
                    if (pceltFetched != null) *pceltFetched = 1;
                    e._index++;
                }
                else if (pceltFetched != null)
                {
                    *pceltFetched = 0;
                }

                return hr;
            }

            [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
            private static int ComSkip(NativeEnumUnknown* self, uint celt)
            {
                var e = Unwrap(self);
                if (e == null) return E_FAIL;

                uint newIndex = e._index + celt;
                if (newIndex > e._metadataBlocks.Count)
                {
                    e._index = (uint)e._metadataBlocks.Count;
                    return S_FALSE;
                }

                e._index += celt;
                return S_OK;
            }

            [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
            private static int ComReset(NativeEnumUnknown* self)
            {
                var e = Unwrap(self);
                if (e == null) return E_FAIL;

                e._index = 0;
                return S_OK;
            }

            [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
            private static int ComClone(NativeEnumUnknown* self, void** ppEnum)
            {
                if (ppEnum != null) *ppEnum = null;
                return (int)WinCodecErrors.WINCODEC_ERR_UNSUPPORTEDOPERATION;
            }

            private readonly List<SafeMILHandle> _metadataBlocks;
            private uint _index;
        }

        #endregion

        #region Constructors

        public BitmapMetadata(string containerFormat)
        {
            ArgumentNullException.ThrowIfNull(containerFormat);

            HRESULT.Check(WicNativeExports.WICMapShortNameToGuid(containerFormat, out Guid guid));
            Init(guid, readOnly: false, fixedSize: false);
        }

        internal BitmapMetadata(Guid containerFormat)
        {
            Init(containerFormat, readOnly: false, fixedSize: false);
        }

        internal BitmapMetadata()
        {
            _metadataHandle = null;
            _readOnly = true;
            _fixedSize = false;
            _blockWriter = null;
        }

        internal BitmapMetadata(
            SafeMILHandle metadataHandle,
            bool readOnly,
            bool fixedSize,
            object syncObject)
        {
            _metadataHandle = metadataHandle;
            _readOnly = readOnly;
            _fixedSize = fixedSize;
            _blockWriter = null;
            _syncObject = syncObject;
        }

        internal BitmapMetadata(BitmapMetadata bitmapMetadata)
        {
            ArgumentNullException.ThrowIfNull(bitmapMetadata);
            Init(bitmapMetadata.GuidFormat, readOnly: false, bitmapMetadata._fixedSize);
        }

        private void Init(Guid containerFormat, bool readOnly, bool fixedSize)
        {
            void* queryWriter = null;
            int hr;

            using (var factoryMaker = new FactoryMaker())
            {
                Guid vendorMicrosoft = MILGuidData.GUID_VendorMicrosoftGuid;
                hr = IWICImagingFactoryExVtbl.CreateQueryWriter(
                    (void*)factoryMaker.ImagingFactoryPtr,
                    containerFormat,
                    &vendorMicrosoft,
                    &queryWriter);
            }

            if (HRESULT.Succeeded(hr))
            {
                _readOnly = readOnly;
                _fixedSize = fixedSize;
                _blockWriter = null;
                _metadataHandle = new SafeMILHandle((IntPtr)queryWriter);
                _syncObject = _metadataHandle;
            }
            else
            {
                InitializeFromBlockWriter(containerFormat, readOnly, fixedSize);
            }
        }

        private void InitializeFromBlockWriter(Guid containerFormat, bool readOnly, bool fixedSize)
        {
            void* blockWriter = null;
            void* queryWriter = null;
            void* componentFactory = null;

            using var factoryMaker = new FactoryMaker();
            try
            {
                _blockWriter = new BitmapMetadataBlockWriter(containerFormat, fixedSize);
                blockWriter = _blockWriter.CreateComWrapper();

                HRESULT.Check(ComPtr<byte>.QueryInterface(
                    (void*)factoryMaker.ImagingFactoryPtr,
                    MILGuidData.IID_IWICComponentFactory,
                    &componentFactory));

                HRESULT.Check(IWICImagingFactoryExVtbl.CreateQueryWriterFromBlockWriter(
                    componentFactory,
                    blockWriter,
                    &queryWriter));

                _readOnly = readOnly;
                _fixedSize = fixedSize;
                _metadataHandle = new SafeMILHandle((IntPtr)queryWriter);
                queryWriter = null;
                _syncObject = _metadataHandle;
            }
            finally
            {
                if (componentFactory != null) ComHelpers.Release(componentFactory);
                if (blockWriter != null) ComHelpers.Release(blockWriter);
                if (queryWriter != null) ComHelpers.Release(queryWriter);
            }
        }

        private void InitializeFromBlockWriter(BitmapMetadataBlockWriter sourceBlockWriter, object syncObject)
        {
            void* blockWriter = null;
            void* queryWriter = null;
            void* componentFactory = null;

            using var factoryMaker = new FactoryMaker();
            try
            {
                _blockWriter = new BitmapMetadataBlockWriter(sourceBlockWriter, syncObject);
                blockWriter = _blockWriter.CreateComWrapper();

                HRESULT.Check(ComPtr<byte>.QueryInterface(
                    (void*)factoryMaker.ImagingFactoryPtr,
                    MILGuidData.IID_IWICComponentFactory,
                    &componentFactory));

                HRESULT.Check(IWICImagingFactoryExVtbl.CreateQueryWriterFromBlockWriter(
                    componentFactory,
                    blockWriter,
                    &queryWriter));

                _readOnly = false;
                _fixedSize = false;
                _metadataHandle = new SafeMILHandle((IntPtr)queryWriter);
                queryWriter = null;
                _syncObject = _metadataHandle;
            }
            finally
            {
                if (componentFactory != null) ComHelpers.Release(componentFactory);
                if (blockWriter != null) ComHelpers.Release(blockWriter);
                if (queryWriter != null) ComHelpers.Release(queryWriter);
            }
        }

        private void InitializeFromMetadataWriter(SafeMILHandle metadataHandle, object syncObject)
        {
            void* queryWriter = null;
            Guid guidVendor = MILGuidData.GUID_VendorMicrosoftGuid;

            try
            {
                int hr;
                using (var factoryMaker = new FactoryMaker())
                {
                    lock (syncObject)
                    {
                        hr = IWICImagingFactoryExVtbl.CreateQueryWriterFromReader(
                            (void*)factoryMaker.ImagingFactoryPtr,
                            (void*)metadataHandle.DangerousGetHandle(),
                            &guidVendor,
                            &queryWriter);
                    }
                }

                if (HRESULT.Succeeded(hr))
                {
                    _readOnly = false;
                    _fixedSize = false;
                    _blockWriter = null;
                    _metadataHandle = new SafeMILHandle((IntPtr)queryWriter);
                    queryWriter = null;
                    _syncObject = _metadataHandle;
                }
                else if (!HRESULT.IsWindowsCodecError(hr))
                {
                    HRESULT.Check(hr);
                }
            }
            finally
            {
                if (queryWriter != null)
                {
                    ComHelpers.Release(queryWriter);
                }
            }
        }

        #endregion

        #region Freezable

        public new BitmapMetadata Clone()
        {
            return (BitmapMetadata)base.Clone();
        }

        protected override Freezable CreateInstanceCore()
        {
            return new BitmapMetadata();
        }

        protected override void CloneCore(Freezable sourceFreezable)
        {
            BitmapMetadata sourceBitmapMetadata = (BitmapMetadata)sourceFreezable;
            base.CloneCore(sourceFreezable);
            CopyCommon(sourceBitmapMetadata);
        }

        protected override void CloneCurrentValueCore(Freezable sourceFreezable)
        {
            BitmapMetadata sourceBitmapMetadata = (BitmapMetadata)sourceFreezable;
            base.CloneCurrentValueCore(sourceFreezable);
            CopyCommon(sourceBitmapMetadata);
        }

        protected override void GetAsFrozenCore(Freezable sourceFreezable)
        {
            BitmapMetadata sourceBitmapMetadata = (BitmapMetadata)sourceFreezable;
            base.GetAsFrozenCore(sourceFreezable);
            CopyCommon(sourceBitmapMetadata);
        }

        protected override void GetCurrentValueAsFrozenCore(Freezable sourceFreezable)
        {
            BitmapMetadata sourceBitmapMetadata = (BitmapMetadata)sourceFreezable;
            base.GetCurrentValueAsFrozenCore(sourceFreezable);
            CopyCommon(sourceBitmapMetadata);
        }

        #endregion

        public string Format
        {
            get
            {
                EnsureBitmapMetadata();
                Guid guid = GuidFormat;
                lock (_syncObject)
                {
                    HRESULT.Check(WicNativeExports.WICMapGuidToShortName(ref guid, out string shortName));
                    return shortName;
                }
            }
        }

        public string Location
        {
            get
            {
                EnsureBitmapMetadata();
                uint length = 0;

                lock (_syncObject)
                {
                    void* pReader = (void*)_metadataHandle.DangerousGetHandle();
                    HRESULT.Check(IWICMetadataQueryReaderVtbl.GetLocation(pReader, 0, null, out length));

                    if (length == 0)
                    {
                        return string.Empty;
                    }

                    char[] buf = new char[length];
                    fixed (char* pBuf = buf)
                    {
                        HRESULT.Check(IWICMetadataQueryReaderVtbl.GetLocation(pReader, length, pBuf, out length));
                        return new string(pBuf).TrimEnd('\0');
                    }
                }
            }
        }

        public bool IsReadOnly
        {
            get
            {
                EnsureBitmapMetadata();
                return _readOnly;
            }
        }

        public bool IsFixedSize
        {
            get
            {
                EnsureBitmapMetadata();
                return _fixedSize;
            }
        }

        #region Query Methods

        public void SetQuery(string query, object value)
        {
            WritePreamble();

            ArgumentNullException.ThrowIfNull(query);
            ArgumentNullException.ThrowIfNull(value);

            if (_readOnly)
            {
                throw new InvalidOperationException(SR.Image_MetadataReadOnly);
            }

            _setQueryString = query;
            _setQueryValue = value;

            EnsureBitmapMetadata();

            PROPVARIANT propVar = default;
            try
            {
                propVar.Init(value);

                if (propVar.RequiresSyncObject)
                {
                    BitmapMetadata metadata = value as BitmapMetadata;
                    Invariant.Assert(metadata != null);
                    metadata.VerifyAccess();

                    lock (metadata._syncObject)
                    {
                        lock (_syncObject)
                        {
                            HRESULT.Check(IWICMetadataQueryWriterVtbl.SetMetadataByName(
                                (void*)_metadataHandle.DangerousGetHandle(),
                                query,
                                &propVar));
                        }
                    }
                }
                else
                {
                    lock (_syncObject)
                    {
                        HRESULT.Check(IWICMetadataQueryWriterVtbl.SetMetadataByName(
                            (void*)_metadataHandle.DangerousGetHandle(),
                            query,
                            &propVar));
                    }
                }
            }
            finally
            {
                propVar.Clear();
            }

            WritePostscript();
        }

        public object GetQuery(string query)
        {
            ArgumentNullException.ThrowIfNull(query);
            EnsureBitmapMetadata();

            PROPVARIANT propVar = default;
            try
            {
                propVar.Init(null);

                int hr;
                lock (_syncObject)
                {
                    hr = IWICMetadataQueryReaderVtbl.GetMetadataByName(
                        (void*)_metadataHandle.DangerousGetHandle(),
                        query,
                        &propVar);
                }

                if (hr != (int)WinCodecErrors.WINCODEC_ERR_PROPERTYNOTFOUND)
                {
                    HRESULT.Check(hr);
                    object objValue = propVar.ToObject(_syncObject);
                    if (IsFrozenInternal && objValue is BitmapMetadata metadata)
                    {
                        metadata.Freeze();
                    }
                    return objValue;
                }
            }
            finally
            {
                propVar.Clear();
            }

            return null;
        }

        public void RemoveQuery(string query)
        {
            WritePreamble();
            ArgumentNullException.ThrowIfNull(query);

            if (_readOnly)
            {
                throw new InvalidOperationException(SR.Image_MetadataReadOnly);
            }

            EnsureBitmapMetadata();

            int hr;
            lock (_syncObject)
            {
                hr = IWICMetadataQueryWriterVtbl.RemoveMetadataByName(
                    (void*)_metadataHandle.DangerousGetHandle(),
                    query);
            }

            if (hr != (int)WinCodecErrors.WINCODEC_ERR_PROPERTYNOTFOUND)
            {
                HRESULT.Check(hr);
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            EnsureBitmapMetadata();
            return new BitmapMetadataEnumerator(_metadataHandle);
        }

        IEnumerator<string> IEnumerable<string>.GetEnumerator()
        {
            EnsureBitmapMetadata();
            return new BitmapMetadataEnumerator(_metadataHandle);
        }

        public bool ContainsQuery(string query)
        {
            ArgumentNullException.ThrowIfNull(query);
            EnsureBitmapMetadata();

            int hr;
            lock (_syncObject)
            {
                hr = IWICMetadataQueryReaderVtbl.GetMetadataByName(
                    (void*)_metadataHandle.DangerousGetHandle(),
                    query,
                    null);
            }

            if (HRESULT.IsWindowsCodecError(hr))
            {
                return false;
            }

            HRESULT.Check(hr);
            return true;
        }

        #endregion

        #region Policy-driven Properties

        public ReadOnlyCollection<string> Author
        {
            get
            {
                string[] strAuthors = GetQuery(policy_Author) as string[];
                return strAuthors == null ? null : new ReadOnlyCollection<string>(strAuthors);
            }
            set
            {
                string[] strAuthors = null;
                if (value != null)
                {
                    strAuthors = new string[value.Count];
                    value.CopyTo(strAuthors, 0);
                }
                SetQuery(policy_Author, strAuthors);
            }
        }

        public string Title
        {
            get => GetQuery(policy_Title) as string;
            set => SetQuery(policy_Title, value);
        }

        public int Rating
        {
            get
            {
                object rating = GetQuery(policy_Rating);
                if (rating is ushort u)
                {
                    return u;
                }
                return 0;
            }
            set => SetQuery(policy_Rating, Convert.ToUInt16(value, CultureInfo.InvariantCulture));
        }

        public string Subject
        {
            get => GetQuery(policy_Subject) as string;
            set => SetQuery(policy_Subject, value);
        }

        public string Comment
        {
            get => GetQuery(policy_Comment) as string;
            set => SetQuery(policy_Comment, value);
        }

        public string DateTaken
        {
            get
            {
                object fileTime = GetQuery(policy_DateTaken);
                if (fileTime is FILETIME time)
                {
                    DateTime dateTime = DateTime.FromFileTime((((long)time.dwHighDateTime) << 32) + (uint)time.dwLowDateTime);
                    return dateTime.ToString();
                }
                return null;
            }
            set
            {
                DateTime dt = Convert.ToDateTime(value, CultureInfo.InvariantCulture);
                PROPVARIANT propVar = new PROPVARIANT
                {
                    varType = (ushort)VarEnum.VT_FILETIME
                };
                long longFileTime = dt.ToFileTime();
                propVar.filetime.dwLowDateTime = (int)longFileTime;
                propVar.filetime.dwHighDateTime = (int)((longFileTime >> 32) & 0xFFFFFFFF);
                object objValue = propVar.ToObject(_syncObject);
                SetQuery(policy_DateTaken, objValue);
            }
        }

        public string ApplicationName
        {
            get => GetQuery(policy_ApplicationName) as string;
            set => SetQuery(policy_ApplicationName, value);
        }

        public string Copyright
        {
            get => GetQuery(policy_Copyright) as string;
            set => SetQuery(policy_Copyright, value);
        }

        public string CameraManufacturer
        {
            get => GetQuery(policy_CameraManufacturer) as string;
            set => SetQuery(policy_CameraManufacturer, value);
        }

        public string CameraModel
        {
            get => GetQuery(policy_CameraModel) as string;
            set => SetQuery(policy_CameraModel, value);
        }

        public ReadOnlyCollection<string> Keywords
        {
            get
            {
                string[] strKeywords = GetQuery(policy_Keywords) as string[];
                return strKeywords == null ? null : new ReadOnlyCollection<string>(strKeywords);
            }
            set
            {
                string[] strKeywords = null;
                if (value != null)
                {
                    strKeywords = new string[value.Count];
                    value.CopyTo(strKeywords, 0);
                }
                SetQuery(policy_Keywords, strKeywords);
            }
        }

        #endregion

        #region Private / Internal

        private void CopyCommon(BitmapMetadata sourceBitmapMetadata)
        {
            BitmapMetadataBlockWriter blockWriter = sourceBitmapMetadata.BlockWriter;

            if (blockWriter == null)
            {
                InitializeFromMetadataWriter(sourceBitmapMetadata._metadataHandle, sourceBitmapMetadata._syncObject);
            }

            if (_metadataHandle == null)
            {
                if (blockWriter != null)
                {
                    InitializeFromBlockWriter(blockWriter, sourceBitmapMetadata._syncObject);
                }
                else
                {
                    InitializeFromBlockWriter(sourceBitmapMetadata.GuidFormat, readOnly: false, fixedSize: false);
                    SetQuery("/", sourceBitmapMetadata);
                }
            }

            _fixedSize = sourceBitmapMetadata._fixedSize;
        }

        internal Guid GuidFormat
        {
            get
            {
                EnsureBitmapMetadata();
                HRESULT.Check(IWICMetadataQueryReaderVtbl.GetContainerFormat((void*)_metadataHandle.DangerousGetHandle(), out Guid guid));
                return guid;
            }
        }

        internal SafeMILHandle InternalMetadataHandle => _metadataHandle;

        internal object SyncObject => _syncObject;

        internal BitmapMetadataBlockWriter BlockWriter => _blockWriter;

        private void EnsureBitmapMetadata()
        {
            ReadPreamble();

            if (_metadataHandle == null)
            {
                throw new InvalidOperationException(SR.Image_MetadataInitializationIncomplete);
            }
        }

        private const string policy_Author = "System.Author";
        private const string policy_Title = "System.Title";
        private const string policy_Subject = "System.Subject";
        private const string policy_Comment = "System.Comment";
        private const string policy_Keywords = "System.Keywords";
        private const string policy_DateTaken = "System.Photo.DateTaken";
        private const string policy_ApplicationName = "System.ApplicationName";
        private const string policy_Copyright = "System.Copyright";
        private const string policy_CameraManufacturer = "System.Photo.CameraManufacturer";
        private const string policy_CameraModel = "System.Photo.CameraModel";
        private const string policy_Rating = "System.SimpleRating";

        private SafeMILHandle _metadataHandle;
        private BitmapMetadataBlockWriter _blockWriter;
        private bool _readOnly;
        private bool _fixedSize;

        private object _setQueryValue;
        private string _setQueryString;

        private object _syncObject = new object();

        #endregion
    }
}

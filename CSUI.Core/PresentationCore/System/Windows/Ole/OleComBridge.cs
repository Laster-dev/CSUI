// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using MS.Win32;

namespace System.Windows.Ole
{
    internal enum DVASPECT : uint
    {
        DVASPECT_CONTENT = 1,
        DVASPECT_THUMBNAIL = 2,
        DVASPECT_ICON = 4,
        DVASPECT_DOCPRINT = 8,
    }

    [Flags]
    internal enum TYMED : uint
    {
        TYMED_NULL = 0,
        TYMED_HGLOBAL = 1,
        TYMED_FILE = 2,
        TYMED_ISTREAM = 4,
        TYMED_ISTORAGE = 8,
        TYMED_GDI = 16,
        TYMED_MFPICT = 32,
        TYMED_ENHMF = 64,
    }

    internal enum DATADIR : uint
    {
        DATADIR_GET = 1,
        DATADIR_SET = 2,
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct FORMATETC
    {
        public ushort cfFormat;
        public nint ptd;
        public uint dwAspect;
        public int lindex;
        public uint tymed;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct STGMEDIUM
    {
        public uint tymed;
        public nint unionValue;
        public void* pUnkForRelease;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct POINTL
    {
        public int x;
        public int y;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct DROPFILES
    {
        public uint pFiles;
        public int ptX;
        public int ptY;
        public int fNC;
        public int fWide;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct BITMAPINFOHEADER
    {
        public uint biSize;
        public int biWidth;
        public int biHeight;
        public ushort biPlanes;
        public ushort biBitCount;
        public uint biCompression;
        public uint biSizeImage;
        public int biXPelsPerMeter;
        public int biYPelsPerMeter;
        public uint biClrUsed;
        public uint biClrImportant;
    }

    internal unsafe interface INativeDropTarget
    {
        void DragEnter(void* pDataObject, uint grfKeyState, POINTL pt, ref uint pdwEffect);
        void DragOver(uint grfKeyState, POINTL pt, ref uint pdwEffect);
        void DragLeave();
        void Drop(void* pDataObject, uint grfKeyState, POINTL pt, ref uint pdwEffect);
    }

    internal interface INativeDropSource
    {
        int QueryContinueDrag(int fEscapePressed, uint grfKeyState);
        int GiveFeedback(uint dwEffect);
    }

    internal unsafe interface INativeOleDataObject
    {
        int GetData(ref FORMATETC format, out STGMEDIUM medium);
        int GetDataHere(ref FORMATETC format, ref STGMEDIUM medium);
        int QueryGetData(ref FORMATETC format);
        int GetCanonicalFormatEtc(ref FORMATETC formatIn, out FORMATETC formatOut);
        int SetData(ref FORMATETC format, ref STGMEDIUM medium, bool fRelease);
        int EnumFormatEtc(uint direction, out void* ppenumFormatEtc);
    }

    /// <summary>
    /// NativeAOT COM vtable bridge for OLE Clipboard, DragDrop, DataObject, and IEnumFORMATETC
    /// using <c>[UnmanagedCallersOnly]</c> vtables without <c>[ComImport]</c> or built-in COM marshaling.
    /// </summary>
    internal static unsafe class OleComBridge
    {
        internal const int S_OK = 0;
        internal const int S_FALSE = 1;
        internal const int E_NOTIMPL = unchecked((int)0x80004001);
        internal const int E_NOINTERFACE = unchecked((int)0x80004002);
        internal const int E_POINTER = unchecked((int)0x80004003);
        internal const int E_FAIL = unchecked((int)0x80004005);
        internal const int E_INVALIDARG = unchecked((int)0x80070057);
        internal const int OLE_E_ADVISENOTSUPPORTED = unchecked((int)0x80040003);
        internal const int DV_E_LINDEX = unchecked((int)0x80040068);
        internal const int DV_E_DVASPECT = unchecked((int)0x8004006B);
        internal const int DV_E_FORMATETC = unchecked((int)0x80040064);
        internal const int DV_E_TYMED = unchecked((int)0x80040069);

        private static readonly Guid IID_IUnknown = new("00000000-0000-0000-C000-000000000046");
        private static readonly Guid IID_IDataObject = new("0000010E-0000-0000-C000-000000000046");
        private static readonly Guid IID_IEnumFORMATETC = new("00000103-0000-0000-C000-000000000046");
        private static readonly Guid IID_IDropSource = new("00000121-0000-0000-C000-000000000046");
        private static readonly Guid IID_IDropTarget = new("00000122-0000-0000-C000-000000000046");

        [StructLayout(LayoutKind.Sequential)]
        private struct NativeComObject
        {
            public void** Vtbl;
            public nint Handle;
            public int RefCount;
        }

        private sealed class DataObjectHolder
        {
            public readonly INativeOleDataObject Adapter;
            public readonly object Key;

            public DataObjectHolder(INativeOleDataObject adapter, object key)
            {
                Adapter = adapter;
                Key = key;
            }
        }

        private sealed class EnumFormatEtcState
        {
            private readonly FORMATETC[] _formats;
            private int _index;

            public EnumFormatEtcState(FORMATETC[] formats, int index = 0)
            {
                _formats = formats ?? Array.Empty<FORMATETC>();
                _index = index;
            }

            public int Next(uint celt, FORMATETC* rgelt, uint* pceltFetched)
            {
                if (celt == 0)
                {
                    if (pceltFetched != null)
                    {
                        *pceltFetched = 0;
                    }
                    return S_OK;
                }

                if (rgelt == null)
                {
                    return E_POINTER;
                }

                if (celt > 1 && pceltFetched == null)
                {
                    return E_INVALIDARG;
                }

                uint fetched = 0;
                while (fetched < celt && _index < _formats.Length)
                {
                    rgelt[fetched] = _formats[_index];
                    rgelt[fetched].ptd = 0;
                    _index++;
                    fetched++;
                }

                if (pceltFetched != null)
                {
                    *pceltFetched = fetched;
                }

                return fetched == celt ? S_OK : S_FALSE;
            }

            public int Skip(uint celt)
            {
                long next = (long)_index + celt;
                if (next >= _formats.Length)
                {
                    _index = _formats.Length;
                    return _formats.Length == next ? S_OK : S_FALSE;
                }

                _index = (int)next;
                return S_OK;
            }

            public int Reset()
            {
                _index = 0;
                return S_OK;
            }

            public EnumFormatEtcState Clone() => new EnumFormatEtcState(_formats, _index);
        }

        private static readonly void** s_dropTargetVtbl;
        private static readonly void** s_dropSourceVtbl;
        private static readonly void** s_dataObjectVtbl;
        private static readonly void** s_enumFormatEtcVtbl;

        private static readonly Lock s_dataObjLock = new();
        private static readonly Dictionary<nint, DataObjectHolder> s_ptrToHolder = new();
        private static readonly Dictionary<object, nint> s_keyToPtr = new(ReferenceEqualityComparer.Instance);

        static OleComBridge()
        {
            s_dropTargetVtbl = (void**)NativeMemory.AllocZeroed(7, (nuint)sizeof(void*));
            s_dropTargetVtbl[0] = (delegate* unmanaged[Stdcall]<NativeComObject*, Guid*, void**, int>)&ComDropTargetQueryInterface;
            s_dropTargetVtbl[1] = (delegate* unmanaged[Stdcall]<NativeComObject*, uint>)&ComAddRef;
            s_dropTargetVtbl[2] = (delegate* unmanaged[Stdcall]<NativeComObject*, uint>)&ComRelease;
            s_dropTargetVtbl[3] = (delegate* unmanaged[Stdcall]<NativeComObject*, void*, uint, POINTL, uint*, int>)&ComDragEnter;
            s_dropTargetVtbl[4] = (delegate* unmanaged[Stdcall]<NativeComObject*, uint, POINTL, uint*, int>)&ComDragOver;
            s_dropTargetVtbl[5] = (delegate* unmanaged[Stdcall]<NativeComObject*, int>)&ComDragLeave;
            s_dropTargetVtbl[6] = (delegate* unmanaged[Stdcall]<NativeComObject*, void*, uint, POINTL, uint*, int>)&ComDrop;

            s_dropSourceVtbl = (void**)NativeMemory.AllocZeroed(5, (nuint)sizeof(void*));
            s_dropSourceVtbl[0] = (delegate* unmanaged[Stdcall]<NativeComObject*, Guid*, void**, int>)&ComDropSourceQueryInterface;
            s_dropSourceVtbl[1] = (delegate* unmanaged[Stdcall]<NativeComObject*, uint>)&ComAddRef;
            s_dropSourceVtbl[2] = (delegate* unmanaged[Stdcall]<NativeComObject*, uint>)&ComRelease;
            s_dropSourceVtbl[3] = (delegate* unmanaged[Stdcall]<NativeComObject*, int, uint, int>)&ComQueryContinueDrag;
            s_dropSourceVtbl[4] = (delegate* unmanaged[Stdcall]<NativeComObject*, uint, int>)&ComGiveFeedback;

            s_dataObjectVtbl = (void**)NativeMemory.AllocZeroed(12, (nuint)sizeof(void*));
            s_dataObjectVtbl[0] = (delegate* unmanaged[Stdcall]<NativeComObject*, Guid*, void**, int>)&ComDataObjectQueryInterface;
            s_dataObjectVtbl[1] = (delegate* unmanaged[Stdcall]<NativeComObject*, uint>)&ComDataObjectAddRef;
            s_dataObjectVtbl[2] = (delegate* unmanaged[Stdcall]<NativeComObject*, uint>)&ComDataObjectRelease;
            s_dataObjectVtbl[3] = (delegate* unmanaged[Stdcall]<NativeComObject*, FORMATETC*, STGMEDIUM*, int>)&ComDataObjectGetData;
            s_dataObjectVtbl[4] = (delegate* unmanaged[Stdcall]<NativeComObject*, FORMATETC*, STGMEDIUM*, int>)&ComDataObjectGetDataHere;
            s_dataObjectVtbl[5] = (delegate* unmanaged[Stdcall]<NativeComObject*, FORMATETC*, int>)&ComDataObjectQueryGetData;
            s_dataObjectVtbl[6] = (delegate* unmanaged[Stdcall]<NativeComObject*, FORMATETC*, FORMATETC*, int>)&ComDataObjectGetCanonicalFormatEtc;
            s_dataObjectVtbl[7] = (delegate* unmanaged[Stdcall]<NativeComObject*, FORMATETC*, STGMEDIUM*, int, int>)&ComDataObjectSetData;
            s_dataObjectVtbl[8] = (delegate* unmanaged[Stdcall]<NativeComObject*, uint, void**, int>)&ComDataObjectEnumFormatEtc;
            s_dataObjectVtbl[9] = (delegate* unmanaged[Stdcall]<NativeComObject*, FORMATETC*, uint, void*, uint*, int>)&ComDataObjectDAdvise;
            s_dataObjectVtbl[10] = (delegate* unmanaged[Stdcall]<NativeComObject*, uint, int>)&ComDataObjectDUnadvise;
            s_dataObjectVtbl[11] = (delegate* unmanaged[Stdcall]<NativeComObject*, void**, int>)&ComDataObjectEnumDAdvise;

            s_enumFormatEtcVtbl = (void**)NativeMemory.AllocZeroed(7, (nuint)sizeof(void*));
            s_enumFormatEtcVtbl[0] = (delegate* unmanaged[Stdcall]<NativeComObject*, Guid*, void**, int>)&ComEnumFormatEtcQueryInterface;
            s_enumFormatEtcVtbl[1] = (delegate* unmanaged[Stdcall]<NativeComObject*, uint>)&ComAddRef;
            s_enumFormatEtcVtbl[2] = (delegate* unmanaged[Stdcall]<NativeComObject*, uint>)&ComRelease;
            s_enumFormatEtcVtbl[3] = (delegate* unmanaged[Stdcall]<NativeComObject*, uint, FORMATETC*, uint*, int>)&ComEnumFormatEtcNext;
            s_enumFormatEtcVtbl[4] = (delegate* unmanaged[Stdcall]<NativeComObject*, uint, int>)&ComEnumFormatEtcSkip;
            s_enumFormatEtcVtbl[5] = (delegate* unmanaged[Stdcall]<NativeComObject*, int>)&ComEnumFormatEtcReset;
            s_enumFormatEtcVtbl[6] = (delegate* unmanaged[Stdcall]<NativeComObject*, void**, int>)&ComEnumFormatEtcClone;
        }

        private static NativeComObject* AllocComObject(void** vtbl, object target)
        {
            NativeComObject* obj = (NativeComObject*)NativeMemory.Alloc((nuint)sizeof(NativeComObject));
            obj->Vtbl = vtbl;
            obj->Handle = GCHandle.ToIntPtr(GCHandle.Alloc(target));
            obj->RefCount = 1;
            return obj;
        }

        private static object UnwrapTarget(NativeComObject* self)
        {
            if (self == null || self->Handle == 0)
            {
                return null;
            }

            return GCHandle.FromIntPtr(self->Handle).Target;
        }

        internal static void* CreateDropTargetCom(INativeDropTarget target)
        {
            ArgumentNullException.ThrowIfNull(target);
            return AllocComObject(s_dropTargetVtbl, target);
        }

        internal static void* CreateDropSourceCom(INativeDropSource source)
        {
            ArgumentNullException.ThrowIfNull(source);
            return AllocComObject(s_dropSourceVtbl, source);
        }

        internal static void* CreateEnumFormatEtcCom(FORMATETC[] formats)
        {
            return AllocComObject(s_enumFormatEtcVtbl, new EnumFormatEtcState(formats));
        }

        internal static void* CreateDataObjectCom(INativeOleDataObject adapter, object identityKey)
        {
            ArgumentNullException.ThrowIfNull(adapter);
            identityKey ??= adapter;

            lock (s_dataObjLock)
            {
                if (s_keyToPtr.TryGetValue(identityKey, out nint existing))
                {
                    NativeComObject* existingObj = (NativeComObject*)existing;
                    if (existingObj->RefCount > 0)
                    {
                        existingObj->RefCount++;
                        return existingObj;
                    }
                }

                DataObjectHolder holder = new(adapter, identityKey);
                NativeComObject* created = AllocComObject(s_dataObjectVtbl, holder);
                nint ptr = (nint)created;
                s_ptrToHolder[ptr] = holder;
                s_keyToPtr[identityKey] = ptr;
                return created;
            }
        }

        internal static bool TryGetLiveDataObjectCom(object identityKey, out void* pDataObject)
        {
            pDataObject = null;
            if (identityKey == null)
            {
                return false;
            }

            lock (s_dataObjLock)
            {
                if (s_keyToPtr.TryGetValue(identityKey, out nint existing))
                {
                    NativeComObject* existingObj = (NativeComObject*)existing;
                    if (existingObj->RefCount > 0)
                    {
                        existingObj->RefCount++;
                        pDataObject = existingObj;
                        return true;
                    }
                }
            }

            return false;
        }

        internal static bool TryUnwrapManagedDataObject(void* pDataObject, out object key)
        {
            key = null;
            if (pDataObject == null)
            {
                return false;
            }

            lock (s_dataObjLock)
            {
                if (s_ptrToHolder.TryGetValue((nint)pDataObject, out DataObjectHolder holder))
                {
                    key = holder.Key;
                    return true;
                }
            }

            return false;
        }

        internal static void AddRefCom(void* pUnk)
        {
            if (pUnk == null)
            {
                return;
            }

            void** vtbl = *(void***)pUnk;
            var addRef = (delegate* unmanaged[Stdcall]<void*, uint>)vtbl[1];
            addRef(pUnk);
        }

        internal static void ReleaseCom(void* pUnk)
        {
            if (pUnk == null)
            {
                return;
            }

            void** vtbl = *(void***)pUnk;
            var release = (delegate* unmanaged[Stdcall]<void*, uint>)vtbl[2];
            release(pUnk);
        }

        internal static int QueryGetData(void* pDataObj, FORMATETC* pFormat)
        {
            if (pDataObj == null || pFormat == null)
            {
                return E_POINTER;
            }

            void** vtbl = *(void***)pDataObj;
            var fn = (delegate* unmanaged[Stdcall]<void*, FORMATETC*, int>)vtbl[5];
            return fn(pDataObj, pFormat);
        }

        internal static int GetData(void* pDataObj, FORMATETC* pFormat, STGMEDIUM* pMedium)
        {
            if (pDataObj == null || pFormat == null || pMedium == null)
            {
                return E_POINTER;
            }

            *pMedium = default;
            void** vtbl = *(void***)pDataObj;
            var fn = (delegate* unmanaged[Stdcall]<void*, FORMATETC*, STGMEDIUM*, int>)vtbl[3];
            return fn(pDataObj, pFormat, pMedium);
        }

        internal static int GetDataHere(void* pDataObj, FORMATETC* pFormat, STGMEDIUM* pMedium)
        {
            if (pDataObj == null || pFormat == null || pMedium == null)
            {
                return E_POINTER;
            }

            void** vtbl = *(void***)pDataObj;
            var fn = (delegate* unmanaged[Stdcall]<void*, FORMATETC*, STGMEDIUM*, int>)vtbl[4];
            return fn(pDataObj, pFormat, pMedium);
        }

        internal static int SetData(void* pDataObj, FORMATETC* pFormat, STGMEDIUM* pMedium, bool fRelease)
        {
            if (pDataObj == null || pFormat == null || pMedium == null)
            {
                return E_POINTER;
            }

            void** vtbl = *(void***)pDataObj;
            var fn = (delegate* unmanaged[Stdcall]<void*, FORMATETC*, STGMEDIUM*, int, int>)vtbl[7];
            return fn(pDataObj, pFormat, pMedium, fRelease ? 1 : 0);
        }

        internal static int EnumFormatEtc(void* pDataObj, uint dwDirection, void** ppenum)
        {
            if (pDataObj == null || ppenum == null)
            {
                return E_POINTER;
            }

            *ppenum = null;
            void** vtbl = *(void***)pDataObj;
            var fn = (delegate* unmanaged[Stdcall]<void*, uint, void**, int>)vtbl[8];
            return fn(pDataObj, dwDirection, ppenum);
        }

        internal static int NextFormatEtc(void* pEnum, uint celt, FORMATETC* rgelt, uint* pceltFetched)
        {
            if (pEnum == null || rgelt == null)
            {
                return E_POINTER;
            }

            void** vtbl = *(void***)pEnum;
            var fn = (delegate* unmanaged[Stdcall]<void*, uint, FORMATETC*, uint*, int>)vtbl[3];
            return fn(pEnum, celt, rgelt, pceltFetched);
        }

        internal static int ResetFormatEtc(void* pEnum)
        {
            if (pEnum == null)
            {
                return E_POINTER;
            }

            void** vtbl = *(void***)pEnum;
            var fn = (delegate* unmanaged[Stdcall]<void*, int>)vtbl[5];
            return fn(pEnum);
        }

        #region HGLOBAL / IStream / Data Conversion Helpers

        internal static IntPtr AllocHGlobalFromBytes(ReadOnlySpan<byte> bytes)
        {
            nuint size = (nuint)Math.Max(bytes.Length, 1);
            IntPtr hMem = UnsafeNativeMethods.GlobalAlloc(NativeMethods.GMEM_MOVEABLE | NativeMethods.GMEM_ZEROINIT, size);
            if (hMem == IntPtr.Zero)
            {
                throw new OutOfMemoryException();
            }

            if (bytes.Length > 0)
            {
                void* pMem = UnsafeNativeMethods.GlobalLock(hMem);
                if (pMem == null)
                {
                    UnsafeNativeMethods.GlobalFree(hMem);
                    throw new OutOfMemoryException();
                }

                try
                {
                    bytes.CopyTo(new Span<byte>(pMem, bytes.Length));
                }
                finally
                {
                    UnsafeNativeMethods.GlobalUnlock(hMem);
                }
            }

            return hMem;
        }

        internal static byte[] ReadBytesFromHGlobal(IntPtr hMem)
        {
            if (hMem == IntPtr.Zero)
            {
                return Array.Empty<byte>();
            }

            nuint size = UnsafeNativeMethods.GlobalSize(hMem);
            if (size == 0)
            {
                return Array.Empty<byte>();
            }

            void* pMem = UnsafeNativeMethods.GlobalLock(hMem);
            if (pMem == null)
            {
                return Array.Empty<byte>();
            }

            try
            {
                int len = (int)Math.Min(size, (nuint)int.MaxValue);
                byte[] result = new byte[len];
                new ReadOnlySpan<byte>(pMem, len).CopyTo(result);
                return result;
            }
            finally
            {
                UnsafeNativeMethods.GlobalUnlock(hMem);
            }
        }

        internal static IntPtr AllocHGlobalFromUnicodeString(string value)
        {
            value ??= string.Empty;
            int byteLen = (value.Length + 1) * sizeof(char);
            IntPtr hMem = UnsafeNativeMethods.GlobalAlloc(NativeMethods.GMEM_MOVEABLE | NativeMethods.GMEM_ZEROINIT, (nuint)byteLen);
            if (hMem == IntPtr.Zero)
            {
                throw new OutOfMemoryException();
            }

            void* pMem = UnsafeNativeMethods.GlobalLock(hMem);
            if (pMem == null)
            {
                UnsafeNativeMethods.GlobalFree(hMem);
                throw new OutOfMemoryException();
            }

            try
            {
                char* dst = (char*)pMem;
                value.AsSpan().CopyTo(new Span<char>(dst, value.Length));
                dst[value.Length] = '\0';
            }
            finally
            {
                UnsafeNativeMethods.GlobalUnlock(hMem);
            }

            return hMem;
        }

        internal static string ReadUnicodeStringFromHGlobal(IntPtr hMem)
        {
            if (hMem == IntPtr.Zero)
            {
                return string.Empty;
            }

            nuint size = UnsafeNativeMethods.GlobalSize(hMem);
            if (size < sizeof(char))
            {
                return string.Empty;
            }

            void* pMem = UnsafeNativeMethods.GlobalLock(hMem);
            if (pMem == null)
            {
                return string.Empty;
            }

            try
            {
                int maxChars = (int)Math.Min(size / sizeof(char), (nuint)int.MaxValue);
                ReadOnlySpan<char> span = new((char*)pMem, maxChars);
                int nul = span.IndexOf('\0');
                return nul >= 0 ? new string(span.Slice(0, nul)) : new string(span);
            }
            finally
            {
                UnsafeNativeMethods.GlobalUnlock(hMem);
            }
        }

        internal static IntPtr AllocHGlobalFromBytesWithNull(ReadOnlySpan<byte> bytes)
        {
            IntPtr hMem = UnsafeNativeMethods.GlobalAlloc(NativeMethods.GMEM_MOVEABLE | NativeMethods.GMEM_ZEROINIT, (nuint)(bytes.Length + 1));
            if (hMem == IntPtr.Zero)
            {
                throw new OutOfMemoryException();
            }

            void* pMem = UnsafeNativeMethods.GlobalLock(hMem);
            if (pMem == null)
            {
                UnsafeNativeMethods.GlobalFree(hMem);
                throw new OutOfMemoryException();
            }

            try
            {
                bytes.CopyTo(new Span<byte>(pMem, bytes.Length));
                ((byte*)pMem)[bytes.Length] = 0;
            }
            finally
            {
                UnsafeNativeMethods.GlobalUnlock(hMem);
            }

            return hMem;
        }

        internal static string ReadAnsiOrUtf8StringFromHGlobal(IntPtr hMem, Encoding encoding)
        {
            if (hMem == IntPtr.Zero)
            {
                return string.Empty;
            }

            nuint size = UnsafeNativeMethods.GlobalSize(hMem);
            if (size == 0)
            {
                return string.Empty;
            }

            void* pMem = UnsafeNativeMethods.GlobalLock(hMem);
            if (pMem == null)
            {
                return string.Empty;
            }

            try
            {
                int maxBytes = (int)Math.Min(size, (nuint)int.MaxValue);
                ReadOnlySpan<byte> span = new((byte*)pMem, maxBytes);
                int nul = span.IndexOf((byte)0);
                if (nul >= 0)
                {
                    span = span.Slice(0, nul);
                }
                return encoding.GetString(span);
            }
            finally
            {
                UnsafeNativeMethods.GlobalUnlock(hMem);
            }
        }

        internal static IntPtr AllocHGlobalFromFileDropList(StringCollection files)
        {
            int headerSize = sizeof(DROPFILES);
            int charCount = 1; // final double-null terminator
            for (int i = 0; i < files.Count; i++)
            {
                string s = files[i] ?? string.Empty;
                charCount += s.Length + 1;
            }

            nuint totalBytes = (nuint)(headerSize + charCount * sizeof(char));
            IntPtr hMem = UnsafeNativeMethods.GlobalAlloc(NativeMethods.GMEM_MOVEABLE | NativeMethods.GMEM_ZEROINIT, totalBytes);
            if (hMem == IntPtr.Zero)
            {
                throw new OutOfMemoryException();
            }

            void* pMem = UnsafeNativeMethods.GlobalLock(hMem);
            if (pMem == null)
            {
                UnsafeNativeMethods.GlobalFree(hMem);
                throw new OutOfMemoryException();
            }

            try
            {
                DROPFILES* df = (DROPFILES*)pMem;
                df->pFiles = (uint)headerSize;
                df->ptX = 0;
                df->ptY = 0;
                df->fNC = 0;
                df->fWide = 1;

                char* cursor = (char*)((byte*)pMem + headerSize);
                for (int i = 0; i < files.Count; i++)
                {
                    string s = files[i] ?? string.Empty;
                    s.AsSpan().CopyTo(new Span<char>(cursor, s.Length));
                    cursor += s.Length;
                    *cursor++ = '\0';
                }
                *cursor = '\0';
            }
            finally
            {
                UnsafeNativeMethods.GlobalUnlock(hMem);
            }

            return hMem;
        }

        internal static string[] ReadFileDropListFromHDrop(IntPtr hDrop)
        {
            if (hDrop == IntPtr.Zero)
            {
                return null;
            }

            uint count = UnsafeNativeMethods.DragQueryFile(hDrop, 0xFFFFFFFFu, null, 0);
            if (count == 0)
            {
                return Array.Empty<string>();
            }

            string[] files = new string[count];
            for (uint i = 0; i < count; i++)
            {
                uint cch = UnsafeNativeMethods.DragQueryFile(hDrop, i, null, 0);
                char[] buf = new char[cch + 1];
                fixed (char* pBuf = buf)
                {
                    uint copied = UnsafeNativeMethods.DragQueryFile(hDrop, i, pBuf, (uint)buf.Length);
                    files[i] = new string(pBuf, 0, (int)copied);
                }
            }

            return files;
        }

        internal static byte[] ReadBytesFromIStream(void* pStream)
        {
            if (pStream == null)
            {
                return Array.Empty<byte>();
            }

            void** vtbl = *(void***)pStream;
            var read = (delegate* unmanaged[Stdcall]<void*, void*, uint, uint*, int>)vtbl[3];
            var seek = (delegate* unmanaged[Stdcall]<void*, long, uint, long*, int>)vtbl[5];

            long ignored = 0;
            seek(pStream, 0, 0 /* STREAM_SEEK_SET */, &ignored);

            using MemoryStream ms = new();
            byte[] buffer = new byte[4096];
            fixed (byte* pBuffer = buffer)
            {
                while (true)
                {
                    uint bytesRead = 0;
                    int hr = read(pStream, pBuffer, (uint)buffer.Length, &bytesRead);
                    if (hr < 0 || bytesRead == 0)
                    {
                        break;
                    }
                    ms.Write(buffer, 0, (int)bytesRead);
                }
            }

            return ms.ToArray();
        }

        #endregion

        #region UnmanagedCallersOnly Slots

        private static int QueryInterfaceCommon(NativeComObject* self, Guid* riid, void** ppvObject, Guid expectedIid)
        {
            if (riid == null || ppvObject == null)
            {
                return E_POINTER;
            }

            if (*riid == IID_IUnknown || *riid == expectedIid)
            {
                *ppvObject = self;
                Interlocked.Increment(ref self->RefCount);
                return S_OK;
            }

            *ppvObject = null;
            return E_NOINTERFACE;
        }

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
        private static uint ComAddRef(NativeComObject* self)
        {
            if (self == null)
            {
                return 0;
            }

            return (uint)Interlocked.Increment(ref self->RefCount);
        }

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
        private static uint ComRelease(NativeComObject* self)
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

        // IDropTarget
        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
        private static int ComDropTargetQueryInterface(NativeComObject* self, Guid* riid, void** ppvObject)
            => QueryInterfaceCommon(self, riid, ppvObject, IID_IDropTarget);

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
        private static int ComDragEnter(NativeComObject* self, void* pDataObj, uint grfKeyState, POINTL pt, uint* pdwEffect)
        {
            try
            {
                if (UnwrapTarget(self) is not INativeDropTarget target)
                {
                    return E_FAIL;
                }

                uint effect = pdwEffect != null ? *pdwEffect : 0;
                target.DragEnter(pDataObj, grfKeyState, pt, ref effect);
                if (pdwEffect != null)
                {
                    *pdwEffect = effect;
                }
                return S_OK;
            }
            catch (Exception ex)
            {
                return ex.HResult != 0 ? ex.HResult : E_FAIL;
            }
        }

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
        private static int ComDragOver(NativeComObject* self, uint grfKeyState, POINTL pt, uint* pdwEffect)
        {
            try
            {
                if (UnwrapTarget(self) is not INativeDropTarget target)
                {
                    return E_FAIL;
                }

                uint effect = pdwEffect != null ? *pdwEffect : 0;
                target.DragOver(grfKeyState, pt, ref effect);
                if (pdwEffect != null)
                {
                    *pdwEffect = effect;
                }
                return S_OK;
            }
            catch (Exception ex)
            {
                return ex.HResult != 0 ? ex.HResult : E_FAIL;
            }
        }

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
        private static int ComDragLeave(NativeComObject* self)
        {
            try
            {
                if (UnwrapTarget(self) is not INativeDropTarget target)
                {
                    return E_FAIL;
                }

                target.DragLeave();
                return S_OK;
            }
            catch (Exception ex)
            {
                return ex.HResult != 0 ? ex.HResult : E_FAIL;
            }
        }

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
        private static int ComDrop(NativeComObject* self, void* pDataObj, uint grfKeyState, POINTL pt, uint* pdwEffect)
        {
            try
            {
                if (UnwrapTarget(self) is not INativeDropTarget target)
                {
                    return E_FAIL;
                }

                uint effect = pdwEffect != null ? *pdwEffect : 0;
                target.Drop(pDataObj, grfKeyState, pt, ref effect);
                if (pdwEffect != null)
                {
                    *pdwEffect = effect;
                }
                return S_OK;
            }
            catch (Exception ex)
            {
                return ex.HResult != 0 ? ex.HResult : E_FAIL;
            }
        }

        // IDropSource
        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
        private static int ComDropSourceQueryInterface(NativeComObject* self, Guid* riid, void** ppvObject)
            => QueryInterfaceCommon(self, riid, ppvObject, IID_IDropSource);

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
        private static int ComQueryContinueDrag(NativeComObject* self, int fEscapePressed, uint grfKeyState)
        {
            try
            {
                return UnwrapTarget(self) is INativeDropSource source
                    ? source.QueryContinueDrag(fEscapePressed, grfKeyState)
                    : E_FAIL;
            }
            catch (Exception ex)
            {
                return ex.HResult != 0 ? ex.HResult : E_FAIL;
            }
        }

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
        private static int ComGiveFeedback(NativeComObject* self, uint dwEffect)
        {
            try
            {
                return UnwrapTarget(self) is INativeDropSource source
                    ? source.GiveFeedback(dwEffect)
                    : E_FAIL;
            }
            catch (Exception ex)
            {
                return ex.HResult != 0 ? ex.HResult : E_FAIL;
            }
        }

        // IDataObject
        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
        private static int ComDataObjectQueryInterface(NativeComObject* self, Guid* riid, void** ppvObject)
        {
            if (riid == null || ppvObject == null)
            {
                return E_POINTER;
            }

            if (*riid == IID_IUnknown || *riid == IID_IDataObject)
            {
                lock (s_dataObjLock)
                {
                    self->RefCount++;
                }
                *ppvObject = self;
                return S_OK;
            }

            *ppvObject = null;
            return E_NOINTERFACE;
        }

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
        private static uint ComDataObjectAddRef(NativeComObject* self)
        {
            if (self == null)
            {
                return 0;
            }

            lock (s_dataObjLock)
            {
                return (uint)(++self->RefCount);
            }
        }

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
        private static uint ComDataObjectRelease(NativeComObject* self)
        {
            if (self == null)
            {
                return 0;
            }

            int remaining;
            lock (s_dataObjLock)
            {
                remaining = --self->RefCount;
                if (remaining == 0)
                {
                    nint ptr = (nint)self;
                    if (s_ptrToHolder.Remove(ptr, out DataObjectHolder holder))
                    {
                        if (s_keyToPtr.TryGetValue(holder.Key, out nint mappedPtr) && mappedPtr == ptr)
                        {
                            s_keyToPtr.Remove(holder.Key);
                        }
                    }
                }
            }

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
        private static int ComDataObjectGetData(NativeComObject* self, FORMATETC* pformatetcIn, STGMEDIUM* pmedium)
        {
            if (pformatetcIn == null || pmedium == null)
            {
                return E_POINTER;
            }

            *pmedium = default;
            try
            {
                if (UnwrapTarget(self) is not DataObjectHolder holder)
                {
                    return E_FAIL;
                }

                FORMATETC fmt = *pformatetcIn;
                int hr = holder.Adapter.GetData(ref fmt, out STGMEDIUM med);
                *pmedium = med;
                return hr;
            }
            catch (Exception ex)
            {
                return ex.HResult != 0 ? ex.HResult : E_FAIL;
            }
        }

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
        private static int ComDataObjectGetDataHere(NativeComObject* self, FORMATETC* pformatetc, STGMEDIUM* pmedium)
        {
            if (pformatetc == null || pmedium == null)
            {
                return E_POINTER;
            }

            try
            {
                if (UnwrapTarget(self) is not DataObjectHolder holder)
                {
                    return E_FAIL;
                }

                FORMATETC fmt = *pformatetc;
                STGMEDIUM med = *pmedium;
                int hr = holder.Adapter.GetDataHere(ref fmt, ref med);
                *pmedium = med;
                return hr;
            }
            catch (Exception ex)
            {
                return ex.HResult != 0 ? ex.HResult : E_FAIL;
            }
        }

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
        private static int ComDataObjectQueryGetData(NativeComObject* self, FORMATETC* pformatetc)
        {
            if (pformatetc == null)
            {
                return E_POINTER;
            }

            try
            {
                if (UnwrapTarget(self) is not DataObjectHolder holder)
                {
                    return E_FAIL;
                }

                FORMATETC fmt = *pformatetc;
                return holder.Adapter.QueryGetData(ref fmt);
            }
            catch (Exception ex)
            {
                return ex.HResult != 0 ? ex.HResult : E_FAIL;
            }
        }

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
        private static int ComDataObjectGetCanonicalFormatEtc(NativeComObject* self, FORMATETC* pformatectIn, FORMATETC* pformatetcOut)
        {
            if (pformatectIn == null || pformatetcOut == null)
            {
                return E_POINTER;
            }

            *pformatetcOut = default;
            try
            {
                if (UnwrapTarget(self) is not DataObjectHolder holder)
                {
                    return E_FAIL;
                }

                FORMATETC fmtIn = *pformatectIn;
                int hr = holder.Adapter.GetCanonicalFormatEtc(ref fmtIn, out FORMATETC fmtOut);
                *pformatetcOut = fmtOut;
                return hr;
            }
            catch (Exception ex)
            {
                return ex.HResult != 0 ? ex.HResult : E_FAIL;
            }
        }

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
        private static int ComDataObjectSetData(NativeComObject* self, FORMATETC* pformatetc, STGMEDIUM* pmedium, int fRelease)
        {
            if (pformatetc == null || pmedium == null)
            {
                return E_POINTER;
            }

            try
            {
                if (UnwrapTarget(self) is not DataObjectHolder holder)
                {
                    return E_FAIL;
                }

                FORMATETC fmt = *pformatetc;
                STGMEDIUM med = *pmedium;
                return holder.Adapter.SetData(ref fmt, ref med, fRelease != 0);
            }
            catch (Exception ex)
            {
                return ex.HResult != 0 ? ex.HResult : E_FAIL;
            }
        }

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
        private static int ComDataObjectEnumFormatEtc(NativeComObject* self, uint dwDirection, void** ppenumFormatEtc)
        {
            if (ppenumFormatEtc == null)
            {
                return E_POINTER;
            }

            *ppenumFormatEtc = null;
            try
            {
                if (UnwrapTarget(self) is not DataObjectHolder holder)
                {
                    return E_FAIL;
                }

                return holder.Adapter.EnumFormatEtc(dwDirection, out *ppenumFormatEtc);
            }
            catch (Exception ex)
            {
                return ex.HResult != 0 ? ex.HResult : E_FAIL;
            }
        }

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
        private static int ComDataObjectDAdvise(NativeComObject* self, FORMATETC* pformatetc, uint advf, void* pAdvSink, uint* pdwConnection)
            => OLE_E_ADVISENOTSUPPORTED;

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
        private static int ComDataObjectDUnadvise(NativeComObject* self, uint dwConnection)
            => OLE_E_ADVISENOTSUPPORTED;

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
        private static int ComDataObjectEnumDAdvise(NativeComObject* self, void** ppenumAdvise)
        {
            if (ppenumAdvise != null)
            {
                *ppenumAdvise = null;
            }
            return OLE_E_ADVISENOTSUPPORTED;
        }

        // IEnumFORMATETC
        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
        private static int ComEnumFormatEtcQueryInterface(NativeComObject* self, Guid* riid, void** ppvObject)
            => QueryInterfaceCommon(self, riid, ppvObject, IID_IEnumFORMATETC);

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
        private static int ComEnumFormatEtcNext(NativeComObject* self, uint celt, FORMATETC* rgelt, uint* pceltFetched)
        {
            try
            {
                return UnwrapTarget(self) is EnumFormatEtcState state
                    ? state.Next(celt, rgelt, pceltFetched)
                    : E_FAIL;
            }
            catch (Exception ex)
            {
                return ex.HResult != 0 ? ex.HResult : E_FAIL;
            }
        }

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
        private static int ComEnumFormatEtcSkip(NativeComObject* self, uint celt)
        {
            try
            {
                return UnwrapTarget(self) is EnumFormatEtcState state
                    ? state.Skip(celt)
                    : E_FAIL;
            }
            catch (Exception ex)
            {
                return ex.HResult != 0 ? ex.HResult : E_FAIL;
            }
        }

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
        private static int ComEnumFormatEtcReset(NativeComObject* self)
        {
            try
            {
                return UnwrapTarget(self) is EnumFormatEtcState state
                    ? state.Reset()
                    : E_FAIL;
            }
            catch (Exception ex)
            {
                return ex.HResult != 0 ? ex.HResult : E_FAIL;
            }
        }

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
        private static int ComEnumFormatEtcClone(NativeComObject* self, void** ppenum)
        {
            if (ppenum == null)
            {
                return E_POINTER;
            }

            *ppenum = null;
            try
            {
                if (UnwrapTarget(self) is not EnumFormatEtcState state)
                {
                    return E_FAIL;
                }

                *ppenum = AllocComObject(s_enumFormatEtcVtbl, state.Clone());
                return S_OK;
            }
            catch (Exception ex)
            {
                return ex.HResult != 0 ? ex.HResult : E_FAIL;
            }
        }

        #endregion
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using CSUI.Core.Interop;
using CSUI.Core.Interop.DirectX;
using MS.Internal;
using SR = MS.Internal.PresentationCore.SR;

namespace System.Windows.Media.Imaging
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal struct PropVariantCA
    {
        internal uint cElems;
        internal IntPtr pElems;
    }

    /// <summary>
    /// NativeAOT-compatible PROPVARIANT wrapper (zero Marshal.SizeOf(Type) / runtime reflection).
    /// </summary>
    [StructLayout(LayoutKind.Explicit, Pack = 1)]
    internal unsafe struct PROPVARIANT
    {
        [FieldOffset(0)] internal ushort varType;
        [FieldOffset(2)] internal ushort wReserved1;
        [FieldOffset(4)] internal ushort wReserved2;
        [FieldOffset(6)] internal ushort wReserved3;

        [FieldOffset(8)] internal sbyte cVal;
        [FieldOffset(8)] internal byte bVal;
        [FieldOffset(8)] internal short iVal;
        [FieldOffset(8)] internal ushort uiVal;
        [FieldOffset(8)] internal int intVal;
        [FieldOffset(8)] internal uint uintVal;
        [FieldOffset(8)] internal long lVal;
        [FieldOffset(8)] internal ulong ulVal;
        [FieldOffset(8)] internal float fltVal;
        [FieldOffset(8)] internal double dblVal;
        [FieldOffset(8)] internal short boolVal;
        [FieldOffset(8)] internal FILETIME filetime;
        [FieldOffset(8)] internal IntPtr pclsidVal;
        [FieldOffset(8)] internal IntPtr pszVal;
        [FieldOffset(8)] internal IntPtr pwszVal;
        [FieldOffset(8)] internal IntPtr punkVal;
        [FieldOffset(8)] internal PropVariantCA ca;

        internal void InitVector<T>(T[] array, VarEnum varEnum) where T : unmanaged
        {
            InitSpan(array.AsSpan(), varEnum | VarEnum.VT_VECTOR);
        }

        internal void InitBlob(byte[] blob)
        {
            InitSpan(blob.AsSpan(), VarEnum.VT_BLOB);
        }

        private void InitSpan<T>(ReadOnlySpan<T> span, VarEnum vt) where T : unmanaged
        {
            varType = (ushort)vt;
            ca.cElems = 0;
            ca.pElems = IntPtr.Zero;

            int length = span.Length;
            if (length <= 0)
            {
                return;
            }

            ReadOnlySpan<byte> bytes = MemoryMarshal.AsBytes(span);
            IntPtr destPtr = IntPtr.Zero;
            try
            {
                destPtr = Marshal.AllocCoTaskMem(bytes.Length);
                bytes.CopyTo(new Span<byte>((void*)destPtr, bytes.Length));

                ca.cElems = (uint)length;
                ca.pElems = destPtr;
                destPtr = IntPtr.Zero;
            }
            finally
            {
                if (destPtr != IntPtr.Zero)
                {
                    Marshal.FreeCoTaskMem(destPtr);
                }
            }
        }

        internal void Init(string[] value, bool fAscii)
        {
            varType = (ushort)((fAscii ? VarEnum.VT_LPSTR : VarEnum.VT_LPWSTR) | VarEnum.VT_VECTOR);
            ca.cElems = 0;
            ca.pElems = IntPtr.Zero;

            int length = value.Length;
            if (length <= 0)
            {
                return;
            }

            IntPtr destPtr = IntPtr.Zero;
            int sizeIntPtr = sizeof(IntPtr);
            int index = 0;

            try
            {
                destPtr = Marshal.AllocCoTaskMem(checked(sizeIntPtr * length));

                for (index = 0; index < length; index++)
                {
                    IntPtr pString = fAscii
                        ? Marshal.StringToCoTaskMemAnsi(value[index])
                        : WicNativeExports.StringToCoTaskMemUni(value[index]);
                    Marshal.WriteIntPtr(destPtr, index * sizeIntPtr, pString);
                }

                ca.cElems = (uint)length;
                ca.pElems = destPtr;
                destPtr = IntPtr.Zero;
            }
            finally
            {
                if (destPtr != IntPtr.Zero)
                {
                    for (int i = 0; i < index; i++)
                    {
                        IntPtr pString = Marshal.ReadIntPtr(destPtr, i * sizeIntPtr);
                        Marshal.FreeCoTaskMem(pString);
                    }

                    Marshal.FreeCoTaskMem(destPtr);
                }
            }
        }

        internal void Init(object value)
        {
            if (value == null)
            {
                varType = (ushort)VarEnum.VT_EMPTY;
                return;
            }

            switch (value)
            {
                case sbyte[] sbArr:
                    InitVector(sbArr, VarEnum.VT_I1);
                    break;
                case byte[] bArr:
                    InitVector(bArr, VarEnum.VT_UI1);
                    break;
                case char[] cArr:
                    varType = (ushort)VarEnum.VT_LPSTR;
                    pszVal = Marshal.StringToCoTaskMemAnsi(new string(cArr));
                    break;
                case char[][] ccArr:
                {
                    string[] strArray = new string[ccArr.Length];
                    for (int i = 0; i < ccArr.Length; i++)
                    {
                        strArray[i] = new string(ccArr[i]);
                    }
                    Init(strArray, fAscii: true);
                    break;
                }
                case short[] sArr:
                    InitVector(sArr, VarEnum.VT_I2);
                    break;
                case ushort[] usArr:
                    InitVector(usArr, VarEnum.VT_UI2);
                    break;
                case int[] iArr:
                    InitVector(iArr, VarEnum.VT_I4);
                    break;
                case uint[] uiArr:
                    InitVector(uiArr, VarEnum.VT_UI4);
                    break;
                case long[] lArr:
                    InitVector(lArr, VarEnum.VT_I8);
                    break;
                case ulong[] ulArr:
                    InitVector(ulArr, VarEnum.VT_UI8);
                    break;
                case float[] fArr:
                    InitVector(fArr, VarEnum.VT_R4);
                    break;
                case double[] dArr:
                    InitVector(dArr, VarEnum.VT_R8);
                    break;
                case Guid[] gArr:
                    InitVector(gArr, VarEnum.VT_CLSID);
                    break;
                case string[] strArr:
                    Init(strArr, fAscii: false);
                    break;
                case bool[] boolArr:
                {
                    short[] array = new short[boolArr.Length];
                    for (int i = 0; i < boolArr.Length; i++)
                    {
                        array[i] = (short)(boolArr[i] ? -1 : 0);
                    }
                    InitVector(array, VarEnum.VT_BOOL);
                    break;
                }
                case string sVal:
                    varType = (ushort)VarEnum.VT_LPWSTR;
                    pwszVal = WicNativeExports.StringToCoTaskMemUni(sVal);
                    break;
                case sbyte sbVal:
                    varType = (ushort)VarEnum.VT_I1;
                    cVal = sbVal;
                    break;
                case byte byVal:
                    varType = (ushort)VarEnum.VT_UI1;
                    bVal = byVal;
                    break;
                case FILETIME ftVal:
                    varType = (ushort)VarEnum.VT_FILETIME;
                    filetime = ftVal;
                    break;
                case char chVal:
                    varType = (ushort)VarEnum.VT_LPSTR;
                    pszVal = Marshal.StringToCoTaskMemAnsi(new string(stackalloc char[] { chVal }));
                    break;
                case short shVal:
                    varType = (ushort)VarEnum.VT_I2;
                    iVal = shVal;
                    break;
                case ushort ushVal:
                    varType = (ushort)VarEnum.VT_UI2;
                    uiVal = ushVal;
                    break;
                case int inVal:
                    varType = (ushort)VarEnum.VT_I4;
                    intVal = inVal;
                    break;
                case uint uinVal:
                    varType = (ushort)VarEnum.VT_UI4;
                    uintVal = uinVal;
                    break;
                case long lgVal:
                    varType = (ushort)VarEnum.VT_I8;
                    lVal = lgVal;
                    break;
                case ulong ulgVal:
                    varType = (ushort)VarEnum.VT_UI8;
                    ulVal = ulgVal;
                    break;
                case float flVal:
                    varType = (ushort)VarEnum.VT_R4;
                    fltVal = flVal;
                    break;
                case double dbVal:
                    varType = (ushort)VarEnum.VT_R8;
                    dblVal = dbVal;
                    break;
                case Guid gVal:
                {
                    varType = (ushort)VarEnum.VT_CLSID;
                    pclsidVal = Marshal.AllocCoTaskMem(sizeof(Guid));
                    *(Guid*)pclsidVal = gVal;
                    break;
                }
                case bool blVal:
                    varType = (ushort)VarEnum.VT_BOOL;
                    boolVal = (short)(blVal ? -1 : 0);
                    break;
                case BitmapMetadataBlob blob:
                    InitBlob(blob.InternalGetBlobValue());
                    break;
                case BitmapMetadata metadata:
                {
                    SafeMILHandle metadataHandle = metadata.InternalMetadataHandle;
                    if (metadataHandle == null || metadataHandle.IsInvalid)
                    {
                        throw new NotImplementedException();
                    }

                    void* queryPtr = null;
                    HRESULT.Check(ComPtr<byte>.QueryInterface(
                        (void*)metadataHandle.DangerousGetHandle(),
                        MILGuidData.IID_IWICMetadataQueryReader,
                        &queryPtr));

                    varType = (ushort)VarEnum.VT_UNKNOWN;
                    punkVal = (IntPtr)queryPtr;
                    break;
                }
                default:
                    throw new InvalidOperationException(SR.Image_PropertyNotSupported);
            }
        }

        internal void Clear()
        {
            VarEnum vt = (VarEnum)varType;

            if ((vt & VarEnum.VT_VECTOR) != 0 || vt == VarEnum.VT_BLOB)
            {
                if (ca.pElems != IntPtr.Zero)
                {
                    VarEnum baseVt = vt & ~VarEnum.VT_VECTOR;
                    if (baseVt == VarEnum.VT_UNKNOWN)
                    {
                        for (uint i = 0; i < ca.cElems; i++)
                        {
                            void* punk = (void*)Marshal.ReadIntPtr(ca.pElems, checked((int)(i * sizeof(IntPtr))));
                            ComHelpers.Release(punk);
                        }
                    }
                    else if (baseVt == VarEnum.VT_LPWSTR || baseVt == VarEnum.VT_LPSTR)
                    {
                        for (uint i = 0; i < ca.cElems; i++)
                        {
                            Marshal.FreeCoTaskMem(Marshal.ReadIntPtr(ca.pElems, checked((int)(i * sizeof(IntPtr)))));
                        }
                    }

                    Marshal.FreeCoTaskMem(ca.pElems);
                }
            }
            else if (vt == VarEnum.VT_LPWSTR || vt == VarEnum.VT_LPSTR || vt == VarEnum.VT_CLSID)
            {
                Marshal.FreeCoTaskMem(pwszVal);
            }
            else if (vt == VarEnum.VT_UNKNOWN && punkVal != IntPtr.Zero)
            {
                ComHelpers.Release((void*)punkVal);
            }

            this = default;
        }

        internal object ToObject(object syncObject)
        {
            VarEnum vt = (VarEnum)varType;

            if ((vt & VarEnum.VT_VECTOR) != 0)
            {
                int count = (int)ca.cElems;
                switch (vt & ~VarEnum.VT_VECTOR)
                {
                    case VarEnum.VT_EMPTY:
                        return null;
                    case VarEnum.VT_I1:
                    {
                        var arr = new sbyte[count];
                        new ReadOnlySpan<sbyte>((void*)ca.pElems, count).CopyTo(arr);
                        return arr;
                    }
                    case VarEnum.VT_UI1:
                    {
                        var arr = new byte[count];
                        new ReadOnlySpan<byte>((void*)ca.pElems, count).CopyTo(arr);
                        return arr;
                    }
                    case VarEnum.VT_I2:
                    {
                        var arr = new short[count];
                        new ReadOnlySpan<short>((void*)ca.pElems, count).CopyTo(arr);
                        return arr;
                    }
                    case VarEnum.VT_UI2:
                    {
                        var arr = new ushort[count];
                        new ReadOnlySpan<ushort>((void*)ca.pElems, count).CopyTo(arr);
                        return arr;
                    }
                    case VarEnum.VT_I4:
                    {
                        var arr = new int[count];
                        new ReadOnlySpan<int>((void*)ca.pElems, count).CopyTo(arr);
                        return arr;
                    }
                    case VarEnum.VT_UI4:
                    {
                        var arr = new uint[count];
                        new ReadOnlySpan<uint>((void*)ca.pElems, count).CopyTo(arr);
                        return arr;
                    }
                    case VarEnum.VT_I8:
                    {
                        var arr = new long[count];
                        new ReadOnlySpan<long>((void*)ca.pElems, count).CopyTo(arr);
                        return arr;
                    }
                    case VarEnum.VT_UI8:
                    {
                        var arr = new ulong[count];
                        new ReadOnlySpan<ulong>((void*)ca.pElems, count).CopyTo(arr);
                        return arr;
                    }
                    case VarEnum.VT_R4:
                    {
                        var arr = new float[count];
                        new ReadOnlySpan<float>((void*)ca.pElems, count).CopyTo(arr);
                        return arr;
                    }
                    case VarEnum.VT_R8:
                    {
                        var arr = new double[count];
                        new ReadOnlySpan<double>((void*)ca.pElems, count).CopyTo(arr);
                        return arr;
                    }
                    case VarEnum.VT_BOOL:
                    {
                        var arr = new bool[count];
                        var src = new ReadOnlySpan<short>((void*)ca.pElems, count);
                        for (int i = 0; i < count; i++) arr[i] = src[i] != 0;
                        return arr;
                    }
                    case VarEnum.VT_CLSID:
                    {
                        var arr = new Guid[count];
                        new ReadOnlySpan<Guid>((void*)ca.pElems, count).CopyTo(arr);
                        return arr;
                    }
                    case VarEnum.VT_LPSTR:
                    {
                        var arr = new string[count];
                        for (int i = 0; i < count; i++)
                        {
                            arr[i] = Marshal.PtrToStringAnsi(Marshal.ReadIntPtr(ca.pElems, i * sizeof(IntPtr)));
                        }
                        return arr;
                    }
                    case VarEnum.VT_LPWSTR:
                    {
                        var arr = new string[count];
                        for (int i = 0; i < count; i++)
                        {
                            arr[i] = Marshal.PtrToStringUni(Marshal.ReadIntPtr(ca.pElems, i * sizeof(IntPtr)));
                        }
                        return arr;
                    }
                }
            }
            else
            {
                switch (vt)
                {
                    case VarEnum.VT_EMPTY: return null;
                    case VarEnum.VT_I1: return cVal;
                    case VarEnum.VT_UI1: return bVal;
                    case VarEnum.VT_I2: return iVal;
                    case VarEnum.VT_UI2: return uiVal;
                    case VarEnum.VT_I4: return intVal;
                    case VarEnum.VT_UI4: return uintVal;
                    case VarEnum.VT_I8: return lVal;
                    case VarEnum.VT_UI8: return ulVal;
                    case VarEnum.VT_R4: return fltVal;
                    case VarEnum.VT_R8: return dblVal;
                    case VarEnum.VT_FILETIME: return filetime;
                    case VarEnum.VT_BOOL: return boolVal != 0;
                    case VarEnum.VT_CLSID: return *(Guid*)pclsidVal;
                    case VarEnum.VT_LPSTR: return Marshal.PtrToStringAnsi(pszVal);
                    case VarEnum.VT_LPWSTR: return Marshal.PtrToStringUni(pwszVal);
                    case VarEnum.VT_BLOB:
                    {
                        byte[] blob = new byte[ca.cElems];
                        new ReadOnlySpan<byte>((void*)ca.pElems, (int)ca.cElems).CopyTo(blob);
                        return new BitmapMetadataBlob(blob);
                    }
                    case VarEnum.VT_UNKNOWN:
                    {
                        void* queryHandle = null;
                        try
                        {
                            int hr = ComPtr<byte>.QueryInterface((void*)punkVal, MILGuidData.IID_IWICMetadataQueryWriter, &queryHandle);
                            if (hr == HRESULT.S_OK)
                            {
                                var metadataHandle = new SafeMILHandle((IntPtr)queryHandle);
                                queryHandle = null;
                                return new BitmapMetadata(metadataHandle, readOnly: false, fixedSize: false, syncObject);
                            }

                            hr = ComPtr<byte>.QueryInterface((void*)punkVal, MILGuidData.IID_IWICMetadataQueryReader, &queryHandle);
                            if (hr == HRESULT.S_OK)
                            {
                                var metadataHandle = new SafeMILHandle((IntPtr)queryHandle);
                                queryHandle = null;
                                return new BitmapMetadata(metadataHandle, readOnly: true, fixedSize: false, syncObject);
                            }

                            HRESULT.Check(hr);
                        }
                        finally
                        {
                            if (queryHandle != null)
                            {
                                ComHelpers.Release(queryHandle);
                            }
                        }
                        break;
                    }
                }
            }

            throw new NotSupportedException(SR.Image_PropertyNotSupported);
        }

        internal bool RequiresSyncObject => varType == (ushort)VarEnum.VT_UNKNOWN;
    }
}

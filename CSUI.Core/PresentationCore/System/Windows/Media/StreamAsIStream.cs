// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using CSUI.Core.Interop.DirectX;
using SR = MS.Internal.PresentationCore.SR;

namespace System.Windows.Media
{
    /// <summary>
    /// NativeAOT COM Callable Wrapper exposing a managed <see cref="Stream"/> as a Win32 COM <c>IStream</c>
    /// using a static <c>[UnmanagedCallersOnly]</c> vtable (zero <c>[ComImport]</c> / zero <c>MilCore</c>).
    /// </summary>
    internal sealed unsafe class StreamAsIStream
    {
        private const int S_OK = 0;
        private const int E_NOTIMPL = unchecked((int)0x80004001);
        private const int E_NOINTERFACE = unchecked((int)0x80004002);
        private const int E_POINTER = unchecked((int)0x80004003);
        private const int E_FAIL = unchecked((int)0x80004005);
        private const int E_INVALIDARG = unchecked((int)0x80070057);
        private const int STG_E_INVALIDFUNCTION = unchecked((int)0x80030001);
        private const int STG_E_INVALIDPOINTER = unchecked((int)0x80030009);
        private const int STG_E_SEEKERROR = unchecked((int)0x80030019);

        private const uint STREAM_SEEK_SET = 0;
        private const uint STREAM_SEEK_CUR = 1;
        private const uint STREAM_SEEK_END = 2;

        private const uint STGTY_STREAM = 2;
        private const uint LOCK_EXCLUSIVE = 2;

        [StructLayout(LayoutKind.Sequential)]
        private struct NativeIStream
        {
            public void** Vtbl;
            public nint Handle;
            public int RefCount;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct NATIVE_STATSTG
        {
            public nint pwcsName;
            public uint type;
            public ulong cbSize;
            public ulong mtime;
            public ulong ctime;
            public ulong atime;
            public uint grfMode;
            public uint grfLocksSupported;
            public Guid clsid;
            public uint grfStateBits;
            public uint reserved;
        }

        private static readonly void** s_vtbl;

        static StreamAsIStream()
        {
            s_vtbl = (void**)NativeMemory.AllocZeroed(14, (nuint)sizeof(void*));
            s_vtbl[0] = (delegate* unmanaged[Stdcall]<NativeIStream*, Guid*, void**, int>)&ComQueryInterface;
            s_vtbl[1] = (delegate* unmanaged[Stdcall]<NativeIStream*, uint>)&ComAddRef;
            s_vtbl[2] = (delegate* unmanaged[Stdcall]<NativeIStream*, uint>)&ComRelease;
            s_vtbl[3] = (delegate* unmanaged[Stdcall]<NativeIStream*, void*, uint, uint*, int>)&ComRead;
            s_vtbl[4] = (delegate* unmanaged[Stdcall]<NativeIStream*, void*, uint, uint*, int>)&ComWrite;
            s_vtbl[5] = (delegate* unmanaged[Stdcall]<NativeIStream*, long, uint, long*, int>)&ComSeek;
            s_vtbl[6] = (delegate* unmanaged[Stdcall]<NativeIStream*, ulong, int>)&ComSetSize;
            s_vtbl[7] = (delegate* unmanaged[Stdcall]<NativeIStream*, void*, ulong, ulong*, ulong*, int>)&ComCopyTo;
            s_vtbl[8] = (delegate* unmanaged[Stdcall]<NativeIStream*, uint, int>)&ComCommit;
            s_vtbl[9] = (delegate* unmanaged[Stdcall]<NativeIStream*, int>)&ComRevert;
            s_vtbl[10] = (delegate* unmanaged[Stdcall]<NativeIStream*, ulong, ulong, uint, int>)&ComLockRegion;
            s_vtbl[11] = (delegate* unmanaged[Stdcall]<NativeIStream*, ulong, ulong, uint, int>)&ComUnlockRegion;
            s_vtbl[12] = (delegate* unmanaged[Stdcall]<NativeIStream*, NATIVE_STATSTG*, uint, int>)&ComStat;
            s_vtbl[13] = (delegate* unmanaged[Stdcall]<NativeIStream*, void**, int>)&ComClone;
        }

        private readonly Stream _dataStream;
        private long _virtualPosition = -1;
        private Exception _lastException;

        private StreamAsIStream(Stream dataStream)
        {
            ArgumentNullException.ThrowIfNull(dataStream);
            _dataStream = dataStream;
        }

        internal Exception LastException => _lastException;

        internal static IntPtr IStreamFrom(Stream stream)
        {
            ArgumentNullException.ThrowIfNull(stream);

            var managed = new StreamAsIStream(stream);
            NativeIStream* native = (NativeIStream*)NativeMemory.Alloc((nuint)sizeof(NativeIStream));
            native->Vtbl = s_vtbl;
            native->Handle = GCHandle.ToIntPtr(GCHandle.Alloc(managed));
            native->RefCount = 1;
            return (IntPtr)native;
        }

        internal static IntPtr IStreamFrom(IntPtr memoryBuffer, int bufferSize)
        {
            if (memoryBuffer == IntPtr.Zero)
            {
                throw new ArgumentNullException(nameof(memoryBuffer));
            }

            if (bufferSize < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(bufferSize));
            }

            var memStream = new UnmanagedMemoryStream((byte*)memoryBuffer.ToPointer(), bufferSize, bufferSize, FileAccess.Read);
            return IStreamFrom(memStream);
        }

        internal static IntPtr IStreamMemoryFrom(IntPtr comStream)
        {
            if (comStream == IntPtr.Zero)
            {
                return IntPtr.Zero;
            }

            void* pStream = (void*)comStream;
            void** vtbl = *(void***)pStream;

            var seek = (delegate* unmanaged[Stdcall]<void*, long, uint, long*, int>)vtbl[5];
            var read = (delegate* unmanaged[Stdcall]<void*, void*, uint, uint*, int>)vtbl[3];
            var stat = (delegate* unmanaged[Stdcall]<void*, NATIVE_STATSTG*, uint, int>)vtbl[12];

            NATIVE_STATSTG statStg = default;
            int hrStat = stat(pStream, &statStg, 1);
            int initialCapacity = (hrStat >= 0 && statStg.cbSize > 0 && statStg.cbSize <= int.MaxValue)
                ? (int)statStg.cbSize
                : 0;

            var memoryStream = initialCapacity > 0 ? new MemoryStream(initialCapacity) : new MemoryStream();

            long ignoredPos = 0;
            seek(pStream, 0, STREAM_SEEK_SET, &ignoredPos);

            byte[] buffer = new byte[4096];
            fixed (byte* pBuffer = buffer)
            {
                while (true)
                {
                    uint bytesRead = 0;
                    int hrRead = read(pStream, pBuffer, (uint)buffer.Length, &bytesRead);
                    if (hrRead < 0)
                    {
                        return IntPtr.Zero;
                    }

                    if (bytesRead == 0)
                    {
                        break;
                    }

                    memoryStream.Write(buffer, 0, (int)bytesRead);
                }
            }

            memoryStream.Position = 0;
            return IStreamFrom(memoryStream);
        }

        private static StreamAsIStream Unwrap(NativeIStream* self)
        {
            if (self == null || self->Handle == 0)
            {
                return null;
            }

            return GCHandle.FromIntPtr(self->Handle).Target as StreamAsIStream;
        }

        private static int GetHResult(Exception e)
        {
            return e.HResult != 0 ? e.HResult : E_FAIL;
        }

        private void Verify()
        {
            if (_dataStream == null)
            {
                throw new ObjectDisposedException(SR.Media_StreamClosed);
            }
        }

        private void ActualizeVirtualPosition()
        {
            if (_virtualPosition == -1)
            {
                return;
            }

            if (_virtualPosition > _dataStream.Length)
            {
                _dataStream.SetLength(_virtualPosition);
            }

            _dataStream.Position = _virtualPosition;
            _virtualPosition = -1;
        }

        private int ReadCore(void* pv, uint cb, uint* pcbRead)
        {
            if (pcbRead != null)
            {
                *pcbRead = 0;
            }

            if (cb > 0 && pv == null)
            {
                return STG_E_INVALIDPOINTER;
            }

            try
            {
                Verify();
                ActualizeVirtualPosition();

                uint totalRead = 0;
                byte* dst = (byte*)pv;
                while (totalRead < cb)
                {
                    int chunk = (int)Math.Min(cb - totalRead, (uint)int.MaxValue);
                    int n = _dataStream.Read(new Span<byte>(dst + totalRead, chunk));
                    if (n <= 0)
                    {
                        break;
                    }
                    totalRead += (uint)n;
                }

                if (pcbRead != null)
                {
                    *pcbRead = totalRead;
                }

                return S_OK;
            }
            catch (Exception e)
            {
                _lastException = e;
                return GetHResult(e);
            }
        }

        private int WriteCore(void* pv, uint cb, uint* pcbWritten)
        {
            if (pcbWritten != null)
            {
                *pcbWritten = 0;
            }

            if (cb > 0 && pv == null)
            {
                return STG_E_INVALIDPOINTER;
            }

            try
            {
                Verify();
                ActualizeVirtualPosition();

                uint totalWritten = 0;
                byte* src = (byte*)pv;
                while (totalWritten < cb)
                {
                    int chunk = (int)Math.Min(cb - totalWritten, (uint)int.MaxValue);
                    _dataStream.Write(new ReadOnlySpan<byte>(src + totalWritten, chunk));
                    totalWritten += (uint)chunk;
                }

                if (pcbWritten != null)
                {
                    *pcbWritten = totalWritten;
                }

                return S_OK;
            }
            catch (Exception e)
            {
                _lastException = e;
                return GetHResult(e);
            }
        }

        private int SeekCore(long offset, uint origin, long* plibNewPosition)
        {
            try
            {
                Verify();

                long pos = _virtualPosition == -1 ? _dataStream.Position : _virtualPosition;
                long len = _dataStream.Length;

                long target;
                switch (origin)
                {
                    case STREAM_SEEK_SET:
                        target = offset;
                        break;
                    case STREAM_SEEK_CUR:
                        target = checked(pos + offset);
                        break;
                    case STREAM_SEEK_END:
                        target = checked(len + offset);
                        break;
                    default:
                        return STG_E_INVALIDFUNCTION;
                }

                if (target < 0)
                {
                    return STG_E_SEEKERROR;
                }

                if (target <= len)
                {
                    _dataStream.Position = target;
                    _virtualPosition = -1;
                }
                else
                {
                    _virtualPosition = target;
                }

                if (plibNewPosition != null)
                {
                    *plibNewPosition = _virtualPosition != -1 ? _virtualPosition : _dataStream.Position;
                }

                return S_OK;
            }
            catch (Exception e)
            {
                _lastException = e;
                return GetHResult(e);
            }
        }

        private int SetSizeCore(ulong value)
        {
            try
            {
                Verify();
                if (value > long.MaxValue)
                {
                    return E_INVALIDARG;
                }

                _dataStream.SetLength((long)value);
                return S_OK;
            }
            catch (Exception e)
            {
                _lastException = e;
                return GetHResult(e);
            }
        }

        private int CopyToCore(void* pstm, ulong cb, ulong* pcbRead, ulong* pcbWritten)
        {
            if (pcbRead != null)
            {
                *pcbRead = 0;
            }

            if (pcbWritten != null)
            {
                *pcbWritten = 0;
            }

            if (pstm == null)
            {
                return STG_E_INVALIDPOINTER;
            }

            try
            {
                Verify();

                void** pstmVtbl = *(void***)pstm;
                var pstmWrite = (delegate* unmanaged[Stdcall]<void*, void*, uint, uint*, int>)pstmVtbl[4];

                const uint BufSize = 4096;
                byte* buffer = stackalloc byte[(int)BufSize];
                ulong totalRead = 0;
                ulong totalWritten = 0;

                while (totalWritten < cb)
                {
                    uint toRead = (uint)Math.Min(BufSize, cb - totalWritten);
                    uint bytesRead = 0;
                    int hr = ReadCore(buffer, toRead, &bytesRead);
                    if (hr < 0)
                    {
                        return hr;
                    }

                    if (bytesRead == 0)
                    {
                        break;
                    }

                    totalRead += bytesRead;

                    uint bytesWritten = 0;
                    hr = pstmWrite(pstm, buffer, bytesRead, &bytesWritten);
                    totalWritten += bytesWritten;

                    if (hr < 0 || bytesWritten != bytesRead)
                    {
                        if (pcbRead != null) *pcbRead = totalRead;
                        if (pcbWritten != null) *pcbWritten = totalWritten;
                        return hr < 0 ? hr : E_FAIL;
                    }
                }

                if (pcbRead != null)
                {
                    *pcbRead = totalRead;
                }

                if (pcbWritten != null)
                {
                    *pcbWritten = totalWritten;
                }

                return S_OK;
            }
            catch (Exception e)
            {
                _lastException = e;
                return GetHResult(e);
            }
        }

        private int CommitCore()
        {
            try
            {
                Verify();
                _dataStream.Flush();
                ActualizeVirtualPosition();
                return S_OK;
            }
            catch (Exception e)
            {
                _lastException = e;
                return GetHResult(e);
            }
        }

        private int StatCore(NATIVE_STATSTG* pstatstg)
        {
            if (pstatstg == null)
            {
                return STG_E_INVALIDPOINTER;
            }

            *pstatstg = default;

            try
            {
                Verify();
                pstatstg->type = STGTY_STREAM;
                pstatstg->cbSize = (ulong)_dataStream.Length;
                pstatstg->grfLocksSupported = LOCK_EXCLUSIVE;
                pstatstg->pwcsName = 0;
                return S_OK;
            }
            catch (Exception e)
            {
                _lastException = e;
                return GetHResult(e);
            }
        }

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
        private static int ComQueryInterface(NativeIStream* self, Guid* riid, void** ppvObject)
        {
            if (riid == null || ppvObject == null)
            {
                return E_POINTER;
            }

            if (*riid == WicGuidsEx.IID_IUnknown ||
                *riid == WicGuidsEx.IID_ISequentialStream ||
                *riid == WicGuidsEx.IID_IStream)
            {
                *ppvObject = self;
                Interlocked.Increment(ref self->RefCount);
                return S_OK;
            }

            *ppvObject = null;
            return E_NOINTERFACE;
        }

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
        private static uint ComAddRef(NativeIStream* self)
        {
            if (self == null)
            {
                return 0;
            }

            return (uint)Interlocked.Increment(ref self->RefCount);
        }

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
        private static uint ComRelease(NativeIStream* self)
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
        private static int ComRead(NativeIStream* self, void* pv, uint cb, uint* pcbRead)
        {
            StreamAsIStream stream = Unwrap(self);
            return stream != null ? stream.ReadCore(pv, cb, pcbRead) : E_FAIL;
        }

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
        private static int ComWrite(NativeIStream* self, void* pv, uint cb, uint* pcbWritten)
        {
            StreamAsIStream stream = Unwrap(self);
            return stream != null ? stream.WriteCore(pv, cb, pcbWritten) : E_FAIL;
        }

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
        private static int ComSeek(NativeIStream* self, long dlibMove, uint dwOrigin, long* plibNewPosition)
        {
            StreamAsIStream stream = Unwrap(self);
            return stream != null ? stream.SeekCore(dlibMove, dwOrigin, plibNewPosition) : E_FAIL;
        }

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
        private static int ComSetSize(NativeIStream* self, ulong libNewSize)
        {
            StreamAsIStream stream = Unwrap(self);
            return stream != null ? stream.SetSizeCore(libNewSize) : E_FAIL;
        }

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
        private static int ComCopyTo(NativeIStream* self, void* pstm, ulong cb, ulong* pcbRead, ulong* pcbWritten)
        {
            StreamAsIStream stream = Unwrap(self);
            return stream != null ? stream.CopyToCore(pstm, cb, pcbRead, pcbWritten) : E_FAIL;
        }

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
        private static int ComCommit(NativeIStream* self, uint grfCommitFlags)
        {
            StreamAsIStream stream = Unwrap(self);
            return stream != null ? stream.CommitCore() : E_FAIL;
        }

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
        private static int ComRevert(NativeIStream* self)
        {
            StreamAsIStream stream = Unwrap(self);
            if (stream == null)
            {
                return E_FAIL;
            }

            try
            {
                stream.Verify();
                return E_NOTIMPL;
            }
            catch (Exception e)
            {
                stream._lastException = e;
                return GetHResult(e);
            }
        }

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
        private static int ComLockRegion(NativeIStream* self, ulong libOffset, ulong cb, uint dwLockType)
        {
            StreamAsIStream stream = Unwrap(self);
            if (stream == null)
            {
                return E_FAIL;
            }

            try
            {
                stream.Verify();
                return E_NOTIMPL;
            }
            catch (Exception e)
            {
                stream._lastException = e;
                return GetHResult(e);
            }
        }

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
        private static int ComUnlockRegion(NativeIStream* self, ulong libOffset, ulong cb, uint dwLockType)
        {
            StreamAsIStream stream = Unwrap(self);
            if (stream == null)
            {
                return E_FAIL;
            }

            try
            {
                stream.Verify();
                return E_NOTIMPL;
            }
            catch (Exception e)
            {
                stream._lastException = e;
                return GetHResult(e);
            }
        }

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
        private static int ComStat(NativeIStream* self, NATIVE_STATSTG* pstatstg, uint grfStatFlag)
        {
            StreamAsIStream stream = Unwrap(self);
            return stream != null ? stream.StatCore(pstatstg) : E_FAIL;
        }

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
        private static int ComClone(NativeIStream* self, void** ppstm)
        {
            if (ppstm != null)
            {
                *ppstm = null;
            }

            StreamAsIStream stream = Unwrap(self);
            if (stream == null)
            {
                return E_FAIL;
            }

            try
            {
                stream.Verify();
                return E_NOTIMPL;
            }
            catch (Exception e)
            {
                stream._lastException = e;
                return GetHResult(e);
            }
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using CSUI.Core.Interop.DirectX;

namespace MS.Internal.Text.TextInterface
{
    internal sealed unsafe class FontFileStream : IDWriteFontFileStreamManaged, IDisposable
    {
        private const int S_OK = 0;
        private const int E_INVALIDARG = unchecked((int)0x80070057L);

        private readonly IFontSource _fontSource;
        private readonly DateTime _lastWriteTimeUtc;
        private readonly Lock _lock = new();

        private UnmanagedMemoryStream? _unmanagedStream;
        private byte* _nativeBuffer;
        private ulong _fileSize;
        private bool _initialized;
        private bool _disposed;

        internal FontFileStream(IFontSource fontSource)
        {
            ArgumentNullException.ThrowIfNull(fontSource);
            _fontSource = fontSource;
            _lastWriteTimeUtc = fontSource.GetLastWriteTimeUtc();
        }

        ~FontFileStream()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            lock (_lock)
            {
                if (_disposed)
                    return;

                _disposed = true;

                if (disposing)
                {
                    _unmanagedStream?.Dispose();
                    _unmanagedStream = null;
                }

                if (_nativeBuffer != null)
                {
                    NativeMemory.Free(_nativeBuffer);
                    _nativeBuffer = null;
                }
            }
        }

        private void EnsureLoaded()
        {
            if (_initialized)
                return;

            lock (_lock)
            {
                if (_initialized || _disposed)
                    return;

                UnmanagedMemoryStream? ums = _fontSource.GetUnmanagedStream();
                if (ums != null)
                {
                    _fileSize = checked((ulong)ums.Length);
                    ums.Position = 0;
                    _unmanagedStream = ums;
                    _nativeBuffer = null;
                }
                else
                {
                    using Stream stream = _fontSource.GetStream();
                    long len = stream.Length;
                    _fileSize = checked((ulong)len);
                    byte* buf = len > 0 ? (byte*)NativeMemory.Alloc((nuint)len) : null;
                    try
                    {
                        if (len > 0 && buf != null)
                        {
                            Span<byte> dest = new Span<byte>(buf, checked((int)len));
                            stream.Position = 0;
                            stream.ReadExactly(dest);
                        }
                        _nativeBuffer = buf;
                    }
                    catch
                    {
                        if (buf != null)
                            NativeMemory.Free(buf);
                        throw;
                    }
                }

                _initialized = true;
            }
        }

        public int ReadFileFragment(void** fragmentStart, ulong fileOffset, ulong fragmentSize, void** fragmentContext)
        {
            if (fragmentStart == null || fragmentContext == null)
                return E_INVALIDARG;

            *fragmentStart = null;
            *fragmentContext = null;

            EnsureLoaded();

            if (fileOffset > _fileSize || fragmentSize > _fileSize - fileOffset)
                return E_INVALIDARG;

            byte* basePtr;
            if (_unmanagedStream != null)
            {
                basePtr = _unmanagedStream.PositionPointer - _unmanagedStream.Position;
            }
            else
            {
                basePtr = _nativeBuffer;
            }

            if (basePtr == null && fragmentSize > 0)
                return NativeComInstance.E_FAIL;

            *fragmentStart = basePtr == null ? null : basePtr + fileOffset;
            return S_OK;
        }

        public void ReleaseFileFragment(void* fragmentContext)
        {
            // Memory-mapped / unmanaged stream view stays valid for the lifetime of FontFileStream.
        }

        public int GetFileSize(ulong* fileSize)
        {
            if (fileSize == null)
                return E_INVALIDARG;

            EnsureLoaded();
            *fileSize = _fileSize;
            return S_OK;
        }

        public int GetLastWriteTime(ulong* lastWriteTime)
        {
            if (lastWriteTime == null)
                return E_INVALIDARG;

            *lastWriteTime = (ulong)_lastWriteTimeUtc.ToFileTimeUtc();
            return S_OK;
        }
    }
}

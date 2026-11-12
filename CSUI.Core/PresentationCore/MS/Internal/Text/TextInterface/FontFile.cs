// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using CSUI.Core.Interop.DirectX;

namespace MS.Internal.Text.TextInterface
{
    internal sealed unsafe class FontFile : IDisposable
    {
        private nint _fontFile;

        internal FontFile(void* fontFile)
        {
            _fontFile = (nint)fontFile;
        }

        ~FontFile()
        {
            Dispose(false);
        }

        internal void* DWriteFontFileNoAddRef => (void*)_fontFile;

        public void Release()
        {
            Dispose();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            ComHelpers.ReleaseAtomic(ref _fontFile);
        }

        internal bool Analyze(
            out FontFileType dwriteFontFileType,
            out FontFaceType dwriteFontFaceType,
            out uint numberOfFaces,
            int* hr)
        {
            void* p = (void*)_fontFile;
            if (p == null)
            {
                dwriteFontFileType = FontFileType.Unknown;
                dwriteFontFaceType = FontFaceType.Unknown;
                numberOfFaces = 0;
                if (hr != null)
                    *hr = NativeComInstance.E_FAIL;
                return false;
            }

            int result = IDWriteFontFileVtbl.Analyze(
                p,
                out bool isSupportedFontType,
                out uint fileType,
                out uint faceType,
                out numberOfFaces);

            if (hr != null)
                *hr = result;

            dwriteFontFileType = (FontFileType)fileType;
            dwriteFontFaceType = (FontFaceType)faceType;
            return result >= 0 && isSupportedFontType;
        }

        internal string GetUriPath()
        {
            void* p = (void*)_fontFile;
            if (p == null)
                throw new ObjectDisposedException(nameof(FontFile));

            void* pRefKey;
            uint refKeySize;
            int hr = IDWriteFontFileVtbl.GetReferenceKey(p, out pRefKey, out refKeySize);
            DWriteUtil.ConvertHresultToException(hr);

            void* pLoader = null;
            hr = IDWriteFontFileVtbl.GetLoader(p, &pLoader);
            DWriteUtil.ConvertHresultToException(hr);

            try
            {
                if (ComHelpers.QueryInterface(pLoader, DxWriteGuids.IID_IDWriteLocalFontFileLoader, out void* pLocalLoader) >= 0 &&
                    pLocalLoader != null)
                {
                    try
                    {
                        hr = IDWriteLocalFontFileLoaderVtbl.GetFilePathLengthFromKey(pLocalLoader, pRefKey, refKeySize, out uint pathLength);
                        DWriteUtil.ConvertHresultToException(hr);

                        char[] buf = new char[checked((int)pathLength + 1)];
                        fixed (char* pBuf = buf)
                        {
                            hr = IDWriteLocalFontFileLoaderVtbl.GetFilePathFromKey(pLocalLoader, pRefKey, refKeySize, pBuf, pathLength + 1);
                            DWriteUtil.ConvertHresultToException(hr);
                            string filePath = new string(pBuf, 0, (int)pathLength);
                            return new Uri(filePath).AbsoluteUri;
                        }
                    }
                    finally
                    {
                        ComHelpers.Release(pLocalLoader);
                    }
                }

                // Custom WPF FontFileLoader stores UTF-16 null-terminated URI string as the reference key.
                int charCount = (int)(refKeySize / sizeof(char));
                char* chars = (char*)pRefKey;
                while (charCount > 0 && chars[charCount - 1] == '\0')
                    charCount--;
                return new string(chars, 0, charCount);
            }
            finally
            {
                ComHelpers.Release(pLoader);
            }
        }
    }
}

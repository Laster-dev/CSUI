// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using CSUI.Core.Interop.DirectX;

namespace MS.Internal.Text.TextInterface
{
    internal sealed unsafe class FontFace : IDisposable
    {
        private nint _fontFace;

        internal FontFace(void* fontFace)
        {
            _fontFace = (nint)fontFace;
        }

        ~FontFace()
        {
            Dispose(false);
        }

        internal void* DWriteFontFaceNoAddRef => (void*)_fontFace;

        internal IntPtr DWriteFontFaceAddRef
        {
            get
            {
                void* p = (void*)_fontFace;
                if (p == null)
                    throw new ObjectDisposedException(nameof(FontFace));
                ComHelpers.AddRef(p);
                return (IntPtr)p;
            }
        }

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
            ComHelpers.ReleaseAtomic(ref _fontFace);
        }

        private void* RequirePointer()
        {
            void* p = (void*)_fontFace;
            if (p == null)
                throw new ObjectDisposedException(nameof(FontFace));
            return p;
        }

        internal FontFaceType Type => (FontFaceType)IDWriteFontFaceVtbl.GetType(RequirePointer());
        internal uint Index => IDWriteFontFaceVtbl.GetIndex(RequirePointer());
        internal ushort GlyphCount => IDWriteFontFaceVtbl.GetGlyphCount(RequirePointer());
        internal bool IsSymbolFont => IDWriteFontFaceVtbl.IsSymbolFont(RequirePointer());
        internal FontSimulations SimulationFlags => (FontSimulations)IDWriteFontFaceVtbl.GetSimulations(RequirePointer());

        internal FontMetrics Metrics
        {
            get
            {
                IDWriteFontFaceVtbl.GetMetrics(RequirePointer(), out DWRITE_FONT_METRICS native);
                return new FontMetrics(native);
            }
        }

        internal FontFile GetFileZero()
        {
            void* p = RequirePointer();
            uint count = 0;
            int hr = IDWriteFontFaceVtbl.GetFiles(p, ref count, null);
            // DirectWrite returns E_NOT_SUFFICIENT_BUFFER or S_OK with count populated when querying count.
            if (count == 0)
            {
                DWriteUtil.ConvertHresultToException(hr);
                throw new InvalidOperationException("FontFace has no associated FontFile.");
            }

            if (count <= 16)
            {
                void** files = stackalloc void*[(int)count];
                hr = IDWriteFontFaceVtbl.GetFiles(p, ref count, files);
                DWriteUtil.ConvertHresultToException(hr);
                for (uint i = 1; i < count; i++)
                {
                    ComHelpers.Release(files[i]);
                }
                return new FontFile(files[0]);
            }
            else
            {
                nint[] heapFiles = new nint[(int)count];
                fixed (nint* pHeap = heapFiles)
                {
                    hr = IDWriteFontFaceVtbl.GetFiles(p, ref count, (void**)pHeap);
                    DWriteUtil.ConvertHresultToException(hr);
                    for (uint i = 1; i < count; i++)
                    {
                        ComHelpers.Release((void*)pHeap[i]);
                    }
                    return new FontFile((void*)pHeap[0]);
                }
            }
        }

        internal void GetDesignGlyphMetrics(ushort* pGlyphIndices, uint numberOfGlyphs, GlyphMetrics* pGlyphMetrics)
        {
            int hr = IDWriteFontFaceVtbl.GetDesignGlyphMetrics(
                RequirePointer(),
                pGlyphIndices,
                numberOfGlyphs,
                (DWRITE_GLYPH_METRICS*)pGlyphMetrics,
                false);
            DWriteUtil.ConvertHresultToException(hr);
        }

        internal void GetDisplayGlyphMetrics(
            ushort* pGlyphIndices,
            uint numberOfGlyphs,
            GlyphMetrics* pGlyphMetrics,
            float emSize,
            bool useGdiNatural,
            bool isSideways,
            float pixelsPerDip)
        {
            int hr = IDWriteFontFaceVtbl.GetGdiCompatibleGlyphMetrics(
                RequirePointer(),
                emSize,
                pixelsPerDip,
                null,
                useGdiNatural,
                pGlyphIndices,
                numberOfGlyphs,
                (DWRITE_GLYPH_METRICS*)pGlyphMetrics,
                isSideways);
            DWriteUtil.ConvertHresultToException(hr);
        }

        internal void GetArrayOfGlyphIndices(uint* pCodePoints, uint characterCount, ushort* pGlyphIndices)
        {
            int hr = IDWriteFontFaceVtbl.GetGlyphIndices(
                RequirePointer(),
                pCodePoints,
                characterCount,
                pGlyphIndices);
            DWriteUtil.ConvertHresultToException(hr);
        }

        internal bool TryGetFontTable(OpenTypeTableTag openTypeTableTag, out byte[]? tableData)
        {
            void* p = RequirePointer();
            int hr = IDWriteFontFaceVtbl.TryGetFontTable(
                p,
                (uint)openTypeTableTag,
                out void* pData,
                out uint tableSize,
                out void* tableContext,
                out bool exists);
            DWriteUtil.ConvertHresultToException(hr);

            if (!exists || pData == null)
            {
                if (tableContext != null)
                    IDWriteFontFaceVtbl.ReleaseFontTable(p, tableContext);
                tableData = null;
                return false;
            }

            try
            {
                tableData = new ReadOnlySpan<byte>(pData, checked((int)tableSize)).ToArray();
                return true;
            }
            finally
            {
                if (tableContext != null)
                    IDWriteFontFaceVtbl.ReleaseFontTable(p, tableContext);
            }
        }

        internal bool ReadFontEmbeddingRights(out ushort fsType)
        {
            void* p = RequirePointer();
            int hr = IDWriteFontFaceVtbl.TryGetFontTable(
                p,
                (uint)OpenTypeTableTag.TTO_OS2,
                out void* pData,
                out uint tableSize,
                out void* tableContext,
                out bool exists);
            DWriteUtil.ConvertHresultToException(hr);

            if (!exists || pData == null || tableSize < 10)
            {
                if (tableContext != null)
                    IDWriteFontFaceVtbl.ReleaseFontTable(p, tableContext);
                fsType = 0;
                return false;
            }

            try
            {
                byte* os2 = (byte*)pData;
                // OpenType OS/2 table field fsType sits at big-endian byte offset 8..9.
                fsType = (ushort)((os2[8] << 8) | os2[9]);
                return true;
            }
            finally
            {
                if (tableContext != null)
                    IDWriteFontFaceVtbl.ReleaseFontTable(p, tableContext);
            }
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using CSUI.Core.Interop.DirectX;

namespace MS.Internal.Text.TextInterface
{
    internal sealed unsafe class FontCollection : IDisposable
    {
        private nint _fontCollection;

        internal FontCollection(void* fontCollection)
        {
            _fontCollection = (nint)fontCollection;
        }

        ~FontCollection()
        {
            Dispose(false);
        }

        internal void* DWriteFontCollectionNoAddRef => (void*)_fontCollection;

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            ComHelpers.ReleaseAtomic(ref _fontCollection);
        }

        private void* RequirePointer()
        {
            void* p = (void*)_fontCollection;
            if (p == null)
                throw new ObjectDisposedException(nameof(FontCollection));
            return p;
        }

        internal uint FamilyCount => IDWriteFontCollectionVtbl.GetFontFamilyCount(RequirePointer());

        internal FontFamily this[uint familyIndex]
        {
            get
            {
                void* pFamily = null;
                int hr = IDWriteFontCollectionVtbl.GetFontFamily(RequirePointer(), familyIndex, &pFamily);
                DWriteUtil.ConvertHresultToException(hr);
                return new FontFamily(pFamily);
            }
        }

        internal FontFamily? this[string familyName]
        {
            get
            {
                if (FindFamilyName(familyName, out uint index))
                {
                    return this[index];
                }
                return null;
            }
        }

        internal bool FindFamilyName(string familyName, out uint index)
        {
            if (string.IsNullOrEmpty(familyName))
            {
                index = 0;
                return false;
            }

            int hr = IDWriteFontCollectionVtbl.FindFamilyName(RequirePointer(), familyName, out index, out bool exists);
            DWriteUtil.ConvertHresultToException(hr);
            return exists;
        }

        internal Font? GetFontFromFontFace(FontFace fontFace)
        {
            ArgumentNullException.ThrowIfNull(fontFace);
            void* pFont = null;
            int hr = IDWriteFontCollectionExVtbl.GetFontFromFontFace(
                RequirePointer(),
                fontFace.DWriteFontFaceNoAddRef,
                &pFont);
            DWriteUtil.ConvertHresultToException(hr);
            return pFont == null ? null : new Font(pFont);
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using CSUI.Core.Interop.DirectX;

namespace MS.Internal.Text.TextInterface
{
    internal sealed unsafe class FontFileLoader : IDWriteFontFileLoaderManaged
    {
        private const int S_OK = 0;
        private const int E_INVALIDARG = unchecked((int)0x80070057L);

        private readonly IFontSourceFactory _fontSourceFactory;

        internal FontFileLoader(IFontSourceFactory fontSourceFactory)
        {
            ArgumentNullException.ThrowIfNull(fontSourceFactory);
            _fontSourceFactory = fontSourceFactory;
        }

        public int CreateStreamFromKey(void* fontFileReferenceKey, uint fontFileReferenceKeySize, void** fontFileStream)
        {
            if (fontFileStream == null)
                return E_INVALIDARG;

            uint numberOfCharacters = fontFileReferenceKeySize / sizeof(char);
            if (fontFileReferenceKey == null
                || (fontFileReferenceKeySize % sizeof(char) != 0)
                || numberOfCharacters <= 1
                || ((char*)fontFileReferenceKey)[numberOfCharacters - 1] != '\0')
            {
                return E_INVALIDARG;
            }

            *fontFileStream = null;
            string uriString = new string((char*)fontFileReferenceKey);

            try
            {
                IFontSource fontSource = _fontSourceFactory.Create(uriString);
                FontFileStream stream = new FontFileStream(fontSource);
                *fontFileStream = DWriteFontFileStreamCCW.Create(stream);
                return S_OK;
            }
            catch (Exception ex)
            {
                return Marshal.GetHRForException(ex);
            }
        }
    }
}

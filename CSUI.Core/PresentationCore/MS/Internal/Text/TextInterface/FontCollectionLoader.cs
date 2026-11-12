// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using CSUI.Core.Interop.DirectX;

namespace MS.Internal.Text.TextInterface
{
    internal sealed unsafe class FontCollectionLoader : IDWriteFontCollectionLoaderManaged
    {
        private const int S_OK = 0;
        private const int E_INVALIDARG = unchecked((int)0x80070057L);

        private readonly IFontSourceCollectionFactory _fontSourceCollectionFactory;
        private readonly void* _fontFileLoaderCCW;

        internal FontCollectionLoader(IFontSourceCollectionFactory fontSourceCollectionFactory, void* fontFileLoaderCCW)
        {
            ArgumentNullException.ThrowIfNull(fontSourceCollectionFactory);
            _fontSourceCollectionFactory = fontSourceCollectionFactory;
            _fontFileLoaderCCW = fontFileLoaderCCW;
        }

        public int CreateEnumeratorFromKey(void* factory, void* collectionKey, uint collectionKeySize, void** fontFileEnumerator)
        {
            uint numberOfCharacters = collectionKeySize / sizeof(char);
            if (fontFileEnumerator == null
                || collectionKey == null
                || (collectionKeySize % sizeof(char) != 0)
                || numberOfCharacters <= 1
                || ((char*)collectionKey)[numberOfCharacters - 1] != '\0')
            {
                return E_INVALIDARG;
            }

            *fontFileEnumerator = null;
            string uriString = new string((char*)collectionKey);

            try
            {
                IFontSourceCollection fontSourceCollection = _fontSourceCollectionFactory.Create(uriString);
                FontFileEnumerator enumerator = new FontFileEnumerator(
                    fontSourceCollection,
                    _fontFileLoaderCCW,
                    factory);

                *fontFileEnumerator = DWriteFontFileEnumeratorCCW.Create(enumerator);
                return S_OK;
            }
            catch (Exception ex)
            {
                return Marshal.GetHRForException(ex);
            }
        }
    }
}

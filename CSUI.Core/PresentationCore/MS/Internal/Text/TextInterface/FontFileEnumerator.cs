// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using CSUI.Core.Interop.DirectX;

namespace MS.Internal.Text.TextInterface
{
    internal sealed unsafe class FontFileEnumerator : IDWriteFontFileEnumeratorManaged, IDisposable
    {
        private const int S_OK = 0;
        private const int E_INVALIDARG = unchecked((int)0x80070057L);

        private readonly IEnumerator<IFontSource> _fontSourceCollectionEnumerator;
        private readonly void* _fontFileLoaderCCW;
        private nint _factory;

        private enum EnumeratorState
        {
            NotStarted,
            Valid,
            Ended
        }

        private EnumeratorState _state = EnumeratorState.NotStarted;

        internal FontFileEnumerator(
            IEnumerable<IFontSource> fontSourceCollection,
            void* fontFileLoaderCCW,
            void* factory)
        {
            ArgumentNullException.ThrowIfNull(fontSourceCollection);
            _fontSourceCollectionEnumerator = fontSourceCollection.GetEnumerator();
            _fontFileLoaderCCW = fontFileLoaderCCW;
            _factory = (nint)factory;
            if (factory != null)
                ComHelpers.AddRef(factory);
        }

        ~FontFileEnumerator()
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
            if (disposing)
            {
                _fontSourceCollectionEnumerator.Dispose();
            }

            ComHelpers.ReleaseAtomic(ref _factory);
        }

        public int MoveNext(int* hasCurrentFile)
        {
            if (hasCurrentFile == null)
                return E_INVALIDARG;

            try
            {
                bool hasNext = _fontSourceCollectionEnumerator.MoveNext();
                _state = hasNext ? EnumeratorState.Valid : EnumeratorState.Ended;
                *hasCurrentFile = hasNext ? 1 : 0;
                return S_OK;
            }
            catch (Exception ex)
            {
                return Marshal.GetHRForException(ex);
            }
        }

        public int GetCurrentFontFile(void** fontFile)
        {
            if (fontFile == null)
                return E_INVALIDARG;

            *fontFile = null;

            if (_state == EnumeratorState.NotStarted)
                throw new InvalidOperationException(LocalizedErrorMsgs.EnumeratorNotStarted);
            if (_state == EnumeratorState.Ended)
                throw new InvalidOperationException(LocalizedErrorMsgs.EnumeratorReachedEnd);

            try
            {
                IFontSource current = _fontSourceCollectionEnumerator.Current;
                return Factory.CreateFontFileCore((void*)_factory, _fontFileLoaderCCW, current.Uri, fontFile);
            }
            catch (Exception ex)
            {
                return Marshal.GetHRForException(ex);
            }
        }
    }
}

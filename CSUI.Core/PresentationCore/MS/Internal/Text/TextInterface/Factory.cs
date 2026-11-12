// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using CSUI.Core.Interop.DirectX;

namespace MS.Internal.Text.TextInterface
{
    internal sealed unsafe class Factory : IDisposable
    {
        private const int DWRITE_E_FILEFORMAT = unchecked((int)0x88985000L);

        private nint _factory;
        private readonly IFontSourceFactory _fontSourceFactory;
        private readonly FontFileLoader _wpfFontFileLoader;
        private readonly FontCollectionLoader _wpfFontCollectionLoader;
        private nint _pFontFileLoaderCCW;
        private nint _pFontCollectionLoaderCCW;

        private Factory(
            FactoryType factoryType,
            IFontSourceCollectionFactory fontSourceCollectionFactory,
            IFontSourceFactory fontSourceFactory)
        {
            ArgumentNullException.ThrowIfNull(fontSourceCollectionFactory);
            ArgumentNullException.ThrowIfNull(fontSourceFactory);

            _fontSourceFactory = fontSourceFactory;

            void* pFactory = null;
            int hr = DxExports.DWriteCreateFactory(
                (uint)factoryType,
                DxWriteFontGuids.IID_IDWriteFactory,
                &pFactory);
            DWriteUtil.ConvertHresultToException(hr);
            _factory = (nint)pFactory;

            _wpfFontFileLoader = new FontFileLoader(fontSourceFactory);
            void* pFileLoaderCCW = DWriteFontFileLoaderCCW.Create(_wpfFontFileLoader);
            _pFontFileLoaderCCW = (nint)pFileLoaderCCW;

            _wpfFontCollectionLoader = new FontCollectionLoader(fontSourceCollectionFactory, pFileLoaderCCW);
            void* pCollLoaderCCW = DWriteFontCollectionLoaderCCW.Create(_wpfFontCollectionLoader);
            _pFontCollectionLoaderCCW = (nint)pCollLoaderCCW;

            hr = IDWriteFactoryWriteVtbl.RegisterFontFileLoader(pFactory, pFileLoaderCCW);
            DWriteUtil.ConvertHresultToException(hr);

            hr = IDWriteFactoryFontVtbl.RegisterFontCollectionLoader(pFactory, pCollLoaderCCW);
            DWriteUtil.ConvertHresultToException(hr);
        }

        ~Factory()
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
            nint factoryPtr = System.Threading.Interlocked.Exchange(ref _factory, IntPtr.Zero);
            if (factoryPtr != IntPtr.Zero)
            {
                void* pFactory = (void*)factoryPtr;
                nint collPtr = System.Threading.Interlocked.Exchange(ref _pFontCollectionLoaderCCW, IntPtr.Zero);
                if (collPtr != IntPtr.Zero)
                {
                    IDWriteFactoryFontVtbl.UnregisterFontCollectionLoader(pFactory, (void*)collPtr);
                    ComHelpers.Release((void*)collPtr);
                }

                nint filePtr = System.Threading.Interlocked.Exchange(ref _pFontFileLoaderCCW, IntPtr.Zero);
                if (filePtr != IntPtr.Zero)
                {
                    IDWriteFactoryWriteVtbl.UnregisterFontFileLoader(pFactory, (void*)filePtr);
                    ComHelpers.Release((void*)filePtr);
                }

                ComHelpers.Release(pFactory);
            }
        }

        internal void* DWriteFactory => (void*)_factory;

        internal static Factory Create(
            FactoryType factoryType,
            IFontSourceCollectionFactory fontSourceCollectionFactory,
            IFontSourceFactory fontSourceFactory)
        {
            return new Factory(factoryType, fontSourceCollectionFactory, fontSourceFactory);
        }

        internal static int CreateFontFileCore(
            void* factory,
            void* fontFileLoaderCCW,
            Uri filePathUri,
            void** dwriteFontFile)
        {
            if (IsLocalUri(filePathUri))
            {
                string localPath = filePathUri.LocalPath;
                return IDWriteFactoryWriteVtbl.CreateFontFileReference(
                    factory,
                    localPath,
                    null,
                    dwriteFontFile);
            }
            else
            {
                string uriString = filePathUri.AbsoluteUri;
                fixed (char* pUri = uriString)
                {
                    uint keySize = (uint)((uriString.Length + 1) * sizeof(char));
                    return IDWriteFactoryWriteVtbl.CreateCustomFontFileReference(
                        factory,
                        pUri,
                        keySize,
                        fontFileLoaderCCW,
                        dwriteFontFile);
                }
            }
        }

        internal FontFile CreateFontFile(Uri filePathUri)
        {
            ArgumentNullException.ThrowIfNull(filePathUri);
            void* pFactory = DWriteFactory;
            if (pFactory == null)
                throw new ObjectDisposedException(nameof(Factory));

            void* dwriteFontFile = null;
            int hr = CreateFontFileCore(pFactory, (void*)_pFontFileLoaderCCW, filePathUri, &dwriteFontFile);

            if (hr != 0)
            {
                IFontSource fontSource = _fontSourceFactory.Create(filePathUri.AbsoluteUri);
                fontSource.TestFileOpenable();
            }

            GC.KeepAlive(this);
            DWriteUtil.ConvertHresultToException(hr);
            return new FontFile(dwriteFontFile);
        }

        internal FontFace CreateFontFace(Uri filePathUri, uint faceIndex)
            => CreateFontFace(filePathUri, faceIndex, FontSimulations.None);

        internal FontFace CreateFontFace(Uri filePathUri, uint faceIndex, FontSimulations fontSimulationFlags)
        {
            using FontFile fontFile = CreateFontFile(filePathUri);
            void* pFactory = DWriteFactory;
            if (pFactory == null)
                throw new ObjectDisposedException(nameof(Factory));

            int hr;
            if (fontFile.Analyze(
                out FontFileType _,
                out FontFaceType dwriteFontFaceType,
                out uint numberOfFaces,
                &hr))
            {
                ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(faceIndex, numberOfFaces);

                void* dwriteFontFace = null;
                void* dwriteFontFile = fontFile.DWriteFontFileNoAddRef;

                hr = IDWriteFactoryWriteVtbl.CreateFontFace(
                    pFactory,
                    (uint)dwriteFontFaceType,
                    1,
                    &dwriteFontFile,
                    faceIndex,
                    (uint)fontSimulationFlags,
                    &dwriteFontFace);

                GC.KeepAlive(fontFile);
                GC.KeepAlive(this);
                DWriteUtil.ConvertHresultToException(hr);
                return new FontFace(dwriteFontFace);
            }

            if (hr == DWRITE_E_FILEFORMAT)
            {
                IFontSource fontSource = _fontSourceFactory.Create(filePathUri.AbsoluteUri);
                fontSource.TestFileOpenable();
            }

            DWriteUtil.ConvertHresultToException(hr);
            throw new InvalidOperationException();
        }

        internal FontCollection GetSystemFontCollection()
            => GetSystemFontCollection(false);

        private FontCollection GetSystemFontCollection(bool checkForUpdates)
        {
            void* pFactory = DWriteFactory;
            if (pFactory == null)
                throw new ObjectDisposedException(nameof(Factory));

            void* fontCollection = null;
            int hr = IDWriteFactoryFontVtbl.GetSystemFontCollection(pFactory, &fontCollection, checkForUpdates);
            DWriteUtil.ConvertHresultToException(hr);
            return new FontCollection(fontCollection);
        }

        internal FontCollection GetFontCollection(Uri uri)
        {
            ArgumentNullException.ThrowIfNull(uri);
            void* pFactory = DWriteFactory;
            if (pFactory == null)
                throw new ObjectDisposedException(nameof(Factory));

            string uriString = uri.AbsoluteUri;
            void* dwriteFontCollection = null;
            int hr;

            fixed (char* uriStringPtr = uriString)
            {
                uint collectionKeySize = (uint)((uriString.Length + 1) * sizeof(char));
                hr = IDWriteFactoryFontVtbl.CreateCustomFontCollection(
                    pFactory,
                    (void*)_pFontCollectionLoaderCCW,
                    uriStringPtr,
                    collectionKeySize,
                    &dwriteFontCollection);
            }

            GC.KeepAlive(this);
            DWriteUtil.ConvertHresultToException(hr);
            return new FontCollection(dwriteFontCollection);
        }

        internal TextAnalyzer CreateTextAnalyzer()
        {
            void* pFactory = DWriteFactory;
            if (pFactory == null)
                throw new ObjectDisposedException(nameof(Factory));

            void* textAnalyzer = null;
            int hr = IDWriteFactoryWriteVtbl.CreateTextAnalyzer(pFactory, &textAnalyzer);
            DWriteUtil.ConvertHresultToException(hr);
            return new TextAnalyzer(textAnalyzer, pFactory);
        }

        internal static bool IsLocalUri(Uri uri)
        {
            return uri.IsFile && uri.IsLoopback && !uri.IsUnc;
        }
    }
}

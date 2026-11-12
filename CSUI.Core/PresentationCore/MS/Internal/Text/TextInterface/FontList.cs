// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using CSUI.Core.Interop.DirectX;

namespace MS.Internal.Text.TextInterface
{
    internal unsafe class FontList : IEnumerable<Font>, IDisposable
    {
        private nint _fontList;

        internal FontList(void* fontList)
        {
            _fontList = (nint)fontList;
        }

        ~FontList()
        {
            Dispose(false);
        }

        internal void* DWriteFontListNoAddRef => (void*)_fontList;

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            ComHelpers.ReleaseAtomic(ref _fontList);
        }

        protected void* RequirePointer()
        {
            void* p = (void*)_fontList;
            if (p == null)
                throw new ObjectDisposedException(GetType().Name);
            return p;
        }

        internal uint Count => IDWriteFontFamilyVtbl.GetFontCount(RequirePointer());

        internal Font this[uint index]
        {
            get
            {
                void* pFont = null;
                int hr = IDWriteFontFamilyVtbl.GetFont(RequirePointer(), index, &pFont);
                DWriteUtil.ConvertHresultToException(hr);
                return new Font(pFont);
            }
        }

        internal FontCollection FontCollection
        {
            get
            {
                void* pCollection = null;
                int hr = IDWriteFontFamilyExVtbl.GetFontCollection(RequirePointer(), &pCollection);
                DWriteUtil.ConvertHresultToException(hr);
                return new FontCollection(pCollection);
            }
        }

        public IEnumerator<Font> GetEnumerator() => new FontsEnumerator(this);
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        internal sealed class FontsEnumerator : IEnumerator<Font>
        {
            private readonly FontList _fontList;
            private long _currentIndex = -1;

            internal FontsEnumerator(FontList fontList)
            {
                _fontList = fontList;
            }

            public Font Current
            {
                get
                {
                    if (_currentIndex < 0)
                        throw new InvalidOperationException(LocalizedErrorMsgs.EnumeratorNotStarted);
                    if (_currentIndex >= _fontList.Count)
                        throw new InvalidOperationException(LocalizedErrorMsgs.EnumeratorReachedEnd);
                    return _fontList[checked((uint)_currentIndex)];
                }
            }

            object IEnumerator.Current => Current;

            public bool MoveNext()
            {
                long next = _currentIndex + 1;
                uint count = _fontList.Count;
                if (next >= count)
                {
                    _currentIndex = count;
                    return false;
                }
                _currentIndex = next;
                return true;
            }

            public void Reset() => _currentIndex = -1;
            public void Dispose() { }
        }
    }
}

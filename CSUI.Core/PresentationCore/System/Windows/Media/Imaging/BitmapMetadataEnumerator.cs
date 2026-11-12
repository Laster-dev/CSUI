// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using CSUI.Core.Interop.DirectX;
using MS.Internal;
using SR = MS.Internal.PresentationCore.SR;

namespace System.Windows.Media.Imaging
{
    /// <summary>
    /// Enumerates metadata query paths inside a <see cref="BitmapMetadata"/> container.
    /// </summary>
    internal sealed unsafe class BitmapMetadataEnumerator : IEnumerator<string>, IEnumerator
    {
        private const int S_FALSE = 1;

        object IEnumerator.Current => Current;

        public bool MoveNext()
        {
            nint strPtr = 0;
            int hr = IEnumStringVtbl.Next((void*)_enumeratorHandle.DangerousGetHandle(), 1, &strPtr, out uint fetched);
            try
            {
                if (hr == HRESULT.S_OK && fetched == 1)
                {
                    _current = Marshal.PtrToStringUni(strPtr);
                    _fStarted = true;
                    return true;
                }

                if (hr == S_FALSE || fetched == 0)
                {
                    _current = null;
                    _fStarted = true;
                    return false;
                }

                HRESULT.Check(hr);
                return false;
            }
            finally
            {
                if (strPtr != 0)
                {
                    Marshal.FreeCoTaskMem(strPtr);
                }
            }
        }

        public void Reset()
        {
            HRESULT.Check(IEnumStringVtbl.Reset((void*)_enumeratorHandle.DangerousGetHandle()));
            _current = null;
            _fStarted = false;
        }

        public string Current
        {
            get
            {
                if (_current == null)
                {
                    throw new InvalidOperationException(_fStarted ? SR.Enumerator_ReachedEnd : SR.Enumerator_NotStarted);
                }

                return _current;
            }
        }

        void IDisposable.Dispose()
        {
        }

        internal BitmapMetadataEnumerator(SafeMILHandle metadataHandle)
        {
            Debug.Assert(metadataHandle != null && !metadataHandle.IsInvalid);

            void* enumPtr = null;
            HRESULT.Check(IWICMetadataQueryReaderVtbl.GetEnumerator((void*)metadataHandle.DangerousGetHandle(), &enumPtr));
            _enumeratorHandle = new SafeMILHandle((IntPtr)enumPtr);
            _current = null;
            _fStarted = false;
        }

        private readonly SafeMILHandle _enumeratorHandle;
        private string _current;
        private bool _fStarted;
    }
}

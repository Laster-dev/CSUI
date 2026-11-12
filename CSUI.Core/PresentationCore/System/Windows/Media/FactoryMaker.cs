// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using CSUI.Core.Interop.DirectX;

namespace System.Windows.Media
{
    /// <summary>
    /// Provides a reference-counted shared IWICImagingFactory (and Direct2D shared device) handle without [DllImport].
    /// </summary>
    internal unsafe class FactoryMaker : IDisposable
    {
        private bool _disposed;
        private bool _fValidObject;

        internal FactoryMaker()
        {
            lock (s_factoryMakerLock)
            {
                s_cInstance++;
                _fValidObject = true;
            }
        }

        ~FactoryMaker()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
        }

        protected virtual void Dispose(bool fDisposing)
        {
            if (!_disposed)
            {
                if (_fValidObject)
                {
                    lock (s_factoryMakerLock)
                    {
                        s_cInstance--;
                        _fValidObject = false;

                        if (s_cInstance == 0)
                        {
                            if (s_pImagingFactory != IntPtr.Zero)
                            {
                                ComHelpers.Release((void*)s_pImagingFactory);
                                s_pImagingFactory = IntPtr.Zero;
                            }
                        }
                    }
                }

                _disposed = true;
                if (fDisposing)
                {
                    GC.SuppressFinalize(this);
                }
            }
        }

        internal IntPtr ImagingFactoryPtr
        {
            get
            {
                lock (s_factoryMakerLock)
                {
                    if (s_pImagingFactory == IntPtr.Zero)
                    {
                        using var factory = WicExports.CreateImagingFactory();
                        s_pImagingFactory = (IntPtr)factory.Detach();
                    }
                    return s_pImagingFactory;
                }
            }
        }

        private static IntPtr s_pImagingFactory;
        private static int s_cInstance;
        private static readonly object s_factoryMakerLock = new object();
    }
}

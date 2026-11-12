// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Ole;
using System.Windows.Threading;
using MS.Win32;
using SR = MS.Internal.PresentationCore.SR;

namespace System.Windows
{
    /// <summary>
    /// Thread-affine OLE services context initialized per STA UI Dispatcher thread.
    /// </summary>
    internal sealed unsafe class OleServicesContext
    {
        [ThreadStatic]
        private static OleServicesContext s_currentContext;

        private bool _oleInitialized;

        private OleServicesContext()
        {
            SetDispatcherThread();
        }

        internal static OleServicesContext CurrentOleServicesContext
        {
            get
            {
                OleServicesContext oleServicesContext = s_currentContext;
                if (oleServicesContext == null)
                {
                    oleServicesContext = new OleServicesContext();
                    s_currentContext = oleServicesContext;
                }

                return oleServicesContext;
            }
        }

        internal int OleSetClipboard(IDataObject dataObject)
        {
            if (dataObject == null)
            {
                return UnsafeNativeMethods.OleSetClipboard(null);
            }

            void* pDataObj = DataObject.CreateComDataObject(dataObject);
            try
            {
                return UnsafeNativeMethods.OleSetClipboard(pDataObj);
            }
            finally
            {
                OleComBridge.ReleaseCom(pDataObj);
            }
        }

        internal int OleGetClipboard(ref IDataObject dataObject)
        {
            void* pDataObj = null;
            int hr = UnsafeNativeMethods.OleGetClipboard(&pDataObj);
            if (hr >= 0 && pDataObj != null)
            {
                try
                {
                    if (OleComBridge.TryUnwrapManagedDataObject(pDataObj, out object unwrapped) &&
                        unwrapped is IDataObject managedDataObject)
                    {
                        dataObject = managedDataObject;
                    }
                    else
                    {
                        dataObject = new DataObject(pDataObj);
                    }
                }
                finally
                {
                    OleComBridge.ReleaseCom(pDataObj);
                }
            }

            return hr;
        }

        internal int OleFlushClipboard()
        {
            return UnsafeNativeMethods.OleFlushClipboard();
        }

        internal int OleIsCurrentClipboard(IDataObject dataObject)
        {
            if (dataObject == null || !OleComBridge.TryGetLiveDataObjectCom(dataObject, out void* pDataObj))
            {
                return OleComBridge.S_FALSE;
            }

            try
            {
                return UnsafeNativeMethods.OleIsCurrentClipboard(pDataObj);
            }
            finally
            {
                OleComBridge.ReleaseCom(pDataObj);
            }
        }

        internal int OleDoDragDrop(IDataObject dataObject, INativeDropSource dropSource, int allowedEffects, int[] finalEffect)
        {
            ArgumentNullException.ThrowIfNull(dataObject);
            ArgumentNullException.ThrowIfNull(dropSource);

            void* pDataObj = DataObject.CreateComDataObject(dataObject);
            void* pDropSource = OleComBridge.CreateDropSourceCom(dropSource);
            try
            {
                int hr = UnsafeNativeMethods.DoDragDrop(pDataObj, pDropSource, allowedEffects, out int effect);
                if (finalEffect != null && finalEffect.Length > 0)
                {
                    finalEffect[0] = effect;
                }
                return hr;
            }
            finally
            {
                OleComBridge.ReleaseCom(pDropSource);
                OleComBridge.ReleaseCom(pDataObj);
            }
        }

        internal int OleRegisterDragDrop(HandleRef windowHandle, INativeDropTarget dropTarget)
        {
            ArgumentNullException.ThrowIfNull(dropTarget);

            void* pDropTarget = OleComBridge.CreateDropTargetCom(dropTarget);
            try
            {
                return UnsafeNativeMethods.RegisterDragDrop(windowHandle, pDropTarget);
            }
            finally
            {
                OleComBridge.ReleaseCom(pDropTarget);
            }
        }

        internal int OleRevokeDragDrop(HandleRef windowHandle)
        {
            return UnsafeNativeMethods.RevokeDragDrop(windowHandle);
        }

        private void SetDispatcherThread()
        {
            if (Thread.CurrentThread.GetApartmentState() != ApartmentState.STA)
            {
                throw new ThreadStateException(SR.OleServicesContext_ThreadMustBeSTA);
            }

            int hr = OleInitialize();
            if (!NativeMethods.Succeeded(hr))
            {
                throw new SystemException(SR.Format(SR.OleServicesContext_oleInitializeFailure, hr));
            }

            _oleInitialized = true;
            Dispatcher.CurrentDispatcher.ShutdownFinished += OnDispatcherShutdown;
        }

        private void OnDispatcherShutdown(object sender, EventArgs args)
        {
            if (sender is Dispatcher dispatcher)
            {
                dispatcher.ShutdownFinished -= OnDispatcherShutdown;
            }

            if (_oleInitialized)
            {
                _oleInitialized = false;
                UnsafeNativeMethods.OleUninitialize();
            }

            s_currentContext = null;
        }

        private static int OleInitialize()
        {
            return UnsafeNativeMethods.OleInitialize();
        }
    }
}

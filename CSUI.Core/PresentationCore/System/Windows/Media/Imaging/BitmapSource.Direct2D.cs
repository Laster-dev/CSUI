// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System.Threading;
using MS.Internal;
using CSUI.Core.Interop.DirectX;

namespace System.Windows.Media.Imaging
{
    public abstract partial class BitmapSource
    {
        private readonly object _d2dCacheLock = new object();
        private SafeMILHandle _d2dBitmapCache;
        private long _cachedDeviceGen;
        private long _cachedContentVersion = -1;
        private long _d2dContentVersion;

        internal void InvalidateD2DSource()
        {
            Interlocked.Increment(ref _d2dContentVersion);
            lock (_d2dCacheLock)
            {
                _d2dBitmapCache?.Dispose();
                _d2dBitmapCache = null;
                _cachedDeviceGen = 0;
                _cachedContentVersion = -1;
            }
        }

        internal override unsafe void DrawD2D(void* d2dContext, Rect rectangle)
        {
            if (d2dContext == null || rectangle.IsEmpty || rectangle.Width <= 0.0 || rectangle.Height <= 0.0)
            {
                return;
            }

            void* d2dBitmap = AcquireD2DBitmap(d2dContext);
            if (d2dBitmap == null)
            {
                return;
            }

            try
            {
                D2D1_RECT_F destRect = Direct2DRenderWalker.ToD2DRect(rectangle);
                ID2D1DeviceContextExVtbl.DrawBitmap(
                    d2dContext,
                    d2dBitmap,
                    &destRect,
                    1.0f,
                    DxMediaConst.D2D1_INTERPOLATION_MODE_LINEAR,
                    null);
            }
            finally
            {
                ComHelpers.Release(d2dBitmap);
            }
        }

        internal unsafe void* AcquireD2DBitmap(void* d2dContext)
        {
            if (d2dContext == null)
            {
                return null;
            }

            CompleteDelayedCreation();

            if (PixelWidth <= 0 || PixelHeight <= 0)
            {
                return null;
            }

            DxSharedDevice sharedDevice = MediaSystem.EnsureSharedDevice();
            void* ctxDevice = null;
            ID2D1DeviceContextVtbl.GetDevice(d2dContext, &ctxDevice);
            bool isSharedDevice = ctxDevice != null && ctxDevice == sharedDevice.D2DDevice;
            if (ctxDevice != null)
            {
                ComHelpers.Release(ctxDevice);
            }

            long deviceGen = sharedDevice.DeviceGeneration;
            long contentVersion = Interlocked.Read(ref _d2dContentVersion);

            if (isSharedDevice)
            {
                lock (_d2dCacheLock)
                {
                    if (_d2dBitmapCache != null &&
                        !_d2dBitmapCache.IsInvalid &&
                        _cachedDeviceGen == deviceGen &&
                        _cachedContentVersion == contentVersion)
                    {
                        bool addRef = false;
                        _d2dBitmapCache.DangerousAddRef(ref addRef);
                        try
                        {
                            void* cachedPtr = (void*)_d2dBitmapCache.DangerousGetHandle();
                            if (cachedPtr != null)
                            {
                                ComHelpers.AddRef(cachedPtr);
                                return cachedPtr;
                            }
                        }
                        finally
                        {
                            if (addRef)
                            {
                                _d2dBitmapCache.DangerousRelease();
                            }
                        }
                    }
                }
            }

            BitmapSourceSafeMILHandle wicHandle = WicSourceHandle;
            if (wicHandle == null || wicHandle.IsInvalid)
            {
                return null;
            }

            SafeMILHandle convertedHandle = null;
            SafeMILHandle uploadHandle = wicHandle;

            try
            {
                PixelFormat fmt = PixelFormat.GetPixelFormat(wicHandle);
                if (fmt != PixelFormats.Pbgra32)
                {
                    Guid pbgraGuid = PixelFormats.Pbgra32.Guid;
                    bool addRefSrc = false;
                    wicHandle.DangerousAddRef(ref addRefSrc);
                    try
                    {
                        void* pConvertedWic = null;
                        int hrConvert = WicNativeExports.WICConvertBitmapSource(
                            ref pbgraGuid,
                            (void*)wicHandle.DangerousGetHandle(),
                            &pConvertedWic);
                        if (hrConvert < 0 || pConvertedWic == null)
                        {
                            return null;
                        }

                        convertedHandle = new SafeMILHandle((IntPtr)pConvertedWic);
                        uploadHandle = convertedHandle;
                    }
                    finally
                    {
                        if (addRefSrc)
                        {
                            wicHandle.DangerousRelease();
                        }
                    }
                }

                double rawDpiX = DpiX;
                double rawDpiY = DpiY;
                float dpiX = DoubleUtil.GreaterThan(rawDpiX, 0.0) && !double.IsInfinity(rawDpiX) && !double.IsNaN(rawDpiX)
                    ? (float)rawDpiX
                    : 96.0f;
                float dpiY = DoubleUtil.GreaterThan(rawDpiY, 0.0) && !double.IsInfinity(rawDpiY) && !double.IsNaN(rawDpiY)
                    ? (float)rawDpiY
                    : 96.0f;

                D2D1_BITMAP_PROPERTIES1 props = new D2D1_BITMAP_PROPERTIES1
                {
                    PixelFormat = new D2D1_PIXEL_FORMAT
                    {
                        Format = DxConst.DXGI_FORMAT_B8G8R8A8_UNORM,
                        AlphaMode = DxConst.D2D1_ALPHA_MODE_PREMULTIPLIED
                    },
                    DpiX = dpiX,
                    DpiY = dpiY,
                    BitmapOptions = 0,
                    ColorContext = null
                };

                void* d2dBitmap = null;
                bool addRefUpload = false;
                uploadHandle.DangerousAddRef(ref addRefUpload);
                try
                {
                    int hrCreate = ID2D1DeviceContextExVtbl.CreateBitmapFromWicBitmap(
                        d2dContext,
                        (void*)uploadHandle.DangerousGetHandle(),
                        &props,
                        &d2dBitmap);

                    if (hrCreate < 0 || d2dBitmap == null)
                    {
                        return null;
                    }
                }
                finally
                {
                    if (addRefUpload)
                    {
                        uploadHandle.DangerousRelease();
                    }
                }

                if (isSharedDevice)
                {
                    lock (_d2dCacheLock)
                    {
                        if (Interlocked.Read(ref _d2dContentVersion) == contentVersion &&
                            sharedDevice.DeviceGeneration == deviceGen)
                        {
                            _d2dBitmapCache?.Dispose();
                            _d2dBitmapCache = new SafeMILHandle((IntPtr)d2dBitmap);
                            _cachedDeviceGen = deviceGen;
                            _cachedContentVersion = contentVersion;
                            ComHelpers.AddRef(d2dBitmap);
                        }
                    }
                }

                return d2dBitmap;
            }
            finally
            {
                convertedHandle?.Dispose();
            }
        }
    }
}

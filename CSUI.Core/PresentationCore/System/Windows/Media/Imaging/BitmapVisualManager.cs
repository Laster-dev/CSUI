// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System.Diagnostics;
using System.Windows.Threading;
using CSUI.Core.Interop.DirectX;
using MS.Internal;
using MS.Internal.PresentationCore;

namespace System.Windows.Media.Imaging
{
    #region public class BitmapVisualManager

    /// <summary>
    /// BitmapVisualManager holds state and context for drawing a visual to a RenderTargetBitmap.
    /// </summary>
    internal sealed class BitmapVisualManager : DispatcherObject
    {
        private BitmapVisualManager()
        {
        }

        public BitmapVisualManager(RenderTargetBitmap bitmapTarget)
        {
            ArgumentNullException.ThrowIfNull(bitmapTarget);

            if (bitmapTarget.IsFrozen)
            {
                throw new ArgumentException(SR.Format(SR.Image_CantBeFrozen, null));
            }

            _bitmapTarget = bitmapTarget;
        }

        public void Render(Visual visual)
        {
            Render(visual, Matrix.Identity, Rect.Empty);
        }

        internal unsafe void Render(Visual visual, Matrix worldTransform, Rect windowClip)
        {
            ArgumentNullException.ThrowIfNull(visual);

            if (_bitmapTarget.IsFrozen)
            {
                throw new ArgumentException(SR.Image_CantBeFrozen);
            }

            int sizeX = _bitmapTarget.PixelWidth;
            int sizeY = _bitmapTarget.PixelHeight;
            double dpiX = _bitmapTarget.DpiX;
            double dpiY = _bitmapTarget.DpiY;

            Debug.Assert((sizeX > 0) && (sizeY > 0));
            Debug.Assert((dpiX > 0) && (dpiY > 0));

            if ((sizeX <= 0) || (sizeY <= 0))
            {
                return;
            }

            if ((dpiX <= 0) || (dpiY <= 0))
            {
                dpiX = 96.0;
                dpiY = 96.0;
            }

            DxSharedDevice sharedDevice = MediaSystem.EnsureSharedDevice();
            void* d2dCtx = null;
            void* targetBitmap = null;
            void* stagingBitmap = null;

            try
            {
                HRESULT.Check(ID2D1DeviceVtbl.CreateDeviceContext(
                    sharedDevice.D2DDevice,
                    DxConst.D2D1_DEVICE_CONTEXT_OPTIONS_NONE,
                    &d2dCtx));

                D2D1_BITMAP_PROPERTIES1 targetProps = new D2D1_BITMAP_PROPERTIES1
                {
                    PixelFormat = new D2D1_PIXEL_FORMAT
                    {
                        Format = DxConst.DXGI_FORMAT_B8G8R8A8_UNORM,
                        AlphaMode = DxConst.D2D1_ALPHA_MODE_PREMULTIPLIED
                    },
                    DpiX = (float)dpiX,
                    DpiY = (float)dpiY,
                    BitmapOptions = DxConst.D2D1_BITMAP_OPTIONS_TARGET,
                    ColorContext = null
                };

                D2D1_SIZE_U targetSize = new D2D1_SIZE_U((uint)sizeX, (uint)sizeY);
                HRESULT.Check(ID2D1DeviceContextExVtbl.CreateBitmap(
                    d2dCtx,
                    targetSize,
                    null,
                    0,
                    in targetProps,
                    &targetBitmap));

                lock (_bitmapTarget.SyncObject)
                {
                    if (!_bitmapTarget._isClear)
                    {
                        BitmapSourceSafeMILHandle wicBitmap = _bitmapTarget.WicSourceHandle;
                        WICRect rect = new WICRect(0, 0, sizeX, sizeY);
                        bool addRefWic = false;
                        wicBitmap.DangerousAddRef(ref addRefWic);
                        try
                        {
                            void* pLock = null;
                            HRESULT.Check(IWICBitmapVtbl.Lock(
                                (void*)wicBitmap.DangerousGetHandle(),
                                &rect,
                                (uint)LockFlags.MIL_LOCK_READ,
                                &pLock));
                            try
                            {
                                HRESULT.Check(IWICBitmapLockVtbl.GetStride(pLock, out uint wicStride));
                                HRESULT.Check(IWICBitmapLockVtbl.GetDataPointer(pLock, out uint wicSize, out byte* wicPtr));
                                if (wicPtr != null && wicSize > 0)
                                {
                                    HRESULT.Check(ID2D1BitmapVtbl.CopyFromMemory(
                                        targetBitmap,
                                        null,
                                        wicPtr,
                                        wicStride));
                                }
                            }
                            finally
                            {
                                ComHelpers.Release(pLock);
                            }
                        }
                        finally
                        {
                            if (addRefWic)
                            {
                                wicBitmap.DangerousRelease();
                            }
                        }
                    }

                    ID2D1DeviceContextVtbl.SetTarget(d2dCtx, targetBitmap);
                    ID2D1DeviceContextVtbl.SetDpi(d2dCtx, (float)dpiX, (float)dpiY);

                    ID2D1DeviceContextVtbl.BeginDraw(d2dCtx);
                    bool pushedClip = false;
                    int endDrawHr = HRESULT.S_OK;
                    try
                    {
                        if (_bitmapTarget._isClear)
                        {
                            ID2D1DeviceContextVtbl.Clear(d2dCtx, new D2D1_COLOR_F { R = 0f, G = 0f, B = 0f, A = 0f });
                        }

                        if (!windowClip.IsEmpty && windowClip.Width > 0.0 && windowClip.Height > 0.0)
                        {
                            ID2D1DeviceContextVtbl.PushAxisAlignedClip(
                                d2dCtx,
                                Direct2DRenderWalker.ToD2DRect(windowClip),
                                DxConst.D2D1_ANTIALIAS_MODE_PER_PRIMITIVE);
                            pushedClip = true;
                        }

                        ID2D1DeviceContextVtbl.SetTransform(
                            d2dCtx,
                            MILUtilities.ConvertToD2DMatrix(worldTransform));

                        RenderContext rc = new RenderContext();
                        rc.Initialize(d2dCtx);
                        visual.RenderRecursive(rc);
                    }
                    finally
                    {
                        if (pushedClip)
                        {
                            ID2D1DeviceContextVtbl.PopAxisAlignedClip(d2dCtx);
                        }

                        endDrawHr = ID2D1DeviceContextVtbl.EndDraw(d2dCtx);
                        ID2D1DeviceContextVtbl.SetTarget(d2dCtx, null);
                    }

                    HRESULT.Check(endDrawHr);

                    D2D1_BITMAP_PROPERTIES1 stagingProps = new D2D1_BITMAP_PROPERTIES1
                    {
                        PixelFormat = new D2D1_PIXEL_FORMAT
                        {
                            Format = DxConst.DXGI_FORMAT_B8G8R8A8_UNORM,
                            AlphaMode = DxConst.D2D1_ALPHA_MODE_PREMULTIPLIED
                        },
                        DpiX = (float)dpiX,
                        DpiY = (float)dpiY,
                        BitmapOptions = DxConst.D2D1_BITMAP_OPTIONS_CPU_READ | DxConst.D2D1_BITMAP_OPTIONS_CANNOT_DRAW,
                        ColorContext = null
                    };

                    HRESULT.Check(ID2D1DeviceContextExVtbl.CreateBitmap(
                        d2dCtx,
                        targetSize,
                        null,
                        0,
                        in stagingProps,
                        &stagingBitmap));

                    HRESULT.Check(ID2D1BitmapVtbl.CopyFromBitmap(
                        stagingBitmap,
                        null,
                        targetBitmap,
                        null));

                    HRESULT.Check(ID2D1Bitmap1Vtbl.Map(
                        stagingBitmap,
                        DxConst.D2D1_MAP_OPTIONS_READ,
                        out D2D1_MAPPED_RECT mapped));
                    try
                    {
                        BitmapSourceSafeMILHandle wicBitmap = _bitmapTarget.WicSourceHandle;
                        WICRect rect = new WICRect(0, 0, sizeX, sizeY);
                        bool addRefWic = false;
                        wicBitmap.DangerousAddRef(ref addRefWic);
                        try
                        {
                            void* pLock = null;
                            HRESULT.Check(IWICBitmapVtbl.Lock(
                                (void*)wicBitmap.DangerousGetHandle(),
                                &rect,
                                (uint)LockFlags.MIL_LOCK_WRITE,
                                &pLock));
                            try
                            {
                                HRESULT.Check(IWICBitmapLockVtbl.GetStride(pLock, out uint wicStride));
                                HRESULT.Check(IWICBitmapLockVtbl.GetDataPointer(pLock, out uint wicSize, out byte* wicPtr));

                                uint rowBytes = checked((uint)sizeX * 4u);
                                if (mapped.Bits != null &&
                                    wicPtr != null &&
                                    mapped.Pitch >= rowBytes &&
                                    wicStride >= rowBytes &&
                                    (ulong)wicSize >= (ulong)wicStride * (uint)(sizeY - 1) + rowBytes)
                                {
                                    for (int y = 0; y < sizeY; y++)
                                    {
                                        Buffer.MemoryCopy(
                                            mapped.Bits + (nint)y * mapped.Pitch,
                                            wicPtr + (nint)y * wicStride,
                                            rowBytes,
                                            rowBytes);
                                    }
                                }
                            }
                            finally
                            {
                                ComHelpers.Release(pLock);
                            }
                        }
                        finally
                        {
                            if (addRefWic)
                            {
                                wicBitmap.DangerousRelease();
                            }
                        }
                    }
                    finally
                    {
                        ID2D1Bitmap1Vtbl.Unmap(stagingBitmap);
                    }

                    _bitmapTarget._isClear = false;
                }
            }
            finally
            {
                ComHelpers.Release(stagingBitmap);
                ComHelpers.Release(targetBitmap);
                ComHelpers.Release(d2dCtx);
            }

            _bitmapTarget.RenderTargetContentsChanged();
        }

        private RenderTargetBitmap _bitmapTarget = null;
    }

    #endregion public class BitmapVisualManager
}

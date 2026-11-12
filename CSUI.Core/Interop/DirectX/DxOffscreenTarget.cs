// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace CSUI.Core.Interop.DirectX;

/// <summary>
/// Thread-safe reference-counted offscreen D3D11 texture / D2D1 target bitmap pair
/// produced by a worker CompositionTarget (such as VisualTarget) and consumed by HostVisual.
/// </summary>
internal sealed unsafe class DxOffscreenSurfaceSlot
{
    private readonly DxSharedDevice _shared;
    private int _refCount = 1; // Starts at 1 for the producer pool ownership.
    private ComPtr<byte> _texture;
    private ComPtr<byte> _targetBitmap;
    private uint _capacityWidth;
    private uint _capacityHeight;
    private long _deviceGeneration;

    public DxOffscreenSurfaceSlot(DxSharedDevice shared)
    {
        _shared = shared;
    }

    public DxSharedDevice SharedDevice => _shared;
    public void* TexturePtr => _texture.Ptr;
    public void* TargetBitmapPtr => _targetBitmap.Ptr;
    public uint ContentPixelWidth { get; private set; }
    public uint ContentPixelHeight { get; private set; }
    public float DpiX { get; private set; } = 96f;
    public float DpiY { get; private set; } = 96f;
    public double OriginDipX { get; private set; }
    public double OriginDipY { get; private set; }
    public long DeviceGeneration => _deviceGeneration;

    public bool IsIdleForProducer => Volatile.Read(ref _refCount) == 1;

    public void AddRef()
    {
        Interlocked.Increment(ref _refCount);
    }

    public void Release()
    {
        if (Interlocked.Decrement(ref _refCount) == 0)
        {
            _targetBitmap.Dispose();
            _texture.Dispose();
        }
    }

    public void EnsureAllocated(
        void* producerD2dContext,
        uint contentPixelWidth,
        uint contentPixelHeight,
        float dpiX,
        float dpiY,
        double originDipX,
        double originDipY)
    {
        uint targetWidth = Math.Clamp(contentPixelWidth, 1u, 16384u);
        uint targetHeight = Math.Clamp(contentPixelHeight, 1u, 16384u);
        long gen = _shared.DeviceGeneration;

        bool needTexture =
            _texture.IsNull ||
            _deviceGeneration != gen ||
            _capacityWidth < targetWidth ||
            _capacityHeight < targetHeight ||
            (_capacityWidth > targetWidth * 2 && _capacityWidth > 256) ||
            (_capacityHeight > targetHeight * 2 && _capacityHeight > 256);

        bool needBitmap = needTexture || _targetBitmap.IsNull || Math.Abs(DpiX - dpiX) > 0.01f || Math.Abs(DpiY - dpiY) > 0.01f;

        if (needTexture)
        {
            _targetBitmap.Dispose();
            _texture.Dispose();

            uint allocW = AlignSize(targetWidth);
            uint allocH = AlignSize(targetHeight);

            var desc = new D3D11_TEXTURE2D_DESC
            {
                Width = allocW,
                Height = allocH,
                MipLevels = 1,
                ArraySize = 1,
                Format = DxConst.DXGI_FORMAT_B8G8R8A8_UNORM,
                SampleDesc = new DXGI_SAMPLE_DESC { Count = 1, Quality = 0 },
                Usage = DxConst.D3D11_USAGE_DEFAULT,
                BindFlags = DxConst.D3D11_BIND_RENDER_TARGET | DxConst.D3D11_BIND_SHADER_RESOURCE,
                CPUAccessFlags = 0,
                MiscFlags = 0,
            };

            void* texPtr = null;
            HResult.ThrowIfFailed(
                ID3D11DeviceBaseVtbl.CreateTexture2D(_shared.D3DDevice, desc, null, &texPtr),
                "CreateTexture2D(Offscreen)");
            _texture.Attach((byte*)texPtr);
            _capacityWidth = allocW;
            _capacityHeight = allocH;
            _deviceGeneration = gen;
            needBitmap = true;
        }

        if (needBitmap)
        {
            _targetBitmap.Dispose();

            using var dxgiSurface = new ComPtr<byte>();
            HResult.ThrowIfFailed(
                ComPtr<byte>.QueryInterface(_texture.Ptr, DxGuids.IID_IDXGISurface, (void**)dxgiSurface.AddressOf()),
                "QueryInterface(IDXGISurface)");

            var props = new D2D1_BITMAP_PROPERTIES1
            {
                PixelFormat = new D2D1_PIXEL_FORMAT
                {
                    Format = DxConst.DXGI_FORMAT_B8G8R8A8_UNORM,
                    AlphaMode = DxConst.D2D1_ALPHA_MODE_PREMULTIPLIED,
                },
                DpiX = dpiX,
                DpiY = dpiY,
                BitmapOptions = DxConst.D2D1_BITMAP_OPTIONS_TARGET,
                ColorContext = null,
            };

            void* bmpPtr = null;
            HResult.ThrowIfFailed(
                ID2D1DeviceContextVtbl.CreateBitmapFromDxgiSurface(producerD2dContext, dxgiSurface.Ptr, props, &bmpPtr),
                "CreateBitmapFromDxgiSurface(OffscreenTarget)");
            _targetBitmap.Attach((byte*)bmpPtr);
        }

        ContentPixelWidth = targetWidth;
        ContentPixelHeight = targetHeight;
        DpiX = dpiX;
        DpiY = dpiY;
        OriginDipX = originDipX;
        OriginDipY = originDipY;
    }

    public bool TryCopyPixelsBgra8(out uint width, out uint height, out byte[] bgraPixels)
    {
        width = ContentPixelWidth;
        height = ContentPixelHeight;
        bgraPixels = Array.Empty<byte>();
        if (_texture.IsNull || width == 0 || height == 0 || _deviceGeneration != _shared.DeviceGeneration)
            return false;

        _shared.EnterGpuLock();
        try
        {
            var desc = new D3D11_TEXTURE2D_DESC
            {
                Width = _capacityWidth,
                Height = _capacityHeight,
                MipLevels = 1,
                ArraySize = 1,
                Format = DxConst.DXGI_FORMAT_B8G8R8A8_UNORM,
                SampleDesc = new DXGI_SAMPLE_DESC { Count = 1, Quality = 0 },
                Usage = DxConst.D3D11_USAGE_STAGING,
                BindFlags = 0,
                CPUAccessFlags = DxConst.D3D11_CPU_ACCESS_READ,
                MiscFlags = 0,
            };

            using var staging = new ComPtr<byte>();
            if (ID3D11DeviceBaseVtbl.CreateTexture2D(_shared.D3DDevice, desc, null, (void**)staging.AddressOf()) < 0)
                return false;

            ID3D11DeviceContextBaseVtbl.CopyResource(_shared.D3DContext, staging.Ptr, _texture.Ptr);
            ID3D11DeviceContextBaseVtbl.Flush(_shared.D3DContext);

            if (ID3D11DeviceContextBaseVtbl.Map(_shared.D3DContext, staging.Ptr, 0, DxConst.D3D11_MAP_READ, 0, out D3D11_MAPPED_SUBRESOURCE mapped) < 0)
                return false;

            try
            {
                uint rowBytes = width * 4;
                bgraPixels = new byte[rowBytes * height];
                byte* srcBase = (byte*)mapped.PData;
                fixed (byte* dstBase = bgraPixels)
                {
                    for (uint y = 0; y < height; y++)
                    {
                        Buffer.MemoryCopy(
                            srcBase + (nuint)y * mapped.RowPitch,
                            dstBase + (nuint)y * rowBytes,
                            rowBytes,
                            rowBytes);
                    }
                }
                return true;
            }
            finally
            {
                ID3D11DeviceContextBaseVtbl.Unmap(_shared.D3DContext, staging.Ptr, 0);
            }
        }
        finally
        {
            _shared.LeaveGpuLock();
        }
    }

    private static uint AlignSize(uint value)
    {
        if (value <= 64)
            return 64;
        uint aligned = ((value + 63u) / 64u) * 64u;
        return Math.Min(aligned, 16384u);
    }
}

/// <summary>
/// Manages an independent ID2D1DeviceContext and a triple-buffered ring of DXGI offscreen
/// surfaces for off-thread rendering targets such as VisualTarget.
/// </summary>
internal sealed unsafe class DxOffscreenRenderTarget : IDisposable
{
    private readonly DxSharedDevice _shared;
    private readonly DxOffscreenSurfaceSlot?[] _slots = new DxOffscreenSurfaceSlot?[3];
    private ComPtr<byte> _d2dContext;
    private long _contextGeneration = -1;
    private int _nextSlotIndex;
    private bool _disposed;

    public DxOffscreenRenderTarget(DxSharedDevice shared)
    {
        _shared = shared;
    }

    public DxSharedDevice SharedDevice => _shared;

    public void* EnsureContext()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        long gen = _shared.DeviceGeneration;
        if (_d2dContext.IsNull || _contextGeneration != gen)
        {
            ReleaseContextAndSlots();
            void* ctxPtr = null;
            HResult.ThrowIfFailed(_shared.CreateDeviceContext(out ctxPtr), "CreateDeviceContext(Offscreen)");
            _d2dContext.Attach((byte*)ctxPtr);
            _contextGeneration = gen;
        }

        return _d2dContext.Ptr;
    }

    public DxOffscreenSurfaceSlot? BeginRender(
        uint pixelWidth,
        uint pixelHeight,
        float dpiX,
        float dpiY,
        double originDipX,
        double originDipY,
        out void* d2dContext)
    {
        d2dContext = EnsureContext();

        DxOffscreenSurfaceSlot? chosen = null;
        for (int attempt = 0; attempt < _slots.Length; attempt++)
        {
            int idx = (_nextSlotIndex + attempt) % _slots.Length;
            var candidate = _slots[idx];
            if (candidate == null)
            {
                candidate = new DxOffscreenSurfaceSlot(_shared);
                _slots[idx] = candidate;
                chosen = candidate;
                _nextSlotIndex = (idx + 1) % _slots.Length;
                break;
            }

            if (candidate.IsIdleForProducer)
            {
                chosen = candidate;
                _nextSlotIndex = (idx + 1) % _slots.Length;
                break;
            }
        }

        if (chosen == null)
        {
            // Fallback if all 3 slots were concurrently retained during a transient burst.
            chosen = new DxOffscreenSurfaceSlot(_shared);
        }

        chosen.EnsureAllocated(d2dContext, pixelWidth, pixelHeight, dpiX, dpiY, originDipX, originDipY);

        ID2D1DeviceContextVtbl.SetTarget(d2dContext, chosen.TargetBitmapPtr);
        ID2D1DeviceContextVtbl.SetDpi(d2dContext, dpiX, dpiY);
        ID2D1DeviceContextVtbl.BeginDraw(d2dContext);
        ID2D1DeviceContextVtbl.SetTransform(d2dContext, D2D1_MATRIX_3X2_F.Identity);
        ID2D1DeviceContextVtbl.Clear(d2dContext, new D2D1_COLOR_F(0f, 0f, 0f, 0f));

        double dipW = pixelWidth * 96.0 / dpiX;
        double dipH = pixelHeight * 96.0 / dpiY;
        ID2D1DeviceContextVtbl.PushAxisAlignedClip(
            d2dContext,
            new D2D1_RECT_F(0f, 0f, (float)dipW, (float)dipH),
            DxConst.D2D1_ANTIALIAS_MODE_PER_PRIMITIVE);

        var translate = new D2D1_MATRIX_3X2_F
        {
            M11 = 1f,
            M22 = 1f,
            M31 = (float)(-originDipX),
            M32 = (float)(-originDipY),
        };
        ID2D1DeviceContextVtbl.SetTransform(d2dContext, translate);

        return chosen;
    }

    public bool EndRender(void* d2dContext)
    {
        if (d2dContext == null)
            return false;

        ID2D1DeviceContextVtbl.SetTransform(d2dContext, D2D1_MATRIX_3X2_F.Identity);
        ID2D1DeviceContextVtbl.PopAxisAlignedClip(d2dContext);
        int hr = ID2D1DeviceContextVtbl.EndDraw(d2dContext);
        ID2D1DeviceContextVtbl.SetTarget(d2dContext, null);

        if (hr == HResult.D2DERR_RECREATE_TARGET ||
            hr == HResult.DXGI_ERROR_DEVICE_REMOVED ||
            hr == HResult.DXGI_ERROR_DEVICE_RESET)
        {
            ReleaseContextAndSlots();
            return false;
        }

        HResult.ThrowIfFailed(hr, "Offscreen.EndDraw");
        return true;
    }

    private void ReleaseContextAndSlots()
    {
        if (!_d2dContext.IsNull)
        {
            ID2D1DeviceContextVtbl.SetTarget(_d2dContext.Ptr, null);
            _d2dContext.Dispose();
        }

        for (int i = 0; i < _slots.Length; i++)
        {
            if (_slots[i] != null)
            {
                _slots[i]!.Release();
                _slots[i] = null;
            }
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        ReleaseContextAndSlots();
    }
}

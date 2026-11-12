namespace CSUI.Core.Interop.DirectX;

/// <summary>
/// Shared Direct3D11 / Direct2D1 / DirectWrite device resources for the UI thread.
/// </summary>
internal sealed unsafe class DxSharedDevice : IDisposable
{
    private static readonly object s_sharedLock = new();
    private static DxSharedDevice? s_sharedInstance;

    public static DxSharedDevice GetOrCreateShared()
    {
        if (s_sharedInstance is { } existing)
            return existing;

        lock (s_sharedLock)
        {
            s_sharedInstance ??= new DxSharedDevice();
            return s_sharedInstance;
        }
    }

    private ComPtr<byte> _d3dDevice;
    private ComPtr<byte> _d3dContext;
    private ComPtr<byte> _d3dMultithread;
    private ComPtr<byte> _dxgiFactory;
    private ComPtr<byte> _d2dFactory;
    private ComPtr<byte> _d2dMultithread;
    private ComPtr<byte> _d2dDevice;
    private ComPtr<byte> _dwriteFactory;
    private long _deviceGeneration;

    public void* D3DDevice => _d3dDevice.Ptr;
    public void* D3DContext => _d3dContext.Ptr;
    public void* DxgiFactory => _dxgiFactory.Ptr;
    public void* D2DFactory => _d2dFactory.Ptr;
    public void* D2DDevice => _d2dDevice.Ptr;
    public void* DWriteFactory => _dwriteFactory.Ptr;
    public long DeviceGeneration => _deviceGeneration;

    public void EnterGpuLock()
    {
        if (!_d2dMultithread.IsNull)
            ID2D1MultithreadVtbl.Enter(_d2dMultithread.Ptr);
        if (!_d3dMultithread.IsNull)
            ID3D11MultithreadVtbl.Enter(_d3dMultithread.Ptr);
    }

    public void LeaveGpuLock()
    {
        if (!_d3dMultithread.IsNull)
            ID3D11MultithreadVtbl.Leave(_d3dMultithread.Ptr);
        if (!_d2dMultithread.IsNull)
            ID2D1MultithreadVtbl.Leave(_d2dMultithread.Ptr);
    }

    public int CreateDeviceContext(out void* d2dContext)
    {
        void* ctxPtr = null;
        int hr = ID2D1DeviceVtbl.CreateDeviceContext(_d2dDevice.Ptr, DxConst.D2D1_DEVICE_CONTEXT_OPTIONS_NONE, &ctxPtr);
        d2dContext = ctxPtr;
        return hr;
    }

    public DxSharedDevice()
    {
        Initialize();
    }

    public void Recreate()
    {
        Dispose();
        Initialize();
    }

    private void Initialize()
    {
        void* d3dDev = null;
        void* d3dCtx = null;
        int hr = DxExports.D3D11CreateDevice(
            DxConst.D3D_DRIVER_TYPE_HARDWARE,
            DxConst.D3D11_CREATE_DEVICE_BGRA_SUPPORT,
            &d3dDev,
            &d3dCtx);

        if (hr < 0)
        {
            hr = DxExports.D3D11CreateDevice(
                DxConst.D3D_DRIVER_TYPE_WARP,
                DxConst.D3D11_CREATE_DEVICE_BGRA_SUPPORT,
                &d3dDev,
                &d3dCtx);
            HResult.ThrowIfFailed(hr, "D3D11CreateDevice");
        }

        _d3dDevice.Attach((byte*)d3dDev);
        _d3dContext.Attach((byte*)d3dCtx);

        void* d3dMtPtr = null;
        if (ComPtr<byte>.QueryInterface(_d3dContext.Ptr, DxGuids.IID_ID3D11Multithread, &d3dMtPtr) >= 0 && d3dMtPtr != null)
        {
            _d3dMultithread.Attach((byte*)d3dMtPtr);
            ID3D11MultithreadVtbl.SetMultithreadProtected(_d3dMultithread.Ptr, 1);
        }

        using var dxgiDevice = new ComPtr<byte>();
        HResult.ThrowIfFailed(
            ComPtr<byte>.QueryInterface(_d3dDevice.Ptr, DxGuids.IID_IDXGIDevice, (void**)dxgiDevice.AddressOf()),
            "QI IDXGIDevice");

        using var dxgiAdapter = new ComPtr<byte>();
        HResult.ThrowIfFailed(
            IDXGIDeviceVtbl.GetAdapter(dxgiDevice.Ptr, (void**)dxgiAdapter.AddressOf()),
            "IDXGIDevice::GetAdapter");

        void* factoryPtr = null;
        HResult.ThrowIfFailed(
            IDXGIAdapterVtbl.GetParent(dxgiAdapter.Ptr, DxGuids.IID_IDXGIFactory2, &factoryPtr),
            "IDXGIAdapter::GetParent(IDXGIFactory2)");
        _dxgiFactory.Attach((byte*)factoryPtr);

        void* d2dFactoryPtr = null;
        var opts = new D2D1_FACTORY_OPTIONS { DebugLevel = 0 };
        HResult.ThrowIfFailed(
            DxExports.D2D1CreateFactory(DxConst.D2D1_FACTORY_TYPE_MULTI_THREADED, DxGuids.IID_ID2D1Factory1, opts, &d2dFactoryPtr),
            "D2D1CreateFactory");
        _d2dFactory.Attach((byte*)d2dFactoryPtr);

        void* d2dMtPtr = null;
        if (ComPtr<byte>.QueryInterface(_d2dFactory.Ptr, DxGuids.IID_ID2D1Multithread, &d2dMtPtr) >= 0 && d2dMtPtr != null)
        {
            _d2dMultithread.Attach((byte*)d2dMtPtr);
        }

        void* d2dDevicePtr = null;
        HResult.ThrowIfFailed(
            ID2D1Factory1Vtbl.CreateDevice(_d2dFactory.Ptr, dxgiDevice.Ptr, &d2dDevicePtr),
            "ID2D1Factory1::CreateDevice");
        _d2dDevice.Attach((byte*)d2dDevicePtr);

        void* dwFactoryPtr = null;
        HResult.ThrowIfFailed(
            DxExports.DWriteCreateFactory(DxConst.DWRITE_FACTORY_TYPE_SHARED, DxGuids.IID_IDWriteFactory, &dwFactoryPtr),
            "DWriteCreateFactory");
        _dwriteFactory.Attach((byte*)dwFactoryPtr);
        _deviceGeneration++;
    }

    public void Dispose()
    {
        _dwriteFactory.Dispose();
        _d2dDevice.Dispose();
        _d2dMultithread.Dispose();
        _d2dFactory.Dispose();
        _dxgiFactory.Dispose();
        _d3dMultithread.Dispose();
        _d3dContext.Dispose();
        _d3dDevice.Dispose();
    }
}

/// <summary>
/// Per-HWND DXGI SwapChain + Direct2D DeviceContext render surface.
/// </summary>
internal sealed unsafe class DxWindowRenderTarget : IDisposable
{
    private readonly DxSharedDevice _shared;
    private readonly nint _hwnd;
    private ComPtr<byte> _swapChain;
    private ComPtr<byte> _d2dContext;
    private ComPtr<byte> _targetBitmap;
    private uint _pixelWidth;
    private uint _pixelHeight;
    private float _dpi = 96f;

    public void* D2DFactory => _shared.D2DFactory;
    public void* D2DContext => _d2dContext.Ptr;
    public void* DWriteFactory => _shared.DWriteFactory;
    public float Dpi => _dpi;

    public DxWindowRenderTarget(DxSharedDevice shared, nint hwnd, uint pixelWidth, uint pixelHeight, float dpi)
    {
        _shared = shared;
        _hwnd = hwnd;
        _dpi = dpi > 0 ? dpi : 96f;
        CreateTarget(Math.Max(1u, pixelWidth), Math.Max(1u, pixelHeight));
    }

    private void CreateTarget(uint pixelWidth, uint pixelHeight)
    {
        _pixelWidth = pixelWidth;
        _pixelHeight = pixelHeight;

        void* ctxPtr = null;
        HResult.ThrowIfFailed(
            ID2D1DeviceVtbl.CreateDeviceContext(_shared.D2DDevice, DxConst.D2D1_DEVICE_CONTEXT_OPTIONS_NONE, &ctxPtr),
            "ID2D1Device::CreateDeviceContext");
        _d2dContext.Attach((byte*)ctxPtr);
        ID2D1DeviceContextVtbl.SetTextAntialiasMode(_d2dContext.Ptr, DxConst.D2D1_TEXT_ANTIALIAS_MODE_CLEARTYPE);

        var desc = new DXGI_SWAP_CHAIN_DESC1
        {
            Width = pixelWidth,
            Height = pixelHeight,
            Format = DxConst.DXGI_FORMAT_B8G8R8A8_UNORM,
            Stereo = 0,
            SampleDesc = new DXGI_SAMPLE_DESC { Count = 1, Quality = 0 },
            BufferUsage = DxConst.DXGI_USAGE_RENDER_TARGET_OUTPUT,
            BufferCount = 2,
            Scaling = DxConst.DXGI_SCALING_STRETCH,
            SwapEffect = DxConst.DXGI_SWAP_EFFECT_FLIP_SEQUENTIAL,
            AlphaMode = DxConst.DXGI_ALPHA_MODE_IGNORE,
            Flags = 0
        };

        void* scPtr = null;
        HResult.ThrowIfFailed(
            IDXGIFactory2Vtbl.CreateSwapChainForHwnd(_shared.DxgiFactory, _shared.D3DDevice, _hwnd, desc, null, null, &scPtr),
            "CreateSwapChainForHwnd");
        _swapChain.Attach((byte*)scPtr);

        // Disable Alt+Enter fullscreen toggle by default.
        IDXGIFactory2Vtbl.MakeWindowAssociation(_shared.DxgiFactory, _hwnd, 1);

        BindBackBuffer();
    }

    private void BindBackBuffer()
    {
        using var surface = new ComPtr<byte>();
        HResult.ThrowIfFailed(
            IDXGISwapChain1Vtbl.GetBuffer(_swapChain.Ptr, 0, DxGuids.IID_IDXGISurface, (void**)surface.AddressOf()),
            "IDXGISwapChain1::GetBuffer");

        var props = new D2D1_BITMAP_PROPERTIES1
        {
            PixelFormat = new D2D1_PIXEL_FORMAT
            {
                Format = DxConst.DXGI_FORMAT_B8G8R8A8_UNORM,
                AlphaMode = DxConst.D2D1_ALPHA_MODE_IGNORE
            },
            DpiX = _dpi,
            DpiY = _dpi,
            BitmapOptions = DxConst.D2D1_BITMAP_OPTIONS_TARGET | DxConst.D2D1_BITMAP_OPTIONS_CANNOT_DRAW,
            ColorContext = null
        };

        void* bmpPtr = null;
        HResult.ThrowIfFailed(
            ID2D1DeviceContextVtbl.CreateBitmapFromDxgiSurface(_d2dContext.Ptr, surface.Ptr, props, &bmpPtr),
            "CreateBitmapFromDxgiSurface");
        _targetBitmap.Attach((byte*)bmpPtr);

        ID2D1DeviceContextVtbl.SetTarget(_d2dContext.Ptr, _targetBitmap.Ptr);
        ID2D1DeviceContextVtbl.SetDpi(_d2dContext.Ptr, _dpi, _dpi);
    }

    public void Resize(uint pixelWidth, uint pixelHeight, float dpi)
    {
        pixelWidth = Math.Max(1u, pixelWidth);
        pixelHeight = Math.Max(1u, pixelHeight);
        _dpi = dpi > 0 ? dpi : _dpi;

        if (_swapChain.IsNull || _d2dContext.IsNull)
            return;

        if (_pixelWidth == pixelWidth && _pixelHeight == pixelHeight)
        {
            ID2D1DeviceContextVtbl.SetDpi(_d2dContext.Ptr, _dpi, _dpi);
            return;
        }

        _pixelWidth = pixelWidth;
        _pixelHeight = pixelHeight;

        ID2D1DeviceContextVtbl.SetTarget(_d2dContext.Ptr, null);
        _targetBitmap.Dispose();

        int hr = IDXGISwapChain1Vtbl.ResizeBuffers(_swapChain.Ptr, 0, pixelWidth, pixelHeight, DxConst.DXGI_FORMAT_UNKNOWN, 0);
        if (hr == HResult.DXGI_ERROR_DEVICE_REMOVED || hr == HResult.DXGI_ERROR_DEVICE_RESET)
        {
            RecreateAll();
            return;
        }
        HResult.ThrowIfFailed(hr, "ResizeBuffers");
        BindBackBuffer();
    }

    public void BeginDraw()
    {
        ID2D1DeviceContextVtbl.BeginDraw(_d2dContext.Ptr);
        ID2D1DeviceContextVtbl.SetTransform(_d2dContext.Ptr, D2D1_MATRIX_3X2_F.Identity);
    }

    public void EndDrawAndPresent()
    {
        EndDrawAndPresent(default, inResize: false);
    }

    public void EndDrawAndPresent(ReadOnlySpan<RECT> dirtyRects)
    {
        EndDrawAndPresent(dirtyRects, inResize: false);
    }

    public void EndDrawAndPresent(ReadOnlySpan<RECT> dirtyRects, bool inResize)
    {
        int hr = ID2D1DeviceContextVtbl.EndDraw(_d2dContext.Ptr);
        if (hr == HResult.D2DERR_RECREATE_TARGET || hr == HResult.DXGI_ERROR_DEVICE_REMOVED || hr == HResult.DXGI_ERROR_DEVICE_RESET)
        {
            RecreateAll();
            return;
        }
        HResult.ThrowIfFailed(hr, "EndDraw");

        uint syncInterval = inResize ? 0u : 1u;

        if (dirtyRects.Length > 0)
        {
            fixed (RECT* pRects = dirtyRects)
            {
                var presentParams = new DXGI_PRESENT_PARAMETERS
                {
                    DirtyRectsCount = (uint)dirtyRects.Length,
                    DirtyRects = pRects,
                    ScrollRect = null,
                    ScrollOffset = null
                };
                hr = IDXGISwapChain1Vtbl.Present1(_swapChain.Ptr, syncInterval, 0, &presentParams);
            }
        }
        else
        {
            hr = IDXGISwapChain1Vtbl.Present(_swapChain.Ptr, syncInterval, 0);
        }

        if (hr == HResult.DXGI_ERROR_DEVICE_REMOVED || hr == HResult.DXGI_ERROR_DEVICE_RESET)
        {
            RecreateAll();
            return;
        }
        HResult.ThrowIfFailed(hr, "Present");
    }

    private void RecreateAll()
    {
        Dispose();
        _shared.Recreate();
        CreateTarget(_pixelWidth, _pixelHeight);
    }

    public void Dispose()
    {
        if (!_d2dContext.IsNull)
        {
            ID2D1DeviceContextVtbl.SetTarget(_d2dContext.Ptr, null);
        }
        _targetBitmap.Dispose();
        _swapChain.Dispose();
        _d2dContext.Dispose();
    }
}

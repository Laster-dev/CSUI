namespace CSUI.Core.Interop.DirectX;

internal static unsafe class DxExports
{
    private static readonly nint D3D11 = NativeModule.Load("d3d11.dll");
    private static readonly nint D2D1 = NativeModule.Load("d2d1.dll");
    private static readonly nint DWrite = NativeModule.Load("dwrite.dll");
    private static readonly nint DComp = NativeModule.Load("dcomp.dll");

    private static readonly delegate* unmanaged[Stdcall]<void*, uint, nint, uint, uint*, uint, uint, void**, uint*, void**, int> _d3d11CreateDevice =
        (delegate* unmanaged[Stdcall]<void*, uint, nint, uint, uint*, uint, uint, void**, uint*, void**, int>)
        NativeModule.GetExport(D3D11, "D3D11CreateDevice");

    private static readonly delegate* unmanaged[Stdcall]<uint, Guid*, D2D1_FACTORY_OPTIONS*, void**, int> _d2d1CreateFactory =
        (delegate* unmanaged[Stdcall]<uint, Guid*, D2D1_FACTORY_OPTIONS*, void**, int>)
        NativeModule.GetExport(D2D1, "D2D1CreateFactory");

    private static readonly delegate* unmanaged[Stdcall]<uint, Guid*, void**, int> _dwriteCreateFactory =
        (delegate* unmanaged[Stdcall]<uint, Guid*, void**, int>)
        NativeModule.GetExport(DWrite, "DWriteCreateFactory");

    private static readonly delegate* unmanaged[Stdcall]<void*, Guid*, void**, int> _dcompCreateDevice =
        DComp != 0
            ? (delegate* unmanaged[Stdcall]<void*, Guid*, void**, int>)NativeModule.GetExport(DComp, "DCompositionCreateDevice")
            : null;

    public static int D3D11CreateDevice(
        uint driverType,
        uint flags,
        void** ppDevice,
        void** ppImmediateContext)
        => _d3d11CreateDevice(null, driverType, 0, flags, null, 0, DxConst.D3D11_SDK_VERSION, ppDevice, null, ppImmediateContext);

    public static int D2D1CreateFactory(uint factoryType, in Guid riid, in D2D1_FACTORY_OPTIONS options, void** ppFactory)
    {
        fixed (Guid* pIid = &riid)
        fixed (D2D1_FACTORY_OPTIONS* pOpt = &options)
            return _d2d1CreateFactory(factoryType, pIid, pOpt, ppFactory);
    }

    public static int DWriteCreateFactory(uint factoryType, in Guid iid, void** ppFactory)
    {
        fixed (Guid* pIid = &iid)
            return _dwriteCreateFactory(factoryType, pIid, ppFactory);
    }

    public static int DCompositionCreateDevice(void* dxgiDevice, in Guid iid, void** ppDevice)
    {
        if (_dcompCreateDevice == null) return -1;
        fixed (Guid* pIid = &iid)
            return _dcompCreateDevice(dxgiDevice, pIid, ppDevice);
    }
}

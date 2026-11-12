using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace CSUI.Core.Interop;

/// <summary>
/// Lightweight RAII wrapper around a raw COM interface pointer using direct vtable calls.
/// </summary>
internal unsafe struct ComPtr<T> : IDisposable where T : unmanaged
{
    private T* _ptr;

    public ComPtr(T* ptr)
    {
        _ptr = ptr;
    }

    public readonly T* Ptr
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _ptr;
    }

    public readonly bool IsNull
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _ptr == null;
    }

    public readonly T** AddressOf()
    {
        fixed (T** pp = &_ptr)
            return pp;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Attach(T* ptr)
    {
        Dispose();
        _ptr = ptr;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T* Detach()
    {
        T* tmp = _ptr;
        _ptr = null;
        return tmp;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AddRef()
    {
        if (_ptr != null)
        {
            ((delegate* unmanaged[Stdcall]<void*, uint>)(*(void***)_ptr)[1])(_ptr);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Dispose()
    {
        T* ptr = _ptr;
        if (ptr != null)
        {
            _ptr = null;
            ((delegate* unmanaged[Stdcall]<void*, uint>)(*(void***)ptr)[2])(ptr);
        }
    }

    public readonly int QueryInterface<TTarget>(in Guid iid, out ComPtr<TTarget> target)
        where TTarget : unmanaged
    {
        TTarget* raw = null;
        int hr = QueryInterface(_ptr, in iid, (void**)&raw);
        target = new ComPtr<TTarget>(raw);
        return hr;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int QueryInterface(void* unknown, in Guid iid, void** ppvObject)
    {
        fixed (Guid* pIid = &iid)
        {
            return ((delegate* unmanaged[Stdcall]<void*, Guid*, void**, int>)(*(void***)unknown)[0])(unknown, pIid, ppvObject);
        }
    }
}

internal static class HResult
{
    public const int S_OK = 0;
    public const int DXGI_ERROR_DEVICE_REMOVED = unchecked((int)0x887A0005);
    public const int DXGI_ERROR_DEVICE_RESET = unchecked((int)0x887A0007);
    public const int D2DERR_RECREATE_TARGET = unchecked((int)0x8899000C);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfFailed(int hr, string operation = "COM call")
    {
        if (hr < 0)
        {
            Marshal.ThrowExceptionForHR(hr);
        }
    }
}

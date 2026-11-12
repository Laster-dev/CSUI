// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

namespace CSUI.Core.Interop.DirectX;

internal unsafe struct IDCompositionDeviceVtbl
{
    public static int Commit(void* self)
        => ((delegate* unmanaged[Stdcall]<void*, int>)(*(void***)self)[3])(self);

    public static int WaitForCommitCompletion(void* self)
        => ((delegate* unmanaged[Stdcall]<void*, int>)(*(void***)self)[4])(self);

    public static int CreateTargetForHwnd(void* self, nint hwnd, int topmost, void** target)
        => ((delegate* unmanaged[Stdcall]<void*, nint, int, void**, int>)(*(void***)self)[6])(self, hwnd, topmost, target);

    public static int CreateVisual(void* self, void** visual)
        => ((delegate* unmanaged[Stdcall]<void*, void**, int>)(*(void***)self)[7])(self, visual);
}

internal unsafe struct IDCompositionTargetVtbl
{
    public static int SetRoot(void* self, void* visual)
        => ((delegate* unmanaged[Stdcall]<void*, void*, int>)(*(void***)self)[3])(self, visual);
}

internal unsafe struct IDCompositionVisualVtbl
{
    public static int SetOffsetX(void* self, float offsetX)
        => ((delegate* unmanaged[Stdcall]<void*, float, int>)(*(void***)self)[3])(self, offsetX);

    public static int SetOffsetY(void* self, float offsetY)
        => ((delegate* unmanaged[Stdcall]<void*, float, int>)(*(void***)self)[5])(self, offsetY);

    public static int SetClip(void* self, in D2D1_RECT_F rect)
    {
        fixed (D2D1_RECT_F* p = &rect)
            return ((delegate* unmanaged[Stdcall]<void*, D2D1_RECT_F*, int>)(*(void***)self)[14])(self, p);
    }

    public static int SetContent(void* self, void* content)
        => ((delegate* unmanaged[Stdcall]<void*, void*, int>)(*(void***)self)[15])(self, content);

    public static int AddVisual(void* self, void* visual, int insertAbove, void* referenceVisual)
        => ((delegate* unmanaged[Stdcall]<void*, void*, int, void*, int>)(*(void***)self)[16])(self, visual, insertAbove, referenceVisual);

    public static int RemoveVisual(void* self, void* visual)
        => ((delegate* unmanaged[Stdcall]<void*, void*, int>)(*(void***)self)[17])(self, visual);

    public static int RemoveAllVisuals(void* self)
        => ((delegate* unmanaged[Stdcall]<void*, int>)(*(void***)self)[18])(self);
}

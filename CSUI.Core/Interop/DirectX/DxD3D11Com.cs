// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;

namespace CSUI.Core.Interop.DirectX;

internal unsafe struct ID3D11Device3DVtbl
{
    public static int CreateBuffer(void* self, in D3D11_BUFFER_DESC desc, D3D11_SUBRESOURCE_DATA* initialData, void** buffer)
    {
        fixed (D3D11_BUFFER_DESC* p = &desc)
            return ((delegate* unmanaged[Stdcall]<void*, D3D11_BUFFER_DESC*, D3D11_SUBRESOURCE_DATA*, void**, int>)(*(void***)self)[3])(
                self, p, initialData, buffer);
    }

    public static int CreateShaderResourceView(void* self, void* resource, D3D11_SHADER_RESOURCE_VIEW_DESC* desc, void** srv)
        => ((delegate* unmanaged[Stdcall]<void*, void*, D3D11_SHADER_RESOURCE_VIEW_DESC*, void**, int>)(*(void***)self)[7])(
            self, resource, desc, srv);

    public static int CreateRenderTargetView(void* self, void* resource, D3D11_RENDER_TARGET_VIEW_DESC* desc, void** rtv)
        => ((delegate* unmanaged[Stdcall]<void*, void*, D3D11_RENDER_TARGET_VIEW_DESC*, void**, int>)(*(void***)self)[9])(
            self, resource, desc, rtv);

    public static int CreateDepthStencilView(void* self, void* resource, D3D11_DEPTH_STENCIL_VIEW_DESC* desc, void** dsv)
        => ((delegate* unmanaged[Stdcall]<void*, void*, D3D11_DEPTH_STENCIL_VIEW_DESC*, void**, int>)(*(void***)self)[10])(
            self, resource, desc, dsv);

    public static int CreateInputLayout(void* self, D3D11_INPUT_ELEMENT_DESC* elems, uint numElements, void* shaderBytecode, nuint bytecodeLength, void** inputLayout)
        => ((delegate* unmanaged[Stdcall]<void*, D3D11_INPUT_ELEMENT_DESC*, uint, void*, nuint, void**, int>)(*(void***)self)[11])(
            self, elems, numElements, shaderBytecode, bytecodeLength, inputLayout);

    public static int CreateVertexShader(void* self, void* shaderBytecode, nuint bytecodeLength, void* classLinkage, void** vertexShader)
        => ((delegate* unmanaged[Stdcall]<void*, void*, nuint, void*, void**, int>)(*(void***)self)[12])(
            self, shaderBytecode, bytecodeLength, classLinkage, vertexShader);

    public static int CreatePixelShader(void* self, void* shaderBytecode, nuint bytecodeLength, void* classLinkage, void** pixelShader)
        => ((delegate* unmanaged[Stdcall]<void*, void*, nuint, void*, void**, int>)(*(void***)self)[15])(
            self, shaderBytecode, bytecodeLength, classLinkage, pixelShader);

    public static int CreateBlendState(void* self, in D3D11_BLEND_DESC desc, void** blendState)
    {
        fixed (D3D11_BLEND_DESC* p = &desc)
            return ((delegate* unmanaged[Stdcall]<void*, D3D11_BLEND_DESC*, void**, int>)(*(void***)self)[20])(
                self, p, blendState);
    }

    public static int CreateDepthStencilState(void* self, in D3D11_DEPTH_STENCIL_DESC desc, void** depthStencilState)
    {
        fixed (D3D11_DEPTH_STENCIL_DESC* p = &desc)
            return ((delegate* unmanaged[Stdcall]<void*, D3D11_DEPTH_STENCIL_DESC*, void**, int>)(*(void***)self)[21])(
                self, p, depthStencilState);
    }

    public static int CreateRasterizerState(void* self, in D3D11_RASTERIZER_DESC desc, void** rasterizerState)
    {
        fixed (D3D11_RASTERIZER_DESC* p = &desc)
            return ((delegate* unmanaged[Stdcall]<void*, D3D11_RASTERIZER_DESC*, void**, int>)(*(void***)self)[22])(
                self, p, rasterizerState);
    }

    public static int CreateSamplerState(void* self, in D3D11_SAMPLER_DESC desc, void** samplerState)
    {
        fixed (D3D11_SAMPLER_DESC* p = &desc)
            return ((delegate* unmanaged[Stdcall]<void*, D3D11_SAMPLER_DESC*, void**, int>)(*(void***)self)[23])(
                self, p, samplerState);
    }
}

internal unsafe struct ID3D11DeviceContext3DVtbl
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void VSSetConstantBuffers(void* self, uint startSlot, uint numBuffers, void** constantBuffers)
        => ((delegate* unmanaged[Stdcall]<void*, uint, uint, void**, void>)(*(void***)self)[7])(
            self, startSlot, numBuffers, constantBuffers);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void PSSetShaderResources(void* self, uint startSlot, uint numViews, void** shaderResourceViews)
        => ((delegate* unmanaged[Stdcall]<void*, uint, uint, void**, void>)(*(void***)self)[8])(
            self, startSlot, numViews, shaderResourceViews);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void PSSetShader(void* self, void* pixelShader, void** classInstances, uint numClassInstances)
        => ((delegate* unmanaged[Stdcall]<void*, void*, void**, uint, void>)(*(void***)self)[9])(
            self, pixelShader, classInstances, numClassInstances);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void PSSetSamplers(void* self, uint startSlot, uint numSamplers, void** samplers)
        => ((delegate* unmanaged[Stdcall]<void*, uint, uint, void**, void>)(*(void***)self)[10])(
            self, startSlot, numSamplers, samplers);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void VSSetShader(void* self, void* vertexShader, void** classInstances, uint numClassInstances)
        => ((delegate* unmanaged[Stdcall]<void*, void*, void**, uint, void>)(*(void***)self)[11])(
            self, vertexShader, classInstances, numClassInstances);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void DrawIndexed(void* self, uint indexCount, uint startIndexLocation, int baseVertexLocation)
        => ((delegate* unmanaged[Stdcall]<void*, uint, uint, int, void>)(*(void***)self)[12])(
            self, indexCount, startIndexLocation, baseVertexLocation);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Draw(void* self, uint vertexCount, uint startVertexLocation)
        => ((delegate* unmanaged[Stdcall]<void*, uint, uint, void>)(*(void***)self)[13])(
            self, vertexCount, startVertexLocation);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void PSSetConstantBuffers(void* self, uint startSlot, uint numBuffers, void** constantBuffers)
        => ((delegate* unmanaged[Stdcall]<void*, uint, uint, void**, void>)(*(void***)self)[16])(
            self, startSlot, numBuffers, constantBuffers);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void IASetInputLayout(void* self, void* inputLayout)
        => ((delegate* unmanaged[Stdcall]<void*, void*, void>)(*(void***)self)[17])(self, inputLayout);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void IASetVertexBuffers(void* self, uint startSlot, uint numBuffers, void** vertexBuffers, uint* strides, uint* offsets)
        => ((delegate* unmanaged[Stdcall]<void*, uint, uint, void**, uint*, uint*, void>)(*(void***)self)[18])(
            self, startSlot, numBuffers, vertexBuffers, strides, offsets);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void IASetIndexBuffer(void* self, void* indexBuffer, uint format, uint offset)
        => ((delegate* unmanaged[Stdcall]<void*, void*, uint, uint, void>)(*(void***)self)[19])(
            self, indexBuffer, format, offset);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void IASetPrimitiveTopology(void* self, uint topology)
        => ((delegate* unmanaged[Stdcall]<void*, uint, void>)(*(void***)self)[24])(self, topology);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void OMSetRenderTargets(void* self, uint numViews, void** renderTargetViews, void* depthStencilView)
        => ((delegate* unmanaged[Stdcall]<void*, uint, void**, void*, void>)(*(void***)self)[33])(
            self, numViews, renderTargetViews, depthStencilView);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void OMSetBlendState(void* self, void* blendState, float* blendFactor, uint sampleMask)
        => ((delegate* unmanaged[Stdcall]<void*, void*, float*, uint, void>)(*(void***)self)[35])(
            self, blendState, blendFactor, sampleMask);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void OMSetDepthStencilState(void* self, void* depthStencilState, uint stencilRef)
        => ((delegate* unmanaged[Stdcall]<void*, void*, uint, void>)(*(void***)self)[36])(
            self, depthStencilState, stencilRef);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void RSSetState(void* self, void* rasterizerState)
        => ((delegate* unmanaged[Stdcall]<void*, void*, void>)(*(void***)self)[43])(self, rasterizerState);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void RSSetViewports(void* self, uint numViewports, D3D11_VIEWPORT* viewports)
        => ((delegate* unmanaged[Stdcall]<void*, uint, D3D11_VIEWPORT*, void>)(*(void***)self)[44])(
            self, numViewports, viewports);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ClearRenderTargetView(void* self, void* renderTargetView, float* colorRgba)
        => ((delegate* unmanaged[Stdcall]<void*, void*, float*, void>)(*(void***)self)[50])(
            self, renderTargetView, colorRgba);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ClearDepthStencilView(void* self, void* depthStencilView, uint clearFlags, float depth, byte stencil)
        => ((delegate* unmanaged[Stdcall]<void*, void*, uint, float, byte, void>)(*(void***)self)[53])(
            self, depthStencilView, clearFlags, depth, stencil);
}

internal unsafe struct ID2D1RenderTargetFlushVtbl
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int Flush(void* self, ulong* tag1, ulong* tag2)
        => ((delegate* unmanaged[Stdcall]<void*, ulong*, ulong*, int>)(*(void***)self)[42])(self, tag1, tag2);
}

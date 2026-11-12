// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace CSUI.Core.Interop.DirectX;

internal static class DxD3D11Const
{
    public const uint D3D_PRIMITIVE_TOPOLOGY_TRIANGLELIST = 4;

    public const uint D3D11_INPUT_PER_VERTEX_DATA = 0;
    public const uint D3D11_APPEND_ALIGNED_ELEMENT = 0xffffffffu;

    public const uint D3D11_FILL_SOLID = 3;
    public const uint D3D11_CULL_NONE = 1;
    public const uint D3D11_CULL_FRONT = 2;
    public const uint D3D11_CULL_BACK = 3;

    public const uint D3D11_DEPTH_WRITE_MASK_ZERO = 0;
    public const uint D3D11_DEPTH_WRITE_MASK_ALL = 1;

    public const uint D3D11_COMPARISON_LESS_EQUAL = 4;
    public const uint D3D11_COMPARISON_ALWAYS = 8;

    public const uint D3D11_STENCIL_OP_KEEP = 1;

    public const uint D3D11_BLEND_ONE = 2;
    public const uint D3D11_BLEND_INV_SRC_ALPHA = 6;
    public const uint D3D11_BLEND_OP_ADD = 1;
    public const byte D3D11_COLOR_WRITE_ENABLE_ALL = 0x0F;

    public const uint D3D11_FILTER_MIN_MAG_MIP_LINEAR = 0x15;
    public const uint D3D11_TEXTURE_ADDRESS_WRAP = 1;
    public const uint D3D11_TEXTURE_ADDRESS_MIRROR = 2;
    public const uint D3D11_TEXTURE_ADDRESS_CLAMP = 3;

    public const uint D3D11_SRV_DIMENSION_TEXTURE2D = 4;
    public const uint D3D11_RTV_DIMENSION_TEXTURE2D = 4;
    public const uint D3D11_DSV_DIMENSION_TEXTURE2D = 3;

    public const uint D3D11_CLEAR_DEPTH = 0x1;
    public const uint D3D11_CLEAR_STENCIL = 0x2;
}

[StructLayout(LayoutKind.Sequential)]
internal struct D3D11_BUFFER_DESC
{
    public uint ByteWidth;
    public uint Usage;
    public uint BindFlags;
    public uint CPUAccessFlags;
    public uint MiscFlags;
    public uint StructureByteStride;
}

[StructLayout(LayoutKind.Sequential)]
internal unsafe struct D3D11_INPUT_ELEMENT_DESC
{
    public byte* SemanticName;
    public uint SemanticIndex;
    public uint Format;
    public uint InputSlot;
    public uint AlignedByteOffset;
    public uint InputSlotClass;
    public uint InstanceDataStepRate;
}

[StructLayout(LayoutKind.Sequential)]
internal struct D3D11_RASTERIZER_DESC
{
    public uint FillMode;
    public uint CullMode;
    public int FrontCounterClockwise;
    public int DepthBias;
    public float DepthBiasClamp;
    public float SlopeScaledDepthBias;
    public int DepthClipEnable;
    public int ScissorEnable;
    public int MultisampleEnable;
    public int AntialiasedLineEnable;
}

[StructLayout(LayoutKind.Sequential)]
internal struct D3D11_DEPTH_STENCILOP_DESC
{
    public uint StencilFailOp;
    public uint StencilDepthFailOp;
    public uint StencilPassOp;
    public uint StencilFunc;
}

[StructLayout(LayoutKind.Sequential)]
internal struct D3D11_DEPTH_STENCIL_DESC
{
    public int DepthEnable;
    public uint DepthWriteMask;
    public uint DepthFunc;
    public int StencilEnable;
    public byte StencilReadMask;
    public byte StencilWriteMask;
    public D3D11_DEPTH_STENCILOP_DESC FrontFace;
    public D3D11_DEPTH_STENCILOP_DESC BackFace;
}

[StructLayout(LayoutKind.Sequential)]
internal struct D3D11_RENDER_TARGET_BLEND_DESC
{
    public int BlendEnable;
    public uint SrcBlend;
    public uint DestBlend;
    public uint BlendOp;
    public uint SrcBlendAlpha;
    public uint DestBlendAlpha;
    public uint BlendOpAlpha;
    public byte RenderTargetWriteMask;
}

[InlineArray(8)]
internal struct D3D11_RENDER_TARGET_BLEND_DESC_ARRAY
{
    private D3D11_RENDER_TARGET_BLEND_DESC _element0;
}

[StructLayout(LayoutKind.Sequential)]
internal struct D3D11_BLEND_DESC
{
    public int AlphaToCoverageEnable;
    public int IndependentBlendEnable;
    public D3D11_RENDER_TARGET_BLEND_DESC_ARRAY RenderTarget;
}

[InlineArray(4)]
internal struct D3D11_FLOAT4
{
    private float _element0;
}

[StructLayout(LayoutKind.Sequential)]
internal struct D3D11_SAMPLER_DESC
{
    public uint Filter;
    public uint AddressU;
    public uint AddressV;
    public uint AddressW;
    public float MipLODBias;
    public uint MaxAnisotropy;
    public uint ComparisonFunc;
    public D3D11_FLOAT4 BorderColor;
    public float MinLOD;
    public float MaxLOD;
}

[StructLayout(LayoutKind.Sequential)]
internal struct D3D11_VIEWPORT
{
    public float TopLeftX;
    public float TopLeftY;
    public float Width;
    public float Height;
    public float MinDepth;
    public float MaxDepth;
}

[StructLayout(LayoutKind.Sequential)]
internal struct D3D11_SHADER_RESOURCE_VIEW_DESC
{
    public uint Format;
    public uint ViewDimension;
    public uint MostDetailedMip;
    public uint MipLevels;
    public uint UnusedParam2;
    public uint UnusedParam3;
}

[StructLayout(LayoutKind.Sequential)]
internal struct D3D11_RENDER_TARGET_VIEW_DESC
{
    public uint Format;
    public uint ViewDimension;
    public uint MipSlice;
    public uint UnusedParam1;
    public uint UnusedParam2;
}

[StructLayout(LayoutKind.Sequential)]
internal struct D3D11_DEPTH_STENCIL_VIEW_DESC
{
    public uint Format;
    public uint ViewDimension;
    public uint Flags;
    public uint MipSlice;
    public uint UnusedParam1;
    public uint UnusedParam2;
}

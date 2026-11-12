// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using MS.Internal.Media3D;

namespace CSUI.Core.Interop.DirectX;

[StructLayout(LayoutKind.Sequential)]
internal struct DxVertex3D
{
    public float Px;
    public float Py;
    public float Pz;
    public float Nx;
    public float Ny;
    public float Nz;
    public float U;
    public float V;
}

[StructLayout(LayoutKind.Sequential)]
internal struct DxLightData
{
    public float ColorR;
    public float ColorG;
    public float ColorB;
    public float Kind; // 1=Ambient, 2=Directional, 3=Point, 4=Spot

    public float PosX;
    public float PosY;
    public float PosZ;
    public float Range;

    public float DirX;
    public float DirY;
    public float DirZ;
    public float CosHalfInner;

    public float ConstAtten;
    public float LinearAtten;
    public float QuadAtten;
    public float CosHalfOuter;
}

[InlineArray(32)]
internal struct DxLightData32
{
    private DxLightData _element0;
}

[StructLayout(LayoutKind.Sequential)]
internal struct DxPerObjectCB
{
    public float WorldM11, WorldM12, WorldM13, WorldM14;
    public float WorldM21, WorldM22, WorldM23, WorldM24;
    public float WorldM31, WorldM32, WorldM33, WorldM34;
    public float WorldM41, WorldM42, WorldM43, WorldM44;

    public float NormM11, NormM12, NormM13, NormM14;
    public float NormM21, NormM22, NormM23, NormM24;
    public float NormM31, NormM32, NormM33, NormM34;
    public float NormM41, NormM42, NormM43, NormM44;

    public float ViewProjM11, ViewProjM12, ViewProjM13, ViewProjM14;
    public float ViewProjM21, ViewProjM22, ViewProjM23, ViewProjM24;
    public float ViewProjM31, ViewProjM32, ViewProjM33, ViewProjM34;
    public float ViewProjM41, ViewProjM42, ViewProjM43, ViewProjM44;

    public float CameraPosX, CameraPosY, CameraPosZ, NormalSign;
    public float DiffuseR, DiffuseG, DiffuseB, DiffuseAlpha;
    public float AmbientR, AmbientG, AmbientB, UnusedAmbientA;
    public float SpecularR, SpecularG, SpecularB, SpecularPower;
    public float EmissiveR, EmissiveG, EmissiveB, UnusedEmissiveA;
    public float DiffMulAlpha, SpecMulAlpha, EmisMulAlpha, UnusedAlphasW;
    public float HasDiffuse, HasSpecular, HasEmissive, LightCount;

    public DxLightData32 Lights;
}

internal struct DxMaterialPassDesc
{
    public bool HasDiffuse;
    public Color DiffuseColor;
    public Color AmbientColor;
    public Brush? DiffuseBrush;

    public bool HasSpecular;
    public Color SpecularColor;
    public double SpecularPower;
    public Brush? SpecularBrush;

    public bool HasEmissive;
    public Color EmissiveColor;
    public Brush? EmissiveBrush;

    public readonly bool IsPotentiallyTranslucent
    {
        get
        {
            if (HasDiffuse)
            {
                if (DiffuseColor.A < 255)
                    return true;
                if (DiffuseBrush is SolidColorBrush scb)
                {
                    if (scb.Color.A < 255 || scb.Opacity < 0.999)
                        return true;
                }
                else if (DiffuseBrush != null)
                {
                    // Textured brushes (VisualBrush, ImageBrush, Gradients, Viewport2DVisual3D) may contain alpha.
                    return true;
                }
            }

            if (HasEmissive)
            {
                if (EmissiveColor.A < 255)
                    return true;
                if (EmissiveBrush is SolidColorBrush scb && (scb.Color.A < 255 || scb.Opacity < 0.999))
                    return true;
            }

            return false;
        }
    }
}

internal struct DxDrawCommand
{
    public MeshGeometry3D Mesh;
    public Matrix3D WorldMatrix;
    public DxMaterialPassDesc Material;
    public bool FlipWinding;
    public bool CullFrontForBackMaterial;
    public bool NormalNegate;
    public double SortDepthSq;
    public bool Translucent;
}

/// <summary>
/// Shared Direct3D 11 3D pipeline and offscreen D2D interop renderer for Viewport3DVisual.
/// </summary>
internal sealed unsafe class DxViewport3DRenderer : IDisposable
{
    private static readonly object s_rendererLock = new();
    private static DxViewport3DRenderer? s_instance;

    public static void EnsureRegistered()
    {
        Viewport3DVisual.RenderContentD2DHook ??= static (visual, d2dContextPtr) =>
            RenderViewport(visual, (void*)d2dContextPtr);
    }

    public static void RenderViewport(Viewport3DVisual visual, void* d2dContext)
    {
        if (visual == null || d2dContext == null)
            return;

        DxViewport3DRenderer renderer;
        lock (s_rendererLock)
        {
            s_instance ??= new DxViewport3DRenderer();
            renderer = s_instance;
        }

        renderer.RenderCore(visual, d2dContext);
    }

    private readonly DxSharedDevice _shared;
    private readonly object _renderLock = new();
    private long _deviceGeneration = -1;

    private ComPtr<byte> _vertexShader;
    private ComPtr<byte> _pixelShader;
    private ComPtr<byte> _inputLayout;
    private ComPtr<byte> _constantBuffer;
    private ComPtr<byte> _vertexBuffer;
    private ComPtr<byte> _indexBuffer;
    private uint _vertexBufferBytes;
    private uint _indexBufferBytes;

    private ComPtr<byte> _rsCullBack;
    private ComPtr<byte> _rsCullFront;
    private ComPtr<byte> _rsCullNone;
    private ComPtr<byte> _dssDepthWrite;
    private ComPtr<byte> _dssDepthReadOnly;
    private ComPtr<byte> _blendPremul;

    private ComPtr<byte> _whiteTexture;
    private ComPtr<byte> _whiteSrv;

    private readonly ComPtr<byte>[] _samplers = new ComPtr<byte>[9]; // [addrUIndex (0=Wrap,1=Mirror,2=Clamp) * 3 + addrVIndex]

    // Offscreen viewport render target + depth buffer + D2D wrapper bitmap
    private ComPtr<byte> _rtTexture;
    private ComPtr<byte> _rtView;
    private ComPtr<byte> _dsTexture;
    private ComPtr<byte> _dsView;
    private ComPtr<byte> _d2dTargetBitmap;
    private void* _d2dTargetBitmapContext;
    private float _rtDpiX;
    private float _rtDpiY;
    private uint _rtWidth;
    private uint _rtHeight;

    // Reusable per-material D2D-to-D3D brush texture pool
    private readonly List<BrushTexSlot> _brushPool = new();
    private int _brushPoolCursor;

    // Scratch managed buffers to minimize allocations
    private DxVertex3D[] _scratchVertices = Array.Empty<DxVertex3D>();
    private uint[] _scratchIndices = Array.Empty<uint>();
    private readonly List<DxLightData> _scratchLights = new(16);
    private readonly List<DxDrawCommand> _scratchDraws = new(32);

    private sealed class BrushTexSlot : IDisposable
    {
        public uint Width;
        public uint Height;
        public ComPtr<byte> Texture;
        public ComPtr<byte> Srv;

        public void Dispose()
        {
            Srv.Dispose();
            Texture.Dispose();
            Width = 0;
            Height = 0;
        }
    }

    private DxViewport3DRenderer()
    {
        _shared = MediaSystem.EnsureSharedDevice();
    }

    private void RenderCore(Viewport3DVisual visual, void* d2dContext)
    {
        Rect viewport = visual.Viewport;
        Camera? camera = visual.Camera;
        if (viewport.IsEmpty || viewport.Width <= 0.0 || viewport.Height <= 0.0 || camera == null)
            return;

        lock (_renderLock)
        {
            _shared.EnterGpuLock();
            try
            {
                if (_shared.D3DDevice == null || _shared.D3DContext == null)
                    return;

                EnsurePipelineInitialized();

                ID2D1DeviceContextExVtbl.GetDpi(d2dContext, out float dpiX, out float dpiY);
                if (dpiX <= 0.0f) dpiX = 96.0f;
                if (dpiY <= 0.0f) dpiY = 96.0f;

                double dpiScaleX = dpiX / 96.0;
                double dpiScaleY = dpiY / 96.0;

                // Incorporate current D2D render scale so 3D output stays sharp under Viewbox / ScaleTransform
                ID2D1DeviceContextExVtbl.GetTransform(d2dContext, out D2D1_MATRIX_3X2_F d2dXform);
                double xScale = Math.Sqrt(d2dXform.M11 * d2dXform.M11 + d2dXform.M12 * d2dXform.M12);
                double yScale = Math.Sqrt(d2dXform.M21 * d2dXform.M21 + d2dXform.M22 * d2dXform.M22);
                if (xScale <= 0.01 || double.IsNaN(xScale)) xScale = 1.0;
                if (yScale <= 0.01 || double.IsNaN(yScale)) yScale = 1.0;
                xScale = Math.Clamp(xScale, 0.25, 4.0);
                yScale = Math.Clamp(yScale, 0.25, 4.0);

                uint pixelW = (uint)Math.Clamp(Math.Ceiling(viewport.Width * dpiScaleX * xScale), 1.0, 4096.0);
                uint pixelH = (uint)Math.Clamp(Math.Ceiling(viewport.Height * dpiScaleY * yScale), 1.0, 4096.0);

                // Collect scene lights and draw items using exact Viewport3DVisual traversal
                _scratchLights.Clear();
                _scratchDraws.Clear();
                _brushPoolCursor = 0;

                Matrix3D viewMatrix = camera.GetViewMatrix();
                Rect3D sceneBounds = visual.ComputeSubgraphBounds3D();
                Matrix3D projMatrix = ComputeProjectionMatrix(camera, viewport.Size, viewMatrix, sceneBounds);
                Matrix3D viewProjMatrix = viewMatrix * projMatrix;

                Point3D cameraPos = ExtractCameraWorldPosition(camera, ref viewMatrix);

                Viewport3DSceneCollector.CollectScene(
                    visual,
                    in cameraPos,
                    _scratchLights,
                    _scratchDraws);

                // Flush prior D2D commands cleanly before touching shared D3D11 immediate context
                ID2D1RenderTargetFlushVtbl.Flush(d2dContext, null, null);

                EnsureRenderTargets(d2dContext, pixelW, pixelH, dpiX, dpiY);

                void* ctx = _shared.D3DContext;
                void* rtv = _rtView.Ptr;
                void* dsv = _dsView.Ptr;

                float* clearColor = stackalloc float[4] { 0f, 0f, 0f, 0f };
                ID3D11DeviceContext3DVtbl.ClearRenderTargetView(ctx, rtv, clearColor);
                ID3D11DeviceContext3DVtbl.ClearDepthStencilView(
                    ctx,
                    dsv,
                    DxD3D11Const.D3D11_CLEAR_DEPTH | DxD3D11Const.D3D11_CLEAR_STENCIL,
                    1.0f,
                    0);

                if (_scratchDraws.Count > 0)
                {
                    RenderSceneDraws(ctx, rtv, dsv, pixelW, pixelH, in cameraPos, in viewProjMatrix);
                }

                // Unbind RTV/DSV/SRVs cleanly so Direct2D can read _rtTexture safely
                void* nullRtv = null;
                ID3D11DeviceContext3DVtbl.OMSetRenderTargets(ctx, 1, &nullRtv, null);
                void** nullSrvs = stackalloc void*[3] { null, null, null };
                ID3D11DeviceContext3DVtbl.PSSetShaderResources(ctx, 0, 3, nullSrvs);

                if (!_d2dTargetBitmap.IsNull)
                {
                    float bmpDipW = pixelW * 96.0f / dpiX;
                    float bmpDipH = pixelH * 96.0f / dpiY;
                    var srcRect = new D2D1_RECT_F(0f, 0f, bmpDipW, bmpDipH);
                    var dstRect = new D2D1_RECT_F(
                        (float)viewport.X,
                        (float)viewport.Y,
                        (float)viewport.Right,
                        (float)viewport.Bottom);

                    ID2D1DeviceContextExVtbl.DrawBitmap(
                        d2dContext,
                        _d2dTargetBitmap.Ptr,
                        &dstRect,
                        1.0f,
                        DxConst.D2D1_INTERPOLATION_MODE_LINEAR,
                        &srcRect,
                        null);

                    // Flush immediately so subsequent Viewport3D draws in the same frame cannot overwrite _rtTexture
                    ID2D1RenderTargetFlushVtbl.Flush(d2dContext, null, null);
                }
            }
            finally
            {
                _scratchDraws.Clear();
                _scratchLights.Clear();
                _shared.LeaveGpuLock();
            }
        }
    }

    private void RenderSceneDraws(
        void* ctx,
        void* rtv,
        void* dsv,
        uint pixelW,
        uint pixelH,
        in Point3D cameraPos,
        in Matrix3D viewProjMatrix)
    {
        var vp = new D3D11_VIEWPORT
        {
            TopLeftX = 0f,
            TopLeftY = 0f,
            Width = pixelW,
            Height = pixelH,
            MinDepth = 0f,
            MaxDepth = 1f,
        };

        ID3D11DeviceContext3DVtbl.RSSetViewports(ctx, 1, &vp);
        ID3D11DeviceContext3DVtbl.OMSetRenderTargets(ctx, 1, &rtv, dsv);
        ID3D11DeviceContext3DVtbl.IASetInputLayout(ctx, _inputLayout.Ptr);
        ID3D11DeviceContext3DVtbl.IASetPrimitiveTopology(ctx, DxD3D11Const.D3D_PRIMITIVE_TOPOLOGY_TRIANGLELIST);
        ID3D11DeviceContext3DVtbl.VSSetShader(ctx, _vertexShader.Ptr, null, 0);
        ID3D11DeviceContext3DVtbl.PSSetShader(ctx, _pixelShader.Ptr, null, 0);

        void* cb = _constantBuffer.Ptr;
        ID3D11DeviceContext3DVtbl.VSSetConstantBuffers(ctx, 0, 1, &cb);
        ID3D11DeviceContext3DVtbl.PSSetConstantBuffers(ctx, 0, 1, &cb);

        float* blendFactor = stackalloc float[4] { 0f, 0f, 0f, 0f };
        ID3D11DeviceContext3DVtbl.OMSetBlendState(ctx, _blendPremul.Ptr, blendFactor, 0xffffffffu);

        // Pass 1: Opaque draws (preserving tree order)
        ID3D11DeviceContext3DVtbl.OMSetDepthStencilState(ctx, _dssDepthWrite.Ptr, 0);
        for (int i = 0; i < _scratchDraws.Count; i++)
        {
            if (!_scratchDraws[i].Translucent)
            {
                ExecuteSingleDraw(ctx, _scratchDraws[i], in cameraPos, in viewProjMatrix);
            }
        }

        // Pass 2: Translucent draws sorted back-to-front with depth read-only
        int translucentCount = 0;
        for (int i = 0; i < _scratchDraws.Count; i++)
        {
            if (_scratchDraws[i].Translucent)
                translucentCount++;
        }

        if (translucentCount > 0)
        {
            // Sort translucent subset back-to-front by squared distance to camera
            if (translucentCount > 1)
            {
                _scratchDraws.Sort(static (a, b) =>
                {
                    if (a.Translucent != b.Translucent)
                        return a.Translucent ? 1 : -1;
                    if (!a.Translucent)
                        return 0;
                    return b.SortDepthSq.CompareTo(a.Translucent ? a.SortDepthSq : b.SortDepthSq);
                });
            }

            ID3D11DeviceContext3DVtbl.OMSetDepthStencilState(ctx, _dssDepthReadOnly.Ptr, 0);
            for (int i = 0; i < _scratchDraws.Count; i++)
            {
                if (_scratchDraws[i].Translucent)
                {
                    ExecuteSingleDraw(ctx, _scratchDraws[i], in cameraPos, in viewProjMatrix);
                }
            }
        }
    }

    private void ExecuteSingleDraw(
        void* ctx,
        in DxDrawCommand draw,
        in Point3D cameraPos,
        in Matrix3D viewProjMatrix)
    {
        if (!UploadMesh(ctx, draw.Mesh, out uint indexOrVertexCount, out bool indexed))
            return;

        void* diffSrv = _whiteSrv.Ptr;
        void* specSrv = _whiteSrv.Ptr;
        void* emisSrv = _whiteSrv.Ptr;
        void* diffSamp = GetSampler(draw.Material.DiffuseBrush);
        void* specSamp = GetSampler(draw.Material.SpecularBrush);
        void* emisSamp = GetSampler(draw.Material.EmissiveBrush);

        DxPerObjectCB cbData = default;
        WriteMatrixRowMajor(in draw.WorldMatrix, &cbData.WorldM11);
        ComputeNormalMatrix(in draw.WorldMatrix, &cbData.NormM11);
        WriteMatrixRowMajor(in viewProjMatrix, &cbData.ViewProjM11);

        cbData.CameraPosX = (float)cameraPos.X;
        cbData.CameraPosY = (float)cameraPos.Y;
        cbData.CameraPosZ = (float)cameraPos.Z;
        cbData.NormalSign = draw.NormalNegate ? -1.0f : 1.0f;

        if (draw.Material.HasDiffuse)
        {
            cbData.HasDiffuse = 1.0f;
            ResolveMaterialChannel(
                draw.Material.DiffuseBrush,
                draw.Material.DiffuseColor,
                draw.Material.AmbientColor,
                out cbData.DiffuseR, out cbData.DiffuseG, out cbData.DiffuseB, out cbData.DiffuseAlpha,
                out cbData.AmbientR, out cbData.AmbientG, out cbData.AmbientB,
                out cbData.DiffMulAlpha,
                ref diffSrv);
        }

        if (draw.Material.HasSpecular)
        {
            cbData.HasSpecular = 1.0f;
            ResolveMaterialChannel(
                draw.Material.SpecularBrush,
                draw.Material.SpecularColor,
                Colors.White,
                out cbData.SpecularR, out cbData.SpecularG, out cbData.SpecularB, out float specColorA,
                out _, out _, out _,
                out float specMulA,
                ref specSrv);
            cbData.SpecularPower = (float)Math.Clamp(draw.Material.SpecularPower, 0.001, 4096.0);
            cbData.SpecMulAlpha = specColorA * specMulA;
        }

        if (draw.Material.HasEmissive)
        {
            cbData.HasEmissive = 1.0f;
            ResolveMaterialChannel(
                draw.Material.EmissiveBrush,
                draw.Material.EmissiveColor,
                Colors.White,
                out cbData.EmissiveR, out cbData.EmissiveG, out cbData.EmissiveB, out float emisColorA,
                out _, out _, out _,
                out float emisMulA,
                ref emisSrv);
            cbData.EmisMulAlpha = emisColorA * emisMulA;
        }

        int lightCount = Math.Min(_scratchLights.Count, 32);
        cbData.LightCount = lightCount;
        for (int i = 0; i < lightCount; i++)
        {
            cbData.Lights[i] = _scratchLights[i];
        }

        if (ID3D11DeviceContextBaseVtbl.Map(ctx, _constantBuffer.Ptr, 0, DxConst.D3D11_MAP_WRITE_DISCARD, 0, out D3D11_MAPPED_SUBRESOURCE mapped) >= 0)
        {
            Buffer.MemoryCopy(&cbData, mapped.PData, sizeof(DxPerObjectCB), sizeof(DxPerObjectCB));
            ID3D11DeviceContextBaseVtbl.Unmap(ctx, _constantBuffer.Ptr, 0);
        }

        bool cullFront = draw.FlipWinding ^ draw.CullFrontForBackMaterial;
        void* rs = cullFront ? _rsCullFront.Ptr : _rsCullBack.Ptr;
        ID3D11DeviceContext3DVtbl.RSSetState(ctx, rs);

        void** srvs = stackalloc void*[3] { diffSrv, specSrv, emisSrv };
        void** samps = stackalloc void*[3] { diffSamp, specSamp, emisSamp };
        ID3D11DeviceContext3DVtbl.PSSetShaderResources(ctx, 0, 3, srvs);
        ID3D11DeviceContext3DVtbl.PSSetSamplers(ctx, 0, 3, samps);

        uint stride = (uint)sizeof(DxVertex3D);
        uint offset = 0;
        void* vb = _vertexBuffer.Ptr;
        ID3D11DeviceContext3DVtbl.IASetVertexBuffers(ctx, 0, 1, &vb, &stride, &offset);

        if (indexed)
        {
            ID3D11DeviceContext3DVtbl.IASetIndexBuffer(ctx, _indexBuffer.Ptr, DxConst.DXGI_FORMAT_R32_UINT, 0);
            ID3D11DeviceContext3DVtbl.DrawIndexed(ctx, indexOrVertexCount, 0, 0);
        }
        else
        {
            ID3D11DeviceContext3DVtbl.Draw(ctx, indexOrVertexCount, 0);
        }
    }

    private void ResolveMaterialChannel(
        Brush? brush,
        Color matColor,
        Color matAmbient,
        out float r, out float g, out float b, out float matAlpha,
        out float ambR, out float ambG, out float ambB,
        out float brushAlpha,
        ref void* srv)
    {
        float mr = matColor.R / 255f;
        float mg = matColor.G / 255f;
        float mb = matColor.B / 255f;
        matAlpha = matColor.A / 255f;

        float ar = matAmbient.R / 255f;
        float ag = matAmbient.G / 255f;
        float ab = matAmbient.B / 255f;

        if (brush == null)
        {
            r = 0f; g = 0f; b = 0f;
            ambR = 0f; ambG = 0f; ambB = 0f;
            brushAlpha = 0f;
            return;
        }

        if (brush is SolidColorBrush scb)
        {
            Color bc = scb.Color;
            float br = bc.R / 255f;
            float bg = bc.G / 255f;
            float bb = bc.B / 255f;
            float ba = (bc.A / 255f) * (float)Math.Clamp(scb.Opacity, 0.0, 1.0);

            r = mr * br;
            g = mg * bg;
            b = mb * bb;
            ambR = ar * br;
            ambG = ag * bg;
            ambB = ab * bb;
            brushAlpha = ba;
            return;
        }

        r = mr; g = mg; b = mb;
        ambR = ar; ambG = ag; ambB = ab;
        // Brush.Opacity and gradient/stop alpha are already baked when rasterized into the D2D surface.
        brushAlpha = 1.0f;

        void* renderedSrv = RasterizeBrushToSrv(brush);
        if (renderedSrv != null)
        {
            srv = renderedSrv;
        }
        else
        {
            // Brush produced empty visuals/images -> draw transparent
            brushAlpha = 0f;
        }
    }

    private void* RasterizeBrushToSrv(Brush brush)
    {
        if (brush is VisualBrush vbLayout)
        {
            vbLayout.EnsureAutoLayoutForRender();
        }

        ComputeBrushTextureSize(brush, out uint texW, out uint texH);

        BrushTexSlot slot = AcquireBrushSlot(texW, texH);
        if (slot.Texture.IsNull || slot.Srv.IsNull)
            return null;

        void* scratchCtx = null;
        void* targetBmp = null;
        try
        {
            if (_shared.CreateDeviceContext(out scratchCtx) < 0 || scratchCtx == null)
                return null;

            using var dxgiSurface = new ComPtr<byte>();
            if (ComPtr<byte>.QueryInterface(slot.Texture.Ptr, DxGuids.IID_IDXGISurface, (void**)dxgiSurface.AddressOf()) < 0)
                return null;

            var props = new D2D1_BITMAP_PROPERTIES1
            {
                PixelFormat = new D2D1_PIXEL_FORMAT
                {
                    Format = DxConst.DXGI_FORMAT_B8G8R8A8_UNORM,
                    AlphaMode = DxConst.D2D1_ALPHA_MODE_PREMULTIPLIED,
                },
                DpiX = 96.0f,
                DpiY = 96.0f,
                BitmapOptions = DxConst.D2D1_BITMAP_OPTIONS_TARGET | DxConst.D2D1_BITMAP_OPTIONS_CANNOT_DRAW,
                ColorContext = null,
            };

            if (ID2D1DeviceContextVtbl.CreateBitmapFromDxgiSurface(scratchCtx, dxgiSurface.Ptr, in props, &targetBmp) < 0 || targetBmp == null)
                return null;

            // WPF 3D material brushes evaluate across UV [0,1]x[0,1], then scale up cleanly to texW x texH.
            var bounds = new Rect(0.0, 0.0, 1.0, 1.0);
            var uvToPx = new D2D1_MATRIX_3X2_F { M11 = texW, M22 = texH };
            ID2D1DeviceContextVtbl.SetTarget(scratchCtx, targetBmp);
            ID2D1DeviceContextVtbl.SetDpi(scratchCtx, 96.0f, 96.0f);
            ID2D1DeviceContextVtbl.BeginDraw(scratchCtx);
            ID2D1DeviceContextVtbl.SetTransform(scratchCtx, uvToPx);
            ID2D1DeviceContextVtbl.Clear(scratchCtx, new D2D1_COLOR_F(0f, 0f, 0f, 0f));

            void* d2dBrush = null;
            try
            {
                d2dBrush = brush.CreateD2DBrush(scratchCtx, in bounds);
                if (d2dBrush != null)
                {
                    ID2D1DeviceContextVtbl.FillRectangle(scratchCtx, new D2D1_RECT_F(0f, 0f, 1f, 1f), d2dBrush);
                }
            }
            finally
            {
                if (d2dBrush != null)
                    ComHelpers.Release(d2dBrush);
                ID2D1DeviceContextVtbl.EndDraw(scratchCtx);
                ID2D1DeviceContextVtbl.SetTarget(scratchCtx, null);
            }

            return d2dBrush != null ? slot.Srv.Ptr : null;
        }
        finally
        {
            if (targetBmp != null)
                ComHelpers.Release(targetBmp);
            if (scratchCtx != null)
                ComHelpers.Release(scratchCtx);
        }
    }

    private static void ComputeBrushTextureSize(Brush brush, out uint width, out uint height)
    {
        if (brush is ImageBrush ib && ib.ImageSource is BitmapSource bs && bs.PixelWidth > 0 && bs.PixelHeight > 0)
        {
            width = (uint)Math.Clamp(bs.PixelWidth, 1, 2048);
            height = (uint)Math.Clamp(bs.PixelHeight, 1, 2048);
            return;
        }

        if (brush is VisualBrush vb && vb.Visual != null)
        {
            Rect b = vb.Visual.CalculateSubgraphBoundsOuterSpace();
            if (!b.IsEmpty && b.Width > 0.0 && b.Height > 0.0)
            {
                double maxDim = Math.Max(b.Width, b.Height);
                double scale = maxDim < 256.0 ? (512.0 / maxDim) : (maxDim > 1024.0 ? (1024.0 / maxDim) : 1.0);
                width = (uint)Math.Clamp(Math.Ceiling(b.Width * scale), 64.0, 1024.0);
                height = (uint)Math.Clamp(Math.Ceiling(b.Height * scale), 64.0, 1024.0);
                return;
            }
        }

        width = 512;
        height = 512;
    }

    private BrushTexSlot AcquireBrushSlot(uint width, uint height)
    {
        for (int i = _brushPoolCursor; i < _brushPool.Count; i++)
        {
            BrushTexSlot existing = _brushPool[i];
            if (existing.Width == width && existing.Height == height && !existing.Texture.IsNull)
            {
                if (i != _brushPoolCursor)
                {
                    _brushPool[i] = _brushPool[_brushPoolCursor];
                    _brushPool[_brushPoolCursor] = existing;
                }
                _brushPoolCursor++;
                return existing;
            }
        }

        BrushTexSlot slot;
        if (_brushPoolCursor < _brushPool.Count)
        {
            slot = _brushPool[_brushPoolCursor];
            slot.Dispose();
        }
        else
        {
            slot = new BrushTexSlot();
            _brushPool.Add(slot);
        }

        var texDesc = new D3D11_TEXTURE2D_DESC
        {
            Width = width,
            Height = height,
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
        if (ID3D11DeviceBaseVtbl.CreateTexture2D(_shared.D3DDevice, in texDesc, null, &texPtr) >= 0 && texPtr != null)
        {
            slot.Texture.Attach((byte*)texPtr);

            var srvDesc = new D3D11_SHADER_RESOURCE_VIEW_DESC
            {
                Format = DxConst.DXGI_FORMAT_B8G8R8A8_UNORM,
                ViewDimension = DxD3D11Const.D3D11_SRV_DIMENSION_TEXTURE2D,
                MostDetailedMip = 0,
                MipLevels = 1,
            };

            void* srvPtr = null;
            if (ID3D11Device3DVtbl.CreateShaderResourceView(_shared.D3DDevice, slot.Texture.Ptr, &srvDesc, &srvPtr) >= 0 && srvPtr != null)
            {
                slot.Srv.Attach((byte*)srvPtr);
                slot.Width = width;
                slot.Height = height;
            }
            else
            {
                slot.Dispose();
            }
        }

        _brushPoolCursor++;
        return slot;
    }

    private bool UploadMesh(void* ctx, MeshGeometry3D mesh, out uint count, out bool indexed)
    {
        count = 0;
        indexed = false;

        Point3DCollection? positions = mesh.Positions;
        if (positions == null || positions.Count < 3)
            return false;

        Vector3DCollection? normals = mesh.Normals;
        PointCollection? uvs = mesh.TextureCoordinates;
        Int32Collection? indices = mesh.TriangleIndices;

        int posCount = positions.Count;
        bool hasExplicitIndices = indices != null && indices.Count >= 3;
        int triCount = hasExplicitIndices ? (indices!.Count / 3) : (posCount / 3);
        if (triCount <= 0)
            return false;

        bool canUseIndexed = hasExplicitIndices && normals != null && normals.Count >= posCount;
        bool hasUvs = uvs != null && uvs.Count > 0;

        if (canUseIndexed)
        {
            indexed = true;
            if (_scratchVertices.Length < posCount)
                _scratchVertices = new DxVertex3D[Math.Max(posCount, 64)];

            for (int i = 0; i < posCount; i++)
            {
                Point3D p = positions[i];
                Vector3D n = normals![i];
                Point uv = (uvs != null && i < uvs.Count) ? uvs[i] : default;

                _scratchVertices[i] = new DxVertex3D
                {
                    Px = (float)p.X,
                    Py = (float)p.Y,
                    Pz = (float)p.Z,
                    Nx = (float)n.X,
                    Ny = (float)n.Y,
                    Nz = (float)n.Z,
                    U = (float)uv.X,
                    V = (float)uv.Y,
                };
            }

            int idxCount = triCount * 3;
            if (_scratchIndices.Length < idxCount)
                _scratchIndices = new uint[Math.Max(idxCount, 128)];

            int validIdx = 0;
            for (int t = 0; t < triCount; t++)
            {
                int i0 = indices![t * 3];
                int i1 = indices[t * 3 + 1];
                int i2 = indices[t * 3 + 2];
                if ((uint)i0 >= (uint)posCount || (uint)i1 >= (uint)posCount || (uint)i2 >= (uint)posCount)
                    break;

                _scratchIndices[validIdx++] = (uint)i0;
                _scratchIndices[validIdx++] = (uint)i1;
                _scratchIndices[validIdx++] = (uint)i2;
            }

            if (validIdx < 3)
                return false;

            EnsureDynamicVertexBuffer((uint)(posCount * sizeof(DxVertex3D)));
            EnsureDynamicIndexBuffer((uint)(validIdx * sizeof(uint)));

            UploadBufferData(ctx, _vertexBuffer.Ptr, _scratchVertices, posCount * sizeof(DxVertex3D));
            UploadBufferData(ctx, _indexBuffer.Ptr, _scratchIndices, validIdx * sizeof(uint));

            count = (uint)validIdx;
            return true;
        }
        else
        {
            indexed = false;
            int outVertsNeeded = triCount * 3;
            if (_scratchVertices.Length < outVertsNeeded)
                _scratchVertices = new DxVertex3D[Math.Max(outVertsNeeded, 128)];

            int vOut = 0;
            for (int t = 0; t < triCount; t++)
            {
                int i0 = hasExplicitIndices ? indices![t * 3] : (t * 3);
                int i1 = hasExplicitIndices ? indices![t * 3 + 1] : (t * 3 + 1);
                int i2 = hasExplicitIndices ? indices![t * 3 + 2] : (t * 3 + 2);
                if ((uint)i0 >= (uint)posCount || (uint)i1 >= (uint)posCount || (uint)i2 >= (uint)posCount)
                    break;

                Point3D p0 = positions[i0];
                Point3D p1 = positions[i1];
                Point3D p2 = positions[i2];

                Vector3D faceNormal = Vector3D.CrossProduct(p1 - p0, p2 - p0);
                if (faceNormal.LengthSquared > 1e-24)
                    faceNormal.Normalize();
                else
                    faceNormal = new Vector3D(0, 0, 1);

                Vector3D n0 = (normals != null && i0 < normals.Count) ? normals[i0] : faceNormal;
                Vector3D n1 = (normals != null && i1 < normals.Count) ? normals[i1] : faceNormal;
                Vector3D n2 = (normals != null && i2 < normals.Count) ? normals[i2] : faceNormal;

                Point uv0 = (hasUvs && i0 < uvs!.Count) ? uvs[i0] : default;
                Point uv1 = (hasUvs && i1 < uvs!.Count) ? uvs[i1] : default;
                Point uv2 = (hasUvs && i2 < uvs!.Count) ? uvs[i2] : default;

                _scratchVertices[vOut++] = new DxVertex3D
                {
                    Px = (float)p0.X, Py = (float)p0.Y, Pz = (float)p0.Z,
                    Nx = (float)n0.X, Ny = (float)n0.Y, Nz = (float)n0.Z,
                    U = (float)uv0.X, V = (float)uv0.Y,
                };
                _scratchVertices[vOut++] = new DxVertex3D
                {
                    Px = (float)p1.X, Py = (float)p1.Y, Pz = (float)p1.Z,
                    Nx = (float)n1.X, Ny = (float)n1.Y, Nz = (float)n1.Z,
                    U = (float)uv1.X, V = (float)uv1.Y,
                };
                _scratchVertices[vOut++] = new DxVertex3D
                {
                    Px = (float)p2.X, Py = (float)p2.Y, Pz = (float)p2.Z,
                    Nx = (float)n2.X, Ny = (float)n2.Y, Nz = (float)n2.Z,
                    U = (float)uv2.X, V = (float)uv2.Y,
                };
            }

            if (vOut < 3)
                return false;

            EnsureDynamicVertexBuffer((uint)(vOut * sizeof(DxVertex3D)));
            UploadBufferData(ctx, _vertexBuffer.Ptr, _scratchVertices, vOut * sizeof(DxVertex3D));
            count = (uint)vOut;
            return true;
        }
    }

    private static void UploadBufferData<T>(void* ctx, void* buffer, T[] source, int byteLength) where T : unmanaged
    {
        fixed (T* pSrc = source)
        {
            if (ID3D11DeviceContextBaseVtbl.Map(ctx, buffer, 0, DxConst.D3D11_MAP_WRITE_DISCARD, 0, out D3D11_MAPPED_SUBRESOURCE mapped) >= 0)
            {
                Buffer.MemoryCopy(pSrc, mapped.PData, byteLength, byteLength);
                ID3D11DeviceContextBaseVtbl.Unmap(ctx, buffer, 0);
            }
        }
    }

    private void* GetSampler(Brush? brush)
    {
        int uIdx = 2; // Clamp default
        int vIdx = 2;

        if (brush is TileBrush tb)
        {
            (uIdx, vIdx) = tb.TileMode switch
            {
                TileMode.Tile => (0, 0),
                TileMode.FlipX => (1, 0),
                TileMode.FlipY => (0, 1),
                TileMode.FlipXY => (1, 1),
                _ => (2, 2),
            };
        }
        else if (brush is GradientBrush gb)
        {
            int idx = gb.SpreadMethod switch
            {
                GradientSpreadMethod.Repeat => 0,
                GradientSpreadMethod.Reflect => 1,
                _ => 2,
            };
            uIdx = idx;
            vIdx = idx;
        }

        int slot = uIdx * 3 + vIdx;
        if (_samplers[slot].IsNull)
        {
            uint addrU = uIdx == 0 ? DxD3D11Const.D3D11_TEXTURE_ADDRESS_WRAP : (uIdx == 1 ? DxD3D11Const.D3D11_TEXTURE_ADDRESS_MIRROR : DxD3D11Const.D3D11_TEXTURE_ADDRESS_CLAMP);
            uint addrV = vIdx == 0 ? DxD3D11Const.D3D11_TEXTURE_ADDRESS_WRAP : (vIdx == 1 ? DxD3D11Const.D3D11_TEXTURE_ADDRESS_MIRROR : DxD3D11Const.D3D11_TEXTURE_ADDRESS_CLAMP);

            var desc = new D3D11_SAMPLER_DESC
            {
                Filter = DxD3D11Const.D3D11_FILTER_MIN_MAG_MIP_LINEAR,
                AddressU = addrU,
                AddressV = addrV,
                AddressW = DxD3D11Const.D3D11_TEXTURE_ADDRESS_CLAMP,
                MipLODBias = 0f,
                MaxAnisotropy = 1,
                ComparisonFunc = DxD3D11Const.D3D11_COMPARISON_ALWAYS,
                MinLOD = 0f,
                MaxLOD = float.MaxValue,
            };

            void* sampPtr = null;
            HResult.ThrowIfFailed(
                ID3D11Device3DVtbl.CreateSamplerState(_shared.D3DDevice, in desc, &sampPtr),
                "CreateSamplerState(Viewport3D)");
            _samplers[slot].Attach((byte*)sampPtr);
        }

        return _samplers[slot].Ptr;
    }

    private void EnsureDynamicVertexBuffer(uint neededBytes)
    {
        if (!_vertexBuffer.IsNull && _vertexBufferBytes >= neededBytes)
            return;

        _vertexBuffer.Dispose();
        uint allocBytes = Math.Max(neededBytes, Math.Max(_vertexBufferBytes * 2u, 4096u));
        var desc = new D3D11_BUFFER_DESC
        {
            ByteWidth = allocBytes,
            Usage = DxConst.D3D11_USAGE_DYNAMIC,
            BindFlags = DxConst.D3D11_BIND_VERTEX_BUFFER,
            CPUAccessFlags = DxConst.D3D11_CPU_ACCESS_WRITE,
        };

        void* ptr = null;
        HResult.ThrowIfFailed(ID3D11Device3DVtbl.CreateBuffer(_shared.D3DDevice, in desc, null, &ptr), "CreateVertexBuffer(Viewport3D)");
        _vertexBuffer.Attach((byte*)ptr);
        _vertexBufferBytes = allocBytes;
    }

    private void EnsureDynamicIndexBuffer(uint neededBytes)
    {
        if (!_indexBuffer.IsNull && _indexBufferBytes >= neededBytes)
            return;

        _indexBuffer.Dispose();
        uint allocBytes = Math.Max(neededBytes, Math.Max(_indexBufferBytes * 2u, 4096u));
        var desc = new D3D11_BUFFER_DESC
        {
            ByteWidth = allocBytes,
            Usage = DxConst.D3D11_USAGE_DYNAMIC,
            BindFlags = DxConst.D3D11_BIND_INDEX_BUFFER,
            CPUAccessFlags = DxConst.D3D11_CPU_ACCESS_WRITE,
        };

        void* ptr = null;
        HResult.ThrowIfFailed(ID3D11Device3DVtbl.CreateBuffer(_shared.D3DDevice, in desc, null, &ptr), "CreateIndexBuffer(Viewport3D)");
        _indexBuffer.Attach((byte*)ptr);
        _indexBufferBytes = allocBytes;
    }

    private void EnsurePipelineInitialized()
    {
        long gen = _shared.DeviceGeneration;
        if (_deviceGeneration == gen && !_vertexShader.IsNull)
            return;

        ReleaseDeviceResources();

        void* dev = _shared.D3DDevice;

        ReadOnlySpan<byte> vsBytes = DxViewport3DShaders.VertexShaderBytecode;
        ReadOnlySpan<byte> psBytes = DxViewport3DShaders.PixelShaderBytecode;

        fixed (byte* pVs = vsBytes)
        fixed (byte* pPs = psBytes)
        {
            void* vsPtr = null;
            HResult.ThrowIfFailed(
                ID3D11Device3DVtbl.CreateVertexShader(dev, pVs, (nuint)vsBytes.Length, null, &vsPtr),
                "CreateVertexShader(Viewport3D)");
            _vertexShader.Attach((byte*)vsPtr);

            void* psPtr = null;
            HResult.ThrowIfFailed(
                ID3D11Device3DVtbl.CreatePixelShader(dev, pPs, (nuint)psBytes.Length, null, &psPtr),
                "CreatePixelShader(Viewport3D)");
            _pixelShader.Attach((byte*)psPtr);

            fixed (byte* pPos = "POSITION\0"u8)
            fixed (byte* pNrm = "NORMAL\0"u8)
            fixed (byte* pTex = "TEXCOORD\0"u8)
            {
                D3D11_INPUT_ELEMENT_DESC* elems = stackalloc D3D11_INPUT_ELEMENT_DESC[3];
                elems[0] = new D3D11_INPUT_ELEMENT_DESC
                {
                    SemanticName = pPos,
                    SemanticIndex = 0,
                    Format = DxConst.DXGI_FORMAT_R32G32B32_FLOAT,
                    InputSlot = 0,
                    AlignedByteOffset = 0,
                    InputSlotClass = DxD3D11Const.D3D11_INPUT_PER_VERTEX_DATA,
                    InstanceDataStepRate = 0,
                };
                elems[1] = new D3D11_INPUT_ELEMENT_DESC
                {
                    SemanticName = pNrm,
                    SemanticIndex = 0,
                    Format = DxConst.DXGI_FORMAT_R32G32B32_FLOAT,
                    InputSlot = 0,
                    AlignedByteOffset = 12,
                    InputSlotClass = DxD3D11Const.D3D11_INPUT_PER_VERTEX_DATA,
                    InstanceDataStepRate = 0,
                };
                elems[2] = new D3D11_INPUT_ELEMENT_DESC
                {
                    SemanticName = pTex,
                    SemanticIndex = 0,
                    Format = DxConst.DXGI_FORMAT_R32G32_FLOAT,
                    InputSlot = 0,
                    AlignedByteOffset = 24,
                    InputSlotClass = DxD3D11Const.D3D11_INPUT_PER_VERTEX_DATA,
                    InstanceDataStepRate = 0,
                };

                void* ilPtr = null;
                HResult.ThrowIfFailed(
                    ID3D11Device3DVtbl.CreateInputLayout(dev, elems, 3, pVs, (nuint)vsBytes.Length, &ilPtr),
                    "CreateInputLayout(Viewport3D)");
                _inputLayout.Attach((byte*)ilPtr);
            }
        }

        uint cbSize = (uint)sizeof(DxPerObjectCB);
        var cbDesc = new D3D11_BUFFER_DESC
        {
            ByteWidth = cbSize,
            Usage = DxConst.D3D11_USAGE_DYNAMIC,
            BindFlags = DxConst.D3D11_BIND_CONSTANT_BUFFER,
            CPUAccessFlags = DxConst.D3D11_CPU_ACCESS_WRITE,
        };
        void* cbPtr = null;
        HResult.ThrowIfFailed(ID3D11Device3DVtbl.CreateBuffer(dev, in cbDesc, null, &cbPtr), "CreateConstantBuffer(Viewport3D)");
        _constantBuffer.Attach((byte*)cbPtr);

        // Rasterizer states (WPF uses right-handed CCW front winding)
        _rsCullBack.Attach((byte*)CreateRasterizer(dev, DxD3D11Const.D3D11_CULL_BACK));
        _rsCullFront.Attach((byte*)CreateRasterizer(dev, DxD3D11Const.D3D11_CULL_FRONT));
        _rsCullNone.Attach((byte*)CreateRasterizer(dev, DxD3D11Const.D3D11_CULL_NONE));

        // Depth stencil states
        _dssDepthWrite.Attach((byte*)CreateDepthStencil(dev, writeDepth: true));
        _dssDepthReadOnly.Attach((byte*)CreateDepthStencil(dev, writeDepth: false));

        // Premultiplied alpha blend state matching Direct2D B8G8R8A8_UNORM PREMULTIPLIED
        var blendDesc = new D3D11_BLEND_DESC
        {
            AlphaToCoverageEnable = 0,
            IndependentBlendEnable = 0,
        };
        blendDesc.RenderTarget[0] = new D3D11_RENDER_TARGET_BLEND_DESC
        {
            BlendEnable = 1,
            SrcBlend = DxD3D11Const.D3D11_BLEND_ONE,
            DestBlend = DxD3D11Const.D3D11_BLEND_INV_SRC_ALPHA,
            BlendOp = DxD3D11Const.D3D11_BLEND_OP_ADD,
            SrcBlendAlpha = DxD3D11Const.D3D11_BLEND_ONE,
            DestBlendAlpha = DxD3D11Const.D3D11_BLEND_INV_SRC_ALPHA,
            BlendOpAlpha = DxD3D11Const.D3D11_BLEND_OP_ADD,
            RenderTargetWriteMask = DxD3D11Const.D3D11_COLOR_WRITE_ENABLE_ALL,
        };
        void* bsPtr = null;
        HResult.ThrowIfFailed(ID3D11Device3DVtbl.CreateBlendState(dev, in blendDesc, &bsPtr), "CreateBlendState(Viewport3D)");
        _blendPremul.Attach((byte*)bsPtr);

        // 1x1 opaque white fallback texture for solid-color materials
        uint whitePixel = 0xffffffffu;
        var whiteDesc = new D3D11_TEXTURE2D_DESC
        {
            Width = 1,
            Height = 1,
            MipLevels = 1,
            ArraySize = 1,
            Format = DxConst.DXGI_FORMAT_B8G8R8A8_UNORM,
            SampleDesc = new DXGI_SAMPLE_DESC { Count = 1, Quality = 0 },
            Usage = DxConst.D3D11_USAGE_IMMUTABLE,
            BindFlags = DxConst.D3D11_BIND_SHADER_RESOURCE,
        };
        var whiteInit = new D3D11_SUBRESOURCE_DATA
        {
            PSysMem = &whitePixel,
            SysMemPitch = 4,
            SysMemSlicePitch = 4,
        };
        void* wTex = null;
        HResult.ThrowIfFailed(ID3D11DeviceBaseVtbl.CreateTexture2D(dev, in whiteDesc, &whiteInit, &wTex), "CreateWhiteTex(Viewport3D)");
        _whiteTexture.Attach((byte*)wTex);

        var whiteSrvDesc = new D3D11_SHADER_RESOURCE_VIEW_DESC
        {
            Format = DxConst.DXGI_FORMAT_B8G8R8A8_UNORM,
            ViewDimension = DxD3D11Const.D3D11_SRV_DIMENSION_TEXTURE2D,
            MostDetailedMip = 0,
            MipLevels = 1,
        };
        void* wSrv = null;
        HResult.ThrowIfFailed(ID3D11Device3DVtbl.CreateShaderResourceView(dev, _whiteTexture.Ptr, &whiteSrvDesc, &wSrv), "CreateWhiteSrv(Viewport3D)");
        _whiteSrv.Attach((byte*)wSrv);

        _deviceGeneration = gen;
    }

    private static void* CreateRasterizer(void* dev, uint cullMode)
    {
        var desc = new D3D11_RASTERIZER_DESC
        {
            FillMode = DxD3D11Const.D3D11_FILL_SOLID,
            CullMode = cullMode,
            FrontCounterClockwise = 1,
            DepthClipEnable = 1,
        };
        void* ptr = null;
        HResult.ThrowIfFailed(ID3D11Device3DVtbl.CreateRasterizerState(dev, in desc, &ptr), "CreateRasterizerState(Viewport3D)");
        return ptr;
    }

    private static void* CreateDepthStencil(void* dev, bool writeDepth)
    {
        var op = new D3D11_DEPTH_STENCILOP_DESC
        {
            StencilFailOp = DxD3D11Const.D3D11_STENCIL_OP_KEEP,
            StencilDepthFailOp = DxD3D11Const.D3D11_STENCIL_OP_KEEP,
            StencilPassOp = DxD3D11Const.D3D11_STENCIL_OP_KEEP,
            StencilFunc = DxD3D11Const.D3D11_COMPARISON_ALWAYS,
        };
        var desc = new D3D11_DEPTH_STENCIL_DESC
        {
            DepthEnable = 1,
            DepthWriteMask = writeDepth ? DxD3D11Const.D3D11_DEPTH_WRITE_MASK_ALL : DxD3D11Const.D3D11_DEPTH_WRITE_MASK_ZERO,
            DepthFunc = DxD3D11Const.D3D11_COMPARISON_LESS_EQUAL,
            StencilEnable = 0,
            StencilReadMask = 0xff,
            StencilWriteMask = 0xff,
            FrontFace = op,
            BackFace = op,
        };
        void* ptr = null;
        HResult.ThrowIfFailed(ID3D11Device3DVtbl.CreateDepthStencilState(dev, in desc, &ptr), "CreateDepthStencilState(Viewport3D)");
        return ptr;
    }

    private void EnsureRenderTargets(void* d2dContext, uint width, uint height, float dpiX, float dpiY)
    {
        bool needTarget =
            _rtTexture.IsNull ||
            _rtView.IsNull ||
            _dsView.IsNull ||
            _rtWidth != width ||
            _rtHeight != height;

        bool needBitmap =
            needTarget ||
            _d2dTargetBitmap.IsNull ||
            _d2dTargetBitmapContext != d2dContext ||
            Math.Abs(_rtDpiX - dpiX) > 0.01f ||
            Math.Abs(_rtDpiY - dpiY) > 0.01f;

        if (needTarget)
        {
            _d2dTargetBitmap.Dispose();
            _rtView.Dispose();
            _rtTexture.Dispose();
            _dsView.Dispose();
            _dsTexture.Dispose();

            void* dev = _shared.D3DDevice;

            var colorDesc = new D3D11_TEXTURE2D_DESC
            {
                Width = width,
                Height = height,
                MipLevels = 1,
                ArraySize = 1,
                Format = DxConst.DXGI_FORMAT_B8G8R8A8_UNORM,
                SampleDesc = new DXGI_SAMPLE_DESC { Count = 1, Quality = 0 },
                Usage = DxConst.D3D11_USAGE_DEFAULT,
                BindFlags = DxConst.D3D11_BIND_RENDER_TARGET | DxConst.D3D11_BIND_SHADER_RESOURCE,
            };
            void* rtTex = null;
            HResult.ThrowIfFailed(ID3D11DeviceBaseVtbl.CreateTexture2D(dev, in colorDesc, null, &rtTex), "Create3DColorTarget");
            _rtTexture.Attach((byte*)rtTex);

            var rtvDesc = new D3D11_RENDER_TARGET_VIEW_DESC
            {
                Format = DxConst.DXGI_FORMAT_B8G8R8A8_UNORM,
                ViewDimension = DxD3D11Const.D3D11_RTV_DIMENSION_TEXTURE2D,
                MipSlice = 0,
            };
            void* rtvPtr = null;
            HResult.ThrowIfFailed(ID3D11Device3DVtbl.CreateRenderTargetView(dev, _rtTexture.Ptr, &rtvDesc, &rtvPtr), "Create3DRTV");
            _rtView.Attach((byte*)rtvPtr);

            var depthDesc = new D3D11_TEXTURE2D_DESC
            {
                Width = width,
                Height = height,
                MipLevels = 1,
                ArraySize = 1,
                Format = DxConst.DXGI_FORMAT_D24_UNORM_S8_UINT,
                SampleDesc = new DXGI_SAMPLE_DESC { Count = 1, Quality = 0 },
                Usage = DxConst.D3D11_USAGE_DEFAULT,
                BindFlags = DxConst.D3D11_BIND_DEPTH_STENCIL,
            };
            void* dsTex = null;
            HResult.ThrowIfFailed(ID3D11DeviceBaseVtbl.CreateTexture2D(dev, in depthDesc, null, &dsTex), "Create3DDepthTarget");
            _dsTexture.Attach((byte*)dsTex);

            var dsvDesc = new D3D11_DEPTH_STENCIL_VIEW_DESC
            {
                Format = DxConst.DXGI_FORMAT_D24_UNORM_S8_UINT,
                ViewDimension = DxD3D11Const.D3D11_DSV_DIMENSION_TEXTURE2D,
                Flags = 0,
                MipSlice = 0,
            };
            void* dsvPtr = null;
            HResult.ThrowIfFailed(ID3D11Device3DVtbl.CreateDepthStencilView(dev, _dsTexture.Ptr, &dsvDesc, &dsvPtr), "Create3DDSV");
            _dsView.Attach((byte*)dsvPtr);

            _rtWidth = width;
            _rtHeight = height;
            needBitmap = true;
        }

        if (needBitmap)
        {
            _d2dTargetBitmap.Dispose();

            using var dxgiSurface = new ComPtr<byte>();
            HResult.ThrowIfFailed(
                ComPtr<byte>.QueryInterface(_rtTexture.Ptr, DxGuids.IID_IDXGISurface, (void**)dxgiSurface.AddressOf()),
                "QI IDXGISurface(3DRenderTarget)");

            var props = new D2D1_BITMAP_PROPERTIES1
            {
                PixelFormat = new D2D1_PIXEL_FORMAT
                {
                    Format = DxConst.DXGI_FORMAT_B8G8R8A8_UNORM,
                    AlphaMode = DxConst.D2D1_ALPHA_MODE_PREMULTIPLIED,
                },
                DpiX = dpiX,
                DpiY = dpiY,
                BitmapOptions = DxConst.D2D1_BITMAP_OPTIONS_NONE,
                ColorContext = null,
            };

            void* bmpPtr = null;
            HResult.ThrowIfFailed(
                ID2D1DeviceContextVtbl.CreateBitmapFromDxgiSurface(d2dContext, dxgiSurface.Ptr, in props, &bmpPtr),
                "CreateBitmapFromDxgiSurface(Viewport3D)");
            _d2dTargetBitmap.Attach((byte*)bmpPtr);
            _d2dTargetBitmapContext = d2dContext;
            _rtDpiX = dpiX;
            _rtDpiY = dpiY;
        }
    }

    private static Matrix3D ComputeProjectionMatrix(Camera camera, Size viewportSize, Matrix3D viewMatrix, Rect3D sceneBounds)
    {
        double aspectRatio = M3DUtil.GetAspectRatio(viewportSize);

        if (camera is OrthographicCamera ortho)
        {
            double zn = ortho.NearPlaneDistance;
            double zf = ortho.FarPlaneDistance;

            if (double.IsInfinity(zn) || double.IsInfinity(zf))
            {
                double sceneNear = 0.125;
                double sceneFar = 100.0;

                if (!sceneBounds.IsEmpty)
                {
                    Rect3D viewBox = M3DUtil.ComputeTransformedAxisAlignedBoundingBox(ref sceneBounds, ref viewMatrix);
                    if (!viewBox.IsEmpty)
                    {
                        sceneNear = -(viewBox.Z + viewBox.SizeZ);
                        sceneFar = -viewBox.Z;
                    }
                }

                if (double.IsInfinity(zn))
                {
                    zn = sceneNear - 1.0;
                }
                if (double.IsInfinity(zf))
                {
                    zf = Math.Max(zn + 1.0, sceneFar + 1.0);
                }
            }

            return ortho.GetProjectionMatrix(aspectRatio, zn, zf);
        }

        if (camera is PerspectiveCamera persp)
        {
            double zn = Math.Max(1e-4, persp.NearPlaneDistance);
            double zf = persp.FarPlaneDistance;
            if (!double.IsInfinity(zf) && zf <= zn)
            {
                zf = zn + 1e-3;
            }

            return persp.GetProjectionMatrix(aspectRatio, zn, zf);
        }

        return camera.GetProjectionMatrix(aspectRatio);
    }

    private static Point3D ExtractCameraWorldPosition(Camera camera, ref Matrix3D viewMatrix)
    {
        if (camera is ProjectionCamera pc)
        {
            Point3D pos = pc.Position;
            if (pc.Transform != null && !pc.Transform.Value.IsIdentity)
            {
                pos = pc.Transform.Transform(pos);
            }
            return pos;
        }

        Matrix3D invView = viewMatrix;
        if (invView.HasInverse)
        {
            invView.Invert();
            return new Point3D(invView.OffsetX, invView.OffsetY, invView.OffsetZ);
        }

        return new Point3D(0, 0, 0);
    }

    private static void WriteMatrixRowMajor(in Matrix3D m, float* dst)
    {
        dst[0] = (float)m.M11;
        dst[1] = (float)m.M12;
        dst[2] = (float)m.M13;
        dst[3] = (float)m.M14;

        dst[4] = (float)m.M21;
        dst[5] = (float)m.M22;
        dst[6] = (float)m.M23;
        dst[7] = (float)m.M24;

        dst[8] = (float)m.M31;
        dst[9] = (float)m.M32;
        dst[10] = (float)m.M33;
        dst[11] = (float)m.M34;

        dst[12] = (float)m.OffsetX;
        dst[13] = (float)m.OffsetY;
        dst[14] = (float)m.OffsetZ;
        dst[15] = (float)m.M44;
    }

    internal static double ComputeDeterminant3x3(in Matrix3D m)
    {
        return m.M11 * (m.M22 * m.M33 - m.M23 * m.M32)
             - m.M12 * (m.M21 * m.M33 - m.M23 * m.M31)
             + m.M13 * (m.M21 * m.M32 - m.M22 * m.M31);
    }

    private static void ComputeNormalMatrix(in Matrix3D m, float* dst)
    {
        double det = ComputeDeterminant3x3(in m);

        if (Math.Abs(det) < 1e-18)
        {
            dst[0] = 1f; dst[1] = 0f; dst[2] = 0f; dst[3] = 0f;
            dst[4] = 0f; dst[5] = 1f; dst[6] = 0f; dst[7] = 0f;
            dst[8] = 0f; dst[9] = 0f; dst[10] = 1f; dst[11] = 0f;
            dst[12] = 0f; dst[13] = 0f; dst[14] = 0f; dst[15] = 1f;
            return;
        }

        double invDet = 1.0 / det;
        // Row-major (M^-1)^T equals the cofactor matrix divided by det.
        dst[0] = (float)((m.M22 * m.M33 - m.M23 * m.M32) * invDet);
        dst[1] = (float)(-(m.M21 * m.M33 - m.M23 * m.M31) * invDet);
        dst[2] = (float)((m.M21 * m.M32 - m.M22 * m.M31) * invDet);
        dst[3] = 0f;

        dst[4] = (float)(-(m.M12 * m.M33 - m.M13 * m.M32) * invDet);
        dst[5] = (float)((m.M11 * m.M33 - m.M13 * m.M31) * invDet);
        dst[6] = (float)(-(m.M11 * m.M32 - m.M12 * m.M31) * invDet);
        dst[7] = 0f;

        dst[8] = (float)((m.M12 * m.M23 - m.M13 * m.M22) * invDet);
        dst[9] = (float)(-(m.M11 * m.M23 - m.M13 * m.M21) * invDet);
        dst[10] = (float)((m.M11 * m.M22 - m.M12 * m.M21) * invDet);
        dst[11] = 0f;

        dst[12] = 0f;
        dst[13] = 0f;
        dst[14] = 0f;
        dst[15] = 1f;
    }

    private void ReleaseDeviceResources()
    {
        for (int i = 0; i < _brushPool.Count; i++)
            _brushPool[i].Dispose();
        _brushPool.Clear();

        _d2dTargetBitmap.Dispose();
        _d2dTargetBitmapContext = null;
        _rtView.Dispose();
        _rtTexture.Dispose();
        _dsView.Dispose();
        _dsTexture.Dispose();
        _rtWidth = 0;
        _rtHeight = 0;

        for (int i = 0; i < _samplers.Length; i++)
            _samplers[i].Dispose();

        _whiteSrv.Dispose();
        _whiteTexture.Dispose();
        _blendPremul.Dispose();
        _dssDepthReadOnly.Dispose();
        _dssDepthWrite.Dispose();
        _rsCullNone.Dispose();
        _rsCullFront.Dispose();
        _rsCullBack.Dispose();
        _indexBuffer.Dispose();
        _indexBufferBytes = 0;
        _vertexBuffer.Dispose();
        _vertexBufferBytes = 0;
        _constantBuffer.Dispose();
        _inputLayout.Dispose();
        _pixelShader.Dispose();
        _vertexShader.Dispose();
    }

    public void Dispose()
    {
        lock (_renderLock)
        {
            ReleaseDeviceResources();
            _deviceGeneration = -1;
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System.Collections.Generic;
using System.Windows.Media.Effects;
using CSUI.Core.Interop.DirectX;
using MS.Internal;

namespace System.Windows.Media
{
    public abstract partial class Visual
    {
        private VisualProxyFlags _proxyFlags;

        // Clean forward-decoupling hooks wired by Stage C3 (UIElement, HwndTarget, PresentationSource, PointUtil).
        internal virtual bool IsUIElementRoot => false;
        internal static Action<Visual, Visual> PropagateResumeLayoutHook { get; set; }
        internal static Action<Visual> PropagateSuspendLayoutHook { get; set; }
        internal static Action<Visual, DependencyProperty> InvalidateForceInheritHook { get; set; }
        internal static Func<bool> IsPerMonitorDpiAwareHook { get; set; }
        internal static Func<bool> IsPerMonitorDpiScalingEnabledHook { get; set; }
        internal static Func<DependencyObject, object> PresentationSourceFromVisualHook { get; set; }
        internal static Func<Point, object, Point> PointToScreenHook { get; set; }
        internal static Func<Point, object, Point> PointFromScreenHook { get; set; }
        internal static Func<Visual, Visual, DependencyObject, bool> ShouldAutoLayoutVisualHook { get; set; }
        internal static Action<Visual, EventHandler> SubscribeLayoutUpdatedHook { get; set; }
        internal static Action<Visual, EventHandler> UnsubscribeLayoutUpdatedHook { get; set; }
        internal static Action<Visual, DependencyObject> PerformAutoLayoutHook { get; set; }
        internal static Action NotifyHitTestInvalidatedHook { get; set; }

        internal void SetFlagsOnAllChannels(bool value, VisualProxyFlags flagsToChange)
        {
            _proxyFlags = value ? (_proxyFlags | flagsToChange) : (_proxyFlags & (~flagsToChange));
        }

        internal void SetFlags(bool value, VisualProxyFlags flagsToChange)
        {
            SetFlagsOnAllChannels(value, flagsToChange);
        }

        internal bool CheckFlagsOnAllChannels(VisualProxyFlags flagsToCheck)
        {
            return (_proxyFlags & flagsToCheck) == flagsToCheck;
        }

        internal bool CheckFlagsAnd(VisualProxyFlags flagsToCheck)
        {
            return (_proxyFlags & flagsToCheck) == flagsToCheck;
        }

        internal bool CheckFlagsOr(VisualProxyFlags flagsToCheck)
        {
            return (flagsToCheck == VisualProxyFlags.None) || ((_proxyFlags & flagsToCheck) != VisualProxyFlags.None);
        }

        internal virtual unsafe void RenderContentD2D(void* d2dContext)
        {
        }

        internal unsafe void RenderRecursive(RenderContext ctx)
        {
            if (ctx == null)
            {
                return;
            }

            void* d2dContext = ctx.D2dDeviceContext;
            if (d2dContext == null)
            {
                return;
            }

            void* d2dFactory = ctx.D2dFactory;
            if (d2dFactory == null)
            {
                d2dFactory = MediaSystem.EnsureSharedDevice().D2DFactory;
            }

            double opacity = Math.Clamp(VisualOpacity, 0.0, 1.0);
            if (opacity <= 0.0)
            {
                SetFlagsOnAllChannels(false, c_ProxyFlagsDirtyMask | VisualProxyFlags.IsSubtreeDirtyForRender);
                return;
            }

            D2D1_MATRIX_3X2_F savedTransform = default;
            bool pushedTransform = false;
            bool pushedScrollClip = false;
            int clipLayerOrAxis = 0; // 0 = none, 1 = axis-aligned clip, 2 = geometric layer
            bool pushedOpacityLayer = false;

            try
            {
                Transform transform = VisualTransform;
                Vector offset = VisualOffset;
                bool hasTransform = transform != null && !transform.IsIdentity;
                bool hasOffset = !DoubleUtil.IsZero(offset.X) || !DoubleUtil.IsZero(offset.Y);

                if (hasTransform || hasOffset)
                {
                    ID2D1DeviceContextExVtbl.GetTransform(d2dContext, out savedTransform);
                    pushedTransform = true;

                    if (!hasTransform)
                    {
                        D2D1_MATRIX_3X2_F nextTransform = savedTransform;
                        nextTransform.M31 += (float)(offset.X * savedTransform.M11 + offset.Y * savedTransform.M21);
                        nextTransform.M32 += (float)(offset.X * savedTransform.M12 + offset.Y * savedTransform.M22);
                        ID2D1DeviceContextVtbl.SetTransform(d2dContext, nextTransform);
                    }
                    else
                    {
                        Matrix local = transform.Value;
                        if (hasOffset)
                        {
                            local.Translate(offset.X, offset.Y);
                        }

                        Matrix world = Matrix.Multiply(local, MILUtilities.ConvertFromD2DMatrix(savedTransform));
                        ID2D1DeviceContextVtbl.SetTransform(d2dContext, MILUtilities.ConvertToD2DMatrix(world));
                    }
                }

                Rect? scrollClip = VisualScrollableAreaClip;
                if (scrollClip.HasValue && !scrollClip.Value.IsEmpty)
                {
                    ID2D1DeviceContextVtbl.PushAxisAlignedClip(
                        d2dContext,
                        Direct2DRenderWalker.ToD2DRect(scrollClip.Value),
                        DxConst.D2D1_ANTIALIAS_MODE_PER_PRIMITIVE);
                    pushedScrollClip = true;
                }

                Geometry clip = VisualClip;
                if (clip != null)
                {
                    if (clip is RectangleGeometry rectGeom &&
                        (rectGeom.Transform == null || rectGeom.Transform.IsIdentity) &&
                        rectGeom.RadiusX == 0.0 &&
                        rectGeom.RadiusY == 0.0 &&
                        !rectGeom.Rect.IsEmpty)
                    {
                        ID2D1DeviceContextVtbl.PushAxisAlignedClip(
                            d2dContext,
                            Direct2DRenderWalker.ToD2DRect(rectGeom.Rect),
                            DxConst.D2D1_ANTIALIAS_MODE_PER_PRIMITIVE);
                        clipLayerOrAxis = 1;
                    }
                    else if (d2dFactory != null)
                    {
                        void* d2dClipGeom = clip.CreateD2DGeometry(d2dFactory);
                        if (d2dClipGeom != null)
                        {
                            try
                            {
                                D2D1_LAYER_PARAMETERS1 clipParams = Direct2DRenderWalker.CreateDefaultLayerParameters();
                                clipParams.GeometricMask = d2dClipGeom;
                                ID2D1DeviceContextVtbl.PushLayer(d2dContext, clipParams, null);
                                clipLayerOrAxis = 2;
                            }
                            finally
                            {
                                ComHelpers.Release(d2dClipGeom);
                            }
                        }
                    }
                }

                Brush opacityMask = VisualOpacityMask;
                if (opacity < 1.0 || opacityMask != null)
                {
                    void* d2dMaskBrush = null;
                    if (opacityMask != null)
                    {
                        Rect maskBounds = CalculateSubgraphBoundsInnerSpace();
                        if (maskBounds.IsEmpty || maskBounds.Width <= 0.0 || maskBounds.Height <= 0.0)
                        {
                            maskBounds = new Rect(0, 0, 1, 1);
                        }
                        d2dMaskBrush = opacityMask.CreateD2DBrush(d2dContext, maskBounds);
                    }

                    try
                    {
                        D2D1_LAYER_PARAMETERS1 layerParams = Direct2DRenderWalker.CreateDefaultLayerParameters();
                        layerParams.Opacity = (float)opacity;
                        layerParams.OpacityBrush = d2dMaskBrush;
                        ID2D1DeviceContextVtbl.PushLayer(d2dContext, layerParams, null);
                        pushedOpacityLayer = true;
                    }
                    finally
                    {
                        if (d2dMaskBrush != null)
                        {
                            ComHelpers.Release(d2dMaskBrush);
                        }
                    }
                }

                Effect effect = VisualEffect;
                CacheMode cacheMode = VisualCacheMode;
                if (cacheMode is BitmapCache bitmapCache)
                {
                    RenderSubtreeWithBitmapCache(ctx, d2dContext, bitmapCache);
                }
                else if (effect != null)
                {
                    RenderSubtreeWithEffect(ctx, d2dContext, effect);
                }
                else
                {
                    RenderContentD2D(d2dContext);
                    RenderChildrenD2D(ctx);
                }

                SetFlagsOnAllChannels(false, c_ProxyFlagsDirtyMask | VisualProxyFlags.IsSubtreeDirtyForRender | VisualProxyFlags.IsChildrenZOrderDirty);
            }
            finally
            {
                if (pushedOpacityLayer)
                {
                    ID2D1DeviceContextVtbl.PopLayer(d2dContext);
                }

                if (clipLayerOrAxis == 1)
                {
                    ID2D1DeviceContextVtbl.PopAxisAlignedClip(d2dContext);
                }
                else if (clipLayerOrAxis == 2)
                {
                    ID2D1DeviceContextVtbl.PopLayer(d2dContext);
                }

                if (pushedScrollClip)
                {
                    ID2D1DeviceContextVtbl.PopAxisAlignedClip(d2dContext);
                }

                if (pushedTransform)
                {
                    ID2D1DeviceContextVtbl.SetTransform(d2dContext, savedTransform);
                }
            }
        }

        private unsafe void RenderSubtreeWithEffect(RenderContext ctx, void* d2dContext, Effect effect)
        {
            void* commandList = null;
            void* prevTarget = null;
            if (ID2D1DeviceContextVtbl.CreateCommandList(d2dContext, &commandList) < 0 || commandList == null)
            {
                RenderContentD2D(d2dContext);
                RenderChildrenD2D(ctx);
                return;
            }

            ID2D1DeviceContextExVtbl.GetTransform(d2dContext, out D2D1_MATRIX_3X2_F worldTransform);
            ID2D1DeviceContextVtbl.GetTarget(d2dContext, &prevTarget);
            bool switchedTarget = false;
            try
            {
                ID2D1DeviceContextVtbl.SetTarget(d2dContext, commandList);
                switchedTarget = true;
                ID2D1DeviceContextVtbl.SetTransform(d2dContext, new D2D1_MATRIX_3X2_F { M11 = 1.0f, M22 = 1.0f });

                RenderContentD2D(d2dContext);
                RenderChildrenD2D(ctx);

                ID2D1CommandListVtbl.Close(commandList);
            }
            finally
            {
                if (switchedTarget)
                {
                    ID2D1DeviceContextVtbl.SetTarget(d2dContext, prevTarget);
                }

                ID2D1DeviceContextVtbl.SetTransform(d2dContext, worldTransform);
                if (prevTarget != null)
                {
                    ComHelpers.Release(prevTarget);
                }
            }

            try
            {
                effect.DrawD2DWithInput(d2dContext, commandList);
            }
            finally
            {
                ComHelpers.Release(commandList);
            }
        }

        private unsafe void RenderSubtreeWithBitmapCache(RenderContext ctx, void* d2dContext, BitmapCache bitmapCache)
        {
            string key = $"BitmapCache_{GetHashCode()}";
            bool isDirty = CheckFlagsAnd(VisualProxyFlags.IsSubtreeDirtyForRender);

            if (!isDirty)
            {
                void* cachedImage = GpuResourceCache.Instance.TryGetTexture(key);
                if (cachedImage != null)
                {
                    // Instant single-call DrawImage GPU blit, skipping all child rendering!
                    ID2D1DeviceContextVtbl.DrawImage(
                        d2dContext,
                        cachedImage,
                        null,
                        null,
                        DxConst.D2D1_INTERPOLATION_MODE_LINEAR,
                        DxConst.D2D1_COMPOSITE_MODE_SOURCE_OVER);
                    return;
                }
            }

            void* commandList = null;
            void* prevTarget = null;
            if (ID2D1DeviceContextVtbl.CreateCommandList(d2dContext, &commandList) < 0 || commandList == null)
            {
                RenderContentD2D(d2dContext);
                RenderChildrenD2D(ctx);
                return;
            }

            ID2D1DeviceContextVtbl.GetTarget(d2dContext, &prevTarget);
            bool switchedTarget = false;
            try
            {
                ID2D1DeviceContextVtbl.SetTarget(d2dContext, commandList);
                switchedTarget = true;
                ID2D1DeviceContextExVtbl.GetTransform(d2dContext, out D2D1_MATRIX_3X2_F curTransform);
                ID2D1DeviceContextVtbl.SetTransform(d2dContext, new D2D1_MATRIX_3X2_F { M11 = 1.0f, M22 = 1.0f });

                RenderContentD2D(d2dContext);
                RenderChildrenD2D(ctx);

                ID2D1CommandListVtbl.Close(commandList);
                ID2D1DeviceContextVtbl.SetTransform(d2dContext, curTransform);
            }
            finally
            {
                if (switchedTarget)
                {
                    ID2D1DeviceContextVtbl.SetTarget(d2dContext, prevTarget);
                }
            }

            ID2D1DeviceContextVtbl.DrawImage(
                d2dContext,
                commandList,
                null,
                null,
                DxConst.D2D1_INTERPOLATION_MODE_LINEAR,
                DxConst.D2D1_COMPOSITE_MODE_SOURCE_OVER);

            Rect bounds = CalculateSubgraphBoundsInnerSpace();
            int w = (int)Math.Max(1, Math.Ceiling(bounds.Width));
            int h = (int)Math.Max(1, Math.Ceiling(bounds.Height));
            GpuResourceCache.Instance.RegisterTexture(key, commandList, w, h);
        }

        private void RenderChildrenD2D(RenderContext ctx)
        {
            int count = InternalVisualChildrenCount;
            for (int i = 0; i < count; i++)
            {
                Visual child = InternalGetVisualChild(i);
                child?.RenderRecursive(ctx);
            }
        }

        internal static DpiScale EnsureDpiScaleFromDpiUtil()
        {
            DpiScale2 dpi = DpiUtil.GetSystemDpiFromUIElementCache();
            return new DpiScale(dpi.DpiScaleX, dpi.DpiScaleY);
        }
    }
}

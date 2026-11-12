// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System.Diagnostics;
using System.Threading;
using System.Windows.Threading;
using CSUI.Core.Interop;
using CSUI.Core.Interop.DirectX;
using SR = MS.Internal.PresentationCore.SR;

namespace System.Windows.Media
{
    /// <summary>
    /// Hosts a Visual subtree rendered on another Dispatcher thread via VisualTarget,
    /// compositing its Direct2D/DXGI surface seamlessly into this Visual tree.
    /// </summary>
    public class HostVisual : ContainerVisual
    {
        //----------------------------------------------------------------------
        //
        //  Constructors
        //
        //----------------------------------------------------------------------

        #region Constructors

        public HostVisual()
        {
        }

        #endregion Constructors

        //----------------------------------------------------------------------
        //
        //  Protected Methods
        //
        //----------------------------------------------------------------------

        #region Protected Methods

        /// <summary>
        /// HostVisual never reports itself as being hit directly unless subclassed.
        /// </summary>
        protected override HitTestResult HitTestCore(PointHitTestParameters hitTestParameters)
        {
            return null;
        }

        /// <summary>
        /// HostVisual never reports itself as being hit directly unless subclassed.
        /// </summary>
        protected override GeometryHitTestResult HitTestCore(GeometryHitTestParameters hitTestParameters)
        {
            return null;
        }

        protected internal override void OnVisualParentChanged(DependencyObject oldParent)
        {
            base.OnVisualParentChanged(oldParent);

            if (InternalVisualParent == null)
            {
                ReleaseBitmapCache();
            }
            else
            {
                SyncHostDpiToTarget(GetDpi());
            }
        }

        protected override void OnDpiChanged(DpiScale oldDpi, DpiScale newDpi)
        {
            base.OnDpiChanged(oldDpi, newDpi);
            SyncHostDpiToTarget(newDpi);
        }

        #endregion Protected Methods

        //----------------------------------------------------------------------
        //
        //  Internal Methods
        //
        //----------------------------------------------------------------------

        #region Internal Methods

        internal override Rect GetContentBounds()
        {
            return Rect.Empty;
        }

        internal override unsafe void RenderContentD2D(void* d2dContext)
        {
            if (d2dContext == null)
                return;

            SyncHostDpiToTarget(GetDpi());

            DxOffscreenSurfaceSlot frame;
            lock (_surfaceLock)
            {
                frame = _frontFrame;
                frame?.AddRef();
            }

            if (frame == null)
                return;

            try
            {
                void* texturePtr = frame.TexturePtr;
                if (texturePtr == null || frame.ContentPixelWidth == 0 || frame.ContentPixelHeight == 0)
                    return;

                if (frame.DeviceGeneration != frame.SharedDevice.DeviceGeneration)
                    return;

                void* drawBitmap = GetOrCreateHostBitmap(d2dContext, frame);
                if (drawBitmap == null)
                    return;

                double dipW = frame.ContentPixelWidth * 96.0 / frame.DpiX;
                double dipH = frame.ContentPixelHeight * 96.0 / frame.DpiY;
                var srcRect = new D2D1_RECT_F(0f, 0f, (float)dipW, (float)dipH);
                var dstRect = new D2D1_RECT_F(
                    (float)frame.OriginDipX,
                    (float)frame.OriginDipY,
                    (float)(frame.OriginDipX + dipW),
                    (float)(frame.OriginDipY + dipH));

                ID2D1DeviceContextExVtbl.DrawBitmap(
                    d2dContext,
                    drawBitmap,
                    &dstRect,
                    1.0f,
                    DxConst.D2D1_INTERPOLATION_MODE_LINEAR,
                    &srcRect,
                    null);
            }
            finally
            {
                frame.Release();
            }
        }

        internal void BeginHosting(VisualTarget target)
        {
            ArgumentNullException.ThrowIfNull(target);
            Debug.Assert(target.Dispatcher.Thread == Thread.CurrentThread);

            DpiScale dpiSnapshot;
            lock (_surfaceLock)
            {
                if (_target != null)
                {
                    throw new InvalidOperationException(SR.VisualTarget_AnotherTargetAlreadyConnected);
                }

                _target = target;
                dpiSnapshot = new DpiScale(_hostDpiX / 96.0, _hostDpiY / 96.0);
            }

            target.UpdateHostDpi(dpiSnapshot);
            ScheduleHostInvalidate();
        }

        internal void EndHosting()
        {
            DxOffscreenSurfaceSlot oldFront;
            lock (_surfaceLock)
            {
                Debug.Assert(_target != null);
                Debug.Assert(_target.Dispatcher.Thread == Thread.CurrentThread);

                oldFront = _frontFrame;
                _frontFrame = null;
                _target = null;
            }

            oldFront?.Release();
            ScheduleHostInvalidate();
        }

        internal void PublishFrame(DxOffscreenSurfaceSlot newFrame)
        {
            if (newFrame != null)
            {
                newFrame.AddRef();
            }

            DxOffscreenSurfaceSlot oldFront;
            lock (_surfaceLock)
            {
                oldFront = _frontFrame;
                _frontFrame = newFrame;
            }

            oldFront?.Release();
            ScheduleHostInvalidate();
        }

        internal DpiScale GetHostDpiSnapshot()
        {
            lock (_surfaceLock)
            {
                return new DpiScale(_hostDpiX / 96.0, _hostDpiY / 96.0);
            }
        }

        internal DxOffscreenSurfaceSlot TryAcquireFrontFrame()
        {
            lock (_surfaceLock)
            {
                _frontFrame?.AddRef();
                return _frontFrame;
            }
        }

        #endregion Internal Methods

        //----------------------------------------------------------------------
        //
        //  Private Methods
        //
        //----------------------------------------------------------------------

        #region Private Methods

        private void SyncHostDpiToTarget(DpiScale dpi)
        {
            VisualTarget target;
            bool changed = false;
            lock (_surfaceLock)
            {
                if (dpi.PixelsPerInchX > 0 && dpi.PixelsPerInchY > 0 &&
                    (Math.Abs(_hostDpiX - dpi.PixelsPerInchX) > 0.01 ||
                     Math.Abs(_hostDpiY - dpi.PixelsPerInchY) > 0.01))
                {
                    _hostDpiX = dpi.PixelsPerInchX;
                    _hostDpiY = dpi.PixelsPerInchY;
                    changed = true;
                }

                target = _target;
            }

            if (changed && target != null)
            {
                target.UpdateHostDpi(dpi);
            }
        }

        private void ScheduleHostInvalidate()
        {
            if (CheckAccess())
            {
                Invalidate();
            }
            else if (!Dispatcher.HasShutdownStarted && !Dispatcher.HasShutdownFinished)
            {
                Dispatcher.BeginInvoke(
                    DispatcherPriority.Normal,
                    static void (object args) => ((HostVisual)args).Invalidate(),
                    this);
            }
        }

        private void Invalidate()
        {
            if (Dispatcher.HasShutdownStarted || Dispatcher.HasShutdownFinished)
                return;

            SetFlagsOnAllChannels(true, VisualProxyFlags.IsContentDirty);
            PropagateChangedFlags();
        }

        private unsafe void* GetOrCreateHostBitmap(void* d2dContext, DxOffscreenSurfaceSlot frame)
        {
            void* texPtr = frame.TexturePtr;
            long gen = frame.DeviceGeneration;

            // Search tiny 3-slot cache matching VisualTarget's triple buffer.
            for (int i = 0; i < _bitmapCache.Length; i++)
            {
                ref CachedBitmapEntry entry = ref _bitmapCache[i];
                if (entry.Bitmap != null &&
                    entry.D2dContext == d2dContext &&
                    entry.Texture == texPtr &&
                    entry.DeviceGeneration == gen &&
                    Math.Abs(entry.DpiX - frame.DpiX) < 0.01f &&
                    Math.Abs(entry.DpiY - frame.DpiY) < 0.01f)
                {
                    return entry.Bitmap;
                }
            }

            // Allocate new D2D1Bitmap1 backed by the shared DXGI surface on this D2D device context.
            using var dxgiSurface = new ComPtr<byte>();
            if (ComPtr<byte>.QueryInterface(texPtr, DxGuids.IID_IDXGISurface, (void**)dxgiSurface.AddressOf()) < 0)
                return null;

            var props = new D2D1_BITMAP_PROPERTIES1
            {
                PixelFormat = new D2D1_PIXEL_FORMAT
                {
                    Format = DxConst.DXGI_FORMAT_B8G8R8A8_UNORM,
                    AlphaMode = DxConst.D2D1_ALPHA_MODE_PREMULTIPLIED,
                },
                DpiX = frame.DpiX,
                DpiY = frame.DpiY,
                BitmapOptions = DxConst.D2D1_BITMAP_OPTIONS_NONE,
                ColorContext = null,
            };

            void* bmpPtr = null;
            if (ID2D1DeviceContextVtbl.CreateBitmapFromDxgiSurface(d2dContext, dxgiSurface.Ptr, props, &bmpPtr) < 0 || bmpPtr == null)
                return null;

            int replaceIdx = -1;
            for (int i = 0; i < _bitmapCache.Length; i++)
            {
                if (_bitmapCache[i].Bitmap == null || _bitmapCache[i].DeviceGeneration != gen || _bitmapCache[i].D2dContext != d2dContext)
                {
                    replaceIdx = i;
                    break;
                }
            }

            if (replaceIdx < 0)
            {
                replaceIdx = _nextCacheEvict;
                _nextCacheEvict = (_nextCacheEvict + 1) % _bitmapCache.Length;
            }

            if (_bitmapCache[replaceIdx].Bitmap != null)
            {
                ComHelpers.Release(_bitmapCache[replaceIdx].Bitmap);
            }

            _bitmapCache[replaceIdx] = new CachedBitmapEntry
            {
                D2dContext = d2dContext,
                Texture = texPtr,
                Bitmap = bmpPtr,
                DeviceGeneration = gen,
                DpiX = frame.DpiX,
                DpiY = frame.DpiY,
            };

            return bmpPtr;
        }

        private unsafe void ReleaseBitmapCache()
        {
            for (int i = 0; i < _bitmapCache.Length; i++)
            {
                if (_bitmapCache[i].Bitmap != null)
                {
                    ComHelpers.Release(_bitmapCache[i].Bitmap);
                    _bitmapCache[i] = default;
                }
            }
        }

        #endregion Private Methods

        //----------------------------------------------------------------------
        //
        //  Private Fields
        //
        //----------------------------------------------------------------------

        #region Private Fields

        private unsafe struct CachedBitmapEntry
        {
            public void* D2dContext;
            public void* Texture;
            public void* Bitmap;
            public long DeviceGeneration;
            public float DpiX;
            public float DpiY;
        }

        private readonly object _surfaceLock = new object();
        private VisualTarget _target;
        private DxOffscreenSurfaceSlot _frontFrame;
        private double _hostDpiX = 96.0;
        private double _hostDpiY = 96.0;

        // Accessed solely on HostVisual's owning Dispatcher thread during rendering / cleanup.
        private readonly CachedBitmapEntry[] _bitmapCache = new CachedBitmapEntry[3];
        private int _nextCacheEvict;

        #endregion Private Fields
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Description: D3DImage class
//              An ImageSource that displays a user created D3D surface

#nullable disable

using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using CSUI.Core.Interop.DirectX;
using MS.Internal;
using MS.Internal.KnownBoxes;
using SR = MS.Internal.PresentationCore.SR;

namespace System.Windows.Interop
{
    /// <summary>
    ///     Specifies the acceptable resources for SetBackBuffer.
    /// </summary>
    public enum D3DResourceType
    {
        IDirect3DSurface9
    }

    public class D3DImage : ImageSource
    {
        private enum SurfaceKind
        {
            None = 0,
            Direct3DSurface9 = 1,
            DxgiSurface = 2,
            D3D11Texture2D = 3,
        }

        private const uint D3DFMT_A8R8G8B8 = 21;
        private const uint D3DFMT_X8R8G8B8 = 22;
        private const uint D3DLOCK_READONLY = 0x00000010;
        private const uint DXGI_MAP_READ = 1;
        private const uint D3D11_USAGE_STAGING = 3;
        private const uint D3D11_CPU_ACCESS_READ = 0x20000;
        private const uint D3D11_MAP_READ = 1;
        private const uint DXGI_FORMAT_R8G8B8A8_UNORM = 28;
        private const uint DXGI_FORMAT_B8G8R8A8_UNORM = 87;
        private const uint DXGI_FORMAT_B8G8R8X8_UNORM = 88;

        private static readonly Guid s_iidIDirect3DSurface9 = new("0cfbaf3a-9ff6-429a-99b3-a2796af8b89b");

        [StructLayout(LayoutKind.Sequential)]
        private struct D3DSURFACE_DESC
        {
            public uint Format;
            public uint Type;
            public uint Usage;
            public uint Pool;
            public uint MultiSampleType;
            public uint MultiSampleQuality;
            public uint Width;
            public uint Height;
        }

        [StructLayout(LayoutKind.Sequential)]
        private unsafe struct D3DLOCKED_RECT
        {
            public int Pitch;
            public void* pBits;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct DXGI_SURFACE_DESC
        {
            public uint Width;
            public uint Height;
            public uint Format;
            public uint SampleCount;
            public uint SampleQuality;
        }

        [StructLayout(LayoutKind.Sequential)]
        private unsafe struct DXGI_MAPPED_RECT
        {
            public int Pitch;
            public byte* pBits;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct D3D11_TEXTURE2D_DESC
        {
            public uint Width;
            public uint Height;
            public uint MipLevels;
            public uint ArraySize;
            public uint Format;
            public uint SampleCount;
            public uint SampleQuality;
            public uint Usage;
            public uint BindFlags;
            public uint CPUAccessFlags;
            public uint MiscFlags;
        }

        [StructLayout(LayoutKind.Sequential)]
        private unsafe struct D3D11_MAPPED_SUBRESOURCE
        {
            public void* pData;
            public uint RowPitch;
            public uint DepthPitch;
        }

        static D3DImage()
        {
            IsFrontBufferAvailablePropertyKey =
                DependencyProperty.RegisterReadOnly(
                    "IsFrontBufferAvailable",
                    typeof(bool),
                    typeof(D3DImage),
                    new UIPropertyMetadata(
                        BooleanBoxes.TrueBox,
                        new PropertyChangedCallback(IsFrontBufferAvailablePropertyChanged)
                        )
                    );

            IsFrontBufferAvailableProperty = IsFrontBufferAvailablePropertyKey.DependencyProperty;
        }

        /// <summary>
        ///     Default constructor, sets DPI to 96.0
        /// </summary>
        public D3DImage() : this(96.0, 96.0)
        {
        }

        /// <summary>
        ///     DPI constructor
        /// </summary>
        public D3DImage(double dpiX, double dpiY)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(dpiX);
            ArgumentOutOfRangeException.ThrowIfNegative(dpiY);

            _dpiX = dpiX;
            _dpiY = dpiY;
        }

        ~D3DImage()
        {
            ReleaseCachedResources(fromFinalizer: true);
        }

        /// <summary>
        ///     Sets the back buffer for the D3DImage.
        /// </summary>
        public void SetBackBuffer(D3DResourceType backBufferType, IntPtr backBuffer)
        {
            SetBackBuffer(backBufferType, backBuffer, false);
        }

        /// <summary>
        ///     Sets the back buffer for the D3DImage.
        /// </summary>
        public unsafe void SetBackBuffer(D3DResourceType backBufferType, IntPtr backBuffer, bool enableSoftwareFallback)
        {
            WritePreamble();

            if (_lockCount == 0)
            {
                throw new InvalidOperationException(SR.Image_MustBeLocked);
            }

            if (backBufferType != D3DResourceType.IDirect3DSurface9)
            {
                throw new ArgumentOutOfRangeException(nameof(backBufferType));
            }

            if (backBuffer != IntPtr.Zero && backBuffer == _pUserSurfaceUnsafe)
            {
                return;
            }

            void* newSurface = null;
            SurfaceKind newKind = SurfaceKind.None;
            uint newPixelWidth = 0;
            uint newPixelHeight = 0;
            uint newFormat = 0;

            if (backBuffer != IntPtr.Zero)
            {
                AcquireSurface(backBuffer, out newSurface, out newKind, out newPixelWidth, out newPixelHeight, out newFormat);
            }

            lock (_surfaceLock)
            {
                ReleaseSurfaceLocked(fromFinalizer: false);

                _surfacePtr = (IntPtr)newSurface;
                _surfaceKind = newKind;
                _surfaceFormat = newFormat;
                _pUserSurfaceUnsafe = backBuffer;
                _pixelWidth = newPixelWidth;
                _pixelHeight = newPixelHeight;
                _isSoftwareFallbackEnabled = enableSoftwareFallback;
                _isDirty = false;
                _pendingDirtyRect = Int32Rect.Empty;

                if (newSurface == null)
                {
                    _frontBufferPixels = null;
                    _frontBufferWidth = 0;
                    _frontBufferHeight = 0;
                    InvalidateD2DCacheLocked(fromFinalizer: false);
                    _isChangePending = true;
                }
                else
                {
                    // Trigger front buffer snapshot on Unlock even if AddDirtyRect wasn't called yet
                    _isDirty = true;
                    _pendingDirtyRect = new Int32Rect(0, 0, (int)newPixelWidth, (int)newPixelHeight);
                    _isChangePending = true;
                }

                ++_version;
            }
        }

        /// <summary>
        ///     Locks the D3DImage.
        /// </summary>
        public void Lock()
        {
            WritePreamble();
            LockImpl(Duration.Forever);
        }

        /// <summary>
        ///     Tries to lock the D3DImage, giving up once duration expires.
        /// </summary>
        public bool TryLock(Duration timeout)
        {
            WritePreamble();

            if (timeout == Duration.Automatic)
            {
                throw new ArgumentOutOfRangeException(nameof(timeout));
            }

            return LockImpl(timeout);
        }

        /// <summary>
        ///     Unlocks the D3DImage.
        /// </summary>
        public void Unlock()
        {
            WritePreamble();

            if (_lockCount == 0)
            {
                throw new InvalidOperationException(SR.Image_MustBeLocked);
            }

            --_lockCount;

            if (_lockCount == 0)
            {
                if (_isDirty)
                {
                    SnapshotFrontBuffer();
                    _isDirty = false;
                    _pendingDirtyRect = Int32Rect.Empty;
                }

                if (_isChangePending)
                {
                    _isChangePending = false;
                    WritePostscript();
                }

                Monitor.Exit(_lockSync);
            }
        }

        /// <summary>
        ///     Adds a dirty rect to the D3DImage.
        /// </summary>
        public void AddDirtyRect(Int32Rect dirtyRect)
        {
            WritePreamble();

            if (_lockCount == 0)
            {
                throw new InvalidOperationException(SR.Image_MustBeLocked);
            }

            if (_surfacePtr == IntPtr.Zero)
            {
                throw new InvalidOperationException(SR.D3DImage_MustHaveBackBuffer);
            }

            dirtyRect.ValidateForDirtyRect(nameof(dirtyRect), PixelWidth, PixelHeight);
            if (dirtyRect.HasArea)
            {
                if (_pendingDirtyRect.IsEmpty)
                {
                    _pendingDirtyRect = dirtyRect;
                }
                else
                {
                    int x1 = Math.Min(_pendingDirtyRect.X, dirtyRect.X);
                    int y1 = Math.Min(_pendingDirtyRect.Y, dirtyRect.Y);
                    int x2 = Math.Max(_pendingDirtyRect.X + _pendingDirtyRect.Width, dirtyRect.X + dirtyRect.Width);
                    int y2 = Math.Max(_pendingDirtyRect.Y + _pendingDirtyRect.Height, dirtyRect.Y + dirtyRect.Height);
                    _pendingDirtyRect = new Int32Rect(x1, y1, x2 - x1, y2 - y1);
                }

                _isDirty = true;
                _isChangePending = true;
            }
        }

        /// <summary>
        ///     When true, a front buffer exists and back buffer updates will be copied forward.
        /// </summary>
        public bool IsFrontBufferAvailable
        {
            get
            {
                return (bool)GetValue(IsFrontBufferAvailableProperty);
            }
        }

        public static readonly DependencyProperty IsFrontBufferAvailableProperty;

        /// <summary>
        ///     Event that fires when IsFrontBufferAvailable changes.
        /// </summary>
        public event DependencyPropertyChangedEventHandler IsFrontBufferAvailableChanged
        {
            add
            {
                WritePreamble();

                if (value != null)
                {
                    _isFrontBufferAvailableChangedHandlers += value;
                }
            }

            remove
            {
                WritePreamble();

                if (value != null)
                {
                    _isFrontBufferAvailableChangedHandlers -= value;
                }
            }
        }

        /// <summary>
        ///     Width in pixels.
        /// </summary>
        public int PixelWidth
        {
            get
            {
                ReadPreamble();
                return (int)_pixelWidth;
            }
        }

        /// <summary>
        ///     Height in pixels.
        /// </summary>
        public int PixelHeight
        {
            get
            {
                ReadPreamble();
                return (int)_pixelHeight;
            }
        }

        /// <summary>
        ///     Get the width of the image in measure units (96ths of an inch).
        /// </summary>
        public sealed override double Width
        {
            get
            {
                ReadPreamble();
                return PixelsToDIPs(_dpiX, (int)_pixelWidth);
            }
        }

        /// <summary>
        ///     Get the height of the image in measure units (96ths of an inch).
        /// </summary>
        public sealed override double Height
        {
            get
            {
                ReadPreamble();
                return PixelsToDIPs(_dpiY, (int)_pixelHeight);
            }
        }

        /// <summary>
        ///     Get the Metadata of the image (always null for D3DImage).
        /// </summary>
        public sealed override ImageMetadata Metadata
        {
            get
            {
                ReadPreamble();
                return null;
            }
        }

        public new D3DImage Clone()
        {
            return (D3DImage)base.Clone();
        }

        public new D3DImage CloneCurrentValue()
        {
            return (D3DImage)base.CloneCurrentValue();
        }

        protected override Freezable CreateInstanceCore()
        {
            return new D3DImage();
        }

        protected sealed override bool FreezeCore(bool isChecking)
        {
            return false;
        }

        protected override void CloneCore(Freezable sourceFreezable)
        {
            base.CloneCore(sourceFreezable);
            CloneCommon(sourceFreezable);
        }

        protected override void CloneCurrentValueCore(Freezable sourceFreezable)
        {
            base.CloneCurrentValueCore(sourceFreezable);
            CloneCommon(sourceFreezable);
        }

        protected override void GetAsFrozenCore(Freezable sourceFreezable)
        {
            base.GetAsFrozenCore(sourceFreezable);
            CloneCommon(sourceFreezable);
        }

        protected override void GetCurrentValueAsFrozenCore(Freezable sourceFreezable)
        {
            base.GetCurrentValueAsFrozenCore(sourceFreezable);
            CloneCommon(sourceFreezable);
        }

        /// <summary>
        ///     Gets a software copy of D3DImage.
        /// </summary>
        protected internal virtual BitmapSource CopyBackBuffer()
        {
            lock (_surfaceLock)
            {
                if (_frontBufferWidth <= 0 || _frontBufferHeight <= 0 || _frontBufferPixels == null)
                {
                    if (_surfacePtr != IntPtr.Zero)
                    {
                        SnapshotFrontBufferLocked(new Int32Rect(0, 0, (int)_pixelWidth, (int)_pixelHeight));
                    }
                }

                if (_frontBufferWidth <= 0 || _frontBufferHeight <= 0 || _frontBufferPixels == null)
                {
                    return null;
                }

                int stride = _frontBufferWidth * 4;
                byte[] copy = new byte[_frontBufferPixels.Length];
                Buffer.BlockCopy(_frontBufferPixels, 0, copy, 0, copy.Length);

                return BitmapSource.Create(
                    _frontBufferWidth,
                    _frontBufferHeight,
                    _dpiX,
                    _dpiY,
                    PixelFormats.Pbgra32,
                    null,
                    copy,
                    stride);
            }
        }

        internal override unsafe void DrawD2D(void* d2dContext, Rect rectangle)
        {
            if (d2dContext == null || rectangle.IsEmpty || rectangle.Width <= 0.0 || rectangle.Height <= 0.0)
            {
                return;
            }

            void* d2dBitmap = AcquireD2DBitmap(d2dContext);
            if (d2dBitmap == null)
            {
                return;
            }

            try
            {
                D2D1_RECT_F destRect = Direct2DRenderWalker.ToD2DRect(rectangle);
                ID2D1DeviceContextExVtbl.DrawBitmap(
                    d2dContext,
                    d2dBitmap,
                    &destRect,
                    1.0f,
                    DxMediaConst.D2D1_INTERPOLATION_MODE_LINEAR,
                    null);
            }
            finally
            {
                ComHelpers.Release(d2dBitmap);
            }
        }

        private unsafe void* AcquireD2DBitmap(void* d2dContext)
        {
            byte[] pixels;
            int width;
            int height;
            long version;

            void* ctxDevice = null;
            ID2D1DeviceContextVtbl.GetDevice(d2dContext, &ctxDevice);

            try
            {
                lock (_surfaceLock)
                {
                    if (_frontBufferPixels == null || _frontBufferWidth <= 0 || _frontBufferHeight <= 0)
                    {
                        if (_surfacePtr != IntPtr.Zero)
                        {
                            SnapshotFrontBufferLocked(new Int32Rect(0, 0, (int)_pixelWidth, (int)_pixelHeight));
                        }
                    }

                    if (_frontBufferPixels == null || _frontBufferWidth <= 0 || _frontBufferHeight <= 0)
                    {
                        return null;
                    }

                    if (_d2dBitmapCache != IntPtr.Zero &&
                        _d2dBitmapDevice == (IntPtr)ctxDevice &&
                        _d2dBitmapVersion == _frontBufferVersion)
                    {
                        void* cached = (void*)_d2dBitmapCache;
                        ComHelpers.AddRef(cached);
                        return cached;
                    }

                    pixels = _frontBufferPixels;
                    width = _frontBufferWidth;
                    height = _frontBufferHeight;
                    version = _frontBufferVersion;
                }

                float dpiX = _dpiX > 0.0 && !double.IsInfinity(_dpiX) && !double.IsNaN(_dpiX) ? (float)_dpiX : 96.0f;
                float dpiY = _dpiY > 0.0 && !double.IsInfinity(_dpiY) && !double.IsNaN(_dpiY) ? (float)_dpiY : 96.0f;

                D2D1_BITMAP_PROPERTIES1 props = new D2D1_BITMAP_PROPERTIES1
                {
                    PixelFormat = new D2D1_PIXEL_FORMAT
                    {
                        Format = DxConst.DXGI_FORMAT_B8G8R8A8_UNORM,
                        AlphaMode = DxConst.D2D1_ALPHA_MODE_PREMULTIPLIED
                    },
                    DpiX = dpiX,
                    DpiY = dpiY,
                    BitmapOptions = 0,
                    ColorContext = null
                };

                void* d2dBitmap = null;
                fixed (byte* pPixels = pixels)
                {
                    int hr = ID2D1DeviceContextExVtbl.CreateBitmap(
                        d2dContext,
                        new D2D1_SIZE_U((uint)width, (uint)height),
                        pPixels,
                        (uint)(width * 4),
                        in props,
                        &d2dBitmap);
                    if (hr < 0 || d2dBitmap == null)
                    {
                        return null;
                    }
                }

                lock (_surfaceLock)
                {
                    if (_frontBufferVersion == version && ctxDevice != null)
                    {
                        InvalidateD2DCacheLocked(fromFinalizer: false);
                        ComHelpers.AddRef(d2dBitmap);
                        _d2dBitmapCache = (IntPtr)d2dBitmap;
                        _d2dBitmapDevice = (IntPtr)ctxDevice;
                        _d2dBitmapVersion = version;
                    }
                }

                return d2dBitmap;
            }
            finally
            {
                if (ctxDevice != null)
                {
                    ComHelpers.Release(ctxDevice);
                }
            }
        }

        private void CloneCommon(Freezable sourceFreezable)
        {
            D3DImage source = (D3DImage)sourceFreezable;

            _dpiX = source._dpiX;
            _dpiY = source._dpiY;

            Lock();
            SetBackBuffer(D3DResourceType.IDirect3DSurface9, source._pUserSurfaceUnsafe);
            Unlock();
        }

        private bool LockImpl(Duration timeout)
        {
            bool acquired;
            if (timeout == Duration.Forever)
            {
                Monitor.Enter(_lockSync);
                acquired = true;
            }
            else
            {
                int ms = (int)Math.Min(int.MaxValue, Math.Max(0.0, timeout.TimeSpan.TotalMilliseconds));
                acquired = Monitor.TryEnter(_lockSync, ms);
            }

            if (!acquired)
            {
                return false;
            }

            if (_lockCount == uint.MaxValue)
            {
                Monitor.Exit(_lockSync);
                throw new InvalidOperationException(SR.Image_LockCountLimit);
            }

            ++_lockCount;
            return true;
        }

        private void SnapshotFrontBuffer()
        {
            lock (_surfaceLock)
            {
                if (_surfacePtr == IntPtr.Zero || _pixelWidth == 0 || _pixelHeight == 0)
                {
                    return;
                }

                Int32Rect rect = _pendingDirtyRect.IsEmpty || !_pendingDirtyRect.HasArea
                    ? new Int32Rect(0, 0, (int)_pixelWidth, (int)_pixelHeight)
                    : _pendingDirtyRect;
                SnapshotFrontBufferLocked(rect);
            }
        }

        private unsafe void SnapshotFrontBufferLocked(Int32Rect dirtyRect)
        {
            if (_surfacePtr == IntPtr.Zero || _pixelWidth == 0 || _pixelHeight == 0)
            {
                return;
            }

            int width = (int)_pixelWidth;
            int height = (int)_pixelHeight;
            int stride = width * 4;
            int totalBytes = stride * height;

            if (_frontBufferPixels == null || _frontBufferWidth != width || _frontBufferHeight != height)
            {
                _frontBufferPixels = new byte[totalBytes];
                _frontBufferWidth = width;
                _frontBufferHeight = height;
                dirtyRect = new Int32Rect(0, 0, width, height);
            }

            void* surface = (void*)_surfacePtr;
            bool copied = false;

            if (_surfaceKind == SurfaceKind.Direct3DSurface9)
            {
                copied = TryReadD3D9Surface(surface, _surfaceFormat, dirtyRect, width, stride, _frontBufferPixels);
            }
            else if (_surfaceKind == SurfaceKind.DxgiSurface)
            {
                copied = TryReadDxgiSurface(surface, _surfaceFormat, dirtyRect, width, height, stride, _frontBufferPixels);
            }
            else if (_surfaceKind == SurfaceKind.D3D11Texture2D)
            {
                copied = TryReadD3D11Texture2D(surface, _surfaceFormat, dirtyRect, width, height, stride, _frontBufferPixels);
            }

            if (copied)
            {
                ++_frontBufferVersion;
                InvalidateD2DCacheLocked(fromFinalizer: false);
            }
        }

        private static unsafe int QueryInterface(void* unknown, in Guid iid, out void* result)
        {
            void* pOut = null;
            fixed (Guid* pIid = &iid)
            {
                int hr = ((delegate* unmanaged[Stdcall]<void*, Guid*, void**, int>)(*(void***)unknown)[0])(unknown, pIid, &pOut);
                result = pOut;
                return hr;
            }
        }

        private static unsafe void AcquireSurface(
            IntPtr backBuffer,
            out void* surface,
            out SurfaceKind kind,
            out uint width,
            out uint height,
            out uint format)
        {
            void* unknown = (void*)backBuffer;

            if (QueryInterface(unknown, in s_iidIDirect3DSurface9, out void* d3d9Surf) >= 0 && d3d9Surf != null)
            {
                D3DSURFACE_DESC desc;
                int hr = ((delegate* unmanaged[Stdcall]<void*, D3DSURFACE_DESC*, int>)(*(void***)d3d9Surf)[12])(d3d9Surf, &desc);
                if (hr < 0 || (desc.Format != D3DFMT_A8R8G8B8 && desc.Format != D3DFMT_X8R8G8B8))
                {
                    ComHelpers.Release(d3d9Surf);
                    throw new ArgumentException(SR.D3DImage_InvalidUsage, nameof(backBuffer));
                }

                surface = d3d9Surf;
                kind = SurfaceKind.Direct3DSurface9;
                width = desc.Width;
                height = desc.Height;
                format = desc.Format;
                return;
            }

            if (QueryInterface(unknown, in DxGuids.IID_IDXGISurface, out void* dxgiSurf) >= 0 && dxgiSurf != null)
            {
                DXGI_SURFACE_DESC desc;
                int hr = ((delegate* unmanaged[Stdcall]<void*, DXGI_SURFACE_DESC*, int>)(*(void***)dxgiSurf)[8])(dxgiSurf, &desc);
                if (hr < 0 ||
                    (desc.Format != DXGI_FORMAT_B8G8R8A8_UNORM &&
                     desc.Format != DXGI_FORMAT_B8G8R8X8_UNORM &&
                     desc.Format != DXGI_FORMAT_R8G8B8A8_UNORM))
                {
                    ComHelpers.Release(dxgiSurf);
                    throw new ArgumentException(SR.D3DImage_InvalidUsage, nameof(backBuffer));
                }

                surface = dxgiSurf;
                kind = SurfaceKind.DxgiSurface;
                width = desc.Width;
                height = desc.Height;
                format = desc.Format;
                return;
            }

            if (QueryInterface(unknown, in DxGuids.IID_ID3D11Texture2D, out void* tex2d) >= 0 && tex2d != null)
            {
                D3D11_TEXTURE2D_DESC desc;
                ((delegate* unmanaged[Stdcall]<void*, D3D11_TEXTURE2D_DESC*, void>)(*(void***)tex2d)[10])(tex2d, &desc);
                if (desc.Format != DXGI_FORMAT_B8G8R8A8_UNORM &&
                    desc.Format != DXGI_FORMAT_B8G8R8X8_UNORM &&
                    desc.Format != DXGI_FORMAT_R8G8B8A8_UNORM)
                {
                    ComHelpers.Release(tex2d);
                    throw new ArgumentException(SR.D3DImage_InvalidUsage, nameof(backBuffer));
                }

                surface = tex2d;
                kind = SurfaceKind.D3D11Texture2D;
                width = desc.Width;
                height = desc.Height;
                format = desc.Format;
                return;
            }

            throw new ArgumentException(SR.D3DImage_InvalidUsage, nameof(backBuffer));
        }

        private static unsafe bool TryReadD3D9Surface(
            void* d3d9Surf,
            uint format,
            Int32Rect dirtyRect,
            int totalWidth,
            int dstStride,
            byte[] dstBuffer)
        {
            D3DLOCKED_RECT locked;
            int hr = ((delegate* unmanaged[Stdcall]<void*, D3DLOCKED_RECT*, void*, uint, int>)(*(void***)d3d9Surf)[13])(
                d3d9Surf, &locked, null, D3DLOCK_READONLY);
            if (hr < 0 || locked.pBits == null)
            {
                return false;
            }

            try
            {
                CopyRectToPbgra32(
                    (byte*)locked.pBits,
                    locked.Pitch,
                    dirtyRect,
                    dstStride,
                    dstBuffer,
                    forceOpaque: format == D3DFMT_X8R8G8B8,
                    swapRedBlue: false);
                return true;
            }
            finally
            {
                ((delegate* unmanaged[Stdcall]<void*, int>)(*(void***)d3d9Surf)[14])(d3d9Surf);
            }
        }

        private static unsafe bool TryReadDxgiSurface(
            void* dxgiSurf,
            uint format,
            Int32Rect dirtyRect,
            int totalWidth,
            int totalHeight,
            int dstStride,
            byte[] dstBuffer)
        {
            DXGI_MAPPED_RECT mapped;
            int hr = ((delegate* unmanaged[Stdcall]<void*, DXGI_MAPPED_RECT*, uint, int>)(*(void***)dxgiSurf)[9])(
                dxgiSurf, &mapped, DXGI_MAP_READ);
            if (hr >= 0 && mapped.pBits != null)
            {
                try
                {
                    CopyRectToPbgra32(
                        mapped.pBits,
                        mapped.Pitch,
                        dirtyRect,
                        dstStride,
                        dstBuffer,
                        forceOpaque: format == DXGI_FORMAT_B8G8R8X8_UNORM,
                        swapRedBlue: format == DXGI_FORMAT_R8G8B8A8_UNORM);
                    return true;
                }
                finally
                {
                    ((delegate* unmanaged[Stdcall]<void*, int>)(*(void***)dxgiSurf)[10])(dxgiSurf);
                }
            }

            if (QueryInterface(dxgiSurf, in DxGuids.IID_ID3D11Texture2D, out void* tex2d) >= 0 && tex2d != null)
            {
                try
                {
                    return TryReadD3D11Texture2D(tex2d, format, dirtyRect, totalWidth, totalHeight, dstStride, dstBuffer);
                }
                finally
                {
                    ComHelpers.Release(tex2d);
                }
            }

            return false;
        }

        private static unsafe bool TryReadD3D11Texture2D(
            void* tex2d,
            uint format,
            Int32Rect dirtyRect,
            int totalWidth,
            int totalHeight,
            int dstStride,
            byte[] dstBuffer)
        {
            void* d3dDevice = null;
            ((delegate* unmanaged[Stdcall]<void*, void**, void>)(*(void***)tex2d)[3])(tex2d, &d3dDevice);
            if (d3dDevice == null)
            {
                return false;
            }

            void* d3dCtx = null;
            void* stagingTex = null;
            try
            {
                ((delegate* unmanaged[Stdcall]<void*, void**, void>)(*(void***)d3dDevice)[40])(d3dDevice, &d3dCtx);
                if (d3dCtx == null)
                {
                    return false;
                }

                D3D11_TEXTURE2D_DESC desc;
                ((delegate* unmanaged[Stdcall]<void*, D3D11_TEXTURE2D_DESC*, void>)(*(void***)tex2d)[10])(tex2d, &desc);

                if (desc.Usage == D3D11_USAGE_STAGING && (desc.CPUAccessFlags & D3D11_CPU_ACCESS_READ) != 0)
                {
                    D3D11_MAPPED_SUBRESOURCE mappedDirect;
                    int hrMap = ((delegate* unmanaged[Stdcall]<void*, void*, uint, uint, uint, D3D11_MAPPED_SUBRESOURCE*, int>)(*(void***)d3dCtx)[14])(
                        d3dCtx, tex2d, 0, D3D11_MAP_READ, 0, &mappedDirect);
                    if (hrMap < 0 || mappedDirect.pData == null)
                    {
                        return false;
                    }

                    try
                    {
                        CopyRectToPbgra32(
                            (byte*)mappedDirect.pData,
                            (int)mappedDirect.RowPitch,
                            dirtyRect,
                            dstStride,
                            dstBuffer,
                            forceOpaque: format == DXGI_FORMAT_B8G8R8X8_UNORM,
                            swapRedBlue: format == DXGI_FORMAT_R8G8B8A8_UNORM);
                        return true;
                    }
                    finally
                    {
                        ((delegate* unmanaged[Stdcall]<void*, void*, uint, void>)(*(void***)d3dCtx)[15])(d3dCtx, tex2d, 0);
                    }
                }

                desc.MipLevels = 1;
                desc.ArraySize = 1;
                desc.SampleCount = 1;
                desc.SampleQuality = 0;
                desc.Usage = D3D11_USAGE_STAGING;
                desc.BindFlags = 0;
                desc.CPUAccessFlags = D3D11_CPU_ACCESS_READ;
                desc.MiscFlags = 0;

                int hrCreate = ((delegate* unmanaged[Stdcall]<void*, D3D11_TEXTURE2D_DESC*, void*, void**, int>)(*(void***)d3dDevice)[5])(
                    d3dDevice, &desc, null, &stagingTex);
                if (hrCreate < 0 || stagingTex == null)
                {
                    return false;
                }

                ((delegate* unmanaged[Stdcall]<void*, void*, void*, void>)(*(void***)d3dCtx)[47])(d3dCtx, stagingTex, tex2d);

                D3D11_MAPPED_SUBRESOURCE mapped;
                int hrStageMap = ((delegate* unmanaged[Stdcall]<void*, void*, uint, uint, uint, D3D11_MAPPED_SUBRESOURCE*, int>)(*(void***)d3dCtx)[14])(
                    d3dCtx, stagingTex, 0, D3D11_MAP_READ, 0, &mapped);
                if (hrStageMap < 0 || mapped.pData == null)
                {
                    return false;
                }

                try
                {
                    CopyRectToPbgra32(
                        (byte*)mapped.pData,
                        (int)mapped.RowPitch,
                        dirtyRect,
                        dstStride,
                        dstBuffer,
                        forceOpaque: format == DXGI_FORMAT_B8G8R8X8_UNORM,
                        swapRedBlue: format == DXGI_FORMAT_R8G8B8A8_UNORM);
                    return true;
                }
                finally
                {
                    ((delegate* unmanaged[Stdcall]<void*, void*, uint, void>)(*(void***)d3dCtx)[15])(d3dCtx, stagingTex, 0);
                }
            }
            finally
            {
                if (stagingTex != null)
                {
                    ComHelpers.Release(stagingTex);
                }
                if (d3dCtx != null)
                {
                    ComHelpers.Release(d3dCtx);
                }
                ComHelpers.Release(d3dDevice);
            }
        }

        private static unsafe void CopyRectToPbgra32(
            byte* srcBits,
            int srcPitch,
            Int32Rect rect,
            int dstStride,
            byte[] dstBuffer,
            bool forceOpaque,
            bool swapRedBlue)
        {
            if (srcBits == null || rect.Width <= 0 || rect.Height <= 0)
            {
                return;
            }

            fixed (byte* dstBase = dstBuffer)
            {
                for (int row = 0; row < rect.Height; row++)
                {
                    int y = rect.Y + row;
                    byte* srcRow = srcBits + (nint)y * srcPitch + (nint)rect.X * 4;
                    byte* dstRow = dstBase + (nint)y * dstStride + (nint)rect.X * 4;

                    if (!forceOpaque && !swapRedBlue)
                    {
                        Buffer.MemoryCopy(srcRow, dstRow, (long)rect.Width * 4, (long)rect.Width * 4);
                    }
                    else
                    {
                        for (int col = 0; col < rect.Width; col++)
                        {
                            int off = col * 4;
                            if (swapRedBlue)
                            {
                                byte r = srcRow[off + 0];
                                byte g = srcRow[off + 1];
                                byte b = srcRow[off + 2];
                                byte a = forceOpaque ? (byte)0xFF : srcRow[off + 3];
                                dstRow[off + 0] = b;
                                dstRow[off + 1] = g;
                                dstRow[off + 2] = r;
                                dstRow[off + 3] = a;
                            }
                            else
                            {
                                dstRow[off + 0] = srcRow[off + 0];
                                dstRow[off + 1] = srcRow[off + 1];
                                dstRow[off + 2] = srcRow[off + 2];
                                dstRow[off + 3] = 0xFF;
                            }
                        }
                    }
                }
            }
        }

        private void ReleaseCachedResources(bool fromFinalizer)
        {
            lock (_surfaceLock)
            {
                ReleaseSurfaceLocked(fromFinalizer);
                InvalidateD2DCacheLocked(fromFinalizer);
            }
        }

        private unsafe void ReleaseSurfaceLocked(bool fromFinalizer)
        {
            if (_surfacePtr != IntPtr.Zero)
            {
                if (!fromFinalizer || !Environment.HasShutdownStarted)
                {
                    ComHelpers.Release((void*)_surfacePtr);
                }

                _surfacePtr = IntPtr.Zero;
            }

            _surfaceKind = SurfaceKind.None;
            _surfaceFormat = 0;
        }

        private unsafe void InvalidateD2DCacheLocked(bool fromFinalizer)
        {
            if (_d2dBitmapCache != IntPtr.Zero)
            {
                if (!fromFinalizer || !Environment.HasShutdownStarted)
                {
                    ComHelpers.Release((void*)_d2dBitmapCache);
                }

                _d2dBitmapCache = IntPtr.Zero;
            }

            _d2dBitmapDevice = IntPtr.Zero;
            _d2dBitmapVersion = -1;
        }

        private static void IsFrontBufferAvailablePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            bool isAvailable = (bool)e.NewValue;
            D3DImage image = (D3DImage)d;

            if (!isAvailable && !image._isSoftwareFallbackEnabled)
            {
                image._pUserSurfaceUnsafe = IntPtr.Zero;
            }

            if (image._isFrontBufferAvailableChangedHandlers != null)
            {
                image._isFrontBufferAvailableChangedHandlers(image, e);
            }
        }

        private static readonly DependencyPropertyKey IsFrontBufferAvailablePropertyKey;

        private readonly object _lockSync = new object();
        private readonly object _surfaceLock = new object();

        private double _dpiX;
        private double _dpiY;

        private IntPtr _surfacePtr;
        private SurfaceKind _surfaceKind;
        private uint _surfaceFormat;
        private IntPtr _pUserSurfaceUnsafe;

        private uint _pixelWidth;
        private uint _pixelHeight;

        private byte[] _frontBufferPixels;
        private int _frontBufferWidth;
        private int _frontBufferHeight;
        private long _frontBufferVersion;

        private IntPtr _d2dBitmapCache;
        private IntPtr _d2dBitmapDevice;
        private long _d2dBitmapVersion = -1;

        private uint _lockCount;
        private uint _version;
        private bool _isDirty;
        private bool _isChangePending;
        private bool _isSoftwareFallbackEnabled;
        private Int32Rect _pendingDirtyRect = Int32Rect.Empty;

        private DependencyPropertyChangedEventHandler _isFrontBufferAvailableChangedHandlers;
    }
}

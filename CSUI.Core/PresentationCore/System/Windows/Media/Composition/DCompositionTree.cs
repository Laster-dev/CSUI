// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System;
using CSUI.Core.Interop;
using CSUI.Core.Interop.DirectX;

namespace System.Windows.Media.Composition
{
    /// <summary>
    /// Type of layer in the modern DirectComposition hardware visual tree.
    /// Eliminates WPF's 20-year Airspace bug by allowing arbitrary hardware z-ordering.
    /// </summary>
    public enum DCompLayerKind
    {
        Background = 0,
        ForeignContent = 1, // External Win32 child HWND, WebView2, or Media Stream
        MainUiContent = 2,  // Direct2D / D3D11 SwapChain Surface
        OverlayAdorner = 3  // Popups, tooltips, semi-transparent masks overlapping foreign content
    }

    /// <summary>
    /// Represents a hardware visual node in the DirectComposition visual tree.
    /// </summary>
    public sealed unsafe class DCompVisualNode : IDisposable
    {
        private ComPtr<byte> _visual;
        public void* VisualPtr => _visual.Ptr;
        public DCompLayerKind Kind { get; }
        public float OffsetX { get; private set; }
        public float OffsetY { get; private set; }

        public DCompVisualNode(void* visualPtr, DCompLayerKind kind)
        {
            _visual.Attach((byte*)visualPtr);
            Kind = kind;
        }

        public void SetOffset(float x, float y)
        {
            OffsetX = x;
            OffsetY = y;
            if (!_visual.IsNull)
            {
                IDCompositionVisualVtbl.SetOffsetX(_visual.Ptr, x);
                IDCompositionVisualVtbl.SetOffsetY(_visual.Ptr, y);
            }
        }

        public void SetContent(void* contentPtr)
        {
            if (!_visual.IsNull)
            {
                IDCompositionVisualVtbl.SetContent(_visual.Ptr, contentPtr);
            }
        }

        public void SetClip(Rect clipRect)
        {
            if (!_visual.IsNull && !clipRect.IsEmpty)
            {
                var d2dRect = new D2D1_RECT_F((float)clipRect.Left, (float)clipRect.Top, (float)clipRect.Right, (float)clipRect.Bottom);
                IDCompositionVisualVtbl.SetClip(_visual.Ptr, d2dRect);
            }
        }

        public void Dispose()
        {
            _visual.Dispose();
        }
    }

    /// <summary>
    /// Modern DirectComposition Desktop Tree manager.
    /// Replaces legacy exclusive HWND rendering with a multi-layered hardware composition tree,
    /// enabling seamless alpha blending, rounded clipping, and zero-airspace overlays.
    /// </summary>
    public sealed unsafe class DCompositionTree : IDisposable
    {
        private ComPtr<byte> _device;
        private ComPtr<byte> _target;
        private ComPtr<byte> _rootVisual;
        private readonly nint _hwnd;
        private bool _isSupported;

        public bool IsSupported => _isSupported;
        public void* DevicePtr => _device.Ptr;
        public void* RootVisualPtr => _rootVisual.Ptr;

        internal DCompositionTree(DxSharedDevice sharedDevice, nint hwnd)
        {
            _hwnd = hwnd;
            Initialize(sharedDevice);
        }

        private void Initialize(DxSharedDevice sharedDevice)
        {
            if (sharedDevice == null || _hwnd == 0) return;

            using var dxgiDevice = new ComPtr<byte>();
            if (ComPtr<byte>.QueryInterface(sharedDevice.D3DDevice, DxGuids.IID_IDXGIDevice, (void**)dxgiDevice.AddressOf()) < 0)
                return;

            void* devPtr = null;
            int hr = DxExports.DCompositionCreateDevice(dxgiDevice.Ptr, DxGuids.IID_IDCompositionDevice, &devPtr);
            if (hr < 0 || devPtr == null)
                return;

            _device.Attach((byte*)devPtr);

            void* targetPtr = null;
            hr = IDCompositionDeviceVtbl.CreateTargetForHwnd(_device.Ptr, _hwnd, 1, &targetPtr);
            if (hr < 0 || targetPtr == null)
            {
                _device.Dispose();
                return;
            }
            _target.Attach((byte*)targetPtr);

            void* rootPtr = null;
            hr = IDCompositionDeviceVtbl.CreateVisual(_device.Ptr, &rootPtr);
            if (hr < 0 || rootPtr == null)
            {
                _target.Dispose();
                _device.Dispose();
                return;
            }
            _rootVisual.Attach((byte*)rootPtr);

            IDCompositionTargetVtbl.SetRoot(_target.Ptr, _rootVisual.Ptr);
            _isSupported = true;
        }

        public DCompVisualNode CreateVisualNode(DCompLayerKind kind)
        {
            if (!_isSupported || _device.IsNull) return null;

            void* vPtr = null;
            int hr = IDCompositionDeviceVtbl.CreateVisual(_device.Ptr, &vPtr);
            if (hr < 0 || vPtr == null) return null;

            var node = new DCompVisualNode(vPtr, kind);
            // Append node to root visual
            IDCompositionVisualVtbl.AddVisual(_rootVisual.Ptr, vPtr, 0, null);
            return node;
        }

        public void AttachOverlayAbove(DCompVisualNode overlay, DCompVisualNode underlay)
        {
            if (!_isSupported || _rootVisual.IsNull || overlay == null || underlay == null) return;
            IDCompositionVisualVtbl.AddVisual(_rootVisual.Ptr, overlay.VisualPtr, 1, underlay.VisualPtr);
        }

        public void Commit()
        {
            if (_isSupported && !_device.IsNull)
            {
                IDCompositionDeviceVtbl.Commit(_device.Ptr);
            }
        }

        public void Dispose()
        {
            _rootVisual.Dispose();
            _target.Dispose();
            _device.Dispose();
            _isSupported = false;
        }
    }
}

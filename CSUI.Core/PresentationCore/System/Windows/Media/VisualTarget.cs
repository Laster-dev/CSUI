// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System.Diagnostics;
using System.Windows.Threading;
using CSUI.Core.Interop.DirectX;

namespace System.Windows.Media
{
    /// <summary>
    /// Renders a Visual tree on a background (or secondary) Dispatcher thread into a
    /// thread-safe Direct2D/DXGI offscreen surface hosted by a HostVisual on the UI thread.
    /// </summary>
    public class VisualTarget : CompositionTarget
    {
        //----------------------------------------------------------------------
        //
        //  Constructors
        //
        //----------------------------------------------------------------------

        #region Constructors

        /// <summary>
        /// Creates a VisualTarget connected to the specified HostVisual.
        /// </summary>
        public VisualTarget(HostVisual hostVisual)
        {
            ArgumentNullException.ThrowIfNull(hostVisual);

            _hostVisual = hostVisual;
            _connected = false;
            MediaContext.RegisterICompositionTarget(Dispatcher, this);
            BeginHosting();
        }

        private void BeginHosting()
        {
            Debug.Assert(!_connected);
            try
            {
                _hostVisual.BeginHosting(this);
                _connected = true;
            }
            catch
            {
                MediaContext.UnregisterICompositionTarget(Dispatcher, this);
                throw;
            }
        }

        #endregion Constructors

        //----------------------------------------------------------------------
        //
        //  Public Properties
        //
        //----------------------------------------------------------------------

        #region Public Properties

        /// <summary>
        /// Returns matrix that can be used to transform coordinates from this
        /// target to the rendering destination device.
        /// </summary>
        public override Matrix TransformToDevice
        {
            get
            {
                VerifyAPIReadOnly();
                Matrix m = WorldTransform;
                m.Invert();
                return m;
            }
        }

        /// <summary>
        /// Returns matrix that can be used to transform coordinates from
        /// the rendering destination device to this target.
        /// </summary>
        public override Matrix TransformFromDevice
        {
            get
            {
                VerifyAPIReadOnly();
                return WorldTransform;
            }
        }

        #endregion Public Properties

        //----------------------------------------------------------------------
        //
        //  Public & Internal Methods
        //
        //----------------------------------------------------------------------

        #region Public & Internal Methods

        /// <summary>
        /// Dispose cleans up the state associated with VisualTarget.
        /// </summary>
        public override void Dispose()
        {
            try
            {
                VerifyAccess();

                if (!IsDisposed)
                {
                    if (_hostVisual != null && _connected)
                    {
                        RootVisual = null;
                        EndHosting();
                        MediaContext.UnregisterICompositionTarget(Dispatcher, this);
                    }

                    _offscreenTarget?.Dispose();
                    _offscreenTarget = null;
                }
            }
            finally
            {
                base.Dispose();
            }
        }

        internal void UpdateHostDpi(DpiScale dpi)
        {
            if (dpi.PixelsPerInchX <= 0 || dpi.PixelsPerInchY <= 0)
                return;

            if (CheckAccess())
            {
                ApplyHostDpi(dpi);
            }
            else if (!IsDisposed && !Dispatcher.HasShutdownStarted && !Dispatcher.HasShutdownFinished)
            {
                Dispatcher.BeginInvoke(
                    DispatcherPriority.Normal,
                    static void (object arg) =>
                    {
                        var pair = (Tuple<VisualTarget, DpiScale>)arg;
                        if (!pair.Item1.IsDisposed)
                        {
                            pair.Item1.ApplyHostDpi(pair.Item2);
                        }
                    },
                    Tuple.Create(this, dpi));
            }
        }

        protected internal override unsafe void OnRenderFrame(bool inResize)
        {
            if (IsDisposed || _hostVisual == null)
                return;

            Visual root = RootVisual;
            if (root == null)
            {
                _hostVisual.PublishFrame(null);
                return;
            }

            if (root is UIElement rootElement && !rootElement.IsArrangeValid)
            {
                rootElement.UpdateLayout();
                if (!rootElement.IsArrangeValid)
                {
                    if (!rootElement.IsMeasureValid)
                    {
                        rootElement.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                    }
                    rootElement.Arrange(new Rect(rootElement.DesiredSize));
                }
                root.Precompute();
            }

            Rect bounds = root.CalculateSubgraphBoundsOuterSpace();
            if (bounds.IsEmpty || double.IsNaN(bounds.Width) || double.IsNaN(bounds.Height) ||
                bounds.Width <= 0.0 || bounds.Height <= 0.0 ||
                double.IsInfinity(bounds.Width) || double.IsInfinity(bounds.Height))
            {
                _hostVisual.PublishFrame(null);
                return;
            }

            float dpiX = (float)_dpiScale.PixelsPerInchX;
            float dpiY = (float)_dpiScale.PixelsPerInchY;
            double scaleX = dpiX / 96.0;
            double scaleY = dpiY / 96.0;

            // Add 1-pixel guard margin for antialiasing strokes at edges.
            int pxLeft = (int)Math.Floor(bounds.Left * scaleX) - 1;
            int pxTop = (int)Math.Floor(bounds.Top * scaleY) - 1;
            int pxRight = (int)Math.Ceiling(bounds.Right * scaleX) + 1;
            int pxBottom = (int)Math.Ceiling(bounds.Bottom * scaleY) + 1;

            int rawW = Math.Clamp(pxRight - pxLeft, 1, 16384);
            int rawH = Math.Clamp(pxBottom - pxTop, 1, 16384);
            double originDipX = pxLeft / scaleX;
            double originDipY = pxTop / scaleY;

            DxSharedDevice shared = MediaSystem.EnsureSharedDevice();
            _offscreenTarget ??= new DxOffscreenRenderTarget(shared);

            DxOffscreenSurfaceSlot slot = _offscreenTarget.BeginRender(
                (uint)rawW,
                (uint)rawH,
                dpiX,
                dpiY,
                originDipX,
                originDipY,
                out void* d2dContext);

            if (slot == null)
                return;

            CompileD2D(d2dContext);

            if (_offscreenTarget.EndRender(d2dContext))
            {
                _hostVisual.PublishFrame(slot);
            }
            else
            {
                // Recover from DXGI/D2D device reset cleanly.
                _offscreenTarget.Dispose();
                _offscreenTarget = null;
                MediaSystem.RecreateSharedDevice();
                MediaContext.From(Dispatcher)?.PostRender();
            }
        }

        #endregion Public & Internal Methods

        //----------------------------------------------------------------------
        //
        //  Private Methods
        //
        //----------------------------------------------------------------------

        #region Private Methods

        private void ApplyHostDpi(DpiScale dpi)
        {
            if (IsDisposed)
                return;

            _dpiScale = dpi;
            Matrix world = Matrix.Identity;
            world.Scale(1.0 / dpi.DpiScaleX, 1.0 / dpi.DpiScaleY);

            StateChangedCallback(new object[]
            {
                HostStateFlags.WorldTransform,
                world,
            });

            if (RootVisual != null)
            {
                MediaContext.From(Dispatcher)?.PostRender();
            }
        }

        private void EndHosting()
        {
            Debug.Assert(_connected);
            _hostVisual.EndHosting();
            _connected = false;
        }

        #endregion Private Methods

        //----------------------------------------------------------------------
        //
        //  Private Fields
        //
        //----------------------------------------------------------------------

        #region Private Fields

        private readonly HostVisual _hostVisual;
        private DxOffscreenRenderTarget _offscreenTarget;
        private DpiScale _dpiScale = new DpiScale(1.0, 1.0);
        private bool _connected;

        #endregion Private Fields
    }
}

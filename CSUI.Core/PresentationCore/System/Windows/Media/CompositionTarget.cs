// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Windows.Threading;
using System.Diagnostics;
using CSUI.Core.Interop.DirectX;
using MS.Internal;
using MS.Utility;


#nullable disable
using SR = MS.Internal.PresentationCore.SR;

namespace System.Windows.Media
{
    public abstract class CompositionTarget : DispatcherObject, IDisposable, ICompositionTarget
    {
        //
        // Data types for communicating state information between
        // CompositionTarget and its host.
        //

        internal enum HostStateFlags : uint
        {
            None            = 0,
            WorldTransform  = 1,
            ClipBounds      = 2
        };

        //----------------------------------------------------------------------
        //
        //  Constructors
        //
        //----------------------------------------------------------------------

        #region Constructors

        /// <summary>
        /// CompositionTarget
        /// </summary>
        internal CompositionTarget()
        {
#if TRACE_MVR
            markVisibleCountTotal = 0;
#endif
        }



        #endregion Constructors

        //----------------------------------------------------------------------
        //
        //  Public Methods
        //
        //----------------------------------------------------------------------

        #region Public Methods

        /// <summary>
        /// Disposes CompositionTarget.
        /// </summary>
        public virtual void Dispose()
        {
            //
            // Here we cannot use VerifyAPI methods because they check
            // for the disposed state.
            //
            VerifyAccess();

            if (!_isDisposed)
            {
                //
                // Disconnect the root visual so that all of the child
                // animations and resources are cleaned up.
                //

                _isDisposed = true;

                GC.SuppressFinalize(this);
            }
        }

        /// <summary>
        /// Returns true if the CompositionTarget is disposed; otherwise returns false.
        /// </summary>
        internal bool IsDisposed { get { return _isDisposed; } }

        #endregion Public Methods

        //----------------------------------------------------------------------
        //
        //  Public Properties
        //
        //----------------------------------------------------------------------

        #region Public Properties

        /// <summary>
        /// Gets and sets the root Visual of the CompositionTarget.
        /// </summary>
        /// <value></value>
        /// <remarks>
        ///     Callers must have UIPermission(UIPermissionWindow.AllWindows) to call this API.
        /// </remarks>
        public virtual Visual RootVisual
        {
            get
            {
                VerifyAPIReadOnly();
                return (_rootVisual);
            }

            set
            {
                VerifyAPIReadWrite();
                if (_rootVisual != value)
                {
                    SetRootVisual(value);

                    MediaContext.From(Dispatcher).PostRender();
                }
            }
        }

        /// <summary>
        /// Returns matrix that can be used to transform coordinates from this
        /// target to the rendering destination device.
        /// </summary>
        public abstract Matrix TransformToDevice { get; }

        /// <summary>
        /// Returns matrix that can be used to transform coordinates from
        /// the rendering destination device to this target.
        /// </summary>
        public abstract Matrix TransformFromDevice { get; }

        #endregion Public Properties

        //----------------------------------------------------------------------
        //
        //  Internal Methods
        //
        //----------------------------------------------------------------------

        #region Internal Methods

        /// <summary>
        ///
        /// </summary>
        internal object StateChangedCallback(object arg)
        {
            object[] argArray = arg as object[];

            HostStateFlags stateFlags = (HostStateFlags)argArray[0];

            //
            // Check if world transform of the host has changed and
            // update cached value accordingly.
            //

            if ((stateFlags & HostStateFlags.WorldTransform) != 0)
            {
                _worldTransform = (Matrix)argArray[1];
            }

            //
            // Check if clip bounds have changed, update cached value.
            //

            if ((stateFlags & HostStateFlags.ClipBounds) != 0)
            {
                _worldClipBounds = (Rect)argArray[2];
            }

            //
            // Set corresponding flags on the root visual and schedule
            // render if one has not already been scheduled.
            //

            if (_rootVisual != null)
            {
                //
                // When replacing the root visual, we need to re-realize all
                // content in the new tree
                //
                Visual.PropagateFlags(
                    _rootVisual,
                    VisualFlags.IsSubtreeDirtyForPrecompute,
                    VisualProxyFlags.IsSubtreeDirtyForRender
                    );
            }

            return null;
        }



        /// <summary>
        /// Render method renders the visual tree.
        /// </summary>
        void ICompositionTarget.Render(bool inResize)
        {
#if DEBUG_CLR_MEM
            bool clrTracingEnabled = false;

            {
                clrTracingEnabled = true;
                ++_renderCLRPass;
            }
#endif // DEBUG_CLR_MEM

            //
            // Now we render the scene
            //

#if MEDIA_PERFORMANCE_COUNTERS
            _frameRateTimer.Begin();
#endif

            if (_rootVisual != null)
            {
                bool etwTracingEnabled = false;

                if (EventTrace.IsEnabled(EventTrace.Keyword.KeywordGeneral | EventTrace.Keyword.KeywordPerf, EventTrace.Level.Info))
                {
                    etwTracingEnabled = true;
                }

#if MEDIA_PERFORMANCE_COUNTERS
                _precomputeRateTimer.Begin();
#endif
                // precompute is channel agnostic
                _rootVisual.Precompute();

#if MEDIA_PERFORMANCE_COUNTERS
                _precomputeRateTimer.End();
#endif

                if (etwTracingEnabled)
                {
                    EventTrace.EventProvider.TraceEvent(EventTrace.Event.WClientPrecomputeSceneEnd, EventTrace.Keyword.KeywordGraphics | EventTrace.Keyword.KeywordPerf, EventTrace.Level.Info);
                }

#if DEBUG
                MediaTrace.RenderPass.Trace("Full Update");
#endif

                if (etwTracingEnabled)
                {
                    EventTrace.EventProvider.TraceEvent(
                        EventTrace.Event.WClientCompileSceneBegin, EventTrace.Keyword.KeywordGraphics | EventTrace.Keyword.KeywordPerf, EventTrace.Level.Info);
                }

#if MEDIA_PERFORMANCE_COUNTERS
                _renderRateTimer.Begin();
#endif

                Compile(inResize);

#if MEDIA_PERFORMANCE_COUNTERS
                _renderRateTimer.End();
#endif

                if (etwTracingEnabled)
                {
                    EventTrace.EventProvider.TraceEvent(
                            EventTrace.Event.WClientCompileSceneEnd, EventTrace.Keyword.KeywordGraphics | EventTrace.Keyword.KeywordPerf, EventTrace.Level.Info);
                }
            }

#if MEDIA_PERFORMANCE_COUNTERS
            _frameRateTimer.End();
            System.Console.WriteLine("RENDERING PERFORMANCE DATA");
            System.Console.WriteLine("Frame rendering time:  " + _frameRateTimer.TimeOfLastPeriod + "ms");
            System.Console.WriteLine("Frame precompute time: " + _precomputeRateTimer.TimeOfLastPeriod + "ms");
            System.Console.WriteLine("Frame render time:     " + _renderRateTimer.TimeOfLastPeriod + "ms");
#endif
        }

        #endregion Internal Methods

        //----------------------------------------------------------------------
        //
        //  Internal Properties
        //
        //----------------------------------------------------------------------

        #region Internal Properties


        /// <summary>
        ///
        /// </summary>
        internal Matrix WorldTransform
        {
            get
            {
                return _worldTransform;
            }
        }

        internal Rect WorldClipBounds
        {
            get
            {
                return _worldClipBounds;
            }
        }
        #endregion Internal Properties

        //----------------------------------------------------------------------
        //
        //  Private Methods
        //
        //----------------------------------------------------------------------

        #region Private Methods

        private void Compile(bool inResize)
        {
            if (_rootVisual == null || _isDisposed)
            {
                return;
            }

            OnRenderFrame(inResize);
        }

        protected internal virtual void OnRenderFrame(bool inResize)
        {
        }

        internal unsafe void CompileD2D(void* d2dContext)
        {
            if (_rootVisual == null || d2dContext == null)
            {
                return;
            }

            RenderContext rc = _cachedRenderContext;
            _cachedRenderContext = null;
            rc ??= new RenderContext();
            rc.Initialize(d2dContext);
            try
            {
                _rootVisual.RenderRecursive(rc);
            }
            finally
            {
                rc.Reset();
                _cachedRenderContext ??= rc;
            }
        }



        /// <summary>
        /// Internal method to set the root visual.
        /// </summary>
        /// <param name="visual">Root visual, can be null, but can not be a child of another
        /// Visual.</param>
        private void SetRootVisual(Visual visual)
        {
            // We need to make this function robust by leaving the
            // _rootVisual in a consistent state.

            if (visual != null &&
                (visual._parent != null
                 || visual.IsRootElement))
            {
                // If a Visual has already a parent it can not be the root in a CompositionTarget because
                // otherwise we would have two CompositionTargets party on the same Visual tree.
                // If want to allow this we need to explicitly add support for this.
                throw new System.ArgumentException(SR.CompositionTarget_RootVisual_HasParent);
            }

            if (_rootVisual != null)
            {


                _rootVisual.IsRootElement = false;
            }

            _rootVisual = visual;

            if (_rootVisual != null)
            {
                _rootVisual.IsRootElement = true;

                _rootVisual.SetFlagsOnAllChannels(
                    true,
                    VisualProxyFlags.IsSubtreeDirtyForRender);
            }
        }


        /// <summary>
        /// Verifies that the CompositionTarget can be accessed.
        /// </summary>
        internal void VerifyAPIReadOnly()
        {
            VerifyAccess();
            ObjectDisposedException.ThrowIf(_isDisposed, typeof(CompositionTarget));
        }

        /// <summary>
        /// Verifies that the CompositionTarget can be accessed.
        /// </summary>
        internal void VerifyAPIReadWrite()
        {
            VerifyAccess();
            ObjectDisposedException.ThrowIf(_isDisposed, typeof(CompositionTarget));

            MediaContext.From(Dispatcher).VerifyWriteAccess();
        }

        #endregion

        //----------------------------------------------------------------------
        //
        //  Private Fields
        //
        //----------------------------------------------------------------------

        #region Private Fields

        private bool _isDisposed;
        private Visual _rootVisual;
        private RenderContext _cachedRenderContext;
        private Matrix _worldTransform = Matrix.Identity;

        //
        // ISSUE-ABaioura-10/19/2004: For now we assume a very large client
        // rect, because currently clip infromation cannot be robustly
        // communicated from the host. When this is fixed, we can start off
        // an empty rect; clip bounds will be updated based on the host clip.
        //
        private Rect _worldClipBounds = new Rect(
                     Double.MinValue / 2.0,
                     Double.MinValue / 2.0,
                     Double.MaxValue,
                     Double.MaxValue);
#if DEBUG_CLR_MEM
        //
        // Used for CLRProfiler comments.
        //
        private static int _renderCLRPass = 0;
#endif // DEBUG_CLR_MEM


#if MEDIA_PERFORMANCE_COUNTERS
        private HFTimer _frameRateTimer;
        private HFTimer _precomputeRateTimer;
        private HFTimer _renderRateTimer;
#endif

        #endregion Private Fields


        //----------------------------------------------------------------------
        //
        //  Static Events
        //
        //----------------------------------------------------------------------
        #region Static Events
        /// <summary>
        /// Rendering event.  Registers a delegate to be notified after animation and layout but before rendering
        /// </summary>
        public static event EventHandler Rendering
        {
            add
            {
                MediaContext mc = MediaContext.From(Dispatcher.CurrentDispatcher);
                mc.Rendering += value;

                // We need to get a new rendering operation queued.
                mc.PostRender();
            }
            remove
            {
                MediaContext mc = MediaContext.From(Dispatcher.CurrentDispatcher);
                mc.Rendering -= value;
            }
        }

        #endregion
    }
}


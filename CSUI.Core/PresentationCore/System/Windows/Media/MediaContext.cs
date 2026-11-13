// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable
using SR = MS.Internal.PresentationCore.SR;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using MS.Internal;
using MS.Utility;
using MS.Win32;

namespace System.Windows.Media
{
    /// <summary>
    /// The MediaContext class manages per-Dispatcher timing, animation ticks,
    /// deferred Direct2D resource invalidation, and composition target rendering.
    /// </summary>
    internal partial class MediaContext : DispatcherObject, IDisposable, IClock
    {
        private class InvokeOnRenderCallback
        {
            private readonly DispatcherOperationCallback _callback;
            private readonly object _arg;

            public InvokeOnRenderCallback(DispatcherOperationCallback callback, object arg)
            {
                _callback = callback;
                _arg = arg;
            }

            public void DoWork()
            {
                _callback(_arg);
            }
        }

        internal delegate void ResourcesUpdatedHandler();

        private static readonly bool s_isClockSupported;
        private static readonly long s_countsPerSecond;
        private static int _contextRenderID;

        static MediaContext()
        {
            if (SafeNativeMethods.QueryPerformanceFrequency(out long freq) && freq > 0)
            {
                s_countsPerSecond = freq;
                s_isClockSupported = true;
            }
            else
            {
                s_countsPerSecond = TimeSpan.TicksPerSecond;
                s_isClockSupported = false;
            }
        }

        internal static bool IsClockSupported => s_isClockSupported;

        internal static bool ShouldRenderEvenWhenNoDisplayDevicesAreAvailable { get; } =
            true ?
            !CoreAppContextSwitches.ShouldNotRenderInNonInteractiveWindowStation :
            CoreAppContextSwitches.ShouldRenderEvenWhenNoDisplayDevicesAreAvailable;

        internal static long CurrentTicks
        {
            get
            {
                if (!s_isClockSupported || !SafeNativeMethods.QueryPerformanceCounter(out long qpc))
                {
                    return Environment.TickCount64 * TimeSpan.TicksPerMillisecond;
                }

                long q = qpc / s_countsPerSecond;
                long r = qpc % s_countsPerSecond;
                return (q * TimeSpan.TicksPerSecond) + ((r * TimeSpan.TicksPerSecond) / s_countsPerSecond);
            }
        }

        private MediaContext(Dispatcher dispatcher)
        {
            Debug.Assert(dispatcher != null);
            Debug.Assert(dispatcher.Reserved0 == null, "A MediaContext already exists for this Dispatcher");

            _isDisposed = false;
            _timeManager = new TimeManager(IsClockSupported ? this : null);
            _timeManager.NeedTickSooner += new EventHandler(OnNeedTickSooner);

            _registeredICompositionTargets = new HashSet<ICompositionTarget>();
            _renderMessage = new DispatcherOperationCallback(RenderMessageHandler);
            _animRenderMessage = new DispatcherOperationCallback(AnimatedRenderMessageHandler);
            _inputMarkerMessage = new DispatcherOperationCallback(InputMarkerMessageHandler);
            _renderModeMessage = new DispatcherOperationCallback(InvalidateRenderMode);

            _estimatedNextVSyncTimer = new DispatcherTimer(DispatcherPriority.Render, dispatcher);
            _estimatedNextVSyncTimer.Tick += new EventHandler(EstimatedNextVSyncTimeExpired);

            _displayRefreshRate = 60;
            _animationRenderRate = _displayRefreshRate;

            UpdateRenderCapabilities();

            dispatcher.Reserved0 = this;
            _destroyHandler = new EventHandler(OnDestroyContext);
            dispatcher.ShutdownStarted += _destroyHandler;

            MediaSystem.Startup(this);
            _isConnected = MediaSystem.IsTransportConnected;
            _timeManager.Start();
            InvalidateInputDevicesHook = DefaultInvalidateInputDevicesHook;
        }

        internal static MediaContext From(Dispatcher dispatcher)
        {
            ArgumentNullException.ThrowIfNull(dispatcher);

            MediaContext cm = (MediaContext)dispatcher.Reserved0;
            if (cm == null)
            {
                cm = new MediaContext(dispatcher);
                Debug.Assert(dispatcher.Reserved0 == cm);
            }

            return cm;
        }

        internal static MediaContext CurrentMediaContext
        {
            get
            {
                return From(Dispatcher.CurrentDispatcher);
            }
        }

        private void OnDestroyContext(object sender, EventArgs e)
        {
            Debug.Assert(CheckAccess());
            Dispose();
        }

        public virtual void Dispose()
        {
            if (!_isDisposed)
            {
                _isDisposed = true;
                _isConnected = false;

                Dispatcher?.ShutdownStarted -= _destroyHandler;
                if (Dispatcher != null && ReferenceEquals(Dispatcher.Reserved0, this))
                {
                    Dispatcher.Reserved0 = null;
                }

                _estimatedNextVSyncTimer?.Stop();
                _currentRenderOp?.Abort();
                _currentRenderOp = null;
                _inputMarkerOp?.Abort();
                _inputMarkerOp = null;

                if (_timeManager != null)
                {
                    _timeManager.NeedTickSooner -= new EventHandler(OnNeedTickSooner);
                    _timeManager.Stop();
                }

                _registeredICompositionTargets.Clear();
                MediaSystem.Shutdown(this);
                GC.SuppressFinalize(this);
            }
        }

        TimeSpan IClock.CurrentTime
        {
            get
            {
                if (_isDisposed)
                {
                    return TimeSpan.Zero;
                }

                return TimeSpan.FromTicks(CurrentTicks);
            }
        }

        internal TimeSpan EstimatedNextPresentationTime
        {
            get
            {
                long countsTicks = CurrentTicks;
                if (_animationRenderRate > 0)
                {
                    long framePeriod = TimeSpan.TicksPerSecond / _animationRenderRate;
                    long nextPresentationTicks = countsTicks + framePeriod;
                    if (Math.Abs(nextPresentationTicks - _estimatedNextPresentationTime.Ticks) > (framePeriod / 2))
                    {
                        _estimatedNextPresentationTime = TimeSpan.FromTicks(nextPresentationTicks);
                    }
                }
                else
                {
                    _estimatedNextPresentationTime = TimeSpan.FromTicks(countsTicks);
                }

                return _estimatedNextPresentationTime;
            }
        }

        internal static void RegisterICompositionTarget(Dispatcher dispatcher, ICompositionTarget iv)
        {
            MediaContext mc = From(dispatcher);
            mc.RegisterICompositionTargetInternal(iv);
        }

        internal static void UnregisterICompositionTarget(Dispatcher dispatcher, ICompositionTarget iv)
        {
            if (dispatcher == null || dispatcher.Reserved0 == null)
            {
                return;
            }

            MediaContext mc = From(dispatcher);
            mc.UnregisterICompositionTargetInternal(iv);
        }

        private void RegisterICompositionTargetInternal(ICompositionTarget iv)
        {
            ArgumentNullException.ThrowIfNull(iv);
            _registeredICompositionTargets.Add(iv);
        }

        private void UnregisterICompositionTargetInternal(ICompositionTarget iv)
        {
            if (iv != null)
            {
                _registeredICompositionTargets.Remove(iv);
            }
        }

        internal void BeginInvokeOnRender(
            DispatcherOperationCallback callback,
            object arg)
        {
            ArgumentNullException.ThrowIfNull(callback);
            VerifyAccess();

            _invokeOnRenderCallbacks ??= new FrugalObjectList<InvokeOnRenderCallback>();
            _invokeOnRenderCallbacks.Add(new InvokeOnRenderCallback(callback, arg));

            if (!_isRendering)
            {
                PostRender();
            }
        }

        internal LoadedOrUnloadedOperation AddLoadedOrUnloadedCallback(
            DispatcherOperationCallback callback,
            DependencyObject target)
        {
            var op = new LoadedOrUnloadedOperation(callback, target);
            _loadedOrUnloadedPendingOperations ??= new FrugalObjectList<LoadedOrUnloadedOperation>();
            _loadedOrUnloadedPendingOperations.Add(op);
            return op;
        }

        internal void RemoveLoadedOrUnloadedCallback(LoadedOrUnloadedOperation op)
        {
            if (op == null)
            {
                return;
            }

            op.Cancel();
            _loadedOrUnloadedPendingOperations?.Remove(op);
        }

        internal void PostRender()
        {
            VerifyAccess();

            if (_isDisposed)
            {
                return;
            }

            if (_isRendering)
            {
                _postRenderDuringRender = true;
                return;
            }

            if (_currentRenderOp != null)
            {
                if (_currentRenderOp.Priority < DispatcherPriority.Render)
                {
                    _currentRenderOp.Priority = DispatcherPriority.Render;
                }
                return;
            }

            ScheduleNextRenderOp(TimeSpan.Zero);
        }

        internal void Resize(ICompositionTarget resizedCompositionTarget)
        {
            VerifyAccess();

            if (_isDisposed || _isRendering)
            {
                return;
            }

            Render(resizedCompositionTarget);
        }

        private BoundsDrawingContextWalker _cachedBoundsDrawingContextWalker = new BoundsDrawingContextWalker();

        internal BoundsDrawingContextWalker AcquireBoundsDrawingContextWalker()
        {
            if (_cachedBoundsDrawingContextWalker != null)
            {
                BoundsDrawingContextWalker walker = _cachedBoundsDrawingContextWalker;
                _cachedBoundsDrawingContextWalker = null;
                return walker;
            }

            return new BoundsDrawingContextWalker();
        }

        internal void ReleaseBoundsDrawingContextWalker(BoundsDrawingContextWalker walker)
        {
            Debug.Assert(walker != null);
            walker.ClearState();
            _cachedBoundsDrawingContextWalker ??= walker;
        }

        internal void CompleteRender()
        {
            if (!_isDisposed && !_isRendering && _commitPendingAfterRender)
            {
                _commitPendingAfterRender = false;
            }
        }

        private void ScheduleNextRenderOp(TimeSpan minimumDelay)
        {
            if (_isDisposed)
            {
                return;
            }

            if (minimumDelay <= TimeSpan.Zero)
            {
                if (_isRendering)
                {
                    return;
                }

                _estimatedNextVSyncTimer.Stop();
                if (_currentRenderOp == null)
                {
                    DispatcherOperationCallback callback =
                        _timeManager.IsDirty ? _animRenderMessage : _renderMessage;
                    _currentRenderOp = Dispatcher.BeginInvoke(DispatcherPriority.Render, callback, null);
                }
            }
            else
            {
                _estimatedNextVSyncTimer.Stop();
                _estimatedNextVSyncTimer.Tag = CurrentTicks + minimumDelay.Ticks;
                _estimatedNextVSyncTimer.Interval = minimumDelay;
                _estimatedNextVSyncTimer.Start();
            }
        }

        private object RenderMessageHandler(object resizedCompositionTarget)
        {
            if (EventTrace.IsEnabled(EventTrace.Keyword.KeywordGraphics | EventTrace.Keyword.KeywordPerf, EventTrace.Level.Info))
            {
                EventTrace.EventProvider.TraceEvent(
                    EventTrace.Event.WClientRenderHandlerBegin,
                    EventTrace.Keyword.KeywordGraphics | EventTrace.Keyword.KeywordPerf,
                    EventTrace.Level.Info,
                    GetHashCode());
            }

            try
            {
                RenderMessageHandlerCore(resizedCompositionTarget);
            }
            finally
            {
                if (EventTrace.IsEnabled(EventTrace.Keyword.KeywordGraphics | EventTrace.Keyword.KeywordPerf, EventTrace.Level.Info))
                {
                    EventTrace.EventProvider.TraceEvent(
                        EventTrace.Event.WClientRenderHandlerEnd,
                        EventTrace.Keyword.KeywordGraphics | EventTrace.Keyword.KeywordPerf,
                        EventTrace.Level.Info);
                }
            }

            return null;
        }

        private object AnimatedRenderMessageHandler(object resizedCompositionTarget)
        {
            if (EventTrace.IsEnabled(EventTrace.Keyword.KeywordGraphics | EventTrace.Keyword.KeywordPerf, EventTrace.Level.Info))
            {
                EventTrace.EventProvider.TraceEvent(
                    EventTrace.Event.WClientAnimRenderHandlerBegin,
                    EventTrace.Keyword.KeywordGraphics | EventTrace.Keyword.KeywordPerf,
                    EventTrace.Level.Info);
            }

            try
            {
                RenderMessageHandlerCore(resizedCompositionTarget);
            }
            finally
            {
                if (EventTrace.IsEnabled(EventTrace.Keyword.KeywordGraphics | EventTrace.Keyword.KeywordPerf, EventTrace.Level.Info))
                {
                    EventTrace.EventProvider.TraceEvent(
                        EventTrace.Event.WClientAnimRenderHandlerEnd,
                        EventTrace.Keyword.KeywordGraphics | EventTrace.Keyword.KeywordPerf,
                        EventTrace.Level.Info);
                }
            }

            return null;
        }

        private object InputMarkerMessageHandler(object arg)
        {
            _inputMarkerOp = null;
            return null;
        }

        private void RenderMessageHandlerCore(object resizedCompositionTarget)
        {
            _currentRenderOp = null;

            if (_isDisposed)
            {
                return;
            }

            Render(resizedCompositionTarget as ICompositionTarget);
        }

        private void Render(ICompositionTarget resizedCompositionTarget)
        {
            if (_isDisposed)
            {
                return;
            }

            using (Dispatcher.DisableProcessing())
            {
                bool gotLock = false;
                bool etwTracingEnabled = EventTrace.IsEnabled(EventTrace.Keyword.KeywordGraphics | EventTrace.Keyword.KeywordPerf, EventTrace.Level.Info);
                int renderID = ++_contextRenderID;

                if (etwTracingEnabled)
                {
                    EventTrace.EventProvider.TraceEvent(
                        EventTrace.Event.WClientMediaRenderBegin,
                        EventTrace.Keyword.KeywordGraphics | EventTrace.Keyword.KeywordPerf,
                        EventTrace.Level.Info,
                        renderID,
                        GetHashCode());
                }

                bool scheduleImmediateRender = false;
                _isRendering = true;
                _postRenderDuringRender = false;
                try
                {
                    (InvalidateInputDevicesHook ?? DefaultInvalidateInputDevicesHook)?.Invoke();

                    int maxTicks = 100;

                    do
                    {
                        _animationRenderRate = Math.Max(_displayRefreshRate, _timeManager.GetMaxDesiredFrameRate());

                        int tickIterations = 0;
                        while (_timeManager.IsDirty || tickIterations == 0)
                        {
                            _timeManager.Tick();
                            if (!gotLock)
                            {
                                _timeManager.LockTickTime();
                                gotLock = true;
                            }
                            if (++tickIterations > maxTicks)
                            {
                                throw new InvalidOperationException(SR.MediaContext_InfiniteTickLoop);
                            }

                            if (!_timeManager.IsDirty)
                            {
                                break;
                            }
                        }

                        // Drain queued layout/render callbacks (up to bounded iterations).
                        if (_invokeOnRenderCallbacks != null && _invokeOnRenderCallbacks.Count > 0)
                        {
                            int iterations = _invokeOnRenderCallbacks.Count;
                            int count = 0;
                            while (_invokeOnRenderCallbacks != null && _invokeOnRenderCallbacks.Count > 0)
                            {
                                var callbacks = _invokeOnRenderCallbacks;
                                _invokeOnRenderCallbacks = null;

                                for (int i = 0; i < callbacks.Count; i++)
                                {
                                    callbacks[i].DoWork();
                                }

                                if (++count > maxTicks + iterations)
                                {
                                    throw new InvalidOperationException(SR.MediaContext_InfiniteLayoutLoop);
                                }
                            }
                        }
                    } while (_timeManager.IsDirty);

                    // Flush Direct2D/Freezable resource updates registered via Animatable.
                    ResourcesUpdatedHandler resourcesUpdated = _resourcesUpdatedHandlers;
                    if (resourcesUpdated != null)
                    {
                        _resourcesUpdatedHandlers = null;
                        resourcesUpdated();
                    }

                    // Any PostRender() calls before this point are already covered by this Render() pass.
                    _postRenderDuringRender = false;

                    // Raise CompositionTarget.Rendering.
                    if (Rendering != null)
                    {
                        Rendering(this, new RenderingEventArgs(EstimatedNextPresentationTime));
                    }

                    // Commit / render to registered ICompositionTargets.
                    CommittingBatch?.Invoke(this, EventArgs.Empty);

                    if (resizedCompositionTarget != null)
                    {
                        resizedCompositionTarget.Render(inResize: true);
                    }
                    else
                    {
                        foreach (ICompositionTarget target in _registeredICompositionTargets)
                        {
                            target.Render(inResize: false);
                        }
                    }

                    _commitPendingAfterRender = false;
                    _renderCompleteHandlers?.Invoke(this, EventArgs.Empty);

                    // Execute pending Loaded/Unloaded callbacks queued during tree changes.
                    if (_loadedOrUnloadedPendingOperations != null && _loadedOrUnloadedPendingOperations.Count > 0)
                    {
                        var ops = _loadedOrUnloadedPendingOperations;
                        _loadedOrUnloadedPendingOperations = null;
                        for (int i = 0; i < ops.Count; i++)
                        {
                            ops[i].DoWork();
                        }
                    }

                    TimeSpan nextTick = _timeManager.GetNextTickNeeded();
                    if ((_postRenderDuringRender || (_invokeOnRenderCallbacks != null && _invokeOnRenderCallbacks.Count > 0))
                        && resizedCompositionTarget == null)
                    {
                        scheduleImmediateRender = true;
                    }
                    else if (nextTick >= TimeSpan.Zero && resizedCompositionTarget == null)
                    {
                        long framePeriod = _animationRenderRate > 0
                            ? TimeSpan.TicksPerSecond / _animationRenderRate
                            : TimeSpan.FromMilliseconds(16).Ticks;
                        TimeSpan delay = nextTick.Ticks > framePeriod
                            ? nextTick
                            : TimeSpan.FromTicks(framePeriod);
                        ScheduleNextRenderOp(delay);
                    }
                    _postRenderDuringRender = false;
                }
                finally
                {
                    if (gotLock)
                    {
                        _timeManager.UnlockTickTime();
                    }

                    _isRendering = false;

                    if (etwTracingEnabled)
                    {
                        EventTrace.EventProvider.TraceEvent(
                            EventTrace.Event.WClientMediaRenderEnd,
                            EventTrace.Keyword.KeywordGraphics | EventTrace.Keyword.KeywordPerf,
                            EventTrace.Level.Info);

                        EventTrace.EventProvider.TraceEvent(
                            EventTrace.Event.WClientUIResponse,
                            EventTrace.Keyword.KeywordGraphics,
                            EventTrace.Level.Info,
                            GetHashCode(),
                            renderID);
                    }
                }

                if (scheduleImmediateRender && !_isDisposed)
                {
                    ScheduleNextRenderOp(TimeSpan.Zero);
                }
            }
        }

        private void OnNeedTickSooner(object sender, EventArgs e)
        {
            if (_isDisposed || _isRendering)
            {
                return;
            }

            TimeSpan nextTick = _timeManager.GetNextTickNeeded();
            if (nextTick >= TimeSpan.Zero)
            {
                ScheduleNextRenderOp(nextTick);
            }
        }

        private void EstimatedNextVSyncTimeExpired(object sender, EventArgs e)
        {
            DispatcherTimer timer = (DispatcherTimer)sender;
            long currentTicks = CurrentTicks;
            long earliestWakeupTicks = timer.Tag != null ? (long)timer.Tag : 0;

            if (earliestWakeupTicks > currentTicks)
            {
                timer.Stop();
                timer.Interval = TimeSpan.FromTicks(earliestWakeupTicks - currentTicks);
                timer.Start();
                return;
            }

            timer.Stop();
            ScheduleNextRenderOp(TimeSpan.Zero);
        }

        internal void PostInvalidateRenderMode()
        {
            Dispatcher.BeginInvoke(DispatcherPriority.Normal, _renderModeMessage, null);
        }

        private object InvalidateRenderMode(object dontCare)
        {
            Debug.Assert(CheckAccess());
            UpdateRenderCapabilities();
            RenderModeInvalidated?.Invoke(this, EventArgs.Empty);
            return null;
        }

        private void UpdateRenderCapabilities()
        {
            int newTier = MediaSystem.IsSoftwareRenderingForcedForProcess ? 0 : 2;
            PixelShaderVersion = 0x00030000;
            MaxPixelShader30InstructionSlots = 512;
            HasSSE2Support = true;
            MaxTextureSize = new Size(16384, 16384);

            if (_tier != newTier)
            {
                _tier = newTier;
                TierChanged?.Invoke(null, EventArgs.Empty);
            }
        }

        internal void VerifyWriteAccess()
        {
            if (!WriteAccessEnabled)
            {
                throw new InvalidOperationException(SR.MediaContext_APINotAllowed);
            }
        }

        internal bool WriteAccessEnabled => _readOnlyAccessCounter <= 0;

        internal void PushReadOnlyAccess()
        {
            _readOnlyAccessCounter++;
        }

        internal void PopReadOnlyAccess()
        {
            _readOnlyAccessCounter--;
        }

        internal TimeManager TimeManager => _timeManager;

        internal bool IsConnected => _isConnected;

        internal int Tier => _tier;

        internal UInt32 PixelShaderVersion { get; private set; }

        internal UInt32 MaxPixelShader30InstructionSlots { get; private set; }

        internal bool HasSSE2Support { get; private set; }

        internal Size MaxTextureSize { get; private set; }

        internal event EventHandler TierChanged;

        internal event EventHandler InvalidPixelShaderEncountered;

        internal event EventHandler RenderModeInvalidated;

        internal event EventHandler RenderComplete
        {
            add { _renderCompleteHandlers += value; }
            remove { _renderCompleteHandlers -= value; }
        }

        internal event ResourcesUpdatedHandler ResourcesUpdated
        {
            add { _resourcesUpdatedHandlers += value; }
            remove { _resourcesUpdatedHandlers -= value; }
        }

        internal event EventHandler Rendering;

        internal event EventHandler CommittingBatch;

        /// <summary>
        /// Hook wired by Stage C2 InputManager so input devices invalidate at frame start.
        /// </summary>
        internal Action InvalidateInputDevicesHook { get; set; }

        internal static Action DefaultInvalidateInputDevicesHook { get; set; }

        private void NotifyBadPixelShader()
        {
            if (InvalidPixelShaderEncountered != null)
            {
                InvalidPixelShaderEncountered(null, EventArgs.Empty);
            }
            else
            {
                throw new InvalidOperationException(SR.MediaContext_NoBadShaderHandler);
            }
        }

        private bool _isDisposed;
        private bool _isRendering;
        private bool _postRenderDuringRender;
        private bool _isConnected;
        private bool _commitPendingAfterRender;
        private int _readOnlyAccessCounter;
        private int _tier = 2;
        private int _displayRefreshRate;
        private int _animationRenderRate;
        private TimeSpan _estimatedNextPresentationTime;

        private readonly TimeManager _timeManager;
        private readonly EventHandler _destroyHandler;
        private readonly HashSet<ICompositionTarget> _registeredICompositionTargets;
        private readonly DispatcherOperationCallback _renderMessage;
        private readonly DispatcherOperationCallback _animRenderMessage;
        private readonly DispatcherOperationCallback _inputMarkerMessage;
        private readonly DispatcherOperationCallback _renderModeMessage;
        private readonly DispatcherTimer _estimatedNextVSyncTimer;

        private DispatcherOperation _currentRenderOp;
        private DispatcherOperation _inputMarkerOp;
        private FrugalObjectList<InvokeOnRenderCallback> _invokeOnRenderCallbacks;
        private FrugalObjectList<LoadedOrUnloadedOperation> _loadedOrUnloadedPendingOperations;
        private event EventHandler _renderCompleteHandlers;
        private event ResourcesUpdatedHandler _resourcesUpdatedHandlers;
    }
}

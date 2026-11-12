// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Windows.Interop;
using CSUI.Core.Interop;
using CSUI.Core.Interop.DirectX;

namespace System.Windows.Media.Composition
{
    /// <summary>
    /// Contract for an animation that runs independently on the compositor thread,
    /// decoupling frame presentation and visual transformations from UI thread stalls.
    /// </summary>
    public interface ICompositorAnimation
    {
        string Id { get; }
        bool IsCompleted { get; }
        void Step(double deltaSeconds);
        void Apply(ref Matrix transform, ref double opacity);
    }

    /// <summary>
    /// Independent continuous rotation animation executed solely by the AsyncCompositor.
    /// </summary>
    public sealed class CompositorRotateAnimation : ICompositorAnimation
    {
        public string Id { get; }
        public double SpeedDegreesPerSecond { get; set; }
        public Point Center { get; set; }
        public double CurrentAngle { get; private set; }
        public bool IsCompleted => false; // Continuous

        public CompositorRotateAnimation(string id, double speedDegPerSec, Point center = default)
        {
            Id = id;
            SpeedDegreesPerSecond = speedDegPerSec;
            Center = center;
        }

        public void Step(double deltaSeconds)
        {
            CurrentAngle = (CurrentAngle + SpeedDegreesPerSecond * deltaSeconds) % 360.0;
        }

        public void Apply(ref Matrix transform, ref double opacity)
        {
            transform.RotateAt(CurrentAngle, Center.X, Center.Y);
        }
    }

    /// <summary>
    /// Independent smooth momentum glide/translation animation executed solely by the AsyncCompositor.
    /// </summary>
    public sealed class CompositorGlideAnimation : ICompositorAnimation
    {
        public string Id { get; }
        public Vector Velocity { get; private set; }
        public double Friction { get; set; }
        public Vector CurrentOffset { get; private set; }
        public bool IsCompleted { get; private set; }

        public CompositorGlideAnimation(string id, Vector initialVelocity, double friction = 0.85)
        {
            Id = id;
            Velocity = initialVelocity;
            Friction = friction;
        }

        public void Step(double deltaSeconds)
        {
            if (IsCompleted) return;

            CurrentOffset += Velocity * deltaSeconds;
            Velocity *= Math.Pow(Friction, deltaSeconds * 60.0);

            if (Velocity.Length < 0.5)
            {
                IsCompleted = true;
            }
        }

        public void Apply(ref Matrix transform, ref double opacity)
        {
            transform.Translate(CurrentOffset.X, CurrentOffset.Y);
        }
    }

    /// <summary>
    /// Immutable frame payload committed from the UI thread to the AsyncCompositor.
    /// </summary>
    public sealed class CompositorFrame
    {
        public Drawing DrawingSnapshot { get; }
        public Color BackgroundColor { get; }
        public Matrix WorldTransform { get; }
        public double Opacity { get; }
        public long FrameSequence { get; }

        public CompositorFrame(Drawing drawing, Color bg, Matrix transform, double opacity, long seq)
        {
            DrawingSnapshot = drawing;
            BackgroundColor = bg;
            WorldTransform = transform;
            Opacity = opacity;
            FrameSequence = seq;
        }
    }

    internal sealed class CompositorTargetSlot
    {
        public IntPtr Hwnd { get; }
        public DxWindowRenderTarget DxTarget { get; set; }
        public CompositorFrame PendingFrame;
        public CompositorFrame CurrentFrame;
        public readonly List<ICompositorAnimation> Animations = new();
        public readonly object SyncLock = new();
        public long PresentedCount;

        public CompositorTargetSlot(IntPtr hwnd, DxWindowRenderTarget dxTarget)
        {
            Hwnd = hwnd;
            DxTarget = dxTarget;
        }
    }

    /// <summary>
    /// High-performance independent asynchronous compositor pipeline.
    /// Manages an out-of-process/dedicated high-priority render thread that drives Direct3D 11
    /// DXGI Flip Model presents and independent animations without being blocked by UI thread workloads.
    /// </summary>
    public sealed class AsyncCompositor : IDisposable
    {
        private static readonly Lazy<AsyncCompositor> s_instance = new(() => new AsyncCompositor());
        public static AsyncCompositor Instance => s_instance.Value;

        private readonly ConcurrentDictionary<IntPtr, CompositorTargetSlot> _slots = new();
        private readonly Thread _compositorThread;
        private readonly AutoResetEvent _wakeSignal = new(false);
        private volatile bool _isRunning = true;
        private long _sequenceCounter = 0;

        public bool IsRunning => _isRunning;

        private AsyncCompositor()
        {
            _compositorThread = new Thread(CompositorThreadLoop)
            {
                Name = "CSUI-AsyncCompositor-Thread",
                IsBackground = true,
                Priority = ThreadPriority.AboveNormal
            };
            _compositorThread.Start();
        }

        internal void RegisterTarget(IntPtr hwnd, DxWindowRenderTarget target)
        {
            if (hwnd == IntPtr.Zero || target == null) return;
            var slot = new CompositorTargetSlot(hwnd, target);
            _slots[hwnd] = slot;
            _wakeSignal.Set();
        }

        public void UnregisterTarget(IntPtr hwnd)
        {
            if (_slots.TryRemove(hwnd, out var slot))
            {
                lock (slot.SyncLock)
                {
                    slot.DxTarget = null;
                    slot.Animations.Clear();
                }
            }
        }

        public void CommitFrame(IntPtr hwnd, Drawing drawing, Color backgroundColor, Matrix? transform = null, double opacity = 1.0)
        {
            if (!_slots.TryGetValue(hwnd, out var slot)) return;

            long seq = Interlocked.Increment(ref _sequenceCounter);
            var frame = new CompositorFrame(drawing, backgroundColor, transform ?? Matrix.Identity, opacity, seq);

            lock (slot.SyncLock)
            {
                slot.PendingFrame = frame;
            }

            _wakeSignal.Set();
        }

        public void StartIndependentAnimation(IntPtr hwnd, ICompositorAnimation animation)
        {
            if (animation == null || !_slots.TryGetValue(hwnd, out var slot)) return;

            lock (slot.SyncLock)
            {
                slot.Animations.RemoveAll(a => a.Id == animation.Id);
                slot.Animations.Add(animation);
            }

            _wakeSignal.Set();
        }

        public void StopIndependentAnimation(IntPtr hwnd, string animationId)
        {
            if (string.IsNullOrEmpty(animationId) || !_slots.TryGetValue(hwnd, out var slot)) return;

            lock (slot.SyncLock)
            {
                slot.Animations.RemoveAll(a => a.Id == animationId);
            }
        }

        public long GetPresentedFrameCount(IntPtr hwnd)
        {
            if (_slots.TryGetValue(hwnd, out var slot))
            {
                return Volatile.Read(ref slot.PresentedCount);
            }
            return 0;
        }

        private void CompositorThreadLoop()
        {
            var stopwatch = Stopwatch.StartNew();
            double lastTime = stopwatch.Elapsed.TotalSeconds;

            while (_isRunning)
            {
                // Wait for wake signal or 120Hz VSync interval (~8ms)
                _wakeSignal.WaitOne(8);

                double now = stopwatch.Elapsed.TotalSeconds;
                double delta = Math.Clamp(now - lastTime, 0.001, 0.1);
                lastTime = now;

                foreach (var kvp in _slots)
                {
                    var slot = kvp.Value;
                    DxWindowRenderTarget dxTarget;
                    CompositorFrame frameToRender = null;
                    List<ICompositorAnimation> animsCopy = null;

                    lock (slot.SyncLock)
                    {
                        dxTarget = slot.DxTarget;
                        if (slot.PendingFrame != null)
                        {
                            slot.CurrentFrame = slot.PendingFrame;
                            slot.PendingFrame = null;
                        }
                        frameToRender = slot.CurrentFrame;

                        if (slot.Animations.Count > 0)
                        {
                            animsCopy = new List<ICompositorAnimation>(slot.Animations);
                        }
                    }

                    if (dxTarget == null || frameToRender == null) continue;

                    // Step independent compositor animations
                    Matrix renderTransform = frameToRender.WorldTransform;
                    double renderOpacity = frameToRender.Opacity;

                    if (animsCopy != null && animsCopy.Count > 0)
                    {
                        for (int i = animsCopy.Count - 1; i >= 0; i--)
                        {
                            var anim = animsCopy[i];
                            anim.Step(delta);
                            anim.Apply(ref renderTransform, ref renderOpacity);

                            if (anim.IsCompleted)
                            {
                                lock (slot.SyncLock)
                                {
                                    slot.Animations.Remove(anim);
                                }
                            }
                        }
                    }

                    // Render on dedicated compositor thread
                    unsafe
                    {
                        try
                        {
                            dxTarget.BeginDraw();
                            void* d2dContext = dxTarget.D2DContext;
                            if (d2dContext != null)
                            {
                                ID2D1DeviceContextVtbl.Clear(d2dContext, BrushDirect2DHelpers.ToD2DColor(frameToRender.BackgroundColor));

                                if (!renderTransform.IsIdentity)
                                {
                                    ID2D1DeviceContextVtbl.SetTransform(d2dContext, MILUtilities.ConvertToD2DMatrix(renderTransform));
                                }

                                if (frameToRender.DrawingSnapshot != null)
                                {
                                    Direct2DRenderWalker.Render(frameToRender.DrawingSnapshot, d2dContext);
                                }
                            }
                        }
                        catch
                        {
                            // Avoid terminating thread on graphics resets
                        }
                        finally
                        {
                            dxTarget.EndDrawAndPresent();
                            Interlocked.Increment(ref slot.PresentedCount);
                        }
                    }
                }
            }
        }

        public void Dispose()
        {
            _isRunning = false;
            _wakeSignal.Set();
            _slots.Clear();
        }
    }
}

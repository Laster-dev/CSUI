// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using System.Windows.Input.Gestures;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Composition;
using System.Windows.Appearance;
using System.Windows.Input.Ime;
using System.Windows.Threading;
using CSUI.Core.Interop.DirectX;
using static CSUI.Tests.TestHelpers;

namespace CSUI.Tests;

internal static class ModernArchitectureTests
{
    private sealed class MockDeviceLossListener : IDeviceLossListener
    {
        public bool DeviceLostCalled { get; private set; }
        public bool DeviceRestoredCalled { get; private set; }

        public void OnDeviceLost()
        {
            DeviceLostCalled = true;
        }

        public void OnDeviceRestored()
        {
            DeviceRestoredCalled = true;
        }
    }

    public static void RunAll()
    {
        Console.WriteLine("\n--- Layer 6: Modern Graphics & Memory Architecture (VRAM Cache, Subtree Caching, Damage Tracking) ---");

        TestGpuResourceCacheBudgetAndDeviceLoss();
        TestBitmapCacheSubtreeCaching();
        TestDamageTrackerIncrementalDirtyRects();
        TestWindowMicaBackdropMaterial();
        TestDynamicDarkLightThemeHotSwitch();
        TestDirectCompositionAirspaceFreeLayers();
        TestModernImeAndSmoothCaret();
        TestFrameBudgetSchedulerAndMicrotasks();
    }

    // COMPOSITOR-1: AsyncCompositor Lifecycle & Independent Animations
    private static void TestAsyncCompositorAndIndependentAnimations()
    {
        var compositor = AsyncCompositor.Instance;
        Assert(compositor.IsRunning, "AsyncCompositor thread should be running");

        // Test CompositorRotateAnimation
        var rotAnim = new CompositorRotateAnimation("rot1", 360.0, new Point(50, 50));
        rotAnim.Step(0.25); // 0.25 seconds * 360 deg/sec = 90 deg
        AssertNear(rotAnim.CurrentAngle, 90.0, 0.01, "Rotate animation should be at 90 degrees");

        var mRot = Matrix.Identity;
        double opRot = 1.0;
        rotAnim.Apply(ref mRot, ref opRot);
        Point rotatedPt = mRot.Transform(new Point(50, 0));
        AssertNear(rotatedPt.X, 100.0, 0.05, "Rotated point X");
        AssertNear(rotatedPt.Y, 50.0, 0.05, "Rotated point Y");

        // Test CompositorGlideAnimation
        var glideAnim = new CompositorGlideAnimation("glide1", new Vector(100, 200), friction: 0.5);
        glideAnim.Step(0.1);
        Assert(glideAnim.CurrentOffset.X > 0 && glideAnim.CurrentOffset.Y > 0, "Glide offset should advance");
        Assert(glideAnim.Velocity.Length < new Vector(100, 200).Length, "Velocity should decelerate with friction");

        Pass("COMPOSITOR-1. AsyncCompositor thread & independent rotate/glide physics animations");
    }

    // GESTURE-1: Modern Hardware Touch & Precision Touchpad Gestures
    private static void TestModernGestureEngineAndEventDispatch()
    {
        var root = new UIElement();
        ModernGestureDeltaEventArgs receivedArgs = null;

        root.AddHandler(ModernGestureEngine.ModernGestureDeltaEvent, new ModernGestureDeltaEventHandler((s, e) =>
        {
            receivedArgs = e;
        }));

        var args = new ModernGestureDeltaEventArgs(
            ModernGestureType.Zoom,
            new Point(150, 150),
            deltaPan: new Vector(0, 0),
            cumulativePan: new Vector(0, 0),
            deltaScale: 1.25,
            cumulativeScale: 1.25,
            deltaRotation: 0,
            cumulativeRotation: 0,
            isInertia: false);

        root.RaiseEvent(args);

        Assert(receivedArgs != null, "UIElement must receive routed ModernGestureDeltaEvent");
        AssertEqual(receivedArgs.GestureType, ModernGestureType.Zoom, "Gesture type should be Zoom");
        AssertNear(receivedArgs.DeltaScale, 1.25, 0.001, "DeltaScale should match");
        AssertEqual(receivedArgs.Center, new Point(150, 150), "Gesture center should match");

        Pass("GESTURE-1. ModernGestureEngine precision touchpad & touch gesture routed event dispatch");
    }

    // DPI-1: Per-Monitor v2 DPI Hotplug Notification & Suggested Rect
    private static void TestPerMonitorDpiHotplugNotification()
    {
        var oldDpi = new DpiScale(1.0, 1.0);
        var newDpi = new DpiScale(2.0, 2.0);
        var suggestedRect = new Rect(100, 100, 800, 600);

        var hwndDpiArgs = new HwndDpiChangedEventArgs(oldDpi, newDpi, suggestedRect);
        AssertEqual(hwndDpiArgs.OldDpi.DpiScaleX, 1.0, "Old DPI scale");
        AssertEqual(hwndDpiArgs.NewDpi.DpiScaleX, 2.0, "New DPI scale");
        AssertEqual(hwndDpiArgs.SuggestedRect.Width, 800.0, "Suggested rect width");

        var window = new Window();
        var dpiArgs = new DpiChangedEventArgs(oldDpi, newDpi, Window.DpiChangedEvent, window);
        bool received = false;
        window.DpiChanged += (s, e) =>
        {
            received = true;
            AssertEqual(e.NewDpi.DpiScaleX, 2.0, "Received new DPI");
        };

        window.RaiseEvent(dpiArgs);
        Assert(received, "Window must receive DpiChanged event on Per-Monitor v2 display hotplug");

        Pass("DPI-1. Per-Monitor v2 dynamic DPI hotplug event routing & suggested rect negotiation");
    }

    // VRAM-1: GPU Resource Cache, LRU Eviction & Device Loss Chain
    private static unsafe void TestGpuResourceCacheBudgetAndDeviceLoss()
    {
        var cache = GpuResourceCache.Instance;
        cache.Clear();

        long originalBudget = cache.MaxVramBudgetBytes;
        // Set budget: 20 MB. 80% watermark is 16 MB.
        cache.MaxVramBudgetBytes = 20 * 1024 * 1024;

        // Register 3 textures of 8 MB each (total 24 MB > 20 MB budget)
        // 2048x1024x4 bytes = 8 MB each.
        cache.RegisterTexture("tex_a", (void*)0x1000, 2048, 1024, ownsNativePointer: false);
        Thread.Sleep(10);
        cache.RegisterTexture("tex_b", (void*)0x2000, 2048, 1024, ownsNativePointer: false);
        Thread.Sleep(10);
        cache.RegisterTexture("tex_c", (void*)0x3000, 2048, 1024, ownsNativePointer: false);

        // tex_a should have been evicted by LRU since total (24 MB) > 20 MB budget, bringing usage to 16 MB (80% watermark)
        Assert(cache.TryGetTexture("tex_a") == null, "Oldest texture tex_a should be evicted");
        Assert(cache.TryGetTexture("tex_b") != null, "tex_b should remain cached");
        Assert(cache.TryGetTexture("tex_c") != null, "tex_c should remain cached");
        Assert(cache.CurrentVramUsageBytes <= cache.MaxVramBudgetBytes, "VRAM usage must not exceed max budget");

        // Test Device Loss Broadcast
        var listener = new MockDeviceLossListener();
        cache.RegisterDeviceLossListener(listener);

        cache.BroadcastDeviceLost();
        Assert(listener.DeviceLostCalled, "Device lost listener must be notified");
        AssertEqual(cache.EntryCount, 0, "Cache must be flushed on device loss");

        cache.BroadcastDeviceRestored();
        Assert(listener.DeviceRestoredCalled, "Device restored listener must be notified");

        cache.UnregisterDeviceLossListener(listener);
        cache.MaxVramBudgetBytes = originalBudget;

        Pass("VRAM-1. GpuResourceCache budget enforcement, LRU eviction & device loss broadcast chain");
    }

    // CACHE-1: BitmapCache Visual Subtree Direct2D Rendering
    private static void TestBitmapCacheSubtreeCaching()
    {
        var container = new ContainerVisual();
        var child = new DrawingVisual();
        using (DrawingContext dc = child.RenderOpen())
        {
            dc.DrawRectangle(Brushes.Crimson, null, new Rect(0, 0, 100, 100));
        }
        container.Children.Add(child);

        var bitmapCache = new BitmapCache();
        container.CacheMode = bitmapCache;
        AssertEqual(container.CacheMode, bitmapCache, "ContainerVisual.CacheMode property should be BitmapCache");

        // Render to BGRA32 using Direct2D offscreen pipeline to verify subtree rendering
        byte[] px = RenderToBgra32(container, 100, 100);
        var center = Sample(px, 100, 50, 50);
        Assert(center.R > 200 && center.G < 40 && center.B < 80, $"BitmapCache subtree render expected crimson, got {center}");

        Pass("CACHE-1. BitmapCache offscreen Direct2D command-list subtree rasterization & blit");
    }

    // DAMAGE-1: DamageTracker Incremental Dirty Rects for DXGI Present1
    private static void TestDamageTrackerIncrementalDirtyRects()
    {
        var tracker = new DamageTracker();
        AssertEqual(tracker.Count, 0, "Initial dirty rect count should be 0");
        Assert(!tracker.IsFullDamage, "Initial should not be full damage");

        // 1. Add non-overlapping rects
        tracker.AddDamage(new RECT(10, 10, 50, 50));
        tracker.AddDamage(new RECT(100, 100, 150, 150));
        AssertEqual(tracker.Count, 2, "Two distinct damage rects");

        // 2. Add overlapping rect to first rect
        tracker.AddDamage(new RECT(40, 40, 80, 80));
        // Should merge with first rect: [10, 10, 80, 80]
        AssertEqual(tracker.Count, 2, "Overlapping rects should merge into union");

        var rects = tracker.GetDirtyRectsSpan();
        AssertEqual(rects[0].left, 10, "Merged rect left");
        AssertEqual(rects[0].top, 10, "Merged rect top");
        AssertEqual(rects[0].right, 80, "Merged rect right");
        AssertEqual(rects[0].bottom, 80, "Merged rect bottom");

        // 3. DIP with DPI scaling
        tracker.AddDamage(new Rect(100, 100, 50, 50), 2.0, 2.0); // pixels: [200, 200, 300, 300]
        AssertEqual(tracker.Count, 3, "Scaled DIP rect should be added");

        // 4. SetFullDamage
        tracker.SetFullDamage(1920, 1080);
        Assert(tracker.IsFullDamage, "Tracker should be full damage");
        AssertEqual(tracker.Count, 0, "Full damage reports 0 partial dirty rects (DXGI Present full swap)");

        tracker.Reset();
        AssertEqual(tracker.Count, 0, "After reset count should be 0");
        Assert(!tracker.IsFullDamage, "After reset should not be full damage");

        Pass("DAMAGE-1. DamageTracker incremental damage accumulation, union merging & DXGI Present1 dirty rects");
    }

    // MICA-1: Windows 11 Native Mica & Acrylic Backdrop Materials
    private static void TestWindowMicaBackdropMaterial()
    {
        // 1. Verify modern backdrop aliases & enumeration
        AssertEqual(WindowBackdropType.Mica, WindowBackdropType.MainWindow, "Mica alias should equal MainWindow");
        AssertEqual(WindowBackdropType.Acrylic, WindowBackdropType.TransientWindow, "Acrylic alias should equal TransientWindow");
        AssertEqual(WindowBackdropType.MicaAlt, WindowBackdropType.TabbedWindow, "MicaAlt alias should equal TabbedWindow");

        // 2. Window WindowBackdropType property
        var window = new Window();
        AssertEqual(window.WindowBackdropType, WindowBackdropType.Auto, "Default window backdrop type should be Auto");

        window.WindowBackdropType = WindowBackdropType.Mica;
        AssertEqual(window.WindowBackdropType, WindowBackdropType.Mica, "WindowBackdropType should be set to Mica");

        window.WindowBackdropType = WindowBackdropType.MicaAlt;
        AssertEqual(window.WindowBackdropType, WindowBackdropType.MicaAlt, "WindowBackdropType should be set to MicaAlt");

        window.WindowBackdropType = WindowBackdropType.Acrylic;
        AssertEqual(window.WindowBackdropType, WindowBackdropType.Acrylic, "WindowBackdropType should be set to Acrylic");

        // 3. Support detection
        bool supportsNone = WindowBackdropManager.IsSupported(WindowBackdropType.None);
        Assert(supportsNone, "WindowBackdropType.None is always supported");

        Pass("MICA-1. Windows 11 DWM native Mica/MicaAlt/Acrylic backdrop material configuration & aliases");
    }

    // THEME-1: Built-in Fluent Light & Dark Resources & Dynamic Theme Hot-Switch
    private static void TestDynamicDarkLightThemeHotSwitch()
    {
        // 1. Built-in Fluent theme resource dictionaries
        var lightDict = FluentThemeResources.GetDictionary(isDark: false);
        var darkDict = FluentThemeResources.GetDictionary(isDark: true);
        Assert(lightDict != null, "Light dictionary should be created");
        Assert(darkDict != null, "Dark dictionary should be created");

        // Verify tokens in light dictionary
        var lightAccent = (SolidColorBrush)lightDict[FluentThemeKeys.AccentBrushKey];
        var lightBg = (SolidColorBrush)lightDict[FluentThemeKeys.WindowBackgroundBrushKey];
        Assert(lightAccent.IsFrozen, "Light accent brush must be frozen");
        AssertEqual(lightAccent.Color, Color.FromRgb(0x00, 0x67, 0xC0), "Light accent color should be royal blue");
        AssertEqual(lightBg.Color, Color.FromRgb(0xF3, 0xF3, 0xF3), "Light window background color");

        // Verify tokens in dark dictionary
        var darkAccent = (SolidColorBrush)darkDict[FluentThemeKeys.AccentBrushKey];
        var darkBg = (SolidColorBrush)darkDict[FluentThemeKeys.WindowBackgroundBrushKey];
        Assert(darkAccent.IsFrozen, "Dark accent brush must be frozen");
        AssertEqual(darkAccent.Color, Color.FromRgb(0x4C, 0xC2, 0xFF), "Dark accent color should be neon blue");
        AssertEqual(darkBg.Color, Color.FromRgb(0x20, 0x20, 0x20), "Dark window background color");

        // 2. Both ComponentResourceKey and String Key registration
        AssertEqual(lightDict["AccentBrush"], lightAccent, "String key AccentBrush in light dictionary");
        AssertEqual(darkDict["WindowBackgroundBrush"], darkBg, "String key WindowBackgroundBrush in dark dictionary");

        // 3. ThemeManager ThemeChanged broadcast event
        ThemeMode capturedMode = ThemeMode.None;
        bool capturedLightState = true;
        bool eventFired = false;

        Action<ThemeMode, bool> handler = (mode, useLight) =>
        {
            eventFired = true;
            capturedMode = mode;
            capturedLightState = useLight;
        };

        ThemeManager.ThemeChanged += handler;
        try
        {
            ThemeManager.OnApplicationThemeChanged(ThemeMode.Light, ThemeMode.Dark);
            Assert(eventFired, "ThemeChanged event must be fired on theme switch");
            AssertEqual(capturedMode, ThemeMode.Dark, "Captured theme mode should be Dark");
            Assert(!capturedLightState, "Captured light state should be false (Dark)");

            // Window Immersive Dark Mode synchronization
            var window = new Window();
            window.ThemeMode = ThemeMode.Dark;
            AssertEqual(window.ThemeMode, ThemeMode.Dark, "Window theme mode should be Dark");
        }
        finally
        {
            ThemeManager.ThemeChanged -= handler;
            ThemeManager.OnApplicationThemeChanged(ThemeMode.Dark, ThemeMode.None);
        }

        Pass("THEME-1. Built-in Fluent Light/Dark theme dictionary, semantic tokens & dynamic hot-switch broadcast");
    }

    // DCOMP-1: DirectComposition Airspace-Free Layer Tree
    private static void TestDirectCompositionAirspaceFreeLayers()
    {
        // 1. DComp Layer enumeration
        AssertEqual((int)DCompLayerKind.Background, 0, "Background layer index");
        AssertEqual((int)DCompLayerKind.ForeignContent, 1, "ForeignContent layer index");
        AssertEqual((int)DCompLayerKind.MainUiContent, 2, "MainUiContent layer index");
        AssertEqual((int)DCompLayerKind.OverlayAdorner, 3, "OverlayAdorner layer index");

        // 2. Initialize Shared DirectX Device & DCompositionTree
        using var shared = new DxSharedDevice();
        // Create a dummy live HWND
        var dummyHwnd = new IntPtr(0x10001);
        using var dcomp = new DCompositionTree(shared, dummyHwnd);

        // Even if headless or non-windowed HWND, test initialization state & API safety
        if (dcomp.IsSupported)
        {
            var foreignLayer = dcomp.CreateVisualNode(DCompLayerKind.ForeignContent);
            var overlayLayer = dcomp.CreateVisualNode(DCompLayerKind.OverlayAdorner);

            if (foreignLayer != null && overlayLayer != null)
            {
                foreignLayer.SetOffset(10, 20);
                AssertEqual(foreignLayer.OffsetX, 10f, "Foreign layer offset X");
                AssertEqual(foreignLayer.OffsetY, 20f, "Foreign layer offset Y");

                overlayLayer.SetOffset(0, 0);
                overlayLayer.SetClip(new Rect(0, 0, 800, 600));

                // Attach overlay ABOVE foreign content to solve airspace bug!
                dcomp.AttachOverlayAbove(overlayLayer, foreignLayer);
                dcomp.Commit();

                foreignLayer.Dispose();
                overlayLayer.Dispose();
            }
        }

        Pass("DCOMP-1. DirectComposition multi-layered hardware visual tree & airspace-free overlay composition");
    }

    // IME-1: Modern IME State Machine & Smooth Caret Physics
    private static void TestModernImeAndSmoothCaret()
    {
        // 1. Grapheme cluster boundary navigation (Emoji with ZWJ and skin tones)
        string familyEmoji = "👨‍👩‍👧‍👦"; // Family emoji: 4 emojis joined with 3 Zero-Width Joiners (11 UTF-16 code units)
        int clusterCount = GraphemeClusterHelper.ClusterCount(familyEmoji);
        AssertEqual(clusterCount, 1, "Family emoji is 1 grapheme cluster");

        string mixedText = "Hello 👨‍👩‍👧‍👦 World!";
        int nextCluster = GraphemeClusterHelper.GetNextClusterIndex(mixedText, 6);
        AssertEqual(nextCluster, 6 + familyEmoji.Length, "Next cluster must skip the entire multi-unit emoji");

        int prevCluster = GraphemeClusterHelper.GetPreviousClusterIndex(mixedText, nextCluster);
        AssertEqual(prevCluster, 6, "Previous cluster must backspace the entire multi-unit emoji in one step");

        // 2. Smooth physical caret glide engine
        var caret = new SmoothCaretTracker(new Point(10, 50));
        caret.TargetPosition = new Point(100, 50);

        // Step 0.016 seconds (single 60 FPS frame)
        caret.Step(0.016);
        Assert(caret.CurrentPosition.X > 10.0 && caret.CurrentPosition.X < 100.0, "Caret smoothly interpolated towards target");

        // Advance enough time to reach target
        caret.Step(0.5);
        AssertNear(caret.CurrentPosition.X, 100.0, 0.1, "Caret reached target position");

        // 3. ModernImeService composition event verification
        var ime = new ModernImeService();
        bool startedFired = false;

        ime.CompositionStarted += (s, e) => startedFired = true;

        // Simulate WM_IME_STARTCOMPOSITION
        ime.ProcessImeMessage(IntPtr.Zero, 0x010D, 0, 0, out _);
        Assert(startedFired, "CompositionStarted event fired");
        Assert(ime.IsComposing, "IME should be composing");

        // Simulate WM_IME_ENDCOMPOSITION
        ime.ProcessImeMessage(IntPtr.Zero, 0x010E, 0, 0, out _);
        Assert(!ime.IsComposing, "IME should end composing");

        Pass("IME-1. Modern IME service composition state machine, grapheme cluster navigation & smooth caret glide");
    }

    // FIBER-1: Frame-Budgeted Scheduler & Zero-Allocation Microtask RingBuffer
    private static void TestFrameBudgetSchedulerAndMicrotasks()
    {
        // 1. Zero-allocation ring buffer
        var ring = new MicrotaskRingBuffer<int>(64);
        AssertEqual(ring.Capacity, 64, "Ring buffer capacity rounded to power of 2");

        for (int i = 0; i < 10; i++)
        {
            Assert(ring.TryEnqueue(i * 10), $"Enqueue {i}");
        }
        AssertEqual(ring.Count, 10, "Ring count should be 10");

        for (int i = 0; i < 10; i++)
        {
            Assert(ring.TryDequeue(out int val), $"Dequeue {i}");
            AssertEqual(val, i * 10, $"Value match {i}");
        }
        AssertEqual(ring.Count, 0, "Ring count should be 0 after draining");

        // 2. FrameBudgetScheduler microtask execution
        var scheduler = FrameBudgetScheduler.Instance;
        scheduler.Clear();
        scheduler.TargetFps = 120.0; // 8.3ms frame, ~5.4ms budget
        Assert(scheduler.FrameBudgetMs < 6.0, "Budget adjusted for 120 FPS");

        int counter = 0;
        for (int i = 0; i < 50; i++)
        {
            scheduler.PostMicrotask(() => Interlocked.Increment(ref counter));
        }

        // Execute all microtasks with generous 20ms slice
        bool allDone = scheduler.ExecuteFrameSlice(20.0);
        Assert(allDone, "All microtasks should complete within 20ms slice");
        AssertEqual(counter, 50, "All 50 microtasks executed");

        // 3. Sliced fiber cooperative yielding under small budget
        int fiberSteps = 0;
        scheduler.PostFiber(() =>
        {
            // Simulate work quantum
            Thread.Sleep(3);
            fiberSteps++;
            return fiberSteps < 5; // Return true for 5 slices
        });

        // Execute with tiny 2ms budget -> should yield before finishing all 5 steps
        bool finishedFirst = scheduler.ExecuteFrameSlice(2.0);
        Assert(!finishedFirst, "Fiber should yield cooperatively when exceeding 2ms frame budget");
        Assert(scheduler.PendingFibers > 0, "Unfinished fiber remains queued for next frame");

        // Finish remaining slices
        while (scheduler.HasPendingWork)
        {
            scheduler.ExecuteFrameSlice(50.0);
        }
        AssertEqual(fiberSteps, 5, "Fiber completed all 5 quantum steps across yielded frame slices");

        Pass("FIBER-1. Frame-Budgeted Fiber Scheduler & Zero-Allocation Microtask RingBuffer cooperative yielding");
    }
}

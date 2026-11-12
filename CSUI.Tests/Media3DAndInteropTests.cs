using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Threading;
using static CSUI.Tests.TestHelpers;

namespace CSUI.Tests;

internal static unsafe class Media3DAndInteropTests
{
    private static readonly RoutedEvent TestPingEvent = EventManager.RegisterRoutedEvent(
        "TestPing",
        RoutingStrategy.Bubble,
        typeof(RoutedEventHandler),
        typeof(Media3DAndInteropTests));

    public static void RunAll()
    {
        TestHostVisualAndVisualTarget();
        TestViewport3DPerspectiveLitGradientAndNegativeSpace();
        TestViewport3DOrthographicInfinityAndMatrixCamera();
        TestViewport3DDepthOcclusion();
        TestViewport3DBackMaterialAndNegativeScaleMirror();
        TestViewport3DEmissiveNoLights();
        TestViewport3DSemiTransparentBlend();
        TestViewport3DElementLayoutAndAutomation();
        TestMedia3DAnimationsAndKeyFrames();
        TestRayHitTestAndUIElement3DRoutedEvents();
        TestViewport2DVisual3DRenderAndHitTest();
        TestD3DImageInteropAndNegativeLockCheck();
        TestLiveHwndSourceAndHwndTargetWindow();
    }

    // M3D-1: HostVisual + VisualTarget (cross-thread D2D synthesis)
    private static void TestHostVisualAndVisualTarget()
    {
        var host = new HostVisual();
        using var readyEvent = new ManualResetEventSlim(false);
        Exception? workerError = null;
        Dispatcher? workerDispatcher = null;
        VisualTarget? target = null;

        var worker = new Thread(() =>
        {
            try
            {
                workerDispatcher = Dispatcher.CurrentDispatcher;
                target = new VisualTarget(host);
                var dv = new DrawingVisual();
                using (DrawingContext dc = dv.RenderOpen())
                {
                    dc.DrawRectangle(new SolidColorBrush(Color.FromArgb(255, 0, 176, 240)), null, new Rect(20, 20, 60, 60));
                }

                target.RootVisual = dv;

                void OnRendering(object? s, EventArgs e)
                {
                    CompositionTarget.Rendering -= OnRendering;
                    readyEvent.Set();
                }
                CompositionTarget.Rendering += OnRendering;
                var timer = new DispatcherTimer(TimeSpan.FromMilliseconds(16), DispatcherPriority.Render, (_, _) => { }, workerDispatcher);
                timer.Start();

                Dispatcher.Run();
            }
            catch (Exception ex)
            {
                workerError = ex;
                readyEvent.Set();
            }
            finally
            {
                target?.Dispose();
            }
        });
        if (OperatingSystem.IsWindows())
            worker.SetApartmentState(ApartmentState.STA);
        worker.IsBackground = true;
        worker.Start();

        try
        {
            Assert(readyEvent.Wait(TimeSpan.FromSeconds(5)), "Worker VisualTarget frame timed out");
            if (workerError != null)
                throw new InvalidOperationException("Worker thread threw exception", workerError);

            bool rendered = false;
            var sw = Stopwatch.StartNew();
            (byte B, byte G, byte R, byte A) center = default;
            (byte B, byte G, byte R, byte A) corner = default;

            while (sw.ElapsedMilliseconds < 3000)
            {
                Dispatcher.CurrentDispatcher.Invoke(DispatcherPriority.Background, static () => { });
                byte[] pixels = RenderToBgra32(host, 100, 100);
                center = Sample(pixels, 100, 50, 50);
                corner = Sample(pixels, 100, 5, 5);
                if (center.A == 255 && center.B >= 235 && center.G >= 170 && center.R <= 5)
                {
                    rendered = true;
                    break;
                }
                Thread.Sleep(20);
            }

            Assert(rendered, $"HostVisual did not publish cyan pixel in time; last center={center}, corner={corner}");
            Assert(corner.A == 0, $"HostVisual outer corner expected transparent, got {corner}");
            Pass("M3D-1. HostVisual + VisualTarget cross-thread D2D synthesis", $"center={center}, corner={corner}");
        }
        finally
        {
            workerDispatcher?.InvokeShutdown();
            worker.Join(TimeSpan.FromSeconds(3));
        }
    }

    // M3D-2A: PerspectiveCamera + LinearGradientBrush + Specular + 4 Light Types
    private static void TestViewport3DPerspectiveLitGradientAndNegativeSpace()
    {
        var vp = new Viewport3DVisual
        {
            Viewport = new Rect(0, 0, 128, 128),
            Camera = new PerspectiveCamera(new Point3D(0, 0, 3.5), new Vector3D(0, 0, -1), new Vector3D(0, 1, 0), 45.0)
        };

        var lights = new Model3DGroup();
        lights.Children.Add(new AmbientLight(Color.FromRgb(40, 40, 40)));
        lights.Children.Add(new DirectionalLight(Color.FromRgb(180, 180, 180), new Vector3D(0.3, 0, -1)));
        lights.Children.Add(new PointLight(Color.FromRgb(70, 70, 70), new Point3D(0, 0, 2.5)));
        lights.Children.Add(new SpotLight(Color.FromRgb(60, 60, 60), new Point3D(0, 0, 2.5), new Vector3D(0, 0, -1), 45, 30));

        var grad = new LinearGradientBrush(Colors.Red, Colors.Blue, new Point(0, 0.5), new Point(1, 0.5));
        grad.Freeze();
        var matGroup = new MaterialGroup();
        matGroup.Children.Add(new DiffuseMaterial(grad));
        matGroup.Children.Add(new SpecularMaterial(new SolidColorBrush(Color.FromRgb(90, 90, 90)), 96.0));

        var quad = new GeometryModel3D(CreateQuadMesh(1.0, 0.0), matGroup);
        var rootGroup = new Model3DGroup();
        rootGroup.Children.Add(lights);
        rootGroup.Children.Add(quad);

        vp.Children.Add(new ModelVisual3D { Content = rootGroup });

        byte[] px = RenderToBgra32(vp, 128, 128);
        var left = Sample(px, 128, 30, 64);
        var right = Sample(px, 128, 98, 64);
        var center = Sample(px, 128, 64, 64);
        var corner = Sample(px, 128, 2, 2);

        Assert(left.A == 255 && left.R > left.B + 40, $"Left side of gradient quad expected red-dominant, got {left}");
        Assert(right.A == 255 && right.B > right.R + 40, $"Right side of gradient quad expected blue-dominant, got {right}");
        Assert(center.A == 255 && center.R > 60 && center.B > 60, $"Center pixel expected gradient+specular mix, got {center}");
        Assert(corner.A == 0, $"Outside quad corner expected transparent, got {corner}");

        Pass("M3D-2A. PerspectiveCamera + Gradient + Specular + 4 Light Types",
            $"left={left}, center={center}, right={right}");
    }

    // M3D-2B: OrthographicCamera (+Infinity far plane) & MatrixCamera
    private static void TestViewport3DOrthographicInfinityAndMatrixCamera()
    {
        var ortho = new OrthographicCamera(new Point3D(0, 0, 5), new Vector3D(0, 0, -1), new Vector3D(0, 1, 0), 4.0);
        Assert(double.IsPositiveInfinity(ortho.FarPlaneDistance), "Default OrthographicCamera FarPlaneDistance should be +Infinity");

        var vp = new Viewport3DVisual
        {
            Viewport = new Rect(0, 0, 100, 100),
            Camera = ortho
        };
        var model = new GeometryModel3D(CreateQuadMesh(1.0, 0.0), new EmissiveMaterial(new SolidColorBrush(Color.FromRgb(10, 200, 120))));
        vp.Children.Add(new ModelVisual3D { Content = model });

        byte[] orthoPx = RenderToBgra32(vp, 100, 100);
        var orthoCenter = Sample(orthoPx, 100, 50, 50);
        var orthoEdgeInside = Sample(orthoPx, 100, 27, 50);
        var orthoOutside = Sample(orthoPx, 100, 15, 50);

        Assert(orthoCenter.A == 255 && orthoCenter.G >= 195 && orthoCenter.B >= 115, $"Ortho center unexpected: {orthoCenter}");
        Assert(orthoEdgeInside.A == 255 && orthoEdgeInside.G >= 195, $"Ortho inside edge unexpected: {orthoEdgeInside}");
        Assert(orthoOutside.A == 0, $"Ortho outside quad expected transparent, got {orthoOutside}");

        var proj = new Matrix3D(
            0.5, 0, 0, 0,
            0, 0.5, 0, 0,
            0, 0, -0.5, 0,
            0, 0, 0.5, 1);
        vp.Camera = new MatrixCamera(Matrix3D.Identity, proj);
        byte[] matPx = RenderToBgra32(vp, 100, 100);
        var matCenter = Sample(matPx, 100, 50, 50);
        var matOutside = Sample(matPx, 100, 15, 50);
        Assert(matCenter.A == 255 && matCenter.G >= 195, $"MatrixCamera center unexpected: {matCenter}");
        Assert(matOutside.A == 0, $"MatrixCamera outside unexpected: {matOutside}");

        Pass("M3D-2B. OrthographicCamera (+Inf far plane) & MatrixCamera",
            $"ortho={orthoCenter}, matrix={matCenter}");
    }

    // M3D-2C: Z-Buffer Depth Occlusion
    private static void TestViewport3DDepthOcclusion()
    {
        var vp = new Viewport3DVisual
        {
            Viewport = new Rect(0, 0, 100, 100),
            Camera = new PerspectiveCamera(new Point3D(0, 0, 5), new Vector3D(0, 0, -1), new Vector3D(0, 1, 0), 45)
        };

        var nearGreen = new GeometryModel3D(CreateQuadMesh(0.5, 1.0), new EmissiveMaterial(Brushes.Lime));
        var farRed = new GeometryModel3D(CreateQuadMesh(1.5, -1.0), new EmissiveMaterial(Brushes.Red));

        var group = new Model3DGroup();
        group.Children.Add(nearGreen);
        group.Children.Add(farRed);
        vp.Children.Add(new ModelVisual3D { Content = group });

        byte[] px = RenderToBgra32(vp, 100, 100);
        var center = Sample(px, 100, 50, 50);
        var outerFarRed = Sample(px, 100, 25, 50);

        Assert(center.A == 255 && center.G >= 245 && center.R <= 5 && center.B <= 5, $"Depth center expected near lime, got {center}");
        Assert(outerFarRed.A == 255 && outerFarRed.R >= 245 && outerFarRed.G <= 5, $"Depth outer expected far red, got {outerFarRed}");
        Pass("M3D-2C. Z-Buffer depth occlusion", $"center={center}, outer={outerFarRed}");
    }

    // M3D-2D: BackMaterial & negative-determinant ScaleTransform3D(-1,1,1) mirror
    private static void TestViewport3DBackMaterialAndNegativeScaleMirror()
    {
        var vp = new Viewport3DVisual
        {
            Viewport = new Rect(0, 0, 100, 100),
            Camera = new PerspectiveCamera(new Point3D(0, 0, 5), new Vector3D(0, 0, -1), new Vector3D(0, 1, 0), 45)
        };

        var backQuad = new GeometryModel3D(CreateQuadMesh(0.7, 0.0, ccwFront: false), new EmissiveMaterial(Brushes.Red))
        {
            BackMaterial = new EmissiveMaterial(Brushes.Cyan),
            Transform = new TranslateTransform3D(-0.9, 0, 0)
        };

        var mirroredGroup = new Transform3DGroup();
        mirroredGroup.Children.Add(new ScaleTransform3D(-1, 1, 1));
        mirroredGroup.Children.Add(new TranslateTransform3D(0.9, 0, 0));
        var mirrorQuad = new GeometryModel3D(CreateQuadMesh(0.7, 0.0, ccwFront: true), new EmissiveMaterial(Brushes.Yellow))
        {
            Transform = mirroredGroup
        };

        var root = new Model3DGroup();
        root.Children.Add(backQuad);
        root.Children.Add(mirrorQuad);
        vp.Children.Add(new ModelVisual3D { Content = root });

        byte[] px = RenderToBgra32(vp, 100, 100);
        var left = Sample(px, 100, 30, 50);
        var right = Sample(px, 100, 70, 50);

        Assert(left.A == 255 && left.G >= 245 && left.B >= 245 && left.R <= 5, $"BackMaterial expected cyan, got {left}");
        Assert(right.A == 255 && right.R >= 245 && right.G >= 245 && right.B <= 5, $"Mirrored scale expected yellow, got {right}");
        Pass("M3D-2D. BackMaterial & negative-determinant mirror winding", $"back={left}, mirror={right}");
    }

    // M3D-2E: EmissiveMaterial with zero lights
    private static void TestViewport3DEmissiveNoLights()
    {
        var vp = new Viewport3DVisual
        {
            Viewport = new Rect(0, 0, 80, 80),
            Camera = new PerspectiveCamera(new Point3D(0, 0, 4), new Vector3D(0, 0, -1), new Vector3D(0, 1, 0), 45)
        };
        vp.Children.Add(new ModelVisual3D
        {
            Content = new GeometryModel3D(CreateQuadMesh(1.0, 0.0), new EmissiveMaterial(Brushes.Yellow))
        });

        var c = RenderAndSamplePixel(vp, 80, 80, 40, 40);
        Assert(c.A == 255 && c.R == 255 && c.G == 255 && c.B == 0, $"Zero-light EmissiveMaterial expected (0,255,255,255), got {c}");
        Pass("M3D-2E. EmissiveMaterial with 0 lights", $"center={c}");
    }

    // M3D-2F: Semi-transparent blend over 2D backdrop
    private static void TestViewport3DSemiTransparentBlend()
    {
        var host = new ContainerVisual();
        var bg = new DrawingVisual();
        using (DrawingContext dc = bg.RenderOpen())
        {
            dc.DrawRectangle(Brushes.Blue, null, new Rect(0, 0, 100, 100));
        }

        var vp = new Viewport3DVisual
        {
            Viewport = new Rect(0, 0, 100, 100),
            Camera = new PerspectiveCamera(new Point3D(0, 0, 4), new Vector3D(0, 0, -1), new Vector3D(0, 1, 0), 45)
        };
        var red50 = new SolidColorBrush(Color.FromArgb(128, 255, 0, 0));
        red50.Freeze();
        vp.Children.Add(new ModelVisual3D
        {
            Content = new GeometryModel3D(CreateQuadMesh(1.2, 0.0), new EmissiveMaterial(red50))
        });

        host.Children.Add(bg);
        host.Children.Add(vp);

        byte[] px = RenderToBgra32(host, 100, 100);
        var center = Sample(px, 100, 50, 50);
        var corner = Sample(px, 100, 2, 2);

        Assert(center.A == 255 && center.R > 90 && center.B > 90 && center.G < 10, $"50% red over blue expected purple, got {center}");
        Assert(corner.A == 255 && corner.B == 255 && corner.R == 0, $"Corner expected pure blue backdrop, got {corner}");
        Pass("M3D-2F. Translucent 3D over 2D backdrop", $"center={center}, corner={corner}");
    }

    // M3D-2G: Viewport3D element Measure/Arrange/Render & Viewport3DAutomationPeer
    private static void TestViewport3DElementLayoutAndAutomation()
    {
        var vp3d = new Viewport3D
        {
            Width = 120,
            Height = 120,
            Camera = new PerspectiveCamera(new Point3D(0, 0, 4), new Vector3D(0, 0, -1), new Vector3D(0, 1, 0), 45)
        };
        vp3d.Children.Add(new ModelVisual3D
        {
            Content = new GeometryModel3D(CreateQuadMesh(1.0, 0.0), new EmissiveMaterial(Brushes.OrangeRed))
        });

        vp3d.Measure(new Size(120, 120));
        vp3d.Arrange(new Rect(0, 0, 120, 120));

        AutomationPeer peer = UIElementAutomationPeer.CreatePeerForElement(vp3d);
        Assert(peer is Viewport3DAutomationPeer, $"Expected Viewport3DAutomationPeer, got {peer?.GetType().Name}");

        var c = RenderAndSamplePixel(vp3d, 120, 120, 60, 60);
        Assert(c.A == 255 && c.R >= 245 && c.G > 40 && c.G < 110 && c.B < 10, $"Viewport3D element center expected OrangeRed, got {c}");
        Pass("M3D-2G. Viewport3D layout + Viewport3DAutomationPeer + Render", $"center={c}");
    }

    // M3D-3: Media3D Animations & KeyFrames
    private static void TestMedia3DAnimationsAndKeyFrames()
    {
        var dur = new Duration(TimeSpan.FromSeconds(1.0));

        var p3Anim = new Point3DAnimation(new Point3D(0, 0, 0), new Point3D(10, 20, -30), dur);
        AnimationClock p3Clock = p3Anim.CreateClock();
        p3Clock.Controller!.SeekAlignedToLastTick(TimeSpan.FromSeconds(0.5), TimeSeekOrigin.BeginTime);
        p3Clock.Controller.Pause();
        Point3D pMid = (Point3D)p3Anim.GetCurrentValue(new Point3D(), new Point3D(), p3Clock);
        AssertNear(pMid.X, 5.0, 1e-4, "Point3DAnimation mid X");
        AssertNear(pMid.Y, 10.0, 1e-4, "Point3DAnimation mid Y");
        AssertNear(pMid.Z, -15.0, 1e-4, "Point3DAnimation mid Z");

        var v3Anim = new Vector3DAnimation(new Vector3D(2, 4, 6), new Vector3D(12, 24, 36), dur);
        AnimationClock v3Clock = v3Anim.CreateClock();
        v3Clock.Controller!.SeekAlignedToLastTick(TimeSpan.FromSeconds(0.25), TimeSeekOrigin.BeginTime);
        var trans = new TranslateTransform3D();
        trans.ApplyAnimationClock(TranslateTransform3D.OffsetXProperty, new DoubleAnimation(1.0, 5.0, dur).CreateClock());
        Vector3D vQtr = (Vector3D)v3Anim.GetCurrentValue(new Vector3D(), new Vector3D(), v3Clock);
        AssertNear(vQtr.X, 4.5, 1e-4, "Vector3DAnimation qtr X");
        AssertNear(vQtr.Z, 13.5, 1e-4, "Vector3DAnimation qtr Z");

        var rKf = new Rotation3DAnimationUsingKeyFrames();
        rKf.KeyFrames.Add(new LinearRotation3DKeyFrame(new AxisAngleRotation3D(new Vector3D(0, 1, 0), 90), KeyTime.FromTimeSpan(TimeSpan.FromSeconds(1.0))));
        AnimationClock rKfClock = rKf.CreateClock();
        rKfClock.Controller!.SeekAlignedToLastTick(TimeSpan.FromSeconds(0.5), TimeSeekOrigin.BeginTime);
        Rotation3D rKfMid = (Rotation3D)rKf.GetCurrentValue(Rotation3D.Identity, Rotation3D.Identity, rKfClock);
        Point3D rKfPt = new RotateTransform3D(rKfMid).Value.Transform(new Point3D(0, 0, 1));
        AssertNear(rKfPt.X, Math.Sin(Math.PI / 4.0), 1e-3, "Rotation3DAnimationUsingKeyFrames mid X");
        AssertNear(rKfPt.Z, Math.Cos(Math.PI / 4.0), 1e-3, "Rotation3DAnimationUsingKeyFrames mid Z");

        var discV = new Vector3DAnimationUsingKeyFrames();
        discV.KeyFrames.Add(new DiscreteVector3DKeyFrame(new Vector3D(9, 8, 7), KeyTime.FromTimeSpan(TimeSpan.FromSeconds(0.5))));
        AnimationClock discClock = discV.CreateClock();
        discClock.Controller!.SeekAlignedToLastTick(TimeSpan.FromSeconds(0.49), TimeSeekOrigin.BeginTime);
        Vector3D discBefore = (Vector3D)discV.GetCurrentValue(new Vector3D(1, 2, 3), new Vector3D(1, 2, 3), discClock);
        AssertNear(discBefore.X, 1.0, 1e-4, "DiscreteVector3DKeyFrame before keytime");
        discClock.Controller.SeekAlignedToLastTick(TimeSpan.FromSeconds(0.5), TimeSeekOrigin.BeginTime);
        Vector3D discAt = (Vector3D)discV.GetCurrentValue(new Vector3D(1, 2, 3), new Vector3D(1, 2, 3), discClock);
        AssertNear(discAt.X, 9.0, 1e-4, "DiscreteVector3DKeyFrame at keytime");

        var qFrom = new Quaternion(new Vector3D(0, 1, 0), 0);
        var qTo = new Quaternion(new Vector3D(0, 1, 0), 90);
        var qAnim = new QuaternionAnimation(qFrom, qTo, dur);
        AnimationClock qClock = qAnim.CreateClock();
        qClock.Controller!.SeekAlignedToLastTick(TimeSpan.FromSeconds(0.5), TimeSeekOrigin.BeginTime);
        Quaternion qMid = (Quaternion)qAnim.GetCurrentValue(Quaternion.Identity, Quaternion.Identity, qClock);
        AssertNear(qMid.Angle, 45.0, 1e-3, "QuaternionAnimation mid Angle");

        var rotFrom = new AxisAngleRotation3D(new Vector3D(0, 0, 1), 0);
        var rotTo = new AxisAngleRotation3D(new Vector3D(0, 0, 1), 180);
        var rotAnim = new Rotation3DAnimation(rotFrom, rotTo, dur);
        AnimationClock rotClock = rotAnim.CreateClock();
        rotClock.Controller!.SeekAlignedToLastTick(TimeSpan.FromSeconds(0.5), TimeSeekOrigin.BeginTime);
        Rotation3D rotMid = (Rotation3D)rotAnim.GetCurrentValue(Rotation3D.Identity, Rotation3D.Identity, rotClock);
        var rotMat = new RotateTransform3D(rotMid).Value;
        Point3D rotated = rotMat.Transform(new Point3D(1, 0, 0));
        AssertNear(rotated.X, 0.0, 1e-3, "Rotation3DAnimation 90deg X");
        AssertNear(rotated.Y, 1.0, 1e-3, "Rotation3DAnimation 90deg Y");

        var vKf = new Vector3DAnimationUsingKeyFrames();
        vKf.KeyFrames.Add(new LinearVector3DKeyFrame(new Vector3D(0, 0, 0), KeyTime.FromTimeSpan(TimeSpan.Zero)));
        vKf.KeyFrames.Add(new LinearVector3DKeyFrame(new Vector3D(10, 0, 0), KeyTime.FromTimeSpan(TimeSpan.FromSeconds(1.0))));
        AnimationClock vKfClock = vKf.CreateClock();
        vKfClock.Controller!.SeekAlignedToLastTick(TimeSpan.FromSeconds(0.3), TimeSeekOrigin.BeginTime);
        Vector3D vKfVal = (Vector3D)vKf.GetCurrentValue(new Vector3D(), new Vector3D(), vKfClock);
        AssertNear(vKfVal.X, 3.0, 1e-4, "LinearVector3DKeyFrame 0.3s X");

        var pKf = new Point3DAnimationUsingKeyFrames();
        pKf.KeyFrames.Add(new EasingPoint3DKeyFrame(
            new Point3D(100, 0, 0),
            KeyTime.FromTimeSpan(TimeSpan.FromSeconds(1.0)),
            new QuadraticEase { EasingMode = EasingMode.EaseIn }));
        AnimationClock pKfClock = pKf.CreateClock();
        pKfClock.Controller!.SeekAlignedToLastTick(TimeSpan.FromSeconds(0.5), TimeSeekOrigin.BeginTime);
        Point3D pKfVal = (Point3D)pKf.GetCurrentValue(new Point3D(0, 0, 0), new Point3D(0, 0, 0), pKfClock);
        AssertNear(pKfVal.X, 25.0, 1e-4, "EasingPoint3DKeyFrame QuadraticEaseIn 0.5s X");

        var qKf = new QuaternionAnimationUsingKeyFrames();
        qKf.KeyFrames.Add(new SplineQuaternionKeyFrame(
            new Quaternion(new Vector3D(1, 0, 0), 90),
            KeyTime.FromTimeSpan(TimeSpan.FromSeconds(1.0)),
            new KeySpline(0.25, 0.1, 0.25, 1.0)));
        AnimationClock qKfClock = qKf.CreateClock();
        qKfClock.Controller!.SeekAlignedToLastTick(TimeSpan.FromSeconds(1.0), TimeSeekOrigin.BeginTime);
        Quaternion qKfEnd = (Quaternion)qKf.GetCurrentValue(Quaternion.Identity, Quaternion.Identity, qKfClock);
        AssertNear(qKfEnd.Angle, 90.0, 1e-3, "SplineQuaternionKeyFrame 1.0s Angle");

        Pass("M3D-3. Media3D Animations & KeyFrames (Point3D, Vector3D, Quaternion, Rotation3D, Discrete/Linear/Easing/Spline)");
    }

    // M3D-4: 3D Ray Hit-Testing + UIElement3D Routed Events + Negative Visibility/IsHitTestVisible Checks
    private static void TestRayHitTestAndUIElement3DRoutedEvents()
    {
        var vp3d = new Viewport3D
        {
            Width = 120,
            Height = 120,
            Camera = new PerspectiveCamera(new Point3D(0, 0, 4), new Vector3D(0, 0, -1), new Vector3D(0, 1, 0), 45)
        };

        var container = new ContainerUIElement3D();
        var modelEl = new ModelUIElement3D
        {
            Model = new GeometryModel3D(CreateQuadMesh(1.0, 0.0), new EmissiveMaterial(Brushes.Green))
        };
        container.Children.Add(modelEl);
        vp3d.Children.Add(container);

        vp3d.Measure(new Size(120, 120));
        vp3d.Arrange(new Rect(0, 0, 120, 120));

        RayMeshGeometry3DHitTestResult? rayHit = null;
        VisualTreeHelper.HitTest(
            vp3d,
            null,
            result =>
            {
                if (result is RayMeshGeometry3DHitTestResult r)
                {
                    rayHit = r;
                    return HitTestResultBehavior.Stop;
                }
                return HitTestResultBehavior.Continue;
            },
            new PointHitTestParameters(new Point(60, 60)));

        Assert(rayHit != null, "Expected non-null RayMeshGeometry3DHitTestResult at center");
        Assert(ReferenceEquals(rayHit!.VisualHit, modelEl), "Expected VisualHit == ModelUIElement3D");
        double wSum = rayHit.VertexWeight1 + rayHit.VertexWeight2 + rayHit.VertexWeight3;
        AssertNear(wSum, 1.0, 1e-4, "Barycentric weights sum");
        AssertNear(rayHit.PointHit.Z, 0.0, 1e-3, "PointHit Z");

        RayHitTestResult? cornerHit = null;
        VisualTreeHelper.HitTest(
            vp3d,
            null,
            result =>
            {
                if (result is RayHitTestResult r)
                {
                    cornerHit = r;
                    return HitTestResultBehavior.Stop;
                }
                return HitTestResultBehavior.Continue;
            },
            new PointHitTestParameters(new Point(2, 2)));
        Assert(cornerHit == null, "Expected null hit result outside quad");

        int modelHits = 0, containerHits = 0, vpHits = 0;
        modelEl.AddHandler(TestPingEvent, new RoutedEventHandler((_, _) => modelHits++));
        container.AddHandler(TestPingEvent, new RoutedEventHandler((_, _) => containerHits++));
        vp3d.AddHandler(TestPingEvent, new RoutedEventHandler((_, _) => vpHits++));
        modelEl.RaiseEvent(new RoutedEventArgs(TestPingEvent, modelEl));
        Assert(modelHits == 1 && containerHits == 1 && vpHits == 1,
            $"Expected bubbling counts 1/1/1, got {modelHits}/{containerHits}/{vpHits}");

        HitTestFilterBehavior Element3DFilter(DependencyObject node)
        {
            if (node is UIElement3D u3 && (!u3.IsHitTestVisible || u3.Visibility != Visibility.Visible))
                return HitTestFilterBehavior.ContinueSkipSelfAndChildren;
            return HitTestFilterBehavior.Continue;
        }

        container.IsHitTestVisible = false;
        Assert(!modelEl.IsHitTestVisible, "Expected ModelUIElement3D.IsHitTestVisible to coerce false when parent ContainerUIElement3D.IsHitTestVisible == false");
        bool anyHit = false;
        VisualTreeHelper.HitTest(vp3d, Element3DFilter, r => { if (r is RayHitTestResult) anyHit = true; return HitTestResultBehavior.Continue; }, new PointHitTestParameters(new Point(60, 60)));
        Assert(!anyHit, "Expected no hit when ModelUIElement3D.IsHitTestVisible == false");

        container.IsHitTestVisible = true;
        Assert(modelEl.IsHitTestVisible, "Expected ModelUIElement3D.IsHitTestVisible to return true");
        container.Visibility = Visibility.Collapsed;
        VisualTreeHelper.HitTest(vp3d, Element3DFilter, r => { if (r is RayHitTestResult) anyHit = true; return HitTestResultBehavior.Continue; }, new PointHitTestParameters(new Point(60, 60)));
        Assert(!anyHit, "Expected no hit when ContainerUIElement3D.Visibility == Collapsed");

        AutomationPeer modelPeer = UIElement3DAutomationPeer.CreatePeerForElement(modelEl);
        AutomationPeer containerPeer = UIElement3DAutomationPeer.CreatePeerForElement(container);
        Assert(modelPeer is UIElement3DAutomationPeer && containerPeer is UIElement3DAutomationPeer,
            "Expected UIElement3DAutomationPeer for ModelUIElement3D and ContainerUIElement3D");
        Assert(ReferenceEquals(UIElement3DAutomationPeer.FromElement(modelEl), modelPeer),
            "Expected FromElement to return cached UIElement3DAutomationPeer");

        Pass("M3D-4. RayHitTest & UIElement3D RoutedEvents/Peer",
            $"hit={rayHit.PointHit}, weightsSum={wSum:F3}");
    }

    private sealed class MagentaElement2D : FrameworkElement
    {
        protected override void OnRender(DrawingContext drawingContext)
        {
            drawingContext.DrawRectangle(Brushes.Magenta, null, new Rect(0, 0, RenderSize.Width, RenderSize.Height));
        }
    }

    // M3D-5: Viewport2DVisual3D: 2D child renders onto 3D quad AND receives 2D HitTest via UV
    private static void TestViewport2DVisual3DRenderAndHitTest()
    {
        var vp3d = new Viewport3D
        {
            Width = 120,
            Height = 120,
            Camera = new PerspectiveCamera(new Point3D(0, 0, 3.2), new Vector3D(0, 0, -1), new Vector3D(0, 1, 0), 45)
        };

        var child2d = new MagentaElement2D
        {
            Width = 100,
            Height = 100
        };

        var v2d3d = new Viewport2DVisual3D
        {
            Geometry = CreateQuadMesh(1.0, 0.0)
        };
        var mat = new DiffuseMaterial(Brushes.White);
        Viewport2DVisual3D.SetIsVisualHostMaterial(mat, true);
        v2d3d.Material = mat;
        v2d3d.Visual = child2d;

        vp3d.Children.Add(new ModelVisual3D { Content = new AmbientLight(Colors.White) });
        vp3d.Children.Add(v2d3d);

        vp3d.Measure(new Size(120, 120));
        vp3d.Arrange(new Rect(0, 0, 120, 120));
        vp3d.UpdateLayout();

        byte[] px = RenderToBgra32(vp3d, 120, 120);
        var center = Sample(px, 120, 60, 60);
        var corner = Sample(px, 120, 2, 2);

        Assert(center.A == 255 && center.R >= 240 && center.B >= 240 && center.G <= 10,
            $"Viewport2DVisual3D center expected Magenta from 2D FrameworkElement, got {center}");
        Assert(corner.A == 0, $"Viewport2DVisual3D outer corner expected transparent, got {corner}");

        bool hit2dElement = false;
        VisualTreeHelper.HitTest(
            vp3d,
            null,
            result =>
            {
                if (ReferenceEquals(result.VisualHit, child2d) || (result.VisualHit is DependencyObject d && IsDescendantOrSelf(d, child2d)))
                {
                    hit2dElement = true;
                    return HitTestResultBehavior.Stop;
                }
                return HitTestResultBehavior.Continue;
            },
            new PointHitTestParameters(new Point(60, 60)));

        Assert(hit2dElement, "Viewport2DVisual3D 2D hit test forwarding did not reach hosted FrameworkElement");
        Pass("M3D-5. Viewport2DVisual3D render + 2D HitTest",
            $"center={center}, corner={corner}, hit2D=true");
    }

    private static bool IsDescendantOrSelf(DependencyObject node, DependencyObject ancestor)
    {
        DependencyObject? cur = node;
        while (cur != null)
        {
            if (ReferenceEquals(cur, ancestor))
                return true;
            cur = VisualTreeHelper.GetParent(cur);
        }
        return false;
    }

    // M3D-6: D3DImage real D3D11 ID3D11Texture2D backbuffer + pixel verify + negative lock check
    private static void TestD3DImageInteropAndNegativeLockCheck()
    {
        var d3dImg = new D3DImage();

        AssertThrows<InvalidOperationException>(
            () => d3dImg.SetBackBuffer(D3DResourceType.IDirect3DSurface9, IntPtr.Zero),
            "D3DImage.SetBackBuffer without Lock() must throw InvalidOperationException");

        void* d3dDevice = null;
        void* d3dCtx = null;
        void* tex2d = null;
        try
        {
            CreateExternalD3D11TextureWithSolidColor(64, 64, 0xFFFF8000u, out d3dDevice, out d3dCtx, out tex2d);

            d3dImg.Lock();
            d3dImg.SetBackBuffer(D3DResourceType.IDirect3DSurface9, (IntPtr)tex2d);
            d3dImg.AddDirtyRect(new Int32Rect(0, 0, 64, 64));
            d3dImg.Unlock();

            Assert(d3dImg.PixelWidth == 64 && d3dImg.PixelHeight == 64,
                $"D3DImage expected 64x64, got {d3dImg.PixelWidth}x{d3dImg.PixelHeight}");

            var dv = new DrawingVisual();
            using (DrawingContext dc = dv.RenderOpen())
            {
                dc.DrawImage(d3dImg, new Rect(0, 0, 64, 64));
            }

            var center = RenderAndSamplePixel(dv, 64, 64, 32, 32);
            Assert(center.A == 255 && center.R == 255 && center.G >= 127 && center.G <= 129 && center.B == 0,
                $"D3DImage rendered pixel expected orange (B=0,G=128,R=255,A=255), got {center}");

            Pass("M3D-6. D3DImage external D3D11 texture + Lock/DirtyRect/Render", $"center={center}");
        }
        finally
        {
            ReleaseCom(tex2d);
            ReleaseCom(d3dCtx);
            ReleaseCom(d3dDevice);
        }
    }

    // HWND-1: Live Win32 HwndSource + HwndTarget Window Creation, 2D+3D Composite Tree & Message Pump
    private static void TestLiveHwndSourceAndHwndTargetWindow()
    {
        var parameters = new HwndSourceParameters("CSUI_Verify_Window", 160, 120)
        {
            WindowStyle = 0 // Hidden popup style for non-intrusive automated verification
        };

        using var hwndSource = new HwndSource(parameters);
        Assert(hwndSource.Handle != IntPtr.Zero, "HwndSource.Handle must be non-zero");
        Assert(hwndSource.CompositionTarget != null, "HwndSource.CompositionTarget (HwndTarget) must be initialized");

        var rootElement = new Viewport3D
        {
            Width = 160,
            Height = 120,
            Camera = new PerspectiveCamera(new Point3D(0, 0, 4), new Vector3D(0, 0, -1), new Vector3D(0, 1, 0), 45)
        };
        var modelUi = new ModelUIElement3D
        {
            Model = new GeometryModel3D(CreateQuadMesh(1.0, 0.0), new EmissiveMaterial(Brushes.SpringGreen))
        };
        rootElement.Children.Add(modelUi);

        hwndSource.RootVisual = rootElement;
        Assert(ReferenceEquals(PresentationSource.FromVisual(rootElement), hwndSource),
            "PresentationSource.FromVisual(rootElement) must resolve to HwndSource");

        // Pump a short DispatcherFrame so layout + render passes execute against the live HwndSource
        var frame = new DispatcherFrame();
        var stopTimer = new DispatcherTimer(
            TimeSpan.FromMilliseconds(25),
            DispatcherPriority.Background,
            (s, _) =>
            {
                ((DispatcherTimer)s!).Stop();
                frame.Continue = false;
            },
            Dispatcher.CurrentDispatcher);
        stopTimer.Start();
        Dispatcher.PushFrame(frame);

        Assert(rootElement.IsMeasureValid && rootElement.IsArrangeValid,
            "RootVisual inside HwndSource should complete Measure and Arrange");

        // InputHitTest against connected PresentationSource
        IInputElement? hitInput = rootElement.InputHitTest(new Point(80, 60));
        Assert(ReferenceEquals(hitInput, modelUi), $"Expected InputHitTest on live HwndSource root to return ModelUIElement3D, got {hitInput}");

        Pass("HWND-1. Live Win32 HwndSource + HwndTarget creation, layout/render pump & InputHitTest",
            $"hwnd=0x{hwndSource.Handle:X}, hit={hitInput?.GetType().Name}");
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
        public uint CpuAccessFlags;
        public uint MiscFlags;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct D3D11_SUBRESOURCE_DATA
    {
        public void* pSysMem;
        public uint SysMemPitch;
        public uint SysMemSlicePitch;
    }

    private static void CreateExternalD3D11TextureWithSolidColor(
        uint width,
        uint height,
        uint bgraPixel,
        out void* device,
        out void* context,
        out void* texture)
    {
        nint lib = NativeLibrary.Load("d3d11.dll");
        nint fn = NativeLibrary.GetExport(lib, "D3D11CreateDevice");
        var createDevice = (delegate* unmanaged[Stdcall]<
            void*, int, nint, uint, uint*, uint, uint, void**, uint*, void**, int>)fn;

        uint* featureLevels = stackalloc uint[] { 0xb100u, 0xb000u, 0xa100u, 0xa000u };
        void* dev = null;
        void* ctx = null;
        int hr = createDevice(null, 1 /*HARDWARE*/, 0, 0x20 /*BGRA_SUPPORT*/, featureLevels, 4, 7 /*SDK_VERSION*/, &dev, null, &ctx);
        if (hr < 0 || dev == null)
        {
            hr = createDevice(null, 5 /*WARP*/, 0, 0x20 /*BGRA_SUPPORT*/, featureLevels, 4, 7, &dev, null, &ctx);
        }
        if (hr < 0 || dev == null || ctx == null)
            throw new InvalidOperationException($"D3D11CreateDevice failed: 0x{hr:X8}");

        uint[] initData = new uint[width * height];
        Array.Fill(initData, bgraPixel);

        var desc = new D3D11_TEXTURE2D_DESC
        {
            Width = width,
            Height = height,
            MipLevels = 1,
            ArraySize = 1,
            Format = 87, // DXGI_FORMAT_B8G8R8A8_UNORM
            SampleCount = 1,
            SampleQuality = 0,
            Usage = 0, // DEFAULT
            BindFlags = 0x08 | 0x20, // SHADER_RESOURCE | RENDER_TARGET
            CpuAccessFlags = 0,
            MiscFlags = 0
        };

        void* tex = null;
        fixed (uint* pData = initData)
        {
            var sub = new D3D11_SUBRESOURCE_DATA
            {
                pSysMem = pData,
                SysMemPitch = width * 4,
                SysMemSlicePitch = 0
            };
            hr = ((delegate* unmanaged[Stdcall]<void*, D3D11_TEXTURE2D_DESC*, D3D11_SUBRESOURCE_DATA*, void**, int>)(*(void***)dev)[5])(
                dev, &desc, &sub, &tex);
        }

        if (hr < 0 || tex == null)
            throw new InvalidOperationException($"CreateTexture2D failed: 0x{hr:X8}");

        device = dev;
        context = ctx;
        texture = tex;
    }

    private static void ReleaseCom(void* ptr)
    {
        if (ptr != null)
        {
            ((delegate* unmanaged[Stdcall]<void*, uint>)(*(void***)ptr)[2])(ptr);
        }
    }
}

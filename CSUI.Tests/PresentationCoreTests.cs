using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using static CSUI.Tests.TestHelpers;

namespace CSUI.Tests;

internal static class PresentationCoreTests
{
    public static void RunAll()
    {
        TestVisualTreeBoundsAnd2DHitTesting();
        TestRoutedEventsTunnelBubbleDirectAndCommands();
        Test2DAnimationsAndKeyFrames();
        TestDirect2DBrushesGeometriesAndOpacityMask();
        TestDirectWriteFormattedTextAndGlyphRasterization();
        TestWicImagingWriteableCropAndPngRoundtrip();
        TestDirect2DCombinedGeometryAndCombineModes();
    }

    // PC-1: 2D Visual Tree Bounds, Transform, Clip & Point/Geometry Hit-Testing
    private static void TestVisualTreeBoundsAnd2DHitTesting()
    {
        var root = new ContainerVisual();
        var childA = new DrawingVisual
        {
            Offset = new Vector(10, 20)
        };
        using (DrawingContext dc = childA.RenderOpen())
        {
            dc.DrawRectangle(Brushes.Red, null, new Rect(0, 0, 50, 40));
        }

        var childB = new DrawingVisual
        {
            Transform = new TranslateTransform(100, 100),
            Clip = new RectangleGeometry(new Rect(0, 0, 30, 30))
        };
        using (DrawingContext dc = childB.RenderOpen())
        {
            dc.DrawEllipse(Brushes.Blue, null, new Point(20, 20), 20, 20);
        }

        root.Children.Add(childA);
        root.Children.Add(childB);

        Rect boundsA = VisualTreeHelper.GetContentBounds(childA);
        AssertNear(boundsA.Width, 50, 1e-4, "childA content width");
        AssertNear(boundsA.Height, 40, 1e-4, "childA content height");

        Rect descBounds = VisualTreeHelper.GetDescendantBounds(root);
        Assert(descBounds.Contains(new Point(15, 25)), "DescendantBounds contains childA");

        // Point HitTest inside childA ([10..60, 20..60])
        HitTestResult? hitA = VisualTreeHelper.HitTest(root, new Point(35, 40));
        Assert(hitA != null && ReferenceEquals(hitA.VisualHit, childA), "Point hit at (35,40) must hit childA");

        // Point HitTest inside clipped portion of childB ([100..130, 100..130])
        HitTestResult? hitBIn = VisualTreeHelper.HitTest(root, new Point(115, 115));
        Assert(hitBIn != null && ReferenceEquals(hitBIn.VisualHit, childB), "Point hit at (115,115) must hit childB");

        // Point HitTest in clipped-out region of childB ((135, 115) is inside ellipse [0..40] but outside Clip [0..30])
        HitTestResult? hitBOut = VisualTreeHelper.HitTest(root, new Point(135, 115));
        Assert(hitBOut == null, "Point hit at clipped-out (135,115) must miss childB");

        // GeometryHitTestParameters over entire area
        var geoHits = new List<DependencyObject>();
        VisualTreeHelper.HitTest(
            root,
            null,
            r =>
            {
                geoHits.Add(r.VisualHit);
                return HitTestResultBehavior.Continue;
            },
            new GeometryHitTestParameters(new RectangleGeometry(new Rect(0, 0, 200, 200))));
        Assert(geoHits.Contains(childA) && geoHits.Contains(childB), "GeometryHitTest must hit both childA and childB");

        Pass("PC-1. 2D VisualTree bounds, Clip, Transform & Point/Geometry HitTest",
            $"boundsA={boundsA.Width}x{boundsA.Height}, geoHits={geoHits.Count}");
    }

    private sealed class RouteNode : UIElement
    {
        private readonly List<UIElement> _children = new();
        public readonly string Label;

        public RouteNode(string label) => Label = label;

        public void AddChild(UIElement child)
        {
            _children.Add(child);
            AddVisualChild(child);
        }

        protected override int VisualChildrenCount => _children.Count;
        protected override Visual GetVisualChild(int index) => _children[index];
    }

    private static readonly RoutedEvent PreviewCustomActionEvent = EventManager.RegisterRoutedEvent(
        "PreviewCustomAction", RoutingStrategy.Tunnel, typeof(RoutedEventHandler), typeof(PresentationCoreTests));

    private static readonly RoutedEvent CustomActionEvent = EventManager.RegisterRoutedEvent(
        "CustomAction", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(PresentationCoreTests));

    private static readonly RoutedEvent DirectCustomActionEvent = EventManager.RegisterRoutedEvent(
        "DirectCustomAction", RoutingStrategy.Direct, typeof(RoutedEventHandler), typeof(PresentationCoreTests));

    // PC-2: RoutedEvent (Tunnel, Bubble, Direct, HandledEventsToo) & RoutedCommand
    private static void TestRoutedEventsTunnelBubbleDirectAndCommands()
    {
        var root = new RouteNode("Root");
        var mid = new RouteNode("Mid");
        var leaf = new RouteNode("Leaf");
        root.AddChild(mid);
        mid.AddChild(leaf);

        var trace = new List<string>();
        foreach (var node in new[] { root, mid, leaf })
        {
            RouteNode capture = node;
            capture.AddHandler(PreviewCustomActionEvent, new RoutedEventHandler((_, _) => trace.Add("T:" + capture.Label)));
            capture.AddHandler(CustomActionEvent, new RoutedEventHandler((_, _) => trace.Add("B:" + capture.Label)));
            capture.AddHandler(DirectCustomActionEvent, new RoutedEventHandler((_, _) => trace.Add("D:" + capture.Label)));
        }

        leaf.RaiseEvent(new RoutedEventArgs(PreviewCustomActionEvent, leaf));
        leaf.RaiseEvent(new RoutedEventArgs(CustomActionEvent, leaf));
        leaf.RaiseEvent(new RoutedEventArgs(DirectCustomActionEvent, leaf));

        AssertEqual(
            string.Join(",", trace),
            "T:Root,T:Mid,T:Leaf,B:Leaf,B:Mid,B:Root,D:Leaf",
            "Tunnel + Bubble + Direct routing sequence");

        // Handled suppression & handledEventsToo
        bool rootSawHandled = false;
        bool rootSawUnhandledOnly = false;
        mid.AddHandler(CustomActionEvent, new RoutedEventHandler((_, e) => e.Handled = true));
        root.AddHandler(CustomActionEvent, new RoutedEventHandler((_, _) => rootSawUnhandledOnly = true), handledEventsToo: false);
        root.AddHandler(CustomActionEvent, new RoutedEventHandler((_, _) => rootSawHandled = true), handledEventsToo: true);

        leaf.RaiseEvent(new RoutedEventArgs(CustomActionEvent, leaf));
        Assert(!rootSawUnhandledOnly && rootSawHandled, "Handled suppression vs handledEventsToo=true");

        // RoutedCommand CanExecute + Execute bubbling from leaf to root CommandBinding
        var cmd = new RoutedCommand("RunVerify", typeof(PresentationCoreTests));
        bool executed = false;
        string? commandParam = null;
        root.CommandBindings.Add(new CommandBinding(
            cmd,
            (_, e) =>
            {
                executed = true;
                commandParam = e.Parameter as string;
            },
            (_, e) => e.CanExecute = true));

        Assert(cmd.CanExecute("Payload42", leaf), "RoutedCommand.CanExecute should bubble from leaf to root");
        cmd.Execute("Payload42", leaf);
        Assert(executed && commandParam == "Payload42", "RoutedCommand.Execute should invoke root CommandBinding");

        Pass("PC-2. RoutedEvent (Tunnel/Bubble/Direct/HandledEventsToo) & RoutedCommand",
            $"route={string.Join("->", trace)}, cmdParam={commandParam}");
    }

    // PC-3: 2D Animations & KeyFrames
    private static void Test2DAnimationsAndKeyFrames()
    {
        var dur = new Duration(TimeSpan.FromSeconds(1.0));

        // DoubleAnimation with CubicEase
        var dAnim = new DoubleAnimation(10.0, 110.0, dur)
        {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
        };
        AnimationClock dClock = dAnim.CreateClock();
        dClock.Controller!.SeekAlignedToLastTick(TimeSpan.FromSeconds(0.5), TimeSeekOrigin.BeginTime);
        double dMid = (double)dAnim.GetCurrentValue(0.0, 0.0, dClock);
        AssertNear(dMid, 22.5, 1e-3, "DoubleAnimation CubicEaseIn at t=0.5 (10 + 100 * 0.125)");

        // ColorAnimation (WPF interpolates in linear scRGB space)
        Color cTo = Color.FromArgb(255, 200, 100, 50);
        var cAnim = new ColorAnimation(Color.FromArgb(255, 0, 0, 0), cTo, dur);
        AnimationClock cClock = cAnim.CreateClock();
        cClock.Controller!.SeekAlignedToLastTick(TimeSpan.FromSeconds(0.5), TimeSeekOrigin.BeginTime);
        Color cMid = (Color)cAnim.GetCurrentValue(Colors.Black, Colors.Black, cClock);
        AssertNear(cMid.ScR, cTo.ScR * 0.5f, 1e-4, "ColorAnimation linear scRGB midpoint ScR");
        AssertNear(cMid.ScG, cTo.ScG * 0.5f, 1e-4, "ColorAnimation linear scRGB midpoint ScG");
        Assert(cMid.R == 146 && cMid.G == 71 && cMid.B == 34, $"ColorAnimation sRGB gamma-encoded mid color: {cMid}");

        // PointAnimation, VectorAnimation, RectAnimation, SizeAnimation
        var ptAnim = new PointAnimation(new Point(0, 10), new Point(20, 50), dur);
        AnimationClock ptClock = ptAnim.CreateClock();
        ptClock.Controller!.SeekAlignedToLastTick(TimeSpan.FromSeconds(0.5), TimeSeekOrigin.BeginTime);
        Point ptMid = (Point)ptAnim.GetCurrentValue(new Point(), new Point(), ptClock);
        AssertNear(ptMid.X, 10.0, 1e-4, "PointAnimation mid X");
        AssertNear(ptMid.Y, 30.0, 1e-4, "PointAnimation mid Y");

        var vecAnim = new VectorAnimation(new Vector(2, 6), new Vector(10, 18), dur);
        AnimationClock vecClock = vecAnim.CreateClock();
        vecClock.Controller!.SeekAlignedToLastTick(TimeSpan.FromSeconds(0.5), TimeSeekOrigin.BeginTime);
        Vector vecMid = (Vector)vecAnim.GetCurrentValue(new Vector(), new Vector(), vecClock);
        AssertNear(vecMid.X, 6.0, 1e-4, "VectorAnimation mid X");
        AssertNear(vecMid.Y, 12.0, 1e-4, "VectorAnimation mid Y");

        var rectAnim = new RectAnimation(new Rect(0, 0, 10, 20), new Rect(10, 20, 50, 60), dur);
        AnimationClock rectClock = rectAnim.CreateClock();
        rectClock.Controller!.SeekAlignedToLastTick(TimeSpan.FromSeconds(0.5), TimeSeekOrigin.BeginTime);
        Rect rectMid = (Rect)rectAnim.GetCurrentValue(Rect.Empty, Rect.Empty, rectClock);
        AssertNear(rectMid.X, 5.0, 1e-4, "RectAnimation mid X");
        AssertNear(rectMid.Width, 30.0, 1e-4, "RectAnimation mid Width");

        var szAnim = new SizeAnimation(new Size(10, 20), new Size(30, 60), dur);
        AnimationClock szClock = szAnim.CreateClock();
        szClock.Controller!.SeekAlignedToLastTick(TimeSpan.FromSeconds(0.5), TimeSeekOrigin.BeginTime);
        Size szMid = (Size)szAnim.GetCurrentValue(Size.Empty, Size.Empty, szClock);
        AssertNear(szMid.Width, 20.0, 1e-4, "SizeAnimation mid Width");
        AssertNear(szMid.Height, 40.0, 1e-4, "SizeAnimation mid Height");

        // DoubleAnimationUsingKeyFrames (Discrete + Linear + Spline + Easing)
        var kfAnim = new DoubleAnimationUsingKeyFrames();
        kfAnim.KeyFrames.Add(new DiscreteDoubleKeyFrame(10.0, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(0.2))));
        kfAnim.KeyFrames.Add(new LinearDoubleKeyFrame(50.0, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(0.6))));
        kfAnim.KeyFrames.Add(new SplineDoubleKeyFrame(100.0, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(1.0)), new KeySpline(0.25, 0.1, 0.25, 1.0)));

        AnimationClock kfClock = kfAnim.CreateClock();
        kfClock.Controller!.SeekAlignedToLastTick(TimeSpan.FromSeconds(0.4), TimeSeekOrigin.BeginTime);
        double kfAt04 = (double)kfAnim.GetCurrentValue(0.0, 0.0, kfClock);
        AssertNear(kfAt04, 30.0, 1e-4, "LinearDoubleKeyFrame midpoint between 0.2s(10) and 0.6s(50)");

        // ApplyAnimationClock to TranslateTransform
        var tt = new TranslateTransform(5, 5);
        var moveClock = new DoubleAnimation(10, 30, dur).CreateClock();
        tt.ApplyAnimationClock(TranslateTransform.XProperty, moveClock);
        moveClock.Controller!.SeekAlignedToLastTick(TimeSpan.FromSeconds(0.5), TimeSeekOrigin.BeginTime);
        AssertNear(tt.X, 20.0, 1e-4, "Animated TranslateTransform.X");

        Pass("PC-3. 2D Animations & KeyFrames (Double, Color, Point, Vector, Rect, Size, CubicEase, KeyFrames)",
            $"cubicMid={dMid:F2}, animatedX={tt.X:F1}");
    }

    // PC-4: Direct2D GPU 2D Rasterization (Gradients, Geometries, VisualBrush, DrawingBrush, BitmapCacheBrush, OpacityMask)
    private static void TestDirect2DBrushesGeometriesAndOpacityMask()
    {
        // 1. RadialGradientBrush & StreamGeometry
        var dv = new DrawingVisual();
        using (DrawingContext dc = dv.RenderOpen())
        {
            var radial = new RadialGradientBrush(Colors.Lime, Colors.Blue)
            {
                Center = new Point(0.5, 0.5),
                GradientOrigin = new Point(0.5, 0.5),
                RadiusX = 0.5,
                RadiusY = 0.5
            };
            var sg = new StreamGeometry();
            using (StreamGeometryContext sgc = sg.Open())
            {
                sgc.BeginFigure(new Point(0, 0), isFilled: true, isClosed: true);
                sgc.LineTo(new Point(80, 0), isStroked: false, isSmoothJoin: false);
                sgc.LineTo(new Point(80, 80), isStroked: false, isSmoothJoin: false);
                sgc.LineTo(new Point(0, 80), isStroked: false, isSmoothJoin: false);
            }
            dc.DrawGeometry(radial, null, sg);
        }

        byte[] radPx = RenderToBgra32(dv, 80, 80);
        var radCenter = Sample(radPx, 80, 40, 40);
        var radCorner = Sample(radPx, 80, 2, 2);
        Assert(radCenter.A == 255 && radCenter.G > 220 && radCenter.B < 40, $"Radial center expected lime, got {radCenter}");
        Assert(radCorner.A == 255 && radCorner.B > 220 && radCorner.G < 40, $"Radial corner expected blue, got {radCorner}");

        // 2. OpacityMask on Visual (left half opaque, right half transparent)
        var masked = new DrawingVisual
        {
            OpacityMask = new LinearGradientBrush(
                new GradientStopCollection
                {
                    new GradientStop(Color.FromArgb(255, 0, 0, 0), 0.0),
                    new GradientStop(Color.FromArgb(255, 0, 0, 0), 0.45),
                    new GradientStop(Color.FromArgb(0, 0, 0, 0), 0.55),
                    new GradientStop(Color.FromArgb(0, 0, 0, 0), 1.0)
                },
                new Point(0, 0.5),
                new Point(1, 0.5))
        };
        using (DrawingContext dc = masked.RenderOpen())
        {
            dc.DrawRectangle(Brushes.Red, null, new Rect(0, 0, 80, 80));
        }

        byte[] maskPx = RenderToBgra32(masked, 80, 80);
        var maskLeft = Sample(maskPx, 80, 15, 40);
        var maskRight = Sample(maskPx, 80, 65, 40);
        Assert(maskLeft.A >= 245 && maskLeft.R >= 245, $"OpacityMask left expected opaque red, got {maskLeft}");
        Assert(maskRight.A <= 5, $"OpacityMask right expected transparent, got {maskRight}");

        // 3. DrawingBrush & VisualBrush
        var sourceVisual = new DrawingVisual();
        using (DrawingContext dc = sourceVisual.RenderOpen())
        {
            dc.DrawRectangle(Brushes.Yellow, null, new Rect(0, 0, 40, 40));
        }
        var vbTarget = new DrawingVisual();
        using (DrawingContext dc = vbTarget.RenderOpen())
        {
            dc.DrawRectangle(new VisualBrush(sourceVisual), null, new Rect(0, 0, 40, 40));
        }
        var vbCenter = RenderAndSamplePixel(vbTarget, 40, 40, 20, 20);
        Assert(vbCenter.A == 255 && vbCenter.R >= 245 && vbCenter.G >= 245 && vbCenter.B <= 5,
            $"VisualBrush expected yellow, got {vbCenter}");

        Pass("PC-4. Direct2D GPU 2D rasterization (RadialGradient, StreamGeometry, OpacityMask, VisualBrush)",
            $"radCenter={radCenter}, maskLeftA={maskLeft.A}, maskRightA={maskRight.A}, vb={vbCenter}");
    }

    // PC-5: DirectWrite Typography (FormattedText Metrics + GPU Glyph Rasterization)
    private static void TestDirectWriteFormattedTextAndGlyphRasterization()
    {
        var ft = new FormattedText(
            "CSUI",
            CultureInfo.InvariantCulture,
            FlowDirection.LeftToRight,
            new Typeface(new FontFamily("Segoe UI"), FontStyles.Normal, FontWeights.Bold, FontStretches.Normal),
            32.0,
            Brushes.White,
            1.0);

        Assert(ft.Width > 20 && ft.Height > 15 && ft.Baseline > 10,
            $"FormattedText metrics invalid: {ft.Width}x{ft.Height}, baseline={ft.Baseline}");

        var dv = new DrawingVisual();
        using (DrawingContext dc = dv.RenderOpen())
        {
            dc.DrawRectangle(Brushes.Black, null, new Rect(0, 0, 160, 60));
            dc.DrawText(ft, new Point(8, 8));
        }

        byte[] px = RenderToBgra32(dv, 160, 60);
        int litGlyphPixels = 0;
        for (int i = 0; i < px.Length; i += 4)
        {
            if (px[i] > 100 && px[i + 1] > 100 && px[i + 2] > 100)
                litGlyphPixels++;
        }

        Assert(litGlyphPixels >= 50, $"Expected DirectWrite DrawText to rasterize white glyph pixels, got {litGlyphPixels}");
        Pass("PC-5. DirectWrite FormattedText measurement & DrawText GPU glyph rasterization",
            $"size={ft.Width:F1}x{ft.Height:F1}, litGlyphPixels={litGlyphPixels}");
    }

    // PC-6: WIC Imaging Pipeline (WriteableBitmap, CroppedBitmap, PngBitmapEncoder/PngBitmapDecoder Roundtrip)
    private static void TestWicImagingWriteableCropAndPngRoundtrip()
    {
        const int w = 32, h = 32;
        var wb = new WriteableBitmap(w, h, 96.0, 96.0, PixelFormats.Bgra32, null);
        byte[] srcPixels = new byte[w * h * 4];
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                int idx = (y * w + x) * 4;
                bool rightHalf = x >= w / 2;
                srcPixels[idx + 0] = rightHalf ? (byte)255 : (byte)10;  // B
                srcPixels[idx + 1] = rightHalf ? (byte)40 : (byte)220;  // G
                srcPixels[idx + 2] = rightHalf ? (byte)20 : (byte)250;  // R
                srcPixels[idx + 3] = 255;                               // A
            }
        }
        wb.WritePixels(new Int32Rect(0, 0, w, h), srcPixels, w * 4, 0);

        // CroppedBitmap of right half [16..31, 0..31]
        var cropped = new CroppedBitmap(wb, new Int32Rect(16, 0, 16, 32));
        AssertEqual(cropped.PixelWidth, 16, "CroppedBitmap width");
        AssertEqual(cropped.PixelHeight, 32, "CroppedBitmap height");

        // Encode to PNG in MemoryStream and decode back via PngBitmapDecoder
        using var ms = new MemoryStream();
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(wb));
        encoder.Save(ms);
        Assert(ms.Length > 64, $"Encoded PNG stream size too small: {ms.Length}");

        ms.Position = 0;
        var decoder = new PngBitmapDecoder(ms, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
        BitmapFrame frame = decoder.Frames[0];
        AssertEqual(frame.PixelWidth, w, "Decoded PNG width");
        AssertEqual(frame.PixelHeight, h, "Decoded PNG height");

        byte[] roundtrip = new byte[w * h * 4];
        frame.CopyPixels(roundtrip, w * 4, 0);
        var leftSample = Sample(roundtrip, w, 4, 16);
        var rightSample = Sample(roundtrip, w, 24, 16);
        AssertEqual(leftSample, ((byte)10, (byte)220, (byte)250, (byte)255), "PNG roundtrip left pixel");
        AssertEqual(rightSample, ((byte)255, (byte)40, (byte)20, (byte)255), "PNG roundtrip right pixel");

        Pass("PC-6. WIC WriteableBitmap, CroppedBitmap & PNG encode/decode lossless roundtrip",
            $"pngBytes={ms.Length}, left={leftSample}, right={rightSample}");
    }

    // PC-7: Direct2D CombinedGeometry & CombineWithGeometry Modes
    private static void TestDirect2DCombinedGeometryAndCombineModes()
    {
        // 1. CombinedGeometry with null operand (AdornerLayer.UpdateElementAdorners scenario)
        var rectGeom = new RectangleGeometry(new Rect(10, 20, 100, 50));
        var combinedWithNull = new CombinedGeometry(GeometryCombineMode.Union, rectGeom, null);
        Rect bounds = combinedWithNull.Bounds;
        AssertNear(bounds.X, 10.0, 1e-4, "CombinedGeometry with null second operand bounds.X");
        AssertNear(bounds.Y, 20.0, 1e-4, "CombinedGeometry with null second operand bounds.Y");
        AssertNear(bounds.Width, 100.0, 1e-4, "CombinedGeometry with null second operand bounds.Width");
        AssertNear(bounds.Height, 50.0, 1e-4, "CombinedGeometry with null second operand bounds.Height");

        // 2. CombinedGeometry with null first operand
        var combinedFirstNull = new CombinedGeometry(GeometryCombineMode.Union, null, rectGeom);
        Rect bounds2 = combinedFirstNull.Bounds;
        AssertNear(bounds2.Width, 100.0, 1e-4, "CombinedGeometry with null first operand bounds.Width");

        // 3. Geometry.Combine modes with empty / null operand
        var gUnion = Geometry.Combine(rectGeom, null, GeometryCombineMode.Union, null);
        Assert(!gUnion.IsEmpty(), "Geometry.Combine Union with null operand should not be empty");
        AssertNear(gUnion.Bounds.Width, 100.0, 1e-4, "Geometry.Combine Union bounds.Width");

        var gIntersect = Geometry.Combine(rectGeom, null, GeometryCombineMode.Intersect, null);
        Assert(gIntersect.IsEmpty(), "Geometry.Combine Intersect with null operand should be empty");

        var gExclude = Geometry.Combine(rectGeom, null, GeometryCombineMode.Exclude, null);
        Assert(!gExclude.IsEmpty(), "Geometry.Combine Exclude with null operand should preserve first operand");
        AssertNear(gExclude.Bounds.Width, 100.0, 1e-4, "Geometry.Combine Exclude bounds.Width");

        var gXor = Geometry.Combine(rectGeom, null, GeometryCombineMode.Xor, null);
        Assert(!gXor.IsEmpty(), "Geometry.Combine Xor with null operand should preserve first operand");

        // 4. Combine two non-empty geometries
        var rectGeom2 = new RectangleGeometry(new Rect(50, 20, 100, 50));
        var gCombined2 = Geometry.Combine(rectGeom, rectGeom2, GeometryCombineMode.Union, null);
        Assert(!gCombined2.IsEmpty(), "Geometry.Combine Union of two rects");
        AssertNear(gCombined2.Bounds.Width, 140.0, 1e-4, "Union bounds width (10..150)");

        Pass("PC-7. Direct2D CombinedGeometry & CombineWithGeometry Modes (empty/null handling, D2DERR_WRONG_STATE prevention)",
            $"bounds={bounds}, unionBounds={gCombined2.Bounds}");
    }
}

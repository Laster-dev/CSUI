using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;

namespace CSUI.Tests;

internal static class TestHelpers
{
    public static int PassedSuites { get; private set; }

    public static void Pass(string suiteName, string detail = "")
    {
        PassedSuites++;
        if (string.IsNullOrEmpty(detail))
            Console.WriteLine($"[PASS] {suiteName}");
        else
            Console.WriteLine($"[PASS] {suiteName}: {detail}");
    }

    public static void Assert(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException("Assertion failed: " + message);
    }

    public static void AssertEqual<T>(T actual, T expected, string message)
    {
        if (!Equals(actual, expected))
            throw new InvalidOperationException($"Assertion failed ({message}): expected '{expected}', got '{actual}'");
    }

    public static void AssertNear(double actual, double expected, double tol, string message)
    {
        if (double.IsNaN(actual) || Math.Abs(actual - expected) > tol)
            throw new InvalidOperationException($"Assertion failed ({message}): expected {expected} +/- {tol}, got {actual}");
    }

    public static void AssertThrows<TException>(Action action, string message) where TException : Exception
    {
        bool threw = false;
        try
        {
            action();
        }
        catch (TException)
        {
            threw = true;
        }
        if (!threw)
            throw new InvalidOperationException($"Assertion failed ({message}): expected {typeof(TException).Name} to be thrown.");
    }

    public static byte[] RenderToBgra32(Visual visual, int width, int height)
    {
        var rtb = new RenderTargetBitmap(width, height, 96.0, 96.0, PixelFormats.Pbgra32);
        rtb.Render(visual);
        int stride = width * 4;
        byte[] pixels = new byte[stride * height];
        rtb.CopyPixels(pixels, stride, 0);
        return pixels;
    }

    public static (byte B, byte G, byte R, byte A) Sample(byte[] px, int width, int x, int y)
    {
        int i = (y * width + x) * 4;
        return (px[i], px[i + 1], px[i + 2], px[i + 3]);
    }

    public static (byte B, byte G, byte R, byte A) RenderAndSamplePixel(Visual visual, int width, int height, int x, int y)
    {
        byte[] pixels = RenderToBgra32(visual, width, height);
        return Sample(pixels, width, x, y);
    }

    public static MeshGeometry3D CreateQuadMesh(double halfSize = 1.0, double z = 0.0, bool ccwFront = true)
    {
        var mesh = new MeshGeometry3D();
        mesh.Positions.Add(new Point3D(-halfSize, -halfSize, z));
        mesh.Positions.Add(new Point3D(halfSize, -halfSize, z));
        mesh.Positions.Add(new Point3D(halfSize, halfSize, z));
        mesh.Positions.Add(new Point3D(-halfSize, halfSize, z));

        mesh.TextureCoordinates.Add(new Point(0, 1));
        mesh.TextureCoordinates.Add(new Point(1, 1));
        mesh.TextureCoordinates.Add(new Point(1, 0));
        mesh.TextureCoordinates.Add(new Point(0, 0));

        if (ccwFront)
        {
            mesh.TriangleIndices.Add(0);
            mesh.TriangleIndices.Add(1);
            mesh.TriangleIndices.Add(2);
            mesh.TriangleIndices.Add(0);
            mesh.TriangleIndices.Add(2);
            mesh.TriangleIndices.Add(3);
        }
        else
        {
            mesh.TriangleIndices.Add(0);
            mesh.TriangleIndices.Add(2);
            mesh.TriangleIndices.Add(1);
            mesh.TriangleIndices.Add(0);
            mesh.TriangleIndices.Add(3);
            mesh.TriangleIndices.Add(2);
        }

        mesh.Freeze();
        return mesh;
    }
}

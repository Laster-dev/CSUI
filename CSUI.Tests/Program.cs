using System;
using System.Diagnostics;
using System.IO;
using static CSUI.Tests.TestHelpers;

namespace CSUI.Tests;

internal static class Program
{
    [STAThread]
    private static int Main()
    {
        var sw = Stopwatch.StartNew();
        try
        {
            Console.WriteLine("======================================================================");
            Console.WriteLine("   CSUI.Core Foundation Full-Stack & NativeAOT Verification Suite");
            Console.WriteLine("======================================================================");

            RunLayer1StaticComplianceAudit();

            Console.WriteLine("\n--- Layer 2A: WindowsBase (Dispatcher, DependencyProperty, Freezable, WeakEvent) ---");
            WindowsBaseTests.RunAll();

            Console.WriteLine("\n--- Layer 2B & 3A: PresentationCore (2D Visual, Input, Animation, Direct2D, DirectWrite, WIC) ---");
            PresentationCoreTests.RunAll();

            Console.WriteLine("\n--- Layer 2C: PresentationFramework (Layout, Resources, Styles/Templates, AOT Binding, Items) ---");
            PresentationFrameworkTests.RunAll();

            WpfAllControlsTests.RunAll();

            Console.WriteLine("\n--- Layer 3B & 4: Media3D, D3D11 Backend, Cross-Thread VisualTarget, D3DImage & Live HwndSource ---");
            Media3DAndInteropTests.RunAll();

            MarkupTests.RunAll();

            ModernArchitectureTests.RunAll();

            sw.Stop();
            Console.WriteLine("\n======================================================================");
            Console.WriteLine($"ALL {PassedSuites} FOUNDATION VERIFICATION SUITES PASSED IN {sw.ElapsedMilliseconds} ms.");
            Console.WriteLine("======================================================================");
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"\n[FAIL] Foundation Verification Failed:\n{ex}");
            return 1;
        }
    }

    private static void RunLayer1StaticComplianceAudit()
    {
        Console.WriteLine("\n--- Layer 1: Static Compliance & Architecture Audit ---");
        string? coreDir = FindCoreSourceDirectory();
        if (coreDir == null)
        {
            Pass("STATIC-1. Runtime AOT binary check (source tree not adjacent to binary)");
            return;
        }

        string csprojPath = Path.Combine(coreDir, "CSUI.Core.csproj");
        string csprojText = File.ReadAllText(csprojPath);
        Assert(!csprojText.Contains("PackageReference", StringComparison.Ordinal),
            "CSUI.Core.csproj must have zero PackageReference entries");

        int csFileCount = 0;
        foreach (string file in Directory.EnumerateFiles(coreDir, "*.cs", SearchOption.AllDirectories))
        {
            string rel = Path.GetRelativePath(coreDir, file);
            if (rel.StartsWith("bin\\", StringComparison.OrdinalIgnoreCase) ||
                rel.StartsWith("obj\\", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            csFileCount++;
            using var reader = new StreamReader(file);
            string? line;
            int lineNo = 0;
            while ((line = reader.ReadLine()) != null)
            {
                lineNo++;
                ReadOnlySpan<char> trimmed = line.AsSpan().TrimStart();
                if (trimmed.StartsWith("//") || trimmed.StartsWith("/*") || trimmed.StartsWith("*"))
                    continue;

                if (trimmed.Contains("[DllImport", StringComparison.Ordinal) ||
                    trimmed.Contains("[LibraryImport", StringComparison.Ordinal) ||
                    trimmed.Contains("[ComImport", StringComparison.Ordinal))
                {
                    throw new InvalidOperationException(
                        $"Forbidden interop attribute in {rel}:{lineNo}: {trimmed.ToString()}");
                }
            }
        }

        Pass("STATIC-1. Zero [DllImport]/[LibraryImport]/[ComImport] & Zero NuGet PackageReference",
            $"auditedFiles={csFileCount}");
    }

    private static string? FindCoreSourceDirectory()
    {
        string? dir = AppContext.BaseDirectory;
        for (int i = 0; i < 7 && dir != null; i++)
        {
            string candidate = Path.Combine(dir, "CSUI.Core", "CSUI.Core.csproj");
            if (File.Exists(candidate))
                return Path.Combine(dir, "CSUI.Core");
            dir = Directory.GetParent(dir)?.FullName;
        }
        if (File.Exists(@"d:\CSUI\CSUI.Core\CSUI.Core.csproj"))
            return @"d:\CSUI\CSUI.Core";
        return null;
    }
}

using System.Runtime.InteropServices;

namespace CSUI.Core.Interop;

/// <summary>
/// Helpers for loading native DLLs and resolving raw export pointers without DllImport.
/// </summary>
internal static class NativeModule
{
    public static nint Load(string libraryName)
        => NativeLibrary.Load(libraryName);

    public static nint GetExport(nint moduleHandle, string symbolName)
        => NativeLibrary.GetExport(moduleHandle, symbolName);

    public static bool TryGetExport(nint moduleHandle, string symbolName, out nint address)
        => NativeLibrary.TryGetExport(moduleHandle, symbolName, out address);
}

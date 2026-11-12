using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;

namespace CSUI.Core.Interop.DirectX;

[StructLayout(LayoutKind.Sequential)]
internal struct DWRITE_SCRIPT_ANALYSIS
{
    public ushort script;
    public uint shapes;
}

[StructLayout(LayoutKind.Sequential)]
internal struct DWRITE_LINE_BREAKPOINT
{
    public byte Packed;

    public uint BreakConditionBefore => (uint)(Packed & 0x03);
    public uint BreakConditionAfter => (uint)((Packed >> 2) & 0x03);
    public bool IsWhitespace => (Packed & 0x10) != 0;
    public bool IsSoftHyphen => (Packed & 0x20) != 0;
}

[StructLayout(LayoutKind.Sequential)]
internal struct DWRITE_FONT_FEATURE
{
    public uint nameTag;
    public uint parameter;
}

[StructLayout(LayoutKind.Sequential)]
internal unsafe struct DWRITE_TYPOGRAPHIC_FEATURES
{
    public DWRITE_FONT_FEATURE* features;
    public uint featureCount;
}

internal static class DxWriteFontConst
{
    public const uint DWRITE_FACTORY_TYPE_SHARED = 0;
    public const uint DWRITE_FACTORY_TYPE_ISOLATED = 1;

    public const uint DWRITE_SCRIPT_SHAPES_DEFAULT = 0;
    public const uint DWRITE_SCRIPT_SHAPES_NO_VISUAL = 1;

    public const uint DWRITE_READING_DIRECTION_LEFT_TO_RIGHT = 0;
    public const uint DWRITE_READING_DIRECTION_RIGHT_TO_LEFT = 1;
    public const uint DWRITE_READING_DIRECTION_TOP_TO_BOTTOM = 2;
    public const uint DWRITE_READING_DIRECTION_BOTTOM_TO_TOP = 3;

    public const int E_NOT_SUFFICIENT_BUFFER = unchecked((int)0x8007007A);

    public static uint MakeOpenTypeTag(char a, char b, char c, char d) =>
        ((uint)(byte)d << 24) | ((uint)(byte)c << 16) | ((uint)(byte)b << 8) | (uint)(byte)a;
}

internal static class DxWriteFontGuids
{
    public static readonly Guid IID_IDWriteFactory = new("b859ee5a-d838-4b5b-a2e8-1adc7d93db48");
    public static readonly Guid IID_IDWriteFontCollectionLoader = new("cca920e4-52f0-492b-bfa8-29c72ee0a468");
    public static readonly Guid IID_IDWriteFontFileEnumerator = new("72755049-5ff7-435d-8348-4be97cfa6c7c");
    public static readonly Guid IID_IDWriteTextAnalysisSource = new("688e1a58-5094-47c8-adc8-fbcea60ae92b");
    public static readonly Guid IID_IDWriteTextAnalysisSink = new("5810cd44-0ca0-4701-b3fa-bec5182ae4f6");
}

internal unsafe struct IDWriteFactoryFontVtbl
{
    public static int GetSystemFontCollection(void* self, void** fontCollection, bool checkForUpdates = false)
        => ((delegate* unmanaged[Stdcall]<void*, void**, int, int>)(*(void***)self)[3])(
            self, fontCollection, checkForUpdates ? 1 : 0);

    public static int CreateCustomFontCollection(
        void* self,
        void* collectionLoader,
        void* collectionKey,
        uint collectionKeySize,
        void** fontCollection)
        => ((delegate* unmanaged[Stdcall]<void*, void*, void*, uint, void**, int>)(*(void***)self)[4])(
            self, collectionLoader, collectionKey, collectionKeySize, fontCollection);

    public static int RegisterFontCollectionLoader(void* self, void* fontCollectionLoader)
        => ((delegate* unmanaged[Stdcall]<void*, void*, int>)(*(void***)self)[5])(self, fontCollectionLoader);

    public static int UnregisterFontCollectionLoader(void* self, void* fontCollectionLoader)
        => ((delegate* unmanaged[Stdcall]<void*, void*, int>)(*(void***)self)[6])(self, fontCollectionLoader);
}

internal unsafe struct IDWriteTextAnalyzerVtbl
{
    public static int AnalyzeScript(
        void* self,
        void* analysisSource,
        uint textPosition,
        uint textLength,
        void* analysisSink)
        => ((delegate* unmanaged[Stdcall]<void*, void*, uint, uint, void*, int>)(*(void***)self)[3])(
            self, analysisSource, textPosition, textLength, analysisSink);

    public static int AnalyzeBidi(
        void* self,
        void* analysisSource,
        uint textPosition,
        uint textLength,
        void* analysisSink)
        => ((delegate* unmanaged[Stdcall]<void*, void*, uint, uint, void*, int>)(*(void***)self)[4])(
            self, analysisSource, textPosition, textLength, analysisSink);

    public static int AnalyzeNumberSubstitution(
        void* self,
        void* analysisSource,
        uint textPosition,
        uint textLength,
        void* analysisSink)
        => ((delegate* unmanaged[Stdcall]<void*, void*, uint, uint, void*, int>)(*(void***)self)[5])(
            self, analysisSource, textPosition, textLength, analysisSink);

    public static int AnalyzeLineBreakpoints(
        void* self,
        void* analysisSource,
        uint textPosition,
        uint textLength,
        void* analysisSink)
        => ((delegate* unmanaged[Stdcall]<void*, void*, uint, uint, void*, int>)(*(void***)self)[6])(
            self, analysisSource, textPosition, textLength, analysisSink);

    public static int GetGlyphs(
        void* self,
        char* textString,
        uint textLength,
        void* fontFace,
        bool isSideways,
        bool isRightToLeft,
        DWRITE_SCRIPT_ANALYSIS* scriptAnalysis,
        char* localeName,
        void* numberSubstitution,
        DWRITE_TYPOGRAPHIC_FEATURES** features,
        uint* featureRangeLengths,
        uint featureRanges,
        uint maxGlyphCount,
        ushort* clusterMap,
        ushort* textProps,
        ushort* glyphIndices,
        ushort* glyphProps,
        out uint actualGlyphCount)
    {
        fixed (uint* pCount = &actualGlyphCount)
        {
            return ((delegate* unmanaged[Stdcall]<
                void*, char*, uint, void*, int, int, DWRITE_SCRIPT_ANALYSIS*, char*, void*,
                DWRITE_TYPOGRAPHIC_FEATURES**, uint*, uint, uint,
                ushort*, ushort*, ushort*, ushort*, uint*, int>)(*(void***)self)[7])(
                self, textString, textLength, fontFace,
                isSideways ? 1 : 0, isRightToLeft ? 1 : 0,
                scriptAnalysis, localeName, numberSubstitution,
                features, featureRangeLengths, featureRanges, maxGlyphCount,
                clusterMap, textProps, glyphIndices, glyphProps, pCount);
        }
    }

    public static int GetGlyphPlacements(
        void* self,
        char* textString,
        ushort* clusterMap,
        ushort* textProps,
        uint textLength,
        ushort* glyphIndices,
        ushort* glyphProps,
        uint glyphCount,
        void* fontFace,
        float fontEmSize,
        bool isSideways,
        bool isRightToLeft,
        DWRITE_SCRIPT_ANALYSIS* scriptAnalysis,
        char* localeName,
        DWRITE_TYPOGRAPHIC_FEATURES** features,
        uint* featureRangeLengths,
        uint featureRanges,
        float* glyphAdvances,
        DWRITE_GLYPH_OFFSET* glyphOffsets)
        => ((delegate* unmanaged[Stdcall]<
            void*, char*, ushort*, ushort*, uint, ushort*, ushort*, uint,
            void*, float, int, int, DWRITE_SCRIPT_ANALYSIS*, char*,
            DWRITE_TYPOGRAPHIC_FEATURES**, uint*, uint, float*, DWRITE_GLYPH_OFFSET*, int>)(*(void***)self)[8])(
            self, textString, clusterMap, textProps, textLength, glyphIndices, glyphProps, glyphCount,
            fontFace, fontEmSize, isSideways ? 1 : 0, isRightToLeft ? 1 : 0,
            scriptAnalysis, localeName, features, featureRangeLengths, featureRanges,
            glyphAdvances, glyphOffsets);

    public static int GetGdiCompatibleGlyphPlacements(
        void* self,
        char* textString,
        ushort* clusterMap,
        ushort* textProps,
        uint textLength,
        ushort* glyphIndices,
        ushort* glyphProps,
        uint glyphCount,
        void* fontFace,
        float fontEmSize,
        float pixelsPerDip,
        DWRITE_MATRIX* transform,
        bool useGdiNatural,
        bool isSideways,
        bool isRightToLeft,
        DWRITE_SCRIPT_ANALYSIS* scriptAnalysis,
        char* localeName,
        DWRITE_TYPOGRAPHIC_FEATURES** features,
        uint* featureRangeLengths,
        uint featureRanges,
        float* glyphAdvances,
        DWRITE_GLYPH_OFFSET* glyphOffsets)
        => ((delegate* unmanaged[Stdcall]<
            void*, char*, ushort*, ushort*, uint, ushort*, ushort*, uint,
            void*, float, float, DWRITE_MATRIX*, int, int, int, DWRITE_SCRIPT_ANALYSIS*, char*,
            DWRITE_TYPOGRAPHIC_FEATURES**, uint*, uint, float*, DWRITE_GLYPH_OFFSET*, int>)(*(void***)self)[9])(
            self, textString, clusterMap, textProps, textLength, glyphIndices, glyphProps, glyphCount,
            fontFace, fontEmSize, pixelsPerDip, transform,
            useGdiNatural ? 1 : 0, isSideways ? 1 : 0, isRightToLeft ? 1 : 0,
            scriptAnalysis, localeName, features, featureRangeLengths, featureRanges,
            glyphAdvances, glyphOffsets);
}

internal unsafe interface IDWriteFontFileLoaderManaged
{
    int CreateStreamFromKey(void* fontFileReferenceKey, uint fontFileReferenceKeySize, void** fontFileStream);
}

internal unsafe interface IDWriteFontFileStreamManaged
{
    int ReadFileFragment(void** fragmentStart, ulong fileOffset, ulong fragmentSize, void** fragmentContext);
    void ReleaseFileFragment(void* fragmentContext);
    int GetFileSize(ulong* fileSize);
    int GetLastWriteTime(ulong* lastWriteTime);
}

internal unsafe interface IDWriteFontCollectionLoaderManaged
{
    int CreateEnumeratorFromKey(void* factory, void* collectionKey, uint collectionKeySize, void** fontFileEnumerator);
}

internal unsafe interface IDWriteFontFileEnumeratorManaged
{
    int MoveNext(int* hasCurrentFile);
    int GetCurrentFontFile(void** fontFile);
}

internal unsafe interface IDWriteTextAnalysisSourceManaged
{
    int GetTextAtPosition(uint textPosition, char** textString, uint* textLength);
    int GetTextBeforePosition(uint textPosition, char** textString, uint* textLength);
    uint GetParagraphReadingDirection();
    int GetLocaleName(uint textPosition, uint* textLength, char** localeName);
    int GetNumberSubstitution(uint textPosition, uint* textLength, void** numberSubstitution);
}

internal unsafe interface IDWriteTextAnalysisSinkManaged
{
    int SetScriptAnalysis(uint textPosition, uint textLength, DWRITE_SCRIPT_ANALYSIS* scriptAnalysis);
    int SetLineBreakpoints(uint textPosition, uint textLength, DWRITE_LINE_BREAKPOINT* lineBreakpoints);
    int SetBidiLevel(uint textPosition, uint textLength, byte explicitLevel, byte resolvedLevel);
    int SetNumberSubstitution(uint textPosition, uint textLength, void* numberSubstitution);
}

[StructLayout(LayoutKind.Sequential)]
internal unsafe struct NativeComHeader
{
    public void** Vtbl;
    public IntPtr Handle;
    public int RefCount;
}

internal static unsafe class NativeComInstance
{
    public const int S_OK = 0;
    public const int E_NOINTERFACE = unchecked((int)0x80004002);
    public const int E_POINTER = unchecked((int)0x80004003);
    public const int E_FAIL = unchecked((int)0x80004005);

    public static NativeComHeader* Create(void** vtbl, object target)
    {
        NativeComHeader* obj = (NativeComHeader*)NativeMemory.AllocZeroed(1, (nuint)sizeof(NativeComHeader));
        obj->Vtbl = vtbl;
        obj->Handle = GCHandle.ToIntPtr(GCHandle.Alloc(target));
        obj->RefCount = 1;
        return obj;
    }

    public static T? Unwrap<T>(NativeComHeader* self) where T : class
    {
        if (self == null || self->Handle == IntPtr.Zero)
            return null;
        return GCHandle.FromIntPtr(self->Handle).Target as T;
    }

    public static int QueryInterfaceSingle(NativeComHeader* self, Guid* riid, void** ppv, in Guid supportedIid)
    {
        if (self == null || riid == null || ppv == null)
            return E_POINTER;

        Guid g = *riid;
        if (g == DxWriteGuids.IID_IUnknown || g == supportedIid)
        {
            *ppv = self;
            Interlocked.Increment(ref self->RefCount);
            return S_OK;
        }

        *ppv = null;
        return E_NOINTERFACE;
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
    public static uint AddRef(NativeComHeader* self)
        => self == null ? 0u : (uint)Interlocked.Increment(ref self->RefCount);

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
    public static uint Release(NativeComHeader* self)
    {
        if (self == null)
            return 0u;

        int remaining = Interlocked.Decrement(ref self->RefCount);
        if (remaining == 0)
        {
            if (self->Handle != IntPtr.Zero)
            {
                GCHandle handle = GCHandle.FromIntPtr(self->Handle);
                if (handle.IsAllocated)
                {
                    if (handle.Target is IDisposable disposable)
                    {
                        try { disposable.Dispose(); } catch { }
                    }
                    handle.Free();
                }
                self->Handle = IntPtr.Zero;
            }
            NativeMemory.Free(self);
        }
        return (uint)Math.Max(0, remaining);
    }
}

internal static unsafe class DWriteFontFileLoaderCCW
{
    private static readonly void** s_vtbl;

    static DWriteFontFileLoaderCCW()
    {
        s_vtbl = (void**)NativeMemory.AllocZeroed(4, (nuint)sizeof(void*));
        s_vtbl[0] = (delegate* unmanaged[Stdcall]<NativeComHeader*, Guid*, void**, int>)&QueryInterface;
        s_vtbl[1] = (delegate* unmanaged[Stdcall]<NativeComHeader*, uint>)&NativeComInstance.AddRef;
        s_vtbl[2] = (delegate* unmanaged[Stdcall]<NativeComHeader*, uint>)&NativeComInstance.Release;
        s_vtbl[3] = (delegate* unmanaged[Stdcall]<NativeComHeader*, void*, uint, void**, int>)&CreateStreamFromKey;
    }

    public static void* Create(IDWriteFontFileLoaderManaged target)
        => NativeComInstance.Create(s_vtbl, target);

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
    private static int QueryInterface(NativeComHeader* self, Guid* riid, void** ppv)
        => NativeComInstance.QueryInterfaceSingle(self, riid, ppv, DxWriteGuids.IID_IDWriteFontFileLoader);

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
    private static int CreateStreamFromKey(NativeComHeader* self, void* key, uint keySize, void** stream)
    {
        try
        {
            IDWriteFontFileLoaderManaged? mgr = NativeComInstance.Unwrap<IDWriteFontFileLoaderManaged>(self);
            return mgr is null ? NativeComInstance.E_FAIL : mgr.CreateStreamFromKey(key, keySize, stream);
        }
        catch (Exception ex) { return ex.HResult; }
    }
}

internal static unsafe class DWriteFontFileStreamCCW
{
    private static readonly void** s_vtbl;

    static DWriteFontFileStreamCCW()
    {
        s_vtbl = (void**)NativeMemory.AllocZeroed(7, (nuint)sizeof(void*));
        s_vtbl[0] = (delegate* unmanaged[Stdcall]<NativeComHeader*, Guid*, void**, int>)&QueryInterface;
        s_vtbl[1] = (delegate* unmanaged[Stdcall]<NativeComHeader*, uint>)&NativeComInstance.AddRef;
        s_vtbl[2] = (delegate* unmanaged[Stdcall]<NativeComHeader*, uint>)&NativeComInstance.Release;
        s_vtbl[3] = (delegate* unmanaged[Stdcall]<NativeComHeader*, void**, ulong, ulong, void**, int>)&ReadFileFragment;
        s_vtbl[4] = (delegate* unmanaged[Stdcall]<NativeComHeader*, void*, void>)&ReleaseFileFragment;
        s_vtbl[5] = (delegate* unmanaged[Stdcall]<NativeComHeader*, ulong*, int>)&GetFileSize;
        s_vtbl[6] = (delegate* unmanaged[Stdcall]<NativeComHeader*, ulong*, int>)&GetLastWriteTime;
    }

    public static void* Create(IDWriteFontFileStreamManaged target)
        => NativeComInstance.Create(s_vtbl, target);

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
    private static int QueryInterface(NativeComHeader* self, Guid* riid, void** ppv)
        => NativeComInstance.QueryInterfaceSingle(self, riid, ppv, DxWriteGuids.IID_IDWriteFontFileStream);

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
    private static int ReadFileFragment(NativeComHeader* self, void** fragmentStart, ulong fileOffset, ulong fragmentSize, void** fragmentContext)
    {
        try
        {
            IDWriteFontFileStreamManaged? mgr = NativeComInstance.Unwrap<IDWriteFontFileStreamManaged>(self);
            return mgr is null ? NativeComInstance.E_FAIL : mgr.ReadFileFragment(fragmentStart, fileOffset, fragmentSize, fragmentContext);
        }
        catch (Exception ex) { return ex.HResult; }
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
    private static void ReleaseFileFragment(NativeComHeader* self, void* fragmentContext)
    {
        try
        {
            NativeComInstance.Unwrap<IDWriteFontFileStreamManaged>(self)?.ReleaseFileFragment(fragmentContext);
        }
        catch { }
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
    private static int GetFileSize(NativeComHeader* self, ulong* fileSize)
    {
        try
        {
            IDWriteFontFileStreamManaged? mgr = NativeComInstance.Unwrap<IDWriteFontFileStreamManaged>(self);
            return mgr is null ? NativeComInstance.E_FAIL : mgr.GetFileSize(fileSize);
        }
        catch (Exception ex) { return ex.HResult; }
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
    private static int GetLastWriteTime(NativeComHeader* self, ulong* lastWriteTime)
    {
        try
        {
            IDWriteFontFileStreamManaged? mgr = NativeComInstance.Unwrap<IDWriteFontFileStreamManaged>(self);
            return mgr is null ? NativeComInstance.E_FAIL : mgr.GetLastWriteTime(lastWriteTime);
        }
        catch (Exception ex) { return ex.HResult; }
    }
}

internal static unsafe class DWriteFontCollectionLoaderCCW
{
    private static readonly void** s_vtbl;

    static DWriteFontCollectionLoaderCCW()
    {
        s_vtbl = (void**)NativeMemory.AllocZeroed(4, (nuint)sizeof(void*));
        s_vtbl[0] = (delegate* unmanaged[Stdcall]<NativeComHeader*, Guid*, void**, int>)&QueryInterface;
        s_vtbl[1] = (delegate* unmanaged[Stdcall]<NativeComHeader*, uint>)&NativeComInstance.AddRef;
        s_vtbl[2] = (delegate* unmanaged[Stdcall]<NativeComHeader*, uint>)&NativeComInstance.Release;
        s_vtbl[3] = (delegate* unmanaged[Stdcall]<NativeComHeader*, void*, void*, uint, void**, int>)&CreateEnumeratorFromKey;
    }

    public static void* Create(IDWriteFontCollectionLoaderManaged target)
        => NativeComInstance.Create(s_vtbl, target);

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
    private static int QueryInterface(NativeComHeader* self, Guid* riid, void** ppv)
        => NativeComInstance.QueryInterfaceSingle(self, riid, ppv, DxWriteFontGuids.IID_IDWriteFontCollectionLoader);

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
    private static int CreateEnumeratorFromKey(NativeComHeader* self, void* factory, void* collectionKey, uint collectionKeySize, void** enumerator)
    {
        try
        {
            IDWriteFontCollectionLoaderManaged? mgr = NativeComInstance.Unwrap<IDWriteFontCollectionLoaderManaged>(self);
            return mgr is null ? NativeComInstance.E_FAIL : mgr.CreateEnumeratorFromKey(factory, collectionKey, collectionKeySize, enumerator);
        }
        catch (Exception ex) { return ex.HResult; }
    }
}

internal static unsafe class DWriteFontFileEnumeratorCCW
{
    private static readonly void** s_vtbl;

    static DWriteFontFileEnumeratorCCW()
    {
        s_vtbl = (void**)NativeMemory.AllocZeroed(5, (nuint)sizeof(void*));
        s_vtbl[0] = (delegate* unmanaged[Stdcall]<NativeComHeader*, Guid*, void**, int>)&QueryInterface;
        s_vtbl[1] = (delegate* unmanaged[Stdcall]<NativeComHeader*, uint>)&NativeComInstance.AddRef;
        s_vtbl[2] = (delegate* unmanaged[Stdcall]<NativeComHeader*, uint>)&NativeComInstance.Release;
        s_vtbl[3] = (delegate* unmanaged[Stdcall]<NativeComHeader*, int*, int>)&MoveNext;
        s_vtbl[4] = (delegate* unmanaged[Stdcall]<NativeComHeader*, void**, int>)&GetCurrentFontFile;
    }

    public static void* Create(IDWriteFontFileEnumeratorManaged target)
        => NativeComInstance.Create(s_vtbl, target);

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
    private static int QueryInterface(NativeComHeader* self, Guid* riid, void** ppv)
        => NativeComInstance.QueryInterfaceSingle(self, riid, ppv, DxWriteFontGuids.IID_IDWriteFontFileEnumerator);

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
    private static int MoveNext(NativeComHeader* self, int* hasCurrentFile)
    {
        try
        {
            IDWriteFontFileEnumeratorManaged? mgr = NativeComInstance.Unwrap<IDWriteFontFileEnumeratorManaged>(self);
            return mgr is null ? NativeComInstance.E_FAIL : mgr.MoveNext(hasCurrentFile);
        }
        catch (Exception ex) { return ex.HResult; }
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
    private static int GetCurrentFontFile(NativeComHeader* self, void** fontFile)
    {
        try
        {
            IDWriteFontFileEnumeratorManaged? mgr = NativeComInstance.Unwrap<IDWriteFontFileEnumeratorManaged>(self);
            return mgr is null ? NativeComInstance.E_FAIL : mgr.GetCurrentFontFile(fontFile);
        }
        catch (Exception ex) { return ex.HResult; }
    }
}

internal static unsafe class DWriteTextAnalysisSourceCCW
{
    private static readonly void** s_vtbl;

    static DWriteTextAnalysisSourceCCW()
    {
        s_vtbl = (void**)NativeMemory.AllocZeroed(8, (nuint)sizeof(void*));
        s_vtbl[0] = (delegate* unmanaged[Stdcall]<NativeComHeader*, Guid*, void**, int>)&QueryInterface;
        s_vtbl[1] = (delegate* unmanaged[Stdcall]<NativeComHeader*, uint>)&NativeComInstance.AddRef;
        s_vtbl[2] = (delegate* unmanaged[Stdcall]<NativeComHeader*, uint>)&NativeComInstance.Release;
        s_vtbl[3] = (delegate* unmanaged[Stdcall]<NativeComHeader*, uint, char**, uint*, int>)&GetTextAtPosition;
        s_vtbl[4] = (delegate* unmanaged[Stdcall]<NativeComHeader*, uint, char**, uint*, int>)&GetTextBeforePosition;
        s_vtbl[5] = (delegate* unmanaged[Stdcall]<NativeComHeader*, uint>)&GetParagraphReadingDirection;
        s_vtbl[6] = (delegate* unmanaged[Stdcall]<NativeComHeader*, uint, uint*, char**, int>)&GetLocaleName;
        s_vtbl[7] = (delegate* unmanaged[Stdcall]<NativeComHeader*, uint, uint*, void**, int>)&GetNumberSubstitution;
    }

    public static void* Create(IDWriteTextAnalysisSourceManaged target)
        => NativeComInstance.Create(s_vtbl, target);

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
    private static int QueryInterface(NativeComHeader* self, Guid* riid, void** ppv)
        => NativeComInstance.QueryInterfaceSingle(self, riid, ppv, DxWriteFontGuids.IID_IDWriteTextAnalysisSource);

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
    private static int GetTextAtPosition(NativeComHeader* self, uint textPosition, char** textString, uint* textLength)
    {
        try
        {
            IDWriteTextAnalysisSourceManaged? mgr = NativeComInstance.Unwrap<IDWriteTextAnalysisSourceManaged>(self);
            return mgr is null ? NativeComInstance.E_FAIL : mgr.GetTextAtPosition(textPosition, textString, textLength);
        }
        catch (Exception ex) { return ex.HResult; }
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
    private static int GetTextBeforePosition(NativeComHeader* self, uint textPosition, char** textString, uint* textLength)
    {
        try
        {
            IDWriteTextAnalysisSourceManaged? mgr = NativeComInstance.Unwrap<IDWriteTextAnalysisSourceManaged>(self);
            return mgr is null ? NativeComInstance.E_FAIL : mgr.GetTextBeforePosition(textPosition, textString, textLength);
        }
        catch (Exception ex) { return ex.HResult; }
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
    private static uint GetParagraphReadingDirection(NativeComHeader* self)
    {
        try
        {
            return NativeComInstance.Unwrap<IDWriteTextAnalysisSourceManaged>(self)?.GetParagraphReadingDirection() ?? 0u;
        }
        catch { return 0u; }
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
    private static int GetLocaleName(NativeComHeader* self, uint textPosition, uint* textLength, char** localeName)
    {
        try
        {
            IDWriteTextAnalysisSourceManaged? mgr = NativeComInstance.Unwrap<IDWriteTextAnalysisSourceManaged>(self);
            return mgr is null ? NativeComInstance.E_FAIL : mgr.GetLocaleName(textPosition, textLength, localeName);
        }
        catch (Exception ex) { return ex.HResult; }
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
    private static int GetNumberSubstitution(NativeComHeader* self, uint textPosition, uint* textLength, void** numberSubstitution)
    {
        try
        {
            IDWriteTextAnalysisSourceManaged? mgr = NativeComInstance.Unwrap<IDWriteTextAnalysisSourceManaged>(self);
            return mgr is null ? NativeComInstance.E_FAIL : mgr.GetNumberSubstitution(textPosition, textLength, numberSubstitution);
        }
        catch (Exception ex) { return ex.HResult; }
    }
}

internal static unsafe class DWriteTextAnalysisSinkCCW
{
    private static readonly void** s_vtbl;

    static DWriteTextAnalysisSinkCCW()
    {
        s_vtbl = (void**)NativeMemory.AllocZeroed(7, (nuint)sizeof(void*));
        s_vtbl[0] = (delegate* unmanaged[Stdcall]<NativeComHeader*, Guid*, void**, int>)&QueryInterface;
        s_vtbl[1] = (delegate* unmanaged[Stdcall]<NativeComHeader*, uint>)&NativeComInstance.AddRef;
        s_vtbl[2] = (delegate* unmanaged[Stdcall]<NativeComHeader*, uint>)&NativeComInstance.Release;
        s_vtbl[3] = (delegate* unmanaged[Stdcall]<NativeComHeader*, uint, uint, DWRITE_SCRIPT_ANALYSIS*, int>)&SetScriptAnalysis;
        s_vtbl[4] = (delegate* unmanaged[Stdcall]<NativeComHeader*, uint, uint, DWRITE_LINE_BREAKPOINT*, int>)&SetLineBreakpoints;
        s_vtbl[5] = (delegate* unmanaged[Stdcall]<NativeComHeader*, uint, uint, byte, byte, int>)&SetBidiLevel;
        s_vtbl[6] = (delegate* unmanaged[Stdcall]<NativeComHeader*, uint, uint, void*, int>)&SetNumberSubstitution;
    }

    public static void* Create(IDWriteTextAnalysisSinkManaged target)
        => NativeComInstance.Create(s_vtbl, target);

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
    private static int QueryInterface(NativeComHeader* self, Guid* riid, void** ppv)
        => NativeComInstance.QueryInterfaceSingle(self, riid, ppv, DxWriteFontGuids.IID_IDWriteTextAnalysisSink);

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
    private static int SetScriptAnalysis(NativeComHeader* self, uint textPosition, uint textLength, DWRITE_SCRIPT_ANALYSIS* scriptAnalysis)
    {
        try
        {
            IDWriteTextAnalysisSinkManaged? mgr = NativeComInstance.Unwrap<IDWriteTextAnalysisSinkManaged>(self);
            return mgr is null ? NativeComInstance.E_FAIL : mgr.SetScriptAnalysis(textPosition, textLength, scriptAnalysis);
        }
        catch (Exception ex) { return ex.HResult; }
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
    private static int SetLineBreakpoints(NativeComHeader* self, uint textPosition, uint textLength, DWRITE_LINE_BREAKPOINT* lineBreakpoints)
    {
        try
        {
            IDWriteTextAnalysisSinkManaged? mgr = NativeComInstance.Unwrap<IDWriteTextAnalysisSinkManaged>(self);
            return mgr is null ? NativeComInstance.E_FAIL : mgr.SetLineBreakpoints(textPosition, textLength, lineBreakpoints);
        }
        catch (Exception ex) { return ex.HResult; }
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
    private static int SetBidiLevel(NativeComHeader* self, uint textPosition, uint textLength, byte explicitLevel, byte resolvedLevel)
    {
        try
        {
            IDWriteTextAnalysisSinkManaged? mgr = NativeComInstance.Unwrap<IDWriteTextAnalysisSinkManaged>(self);
            return mgr is null ? NativeComInstance.E_FAIL : mgr.SetBidiLevel(textPosition, textLength, explicitLevel, resolvedLevel);
        }
        catch (Exception ex) { return ex.HResult; }
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
    private static int SetNumberSubstitution(NativeComHeader* self, uint textPosition, uint textLength, void* numberSubstitution)
    {
        try
        {
            IDWriteTextAnalysisSinkManaged? mgr = NativeComInstance.Unwrap<IDWriteTextAnalysisSinkManaged>(self);
            return mgr is null ? NativeComInstance.E_FAIL : mgr.SetNumberSubstitution(textPosition, textLength, numberSubstitution);
        }
        catch (Exception ex) { return ex.HResult; }
    }
}

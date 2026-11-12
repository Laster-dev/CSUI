using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

#nullable enable

namespace CSUI.Core.Interop.DirectX;

[StructLayout(LayoutKind.Sequential)]
internal struct DWRITE_GLYPH_METRICS
{
    public int leftSideBearing;
    public uint advanceWidth;
    public int rightSideBearing;
    public int topSideBearing;
    public uint advanceHeight;
    public int bottomSideBearing;
    public int verticalOriginY;
}

[StructLayout(LayoutKind.Sequential)]
internal struct DWRITE_GLYPH_OFFSET
{
    public float advanceOffset;
    public float ascenderOffset;
}

[StructLayout(LayoutKind.Sequential)]
internal unsafe struct DWRITE_GLYPH_RUN
{
    public void* fontFace;
    public float fontEmSize;
    public uint glyphCount;
    public ushort* glyphIndices;
    public float* glyphAdvances;
    public DWRITE_GLYPH_OFFSET* glyphOffsets;
    public int isSideways;
    public uint bidiLevel;
}

[StructLayout(LayoutKind.Sequential)]
internal unsafe struct DWRITE_GLYPH_RUN_DESCRIPTION
{
    public char* localeName;
    public char* @string;
    public uint stringLength;
    public ushort* clusterMap;
    public uint textPosition;
}

[StructLayout(LayoutKind.Sequential)]
internal unsafe struct DWRITE_UNDERLINE
{
    public float width;
    public float thickness;
    public float offset;
    public float runHeight;
    public uint readingDirection;
    public uint flowDirection;
    public char* localeName;
    public uint measuringMode;
}

[StructLayout(LayoutKind.Sequential)]
internal unsafe struct DWRITE_STRIKETHROUGH
{
    public float width;
    public float thickness;
    public float offset;
    public uint readingDirection;
    public uint flowDirection;
    public char* localeName;
    public uint measuringMode;
}

[StructLayout(LayoutKind.Sequential)]
internal struct DWRITE_MATRIX
{
    public float m11;
    public float m12;
    public float m21;
    public float m22;
    public float dx;
    public float dy;

    public static DWRITE_MATRIX Identity => new()
    {
        m11 = 1.0f,
        m12 = 0.0f,
        m21 = 0.0f,
        m22 = 1.0f,
        dx = 0.0f,
        dy = 0.0f,
    };
}

internal static class DxWriteConst
{
    // DWRITE_FONT_FACE_TYPE
    public const uint DWRITE_FONT_FACE_TYPE_CFF = 0;
    public const uint DWRITE_FONT_FACE_TYPE_TRUETYPE = 1;
    public const uint DWRITE_FONT_FACE_TYPE_OPENTYPE_COLLECTION = 2;
    public const uint DWRITE_FONT_FACE_TYPE_TYPE1 = 3;
    public const uint DWRITE_FONT_FACE_TYPE_VECTOR = 4;
    public const uint DWRITE_FONT_FACE_TYPE_BITMAP = 5;
    public const uint DWRITE_FONT_FACE_TYPE_UNKNOWN = 6;
    public const uint DWRITE_FONT_FACE_TYPE_RAW_CFF = 7;

    // DWRITE_FONT_SIMULATIONS
    public const uint DWRITE_FONT_SIMULATIONS_NONE = 0x0000;
    public const uint DWRITE_FONT_SIMULATIONS_BOLD = 0x0001;
    public const uint DWRITE_FONT_SIMULATIONS_OBLIQUE = 0x0002;

    // DWRITE_INFORMATIONAL_STRING_ID
    public const uint DWRITE_INFORMATIONAL_STRING_NONE = 0;
    public const uint DWRITE_INFORMATIONAL_STRING_COPYRIGHT_NOTICE = 1;
    public const uint DWRITE_INFORMATIONAL_STRING_VERSION_STRINGS = 2;
    public const uint DWRITE_INFORMATIONAL_STRING_TRADEMARK = 3;
    public const uint DWRITE_INFORMATIONAL_STRING_MANUFACTURER = 4;
    public const uint DWRITE_INFORMATIONAL_STRING_DESIGNER = 5;
    public const uint DWRITE_INFORMATIONAL_STRING_DESIGNER_URL = 6;
    public const uint DWRITE_INFORMATIONAL_STRING_DESCRIPTION = 7;
    public const uint DWRITE_INFORMATIONAL_STRING_FONT_VENDOR_URL = 8;
    public const uint DWRITE_INFORMATIONAL_STRING_LICENSE_DESCRIPTION = 9;
    public const uint DWRITE_INFORMATIONAL_STRING_LICENSE_INFO_URL = 10;
    public const uint DWRITE_INFORMATIONAL_STRING_WIN32_FAMILY_NAMES = 11;
    public const uint DWRITE_INFORMATIONAL_STRING_WIN32_SUBFAMILY_NAMES = 12;
    public const uint DWRITE_INFORMATIONAL_STRING_TYPOGRAPHIC_FAMILY_NAMES = 13;
    public const uint DWRITE_INFORMATIONAL_STRING_TYPOGRAPHIC_SUBFAMILY_NAMES = 14;

    // DWRITE_RENDERING_MODE
    public const uint DWRITE_RENDERING_MODE_DEFAULT = 0;
    public const uint DWRITE_RENDERING_MODE_ALIASED = 1;
    public const uint DWRITE_RENDERING_MODE_GDI_CLASSIC = 2;
    public const uint DWRITE_RENDERING_MODE_GDI_NATURAL = 3;
    public const uint DWRITE_RENDERING_MODE_NATURAL = 4;
    public const uint DWRITE_RENDERING_MODE_NATURAL_SYMMETRIC = 5;
    public const uint DWRITE_RENDERING_MODE_OUTLINE = 6;

    // DWRITE_NUMBER_SUBSTITUTION_METHOD
    public const uint DWRITE_NUMBER_SUBSTITUTION_METHOD_FROM_CULTURE = 0;
    public const uint DWRITE_NUMBER_SUBSTITUTION_METHOD_CONTEXTUAL = 1;
    public const uint DWRITE_NUMBER_SUBSTITUTION_METHOD_NONE = 2;
    public const uint DWRITE_NUMBER_SUBSTITUTION_METHOD_NATIONAL = 3;
    public const uint DWRITE_NUMBER_SUBSTITUTION_METHOD_TRADITIONAL = 4;
}

internal static class DxWriteGuids
{
    public static readonly Guid IID_IUnknown = new("00000000-0000-0000-c000-000000000046");
    public static readonly Guid IID_IDWriteFontFace = new("5f49804d-7024-4d43-bfa9-d25984f53849");
    public static readonly Guid IID_IDWriteFontFile = new("739d886a-cef5-47dc-8769-1a8b41bebbb0");
    public static readonly Guid IID_IDWriteFontFileLoader = new("727cad4e-d6af-4c9e-8a08-d695b11caa49");
    public static readonly Guid IID_IDWriteLocalFontFileLoader = new("b2d9f3ec-c9fe-4a11-a2ec-d86208f7c0a2");
    public static readonly Guid IID_IDWriteFontFileStream = new("6d4865fe-0ab8-4d91-8f62-5dd6be34a3e0");
    public static readonly Guid IID_IDWriteFontList = new("1a0d8438-1d97-4ec1-aef9-a2fb86ed6acb");
    public static readonly Guid IID_IDWritePixelSnapping = new("eaf3a2da-ecf4-4d24-b644-b34f6842024b");
    public static readonly Guid IID_IDWriteTextRenderer = new("ef8a8135-5cc6-45fe-8825-c5a0724eb819");
    public static readonly Guid IID_IDWriteTextAnalyzer = new("b7e6163e-7f46-43b4-84b3-e4e6249c365d");
}

internal unsafe struct ID2D1RenderTargetExVtbl
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void DrawGlyphRun(
        void* self,
        D2D1_POINT_2F baselineOrigin,
        in DWRITE_GLYPH_RUN glyphRun,
        void* foregroundBrush,
        uint measuringMode = DxMediaConst.DWRITE_MEASURING_MODE_NATURAL)
    {
        fixed (DWRITE_GLYPH_RUN* pRun = &glyphRun)
        {
            ((delegate* unmanaged[Stdcall]<void*, D2D1_POINT_2F, DWRITE_GLYPH_RUN*, void*, uint, void>)(*(void***)self)[29])(
                self, baselineOrigin, pRun, foregroundBrush, measuringMode);
        }
    }
}

internal unsafe struct IDWriteFactoryWriteVtbl
{
    public static int CreateFontFileReference(void* self, string filePath, ulong* lastWriteTime, void** fontFile)
    {
        fixed (char* pPath = filePath)
        {
            return ((delegate* unmanaged[Stdcall]<void*, char*, ulong*, void**, int>)(*(void***)self)[7])(
                self, pPath, lastWriteTime, fontFile);
        }
    }

    public static int CreateCustomFontFileReference(
        void* self,
        void* fontFileReferenceKey,
        uint fontFileReferenceKeySize,
        void* fontFileLoader,
        void** fontFile)
        => ((delegate* unmanaged[Stdcall]<void*, void*, uint, void*, void**, int>)(*(void***)self)[8])(
            self, fontFileReferenceKey, fontFileReferenceKeySize, fontFileLoader, fontFile);

    public static int CreateFontFace(
        void* self,
        uint fontFaceType,
        uint numberOfFiles,
        void** fontFiles,
        uint faceIndex,
        uint fontFaceSimulationFlags,
        void** fontFace)
        => ((delegate* unmanaged[Stdcall]<void*, uint, uint, void**, uint, uint, void**, int>)(*(void***)self)[9])(
            self, fontFaceType, numberOfFiles, fontFiles, faceIndex, fontFaceSimulationFlags, fontFace);

    public static int RegisterFontFileLoader(void* self, void* fontFileLoader)
        => ((delegate* unmanaged[Stdcall]<void*, void*, int>)(*(void***)self)[13])(self, fontFileLoader);

    public static int UnregisterFontFileLoader(void* self, void* fontFileLoader)
        => ((delegate* unmanaged[Stdcall]<void*, void*, int>)(*(void***)self)[14])(self, fontFileLoader);

    public static int CreateTypography(void* self, void** typography)
        => ((delegate* unmanaged[Stdcall]<void*, void**, int>)(*(void***)self)[16])(self, typography);

    public static int CreateGdiCompatibleTextLayout(
        void* self,
        string text,
        uint stringLength,
        void* textFormat,
        float layoutWidth,
        float layoutHeight,
        float pixelsPerDip,
        DWRITE_MATRIX* transform,
        bool useGdiNatural,
        void** textLayout)
    {
        fixed (char* pText = text)
        {
            return ((delegate* unmanaged[Stdcall]<void*, char*, uint, void*, float, float, float, DWRITE_MATRIX*, int, void**, int>)(*(void***)self)[19])(
                self, pText, stringLength, textFormat, layoutWidth, layoutHeight, pixelsPerDip, transform, useGdiNatural ? 1 : 0, textLayout);
        }
    }

    public static int CreateTextAnalyzer(void* self, void** textAnalyzer)
        => ((delegate* unmanaged[Stdcall]<void*, void**, int>)(*(void***)self)[21])(self, textAnalyzer);

    public static int CreateNumberSubstitution(
        void* self,
        uint substitutionMethod,
        string? localeName,
        bool ignoreUserOverride,
        void** numberSubstitution)
    {
        fixed (char* pLocale = localeName)
        {
            return ((delegate* unmanaged[Stdcall]<void*, uint, char*, int, void**, int>)(*(void***)self)[22])(
                self, substitutionMethod, pLocale, ignoreUserOverride ? 1 : 0, numberSubstitution);
        }
    }
}

internal unsafe struct IDWriteFontCollectionExVtbl
{
    public static int GetFontFromFontFace(void* self, void* fontFace, void** font)
        => ((delegate* unmanaged[Stdcall]<void*, void*, void**, int>)(*(void***)self)[6])(self, fontFace, font);
}

internal unsafe struct IDWriteFontFamilyExVtbl
{
    public static int GetFontCollection(void* self, void** fontCollection)
        => ((delegate* unmanaged[Stdcall]<void*, void**, int>)(*(void***)self)[3])(self, fontCollection);

    public static int GetMatchingFonts(void* self, uint weight, uint stretch, uint style, void** matchingFonts)
        => ((delegate* unmanaged[Stdcall]<void*, uint, uint, uint, void**, int>)(*(void***)self)[8])(
            self, weight, stretch, style, matchingFonts);
}

internal unsafe struct IDWriteFontExVtbl
{
    public static int GetFontFamily(void* self, void** fontFamily)
        => ((delegate* unmanaged[Stdcall]<void*, void**, int>)(*(void***)self)[3])(self, fontFamily);

    public static int GetFaceNames(void* self, void** names)
        => ((delegate* unmanaged[Stdcall]<void*, void**, int>)(*(void***)self)[8])(self, names);

    public static int GetInformationalStrings(void* self, uint informationalStringID, void** informationalStrings, out bool exists)
    {
        int b = 0;
        int hr = ((delegate* unmanaged[Stdcall]<void*, uint, void**, int*, int>)(*(void***)self)[9])(
            self, informationalStringID, informationalStrings, &b);
        exists = b != 0;
        return hr;
    }

    public static uint GetSimulations(void* self)
        => ((delegate* unmanaged[Stdcall]<void*, uint>)(*(void***)self)[10])(self);

    public static int CreateFontFace(void* self, void** fontFace)
        => ((delegate* unmanaged[Stdcall]<void*, void**, int>)(*(void***)self)[13])(self, fontFace);
}

internal unsafe struct IDWriteLocalizedStringsExVtbl
{
    public static int GetLocaleNameLength(void* self, uint index, out uint length)
    {
        fixed (uint* p = &length)
            return ((delegate* unmanaged[Stdcall]<void*, uint, uint*, int>)(*(void***)self)[5])(self, index, p);
    }

    public static int GetLocaleName(void* self, uint index, char* localeName, uint size)
        => ((delegate* unmanaged[Stdcall]<void*, uint, char*, uint, int>)(*(void***)self)[6])(self, index, localeName, size);

    public static string? ReadStringAt(void* self, uint index)
    {
        if (self == null || IDWriteLocalizedStringsVtbl.GetStringLength(self, index, out uint len) < 0)
            return null;

        uint size = len + 1;
        char[] buf = new char[(int)size];
        fixed (char* p = buf)
        {
            if (IDWriteLocalizedStringsVtbl.GetString(self, index, p, size) < 0)
                return null;
            return new string(p, 0, (int)len);
        }
    }

    public static string? ReadLocaleAt(void* self, uint index)
    {
        if (self == null || GetLocaleNameLength(self, index, out uint len) < 0)
            return null;

        uint size = len + 1;
        char[] buf = new char[(int)size];
        fixed (char* p = buf)
        {
            if (GetLocaleName(self, index, p, size) < 0)
                return null;
            return new string(p, 0, (int)len);
        }
    }

    public static string? ReadBestMatch(void* self, string? preferredLocale = null)
    {
        if (self == null)
            return null;

        uint count = IDWriteLocalizedStringsVtbl.GetCount(self);
        if (count == 0)
            return null;

        if (!string.IsNullOrEmpty(preferredLocale) &&
            IDWriteLocalizedStringsVtbl.FindLocaleName(self, preferredLocale, out uint idx, out bool exists) >= 0 &&
            exists)
        {
            return ReadStringAt(self, idx);
        }

        if (IDWriteLocalizedStringsVtbl.FindLocaleName(self, "en-us", out idx, out exists) >= 0 && exists)
        {
            return ReadStringAt(self, idx);
        }

        return ReadStringAt(self, 0);
    }

    public static Dictionary<string, string> ReadAll(void* self)
    {
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (self == null)
            return map;

        uint count = IDWriteLocalizedStringsVtbl.GetCount(self);
        for (uint i = 0; i < count; i++)
        {
            string? value = ReadStringAt(self, i);
            if (value == null)
                continue;

            string locale = ReadLocaleAt(self, i) ?? string.Empty;
            map[locale] = value;
        }
        return map;
    }
}

internal unsafe struct IDWriteFontFaceVtbl
{
    public static uint GetType(void* self)
        => ((delegate* unmanaged[Stdcall]<void*, uint>)(*(void***)self)[3])(self);

    public static int GetFiles(void* self, ref uint numberOfFiles, void** fontFiles)
    {
        fixed (uint* p = &numberOfFiles)
            return ((delegate* unmanaged[Stdcall]<void*, uint*, void**, int>)(*(void***)self)[4])(self, p, fontFiles);
    }

    public static uint GetIndex(void* self)
        => ((delegate* unmanaged[Stdcall]<void*, uint>)(*(void***)self)[5])(self);

    public static uint GetSimulations(void* self)
        => ((delegate* unmanaged[Stdcall]<void*, uint>)(*(void***)self)[6])(self);

    public static bool IsSymbolFont(void* self)
        => ((delegate* unmanaged[Stdcall]<void*, int>)(*(void***)self)[7])(self) != 0;

    public static void GetMetrics(void* self, out DWRITE_FONT_METRICS fontFaceMetrics)
    {
        fixed (DWRITE_FONT_METRICS* p = &fontFaceMetrics)
            ((delegate* unmanaged[Stdcall]<void*, DWRITE_FONT_METRICS*, void>)(*(void***)self)[8])(self, p);
    }

    public static ushort GetGlyphCount(void* self)
        => ((delegate* unmanaged[Stdcall]<void*, ushort>)(*(void***)self)[9])(self);

    public static int GetDesignGlyphMetrics(
        void* self,
        ushort* glyphIndices,
        uint glyphCount,
        DWRITE_GLYPH_METRICS* glyphMetrics,
        bool isSideways = false)
        => ((delegate* unmanaged[Stdcall]<void*, ushort*, uint, DWRITE_GLYPH_METRICS*, int, int>)(*(void***)self)[10])(
            self, glyphIndices, glyphCount, glyphMetrics, isSideways ? 1 : 0);

    public static int GetGlyphIndices(void* self, uint* codePoints, uint codePointCount, ushort* glyphIndices)
        => ((delegate* unmanaged[Stdcall]<void*, uint*, uint, ushort*, int>)(*(void***)self)[11])(
            self, codePoints, codePointCount, glyphIndices);

    public static int TryGetFontTable(
        void* self,
        uint openTypeTableTag,
        out void* tableData,
        out uint tableSize,
        out void* tableContext,
        out bool exists)
    {
        int b = 0;
        fixed (void** pData = &tableData)
        fixed (uint* pSize = &tableSize)
        fixed (void** pCtx = &tableContext)
        {
            int hr = ((delegate* unmanaged[Stdcall]<void*, uint, void**, uint*, void**, int*, int>)(*(void***)self)[12])(
                self, openTypeTableTag, pData, pSize, pCtx, &b);
            exists = b != 0;
            return hr;
        }
    }

    public static void ReleaseFontTable(void* self, void* tableContext)
        => ((delegate* unmanaged[Stdcall]<void*, void*, void>)(*(void***)self)[13])(self, tableContext);

    public static int GetGlyphRunOutline(
        void* self,
        float emSize,
        ushort* glyphIndices,
        float* glyphAdvances,
        DWRITE_GLYPH_OFFSET* glyphOffsets,
        uint glyphCount,
        bool isSideways,
        bool isRightToLeft,
        void* geometrySink)
        => ((delegate* unmanaged[Stdcall]<void*, float, ushort*, float*, DWRITE_GLYPH_OFFSET*, uint, int, int, void*, int>)(*(void***)self)[14])(
            self, emSize, glyphIndices, glyphAdvances, glyphOffsets, glyphCount, isSideways ? 1 : 0, isRightToLeft ? 1 : 0, geometrySink);

    public static int GetRecommendedRenderingMode(
        void* self,
        float emSize,
        float pixelsPerDip,
        uint measuringMode,
        void* renderingParams,
        out uint renderingMode)
    {
        fixed (uint* p = &renderingMode)
            return ((delegate* unmanaged[Stdcall]<void*, float, float, uint, void*, uint*, int>)(*(void***)self)[15])(
                self, emSize, pixelsPerDip, measuringMode, renderingParams, p);
    }

    public static int GetGdiCompatibleMetrics(
        void* self,
        float emSize,
        float pixelsPerDip,
        DWRITE_MATRIX* transform,
        out DWRITE_FONT_METRICS fontFaceMetrics)
    {
        fixed (DWRITE_FONT_METRICS* p = &fontFaceMetrics)
            return ((delegate* unmanaged[Stdcall]<void*, float, float, DWRITE_MATRIX*, DWRITE_FONT_METRICS*, int>)(*(void***)self)[16])(
                self, emSize, pixelsPerDip, transform, p);
    }

    public static int GetGdiCompatibleGlyphMetrics(
        void* self,
        float emSize,
        float pixelsPerDip,
        DWRITE_MATRIX* transform,
        bool useGdiNatural,
        ushort* glyphIndices,
        uint glyphCount,
        DWRITE_GLYPH_METRICS* glyphMetrics,
        bool isSideways = false)
        => ((delegate* unmanaged[Stdcall]<void*, float, float, DWRITE_MATRIX*, int, ushort*, uint, DWRITE_GLYPH_METRICS*, int, int>)(*(void***)self)[17])(
            self, emSize, pixelsPerDip, transform, useGdiNatural ? 1 : 0, glyphIndices, glyphCount, glyphMetrics, isSideways ? 1 : 0);
}

internal unsafe struct IDWriteFontFileVtbl
{
    public static int GetReferenceKey(void* self, out void* fontFileReferenceKey, out uint fontFileReferenceKeySize)
    {
        fixed (void** pKey = &fontFileReferenceKey)
        fixed (uint* pSize = &fontFileReferenceKeySize)
            return ((delegate* unmanaged[Stdcall]<void*, void**, uint*, int>)(*(void***)self)[3])(self, pKey, pSize);
    }

    public static int GetLoader(void* self, void** fontFileLoader)
        => ((delegate* unmanaged[Stdcall]<void*, void**, int>)(*(void***)self)[4])(self, fontFileLoader);

    public static int Analyze(void* self, out bool isSupportedFontType, out uint fontFileType, out uint fontFaceType, out uint numberOfFaces)
    {
        int b = 0;
        fixed (uint* pFileType = &fontFileType)
        fixed (uint* pFaceType = &fontFaceType)
        fixed (uint* pFaces = &numberOfFaces)
        {
            int hr = ((delegate* unmanaged[Stdcall]<void*, int*, uint*, uint*, uint*, int>)(*(void***)self)[5])(
                self, &b, pFileType, pFaceType, pFaces);
            isSupportedFontType = b != 0;
            return hr;
        }
    }
}

internal unsafe struct IDWriteLocalFontFileLoaderVtbl
{
    // Slot [3] is inherited IDWriteFontFileLoader::CreateStreamFromKey
    public static int GetFilePathLengthFromKey(void* self, void* fontFileReferenceKey, uint fontFileReferenceKeySize, out uint filePathLength)
    {
        fixed (uint* p = &filePathLength)
            return ((delegate* unmanaged[Stdcall]<void*, void*, uint, uint*, int>)(*(void***)self)[4])(
                self, fontFileReferenceKey, fontFileReferenceKeySize, p);
    }

    public static int GetFilePathFromKey(void* self, void* fontFileReferenceKey, uint fontFileReferenceKeySize, char* filePath, uint filePathSize)
        => ((delegate* unmanaged[Stdcall]<void*, void*, uint, char*, uint, int>)(*(void***)self)[5])(
            self, fontFileReferenceKey, fontFileReferenceKeySize, filePath, filePathSize);

    public static int GetLastWriteTimeFromKey(void* self, void* fontFileReferenceKey, uint fontFileReferenceKeySize, out ulong lastWriteTime)
    {
        fixed (ulong* p = &lastWriteTime)
            return ((delegate* unmanaged[Stdcall]<void*, void*, uint, ulong*, int>)(*(void***)self)[6])(
                self, fontFileReferenceKey, fontFileReferenceKeySize, p);
    }

    public static bool TryGetLocalFilePath(void* fontFile, out string? filePath)
    {
        filePath = null;
        if (fontFile == null)
            return false;

        void* loader = null;
        try
        {
            if (IDWriteFontFileVtbl.GetLoader(fontFile, &loader) < 0 || loader == null)
                return false;

            void* localLoader = null;
            Guid iid = DxWriteGuids.IID_IDWriteLocalFontFileLoader;
            int hrQi = ((delegate* unmanaged[Stdcall]<void*, Guid*, void**, int>)(*(void***)loader)[0])(loader, &iid, &localLoader);
            if (hrQi < 0 || localLoader == null)
                return false;

            try
            {
                if (IDWriteFontFileVtbl.GetReferenceKey(fontFile, out void* key, out uint keySize) < 0)
                    return false;

                if (GetFilePathLengthFromKey(localLoader, key, keySize, out uint len) < 0)
                    return false;

                uint bufSize = len + 1;
                char[] buf = new char[(int)bufSize];
                fixed (char* pBuf = buf)
                {
                    if (GetFilePathFromKey(localLoader, key, keySize, pBuf, bufSize) < 0)
                        return false;

                    filePath = new string(pBuf, 0, (int)len);
                    return true;
                }
            }
            finally
            {
                ComHelpers.Release(localLoader);
            }
        }
        finally
        {
            ComHelpers.Release(loader);
        }
    }
}

internal unsafe struct IDWriteTextLayoutDrawVtbl
{
    public static int GetFontCollection(void* self, uint currentPosition, void** fontCollection, DWRITE_TEXT_RANGE* textRange = null)
        => ((delegate* unmanaged[Stdcall]<void*, uint, void**, DWRITE_TEXT_RANGE*, int>)(*(void***)self)[44])(
            self, currentPosition, fontCollection, textRange);

    public static int GetFontFamilyNameLength(void* self, uint currentPosition, out uint nameLength, DWRITE_TEXT_RANGE* textRange = null)
    {
        fixed (uint* p = &nameLength)
            return ((delegate* unmanaged[Stdcall]<void*, uint, uint*, DWRITE_TEXT_RANGE*, int>)(*(void***)self)[45])(
                self, currentPosition, p, textRange);
    }

    public static int GetFontFamilyName(void* self, uint currentPosition, char* fontFamilyName, uint nameSize, DWRITE_TEXT_RANGE* textRange = null)
        => ((delegate* unmanaged[Stdcall]<void*, uint, char*, uint, DWRITE_TEXT_RANGE*, int>)(*(void***)self)[46])(
            self, currentPosition, fontFamilyName, nameSize, textRange);

    public static int GetFontWeight(void* self, uint currentPosition, out uint fontWeight, DWRITE_TEXT_RANGE* textRange = null)
    {
        fixed (uint* p = &fontWeight)
            return ((delegate* unmanaged[Stdcall]<void*, uint, uint*, DWRITE_TEXT_RANGE*, int>)(*(void***)self)[47])(
                self, currentPosition, p, textRange);
    }

    public static int GetFontStyle(void* self, uint currentPosition, out uint fontStyle, DWRITE_TEXT_RANGE* textRange = null)
    {
        fixed (uint* p = &fontStyle)
            return ((delegate* unmanaged[Stdcall]<void*, uint, uint*, DWRITE_TEXT_RANGE*, int>)(*(void***)self)[48])(
                self, currentPosition, p, textRange);
    }

    public static int GetFontStretch(void* self, uint currentPosition, out uint fontStretch, DWRITE_TEXT_RANGE* textRange = null)
    {
        fixed (uint* p = &fontStretch)
            return ((delegate* unmanaged[Stdcall]<void*, uint, uint*, DWRITE_TEXT_RANGE*, int>)(*(void***)self)[49])(
                self, currentPosition, p, textRange);
    }

    public static int GetFontSize(void* self, uint currentPosition, out float fontSize, DWRITE_TEXT_RANGE* textRange = null)
    {
        fixed (float* p = &fontSize)
            return ((delegate* unmanaged[Stdcall]<void*, uint, float*, DWRITE_TEXT_RANGE*, int>)(*(void***)self)[50])(
                self, currentPosition, p, textRange);
    }

    public static int GetUnderline(void* self, uint currentPosition, out bool hasUnderline, DWRITE_TEXT_RANGE* textRange = null)
    {
        int b = 0;
        int hr = ((delegate* unmanaged[Stdcall]<void*, uint, int*, DWRITE_TEXT_RANGE*, int>)(*(void***)self)[51])(
            self, currentPosition, &b, textRange);
        hasUnderline = b != 0;
        return hr;
    }

    public static int GetStrikethrough(void* self, uint currentPosition, out bool hasStrikethrough, DWRITE_TEXT_RANGE* textRange = null)
    {
        int b = 0;
        int hr = ((delegate* unmanaged[Stdcall]<void*, uint, int*, DWRITE_TEXT_RANGE*, int>)(*(void***)self)[52])(
            self, currentPosition, &b, textRange);
        hasStrikethrough = b != 0;
        return hr;
    }

    public static int GetLocaleNameLength(void* self, uint currentPosition, out uint nameLength, DWRITE_TEXT_RANGE* textRange = null)
    {
        fixed (uint* p = &nameLength)
            return ((delegate* unmanaged[Stdcall]<void*, uint, uint*, DWRITE_TEXT_RANGE*, int>)(*(void***)self)[56])(
                self, currentPosition, p, textRange);
    }

    public static int GetLocaleName(void* self, uint currentPosition, char* localeName, uint nameSize, DWRITE_TEXT_RANGE* textRange = null)
        => ((delegate* unmanaged[Stdcall]<void*, uint, char*, uint, DWRITE_TEXT_RANGE*, int>)(*(void***)self)[57])(
            self, currentPosition, localeName, nameSize, textRange);

    public static int Draw(void* self, void* clientDrawingContext, void* renderer, float originX, float originY)
        => ((delegate* unmanaged[Stdcall]<void*, void*, void*, float, float, int>)(*(void***)self)[58])(
            self, clientDrawingContext, renderer, originX, originY);
}

internal unsafe interface IDWriteTextRendererCallback
{
    void OnGlyphRun(
        float baselineOriginX,
        float baselineOriginY,
        uint measuringMode,
        in DWRITE_GLYPH_RUN glyphRun,
        DWRITE_GLYPH_RUN_DESCRIPTION* glyphRunDescription,
        void* clientDrawingEffect);

    void OnUnderline(
        float baselineOriginX,
        float baselineOriginY,
        in DWRITE_UNDERLINE underline,
        void* clientDrawingEffect);

    void OnStrikethrough(
        float baselineOriginX,
        float baselineOriginY,
        in DWRITE_STRIKETHROUGH strikethrough,
        void* clientDrawingEffect);

    void OnInlineObject(
        float originX,
        float originY,
        void* inlineObject,
        bool isSideways,
        bool isRightToLeft,
        void* clientDrawingEffect);
}

/// <summary>
/// NativeAOT-compatible COM CCW implementing IDWriteTextRenderer : IDWritePixelSnapping.
/// </summary>
internal static unsafe class DWriteTextRendererCCW
{
    private const int S_OK = 0;
    private const int E_NOINTERFACE = unchecked((int)0x80004002);
    private const int E_POINTER = unchecked((int)0x80004003);
    private const int E_FAIL = unchecked((int)0x80004005);

    private static readonly void** s_vtbl;

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeRenderer
    {
        public void** Vtbl;
        public IntPtr Handle;
        public int RefCount;
        public int DisablePixelSnapping;
        public float PixelsPerDip;
    }

    static DWriteTextRendererCCW()
    {
        s_vtbl = (void**)NativeMemory.AllocZeroed(10, (nuint)sizeof(void*));
        s_vtbl[0] = (delegate* unmanaged[Stdcall]<NativeRenderer*, Guid*, void**, int>)&QueryInterface;
        s_vtbl[1] = (delegate* unmanaged[Stdcall]<NativeRenderer*, uint>)&AddRef;
        s_vtbl[2] = (delegate* unmanaged[Stdcall]<NativeRenderer*, uint>)&Release;
        s_vtbl[3] = (delegate* unmanaged[Stdcall]<NativeRenderer*, void*, int*, int>)&IsPixelSnappingDisabled;
        s_vtbl[4] = (delegate* unmanaged[Stdcall]<NativeRenderer*, void*, DWRITE_MATRIX*, int>)&GetCurrentTransform;
        s_vtbl[5] = (delegate* unmanaged[Stdcall]<NativeRenderer*, void*, float*, int>)&GetPixelsPerDip;
        s_vtbl[6] = (delegate* unmanaged[Stdcall]<NativeRenderer*, void*, float, float, uint, DWRITE_GLYPH_RUN*, DWRITE_GLYPH_RUN_DESCRIPTION*, void*, int>)&DrawGlyphRun;
        s_vtbl[7] = (delegate* unmanaged[Stdcall]<NativeRenderer*, void*, float, float, DWRITE_UNDERLINE*, void*, int>)&DrawUnderline;
        s_vtbl[8] = (delegate* unmanaged[Stdcall]<NativeRenderer*, void*, float, float, DWRITE_STRIKETHROUGH*, void*, int>)&DrawStrikethrough;
        s_vtbl[9] = (delegate* unmanaged[Stdcall]<NativeRenderer*, void*, float, float, void*, int, int, void*, int>)&DrawInlineObject;
    }

    public static void DrawLayout(
        void* textLayout,
        IDWriteTextRendererCallback callback,
        float originX = 0f,
        float originY = 0f,
        bool disablePixelSnapping = true,
        float pixelsPerDip = 1.0f)
    {
        NativeRenderer* native = (NativeRenderer*)NativeMemory.AllocZeroed(1, (nuint)sizeof(NativeRenderer));
        native->Vtbl = s_vtbl;
        native->Handle = GCHandle.ToIntPtr(GCHandle.Alloc(callback));
        native->RefCount = 1;
        native->DisablePixelSnapping = disablePixelSnapping ? 1 : 0;
        native->PixelsPerDip = pixelsPerDip > 0f ? pixelsPerDip : 1.0f;

        try
        {
            int hr = IDWriteTextLayoutDrawVtbl.Draw(textLayout, null, native, originX, originY);
            if (hr < 0)
            {
                Marshal.ThrowExceptionForHR(hr);
            }
        }
        finally
        {
            ReleaseCore(native);
        }
    }

    private static IDWriteTextRendererCallback? Unwrap(NativeRenderer* self)
    {
        if (self == null || self->Handle == IntPtr.Zero)
            return null;
        return GCHandle.FromIntPtr(self->Handle).Target as IDWriteTextRendererCallback;
    }

    private static uint AddRefCore(NativeRenderer* self)
        => (uint)Interlocked.Increment(ref self->RefCount);

    private static uint ReleaseCore(NativeRenderer* self)
    {
        int remaining = Interlocked.Decrement(ref self->RefCount);
        if (remaining == 0)
        {
            if (self->Handle != IntPtr.Zero)
            {
                GCHandle.FromIntPtr(self->Handle).Free();
                self->Handle = IntPtr.Zero;
            }
            NativeMemory.Free(self);
        }
        return (uint)Math.Max(0, remaining);
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
    private static int QueryInterface(NativeRenderer* self, Guid* riid, void** ppv)
    {
        if (self == null || riid == null || ppv == null)
            return E_POINTER;

        Guid g = *riid;
        if (g == DxWriteGuids.IID_IUnknown ||
            g == DxWriteGuids.IID_IDWritePixelSnapping ||
            g == DxWriteGuids.IID_IDWriteTextRenderer)
        {
            *ppv = self;
            AddRefCore(self);
            return S_OK;
        }

        *ppv = null;
        return E_NOINTERFACE;
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
    private static uint AddRef(NativeRenderer* self) => AddRefCore(self);

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
    private static uint Release(NativeRenderer* self) => ReleaseCore(self);

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
    private static int IsPixelSnappingDisabled(NativeRenderer* self, void* clientDrawingContext, int* isDisabled)
    {
        if (isDisabled == null)
            return E_POINTER;
        *isDisabled = self != null ? self->DisablePixelSnapping : 1;
        return S_OK;
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
    private static int GetCurrentTransform(NativeRenderer* self, void* clientDrawingContext, DWRITE_MATRIX* transform)
    {
        if (transform == null)
            return E_POINTER;
        *transform = DWRITE_MATRIX.Identity;
        return S_OK;
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
    private static int GetPixelsPerDip(NativeRenderer* self, void* clientDrawingContext, float* pixelsPerDip)
    {
        if (pixelsPerDip == null)
            return E_POINTER;
        *pixelsPerDip = self != null ? self->PixelsPerDip : 1.0f;
        return S_OK;
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
    private static int DrawGlyphRun(
        NativeRenderer* self,
        void* clientDrawingContext,
        float baselineOriginX,
        float baselineOriginY,
        uint measuringMode,
        DWRITE_GLYPH_RUN* glyphRun,
        DWRITE_GLYPH_RUN_DESCRIPTION* glyphRunDescription,
        void* clientDrawingEffect)
    {
        if (glyphRun == null)
            return E_POINTER;

        var cb = Unwrap(self);
        if (cb == null)
            return E_FAIL;

        try
        {
            cb.OnGlyphRun(baselineOriginX, baselineOriginY, measuringMode, in *glyphRun, glyphRunDescription, clientDrawingEffect);
            return S_OK;
        }
        catch
        {
            return E_FAIL;
        }
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
    private static int DrawUnderline(
        NativeRenderer* self,
        void* clientDrawingContext,
        float baselineOriginX,
        float baselineOriginY,
        DWRITE_UNDERLINE* underline,
        void* clientDrawingEffect)
    {
        if (underline == null)
            return E_POINTER;

        var cb = Unwrap(self);
        if (cb == null)
            return E_FAIL;

        try
        {
            cb.OnUnderline(baselineOriginX, baselineOriginY, in *underline, clientDrawingEffect);
            return S_OK;
        }
        catch
        {
            return E_FAIL;
        }
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
    private static int DrawStrikethrough(
        NativeRenderer* self,
        void* clientDrawingContext,
        float baselineOriginX,
        float baselineOriginY,
        DWRITE_STRIKETHROUGH* strikethrough,
        void* clientDrawingEffect)
    {
        if (strikethrough == null)
            return E_POINTER;

        var cb = Unwrap(self);
        if (cb == null)
            return E_FAIL;

        try
        {
            cb.OnStrikethrough(baselineOriginX, baselineOriginY, in *strikethrough, clientDrawingEffect);
            return S_OK;
        }
        catch
        {
            return E_FAIL;
        }
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
    private static int DrawInlineObject(
        NativeRenderer* self,
        void* clientDrawingContext,
        float originX,
        float originY,
        void* inlineObject,
        int isSideways,
        int isRightToLeft,
        void* clientDrawingEffect)
    {
        var cb = Unwrap(self);
        if (cb == null)
            return E_FAIL;

        try
        {
            cb.OnInlineObject(originX, originY, inlineObject, isSideways != 0, isRightToLeft != 0, clientDrawingEffect);
            return S_OK;
        }
        catch
        {
            return E_FAIL;
        }
    }
}

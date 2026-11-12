// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using CSUI.Core.Interop.DirectX;

namespace MS.Internal.Text.TextInterface
{
    internal enum FactoryType : uint
    {
        Shared = 0,
        Isolated = 1,
    }

    internal enum FontFaceType : uint
    {
        CFF = 0,
        TrueType = 1,
        TrueTypeCollection = 2,
        Type1 = 3,
        Vector = 4,
        Bitmap = 5,
        Unknown = 6,
        RawCFF = 7,
    }

    internal enum FontFileType : uint
    {
        Unknown = 0,
        CFF = 1,
        TrueType = 2,
        TrueTypeCollection = 3,
        Type1_PFM = 4,
        Type1_PFB = 5,
        Vector = 6,
        Bitmap = 7,
    }

    [Flags]
    internal enum FontSimulations : uint
    {
        None = 0x0000,
        Bold = 0x0001,
        Oblique = 0x0002,
    }

    internal enum FontWeight : int
    {
        Thin = 100,
        ExtraLight = 200,
        UltraLight = 200,
        Light = 300,
        SemiLight = 350,
        Normal = 400,
        Regular = 400,
        Medium = 500,
        DemiBold = 600,
        SemiBold = 600,
        Bold = 700,
        ExtraBold = 800,
        UltraBold = 800,
        Black = 900,
        Heavy = 900,
        ExtraBlack = 950,
        UltraBlack = 950,
    }

    internal enum FontStretch : int
    {
        Undefined = 0,
        UltraCondensed = 1,
        ExtraCondensed = 2,
        Condensed = 3,
        SemiCondensed = 4,
        Normal = 5,
        Medium = 5,
        SemiExpanded = 6,
        Expanded = 7,
        ExtraExpanded = 8,
        UltraExpanded = 9,
    }

    internal enum FontStyle : int
    {
        Normal = 0,
        Oblique = 1,
        Italic = 2,
    }

    internal enum InformationalStringID : uint
    {
        None = 0,
        CopyrightNotice = 1,
        VersionStrings = 2,
        Trademark = 3,
        Manufacturer = 4,
        Designer = 5,
        DesignerURL = 6,
        Description = 7,
        FontVendorURL = 8,
        LicenseDescription = 9,
        LicenseInfoURL = 10,
        WIN32FamilyNames = 11,
        Win32SubFamilyNames = 12,
        PreferredFamilyNames = 13,
        PreferredSubFamilyNames = 14,
        SampleText = 15,
        FullName = 16,
        PostScriptName = 17,
        PostScriptCidName = 18,
        WwsFamilyName = 19,
    }

    internal enum OpenTypeTableTag : uint
    {
        TTO_GSUB = ('G') | ('S' << 8) | ('U' << 16) | ('B' << 24),
        TTO_GPOS = ('G') | ('P' << 8) | ('O' << 16) | ('S' << 24),
        TTO_GDEF = ('G') | ('D' << 8) | ('E' << 16) | ('F' << 24),
        TTO_OS2  = ('O') | ('S' << 8) | ('/' << 16) | ('2' << 24),
        TTO_head = ('h') | ('e' << 8) | ('a' << 16) | ('d' << 24),
        TTO_name = ('n') | ('a' << 8) | ('m' << 16) | ('e' << 24),
    }

    internal enum DWriteFontFeatureTag : uint
    {
        AlternativeFractions            = ('a') | ('f' << 8) | ('r' << 16) | ('c' << 24),
        PetiteCapitalsFromCapitals      = ('c') | ('2' << 8) | ('p' << 16) | ('c' << 24),
        SmallCapitalsFromCapitals       = ('c') | ('2' << 8) | ('s' << 16) | ('c' << 24),
        ContextualAlternates            = ('c') | ('a' << 8) | ('l' << 16) | ('t' << 24),
        CaseSensitiveForms              = ('c') | ('a' << 8) | ('s' << 16) | ('e' << 24),
        GlyphCompositionDecomposition   = ('c') | ('c' << 8) | ('m' << 16) | ('p' << 24),
        ContextualLigatures             = ('c') | ('l' << 8) | ('i' << 16) | ('g' << 24),
        CapitalSpacing                  = ('c') | ('p' << 8) | ('s' << 16) | ('p' << 24),
        ContextualSwash                 = ('c') | ('s' << 8) | ('w' << 16) | ('h' << 24),
        CursivePositioning              = ('c') | ('u' << 8) | ('r' << 16) | ('s' << 24),
        Default                         = ('d') | ('f' << 8) | ('l' << 16) | ('t' << 24),
        DiscretionaryLigatures          = ('d') | ('l' << 8) | ('i' << 16) | ('g' << 24),
        ExpertForms                     = ('e') | ('x' << 8) | ('p' << 16) | ('t' << 24),
        Fractions                       = ('f') | ('r' << 8) | ('a' << 16) | ('c' << 24),
        FullWidth                       = ('f') | ('w' << 8) | ('i' << 16) | ('d' << 24),
        HalfForms                       = ('h') | ('a' << 8) | ('l' << 16) | ('f' << 24),
        HalantForms                     = ('h') | ('a' << 8) | ('l' << 16) | ('n' << 24),
        AlternateHalfWidth              = ('h') | ('a' << 8) | ('l' << 16) | ('t' << 24),
        HistoricalForms                 = ('h') | ('i' << 8) | ('s' << 16) | ('t' << 24),
        HorizontalKanaAlternates        = ('h') | ('k' << 8) | ('n' << 16) | ('a' << 24),
        HistoricalLigatures             = ('h') | ('l' << 8) | ('i' << 16) | ('g' << 24),
        HalfWidth                       = ('h') | ('w' << 8) | ('i' << 16) | ('d' << 24),
        HojoKanjiForms                  = ('h') | ('o' << 8) | ('j' << 16) | ('o' << 24),
        JIS04Forms                      = ('j') | ('p' << 8) | ('0' << 16) | ('4' << 24),
        JIS78Forms                      = ('j') | ('p' << 8) | ('7' << 16) | ('8' << 24),
        JIS83Forms                      = ('j') | ('p' << 8) | ('8' << 16) | ('3' << 24),
        JIS90Forms                      = ('j') | ('p' << 8) | ('9' << 16) | ('0' << 24),
        Kerning                         = ('k') | ('e' << 8) | ('r' << 16) | ('n' << 24),
        StandardLigatures               = ('l') | ('i' << 8) | ('g' << 16) | ('a' << 24),
        LiningFigures                   = ('l') | ('n' << 8) | ('u' << 16) | ('m' << 24),
        LocalizedForms                  = ('l') | ('o' << 8) | ('c' << 16) | ('l' << 24),
        MarkPositioning                 = ('m') | ('a' << 8) | ('r' << 16) | ('k' << 24),
        MathematicalGreek               = ('m') | ('g' << 8) | ('r' << 16) | ('k' << 24),
        MarkToMarkPositioning           = ('m') | ('k' << 8) | ('m' << 16) | ('k' << 24),
        AlternateAnnotationForms        = ('n') | ('a' << 8) | ('l' << 16) | ('t' << 24),
        NLCKanjiForms                   = ('n') | ('l' << 8) | ('c' << 16) | ('k' << 24),
        OldStyleFigures                 = ('o') | ('n' << 8) | ('u' << 16) | ('m' << 24),
        Ordinals                        = ('o') | ('r' << 8) | ('d' << 16) | ('n' << 24),
        ProportionalAlternateWidth      = ('p') | ('a' << 8) | ('l' << 16) | ('t' << 24),
        PetiteCapitals                  = ('p') | ('c' << 8) | ('a' << 16) | ('p' << 24),
        ProportionalFigures             = ('p') | ('n' << 8) | ('u' << 16) | ('m' << 24),
        ProportionalWidths              = ('p') | ('w' << 8) | ('i' << 16) | ('d' << 24),
        QuarterWidths                   = ('q') | ('w' << 8) | ('i' << 16) | ('d' << 24),
        RequiredLigatures               = ('r') | ('l' << 8) | ('i' << 16) | ('g' << 24),
        RubyNotationForms               = ('r') | ('u' << 8) | ('b' << 16) | ('y' << 24),
        StylisticAlternates             = ('s') | ('a' << 8) | ('l' << 16) | ('t' << 24),
        ScientificInferiors             = ('s') | ('i' << 8) | ('n' << 16) | ('f' << 24),
        SmallCapitals                   = ('s') | ('m' << 8) | ('c' << 16) | ('p' << 24),
        SimplifiedForms                 = ('s') | ('m' << 8) | ('p' << 16) | ('l' << 24),
        StylisticSet1                   = ('s') | ('s' << 8) | ('0' << 16) | ('1' << 24),
        StylisticSet2                   = ('s') | ('s' << 8) | ('0' << 16) | ('2' << 24),
        StylisticSet3                   = ('s') | ('s' << 8) | ('0' << 16) | ('3' << 24),
        StylisticSet4                   = ('s') | ('s' << 8) | ('0' << 16) | ('4' << 24),
        StylisticSet5                   = ('s') | ('s' << 8) | ('0' << 16) | ('5' << 24),
        StylisticSet6                   = ('s') | ('s' << 8) | ('0' << 16) | ('6' << 24),
        StylisticSet7                   = ('s') | ('s' << 8) | ('0' << 16) | ('7' << 24),
        StylisticSet8                   = ('s') | ('s' << 8) | ('0' << 16) | ('8' << 24),
        StylisticSet9                   = ('s') | ('s' << 8) | ('0' << 16) | ('9' << 24),
        StylisticSet10                  = ('s') | ('s' << 8) | ('1' << 16) | ('0' << 24),
        StylisticSet11                  = ('s') | ('s' << 8) | ('1' << 16) | ('1' << 24),
        StylisticSet12                  = ('s') | ('s' << 8) | ('1' << 16) | ('2' << 24),
        StylisticSet13                  = ('s') | ('s' << 8) | ('1' << 16) | ('3' << 24),
        StylisticSet14                  = ('s') | ('s' << 8) | ('1' << 16) | ('4' << 24),
        StylisticSet15                  = ('s') | ('s' << 8) | ('1' << 16) | ('5' << 24),
        StylisticSet16                  = ('s') | ('s' << 8) | ('1' << 16) | ('6' << 24),
        StylisticSet17                  = ('s') | ('s' << 8) | ('1' << 16) | ('7' << 24),
        StylisticSet18                  = ('s') | ('s' << 8) | ('1' << 16) | ('8' << 24),
        StylisticSet19                  = ('s') | ('s' << 8) | ('1' << 16) | ('9' << 24),
        StylisticSet20                  = ('s') | ('s' << 8) | ('2' << 16) | ('0' << 24),
        Subscript                       = ('s') | ('u' << 8) | ('b' << 16) | ('s' << 24),
        Superscript                     = ('s') | ('u' << 8) | ('p' << 16) | ('s' << 24),
        Swash                           = ('s') | ('w' << 8) | ('s' << 16) | ('h' << 24),
        Titling                         = ('t') | ('i' << 8) | ('t' << 16) | ('l' << 24),
        TraditionalNameForms            = ('t') | ('n' << 8) | ('a' << 16) | ('m' << 24),
        TabularFigures                  = ('t') | ('n' << 8) | ('u' << 16) | ('m' << 24),
        TraditionalForms                = ('t') | ('r' << 8) | ('a' << 16) | ('d' << 24),
        ThirdWidths                     = ('t') | ('w' << 8) | ('i' << 16) | ('d' << 24),
        Unicase                         = ('u') | ('n' << 8) | ('i' << 16) | ('c' << 24),
        VerticalWriting                 = ('v') | ('e' << 8) | ('r' << 16) | ('t' << 24),
        VerticalAlternatesAndRotation   = ('v') | ('r' << 8) | ('t' << 16) | ('2' << 24),
        SlashedZero                     = ('z') | ('e' << 8) | ('r' << 16) | ('o' << 24),
    }

    internal struct DWriteFontFeature
    {
        internal DWriteFontFeatureTag NameTag { get; }
        internal uint Parameter { get; }

        internal DWriteFontFeature(DWriteFontFeatureTag nameTag, uint parameter)
        {
            NameTag = nameTag;
            Parameter = parameter;
        }
    }

    internal struct GlyphOffset
    {
        public int du;
        public int dv;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct GlyphMetrics
    {
        public int LeftSideBearing;
        public uint AdvanceWidth;
        public int RightSideBearing;
        public int TopSideBearing;
        public uint AdvanceHeight;
        public int BottomSideBearing;
        public int VerticalOriginY;
    }

    internal sealed class FontMetrics
    {
        internal FontMetrics(in DWRITE_FONT_METRICS native)
        {
            DesignUnitsPerEm = native.designUnitsPerEm;
            Ascent = native.ascent;
            Descent = native.descent;
            LineGap = native.lineGap;
            CapHeight = native.capHeight;
            XHeight = native.xHeight;
            UnderlinePosition = native.underlinePosition;
            UnderlineThickness = native.underlineThickness;
            StrikethroughPosition = native.strikethroughPosition;
            StrikethroughThickness = native.strikethroughThickness;
        }

        internal ushort DesignUnitsPerEm { get; }
        internal ushort Ascent { get; }
        internal ushort Descent { get; }
        internal short LineGap { get; }
        internal ushort CapHeight { get; }
        internal ushort XHeight { get; }
        internal short UnderlinePosition { get; }
        internal ushort UnderlineThickness { get; }
        internal short StrikethroughPosition { get; }
        internal ushort StrikethroughThickness { get; }

        internal double Baseline => DesignUnitsPerEm == 0 ? 0.0 : (double)Ascent / DesignUnitsPerEm;
        internal double LineSpacing => DesignUnitsPerEm == 0 ? 0.0 : (double)(Ascent + Descent + LineGap) / DesignUnitsPerEm;
    }

    internal sealed class ItemProps
    {
        internal ItemProps()
        {
            NumberSubstitutionMethod = DxWriteConst.DWRITE_NUMBER_SUBSTITUTION_METHOD_NONE;
        }

        internal ItemProps(
            DWRITE_SCRIPT_ANALYSIS scriptAnalysis,
            CultureInfo? digitCulture,
            uint numberSubstitutionMethod,
            bool ignoreUserOverride,
            bool hasCombiningMark,
            bool needsCaretInfo,
            bool hasExtendedCharacter,
            bool isIndic,
            bool isLatin)
        {
            ScriptAnalysis = scriptAnalysis;
            DigitCulture = digitCulture;
            NumberSubstitutionMethod = numberSubstitutionMethod;
            IgnoreUserOverride = ignoreUserOverride;
            HasCombiningMark = hasCombiningMark;
            NeedsCaretInfo = needsCaretInfo;
            HasExtendedCharacter = hasExtendedCharacter;
            IsIndic = isIndic;
            IsLatin = isLatin;
        }

        internal DWRITE_SCRIPT_ANALYSIS ScriptAnalysis { get; }
        internal CultureInfo? DigitCulture { get; }
        internal uint NumberSubstitutionMethod { get; }
        internal bool IgnoreUserOverride { get; }
        internal bool HasCombiningMark { get; }
        internal bool NeedsCaretInfo { get; }
        internal bool HasExtendedCharacter { get; }
        internal bool IsIndic { get; }
        internal bool IsLatin { get; }

        internal bool CanShapeTogether(ItemProps other)
        {
            if (other == null)
                return false;

            return ScriptAnalysis.script == other.ScriptAnalysis.script
                && ScriptAnalysis.shapes == other.ScriptAnalysis.shapes
                && HasCombiningMark == other.HasCombiningMark
                && NeedsCaretInfo == other.NeedsCaretInfo
                && HasExtendedCharacter == other.HasExtendedCharacter
                && IsIndic == other.IsIndic
                && IsLatin == other.IsLatin
                && NumberSubstitutionMethod == other.NumberSubstitutionMethod
                && IgnoreUserOverride == other.IgnoreUserOverride
                && Equals(DigitCulture, other.DigitCulture);
        }

        public override bool Equals(object? obj)
            => ReferenceEquals(this, obj) || (obj is ItemProps other && CanShapeTogether(other));

        public override int GetHashCode()
        {
            HashCode hc = new();
            hc.Add(ScriptAnalysis.script);
            hc.Add(ScriptAnalysis.shapes);
            hc.Add(DigitCulture);
            hc.Add(NumberSubstitutionMethod);
            hc.Add(IgnoreUserOverride);
            hc.Add(HasCombiningMark);
            hc.Add(NeedsCaretInfo);
            hc.Add(HasExtendedCharacter);
            hc.Add(IsIndic);
            hc.Add(IsLatin);
            return hc.ToHashCode();
        }
    }

    internal interface IClassification
    {
        void GetCharAttribute(
            int unicodeScalar,
            out bool isCombining,
            out bool needsCaretInfo,
            out bool isIndic,
            out bool isDigit,
            out bool isLatin,
            out bool isStrong);
    }

    internal interface IFontSource
    {
        Uri Uri { get; }
        bool IsComposite { get; }
        DateTime GetLastWriteTimeUtc();
        UnmanagedMemoryStream GetUnmanagedStream();
        Stream GetStream();
        void TestFileOpenable();
    }

    internal interface IFontSourceFactory
    {
        IFontSource Create(string uriString);
    }

    internal interface IFontSourceCollection : IEnumerable<IFontSource>
    {
    }

    internal interface IFontSourceCollectionFactory
    {
        IFontSourceCollection Create(string uriString);
    }

    internal static class LocalizedErrorMsgs
    {
        private static string _enumeratorNotStarted = "Enumeration has not started. Call MoveNext.";
        private static string _enumeratorReachedEnd = "Enumeration already finished.";

        internal static string EnumeratorNotStarted
        {
            get => _enumeratorNotStarted;
            set => _enumeratorNotStarted = value ?? string.Empty;
        }

        internal static string EnumeratorReachedEnd
        {
            get => _enumeratorReachedEnd;
            set => _enumeratorReachedEnd = value ?? string.Empty;
        }
    }

    internal static class DWriteUtil
    {
        private const int COR_E_INVALIDOPERATION = 0x1509;
        private const int DWRITE_E_FILENOTFOUND = unchecked((int)0x88985003L);
        private const int DWRITE_E_FILEACCESS = unchecked((int)0x88985004L);
        private const int DWRITE_E_FILEFORMAT = unchecked((int)0x88985000L);

        internal static void ConvertHresultToException(int hr)
        {
            if (hr == 0)
                return;

            if (hr == DWRITE_E_FILENOTFOUND)
                throw new FileNotFoundException();
            if (hr == DWRITE_E_FILEACCESS)
                throw new UnauthorizedAccessException();
            if (hr == DWRITE_E_FILEFORMAT)
                throw new FileFormatException();

            SanitizeAndThrowIfKnownException(hr);
            Marshal.ThrowExceptionForHR(hr, new IntPtr(-1));
        }

        private static void SanitizeAndThrowIfKnownException(int hr)
        {
            if (hr == COR_E_INVALIDOPERATION)
            {
                Exception? e = Marshal.GetExceptionForHR(hr);
                if (e is WebException)
                {
                    throw e;
                }
            }
        }
    }

    internal static unsafe class ComHelpers
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static uint AddRef(void* pUnknown)
        {
            if (pUnknown == null)
                return 0u;
            return ((delegate* unmanaged[Stdcall]<void*, uint>)(*(void***)pUnknown)[1])(pUnknown);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static uint Release(void* pUnknown)
        {
            if (pUnknown == null)
                return 0u;
            return ((delegate* unmanaged[Stdcall]<void*, uint>)(*(void***)pUnknown)[2])(pUnknown);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static int QueryInterface(void* pUnknown, in Guid iid, out void* ppvObject)
        {
            if (pUnknown == null)
            {
                ppvObject = null;
                return NativeComInstance.E_POINTER;
            }

            fixed (Guid* pIid = &iid)
            fixed (void** pOut = &ppvObject)
            {
                return ((delegate* unmanaged[Stdcall]<void*, Guid*, void**, int>)(*(void***)pUnknown)[0])(pUnknown, pIid, pOut);
            }
        }

        internal static void ReleaseAtomic(ref nint ptr)
        {
            nint p = System.Threading.Interlocked.Exchange(ref ptr, IntPtr.Zero);
            if (p != IntPtr.Zero)
            {
                Release((void*)p);
            }
        }
    }
}

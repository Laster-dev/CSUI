// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Windows.Media;
using CSUI.Core.Interop.DirectX;

namespace MS.Internal.Text.TextInterface
{
    internal sealed unsafe class TextAnalyzer : IDisposable
    {
        internal const char CharHyphen = '\x002d';
        private const int S_OK = 0;

        private nint _textAnalyzer;
        private nint _dwriteFactory;

        internal TextAnalyzer(void* textAnalyzer, void* dwriteFactory)
        {
            _textAnalyzer = (nint)textAnalyzer;
            _dwriteFactory = (nint)dwriteFactory;
            if (dwriteFactory != null)
                ComHelpers.AddRef(dwriteFactory);
        }

        ~TextAnalyzer()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            ComHelpers.ReleaseAtomic(ref _textAnalyzer);
            ComHelpers.ReleaseAtomic(ref _dwriteFactory);
        }

        private void* RequireAnalyzer()
        {
            void* p = (void*)_textAnalyzer;
            if (p == null)
                throw new ObjectDisposedException(nameof(TextAnalyzer));
            return p;
        }

        internal static IList<MS.Internal.Span> Itemize(
            char* textString,
            uint length,
            CultureInfo culture,
            void* pDWriteFactory,
            bool isRightToLeftParagraph,
            CultureInfo? numberCulture,
            bool ignoreUserOverride,
            uint numberSubstitutionMethod,
            IClassification classification)
        {
            if (textString == null || length == 0)
                return new List<MS.Internal.Span>(0);

            if (pDWriteFactory == null)
                throw new ArgumentNullException(nameof(pDWriteFactory));
            ArgumentNullException.ThrowIfNull(classification);

            void* pAnalyzer = null;
            int hr = IDWriteFactoryWriteVtbl.CreateTextAnalyzer(pDWriteFactory, &pAnalyzer);
            DWriteUtil.ConvertHresultToException(hr);

            void* pNumberSubstitution = null;
            if (numberCulture != null &&
                numberSubstitutionMethod != DxWriteConst.DWRITE_NUMBER_SUBSTITUTION_METHOD_NONE)
            {
                hr = IDWriteFactoryWriteVtbl.CreateNumberSubstitution(
                    pDWriteFactory,
                    numberSubstitutionMethod,
                    numberCulture.Name,
                    ignoreUserOverride,
                    &pNumberSubstitution);
                DWriteUtil.ConvertHresultToException(hr);
            }

            DWRITE_SCRIPT_ANALYSIS[] scripts = new DWRITE_SCRIPT_ANALYSIS[length];
            bool[] hasSubstitution = new bool[length];

            string localeName = culture?.Name ?? string.Empty;
            void* pSourceCCW = null;
            void* pSinkCCW = null;

            try
            {
                fixed (char* pLocale = localeName)
                {
                    var source = new AnalysisSource(
                        textString,
                        length,
                        isRightToLeftParagraph,
                        pLocale,
                        pNumberSubstitution);
                    var sink = new AnalysisSink(scripts, hasSubstitution);

                    pSourceCCW = DWriteTextAnalysisSourceCCW.Create(source);
                    pSinkCCW = DWriteTextAnalysisSinkCCW.Create(sink);

                    hr = IDWriteTextAnalyzerVtbl.AnalyzeScript(pAnalyzer, pSourceCCW, 0, length, pSinkCCW);
                    DWriteUtil.ConvertHresultToException(hr);

                    if (pNumberSubstitution != null)
                    {
                        hr = IDWriteTextAnalyzerVtbl.AnalyzeNumberSubstitution(pAnalyzer, pSourceCCW, 0, length, pSinkCCW);
                        DWriteUtil.ConvertHresultToException(hr);
                    }
                }
            }
            finally
            {
                if (pSourceCCW != null)
                    ComHelpers.Release(pSourceCCW);
                if (pSinkCCW != null)
                    ComHelpers.Release(pSinkCCW);
                if (pNumberSubstitution != null)
                    ComHelpers.Release(pNumberSubstitution);
                if (pAnalyzer != null)
                    ComHelpers.Release(pAnalyzer);
            }

            var spans = new List<MS.Internal.Span>();
            int total = checked((int)length);
            int idx = 0;

            DWRITE_SCRIPT_ANALYSIS curScript = default;
            bool curHasNumSub = false;
            bool curHasCombining = false;
            bool curNeedsCaret = false;
            bool curHasExtended = false;
            bool curIsIndic = false;
            bool curIsLatin = false;
            int runLen = 0;

            while (idx < total)
            {
                char ch = textString[idx];
                int step = 1;
                int scalar = ch;
                if (char.IsHighSurrogate(ch) && idx + 1 < total && char.IsLowSurrogate(textString[idx + 1]))
                {
                    scalar = char.ConvertToUtf32(ch, textString[idx + 1]);
                    step = 2;
                }

                bool extended = step == 2 || char.IsSurrogate(ch);
                classification.GetCharAttribute(
                    scalar,
                    out bool isCombining,
                    out bool needsCaretInfo,
                    out bool isIndic,
                    out bool _,
                    out bool isLatin,
                    out bool _);

                DWRITE_SCRIPT_ANALYSIS sa = scripts[idx];
                bool numSub = hasSubstitution[idx] && numberCulture != null;

                if (runLen == 0)
                {
                    curScript = sa;
                    curHasNumSub = numSub;
                    curHasCombining = isCombining;
                    curNeedsCaret = needsCaretInfo;
                    curHasExtended = extended;
                    curIsIndic = isIndic;
                    curIsLatin = isLatin;
                    runLen = step;
                }
                else
                {
                    bool sameScript = curScript.script == sa.script && curScript.shapes == sa.shapes;
                    bool sameSub = curHasNumSub == numSub;

                    bool canExtend = sameScript && sameSub && (
                        isCombining
                        || (!curHasCombining
                            && curIndicMatch(curIsIndic, isIndic, curIsLatin, isLatin, curNeedsCaret, needsCaretInfo, curHasExtended, extended)));

                    if (canExtend)
                    {
                        curHasCombining |= isCombining;
                        curNeedsCaret |= needsCaretInfo;
                        curHasExtended |= extended;
                        runLen += step;
                    }
                    else
                    {
                        spans.Add(new MS.Internal.Span(
                            new ItemProps(
                                curScript,
                                curHasNumSub ? numberCulture : null,
                                curHasNumSub ? numberSubstitutionMethod : DxWriteConst.DWRITE_NUMBER_SUBSTITUTION_METHOD_NONE,
                                ignoreUserOverride,
                                curHasCombining,
                                curNeedsCaret,
                                curHasExtended,
                                curIsIndic,
                                curIsLatin),
                            runLen));

                        curScript = sa;
                        curHasNumSub = numSub;
                        curHasCombining = isCombining;
                        curNeedsCaret = needsCaretInfo;
                        curHasExtended = extended;
                        curIsIndic = isIndic;
                        curIsLatin = isLatin;
                        runLen = step;
                    }
                }

                idx += step;
            }

            if (runLen > 0)
            {
                spans.Add(new MS.Internal.Span(
                    new ItemProps(
                        curScript,
                        curHasNumSub ? numberCulture : null,
                        curHasNumSub ? numberSubstitutionMethod : DxWriteConst.DWRITE_NUMBER_SUBSTITUTION_METHOD_NONE,
                        ignoreUserOverride,
                        curHasCombining,
                        curNeedsCaret,
                        curHasExtended,
                        curIsIndic,
                        curIsLatin),
                    runLen));
            }

            return spans;
        }

        internal void AnalyzeLineBreakpoints(
            char* textString,
            uint length,
            CultureInfo? culture,
            bool isRightToLeftParagraph,
            DWRITE_LINE_BREAKPOINT[] lineBreakpoints)
        {
            if (textString == null || length == 0)
                return;

            ArgumentNullException.ThrowIfNull(lineBreakpoints);
            if ((uint)lineBreakpoints.Length < length)
                throw new ArgumentException("lineBreakpoints array is smaller than length.", nameof(lineBreakpoints));

            void* pAnalyzer = RequireAnalyzer();
            string localeName = culture?.Name ?? string.Empty;
            void* pSourceCCW = null;
            void* pSinkCCW = null;

            try
            {
                fixed (char* pLocale = localeName)
                {
                    var source = new AnalysisSource(
                        textString,
                        length,
                        isRightToLeftParagraph,
                        pLocale,
                        null);
                    var sink = new AnalysisSink(
                        Array.Empty<DWRITE_SCRIPT_ANALYSIS>(),
                        Array.Empty<bool>(),
                        lineBreakpoints);

                    pSourceCCW = DWriteTextAnalysisSourceCCW.Create(source);
                    pSinkCCW = DWriteTextAnalysisSinkCCW.Create(sink);

                    int hr = IDWriteTextAnalyzerVtbl.AnalyzeLineBreakpoints(pAnalyzer, pSourceCCW, 0, length, pSinkCCW);
                    DWriteUtil.ConvertHresultToException(hr);
                }
            }
            finally
            {
                if (pSourceCCW != null)
                    ComHelpers.Release(pSourceCCW);
                if (pSinkCCW != null)
                    ComHelpers.Release(pSinkCCW);
            }
        }

        private static bool curIndicMatch(
            bool aIndic, bool bIndic,
            bool aLatin, bool bLatin,
            bool aCaret, bool bCaret,
            bool aExt, bool bExt)
            => aIndic == bIndic && aLatin == bLatin && aCaret == bCaret && aExt == bExt;

        internal void GetGlyphs(
            char* textString,
            uint textLength,
            Font font,
            ushort blankGlyphIndex,
            bool isSideways,
            bool isRightToLeft,
            CultureInfo cultureInfo,
            DWriteFontFeature[][]? fontFeatures,
            uint[]? fontFeatureRanges,
            uint maxGlyphCount,
            TextFormattingMode textFormattingMode,
            ItemProps textItem,
            ushort* clusterMap,
            ushort* charProperties,
            ushort* glyphIndices,
            uint* glyphProperties,
            int* pfCanGlyphAlone,
            out uint actualGlyphCount)
        {
            int hr = TryGetGlyphs(
                textString,
                textLength,
                font,
                blankGlyphIndex,
                isSideways,
                isRightToLeft,
                cultureInfo,
                fontFeatures,
                fontFeatureRanges,
                maxGlyphCount,
                textFormattingMode,
                textItem,
                clusterMap,
                charProperties,
                glyphIndices,
                glyphProperties,
                pfCanGlyphAlone,
                out actualGlyphCount);
            DWriteUtil.ConvertHresultToException(hr);
        }

        private int TryGetGlyphs(
            char* textString,
            uint textLength,
            Font font,
            ushort blankGlyphIndex,
            bool isSideways,
            bool isRightToLeft,
            CultureInfo cultureInfo,
            DWriteFontFeature[][]? fontFeatures,
            uint[]? fontFeatureRanges,
            uint maxGlyphCount,
            TextFormattingMode textFormattingMode,
            ItemProps textItem,
            ushort* clusterMap,
            ushort* charProperties,
            ushort* glyphIndices,
            uint* glyphProperties,
            int* pfCanGlyphAlone,
            out uint actualGlyphCount)
        {
            ArgumentNullException.ThrowIfNull(font);
            ArgumentNullException.ThrowIfNull(textItem);

            void* pAnalyzer = RequireAnalyzer();
            using FontFace fontFace = font.GetFontFace();

            void* pNumSub = CreateNumberSubstitutionIfNeeded(textItem);
            bool heapGlyphProps = maxGlyphCount > 512;
            ushort* stackGlyphProps = stackalloc ushort[(int)(heapGlyphProps ? 1 : maxGlyphCount)];
            ushort* pGlyphProps16 = heapGlyphProps
                ? (ushort*)NativeMemory.Alloc(maxGlyphCount, sizeof(ushort))
                : stackGlyphProps;

            FeatureNativeBuffer features = AllocateFeatures(fontFeatures, fontFeatureRanges);

            try
            {
                DWRITE_SCRIPT_ANALYSIS sa = textItem.ScriptAnalysis;
                string locale = cultureInfo?.Name ?? string.Empty;
                int hr;

                fixed (char* pLocale = locale)
                {
                    hr = IDWriteTextAnalyzerVtbl.GetGlyphs(
                        pAnalyzer,
                        textString,
                        textLength,
                        fontFace.DWriteFontFaceNoAddRef,
                        isSideways,
                        isRightToLeft,
                        &sa,
                        pLocale,
                        pNumSub,
                        features.RangePointers,
                        features.RangeLengths,
                        features.RangeCount,
                        maxGlyphCount,
                        clusterMap,
                        charProperties,
                        glyphIndices,
                        pGlyphProps16,
                        out actualGlyphCount);
                }

                if (hr >= 0)
                {
                    if (glyphProperties != null)
                    {
                        for (uint i = 0; i < actualGlyphCount; i++)
                            glyphProperties[i] = pGlyphProps16[i];
                    }

                    if (pfCanGlyphAlone != null && charProperties != null)
                    {
                        for (uint i = 0; i < textLength; i++)
                            pfCanGlyphAlone[i] = (charProperties[i] & 0x1) != 0 ? 1 : 0;
                    }

                    if (blankGlyphIndex != 0 && clusterMap != null && glyphIndices != null)
                    {
                        for (uint i = 0; i < textLength; i++)
                        {
                            if (textString[i] < 0x20)
                            {
                                ushort gi = clusterMap[i];
                                if (gi < actualGlyphCount && glyphIndices[gi] == 0)
                                    glyphIndices[gi] = blankGlyphIndex;
                            }
                        }
                    }
                }

                return hr;
            }
            finally
            {
                features.Free();
                if (heapGlyphProps && pGlyphProps16 != null)
                    NativeMemory.Free(pGlyphProps16);
                if (pNumSub != null)
                    ComHelpers.Release(pNumSub);
            }
        }

        internal void GetGlyphPlacements(
            char* textString,
            ushort* clusterMap,
            ushort* charProperties,
            uint textLength,
            ushort* glyphIndices,
            uint* glyphProperties,
            uint glyphCount,
            Font font,
            double fontEmSize,
            double scalingFactor,
            bool isSideways,
            bool isRightToLeft,
            CultureInfo cultureInfo,
            DWriteFontFeature[][]? fontFeatures,
            uint[]? fontFeatureRanges,
            TextFormattingMode textFormattingMode,
            ItemProps textItem,
            float pixelsPerDip,
            int* glyphAdvances,
            out GlyphOffset[] glyphOffsets)
        {
            ArgumentNullException.ThrowIfNull(font);
            ArgumentNullException.ThrowIfNull(textItem);

            if (glyphCount == 0)
            {
                glyphOffsets = Array.Empty<GlyphOffset>();
                return;
            }

            void* pAnalyzer = RequireAnalyzer();
            using FontFace fontFace = font.GetFontFace();

            bool useStack = glyphCount <= 256;
            ushort* stackGlyphProps = stackalloc ushort[(int)(useStack ? glyphCount : 1)];
            float* stackAdvances = stackalloc float[(int)(useStack ? glyphCount : 1)];
            DWRITE_GLYPH_OFFSET* stackOffsets = stackalloc DWRITE_GLYPH_OFFSET[(int)(useStack ? glyphCount : 1)];

            ushort* pGlyphProps16 = useStack
                ? stackGlyphProps
                : (ushort*)NativeMemory.Alloc(glyphCount, sizeof(ushort));
            float* pAdvances = useStack
                ? stackAdvances
                : (float*)NativeMemory.Alloc(glyphCount, sizeof(float));
            DWRITE_GLYPH_OFFSET* pOffsets = useStack
                ? stackOffsets
                : (DWRITE_GLYPH_OFFSET*)NativeMemory.Alloc(glyphCount, (nuint)sizeof(DWRITE_GLYPH_OFFSET));

            FeatureNativeBuffer features = AllocateFeatures(fontFeatures, fontFeatureRanges);

            try
            {
                if (glyphProperties != null)
                {
                    for (uint i = 0; i < glyphCount; i++)
                        pGlyphProps16[i] = unchecked((ushort)glyphProperties[i]);
                }
                else
                {
                    new Span<ushort>(pGlyphProps16, (int)glyphCount).Clear();
                }

                DWRITE_SCRIPT_ANALYSIS sa = textItem.ScriptAnalysis;
                string locale = cultureInfo?.Name ?? string.Empty;
                float emSizeFloat = checked((float)fontEmSize);
                int hr;

                fixed (char* pLocale = locale)
                {
                    if (textFormattingMode == TextFormattingMode.Ideal)
                    {
                        hr = IDWriteTextAnalyzerVtbl.GetGlyphPlacements(
                            pAnalyzer,
                            textString,
                            clusterMap,
                            charProperties,
                            textLength,
                            glyphIndices,
                            pGlyphProps16,
                            glyphCount,
                            fontFace.DWriteFontFaceNoAddRef,
                            emSizeFloat,
                            isSideways,
                            isRightToLeft,
                            &sa,
                            pLocale,
                            features.RangePointers,
                            features.RangeLengths,
                            features.RangeCount,
                            pAdvances,
                            pOffsets);
                    }
                    else
                    {
                        hr = IDWriteTextAnalyzerVtbl.GetGdiCompatibleGlyphPlacements(
                            pAnalyzer,
                            textString,
                            clusterMap,
                            charProperties,
                            textLength,
                            glyphIndices,
                            pGlyphProps16,
                            glyphCount,
                            fontFace.DWriteFontFaceNoAddRef,
                            emSizeFloat,
                            pixelsPerDip,
                            null,
                            textFormattingMode != TextFormattingMode.Display,
                            isSideways,
                            isRightToLeft,
                            &sa,
                            pLocale,
                            features.RangePointers,
                            features.RangeLengths,
                            features.RangeCount,
                            pAdvances,
                            pOffsets);
                    }
                }

                DWriteUtil.ConvertHresultToException(hr);

                glyphOffsets = new GlyphOffset[glyphCount];
                for (uint i = 0; i < glyphCount; i++)
                {
                    if (glyphAdvances != null)
                        glyphAdvances[i] = (int)Math.Round(pAdvances[i] * scalingFactor);

                    glyphOffsets[i] = new GlyphOffset
                    {
                        du = (int)Math.Round(pOffsets[i].advanceOffset * scalingFactor),
                        dv = (int)Math.Round(pOffsets[i].ascenderOffset * scalingFactor),
                    };
                }
            }
            finally
            {
                features.Free();
                if (!useStack)
                {
                    NativeMemory.Free(pGlyphProps16);
                    NativeMemory.Free(pAdvances);
                    NativeMemory.Free(pOffsets);
                }
            }
        }

        internal void GetGlyphsAndTheirPlacements(
            char* textString,
            uint textLength,
            Font font,
            ushort blankGlyphIndex,
            bool isSideways,
            bool isRightToLeft,
            CultureInfo cultureInfo,
            DWriteFontFeature[][]? fontFeatures,
            uint[]? fontFeatureRanges,
            double fontEmSize,
            double scalingFactor,
            float pixelsPerDip,
            TextFormattingMode textFormattingMode,
            ItemProps textItem,
            out ushort[] clusterMap,
            out ushort[] glyphIndices,
            out int[] glyphAdvances,
            out GlyphOffset[] glyphOffsets)
        {
            if (textLength == 0 || textString == null)
            {
                clusterMap = Array.Empty<ushort>();
                glyphIndices = Array.Empty<ushort>();
                glyphAdvances = Array.Empty<int>();
                glyphOffsets = Array.Empty<GlyphOffset>();
                return;
            }

            clusterMap = new ushort[textLength];
            ushort[] charProps = new ushort[textLength];
            uint maxGlyphCount = 3 * textLength / 2 + 16;

            ushort[] tempGlyphIndices;
            uint[] tempGlyphProps;
            uint actualGlyphCount;

            while (true)
            {
                tempGlyphIndices = new ushort[maxGlyphCount];
                tempGlyphProps = new uint[maxGlyphCount];

                int hr;
                fixed (ushort* pClusterMap = clusterMap)
                fixed (ushort* pCharProps = charProps)
                fixed (ushort* pGlyphIndices = tempGlyphIndices)
                fixed (uint* pGlyphProps = tempGlyphProps)
                {
                    hr = TryGetGlyphs(
                        textString,
                        textLength,
                        font,
                        blankGlyphIndex,
                        isSideways,
                        isRightToLeft,
                        cultureInfo,
                        fontFeatures,
                        fontFeatureRanges,
                        maxGlyphCount,
                        textFormattingMode,
                        textItem,
                        pClusterMap,
                        pCharProps,
                        pGlyphIndices,
                        pGlyphProps,
                        null,
                        out actualGlyphCount);
                }

                if (hr == DxWriteFontConst.E_NOT_SUFFICIENT_BUFFER)
                {
                    maxGlyphCount = checked(maxGlyphCount * 2);
                    continue;
                }

                DWriteUtil.ConvertHresultToException(hr);
                break;
            }

            glyphIndices = new ushort[actualGlyphCount];
            Array.Copy(tempGlyphIndices, glyphIndices, (int)actualGlyphCount);
            glyphAdvances = new int[actualGlyphCount];

            fixed (ushort* pClusterMap = clusterMap)
            fixed (ushort* pCharProps = charProps)
            fixed (ushort* pGlyphIndices = glyphIndices)
            fixed (uint* pGlyphProps = tempGlyphProps)
            fixed (int* pAdvances = glyphAdvances)
            {
                GetGlyphPlacements(
                    textString,
                    pClusterMap,
                    pCharProps,
                    textLength,
                    pGlyphIndices,
                    pGlyphProps,
                    actualGlyphCount,
                    font,
                    fontEmSize,
                    scalingFactor,
                    isSideways,
                    isRightToLeft,
                    cultureInfo,
                    fontFeatures,
                    fontFeatureRanges,
                    textFormattingMode,
                    textItem,
                    pixelsPerDip,
                    pAdvances,
                    out glyphOffsets);
            }
        }

        private void* CreateNumberSubstitutionIfNeeded(ItemProps textItem)
        {
            if (textItem.DigitCulture == null ||
                textItem.NumberSubstitutionMethod == DxWriteConst.DWRITE_NUMBER_SUBSTITUTION_METHOD_NONE ||
                _dwriteFactory == IntPtr.Zero)
            {
                return null;
            }

            void* pNumSub = null;
            int hr = IDWriteFactoryWriteVtbl.CreateNumberSubstitution(
                (void*)_dwriteFactory,
                textItem.NumberSubstitutionMethod,
                textItem.DigitCulture.Name,
                textItem.IgnoreUserOverride,
                &pNumSub);
            DWriteUtil.ConvertHresultToException(hr);
            return pNumSub;
        }

        private static FeatureNativeBuffer AllocateFeatures(DWriteFontFeature[][]? fontFeatures, uint[]? fontFeatureRanges)
        {
            if (fontFeatures == null || fontFeatureRanges == null || fontFeatures.Length == 0 || fontFeatures.Length != fontFeatureRanges.Length)
                return default;

            uint rangeCount = (uint)fontFeatures.Length;
            uint totalFeatureCount = 0;
            for (int r = 0; r < fontFeatures.Length; r++)
            {
                if (fontFeatures[r] != null)
                    totalFeatureCount += (uint)fontFeatures[r].Length;
            }

            DWRITE_FONT_FEATURE* flatFeatures = totalFeatureCount > 0
                ? (DWRITE_FONT_FEATURE*)NativeMemory.Alloc(totalFeatureCount, (nuint)sizeof(DWRITE_FONT_FEATURE))
                : null;
            DWRITE_TYPOGRAPHIC_FEATURES* groups = (DWRITE_TYPOGRAPHIC_FEATURES*)NativeMemory.Alloc(rangeCount, (nuint)sizeof(DWRITE_TYPOGRAPHIC_FEATURES));
            DWRITE_TYPOGRAPHIC_FEATURES** groupPtrs = (DWRITE_TYPOGRAPHIC_FEATURES**)NativeMemory.Alloc(rangeCount, (nuint)sizeof(void*));
            uint* rangeLengths = (uint*)NativeMemory.Alloc(rangeCount, sizeof(uint));

            uint offset = 0;
            for (int r = 0; r < fontFeatures.Length; r++)
            {
                rangeLengths[r] = fontFeatureRanges[r];
                DWriteFontFeature[]? row = fontFeatures[r];
                if (row != null && row.Length > 0 && flatFeatures != null)
                {
                    groups[r].features = flatFeatures + offset;
                    groups[r].featureCount = (uint)row.Length;
                    for (int f = 0; f < row.Length; f++)
                    {
                        flatFeatures[offset++] = new DWRITE_FONT_FEATURE
                        {
                            nameTag = (uint)row[f].NameTag,
                            parameter = row[f].Parameter,
                        };
                    }
                    groupPtrs[r] = &groups[r];
                }
                else
                {
                    groups[r].features = null;
                    groups[r].featureCount = 0;
                    groupPtrs[r] = &groups[r];
                }
            }

            return new FeatureNativeBuffer(flatFeatures, groups, groupPtrs, rangeLengths, rangeCount);
        }

        private readonly struct FeatureNativeBuffer
        {
            private readonly DWRITE_FONT_FEATURE* _flatFeatures;
            private readonly DWRITE_TYPOGRAPHIC_FEATURES* _groups;
            public readonly DWRITE_TYPOGRAPHIC_FEATURES** RangePointers;
            public readonly uint* RangeLengths;
            public readonly uint RangeCount;

            public FeatureNativeBuffer(
                DWRITE_FONT_FEATURE* flatFeatures,
                DWRITE_TYPOGRAPHIC_FEATURES* groups,
                DWRITE_TYPOGRAPHIC_FEATURES** rangePointers,
                uint* rangeLengths,
                uint rangeCount)
            {
                _flatFeatures = flatFeatures;
                _groups = groups;
                RangePointers = rangePointers;
                RangeLengths = rangeLengths;
                RangeCount = rangeCount;
            }

            public void Free()
            {
                if (_flatFeatures != null)
                    NativeMemory.Free(_flatFeatures);
                if (_groups != null)
                    NativeMemory.Free(_groups);
                if (RangePointers != null)
                    NativeMemory.Free(RangePointers);
                if (RangeLengths != null)
                    NativeMemory.Free(RangeLengths);
            }
        }

        private sealed class AnalysisSource : IDWriteTextAnalysisSourceManaged
        {
            private readonly char* _text;
            private readonly uint _length;
            private readonly bool _isRtl;
            private readonly char* _localeName;
            private readonly void* _numberSubstitution;

            public AnalysisSource(char* text, uint length, bool isRtl, char* localeName, void* numberSubstitution)
            {
                _text = text;
                _length = length;
                _isRtl = isRtl;
                _localeName = localeName;
                _numberSubstitution = numberSubstitution;
            }

            public int GetTextAtPosition(uint textPosition, char** textString, uint* textLength)
            {
                if (textPosition >= _length)
                {
                    *textString = null;
                    *textLength = 0;
                }
                else
                {
                    *textString = _text + textPosition;
                    *textLength = _length - textPosition;
                }
                return S_OK;
            }

            public int GetTextBeforePosition(uint textPosition, char** textString, uint* textLength)
            {
                if (textPosition == 0 || textPosition > _length)
                {
                    *textString = null;
                    *textLength = 0;
                }
                else
                {
                    *textString = _text;
                    *textLength = textPosition;
                }
                return S_OK;
            }

            public uint GetParagraphReadingDirection()
                => _isRtl ? DxWriteFontConst.DWRITE_READING_DIRECTION_RIGHT_TO_LEFT : DxWriteFontConst.DWRITE_READING_DIRECTION_LEFT_TO_RIGHT;

            public int GetLocaleName(uint textPosition, uint* textLength, char** localeName)
            {
                *textLength = textPosition >= _length ? 0u : _length - textPosition;
                *localeName = _localeName;
                return S_OK;
            }

            public int GetNumberSubstitution(uint textPosition, uint* textLength, void** numberSubstitution)
            {
                *textLength = textPosition >= _length ? 0u : _length - textPosition;
                if (_numberSubstitution != null)
                    ComHelpers.AddRef(_numberSubstitution);
                *numberSubstitution = _numberSubstitution;
                return S_OK;
            }
        }

        private sealed class AnalysisSink : IDWriteTextAnalysisSinkManaged
        {
            private readonly DWRITE_SCRIPT_ANALYSIS[] _scripts;
            private readonly bool[] _hasSubstitution;
            private readonly DWRITE_LINE_BREAKPOINT[]? _lineBreakpoints;

            public AnalysisSink(
                DWRITE_SCRIPT_ANALYSIS[] scripts,
                bool[] hasSubstitution,
                DWRITE_LINE_BREAKPOINT[]? lineBreakpoints = null)
            {
                _scripts = scripts;
                _hasSubstitution = hasSubstitution;
                _lineBreakpoints = lineBreakpoints;
            }

            public int SetScriptAnalysis(uint textPosition, uint textLength, DWRITE_SCRIPT_ANALYSIS* scriptAnalysis)
            {
                if (scriptAnalysis != null)
                {
                    DWRITE_SCRIPT_ANALYSIS value = *scriptAnalysis;
                    uint end = Math.Min((uint)_scripts.Length, textPosition + textLength);
                    for (uint i = textPosition; i < end; i++)
                        _scripts[i] = value;
                }
                return S_OK;
            }

            public int SetLineBreakpoints(uint textPosition, uint textLength, DWRITE_LINE_BREAKPOINT* lineBreakpoints)
            {
                if (_lineBreakpoints != null && lineBreakpoints != null)
                {
                    uint end = Math.Min((uint)_lineBreakpoints.Length, textPosition + textLength);
                    for (uint i = textPosition; i < end; i++)
                        _lineBreakpoints[i] = lineBreakpoints[i - textPosition];
                }
                return S_OK;
            }

            public int SetBidiLevel(uint textPosition, uint textLength, byte explicitLevel, byte resolvedLevel)
                => S_OK;

            public int SetNumberSubstitution(uint textPosition, uint textLength, void* numberSubstitution)
            {
                bool hasSub = numberSubstitution != null;
                uint end = Math.Min((uint)_hasSubstitution.Length, textPosition + textLength);
                for (uint i = textPosition; i < end; i++)
                    _hasSubstitution[i] = hasSub;
                return S_OK;
            }
        }
    }
}

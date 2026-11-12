// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.TextFormatting;
using CSUI.Core.Interop.DirectX;
using MS.Internal.Text.TextInterface;

namespace MS.Internal.TextFormatting
{
    /// <summary>
    /// Pure managed + DirectWrite replacement for PresentationNative's native LineServices (Lo*).
    /// Drives WPF's own <see cref="LineServicesCallbacks"/> directly without P/Invoke or C++ heaps.
    /// </summary>
    internal static unsafe class ManagedLineServices
    {
        private static long _nextHandle;
        private static readonly ConcurrentDictionary<IntPtr, object> s_handles = new();

        private static IntPtr AllocHandle(object instance)
        {
            long id = Interlocked.Increment(ref _nextHandle);
            if (id <= 0)
            {
                id = Interlocked.Increment(ref _nextHandle);
            }
            IntPtr h = (IntPtr)id;
            s_handles[h] = instance;
            return h;
        }

        private static T TryGet<T>(IntPtr handle) where T : class
            => handle != IntPtr.Zero && s_handles.TryGetValue(handle, out object obj) ? obj as T : null;

        private static void FreeHandle(IntPtr handle)
        {
            if (handle != IntPtr.Zero)
            {
                s_handles.TryRemove(handle, out _);
            }
        }

        #region Managed State Objects

        internal sealed class ManagedLoContext
        {
            public LineServicesCallbacks Callbacks;
            public BreakStrategies BreakingStrategy = BreakStrategies.BreakCJK;
            public bool IsDisplay;
            public bool IsReferencePresentationEqual;
            public LsDevRes DeviceInfo;
            public int IncrementalTab = 1200; // ~40 ideal px fallback
            public LsTbd[] TabStops = Array.Empty<LsTbd>();
            public List<ManagedPenaltyModule> PenaltyModules = new();
        }

        internal sealed class ManagedBreakRecord
        {
            public int CpNext;
            public int OpenReverseCount;

            public ManagedBreakRecord Clone() => new()
            {
                CpNext = CpNext,
                OpenReverseCount = OpenReverseCount,
            };
        }

        internal sealed class ManagedSegment
        {
            public Plsrun Plsrun;
            public ushort IdObj;
            public int LscpFirst;
            public int Cch;
            public bool IsHidden;
            public bool IsControl;
            public bool IsNewline;
            public bool IsTab;
            public char TabLeader;
            public int UStart;
            public int Dur;
            public int TrailingTrimmedDur;
            public int TrailingWhitespaceCount;
            public int Ascent;
            public int Descent;
            public int MultiLineHeight;
            public byte BidiLevel;
            public LsTFlow TextFlow;
            public LsChp.Flags ChpFlags;

            // Text content (for non-inline runs)
            public char[] Chars;
            public int[] CharWidths;

            // Shaped glyph payload (when fGlyphBased is set)
            public bool IsGlyphBased;
            public ushort[] ClusterMap;
            public ushort[] CharProperties;
            public ushort[] GlyphIndices;
            public uint[] GlyphProperties;
            public int[] GlyphAdvances;
            public int[] OriginalGlyphAdvances;
            public GlyphOffset[] GlyphOffsets;
            public int GlyphCount;

            // Inline object dimensions
            public ObjDim ObjectDim;

            public int TrailingWidth => Dur - TrailingTrimmedDur;
        }

        internal sealed class ManagedLoLine
        {
            public ManagedLoContext Context;
            public int CpFirst;
            public int LscpLim;
            public int MaxWidth;
            public int BaselineOffset;
            public int Ascent;
            public int Descent;
            public bool IsRightToLeft;
            public LsTFlow BaseFlow;
            public LsEndRes EndRes;
            public int CchEol;
            public int DcpDepend;
            public int MaxBidiDepth = 1;
            public bool ForcedBreak;
            public LsLineWidths LineWidths;
            public ManagedBreakRecord NextBreakRecord;
            public ManagedSegment MarkerSegment;
            public List<ManagedSegment> LogicalSegments = new();
            public List<ManagedSegment> VisualSegments = new();
            public ManagedPenaltyModule PenaltyResource;
        }

        internal sealed class ManagedParaBreakingSession
        {
            public ManagedLoContext Context;
            public FullTextState FullText;
            public int CpFirst;
            public int MaxWidth;
        }

        internal sealed class ManagedPenaltyModule
        {
            public ManagedLoContext Context;
            public double Penalty;
        }

        private sealed class FetchedChunk
        {
            public Plsrun Plsrun;
            public int LscpStart;
            public int Cch;
            public bool IsHidden;
            public LsChp Chp;
            public char[] Chars;
            public LSRun Run;
            public int DeltaDepth;
            public byte EffectiveBidiLevel;
        }

        #endregion

        #region Public Lo* Entry Points

        internal static LsErr LoCreateContext(
            ref LsContextInfo contextInfo,
            ref LscbkRedefined lscbkRedef,
            LineServicesCallbacks callbacks,
            out IntPtr ploc)
        {
            _ = contextInfo;
            _ = lscbkRedef;
            var ctx = new ManagedLoContext
            {
                Callbacks = callbacks,
            };
            ploc = AllocHandle(ctx);
            return LsErr.None;
        }

        internal static LsErr LoDestroyContext(IntPtr ploc)
        {
            FreeHandle(ploc);
            return LsErr.None;
        }

        internal static LsErr LoSetBreaking(IntPtr ploc, int breaking)
        {
            var ctx = TryGet<ManagedLoContext>(ploc);
            if (ctx == null)
                return LsErr.InvalidParameter;
            ctx.BreakingStrategy = (BreakStrategies)breaking;
            return LsErr.None;
        }

        internal static LsErr LoSetDoc(
            IntPtr ploc,
            int isDisplay,
            int isReferencePresentationEqual,
            ref LsDevRes deviceInfo)
        {
            var ctx = TryGet<ManagedLoContext>(ploc);
            if (ctx == null)
                return LsErr.InvalidParameter;
            ctx.IsDisplay = isDisplay != 0;
            ctx.IsReferencePresentationEqual = isReferencePresentationEqual != 0;
            ctx.DeviceInfo = deviceInfo;
            return LsErr.None;
        }

        internal static LsErr LoSetTabs(
            IntPtr ploc,
            int incrementalTab,
            int tabStopCount,
            LsTbd* tabStops)
        {
            var ctx = TryGet<ManagedLoContext>(ploc);
            if (ctx == null)
                return LsErr.InvalidParameter;
            ctx.IncrementalTab = incrementalTab > 0 ? incrementalTab : 1200;
            if (tabStopCount > 0 && tabStops != null)
            {
                var arr = new LsTbd[tabStopCount];
                for (int i = 0; i < tabStopCount; i++)
                {
                    arr[i] = tabStops[i];
                }
                ctx.TabStops = arr;
            }
            else
            {
                ctx.TabStops = Array.Empty<LsTbd>();
            }
            return LsErr.None;
        }

        internal static LsErr LoCreateLine(
            IntPtr ploc,
            int cpFirst,
            int lineLength,
            int maxWidth,
            uint lineFlags,
            IntPtr previousLineBreakRecord,
            out LsLInfo plslineInfo,
            out IntPtr ploline,
            out int maxDepth,
            out LsLineWidths lineWidths)
        {
            plslineInfo = new LsLInfo();
            lineWidths = new LsLineWidths();
            ploline = IntPtr.Zero;
            maxDepth = 1;

            var ctx = TryGet<ManagedLoContext>(ploc);
            if (ctx == null || ctx.Callbacks == null)
                return LsErr.InvalidParameter;

            FullTextState fullText = ctx.Callbacks.Owner as FullTextState;
            if (fullText == null)
                return LsErr.InvalidParameter;

            TextStore mainStore = fullText.TextStore;
            LineFlags flags = (LineFlags)lineFlags;
            bool isMinMax = (flags & LineFlags.MinMax) != 0;
            bool breakAlways = (flags & LineFlags.BreakAlways) != 0;

            var prevBreak = TryGet<ManagedBreakRecord>(previousLineBreakRecord);

            // 1. Fetch paragraph and line properties
            LsPap lspap = new LsPap();
            LsErr err = ctx.Callbacks.FetchPap(ploc, cpFirst, ref lspap);
            if (err != LsErr.None)
                return err;

            bool isFirstLineInPara = (mainStore.Pap.FirstLineInParagraph);
            LsLineProps lsLineProps = new LsLineProps();
            err = ctx.Callbacks.FetchLineProps(ploc, cpFirst, isFirstLineInPara ? 1 : 0, ref lsLineProps);
            if (err != LsErr.None)
                return err;

            bool baseRtl = (lspap.lstflow == LsTFlow.lstflowWS);
            LsTFlow baseFlow = baseRtl ? LsTFlow.lstflowWS : LsTFlow.lstflowES;
            int durLeft = lsLineProps.durLeft;
            int formatRightMargin = maxWidth > 0 ? maxWidth - lsLineProps.durRightBreak : int.MaxValue;
            if (isMinMax || (lspap.grpf & LsPap.Flags.fFmiApplyBreakingRules) == 0)
            {
                formatRightMargin = Constants.IdealInfiniteWidth;
            }

            // 2. Fetch bullet / marker if requested
            ManagedSegment markerSeg = null;
            int markerStart = durLeft;
            int markerEnd = durLeft;
            if ((lspap.grpf & LsPap.Flags.fFmiAnm) != 0 && fullText.TextMarkerStore != null)
            {
                markerSeg = FetchMarkerSegment(ploc, ctx, lspap, durLeft, out markerStart, out markerEnd, out err);
                if (err != LsErr.None)
                    return err;
            }

            // 3. Fetch chunks starting at cpFirst
            List<FetchedChunk> rawChunks = new();
            int curLscp = cpFirst;
            int depth = 1 + (prevBreak?.OpenReverseCount ?? 0);
            maxDepth = Math.Max(1, depth);

            // Track rough width while fetching so infinite paragraphs still terminate early if wrapping applies
            int roughAccumulatedU = durLeft;
            bool hitEndOfLine = false;
            LsEndRes endRes = LsEndRes.endrNormal;
            int eolCch = 0;

            // Stack to buffer text before calling callbacks/shaper
            const int InitialFetchBuf = 512;
            char* scratchBuf = (char*)NativeMemory.Alloc(InitialFetchBuf, sizeof(char));
            int scratchCap = InitialFetchBuf;
            int* tmpWStack = stackalloc int[256];

            try
            {
                int lookaheadAllowance = maxWidth > 0 && formatRightMargin < Constants.IdealInfiniteWidth
                    ? Math.Max(12000, mainStore.Pap.EmSize * 12)
                    : int.MaxValue;
                int hardFetchLimit = (formatRightMargin < Constants.IdealInfiniteWidth && lineLength <= 0 && !isMinMax)
                    ? (int)Math.Min((long)Constants.IdealInfiniteWidth, (long)formatRightMargin + lookaheadAllowance)
                    : Constants.IdealInfiniteWidth;

                while (true)
                {
                    if (lineLength > 0 && curLscp - cpFirst >= lineLength)
                    {
                        break;
                    }

                    int isBufferUsed = 0;
                    char* pwchText = null;
                    int cchText = 0;
                    int fIsHidden = 0;
                    LsChp lschp = new LsChp();
                    IntPtr lsplsrun = IntPtr.Zero;

                    err = ctx.Callbacks.FetchRunRedefined(
                        ploc,
                        curLscp,
                        0,
                        IntPtr.Zero,
                        scratchBuf,
                        scratchCap,
                        ref isBufferUsed,
                        out pwchText,
                        ref cchText,
                        ref fIsHidden,
                        ref lschp,
                        ref lsplsrun);

                    if (err != LsErr.None)
                        return err;

                    if (pwchText == null && isBufferUsed == 0 && cchText > scratchCap)
                    {
                        NativeMemory.Free(scratchBuf);
                        scratchCap = cchText;
                        scratchBuf = (char*)NativeMemory.Alloc((nuint)scratchCap, sizeof(char));
                        err = ctx.Callbacks.FetchRunRedefined(
                            ploc,
                            curLscp,
                            0,
                            IntPtr.Zero,
                            scratchBuf,
                            scratchCap,
                            ref isBufferUsed,
                            out pwchText,
                            ref cchText,
                            ref fIsHidden,
                            ref lschp,
                            ref lsplsrun);
                        if (err != LsErr.None)
                            return err;
                    }

                    if (cchText <= 0)
                        break;

                    // Clamp if explicit lineLength was requested (RecreateLine)
                    if (lineLength > 0 && (curLscp + cchText) - cpFirst > lineLength)
                    {
                        cchText = (cpFirst + lineLength) - curLscp;
                        if (cchText <= 0)
                            break;
                    }

                    Plsrun plsrun = (Plsrun)lsplsrun;
                    LSRun run = TextStore.IsContent(plsrun) ? fullText.StoreFrom(plsrun).GetRun(plsrun) : null;

                    var chars = new char[cchText];
                    char* srcChars = pwchText != null ? pwchText : (isBufferUsed != 0 ? scratchBuf : null);
                    if (srcChars != null)
                    {
                        new ReadOnlySpan<char>(srcChars, cchText).CopyTo(chars);
                    }

                    int deltaDepth = 0;
                    if (plsrun == Plsrun.Reverse)
                    {
                        depth++;
                        deltaDepth = 1;
                        maxDepth = Math.Max(maxDepth, depth);
                    }
                    else if (plsrun == Plsrun.CloseAnchor)
                    {
                        depth = Math.Max(1, depth - 1);
                        deltaDepth = -1;
                    }

                    byte effectiveLevel = run != null
                        ? run.BidiLevel
                        : (byte)(baseRtl ? 1 : 0);

                    var chunk = new FetchedChunk
                    {
                        Plsrun = plsrun,
                        LscpStart = curLscp,
                        Cch = cchText,
                        IsHidden = fIsHidden != 0,
                        Chp = lschp,
                        Chars = chars,
                        Run = run,
                        DeltaDepth = deltaDepth,
                        EffectiveBidiLevel = effectiveLevel,
                    };
                    rawChunks.Add(chunk);

                    curLscp += cchText;

                    if (plsrun == Plsrun.ParaBreak)
                    {
                        hitEndOfLine = true;
                        endRes = LsEndRes.endrEndPara;
                        eolCch = cchText;
                        break;
                    }
                    if (plsrun == Plsrun.LineBreak)
                    {
                        hitEndOfLine = true;
                        endRes = LsEndRes.endrSoftCR;
                        eolCch = cchText;
                        break;
                    }
                    if (plsrun == Plsrun.FakeLineBreak)
                    {
                        hitEndOfLine = true;
                        endRes = LsEndRes.endrNormal;
                        eolCch = cchText;
                        break;
                    }

                    // Estimate width to avoid over-fetching multi-megabyte documents on wrapped lines
                    if (hardFetchLimit < Constants.IdealInfiniteWidth && fIsHidden == 0 && TextStore.IsContent(plsrun))
                    {
                        if (run != null && run.Type == Plsrun.InlineObject)
                        {
                            roughAccumulatedU += TextFormatterImp.RealToIdeal(mainStore.Pap.EmSize);
                        }
                        else if (run != null && run.Shapeable != null)
                        {
                            int* tmpW = cchText <= 256 ? tmpWStack : (int*)NativeMemory.Alloc((nuint)cchText, sizeof(int));
                            try
                            {
                                fixed (char* pCh = chars)
                                {
                                    run.Shapeable.GetAdvanceWidthsUnshaped(pCh, cchText, TextFormatterImp.ToIdeal, tmpW);
                                    for (int k = 0; k < cchText; k++)
                                        roughAccumulatedU += tmpW[k];
                                }
                            }
                            finally
                            {
                                if (cchText > 256)
                                    NativeMemory.Free(tmpW);
                            }
                        }

                        if (roughAccumulatedU > hardFetchLimit)
                            break;
                    }

                    if (curLscp - cpFirst > TextStore.MaxCharactersPerLine + 256)
                        break;
                }
            }
            finally
            {
                NativeMemory.Free(scratchBuf);
            }

            // 4. Measure/shape fetched chunks into ManagedSegments (merging adjacent shapable runs when allowed)
            List<ManagedSegment> segments = BuildSegments(ploc, ctx, baseFlow, rawChunks, out err);
            if (err != LsErr.None)
                return err;

            // 5. Perform line breaking if finite format width applies and we did not already finish with explicit lineLength
            bool forcedBreak = false;
            int minUnbreakableWidth = 0;
            bool needMinWidth = isMinMax;
            bool needBreak = lineLength <= 0
                && !isMinMax
                && formatRightMargin < Constants.IdealInfiniteWidth
                && (lspap.grpf & LsPap.Flags.fFmiApplyBreakingRules) != 0;

            int lscpEndOfLine = curLscp;
            if (needBreak || needMinWidth)
            {
                ComputeBreakAndMinWidth(
                    ploc,
                    ctx,
                    fullText,
                    mainStore,
                    segments,
                    durLeft,
                    formatRightMargin,
                    needBreak,
                    breakAlways || (lspap.grpf & LsPap.Flags.fFmiForceBreakAsNext) == 0 ? breakAlways : true,
                    (lspap.grpf & LsPap.Flags.fFmiForceBreakAsNext) != 0 || breakAlways,
                    (lspap.grpf & LsPap.Flags.fFmiAllowHyphenation) != 0,
                    baseRtl,
                    ref endRes,
                    ref eolCch,
                    ref lscpEndOfLine,
                    out forcedBreak,
                    out minUnbreakableWidth);
            }

            // 6. Construct ManagedLoLine, resolve tabs, justification, Bidi visual order, metrics
            var line = FinalizeLineMetrics(
                ctx,
                lspap,
                isFirstLineInPara,
                lsLineProps,
                baseFlow,
                cpFirst,
                lscpEndOfLine,
                maxWidth,
                durLeft,
                markerSeg,
                markerStart,
                markerEnd,
                segments,
                endRes,
                hitEndOfLine ? eolCch : (endRes == LsEndRes.endrEndPara || endRes == LsEndRes.endrSoftCR ? eolCch : 0),
                forcedBreak,
                minUnbreakableWidth,
                maxDepth,
                prevBreak,
                out plslineInfo,
                out lineWidths);

            ploline = AllocHandle(line);
            return LsErr.None;
        }

        internal static LsErr LoDisposeLine(IntPtr ploline, bool finalizing)
        {
            _ = finalizing;
            FreeHandle(ploline);
            return LsErr.None;
        }

        internal static LsErr LoDisplayLine(
            IntPtr ploline,
            ref LSPOINT ptStart,
            uint displayMode,
            ref LSRECT clipRect)
        {
            var line = TryGet<ManagedLoLine>(ploline);
            if (line == null || line.Context == null || line.Context.Callbacks == null)
                return LsErr.InvalidParameter;

            var cb = line.Context.Callbacks;
            IntPtr ploc = IntPtr.Zero;

            // Draw marker first if present
            if (line.MarkerSegment != null)
            {
                LsErr me = DrawSegment(ploc, cb, line, line.MarkerSegment, ref ptStart, displayMode, ref clipRect);
                if (me != LsErr.None)
                    return me;
            }

            for (int i = 0; i < line.VisualSegments.Count; i++)
            {
                ManagedSegment seg = line.VisualSegments[i];
                if (seg.IsHidden || seg.IsControl || seg.IsNewline || seg.Cch <= 0)
                    continue;

                LsErr err = DrawSegment(ploc, cb, line, seg, ref ptStart, displayMode, ref clipRect);
                if (err != LsErr.None)
                    return err;
            }

            return LsErr.None;
        }

        internal static LsErr LoEnumLine(
            IntPtr ploline,
            bool reverseOrder,
            bool geometryNeeded,
            ref LSPOINT ptStart)
        {
            var line = TryGet<ManagedLoLine>(ploline);
            if (line == null || line.Context == null || line.Context.Callbacks == null)
                return LsErr.InvalidParameter;

            var cb = line.Context.Callbacks;
            _ = geometryNeeded;
            int revFlag = reverseOrder ? 1 : 0;

            int count = line.VisualSegments.Count;
            for (int idx = 0; idx < count; idx++)
            {
                int i = reverseOrder ? (count - 1 - idx) : idx;
                ManagedSegment seg = line.VisualSegments[i];
                if (seg.IsHidden || seg.IsControl || seg.IsNewline || seg.Cch <= 0)
                    continue;

                LsHeights h = new LsHeights
                {
                    dvAscent = seg.Ascent,
                    dvDescent = seg.Descent,
                    dvMultiLineHeight = seg.MultiLineHeight,
                };

                LSPOINT ptRun = new LSPOINT(
                    line.IsRightToLeft ? -seg.UStart : seg.UStart,
                    ptStart.y);

                if (seg.IsTab)
                {
                    char tabCh = '\t';
                    char leader = seg.TabLeader;
                    LsErr err = cb.EnumTab(
                        IntPtr.Zero,
                        seg.Plsrun,
                        seg.LscpFirst,
                        &tabCh,
                        leader,
                        seg.TextFlow,
                        revFlag,
                        1,
                        ref ptRun,
                        ref h,
                        seg.Dur);
                    if (err != LsErr.None)
                        return err;
                }
                else if (seg.IdObj == (ushort)TextStore.ObjectId.Text_chp && seg.Chars != null)
                {
                    fixed (char* pwch = seg.Chars)
                    fixed (ushort* pCluster = seg.ClusterMap)
                    fixed (ushort* pCharProps = seg.CharProperties)
                    fixed (ushort* pGlyphs = seg.GlyphIndices)
                    fixed (int* pAdv = seg.IsGlyphBased ? seg.GlyphAdvances : seg.CharWidths)
                    fixed (GlyphOffset* pOff = seg.GlyphOffsets)
                    fixed (uint* pGProp = seg.GlyphProperties)
                    {
                        LsErr err = cb.EnumText(
                            IntPtr.Zero,
                            seg.Plsrun,
                            seg.LscpFirst,
                            seg.Cch,
                            pwch,
                            seg.Cch,
                            seg.TextFlow,
                            revFlag,
                            1,
                            ref ptRun,
                            ref h,
                            seg.Dur,
                            seg.IsGlyphBased ? 1 : 0,
                            seg.IsGlyphBased ? null : pAdv,
                            pCluster,
                            pCharProps,
                            pGlyphs,
                            seg.IsGlyphBased ? pAdv : null,
                            pOff,
                            pGProp,
                            seg.GlyphCount);
                        if (err != LsErr.None)
                            return err;
                    }
                }
            }

            return LsErr.None;
        }

        internal static LsErr LoQueryLineCpPpoint(
            IntPtr ploline,
            int lscpQuery,
            int depthQueryMax,
            IntPtr pSubLineInfo,
            out int actualDepthQuery,
            out LsTextCell lsTextCell)
        {
            actualDepthQuery = 0;
            lsTextCell = new LsTextCell();

            var line = TryGet<ManagedLoLine>(ploline);
            if (line == null || depthQueryMax <= 0 || pSubLineInfo == IntPtr.Zero)
                return LsErr.InvalidParameter;

            ManagedSegment target = FindSegmentForCp(line, lscpQuery);
            if (target == null)
                return LsErr.None;

            FillQueryForSegment(line, target, lscpQuery, depthQueryMax, (LsQSubInfo*)pSubLineInfo, out actualDepthQuery, out lsTextCell);
            return LsErr.None;
        }

        internal static LsErr LoQueryLinePointPcp(
            IntPtr ploline,
            ref LSPOINT ptQuery,
            int depthQueryMax,
            IntPtr pSubLineInfo,
            out int actualDepthQuery,
            out LsTextCell lsTextCell)
        {
            actualDepthQuery = 0;
            lsTextCell = new LsTextCell();

            var line = TryGet<ManagedLoLine>(ploline);
            if (line == null || depthQueryMax <= 0 || pSubLineInfo == IntPtr.Zero)
                return LsErr.InvalidParameter;

            int uQuery = ptQuery.x;
            ManagedSegment target = FindSegmentForU(line, uQuery);
            if (target == null)
                return LsErr.None;

            int targetCp = FindCpInSegmentAtU(target, uQuery);
            FillQueryForSegment(line, target, targetCp, depthQueryMax, (LsQSubInfo*)pSubLineInfo, out actualDepthQuery, out lsTextCell);
            return LsErr.None;
        }

        internal static LsErr LoAcquireBreakRecord(IntPtr ploline, out IntPtr pbreakrec)
        {
            pbreakrec = IntPtr.Zero;
            var line = TryGet<ManagedLoLine>(ploline);
            if (line == null)
                return LsErr.InvalidParameter;

            if (line.NextBreakRecord != null && line.NextBreakRecord.OpenReverseCount > 0)
            {
                pbreakrec = AllocHandle(line.NextBreakRecord.Clone());
            }
            return LsErr.None;
        }

        internal static LsErr LoCloneBreakRecord(IntPtr pbreakrec, out IntPtr pbreakrecClone)
        {
            pbreakrecClone = IntPtr.Zero;
            if (pbreakrec == IntPtr.Zero)
                return LsErr.None;

            var rec = TryGet<ManagedBreakRecord>(pbreakrec);
            if (rec == null)
                return LsErr.InvalidParameter;

            pbreakrecClone = AllocHandle(rec.Clone());
            return LsErr.None;
        }

        internal static LsErr LoDisposeBreakRecord(IntPtr pbreakrec, bool finalizing)
        {
            _ = finalizing;
            FreeHandle(pbreakrec);
            return LsErr.None;
        }

        internal static LsErr LoCreateParaBreakingSession(
            IntPtr ploc,
            int cpFirst,
            int maxWidth,
            IntPtr previousLineBreakRecord,
            ref IntPtr ploparabreak,
            ref bool penalizedAsJustified)
        {
            _ = previousLineBreakRecord;
            var ctx = TryGet<ManagedLoContext>(ploc);
            if (ctx == null || ctx.Callbacks == null)
                return LsErr.InvalidParameter;

            FullTextState fullText = ctx.Callbacks.Owner as FullTextState;
            if (fullText == null)
                return LsErr.InvalidParameter;

            penalizedAsJustified = fullText.TextStore.Pap.Justify;
            var session = new ManagedParaBreakingSession
            {
                Context = ctx,
                FullText = fullText,
                CpFirst = cpFirst,
                MaxWidth = maxWidth,
            };
            ploparabreak = AllocHandle(session);
            return LsErr.None;
        }

        internal static LsErr LoDisposeParaBreakingSession(IntPtr ploparabreak, bool finalizing)
        {
            _ = finalizing;
            FreeHandle(ploparabreak);
            return LsErr.None;
        }

        internal static LsErr LoCreateBreaks(
            IntPtr ploc,
            int cpFirst,
            IntPtr previousLineBreakRecord,
            IntPtr ploparabreak,
            IntPtr ptslinevariantRestriction,
            ref LsBreaks lsbreaks,
            out int bestFitIndex)
        {
            _ = previousLineBreakRecord;
            _ = ptslinevariantRestriction;
            bestFitIndex = 0;

            var ctx = TryGet<ManagedLoContext>(ploc);
            var session = TryGet<ManagedParaBreakingSession>(ploparabreak);
            if (ctx == null || session == null)
                return LsErr.InvalidParameter;

            int maxWidth = session.MaxWidth > 0 ? session.MaxWidth : Constants.IdealInfiniteWidth;
            // Create the greedy best-fit line first
            LsErr err = LoCreateLine(
                ploc,
                cpFirst,
                0,
                maxWidth,
                (uint)LineFlags.KeepState,
                previousLineBreakRecord,
                out LsLInfo bestInfo,
                out IntPtr bestLineHandle,
                out _,
                out LsLineWidths _);
            if (err != LsErr.None)
                return err;

            var pm = new ManagedPenaltyModule { Context = ctx, Penalty = 0.0 };
            IntPtr hPm = AllocHandle(pm);
            var bestLine = TryGet<ManagedLoLine>(bestLineHandle);
            if (bestLine != null)
            {
                bestLine.PenaltyResource = pm;
            }

            // Build 1 canonical breakpoint (can expand into shorter break candidates if needed later)
            LsLInfo* infoArr = (LsLInfo*)NativeMemory.Alloc(1, (nuint)sizeof(LsLInfo));
            IntPtr* lineArr = (IntPtr*)NativeMemory.Alloc(1, (nuint)sizeof(IntPtr));
            IntPtr* penArr = (IntPtr*)NativeMemory.Alloc(1, (nuint)sizeof(IntPtr));
            infoArr[0] = bestInfo;
            lineArr[0] = bestLineHandle;
            penArr[0] = hPm;

            lsbreaks.cBreaks = 1;
            lsbreaks.plslinfoArray = infoArr;
            lsbreaks.pplolineArray = lineArr;
            lsbreaks.plinepenaltyArray = penArr;
            bestFitIndex = 0;
            return LsErr.None;
        }

        internal static void LoFreeBreaks(ref LsBreaks lsbreaks)
        {
            if (lsbreaks.plslinfoArray != null)
            {
                NativeMemory.Free(lsbreaks.plslinfoArray);
                lsbreaks.plslinfoArray = null;
            }
            if (lsbreaks.pplolineArray != null)
            {
                NativeMemory.Free(lsbreaks.pplolineArray);
                lsbreaks.pplolineArray = null;
            }
            if (lsbreaks.plinepenaltyArray != null)
            {
                NativeMemory.Free(lsbreaks.plinepenaltyArray);
                lsbreaks.plinepenaltyArray = null;
            }
            lsbreaks.cBreaks = 0;
        }

        internal static LsErr LoRelievePenaltyResource(IntPtr ploline)
        {
            var line = TryGet<ManagedLoLine>(ploline);
            if (line != null && line.PenaltyResource != null)
            {
                line.PenaltyResource = null;
            }
            return LsErr.None;
        }

        internal static LsErr LoAcquirePenaltyModule(IntPtr ploc, out IntPtr ploPenaltyModule)
        {
            var ctx = TryGet<ManagedLoContext>(ploc);
            var pm = new ManagedPenaltyModule { Context = ctx };
            ploPenaltyModule = AllocHandle(pm);
            return LsErr.None;
        }

        internal static LsErr LoDisposePenaltyModule(IntPtr ploPenaltyModule)
        {
            FreeHandle(ploPenaltyModule);
            return LsErr.None;
        }

        internal static LsErr LoGetPenaltyModuleInternalHandle(IntPtr ploPenaltyModule, out IntPtr internalHandle)
        {
            internalHandle = ploPenaltyModule;
            return LsErr.None;
        }

        #endregion

        #region Internal Measurement, Shaping & Line Breaking Helpers

        private static ManagedSegment FetchMarkerSegment(
            IntPtr ploc,
            ManagedLoContext ctx,
            LsPap lspap,
            int durLeft,
            out int markerStart,
            out int markerEnd,
            out LsErr err)
        {
            markerStart = durLeft;
            markerEnd = durLeft;
            err = LsErr.None;

            LsKAlign alignment = LsKAlign.lskalRight;
            LsChp chp = new LsChp();
            IntPtr markerPlsrunPtr = IntPtr.Zero;
            ushort addedChar = 0;
            LsChp chpAdded = new LsChp();
            IntPtr plsrunAdded = IntPtr.Zero;
            int fWord95 = 0;
            int offset = 0;
            int width = 0;

            err = ctx.Callbacks.GetAutoNumberInfo(
                ploc,
                ref alignment,
                ref chp,
                ref markerPlsrunPtr,
                ref addedChar,
                ref chpAdded,
                ref plsrunAdded,
                ref fWord95,
                ref offset,
                ref width);
            if (err != LsErr.None || markerPlsrunPtr == IntPtr.Zero)
                return null;

            FullTextState fullText = ctx.Callbacks.Owner as FullTextState;
            Plsrun plsrun = (Plsrun)markerPlsrunPtr;
            LSRun run = fullText?.TextMarkerStore?.GetRun(plsrun);
            if (run == null || run.Length <= 0)
                return null;

            var markerChars = new char[run.Length];
            for (int i = 0; i < run.Length; i++)
            {
                markerChars[i] = run.CharacterBuffer[run.OffsetToFirstChar + i];
            }

            var chunk = new FetchedChunk
            {
                Plsrun = plsrun,
                LscpStart = TextStore.LscpFirstMarker,
                Cch = run.Length,
                IsHidden = false,
                Chp = chp,
                Chars = markerChars,
                Run = run,
                EffectiveBidiLevel = run.BidiLevel,
            };

            var markerChunks = new List<FetchedChunk>(1) { chunk };
            var built = BuildSegments(ploc, ctx, lspap.lstflow, markerChunks, out err);
            if (err != LsErr.None || built.Count == 0)
                return null;

            ManagedSegment seg = built[0];
            int markerOffsetFromTextStart = fullText.GetMainTextToMarkerIdealDistance();
            seg.UStart = Math.Max(0, durLeft - markerOffsetFromTextStart - seg.Dur);
            markerStart = seg.UStart;
            markerEnd = seg.UStart + seg.Dur;
            return seg;
        }

        private static List<ManagedSegment> BuildSegments(
            IntPtr ploc,
            ManagedLoContext ctx,
            LsTFlow baseFlow,
            List<FetchedChunk> rawChunks,
            out LsErr err)
        {
            err = LsErr.None;
            var segments = new List<ManagedSegment>(rawChunks.Count);

            int i = 0;
            while (i < rawChunks.Count)
            {
                FetchedChunk first = rawChunks[i];

                if (first.IsHidden)
                {
                    segments.Add(new ManagedSegment
                    {
                        Plsrun = first.Plsrun,
                        LscpFirst = first.LscpStart,
                        Cch = first.Cch,
                        IsHidden = true,
                        BidiLevel = first.EffectiveBidiLevel,
                        TextFlow = baseFlow,
                    });
                    i++;
                    continue;
                }

                if (first.Plsrun == Plsrun.Reverse || first.Plsrun == Plsrun.CloseAnchor)
                {
                    segments.Add(new ManagedSegment
                    {
                        Plsrun = first.Plsrun,
                        LscpFirst = first.LscpStart,
                        Cch = first.Cch,
                        IsControl = true,
                        BidiLevel = first.EffectiveBidiLevel,
                        TextFlow = baseFlow,
                    });
                    i++;
                    continue;
                }

                if (TextStore.IsNewline(first.Plsrun))
                {
                    segments.Add(new ManagedSegment
                    {
                        Plsrun = first.Plsrun,
                        LscpFirst = first.LscpStart,
                        Cch = first.Cch,
                        IsNewline = true,
                        Chars = first.Chars,
                        BidiLevel = first.EffectiveBidiLevel,
                        TextFlow = baseFlow,
                    });
                    i++;
                    continue;
                }

                if (first.Chp.idObj == (ushort)TextStore.ObjectId.InlineObject)
                {
                    ObjDim objDim = new ObjDim();
                    int firstReal = 1;
                    int penUsed = 0;
                    err = ctx.Callbacks.InlineFormat(
                        ploc,
                        first.Plsrun,
                        first.LscpStart,
                        0,
                        Constants.IdealInfiniteWidth,
                        ref objDim,
                        out firstReal,
                        out penUsed,
                        out _,
                        out _);
                    if (err != LsErr.None)
                        return segments;

                    segments.Add(new ManagedSegment
                    {
                        Plsrun = first.Plsrun,
                        IdObj = first.Chp.idObj,
                        LscpFirst = first.LscpStart,
                        Cch = first.Cch,
                        Dur = objDim.dur,
                        TrailingTrimmedDur = objDim.dur,
                        Ascent = objDim.heightsRef.dvAscent,
                        Descent = objDim.heightsRef.dvDescent,
                        MultiLineHeight = objDim.heightsPres.dvMultiLineHeight,
                        BidiLevel = first.EffectiveBidiLevel,
                        TextFlow = (first.EffectiveBidiLevel & 1) != 0 ? LsTFlow.lstflowWS : LsTFlow.lstflowES,
                        ChpFlags = first.Chp.flags,
                        ObjectDim = objDim,
                    });
                    i++;
                    continue;
                }

                // Regular text run
                LsTFlow runFlow = (first.EffectiveBidiLevel & 1) != 0 ? LsTFlow.lstflowWS : LsTFlow.lstflowES;
                LsTxM txm = new LsTxM();
                err = ctx.Callbacks.GetRunTextMetrics(ploc, first.Plsrun, LsDevice.Reference, runFlow, ref txm);
                if (err != LsErr.None)
                    return segments;

                // Tab run check
                if (first.Cch == 1 && first.Chars.Length == 1 && first.Chars[0] == '\t')
                {
                    segments.Add(new ManagedSegment
                    {
                        Plsrun = first.Plsrun,
                        IdObj = first.Chp.idObj,
                        LscpFirst = first.LscpStart,
                        Cch = 1,
                        IsTab = true,
                        Chars = first.Chars,
                        CharWidths = new int[1],
                        Ascent = txm.dvAscent,
                        Descent = txm.dvDescent,
                        MultiLineHeight = txm.dvMultiLineHeight,
                        BidiLevel = first.EffectiveBidiLevel,
                        TextFlow = runFlow,
                        ChpFlags = first.Chp.flags,
                    });
                    i++;
                    continue;
                }

                if ((first.Chp.flags & LsChp.Flags.fGlyphBased) != 0)
                {
                    // Merge adjacent fGlyphBased chunks that can shape together
                    int j = i + 1;
                    int totalCch = first.Cch;
                    while (j < rawChunks.Count)
                    {
                        FetchedChunk next = rawChunks[j];
                        if (next.IsHidden ||
                            !TextStore.IsContent(next.Plsrun) ||
                            next.Chp.idObj != (ushort)TextStore.ObjectId.Text_chp ||
                            (next.Chp.flags & LsChp.Flags.fGlyphBased) == 0 ||
                            next.EffectiveBidiLevel != first.EffectiveBidiLevel ||
                            (next.Cch == 1 && next.Chars.Length == 1 && next.Chars[0] == '\t'))
                        {
                            break;
                        }

                        int fInterrupt = 0;
                        ctx.Callbacks.FInterruptShaping(
                            ploc,
                            runFlow,
                            rawChunks[j - 1].Plsrun,
                            next.Plsrun,
                            ref fInterrupt);
                        if (fInterrupt != 0)
                            break;

                        totalCch += next.Cch;
                        j++;
                    }

                    // Shape the merged span rawChunks[i .. j-1] and then split back per plsrun if needed,
                    // OR keep as a single shaped segment if it corresponds to 1 plsrun (WPF callbacks expect 1 plsrun per DrawGlyphs).
                    // Note: Callbacks.DrawGlyphs takes ONE plsrun, whereas EnumText also takes one plsrun (though EnumText maps cpFirst+dcp).
                    // Call GetGlyphsRedefined/GetGlyphPositions across the merged span so contextual OpenType features (e.g., Arabic joining across spans) apply properly!
                    int spanCount = j - i;
                    char[] mergedChars = new char[totalCch];
                    IntPtr[] runPtrs = new IntPtr[spanCount];
                    int[] runCchs = new int[spanCount];
                    int charOffset = 0;
                    for (int k = 0; k < spanCount; k++)
                    {
                        FetchedChunk ck = rawChunks[i + k];
                        Array.Copy(ck.Chars, 0, mergedChars, charOffset, ck.Cch);
                        runPtrs[k] = (IntPtr)ck.Plsrun;
                        runCchs[k] = ck.Cch;
                        charOffset += ck.Cch;
                    }

                    var seg = ShapeChunkGroup(
                        ploc,
                        ctx,
                        first,
                        runFlow,
                        txm,
                        mergedChars,
                        runPtrs,
                        runCchs,
                        out err);
                    if (err != LsErr.None)
                        return segments;

                    // If multiple chunks were merged solely for cross-run OpenType features while sharing the same run, split back cleanly into individual segments
                    if (spanCount == 1)
                    {
                        segments.Add(seg);
                    }
                    else
                    {
                        SplitShapedSegmentByChunks(rawChunks, i, spanCount, seg, segments);
                    }
                    i = j;
                }
                else
                {
                    int[] widths = new int[first.Cch];
                    int totalW = 0;
                    int processed = 0;
                    fixed (char* pwch = first.Chars)
                    fixed (int* pWidths = widths)
                    {
                        err = ctx.Callbacks.GetRunCharWidths(
                            ploc,
                            first.Plsrun,
                            LsDevice.Reference,
                            pwch,
                            first.Cch,
                            Constants.IdealInfiniteWidth,
                            runFlow,
                            pWidths,
                            ref totalW,
                            ref processed);
                    }
                    if (err != LsErr.None)
                        return segments;

                    int trimmedDur = totalW;
                    int trailingWs = 0;
                    for (int c = first.Cch - 1; c >= 0 && IsTrailingWhitespace(first.Chars[c]); c--)
                    {
                        trailingWs++;
                        trimmedDur -= widths[c];
                    }

                    segments.Add(new ManagedSegment
                    {
                        Plsrun = first.Plsrun,
                        IdObj = first.Chp.idObj,
                        LscpFirst = first.LscpStart,
                        Cch = first.Cch,
                        Chars = first.Chars,
                        CharWidths = widths,
                        Dur = totalW,
                        TrailingTrimmedDur = Math.Max(0, trimmedDur),
                        TrailingWhitespaceCount = trailingWs,
                        Ascent = txm.dvAscent,
                        Descent = txm.dvDescent,
                        MultiLineHeight = txm.dvMultiLineHeight,
                        BidiLevel = first.EffectiveBidiLevel,
                        TextFlow = runFlow,
                        ChpFlags = first.Chp.flags,
                    });
                    i++;
                }
            }

            return segments;
        }

        private static ManagedSegment ShapeChunkGroup(
            IntPtr ploc,
            ManagedLoContext ctx,
            FetchedChunk first,
            LsTFlow runFlow,
            LsTxM txm,
            char[] mergedChars,
            IntPtr[] runPtrs,
            int[] runCchs,
            out LsErr err)
        {
            int cch = mergedChars.Length;
            int glyphCap = Math.Max(16, (cch * 3) / 2 + 16);
            ushort[] glyphBuf = new ushort[glyphCap];
            uint[] glyphPropsBuf = new uint[glyphCap];
            ushort[] clusterMap = new ushort[cch];
            ushort[] charProps = new ushort[cch];
            int[] canGlyphAlone = new int[cch];
            int actualGlyphCount = 0;
            int fUsed = 0;

            fixed (IntPtr* pRuns = runPtrs)
            fixed (int* pRunCchs = runCchs)
            fixed (char* pwch = mergedChars)
            fixed (ushort* pCluster = clusterMap)
            fixed (ushort* pCharProps = charProps)
            fixed (int* pAlone = canGlyphAlone)
            {
                fixed (ushort* pGlyphs = glyphBuf)
                fixed (uint* pGlyphProps = glyphPropsBuf)
                {
                    err = ctx.Callbacks.GetGlyphsRedefined(
                        ploc,
                        pRuns,
                        pRunCchs,
                        runPtrs.Length,
                        pwch,
                        cch,
                        runFlow,
                        pGlyphs,
                        pGlyphProps,
                        glyphCap,
                        ref fUsed,
                        pCluster,
                        pCharProps,
                        pAlone,
                        ref actualGlyphCount);
                }
                if (err != LsErr.None)
                    return null;

                if (fUsed == 0 && actualGlyphCount > glyphCap)
                {
                    glyphCap = actualGlyphCount;
                    glyphBuf = new ushort[glyphCap];
                    glyphPropsBuf = new uint[glyphCap];
                    fixed (ushort* pGlyphs = glyphBuf)
                    fixed (uint* pGlyphProps = glyphPropsBuf)
                    {
                        err = ctx.Callbacks.GetGlyphsRedefined(
                            ploc,
                            pRuns,
                            pRunCchs,
                            runPtrs.Length,
                            pwch,
                            cch,
                            runFlow,
                            pGlyphs,
                            pGlyphProps,
                            glyphCap,
                            ref fUsed,
                            pCluster,
                            pCharProps,
                            pAlone,
                            ref actualGlyphCount);
                    }
                    if (err != LsErr.None)
                        return null;
                }
            }

            if (actualGlyphCount < glyphBuf.Length)
            {
                Array.Resize(ref glyphBuf, actualGlyphCount);
                Array.Resize(ref glyphPropsBuf, actualGlyphCount);
            }

            int[] glyphAdvances = new int[actualGlyphCount];
            GlyphOffset[] glyphOffsets = new GlyphOffset[actualGlyphCount];

            fixed (IntPtr* pRuns = runPtrs)
            fixed (int* pRunCchs = runCchs)
            fixed (char* pwch = mergedChars)
            fixed (ushort* pCluster = clusterMap)
            fixed (ushort* pCharProps = charProps)
            fixed (ushort* pGlyphs = glyphBuf)
            fixed (uint* pGlyphProps = glyphPropsBuf)
            fixed (int* pAdvances = glyphAdvances)
            fixed (GlyphOffset* pOffsets = glyphOffsets)
            {
                err = ctx.Callbacks.GetGlyphPositions(
                    ploc,
                    pRuns,
                    pRunCchs,
                    runPtrs.Length,
                    LsDevice.Reference,
                    pwch,
                    pCluster,
                    pCharProps,
                    cch,
                    pGlyphs,
                    pGlyphProps,
                    actualGlyphCount,
                    runFlow,
                    pAdvances,
                    pOffsets);
            }
            if (err != LsErr.None)
                return null;

            // Derive per-character widths from glyph clusters (accurate sum of glyph advances per cluster / evenly over cluster chars)
            int[] charWidths = ComputeCharWidthsFromClusters(cch, clusterMap, glyphAdvances, actualGlyphCount, out int totalDur);
            int trimmedDur = totalDur;
            int trailingWs = 0;
            for (int c = cch - 1; c >= 0 && IsTrailingWhitespace(mergedChars[c]); c--)
            {
                trailingWs++;
                trimmedDur -= charWidths[c];
            }

            int[] origAdvances = (int[])glyphAdvances.Clone();

            return new ManagedSegment
            {
                Plsrun = first.Plsrun,
                IdObj = first.Chp.idObj,
                LscpFirst = first.LscpStart,
                Cch = cch,
                Chars = mergedChars,
                CharWidths = charWidths,
                IsGlyphBased = true,
                ClusterMap = clusterMap,
                CharProperties = charProps,
                GlyphIndices = glyphBuf,
                GlyphProperties = glyphPropsBuf,
                GlyphAdvances = glyphAdvances,
                OriginalGlyphAdvances = origAdvances,
                GlyphOffsets = glyphOffsets,
                GlyphCount = actualGlyphCount,
                Dur = totalDur,
                TrailingTrimmedDur = Math.Max(0, trimmedDur),
                TrailingWhitespaceCount = trailingWs,
                Ascent = txm.dvAscent,
                Descent = txm.dvDescent,
                MultiLineHeight = txm.dvMultiLineHeight,
                BidiLevel = first.EffectiveBidiLevel,
                TextFlow = runFlow,
                ChpFlags = first.Chp.flags,
            };
        }

        private static void SplitShapedSegmentByChunks(
            List<FetchedChunk> rawChunks,
            int startChunkIndex,
            int chunkCount,
            ManagedSegment shaped,
            List<ManagedSegment> output)
        {
            int charCursor = 0;
            for (int k = 0; k < chunkCount; k++)
            {
                FetchedChunk ck = rawChunks[startChunkIndex + k];
                int cch = ck.Cch;
                int gStart = shaped.ClusterMap[charCursor];
                int gEnd = (charCursor + cch < shaped.Cch) ? shaped.ClusterMap[charCursor + cch] : shaped.GlyphCount;
                if (gEnd < gStart)
                    gEnd = shaped.GlyphCount;
                int gLen = Math.Max(0, gEnd - gStart);

                var subChars = new char[cch];
                Array.Copy(ck.Chars, subChars, cch);

                var subCluster = new ushort[cch];
                var subCharProps = new ushort[cch];
                var subCharWidths = new int[cch];
                for (int c = 0; c < cch; c++)
                {
                    int mapped = shaped.ClusterMap[charCursor + c] - gStart;
                    subCluster[c] = (ushort)Math.Clamp(mapped, 0, Math.Max(0, gLen - 1));
                    subCharProps[c] = shaped.CharProperties[charCursor + c];
                    subCharWidths[c] = shaped.CharWidths[charCursor + c];
                }

                var subGlyphs = new ushort[gLen];
                var subGProps = new uint[gLen];
                var subAdv = new int[gLen];
                var subOrigAdv = new int[gLen];
                var subOff = new GlyphOffset[gLen];
                int subDur = 0;
                for (int g = 0; g < gLen; g++)
                {
                    subGlyphs[g] = shaped.GlyphIndices[gStart + g];
                    subGProps[g] = shaped.GlyphProperties[gStart + g];
                    subAdv[g] = shaped.GlyphAdvances[gStart + g];
                    subOrigAdv[g] = shaped.OriginalGlyphAdvances[gStart + g];
                    subOff[g] = shaped.GlyphOffsets[gStart + g];
                    subDur += subAdv[g];
                }

                int trimmedDur = subDur;
                int trailingWs = 0;
                for (int c = cch - 1; c >= 0 && IsTrailingWhitespace(subChars[c]); c--)
                {
                    trailingWs++;
                    trimmedDur -= subCharWidths[c];
                }

                output.Add(new ManagedSegment
                {
                    Plsrun = ck.Plsrun,
                    IdObj = ck.Chp.idObj,
                    LscpFirst = ck.LscpStart,
                    Cch = cch,
                    Chars = subChars,
                    CharWidths = subCharWidths,
                    IsGlyphBased = true,
                    ClusterMap = subCluster,
                    CharProperties = subCharProps,
                    GlyphIndices = subGlyphs,
                    GlyphProperties = subGProps,
                    GlyphAdvances = subAdv,
                    OriginalGlyphAdvances = subOrigAdv,
                    GlyphOffsets = subOff,
                    GlyphCount = gLen,
                    Dur = subDur,
                    TrailingTrimmedDur = Math.Max(0, trimmedDur),
                    TrailingWhitespaceCount = trailingWs,
                    Ascent = shaped.Ascent,
                    Descent = shaped.Descent,
                    MultiLineHeight = shaped.MultiLineHeight,
                    BidiLevel = ck.EffectiveBidiLevel,
                    TextFlow = shaped.TextFlow,
                    ChpFlags = ck.Chp.flags,
                });

                charCursor += cch;
            }
        }

        private static int[] ComputeCharWidthsFromClusters(
            int cch,
            ushort[] clusterMap,
            int[] glyphAdvances,
            int glyphCount,
            out int totalDur)
        {
            totalDur = 0;
            for (int g = 0; g < glyphCount; g++)
                totalDur += glyphAdvances[g];

            int[] charWidths = new int[cch];
            if (cch == 0 || glyphCount == 0)
                return charWidths;

            int ci = 0;
            while (ci < cch)
            {
                ushort gStart = clusterMap[ci];
                int cEnd = ci + 1;
                while (cEnd < cch && clusterMap[cEnd] == gStart)
                    cEnd++;

                ushort gEnd = (cEnd < cch) ? clusterMap[cEnd] : (ushort)glyphCount;
                if (gEnd < gStart)
                    gEnd = (ushort)glyphCount;

                int clusterWidth = 0;
                for (int g = gStart; g < gEnd && g < glyphCount; g++)
                    clusterWidth += glyphAdvances[g];

                int clusterCharCount = cEnd - ci;
                int baseW = clusterWidth / clusterCharCount;
                int remW = clusterWidth % clusterCharCount;
                for (int k = 0; k < clusterCharCount; k++)
                {
                    charWidths[ci + k] = baseW + (k < remW ? 1 : 0);
                }

                ci = cEnd;
            }
            return charWidths;
        }

        private static void ComputeBreakAndMinWidth(
            IntPtr ploc,
            ManagedLoContext ctx,
            FullTextState fullText,
            TextStore store,
            List<ManagedSegment> segments,
            int durLeft,
            int formatRightMargin,
            bool applyBreak,
            bool emergencyCharWrap,
            bool wrapAllowed,
            bool allowHyphenation,
            bool baseRtl,
            ref LsEndRes endRes,
            ref int eolCch,
            ref int lscpEndOfLine,
            out bool forcedBreak,
            out int minUnbreakableWidth)
        {
            forcedBreak = false;
            minUnbreakableWidth = 0;
            _ = ploc;
            _ = allowHyphenation;

            // Flatten all characters from non-hidden/non-control segments for DirectWrite AnalyzeLineBreakpoints
            int totalFlatChars = 0;
            for (int s = 0; s < segments.Count; s++)
            {
                ManagedSegment seg = segments[s];
                if (!seg.IsHidden && !seg.IsControl && !seg.IsNewline)
                {
                    totalFlatChars += (seg.IdObj == (ushort)TextStore.ObjectId.InlineObject) ? 1 : seg.Cch;
                }
            }

            if (totalFlatChars == 0)
            {
                minUnbreakableWidth = durLeft;
                return;
            }

            char[] flatChars = new char[totalFlatChars];
            int[] unitWidth = new int[totalFlatChars];
            bool[] unitIsWs = new bool[totalFlatChars];
            int[] unitSegIdx = new int[totalFlatChars];
            int[] unitSegOffset = new int[totalFlatChars];
            int[] unitLscpNext = new int[totalFlatChars];

            int pos = 0;
            for (int s = 0; s < segments.Count; s++)
            {
                ManagedSegment seg = segments[s];
                if (seg.IsHidden || seg.IsControl || seg.IsNewline)
                    continue;

                if (seg.IdObj == (ushort)TextStore.ObjectId.InlineObject)
                {
                    flatChars[pos] = '\uFFFC';
                    unitWidth[pos] = seg.Dur;
                    unitIsWs[pos] = false;
                    unitSegIdx[pos] = s;
                    unitSegOffset[pos] = 0;
                    unitLscpNext[pos] = seg.LscpFirst + seg.Cch;
                    pos++;
                }
                else
                {
                    for (int c = 0; c < seg.Cch; c++)
                    {
                        char ch = seg.Chars[c];
                        flatChars[pos] = ch;
                        int w = (seg.CharWidths != null && c < seg.CharWidths.Length) ? seg.CharWidths[c] : 0;
                        if (seg.IsTab)
                        {
                            w = ComputeNextTabWidth(durLeft, ctx, out _); // approximation during break walk
                        }
                        unitWidth[pos] = w;
                        unitIsWs[pos] = IsTrailingWhitespace(ch);
                        unitSegIdx[pos] = s;
                        unitSegOffset[pos] = c;
                        unitLscpNext[pos] = seg.LscpFirst + c + 1;
                        pos++;
                    }
                }
            }

            DWRITE_LINE_BREAKPOINT[] dwriteBreaks = new DWRITE_LINE_BREAKPOINT[totalFlatChars];
            fixed (char* pFlat = flatChars)
            {
                fullText.Formatter.TextAnalyzer.AnalyzeLineBreakpoints(
                    pFlat,
                    (uint)totalFlatChars,
                    store.Pap.DefaultCulture,
                    baseRtl,
                    dwriteBreaks);
            }

            // First pass: calculate min unbreakable word width (for FormatMinMaxParagraphWidth / MinWidth)
            int curChunkWidth = 0;
            int maxChunkWidth = 0;
            int runningW = durLeft;
            int lastLegalBreakUnit = -1; // break AFTER unit index lastLegalBreakUnit
            int lastEmergencyUnit = -1;
            int breakAfterUnit = -1;
            bool overflowTriggered = false;

            for (int u = 0; u < totalFlatChars; u++)
            {
                int w = unitWidth[u];
                bool isWs = unitIsWs[u];

                if (!isWs)
                    curChunkWidth += w;

                // Evaluate running width including previously skipped whitespace
                int widthUpToHereTrimmed = runningW + w;
                if (!isWs)
                {
                    // runningW already includes everything before u
                    runningW += w;
                    widthUpToHereTrimmed = runningW;
                }
                else
                {
                    runningW += w;
                    // For trailing space itself, we don't fail wrap check
                }

                // Emergency break point at any cluster boundary that still fits (excluding trailing whitespace)
                if (widthUpToHereTrimmed <= formatRightMargin)
                {
                    lastEmergencyUnit = u;
                }

                // Is break allowed AFTER unit u?
                bool canBreakAfter = CanBreakBetween(flatChars, dwriteBreaks, u, totalFlatChars, ctx.BreakingStrategy);
                if (canBreakAfter)
                {
                    if (curChunkWidth > maxChunkWidth)
                        maxChunkWidth = curChunkWidth;
                    curChunkWidth = 0;
                }

                // Did a non-whitespace character overflow?
                if (applyBreak && !overflowTriggered && !isWs && widthUpToHereTrimmed > formatRightMargin)
                {
                    overflowTriggered = true;
                    if (lastLegalBreakUnit >= 0)
                    {
                        breakAfterUnit = lastLegalBreakUnit;
                        break;
                    }
                    else if (emergencyCharWrap || !wrapAllowed)
                    {
                        // TextWrapping.Wrap: break right before overflow (guarantee at least 1 unit so line progresses)
                        breakAfterUnit = Math.Max(0, lastEmergencyUnit);
                        forcedBreak = true;
                        break;
                    }
                    else
                    {
                        // TextWrapping.WrapWithOverflow: keep going until FIRST legal break after the word!
                    }
                }

                if (canBreakAfter)
                {
                    if (overflowTriggered)
                    {
                        // First legal break after an overflowing unbreakable word (WrapWithOverflow)
                        breakAfterUnit = u;
                        break;
                    }

                    // Consume trailing whitespace immediately following this legal break (e.g. "word   next")
                    // unless u itself is whitespace; in WPF Spaces stay at end of current line up to end of space run.
                    lastLegalBreakUnit = u;
                }
            }

            if (curChunkWidth > maxChunkWidth)
                maxChunkWidth = curChunkWidth;
            minUnbreakableWidth = durLeft + maxChunkWidth;

            if (!applyBreak || breakAfterUnit < 0 || breakAfterUnit >= totalFlatChars - 1)
                return;

            // Advance breakAfterUnit across any immediately following consecutive spaces on the same line
            // (Wait: DirectWrite AnalyzeLineBreakpoints marks canBreakAfter AFTER the last space or on each space;
            // if we broke right after a non-space/space, include trailing spaces that do not overflow non-ws content, up to next non-space!)
            while (breakAfterUnit + 1 < totalFlatChars && unitIsWs[breakAfterUnit + 1] && flatChars[breakAfterUnit + 1] != '\t')
            {
                breakAfterUnit++;
            }

            int cutSegIdx = unitSegIdx[breakAfterUnit];
            int cutOffset = unitSegOffset[breakAfterUnit];
            lscpEndOfLine = unitLscpNext[breakAfterUnit];

            // We broke mid-paragraph, not at EOP/SoftCR
            endRes = LsEndRes.endrNormal;
            eolCch = 0;

            TruncateSegments(ploc, ctx, segments, cutSegIdx, cutOffset);
        }

        private static bool CanBreakBetween(
            char[] chars,
            DWRITE_LINE_BREAKPOINT[] breaks,
            int indexAfter,
            int totalLength,
            BreakStrategies strategy)
        {
            if (indexAfter >= totalLength - 1)
                return false;

            // Never split inside a UTF-16 surrogate pair
            if (char.IsHighSurrogate(chars[indexAfter]) && char.IsLowSurrogate(chars[indexAfter + 1]))
                return false;

            uint afterCond = breaks[indexAfter].BreakConditionAfter;
            uint beforeNextCond = breaks[indexAfter + 1].BreakConditionBefore;

            // DWRITE_BREAK_CONDITION: 0=Neutral, 1=CanBreak, 2=MayNotBreak, 3=MustBreak
            if (afterCond == 3 || beforeNextCond == 3)
                return true;
            if (afterCond == 2 || beforeNextCond == 2)
                return false;

            bool dwriteCan = (afterCond == 1 || beforeNextCond == 1);
            if (!dwriteCan)
                return false;

            if (strategy == BreakStrategies.KeepCJK)
            {
                if (IsCjkChar(chars[indexAfter]) && IsCjkChar(chars[indexAfter + 1]))
                    return false;
            }

            return true;
        }

        private static bool IsCjkChar(char ch)
            => (ch >= '\u4E00' && ch <= '\u9FFF') ||
               (ch >= '\u3400' && ch <= '\u4DBF') ||
               (ch >= '\u3040' && ch <= '\u30FF') ||
               (ch >= '\uAC00' && ch <= '\uD7AF');

        private static void TruncateSegments(
            IntPtr ploc,
            ManagedLoContext ctx,
            List<ManagedSegment> segments,
            int cutSegIdx,
            int cutOffset)
        {
            ManagedSegment seg = segments[cutSegIdx];
            if (seg.IdObj == (ushort)TextStore.ObjectId.Text_chp && cutOffset + 1 < seg.Cch)
            {
                int newCch = cutOffset + 1;
                var newChars = new char[newCch];
                Array.Copy(seg.Chars, newChars, newCch);
                seg.Cch = newCch;
                seg.Chars = newChars;

                if (seg.IsGlyphBased && seg.ClusterMap != null)
                {
                    int keepGlyphs = newCch < seg.ClusterMap.Length ? seg.ClusterMap[newCch] : seg.GlyphCount;
                    keepGlyphs = Math.Clamp(keepGlyphs, 0, seg.GlyphCount);

                    Array.Resize(ref seg.ClusterMap, newCch);
                    Array.Resize(ref seg.CharProperties, newCch);
                    Array.Resize(ref seg.CharWidths, newCch);
                    Array.Resize(ref seg.GlyphIndices, keepGlyphs);
                    Array.Resize(ref seg.GlyphProperties, keepGlyphs);
                    Array.Resize(ref seg.GlyphAdvances, keepGlyphs);
                    Array.Resize(ref seg.OriginalGlyphAdvances, keepGlyphs);
                    Array.Resize(ref seg.GlyphOffsets, keepGlyphs);
                    seg.GlyphCount = keepGlyphs;

                    int dur = 0;
                    for (int g = 0; g < keepGlyphs; g++)
                        dur += seg.GlyphAdvances[g];
                    seg.Dur = dur;
                }
                else if (!seg.IsTab && seg.CharWidths != null)
                {
                    Array.Resize(ref seg.CharWidths, newCch);
                    int dur = 0;
                    for (int c = 0; c < newCch; c++)
                        dur += seg.CharWidths[c];
                    seg.Dur = dur;
                }

                int trimmedDur = seg.Dur;
                int trailingWs = 0;
                for (int c = seg.Cch - 1; c >= 0 && IsTrailingWhitespace(seg.Chars[c]); c--)
                {
                    trailingWs++;
                    if (seg.CharWidths != null && c < seg.CharWidths.Length)
                        trimmedDur -= seg.CharWidths[c];
                }
                seg.TrailingTrimmedDur = Math.Max(0, trimmedDur);
                seg.TrailingWhitespaceCount = trailingWs;
            }

            // Drop all segments after cutSegIdx (preserve trailing closing CloseAnchor if needed for Bidi balance, though next line gets BreakRecord)
            if (cutSegIdx + 1 < segments.Count)
            {
                segments.RemoveRange(cutSegIdx + 1, segments.Count - (cutSegIdx + 1));
            }
            _ = ploc;
            _ = ctx;
        }

        private static ManagedLoLine FinalizeLineMetrics(
            ManagedLoContext ctx,
            LsPap lspap,
            bool isFirstLineInPara,
            LsLineProps lsLineProps,
            LsTFlow baseFlow,
            int cpFirst,
            int lscpLim,
            int maxWidth,
            int durLeft,
            ManagedSegment markerSeg,
            int markerStart,
            int markerEnd,
            List<ManagedSegment> logicalSegments,
            LsEndRes endRes,
            int cchEol,
            bool forcedBreak,
            int minUnbreakableWidth,
            int maxDepth,
            ManagedBreakRecord prevBreak,
            out LsLInfo plslineInfo,
            out LsLineWidths lineWidths)
        {
            // 1. Resolve Tab widths & advance total logical width
            int curU = durLeft;
            int maxAscent = 0;
            int maxDescent = 0;
            int maxMultiHeight = 0;

            if (markerSeg != null)
            {
                maxAscent = Math.Max(maxAscent, markerSeg.Ascent);
                maxDescent = Math.Max(maxDescent, markerSeg.Descent);
                maxMultiHeight = Math.Max(maxMultiHeight, markerSeg.MultiLineHeight);
            }

            for (int i = 0; i < logicalSegments.Count; i++)
            {
                ManagedSegment seg = logicalSegments[i];
                if (seg.IsHidden || seg.IsControl || seg.IsNewline)
                    continue;

                if (seg.IsTab)
                {
                    int tabW = ComputeNextTabWidth(curU, ctx, out char leader);
                    seg.Dur = tabW;
                    seg.TrailingTrimmedDur = tabW;
                    seg.TrailingWhitespaceCount = 0;
                    seg.TabLeader = leader;
                    if (seg.CharWidths != null && seg.CharWidths.Length > 0)
                        seg.CharWidths[0] = tabW;
                }

                curU += seg.Dur;
                maxAscent = Math.Max(maxAscent, seg.Ascent);
                maxDescent = Math.Max(maxDescent, seg.Descent);
                maxMultiHeight = Math.Max(maxMultiHeight, seg.MultiLineHeight);
            }

            int upLimLine = curU;
            // Compute trailing whitespace at the logical end of the line
            int trailingTrimU = upLimLine;
            for (int i = logicalSegments.Count - 1; i >= 0; i--)
            {
                ManagedSegment seg = logicalSegments[i];
                if (seg.IsHidden || seg.IsControl || seg.IsNewline)
                    continue;
                if (seg.TrailingWhitespaceCount == seg.Cch && !seg.IsTab && seg.IdObj == (ushort)TextStore.ObjectId.Text_chp)
                {
                    trailingTrimU -= seg.Dur;
                }
                else
                {
                    trailingTrimU -= seg.TrailingWidth;
                    break;
                }
            }
            if (trailingTrimU < durLeft)
                trailingTrimU = durLeft;

            // 2. Build VisualSegments using Unicode Bidi L2 reordering
            List<ManagedSegment> visualSegments = ReorderVisualSegments(logicalSegments, (byte)(baseFlow == LsTFlow.lstflowWS ? 1 : 0));

            // 3. Justification (if requested and line wrapped cleanly before paragraph end)
            if (lspap.fJustify != 0 &&
                endRes == LsEndRes.endrNormal &&
                maxWidth > 0 &&
                maxWidth < Constants.IdealInfiniteWidth &&
                trailingTrimU < maxWidth - lsLineProps.durRightJustify)
            {
                int targetU = maxWidth - lsLineProps.durRightJustify;
                ApplyInterWordJustification(visualSegments, targetU - trailingTrimU);
                int diff = targetU - trailingTrimU;
                trailingTrimU = targetU;
                upLimLine += diff;
            }

            // 4. Assign U coordinates to VisualSegments
            int uPos = durLeft;
            for (int i = 0; i < visualSegments.Count; i++)
            {
                ManagedSegment seg = visualSegments[i];
                seg.UStart = uPos;
                if (!seg.IsHidden && !seg.IsControl && !seg.IsNewline)
                {
                    uPos += seg.Dur;
                }
            }

            if (maxAscent == 0 && maxDescent == 0)
            {
                maxMultiHeight = int.MaxValue;
            }

            lineWidths = new LsLineWidths
            {
                upStartMarker = markerStart,
                upLimMarker = markerEnd,
                upStartMainText = durLeft,
                upStartTrailing = trailingTrimU,
                upLimLine = upLimLine,
                upMinStartTrailing = Math.Max(durLeft, Math.Min(minUnbreakableWidth, upLimLine)),
                upMinLimLine = Math.Max(durLeft, minUnbreakableWidth),
            };

            plslineInfo = new LsLInfo
            {
                fAdvanced = 1,
                cpLimToContinue = lscpLim,
                dcpDepend = endRes == LsEndRes.endrNormal ? 1 : 0,
                endr = endRes,
                fFirstLineInPara = isFirstLineInPara ? 1 : 0,
                dvrAscent = maxAscent,
                dvrDescent = maxDescent,
                dvrMultiLineHeight = maxMultiHeight,
                fForcedBreak = forcedBreak ? 1 : 0,
                vaAdvance = 0,
            };

            // Track open reverse count for break record
            int openRev = prevBreak?.OpenReverseCount ?? 0;
            for (int i = 0; i < logicalSegments.Count; i++)
            {
                if (logicalSegments[i].Plsrun == Plsrun.Reverse) openRev++;
                else if (logicalSegments[i].Plsrun == Plsrun.CloseAnchor && openRev > 0) openRev--;
            }

            return new ManagedLoLine
            {
                Context = ctx,
                CpFirst = cpFirst,
                LscpLim = lscpLim,
                MaxWidth = maxWidth,
                BaselineOffset = maxAscent,
                Ascent = maxAscent,
                Descent = maxDescent,
                IsRightToLeft = baseFlow == LsTFlow.lstflowWS,
                BaseFlow = baseFlow,
                EndRes = endRes,
                CchEol = cchEol,
                DcpDepend = plslineInfo.dcpDepend,
                MaxBidiDepth = maxDepth,
                ForcedBreak = forcedBreak,
                LineWidths = lineWidths,
                MarkerSegment = markerSeg,
                LogicalSegments = logicalSegments,
                VisualSegments = visualSegments,
                NextBreakRecord = openRev > 0 ? new ManagedBreakRecord { CpNext = lscpLim, OpenReverseCount = openRev } : null,
            };
        }

        private static int ComputeNextTabWidth(int curU, ManagedLoContext ctx, out char leader)
        {
            leader = '\0';
            if (ctx.TabStops != null && ctx.TabStops.Length > 0)
            {
                int bestU = int.MaxValue;
                char bestLeader = '\0';
                for (int i = 0; i < ctx.TabStops.Length; i++)
                {
                    if (ctx.TabStops[i].ur > curU && ctx.TabStops[i].ur < bestU)
                    {
                        bestU = ctx.TabStops[i].ur;
                        bestLeader = ctx.TabStops[i].wchTabLeader;
                    }
                }
                if (bestU != int.MaxValue)
                {
                    leader = bestLeader;
                    return Math.Max(0, bestU - curU);
                }
            }

            int inc = ctx.IncrementalTab > 0 ? ctx.IncrementalTab : 1200;
            int nextInc = ((curU / inc) + 1) * inc;
            return Math.Max(1, nextInc - curU);
        }

        private static List<ManagedSegment> ReorderVisualSegments(List<ManagedSegment> logical, byte baseLevel)
        {
            var visual = new List<ManagedSegment>(logical.Count);
            byte maxLevel = baseLevel;
            for (int i = 0; i < logical.Count; i++)
            {
                visual.Add(logical[i]);
                if (!logical[i].IsHidden && !logical[i].IsControl && !logical[i].IsNewline)
                {
                    if (logical[i].BidiLevel > maxLevel)
                        maxLevel = logical[i].BidiLevel;
                }
            }

            if (maxLevel <= baseLevel)
                return visual;

            // UAX #9 L2: reverse maximal contiguous runs of level >= L from maxLevel down to (baseLevel | 1)
            byte minOdd = (byte)(baseLevel == 0 ? 1 : baseLevel);
            for (int level = maxLevel; level >= minOdd; level--)
            {
                int start = -1;
                for (int i = 0; i <= visual.Count; i++)
                {
                    bool inRun = i < visual.Count && !visual[i].IsControl && !visual[i].IsHidden && visual[i].BidiLevel >= level;
                    if (inRun)
                    {
                        if (start < 0) start = i;
                    }
                    else if (start >= 0)
                    {
                        int end = i - 1;
                        while (start < end)
                        {
                            (visual[start], visual[end]) = (visual[end], visual[start]);
                            start++;
                            end--;
                        }
                        start = -1;
                    }
                }
            }

            return visual;
        }

        private static void ApplyInterWordJustification(List<ManagedSegment> visualSegments, int extraSpace)
        {
            if (extraSpace <= 0)
                return;

            // Count expandable spaces across all non-trailing segments
            List<(ManagedSegment seg, int unitIdx, bool isGlyph)> spaceSlots = new();
            for (int s = 0; s < visualSegments.Count; s++)
            {
                ManagedSegment seg = visualSegments[s];
                if (seg.IsHidden || seg.IsControl || seg.IsNewline || seg.IdObj != (ushort)TextStore.ObjectId.Text_chp || seg.Chars == null)
                    continue;

                int limitChar = seg.Cch - seg.TrailingWhitespaceCount;
                for (int c = 0; c < limitChar; c++)
                {
                    if (seg.Chars[c] == ' ')
                    {
                        if (seg.IsGlyphBased && seg.ClusterMap != null && c < seg.ClusterMap.Length)
                        {
                            int g = seg.ClusterMap[c];
                            if (g < seg.GlyphCount)
                                spaceSlots.Add((seg, g, true));
                        }
                        else if (seg.CharWidths != null && c < seg.CharWidths.Length)
                        {
                            spaceSlots.Add((seg, c, false));
                        }
                    }
                }
            }

            if (spaceSlots.Count == 0)
                return;

            int perSlot = extraSpace / spaceSlots.Count;
            int rem = extraSpace % spaceSlots.Count;
            for (int i = 0; i < spaceSlots.Count; i++)
            {
                int delta = perSlot + (i < rem ? 1 : 0);
                if (delta <= 0)
                    continue;
                var (seg, idx, isGlyph) = spaceSlots[i];
                if (isGlyph)
                {
                    seg.GlyphAdvances[idx] += delta;
                }
                else
                {
                    seg.CharWidths[idx] += delta;
                }
                seg.Dur += delta;
                seg.TrailingTrimmedDur += delta;
            }
        }

        private static LsErr DrawSegment(
            IntPtr ploc,
            LineServicesCallbacks cb,
            ManagedLoLine line,
            ManagedSegment seg,
            ref LSPOINT ptStart,
            uint displayMode,
            ref LSRECT clipRect)
        {
            LsHeights heights = new LsHeights
            {
                dvAscent = seg.Ascent,
                dvDescent = seg.Descent,
                dvMultiLineHeight = seg.MultiLineHeight,
            };

            int leadingU = (seg.TextFlow == line.BaseFlow) ? seg.UStart : (seg.UStart + seg.Dur);
            int lsX = line.IsRightToLeft ? -leadingU : leadingU;

            if (seg.IdObj == (ushort)TextStore.ObjectId.InlineObject)
            {
                // InlineDraw expects leadingU adjusted for mismatched run direction so inner -= Dur matches seg.UStart
                LSPOINT ptInline = new LSPOINT(lsX, ptStart.y);
                return cb.InlineDraw(
                    ploc,
                    seg.Plsrun,
                    ref ptInline,
                    seg.TextFlow,
                    seg.Dur);
            }

            if (seg.IsTab)
            {
                // Tab glyphs aren't drawn unless leader is non-zero; decorations apply to tab width
                return DrawSegmentDecorations(ploc, cb, line, seg, ptStart.y, lsX, heights, displayMode, ref clipRect);
            }

            LSPOINT ptRun = new LSPOINT(lsX, ptStart.y);
            LsErr err;

            if (seg.IsGlyphBased)
            {
                fixed (char* pwch = seg.Chars)
                fixed (ushort* pCluster = seg.ClusterMap)
                fixed (ushort* pCharProps = seg.CharProperties)
                fixed (ushort* pGlyphs = seg.GlyphIndices)
                fixed (int* pAdv = seg.GlyphAdvances)
                fixed (int* pOrigAdv = seg.OriginalGlyphAdvances ?? seg.GlyphAdvances)
                fixed (GlyphOffset* pOff = seg.GlyphOffsets)
                fixed (uint* pGProps = seg.GlyphProperties)
                {
                    err = cb.DrawGlyphs(
                        ploc,
                        seg.Plsrun,
                        pwch,
                        pCluster,
                        pCharProps,
                        seg.Cch,
                        pGlyphs,
                        pAdv,
                        pOrigAdv,
                        pOff,
                        pGProps,
                        null,
                        seg.GlyphCount,
                        seg.TextFlow,
                        displayMode,
                        ref ptRun,
                        ref heights,
                        seg.Dur,
                        ref clipRect);
                }
            }
            else
            {
                fixed (char* pwch = seg.Chars)
                fixed (int* pAdv = seg.CharWidths)
                {
                    err = cb.DrawTextRun(
                        ploc,
                        seg.Plsrun,
                        ref ptRun,
                        pwch,
                        pAdv,
                        seg.Cch,
                        seg.TextFlow,
                        displayMode,
                        ref ptRun,
                        ref heights,
                        seg.Dur,
                        ref clipRect);
                }
            }

            if (err != LsErr.None)
                return err;

            return DrawSegmentDecorations(ploc, cb, line, seg, ptStart.y, lsX, heights, displayMode, ref clipRect);
        }

        private static LsErr DrawSegmentDecorations(
            IntPtr ploc,
            LineServicesCallbacks cb,
            ManagedLoLine line,
            ManagedSegment seg,
            int baselineY,
            int lsX,
            LsHeights heights,
            uint displayMode,
            ref LSRECT clipRect)
        {
            _ = line;
            if ((seg.ChpFlags & LsChp.Flags.fUnderline) != 0)
            {
                LsULInfo ulInfo = new LsULInfo();
                LsErr uerr = cb.GetRunUnderlineInfo(ploc, seg.Plsrun, ref heights, seg.TextFlow, ref ulInfo);
                if (uerr != LsErr.None)
                    return uerr;

                LSPOINT ulOrigin = new LSPOINT(lsX, baselineY + ulInfo.dvpFirstUnderlineOffset);
                uerr = cb.DrawUnderline(
                    ploc,
                    seg.Plsrun,
                    1,
                    ref ulOrigin,
                    seg.Dur,
                    ulInfo.dvpFirstUnderlineSize,
                    seg.TextFlow,
                    displayMode,
                    ref clipRect);
                if (uerr != LsErr.None)
                    return uerr;
            }

            if ((seg.ChpFlags & LsChp.Flags.fStrike) != 0)
            {
                LsStInfo stInfo = new LsStInfo();
                LsErr serr = cb.GetRunStrikethroughInfo(ploc, seg.Plsrun, ref heights, seg.TextFlow, ref stInfo);
                if (serr != LsErr.None)
                    return serr;

                LSPOINT stOrigin = new LSPOINT(lsX, baselineY - stInfo.dvpLowerStrikethroughOffset);
                serr = cb.DrawStrikethrough(
                    ploc,
                    seg.Plsrun,
                    1,
                    ref stOrigin,
                    seg.Dur,
                    stInfo.dvpLowerStrikethroughSize,
                    seg.TextFlow,
                    displayMode,
                    ref clipRect);
                if (serr != LsErr.None)
                    return serr;
            }

            return LsErr.None;
        }

        private static ManagedSegment FindSegmentForCp(ManagedLoLine line, int lscp)
        {
            ManagedSegment fallback = null;
            for (int i = 0; i < line.LogicalSegments.Count; i++)
            {
                ManagedSegment seg = line.LogicalSegments[i];
                if (seg.IsControl || seg.IsHidden)
                    continue;
                fallback ??= seg;
                if (lscp >= seg.LscpFirst && lscp < seg.LscpFirst + seg.Cch)
                    return seg;
                fallback = seg;
            }
            return fallback;
        }

        private static ManagedSegment FindSegmentForU(ManagedLoLine line, int u)
        {
            ManagedSegment best = null;
            for (int i = 0; i < line.VisualSegments.Count; i++)
            {
                ManagedSegment seg = line.VisualSegments[i];
                if (seg.IsControl || seg.IsHidden || seg.IsNewline)
                    continue;
                best ??= seg;
                if (u < seg.UStart + seg.Dur)
                    return seg;
                best = seg;
            }
            return best;
        }

        private static int FindCpInSegmentAtU(ManagedSegment seg, int u)
        {
            if (seg.Cch <= 1 || seg.CharWidths == null || seg.IdObj == (ushort)TextStore.ObjectId.InlineObject)
                return seg.LscpFirst;

            bool segRtl = (seg.BidiLevel & 1) != 0;
            int offsetFromStart = segRtl ? (seg.UStart + seg.Dur - u) : (u - seg.UStart);
            if (offsetFromStart <= 0)
                return seg.LscpFirst;

            int acc = 0;
            for (int c = 0; c < seg.Cch; c++)
            {
                int w = c < seg.CharWidths.Length ? seg.CharWidths[c] : 0;
                if (offsetFromStart <= acc + w)
                    return seg.LscpFirst + c;
                acc += w;
            }
            return seg.LscpFirst + seg.Cch - 1;
        }

        private static void FillQueryForSegment(
            ManagedLoLine line,
            ManagedSegment seg,
            int lscp,
            int depthQueryMax,
            LsQSubInfo* subInfos,
            out int actualDepthQuery,
            out LsTextCell cell)
        {
            int c = Math.Clamp(lscp - seg.LscpFirst, 0, Math.Max(0, seg.Cch - 1));
            int cellStartCp = seg.LscpFirst + c;
            int cellEndCp = cellStartCp;
            int cellOffsetU = 0;
            int cellWidth = seg.Dur;

            if (seg.IdObj == (ushort)TextStore.ObjectId.InlineObject || seg.IsTab)
            {
                cellStartCp = seg.LscpFirst;
                cellEndCp = seg.LscpFirst + Math.Max(0, seg.Cch - 1);
                cellWidth = seg.Dur;
            }
            else if (seg.IsGlyphBased && seg.ClusterMap != null && seg.ClusterMap.Length == seg.Cch)
            {
                ushort targetCluster = seg.ClusterMap[c];
                int c0 = c;
                while (c0 > 0 && seg.ClusterMap[c0 - 1] == targetCluster) c0--;
                int c1 = c;
                while (c1 + 1 < seg.Cch && seg.ClusterMap[c1 + 1] == targetCluster) c1++;

                cellStartCp = seg.LscpFirst + c0;
                cellEndCp = seg.LscpFirst + c1;

                int acc = 0;
                if (seg.CharWidths != null)
                {
                    for (int k = 0; k < c0 && k < seg.CharWidths.Length; k++)
                        acc += seg.CharWidths[k];
                    cellWidth = 0;
                    for (int k = c0; k <= c1 && k < seg.CharWidths.Length; k++)
                        cellWidth += seg.CharWidths[k];
                }
                cellOffsetU = acc;
            }
            else if (seg.CharWidths != null)
            {
                int acc = 0;
                for (int k = 0; k < c && k < seg.CharWidths.Length; k++)
                    acc += seg.CharWidths[k];
                cellOffsetU = acc;
                cellWidth = c < seg.CharWidths.Length ? seg.CharWidths[c] : 0;
            }

            bool segRtl = (seg.TextFlow == LsTFlow.lstflowWS);
            int cellStartU = segRtl
                ? (seg.UStart + seg.Dur - cellOffsetU)
                : (seg.UStart + cellOffsetU);

            cell = new LsTextCell
            {
                lscpStartCell = cellStartCp,
                lscpEndCell = cellEndCp,
                pointUvStartCell = new LSPOINT(cellStartU, 0),
                dupCell = cellWidth,
                cCharsInCell = (cellEndCp - cellStartCp) + 1,
                cGlyphsInCell = seg.IsGlyphBased ? 1 : (cellEndCp - cellStartCp) + 1,
            };

            bool sameDirAsBase = (seg.TextFlow == line.BaseFlow);
            if (sameDirAsBase || depthQueryMax < 2)
            {
                actualDepthQuery = 1;
                subInfos[0] = new LsQSubInfo
                {
                    lstflowSubLine = sameDirAsBase ? line.BaseFlow : seg.TextFlow,
                    lscpFirstSubLine = line.CpFirst,
                    lsdcpSubLine = Math.Max(0, line.LscpLim - line.CpFirst),
                    pointUvStartSubLine = new LSPOINT(line.LineWidths.upStartMainText, 0),
                    dupSubLine = line.LineWidths.upLimLine - line.LineWidths.upStartMainText,
                    idobj = seg.IdObj,
                    plsrun = (IntPtr)seg.Plsrun,
                    lscpFirstRun = seg.LscpFirst,
                    lsdcpRun = seg.Cch,
                    pointUvStartRun = new LSPOINT(segRtl ? seg.UStart + seg.Dur : seg.UStart, 0),
                    dupRun = seg.Dur,
                    pointUvStartObj = new LSPOINT(segRtl ? seg.UStart + seg.Dur : seg.UStart, 0),
                    dupObj = seg.Dur,
                };
            }
            else
            {
                actualDepthQuery = 2;
                subInfos[0] = new LsQSubInfo
                {
                    lstflowSubLine = line.BaseFlow,
                    lscpFirstSubLine = line.CpFirst,
                    lsdcpSubLine = Math.Max(0, line.LscpLim - line.CpFirst),
                    pointUvStartSubLine = new LSPOINT(line.LineWidths.upStartMainText, 0),
                    dupSubLine = line.LineWidths.upLimLine - line.LineWidths.upStartMainText,
                    idobj = (ushort)TextStore.ObjectId.Reverse,
                    plsrun = (IntPtr)Plsrun.Reverse,
                    lscpFirstRun = seg.LscpFirst,
                    lsdcpRun = seg.Cch,
                    pointUvStartRun = new LSPOINT(seg.UStart, 0),
                    dupRun = seg.Dur,
                    pointUvStartObj = new LSPOINT(seg.UStart, 0),
                    dupObj = seg.Dur,
                };
                subInfos[1] = new LsQSubInfo
                {
                    lstflowSubLine = seg.TextFlow,
                    lscpFirstSubLine = seg.LscpFirst,
                    lsdcpSubLine = seg.Cch,
                    pointUvStartSubLine = new LSPOINT(segRtl ? seg.UStart + seg.Dur : seg.UStart, 0),
                    dupSubLine = seg.Dur,
                    idobj = seg.IdObj,
                    plsrun = (IntPtr)seg.Plsrun,
                    lscpFirstRun = seg.LscpFirst,
                    lsdcpRun = seg.Cch,
                    pointUvStartRun = new LSPOINT(segRtl ? seg.UStart + seg.Dur : seg.UStart, 0),
                    dupRun = seg.Dur,
                    pointUvStartObj = new LSPOINT(segRtl ? seg.UStart + seg.Dur : seg.UStart, 0),
                    dupObj = seg.Dur,
                };
            }
        }

        private static bool IsTrailingWhitespace(char ch)
            => ch == ' ' || ch == '\t' || ch == '\u00A0' || ch == '\u2002' || ch == '\u2003' || ch == '\u2009' || ch == '\u3000';

        #endregion
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Globalization;
using CSUI.Core.Interop.DirectX;

namespace MS.Internal.Text.TextInterface
{
    internal sealed unsafe class Font : IDisposable
    {
        private nint _font;
        private FontMetrics? _metrics;
        private double? _version;

        internal Font(void* font)
        {
            _font = (nint)font;
        }

        ~Font()
        {
            Dispose(false);
        }

        internal void* DWriteFontNoAddRef => (void*)_font;

        internal IntPtr DWriteFontAddRef
        {
            get
            {
                void* p = RequirePointer();
                ComHelpers.AddRef(p);
                return (IntPtr)p;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            ComHelpers.ReleaseAtomic(ref _font);
        }

        private void* RequirePointer()
        {
            void* p = (void*)_font;
            if (p == null)
                throw new ObjectDisposedException(nameof(Font));
            return p;
        }

        internal FontWeight Weight => (FontWeight)IDWriteFontVtbl.GetWeight(RequirePointer());
        internal FontStretch Stretch => (FontStretch)IDWriteFontVtbl.GetStretch(RequirePointer());
        internal FontStyle Style => (FontStyle)IDWriteFontVtbl.GetStyle(RequirePointer());
        internal FontSimulations SimulationFlags => (FontSimulations)IDWriteFontExVtbl.GetSimulations(RequirePointer());
        internal bool IsSymbolFont => IDWriteFontVtbl.IsSymbolFont(RequirePointer());

        internal FontMetrics Metrics
        {
            get
            {
                if (_metrics == null)
                {
                    IDWriteFontVtbl.GetMetrics(RequirePointer(), out DWRITE_FONT_METRICS native);
                    _metrics = new FontMetrics(native);
                }
                return _metrics;
            }
        }

        internal LocalizedStrings FaceNames
        {
            get
            {
                void* pNames = null;
                int hr = IDWriteFontExVtbl.GetFaceNames(RequirePointer(), &pNames);
                DWriteUtil.ConvertHresultToException(hr);
                return new LocalizedStrings(pNames);
            }
        }

        internal double Version
        {
            get
            {
                if (_version.HasValue)
                    return _version.Value;

                double parsed = 0.0;
                if (GetInformationalStrings(InformationalStringID.VersionStrings, out LocalizedStrings? strings) &&
                    strings != null)
                {
                    using (strings)
                    {
                        string? raw = null;
                        if (strings.FindLocaleName("en-us", out uint idx))
                        {
                            raw = strings.GetString(idx);
                        }
                        else if (strings.StringsCount > 0)
                        {
                            raw = strings.GetString(0);
                        }

                        if (!string.IsNullOrEmpty(raw))
                        {
                            parsed = ParseVersionString(raw);
                        }
                    }
                }

                _version = parsed;
                return parsed;
            }
        }

        private static double ParseVersionString(string versionString)
        {
            // OpenType NAME ID 5 strings typically look like "Version 5.20" or "Version 1.00;..."
            int start = -1;
            for (int i = 0; i < versionString.Length; i++)
            {
                if (versionString[i] >= '0' && versionString[i] <= '9')
                {
                    start = i;
                    break;
                }
            }

            if (start < 0)
                return 0.0;

            int end = start;
            bool seenDot = false;
            while (end < versionString.Length)
            {
                char ch = versionString[end];
                if (ch >= '0' && ch <= '9')
                {
                    end++;
                }
                else if (ch == '.' && !seenDot)
                {
                    seenDot = true;
                    end++;
                }
                else
                {
                    break;
                }
            }

            return double.TryParse(
                versionString.AsSpan(start, end - start),
                NumberStyles.AllowDecimalPoint,
                CultureInfo.InvariantCulture,
                out double result)
                ? result
                : 0.0;
        }

        internal FontMetrics DisplayMetrics(float emSize, float pixelsPerDip)
        {
            using FontFace fontFace = GetFontFace();
            int hr = IDWriteFontFaceVtbl.GetGdiCompatibleMetrics(
                fontFace.DWriteFontFaceNoAddRef,
                emSize,
                pixelsPerDip,
                null,
                out DWRITE_FONT_METRICS native);
            DWriteUtil.ConvertHresultToException(hr);
            return new FontMetrics(native);
        }

        internal bool GetInformationalStrings(InformationalStringID informationalStringID, out LocalizedStrings? informationalStrings)
        {
            void* pStrings = null;
            int hr = IDWriteFontExVtbl.GetInformationalStrings(
                RequirePointer(),
                (uint)informationalStringID,
                &pStrings,
                out bool exists);
            DWriteUtil.ConvertHresultToException(hr);

            if (!exists || pStrings == null)
            {
                if (pStrings != null)
                    ComHelpers.Release(pStrings);
                informationalStrings = null;
                return false;
            }

            informationalStrings = new LocalizedStrings(pStrings);
            return true;
        }

        internal bool HasCharacter(uint unicodeValue)
        {
            int hr = IDWriteFontVtbl.HasCharacter(RequirePointer(), unicodeValue, out bool exists);
            DWriteUtil.ConvertHresultToException(hr);
            return exists;
        }

        internal FontFace GetFontFace()
        {
            void* pFontFace = null;
            int hr = IDWriteFontExVtbl.CreateFontFace(RequirePointer(), &pFontFace);
            DWriteUtil.ConvertHresultToException(hr);
            return new FontFace(pFontFace);
        }

        internal FontFamily GetFontFamily()
        {
            void* pFamily = null;
            int hr = IDWriteFontExVtbl.GetFontFamily(RequirePointer(), &pFamily);
            DWriteUtil.ConvertHresultToException(hr);
            return new FontFamily(pFamily);
        }

        internal static void ResetFontFaceCache()
        {
            // No cached shared IDWriteFontFace wrappers are kept alive across layout passes;
            // each GetFontFace() call wraps an owned AddRef'd IDWriteFontFace released by the caller.
        }
    }
}

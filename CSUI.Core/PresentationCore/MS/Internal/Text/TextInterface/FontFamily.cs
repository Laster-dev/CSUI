// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using CSUI.Core.Interop.DirectX;

namespace MS.Internal.Text.TextInterface
{
    internal sealed unsafe class FontFamily : FontList
    {
        private string? _ordinalName;
        private FontMetrics? _metrics;

        internal FontFamily(void* fontFamily)
            : base(fontFamily)
        {
        }

        internal LocalizedStrings FamilyNames
        {
            get
            {
                void* pNames = null;
                int hr = IDWriteFontFamilyVtbl.GetFamilyNames(RequirePointer(), &pNames);
                DWriteUtil.ConvertHresultToException(hr);
                return new LocalizedStrings(pNames);
            }
        }

        internal string OrdinalName
        {
            get
            {
                if (_ordinalName == null)
                {
                    using LocalizedStrings names = FamilyNames;
                    if (names.StringsCount == 0)
                    {
                        _ordinalName = string.Empty;
                    }
                    else if (names.FindLocaleName("en-us", out uint idx))
                    {
                        _ordinalName = names.GetString(idx);
                    }
                    else
                    {
                        _ordinalName = names.GetString(0);
                    }
                }
                return _ordinalName;
            }
        }

        internal FontMetrics Metrics
        {
            get
            {
                if (_metrics == null)
                {
                    using Font regularFont = GetFirstMatchingFont(FontWeight.Normal, FontStretch.Normal, FontStyle.Normal);
                    _metrics = regularFont.Metrics;
                }
                return _metrics;
            }
        }

        internal FontMetrics DisplayMetrics(float emSize, float pixelsPerDip)
        {
            using Font regularFont = GetFirstMatchingFont(FontWeight.Normal, FontStretch.Normal, FontStyle.Normal);
            return regularFont.DisplayMetrics(emSize, pixelsPerDip);
        }

        internal Font GetFirstMatchingFont(FontWeight weight, FontStretch stretch, FontStyle style)
        {
            void* pFont = null;
            int hr = IDWriteFontFamilyVtbl.GetFirstMatchingFont(
                RequirePointer(),
                (uint)weight,
                (uint)stretch,
                (uint)style,
                &pFont);
            DWriteUtil.ConvertHresultToException(hr);
            return new Font(pFont);
        }

        internal FontList GetMatchingFonts(FontWeight weight, FontStretch stretch, FontStyle style)
        {
            void* pFonts = null;
            int hr = IDWriteFontFamilyExVtbl.GetMatchingFonts(
                RequirePointer(),
                (uint)weight,
                (uint)stretch,
                (uint)style,
                &pFonts);
            DWriteUtil.ConvertHresultToException(hr);
            return new FontList(pFonts);
        }
    }
}

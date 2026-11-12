// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System;

namespace System.Windows
{
    internal readonly struct PixelUnit
    {
        internal PixelUnit(string name, double factor)
        {
            Name = name;
            Factor = factor;
        }

        internal string Name { get; }
        internal double Factor { get; }

        internal static bool TryParsePixel(ReadOnlySpan<char> value, out PixelUnit pixelUnit)
        {
            if (value.EndsWith("px".AsSpan(), StringComparison.OrdinalIgnoreCase))
            {
                pixelUnit = new PixelUnit("px", 1.0);
                return true;
            }
            pixelUnit = default;
            return false;
        }

        internal static bool TryParsePixelPerInch(ReadOnlySpan<char> value, out PixelUnit pixelUnit)
        {
            if (value.EndsWith("in".AsSpan(), StringComparison.OrdinalIgnoreCase))
            {
                pixelUnit = new PixelUnit("in", 96.0);
                return true;
            }
            pixelUnit = default;
            return false;
        }

        internal static bool TryParsePixelPerCentimeter(ReadOnlySpan<char> value, out PixelUnit pixelUnit)
        {
            if (value.EndsWith("cm".AsSpan(), StringComparison.OrdinalIgnoreCase))
            {
                pixelUnit = new PixelUnit("cm", 96.0 / 2.54);
                return true;
            }
            pixelUnit = default;
            return false;
        }

        internal static bool TryParsePixelPerPoint(ReadOnlySpan<char> value, out PixelUnit pixelUnit)
        {
            if (value.EndsWith("pt".AsSpan(), StringComparison.OrdinalIgnoreCase))
            {
                pixelUnit = new PixelUnit("pt", 96.0 / 72.0);
                return true;
            }
            pixelUnit = default;
            return false;
        }
    }
}

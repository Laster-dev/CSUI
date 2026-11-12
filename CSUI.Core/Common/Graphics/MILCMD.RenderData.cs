// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

#nullable disable

namespace System.Windows.Media.Composition
{
    [StructLayout(LayoutKind.Explicit)]
    internal struct MilRectF
    {
        [FieldOffset(0)] internal float _left;
        [FieldOffset(4)] internal float _top;
        [FieldOffset(8)] internal float _right;
        [FieldOffset(12)] internal float _bottom;
    }

    /// <summary>
    /// Command opcodes used by RenderData instruction records.
    /// </summary>
    internal enum MILCMD
    {
        MilCmdInvalid = 0x00,
        MilCmdRenderData = 0x18,

        // Render Data Commands
        MilDrawLine = 0x3e,
        MilDrawLineAnimate = 0x3f,
        MilDrawRectangle = 0x40,
        MilDrawRectangleAnimate = 0x41,
        MilDrawRoundedRectangle = 0x42,
        MilDrawRoundedRectangleAnimate = 0x43,
        MilDrawEllipse = 0x44,
        MilDrawEllipseAnimate = 0x45,
        MilDrawGeometry = 0x46,
        MilDrawImage = 0x47,
        MilDrawImageAnimate = 0x48,
        MilDrawGlyphRun = 0x49,
        MilDrawDrawing = 0x4a,
        MilPushClip = 0x4d,
        MilPushOpacityMask = 0x4e,
        MilPushOpacity = 0x4f,
        MilPushOpacityAnimate = 0x50,
        MilPushTransform = 0x51,
        MilPushGuidelineSet = 0x52,
        MilPushGuidelineY1 = 0x53,
        MilPushGuidelineY2 = 0x54,
        MilPop = 0x56,
    }
}
